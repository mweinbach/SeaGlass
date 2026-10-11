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
import androidx.compose.animation.core.spring
import androidx.compose.animation.core.tween
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.border
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
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.IntOffset
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import dev.seaglass.compose.LocalSeaGlassMotionScheme
import dev.seaglass.compose.LocalSeaGlassTiltState
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
import dev.seaglass.core.SeaGlassHdrSupport
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
        SeaGlassHdrSupport.enableWindowHdr(this, desiredHdrHeadroom = 1.65f)
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

    // Live adjustable optical parameters initialized for strong sea-glass tint + transparent frosted glass
    var transparency by remember(activePalette) {
        mutableFloatStateOf(activePalette.primaryGlass.transparency)
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
        mutableFloatStateOf(activePalette.primaryGlass.thicknessDp)
    }
    var ior by remember(activePalette) {
        mutableFloatStateOf(activePalette.primaryGlass.ior)
    }
    var chromaticDispersion by remember(activePalette) {
        mutableFloatStateOf(0.58f)
    }
    var rimChalkiness by remember(activePalette) {
        mutableFloatStateOf(activePalette.primaryGlass.rimChalkiness)
    }

    val customPrimaryMaterial = remember(
        activePalette,
        transparency,
        coarseness,
        frostRadiusDp,
        tintDensity,
        thicknessDp,
        ior,
        chromaticDispersion,
        rimChalkiness
    ) {
        activePalette.primaryGlass.copy(
            transparency = transparency,
            coarseness = coarseness,
            frostRadiusDp = frostRadiusDp,
            tintDensity = tintDensity,
            thicknessDp = thicknessDp,
            ior = ior,
            chromaticDispersion = chromaticDispersion,
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
        chromaticDispersion,
        rimChalkiness
    ) {
        activePalette.surfaceGlass.copy(
            transparency = (transparency + 0.04f).coerceIn(0f, 0.95f),
            coarseness = (coarseness + 0.05f).coerceIn(0f, 1f),
            frostRadiusDp = (frostRadiusDp + 3f).coerceAtMost(56f),
            tintDensity = (tintDensity * 0.84f).coerceIn(0.24f, 0.92f),
            thicknessDp = thicknessDp,
            ior = ior,
            chromaticDispersion = chromaticDispersion,
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
            // 1. Luminous Material 3 Expressive coastal lightscape captured into the GPU stage
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
                        top = systemBarsPadding.calculateTopPadding() + 8.dp,
                        bottom = systemBarsPadding.calculateBottomPadding() + 88.dp,
                        start = 16.dp,
                        end = 16.dp
                    )
                    .verticalScroll(rememberScrollState()),
                verticalArrangement = Arrangement.spacedBy(10.dp)
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

                // Expressive Palette Strip with color gems and non-truncated labels
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
                            chromaticDispersion = chromaticDispersion,
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
                        chromaticDispersion = chromaticDispersion,
                        onChromaticDispersionChange = { chromaticDispersion = it },
                        onApplyPreset = { tr, c, f, d, t, i, disp ->
                            transparency = tr
                            coarseness = c
                            frostRadiusDp = f
                            tintDensity = d
                            thicknessDp = t
                            ior = i
                            chromaticDispersion = disp
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
                            chromaticDispersion = chromaticDispersion,
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
                        bottom = systemBarsPadding.calculateBottomPadding() + 10.dp
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

    SeaGlassSurface(
        stageState = stageState,
        material = material.copy(
            cornerRadiusDp = cornerRadius,
            thicknessDp = (material.thicknessDp + extraThickness).coerceAtMost(34f)
        ),
        onClick = { morphedExpanded = !morphedExpanded },
        contentPadding = PaddingValues(horizontal = 18.dp, vertical = 14.dp),
        modifier = Modifier.fillMaxWidth()
    ) {
        Column(verticalArrangement = Arrangement.spacedBy(4.dp)) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Column(modifier = Modifier.weight(1f)) {
                    Text(
                        text = "HDR 1.65× · TILT & TAP REFRACTION",
                        style = MaterialTheme.typography.labelSmall.copy(
                            fontWeight = FontWeight.ExtraBold,
                            letterSpacing = 1.3.sp
                        ),
                        color = LocalContentColor.current.copy(alpha = 0.78f)
                    )
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
                        rimChalkiness = 0.90f
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
                style = MaterialTheme.typography.bodySmall.copy(fontWeight = FontWeight.Medium),
                color = LocalContentColor.current.copy(alpha = 0.90f)
            )
        }
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
            SeaGlassPalette.SeafoamMint to "Mint",
            SeaGlassPalette.CobaltTide to "Cobalt",
            SeaGlassPalette.BeachAmber to "Amber",
            SeaGlassPalette.SeaLavender to "Lavender"
        )
    }

    Row(
        modifier = Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.spacedBy(8.dp)
    ) {
        featured.forEach { (palette, shortLabel) ->
            val selected = palette == activePalette
            val targetMaterial = if (selected) {
                palette.primaryGlass.copy(cornerRadiusDp = 26f, thicknessDp = 18f)
            } else {
                palette.surfaceGlass.copy(cornerRadiusDp = 18f, thicknessDp = 11f)
            }

            SeaGlassButton(
                onClick = { onSelectPalette(palette) },
                stageState = stageState,
                material = targetMaterial,
                contentPadding = PaddingValues(horizontal = 10.dp, vertical = 9.dp),
                modifier = Modifier.weight(1f)
            ) {
                Box(
                    modifier = Modifier
                        .size(10.dp)
                        .clip(CircleShape)
                        .background(Color(palette.primaryGlass.tintColorArgb))
                        .border(1.dp, Color.White.copy(alpha = 0.75f), CircleShape)
                )
                Spacer(modifier = Modifier.width(6.dp))
                Text(
                    text = shortLabel,
                    style = MaterialTheme.typography.labelMedium.copy(
                        fontWeight = if (selected) FontWeight.ExtraBold else FontWeight.SemiBold
                    ),
                    color = LocalContentColor.current
                )
            }
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
    chromaticDispersion: Float,
    onChromaticDispersionChange: (Float) -> Unit,
    onApplyPreset: (Float, Float, Float, Float, Float, Float, Float) -> Unit
) {
    val density = LocalDensity.current

    // 1. Flagship Material 3 Expressive × Liquid Sea Glass Coalescing Media & Soundscape Deck
    var isPlaying by remember { mutableStateOf(true) }
    var heroFlowState by remember { mutableIntStateOf(0) }
    val playOrbGapDp by animateFloatAsState(
        targetValue = when (heroFlowState) {
            0 -> -2f
            1 -> 9f
            else -> 22f
        },
        animationSpec = spring(dampingRatio = 0.54f, stiffness = 300f),
        label = "playOrbGap"
    )

    var deckWidthPx by remember { mutableFloatStateOf(960f) }
    val deckHeight = 112.dp
    val deckHeightPx = with(density) { deckHeight.toPx() }
    val orbRadiusPx = with(density) { 46.dp.toPx() }
    val gapPx = with(density) { playOrbGapDp.dp.toPx() }

    val capsuleWidthPx = (deckWidthPx - orbRadiusPx * 2f - gapPx.coerceAtLeast(0f) - with(density) { 8.dp.toPx() })
        .coerceAtLeast(orbRadiusPx * 3f)
    val capsuleHalfW = capsuleWidthPx * 0.5f
    val capsuleHalfH = deckHeightPx * 0.46f
    val centerAY = deckHeightPx * 0.50f
    val orbCenterX = (capsuleWidthPx + gapPx + orbRadiusPx).coerceAtMost(deckWidthPx - orbRadiusPx - 4f)

    val heroNodes = remember(
        deckWidthPx,
        deckHeightPx,
        capsuleHalfW,
        capsuleHalfH,
        centerAY,
        orbCenterX,
        orbRadiusPx,
        primaryMaterial,
        tertiaryMaterial
    ) {
        listOf(
            SeaGlassSdfEngine.SdfNode(
                centerX = capsuleHalfW,
                centerY = centerAY,
                halfWidth = capsuleHalfW,
                halfHeight = capsuleHalfH,
                uniformCornerRadius = capsuleHalfH,
                cornerSmoothing = 2.2f,
                tintColorArgb = primaryMaterial.tintColorArgb,
                thicknessPx = primaryMaterial.thicknessDp * 2.8f
            ),
            SeaGlassSdfEngine.SdfNode(
                centerX = orbCenterX,
                centerY = centerAY,
                halfWidth = orbRadiusPx,
                halfHeight = orbRadiusPx,
                uniformCornerRadius = orbRadiusPx,
                cornerSmoothing = 2.2f,
                tintColorArgb = tertiaryMaterial.tintColorArgb,
                thicknessPx = tertiaryMaterial.thicknessDp * 2.8f
            )
        )
    }

    // Coalescing Media & Soundscape Capsule + Play Droplet with Adaptive Touch-Point Caustic Flare
    var deckTouchPoint by remember { mutableStateOf<Offset?>(null) }
    var deckPressed by remember { mutableStateOf(false) }
    val deckTouchIntensity by animateFloatAsState(
        targetValue = if (deckPressed) 1f else 0f,
        animationSpec = spring(dampingRatio = 0.62f, stiffness = 620f),
        label = "deckTouchIntensity"
    )

    Box(
        modifier = Modifier
            .fillMaxWidth()
            .height(deckHeight)
            .onSizeChanged { deckWidthPx = it.width.toFloat() }
            .pointerInput(Unit) {
                awaitPointerEventScope {
                    while (true) {
                        val event = awaitPointerEvent(androidx.compose.ui.input.pointer.PointerEventPass.Initial)
                        val down = event.changes.firstOrNull { it.pressed }
                        if (down != null) {
                            deckTouchPoint = down.position
                            deckPressed = true
                        } else {
                            deckPressed = false
                        }
                    }
                }
            }
            .clickable(
                interactionSource = remember { MutableInteractionSource() },
                indication = null,
                onClick = {
                    heroFlowState = (heroFlowState + 1) % 3
                    isPlaying = heroFlowState != 2
                }
            )
            .seaGlass(
                stageState = stageState,
                material = primaryMaterial,
                nodesOptional = heroNodes,
                smoothRadiusDp = 44f,
                touchPointInLocal = deckTouchPoint,
                touchIntensity = deckTouchIntensity
            )
    ) {
        val labelColor = Color(
            SeaGlassColorMath.optimalContentColorFor(
                primaryMaterial.tintColorArgb,
                primaryMaterial.tintDensity
            )
        )

        Row(
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.SpaceBetween,
            modifier = Modifier
                .width(with(density) { capsuleWidthPx.toDp() })
                .height(deckHeight)
                .padding(horizontal = 20.dp)
        ) {
            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = when (heroFlowState) {
                        0 -> "COALESCED SOUNDSCAPE · 432 HZ"
                        1 -> "SURFACE TENSION NECK"
                        else -> "DETACHED ORB MODE"
                    },
                    style = MaterialTheme.typography.labelSmall.copy(
                        fontWeight = FontWeight.ExtraBold,
                        fontSize = 9.sp,
                        letterSpacing = 1.0.sp
                    ),
                    color = labelColor.copy(alpha = 0.78f)
                )
                Text(
                    text = "Pacific Surf & Sea Glass",
                    style = MaterialTheme.typography.titleMedium.copy(fontWeight = FontWeight.Black),
                    color = labelColor
                )
                Text(
                    text = String.format(
                        Locale.US,
                        "%.0f%% Clear · %.2f IOR · Tap to flow ⇄",
                        transparency * 100f,
                        ior
                    ),
                    style = MaterialTheme.typography.labelSmall.copy(fontWeight = FontWeight.SemiBold),
                    color = labelColor.copy(alpha = 0.86f)
                )
            }

            // Expressive 5-Bar Audio Waveform Pill
            Row(
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.spacedBy(3.dp),
                modifier = Modifier.padding(start = 8.dp)
            ) {
                val heights = listOf(12.dp, 24.dp, 18.dp, 28.dp, 15.dp)
                heights.forEach { barH ->
                    Box(
                        modifier = Modifier
                            .width(4.dp)
                            .height(if (isPlaying) barH else 8.dp)
                            .clip(CircleShape)
                            .background(labelColor.copy(alpha = 0.82f))
                    )
                }
            }
        }

        Box(
            contentAlignment = Alignment.Center,
            modifier = Modifier
                .offset {
                    IntOffset(
                        (orbCenterX - orbRadiusPx).roundToInt(),
                        (centerAY - orbRadiusPx).roundToInt()
                    )
                }
                .size(with(density) { (orbRadiusPx * 2f).toDp() })
        ) {
            Column(horizontalAlignment = Alignment.CenterHorizontally) {
                Text(
                    text = if (isPlaying) "❚❚" else "▶",
                    style = MaterialTheme.typography.titleMedium.copy(fontWeight = FontWeight.Black),
                    color = labelColor
                )
                Text(
                    text = if (heroFlowState == 2) "SPLIT" else "FLOW",
                    style = MaterialTheme.typography.labelSmall.copy(
                        fontWeight = FontWeight.ExtraBold,
                        fontSize = 8.sp
                    ),
                    color = labelColor.copy(alpha = 0.80f)
                )
            }
        }
    }

    // 1-Tap Optical Finish Presets
    Row(
        modifier = Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.spacedBy(8.dp)
    ) {
        SeaGlassPill(
            label = "Sanded Beach",
            selected = transparency in 0.55f..0.75f,
            onClick = { onApplyPreset(0.64f, 0.44f, 22f, 0.68f, 16f, 1.54f, 0.80f) },
            stageState = stageState,
            selectedMaterial = primaryMaterial,
            unselectedMaterial = surfaceMaterial,
            modifier = Modifier.weight(1f)
        )
        SeaGlassPill(
            label = "Liquid Lens",
            selected = transparency > 0.75f,
            onClick = { onApplyPreset(0.86f, 0.16f, 12f, 0.62f, 22f, 1.72f, 0.88f) },
            stageState = stageState,
            selectedMaterial = primaryMaterial,
            unselectedMaterial = surfaceMaterial,
            modifier = Modifier.weight(1f)
        )
        SeaGlassPill(
            label = "Heavy Frost",
            selected = transparency < 0.55f,
            onClick = { onApplyPreset(0.36f, 0.78f, 34f, 0.74f, 16f, 1.48f, 0.92f) },
            stageState = stageState,
            selectedMaterial = primaryMaterial,
            unselectedMaterial = surfaceMaterial,
            modifier = Modifier.weight(1f)
        )
    }

    // Dedicated Hero "Frosted Glass Transparency Bar" Card
    SeaGlassSurface(
        stageState = stageState,
        material = primaryMaterial.copy(cornerRadiusDp = 26f),
        contentPadding = PaddingValues(horizontal = 16.dp, vertical = 10.dp),
        modifier = Modifier.fillMaxWidth()
    ) {
        Column(verticalArrangement = Arrangement.spacedBy(2.dp)) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    text = "FROSTED GLASS TRANSPARENCY BAR",
                    style = MaterialTheme.typography.labelSmall.copy(
                        fontWeight = FontWeight.ExtraBold,
                        letterSpacing = 1.1.sp
                    ),
                    color = LocalContentColor.current.copy(alpha = 0.85f)
                )
                Box(
                    modifier = Modifier
                        .clip(RoundedCornerShape(10.dp))
                        .background(LocalContentColor.current.copy(alpha = 0.16f))
                        .padding(horizontal = 9.dp, vertical = 2.dp)
                ) {
                    Text(
                        text = String.format(Locale.US, "%.0f%% Transparent", transparency * 100f),
                        style = MaterialTheme.typography.labelSmall.copy(
                            fontFamily = FontFamily.Monospace,
                            fontWeight = FontWeight.Black
                        ),
                        color = LocalContentColor.current
                    )
                }
            }
            Slider(
                value = transparency,
                onValueChange = onTransparencyChange,
                valueRange = 0.08f..0.95f,
                colors = SliderDefaults.colors(
                    thumbColor = LocalContentColor.current,
                    activeTrackColor = LocalContentColor.current.copy(alpha = 0.88f),
                    inactiveTrackColor = LocalContentColor.current.copy(alpha = 0.24f)
                ),
                modifier = Modifier.height(24.dp)
            )
        }
    }

    // Compact Optical Parameter Controls Card (fits cleanly above bottom bar!)
    SeaGlassSurface(
        stageState = stageState,
        material = surfaceMaterial,
        contentPadding = PaddingValues(horizontal = 16.dp, vertical = 12.dp),
        modifier = Modifier.fillMaxWidth()
    ) {
        Column(verticalArrangement = Arrangement.spacedBy(5.dp)) {
            Text(
                text = "SEA GLASS & LIQUID MENISCUS OPTICS",
                style = MaterialTheme.typography.labelSmall.copy(
                    fontWeight = FontWeight.ExtraBold,
                    letterSpacing = 1.1.sp
                ),
                color = LocalContentColor.current.copy(alpha = 0.78f)
            )

            OpticalSliderRow(
                title = "Beer-Lambert Color Tint Strength",
                valueText = String.format(Locale.US, "%.0f%%", tintDensity * 100f),
                value = tintDensity,
                valueRange = 0.18f..0.95f,
                onValueChange = onTintDensityChange
            )

            OpticalSliderRow(
                title = "Sand Coarseness (Micro-Grit)",
                valueText = String.format(Locale.US, "%.0f%%", coarseness * 100f),
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
                title = "Edge Lens Refraction (IOR)",
                valueText = String.format(Locale.US, "%.2f", ior),
                value = ior,
                valueRange = 1.08f..1.80f,
                onValueChange = onIorChange
            )

            OpticalSliderRow(
                title = "Chromatic Aberration (Prismatic)",
                valueText = String.format(Locale.US, "%.0f%%", chromaticDispersion * 100f),
                value = chromaticDispersion,
                valueRange = 0.0f..1.0f,
                onValueChange = onChromaticDispersionChange
            )
        }
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
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Text(
                text = title,
                style = MaterialTheme.typography.bodySmall.copy(fontWeight = FontWeight.SemiBold),
                color = LocalContentColor.current
            )
            Box(
                modifier = Modifier
                    .clip(RoundedCornerShape(8.dp))
                    .background(LocalContentColor.current.copy(alpha = 0.13f))
                    .padding(horizontal = 7.dp, vertical = 1.dp)
            ) {
                Text(
                    text = valueText,
                    style = MaterialTheme.typography.labelSmall.copy(
                        fontFamily = FontFamily.Monospace,
                        fontWeight = FontWeight.Bold
                    ),
                    color = LocalContentColor.current
                )
            }
        }
        Slider(
            value = value,
            onValueChange = onValueChange,
            valueRange = valueRange,
            colors = SliderDefaults.colors(
                thumbColor = LocalContentColor.current,
                activeTrackColor = LocalContentColor.current.copy(alpha = 0.82f),
                inactiveTrackColor = LocalContentColor.current.copy(alpha = 0.22f)
            ),
            modifier = Modifier.height(21.dp)
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
    var smoothBridgeDp by remember { mutableFloatStateOf(56f) }
    var dragOffsetPx by remember { mutableStateOf(Offset.Zero) }

    // rA = 56dp, rB = 48dp -> sum = 104dp. At 100dp with 56dp smin, forms a deep 20dp concave hourglass waist!
    val targetSeparationDp = if (coalescedPreset) 100f else 168f
    val animatedSepDp by animateFloatAsState(
        targetValue = targetSeparationDp,
        animationSpec = spring(dampingRatio = 0.54f, stiffness = 300f),
        label = "dropletSeparation"
    )

    val arenaHeight = 204.dp
    val arenaHeightPx = with(density) { arenaHeight.toPx() }
    var arenaWidthPx by remember { mutableFloatStateOf(960f) }

    val centerAX = arenaWidthPx * 0.34f
    val centerAY = arenaHeightPx * 0.50f
    val sepPx = with(density) { animatedSepDp.dp.toPx() }
    val centerBX = (centerAX + sepPx + dragOffsetPx.x).coerceIn(80f, arenaWidthPx - 80f)
    val centerBY = (centerAY + dragOffsetPx.y).coerceIn(65f, arenaHeightPx - 65f)

    val dropletARadiusPx = with(density) { 56.dp.toPx() }
    val dropletBRadiusPx = with(density) { 48.dp.toPx() }

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
                halfWidth = dropletARadiusPx,
                halfHeight = dropletARadiusPx,
                uniformCornerRadius = dropletARadiusPx,
                cornerSmoothing = 2.2f,
                tintColorArgb = primaryMaterial.tintColorArgb,
                thicknessPx = dropletARadiusPx * 0.62f
            ),
            SeaGlassSdfEngine.SdfNode(
                centerX = centerBX,
                centerY = centerBY,
                halfWidth = dropletBRadiusPx,
                halfHeight = dropletBRadiusPx,
                uniformCornerRadius = dropletBRadiusPx,
                cornerSmoothing = 2.2f,
                tintColorArgb = tertiaryMaterial.tintColorArgb,
                thicknessPx = dropletBRadiusPx * 0.65f
            )
        )
    }

    // 1. Hero Interactive Multi-Node SDF Arena at the top of Tab 2 with HDR Touch Caustic Flare
    val tiltState = LocalSeaGlassTiltState.current
    var arenaTouchPoint by remember { mutableStateOf<Offset?>(null) }
    var arenaPressed by remember { mutableStateOf(false) }
    val arenaTouchIntensity by animateFloatAsState(
        targetValue = if (arenaPressed) 1f else 0f,
        animationSpec = spring(dampingRatio = 0.60f, stiffness = 580f),
        label = "arenaTouchIntensity"
    )

    Box(
        modifier = Modifier
            .fillMaxWidth()
            .height(arenaHeight)
            .onSizeChanged { arenaWidthPx = it.width.toFloat() }
            .pointerInput(Unit) {
                detectDragGestures(
                    onDragStart = { offset ->
                        arenaTouchPoint = offset
                        arenaPressed = true
                    },
                    onDragEnd = { arenaPressed = false },
                    onDragCancel = { arenaPressed = false },
                    onDrag = { change, dragAmount ->
                        change.consume()
                        arenaTouchPoint = change.position
                        arenaPressed = true
                        dragOffsetPx += dragAmount
                    }
                )
            }
            .seaGlass(
                stageState = stageState,
                material = primaryMaterial,
                nodesOptional = arenaNodes,
                smoothRadiusDp = smoothBridgeDp,
                touchPointInLocal = arenaTouchPoint,
                touchIntensity = arenaTouchIntensity
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
                        (centerAY - dropletARadiusPx).roundToInt()
                    )
                }
                .size(with(density) { (dropletARadiusPx * 2f).toDp() })
        ) {
            Column(horizontalAlignment = Alignment.CenterHorizontally) {
                Text(
                    text = "ANCHOR",
                    style = MaterialTheme.typography.labelLarge.copy(fontWeight = FontWeight.Black),
                    color = labelColor
                )
                Text(
                    text = "Primary Tint",
                    style = MaterialTheme.typography.labelSmall.copy(fontSize = 10.sp),
                    color = labelColor.copy(alpha = 0.80f)
                )
            }
        }

        Box(
            contentAlignment = Alignment.Center,
            modifier = Modifier
                .offset {
                    IntOffset(
                        (centerBX - dropletBRadiusPx).roundToInt(),
                        (centerBY - dropletBRadiusPx).roundToInt()
                    )
                }
                .size(with(density) { (dropletBRadiusPx * 2f).toDp() })
        ) {
            Column(horizontalAlignment = Alignment.CenterHorizontally) {
                Text(
                    text = "DRAG ME",
                    style = MaterialTheme.typography.labelLarge.copy(fontWeight = FontWeight.Black),
                    color = labelColor
                )
                Text(
                    text = "Tertiary Tint",
                    style = MaterialTheme.typography.labelSmall.copy(fontSize = 10.sp),
                    color = labelColor.copy(alpha = 0.80f)
                )
            }
        }
    }

    // 2. Controls & Bridge Smoothness Card below the interactive arena
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
            color = LocalContentColor.current.copy(alpha = 0.78f)
        )
        Text(
            text = "Drag the right sea-glass droplet or tap Merge/Split. Both circular droplets evaluate a shared std140 UBO and blend their Oklab tints across the concave hourglass bridge.",
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
                Text("Merge Bridge", fontWeight = FontWeight.Bold)
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
                Text("Split Teardrops", fontWeight = FontWeight.Bold)
            }
        }

        OpticalSliderRow(
            title = "Smooth-Min Bridge Radius (k)",
            valueText = String.format(Locale.US, "%.0f dp", smoothBridgeDp),
            value = smoothBridgeDp,
            valueRange = 8f..60f,
            onValueChange = { smoothBridgeDp = it }
        )

        Text(
            text = String.format(
                Locale.US,
                "DEVICE TILT & SIDE REFRACTION · %.0f° SPECULAR RIM",
                tiltState.specularAngleDegrees
            ),
            style = MaterialTheme.typography.labelSmall.copy(
                fontWeight = FontWeight.ExtraBold,
                letterSpacing = 1.0.sp
            ),
            color = LocalContentColor.current.copy(alpha = 0.78f)
        )

        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            SeaGlassPill(
                label = "Tilt Left ↙",
                selected = tiltState.manualTiltOverride?.x?.let { it < -0.5f } == true,
                onClick = { tiltState.manualTiltOverride = Offset(-0.85f, -0.35f) },
                stageState = stageState,
                selectedMaterial = primaryMaterial,
                unselectedMaterial = surfaceMaterial,
                modifier = Modifier.weight(1f)
            )
            SeaGlassPill(
                label = "Gyro Auto ⊙",
                selected = tiltState.manualTiltOverride == null,
                onClick = { tiltState.manualTiltOverride = null },
                stageState = stageState,
                selectedMaterial = primaryMaterial,
                unselectedMaterial = surfaceMaterial,
                modifier = Modifier.weight(1f)
            )
            SeaGlassPill(
                label = "Tilt Right ↘",
                selected = tiltState.manualTiltOverride?.x?.let { it > 0.5f } == true,
                onClick = { tiltState.manualTiltOverride = Offset(0.85f, 0.45f) },
                stageState = stageState,
                selectedMaterial = primaryMaterial,
                unselectedMaterial = surfaceMaterial,
                modifier = Modifier.weight(1f)
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
                        .height(136.dp)
                ) {
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        // 3 overlapping tonal sea-glass gems (primary, secondary, tertiary)
                        Row(horizontalArrangement = Arrangement.spacedBy((-6).dp)) {
                            listOf(
                                palette.primaryGlass.tintColorArgb,
                                palette.secondaryGlass.tintColorArgb,
                                palette.tertiaryGlass.tintColorArgb
                            ).forEach { colorArgb ->
                                Box(
                                    modifier = Modifier
                                        .size(18.dp)
                                        .clip(CircleShape)
                                        .background(Color(colorArgb))
                                        .border(1.2.dp, Color.White.copy(alpha = 0.82f), CircleShape)
                                )
                            }
                        }
                        if (isSelected) {
                            Box(
                                modifier = Modifier
                                    .clip(RoundedCornerShape(8.dp))
                                    .background(LocalContentColor.current.copy(alpha = 0.16f))
                                    .padding(horizontal = 6.dp, vertical = 2.dp)
                            ) {
                                Text(
                                    text = "ACTIVE",
                                    style = MaterialTheme.typography.labelSmall.copy(
                                        fontWeight = FontWeight.Black,
                                        fontSize = 9.sp
                                    )
                                )
                            }
                        }
                    }
                    Spacer(modifier = Modifier.weight(1f))
                    Text(
                        text = palette.displayName,
                        style = MaterialTheme.typography.titleMedium.copy(
                            fontWeight = FontWeight.Black
                        )
                    )
                    Text(
                        text = String.format(
                            Locale.US,
                            "%.0f%% Clear · %.0f%% Grit · %.2f IOR",
                            palette.primaryGlass.transparency * 100f,
                            palette.primaryGlass.coarseness * 100f,
                            palette.primaryGlass.ior
                        ),
                        style = MaterialTheme.typography.labelSmall.copy(fontSize = 10.sp),
                        color = LocalContentColor.current.copy(alpha = 0.85f)
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
                .take(16)
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
 * Luminous, palette-harmonized Material 3 Expressive coastal lightscape with high-contrast
 * refraction bands, sunlit coral/amber orbs, and caustic filaments behind the glass.
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

    val isDark = palette == SeaGlassPalette.ObsidianTide
    val bgTop = if (isDark) {
        Color(0xFF07121E)
    } else {
        Color(
            SeaGlassColorMath.lerpOklab(
                palette.primaryGlass.tintColorArgb,
                0xFF102A44.toInt(),
                0.52f
            )
        )
    }
    val bgMid = if (isDark) {
        Color(0xFF0C1F34)
    } else {
        Color(
            SeaGlassColorMath.lerpOklab(
                palette.tertiaryGlass.tintColorArgb,
                0xFF163858.toInt(),
                0.46f
            )
        )
    }
    val bgBottom = if (isDark) {
        Color(0xFF112A44)
    } else {
        Color(
            SeaGlassColorMath.lerpOklab(
                palette.secondaryGlass.tintColorArgb,
                0xFF0E2942.toInt(),
                0.50f
            )
        )
    }

    val primaryGlow = Color(palette.primaryGlass.tintColorArgb)
    val secondaryGlow = Color(palette.secondaryGlass.tintColorArgb)
    val tertiaryGlow = Color(palette.tertiaryGlass.tintColorArgb)
    val sunlitPeach = Color(0xFFFF8E6E)
    val goldenSand = Color(0xFFFFCA58)
    val aquaCaustic = Color(0xFF68F4FF)

    Canvas(modifier = modifier) {
        val w = size.width
        val h = size.height

        drawRect(
            brush = Brush.verticalGradient(
                colors = listOf(bgTop, bgMid, bgBottom)
            )
        )

        val dx = cos(phase) * w * 0.10f
        val dy = sin(phase) * h * 0.05f

        // Soft glowing atmospheric bokeh spheres behind the glass
        drawCircle(
            brush = Brush.radialGradient(
                colors = listOf(primaryGlow.copy(alpha = 0.78f), Color.Transparent),
                center = Offset(w * 0.20f + dx, h * 0.18f + dy),
                radius = w * 0.58f
            ),
            radius = w * 0.58f,
            center = Offset(w * 0.20f + dx, h * 0.18f + dy)
        )

        drawCircle(
            brush = Brush.radialGradient(
                colors = listOf(sunlitPeach.copy(alpha = 0.72f), Color.Transparent),
                center = Offset(w * 0.82f - dx * 0.7f, h * 0.34f - dy),
                radius = w * 0.52f
            ),
            radius = w * 0.52f,
            center = Offset(w * 0.82f - dx * 0.7f, h * 0.34f - dy)
        )

        drawCircle(
            brush = Brush.radialGradient(
                colors = listOf(tertiaryGlow.copy(alpha = 0.76f), Color.Transparent),
                center = Offset(w * 0.24f - dy, h * 0.62f + dx * 0.5f),
                radius = w * 0.56f
            ),
            radius = w * 0.56f,
            center = Offset(w * 0.24f - dy, h * 0.62f + dx * 0.5f)
        )

        drawCircle(
            brush = Brush.radialGradient(
                colors = listOf(goldenSand.copy(alpha = 0.70f), Color.Transparent),
                center = Offset(w * 0.78f + dy, h * 0.84f - dx * 0.4f),
                radius = w * 0.50f
            ),
            radius = w * 0.50f,
            center = Offset(w * 0.78f + dy, h * 0.84f - dx * 0.4f)
        )

        // Flowing Material 3 Expressive Silk Ribbons (spanning edge-to-edge -25% to +125%)
        rotate(degrees = -14f, pivot = Offset(w * 0.5f, h * 0.22f)) {
            drawRoundRect(
                brush = Brush.horizontalGradient(
                    colors = listOf(
                        primaryGlow.copy(alpha = 0.88f),
                        goldenSand.copy(alpha = 0.86f),
                        tertiaryGlow.copy(alpha = 0.88f)
                    )
                ),
                topLeft = Offset(-w * 0.25f + dx * 0.35f, h * 0.15f),
                size = Size(w * 1.50f, 96.dp.toPx()),
                cornerRadius = CornerRadius(48.dp.toPx(), 48.dp.toPx())
            )
        }

        rotate(degrees = 12f, pivot = Offset(w * 0.5f, h * 0.44f)) {
            drawRoundRect(
                brush = Brush.horizontalGradient(
                    colors = listOf(
                        tertiaryGlow.copy(alpha = 0.86f),
                        sunlitPeach.copy(alpha = 0.84f),
                        secondaryGlow.copy(alpha = 0.88f)
                    )
                ),
                topLeft = Offset(-w * 0.25f - dx * 0.30f, h * 0.38f),
                size = Size(w * 1.50f, 104.dp.toPx()),
                cornerRadius = CornerRadius(52.dp.toPx(), 52.dp.toPx())
            )
        }

        rotate(degrees = -10f, pivot = Offset(w * 0.5f, h * 0.68f)) {
            drawRoundRect(
                brush = Brush.horizontalGradient(
                    colors = listOf(
                        sunlitPeach.copy(alpha = 0.82f),
                        primaryGlow.copy(alpha = 0.88f),
                        aquaCaustic.copy(alpha = 0.84f)
                    )
                ),
                topLeft = Offset(-w * 0.25f + dx * 0.25f, h * 0.62f),
                size = Size(w * 1.50f, 98.dp.toPx()),
                cornerRadius = CornerRadius(49.dp.toPx(), 49.dp.toPx())
            )
        }

        // Crisp Luminous Pebbles & Rings for Meniscus Refraction Calibration
        drawCircle(
            color = goldenSand.copy(alpha = 0.92f),
            radius = 48.dp.toPx(),
            center = Offset(w * 0.26f - dx * 0.4f, h * 0.29f + dy)
        )
        drawCircle(
            color = Color.White.copy(alpha = 0.45f),
            radius = 60.dp.toPx(),
            center = Offset(w * 0.26f - dx * 0.4f, h * 0.29f + dy),
            style = Stroke(width = 3.dp.toPx())
        )

        drawCircle(
            color = sunlitPeach.copy(alpha = 0.90f),
            radius = 54.dp.toPx(),
            center = Offset(w * 0.74f + dx * 0.35f, h * 0.48f - dy)
        )
        drawCircle(
            color = Color.White.copy(alpha = 0.45f),
            radius = 68.dp.toPx(),
            center = Offset(w * 0.74f + dx * 0.35f, h * 0.48f - dy),
            style = Stroke(width = 3.dp.toPx())
        )

        drawCircle(
            color = aquaCaustic.copy(alpha = 0.88f),
            radius = 48.dp.toPx(),
            center = Offset(w * 0.32f + dy, h * 0.76f)
        )

        // Luminous Caustic Wave Filaments
        for (i in 0..3) {
            val yBase = h * (0.20f + i * 0.21f)
            val wavePath = Path().apply {
                moveTo(0f, yBase)
                cubicTo(
                    w * 0.33f,
                    yBase - 34.dp.toPx() * sin(phase + i * 0.9f),
                    w * 0.66f,
                    yBase + 34.dp.toPx() * cos(phase + i * 0.9f),
                    w,
                    yBase
                )
            }
            drawPath(
                path = wavePath,
                brush = Brush.horizontalGradient(
                    colors = listOf(
                        Color.White.copy(alpha = 0.16f),
                        Color.White.copy(alpha = 0.62f),
                        aquaCaustic.copy(alpha = 0.54f),
                        Color.White.copy(alpha = 0.16f)
                    )
                ),
                style = Stroke(width = 3.dp.toPx())
            )
        }
    }
}
