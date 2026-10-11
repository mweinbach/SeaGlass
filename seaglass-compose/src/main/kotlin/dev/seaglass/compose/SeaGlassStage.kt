package dev.seaglass.compose

import android.graphics.BlurMaskFilter
import android.graphics.Matrix as AndroidMatrix
import android.graphics.Paint as AndroidPaint
import android.graphics.Path
import android.graphics.RenderEffect as AndroidRenderEffect
import android.graphics.Shader
import android.graphics.SweepGradient as AndroidSweepGradient
import android.os.Build
import androidx.compose.runtime.Composable
import androidx.compose.runtime.Stable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableFloatStateOf
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.drawWithCache
import androidx.compose.ui.draw.drawWithContent
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.BlendMode
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path as ComposePath
import androidx.compose.ui.graphics.asComposePath
import androidx.compose.ui.graphics.asComposeRenderEffect
import androidx.compose.ui.graphics.colorspace.ColorSpaces
import androidx.compose.ui.graphics.drawscope.DrawScope
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.graphics.drawscope.clipPath
import androidx.compose.ui.graphics.drawscope.scale
import androidx.compose.ui.graphics.drawscope.translate
import androidx.compose.ui.graphics.layer.GraphicsLayer
import androidx.compose.ui.graphics.layer.drawLayer
import androidx.compose.ui.graphics.nativeCanvas
import androidx.compose.ui.graphics.rememberGraphicsLayer
import androidx.compose.ui.layout.onGloballyPositioned
import androidx.compose.ui.layout.positionInRoot
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.IntSize
import dev.seaglass.core.SeaGlassColorMath
import dev.seaglass.core.SeaGlassContourPathBuilder
import dev.seaglass.core.SeaGlassDeviceTier
import dev.seaglass.core.SeaGlassMaterial
import dev.seaglass.core.SeaGlassSandGrainTexture
import dev.seaglass.core.SeaGlassSdfEngine
import dev.seaglass.core.backend.SeaGlassUniformBlock
import kotlin.math.abs
import kotlin.math.atan2
import kotlin.math.cos
import kotlin.math.max
import kotlin.math.min
import kotlin.math.sin
import kotlin.math.sqrt

/**
 * Coordinates zero-copy GPU backdrop capture and shared full-stage subsurface blur pyramids
 * between background content ([Modifier.seaGlassBackdrop]) and foreground SeaGlass UI elements
 * ([Modifier.seaGlass] / [SeaGlassFluidGroup]).
 */
@Stable
class SeaGlassStageState internal constructor(
    val backdropLayer: GraphicsLayer,
    val frostedBackdropLayer: GraphicsLayer,
    val refractedEdgeBackdropLayer: GraphicsLayer
) {
    var backdropOriginInRoot: Offset by mutableStateOf(Offset.Zero)
        internal set

    var backdropSize: IntSize by mutableStateOf(IntSize.Zero)
        internal set

    var hasRecordedBackdrop: Boolean by mutableStateOf(false)
        internal set

    var recordTick: Int by mutableIntStateOf(0)
        internal set

    var requestedFrostPx: Float by mutableFloatStateOf(44f)
        internal set

    private var appliedFrostPx: Float = -1f

    internal fun ensureStageBlurUpdated(frostPx: Float) {
        val target = frostPx.coerceAtLeast(1.0f)
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S && abs(target - appliedFrostPx) > 0.5f) {
            appliedFrostPx = target
            frostedBackdropLayer.renderEffect = AndroidRenderEffect.createBlurEffect(
                target,
                target,
                Shader.TileMode.CLAMP
            ).asComposeRenderEffect()

            val edgeBlur = (target * 0.22f).coerceAtLeast(1.0f)
            refractedEdgeBackdropLayer.renderEffect = AndroidRenderEffect.createBlurEffect(
                edgeBlur,
                edgeBlur,
                Shader.TileMode.CLAMP
            ).asComposeRenderEffect()
        }
    }
}

/**
 * Creates and remembers a [SeaGlassStageState] backed by shared hardware-accelerated [GraphicsLayer]s.
 */
