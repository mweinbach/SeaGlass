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
import androidx.compose.runtime.staticCompositionLocalOf
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
import androidx.compose.ui.graphics.layer.CompositingStrategy
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
import dev.seaglass.core.SeaGlassCornerAdaptationConfig
import dev.seaglass.core.SeaGlassCornerAdaptationMode
import dev.seaglass.core.SeaGlassCornerAdaptationSolver
import dev.seaglass.core.SeaGlassCornerMask
import dev.seaglass.core.SeaGlassCornerRadiiPx
import dev.seaglass.core.SeaGlassDeviceTier
import dev.seaglass.core.SeaGlassMaterial
import dev.seaglass.core.SeaGlassSandGrainTexture
import dev.seaglass.core.SeaGlassSdfEngine
import dev.seaglass.core.backend.SeaGlassShaderProgramCache
import dev.seaglass.core.backend.SeaGlassUniformBlock
import kotlin.math.abs
import kotlin.math.atan2
import kotlin.math.max
import kotlin.math.min
import kotlin.math.pow

/**
 * Semantic content role that drives **Adaptive Blur & Legibility Optics** on a SeaGlass surface.
 *
 * Instead of treating blur as a flat manual gauge, SeaGlass adapts its spatial blur profile,
 * luminance stabilization, and meniscus refraction width to the element's UI role:
 * - [TEXT_AND_ICONS]: Deepens interior subsurface diffusion (`>= 18dp` floor even at high
 *   transparency) and adds a soft interior luminance-stabilizing cushion so text and icons
 *   never collide with sharp background features, while keeping the outer `9dp` meniscus rim
 *   crystal-sharp and refractive.
 * - [NAVIGATION_CHROME]: Floating bars and mini-players (`SeaGlassFluidBar`, `SeaGlassMiniPlayerBar`)
 *   with strong cylindrical top/bottom edge stretching and high interior contrast stability.
 * - [PURE_OPTICAL_LENS]: Specimen droplets and decorative orbs where raw refraction and
 *   internal chromatic dispersion extend further inward.
 */
enum class SeaGlassContentRole {
    TEXT_AND_ICONS,
    NAVIGATION_CHROME,
    PURE_OPTICAL_LENS
}

/**
 * Exact 3-channel RGB spectral isolation filters ($M_R + M_G + M_B = \mathbf{I}_{\text{RGB}}$).
 */
private val InnerSlopeStopPositions = floatArrayOf(
    0.00f, 0.08f, 0.125f, 0.17f, 0.25f, 0.375f, 0.50f, 0.625f, 0.71f, 0.875f, 1.00f
)

private val OuterLipStopPositions = floatArrayOf(
    0.00f, 0.125f, 0.25f, 0.375f, 0.50f, 0.625f, 0.75f, 0.875f, 1.00f
)

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
 * Coordinates zero-copy GPU backdrop capture and shared full-stage adaptive blur pyramids
 * between background content ([Modifier.seaGlassBackdrop]) and foreground SeaGlass UI elements.
 */
