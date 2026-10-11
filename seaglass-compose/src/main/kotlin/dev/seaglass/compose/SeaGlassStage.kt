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
import kotlin.math.min

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
 * Renders a 100% edge-to-edge transparent **Liquid Sea Glass** surface over [stageState]:
 *
 * 1. **Continuous Edge-to-Edge Refraction & Volumetric Sea-Glass Tint**:
 *    - Samples from full-stage [SeaGlassStageState.frostedBackdropLayer] and
 *      [SeaGlassStageState.refractedEdgeBackdropLayer] with zero border crop.
 *    - Applies spectral Oklab color-filter absorption (`BlendMode.Modulate`) + frosted pastel
 *      body scatter (`BlendMode.SrcOver`) + toroidal sand-grain micro-facets.
 * 2. **Angular Optical Rim (Zero "Sticker Border" Look)**:
 *    - Instead of a uniform stroke drawn around all 4 sides, uses an angular `SweepGradient`
 *      refractive bevel that gleams along the top-left windward curve ($\sim 225^\circ$) and
 *      concentrates a colored internal caustic along the bottom-right curve ($\sim 45^\circ$),
 *      while **tapering to near-zero opacity (`0.03`) along the tangential flanks** so the edge
 *      dissolves organically into the refracted glass.
 */
