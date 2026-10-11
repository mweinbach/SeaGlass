package dev.seaglass.sample

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.animation.core.LinearEasing
import androidx.compose.animation.core.RepeatMode
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.tween
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.gestures.detectDragGestures
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.WindowInsets
import androidx.compose.foundation.layout.asPaddingValues
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.systemBars
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.LocalContentColor
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Slider
import androidx.compose.material3.SliderDefaults
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableFloatStateOf
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.geometry.CornerRadius
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.graphics.drawscope.rotate
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.IntOffset
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import dev.seaglass.compose.LocalSeaGlassMotionScheme
import dev.seaglass.compose.LocalSeaGlassPalette
import dev.seaglass.compose.SeaGlassButton
import dev.seaglass.compose.SeaGlassCard
import dev.seaglass.compose.SeaGlassFluidBar
import dev.seaglass.compose.SeaGlassPill
import dev.seaglass.compose.SeaGlassStageState
import dev.seaglass.compose.SeaGlassSurface
import dev.seaglass.compose.SeaGlassTheme
import dev.seaglass.compose.animateSeaGlassMaterialAsState
import dev.seaglass.compose.rememberSeaGlassStageState
import dev.seaglass.compose.seaGlass
import dev.seaglass.compose.seaGlassBackdrop
import dev.seaglass.core.SeaGlassColorMath
import dev.seaglass.core.SeaGlassDeviceTier
import dev.seaglass.core.SeaGlassMaterial
import dev.seaglass.core.SeaGlassPalette
import dev.seaglass.core.SeaGlassSdfEngine
import dev.seaglass.core.SeaGlassTelemetry
import dev.seaglass.core.backend.SeaGlassShaderCompiler
import dev.seaglass.core.backend.ShaderDialect
import java.util.Locale
import kotlin.math.cos
import kotlin.math.roundToInt
import kotlin.math.sin

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        setContent {
            SeaGlassShowcaseApp()
        }
    }
}

