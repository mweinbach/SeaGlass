package dev.seaglass.compose

import android.graphics.Path
import android.graphics.RenderEffect as AndroidRenderEffect
import android.graphics.RuntimeShader
import android.graphics.Shader
import android.os.Build
import androidx.compose.runtime.Composable
import androidx.compose.runtime.Stable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.drawWithCache
import androidx.compose.ui.draw.drawWithContent
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path as ComposePath
import androidx.compose.ui.graphics.ShaderBrush
import androidx.compose.ui.graphics.asComposePath
import androidx.compose.ui.graphics.asComposeRenderEffect
import androidx.compose.ui.graphics.drawscope.DrawScope
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.graphics.drawscope.clipPath
import androidx.compose.ui.graphics.drawscope.translate
import androidx.compose.ui.graphics.layer.GraphicsLayer
import androidx.compose.ui.graphics.layer.drawLayer
import androidx.compose.ui.graphics.rememberGraphicsLayer
import androidx.compose.ui.layout.onGloballyPositioned
import androidx.compose.ui.layout.positionInRoot
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.IntSize
import dev.seaglass.core.SeaGlassColorMath
import dev.seaglass.core.SeaGlassContourPathBuilder
import dev.seaglass.core.SeaGlassDeviceTier
import dev.seaglass.core.SeaGlassMaterial
import dev.seaglass.core.SeaGlassSdfEngine
import dev.seaglass.core.backend.SeaGlassAgslBinding
import dev.seaglass.core.backend.SeaGlassShaderCompiler
import dev.seaglass.core.backend.SeaGlassUniformBlock
import dev.seaglass.core.backend.ShaderDialect
import kotlin.math.max
import kotlin.math.min

/**
 * Coordinates zero-copy GPU backdrop capture between background content ([Modifier.seaGlassBackdrop])
 * and foreground SeaGlass UI elements ([Modifier.seaGlass] / [SeaGlassFluidGroup]).
 *
 * Enforces an acyclic render graph: the background hierarchy is recorded once into [backdropLayer],
 * and foreground SeaGlass elements sample from [backdropLayer] without recursive feedback.
 */
@Stable
class SeaGlassStageState internal constructor(
    val backdropLayer: GraphicsLayer
) {
    var backdropOriginInRoot: Offset by mutableStateOf(Offset.Zero)
        internal set

    var backdropSize: IntSize by mutableStateOf(IntSize.Zero)
        internal set

    var hasRecordedBackdrop: Boolean by mutableStateOf(false)
        internal set

    var recordTick: Int by mutableIntStateOf(0)
        internal set
}

/**
 * Creates and remembers a [SeaGlassStageState] backed by a hardware-accelerated [GraphicsLayer].
 */
@Composable
fun rememberSeaGlassStageState(): SeaGlassStageState {
    val graphicsLayer = rememberGraphicsLayer()
    return remember(graphicsLayer) {
        SeaGlassStageState(backdropLayer = graphicsLayer)
    }
}

/**
 * Captures the composable's draw hierarchy into [stageState]'s GPU [GraphicsLayer] so
 * foreground [Modifier.seaGlass] elements can refract, blur, and tint it in real time.
 */
fun Modifier.seaGlassBackdrop(stageState: SeaGlassStageState): Modifier {
    return this
        .onGloballyPositioned { coordinates ->
            stageState.backdropOriginInRoot = coordinates.positionInRoot()
            stageState.backdropSize = coordinates.size
        }
        .drawWithContent {
            stageState.backdropLayer.record(
                size = IntSize(size.width.toInt().coerceAtLeast(1), size.height.toInt().coerceAtLeast(1))
            ) {
                this@drawWithContent.drawContent()
            }
            stageState.hasRecordedBackdrop = true
            stageState.recordTick++
            drawLayer(stageState.backdropLayer)
        }
}

/**
 * Renders a real-time frosted SeaGlass surface over [stageState] using the active GPU backend tier:
 * - **API 33+ (`AGSL_FULL` / `VULKAN_NATIVE`)**: Zero-copy `RenderNode` backdrop blur + edge refraction
 *   paired with the AGSL `RuntimeShader` consuming the 256-byte `std140` [SeaGlassUniformBlock]
 *   in local `DrawScope` coordinates for multi-node SDF coalescing, procedural sand-grain micro-coarseness,
 *   Oklab Beer-Lambert volumetric absorption, and rough-Fresnel chalky rim lighting.
 * - **API 31–32 (`RENDER_EFFECT_HYBRID`)**: Hardware `RenderEffect` blur clipped to the analytical
 *   SDF contour + Oklab Beer-Lambert volumetric body tint + tumbled bevel rim lighting.
 * - **API 26–30 (`GLES3_NATIVE` / `LIGHTWEIGHT_TINT`)**: Analytical SDF smooth contour + Oklab
 *   Beer-Lambert body tint + procedural sand-grain frost & directional bevel rim.
 */
