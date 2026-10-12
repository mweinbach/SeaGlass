package dev.seaglass.compose

import android.graphics.BlurMaskFilter
import android.graphics.LinearGradient as AndroidLinearGradient
import android.graphics.Matrix as AndroidMatrix
import android.graphics.Paint as AndroidPaint
import android.graphics.Path
import android.graphics.PorterDuff
import android.graphics.PorterDuffXfermode
import android.graphics.RenderEffect as AndroidRenderEffect
import android.graphics.Shader
import android.graphics.SweepGradient as AndroidSweepGradient
import android.os.Build
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.BoxScope
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
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
private val BevelSlopeStopPositions = floatArrayOf(0.00f, 0.34f, 0.72f, 1.00f)

private val MidCrestStopPositions = floatArrayOf(0.00f, 0.46f, 1.00f)

private val OuterLipStopPositions = floatArrayOf(0.00f, 0.48f, 1.00f)

private const val AGSL_ETCHED_CONTENT_SHADER_SOURCE = """
    uniform shader content;
    uniform float2 uStepPx;
    uniform float4 uGlassTint;
    uniform float4 uLipColor;

    float sgInkMask(float2 coord, float isDark) {
        half4 s = content.eval(coord);
        float sa = float(s.a);
        if (sa <= 0.34) return 0.0;
        float3 sRgb = clamp(float3(s.rgb) / sa, 0.0, 1.0);
        float lum = dot(sRgb, float3(0.299, 0.587, 0.114));
        float inkGate = (isDark > 0.5)
            ? smoothstep(0.62, 0.80, lum)
            : (1.0 - smoothstep(0.30, 0.44, lum));
        return smoothstep(0.34, 0.70, sa) * inkGate;
    }

    half4 main(float2 fragCoord) {
        half4 c = content.eval(fragCoord);
        float a0 = float(c.a);
        float isDark = uGlassTint.a;

        float ink0 = sgInkMask(fragCoord, isDark);
        float inkTL = sgInkMask(fragCoord - uStepPx, isDark);
        float inkBR = sgInkMask(fragCoord + uStepPx, isDark);

        if (ink0 <= 0.004 && inkTL <= 0.004) {
            return c;
        }

        float3 baseRgb = (a0 > 0.01) ? clamp(float3(c.rgb) / a0, 0.0, 1.0) : uGlassTint.rgb;

        // 1. Inner top-left carved groove cliff (strictly inside the tonal stroke where top-left is outside)
        float innerCliff = ink0 * clamp(1.0 - inkTL, 0.0, 1.0);

        // 2. Inner bottom-right translucent glass bevel (strictly inside the tonal stroke where bottom-right is outside)
        float innerBevel = ink0 * clamp(1.0 - inkBR, 0.0, 1.0);

        // 3. Outer bottom-right frosted catch-light lip (just below/right of the tonal stroke)
        float outerLip = clamp(inkTL - ink0, 0.0, 1.0);

        // Groove floor blends tonal ink with frosted glass tint so strokes look carved into the glass
        float3 glassFloorRgb = mix(baseRgb, mix(uGlassTint.rgb, uLipColor.rgb, 0.42), (isDark > 0.5) ? 0.10 : 0.18);
        float3 cliffRgb = (isDark > 0.5) ? float3(0.01, 0.03, 0.06) : baseRgb * 0.36;
        float3 innerHighlightRgb = mix(baseRgb, uLipColor.rgb, (isDark > 0.5) ? 0.32 : 0.44);

        float3 etchedInkRgb = mix(glassFloorRgb, cliffRgb, clamp(innerCliff * 0.88, 0.0, 0.92));
        etchedInkRgb = mix(etchedInkRgb, innerHighlightRgb, clamp(innerBevel * 0.56, 0.0, 0.65));

        // Blend etched ink on ink pixels, preserving non-ink pixels (such as inlay pillows) untouched
        float3 combinedRgb = mix(baseRgb, etchedInkRgb, clamp(ink0 * 1.15, 0.0, 1.0));

        // Add crisp directional bottom-right frosted catch-light lip
        float lipStrength = (isDark > 0.5) ? 0.50 : 0.88;
        float lipAlpha = clamp(outerLip * lipStrength, 0.0, 0.92);

        float outAlpha = clamp(a0 + lipAlpha * (1.0 - a0), 0.0, 1.0);
        float3 outRgb = mix(combinedRgb, uLipColor.rgb, lipAlpha * (1.0 - ink0));
        return half4(half3(outRgb * outAlpha), half(outAlpha));
    }
"""

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
    val etchedReliefLayer: GraphicsLayer,
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

                val reliefBlurPx = (target * 0.24f).coerceIn(2.0f, 8.5f)
                val cachedReliefBlur = SeaGlassShaderProgramCache
                    .obtainBlurRenderEffect(reliefBlurPx, Shader.TileMode.CLAMP)
                    .asComposeRenderEffect()
                etchedReliefLayer.clip = true
                etchedReliefLayer.compositingStrategy = CompositingStrategy.Auto
                etchedReliefLayer.alpha = 0.24f
                etchedReliefLayer.renderEffect = cachedReliefBlur
                etchedReliefLayer.compositingStrategy = CompositingStrategy.Offscreen
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
    val etchedLayer = rememberGraphicsLayer()
    val redLayer = rememberGraphicsLayer()
    val greenLayer = rememberGraphicsLayer()
    val blueLayer = rememberGraphicsLayer()
    return remember(rawLayer, frostedLayer, etchedLayer, redLayer, greenLayer, blueLayer) {
        SeaGlassStageState(
            backdropLayer = rawLayer,
            frostedBackdropLayer = frostedLayer,
            etchedReliefLayer = etchedLayer,
            refractedRedLayer = redLayer,
            refractedGreenLayer = greenLayer,
            refractedBlueLayer = blueLayer
        )
    }
}