@Composable
fun SeaGlassShowcaseApp() {
    var activePalette by remember { mutableStateOf(SeaGlassPalette.SeafoamMint) }
    var tierOverride by remember { mutableStateOf<SeaGlassDeviceTier?>(null) }
    var selectedTab by remember { mutableIntStateOf(0) }
    var actionOrbDetached by remember { mutableStateOf(false) }

    // Live adjustable optical parameters initialized from the selected palette
    var coarseness by remember(activePalette) {
        mutableFloatStateOf(activePalette.primaryGlass.coarseness)
    }
    var frostRadiusDp by remember(activePalette) {
        mutableFloatStateOf(activePalette.primaryGlass.frostRadiusDp)
    }
    var tintDensity by remember(activePalette) {
        mutableFloatStateOf(activePalette.primaryGlass.tintDensity)
    }
    var thicknessDp by remember(activePalette) {
        mutableFloatStateOf(activePalette.primaryGlass.thicknessDp)
    }
    var ior by remember(activePalette) {
        mutableFloatStateOf(activePalette.primaryGlass.ior)
    }
    var rimChalkiness by remember(activePalette) {
        mutableFloatStateOf(activePalette.primaryGlass.rimChalkiness)
    }

    val customPrimaryMaterial = remember(
        activePalette,
        coarseness,
        frostRadiusDp,
        tintDensity,
        thicknessDp,
        ior,
        rimChalkiness
    ) {
        activePalette.primaryGlass.copy(
            coarseness = coarseness,
            frostRadiusDp = frostRadiusDp,
            tintDensity = tintDensity,
            thicknessDp = thicknessDp,
            ior = ior,
            rimChalkiness = rimChalkiness
        )
    }

    val customSurfaceMaterial = remember(
        activePalette,
        coarseness,
        frostRadiusDp,
        tintDensity,
        thicknessDp,
        ior,
        rimChalkiness
    ) {
        activePalette.surfaceGlass.copy(
            coarseness = (coarseness + 0.08f).coerceIn(0f, 1f),
            frostRadiusDp = (frostRadiusDp + 4f).coerceAtMost(56f),
            tintDensity = (tintDensity * 0.72f).coerceIn(0.15f, 0.90f),
            thicknessDp = thicknessDp,
            ior = ior,
            rimChalkiness = (rimChalkiness + 0.06f).coerceIn(0f, 1f)
        )
    }

    SeaGlassTheme(
        palette = activePalette,
        tierOverride = tierOverride
    ) {
        val stageState = rememberSeaGlassStageState()
        val systemBarsPadding = WindowInsets.systemBars.asPaddingValues()

        Box(modifier = Modifier.fillMaxSize()) {
            // 1. Expressive high-contrast coastal background captured into the GPU stage
            ExpressiveTidalBackdrop(
                palette = activePalette,
                modifier = Modifier
                    .fillMaxSize()
                    .seaGlassBackdrop(stageState)
            )

            // 2. Foreground scrollable SeaGlass showcase content
            Column(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(
                        top = systemBarsPadding.calculateTopPadding() + 12.dp,
                        bottom = systemBarsPadding.calculateBottomPadding() + 92.dp,
                        start = 16.dp,
                        end = 16.dp
                    )
                    .verticalScroll(rememberScrollState()),
                verticalArrangement = Arrangement.spacedBy(16.dp)
            ) {
                // Top Header Bar in frosted SeaGlass
                HeaderSpecimenBanner(
                    stageState = stageState,
                    palette = activePalette,
                    material = customPrimaryMaterial,
                    onCyclePalette = {
                        val all = SeaGlassPalette.entries
                        activePalette = all[(activePalette.ordinal + 1) % all.size]
                    }
                )

                // Quick Palette Pill Strip
                PaletteQuickStrip(
                    stageState = stageState,
                    activePalette = activePalette,
                    onSelectPalette = { activePalette = it }
                )

                when (selectedTab) {
                    0 -> OpticsLabSection(
                        stageState = stageState,
                        primaryMaterial = customPrimaryMaterial,
                        surfaceMaterial = customSurfaceMaterial,
                        coarseness = coarseness,
                        onCoarsenessChange = { coarseness = it },
                        frostRadiusDp = frostRadiusDp,
                        onFrostRadiusChange = { frostRadiusDp = it },
                        tintDensity = tintDensity,
                        onTintDensityChange = { tintDensity = it },
                        thicknessDp = thicknessDp,
                        onThicknessChange = { thicknessDp = it },
                        ior = ior,
                        onIorChange = { ior = it },
                        rimChalkiness = rimChalkiness,
                        onRimChalkinessChange = { rimChalkiness = it }
                    )

                    1 -> FluidCoalescingSection(
                        stageState = stageState,
                        primaryMaterial = customPrimaryMaterial,
                        tertiaryMaterial = activePalette.tertiaryGlass.copy(
                            coarseness = coarseness,
                            frostRadiusDp = frostRadiusDp
                        ),
                        surfaceMaterial = customSurfaceMaterial
                    )

                    2 -> PaletteGallerySection(
                        stageState = stageState,
                        activePalette = activePalette,
                        onSelectPalette = { activePalette = it }
                    )

                    else -> BackendArchitectureSection(
                        stageState = stageState,
                        surfaceMaterial = customSurfaceMaterial,
                        primaryMaterial = customPrimaryMaterial,
                        tierOverride = tierOverride,
                        onSelectTierOverride = { tierOverride = it }
                    )
                }
            }

            // 3. Bottom Adaptive Coalescing SeaGlass Fluid Bar
            Box(
                modifier = Modifier
                    .align(Alignment.BottomCenter)
                    .fillMaxWidth()
                    .padding(
                        start = 16.dp,
                        end = 16.dp,
                        bottom = systemBarsPadding.calculateBottomPadding() + 12.dp
                    )
            ) {
                SeaGlassFluidBar(
                    items = listOf("Optics", "Fluid SDF", "Palettes", "Backends"),
                    selectedIndex = selectedTab,
                    onSelect = { selectedTab = it },
                    stageState = stageState,
                    barMaterial = customSurfaceMaterial,
                    indicatorMaterial = customPrimaryMaterial,
                    actionLabel = if (actionOrbDetached) "⇄" else "✦",
                    actionDetached = actionOrbDetached,
                    onActionClick = { actionOrbDetached = !actionOrbDetached }
                )
            }
        }
    }
}