@Composable
fun Modifier.seaGlass(
    stageState: SeaGlassStageState?,
    material: SeaGlassMaterial,
    nodesOptional: List<SeaGlassSdfEngine.SdfNode>? = null,
    smoothRadiusDp: Float = 24f
): Modifier {
    val context = LocalContext.current
    val tierOverride = LocalSeaGlassTierOverride.current
    val activeTier = remember(context, tierOverride) {
        tierOverride ?: SeaGlassDeviceTier.detect(context)
    }

    val glassLayer = rememberGraphicsLayer()
    val uniformBlock = remember { SeaGlassUniformBlock() }
    val contourAndroidPath = remember { Path() }
    val agslBundle = remember {
        SeaGlassShaderCompiler.compileForDialect(ShaderDialect.AGSL_100)
    }
    val runtimeShaderHolder = remember {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            val grainShader = RuntimeShader(requireNotNull(agslBundle.sandGrainOverlayFragmentSource))
            AgslShaderHolder(
                grainOverlayShader = grainShader,
                grainBrush = ShaderBrush(grainShader)
            )
        } else {
            null
        }
    }

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
            val frostPx = (material.frostRadiusDp * density).coerceAtLeast(1.0f)

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

            // Build analytical contour path (including cubic smin metaball bridges for coalescing nodes)
            if (activeNodes.size >= 2) {
                SeaGlassContourPathBuilder.buildCoalescingPairPath(
                    nodeA = activeNodes[0],
                    nodeB = activeNodes[1],
                    smoothRadiusPx = smoothRadiusPx,
                    outPath = contourAndroidPath
                )
                if (activeNodes.size > 2) {
                    val extraPath = Path()
                    for (i in 2 until min(activeNodes.size, SeaGlassSdfEngine.MAX_SDF_NODES)) {
                        SeaGlassContourPathBuilder.buildSingleNodePath(activeNodes[i], extraPath)
                        contourAndroidPath.op(extraPath, Path.Op.UNION)
                    }
                }
            } else {
                SeaGlassContourPathBuilder.buildSingleNodePath(activeNodes[0], contourAndroidPath)
            }
            val composeClipPath: ComposePath = contourAndroidPath.asComposePath()

            onDrawWithContent {
                val tick = stageState?.recordTick ?: 0
                val hasBackdrop = stageState?.hasRecordedBackdrop == true && tick >= 0
                val relOffset = if (stageState != null) {
                    elementOriginInRoot - stageState.backdropOriginInRoot
                } else {
                    Offset.Zero
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

                // Pass 1: Zero-copy GPU RenderNode subsurface backdrop blur & IOR refraction shift
                if (hasBackdrop && activeTier != SeaGlassDeviceTier.LIGHTWEIGHT_TINT) {
                    if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
                        val blurEffect = AndroidRenderEffect.createBlurEffect(
                            frostPx,
                            frostPx,
                            Shader.TileMode.CLAMP
                        )
                        glassLayer.renderEffect = blurEffect.asComposeRenderEffect()
                    } else {
                        glassLayer.renderEffect = null
                    }

                    // Softened Snell's law refraction shift scaled by (ior - 1.0) and attenuated by coarseness
                    val refractShiftPx = (material.ior - 1.0f) *
                        thicknessPx *
                        (1.0f - 0.55f * material.coarseness) *
                        0.32f

                    glassLayer.record(
                        size = IntSize(widthPx.toInt().coerceAtLeast(1), heightPx.toInt().coerceAtLeast(1))
                    ) {
                        translate(
                            left = -relOffset.x + refractShiftPx,
                            top = -relOffset.y + refractShiftPx * 0.85f
                        ) {
                            drawLayer(stageState.backdropLayer)
                        }
                    }

                    clipPath(composeClipPath) {
                        drawLayer(glassLayer)
                    }
                }

                // Pass 2: Volumetric Oklab Beer-Lambert absorption + AGSL SDF sand-grain & bevel shader
                val useAgslShader = Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU &&
                    runtimeShaderHolder != null &&
                    (activeTier == SeaGlassDeviceTier.AGSL_FULL ||
                        activeTier == SeaGlassDeviceTier.VULKAN_NATIVE)

                drawTumbledBevelAndFrostRim(
                    composePath = composeClipPath,
                    material = material,
                    widthPx = widthPx,
                    heightPx = heightPx,
                    runtimeShaderHolder = if (useAgslShader) runtimeShaderHolder else null,
                    uniformBlock = uniformBlock
                )

                // Pass 3: Render foreground UI content (icons, typography, children)
                drawContent()
            }
        }
}