/**
 * Turnkey root stage container for Jetpack Compose apps: automatically records [background]
 * into the zero-copy GPU [SeaGlassStageState] and provides [LocalSeaGlassStageState] to all
 * descendants inside [foreground] so any SeaGlass component works out of the box without
 * manually threading `stageState`.
 */
@Composable
fun SeaGlassStageHost(
    modifier: Modifier = Modifier,
    stageState: SeaGlassStageState = rememberSeaGlassStageState(),
    background: @Composable BoxScope.() -> Unit,
    foreground: @Composable BoxScope.(SeaGlassStageState) -> Unit
) {
    CompositionLocalProvider(LocalSeaGlassStageState provides stageState) {
        Box(modifier = modifier.fillMaxSize()) {
            Box(
                modifier = Modifier
                    .fillMaxSize()
                    .seaGlassBackdrop(stageState),
                content = background
            )
            foreground(stageState)
        }
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
            stageState.etchedReliefLayer.record(size = stageIntSize) {
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
    val etchedContentLayer = rememberGraphicsLayer()
    val etchedRuntimeShader = remember {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            try {
                android.graphics.RuntimeShader(AGSL_ETCHED_CONTENT_SHADER_SOURCE)
            } catch (_: Throwable) {
                null
            }
        } else {
            null
        }
    }
    val slopeColors = remember { IntArray(4) }
    val midCrestColors = remember { IntArray(3) }
    val lipColors = remember { IntArray(3) }

    val ambientShadowPaint = remember {
        AndroidPaint(AndroidPaint.ANTI_ALIAS_FLAG).apply {
            style = AndroidPaint.Style.FILL
        }
    }
    val causticShadowPaint = remember {
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
    val midFrostRindPaint = remember {
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
            isFilterBitmap = false
            xfermode = PorterDuffXfermode(PorterDuff.Mode.OVERLAY)
            shader = SeaGlassSandGrainTexture.obtainShader()
        }
    }
    val rindStipplePaint = remember {
        AndroidPaint(AndroidPaint.ANTI_ALIAS_FLAG).apply {
            style = AndroidPaint.Style.STROKE
            strokeCap = AndroidPaint.Cap.ROUND
            strokeJoin = AndroidPaint.Join.ROUND
            isFilterBitmap = false
            xfermode = PorterDuffXfermode(PorterDuff.Mode.OVERLAY)
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

            val primaryTintArgb = activeNodes.firstOrNull()?.tintColorArgb ?: material.tintColorArgb
            val secondaryTintArgb = if (activeNodes.size >= 2) {
                activeNodes[1].tintColorArgb
            } else {
                primaryTintArgb
            }

            // Grounding drop shadow (models physical thickness & 3D elevation beneath the bright bottom bevel)
            val shadowColorArgb = SeaGlassColorMath.lerpOklab(
                primaryTintArgb,
                0xFF142030.toInt(),
                0.74f
            )
            val shadowAlpha = ((0.22f + sandWear * 0.12f) * 255f).toInt().coerceIn(48, 86)
            ambientShadowPaint.color = (shadowAlpha shl 24) or (shadowColorArgb and 0x00FFFFFF)
            ambientShadowPaint.maskFilter = SeaGlassShaderProgramCache.obtainBlurMaskFilter(5.5f * density)

            val causticGlowArgb = SeaGlassColorMath.computeSubsurfaceCausticColor(
                secondaryTintArgb,
                material.coarseness
            )

            val rimLight = (0.50f + material.rimChalkiness * 0.44f).coerceIn(0.36f, 0.96f)
            // Ocean-tumbled 3D Pillow Bevel & Subsurface Caustic colors in perceptual Oklab space:
            // Models how sand and pebble abrasion create a wide, pillowy frosted bevel around the
            // perimeter (without a harsh white border line!) while internal refracted light focuses
            // into a luminous bottom bevel arc.
            val topBevelRgb = SeaGlassColorMath.computeFrostedRindColor(
                primaryTintArgb,
                material.coarseness,
                0.52f
            ) and 0x00FFFFFF
            val sideBevelRgb = SeaGlassColorMath.computeFrostedRindColor(
                primaryTintArgb,
                material.coarseness,
                0.66f
            ) and 0x00FFFFFF
            val causticBevelRgb = SeaGlassColorMath.lerpOklab(
                causticGlowArgb,
                0xFFFAFDFF.toInt(),
                0.56f + 0.16f * material.coarseness
            ) and 0x00FFFFFF
            val bottomCrestRgb = SeaGlassColorMath.lerpOklab(
                causticGlowArgb,
                0xFFFFFFFF.toInt(),
                0.76f + 0.14f * material.coarseness
            ) and 0x00FFFFFF
            val milkyRindRgb = SeaGlassColorMath.computeFrostedRindColor(
                primaryTintArgb,
                material.coarseness,
                0.76f
            ) and 0x00FFFFFF

            fun argbWithAlpha(rgb: Int, aFloat: Float): Int {
                val a = (aFloat.coerceIn(0f, 1f) * 255f).toInt()
                return (a shl 24) or rgb
            }

            val cx = widthPx * 0.5f
            val cy = heightPx * 0.5f

            // 1. Wide Pillowy 3D Bevel Cushion (reaches ~4.5dp..8.5dp inward, feathered over 5.8dp):
            // Gentle and tint-harmonized along the top edge so the glass color reaches the top rim,
            // milky-frosted on the lateral caps, and luminous along the bottom subsurface caustic bevel!
            innerSlopeChromaticPaint.strokeWidth = min(minDimPx * 0.36f, 17.0f * density).coerceAtLeast(8.5f * density)
            innerSlopeChromaticPaint.maskFilter = SeaGlassShaderProgramCache.obtainBlurMaskFilter(5.8f * density)
            slopeColors[0] = argbWithAlpha(topBevelRgb, rimLight * 0.22f)
            slopeColors[1] = argbWithAlpha(sideBevelRgb, rimLight * 0.46f)
            slopeColors[2] = argbWithAlpha(causticBevelRgb, rimLight * 0.72f)
            slopeColors[3] = argbWithAlpha(bottomCrestRgb, rimLight * 0.94f)
            innerSlopeChromaticPaint.shader = AndroidLinearGradient(
                cx * 0.92f,
                0f,
                cx * 1.08f,
                heightPx,
                slopeColors,
                BevelSlopeStopPositions,
                Shader.TileMode.CLAMP
            )

            // 2. Mid Bevel Cushion (reaches ~2.2dp..4.2dp inward, 3.2dp feather so there is zero white ring line)
            midFrostRindPaint.strokeWidth = min(minDimPx * 0.18f, 8.4f * density).coerceAtLeast(4.4f * density)
            midFrostRindPaint.maskFilter = SeaGlassShaderProgramCache.obtainBlurMaskFilter(3.2f * density)
            midCrestColors[0] = argbWithAlpha(topBevelRgb, rimLight * 0.14f)
            midCrestColors[1] = argbWithAlpha(sideBevelRgb, rimLight * 0.36f)
            midCrestColors[2] = argbWithAlpha(bottomCrestRgb, rimLight * 0.84f)
            midFrostRindPaint.shader = AndroidLinearGradient(
                cx,
                0f,
                cx,
                heightPx,
                midCrestColors,
                MidCrestStopPositions,
                Shader.TileMode.CLAMP
            )

            // 3. Soft Tumbled Edge Lip (subtle on top/sides, luminous catch-light on the bottom rim)
            outerContactLipPaint.strokeWidth = 1.6f * density
            outerContactLipPaint.maskFilter = SeaGlassShaderProgramCache.obtainBlurMaskFilter(0.85f * density)
            lipColors[0] = argbWithAlpha(milkyRindRgb, rimLight * 0.16f)
            lipColors[1] = argbWithAlpha(milkyRindRgb, rimLight * 0.28f)
            lipColors[2] = argbWithAlpha(bottomCrestRgb, rimLight * 0.74f)
            outerContactLipPaint.shader = AndroidLinearGradient(
                cx,
                0f,
                cx,
                heightPx,
                lipColors,
                OuterLipStopPositions,
                Shader.TileMode.CLAMP
            )

            rindStipplePaint.strokeWidth = min(minDimPx * 0.34f, 15.0f * density).coerceAtLeast(7.0f * density)

            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU && etchedRuntimeShader != null) {
                val isDarkGlass = SeaGlassColorMath.argbToOklab(primaryTintArgb).l < 0.48f
                val tR = ((primaryTintArgb ushr 16) and 0xFF) / 255f
                val tG = ((primaryTintArgb ushr 8) and 0xFF) / 255f
                val tB = (primaryTintArgb and 0xFF) / 255f
                val lipArgb = SeaGlassColorMath.computeEtchedCatchLightColor(primaryTintArgb, material.coarseness)
                val lR = ((lipArgb ushr 16) and 0xFF) / 255f
                val lG = ((lipArgb ushr 8) and 0xFF) / 255f
                val lB = (lipArgb and 0xFF) / 255f

                etchedRuntimeShader.setFloatUniform("uStepPx", 0.52f * density, 1.15f * density)
                etchedRuntimeShader.setFloatUniform("uGlassTint", tR, tG, tB, if (isDarkGlass) 1f else 0f)
                etchedRuntimeShader.setFloatUniform("uLipColor", lR, lG, lB, material.coarseness)
                etchedContentLayer.compositingStrategy = CompositingStrategy.Offscreen
                etchedContentLayer.renderEffect = AndroidRenderEffect
                    .createRuntimeShaderEffect(etchedRuntimeShader, "content")
                    .asComposeRenderEffect()
            }

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

                val combinedTiltX = tiltState.tiltX.coerceIn(-1f, 1f)
                val combinedTiltY = tiltState.tiltY.coerceIn(-1f, 1f)

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

                // Pass 0: Grounding elevation drop shadow beneath the bottom bevel
                nCanvas.save()
                nCanvas.translate(
                    -combinedTiltX * 2.0f * density,
                    (5.2f - combinedTiltY * 1.8f) * density
                )
                nCanvas.drawPath(outerAndroidPath, ambientShadowPaint)
                nCanvas.restore()

                val highPointPivot = Offset(
                    cx + combinedTiltX * minDimPx * 0.14f,
                    cy + combinedTiltY * minDimPx * 0.14f
                )

                // Pass 1: Continuous Glass Refraction & Sand-Diffused Subsurface Transmission
                // Evaluates refraction and diffusion seamlessly across the entire glass contour
                // with zero boolean inner-ring cutout so edges, corners, and interior form one
                // continuous piece of glass!
                if (hasBackdrop && activeTier != SeaGlassDeviceTier.LIGHTWEIGHT_TINT) {
                    val coreLensScale = 1.0f + (material.ior - 1.0f) * 0.055f * (1.0f - 0.45f * sandWear)

                    val maxRefractPx = (material.ior - 1.0f) *
                        (18f * density) *
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

                // Pass 2: Volumetric Tint, Subsurface Caustic Pool, 3D Pillow Bevel, & Tumbled Pitting
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
                    midFrostRindPaint = midFrostRindPaint,
                    outerContactLipPaint = outerContactLipPaint,
                    sandGrainPaint = sandGrainPaint,
                    rindStipplePaint = rindStipplePaint,
                    shaderLocalMatrix = shaderLocalMatrix
                )

                // Pass 3: Foreground UI content etched into the glass contour
                clipPath(composeOuterPath) {
                    if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU && etchedRuntimeShader != null) {
                        val intW = widthPx.toInt().coerceAtLeast(1)
                        val intH = heightPx.toInt().coerceAtLeast(1)
                        etchedContentLayer.record(size = IntSize(intW, intH)) {
                            this@onDrawWithContent.drawContent()
                        }
                        drawLayer(etchedContentLayer)
                    } else {
                        this@onDrawWithContent.drawContent()
                    }
                }
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
    midFrostRindPaint: AndroidPaint,
    outerContactLipPaint: AndroidPaint,
    sandGrainPaint: AndroidPaint,
    rindStipplePaint: AndroidPaint,
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
        Offset(widthPx * 0.15f, 0f)
    }
    val gradEnd = if (activeNodes.size >= 2) {
        Offset(
            activeNodes.last().centerX + activeNodes.last().halfWidth * 0.55f,
            activeNodes.last().centerY
        )
    } else {
        Offset(widthPx * 0.85f, heightPx)
    }

    // 1. Volumetric Beer-Lambert Absorption, Pastel Body Chroma, & Lower Subsurface Caustic Pool:
    // - At sandWear == 0f (completely clear untreated glass): zero frosted wash, pure Beer-Lambert tint.
    // - As sandWear develops (0f -> 1f): the upper-middle interior retains rich Beer-Lambert pastel
    //   saturation (never bleached to white by Screen!), while internal refracted light pools into
    //   a luminous subsurface caustic glow along the lower third and bottom bevel.
    val primaryLab = SeaGlassColorMath.argbToOklab(primaryTintArgb)
    val isDarkGlass = primaryLab.l < 0.48f

    if (isDarkGlass) {
        val filterStrength = ((0.22f + material.tintDensity * 0.48f + sandWear * 0.26f) *
            (0.25f + 0.75f * max(sandWear, material.tintDensity))).coerceIn(0.06f, 0.90f)
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

        val darkBodyAlpha = (sandWear * 0.68f + material.tintDensity * (0.18f + 0.28f * sandWear))
            .coerceIn(0.04f, 0.92f)
        val darkCausticColor = Color(
            SeaGlassColorMath.computeSubsurfaceCausticColor(secondaryTintArgb, material.coarseness)
        )
        drawPath(
            path = composeOuterPath,
            brush = Brush.verticalGradient(
                0.00f to Color(primaryTintArgb).copy(alpha = darkBodyAlpha),
                0.62f to Color(secondaryTintArgb).copy(alpha = darkBodyAlpha),
                1.00f to darkCausticColor.copy(alpha = (darkBodyAlpha * 0.92f).coerceIn(0.04f, 0.90f)),
                startY = 0f,
                endY = heightPx
            )
        )
    } else {
        // Light/pastel sea glass (Seafoam, Cobalt, Amber, Lavender, Rose, Kelp, Milk Glass):
        // Step 1A: Beer-Lambert spectral absorption (`BlendMode.Modulate`) so background text
        // and images seen through the glass are optically tinted into the glass volume.
        val absorbStrength = ((0.26f + material.tintDensity * 0.42f) * (0.35f + 0.65f * sandWear))
            .coerceIn(0.04f, 0.68f)
        if (absorbStrength > 0.01f) {
            val absorbStart = Color(
                SeaGlassColorMath.lerpOklab(0xFFFFFFFF.toInt(), primaryTintArgb, absorbStrength)
            )
            val absorbEnd = Color(
                SeaGlassColorMath.lerpOklab(0xFFFFFFFF.toInt(), secondaryTintArgb, absorbStrength)
            )
            drawPath(
                path = composeOuterPath,
                brush = Brush.linearGradient(
                    colors = listOf(absorbStart, absorbEnd),
                    start = gradStart,
                    end = gradEnd
                ),
                blendMode = BlendMode.Modulate
            )
        }

        // Step 1B: Rich Pastel Sea-Glass Body + Lower Subsurface Caustic Pool
        val roleWeight = when (contentRole) {
            SeaGlassContentRole.NAVIGATION_CHROME -> 1.06f
            SeaGlassContentRole.TEXT_AND_ICONS -> 1.02f
            SeaGlassContentRole.PURE_OPTICAL_LENS -> 0.86f
        }
        val bodyAlpha = ((sandWear * 0.76f + material.tintDensity * (0.18f + 0.25f * sandWear)) * roleWeight)
            .coerceIn(0.0f, 0.90f)

        if (bodyAlpha > 0.01f) {
            val bodyPrimary = Color(
                SeaGlassColorMath.computeFrostedRindColor(
                    primaryTintArgb,
                    material.coarseness,
                    0.12f * sandWear
                )
            )
            val bodySecondary = Color(
                SeaGlassColorMath.computeFrostedRindColor(
                    secondaryTintArgb,
                    material.coarseness,
                    0.14f * sandWear
                )
            )
            drawPath(
                path = composeOuterPath,
                brush = Brush.linearGradient(
                    colors = listOf(
                        bodyPrimary.copy(alpha = bodyAlpha),
                        bodySecondary.copy(alpha = bodyAlpha)
                    ),
                    start = gradStart,
                    end = gradEnd
                )
            )

            // Step 1C: Smooth Vertical Volumetric Body -> Lower Subsurface Caustic Pool
            // Rich pastel body in the upper two-thirds transitioning smoothly into a luminous
            // subsurface caustic glow along the lower third and bottom bevel!
            val deepCorePrimary = Color(
                SeaGlassColorMath.computeDeepVolumetricCoreColor(primaryTintArgb, material.tintDensity)
            )
            val subsurfaceCaustic = Color(
                SeaGlassColorMath.computeSubsurfaceCausticColor(secondaryTintArgb, material.coarseness)
            )
            val coreAlpha = (bodyAlpha * (0.36f + 0.24f * material.tintDensity)).coerceIn(0.0f, 0.58f)
            val causticPoolAlpha = (bodyAlpha * (0.52f + 0.26f * material.rimChalkiness)).coerceIn(0.0f, 0.78f)

            drawPath(
                path = composeOuterPath,
                brush = Brush.verticalGradient(
                    0.00f to deepCorePrimary.copy(alpha = coreAlpha * 0.42f),
                    0.35f to deepCorePrimary.copy(alpha = coreAlpha * 0.60f),
                    0.64f to bodySecondary.copy(alpha = coreAlpha * 0.24f),
                    0.84f to subsurfaceCaustic.copy(alpha = causticPoolAlpha * 0.52f),
                    1.00f to subsurfaceCaustic.copy(alpha = causticPoolAlpha * 0.88f),
                    startY = 0f,
                    endY = heightPx
                )
            )
        }
    }

    // 2. Sculpted 3D Pillow Bevel + Crisp Pinpoint Tumbled Sea-Glass Pitting
    val grainScale = (density * 0.38f).coerceIn(0.78f, 1.22f)
    shaderLocalMatrix.reset()
    shaderLocalMatrix.setScale(grainScale, grainScale)
    sandGrainPaint.shader?.setLocalMatrix(shaderLocalMatrix)
    rindStipplePaint.shader?.setLocalMatrix(shaderLocalMatrix)

    val velvetProgress = material.coarseness.coerceIn(0f, 1f).pow(0.76f)
    val interiorGrainRoleScale = when (contentRole) {
        SeaGlassContentRole.TEXT_AND_ICONS -> 0.44f
        SeaGlassContentRole.NAVIGATION_CHROME -> 0.48f
        SeaGlassContentRole.PURE_OPTICAL_LENS -> 0.92f
    }
    val grainAlpha = (velvetProgress * (0.48f + 0.52f * sandWear) * 180f * interiorGrainRoleScale)
        .toInt()
        .coerceIn(0, 180)
    val bevelStippleAlpha = (velvetProgress * (0.54f + 0.46f * sandWear) * 175f)
        .toInt()
        .coerceIn(0, 185)

    val nCanvas = drawContext.canvas.nativeCanvas
    nCanvas.save()
    nCanvas.clipPath(outerAndroidPath)

    // Pass 2A: Wide pillowy 3D bevel cushion + mid bevel cushion
    val slopeAlpha = ((0.50f + 0.42f * max(sandWear, material.rimChalkiness * 0.60f)) * 225f)
        .toInt()
        .coerceIn(105, 232)
    innerSlopeChromaticPaint.alpha = slopeAlpha
    nCanvas.drawPath(outerAndroidPath, innerSlopeChromaticPaint)
    nCanvas.drawPath(outerAndroidPath, midFrostRindPaint)

    // Pass 2B: Crisp pinpoint sea-glass pitting & crystalline sand grain across body and bevel
    if (grainAlpha > 2) {
        sandGrainPaint.alpha = grainAlpha
        nCanvas.drawRect(0f, 0f, widthPx, heightPx, sandGrainPaint)
    }
    if (bevelStippleAlpha > 4) {
        rindStipplePaint.alpha = bevelStippleAlpha
        nCanvas.drawPath(outerAndroidPath, rindStipplePaint)
    }

    // Pass 2C: Soft tumbled outer edge lip
    nCanvas.drawPath(outerAndroidPath, outerContactLipPaint)
    nCanvas.restore()
}