@Composable
private fun HeaderSpecimenBanner(
    stageState: SeaGlassStageState,
    palette: SeaGlassPalette,
    material: SeaGlassMaterial,
    onCyclePalette: () -> Unit
) {
    SeaGlassCard(
        stageState = stageState,
        material = material.copy(cornerRadiusDp = 32f),
        modifier = Modifier.fillMaxWidth()
    ) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = "SEAGLASS FOUNDATION",
                    style = MaterialTheme.typography.labelSmall.copy(
                        fontWeight = FontWeight.ExtraBold,
                        letterSpacing = 1.6.sp
                    ),
                    color = LocalContentColor.current.copy(alpha = 0.75f)
                )
                Spacer(modifier = Modifier.height(2.dp))
                Text(
                    text = palette.displayName,
                    style = MaterialTheme.typography.headlineSmall.copy(
                        fontWeight = FontWeight.Black
                    ),
                    color = LocalContentColor.current
                )
            }

            SeaGlassButton(
                onClick = onCyclePalette,
                stageState = stageState,
                material = palette.tertiaryGlass.copy(cornerRadiusDp = 22f),
                contentPadding = PaddingValues(horizontal = 16.dp, vertical = 10.dp)
            ) {
                Text(
                    text = "Next Tide ↻",
                    style = MaterialTheme.typography.labelLarge.copy(
                        fontWeight = FontWeight.Bold
                    )
                )
            }
        }

        Text(
            text = palette.tagline,
            style = MaterialTheme.typography.bodyMedium,
            color = LocalContentColor.current.copy(alpha = 0.88f)
        )
    }
}

@Composable
private fun PaletteQuickStrip(
    stageState: SeaGlassStageState,
    activePalette: SeaGlassPalette,
    onSelectPalette: (SeaGlassPalette) -> Unit
) {
    val featured = remember {
        listOf(
            SeaGlassPalette.SeafoamMint,
            SeaGlassPalette.CobaltTide,
            SeaGlassPalette.BeachAmber,
            SeaGlassPalette.SeaLavender
        )
    }

    Row(
        modifier = Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.spacedBy(8.dp)
    ) {
        featured.forEach { palette ->
            val selected = palette == activePalette
            SeaGlassPill(
                label = palette.displayName.substringBefore(" "),
                selected = selected,
                onClick = { onSelectPalette(palette) },
                stageState = stageState,
                selectedMaterial = palette.primaryGlass,
                unselectedMaterial = palette.surfaceGlass,
                modifier = Modifier.weight(1f)
            )
        }
    }
}

@Composable
private fun OpticsLabSection(
    stageState: SeaGlassStageState,
    primaryMaterial: SeaGlassMaterial,
    surfaceMaterial: SeaGlassMaterial,
    coarseness: Float,
    onCoarsenessChange: (Float) -> Unit,
    frostRadiusDp: Float,
    onFrostRadiusChange: (Float) -> Unit,
    tintDensity: Float,
    onTintDensityChange: (Float) -> Unit,
    thicknessDp: Float,
    onThicknessChange: (Float) -> Unit,
    ior: Float,
    onIorChange: (Float) -> Unit,
    rimChalkiness: Float,
    onRimChalkinessChange: (Float) -> Unit
) {
    // Live Dual-Lens Specimen Comparing Heavy Sanded Frost vs Lightly Tumbled Edge Refraction
    Row(
        modifier = Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.spacedBy(12.dp)
    ) {
        SeaGlassCard(
            stageState = stageState,
            material = primaryMaterial.copy(cornerRadiusDp = 30f),
            modifier = Modifier
                .weight(1f)
                .height(148.dp)
        ) {
            Text(
                text = "PRIMARY SPECIMEN",
                style = MaterialTheme.typography.labelSmall.copy(
                    fontWeight = FontWeight.Bold,
                    letterSpacing = 1.1.sp
                ),
                color = LocalContentColor.current.copy(alpha = 0.72f)
            )
            Spacer(modifier = Modifier.weight(1f))
            Text(
                text = String.format(Locale.US, "Grit %.0f%%", coarseness * 100f),
                style = MaterialTheme.typography.titleLarge.copy(fontWeight = FontWeight.Black)
            )
            Text(
                text = String.format(
                    Locale.US,
                    "IOR %.2f · %.0fdp frost",
                    ior,
                    frostRadiusDp
                ),
                style = MaterialTheme.typography.labelMedium,
                color = LocalContentColor.current.copy(alpha = 0.82f)
            )
        }

        SeaGlassCard(
            stageState = stageState,
            material = LocalSeaGlassPalette.current.secondaryGlass.copy(
                coarseness = (coarseness + 0.18f).coerceAtMost(1f),
                frostRadiusDp = (frostRadiusDp + 10f).coerceAtMost(56f),
                tintDensity = tintDensity,
                cornerRadiusDp = 38f
            ),
            modifier = Modifier
                .weight(1f)
                .height(148.dp)
        ) {
            Text(
                text = "HEAVY SURF FROST",
                style = MaterialTheme.typography.labelSmall.copy(
                    fontWeight = FontWeight.Bold,
                    letterSpacing = 1.1.sp
                ),
                color = LocalContentColor.current.copy(alpha = 0.72f)
            )
            Spacer(modifier = Modifier.weight(1f))
            Text(
                text = "Chalky Matte",
                style = MaterialTheme.typography.titleLarge.copy(fontWeight = FontWeight.Black)
            )
            Text(
                text = "Oklab Beer-Lambert",
                style = MaterialTheme.typography.labelMedium,
                color = LocalContentColor.current.copy(alpha = 0.82f)
            )
        }
    }

    // Optical Parameter Controls Card
    SeaGlassCard(
        stageState = stageState,
        material = surfaceMaterial,
        modifier = Modifier.fillMaxWidth()
    ) {
        Text(
            text = "SEA GLASS OPTICAL PHYSICS",
            style = MaterialTheme.typography.labelMedium.copy(
                fontWeight = FontWeight.ExtraBold,
                letterSpacing = 1.2.sp
            ),
            color = LocalContentColor.current.copy(alpha = 0.75f)
        )

        OpticalSliderRow(
            title = "Sand Coarseness (Micro-Grit)",
            valueText = String.format(Locale.US, "%.2f", coarseness),
            value = coarseness,
            valueRange = 0.0f..1.0f,
            onValueChange = onCoarsenessChange
        )

        OpticalSliderRow(
            title = "Subsurface Frost Blur",
            valueText = String.format(Locale.US, "%.0f dp", frostRadiusDp),
            value = frostRadiusDp,
            valueRange = 2f..48f,
            onValueChange = onFrostRadiusChange
        )

        OpticalSliderRow(
            title = "Beer-Lambert Tint Density",
            valueText = String.format(Locale.US, "%.2f", tintDensity),
            value = tintDensity,
            valueRange = 0.12f..0.92f,
            onValueChange = onTintDensityChange
        )

        OpticalSliderRow(
            title = "Glass Slab Thickness",
            valueText = String.format(Locale.US, "%.0f dp", thicknessDp),
            value = thicknessDp,
            valueRange = 4f..32f,
            onValueChange = onThicknessChange
        )

        OpticalSliderRow(
            title = "Edge Refraction (IOR)",
            valueText = String.format(Locale.US, "%.2f", ior),
            value = ior,
            valueRange = 1.05f..1.75f,
            onValueChange = onIorChange
        )

        OpticalSliderRow(
            title = "Chalky Rim Glow (Fresnel)",
            valueText = String.format(Locale.US, "%.2f", rimChalkiness),
            value = rimChalkiness,
            valueRange = 0.10f..1.0f,
            onValueChange = onRimChalkinessChange
        )
    }
}

