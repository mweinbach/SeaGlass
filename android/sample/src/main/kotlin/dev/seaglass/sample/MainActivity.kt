package dev.seaglass.sample

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.animation.AnimatedContent
import androidx.compose.animation.core.LinearEasing
import androidx.compose.animation.core.RepeatMode
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.spring
import androidx.compose.animation.core.tween
import androidx.compose.animation.fadeIn
import androidx.compose.animation.fadeOut
import androidx.compose.animation.scaleIn
import androidx.compose.animation.scaleOut
import androidx.compose.animation.togetherWith
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
import androidx.compose.ui.zIndex
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
import dev.seaglass.compose.SeaGlassActionSheet
import dev.seaglass.compose.SeaGlassAdaptiveCornerLabCard
import dev.seaglass.compose.SeaGlassBadge
import dev.seaglass.compose.SeaGlassBottomSheetSurface
import dev.seaglass.compose.SeaGlassBuddingCluster
import dev.seaglass.compose.SeaGlassButton
import dev.seaglass.compose.SeaGlassCard
import dev.seaglass.compose.SeaGlassCascadedModelMorph
import dev.seaglass.compose.SeaGlassCheckbox
import dev.seaglass.compose.SeaGlassChoreographedShapeOrb
import dev.seaglass.compose.SeaGlassCircularProgressIndicator
import dev.seaglass.compose.SeaGlassContextMenuCard
import dev.seaglass.compose.SeaGlassContextMenuItem
import dev.seaglass.compose.SeaGlassContextMenuOverlay
import dev.seaglass.compose.SeaGlassContextRingMorph
import dev.seaglass.compose.SeaGlassDialogCard
import dev.seaglass.compose.SeaGlassExtendedFab
import dev.seaglass.compose.SeaGlassFilterChip
import dev.seaglass.compose.SeaGlassFloatingSheet
import dev.seaglass.compose.SeaGlassFloatingSheetSurface
import dev.seaglass.compose.SeaGlassFloatingToolbar
import dev.seaglass.compose.SeaGlassFluidBar
import dev.seaglass.compose.SeaGlassGlyph
import dev.seaglass.compose.SeaGlassIcon
import dev.seaglass.compose.SeaGlassIconButton
import dev.seaglass.compose.SeaGlassInspectorSheetContent
import dev.seaglass.compose.SeaGlassLinearProgressIndicator
import dev.seaglass.compose.SeaGlassListItem
import dev.seaglass.compose.SeaGlassLiveActivityPill
import dev.seaglass.compose.SeaGlassMiniPlayerBar
import dev.seaglass.compose.SeaGlassMorphMode
import dev.seaglass.compose.SeaGlassMorphingButton
import dev.seaglass.compose.SeaGlassMorphingContextMenu
import dev.seaglass.compose.SeaGlassPill
import dev.seaglass.compose.SeaGlassRadioButton
import dev.seaglass.compose.SeaGlassSearchBar
import dev.seaglass.compose.SeaGlassSegmentedButtonRow
import dev.seaglass.compose.SeaGlassSnackbar
import dev.seaglass.compose.SeaGlassStageState
import dev.seaglass.compose.SeaGlassSurface
import dev.seaglass.compose.SeaGlassSwitch
import dev.seaglass.compose.SeaGlassTextField
import dev.seaglass.compose.SeaGlassTheme
import dev.seaglass.compose.SeaGlassTopAppBar
import dev.seaglass.compose.SeaGlassTwoElementMorph
import dev.seaglass.compose.defaultIosContextMenuItems
import dev.seaglass.compose.rememberSeaGlassStageState
import dev.seaglass.compose.seaGlass
import dev.seaglass.compose.seaGlassBackdrop
import dev.seaglass.core.SeaGlassColorMath
import dev.seaglass.core.SeaGlassDeviceHardwareProfile
import dev.seaglass.core.SeaGlassDeviceTier
import dev.seaglass.core.SeaGlassHdrSupport
import dev.seaglass.core.SeaGlassMaterial
import dev.seaglass.core.SeaGlassPalette
import dev.seaglass.core.SeaGlassSdfEngine
import dev.seaglass.core.SeaGlassShapeKind
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
    var deviceCornerProfile by remember { mutableStateOf(SeaGlassDeviceHardwareProfile.AUTO_DETECT) }
    var selectedTab by remember { mutableIntStateOf(0) }
    var actionOrbDetached by remember { mutableStateOf(false) }
    var showFloatingInspectorSheet by remember { mutableStateOf(false) }
    var showIosContextMenu by remember { mutableStateOf(false) }
    var showIosActionSheet by remember { mutableStateOf(false) }
    var sheetMorphAnimation by remember { mutableStateOf(true) }

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
        tierOverride = tierOverride,
        deviceCornerProfile = deviceCornerProfile
    ) {
        val stageState = rememberSeaGlassStageState()
        val systemBarsPadding = WindowInsets.systemBars.asPaddingValues()

        Box(modifier = Modifier.fillMaxSize()) {
            // 1. Luminous Material 3 Expressive coastal lightscape captured into the GPU stage
            Box(
                modifier = Modifier
                    .fillMaxSize()
                    .seaGlassBackdrop(stageState)
            ) {
                ExpressiveTidalBackdrop(
                    palette = activePalette,
                    modifier = Modifier.fillMaxSize()
                )

                if (selectedTab == 2 && (showFloatingInspectorSheet || showIosContextMenu || showIosActionSheet)) {
                    SessionTranscriptBackdropOverlay(
                        isDark = activePalette == SeaGlassPalette.ObsidianTide,
                        modifier = Modifier
                            .fillMaxSize()
                            .padding(
                                top = systemBarsPadding.calculateTopPadding() + 320.dp,
                                start = 36.dp,
                                end = 36.dp
                            )
                    )
                }
            }

            // 2. Foreground scrollable SeaGlass showcase content
            Column(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(
                        top = systemBarsPadding.calculateTopPadding() + 8.dp,
                        bottom = systemBarsPadding.calculateBottomPadding() + 148.dp,
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
                    },
                    onOpenFloatingSheet = { showFloatingInspectorSheet = true }
                )

                // Expressive Palette Strip with color gems and non-truncated labels
                PaletteQuickStrip(
                    stageState = stageState,
                    activePalette = activePalette,
                    onSelectPalette = { activePalette = it }
                )

                AnimatedContent(
                    targetState = selectedTab,
                    transitionSpec = {
                        (fadeIn(animationSpec = tween(220)) +
                            scaleIn(
                                initialScale = 0.96f,
                                animationSpec = spring(dampingRatio = 0.76f, stiffness = 420f)
                            )).togetherWith(
                            fadeOut(animationSpec = tween(150)) +
                                scaleOut(targetScale = 0.98f, animationSpec = tween(150))
                        )
                    },
                    label = "showcaseTabTransition"
                ) { activeTab ->
                    Column(
                        modifier = Modifier.fillMaxWidth(),
                        verticalArrangement = Arrangement.spacedBy(12.dp)
                    ) {
                        when (activeTab) {
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
                                activeCornerProfile = deviceCornerProfile,
                                onSelectCornerProfile = { deviceCornerProfile = it },
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

                            2 -> FloatingSheetShowcaseSection(
                                stageState = stageState,
                                surfaceMaterial = customSurfaceMaterial,
                                primaryMaterial = customPrimaryMaterial,
                                morphAnimationEnabled = sheetMorphAnimation,
                                onToggleMorphAnimation = { sheetMorphAnimation = it },
                                isOverlayOpen = showFloatingInspectorSheet || showIosContextMenu || showIosActionSheet,
                                onOpenOverlaySheet = { showFloatingInspectorSheet = true },
                                onOpenContextMenu = { showIosContextMenu = true },
                                onOpenActionSheet = { showIosActionSheet = true }
                            )

                            3 -> PaletteGallerySection(
                                stageState = stageState,
                                activePalette = activePalette,
                                onSelectPalette = { activePalette = it }
                            )

                            else -> UiComponentLibrarySection(
                                stageState = stageState,
                                primaryMaterial = customPrimaryMaterial,
                                surfaceMaterial = customSurfaceMaterial,
                                tertiaryMaterial = activePalette.tertiaryGlass,
                                onOpenFloatingSheet = { showFloatingInspectorSheet = true }
                            )
                        }
                    }
                }
            }

            // 3. Bottom Stacked iOS 26 Apple Music Style Liquid SeaGlass Mini-Player + 5-Tab Bar
            var miniPlayerPlaying by remember { mutableStateOf(true) }
            var miniTrackIndex by remember { mutableIntStateOf(0) }
            val miniTracks = remember {
                listOf(
                    "A Good Song Never Dies" to "Saint Motel",
                    "Pacific Surf & Driftwood" to "SeaGlass Spatial",
                    "Tropical House Essentials" to "Coastal Tide Mix"
                )
            }
            val currentTrack = miniTracks[miniTrackIndex % miniTracks.size]

            Column(
                verticalArrangement = Arrangement.spacedBy(8.dp),
                modifier = Modifier
                    .align(Alignment.BottomCenter)
                    .fillMaxWidth()
                    .padding(
                        start = 16.dp,
                        end = 16.dp,
                        bottom = systemBarsPadding.calculateBottomPadding() + 8.dp
                    )
            ) {
                SeaGlassMiniPlayerBar(
                    title = currentTrack.first,
                    subtitle = currentTrack.second,
                    isPlaying = miniPlayerPlaying,
                    onTogglePlay = { miniPlayerPlaying = !miniPlayerPlaying },
                    onNextTrack = {
                        miniTrackIndex = (miniTrackIndex + 1) % miniTracks.size
                        val all = SeaGlassPalette.entries
                        activePalette = all[(activePalette.ordinal + 1) % all.size]
                    },
                    stageState = stageState,
                    material = customSurfaceMaterial
                )

                SeaGlassFluidBar(
                    items = listOf("Optics", "Morph", "Sheet", "Palettes", "Library"),
                    selectedIndex = selectedTab,
                    onSelect = {
                        val next = it.coerceIn(0, 4)
                        selectedTab = next
                        if (next == 2) {
                            showFloatingInspectorSheet = true
                        }
                    },
                    stageState = stageState,
                    barMaterial = customSurfaceMaterial,
                    indicatorMaterial = customPrimaryMaterial,
                    actionLabel = if (actionOrbDetached) "⇄" else "✦",
                    actionDetached = actionOrbDetached,
                    showActionOrb = false,
                    onActionClick = { actionOrbDetached = !actionOrbDetached }
                )
            }

            // 4. Root iOS-Style Floating Concentric SeaGlass Detent Sheet Overlay
            SeaGlassFloatingSheet(
                visible = showFloatingInspectorSheet,
                onDismissRequest = { showFloatingInspectorSheet = false },
                stageState = stageState,
                material = customSurfaceMaterial.copy(
                    cornerRadiusDp = 42f,
                    transparency = (transparency + 0.08f).coerceIn(0.32f, 0.88f),
                    coarseness = coarseness,
                    frostRadiusDp = (frostRadiusDp + 6f).coerceAtMost(48f),
                    thicknessDp = 22f,
                    rimChalkiness = 0.88f
                ),
                outerInsetDp = 8.dp,
                enableMorphAnimation = sheetMorphAnimation
            ) {
                SeaGlassInspectorSheetContent(
                    stageState = stageState,
                    onDeleteSession = {
                        showFloatingInspectorSheet = false
                        showIosActionSheet = true
                    }
                )
            }

            // 5. Proper iOS Context Menu Overlay (Lifted Target Preview + Quick-Reaction Pill + Submenu Morph)
            SeaGlassContextMenuOverlay(
                visible = showIosContextMenu,
                onDismissRequest = { showIosContextMenu = false },
                stageState = stageState,
                material = customSurfaceMaterial.copy(
                    cornerRadiusDp = 24f,
                    transparency = (transparency + 0.08f).coerceIn(0.32f, 0.88f),
                    coarseness = coarseness,
                    frostRadiusDp = (frostRadiusDp + 6f).coerceAtMost(48f),
                    thicknessDp = 20f
                ),
                onItemSelected = { item ->
                    if (item.id == "delete") {
                        showIosActionSheet = true
                    }
                }
            )

            // 6. iOS Action Sheet Overlay with Detached Concentric "Cancel" Capsule
            SeaGlassActionSheet(
                visible = showIosActionSheet,
                title = "Delete Session Transcript?",
                message = "This will permanently remove Windows UI Redesign for SeaGlass Elements and its 199,692 context tokens.",
                actions = listOf(
                    SeaGlassContextMenuItem(
                        id = "export_first",
                        title = "Export Transcript First",
                        glyph = SeaGlassGlyph.Export
                    ),
                    SeaGlassContextMenuItem(
                        id = "archive",
                        title = "Archive Session",
                        glyph = SeaGlassGlyph.Bookmark
                    ),
                    SeaGlassContextMenuItem(
                        id = "confirm_delete",
                        title = "Delete Session",
                        glyph = SeaGlassGlyph.Close,
                        isDestructive = true
                    )
                ),
                onActionSelected = {},
                onDismissRequest = { showIosActionSheet = false },
                stageState = stageState,
                material = customSurfaceMaterial.copy(
                    cornerRadiusDp = 30f,
                    transparency = (transparency + 0.08f).coerceIn(0.32f, 0.88f),
                    coarseness = coarseness,
                    frostRadiusDp = (frostRadiusDp + 6f).coerceAtMost(48f),
                    thicknessDp = 20f
                )
            )
        }
    }
}

