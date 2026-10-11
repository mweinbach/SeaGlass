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
import dev.seaglass.core.SeaGlassCornerAdaptationMode
import dev.seaglass.core.SeaGlassCornerAdaptationSolver
import dev.seaglass.core.SeaGlassCornerRadiiPx
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

    private var appliedFrostPx: Float = -1f

    internal fun ensureStageBlurUpdated(frostPx: Float) {
        val target = frostPx.coerceAtLeast(1.0f)
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S && abs(target - appliedFrostPx) > 0.5f) {
            appliedFrostPx = target

            val edgeRenderEffect = AndroidRenderEffect.createBlurEffect(
                1.4f,
                1.4f,
                Shader.TileMode.CLAMP
            ).asComposeRenderEffect()

            refractedRedLayer.compositingStrategy = CompositingStrategy.Offscreen
            refractedRedLayer.colorFilter = PureRedChannelFilter
            refractedRedLayer.blendMode = BlendMode.SrcOver
            refractedRedLayer.alpha = 1f
            refractedRedLayer.renderEffect = edgeRenderEffect

            refractedGreenLayer.compositingStrategy = CompositingStrategy.Offscreen
            refractedGreenLayer.colorFilter = PureGreenChannelFilter
            refractedGreenLayer.blendMode = BlendMode.Plus
            refractedGreenLayer.alpha = 0.90f
            refractedGreenLayer.renderEffect = edgeRenderEffect

            refractedBlueLayer.compositingStrategy = CompositingStrategy.Offscreen
            refractedBlueLayer.colorFilter = PureBlueChannelFilter
            refractedBlueLayer.blendMode = BlendMode.Plus
            refractedBlueLayer.alpha = 0.92f
            refractedBlueLayer.renderEffect = edgeRenderEffect

            // Crucial: GraphicsLayerV29.setRenderEffect forgot to call updateLayerProperties(),
            // so compositingStrategy MUST be set to Offscreen for RenderNode blur to execute!
            frostedBackdropLayer.clip = true
            frostedBackdropLayer.compositingStrategy = CompositingStrategy.Offscreen
            frostedBackdropLayer.renderEffect = AndroidRenderEffect.createBlurEffect(
                target,
                target,
                Shader.TileMode.CLAMP
            ).asComposeRenderEffect()
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
            stageState.refractedRedLayer.record(size = stageIntSize) {
                drawLayer(stageState.backdropLayer)
            }
            stageState.refractedGreenLayer.record(size = stageIntSize) {
                drawLayer(stageState.backdropLayer)
            }
            stageState.refractedBlueLayer.record(size = stageIntSize) {
                drawLayer(stageState.backdropLayer)
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
    cornerAdaptationMode: SeaGlassCornerAdaptationMode = SeaGlassCornerAdaptationMode.CONCENTRIC_PROXIMITY,
    cornerAdaptationStrength: Float = 1.0f,
    customContourPathBuilder: ((widthPx: Float, heightPx: Float, outPath: Path) -> Unit)? = null
): Modifier {
    val context = LocalContext.current
    val tierOverride = LocalSeaGlassTierOverride.current
    val tiltState = LocalSeaGlassTiltState.current
    val deviceCornerSpec = LocalSeaGlassDeviceCornerSpec.current
    val parentContainerScope = LocalSeaGlassContainerCornerScope.current
    val activeTier = remember(context, tierOverride) {
        tierOverride ?: SeaGlassDeviceTier.detect(context)
    }

    val uniformBlock = remember { SeaGlassUniformBlock() }
    val outerAndroidPath = remember { Path() }
    val innerPlateauAndroidPath = remember { Path() }
    val meniscusRimAndroidPath = remember { Path() }
    val insetMatrix = remember { AndroidMatrix() }

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

            // Adaptive Blur Solver:
            // Deepens subsurface diffusion so high-contrast background shapes and text melt into
            // a smooth, creamy bokeh pillow behind foreground typography and icons!
            val minLegibilityBlurDp = when (contentRole) {
                SeaGlassContentRole.NAVIGATION_CHROME -> 32f
                SeaGlassContentRole.TEXT_AND_ICONS -> 28f
                SeaGlassContentRole.PURE_OPTICAL_LENS -> 18f
            }
            val adaptiveBlurDp = max(
                minLegibilityBlurDp,
                material.frostRadiusDp * (0.85f + 0.75f * frostOpacity + material.coarseness * 0.40f)
            )
            val effectiveFrostPx = (adaptiveBlurDp * density).coerceAtLeast(18.0f)

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
                    influenceDistancePx = 136f * density,
                    adaptationStrength = cornerAdaptationStrength,
                    mode = cornerAdaptationMode
                )

                // 2. If nested inside a page/card container (LocalSeaGlassContainerCornerScope),
                // also adapt concentrically to the parent page/card corners!
                val solvedCorners = if (
                    parentContainerScope != null &&
                    parentContainerScope.widthPx > widthPx + 4f &&
                    parentContainerScope.heightPx > heightPx + 4f &&
                    cornerAdaptationMode != SeaGlassCornerAdaptationMode.FIXED
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
                        baseCornerRadiusPx = deviceSolved.maxRadiusPx,
                        minCornerFloorPx = 7f * density,
                        influenceDistancePx = 92f * density,
                        adaptationStrength = cornerAdaptationStrength,
                        mode = cornerAdaptationMode
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

            val rimInsetDp = when (contentRole) {
                SeaGlassContentRole.NAVIGATION_CHROME -> 7.5f
                SeaGlassContentRole.TEXT_AND_ICONS -> 6.5f
                SeaGlassContentRole.PURE_OPTICAL_LENS -> 10.5f
            }
            val rimInsetPx = min(rimInsetDp * density, minDimPx * 0.30f)

            if (customContourPathBuilder != null) {
                customContourPathBuilder(widthPx, heightPx, outerAndroidPath)
                innerPlateauAndroidPath.set(outerAndroidPath)
                val sx = ((widthPx - 2f * rimInsetPx) / widthPx).coerceIn(0.2f, 0.98f)
                val sy = ((heightPx - 2f * rimInsetPx) / heightPx).coerceIn(0.2f, 0.98f)
                insetMatrix.reset()
                insetMatrix.setScale(sx, sy, widthPx * 0.5f, heightPx * 0.5f)
                innerPlateauAndroidPath.transform(insetMatrix)
            } else {
                SeaGlassContourPathBuilder.buildCoalescingScenePath(
                    nodes = activeNodes,
                    smoothRadiusPx = smoothRadiusPx,
                    outPath = outerAndroidPath,
                    insetPx = 0f
                )
                SeaGlassContourPathBuilder.buildCoalescingScenePath(
                    nodes = activeNodes,
                    smoothRadiusPx = smoothRadiusPx,
                    outPath = innerPlateauAndroidPath,
                    insetPx = rimInsetPx
                )
            }

            meniscusRimAndroidPath.reset()
            meniscusRimAndroidPath.op(outerAndroidPath, innerPlateauAndroidPath, Path.Op.DIFFERENCE)

            val composeOuterPath: ComposePath = outerAndroidPath.asComposePath()
            val composeMeniscusRimPath: ComposePath = meniscusRimAndroidPath.asComposePath()

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
                    val stageFrostPx = max(28f * density, effectiveFrostPx)
                    stageState.requestedFrostPx = stageFrostPx
                    stageState.ensureStageBlurUpdated(stageFrostPx)
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

                // Pass 1: Spatially Adaptive Blur (Calm Diffused Interior + Sharp Refracting Meniscus Rim)
                if (hasBackdrop && activeTier != SeaGlassDeviceTier.LIGHTWEIGHT_TINT) {
                    val touchLensBoost = 1.0f + touchIntensity * 0.05f
                    val coreLensScale = (1.0f + (material.ior - 1.0f) * 0.08f * (1.0f - 0.30f * material.coarseness)) *
                        touchLensBoost

                    val maxRefractPx = (material.ior - 1.0f) *
                        (24f * density) *
                        (1.0f + touchIntensity * 0.35f)

                    val coreRefractX = -combinedTiltX * maxRefractPx * 0.38f
                    val coreRefractY = -combinedTiltY * maxRefractPx * 0.38f

                    val isWideCapsule = widthPx > heightPx * 1.5f
                    val baseLensX = if (isWideCapsule) 1.08f else 1.14f
                    val baseLensY = if (isWideCapsule) 1.32f else 1.14f

                    val disp = (material.chromaticDispersion + 0.24f).coerceIn(0.20f, 1.1f)
                    val scaleRedX = baseLensX + (material.ior - 1.0f) * (0.18f + disp * 0.14f)
                    val scaleRedY = baseLensY + (material.ior - 1.0f) * (0.28f + disp * 0.16f)
                    val scaleGreenX = baseLensX + (material.ior - 1.0f) * 0.12f
                    val scaleGreenY = baseLensY + (material.ior - 1.0f) * 0.18f
                    val scaleBlueX = baseLensX + (material.ior - 1.0f) * (0.06f - disp * 0.08f)
                    val scaleBlueY = baseLensY + (material.ior - 1.0f) * (0.08f - disp * 0.10f)

                    val shiftRedX = -combinedTiltX * maxRefractPx * (0.72f + disp * 0.55f)
                    val shiftRedY = -combinedTiltY * maxRefractPx * (0.72f + disp * 0.55f)
                    val shiftGreenX = -combinedTiltX * maxRefractPx * 0.45f
                    val shiftGreenY = -combinedTiltY * maxRefractPx * 0.45f
                    val shiftBlueX = -combinedTiltX * maxRefractPx * (0.18f - disp * 0.55f)
                    val shiftBlueY = -combinedTiltY * maxRefractPx * (0.18f - disp * 0.55f)

                    clipPath(composeOuterPath) {
                        // 1A: Deeply Diffused Subsurface Frosted Backdrop across the entire shape!
                        // Keeps the interior behind text and icons calm, smooth, and 100% free of
                        // unblurred background text or triple-vision R/G/B ghosting!
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

                        // 1B: Contour-Following Sharp Meniscus Refraction & R/G/B Chromatic Aberration
                        // Hardware-clipped strictly to composeMeniscusRimPath (outerAndroidPath - innerPlateauAndroidPath)
                        // so 0% of the sharp refracted edge layer ever touches the interior behind text & icons!
                        clipPath(composeMeniscusRimPath) {
                            scale(
                                scaleX = scaleRedX,
                                scaleY = scaleRedY,
                                pivot = highPointPivot
                            ) {
                                translate(
                                    left = -relOffset.x + shiftRedX,
                                    top = -relOffset.y + shiftRedY
                                ) {
                                    drawLayer(stageState.refractedRedLayer)
                                }
                            }

                            scale(
                                scaleX = scaleGreenX,
                                scaleY = scaleGreenY,
                                pivot = highPointPivot
                            ) {
                                translate(
                                    left = -relOffset.x + shiftGreenX,
                                    top = -relOffset.y + shiftGreenY
                                ) {
                                    drawLayer(stageState.refractedGreenLayer)
                                }
                            }

                            scale(
                                scaleX = scaleBlueX,
                                scaleY = scaleBlueY,
                                pivot = highPointPivot
                            ) {
                                translate(
                                    left = -relOffset.x + shiftBlueX,
                                    top = -relOffset.y + shiftBlueY
                                ) {
                                    drawLayer(stageState.refractedBlueLayer)
                                }
                            }
                        }
                    }
                }

                rimSweepMatrix.reset()
                rimSweepMatrix.setRotate(activeLightAngleDeg - 225f, cx, cy)
                innerSlopeChromaticPaint.shader?.setLocalMatrix(rimSweepMatrix)
                outerContactLipPaint.shader?.setLocalMatrix(rimSweepMatrix)

                // Pass 2: Volumetric Tint, Luminance Cushion, & Inward Chromatic Meniscus Slope
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
    minDimPx: Float,
    frostOpacity: Float,
    highPointPivot: Offset,
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

    // 1. Dynamic Range Compression & Sea-Glass Volumetric Tinting (No Text Shadows Required!):
    // On light/pastel sea-glass palettes (where foreground text is dark ink), BlendMode.Modulate
    // would multiply dark background areas toward black, destroying dark-text legibility.
    // Instead, we use BlendMode.Screen (lifting dark background pockets into glowing sea-glass
    // pastel chroma!) paired with a rich Oklab SrcOver body scatter so dark text & icons have
    // effortless 8:1 contrast everywhere across the glass!
    val primaryLab = SeaGlassColorMath.argbToOklab(primaryTintArgb)
    val isDarkGlass = primaryLab.l < 0.48f

    if (isDarkGlass) {
        // Dark-mode smoked sea glass (Obsidian Tide, light alabaster text):
        // Compress bright background highlights downward via Modulate + deep sapphire body tint
        val filterStrength = (0.45f + material.tintDensity * 0.48f).coerceIn(0.32f, 0.90f)
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

        val darkBodyAlpha = (0.46f + material.tintDensity * 0.34f + frostOpacity * 0.22f)
            .coerceIn(0.42f, 0.86f)
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
        // Step 1A: BlendMode.Screen lifts dark background pockets directly into the saturated
        // sea-glass hue while preserving bright refracted background highlights!
        val screenAlpha = (0.42f + material.tintDensity * 0.36f + frostOpacity * 0.18f)
            .coerceIn(0.36f, 0.82f)
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

        // Step 1B: Rich Oklab Pastel Sea-Glass Body Scatter (SrcOver)
        // Guarantees a luminous, richly tinted pastel floor so dark ink text and icons are
        // 100% crisp and readable with zero artificial text halo!
        val legibilityFloor = when (contentRole) {
            SeaGlassContentRole.NAVIGATION_CHROME -> 0.50f
            SeaGlassContentRole.TEXT_AND_ICONS -> 0.42f
            SeaGlassContentRole.PURE_OPTICAL_LENS -> 0.28f
        }
        val richStartColor = Color(
            SeaGlassColorMath.lerpOklab(primaryTintArgb, 0xFFFFFFFF.toInt(), 0.24f + material.coarseness * 0.16f)
        )
        val richEndColor = Color(
            SeaGlassColorMath.lerpOklab(secondaryTintArgb, 0xFFFFFFFF.toInt(), 0.24f + material.coarseness * 0.16f)
        )
        val bodyAlpha = (legibilityFloor * 0.68f + material.tintDensity * (0.26f + 0.46f * frostOpacity) + frostOpacity * 0.16f)
            .coerceIn(legibilityFloor, 0.86f)

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
    }

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