@Composable
private fun OpticalSliderRow(
    title: String,
    valueText: String,
    value: Float,
    valueRange: ClosedFloatingPointRange<Float>,
    onValueChange: (Float) -> Unit
) {
    Column(modifier = Modifier.fillMaxWidth()) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween
        ) {
            Text(
                text = title,
                style = MaterialTheme.typography.bodySmall.copy(fontWeight = FontWeight.SemiBold),
                color = LocalContentColor.current
            )
            Text(
                text = valueText,
                style = MaterialTheme.typography.bodySmall.copy(
                    fontFamily = FontFamily.Monospace,
                    fontWeight = FontWeight.Bold
                ),
                color = LocalContentColor.current
            )
        }
        Slider(
            value = value,
            onValueChange = onValueChange,
            valueRange = valueRange,
            colors = SliderDefaults.colors(
                thumbColor = LocalContentColor.current,
                activeTrackColor = LocalContentColor.current.copy(alpha = 0.78f),
                inactiveTrackColor = LocalContentColor.current.copy(alpha = 0.20f)
            ),
            modifier = Modifier.height(28.dp)
        )
    }
}

@Composable
private fun FluidCoalescingSection(
    stageState: SeaGlassStageState,
    primaryMaterial: SeaGlassMaterial,
    tertiaryMaterial: SeaGlassMaterial,
    surfaceMaterial: SeaGlassMaterial
) {
    val motion = LocalSeaGlassMotionScheme.current
    val density = LocalDensity.current

    var coalescedPreset by remember { mutableStateOf(true) }
    var smoothBridgeDp by remember { mutableFloatStateOf(34f) }
    var dragOffsetPx by remember { mutableStateOf(Offset.Zero) }

    val targetSeparationDp = if (coalescedPreset) 74f else 172f
    val animatedSepDp by animateFloatAsState(
        targetValue = targetSeparationDp,
        animationSpec = motion.defaultSpatial,
        label = "dropletSeparation"
    )

    SeaGlassCard(
        stageState = stageState,
        material = surfaceMaterial,
        modifier = Modifier.fillMaxWidth()
    ) {
        Text(
            text = "LIQUID SDF COALESCING (SMIN)",
            style = MaterialTheme.typography.labelMedium.copy(
                fontWeight = FontWeight.ExtraBold,
                letterSpacing = 1.2.sp
            ),
            color = LocalContentColor.current.copy(alpha = 0.75f)
        )
        Text(
            text = "Drag the right droplet or tap Merge/Split. Both shapes share a single 256-byte std140 UBO and blend their Oklab tints across the cubic smooth-minimum bridge.",
            style = MaterialTheme.typography.bodySmall,
            color = LocalContentColor.current.copy(alpha = 0.85f)
        )

        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(10.dp)
        ) {
            SeaGlassButton(
                onClick = {
                    coalescedPreset = true
                    dragOffsetPx = Offset.Zero
                },
                stageState = stageState,
                material = primaryMaterial,
                modifier = Modifier.weight(1f)
            ) {
                Text("Merge Droplets", fontWeight = FontWeight.Bold)
            }
            SeaGlassButton(
                onClick = {
                    coalescedPreset = false
                    dragOffsetPx = Offset.Zero
                },
                stageState = stageState,
                material = tertiaryMaterial,
                modifier = Modifier.weight(1f)
            ) {
                Text("Split Apart", fontWeight = FontWeight.Bold)
            }
        }

        OpticalSliderRow(
            title = "Smooth-Min Bridge Radius (k)",
            valueText = String.format(Locale.US, "%.0f dp", smoothBridgeDp),
            value = smoothBridgeDp,
            valueRange = 4f..56f,
            onValueChange = { smoothBridgeDp = it }
        )
    }

    // Live Multi-Node SDF Arena rendered directly over the animated backdrop
    val arenaHeight = 210.dp
    val arenaHeightPx = with(density) { arenaHeight.toPx() }
    var arenaWidthPx by remember { mutableFloatStateOf(900f) }

    val centerAX = arenaWidthPx * 0.34f
    val centerAY = arenaHeightPx * 0.50f
    val sepPx = with(density) { animatedSepDp.dp.toPx() }
    val centerBX = (centerAX + sepPx + dragOffsetPx.x).coerceIn(80f, arenaWidthPx - 80f)
    val centerBY = (centerAY + dragOffsetPx.y).coerceIn(65f, arenaHeightPx - 65f)

    val dropletARadiusPx = with(density) { 58.dp.toPx() }
    val dropletBRadiusPx = with(density) { 50.dp.toPx() }

    val arenaNodes = remember(
        centerAX,
        centerAY,
        centerBX,
        centerBY,
        dropletARadiusPx,
        dropletBRadiusPx,
        primaryMaterial,
        tertiaryMaterial
    ) {
        listOf(
            SeaGlassSdfEngine.SdfNode(
                centerX = centerAX,
                centerY = centerAY,
                halfWidth = dropletARadiusPx * 1.18f,
                halfHeight = dropletARadiusPx * 0.92f,
                uniformCornerRadius = dropletARadiusPx * 0.72f,
                cornerSmoothing = 2.6f,
                tintColorArgb = primaryMaterial.tintColorArgb,
                thicknessPx = dropletARadiusPx * 0.55f
            ),
            SeaGlassSdfEngine.SdfNode(
                centerX = centerBX,
                centerY = centerBY,
                halfWidth = dropletBRadiusPx,
                halfHeight = dropletBRadiusPx,
                uniformCornerRadius = dropletBRadiusPx,
                cornerSmoothing = 2.5f,
                tintColorArgb = tertiaryMaterial.tintColorArgb,
                thicknessPx = dropletBRadiusPx * 0.60f
            )
        )
    }

    Box(
        modifier = Modifier
            .fillMaxWidth()
            .height(arenaHeight)
            .pointerInput(Unit) {
                detectDragGestures { change, dragAmount ->
                    change.consume()
                    dragOffsetPx += dragAmount
                }
            }
            .seaGlass(
                stageState = stageState,
                material = primaryMaterial,
                nodesOptional = arenaNodes,
                smoothRadiusDp = smoothBridgeDp
            )
    ) {
        val labelColor = Color(
            SeaGlassColorMath.optimalContentColorFor(
                primaryMaterial.tintColorArgb,
                primaryMaterial.tintDensity
            )
        )
        Box(
            contentAlignment = Alignment.Center,
            modifier = Modifier
                .offset {
                    IntOffset(
                        (centerAX - dropletARadiusPx).roundToInt(),
                        (centerAY - 20f).roundToInt()
                    )
                }
                .width(with(density) { (dropletARadiusPx * 2f).toDp() })
        ) {
            Text(
                text = "ANCHOR",
                style = MaterialTheme.typography.labelLarge.copy(fontWeight = FontWeight.Black),
                color = labelColor
            )
        }

        Box(
            contentAlignment = Alignment.Center,
            modifier = Modifier
                .offset {
                    IntOffset(
                        (centerBX - dropletBRadiusPx).roundToInt(),
                        (centerBY - 20f).roundToInt()
                    )
                }
                .width(with(density) { (dropletBRadiusPx * 2f).toDp() })
        ) {
            Text(
                text = "DRAG ME",
                style = MaterialTheme.typography.labelLarge.copy(fontWeight = FontWeight.Black),
                color = labelColor
            )
        }
    }
}

