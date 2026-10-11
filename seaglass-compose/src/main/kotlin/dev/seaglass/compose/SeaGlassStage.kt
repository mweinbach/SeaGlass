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
 * Exact 3-channel RGB spectral isolation filters ($M_R + M_G + M_B = \mathbf{I}_{\text{RGB}}$).
 * Pivoting the Red, Green, and Blue spectral magnification passes around the water-drop's
 * High-Point Crown ($\mathbf{p}_{\text{crown}}$) keeps displacement at 0 in the center while
 * stretching and separating R, G, and B radially towards the inside of the edges.
 */
private val PureRedChannelFilter: ColorFilter = ColorFilter.colorMatrix(
    ColorMatrix(
        floatArrayOf(
            1f, 0f, 0f, 0f, 0f,
            0f, 0f, 0f, 0f, 0f,
            0f, 0f, 0f, 0f, 0f,
            0f, 0f, 0f, 1f, 0f
        )
    )
)

private val PureGreenChannelFilter: ColorFilter = ColorFilter.colorMatrix(
    ColorMatrix(
        floatArrayOf(
            0f, 0f, 0f, 0f, 0f,
            0f, 1f, 0f, 0f, 0f,
            0f, 0f, 0f, 0f, 0f,
            0f, 0f, 0f, 1f, 0f
        )
    )
)