@Composable
fun rememberSeaGlassStageState(): SeaGlassStageState {
    val rawLayer = rememberGraphicsLayer()
    val frostedLayer = rememberGraphicsLayer()
    val edgeLayer = rememberGraphicsLayer()
    return remember(rawLayer, frostedLayer, edgeLayer) {
        SeaGlassStageState(
            backdropLayer = rawLayer,
            frostedBackdropLayer = frostedLayer,
            refractedEdgeBackdropLayer = edgeLayer
        )
    }
}

/**
 * Captures the composable's draw hierarchy into [stageState]'s GPU [GraphicsLayer]s so
 * foreground [Modifier.seaGlass] elements can refract, frost, and tint it edge-to-edge in real time.
 */
fun Modifier.seaGlassBackdrop(stageState: SeaGlassStageState): Modifier {
    return this
        .onGloballyPositioned { coordinates ->
            stageState.backdropOriginInRoot = coordinates.positionInRoot()
            stageState.backdropSize = coordinates.size
        }
        .drawWithContent {
            val stageIntSize = IntSize(
                size.width.toInt().coerceAtLeast(1),
                size.height.toInt().coerceAtLeast(1)
            )
            stageState.backdropLayer.record(size = stageIntSize) {
                this@drawWithContent.drawContent()
            }
            stageState.ensureStageBlurUpdated(stageState.requestedFrostPx)
            stageState.frostedBackdropLayer.record(size = stageIntSize) {
                drawLayer(stageState.backdropLayer)
            }
            stageState.refractedEdgeBackdropLayer.record(size = stageIntSize) {
                drawLayer(stageState.backdropLayer)
            }
            stageState.hasRecordedBackdrop = true
            stageState.recordTick++
            drawLayer(stageState.backdropLayer)
        }
}

/**
 * Renders a 100% edge-to-edge transparent **Liquid Sea Glass** surface over [stageState] with:
 * 1. **Tilt-Adaptive Side Refraction**: Shifts the Snell's law lens refraction and chromatic
 *    edge-refraction layers dynamically around the sides of the glass based on 3D device tilt
 *    ([LocalSeaGlassTiltState]) and local finger contact ([touchPointInLocal]).
 * 2. **HDR Extended-sRGB Specular Headroom & Adaptive Tap Caustic Wave**:
 *    Uses `ColorSpaces.ExtendedSrgb` (`FP16` HDR values $> 1.0$) and additive `BlendMode.Plus`
 *    for the rotating angular specular rim and localized finger touch-point caustic flare.
 * 3. **Angular `SweepGradient` Optical Rim**: Gleams along the tilt-driven windward curve and
 *    opposite leeward caustic curve while tapering to near-zero at tangential flanks.
 */