@Composable
private fun PaletteGallerySection(
    stageState: SeaGlassStageState,
    activePalette: SeaGlassPalette,
    onSelectPalette: (SeaGlassPalette) -> Unit
) {
    SeaGlassPalette.entries.chunked(2).forEach { pair ->
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            pair.forEach { palette ->
                val isSelected = palette == activePalette
                SeaGlassCard(
                    stageState = stageState,
                    material = palette.primaryGlass.copy(
                        cornerRadiusDp = if (isSelected) 32f else 24f,
                        rimChalkiness = if (isSelected) 0.92f else palette.primaryGlass.rimChalkiness
                    ),
                    onClick = { onSelectPalette(palette) },
                    modifier = Modifier
                        .weight(1f)
                        .height(136.dp)
                ) {
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Box(
                            modifier = Modifier
                                .size(18.dp)
                                .clip(CircleShape)
                                .background(Color(palette.primaryGlass.tintColorArgb))
                        )
                        if (isSelected) {
                            Text(
                                text = "ACTIVE",
                                style = MaterialTheme.typography.labelSmall.copy(
                                    fontWeight = FontWeight.Black
                                )
                            )
                        }
                    }
                    Spacer(modifier = Modifier.weight(1f))
                    Text(
                        text = palette.displayName,
                        style = MaterialTheme.typography.titleMedium.copy(
                            fontWeight = FontWeight.Bold
                        )
                    )
                    Text(
                        text = String.format(
                            Locale.US,
                            "Grit %.0f%% · Density %.0f%%",
                            palette.primaryGlass.coarseness * 100f,
                            palette.primaryGlass.tintDensity * 100f
                        ),
                        style = MaterialTheme.typography.labelSmall,
                        color = LocalContentColor.current.copy(alpha = 0.80f)
                    )
                }
            }
        }
    }
}