private val PureBlueChannelFilter: ColorFilter = ColorFilter.colorMatrix(
    ColorMatrix(
        floatArrayOf(
            0f, 0f, 0f, 0f, 0f,
            0f, 0f, 0f, 0f, 0f,
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

    var requestedFrostPx: Float by mutableFloatStateOf(48f)
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

            // Crisp lens layer (1.4px blur) so text and artwork passing under capsule rims
            // visibly stretch and refract with R/G/B chromatic dispersion!
            val edgeBlur = (target * 0.08f).coerceIn(1.0f, 3.5f)
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
 * Renders a **Top-Down 3D Water-Drop & Cylindrical Capsule SeaGlass** surface over [stageState]:
 *
 * 1. **High-Point Dome Crown + Cylindrical & Radial R/G/B Chromatic Dispersion**:
 *    - Wide capsules (`Mini-Player` & `Bottom Tab Bar`) apply vertical cylindrical magnification
 *      (`baseLensY = 1.24f`) pivoted at `highPointPivot` across `PureRedChannelFilter`,
 *      `PureGreenChannelFilter`, and `PureBlueChannelFilter`—stretching background headlines and
 *      artwork vertically near the top and bottom lips while separating R, G, and B towards the
 *      inside of the edges with zero rectangular mask seams.
 * 2. **Inward-Feathered Prismatic Meniscus Slope & Semicircular End-Cap Crescents**:
 *    - Clipped inside `outerAndroidPath`, draws a monotonic inward-feathered prismatic slope
 *      (`11.0dp`, `6.6dp`, `3.5dp` strokes) with boosted `0°` and `180°` semicircular end-cap
 *      meniscus crescents and a neutral `0.75dp` water-drop surface-tension contact lip.
 */
@Composable
fun Modifier.seaGlass(
    stageState: SeaGlassStageState?,
    material: SeaGlassMaterial,
    nodesOptional: List<SeaGlassSdfEngine.SdfNode>? = null,
    smoothRadiusDp: Float = 28f,
    touchPointInLocal: Offset? = null,
    touchIntensity: Float = 0f,
    customContourPathBuilder: ((widthPx: Float, heightPx: Float, outPath: Path) -> Unit)? = null
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
    val innerSlopeChromaticPaint = remember {
        AndroidPaint(AndroidPaint.ANTI_ALIAS_FLAG).apply {
            style = AndroidPaint.Style.STROKE
            strokeCap = AndroidPaint.Cap.ROUND
            strokeJoin = AndroidPaint.Join.ROUND
        }
    }
    val outerContactLipPaint = remember {
        AndroidPaint(AndroidPaint.ANTI_ALIAS_FLAG).apply {
            style = AndroidPaint.Style.STROKE
            strokeCap = AndroidPaint.Cap.ROUND
            strokeJoin = AndroidPaint.Join.ROUND
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
            val effectiveFrostPx = (material.frostRadiusDp * density * (0.30f + 0.70f * frostOpacity))
                .coerceAtLeast(2.0f)

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

            if (customContourPathBuilder != null) {
                customContourPathBuilder(widthPx, heightPx, outerAndroidPath)
            } else {
                SeaGlassContourPathBuilder.buildCoalescingScenePath(
                    nodes = activeNodes,
                    smoothRadiusPx = smoothRadiusPx,
                    outPath = outerAndroidPath,
                    insetPx = 0f
                )
            }

            val composeOuterPath: ComposePath = outerAndroidPath.asComposePath()

            val shadowColorArgb = SeaGlassColorMath.lerpOklab(
                material.tintColorArgb,
                0xFF04111E.toInt(),
                0.70f
            )
            val shadowAlpha = ((0.12f + frostOpacity * 0.14f) * 255f).toInt().coerceIn(14, 56)
            ambientShadowPaint.color = (shadowAlpha shl 24) or (shadowColorArgb and 0x00FFFFFF)
            ambientShadowPaint.maskFilter = BlurMaskFilter(12f * density, BlurMaskFilter.Blur.NORMAL)

            val rimLight = (0.46f + material.rimChalkiness * 0.54f).coerceIn(0.28f, 0.98f)
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
            val prismaticCyanRgb = 0x0032F4FF
            val prismaticCoralRgb = 0x00FF5C92
            val prismaticAmberRgb = 0x00FFB444
            val leewardCausticRgb = SeaGlassColorMath.lerpOklab(
                secondaryTintArgb,
                0xFFFFFFFF.toInt(),
                0.62f
            ) and 0x00FFFFFF

            fun argbWithAlpha(rgb: Int, aFloat: Float): Int {
                val a = (aFloat.coerceIn(0f, 1f) * 255f).toInt()
                return (a shl 24) or rgb
            }

            val cx = widthPx * 0.5f
            val cy = heightPx * 0.5f

            // 1. Inward-feathered Prismatic Chromatic Meniscus Slope (0.75dp..5.5dp inside outerAndroidPath)
            innerSlopeChromaticPaint.shader = AndroidSweepGradient(
                cx,
                cy,
                intArrayOf(
                    argbWithAlpha(prismaticCyanRgb, rimLight * 0.36f * dispersion),     // 0° Right Cap Crescent
                    argbWithAlpha(prismaticCoralRgb, rimLight * 0.68f * dispersion),    // 28° Inner Leeward Coral
                    argbWithAlpha(leewardCausticRgb, rimLight * 0.80f),                 // 45° Inner Leeward Caustic
                    argbWithAlpha(prismaticAmberRgb, rimLight * 0.64f * dispersion),    // 62° Inner Leeward Amber
                    argbWithAlpha(leewardCausticRgb, rimLight * 0.14f),                 // 90° Bottom
                    argbWithAlpha(windwardCrestRgb, 0.02f),                             // 135° Tangent
                    argbWithAlpha(prismaticCyanRgb, rimLight * 0.68f * dispersion),     // 180° Left Cap Crescent
                    argbWithAlpha(windwardCrestRgb, rimLight * 0.84f),                  // 225° Inner Windward Slope
                    argbWithAlpha(prismaticAmberRgb, rimLight * 0.56f * dispersion),    // 255° Inner Windward Amber
                    argbWithAlpha(windwardCrestRgb, 0.02f),                             // 315° Tangent
                    argbWithAlpha(prismaticCyanRgb, rimLight * 0.36f * dispersion)      // 360° Right Cap Crescent
                ),
                floatArrayOf(0.00f, 0.08f, 0.125f, 0.17f, 0.25f, 0.375f, 0.50f, 0.625f, 0.71f, 0.875f, 1.00f)
            )

            // 2. Neutral Water-Drop Surface-Tension Contact Lip (outermost 0.75dp inside clipPath)
            outerContactLipPaint.strokeWidth = 1.5f * density
            outerContactLipPaint.shader = AndroidSweepGradient(
                cx,
                cy,
                intArrayOf(
                    argbWithAlpha(0x00FFFFFF, rimLight * 0.74f),                        // 0° Right Cap Crescent
                    argbWithAlpha(leewardCausticRgb, rimLight * 0.76f),                 // 45° Bottom-Right Caustic
                    argbWithAlpha(leewardCausticRgb, rimLight * 0.22f),
                    argbWithAlpha(windwardCrestRgb, 0.04f),                             // 135° Tangent
                    argbWithAlpha(0x00FFFFFF, rimLight * 0.80f),                        // 180° Left Cap Crescent
                    argbWithAlpha(0x00FFFFFF, (rimLight * 0.95f).coerceAtMost(0.98f)),  // 225° Top-Left Specular Peak
                    argbWithAlpha(windwardCrestRgb, rimLight * 0.44f),
                    argbWithAlpha(windwardCrestRgb, 0.04f),                             // 315° Tangent
                    argbWithAlpha(0x00FFFFFF, rimLight * 0.74f)                         // 360° Right Cap Crescent
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

                val highPointPivot = touchPointInLocal ?: Offset(
                    cx + combinedTiltX * minDimPx * 0.18f,
                    cy + combinedTiltY * minDimPx * 0.18f
                )

                // Pass 1: Continuous Frosted Backdrop + Cylindrical & Radial R/G/B Chromatic Refraction
                if (hasBackdrop && activeTier != SeaGlassDeviceTier.LIGHTWEIGHT_TINT) {
                    val touchLensBoost = 1.0f + touchIntensity * 0.05f
                    val coreLensScale = (1.0f + (material.ior - 1.0f) * 0.10f * (1.0f - 0.30f * material.coarseness)) *
                        touchLensBoost

                    val maxRefractPx = (material.ior - 1.0f) *
                        (24f * density) *
                        (1.0f + touchIntensity * 0.35f)

                    val coreRefractX = -combinedTiltX * maxRefractPx * 0.42f
                    val coreRefractY = -combinedTiltY * maxRefractPx * 0.42f

                    val isWideCapsule = widthPx > heightPx * 1.5f
                    val baseLensX = if (isWideCapsule) 1.08f else 1.12f
                    val baseLensY = if (isWideCapsule) 1.24f else 1.12f

                    val disp = (material.chromaticDispersion + 0.24f).coerceIn(0.20f, 1.1f)
                    val scaleRedX = baseLensX + (material.ior - 1.0f) * (0.18f + disp * 0.14f)
                    val scaleRedY = baseLensY + (material.ior - 1.0f) * (0.26f + disp * 0.16f)
                    val scaleGreenX = baseLensX + (material.ior - 1.0f) * 0.12f
                    val scaleGreenY = baseLensY + (material.ior - 1.0f) * 0.16f
                    val scaleBlueX = baseLensX + (material.ior - 1.0f) * (0.06f - disp * 0.08f)
                    val scaleBlueY = baseLensY + (material.ior - 1.0f) * (0.06f - disp * 0.10f)

                    val shiftRedX = -combinedTiltX * maxRefractPx * (0.72f + disp * 0.55f)
                    val shiftRedY = -combinedTiltY * maxRefractPx * (0.72f + disp * 0.55f)
                    val shiftGreenX = -combinedTiltX * maxRefractPx * 0.45f
                    val shiftGreenY = -combinedTiltY * maxRefractPx * 0.45f
                    val shiftBlueX = -combinedTiltX * maxRefractPx * (0.18f - disp * 0.55f)
                    val shiftBlueY = -combinedTiltY * maxRefractPx * (0.18f - disp * 0.55f)

                    val crispRefractWeight = (0.28f + material.transparency * 0.44f + (material.ior - 1.0f) * 0.22f)
                        .coerceIn(0.22f, 0.72f)

                    clipPath(composeOuterPath) {
                        // 1A: Base Frosted Sea-Glass Subsurface Backdrop
                        scale(
                            scaleX = coreLensScale,
                            scaleY = coreLensScale,
                            pivot = highPointPivot
                        ) {
                            translate(
                                left = -relOffset.x + coreRefractX,
                                top = -relOffset.y + coreRefractY
                            ) {
                                drawLayer(stageState.frostedBackdropLayer)
                            }
                        }

                        // 1B: Continuous 3-Channel Cylindrical & Radial Chromatic Refraction
                        stageState.refractedEdgeBackdropLayer.colorFilter = PureRedChannelFilter
                        stageState.refractedEdgeBackdropLayer.blendMode = BlendMode.SrcOver
                        stageState.refractedEdgeBackdropLayer.alpha = crispRefractWeight * 0.58f
                        scale(
                            scaleX = scaleRedX,
                            scaleY = scaleRedY,
                            pivot = highPointPivot
                        ) {
                            translate(
                                left = -relOffset.x + shiftRedX,
                                top = -relOffset.y + shiftRedY
                            ) {
                                drawLayer(stageState.refractedEdgeBackdropLayer)
                            }
                        }

                        stageState.refractedEdgeBackdropLayer.colorFilter = PureGreenChannelFilter
                        stageState.refractedEdgeBackdropLayer.blendMode = BlendMode.Plus
                        stageState.refractedEdgeBackdropLayer.alpha = crispRefractWeight * 0.64f
                        scale(
                            scaleX = scaleGreenX,
                            scaleY = scaleGreenY,
                            pivot = highPointPivot
                        ) {
                            translate(
                                left = -relOffset.x + shiftGreenX,
                                top = -relOffset.y + shiftGreenY
                            ) {
                                drawLayer(stageState.refractedEdgeBackdropLayer)
                            }
                        }

                        stageState.refractedEdgeBackdropLayer.colorFilter = PureBlueChannelFilter
                        stageState.refractedEdgeBackdropLayer.blendMode = BlendMode.Plus
                        stageState.refractedEdgeBackdropLayer.alpha = crispRefractWeight * 0.68f
                        scale(
                            scaleX = scaleBlueX,
                            scaleY = scaleBlueY,
                            pivot = highPointPivot
                        ) {
                            translate(
                                left = -relOffset.x + shiftBlueX,
                                top = -relOffset.y + shiftBlueY
                            ) {
                                drawLayer(stageState.refractedEdgeBackdropLayer)
                            }
                        }

                        stageState.refractedEdgeBackdropLayer.colorFilter = null
                        stageState.refractedEdgeBackdropLayer.blendMode = BlendMode.SrcOver
                        stageState.refractedEdgeBackdropLayer.alpha = 1f
                    }
                }

                rimSweepMatrix.reset()
                rimSweepMatrix.setRotate(activeLightAngleDeg - 225f, cx, cy)
                innerSlopeChromaticPaint.shader?.setLocalMatrix(rimSweepMatrix)
                outerContactLipPaint.shader?.setLocalMatrix(rimSweepMatrix)

                // Pass 2: Volumetric Tint, End-Cap Meniscus Crescents, & Inward Chromatic Slope
                drawWaterDropSeaGlassOptics(
                    composeOuterPath = composeOuterPath,
                    outerAndroidPath = outerAndroidPath,
                    material = material,
                    activeNodes = activeNodes,
                    widthPx = widthPx,
                    heightPx = heightPx,
                    minDimPx = minDimPx,
                    frostOpacity = frostOpacity,
                    highPointPivot = highPointPivot,
                    touchPointInLocal = touchPointInLocal,
                    touchIntensity = touchIntensity,
                    innerSlopeChromaticPaint = innerSlopeChromaticPaint,
                    outerContactLipPaint = outerContactLipPaint,
                    sandGrainPaint = sandGrainPaint,
                    shaderLocalMatrix = shaderLocalMatrix
                )

                // Pass 3: Foreground UI content
                drawContent()
            }
        }
}

private fun DrawScope.drawWaterDropSeaGlassOptics(
    composeOuterPath: ComposePath,
    outerAndroidPath: Path,
    material: SeaGlassMaterial,
    activeNodes: List<SeaGlassSdfEngine.SdfNode>,
    widthPx: Float,
    heightPx: Float,
    minDimPx: Float,
    frostOpacity: Float,
    highPointPivot: Offset,
    touchPointInLocal: Offset?,
    touchIntensity: Float,
    innerSlopeChromaticPaint: AndroidPaint,
    outerContactLipPaint: AndroidPaint,
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
    val filterStrength = (0.34f + material.tintDensity * 0.52f).coerceIn(0.20f, 0.86f)
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
        SeaGlassColorMath.lerpOklab(primaryTintArgb, 0xFFFFFFFF.toInt(), 0.12f + material.coarseness * 0.12f)
    )
    val richEndColor = Color(
        SeaGlassColorMath.lerpOklab(secondaryTintArgb, 0xFFFFFFFF.toInt(), 0.12f + material.coarseness * 0.12f)
    )
    val bodyAlpha = (material.tintDensity * (0.18f + 0.48f * frostOpacity) + frostOpacity * 0.08f)
        .coerceIn(0.10f, 0.72f)

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

    // 2. Sand-Tumbled Micro-Facet Texture + Inward-Feathered Prismatic Meniscus Slope
    val grainScale = (density * 0.55f).coerceIn(0.8f, 2.2f)
    shaderLocalMatrix.reset()
    shaderLocalMatrix.setScale(grainScale, grainScale)
    sandGrainPaint.shader?.setLocalMatrix(shaderLocalMatrix)
    val grainAlpha = (material.coarseness * (0.24f + 0.76f * frostOpacity) * 200f)
        .toInt()
        .coerceIn(0, 205)

    val nCanvas = drawContext.canvas.nativeCanvas
    nCanvas.save()
    nCanvas.clipPath(outerAndroidPath)
    if (grainAlpha > 4) {
        sandGrainPaint.alpha = grainAlpha
        nCanvas.drawRect(0f, 0f, widthPx, heightPx, sandGrainPaint)
    }

    // Inward-feathered Prismatic Chromatic Meniscus Slope (0.75dp..5.5dp inside outerAndroidPath)
    innerSlopeChromaticPaint.strokeWidth = 11.0f * density
    innerSlopeChromaticPaint.alpha = 24
    nCanvas.drawPath(outerAndroidPath, innerSlopeChromaticPaint)

    innerSlopeChromaticPaint.strokeWidth = 6.6f * density
    innerSlopeChromaticPaint.alpha = 44
    nCanvas.drawPath(outerAndroidPath, innerSlopeChromaticPaint)

    innerSlopeChromaticPaint.strokeWidth = 3.5f * density
    innerSlopeChromaticPaint.alpha = 68
    nCanvas.drawPath(outerAndroidPath, innerSlopeChromaticPaint)

    // Clean, neutral Water-Drop Contact Lip on the outermost 0.75dp (clipped inside outerAndroidPath!)
    nCanvas.drawPath(outerAndroidPath, outerContactLipPaint)
    nCanvas.restore()

    // 3. Water-Drop High-Point Crown Reflection & Adaptive Touch-Point Caustic Wave (HDR Extended-sRGB)
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
        val crownRadius = (minDimPx * 0.58f).coerceAtLeast(16f)
        drawCircle(
            brush = Brush.radialGradient(
                colors = listOf(
                    Color.White.copy(alpha = 0.16f * material.rimChalkiness),
                    Color(primaryTintArgb).copy(alpha = 0.06f),
                    Color.Transparent
                ),
                center = highPointPivot,
                radius = crownRadius
            ),
            radius = crownRadius,
            center = highPointPivot,
            blendMode = BlendMode.Plus
        )

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
        }
    }
}
