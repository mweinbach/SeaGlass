package dev.seaglass.compose

import android.graphics.BlurMaskFilter
import android.graphics.Matrix as AndroidMatrix
import android.graphics.Paint as AndroidPaint
import android.graphics.Path
import android.graphics.RenderEffect as AndroidRenderEffect
import android.graphics.Shader
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
 *
 * By blurring [frostedBackdropLayer] and [refractedEdgeBackdropLayer] once at the full stage
 * coordinate origin `(0, 0, stageWidth, stageHeight)` rather than inside negatively-translated
 * per-card child `RenderNode`s, Android HWUI never crops or insets the blur at card boundaries—
 * guaranteeing **100% edge-to-edge transparent frosted & refracted glass** up to the exact pixel
 * border of every card, pill, and coalescing droplet.
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

            val edgeBlur = (target * 0.28f).coerceAtLeast(1.0f)
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
 * 1. **Zero Edge Cutoff / Full Edge-to-Edge Transparency**:
 *    - Samples from [SeaGlassStageState.frostedBackdropLayer] and
 *      [SeaGlassStageState.refractedEdgeBackdropLayer] (which are blurred at the full stage bounds),
 *      eliminating Skia `RenderNode` child crop margins at card borders.
 *    - [SeaGlassMaterial.transparency] (`0f..1f`) smoothly dials the frosted glass from dense
 *      tumbled sea glass (`0f`) to crystal-clear transparent glass (`1f`).
 * 2. **Liquid Glass Refracting Light & Chromatic Dispersion**:
 *    - Bends and magnifies the background image (`ior`) with an opposing chromatic-refraction
 *      pass and an additive (`BlendMode.Plus`) refractive light rim along the curved perimeter.
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
    val innerGlintAndroidPath = remember { Path() }

    val ambientShadowPaint = remember {
        AndroidPaint(AndroidPaint.ANTI_ALIAS_FLAG).apply {
            style = AndroidPaint.Style.FILL
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
            val cornerPx = min(material.cornerRadiusDp * density, min(widthPx, heightPx) * 0.5f)
            val thicknessPx = material.thicknessDp * density
            val smoothRadiusPx = smoothRadiusDp * density

            // Frost opacity is inverse of transparency (higher transparency = clearer see-through glass)
            val frostOpacity = (1.0f - material.transparency).coerceIn(0.03f, 0.96f)
            val effectiveFrostPx = (material.frostRadiusDp * density * (0.25f + 0.75f * frostOpacity))
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

            // Build exact C^2-continuous sminCubic outer contour and 1.8dp-inset chromatic glint contour
            SeaGlassContourPathBuilder.buildCoalescingScenePath(
                nodes = activeNodes,
                smoothRadiusPx = smoothRadiusPx,
                outPath = outerAndroidPath,
                insetPx = 0f
            )
            SeaGlassContourPathBuilder.buildCoalescingScenePath(
                nodes = activeNodes,
                smoothRadiusPx = smoothRadiusPx,
                outPath = innerGlintAndroidPath,
                insetPx = 1.8f * density
            )

            val composeOuterPath: ComposePath = outerAndroidPath.asComposePath()
            val composeInnerGlintPath: ComposePath = innerGlintAndroidPath.asComposePath()

            // Subtle ambient elevation shadow
            val shadowColorArgb = SeaGlassColorMath.lerpOklab(
                material.tintColorArgb,
                0xFF061524.toInt(),
                0.72f
            )
            val shadowAlpha = ((0.08f + frostOpacity * 0.12f) * 255f).toInt().coerceIn(10, 46)
            ambientShadowPaint.color = (shadowAlpha shl 24) or (shadowColorArgb and 0x00FFFFFF)
            ambientShadowPaint.maskFilter = BlurMaskFilter(9f * density, BlurMaskFilter.Blur.NORMAL)

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

                // Pack the backend-agnostic 256-byte std140 UBO block
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

                // Pass 0: Subtle ambient elevation shadow
                nCanvas.save()
                nCanvas.translate(0f, 4f * density)
                nCanvas.drawPath(outerAndroidPath, ambientShadowPaint)
                nCanvas.restore()

                // Pass 1: 100% Edge-to-Edge Continuous Frosted & Refracted Backdrop
                if (hasBackdrop && activeTier != SeaGlassDeviceTier.LIGHTWEIGHT_TINT) {
                    val lensScale = 1.0f +
                        (material.ior - 1.0f) * 0.08f * (1.0f - 0.35f * material.coarseness)
                    val refractShiftPx = (material.ior - 1.0f) *
                        (8f * density) *
                        (1.0f - 0.35f * material.coarseness)
                    val edgeRefractShiftPx = -refractShiftPx * (0.75f + material.chromaticDispersion * 0.55f)

                    clipPath(composeOuterPath) {
                        // Draw the full-stage frosted backdrop translated into local coordinates
                        scale(
                            scaleX = lensScale,
                            scaleY = lensScale,
                            pivot = Offset(widthPx * 0.5f, heightPx * 0.5f)
                        ) {
                            translate(
                                left = -relOffset.x + refractShiftPx,
                                top = -relOffset.y + refractShiftPx * 0.8f
                            ) {
                                drawLayer(stageState.frostedBackdropLayer)
                            }
                        }

                        // Blend in the crisper, opposing-refracted Liquid Glass light layer
                        // When transparency is high, more of this crisp refracting layer shines through!
                        val crispRefractWeight = (0.20f + material.transparency * 0.38f + (material.ior - 1.0f) * 0.22f)
                            .coerceIn(0.14f, 0.65f)
                        stageState.refractedEdgeBackdropLayer.alpha = crispRefractWeight
                        translate(
                            left = -relOffset.x + edgeRefractShiftPx,
                            top = -relOffset.y - edgeRefractShiftPx * 0.7f
                        ) {
                            drawLayer(stageState.refractedEdgeBackdropLayer)
                        }
                        stageState.refractedEdgeBackdropLayer.alpha = 1f
                    }
                }

                // Pass 2: Edge-to-Edge Transparent Sea-Glass Tint, Sand Coarseness, & Liquid Glass Refracting Rim
                drawContinuousLiquidSeaGlass(
                    composeOuterPath = composeOuterPath,
                    composeInnerGlintPath = composeInnerGlintPath,
                    outerAndroidPath = outerAndroidPath,
                    material = material,
                    activeNodes = activeNodes,
                    widthPx = widthPx,
                    heightPx = heightPx,
                    frostOpacity = frostOpacity,
                    sandGrainPaint = sandGrainPaint,
                    shaderLocalMatrix = shaderLocalMatrix
                )

                // Pass 3: Foreground UI content (icons, typography, children)
                drawContent()
            }
        }
}