@Composable
private fun BackendArchitectureSection(
    stageState: SeaGlassStageState,
    surfaceMaterial: SeaGlassMaterial,
    primaryMaterial: SeaGlassMaterial,
    tierOverride: SeaGlassDeviceTier?,
    onSelectTierOverride: (SeaGlassDeviceTier?) -> Unit
) {
    val context = LocalContext.current
    val telemetry = remember(context, tierOverride) {
        SeaGlassTelemetry.capture(context, tierOverride)
    }
    var selectedDialect by remember { mutableStateOf(ShaderDialect.VULKAN_GLSL_450) }
    val compiledBundle = remember(selectedDialect) {
        SeaGlassShaderCompiler.compileForDialect(selectedDialect)
    }

    SeaGlassCard(
        stageState = stageState,
        material = surfaceMaterial,
        modifier = Modifier.fillMaxWidth()
    ) {
        Text(
            text = "BACKEND-AGNOSTIC GPU ENGINE",
            style = MaterialTheme.typography.labelMedium.copy(
                fontWeight = FontWeight.ExtraBold,
                letterSpacing = 1.2.sp
            ),
            color = LocalContentColor.current.copy(alpha = 0.75f)
        )
        Text(
            text = "Active: ${telemetry.activeTier.displayName}",
            style = MaterialTheme.typography.titleMedium.copy(fontWeight = FontWeight.Black)
        )
        Text(
            text = "Device: ${telemetry.deviceModel} (API ${telemetry.sdkInt}) · Vulkan 1.1: ${if (telemetry.vulkan11Supported) "Ready" else "N/A"} · std140 UBO: ${telemetry.uboByteSize} bytes",
            style = MaterialTheme.typography.bodySmall,
            color = LocalContentColor.current.copy(alpha = 0.85f)
        )

        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            SeaGlassPill(
                label = "Auto Tier",
                selected = tierOverride == null,
                onClick = { onSelectTierOverride(null) },
                stageState = stageState,
                selectedMaterial = primaryMaterial,
                unselectedMaterial = surfaceMaterial,
                modifier = Modifier.weight(1f)
            )
            SeaGlassPill(
                label = "Vulkan/AGSL",
                selected = tierOverride == SeaGlassDeviceTier.VULKAN_NATIVE,
                onClick = { onSelectTierOverride(SeaGlassDeviceTier.VULKAN_NATIVE) },
                stageState = stageState,
                selectedMaterial = primaryMaterial,
                unselectedMaterial = surfaceMaterial,
                modifier = Modifier.weight(1f)
            )
            SeaGlassPill(
                label = "Lite Frost",
                selected = tierOverride == SeaGlassDeviceTier.LIGHTWEIGHT_TINT,
                onClick = { onSelectTierOverride(SeaGlassDeviceTier.LIGHTWEIGHT_TINT) },
                stageState = stageState,
                selectedMaterial = primaryMaterial,
                unselectedMaterial = surfaceMaterial,
                modifier = Modifier.weight(1f)
            )
        }
    }

    SeaGlassCard(
        stageState = stageState,
        material = surfaceMaterial,
        modifier = Modifier.fillMaxWidth()
    ) {
        Text(
            text = "PORTABLE SHADER COMPILER TARGETS",
            style = MaterialTheme.typography.labelMedium.copy(
                fontWeight = FontWeight.ExtraBold,
                letterSpacing = 1.2.sp
            ),
            color = LocalContentColor.current.copy(alpha = 0.75f)
        )
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            ShaderDialect.entries.forEach { dialect ->
                SeaGlassPill(
                    label = when (dialect) {
                        ShaderDialect.VULKAN_GLSL_450 -> "Vulkan 450"
                        ShaderDialect.GLSL_ES_300 -> "GLES 3.00"
                        ShaderDialect.AGSL_100 -> "AGSL 1.0"
                    },
                    selected = selectedDialect == dialect,
                    onClick = { selectedDialect = dialect },
                    stageState = stageState,
                    selectedMaterial = primaryMaterial,
                    unselectedMaterial = surfaceMaterial,
                    modifier = Modifier.weight(1f)
                )
            }
        }

        val snippet = remember(compiledBundle) {
            compiledBundle.seaGlassCompositeFragmentSource
                .lines()
                .take(18)
                .joinToString("\n")
        }

        Text(
            text = snippet,
            style = MaterialTheme.typography.bodySmall.copy(
                fontFamily = FontFamily.Monospace,
                fontSize = 11.sp,
                lineHeight = 15.sp
            ),
            color = LocalContentColor.current.copy(alpha = 0.90f),
            modifier = Modifier
                .fillMaxWidth()
                .clip(RoundedCornerShape(16.dp))
                .background(Color.Black.copy(alpha = 0.14f))
                .padding(12.dp)
        )
    }
}