@Stable
class SeaGlassStageState internal constructor(
    val backdropLayer: GraphicsLayer,
    val frostedBackdropLayer: GraphicsLayer,
    val refractedRedLayer: GraphicsLayer,
    val refractedGreenLayer: GraphicsLayer,
    val refractedBlueLayer: GraphicsLayer
) {
    val refractedEdgeBackdropLayer: GraphicsLayer
        get() = refractedRedLayer

    var backdropOriginInRoot: Offset by mutableStateOf(Offset.Zero)
        internal set

    var backdropSize: IntSize by mutableStateOf(IntSize.Zero)
        internal set

    var hasRecordedBackdrop: Boolean by mutableStateOf(false)
        internal set

    var recordTick: Int by mutableIntStateOf(0)
        internal set

    var requestedFrostPx: Float by mutableFloatStateOf(72f)
        internal set

    var needsDispersionLayers: Boolean = false
        internal set

    private var appliedFrostPx: Float = -1f
    private var appliedDispersionBlurPx: Float = -1f

    internal fun ensureStageBlurUpdated(frostPx: Float) {
        val target = frostPx.coerceAtLeast(1.0f)
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
            if (abs(target - appliedFrostPx) > 0.5f) {
                appliedFrostPx = target
                val cachedMainBlur = SeaGlassShaderProgramCache
                    .obtainBlurRenderEffect(target, Shader.TileMode.CLAMP)
                    .asComposeRenderEffect()

                frostedBackdropLayer.clip = true
                frostedBackdropLayer.compositingStrategy = CompositingStrategy.Auto
                frostedBackdropLayer.renderEffect = cachedMainBlur
                frostedBackdropLayer.compositingStrategy = CompositingStrategy.Offscreen
            }

            if (needsDispersionLayers) {
                val edgeTarget = (target * 0.74f).coerceAtLeast(1.0f)
                if (abs(edgeTarget - appliedDispersionBlurPx) > 0.5f) {
                    appliedDispersionBlurPx = edgeTarget
                    val edgeRenderEffect = SeaGlassShaderProgramCache
                        .obtainBlurRenderEffect(edgeTarget, Shader.TileMode.CLAMP)
                        .asComposeRenderEffect()

                    refractedRedLayer.compositingStrategy = CompositingStrategy.Auto
                    refractedRedLayer.colorFilter = PureRedChannelFilter
                    refractedRedLayer.blendMode = BlendMode.SrcOver
                    refractedRedLayer.alpha = 1f
                    refractedRedLayer.renderEffect = edgeRenderEffect
                    refractedRedLayer.compositingStrategy = CompositingStrategy.Offscreen

                    refractedGreenLayer.compositingStrategy = CompositingStrategy.Auto
                    refractedGreenLayer.colorFilter = PureGreenChannelFilter
                    refractedGreenLayer.blendMode = BlendMode.Plus
                    refractedGreenLayer.alpha = 1f
                    refractedGreenLayer.renderEffect = edgeRenderEffect
                    refractedGreenLayer.compositingStrategy = CompositingStrategy.Offscreen

                    refractedBlueLayer.compositingStrategy = CompositingStrategy.Auto
                    refractedBlueLayer.colorFilter = PureBlueChannelFilter
                    refractedBlueLayer.blendMode = BlendMode.Plus
                    refractedBlueLayer.alpha = 1f
                    refractedBlueLayer.renderEffect = edgeRenderEffect
                    refractedBlueLayer.compositingStrategy = CompositingStrategy.Offscreen
                }
            }
        }
    }
}

/**
 * Ambient [SeaGlassStageState] provided by `SeaGlassStageHost` or `SeaGlassSkipBridge.StageRoot`
 * so child Compose and Swift/Skip (`ComposeView`) elements can automatically sample the GPU
 * backdrop without manually threading `stageState` through every intermediate layer.
 */
val LocalSeaGlassStageState = staticCompositionLocalOf<SeaGlassStageState?> { null }

/**
 * Creates and remembers a [SeaGlassStageState] backed by shared hardware-accelerated [GraphicsLayer]s.
 */
@Composable
fun rememberSeaGlassStageState(): SeaGlassStageState {
    val rawLayer = rememberGraphicsLayer()
    val frostedLayer = rememberGraphicsLayer()
    val redLayer = rememberGraphicsLayer()
    val greenLayer = rememberGraphicsLayer()
    val blueLayer = rememberGraphicsLayer()
    return remember(rawLayer, frostedLayer, redLayer, greenLayer, blueLayer) {
        SeaGlassStageState(
            backdropLayer = rawLayer,
            frostedBackdropLayer = frostedLayer,
            refractedRedLayer = redLayer,
            refractedGreenLayer = greenLayer,
            refractedBlueLayer = blueLayer
        )
    }
}

