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
import androidx.compose.ui.graphics.ColorFilter
import androidx.compose.ui.graphics.ColorMatrix
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
import kotlin.math.max
import kotlin.math.min

/**
 * Exact energy-conserving spectral channel-split filters ($M_{\text{Red+0.5G}} + M_{\text{0.5G+Blue}} = \mathbf{I}$)
 * used to render physical R / G / B Chromatic Aberration of the refracted backdrop without
 * blowing out backdrop luminance.
 */
private val ChromaticRedHalfGreenFilter: ColorFilter = ColorFilter.colorMatrix(
    ColorMatrix(
        floatArrayOf(
            1f, 0f, 0f, 0f, 0f,
            0f, 0.5f, 0f, 0f, 0f,
            0f, 0f, 0f, 0f, 0f,
            0f, 0f, 0f, 1f, 0f
        )
    )
)

private val ChromaticHalfGreenBlueFilter: ColorFilter = ColorFilter.colorMatrix(
    ColorMatrix(
        floatArrayOf(
            0f, 0f, 0f, 0f, 0f,
            0f, 0.5f, 0f, 0f, 0f,
            0f, 0f, 1f, 0f, 0f,
            0f, 0f, 0f, 1f, 0f
        )
    )
)

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

            val edgeBlur = (target * 0.16f).coerceIn(1.0f, 6.0f)
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
 *
 * 1. **Continuous R / G / B Chromatic Aberration & Tilt-Adaptive Refraction**:
 *    - Combines the subsurface [SeaGlassStageState.frostedBackdropLayer] with two opposing
 *      spectral rays (`ChromaticRedHalfGreenFilter` + `ChromaticHalfGreenBlueFilter` with
 *      `BlendMode.Plus`) on [SeaGlassStageState.refractedEdgeBackdropLayer], displaced dynamically
 *      by 3D device tilt ([LocalSeaGlassTiltState]) and finger contact ([touchPointInLocal]).
 * 2. **3D Micro-Stepped Prismatic Meniscus Bevel Rim**:
 *    - Uses an angular `SweepGradient` that disperses white light into **Prismatic Cyan (`#38F6FF`)
 *      → Specular White (`#FFFFFF`) → Warm Amber (`#FFB848`) → Coral-Magenta (`#FF5E96`)** around
 *      the windward and leeward curves while tapering to `0.02` alpha on the tangential flanks.
 *    - Rendered as a 3-layer micro-stepped 3D bevel (`4.8dp` inner cushion → `2.5dp` prismatic
 *      wall → `1.05dp` specular crest) clipped to `outerAndroidPath` with zero inner ring seams.
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

            val rimLight = (0.44f + material.rimChalkiness * 0.56f).coerceIn(0.24f, 0.98f)
            val dispersion = (material.chromaticDispersion + 0.22f).coerceIn(0.20f, 1.0f)
            val primaryTintArgb = activeNodes.firstOrNull()?.tintColorArgb ?: material.tintColorArgb
            val secondaryTintArgb = if (activeNodes.size >= 2) {
                activeNodes[1].tintColorArgb
            } else {
                primaryTintArgb
            }

            val windwardCrestRgb = SeaGlassColorMath.lerpOklab(
                primaryTintArgb,
                0xFFFFFFFF.toInt(),
                0.90f
            ) and 0x00FFFFFF
            val prismaticCyanRgb = 0x0038F6FF
            val prismaticCoralRgb = 0x00FF5E96
            val prismaticAmberRgb = 0x00FFB848
            val leewardCausticRgb = SeaGlassColorMath.lerpOklab(
                secondaryTintArgb,
                0xFFFFFFFF.toInt(),
                0.60f
            ) and 0x00FFFFFF

            fun argbWithAlpha(rgb: Int, aFloat: Float): Int {
                val a = (aFloat.coerceIn(0f, 1f) * 255f).toInt()
                return (a shl 24) or rgb
            }

            val cx = widthPx * 0.5f
            val cy = heightPx * 0.5f

            // Angular Prismatic SweepGradient shared across the 3D micro-stepped bevel strokes:
            // Splits white light into Cyan (195°) -> Specular White Crest (225°) -> Warm Amber (255°)
            // on the windward bevel, and Coral (28°) -> Caustic Peak (45°) -> Amber (62°) on the
            // leeward bevel, vanishing to 0.02 at the 135° and 315° tangents!
            outerSpecularLipPaint.strokeWidth = 1.05f * density
            outerSpecularLipPaint.shader = AndroidSweepGradient(
                cx,
                cy,
                intArrayOf(
                    argbWithAlpha(prismaticCyanRgb, rimLight * 0.36f * dispersion),     // 0° Right
                    argbWithAlpha(prismaticCoralRgb, rimLight * 0.72f * dispersion),    // 28° Leeward Coral Fringe
                    argbWithAlpha(leewardCausticRgb, rimLight * 0.86f),                 // 45° Bottom-Right Caustic Peak
                    argbWithAlpha(prismaticAmberRgb, rimLight * 0.66f * dispersion),    // 62° Leeward Amber Fringe
                    argbWithAlpha(leewardCausticRgb, rimLight * 0.22f),                 // 90° Bottom
                    argbWithAlpha(windwardCrestRgb, 0.02f),                             // 135° Bottom-Left Tangent (0!)
                    argbWithAlpha(prismaticCyanRgb, rimLight * 0.70f * dispersion),     // 195° Windward Cyan Fringe
                    argbWithAlpha(0x00FFFFFF, (rimLight * 0.96f).coerceAtMost(0.98f)),  // 225° Top-Left Specular Peak
                    argbWithAlpha(prismaticAmberRgb, rimLight * 0.62f * dispersion),    // 255° Windward Amber Fringe
                    argbWithAlpha(windwardCrestRgb, 0.02f),                             // 315° Top-Right Tangent (0!)
                    argbWithAlpha(prismaticCyanRgb, rimLight * 0.36f * dispersion)      // 360° Right
                ),
                floatArrayOf(0.00f, 0.08f, 0.125f, 0.17f, 0.25f, 0.375f, 0.54f, 0.625f, 0.71f, 0.875f, 1.00f)
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

                // Pass 0: Tilt-responsive ambient elevation shadow
                nCanvas.save()
                nCanvas.translate(
                    -combinedTiltX * 4.5f * density,
                    (4.5f - combinedTiltY * 3.5f) * density
                )
                nCanvas.drawPath(outerAndroidPath, ambientShadowPaint)
                nCanvas.restore()

                // Pass 1: Frosted Interior + Continuous R/G/B Chromatic Aberration Refraction
                if (hasBackdrop && activeTier != SeaGlassDeviceTier.LIGHTWEIGHT_TINT) {
                    val touchLensBoost = 1.0f + touchIntensity * 0.045f
                    val coreLensScale = (1.0f + (material.ior - 1.0f) * 0.10f * (1.0f - 0.30f * material.coarseness)) *
                        touchLensBoost

                    val maxRefractPx = (material.ior - 1.0f) *
                        (24f * density) *
                        (1.0f + touchIntensity * 0.35f)

                    val coreRefractX = -combinedTiltX * maxRefractPx * 0.45f
                    val coreRefractY = -combinedTiltY * maxRefractPx * 0.45f

                    val disp = (material.chromaticDispersion + 0.22f).coerceIn(0.18f, 1.1f)
                    val edgeScaleRed = 1.0f + (material.ior - 1.0f) * (0.16f + disp * 0.10f)
                    val edgeScaleBlue = 1.0f + (material.ior - 1.0f) * (0.06f - disp * 0.04f)

                    val edgeShiftRedX = combinedTiltX * maxRefractPx * (0.95f + disp * 0.75f)
                    val edgeShiftRedY = combinedTiltY * maxRefractPx * (0.95f + disp * 0.75f)
                    val edgeShiftBlueX = -combinedTiltX * maxRefractPx * (0.65f + disp * 0.75f)
                    val edgeShiftBlueY = -combinedTiltY * maxRefractPx * (0.65f + disp * 0.75f)

                    clipPath(composeOuterPath) {
                        // 1A: Subsurface Frosted Sea-Glass Backdrop
                        scale(
                            scaleX = coreLensScale,
                            scaleY = coreLensScale,
                            pivot = touchPointInLocal ?: Offset(cx, cy)
                        ) {
                            translate(
                                left = -relOffset.x + coreRefractX,
                                top = -relOffset.y + coreRefractY
                            ) {
                                drawLayer(stageState.frostedBackdropLayer)
                            }
                        }

                        // 1B: Continuous R / G / B Chromatic Aberration Refraction Layer
                        val crispRefractWeight = (0.24f + material.transparency * 0.42f + (material.ior - 1.0f) * 0.22f)
                            .coerceIn(0.16f, 0.70f)

                        // Ray 1: Red + 0.5 Green Chromatic Refraction
                        stageState.refractedEdgeBackdropLayer.colorFilter = ChromaticRedHalfGreenFilter
                        stageState.refractedEdgeBackdropLayer.blendMode = BlendMode.SrcOver
                        stageState.refractedEdgeBackdropLayer.alpha = crispRefractWeight * 0.58f
                        scale(
                            scaleX = edgeScaleRed,
                            scaleY = edgeScaleRed,
                            pivot = Offset(cx, cy)
                        ) {
                            translate(
                                left = -relOffset.x + edgeShiftRedX,
                                top = -relOffset.y + edgeShiftRedY
                            ) {
                                drawLayer(stageState.refractedEdgeBackdropLayer)
                            }
                        }

                        // Ray 2: 0.5 Green + Blue Chromatic Refraction (added via BlendMode.Plus!)
                        stageState.refractedEdgeBackdropLayer.colorFilter = ChromaticHalfGreenBlueFilter
                        stageState.refractedEdgeBackdropLayer.blendMode = BlendMode.Plus
                        stageState.refractedEdgeBackdropLayer.alpha = crispRefractWeight * 0.68f
                        scale(
                            scaleX = edgeScaleBlue,
                            scaleY = edgeScaleBlue,
                            pivot = Offset(cx, cy)
                        ) {
                            translate(
                                left = -relOffset.x + edgeShiftBlueX,
                                top = -relOffset.y + edgeShiftBlueY
                            ) {
                                drawLayer(stageState.refractedEdgeBackdropLayer)
                            }
                        }

                        stageState.refractedEdgeBackdropLayer.colorFilter = null
                        stageState.refractedEdgeBackdropLayer.blendMode = BlendMode.SrcOver
                        stageState.refractedEdgeBackdropLayer.alpha = 1f
                    }
                }

                // Rotate the SweepGradient rim shader to match live device tilt + finger touch angle
                rimSweepMatrix.reset()
                rimSweepMatrix.setRotate(activeLightAngleDeg - 225f, cx, cy)
                outerSpecularLipPaint.shader?.setLocalMatrix(rimSweepMatrix)

                // Pass 2: Volumetric Tint, 3D Micro-Stepped Prismatic Bevel Rim, & HDR Touch Wave
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

    // 1B. Continuous Edge-to-Edge Frosted Pastel Body Scatter (SrcOver)
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

    // 2. Sand-Tumbled Micro-Facet Texture + 3D Micro-Stepped Prismatic Meniscus Bevel
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

    // 3D Micro-Stepped Prismatic Meniscus Bevel (grades smoothly from 2.6dp inward to the outer lip!)
    outerSpecularLipPaint.strokeWidth = 5.2f * density
    outerSpecularLipPaint.alpha = 34
    nCanvas.drawPath(outerAndroidPath, outerSpecularLipPaint)

    outerSpecularLipPaint.strokeWidth = 2.8f * density
    outerSpecularLipPaint.alpha = 74
    nCanvas.drawPath(outerAndroidPath, outerSpecularLipPaint)
    nCanvas.restore()

    // 3. HDR Extended-sRGB Specular Headroom & Adaptive Touch-Point Caustic Wave
    val hdrWhite = Color(
        red = 1.48f,
        green = 1.50f,
        blue = 1.55f,
        alpha = 1.0f,
        colorSpace = ColorSpaces.ExtendedSrgb
    )
    val baseTintSrgb = Color(primaryTintArgb)
    val hdrTintGlow = Color(
        red = (baseTintSrgb.red * 1.40f).coerceAtMost(1.6f),
        green = (baseTintSrgb.green * 1.40f).coerceAtMost(1.6f),
        blue = (baseTintSrgb.blue * 1.40f).coerceAtMost(1.6f),
        alpha = 1.0f,
        colorSpace = ColorSpaces.ExtendedSrgb
    )

    clipPath(composeOuterPath) {
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

    // 4. Razor-crisp 1.05dp outer Prismatic + Specular Crest
    outerSpecularLipPaint.strokeWidth = 1.05f * density
    outerSpecularLipPaint.alpha = 255
    nCanvas.drawPath(outerAndroidPath, outerSpecularLipPaint)
}