@Composable
fun Modifier.seaGlass(
    stageState: SeaGlassStageState?,
    material: SeaGlassMaterial,
    nodesOptional: List<SeaGlassSdfEngine.SdfNode>? = null,
    smoothRadiusDp: Float = 28f
): Modifier {
    val context = LocalContext.current
    val tierOverride = LocalSeaGlassTierOverride.current
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

            // Frost opacity is inverse of transparency (higher transparency = clearer see-through glass)
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

            // Build exact C^2-continuous sminCubic outer contour
            SeaGlassContourPathBuilder.buildCoalescingScenePath(
                nodes = activeNodes,
                smoothRadiusPx = smoothRadiusPx,
                outPath = outerAndroidPath,
                insetPx = 0f
            )

            val composeOuterPath: ComposePath = outerAndroidPath.asComposePath()

            // Subtle ambient elevation shadow tinted with Oklab glass hue
            val shadowColorArgb = SeaGlassColorMath.lerpOklab(
                material.tintColorArgb,
                0xFF04111E.toInt(),
                0.72f
            )
            val shadowAlpha = ((0.12f + frostOpacity * 0.14f) * 255f).toInt().coerceIn(14, 58)
            ambientShadowPaint.color = (shadowAlpha shl 24) or (shadowColorArgb and 0x00FFFFFF)
            ambientShadowPaint.maskFilter = BlurMaskFilter(11f * density, BlurMaskFilter.Blur.NORMAL)

            // Angular SweepGradient Optical Rim:
            // In Android SweepGradient: 0° = Right, 45° = Bottom-Right (leeward internal caustic),
            // 90° = Bottom, 135° = Bottom-Left (tangent zero), 180° = Left,
            // 225° = Top-Left (windward specular crest), 270° = Top, 315° = Top-Right (tangent zero).
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

            // Layer A: Soft Gaussian-feathered inner refractive sheen (gives 3D lens bevel depth)
            innerRefractSheenPaint.strokeWidth = 4.2f * density
            innerRefractSheenPaint.maskFilter = BlurMaskFilter(5.0f * density, BlurMaskFilter.Blur.NORMAL)
            innerRefractSheenPaint.shader = AndroidSweepGradient(
                cx,
                cy,
                intArrayOf(
                    argbWithAlpha(warmDispersionRgb, rimLight * 0.24f),  // 0° Right
                    argbWithAlpha(leewardCausticRgb, rimLight * 0.54f),  // 45° Bottom-Right Caustic
                    argbWithAlpha(leewardCausticRgb, rimLight * 0.18f),  // 90° Bottom
                    argbWithAlpha(primaryTintArgb and 0x00FFFFFF, 0.02f),// 135° Bottom-Left Tangent (vanishes!)
                    argbWithAlpha(prismaticCyanRgb, rimLight * 0.32f),   // 180° Left
                    argbWithAlpha(windwardCrestRgb, rimLight * 0.68f),   // 225° Top-Left Windward Crest
                    argbWithAlpha(windwardCrestRgb, rimLight * 0.36f),   // 270° Top
                    argbWithAlpha(primaryTintArgb and 0x00FFFFFF, 0.02f),// 315° Top-Right Tangent (vanishes!)
                    argbWithAlpha(warmDispersionRgb, rimLight * 0.24f)   // 360° Right
                ),
                floatArrayOf(0.00f, 0.125f, 0.25f, 0.375f, 0.50f, 0.625f, 0.75f, 0.875f, 1.00f)
            )

            // Layer B: Razor-thin (0.95dp) directional specular lip that tapers to near-zero on tangents
            outerSpecularLipPaint.strokeWidth = 0.95f * density
            outerSpecularLipPaint.shader = AndroidSweepGradient(
                cx,
                cy,
                intArrayOf(
                    argbWithAlpha(leewardCausticRgb, rimLight * 0.30f),  // 0° Right
                    argbWithAlpha(leewardCausticRgb, rimLight * 0.76f),  // 45° Bottom-Right Caustic Crest
                    argbWithAlpha(leewardCausticRgb, rimLight * 0.26f),  // 90° Bottom
                    argbWithAlpha(windwardCrestRgb, rimLight * 0.04f),   // 135° Bottom-Left Tangent (almost 0!)
                    argbWithAlpha(prismaticCyanRgb, rimLight * 0.42f),   // 180° Left
                    argbWithAlpha(0x00FFFFFF, (rimLight * 0.94f).coerceAtMost(0.96f)), // 225° Top-Left Specular Peak
                    argbWithAlpha(windwardCrestRgb, rimLight * 0.52f),   // 270° Top
                    argbWithAlpha(windwardCrestRgb, rimLight * 0.04f),   // 315° Top-Right Tangent (almost 0!)
                    argbWithAlpha(leewardCausticRgb, rimLight * 0.30f)   // 360° Right
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

                uniformBlock.pack(
                    widthPx = widthPx,
                    heightPx = heightPx,
                    backdropOffsetX = relOffset.x,
                    backdropOffsetY = relOffset.y,
                    material = material,
                    density = density,
                    nodes = activeNodes,
                    smoothRadiusPx = smoothRadiusPx
                )

                val nCanvas = drawContext.canvas.nativeCanvas

                // Pass 0: Tinted ambient elevation shadow
                nCanvas.save()
                nCanvas.translate(0f, 4.5f * density)
                nCanvas.drawPath(outerAndroidPath, ambientShadowPaint)
                nCanvas.restore()

                // Pass 1: 100% Edge-to-Edge Frosted Backdrop + High-Refraction Liquid Meniscus Layer
                if (hasBackdrop && activeTier != SeaGlassDeviceTier.LIGHTWEIGHT_TINT) {
                    val lensScale = 1.0f +
                        (material.ior - 1.0f) * 0.12f * (1.0f - 0.30f * material.coarseness)
                    val refractShiftPx = (material.ior - 1.0f) *
                        (13f * density) *
                        (1.0f - 0.28f * material.coarseness)
                    val edgeRefractShiftPx = -refractShiftPx * (0.85f + material.chromaticDispersion * 0.65f)

                    clipPath(composeOuterPath) {
                        // 1A: Deeply frosted subsurface backdrop
                        scale(
                            scaleX = lensScale,
                            scaleY = lensScale,
                            pivot = Offset(widthPx * 0.5f, heightPx * 0.5f)
                        ) {
                            translate(
                                left = -relOffset.x + refractShiftPx * 0.55f,
                                top = -relOffset.y + refractShiftPx * 0.45f
                            ) {
                                drawLayer(stageState.frostedBackdropLayer)
                            }
                        }

                        // 1B: Crisp, opposing-displaced Liquid Glass refraction layer
                        val crispRefractWeight = (0.26f + material.transparency * 0.44f + (material.ior - 1.0f) * 0.24f)
                            .coerceIn(0.18f, 0.76f)
                        stageState.refractedEdgeBackdropLayer.alpha = crispRefractWeight
                        scale(
                            scaleX = 1.0f + (material.ior - 1.0f) * 0.06f,
                            scaleY = 1.0f + (material.ior - 1.0f) * 0.06f,
                            pivot = Offset(widthPx * 0.5f, heightPx * 0.5f)
                        ) {
                            translate(
                                left = -relOffset.x + edgeRefractShiftPx,
                                top = -relOffset.y - edgeRefractShiftPx * 0.75f
                            ) {
                                drawLayer(stageState.refractedEdgeBackdropLayer)
                            }
                        }
                        stageState.refractedEdgeBackdropLayer.alpha = 1f
                    }
                }

                // Pass 2: Seamless Edge-to-Edge Tint, Sand Coarseness, & Angular Optical Rim
                drawContinuousLiquidSeaGlass(
                    composeOuterPath = composeOuterPath,
                    outerAndroidPath = outerAndroidPath,
                    material = material,
                    activeNodes = activeNodes,
                    widthPx = widthPx,
                    heightPx = heightPx,
                    frostOpacity = frostOpacity,
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

    // 2. Continuous Edge-to-Edge Sand-Tumbled Micro-Facet Texture + Angular Optical Rim
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
    // Soft Gaussian-feathered angular refractive sheen inside the curved bevel
    nCanvas.drawPath(outerAndroidPath, innerRefractSheenPaint)
    nCanvas.restore()

    // Razor-thin (0.95dp) angular specular lip (gleams at 225°/45°, vanishes at 135°/315° tangents)
    nCanvas.drawPath(outerAndroidPath, outerSpecularLipPaint)
}