@Composable
private fun HeaderSpecimenBanner(
    stageState: SeaGlassStageState,
    palette: SeaGlassPalette,
    material: SeaGlassMaterial,
    onCyclePalette: () -> Unit,
    onOpenFloatingSheet: () -> Unit = {}
) {
    var morphedExpanded by remember { mutableStateOf(false) }
    val cornerRadius by animateFloatAsState(
        targetValue = if (morphedExpanded) 38f else 28f,
        animationSpec = spring(dampingRatio = 0.60f, stiffness = 360f),
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
        contentPadding = PaddingValues(horizontal = 18.dp, vertical = 12.dp),
        modifier = Modifier.fillMaxWidth()
    ) {
        Column(verticalArrangement = Arrangement.spacedBy(3.dp)) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Column(modifier = Modifier.weight(1f)) {
                    Text(
                        text = "SEAGLASS OPTICAL STUDIO",
                        maxLines = 1,
                        style = MaterialTheme.typography.labelSmall.copy(
                            fontWeight = FontWeight.ExtraBold,
                            letterSpacing = 1.2.sp,
                            fontSize = 10.sp
                        ),
                        color = LocalContentColor.current.copy(alpha = 0.76f)
                    )
                    Text(
                        text = palette.displayName,
                        style = MaterialTheme.typography.titleLarge.copy(
                            fontWeight = FontWeight.Black
                        ),
                        color = LocalContentColor.current
                    )
                }

                Row(
                    horizontalArrangement = Arrangement.spacedBy(8.dp),
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    SeaGlassButton(
                        onClick = onCyclePalette,
                        stageState = stageState,
                        material = palette.tertiaryGlass.copy(
                            cornerRadiusDp = 22f,
                            thicknessDp = 16f,
                            rimChalkiness = 0.86f
                        ),
                        contentPadding = PaddingValues(horizontal = 12.dp, vertical = 9.dp)
                    ) {
                        Text(
                            text = "Tide ↻",
                            maxLines = 1,
                            style = MaterialTheme.typography.labelMedium.copy(
                                fontWeight = FontWeight.Bold
                            )
                        )
                    }

                    // (•••) Trigger Button that opens the iOS-style Floating Concentric Glass Sheet
                    SeaGlassButton(
                        onClick = onOpenFloatingSheet,
                        stageState = stageState,
                        material = palette.surfaceGlass.copy(
                            cornerRadiusDp = 20f,
                            thicknessDp = 16f
                        ),
                        contentPadding = PaddingValues(horizontal = 11.dp, vertical = 12.dp)
                    ) {
                        Row(horizontalArrangement = Arrangement.spacedBy(3.dp)) {
                            repeat(3) {
                                Box(
                                    modifier = Modifier
                                        .size(4.dp)
                                        .clip(CircleShape)
                                        .background(LocalContentColor.current)
                                )
                            }
                        }
                    }
                }
            }

            Text(
                text = palette.tagline,
                maxLines = 1,
                style = MaterialTheme.typography.bodySmall.copy(
                    fontWeight = FontWeight.Medium,
                    fontSize = 12.sp
                ),
                color = LocalContentColor.current.copy(alpha = 0.88f)
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
    activeCornerProfile: SeaGlassDeviceHardwareProfile,
    onSelectCornerProfile: (SeaGlassDeviceHardwareProfile) -> Unit,
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
    val targetOrbGapDp = when (heroFlowState) {
        0 -> -2f
        1 -> 9f
        else -> 22f
    }
    val playOrbGapDp by animateFloatAsState(
        targetValue = targetOrbGapDp,
        animationSpec = spring(dampingRatio = 0.54f, stiffness = 300f),
        label = "playOrbGap"
    )
    val deckFlowMalleability = (kotlin.math.abs(playOrbGapDp - targetOrbGapDp) / 10f).coerceIn(0f, 1f)

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
                material = primaryMaterial.deaged(kotlin.math.max(deckFlowMalleability, deckTouchIntensity * 0.7f)),
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

    // 2. 1-Tap Optical Finish Presets across the Sand-Frosted Coarseness Continuum
    Row(
        modifier = Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.spacedBy(8.dp)
    ) {
        SeaGlassPill(
            label = "Clear Glass",
            selected = transparency > 0.80f && coarseness < 0.20f,
            onClick = { onApplyPreset(0.98f, 0.00f, 1.5f, 0.48f, 18f, 1.58f, 0.32f) },
            stageState = stageState,
            selectedMaterial = primaryMaterial,
            unselectedMaterial = surfaceMaterial,
            modifier = Modifier.weight(1f)
        )
        SeaGlassPill(
            label = "Sand Frosted",
            selected = transparency in 0.45f..0.80f,
            onClick = { onApplyPreset(0.64f, 0.44f, 22f, 0.64f, 16f, 1.54f, 0.26f) },
            stageState = stageState,
            selectedMaterial = primaryMaterial,
            unselectedMaterial = surfaceMaterial,
            modifier = Modifier.weight(1f)
        )
        SeaGlassPill(
            label = "Heavy Sand",
            selected = transparency < 0.45f,
            onClick = { onApplyPreset(0.10f, 0.95f, 44f, 0.78f, 18f, 1.50f, 0.14f) },
            stageState = stageState,
            selectedMaterial = primaryMaterial,
            unselectedMaterial = surfaceMaterial,
            modifier = Modifier.weight(1f)
        )
    }

    // 3. Dedicated Hero "Sand-Frosted Coarseness & Transparency Continuum" Card
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
                    text = "SAND-FROSTED CONTINUUM",
                    maxLines = 1,
                    style = MaterialTheme.typography.labelSmall.copy(
                        fontWeight = FontWeight.ExtraBold,
                        letterSpacing = 1.0.sp
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
                        text = String.format(Locale.US, "%.0f%% Clear · %.0f%% Sand", transparency * 100f, coarseness * 100f),
                        maxLines = 1,
                        style = MaterialTheme.typography.labelSmall.copy(
                            fontFamily = FontFamily.Monospace,
                            fontWeight = FontWeight.Black,
                            fontSize = 10.sp
                        ),
                        color = LocalContentColor.current
                    )
                }
            }
            Slider(
                value = transparency,
                onValueChange = { newTrans ->
                    onTransparencyChange(newTrans)
                    // Smoothly couple sand coarseness and frost radius along the sand-erosion continuum
                    val wear = (1f - newTrans).coerceIn(0f, 1f)
                    onCoarsenessChange(wear)
                    onFrostRadiusChange(1.5f + wear * 42f)
                },
                valueRange = 0.00f..1.00f,
                colors = SliderDefaults.colors(
                    thumbColor = LocalContentColor.current,
                    activeTrackColor = LocalContentColor.current.copy(alpha = 0.88f),
                    inactiveTrackColor = LocalContentColor.current.copy(alpha = 0.24f)
                ),
                modifier = Modifier.height(24.dp)
            )
        }
    }

    // 4. Compact Optical Parameter Controls Card
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
                valueRange = 1f..48f,
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

    // 5. Adaptive Device & Page Corner Optics Lab (Concentric Bezel & Page Corner Matching)
    SeaGlassAdaptiveCornerLabCard(
        stageState = stageState,
        activeProfile = activeCornerProfile,
        onSelectProfile = onSelectCornerProfile,
        surfaceMaterial = surfaceMaterial,
        primaryMaterial = primaryMaterial,
        tertiaryMaterial = tertiaryMaterial
    )
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

    // 1. Composer Toolbar Popover & Teardrop Context Menu Morphs Card
    var contextMenuExpanded by remember { mutableStateOf(false) }
    var modelMorphStage by remember { mutableIntStateOf(0) }
    var selectedModelName by remember { mutableStateOf("Gemini 4 Argon EAP") }
    var effortLevelFraction by remember { mutableFloatStateOf(0.76f) }
    var contextRingExpanded by remember { mutableStateOf(false) }

    SeaGlassCard(
        stageState = stageState,
        material = surfaceMaterial,
        modifier = Modifier
            .fillMaxWidth()
            .zIndex(30f)
    ) {
        Text(
            text = "IN-PLACE POPOVER & TEARDROP MENU MORPHS",
            style = MaterialTheme.typography.labelSmall.copy(
                fontWeight = FontWeight.ExtraBold,
                letterSpacing = 1.1.sp
            ),
            color = LocalContentColor.current.copy(alpha = 0.76f)
        )
        Text(
            text = "Tap the model pill, context ring, or (•••) menu to morph in-place with Gaussian content focus.",
            style = MaterialTheme.typography.bodySmall.copy(fontSize = 12.sp),
            color = LocalContentColor.current.copy(alpha = 0.86f)
        )

        Row(
            modifier = Modifier
                .fillMaxWidth()
                .zIndex(35f),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            SeaGlassCascadedModelMorph(
                stage = modelMorphStage,
                onStageChange = {
                    modelMorphStage = it
                    if (it > 0) {
                        contextMenuExpanded = false
                        contextRingExpanded = false
                    }
                },
                selectedModel = selectedModelName,
                onSelectModel = { selectedModelName = it },
                effortFraction = effortLevelFraction,
                onEffortChange = { effortLevelFraction = it },
                stageState = stageState,
                pillMaterial = primaryMaterial,
                popoverMaterial = surfaceMaterial,
                modifier = Modifier.zIndex(if (modelMorphStage > 0) 40f else 1f)
            )

            SeaGlassContextRingMorph(
                expanded = contextRingExpanded,
                onExpandedChange = {
                    contextRingExpanded = it
                    if (it) {
                        contextMenuExpanded = false
                        modelMorphStage = 0
                    }
                },
                progress = 0.35f,
                usedLabel = "366K of 1.05M tokens",
                remainingLabel = "683K left",
                stageState = stageState,
                ringMaterial = primaryMaterial,
                popoverMaterial = surfaceMaterial,
                modifier = Modifier.zIndex(if (contextRingExpanded) 40f else 1f)
            )

            SeaGlassMorphingContextMenu(
                expanded = contextMenuExpanded,
                onExpandedChange = {
                    contextMenuExpanded = it
                    if (it) {
                        modelMorphStage = 0
                        contextRingExpanded = false
                    }
                },
                stageState = stageState,
                anchorAlignment = Alignment.TopEnd,
                buttonMaterial = primaryMaterial,
                menuMaterial = surfaceMaterial,
                modifier = Modifier.zIndex(if (contextMenuExpanded) 40f else 1f)
            )
        }
    }

    // 2. Dynamic 2-Element Morph Primitive (SeaGlassTwoElementMorph)
    var twoElementExpanded by remember { mutableStateOf(false) }
    var buddingExpanded by remember { mutableStateOf(true) }
    var morphBtn0Active by remember { mutableStateOf(false) }
    var morphBtn1Active by remember { mutableStateOf(true) }
    var morphBtn2Active by remember { mutableStateOf(false) }

    Box(
        contentAlignment = Alignment.Center,
        modifier = Modifier.fillMaxWidth()
    ) {
        SeaGlassTwoElementMorph(
            expanded = twoElementExpanded,
            onToggle = { twoElementExpanded = !twoElementExpanded },
            stageState = stageState,
            collapsedMaterial = primaryMaterial.copy(cornerRadiusDp = 28f),
            expandedMaterial = surfaceMaterial.copy(cornerRadiusDp = 32f),
            morphMode = SeaGlassMorphMode.LIQUID_COALESCE,
            collapsedContent = {
                Row(
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(10.dp),
                    modifier = Modifier.padding(horizontal = 22.dp, vertical = 14.dp)
                ) {
                    Text(
                        text = "✦",
                        style = MaterialTheme.typography.titleMedium.copy(fontWeight = FontWeight.Black)
                    )
                    Column {
                        Text(
                            text = "2-Element Dynamic Morph",
                            style = MaterialTheme.typography.labelLarge.copy(fontWeight = FontWeight.Black)
                        )
                        Text(
                            text = "Tap to auto-measure & morph into Sanctuary Sheet ↗",
                            style = MaterialTheme.typography.labelSmall.copy(fontSize = 10.sp),
                            color = LocalContentColor.current.copy(alpha = 0.82f)
                        )
                    }
                }
            },
            expandedContent = {
                Column(
                    verticalArrangement = Arrangement.spacedBy(8.dp),
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(horizontal = 20.dp, vertical = 16.dp)
                ) {
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Text(
                            text = "EXPANDED SANCTUARY SHEET",
                            style = MaterialTheme.typography.labelSmall.copy(
                                fontWeight = FontWeight.ExtraBold,
                                letterSpacing = 1.1.sp
                            ),
                            color = LocalContentColor.current.copy(alpha = 0.78f)
                        )
                        Text(
                            text = "Tap to collapse ↙",
                            style = MaterialTheme.typography.labelSmall.copy(fontWeight = FontWeight.Bold)
                        )
                    }
                    Text(
                        text = "High Tide · 68° Coastal Swell",
                        style = MaterialTheme.typography.titleLarge.copy(fontWeight = FontWeight.Black)
                    )
                    Text(
                        text = "SeaGlassTwoElementMorph automatically measured both composables, synthesized a transient sminCubic liquid bridge, and flexed glass thickness & IOR lensing in flight.",
                        style = MaterialTheme.typography.bodySmall,
                        color = LocalContentColor.current.copy(alpha = 0.88f)
                    )
                }
            }
        )
    }

    // 3. M3 Expressive Polygon Choreography & Shape-Morphing Buttons Card
    SeaGlassCard(
        stageState = stageState,
        material = surfaceMaterial,
        modifier = Modifier.fillMaxWidth()
    ) {
        Text(
            text = "M3 EXPRESSIVE POLYGON CHOREOGRAPHY",
            style = MaterialTheme.typography.labelSmall.copy(
                fontWeight = FontWeight.ExtraBold,
                letterSpacing = 1.1.sp
            ),
            color = LocalContentColor.current.copy(alpha = 0.76f)
        )

        SeaGlassChoreographedShapeOrb(
            stageState = stageState,
            autoPlay = true,
            primaryMaterial = primaryMaterial,
            secondaryMaterial = tertiaryMaterial
        )

        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(10.dp)
        ) {
            SeaGlassMorphingButton(
                active = morphBtn0Active,
                onClick = { morphBtn0Active = !morphBtn0Active },
                stageState = stageState,
                restingShape = SeaGlassShapeKind.Cookie9,
                activeShape = SeaGlassShapeKind.Squircle,
                restingMaterial = primaryMaterial,
                activeMaterial = tertiaryMaterial,
                modifier = Modifier
                    .weight(1f)
                    .height(60.dp)
            ) {
                Text(
                    text = if (morphBtn0Active) "Squircle" else "Cookie9",
                    maxLines = 1,
                    style = MaterialTheme.typography.labelMedium.copy(fontWeight = FontWeight.Black)
                )
            }

            SeaGlassMorphingButton(
                active = morphBtn1Active,
                onClick = { morphBtn1Active = !morphBtn1Active },
                stageState = stageState,
                restingShape = SeaGlassShapeKind.SoftBurst,
                activeShape = SeaGlassShapeKind.Circle,
                restingMaterial = tertiaryMaterial,
                activeMaterial = primaryMaterial,
                modifier = Modifier
                    .weight(1f)
                    .height(60.dp)
            ) {
                Text(
                    text = if (morphBtn1Active) "Droplet" else "Burst12",
                    maxLines = 1,
                    style = MaterialTheme.typography.labelMedium.copy(fontWeight = FontWeight.Black)
                )
            }

            SeaGlassMorphingButton(
                active = morphBtn2Active,
                onClick = { morphBtn2Active = !morphBtn2Active },
                stageState = stageState,
                restingShape = SeaGlassShapeKind.Cookie4,
                activeShape = SeaGlassShapeKind.Gem,
                restingMaterial = primaryMaterial,
                activeMaterial = tertiaryMaterial,
                modifier = Modifier
                    .weight(1f)
                    .height(60.dp)
            ) {
                Text(
                    text = if (morphBtn2Active) "Gem8" else "Clover4",
                    maxLines = 1,
                    style = MaterialTheme.typography.labelMedium.copy(fontWeight = FontWeight.Black)
                )
            }
        }
    }

    // 4. Apple Liquid Glass Style Budding Action Cluster (SeaGlassBuddingCluster)
    SeaGlassBuddingCluster(
        expanded = buddingExpanded,
        onToggleExpanded = { buddingExpanded = !buddingExpanded },
        items = listOf(
            "♪" to "Surf",
            "☀" to "68° UV",
            "◈" to "Lens"
        ),
        onItemClick = { coalescedPreset = !coalescedPreset },
        stageState = stageState,
        anchorMaterial = primaryMaterial,
        satelliteMaterial = tertiaryMaterial
    )

    // 4. Hero Interactive Multi-Node SDF Arena with Polar Polygon Shape Morphing + sminCubic Coalescing!
    val morphShapeTour = remember {
        listOf(
            SeaGlassShapeKind.Circle,
            SeaGlassShapeKind.Cookie9,
            SeaGlassShapeKind.Clover4,
            SeaGlassShapeKind.Flower,
            SeaGlassShapeKind.Heart,
            SeaGlassShapeKind.SoftBurst,
            SeaGlassShapeKind.Sunny,
            SeaGlassShapeKind.Gem,
            SeaGlassShapeKind.Squircle
        )
    }
    var dropletShapeIdx by remember { mutableIntStateOf(1) } // Default Cookie9 + Clover4 coalescing!
    val nextDropletShapeIdx = (dropletShapeIdx + 1) % morphShapeTour.size
    val dropletShapeProgress by animateFloatAsState(
        targetValue = dropletShapeIdx.toFloat(),
        animationSpec = spring(dampingRatio = 0.54f, stiffness = 320f),
        label = "dropletPolygonMorph"
    )
    val baseIdx = kotlin.math.floor(
        dropletShapeProgress.coerceIn(0f, (morphShapeTour.size - 1).toFloat())
    ).toInt().coerceIn(0, morphShapeTour.size - 1)
    val nextIdx = (baseIdx + 1).coerceAtMost(morphShapeTour.size - 1)
    val localMorphFrac = dropletShapeProgress - baseIdx.toFloat()

    val arenaMorphNodes = remember(
        centerAX,
        centerAY,
        centerBX,
        centerBY,
        dropletARadiusPx,
        dropletBRadiusPx,
        primaryMaterial,
        tertiaryMaterial,
        baseIdx,
        nextIdx,
        localMorphFrac
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
                thicknessPx = dropletARadiusPx * 0.62f,
                startShapeKind = morphShapeTour[baseIdx],
                endShapeKind = morphShapeTour[nextIdx],
                shapeMorphProgress = localMorphFrac
            ),
            SeaGlassSdfEngine.SdfNode(
                centerX = centerBX,
                centerY = centerBY,
                halfWidth = dropletBRadiusPx,
                halfHeight = dropletBRadiusPx,
                uniformCornerRadius = dropletBRadiusPx,
                cornerSmoothing = 2.2f,
                tintColorArgb = tertiaryMaterial.tintColorArgb,
                thicknessPx = dropletBRadiusPx * 0.65f,
                startShapeKind = morphShapeTour[(baseIdx + 2) % morphShapeTour.size],
                endShapeKind = morphShapeTour[(nextIdx + 2) % morphShapeTour.size],
                shapeMorphProgress = localMorphFrac
            )
        )
    }

    val tiltState = LocalSeaGlassTiltState.current
    var arenaTouchPoint by remember { mutableStateOf<Offset?>(null) }
    var arenaPressed by remember { mutableStateOf(false) }
    val arenaTouchIntensity by animateFloatAsState(
        targetValue = if (arenaPressed) 1f else 0f,
        animationSpec = spring(dampingRatio = 0.60f, stiffness = 580f),
        label = "arenaTouchIntensity"
    )
    val sepFlightEnergy = (kotlin.math.abs(animatedSepDp - targetSeparationDp) / 36f).coerceIn(0f, 1f)
    val shapeFlightEnergy = (4f * localMorphFrac.coerceIn(0f, 1f) * (1f - localMorphFrac.coerceIn(0f, 1f)))
        .coerceIn(0f, 1f)
    val arenaMalleability = maxOf(sepFlightEnergy, shapeFlightEnergy, arenaTouchIntensity * 0.85f).coerceIn(0f, 1f)

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
                material = primaryMaterial.deaged(arenaMalleability),
                nodesOptional = arenaMorphNodes,
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
                .clickable(
                    interactionSource = remember { MutableInteractionSource() },
                    indication = null,
                    onClick = { dropletShapeIdx = nextDropletShapeIdx }
                )
        ) {
            Column(horizontalAlignment = Alignment.CenterHorizontally) {
                Text(
                    text = morphShapeTour[dropletShapeIdx].shortLabel.uppercase(Locale.US),
                    style = MaterialTheme.typography.labelLarge.copy(fontWeight = FontWeight.Black),
                    color = labelColor
                )
                Text(
                    text = "Tap Shape ↻",
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
                .clickable(
                    interactionSource = remember { MutableInteractionSource() },
                    indication = null,
                    onClick = { dropletShapeIdx = nextDropletShapeIdx }
                )
        ) {
            Column(horizontalAlignment = Alignment.CenterHorizontally) {
                Text(
                    text = morphShapeTour[(dropletShapeIdx + 2) % morphShapeTour.size].shortLabel.uppercase(Locale.US),
                    style = MaterialTheme.typography.labelLarge.copy(fontWeight = FontWeight.Black),
                    color = labelColor
                )
                Text(
                    text = "Drag / Morph",
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
private fun FloatingSheetShowcaseSection(
    stageState: SeaGlassStageState,
    surfaceMaterial: SeaGlassMaterial,
    primaryMaterial: SeaGlassMaterial,
    morphAnimationEnabled: Boolean,
    onToggleMorphAnimation: (Boolean) -> Unit,
    isOverlayOpen: Boolean,
    onOpenOverlaySheet: () -> Unit,
    onOpenContextMenu: () -> Unit = {},
    onOpenActionSheet: () -> Unit = {}
) {
    var liveActivityExpanded by remember { mutableStateOf(false) }

    // 0. iOS Dynamic Island / Live Activity Expandable Capsule
    Box(
        contentAlignment = Alignment.Center,
        modifier = Modifier.fillMaxWidth()
    ) {
        SeaGlassLiveActivityPill(
            expanded = liveActivityExpanded,
            onExpandedChange = { liveActivityExpanded = it },
            stageState = stageState,
            pillMaterial = primaryMaterial,
            expandedMaterial = surfaceMaterial
        )
    }

    // 1. iOS-Style Floating Session Top Bar: [=]  [Windows UI Redesign for SeaGlass...]  [•••]
    Row(
        modifier = Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.spacedBy(10.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        SeaGlassIconButton(
            glyph = SeaGlassGlyph.Menu,
            onClick = onOpenContextMenu,
            stageState = stageState,
            size = 46.dp,
            material = surfaceMaterial
        )

        SeaGlassSurface(
            stageState = stageState,
            material = surfaceMaterial.copy(cornerRadiusDp = 24f, thicknessDp = 16f),
            onClick = onOpenOverlaySheet,
            contentPadding = PaddingValues(horizontal = 16.dp, vertical = 9.dp),
            modifier = Modifier.weight(1f)
        ) {
            Column(verticalArrangement = Arrangement.spacedBy(1.dp)) {
                Text(
                    text = "Windows UI Redesign for SeaGlass…",
                    maxLines = 1,
                    style = MaterialTheme.typography.bodyMedium.copy(
                        fontWeight = FontWeight.Black,
                        fontSize = 13.5.sp
                    ),
                    color = LocalContentColor.current
                )
                Text(
                    text = "C:\\Users\\maxw6\\Projects\\SeaGlass",
                    maxLines = 1,
                    style = MaterialTheme.typography.labelSmall.copy(
                        fontSize = 11.sp
                    ),
                    color = LocalContentColor.current.copy(alpha = 0.65f)
                )
            }
        }

        SeaGlassButton(
            onClick = onOpenOverlaySheet,
            stageState = stageState,
            material = primaryMaterial.copy(cornerRadiusDp = 23f, thicknessDp = 16f),
            contentPadding = PaddingValues(horizontal = 13.dp, vertical = 14.dp),
            modifier = Modifier.size(46.dp)
        ) {
            Row(horizontalArrangement = Arrangement.spacedBy(3.dp)) {
                repeat(3) {
                    Box(
                        modifier = Modifier
                            .size(4.2.dp)
                            .clip(CircleShape)
                            .background(LocalContentColor.current)
                    )
                }
            }
        }
    }

    // 2. 3-Button iOS Overlay Launcher Strip: Detent Sheet · Context Menu · Action Sheet
    Row(
        modifier = Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.spacedBy(8.dp)
    ) {
        SeaGlassPill(
            label = "Detent Sheet",
            selected = true,
            onClick = onOpenOverlaySheet,
            stageState = stageState,
            selectedMaterial = primaryMaterial,
            unselectedMaterial = surfaceMaterial,
            modifier = Modifier.weight(1f)
        )
        SeaGlassPill(
            label = "Context Menu",
            selected = false,
            onClick = onOpenContextMenu,
            stageState = stageState,
            selectedMaterial = primaryMaterial,
            unselectedMaterial = surfaceMaterial,
            modifier = Modifier.weight(1f)
        )
        SeaGlassPill(
            label = "Action Sheet",
            selected = false,
            onClick = onOpenActionSheet,
            stageState = stageState,
            selectedMaterial = primaryMaterial,
            unselectedMaterial = surfaceMaterial,
            modifier = Modifier.weight(1f)
        )
    }

    // 3. Controls for Morph Animation vs. Slide Spring
    SeaGlassSegmentedButtonRow(
        options = listOf("Morph Transition", "Slide Spring"),
        selectedIndex = if (morphAnimationEnabled) 0 else 1,
        onSelect = { onToggleMorphAnimation(it == 0) },
        stageState = stageState,
        containerMaterial = surfaceMaterial,
        indicatorMaterial = primaryMaterial,
        modifier = Modifier.fillMaxWidth()
    )

    if (!isOverlayOpen) {
        // 4. Inline Native Floating Concentric SeaGlass Inspector Sheet Card when modal is dismissed
        SeaGlassFloatingSheetSurface(
            stageState = stageState,
            material = surfaceMaterial.copy(
                cornerRadiusDp = 42f,
                transparency = (surfaceMaterial.transparency + 0.08f).coerceIn(0.32f, 0.88f),
                thicknessDp = 22f,
                rimChalkiness = 0.88f
            )
        ) {
            SeaGlassInspectorSheetContent(
                stageState = stageState,
                onDeleteSession = onOpenActionSheet
            )
        }
    }
}

@Composable
private fun SessionTranscriptBackdropOverlay(
    isDark: Boolean,
    modifier: Modifier = Modifier
) {
    val lines = remember {
        listOf(
            "tool schemas before calling them:",
            "CronCreate",
            "CronDelete",
            "CronList",
            "EnterPlanMode",
            "EnterWorktree",
            "ExitPlanMode",
            "ExitWorktree",
            "Monitor",
            "NotebookEdit",
            "PushNotification",
            "SendMessage",
            "TaskCreate",
            "TaskGet",
            "TaskList",
            "VerifyPlanExecution"
        )
    }
    val textColor = if (isDark) Color.White.copy(alpha = 0.88f) else Color.White.copy(alpha = 0.92f)
    Column(
        modifier = modifier,
        verticalArrangement = Arrangement.spacedBy(6.dp)
    ) {
        lines.forEachIndexed { idx, line ->
            Text(
                text = line,
                color = if (idx == 0) textColor.copy(alpha = 0.72f) else textColor,
                style = MaterialTheme.typography.titleMedium.copy(
                    fontWeight = if (idx == 0) FontWeight.SemiBold else FontWeight.Bold,
                    fontSize = 18.sp,
                    letterSpacing = (-0.2).sp
                )
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
                        maxLines = 1,
                        style = MaterialTheme.typography.titleSmall.copy(
                            fontWeight = FontWeight.Black,
                            fontSize = 15.sp
                        )
                    )
                    Text(
                        text = String.format(
                            Locale.US,
                            "%.0f%% Clear · %.0f%% Sand · %.2f IOR",
                            palette.primaryGlass.transparency * 100f,
                            palette.primaryGlass.coarseness * 100f,
                            palette.primaryGlass.ior
                        ),
                        maxLines = 1,
                        style = MaterialTheme.typography.labelSmall.copy(
                            fontSize = 9.sp,
                            letterSpacing = (-0.1).sp
                        ),
                        color = LocalContentColor.current.copy(alpha = 0.85f)
                    )
                }
            }
        }
    }
}

@Composable
private fun UiComponentLibrarySection(
    stageState: SeaGlassStageState,
    primaryMaterial: SeaGlassMaterial,
    surfaceMaterial: SeaGlassMaterial,
    tertiaryMaterial: SeaGlassMaterial,
    onOpenFloatingSheet: () -> Unit = {}
) {
    var segmentIdx by remember { mutableIntStateOf(0) }
    var toolbarActionIdx by remember { mutableIntStateOf(0) }
    var toolbarFabDetached by remember { mutableStateOf(false) }
    var fabExpanded by remember { mutableStateOf(true) }

    var switchA by remember { mutableStateOf(true) }
    var switchB by remember { mutableStateOf(false) }
    var checkA by remember { mutableStateOf(true) }
    var checkB by remember { mutableStateOf(false) }
    var radioIdx by remember { mutableIntStateOf(0) }

    var filterSpatial by remember { mutableStateOf(true) }
    var filterHdr by remember { mutableStateOf(true) }
    var filterCoalesce by remember { mutableStateOf(false) }

    var textFieldValue by remember { mutableStateOf("Coastal Acoustic Suite") }
    var progressVal by remember { mutableFloatStateOf(0.74f) }
    var showDialog by remember { mutableStateOf(true) }
    var showSnackbar by remember { mutableStateOf(true) }

    // 1. Floating Top App Bar
    SeaGlassTopAppBar(
        title = "SeaGlass App UI Kit",
        subtitle = "Material 3 Expressive × Sand-Frosted Glass",
        stageState = stageState,
        navigationGlyph = SeaGlassGlyph.Menu,
        onNavigationClick = { fabExpanded = !fabExpanded },
        material = surfaceMaterial
    ) {
        SeaGlassBadge(
            text = "26 UI",
            stageState = stageState,
            material = tertiaryMaterial
        )
        SeaGlassIconButton(
            glyph = SeaGlassGlyph.Bell,
            onClick = { showSnackbar = !showSnackbar },
            stageState = stageState,
            size = 38.dp,
            material = surfaceMaterial
        )
        SeaGlassIconButton(
            glyph = SeaGlassGlyph.Settings,
            onClick = { showDialog = !showDialog },
            stageState = stageState,
            selected = showDialog,
            size = 38.dp
        )
    }

    // 2. Segmented Control switching between Controls, Surfaces, and Overlays
    SeaGlassSegmentedButtonRow(
        options = listOf("Controls", "Surfaces", "Overlays"),
        selectedIndex = segmentIdx,
        onSelect = {
            segmentIdx = it
            progressVal = when (it) {
                0 -> 0.74f
                1 -> 0.56f
                else -> 0.90f
            }
        },
        stageState = stageState,
        containerMaterial = surfaceMaterial,
        indicatorMaterial = primaryMaterial
    )

    AnimatedContent(
        targetState = segmentIdx,
        transitionSpec = {
            (fadeIn(animationSpec = tween(200)) +
                scaleIn(
                    initialScale = 0.96f,
                    animationSpec = spring(dampingRatio = 0.74f, stiffness = 420f)
                )).togetherWith(
                fadeOut(animationSpec = tween(140)) +
                    scaleOut(targetScale = 0.98f, animationSpec = tween(140))
            )
        },
        label = "uiLibraryCategoryMorph"
    ) { activeCategory ->
        Column(
            modifier = Modifier.fillMaxWidth(),
            verticalArrangement = Arrangement.spacedBy(10.dp)
        ) {
            when (activeCategory) {
                0 -> {
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.spacedBy(8.dp),
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        SeaGlassFilterChip(
                            label = "Spatial",
                            selected = filterSpatial,
                            onClick = { filterSpatial = !filterSpatial },
                            stageState = stageState,
                            leadingGlyph = SeaGlassGlyph.AudioWave,
                            selectedMaterial = primaryMaterial,
                            unselectedMaterial = surfaceMaterial
                        )
                        SeaGlassFilterChip(
                            label = "Sand Frost",
                            selected = filterHdr,
                            onClick = { filterHdr = !filterHdr },
                            stageState = stageState,
                            leadingGlyph = SeaGlassGlyph.Sparkles,
                            selectedMaterial = primaryMaterial,
                            unselectedMaterial = surfaceMaterial
                        )
                        SeaGlassFilterChip(
                            label = "Coalesce",
                            selected = filterCoalesce,
                            onClick = { filterCoalesce = !filterCoalesce },
                            stageState = stageState,
                            leadingGlyph = SeaGlassGlyph.Layers,
                            selectedMaterial = primaryMaterial,
                            unselectedMaterial = surfaceMaterial
                        )
                    }

                    SeaGlassFloatingToolbar(
                        actions = listOf(
                            SeaGlassGlyph.Play to "Stream",
                            SeaGlassGlyph.Heart to "Favorite",
                            SeaGlassGlyph.Share to "Share",
                            SeaGlassGlyph.Bookmark to "Save"
                        ),
                        selectedActionIndex = toolbarActionIdx,
                        onActionSelect = { toolbarActionIdx = it },
                        fabGlyph = if (toolbarFabDetached) SeaGlassGlyph.Close else SeaGlassGlyph.Plus,
                        fabDetached = toolbarFabDetached,
                        onFabClick = { toolbarFabDetached = !toolbarFabDetached },
                        stageState = stageState,
                        barMaterial = surfaceMaterial,
                        fabMaterial = primaryMaterial
                    )

                    SeaGlassCard(
                        stageState = stageState,
                        material = surfaceMaterial,
                        modifier = Modifier.fillMaxWidth()
                    ) {
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.SpaceBetween,
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Column(modifier = Modifier.weight(1f)) {
                                Text(
                                    text = "SELECTION, INPUT & PROGRESS",
                                    style = MaterialTheme.typography.labelSmall.copy(
                                        fontWeight = FontWeight.ExtraBold,
                                        letterSpacing = 1.2.sp
                                    ),
                                    color = LocalContentColor.current.copy(alpha = 0.76f)
                                )
                                Text(
                                    text = "Switches, Checkboxes & FAB",
                                    style = MaterialTheme.typography.titleMedium.copy(fontWeight = FontWeight.Black)
                                )
                            }
                            SeaGlassExtendedFab(
                                text = "Create",
                                glyph = SeaGlassGlyph.Sparkles,
                                expanded = fabExpanded,
                                onClick = { fabExpanded = !fabExpanded },
                                stageState = stageState,
                                material = primaryMaterial
                            )
                        }

                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.SpaceBetween,
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Row(
                                horizontalArrangement = Arrangement.spacedBy(10.dp),
                                verticalAlignment = Alignment.CenterVertically
                            ) {
                                SeaGlassSwitch(
                                    checked = switchA,
                                    onCheckedChange = { switchA = it },
                                    stageState = stageState,
                                    checkedMaterial = primaryMaterial,
                                    uncheckedMaterial = surfaceMaterial
                                )
                                SeaGlassCheckbox(
                                    checked = checkA,
                                    onCheckedChange = { checkA = it },
                                    stageState = stageState,
                                    checkedMaterial = primaryMaterial,
                                    uncheckedMaterial = surfaceMaterial
                                )
                                SeaGlassCheckbox(
                                    checked = checkB,
                                    onCheckedChange = { checkB = it },
                                    stageState = stageState,
                                    checkedMaterial = tertiaryMaterial,
                                    uncheckedMaterial = surfaceMaterial
                                )
                                SeaGlassRadioButton(
                                    selected = radioIdx == 0,
                                    onClick = { radioIdx = 0 },
                                    stageState = stageState,
                                    selectedMaterial = primaryMaterial,
                                    unselectedMaterial = surfaceMaterial
                                )
                                SeaGlassRadioButton(
                                    selected = radioIdx == 1,
                                    onClick = { radioIdx = 1 },
                                    stageState = stageState,
                                    selectedMaterial = primaryMaterial,
                                    unselectedMaterial = surfaceMaterial
                                )
                            }

                            SeaGlassCircularProgressIndicator(
                                progress = progressVal,
                                stageState = stageState,
                                material = surfaceMaterial,
                                accentColor = Color(primaryMaterial.tintColorArgb)
                            )
                        }

                        SeaGlassLinearProgressIndicator(
                            progress = progressVal,
                            stageState = stageState,
                            trackMaterial = surfaceMaterial,
                            fillMaterial = primaryMaterial
                        )

                        SeaGlassTextField(
                            value = textFieldValue,
                            onValueChange = { textFieldValue = it },
                            stageState = stageState,
                            label = "PRESET SCENE NAME",
                            placeholder = "Enter preset title…",
                            leadingGlyph = SeaGlassGlyph.Tune,
                            material = surfaceMaterial
                        )
                    }
                }

                1 -> {
                    SeaGlassListItem(
                        headlineText = "Sand-Frosted Continuum",
                        supportingText = "Smoothly spans clear untreated glass to opaque sand-frosted glass",
                        leadingGlyph = SeaGlassGlyph.Layers,
                        stageState = stageState,
                        material = surfaceMaterial,
                        onClick = { switchA = !switchA }
                    ) {
                        SeaGlassSwitch(
                            checked = switchA,
                            onCheckedChange = { switchA = it },
                            stageState = stageState,
                            checkedMaterial = primaryMaterial,
                            uncheckedMaterial = surfaceMaterial
                        )
                    }

                    SeaGlassListItem(
                        headlineText = "Gaussian-Feathered Edge Bevel",
                        supportingText = "Continuous refraction & diffusion across corners and edges",
                        leadingGlyph = SeaGlassGlyph.Sparkles,
                        stageState = stageState,
                        material = surfaceMaterial,
                        onClick = { switchB = !switchB }
                    ) {
                        SeaGlassBadge(
                            text = if (switchB) "LIVE" else "AUTO",
                            stageState = stageState,
                            material = if (switchB) primaryMaterial else tertiaryMaterial
                        )
                        SeaGlassSwitch(
                            checked = switchB,
                            onCheckedChange = { switchB = it },
                            stageState = stageState,
                            checkedMaterial = primaryMaterial,
                            uncheckedMaterial = surfaceMaterial
                        )
                    }

                    SeaGlassBottomSheetSurface(
                        title = "Quick Optical Sheet",
                        subtitle = "Sand-frosted drag-handle sheet surface for contextual drawers",
                        stageState = stageState,
                        material = surfaceMaterial
                    ) {
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.spacedBy(8.dp)
                        ) {
                            SeaGlassButton(
                                onClick = { progressVal = 0.35f },
                                stageState = stageState,
                                material = surfaceMaterial,
                                modifier = Modifier.weight(1f)
                            ) {
                                Text("35% Load", fontWeight = FontWeight.Bold)
                            }
                            SeaGlassButton(
                                onClick = { progressVal = 0.88f },
                                stageState = stageState,
                                material = primaryMaterial,
                                modifier = Modifier.weight(1f)
                            ) {
                                Text("88% Peak", fontWeight = FontWeight.ExtraBold)
                            }
                        }
                    }
                }

                else -> {
                    SeaGlassButton(
                        onClick = onOpenFloatingSheet,
                        stageState = stageState,
                        material = primaryMaterial,
                        modifier = Modifier.fillMaxWidth()
                    ) {
                        Text(
                            text = "Present Floating Concentric Sheet ↗",
                            fontWeight = FontWeight.ExtraBold
                        )
                    }

                    SeaGlassSnackbar(
                        message = "Compile-once shader cache & dirty-slot UBO active",
                        actionLabel = if (showSnackbar) "Verified" else "Reset",
                        onActionClick = { showSnackbar = !showSnackbar },
                        stageState = stageState,
                        material = primaryMaterial
                    )

                    SeaGlassDialogCard(
                        title = "Export SeaGlass Design Tokens",
                        body = "Generates Kotlin Compose theme tokens, XML View drawables, and 256-byte std140 shader uniforms for Vulkan 1.1, GLES 3.00, and AGSL.",
                        confirmLabel = "Export Kit",
                        onConfirm = { progressVal = (progressVal + 0.15f).coerceAtMost(1f) },
                        dismissLabel = "Reset",
                        onDismiss = { progressVal = 0.50f },
                        stageState = stageState,
                        material = surfaceMaterial
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
    var searchQuery by remember { mutableStateOf("") }
    var selectedDialect by remember { mutableStateOf(ShaderDialect.VULKAN_GLSL_450) }
    val compiledBundle = remember(selectedDialect) {
        SeaGlassShaderCompiler.compileForDialect(selectedDialect)
    }

    val allCatalogItems = remember {
        listOf(
            "SeaGlassSurface" to "Foundational container with 3D press tilt & HDR caustic flare",
            "SeaGlassCard" to "Squircle card with adaptive 28dp subsurface blur floor",
            "SeaGlassButton" to "Tactile button with spatial spring compression",
            "SeaGlassIconButton" to "Circular/squircle vector icon button",
            "SeaGlassExtendedFab" to "Expandable Floating Action Button with icon & label",
            "SeaGlassPill" to "Selectable pill chip with Oklab state morphing",
            "SeaGlassFilterChip" to "M3 Expressive filter chip with animated checkmark",
            "SeaGlassBadge" to "High-chroma tertiary callout & count badge",
            "SeaGlassTopAppBar" to "Floating navigation chrome header bar",
            "SeaGlassFluidBar" to "5-tab bottom bar with velocity-stretching active lozenge",
            "SeaGlassMiniPlayerBar" to "iOS 26 style floating media capsule with meniscus lens",
            "SeaGlassFloatingToolbar" to "Split action toolbar with sminCubic coalescing FAB orb",
            "SeaGlassSearchBar" to "Interactive search capsule with clear & filter actions",
            "SeaGlassTextField" to "Focus-reactive frosted input field",
            "SeaGlassSwitch" to "Liquid water-drop thumb toggle switch",
            "SeaGlassCheckbox" to "Squircle-to-Cookie4 morphing checkbox",
            "SeaGlassRadioButton" to "Circular radio control with blooming inner droplet",
            "SeaGlassSegmentedButtonRow" to "Multi-segment selector with gliding glass thumb",
            "SeaGlassLiquidSlider" to "Interactive slider with 3D magnifying lens thumb",
            "SeaGlassLinearProgressIndicator" to "Frosted capsule track with Oklab gradient bar",
            "SeaGlassCircularProgressIndicator" to "Circular medallion progress ring",
            "SeaGlassListItem" to "Setting & list row with icon, subtitle & trailing slot",
            "SeaGlassSnackbar" to "Floating notification banner with action button",
            "SeaGlassDialogCard" to "Modal glass dialog with confirm & dismiss actions",
            "SeaGlassBottomSheetSurface" to "Sheet container with frosted drag handle pill",
            "SeaGlassFluidGroup" to "Shared std140 UBO multi-node SDF coalescing coordinator",
            "SeaGlassCardView / SeaGlassButtonView" to "Classic Android View / XML interop components"
        )
    }

    val filteredCatalog = remember(searchQuery) {
        if (searchQuery.isBlank()) {
            allCatalogItems.take(6)
        } else {
            allCatalogItems.filter { (name, desc) ->
                name.contains(searchQuery, ignoreCase = true) ||
                    desc.contains(searchQuery, ignoreCase = true)
            }
        }
    }

    SeaGlassSearchBar(
        query = searchQuery,
        onQueryChange = { searchQuery = it },
        stageState = stageState,
        placeholder = "Search ${allCatalogItems.size} SeaGlass UI elements & shaders…",
        material = surfaceMaterial,
        trailingGlyph = SeaGlassGlyph.Sparkles,
        onTrailingClick = { searchQuery = if (searchQuery.isEmpty()) "Morph" else "" }
    )

    SeaGlassCard(
        stageState = stageState,
        material = surfaceMaterial,
        modifier = Modifier.fillMaxWidth()
    ) {
        Text(
            text = "COMPONENT & VIEW PARITY INDEX (${allCatalogItems.size} INCLUDED)",
            style = MaterialTheme.typography.labelMedium.copy(
                fontWeight = FontWeight.ExtraBold,
                letterSpacing = 1.2.sp
            ),
            color = LocalContentColor.current.copy(alpha = 0.75f)
        )
        filteredCatalog.forEach { (name, desc) ->
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Column(modifier = Modifier.weight(1f)) {
                    Text(
                        text = name,
                        style = MaterialTheme.typography.bodySmall.copy(
                            fontFamily = FontFamily.Monospace,
                            fontWeight = FontWeight.ExtraBold
                        )
                    )
                    Text(
                        text = desc,
                        style = MaterialTheme.typography.labelSmall,
                        color = LocalContentColor.current.copy(alpha = 0.80f)
                    )
                }
            }
        }
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

        rotate(degrees = 12f, pivot = Offset(w * 0.5f, h * 0.48f)) {
            drawRoundRect(
                brush = Brush.horizontalGradient(
                    colors = listOf(
                        tertiaryGlow.copy(alpha = 0.86f),
                        sunlitPeach.copy(alpha = 0.84f),
                        secondaryGlow.copy(alpha = 0.88f)
                    )
                ),
                topLeft = Offset(-w * 0.25f - dx * 0.30f, h * 0.42f),
                size = Size(w * 1.50f, 104.dp.toPx()),
                cornerRadius = CornerRadius(52.dp.toPx(), 52.dp.toPx())
            )
        }

        rotate(degrees = -10f, pivot = Offset(w * 0.5f, h * 0.76f)) {
            drawRoundRect(
                brush = Brush.horizontalGradient(
                    colors = listOf(
                        sunlitPeach.copy(alpha = 0.82f),
                        primaryGlow.copy(alpha = 0.88f),
                        aquaCaustic.copy(alpha = 0.84f)
                    )
                ),
                topLeft = Offset(-w * 0.25f + dx * 0.25f, h * 0.70f),
                size = Size(w * 1.50f, 98.dp.toPx()),
                cornerRadius = CornerRadius(49.dp.toPx(), 49.dp.toPx())
            )
        }

        // Luminous Pebbles & Rings for Refraction & Frost Calibration
        drawCircle(
            color = goldenSand.copy(alpha = 0.88f),
            radius = 44.dp.toPx(),
            center = Offset(w * 0.26f - dx * 0.4f, h * 0.29f + dy)
        )
        drawCircle(
            color = Color.White.copy(alpha = 0.38f),
            radius = 56.dp.toPx(),
            center = Offset(w * 0.26f - dx * 0.4f, h * 0.29f + dy),
            style = Stroke(width = 2.5.dp.toPx())
        )

        drawCircle(
            color = sunlitPeach.copy(alpha = 0.86f),
            radius = 48.dp.toPx(),
            center = Offset(w * 0.74f + dx * 0.35f, h * 0.52f - dy)
        )
        drawCircle(
            color = Color.White.copy(alpha = 0.38f),
            radius = 62.dp.toPx(),
            center = Offset(w * 0.74f + dx * 0.35f, h * 0.52f - dy),
            style = Stroke(width = 2.5.dp.toPx())
        )

        drawCircle(
            color = aquaCaustic.copy(alpha = 0.84f),
            radius = 44.dp.toPx(),
            center = Offset(w * 0.30f + dy, h * 0.82f)
        )

        // Luminous Caustic Wave Filaments
        for (i in 0..3) {
            val yBase = h * (0.20f + i * 0.22f)
            val wavePath = Path().apply {
                moveTo(0f, yBase)
                cubicTo(
                    w * 0.33f,
                    yBase - 32.dp.toPx() * sin(phase + i * 0.9f),
                    w * 0.66f,
                    yBase + 32.dp.toPx() * cos(phase + i * 0.9f),
                    w,
                    yBase
                )
            }
            drawPath(
                path = wavePath,
                brush = Brush.horizontalGradient(
                    colors = listOf(
                        Color.White.copy(alpha = 0.14f),
                        Color.White.copy(alpha = 0.52f),
                        aquaCaustic.copy(alpha = 0.46f),
                        Color.White.copy(alpha = 0.14f)
                    )
                ),
                style = Stroke(width = 2.5.dp.toPx())
            )
        }
    }
}