/**
 * Renders a nested 3D Pillow-Beveled Sea-Glass Inlay Capsule or Medallion inside a host SeaGlass bar
 * (such as the active `"Mail"` pill inside `"Mail | Meet"`, the active `"Photos"` pill inside
 * `SeaGlassPillNavBar`, the active tab pill in `SeaGlassFluidBar`, or the voice medallion in `SeaGlassPromptBar`).
 *
 * Replaces flat wireframe borders with a true 3D frosted bevel slope, subsurface bottom caustic lift,
 * grounding pillow shadow, and continuous multi-scale sea-glass sand pitting.
 */
@Composable
internal fun Modifier.seaGlassBeveledInlay(
    tintColorArgb: Int,
    coarseness: Float = 0.48f,
    cornerRadiusDp: Float = 24f,
    fillOpacityScale: Float = 1.0f
): Modifier {
    val inlayPath = remember { Path() }
    val slopeColors = remember { IntArray(3) }
    val lipColors = remember { IntArray(3) }
    val grainMatrix = remember { AndroidMatrix() }

    val shadowPaint = remember {
        AndroidPaint(AndroidPaint.ANTI_ALIAS_FLAG).apply {
            style = AndroidPaint.Style.FILL
        }
    }
    val bevelSlopePaint = remember {
        AndroidPaint(AndroidPaint.ANTI_ALIAS_FLAG).apply {
            style = AndroidPaint.Style.STROKE
            strokeCap = AndroidPaint.Cap.ROUND
            strokeJoin = AndroidPaint.Join.ROUND
        }
    }
    val outerLipPaint = remember {
        AndroidPaint(AndroidPaint.ANTI_ALIAS_FLAG).apply {
            style = AndroidPaint.Style.STROKE
            strokeCap = AndroidPaint.Cap.ROUND
            strokeJoin = AndroidPaint.Join.ROUND
        }
    }
    val grainPaint = remember {
        AndroidPaint(AndroidPaint.ANTI_ALIAS_FLAG).apply {
            style = AndroidPaint.Style.FILL
            isFilterBitmap = false
            xfermode = PorterDuffXfermode(PorterDuff.Mode.OVERLAY)
            shader = SeaGlassSandGrainTexture.obtainShader()
        }
    }

    return this.drawWithCache {
        val w = size.width.coerceAtLeast(1f)
        val h = size.height.coerceAtLeast(1f)
        val minDim = min(w, h)
        val rPx = min(cornerRadiusDp * density, minDim * 0.5f)

        inlayPath.reset()
        inlayPath.addRoundRect(0f, 0f, w, h, rPx, rPx, Path.Direction.CW)
        val composeInlayPath = inlayPath.asComposePath()

        val lab = SeaGlassColorMath.argbToOklab(tintColorArgb)
        val isDark = lab.l < 0.48f

        val shadowArgb = SeaGlassColorMath.lerpOklab(tintColorArgb, 0xFF0A1828.toInt(), 0.68f)
        val shadowAlpha = ((if (isDark) 0.24f else 0.14f) * fillOpacityScale * 255f).toInt().coerceIn(14, 72)
        shadowPaint.color = (shadowAlpha shl 24) or (shadowArgb and 0x00FFFFFF)
        shadowPaint.maskFilter = SeaGlassShaderProgramCache.obtainBlurMaskFilter(3.2f * density)

        val topFillColor = if (isDark) {
            Color(SeaGlassColorMath.lerpOklab(tintColorArgb, 0xFFFFFFFF.toInt(), 0.20f))
                .copy(alpha = (0.44f * fillOpacityScale).coerceIn(0.12f, 0.80f))
        } else {
            Color(SeaGlassColorMath.computeFrostedRindColor(tintColorArgb, coarseness, 0.68f))
                .copy(alpha = (0.42f * fillOpacityScale).coerceIn(0.12f, 0.78f))
        }
        val midFillColor = if (isDark) {
            Color(tintColorArgb).copy(alpha = (0.38f * fillOpacityScale).coerceIn(0.10f, 0.74f))
        } else {
            Color(SeaGlassColorMath.computeFrostedRindColor(tintColorArgb, coarseness, 0.58f))
                .copy(alpha = (0.36f * fillOpacityScale).coerceIn(0.10f, 0.72f))
        }
        val bottomFillColor = if (isDark) {
            Color(SeaGlassColorMath.computeSubsurfaceCausticColor(tintColorArgb, coarseness))
                .copy(alpha = (0.48f * fillOpacityScale).coerceIn(0.12f, 0.82f))
        } else {
            Color(SeaGlassColorMath.computeFrostedRindColor(tintColorArgb, coarseness, 0.78f))
                .copy(alpha = (0.50f * fillOpacityScale).coerceIn(0.14f, 0.82f))
        }

        val topBevelRgb = SeaGlassColorMath.computeFrostedRindColor(tintColorArgb, coarseness, 0.78f) and 0x00FFFFFF
        val midBevelRgb = SeaGlassColorMath.computeFrostedRindColor(tintColorArgb, coarseness, 0.68f) and 0x00FFFFFF
        val botBevelRgb = SeaGlassColorMath.computeFrostedRindColor(tintColorArgb, coarseness, 0.88f) and 0x00FFFFFF

        fun argb(rgb: Int, aFloat: Float): Int {
            val a = (aFloat.coerceIn(0f, 1f) * 255f).toInt()
            return (a shl 24) or rgb
        }

        bevelSlopePaint.strokeWidth = min(minDim * 0.26f, 9.0f * density).coerceAtLeast(4.5f * density)
        bevelSlopePaint.maskFilter = SeaGlassShaderProgramCache.obtainBlurMaskFilter(3.4f * density)
        slopeColors[0] = argb(topBevelRgb, 0.34f * fillOpacityScale)
        slopeColors[1] = argb(midBevelRgb, 0.28f * fillOpacityScale)
        slopeColors[2] = argb(botBevelRgb, 0.52f * fillOpacityScale)
        bevelSlopePaint.shader = AndroidLinearGradient(
            w * 0.5f,
            0f,
            w * 0.5f,
            h,
            slopeColors,
            MidCrestStopPositions,
            Shader.TileMode.CLAMP
        )

        outerLipPaint.strokeWidth = 1.2f * density
        outerLipPaint.maskFilter = SeaGlassShaderProgramCache.obtainBlurMaskFilter(0.8f * density)
        lipColors[0] = argb(topBevelRgb, 0.22f * fillOpacityScale)
        lipColors[1] = argb(topBevelRgb, 0.18f * fillOpacityScale)
        lipColors[2] = argb(botBevelRgb, 0.38f * fillOpacityScale)
        outerLipPaint.shader = AndroidLinearGradient(
            w * 0.5f,
            0f,
            w * 0.5f,
            h,
            lipColors,
            OuterLipStopPositions,
            Shader.TileMode.CLAMP
        )

        val grainScale = (density * 0.38f).coerceIn(0.78f, 1.22f)
        grainMatrix.reset()
        grainMatrix.setScale(grainScale, grainScale)
        grainPaint.shader?.setLocalMatrix(grainMatrix)
        grainPaint.alpha = (coarseness.coerceIn(0f, 1f).pow(0.74f) * 145f * fillOpacityScale)
            .toInt()
            .coerceIn(0, 165)

        onDrawWithContent {
            val nCanvas = drawContext.canvas.nativeCanvas

            drawPath(
                path = composeInlayPath,
                brush = Brush.verticalGradient(
                    0.00f to topFillColor,
                    0.52f to midFillColor,
                    1.00f to bottomFillColor,
                    startY = 0f,
                    endY = h
                )
            )

            nCanvas.save()
            nCanvas.clipPath(inlayPath)
            nCanvas.drawPath(inlayPath, bevelSlopePaint)
            if (grainPaint.alpha > 4) {
                nCanvas.drawRect(0f, 0f, w, h, grainPaint)
            }
            nCanvas.drawPath(inlayPath, outerLipPaint)
            nCanvas.restore()

            this@onDrawWithContent.drawContent()
        }
    }
}

val LocalInsideSeaGlassEtched = staticCompositionLocalOf { true }

/**
 * Hardware-accelerated **3D Intaglio Etched-Into-Glass** modifier ([Modifier.seaGlassEtched]).
 *
 * On Android 13+ (API 33+), [Modifier.seaGlass] automatically applies the per-pixel AGSL intaglio
 * etching shader (`AGSL_ETCHED_CONTENT_SHADER_SOURCE`) in Pass 3, so [Modifier.seaGlassEtched]
 * acts as a zero-overhead pass-through inside [Modifier.seaGlass] surfaces.
 */
@Composable
fun Modifier.seaGlassEtched(
    tintColorArgb: Int = LocalSeaGlassPalette.current.primaryGlass.tintColorArgb,
    coarseness: Float = 0.45f
): Modifier = this