@Composable
fun Modifier.seaGlass(
    stageState: SeaGlassStageState?,
    material: SeaGlassMaterial,
    nodesOptional: List<SeaGlassSdfEngine.SdfNode>? = null,
    smoothRadiusDp: Float = 28f,
    touchPointInLocal: Offset? = null,
    touchIntensity: Float = 0f
): Modifier {
    val context = LocalContext.current
    val tierOverride = LocalSeaGlassTierOverride.current
    val tiltState = LocalSeaGlassTiltState.current
    val activeTier = remember(context, tierOverride) {
        tierOverride ?: SeaGlassDeviceTier.detect(context)
    }

    val uniformBlock = remember { SeaGlassUniformBlock() }
    val outerAndroidPath = remember { Path() }

    val ambientShadowPaint = remember {
        AndroidPaint(AndroidPaint.ANTI_ALIAS_FLAG).apply {
            style = AndroidPaint.Style.FILL
        }
    }
    val innerRefractSheenPaint = remember {
        AndroidPaint(AndroidPaint.ANTI_ALIAS_FLAG).apply {
            style = AndroidPaint.Style.STROKE
        }
    }
    val outerSpecularLipPaint = remember {
        AndroidPaint(AndroidPaint.ANTI_ALIAS_FLAG).apply {
            style = AndroidPaint.Style.STROKE
        }
    }
    val sandGrainPaint = remember {
        AndroidPaint(AndroidPaint.ANTI_ALIAS_FLAG).apply {
            style = AndroidPaint.Style.FILL
            shader = SeaGlassSandGrainTexture.obtainShader()
        }
    }
    val shaderLocalMatrix = remember { AndroidMatrix() }
    val rimSweepMatrix = remember { AndroidMatrix() }

    var elementOriginInRoot by remember { mutableStateOf(Offset.Zero) }

    return this
        .onGloballyPositioned { coordinates ->
            elementOriginInRoot = coordinates.positionInRoot()
        }
        .drawWithCache {
            val widthPx = size.width.coerceAtLeast(1f)
            val heightPx = size.height.coerceAtLeast(1f)
            val minDimPx = min(widthPx, heightPx)
            val cornerPx = min(material.cornerRadiusDp * density, minDimPx * 0.5f)
            val thicknessPx = material.thicknessDp * density
            val smoothRadiusPx = smoothRadiusDp * density

            val frostOpacity = (1.0f - material.transparency).coerceIn(0.03f, 0.96f)
            val effectiveFrostPx = (material.frostRadiusDp * density * (0.28f + 0.72f * frostOpacity))
                .coerceAtLeast(1.5f)

            val activeNodes = if (!nodesOptional.isNullOrEmpty()) {
                nodesOptional
            } else {
                listOf(
                    SeaGlassSdfEngine.SdfNode(
                        centerX = widthPx * 0.5f,
                        centerY = heightPx * 0.5f,
                        halfWidth = widthPx * 0.5f,
                        halfHeight = heightPx * 0.5f,
                        uniformCornerRadius = cornerPx,
                        cornerSmoothing = material.cornerSmoothing,
                        tintColorArgb = material.tintColorArgb,
                        thicknessPx = thicknessPx
                    )
                )
            }

            SeaGlassContourPathBuilder.buildCoalescingScenePath(
                nodes = activeNodes,
                smoothRadiusPx = smoothRadiusPx,
                outPath = outerAndroidPath,
                insetPx = 0f
            )

            val composeOuterPath: ComposePath = outerAndroidPath.asComposePath()

            val shadowColorArgb = SeaGlassColorMath.lerpOklab(
                material.tintColorArgb,
                0xFF04111E.toInt(),
                0.72f
            )
            val shadowAlpha = ((0.12f + frostOpacity * 0.14f) * 255f).toInt().coerceIn(14, 58)
            ambientShadowPaint.color = (shadowAlpha shl 24) or (shadowColorArgb and 0x00FFFFFF)
            ambientShadowPaint.maskFilter = BlurMaskFilter(11f * density, BlurMaskFilter.Blur.NORMAL)

            val rimLight = (0.42f + material.rimChalkiness * 0.56f).coerceIn(0.24f, 0.98f)
            val primaryTintArgb = activeNodes.firstOrNull()?.tintColorArgb ?: material.tintColorArgb
            val secondaryTintArgb = if (activeNodes.size >= 2) {
                activeNodes[1].tintColorArgb
            } else {
                primaryTintArgb
            }

            val windwardCrestRgb = SeaGlassColorMath.lerpOklab(
                primaryTintArgb,
                0xFFFFFFFF.toInt(),
                0.88f
            ) and 0x00FFFFFF
            val prismaticCyanRgb = SeaGlassColorMath.lerpOklab(
                primaryTintArgb,
                0xFF72F8FF.toInt(),
                0.78f
            ) and 0x00FFFFFF
            val leewardCausticRgb = SeaGlassColorMath.lerpOklab(
                secondaryTintArgb,
                0xFFFFFFFF.toInt(),
                0.54f
            ) and 0x00FFFFFF
            val warmDispersionRgb = SeaGlassColorMath.lerpOklab(
                secondaryTintArgb,
                0xFFFFB894.toInt(),
                0.68f
            ) and 0x00FFFFFF

            fun argbWithAlpha(rgb: Int, aFloat: Float): Int {
                val a = (aFloat.coerceIn(0f, 1f) * 255f).toInt()
                return (a shl 24) or rgb
            }

            val cx = widthPx * 0.5f
            val cy = heightPx * 0.5f

            // Layer A: Soft Gaussian-feathered inner refractive sheen
            innerRefractSheenPaint.strokeWidth = 4.2f * density
            innerRefractSheenPaint.maskFilter = BlurMaskFilter(5.0f * density, BlurMaskFilter.Blur.NORMAL)
            innerRefractSheenPaint.shader = AndroidSweepGradient(
                cx,
                cy,
                intArrayOf(
                    argbWithAlpha(warmDispersionRgb, rimLight * 0.24f),
                    argbWithAlpha(leewardCausticRgb, rimLight * 0.56f),
                    argbWithAlpha(leewardCausticRgb, rimLight * 0.18f),
                    argbWithAlpha(primaryTintArgb and 0x00FFFFFF, 0.02f),
                    argbWithAlpha(prismaticCyanRgb, rimLight * 0.34f),
                    argbWithAlpha(windwardCrestRgb, rimLight * 0.72f),
                    argbWithAlpha(windwardCrestRgb, rimLight * 0.36f),
                    argbWithAlpha(primaryTintArgb and 0x00FFFFFF, 0.02f),
                    argbWithAlpha(warmDispersionRgb, rimLight * 0.24f)
                ),
                floatArrayOf(0.00f, 0.125f, 0.25f, 0.375f, 0.50f, 0.625f, 0.75f, 0.875f, 1.00f)
            )

            // Layer B: Razor-thin (0.95dp) directional specular lip
            outerSpecularLipPaint.strokeWidth = 0.95f * density
            outerSpecularLipPaint.shader = AndroidSweepGradient(
                cx,
                cy,
                intArrayOf(
                    argbWithAlpha(leewardCausticRgb, rimLight * 0.30f),
                    argbWithAlpha(leewardCausticRgb, rimLight * 0.78f),
                    argbWithAlpha(leewardCausticRgb, rimLight * 0.26f),
                    argbWithAlpha(windwardCrestRgb, rimLight * 0.04f),
                    argbWithAlpha(prismaticCyanRgb, rimLight * 0.44f),
                    argbWithAlpha(0x00FFFFFF, (rimLight * 0.95f).coerceAtMost(0.98f)),
                    argbWithAlpha(windwardCrestRgb, rimLight * 0.52f),
                    argbWithAlpha(windwardCrestRgb, rimLight * 0.04f),
                    argbWithAlpha(leewardCausticRgb, rimLight * 0.30f)
                ),
                floatArrayOf(0.00f, 0.125f, 0.25f, 0.375f, 0.50f, 0.625f, 0.75f, 0.875f, 1.00f)
            )

            onDrawWithContent {
                val tick = stageState?.recordTick ?: 0
                val hasBackdrop = stageState?.hasRecordedBackdrop == true && tick >= 0
                val relOffset = if (stageState != null) {
                    elementOriginInRoot - stageState.backdropOriginInRoot
                } else {
                    Offset.Zero
                }

                if (stageState != null) {
                    stageState.requestedFrostPx = effectiveFrostPx
                    stageState.ensureStageBlurUpdated(effectiveFrostPx)
                }

                // Combine 3D device tilt vector with local finger touch vector for adaptive refraction
                val touchNormX = if (touchPointInLocal != null && touchIntensity > 0.01f) {
                    ((touchPointInLocal.x - cx) / max(cx, 1f)).coerceIn(-1f, 1f) * touchIntensity * 0.55f
                } else {
                    0f
                }
                val touchNormY = if (touchPointInLocal != null && touchIntensity > 0.01f) {
                    ((touchPointInLocal.y - cy) / max(cy, 1f)).coerceIn(-1f, 1f) * touchIntensity * 0.55f
                } else {
                    0f
                }

                val combinedTiltX = (tiltState.tiltX + touchNormX).coerceIn(-1f, 1f)
                val combinedTiltY = (tiltState.tiltY + touchNormY).coerceIn(-1f, 1f)
                val activeLightAngleDeg = Math.toDegrees(
                    atan2(combinedTiltY.toDouble(), combinedTiltX.toDouble())
                ).toFloat()

                // Pack the backend-agnostic 256-byte std140 UBO block with live tilt light direction
                uniformBlock.pack(
                    widthPx = widthPx,
                    heightPx = heightPx,
                    backdropOffsetX = relOffset.x,
                    backdropOffsetY = relOffset.y,
                    material = material,
                    density = density,
                    nodes = activeNodes,
                    smoothRadiusPx = smoothRadiusPx,
                    lightDirX = combinedTiltX,
                    lightDirY = combinedTiltY,
                    lightDirZ = 0.52f
                )

                val nCanvas = drawContext.canvas.nativeCanvas

                // Pass 0: Tilt-responsive ambient elevation shadow (shifts opposite to device tilt!)
                nCanvas.save()
                nCanvas.translate(
                    -combinedTiltX * 4.5f * density,
                    (4.5f - combinedTiltY * 3.5f) * density
                )
                nCanvas.drawPath(outerAndroidPath, ambientShadowPaint)
                nCanvas.restore()

                // Pass 1: Tilt-Adaptive Side Refraction & Frosted Backdrop
                if (hasBackdrop && activeTier != SeaGlassDeviceTier.LIGHTWEIGHT_TINT) {
                    val touchLensBoost = 1.0f + touchIntensity * 0.045f
                    val lensScale = (1.0f + (material.ior - 1.0f) * 0.12f * (1.0f - 0.30f * material.coarseness)) *
                        touchLensBoost

                    val maxRefractPx = (material.ior - 1.0f) *
                        (24f * density) *
                        (1.0f - 0.25f * material.coarseness) *
                        (1.0f + touchIntensity * 0.35f)

                    // Side refraction vector shifts dynamically with 3D device tilt & touch!
                    val refractShiftX = -combinedTiltX * maxRefractPx
                    val refractShiftY = -combinedTiltY * maxRefractPx

                    // Opposing chromatic edge-refraction shift
                    val edgeScale = 1.0f + (material.ior - 1.0f) * 0.55f
                    val edgeRefractShiftX = combinedTiltX * maxRefractPx * (0.95f + material.chromaticDispersion * 0.65f)
                    val edgeRefractShiftY = combinedTiltY * maxRefractPx * (0.95f + material.chromaticDispersion * 0.65f)

                    clipPath(composeOuterPath) {
                        // 1A: Deeply frosted subsurface backdrop shifted by tilt
                        scale(
                            scaleX = lensScale,
                            scaleY = lensScale,
                            pivot = touchPointInLocal ?: Offset(cx, cy)
                        ) {
                            translate(
                                left = -relOffset.x + refractShiftX * 0.60f,
                                top = -relOffset.y + refractShiftY * 0.60f
                            ) {
                                drawLayer(stageState.frostedBackdropLayer)
                            }
                        }

                        // 1B: Crisp, opposing-displaced Liquid Glass side-refraction layer
                        val crispRefractWeight = (0.28f + material.transparency * 0.44f + (material.ior - 1.0f) * 0.24f)
                            .coerceIn(0.18f, 0.78f)
                        stageState.refractedEdgeBackdropLayer.alpha = crispRefractWeight
                        scale(
                            scaleX = 1.0f + (material.ior - 1.0f) * 0.07f * edgeScale.coerceIn(1f, 1.4f),
                            scaleY = 1.0f + (material.ior - 1.0f) * 0.07f * edgeScale.coerceIn(1f, 1.4f),
                            pivot = Offset(cx, cy)
                        ) {
                            translate(
                                left = -relOffset.x + edgeRefractShiftX,
                                top = -relOffset.y + edgeRefractShiftY
                            ) {
                                drawLayer(stageState.refractedEdgeBackdropLayer)
                            }
                        }
                        stageState.refractedEdgeBackdropLayer.alpha = 1f
                    }
                }

                // Rotate the SweepGradient rim shaders to match live device tilt + finger touch angle!
                rimSweepMatrix.reset()
                rimSweepMatrix.setRotate(activeLightAngleDeg - 225f, cx, cy)
                innerRefractSheenPaint.shader?.setLocalMatrix(rimSweepMatrix)
                outerSpecularLipPaint.shader?.setLocalMatrix(rimSweepMatrix)

                // Pass 2: Volumetric Tint, Sand Grit, Tilt-Adaptive Rim, & HDR Touch-Point Caustic Wave
                drawContinuousLiquidSeaGlass(
                    composeOuterPath = composeOuterPath,
                    outerAndroidPath = outerAndroidPath,
                    material = material,
                    activeNodes = activeNodes,
                    widthPx = widthPx,
                    heightPx = heightPx,
                    frostOpacity = frostOpacity,
                    combinedTiltX = combinedTiltX,
                    combinedTiltY = combinedTiltY,
                    touchPointInLocal = touchPointInLocal,
                    touchIntensity = touchIntensity,
                    innerRefractSheenPaint = innerRefractSheenPaint,
                    outerSpecularLipPaint = outerSpecularLipPaint,
                    sandGrainPaint = sandGrainPaint,
                    shaderLocalMatrix = shaderLocalMatrix
                )

                // Pass 3: Foreground UI content
                drawContent()
            }
        }
}