/**
 * Rich, animated Material 3 Expressive coastal backdrop with high-contrast geometric ribbons,
 * floating pastel orbs, and editorial grid lines so real-time sea-glass refraction, frosted
 * sand-grain coarseness, and Beer-Lambert volumetric tinting are immediately visible.
 */
@Composable
private fun ExpressiveTidalBackdrop(
    palette: SeaGlassPalette,
    modifier: Modifier = Modifier
) {
    val infiniteTransition = rememberInfiniteTransition(label = "tidalBackdrop")
    val phase by infiniteTransition.animateFloat(
        initialValue = 0f,
        targetValue = (2.0 * Math.PI).toFloat(),
        animationSpec = infiniteRepeatable(
            animation = tween(durationMillis = 14000, easing = LinearEasing),
            repeatMode = RepeatMode.Restart
        ),
        label = "tidalPhase"
    )

    val isDark = palette == SeaGlassPalette.ObsidianTide
    val bgTop = if (isDark) Color(0xFF0A1520) else Color(0xFFEAF4F8)
    val bgBottom = if (isDark) Color(0xFF13283C) else Color(0xFFD5E9F2)

    val orbPrimary = Color(palette.primaryGlass.tintColorArgb)
    val orbTertiary = Color(palette.tertiaryGlass.tintColorArgb)
    val coralAccent = Color(0xFFFF7D6B)
    val goldAccent = Color(0xFFFFC857)
    val cobaltAccent = Color(0xFF3B7BFF)

    Canvas(modifier = modifier) {
        val w = size.width
        val h = size.height

        drawRect(
            brush = Brush.verticalGradient(
                colors = listOf(bgTop, bgBottom)
            )
        )

        // Subtle optical calibration grid so IOR edge refraction bending is clearly observable
        val gridStep = 44.dp.toPx()
        val gridColor = if (isDark) {
            Color.White.copy(alpha = 0.07f)
        } else {
            Color(0xFF10324A).copy(alpha = 0.08f)
        }
        var gx = 0f
        while (gx < w) {
            drawLine(gridColor, Offset(gx, 0f), Offset(gx, h), strokeWidth = 1.5f)
            gx += gridStep
        }
        var gy = 0f
        while (gy < h) {
            drawLine(gridColor, Offset(0f, gy), Offset(w, gy), strokeWidth = 1.5f)
            gy += gridStep
        }

        // Animated Material 3 Expressive organic ribbons and pebbles behind the glass
        val dx1 = cos(phase) * w * 0.14f
        val dy1 = sin(phase) * h * 0.08f

        drawCircle(
            brush = Brush.radialGradient(
                colors = listOf(orbPrimary.copy(alpha = 0.85f), orbPrimary.copy(alpha = 0.0f)),
                center = Offset(w * 0.25f + dx1, h * 0.22f + dy1),
                radius = w * 0.48f
            ),
            radius = w * 0.48f,
            center = Offset(w * 0.25f + dx1, h * 0.22f + dy1)
        )

        drawCircle(
            brush = Brush.radialGradient(
                colors = listOf(coralAccent.copy(alpha = 0.78f), coralAccent.copy(alpha = 0.0f)),
                center = Offset(w * 0.78f - dx1 * 0.8f, h * 0.36f + dy1 * 0.6f),
                radius = w * 0.42f
            ),
            radius = w * 0.42f,
            center = Offset(w * 0.78f - dx1 * 0.8f, h * 0.36f + dy1 * 0.6f)
        )

        drawCircle(
            brush = Brush.radialGradient(
                colors = listOf(cobaltAccent.copy(alpha = 0.72f), cobaltAccent.copy(alpha = 0.0f)),
                center = Offset(w * 0.30f - dy1, h * 0.68f - dx1 * 0.5f),
                radius = w * 0.46f
            ),
            radius = w * 0.46f,
            center = Offset(w * 0.30f - dy1, h * 0.68f - dx1 * 0.5f)
        )

        drawCircle(
            brush = Brush.radialGradient(
                colors = listOf(goldAccent.copy(alpha = 0.80f), goldAccent.copy(alpha = 0.0f)),
                center = Offset(w * 0.75f + dy1 * 0.7f, h * 0.82f + dx1 * 0.4f),
                radius = w * 0.40f
            ),
            radius = w * 0.40f,
            center = Offset(w * 0.75f + dy1 * 0.7f, h * 0.82f + dx1 * 0.4f)
        )

        // Crisp high-contrast M3 Expressive geometric pills behind the glass to demonstrate blur & refraction
        rotate(degrees = -14f, pivot = Offset(w * 0.5f, h * 0.32f)) {
            drawRoundRect(
                color = orbTertiary.copy(alpha = 0.88f),
                topLeft = Offset(w * 0.12f + dx1 * 0.3f, h * 0.26f),
                size = Size(w * 0.76f, 68.dp.toPx()),
                cornerRadius = CornerRadius(34.dp.toPx(), 34.dp.toPx())
            )
        }

        rotate(degrees = 11f, pivot = Offset(w * 0.5f, h * 0.58f)) {
            drawRoundRect(
                color = coralAccent.copy(alpha = 0.82f),
                topLeft = Offset(w * 0.10f - dx1 * 0.25f, h * 0.54f),
                size = Size(w * 0.80f, 64.dp.toPx()),
                cornerRadius = CornerRadius(32.dp.toPx(), 32.dp.toPx())
            )
        }

        // Bold wave contour lines
        val wavePath = Path().apply {
            moveTo(0f, h * 0.44f)
            cubicTo(
                w * 0.33f,
                h * (0.36f + 0.04f * sin(phase)),
                w * 0.66f,
                h * (0.52f - 0.04f * cos(phase)),
                w,
                h * 0.44f
            )
        }
        drawPath(
            path = wavePath,
            color = if (isDark) Color.White.copy(alpha = 0.35f) else Color(0xFF0D2B3E).copy(alpha = 0.28f),
            style = Stroke(width = 5.dp.toPx())
        )
    }
}