private class AgslShaderHolder(
    val grainOverlayShader: RuntimeShader,
    val grainBrush: ShaderBrush
)

/**
 * Renders the volumetric Oklab Beer-Lambert body absorption, AGSL local-space sand-grain
 * micro-coarseness & SDF smooth-union bevel field, and chalky rough-Fresnel rim lighting.
 */
private fun DrawScope.drawTumbledBevelAndFrostRim(
    composePath: ComposePath,
    material: SeaGlassMaterial,
    widthPx: Float,
    heightPx: Float,
    runtimeShaderHolder: AgslShaderHolder?,
    uniformBlock: SeaGlassUniformBlock
) {
    val baseTint = Color(material.tintColorArgb)
    val chalkWhite = Color(0xFFF8FCFF)

    // 1. Volumetric Oklab Beer-Lambert body transmittance + pastel chroma scatter
    val bodyTransmittedTopArgb = SeaGlassColorMath.computeBeerLambertTransmission(
        backdropArgb = 0xFFEAF4FA.toInt(),
        tintArgb = material.tintColorArgb,
        tintDensity = material.tintDensity,
        coarseness = material.coarseness,
        normalizedThickness = 0.85f
    )
    val bodyTransmittedBottomArgb = SeaGlassColorMath.computeBeerLambertTransmission(
        backdropArgb = 0xFFCEE2F0.toInt(),
        tintArgb = material.tintColorArgb,
        tintDensity = material.tintDensity,
        coarseness = material.coarseness,
        normalizedThickness = 1.35f
    )

    val topBodyColor = Color(bodyTransmittedTopArgb)
    val bottomBodyColor = Color(bodyTransmittedBottomArgb)
    val bodyAlpha = (0.36f + material.tintDensity * 0.36f + material.coarseness * 0.14f)
        .coerceIn(0.28f, 0.86f)

    drawPath(
        path = composePath,
        brush = Brush.linearGradient(
            colors = listOf(
                topBodyColor.copy(alpha = bodyAlpha),
                bottomBodyColor.copy(alpha = (bodyAlpha * 0.94f).coerceIn(0.25f, 0.86f))
            ),
            start = Offset.Zero,
            end = Offset(widthPx, heightPx)
        )
    )

    // 2. GPU AGSL SDF + Sand-Grain Micro-Coarseness + Bevel Rim Pass (API 33+)
    if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU && runtimeShaderHolder != null) {
        SeaGlassAgslBinding.bindSandGrainOverlayUniforms(
            shader = runtimeShaderHolder.grainOverlayShader,
            uniformBlock = uniformBlock
        )
        drawRect(
            brush = runtimeShaderHolder.grainBrush,
            topLeft = Offset.Zero,
            size = size
        )
    } else {
        // Fallback isotropic subsurface frost diffusion glow
        val frostAlpha = (material.coarseness * 0.26f).coerceIn(0.05f, 0.30f)
        drawPath(
            path = composePath,
            brush = Brush.radialGradient(
                colors = listOf(
                    chalkWhite.copy(alpha = frostAlpha),
                    baseTint.copy(alpha = frostAlpha * 0.5f),
                    Color.Transparent
                ),
                center = Offset(widthPx * 0.32f, heightPx * 0.25f),
                radius = max(widthPx, heightPx) * 0.85f
            )
        )
    }

    // 3. Crisp Windward Chalky Rough-Fresnel Rim + Leeward Refracted Caustic Bevel Stroke
    val rimAlpha = (0.48f + material.rimChalkiness * 0.46f).coerceIn(0.32f, 0.94f)
    val windwardRimColor = Color(
        SeaGlassColorMath.lerpOklab(
            material.tintColorArgb,
            0xFFFFFFFF.toInt(),
            (0.72f + material.coarseness * 0.20f).coerceIn(0f, 0.96f)
        )
    )
    val leewardCausticColor = Color(
        SeaGlassColorMath.lerpOklab(
            material.tintColorArgb,
            0xFFFFFFFF.toInt(),
            0.32f
        )
    )

    val strokeWidthPx = (1.5f + material.coarseness * 1.1f) * density
    drawPath(
        path = composePath,
        brush = Brush.linearGradient(
            colors = listOf(
                windwardRimColor.copy(alpha = rimAlpha),
                windwardRimColor.copy(alpha = rimAlpha * 0.36f),
                leewardCausticColor.copy(alpha = rimAlpha * 0.68f)
            ),
            start = Offset.Zero,
            end = Offset(widthPx, heightPx)
        ),
        style = Stroke(width = strokeWidthPx)
    )
}
