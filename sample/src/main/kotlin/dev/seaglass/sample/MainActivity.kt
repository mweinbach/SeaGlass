package dev.seaglass.sample

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.animation.core.LinearEasing
import androidx.compose.animation.core.RepeatMode
import androidx.compose.animation.core.animateDpAsState
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.spring
import androidx.compose.animation.core.tween
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.gestures.detectDragGestures
import androidx.compose.foundation.interaction.MutableInteractionSource
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
import androidx.compose.ui.layout.onSizeChanged
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.drawText
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.rememberTextMeasurer
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

    // Live adjustable optical parameters initialized for transparent frosted sea glass + liquid meniscus refraction
    var transparency by remember(activePalette) {
        mutableFloatStateOf(0.74f)
    }
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
        mutableFloatStateOf(18f)
    }
    var ior by remember(activePalette) {
        mutableFloatStateOf(1.58f)
    }
    var rimChalkiness by remember(activePalette) {
        mutableFloatStateOf(0.78f)
    }

    val customPrimaryMaterial = remember(
        activePalette,
        transparency,
        coarseness,
        frostRadiusDp,
        tintDensity,
        thicknessDp,
        ior,
        rimChalkiness
    ) {
        activePalette.primaryGlass.copy(
            transparency = transparency,
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
        transparency,
        coarseness,
        frostRadiusDp,
        tintDensity,
        thicknessDp,
        ior,
        rimChalkiness
    ) {
        activePalette.surfaceGlass.copy(
            transparency = (transparency + 0.06f).coerceIn(0f, 0.96f),
            coarseness = (coarseness + 0.06f).coerceIn(0f, 1f),
            frostRadiusDp = (frostRadiusDp + 3f).coerceAtMost(56f),
            tintDensity = (tintDensity * 0.65f).coerceIn(0.12f, 0.85f),
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
            // 1. Vibrant, high-contrast Material 3 Expressive coastal artwork captured into the GPU stage
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
                        top = systemBarsPadding.calculateTopPadding() + 10.dp,
                        bottom = systemBarsPadding.calculateBottomPadding() + 94.dp,
                        start = 16.dp,
                        end = 16.dp
                    )
                    .verticalScroll(rememberScrollState()),
                verticalArrangement = Arrangement.spacedBy(14.dp)
            ) {
                // Top Morphing Header Specimen Card
                HeaderSpecimenBanner(
                    stageState = stageState,
                    palette = activePalette,
                    material = customPrimaryMaterial,
                    onCyclePalette = {
                        val all = SeaGlassPalette.entries
                        activePalette = all[(activePalette.ordinal + 1) % all.size]
                    }
                )

                // Expressive Palette Strip
                PaletteQuickStrip(
                    stageState = stageState,
                    activePalette = activePalette,
                    onSelectPalette = { activePalette = it }
                )

                when (selectedTab) {
                    0 -> OpticsLabSection(
                        stageState = stageState,
                        primaryMaterial = customPrimaryMaterial,
                        tertiaryMaterial = activePalette.tertiaryGlass.copy(
                            transparency = transparency,
                            coarseness = coarseness,
                            frostRadiusDp = frostRadiusDp,
                            ior = ior,
                            thicknessDp = thicknessDp
                        ),
                        surfaceMaterial = customSurfaceMaterial,
                        transparency = transparency,
                        onTransparencyChange = { transparency = it },
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
                        onRimChalkinessChange = { rimChalkiness = it },
                        onApplyPreset = { tr, c, f, d, t, i, r ->
                            transparency = tr
                            coarseness = c
                            frostRadiusDp = f
                            tintDensity = d
                            thicknessDp = t
                            ior = i
                            rimChalkiness = r
                        }
                    )

                    1 -> FluidCoalescingSection(
                        stageState = stageState,
                        primaryMaterial = customPrimaryMaterial,
                        tertiaryMaterial = activePalette.tertiaryGlass.copy(
                            transparency = transparency,
                            coarseness = coarseness,
                            frostRadiusDp = frostRadiusDp,
                            ior = ior,
                            thicknessDp = thicknessDp
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
    var morphedExpanded by remember { mutableStateOf(false) }
    val cornerRadius by animateFloatAsState(
        targetValue = if (morphedExpanded) 44f else 30f,
        animationSpec = spring(dampingRatio = 0.58f, stiffness = 340f),
        label = "headerCornerMorph"
    )
    val extraThickness by animateFloatAsState(
        targetValue = if (morphedExpanded) 8f else 0f,
        animationSpec = spring(dampingRatio = 0.62f, stiffness = 360f),
        label = "headerThicknessMorph"
    )

    SeaGlassCard(
        stageState = stageState,
        material = material.copy(
            cornerRadiusDp = cornerRadius,
            thicknessDp = (material.thicknessDp + extraThickness).coerceAtMost(34f)
        ),
        onClick = { morphedExpanded = !morphedExpanded },
        modifier = Modifier.fillMaxWidth()
    ) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = "LIQUID SEA GLASS · M3 EXPRESSIVE",
                    style = MaterialTheme.typography.labelSmall.copy(
                        fontWeight = FontWeight.ExtraBold,
                        letterSpacing = 1.5.sp
                    ),
                    color = LocalContentColor.current.copy(alpha = 0.78f)
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
                material = palette.tertiaryGlass.copy(
                    cornerRadiusDp = 24f,
                    thicknessDp = 18f,
                    rimChalkiness = 0.88f
                ),
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
            style = MaterialTheme.typography.bodyMedium.copy(fontWeight = FontWeight.Medium),
            color = LocalContentColor.current.copy(alpha = 0.90f)
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
                selectedMaterial = palette.primaryGlass.copy(thicknessDp = 18f),
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
    tertiaryMaterial: SeaGlassMaterial,
    surfaceMaterial: SeaGlassMaterial,
    transparency: Float,
    onTransparencyChange: (Float) -> Unit,
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
    onRimChalkinessChange: (Float) -> Unit,
    onApplyPreset: (Float, Float, Float, Float, Float, Float, Float) -> Unit
) {
    val motion = LocalSeaGlassMotionScheme.current
    val density = LocalDensity.current

    // Interactive 3-State Coalescing Hero Lens Deck right on the main tab!
    // State 0 = Hourglass Liquid Bridge, State 1 = Pinching Surface-Tension Neck, State 2 = Split Teardrops
    var heroFlowState by remember { mutableIntStateOf(0) }
    val heroSeparationDp by animateFloatAsState(
        targetValue = when (heroFlowState) {
            0 -> 114f
            1 -> 132f
            else -> 172f
        },
        animationSpec = spring(dampingRatio = 0.54f, stiffness = 300f),
        label = "heroCoalesce"
    )

    var deckWidthPx by remember { mutableFloatStateOf(960f) }
    val deckHeight = 172.dp
    val deckHeightPx = with(density) { deckHeight.toPx() }
    val rAPx = with(density) { 58.dp.toPx() }
    val rBPx = with(density) { 50.dp.toPx() }
    val sepPx = with(density) { heroSeparationDp.dp.toPx() }

    val centerAX = deckWidthPx * 0.33f
    val centerAY = deckHeightPx * 0.50f
    val centerBX = (centerAX + sepPx).coerceAtMost(deckWidthPx - rBPx - 16f)

    val heroNodes = remember(
        deckWidthPx,
        deckHeightPx,
        centerAX,
        centerAY,
        centerBX,
        rAPx,
        rBPx,
        primaryMaterial,
        tertiaryMaterial
    ) {
        listOf(
            SeaGlassSdfEngine.SdfNode(
                centerX = centerAX,
                centerY = centerAY,
                halfWidth = rAPx * 1.22f,
                halfHeight = rAPx * 0.96f,
                uniformCornerRadius = rAPx * 0.90f,
                cornerSmoothing = 2.4f,
                tintColorArgb = primaryMaterial.tintColorArgb,
                thicknessPx = primaryMaterial.thicknessDp * 2.8f
            ),
            SeaGlassSdfEngine.SdfNode(
                centerX = centerBX,
                centerY = centerAY,
                halfWidth = rBPx,
                halfHeight = rBPx,
                uniformCornerRadius = rBPx,
                cornerSmoothing = 2.4f,
                tintColorArgb = tertiaryMaterial.tintColorArgb,
                thicknessPx = tertiaryMaterial.thicknessDp * 2.8f
            )
        )
    }

    // Live Coalescing SeaGlass Specimen Lens
    Box(
        modifier = Modifier
            .fillMaxWidth()
            .height(deckHeight)
            .onSizeChanged { deckWidthPx = it.width.toFloat() }
            .clickable(
                interactionSource = remember { MutableInteractionSource() },
                indication = null,
                onClick = { heroFlowState = (heroFlowState + 1) % 3 }
            )
            .seaGlass(
                stageState = stageState,
                material = primaryMaterial,
                nodesOptional = heroNodes,
                smoothRadiusDp = 52f
            )
    ) {
        val labelColor = Color(
            SeaGlassColorMath.optimalContentColorFor(
                primaryMaterial.tintColorArgb,
                primaryMaterial.tintDensity
            )
        )

        Column(
            horizontalAlignment = Alignment.CenterHorizontally,
            modifier = Modifier
                .offset {
                    IntOffset(
                        (centerAX - rAPx * 1.05f).roundToInt().coerceAtLeast(12),
                        (centerAY - rAPx * 0.56f).roundToInt()
                    )
                }
                .width(with(density) { (rAPx * 2.1f).toDp() })
        ) {
            Text(
                text = when (heroFlowState) {
                    0 -> "LIQUID BRIDGE"
                    1 -> "PINCH NECK"
                    else -> "TEARDROP SPLIT"
                },
                style = MaterialTheme.typography.labelSmall.copy(
                    fontWeight = FontWeight.ExtraBold,
                    letterSpacing = 1.0.sp
                ),
                color = labelColor.copy(alpha = 0.78f)
            )
            Text(
                text = String.format(Locale.US, "Grit %.0f%%", coarseness * 100f),
                style = MaterialTheme.typography.titleMedium.copy(fontWeight = FontWeight.Black),
                color = labelColor
            )
            Text(
                text = "Tap to morph ⇄",
                style = MaterialTheme.typography.labelSmall.copy(fontWeight = FontWeight.Bold),
                color = labelColor.copy(alpha = 0.85f)
            )
        }

        Box(
            contentAlignment = Alignment.Center,
            modifier = Modifier
                .offset {
                    IntOffset(
                        (centerBX - rBPx).roundToInt(),
                        (centerAY - 18f).roundToInt()
                    )
                }
                .width(with(density) { (rBPx * 2f).toDp() })
        ) {
            Text(
                text = String.format(Locale.US, "%.2f", ior),
                style = MaterialTheme.typography.labelLarge.copy(fontWeight = FontWeight.Black),
                color = labelColor
            )
        }
    }

    // 1-Tap Optical Finish Presets (Sanded Beach Glass vs Liquid Meniscus vs Heavy Surf)
    Row(
        modifier = Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.spacedBy(8.dp)
    ) {
        SeaGlassPill(
            label = "Sanded Beach",
            selected = transparency in 0.62f..0.78f,
            onClick = { onApplyPreset(0.72f, 0.44f, 20f, 0.48f, 18f, 1.58f, 0.80f) },
            stageState = stageState,
            selectedMaterial = primaryMaterial,
            unselectedMaterial = surfaceMaterial,
            modifier = Modifier.weight(1f)
        )
        SeaGlassPill(
            label = "Liquid Lens",
            selected = transparency > 0.78f,
            onClick = { onApplyPreset(0.88f, 0.16f, 12f, 0.36f, 24f, 1.72f, 0.86f) },
            stageState = stageState,
            selectedMaterial = primaryMaterial,
            unselectedMaterial = surfaceMaterial,
            modifier = Modifier.weight(1f)
        )
        SeaGlassPill(
            label = "Heavy Frost",
            selected = transparency < 0.62f,
            onClick = { onApplyPreset(0.42f, 0.76f, 32f, 0.56f, 16f, 1.48f, 0.88f) },
            stageState = stageState,
            selectedMaterial = primaryMaterial,
            unselectedMaterial = surfaceMaterial,
            modifier = Modifier.weight(1f)
        )
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
            title = "Frosted Glass Transparency",
            valueText = String.format(Locale.US, "%.0f%%", transparency * 100f),
            value = transparency,
            valueRange = 0.08f..0.95f,
            onValueChange = onTransparencyChange
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
            title = "3D Meniscus Thickness",
            valueText = String.format(Locale.US, "%.0f dp", thicknessDp),
            value = thicknessDp,
            valueRange = 6f..34f,
            onValueChange = onThicknessChange
        )

        OpticalSliderRow(
            title = "Lens Refraction (IOR)",
            valueText = String.format(Locale.US, "%.2f", ior),
            value = ior,
            valueRange = 1.08f..1.80f,
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
            modifier = Modifier.height(26.dp)
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
    val density = LocalDensity.current

    var coalescedPreset by remember { mutableStateOf(true) }
    var smoothBridgeDp by remember { mutableFloatStateOf(52f) }
    var dragOffsetPx by remember { mutableStateOf(Offset.Zero) }

    val targetSeparationDp = if (coalescedPreset) 118f else 186f
    val animatedSepDp by animateFloatAsState(
        targetValue = targetSeparationDp,
        animationSpec = spring(dampingRatio = 0.56f, stiffness = 320f),
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
            text = "Drag the right sea-glass droplet or tap Merge/Split. Newton-Raphson SDF isosurface projection traces a C²-continuous liquid bridge with dual-zone meniscus lens refraction.",
            style = MaterialTheme.typography.bodySmall,
            color = LocalContentColor.current.copy(alpha = 0.88f)
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
            valueRange = 8f..60f,
            onValueChange = { smoothBridgeDp = it }
        )
    }

    // Live Multi-Node SDF Arena rendered directly over the animated backdrop
    val arenaHeight = 220.dp
    val arenaHeightPx = with(density) { arenaHeight.toPx() }
    var arenaWidthPx by remember { mutableFloatStateOf(960f) }

    val centerAX = arenaWidthPx * 0.34f
    val centerAY = arenaHeightPx * 0.50f
    val sepPx = with(density) { animatedSepDp.dp.toPx() }
    val centerBX = (centerAX + sepPx + dragOffsetPx.x).coerceIn(80f, arenaWidthPx - 80f)
    val centerBY = (centerAY + dragOffsetPx.y).coerceIn(65f, arenaHeightPx - 65f)

    val dropletARadiusPx = with(density) { 62.dp.toPx() }
    val dropletBRadiusPx = with(density) { 54.dp.toPx() }

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
                halfHeight = dropletARadiusPx * 0.94f,
                uniformCornerRadius = dropletARadiusPx * 0.82f,
                cornerSmoothing = 2.6f,
                tintColorArgb = primaryMaterial.tintColorArgb,
                thicknessPx = dropletARadiusPx * 0.62f
            ),
            SeaGlassSdfEngine.SdfNode(
                centerX = centerBX,
                centerY = centerBY,
                halfWidth = dropletBRadiusPx,
                halfHeight = dropletBRadiusPx,
                uniformCornerRadius = dropletBRadiusPx,
                cornerSmoothing = 2.5f,
                tintColorArgb = tertiaryMaterial.tintColorArgb,
                thicknessPx = dropletBRadiusPx * 0.65f
            )
        )
    }

    Box(
        modifier = Modifier
            .fillMaxWidth()
            .height(arenaHeight)
            .onSizeChanged { arenaWidthPx = it.width.toFloat() }
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
                        cornerRadiusDp = if (isSelected) 34f else 26f,
                        thicknessDp = if (isSelected) 20f else 15f,
                        rimChalkiness = if (isSelected) 0.94f else palette.primaryGlass.rimChalkiness
                    ),
                    onClick = { onSelectPalette(palette) },
                    modifier = Modifier
                        .weight(1f)
                        .height(138.dp)
                ) {
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Box(
                            modifier = Modifier
                                .size(20.dp)
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
                        color = LocalContentColor.current.copy(alpha = 0.82f)
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
 * Rich, high-contrast Material 3 Expressive coastal artwork canvas with vivid organic ribbons,
 * floating coral/amber/cobalt/jade pebbles, and bold typographic watermarks positioned behind
 * the glass cards so 3D meniscus lens refraction and sand-tumbled frost diffusion are
 * immediately striking.
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
            animation = tween(durationMillis = 16000, easing = LinearEasing),
            repeatMode = RepeatMode.Restart
        ),
        label = "tidalPhase"
    )

    val textMeasurer = rememberTextMeasurer()
    val isDark = palette == SeaGlassPalette.ObsidianTide
    val bgTop = if (isDark) Color(0xFF07131E) else Color(0xFFDCEEF8)
    val bgMid = if (isDark) Color(0xFF0E2236) else Color(0xFFC8E4F4)
    val bgBottom = if (isDark) Color(0xFF15304A) else Color(0xFFB8DCF0)

    val orbPrimary = Color(palette.primaryGlass.tintColorArgb)
    val orbTertiary = Color(palette.tertiaryGlass.tintColorArgb)
    val vividCoral = Color(0xFFFF5E4D)
    val sunlitAmber = Color(0xFFFFB224)
    val deepCobalt = Color(0xFF2868FF)
    val coastalJade = Color(0xFF0FD6A0)
    val seaAmethyst = Color(0xFF9C5CFF)

    val watermarkStyle = remember(isDark) {
        TextStyle(
            fontSize = 54.sp,
            fontWeight = FontWeight.Black,
            letterSpacing = 2.sp,
            color = if (isDark) {
                Color.White.copy(alpha = 0.16f)
            } else {
                Color(0xFF0A2840).copy(alpha = 0.16f)
            }
        )
    }

    Canvas(modifier = modifier) {
        val w = size.width
        val h = size.height

        drawRect(
            brush = Brush.verticalGradient(
                colors = listOf(bgTop, bgMid, bgBottom)
            )
        )

        // Optical calibration grid so 3D meniscus lens magnification & refraction are unmistakable
        val gridStep = 38.dp.toPx()
        val gridColor = if (isDark) {
            Color.White.copy(alpha = 0.08f)
        } else {
            Color(0xFF0B2D48).copy(alpha = 0.09f)
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

        val dx = cos(phase) * w * 0.12f
        val dy = sin(phase) * h * 0.06f

        // Vivid Material 3 Expressive Diagonal Color-Field Ribbons (spanning edge-to-edge -25% to +125%
        // so rich colors pass continuously through the left and right edges of every glass surface!)
        rotate(degrees = -16f, pivot = Offset(w * 0.5f, h * 0.24f)) {
            drawRoundRect(
                brush = Brush.horizontalGradient(
                    colors = listOf(vividCoral, sunlitAmber, vividCoral)
                ),
                topLeft = Offset(-w * 0.25f + dx * 0.4f, h * 0.14f),
                size = Size(w * 1.50f, 118.dp.toPx()),
                cornerRadius = CornerRadius(59.dp.toPx(), 59.dp.toPx())
            )
        }

        rotate(degrees = 14f, pivot = Offset(w * 0.5f, h * 0.42f)) {
            drawRoundRect(
                brush = Brush.horizontalGradient(
                    colors = listOf(deepCobalt, coastalJade, deepCobalt)
                ),
                topLeft = Offset(-w * 0.25f - dx * 0.35f, h * 0.34f),
                size = Size(w * 1.50f, 124.dp.toPx()),
                cornerRadius = CornerRadius(62.dp.toPx(), 62.dp.toPx())
            )
        }

        rotate(degrees = -11f, pivot = Offset(w * 0.5f, h * 0.66f)) {
            drawRoundRect(
                brush = Brush.horizontalGradient(
                    colors = listOf(seaAmethyst, vividCoral, sunlitAmber)
                ),
                topLeft = Offset(-w * 0.25f + dx * 0.3f, h * 0.58f),
                size = Size(w * 1.50f, 120.dp.toPx()),
                cornerRadius = CornerRadius(60.dp.toPx(), 60.dp.toPx())
            )
        }

        rotate(degrees = 9f, pivot = Offset(w * 0.5f, h * 0.88f)) {
            drawRoundRect(
                brush = Brush.horizontalGradient(
                    colors = listOf(sunlitAmber, orbPrimary, deepCobalt)
                ),
                topLeft = Offset(-w * 0.25f - dx * 0.3f, h * 0.82f),
                size = Size(w * 1.50f, 116.dp.toPx()),
                cornerRadius = CornerRadius(58.dp.toPx(), 58.dp.toPx())
            )
        }

        // Crisp Floating Expressive Pebbles & Orbs
        drawCircle(
            color = sunlitAmber.copy(alpha = 0.92f),
            radius = 56.dp.toPx(),
            center = Offset(w * 0.22f - dx * 0.5f, h * 0.30f + dy)
        )
        drawCircle(
            color = vividCoral.copy(alpha = 0.90f),
            radius = 64.dp.toPx(),
            center = Offset(w * 0.78f + dx * 0.4f, h * 0.48f - dy)
        )
        drawCircle(
            color = coastalJade.copy(alpha = 0.90f),
            radius = 58.dp.toPx(),
            center = Offset(w * 0.28f + dy, h * 0.76f + dx * 0.3f)
        )
        drawCircle(
            color = orbTertiary.copy(alpha = 0.92f),
            radius = 52.dp.toPx(),
            center = Offset(w * 0.76f - dy, h * 0.91f)
        )

        // High-Contrast Editorial Watermark Typography (makes lens refraction & frost blur pop!)
        drawText(
            textMeasurer = textMeasurer,
            text = "SEA GLASS",
            topLeft = Offset(w * 0.08f, h * 0.11f),
            style = watermarkStyle
        )
        drawText(
            textMeasurer = textMeasurer,
            text = "LIQUID × M3",
            topLeft = Offset(w * 0.10f, h * 0.31f),
            style = watermarkStyle
        )
        drawText(
            textMeasurer = textMeasurer,
            text = "TUMBLED FROST",
            topLeft = Offset(w * 0.06f, h * 0.52f),
            style = watermarkStyle
        )
        drawText(
            textMeasurer = textMeasurer,
            text = "OKLAB OPTICS",
            topLeft = Offset(w * 0.08f, h * 0.74f),
            style = watermarkStyle
        )

        // Crisp Caustic Wave Contours
        for (i in 0..2) {
            val yBase = h * (0.26f + i * 0.25f)
            val wavePath = Path().apply {
                moveTo(0f, yBase)
                cubicTo(
                    w * 0.33f,
                    yBase - 36.dp.toPx() * sin(phase + i),
                    w * 0.66f,
                    yBase + 36.dp.toPx() * cos(phase + i),
                    w,
                    yBase
                )
            }
            drawPath(
                path = wavePath,
                color = Color.White.copy(alpha = if (isDark) 0.42f else 0.68f),
                style = Stroke(width = 4.dp.toPx())
            )
        }
    }
}
