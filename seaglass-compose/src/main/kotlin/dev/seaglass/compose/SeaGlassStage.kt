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
import kotlin.math.max
import kotlin.math.min

/**
 * Coordinates zero-copy GPU backdrop capture between background content ([Modifier.seaGlassBackdrop])
 * and foreground SeaGlass UI elements ([Modifier.seaGlass] / [SeaGlassFluidGroup]).
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
 * foreground [Modifier.seaGlass] elements can refract, frost, and tint it in real time.
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
 * Renders a real-time **Liquid Sea Glass** surface over [stageState]:
 *
 * 1. **Ambient Elevation Shadow & Colored Caustic Under-Glow**: Casts a soft ambient shadow
 *    plus a translucent pastel caustic glow beneath the bottom-right rim.
 * 2. **Dual-Zone 3D Liquid Glass Lens Meniscus + Subsurface Sea-Glass Frost**:
 *    - **Outer Meniscus Ring (`meniscusLayer`)**: Magnifies and refracts the background with a
 *      crisper lens blur (`0.22 × frostRadius`) around the curved 3D perimeter and across
 *      `sminCubic` liquid bridges.
 *    - **Inner Frosted Plateau (`frostedLayer`)**: Deep subsurface sea-glass diffusion blur
 *      inside the meniscus ring.
 * 3. **Exact Newton-Raphson `sminCubic` Metaball Contours**: Smooth $C^2$-continuous coalescing
 *    contours with zero polygon kinks.
 * 4. **Volumetric Oklab Beer-Lambert Body Absorption + Tiled Sand-Grain Micro-Facet Texture**:
 *    Translucent pastel volume tinting paired with a multi-octave toroidal sand-grain
 *    `BitmapShader` (`SeaGlassSandGrainTexture`).
 * 5. **4-Layer 3D Meniscus Lighting**: Inner refractive depth groove, leeward internal caustic
 *    arc, wide windward frosted bevel sheen, and crisp outer Fresnel chalk rim.
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

    val frostedLayer = rememberGraphicsLayer()
    val meniscusLayer = rememberGraphicsLayer()
    val uniformBlock = remember { SeaGlassUniformBlock() }
    val outerAndroidPath = remember { Path() }
    val innerPlateauAndroidPath = remember { Path() }

    val ambientShadowPaint = remember {
        AndroidPaint(AndroidPaint.ANTI_ALIAS_FLAG).apply {
            style = AndroidPaint.Style.FILL
        }
    }
    val causticUnderGlowPaint = remember {
        AndroidPaint(AndroidPaint.ANTI_ALIAS_FLAG).apply {
            style = AndroidPaint.Style.FILL
        }
    }
    val meniscusGroovePaint = remember {
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
            val cornerPx = min(material.cornerRadiusDp * density, min(widthPx, heightPx) * 0.5f)
            val thicknessPx = material.thicknessDp * density
            val smoothRadiusPx = smoothRadiusDp * density
            val frostPx = (material.frostRadiusDp * density).coerceAtLeast(1.5f)
            // Crisper lens blur at the 3D curved meniscus rim so background refraction is clearly visible
            val meniscusBlurPx = (frostPx * 0.22f).coerceIn(1.0f, 10f)
            val meniscusWidthPx = (thicknessPx * 0.52f).coerceIn(
                6f * density,
                min(widthPx, heightPx) * 0.28f
            )

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

            // Build exact C^2-continuous sminCubic outer contour and inset 3D meniscus plateau contour
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
                insetPx = meniscusWidthPx
            )

            val composeOuterPath: ComposePath = outerAndroidPath.asComposePath()
            val composePlateauPath: ComposePath = innerPlateauAndroidPath.asComposePath()

            // Configure soft ambient shadow + colored sea-glass caustic under-glow
            val shadowColorArgb = SeaGlassColorMath.lerpOklab(
                material.tintColorArgb,
                0xFF071624.toInt(),
                0.70f
            )
            ambientShadowPaint.color = (0x2E shl 24) or (shadowColorArgb and 0x00FFFFFF)
            ambientShadowPaint.maskFilter = BlurMaskFilter(11f * density, BlurMaskFilter.Blur.NORMAL)

            val causticGlowArgb = SeaGlassColorMath.lerpOklab(
                material.tintColorArgb,
                0xFFFFFFFF.toInt(),
                0.18f
            )
            causticUnderGlowPaint.color = (0x34 shl 24) or (causticGlowArgb and 0x00FFFFFF)
            causticUnderGlowPaint.maskFilter = BlurMaskFilter(14f * density, BlurMaskFilter.Blur.NORMAL)

            // Configure 3D inner meniscus refraction groove (darkened tinted ring between rim & plateau)
            val grooveArgb = SeaGlassColorMath.lerpOklab(
                material.tintColorArgb,
                0xFF0A2032.toInt(),
                0.52f
            )
            val grooveAlpha = ((0.18f + (1f - material.coarseness) * 0.16f) * 255f).toInt().coerceIn(20, 95)
            meniscusGroovePaint.color = (grooveAlpha shl 24) or (grooveArgb and 0x00FFFFFF)
            meniscusGroovePaint.strokeWidth = (meniscusWidthPx * 0.48f).coerceAtLeast(2f * density)
            meniscusGroovePaint.maskFilter = BlurMaskFilter(
                (meniscusWidthPx * 0.42f).coerceAtLeast(2f * density),
                BlurMaskFilter.Blur.NORMAL
            )

            onDrawWithContent {
                val tick = stageState?.recordTick ?: 0
                val hasBackdrop = stageState?.hasRecordedBackdrop == true && tick >= 0
                val relOffset = if (stageState != null) {
                    elementOriginInRoot - stageState.backdropOriginInRoot
                } else {
                    Offset.Zero
                }

                // Pack the backend-agnostic 256-byte std140 UBO block for Vulkan/GLES3/AGSL parity
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

                // Pass 0: Ambient drop shadow + colored sea-glass caustic under-glow
                nCanvas.save()
                nCanvas.translate(0f, 5.5f * density)
                nCanvas.drawPath(outerAndroidPath, ambientShadowPaint)
                nCanvas.translate(2.5f * density, 2.5f * density)
                nCanvas.drawPath(innerPlateauAndroidPath, causticUnderGlowPaint)
                nCanvas.restore()

                val layerW = widthPx.toInt().coerceAtLeast(1)
                val layerH = heightPx.toInt().coerceAtLeast(1)

                // Pass 1: Dual-Zone GPU RenderNode Backdrop (Liquid Glass Meniscus Lens + Frosted Sea-Glass Core)
                if (hasBackdrop && activeTier != SeaGlassDeviceTier.LIGHTWEIGHT_TINT) {
                    if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
                        val rimBlurEffect = AndroidRenderEffect.createBlurEffect(
                            meniscusBlurPx,
                            meniscusBlurPx,
                            Shader.TileMode.CLAMP
                        )
                        meniscusLayer.renderEffect = rimBlurEffect.asComposeRenderEffect()

                        val coreBlurEffect = AndroidRenderEffect.createBlurEffect(
                            frostPx,
                            frostPx,
                            Shader.TileMode.CLAMP
                        )
                        frostedLayer.renderEffect = coreBlurEffect.asComposeRenderEffect()
                    } else {
                        meniscusLayer.renderEffect = null
                        frostedLayer.renderEffect = null
                    }

                    // 3D Meniscus Lens Magnification & Snell Refraction Shift around the perimeter
                    val lensMagnification = 1.0f +
                        (material.ior - 1.0f) * 0.28f * (1.0f - 0.40f * material.coarseness)
                    val meniscusShiftPx = (material.ior - 1.0f) *
                        thicknessPx *
                        (1.0f - 0.35f * material.coarseness) *
                        0.55f

                    meniscusLayer.record(size = IntSize(layerW, layerH)) {
                        scale(
                            scaleX = lensMagnification,
                            scaleY = lensMagnification,
                            pivot = Offset(widthPx * 0.5f, heightPx * 0.5f)
                        ) {
                            translate(
                                left = -relOffset.x + meniscusShiftPx,
                                top = -relOffset.y + meniscusShiftPx * 0.9f
                            ) {
                                drawLayer(stageState.backdropLayer)
                            }
                        }
                    }

                    // Interior Sea-Glass Subsurface Frosted Core
                    val coreShiftPx = (material.ior - 1.0f) *
                        thicknessPx *
                        (1.0f - 0.65f * material.coarseness) *
                        0.18f

                    frostedLayer.record(size = IntSize(layerW, layerH)) {
                        translate(
                            left = -relOffset.x - coreShiftPx,
                            top = -relOffset.y - coreShiftPx
                        ) {
                            drawLayer(stageState.backdropLayer)
                        }
                    }

                    // Draw lens-refracted meniscus across full contour, then deeply frosted core in interior plateau
                    clipPath(composeOuterPath) {
                        drawLayer(meniscusLayer)
                        clipPath(composePlateauPath) {
                            drawLayer(frostedLayer)
                        }
                    }
                }

                // Pass 2: Volumetric Oklab Beer-Lambert Tint + Sanded Micro-Grit + 4-Layer 3D Meniscus Rim
                drawLiquidSeaGlassOptics(
                    composeOuterPath = composeOuterPath,
                    composePlateauPath = composePlateauPath,
                    outerAndroidPath = outerAndroidPath,
                    innerPlateauAndroidPath = innerPlateauAndroidPath,
                    material = material,
                    activeNodes = activeNodes,
                    widthPx = widthPx,
                    heightPx = heightPx,
                    meniscusWidthPx = meniscusWidthPx,
                    meniscusGroovePaint = meniscusGroovePaint,
                    sandGrainPaint = sandGrainPaint,
                    shaderLocalMatrix = shaderLocalMatrix
                )

                // Pass 3: Foreground UI content (icons, typography, children)
                drawContent()
            }
        }
}

private fun DrawScope.drawLiquidSeaGlassOptics(
    composeOuterPath: ComposePath,
    composePlateauPath: ComposePath,
    outerAndroidPath: Path,
    innerPlateauAndroidPath: Path,
    material: SeaGlassMaterial,
    activeNodes: List<SeaGlassSdfEngine.SdfNode>,
    widthPx: Float,
    heightPx: Float,
    meniscusWidthPx: Float,
    meniscusGroovePaint: AndroidPaint,
    sandGrainPaint: AndroidPaint,
    shaderLocalMatrix: AndroidMatrix
) {
    val primaryTintArgb = activeNodes.firstOrNull()?.tintColorArgb ?: material.tintColorArgb
    val secondaryTintArgb = if (activeNodes.size >= 2) {
        activeNodes[1].tintColorArgb
    } else {
        primaryTintArgb
    }

    // 1. Volumetric Oklab Beer-Lambert Sea-Glass Body Transmittance
    // Kept translucent (0.16..0.52) so the refracted & frosted backdrop shines vividly through the glass!
    val transmittedStartArgb = SeaGlassColorMath.computeBeerLambertTransmission(
        backdropArgb = 0xFFF2F8FC.toInt(),
        tintArgb = primaryTintArgb,
        tintDensity = material.tintDensity,
        coarseness = material.coarseness,
        normalizedThickness = 0.80f
    )
    val transmittedEndArgb = SeaGlassColorMath.computeBeerLambertTransmission(
        backdropArgb = 0xFFD6E8F5.toInt(),
        tintArgb = secondaryTintArgb,
        tintDensity = material.tintDensity,
        coarseness = material.coarseness,
        normalizedThickness = 1.25f
    )

    val startBodyColor = Color(transmittedStartArgb)
    val endBodyColor = Color(transmittedEndArgb)
    val bodyAlpha = (0.16f + material.tintDensity * 0.34f + material.coarseness * 0.08f)
        .coerceIn(0.14f, 0.54f)

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

    // 2. Tactile Sand-Tumbled Micro-Coarseness Texture (Toroidal 128x128 BitmapShader)
    // Scale the sand-grain pattern with display density so micro-facets have authentic physical size
    val grainScale = (density * 0.55f).coerceIn(0.8f, 2.2f)
    shaderLocalMatrix.reset()
    shaderLocalMatrix.setScale(grainScale, grainScale)
    sandGrainPaint.shader?.setLocalMatrix(shaderLocalMatrix)
    val grainAlpha = (material.coarseness * 245f).toInt().coerceIn(18, 245)
    sandGrainPaint.alpha = grainAlpha

    val nCanvas = drawContext.canvas.nativeCanvas
    nCanvas.save()
    nCanvas.clipPath(outerAndroidPath)
    // Draw sand-grain micro-facets across the entire sea-glass body
    nCanvas.drawRect(0f, 0f, widthPx, heightPx, sandGrainPaint)

    // Draw 3D inner meniscus refraction groove along the plateau boundary
    nCanvas.drawPath(innerPlateauAndroidPath, meniscusGroovePaint)
    nCanvas.restore()

    // 3. Subsurface Frosted Velvety Sheen in the Upper-Left Dome
    val chalkWhite = Color(0xFFFAFDFF)
    val domeFrostAlpha = (0.08f + material.coarseness * 0.22f).coerceIn(0.06f, 0.30f)
    drawPath(
        path = composePlateauPath,
        brush = Brush.radialGradient(
            colors = listOf(
                chalkWhite.copy(alpha = domeFrostAlpha),
                startBodyColor.copy(alpha = domeFrostAlpha * 0.4f),
                Color.Transparent
            ),
            center = Offset(widthPx * 0.28f, heightPx * 0.22f),
            radius = max(widthPx, heightPx) * 0.78f
        )
    )

    // 4. 4-Component 3D Liquid Glass + Sea Glass Meniscus Rim Lighting:
    val windwardChalkColor = Color(
        SeaGlassColorMath.lerpOklab(
            primaryTintArgb,
            0xFFFFFFFF.toInt(),
            (0.78f + material.coarseness * 0.16f).coerceIn(0f, 0.96f)
        )
    )
    val leewardCausticColor = Color(
        SeaGlassColorMath.lerpOklab(
            secondaryTintArgb,
            0xFFFFFFFF.toInt(),
            0.30f
        )
    )
    val prismaticCyanRim = Color(
        SeaGlassColorMath.lerpOklab(
            primaryTintArgb,
            0xFF78F0FF.toInt(),
            (material.chromaticDispersion * 0.65f).coerceIn(0f, 0.65f)
        )
    )
    val prismaticCoralRim = Color(
        SeaGlassColorMath.lerpOklab(
            secondaryTintArgb,
            0xFFFF9EBA.toInt(),
            (material.chromaticDispersion * 0.65f).coerceIn(0f, 0.65f)
        )
    )

    val rimStrength = (0.48f + material.rimChalkiness * 0.48f).coerceIn(0.35f, 0.96f)

    // 4a. Wide Soft Meniscus Bevel Sheen + Leeward Internal Refracted Caustic Arc
    val bevelSheenWidth = (meniscusWidthPx * 0.55f).coerceIn(3f * density, 10f * density)
    clipPath(composeOuterPath) {
        drawPath(
            path = composeOuterPath,
            brush = Brush.linearGradient(
                colorStops = arrayOf(
                    0.00f to windwardChalkColor.copy(alpha = rimStrength * 0.52f),
                    0.35f to prismaticCyanRim.copy(alpha = rimStrength * 0.18f),
                    0.68f to prismaticCoralRim.copy(alpha = rimStrength * 0.22f),
                    1.00f to leewardCausticColor.copy(alpha = rimStrength * 0.58f)
                ),
                start = Offset.Zero,
                end = Offset(widthPx, heightPx)
            ),
            style = Stroke(width = bevelSheenWidth)
        )
    }

    // 4b. Crisp Outer Specular-Chalk Fresnel Meniscus Edge
    val outerRimWidth = (1.6f + material.coarseness * 0.8f) * density
    drawPath(
        path = composeOuterPath,
        brush = Brush.linearGradient(
            colorStops = arrayOf(
                0.00f to Color.White.copy(alpha = (rimStrength * 0.96f).coerceAtMost(0.98f)),
                0.30f to windwardChalkColor.copy(alpha = rimStrength * 0.72f),
                0.65f to leewardCausticColor.copy(alpha = rimStrength * 0.45f),
                1.00f to windwardChalkColor.copy(alpha = rimStrength * 0.84f)
            ),
            start = Offset.Zero,
            end = Offset(widthPx, heightPx)
        ),
        style = Stroke(width = outerRimWidth)
    )
}