/**
 * Captures the composable's draw hierarchy into [stageState]'s GPU [GraphicsLayer]s so
 * foreground [Modifier.seaGlass] elements can refract, frost, and tint it edge-to-edge in real time.
 *
 * Only records the 3 R/G/B dispersion layers when an active clear-glass surface requests
 * multi-channel chromatic dispersion ([SeaGlassStageState.needsDispersionLayers]), eliminating
 * 3 full-screen offscreen `RenderNode` passes per frame during normal sand-frosted rendering.
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
            if (stageState.needsDispersionLayers) {
                stageState.refractedRedLayer.record(size = stageIntSize) {
                    drawLayer(stageState.backdropLayer)
                }
                stageState.refractedGreenLayer.record(size = stageIntSize) {
                    drawLayer(stageState.backdropLayer)
                }
                stageState.refractedBlueLayer.record(size = stageIntSize) {
                    drawLayer(stageState.backdropLayer)
                }
            }
            stageState.hasRecordedBackdrop = true
            stageState.recordTick++
            drawLayer(stageState.backdropLayer)
        }
}

/**
 * Renders an **Adaptive Blur & High-Legibility Liquid Sea Glass** surface over [stageState]:
 *
 * 1. **Spatially Adaptive Blur (Calm Legible Interior + Sharp Refracting Meniscus Rim)**:
 *    - **Interior Content Zone (`> 9dp` from the edge, behind all text and icons)**:
 *      Renders 100% pure, deeply diffused [SeaGlassStageState.frostedBackdropLayer] (enforcing
 *      an adaptive legibility blur floor so sharp background text or edges never bleed behind
 *      foreground typography or icons, even when `transparency` is high!).
 *    - **Outer Curved Meniscus Bevel (`0dp..9dp` inward along `outerAndroidPath`)**:
 *      Uses a contour-following multi-step stroke alpha mask + `BlendMode.SrcIn` (`rimSave`)
 *      to composite the crystal-sharp, cylindrical/radially stretched 3-channel (`PureRed`,
 *      `PureGreen`, `PureBlue`) [SeaGlassStageState.refractedEdgeBackdropLayer] **only along
 *      the curved perimeter**, with zero rectangular corners and zero R/G/B ghosting behind text!
 * 2. **Adaptive Luminance & Contrast Cushion**:
 *    - Stabilizes backdrop luminance inside text/icon surfaces (`ambientAdaptation`) so both
 *      dark-ink and light-alabaster icons and labels pop with effortless legibility.
 */