private fun DrawScope.drawContinuousLiquidSeaGlass(
    composeOuterPath: ComposePath,
    outerAndroidPath: Path,
    material: SeaGlassMaterial,
    activeNodes: List<SeaGlassSdfEngine.SdfNode>,
    widthPx: Float,
    heightPx: Float,
    frostOpacity: Float,
    combinedTiltX: Float,
    combinedTiltY: Float,
    touchPointInLocal: Offset?,
    touchIntensity: Float,
    innerRefractSheenPaint: AndroidPaint,
    outerSpecularLipPaint: AndroidPaint,
    sandGrainPaint: AndroidPaint,
    shaderLocalMatrix: AndroidMatrix
) {
    val primaryTintArgb = activeNodes.firstOrNull()?.tintColorArgb ?: material.tintColorArgb
    val secondaryTintArgb = if (activeNodes.size >= 2) {
        activeNodes[1].tintColorArgb
    } else {
        primaryTintArgb
    }

    val gradStart = if (activeNodes.size >= 2) {
        Offset(
            activeNodes.first().centerX - activeNodes.first().halfWidth * 0.55f,
            activeNodes.first().centerY
        )
    } else {
        Offset.Zero
    }
    val gradEnd = if (activeNodes.size >= 2) {
        Offset(
            activeNodes.last().centerX + activeNodes.last().halfWidth * 0.55f,
            activeNodes.last().centerY
        )
    } else {
        Offset(widthPx, heightPx)
    }

    // 1A. Spectral Optical Color-Filter Absorption (BlendMode.Modulate)
    val filterStrength = (0.36f + material.tintDensity * 0.54f).coerceIn(0.22f, 0.88f)
    val filterStartColor = Color(
        SeaGlassColorMath.lerpOklab(0xFFFFFFFF.toInt(), primaryTintArgb, filterStrength)
    )
    val filterEndColor = Color(
        SeaGlassColorMath.lerpOklab(0xFFFFFFFF.toInt(), secondaryTintArgb, filterStrength)
    )
    drawPath(
        path = composeOuterPath,
        brush = Brush.linearGradient(
            colors = listOf(filterStartColor, filterEndColor),
            start = gradStart,
            end = gradEnd
        ),
        blendMode = BlendMode.Modulate
    )

    // 1B. 100% Continuous Edge-to-Edge Frosted Pastel Body Scatter (SrcOver)
    val richStartColor = Color(
        SeaGlassColorMath.lerpOklab(primaryTintArgb, 0xFFFFFFFF.toInt(), 0.10f + material.coarseness * 0.10f)
    )
    val richEndColor = Color(
        SeaGlassColorMath.lerpOklab(secondaryTintArgb, 0xFFFFFFFF.toInt(), 0.10f + material.coarseness * 0.10f)
    )
    val bodyAlpha = (material.tintDensity * (0.20f + 0.52f * frostOpacity) + frostOpacity * 0.08f)
        .coerceIn(0.12f, 0.76f)

    drawPath(
        path = composeOuterPath,
        brush = Brush.linearGradient(
            colors = listOf(
                richStartColor.copy(alpha = bodyAlpha),
                richEndColor.copy(alpha = bodyAlpha)
            ),
            start = gradStart,
            end = gradEnd
        )
    )

    // 2. Continuous Edge-to-Edge Sand-Tumbled Micro-Facet Texture + Tilt-Rotated Angular Rim
    val grainScale = (density * 0.55f).coerceIn(0.8f, 2.2f)
    shaderLocalMatrix.reset()
    shaderLocalMatrix.setScale(grainScale, grainScale)
    sandGrainPaint.shader?.setLocalMatrix(shaderLocalMatrix)
    val grainAlpha = (material.coarseness * (0.26f + 0.74f * frostOpacity) * 205f)
        .toInt()
        .coerceIn(0, 210)

    val nCanvas = drawContext.canvas.nativeCanvas
    nCanvas.save()
    nCanvas.clipPath(outerAndroidPath)
    if (grainAlpha > 4) {
        sandGrainPaint.alpha = grainAlpha
        nCanvas.drawRect(0f, 0f, widthPx, heightPx, sandGrainPaint)
    }
    nCanvas.drawPath(outerAndroidPath, innerRefractSheenPaint)
    nCanvas.restore()

    // 3. HDR Extended-sRGB Specular Headroom & Adaptive Touch-Point Caustic Wave
    val hdrWhite = Color(
        red = 1.45f,
        green = 1.48f,
        blue = 1.52f,
        alpha = 1.0f,
        colorSpace = ColorSpaces.ExtendedSrgb
    )
    val baseTintSrgb = Color(primaryTintArgb)
    val hdrTintGlow = Color(
        red = (baseTintSrgb.red * 1.38f).coerceAtMost(1.6f),
        green = (baseTintSrgb.green * 1.38f).coerceAtMost(1.6f),
        blue = (baseTintSrgb.blue * 1.38f).coerceAtMost(1.6f),
        alpha = 1.0f,
        colorSpace = ColorSpaces.ExtendedSrgb
    )

    clipPath(composeOuterPath) {
        // 3a. Adaptive Touch-Point Liquid Glass Caustic Wave & Rim Flare (blooms under finger!)
        if (touchPointInLocal != null && touchIntensity > 0.01f) {
            val waveRadius = max(widthPx, heightPx) * (0.32f + 0.38f * touchIntensity)
            drawCircle(
                brush = Brush.radialGradient(
                    colors = listOf(
                        hdrWhite.copy(alpha = 0.44f * touchIntensity),
                        hdrTintGlow.copy(alpha = 0.28f * touchIntensity),
                        Color.Transparent
                    ),
                    center = touchPointInLocal,
                    radius = waveRadius
                ),
                radius = waveRadius,
                center = touchPointInLocal,
                blendMode = BlendMode.Plus
            )

            // Extra HDR rim flare along the border nearest the touch point
            drawPath(
                path = composeOuterPath,
                brush = Brush.radialGradient(
                    colors = listOf(
                        hdrWhite.copy(alpha = 0.72f * touchIntensity),
                        hdrTintGlow.copy(alpha = 0.35f * touchIntensity),
                        Color.Transparent
                    ),
                    center = touchPointInLocal,
                    radius = waveRadius * 0.95f
                ),
                style = Stroke(width = 3.2f * density),
                blendMode = BlendMode.Plus
            )
        }

        // 3b. Tilt-Driven HDR Windward Specular Crest & Leeward Internal Caustic Glint
        val crestX = widthPx * (0.5f + combinedTiltX * 0.45f)
        val crestY = heightPx * (0.5f + combinedTiltY * 0.45f)
        val causticX = widthPx * (0.5f - combinedTiltX * 0.45f)
        val causticY = heightPx * (0.5f - combinedTiltY * 0.45f)

        drawPath(
            path = composeOuterPath,
            brush = Brush.linearGradient(
                colorStops = arrayOf(
                    0.00f to hdrWhite.copy(alpha = 0.38f * material.rimChalkiness),
                    0.30f to Color.Transparent,
                    0.70f to Color.Transparent,
                    1.00f to hdrTintGlow.copy(alpha = 0.34f * material.rimChalkiness)
                ),
                start = Offset(crestX, crestY),
                end = Offset(causticX, causticY)
            ),
            style = Stroke(width = 2.2f * density),
            blendMode = BlendMode.Plus
        )
    }

    // 4. Razor-thin (0.95dp) tilt-rotated angular specular lip
    nCanvas.drawPath(outerAndroidPath, outerSpecularLipPaint)
}