private fun DrawScope.drawContinuousLiquidSeaGlass(
    composeOuterPath: ComposePath,
    composeInnerGlintPath: ComposePath,
    outerAndroidPath: Path,
    material: SeaGlassMaterial,
    activeNodes: List<SeaGlassSdfEngine.SdfNode>,
    widthPx: Float,
    heightPx: Float,
    frostOpacity: Float,
    sandGrainPaint: AndroidPaint,
    shaderLocalMatrix: AndroidMatrix
) {
    val primaryTintArgb = activeNodes.firstOrNull()?.tintColorArgb ?: material.tintColorArgb
    val secondaryTintArgb = if (activeNodes.size >= 2) {
        activeNodes[1].tintColorArgb
    } else {
        primaryTintArgb
    }

    // 1. Edge-to-Edge Transparent Volumetric Oklab Beer-Lambert Body Tint
    // Controlled by frostOpacity = (1 - material.transparency) so sliding "Frosted Glass Transparency"
    // smoothly dials from crystal-clear see-through glass to rich frosted sea glass!
    val transmittedStartArgb = SeaGlassColorMath.computeBeerLambertTransmission(
        backdropArgb = 0xFFF4FAFF.toInt(),
        tintArgb = primaryTintArgb,
        tintDensity = material.tintDensity,
        coarseness = material.coarseness,
        normalizedThickness = 0.85f
    )
    val transmittedEndArgb = SeaGlassColorMath.computeBeerLambertTransmission(
        backdropArgb = 0xFFDCECF8.toInt(),
        tintArgb = secondaryTintArgb,
        tintDensity = material.tintDensity,
        coarseness = material.coarseness,
        normalizedThickness = 1.20f
    )

    val startBodyColor = Color(transmittedStartArgb)
    val endBodyColor = Color(transmittedEndArgb)
    val bodyAlpha = (frostOpacity * (0.12f + material.tintDensity * 0.52f))
        .coerceIn(0.02f, 0.65f)

    drawPath(
        path = composeOuterPath,
        brush = Brush.linearGradient(
            colors = listOf(
                startBodyColor.copy(alpha = bodyAlpha * 0.88f),
                endBodyColor.copy(alpha = bodyAlpha)
            ),
            start = Offset.Zero,
            end = Offset(widthPx, heightPx)
        )
    )

    // 2. Continuous Edge-to-Edge Sand-Tumbled Micro-Facet Texture
    val grainScale = (density * 0.55f).coerceIn(0.8f, 2.2f)
    shaderLocalMatrix.reset()
    shaderLocalMatrix.setScale(grainScale, grainScale)
    sandGrainPaint.shader?.setLocalMatrix(shaderLocalMatrix)
    val grainAlpha = (material.coarseness * (0.22f + 0.78f * frostOpacity) * 205f)
        .toInt()
        .coerceIn(0, 210)

    if (grainAlpha > 4) {
        sandGrainPaint.alpha = grainAlpha
        val nCanvas = drawContext.canvas.nativeCanvas
        nCanvas.save()
        nCanvas.clipPath(outerAndroidPath)
        nCanvas.drawRect(0f, 0f, widthPx, heightPx, sandGrainPaint)
        nCanvas.restore()
    }

    // 3. Apple Liquid Glass Directional Refracting Light Rim
    // Uses BlendMode.Plus for the inner refractive sheen so it ADDS light photons to the background
    // colors passing through the edge rather than painting an opaque border!
    val windwardSpecColor = Color(
        SeaGlassColorMath.lerpOklab(
            primaryTintArgb,
            0xFFFFFFFF.toInt(),
            0.86f
        )
    )
    val prismaticCyan = Color(
        SeaGlassColorMath.lerpOklab(
            primaryTintArgb,
            0xFF60F4FF.toInt(),
            0.74f
        )
    )
    val prismaticAmberCoral = Color(
        SeaGlassColorMath.lerpOklab(
            secondaryTintArgb,
            0xFFFFAA84.toInt(),
            0.70f
        )
    )
    val leewardCausticColor = Color(
        SeaGlassColorMath.lerpOklab(
            secondaryTintArgb,
            0xFFFFFFFF.toInt(),
            0.56f
        )
    )

    val rimLight = (0.38f + material.rimChalkiness * 0.58f).coerceIn(0.20f, 0.96f)

    // 3a. Wide Additive Refracted Light Sheen inside the curved lip (BlendMode.Plus)
    clipPath(composeOuterPath) {
        drawPath(
            path = composeOuterPath,
            brush = Brush.linearGradient(
                colorStops = arrayOf(
                    0.00f to windwardSpecColor.copy(alpha = rimLight * 0.36f),
                    0.28f to prismaticCyan.copy(alpha = rimLight * 0.16f),
                    0.50f to Color.Transparent,
                    0.72f to prismaticAmberCoral.copy(alpha = rimLight * 0.16f),
                    1.00f to leewardCausticColor.copy(alpha = rimLight * 0.42f)
                ),
                start = Offset.Zero,
                end = Offset(widthPx, heightPx)
            ),
            style = Stroke(width = 4.2f * density),
            blendMode = BlendMode.Plus
        )
    }

    // 3b. Prismatic Chromatic-Refraction Inner Glint (BlendMode.Plus)
    drawPath(
        path = composeInnerGlintPath,
        brush = Brush.linearGradient(
            colorStops = arrayOf(
                0.00f to prismaticCyan.copy(alpha = rimLight * 0.48f),
                0.26f to prismaticCyan.copy(alpha = rimLight * 0.08f),
                0.50f to Color.Transparent,
                0.74f to prismaticAmberCoral.copy(alpha = rimLight * 0.08f),
                1.00f to prismaticAmberCoral.copy(alpha = rimLight * 0.52f)
            ),
            start = Offset.Zero,
            end = Offset(widthPx, heightPx)
        ),
        style = Stroke(width = 1.5f * density),
        blendMode = BlendMode.Plus
    )

    // 3c. Crisp 1.35dp Directional Specular-Refraction Lip
    drawPath(
        path = composeOuterPath,
        brush = Brush.linearGradient(
            colorStops = arrayOf(
                0.00f to Color.White.copy(alpha = (rimLight * 0.92f).coerceAtMost(0.96f)),
                0.24f to windwardSpecColor.copy(alpha = rimLight * 0.58f),
                0.50f to windwardSpecColor.copy(alpha = rimLight * 0.14f),
                0.76f to leewardCausticColor.copy(alpha = rimLight * 0.54f),
                1.00f to windwardSpecColor.copy(alpha = rimLight * 0.84f)
            ),
            start = Offset.Zero,
            end = Offset(widthPx, heightPx)
        ),
        style = Stroke(width = 1.35f * density)
    )
}
