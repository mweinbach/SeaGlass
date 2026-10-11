package dev.seaglass.compose

import android.graphics.BlurMaskFilter
import android.graphics.LinearGradient as AndroidLinearGradient
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

            val edgeBlur = (target * 0.24f).coerceAtLeast(1.0f)
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
 * Renders a 100% edge-to-edge transparent **Liquid Sea Glass** surface over [stageState]
 * with zero inner contour seams, strong Oklab Beer-Lambert color tinting, tactile sand-tumbled
 * micro-coarseness, and Gaussian-feathered Liquid Glass refractive edge light.
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
    val meniscusGlowPaint = remember {
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

            // Build exact C^2-continuous sminCubic outer contour only (zero inset sub-paths!)
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
                0.70f
            )
            val shadowAlpha = ((0.10f + frostOpacity * 0.14f) * 255f).toInt().coerceIn(12, 56)
            ambientShadowPaint.color = (shadowAlpha shl 24) or (shadowColorArgb and 0x00FFFFFF)
            ambientShadowPaint.maskFilter = BlurMaskFilter(10f * density, BlurMaskFilter.Blur.NORMAL)

            // Gaussian-feathered refractive edge light bloom (zero hard inner stroke line!)
            val rimLight = (0.44f + material.rimChalkiness * 0.54f).coerceIn(0.26f, 0.98f)
            val glowStartArgb = SeaGlassColorMath.lerpOklab(
                material.tintColorArgb,
                0xFFFFFFFF.toInt(),
                0.84f
            )
            val glowMidCyanArgb = SeaGlassColorMath.lerpOklab(
                material.tintColorArgb,
                0xFF64F6FF.toInt(),
                0.76f
            )
            val secondaryNodeTint = if (activeNodes.size >= 2) {
                activeNodes[1].tintColorArgb
            } else {
                material.tintColorArgb
            }
            val glowEndCoralArgb = SeaGlassColorMath.lerpOklab(
                secondaryNodeTint,
                0xFFFFB48A.toInt(),
                0.70f
            )
            val glowAlpha = (rimLight * 145f).toInt().coerceIn(32, 165)

            meniscusGlowPaint.strokeWidth = 6.5f * density
            meniscusGlowPaint.maskFilter = BlurMaskFilter(8.5f * density, BlurMaskFilter.Blur.NORMAL)
            meniscusGlowPaint.shader = AndroidLinearGradient(
                0f,
                0f,
                widthPx,
                heightPx,
                intArrayOf(
                    (glowAlpha shl 24) or (glowStartArgb and 0x00FFFFFF),
                    ((glowAlpha * 0.68f).toInt() shl 24) or (glowMidCyanArgb and 0x00FFFFFF),
                    ((glowAlpha * 0.22f).toInt() shl 24) or (glowStartArgb and 0x00FFFFFF),
                    ((glowAlpha * 0.68f).toInt() shl 24) or (glowEndCoralArgb and 0x00FFFFFF),
                    (glowAlpha shl 24) or (glowStartArgb and 0x00FFFFFF)
                ),
                floatArrayOf(0f, 0.26f, 0.50f, 0.74f, 1f),
                Shader.TileMode.CLAMP
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

                // Pass 2: Seamless Edge-to-Edge Tint, Sand Coarseness, & Gaussian-Feathered Liquid Glass Rim
                drawContinuousLiquidSeaGlass(
                    composeOuterPath = composeOuterPath,
                    outerAndroidPath = outerAndroidPath,
                    material = material,
                    activeNodes = activeNodes,
                    widthPx = widthPx,
                    heightPx = heightPx,
                    frostOpacity = frostOpacity,
                    meniscusGlowPaint = meniscusGlowPaint,
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
    meniscusGlowPaint: AndroidPaint,
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

    // 2. Continuous Edge-to-Edge Sand-Tumbled Micro-Facet Texture + Gaussian-Feathered Meniscus Light
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
    // Gaussian-feathered refractive light bloom along the curved rim (melts seamlessly inward!)
    nCanvas.drawPath(outerAndroidPath, meniscusGlowPaint)
    nCanvas.restore()

    // 3. Crisp 1.5dp Directional Specular-Refraction Lip
    val windwardSpecColor = Color(
        SeaGlassColorMath.lerpOklab(primaryTintArgb, 0xFFFFFFFF.toInt(), 0.86f)
    )
    val leewardCausticColor = Color(
        SeaGlassColorMath.lerpOklab(secondaryTintArgb, 0xFFFFFFFF.toInt(), 0.56f)
    )
    val rimLight = (0.44f + material.rimChalkiness * 0.54f).coerceIn(0.26f, 0.98f)

    drawPath(
        path = composeOuterPath,
        brush = Brush.linearGradient(
            colorStops = arrayOf(
                0.00f to Color.White.copy(alpha = (rimLight * 0.95f).coerceAtMost(0.98f)),
                0.24f to windwardSpecColor.copy(alpha = rimLight * 0.64f),
                0.50f to windwardSpecColor.copy(alpha = rimLight * 0.18f),
                0.76f to leewardCausticColor.copy(alpha = rimLight * 0.60f),
                1.00f to windwardSpecColor.copy(alpha = rimLight * 0.90f)
            ),
            start = Offset.Zero,
            end = Offset(widthPx, heightPx)
        ),
        style = Stroke(width = 1.5f * density)
    )
}