@Composable
fun Modifier.seaGlass(
    stageState: SeaGlassStageState? = LocalSeaGlassStageState.current,
    material: SeaGlassMaterial = LocalSeaGlassPalette.current.surfaceGlass,
    nodesOptional: List<SeaGlassSdfEngine.SdfNode>? = null,
    smoothRadiusDp: Float = 28f,
    touchPointInLocal: Offset? = null,
    touchIntensity: Float = 0f,
    contentRole: SeaGlassContentRole = SeaGlassContentRole.TEXT_AND_ICONS,
    cornerAdaptationMode: SeaGlassCornerAdaptationMode = LocalSeaGlassCornerAdaptationConfig.current.mode,
    cornerMask: SeaGlassCornerMask = LocalSeaGlassCornerAdaptationConfig.current.corners,
    cornerProximityThresholdDp: Float = LocalSeaGlassCornerAdaptationConfig.current.proximityThresholdDp,
    cornerFullMatchDistanceDp: Float = LocalSeaGlassCornerAdaptationConfig.current.fullMatchDistanceDp,
    cornerAdaptationStrength: Float = LocalSeaGlassCornerAdaptationConfig.current.strength,
    allowAsymmetricCorners: Boolean = LocalSeaGlassCornerAdaptationConfig.current.allowAsymmetricCorners,
    adaptToParentContainer: Boolean = LocalSeaGlassCornerAdaptationConfig.current.adaptToParentContainer,
    customContourPathBuilder: ((widthPx: Float, heightPx: Float, outPath: Path) -> Unit)? = null
): Modifier {
    val context = LocalContext.current
    val tierOverride = LocalSeaGlassTierOverride.current
    val tiltState = LocalSeaGlassTiltState.current
    val deviceCornerSpec = LocalSeaGlassDeviceCornerSpec.current
    val parentContainerScope = LocalSeaGlassContainerCornerScope.current
    val activeTier = remember(context, tierOverride) {
        tierOverride ?: SeaGlassShaderProgramCache.cachedDeviceTier ?: SeaGlassDeviceTier.detect(context)
    }

    val uniformBlock = remember { SeaGlassUniformBlock() }
    val outerAndroidPath = remember { Path() }
    val slopeColors = remember { IntArray(11) }
    val lipColors = remember { IntArray(9) }

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

            // Unified Sand-Tumbled Coarseness Continuum in [0f..1f]:
            // 0f = completely clear untreated glass; 1f = heavily sand-tumbled velvet glass.
            val sandWear = material.effectiveSandWear

            val roleScale = when (contentRole) {
                SeaGlassContentRole.NAVIGATION_CHROME -> 1.12f
                SeaGlassContentRole.TEXT_AND_ICONS -> 1.05f
                SeaGlassContentRole.PURE_OPTICAL_LENS -> 0.82f
            }
            val adaptiveBlurDp = if (sandWear <= 0.01f) {
                0.5f
            } else {
                max(
                    sandWear * 26f * roleScale,
                    material.frostRadiusDp * (0.10f + 1.32f * sandWear)
                )
            }
            val effectiveFrostPx = (adaptiveBlurDp * density).coerceIn(1.0f, 64f * density)

            val activeNodes = if (!nodesOptional.isNullOrEmpty()) {
                nodesOptional
            } else {
                val stageW = (stageState?.backdropSize?.width?.toFloat()
                    ?: context.resources.displayMetrics.widthPixels.toFloat()).coerceAtLeast(1f)
                val stageH = (stageState?.backdropSize?.height?.toFloat()
                    ?: context.resources.displayMetrics.heightPixels.toFloat()).coerceAtLeast(1f)
                val stageOrigin = stageState?.backdropOriginInRoot ?: Offset.Zero
                val elemLeftInStage = elementOriginInRoot.x - stageOrigin.x
                val elemTopInStage = elementOriginInRoot.y - stageOrigin.y

                // 1. Solve concentric corner radii against physical device display corners
                // using developer-configured mode, cornerMask, and proximityThresholdDp.
                val deviceSolved = SeaGlassCornerAdaptationSolver.solveElementCornerRadiiPx(
                    elementLeftPx = elemLeftInStage,
                    elementTopPx = elemTopInStage,
                    elementRightPx = elemLeftInStage + widthPx,
                    elementBottomPx = elemTopInStage + heightPx,
                    containerWidthPx = stageW,
                    containerHeightPx = stageH,
                    containerCornersPx = deviceCornerSpec.toRadiiPx(),
                    baseCornerRadiusPx = cornerPx,
                    minCornerFloorPx = 8f * density,
                    influenceDistancePx = cornerProximityThresholdDp.coerceAtLeast(0f) * density,
                    fullMatchDistancePx = cornerFullMatchDistanceDp.coerceAtLeast(0f) * density,
                    adaptationStrength = cornerAdaptationStrength,
                    mode = cornerAdaptationMode,
                    cornerMask = cornerMask,
                    allowAsymmetricCorners = allowAsymmetricCorners
                )

                // 2. Only adapt to a parent container when explicitly requested (adaptToParentContainer
                // or FLUSH_CORNER_EMBED) so buttons, pills, and chips inside cards keep their own shape!
                val solvedCorners = if (
                    (adaptToParentContainer || cornerAdaptationMode == SeaGlassCornerAdaptationMode.FLUSH_CORNER_EMBED) &&
                    parentContainerScope != null &&
                    parentContainerScope.widthPx > widthPx + 4f &&
                    parentContainerScope.heightPx > heightPx + 4f &&
                    cornerAdaptationMode != SeaGlassCornerAdaptationMode.FIXED &&
                    !cornerMask.isNone
                ) {
                    val relLeft = elementOriginInRoot.x - parentContainerScope.originInRoot.x
                    val relTop = elementOriginInRoot.y - parentContainerScope.originInRoot.y
                    SeaGlassCornerAdaptationSolver.solveElementCornerRadiiPx(
                        elementLeftPx = relLeft,
                        elementTopPx = relTop,
                        elementRightPx = relLeft + widthPx,
                        elementBottomPx = relTop + heightPx,
                        containerWidthPx = parentContainerScope.widthPx,
                        containerHeightPx = parentContainerScope.heightPx,
                        containerCornersPx = parentContainerScope.cornerRadiiPx,
                        baseCornerRadiusPx = cornerPx,
                        minCornerFloorPx = 7f * density,
                        influenceDistancePx = cornerProximityThresholdDp.coerceAtLeast(0f) * density,
                        fullMatchDistancePx = cornerFullMatchDistanceDp.coerceAtLeast(0f) * density,
                        adaptationStrength = cornerAdaptationStrength,
                        mode = cornerAdaptationMode,
                        cornerMask = cornerMask,
                        allowAsymmetricCorners = allowAsymmetricCorners
                    )
                } else {
                    deviceSolved
                }

                listOf(
                    SeaGlassSdfEngine.SdfNode(
                        centerX = widthPx * 0.5f,
                        centerY = heightPx * 0.5f,
                        halfWidth = widthPx * 0.5f,
                        halfHeight = heightPx * 0.5f,
                        cornerTopLeft = solvedCorners.topLeftPx,
                        cornerTopRight = solvedCorners.topRightPx,
                        cornerBottomRight = solvedCorners.bottomRightPx,
                        cornerBottomLeft = solvedCorners.bottomLeftPx,
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
            val shadowAlpha = ((0.08f + sandWear * 0.14f) * 255f).toInt().coerceIn(10, 52)
            ambientShadowPaint.color = (shadowAlpha shl 24) or (shadowColorArgb and 0x00FFFFFF)
            ambientShadowPaint.maskFilter = SeaGlassShaderProgramCache.obtainBlurMaskFilter(11f * density)

            val rimLight = (0.36f + material.rimChalkiness * 0.52f).coerceIn(0.20f, 0.92f)
            // As sand coarseness increases, prism dispersion diffuses into a soft sand-frosted glass rim
            val effectiveDispersion = (material.chromaticDispersion * (1f - 0.72f * material.coarseness))
                .coerceIn(0.0f, 0.85f)
            val primaryTintArgb = activeNodes.firstOrNull()?.tintColorArgb ?: material.tintColorArgb
            val secondaryTintArgb = if (activeNodes.size >= 2) {
                activeNodes[1].tintColorArgb
            } else {
                primaryTintArgb
            }

            // Glass-authentic Oklab bevel colors (harmonized with the glass tint instead of neon cyan/pink)
            val windwardCrestRgb = SeaGlassColorMath.lerpOklab(
                primaryTintArgb,
                0xFFFFFFFF.toInt(),
                0.72f - 0.16f * material.coarseness
            ) and 0x00FFFFFF
            val leewardCausticRgb = SeaGlassColorMath.lerpOklab(
                secondaryTintArgb,
                0xFFFFFFFF.toInt(),
                0.42f - 0.12f * material.coarseness
            ) and 0x00FFFFFF
            val coolDispersionRgb = SeaGlassColorMath.lerpOklab(
                primaryTintArgb,
                0xFFB8F2FF.toInt(),
                0.35f * effectiveDispersion + 0.20f
            ) and 0x00FFFFFF
            val warmDispersionRgb = SeaGlassColorMath.lerpOklab(
                secondaryTintArgb,
                0xFFFFE4C4.toInt(),
                0.35f * effectiveDispersion + 0.20f
            ) and 0x00FFFFFF

            fun argbWithAlpha(rgb: Int, aFloat: Float): Int {
                val a = (aFloat.coerceIn(0f, 1f) * 255f).toInt()
                return (a shl 24) or rgb
            }

            val cx = widthPx * 0.5f
            val cy = heightPx * 0.5f

            // 1. Gaussian-Feathered Glass Bevel Slope: uses cached BlurMaskFilter and pre-allocated
            // stop arrays so bevel light tapers smoothly into the interior with zero step lines.
            innerSlopeChromaticPaint.strokeWidth = 8.0f * density
            innerSlopeChromaticPaint.maskFilter = SeaGlassShaderProgramCache.obtainBlurMaskFilter(6.5f * density)
            slopeColors[0] = argbWithAlpha(coolDispersionRgb, rimLight * 0.30f)
            slopeColors[1] = argbWithAlpha(warmDispersionRgb, rimLight * 0.36f)
            slopeColors[2] = argbWithAlpha(leewardCausticRgb, rimLight * 0.48f)
            slopeColors[3] = argbWithAlpha(warmDispersionRgb, rimLight * 0.32f)
            slopeColors[4] = argbWithAlpha(leewardCausticRgb, rimLight * 0.12f)
            slopeColors[5] = argbWithAlpha(windwardCrestRgb, 0.04f)
            slopeColors[6] = argbWithAlpha(coolDispersionRgb, rimLight * 0.38f)
            slopeColors[7] = argbWithAlpha(windwardCrestRgb, rimLight * 0.56f)
            slopeColors[8] = argbWithAlpha(warmDispersionRgb, rimLight * 0.30f)
            slopeColors[9] = argbWithAlpha(windwardCrestRgb, 0.04f)
            slopeColors[10] = argbWithAlpha(coolDispersionRgb, rimLight * 0.30f)
            innerSlopeChromaticPaint.shader = AndroidSweepGradient(cx, cy, slopeColors, InnerSlopeStopPositions)

            // 2. Soft Fresnel Edge Contact Lip (outermost 0.6dp inside clipPath)
            outerContactLipPaint.strokeWidth = 1.2f * density
            lipColors[0] = argbWithAlpha(windwardCrestRgb, rimLight * 0.44f)
            lipColors[1] = argbWithAlpha(leewardCausticRgb, rimLight * 0.48f)
            lipColors[2] = argbWithAlpha(leewardCausticRgb, rimLight * 0.16f)
            lipColors[3] = argbWithAlpha(windwardCrestRgb, 0.05f)
            lipColors[4] = argbWithAlpha(windwardCrestRgb, rimLight * 0.50f)
            lipColors[5] = argbWithAlpha(0x00FFFFFF, (rimLight * (0.64f - 0.18f * material.coarseness)).coerceIn(0.16f, 0.68f))
            lipColors[6] = argbWithAlpha(windwardCrestRgb, rimLight * 0.32f)
            lipColors[7] = argbWithAlpha(windwardCrestRgb, 0.05f)
            lipColors[8] = argbWithAlpha(windwardCrestRgb, rimLight * 0.44f)
            outerContactLipPaint.shader = AndroidSweepGradient(cx, cy, lipColors, OuterLipStopPositions)

            val dispPx = material.chromaticDispersion *
                (1.8f * density) *
                (1.0f - 0.82f * material.coarseness)
            val useDispersionChannels = dispPx > 0.35f && sandWear < 0.28f

            onDrawWithContent {
                val tick = stageState?.recordTick ?: 0
                val hasBackdrop = stageState?.hasRecordedBackdrop == true && tick >= 0
                val relOffset = if (stageState != null) {
                    elementOriginInRoot - stageState.backdropOriginInRoot
                } else {
                    Offset.Zero
                }

                if (stageState != null) {
                    if (useDispersionChannels) {
                        stageState.needsDispersionLayers = true
                    }
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
                    -combinedTiltX * 4.0f * density,
                    (4.0f - combinedTiltY * 3.0f) * density
                )
                nCanvas.drawPath(outerAndroidPath, ambientShadowPaint)
                nCanvas.restore()

                val highPointPivot = touchPointInLocal ?: Offset(
                    cx + combinedTiltX * minDimPx * 0.14f,
                    cy + combinedTiltY * minDimPx * 0.14f
                )

                // Pass 1: Continuous Glass Refraction & Sand-Diffused Subsurface Transmission
                // Evaluates refraction and diffusion seamlessly across the entire glass contour
                // with zero boolean inner-ring cutout so edges, corners, and interior form one
                // continuous piece of glass!
                if (hasBackdrop && activeTier != SeaGlassDeviceTier.LIGHTWEIGHT_TINT) {
                    val touchLensBoost = 1.0f + touchIntensity * 0.035f
                    val coreLensScale = (1.0f + (material.ior - 1.0f) * 0.055f * (1.0f - 0.45f * sandWear)) *
                        touchLensBoost

                    val maxRefractPx = (material.ior - 1.0f) *
                        (18f * density) *
                        (1.0f + touchIntensity * 0.28f) *
                        (1.0f - 0.42f * sandWear)

                    val coreRefractX = -combinedTiltX * maxRefractPx * 0.36f
                    val coreRefractY = -combinedTiltY * maxRefractPx * 0.36f

                    val dispPx = material.chromaticDispersion *
                        (1.8f * density) *
                        (1.0f - 0.82f * material.coarseness)

                    clipPath(composeOuterPath) {
                        if (dispPx > 0.35f && sandWear < 0.28f) {
                            val shiftRedX = coreRefractX - combinedTiltX * dispPx - dispPx * 0.35f
                            val shiftRedY = coreRefractY - combinedTiltY * dispPx - dispPx * 0.25f
                            val shiftBlueX = coreRefractX + combinedTiltX * dispPx + dispPx * 0.35f
                            val shiftBlueY = coreRefractY + combinedTiltY * dispPx + dispPx * 0.25f

                            scale(
                                scaleX = coreLensScale,
                                scaleY = coreLensScale,
                                pivot = highPointPivot
                            ) {
                                translate(
                                    left = -relOffset.x + shiftRedX,
                                    top = -relOffset.y + shiftRedY
                                ) {
                                    drawLayer(stageState.refractedRedLayer)
                                }
                                translate(
                                    left = -relOffset.x + coreRefractX,
                                    top = -relOffset.y + coreRefractY
                                ) {
                                    drawLayer(stageState.refractedGreenLayer)
                                }
                                translate(
                                    left = -relOffset.x + shiftBlueX,
                                    top = -relOffset.y + shiftBlueY
                                ) {
                                    drawLayer(stageState.refractedBlueLayer)
                                }
                            }
                        } else {
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
                        }
                    }
                }

                rimSweepMatrix.reset()
                rimSweepMatrix.setRotate(activeLightAngleDeg - 225f, cx, cy)
                innerSlopeChromaticPaint.shader?.setLocalMatrix(rimSweepMatrix)
                outerContactLipPaint.shader?.setLocalMatrix(rimSweepMatrix)

                // Pass 2: Volumetric Tint, Velvet Coarseness Continuum, & Feathered Corner/Edge Bevel
                drawWaterDropSeaGlassOptics(
                    composeOuterPath = composeOuterPath,
                    outerAndroidPath = outerAndroidPath,
                    material = material,
                    activeNodes = activeNodes,
                    widthPx = widthPx,
                    heightPx = heightPx,
                    sandWear = sandWear,
                    touchPointInLocal = touchPointInLocal,
                    touchIntensity = touchIntensity,
                    contentRole = contentRole,
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
    sandWear: Float,
    touchPointInLocal: Offset?,
    touchIntensity: Float,
    contentRole: SeaGlassContentRole,
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

    // 1. Continuous Volumetric Tinting & Velvet Subsurface Scatter:
    // - At sandWear == 0f (completely clear untreated glass): zero frosted wash, pure Beer-Lambert tint.
    // - As sandWear develops (0f -> 1f): velvet subsurface scatter increases smoothly in the glass's
    //   true Oklab hue (never bleaching the center toward white with depth!) up to effectively
    //   opaque velvet glass at sandWear == 1f that still transmits diffused ambient light.
    val primaryLab = SeaGlassColorMath.argbToOklab(primaryTintArgb)
    val isDarkGlass = primaryLab.l < 0.48f

    if (isDarkGlass) {
        val filterStrength = ((0.18f + material.tintDensity * 0.46f + sandWear * 0.28f) *
            (0.25f + 0.75f * max(sandWear, material.tintDensity))).coerceIn(0.04f, 0.88f)
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

        val darkBodyAlpha = (sandWear * 0.66f + material.tintDensity * (0.14f + 0.26f * sandWear))
            .coerceIn(0.02f, 0.90f)
        drawPath(
            path = composeOuterPath,
            brush = Brush.linearGradient(
                colors = listOf(
                    Color(primaryTintArgb).copy(alpha = darkBodyAlpha),
                    Color(secondaryTintArgb).copy(alpha = darkBodyAlpha)
                ),
                start = gradStart,
                end = gradEnd
            )
        )
    } else {
        // Light/pastel sea glass (Seafoam, Cobalt, Amber, Lavender, Rose, Kelp, Milk Glass):
        // Step 1A: Subtle Beer-Lambert / Screen subsurface lift (scaled by sandWear so clear glass
        // stays clear and frosted glass gently lifts dark background pockets without bleaching to white)
        val screenAlpha = (sandWear * 0.32f + material.tintDensity * 0.20f * (0.20f + 0.80f * sandWear))
            .coerceIn(0.0f, 0.50f)
        if (screenAlpha > 0.01f) {
            drawPath(
                path = composeOuterPath,
                brush = Brush.linearGradient(
                    colors = listOf(
                        Color(primaryTintArgb).copy(alpha = screenAlpha),
                        Color(secondaryTintArgb).copy(alpha = screenAlpha)
                    ),
                    start = gradStart,
                    end = gradEnd
                ),
                blendMode = BlendMode.Screen
            )
        }

        // Step 1B: True Oklab Sea-Glass Body Scatter (SrcOver)
        // Uses the authentic glass tint without whitewashing the center, scaling smoothly from
        // crystal-clear glass (sandWear = 0) to dense velvet glass (sandWear = 1).
        val roleWeight = when (contentRole) {
            SeaGlassContentRole.NAVIGATION_CHROME -> 1.06f
            SeaGlassContentRole.TEXT_AND_ICONS -> 1.00f
            SeaGlassContentRole.PURE_OPTICAL_LENS -> 0.84f
        }
        val bodyAlpha = ((sandWear * 0.68f + material.tintDensity * (0.12f + 0.26f * sandWear)) * roleWeight)
            .coerceIn(0.0f, 0.90f)

        if (bodyAlpha > 0.01f) {
            drawPath(
                path = composeOuterPath,
                brush = Brush.linearGradient(
                    colors = listOf(
                        Color(primaryTintArgb).copy(alpha = bodyAlpha),
                        Color(secondaryTintArgb).copy(alpha = bodyAlpha)
                    ),
                    start = gradStart,
                    end = gradEnd
                )
            )
        }
    }

    // 2. Velvet Sand-Tumbled Surface Texture + Multi-Step Feathered Corner/Edge Bevel
    // At coarseness == 0f (untreated clear glass), grainAlpha == 0. As sand abrasion increases,
    // the velvet micro-facet texture develops smoothly across the glass and its corners.
    val grainScale = (density * 0.42f).coerceIn(0.65f, 1.65f)
    shaderLocalMatrix.reset()
    shaderLocalMatrix.setScale(grainScale, grainScale)
    sandGrainPaint.shader?.setLocalMatrix(shaderLocalMatrix)
    val velvetProgress = material.coarseness.coerceIn(0f, 1f).pow(0.85f)
    val grainAlpha = (velvetProgress * (0.38f + 0.62f * sandWear) * 155f)
        .toInt()
        .coerceIn(0, 155)

    val nCanvas = drawContext.canvas.nativeCanvas
    nCanvas.save()
    nCanvas.clipPath(outerAndroidPath)
    if (grainAlpha > 2) {
        sandGrainPaint.alpha = grainAlpha
        nCanvas.drawRect(0f, 0f, widthPx, heightPx, sandGrainPaint)
    }

    // Gaussian-feathered glass bevel slope + soft contact lip: blends the edge and corners
    // smoothly into the interior with zero step ring or neon chromatic glare.
    innerSlopeChromaticPaint.alpha = 52
    nCanvas.drawPath(outerAndroidPath, innerSlopeChromaticPaint)
    nCanvas.drawPath(outerAndroidPath, outerContactLipPaint)
    nCanvas.restore()

    // 3. Subtle Tint-Harmonized Touch Compression Wave (only on active touch, never a white center spot!)
    if (touchPointInLocal != null && touchIntensity > 0.01f) {
        val baseTintSrgb = Color(primaryTintArgb)
        val hdrTintGlow = Color(
            red = (baseTintSrgb.red * 1.25f).coerceAtMost(1.35f),
            green = (baseTintSrgb.green * 1.25f).coerceAtMost(1.35f),
            blue = (baseTintSrgb.blue * 1.25f).coerceAtMost(1.35f),
            alpha = 1.0f,
            colorSpace = ColorSpaces.ExtendedSrgb
        )
        clipPath(composeOuterPath) {
            val waveRadius = max(widthPx, heightPx) * (0.30f + 0.34f * touchIntensity)
            drawCircle(
                brush = Brush.radialGradient(
                    colors = listOf(
                        hdrTintGlow.copy(alpha = 0.22f * touchIntensity),
                        Color(primaryTintArgb).copy(alpha = 0.10f * touchIntensity),
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
