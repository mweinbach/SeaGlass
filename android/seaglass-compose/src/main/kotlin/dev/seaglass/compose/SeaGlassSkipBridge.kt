package dev.seaglass.compose

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import dev.seaglass.core.SeaGlassCornerAdaptationConfig
import dev.seaglass.core.SeaGlassCornerAdaptationMode
import dev.seaglass.core.SeaGlassCornerMask
import dev.seaglass.core.SeaGlassDeviceHardwareProfile
import dev.seaglass.core.SeaGlassMaterial
import dev.seaglass.core.SeaGlassPalette
import dev.seaglass.core.SeaGlassShapeKind
import java.util.Locale

/**
 * First-class **Swift / Skip (`skip.tools` — Skip Fuse & Skip Lite)** interop bridge for SeaGlass.
 *
 * ### Why `SeaGlassSkipBridge` Exists
 * When building Android apps in Swift/SwiftUI via **Skip** (`#if SKIP` with `ComposeView` /
 * `ContentComposer`), Kotlin inline value classes (`Dp`, `Color`, `TextUnit`) and manually
 * threading a `SeaGlassStageState` reference across SwiftUI value-type views add friction.
 *
 * [SeaGlassSkipBridge] solves both:
 * 1. **Ambient GPU Stage (`LocalSeaGlassStageState`)**:
 *    Wrapping the root screen in [StageRoot] automatically records the background content into
 *    the GPU backdrop pyramid and provides [LocalSeaGlassStageState] to the entire composition
 *    tree—so any SwiftUI `View` bridged via `ComposeView` can render frosted, refractive,
 *    corner-adaptive SeaGlass without passing `stageState` manually.
 * 2. **Primitive-Typed Signatures (`String`, `Float`, `Int`, `Boolean`)**:
 *    Every entry point accepts primitive `Float` dimensions (in `dp`), `Int` ARGB colors, and
 *    `String` palette/shape/mode identifiers, avoiding Kotlin inline value classes across the
 *    Swift/Kotlin bridge.
 */
object SeaGlassSkipBridge {

    /**
     * Resolves a [SeaGlassPalette] by case-insensitive ID or name (`"seafoam_mint"`, `"cobalt_tide"`,
     * `"beach_amber"`, `"sea_lavender"`, `"rose_quartz_shell"`, `"kelp_forest"`, `"milk_glass_frost"`,
     * `"obsidian_tide"`).
     */
    @JvmStatic
    fun resolvePalette(paletteId: String): SeaGlassPalette {
        val clean = paletteId.trim().lowercase(Locale.US)
        return SeaGlassPalette.entries.firstOrNull {
            it.id.lowercase(Locale.US) == clean || it.displayName.lowercase(Locale.US) == clean
        } ?: SeaGlassPalette.SeafoamMint
    }

    /**
     * Resolves a [SeaGlassDeviceHardwareProfile] by ID (`"auto"`, `"pixel_9_pro"`, `"iphone_16_pro"`,
     * `"galaxy_s25_ultra"`, `"galaxy_s25_plus"`, `"fold_asymmetric"`).
     */
    @JvmStatic
    fun resolveDeviceCornerProfile(profileId: String): SeaGlassDeviceHardwareProfile {
        val clean = profileId.trim().lowercase(Locale.US)
        return SeaGlassDeviceHardwareProfile.entries.firstOrNull {
            it.id.lowercase(Locale.US) == clean || it.name.lowercase(Locale.US) == clean
        } ?: SeaGlassDeviceHardwareProfile.AUTO_DETECT
    }

    /**
     * Resolves a [SeaGlassCornerAdaptationMode] by name (`"concentric_proximity"`, `"flush_corner_embed"`,
     * `"uniform_concentric"`, `"fixed"`).
     */
    @JvmStatic
    fun resolveCornerMode(modeId: String): SeaGlassCornerAdaptationMode {
        return when (modeId.trim().lowercase(Locale.US)) {
            "flush", "flush_corner_embed", "embed" -> SeaGlassCornerAdaptationMode.FLUSH_CORNER_EMBED
            "uniform", "uniform_concentric" -> SeaGlassCornerAdaptationMode.UNIFORM_CONCENTRIC
            "fixed", "none", "off" -> SeaGlassCornerAdaptationMode.FIXED
            else -> SeaGlassCornerAdaptationMode.CONCENTRIC_PROXIMITY
        }
    }

    /**
     * Resolves a [SeaGlassCornerMask] by name (`"all"`, `"none"`, `"top"`, `"bottom"`, `"left"`,
     * `"right"`, `"top_left"`, `"top_right"`, `"bottom_left"`, `"bottom_right"`).
     */
    @JvmStatic
    fun resolveCornerMask(maskId: String): SeaGlassCornerMask {
        return when (maskId.trim().lowercase(Locale.US)) {
            "none", "off" -> SeaGlassCornerMask.NONE
            "top", "top_only" -> SeaGlassCornerMask.TOP_ONLY
            "bottom", "bottom_only" -> SeaGlassCornerMask.BOTTOM_ONLY
            "left", "left_only" -> SeaGlassCornerMask.LEFT_ONLY
            "right", "right_only" -> SeaGlassCornerMask.RIGHT_ONLY
            "top_left", "tl" -> SeaGlassCornerMask.only(topLeft = true)
            "top_right", "tr" -> SeaGlassCornerMask.only(topRight = true)
            "bottom_right", "br" -> SeaGlassCornerMask.only(bottomRight = true)
            "bottom_left", "bl" -> SeaGlassCornerMask.only(bottomLeft = true)
            else -> SeaGlassCornerMask.ALL
        }
    }

    /**
     * Resolves a [SeaGlassShapeKind] by name or label (`"circle"`, `"squircle"`, `"cookie9"`,
     * `"clover4"`, `"flower"`, `"heart"`, etc.).
     */
    @JvmStatic
    fun resolveShapeKind(shapeId: String): SeaGlassShapeKind {
        val clean = shapeId.trim().lowercase(Locale.US)
        return SeaGlassShapeKind.entries.firstOrNull {
            it.name.lowercase(Locale.US) == clean || it.shortLabel.lowercase(Locale.US) == clean
        } ?: SeaGlassShapeKind.Squircle
    }

    /**
     * Creates a custom [SeaGlassMaterial] using primitive `Int` / `Float` arguments for Skip callers.
     */
    @JvmStatic
    @JvmOverloads
    fun createMaterial(
        tintColorArgb: Int = SeaGlassPalette.SeafoamMint.primaryGlass.tintColorArgb,
        tintDensity: Float = 0.56f,
        coarseness: Float = 0.42f,
        frostRadiusDp: Float = 24f,
        ior: Float = 1.54f,
        chromaticDispersion: Float = 0.28f,
        rimChalkiness: Float = 0.78f,
        thicknessDp: Float = 16f,
        cornerRadiusDp: Float = 28f,
        transparency: Float = 0.66f
    ): SeaGlassMaterial = SeaGlassMaterial(
        tintColorArgb = tintColorArgb,
        tintDensity = tintDensity.coerceIn(0f, 1f),
        coarseness = coarseness.coerceIn(0f, 1f),
        frostRadiusDp = frostRadiusDp.coerceAtLeast(0f),
        ior = ior.coerceIn(1f, 2f),
        chromaticDispersion = chromaticDispersion.coerceIn(0f, 1f),
        rimChalkiness = rimChalkiness.coerceIn(0f, 1f),
        thicknessDp = thicknessDp.coerceAtLeast(1f),
        cornerRadiusDp = cornerRadiusDp.coerceAtLeast(0f),
        transparency = transparency.coerceIn(0f, 1f)
    )

    /**
     * Creates a [SeaGlassMaterial] along the continuous **Sand-Tumbled Coarseness Continuum**
     * (`sandWear` $\in [0, 1]$: `0f` = completely clear untreated glass, `1f` = effectively opaque
     * sand-tumbled velvet glass).
     */
    @JvmStatic
    @JvmOverloads
    fun createMaterialFromSandWear(
        tintColorArgb: Int = SeaGlassPalette.SeafoamMint.primaryGlass.tintColorArgb,
        sandWear: Float = 0.45f,
        tintDensity: Float = 0.58f,
        ior: Float = 1.54f,
        thicknessDp: Float = 16f,
        cornerRadiusDp: Float = 28f
    ): SeaGlassMaterial = SeaGlassMaterial(
        tintColorArgb = tintColorArgb,
        tintDensity = tintDensity.coerceIn(0f, 1f),
        ior = ior.coerceIn(1f, 2f),
        thicknessDp = thicknessDp.coerceAtLeast(1f),
        cornerRadiusDp = cornerRadiusDp.coerceAtLeast(0f)
    ).withSandWear(sandWear)

    /**
     * Root stage wrapper for Swift/Skip screens: initializes [SeaGlassTheme], captures
     * [backgroundContent] into the zero-copy GPU [SeaGlassStageState], and provides
     * [LocalSeaGlassStageState] to all descendants inside [foregroundContent].
     */
    @Composable
    fun StageRoot(
        paletteId: String = "seafoam_mint",
        deviceCornerProfileId: String = "auto",
        backgroundContent: @Composable () -> Unit,
        foregroundContent: @Composable () -> Unit
    ) {
        val palette = resolvePalette(paletteId)
        val cornerProfile = resolveDeviceCornerProfile(deviceCornerProfileId)

        SeaGlassTheme(
            palette = palette,
            deviceCornerProfile = cornerProfile
        ) {
            val stageState = rememberSeaGlassStageState()
            CompositionLocalProvider(LocalSeaGlassStageState provides stageState) {
                Box(modifier = Modifier.fillMaxSize()) {
                    Box(
                        modifier = Modifier
                            .fillMaxSize()
                            .seaGlassBackdrop(stageState)
                    ) {
                        backgroundContent()
                    }
                    foregroundContent()
                }
            }
        }
    }

    /**
     * Skip-friendly SeaGlass surface container with developer-configurable device & page corner adaptation.
     */
    @Composable
    fun Surface(
        modifier: Modifier = Modifier,
        role: String = "surface",
        cornerRadiusDp: Float = -1f,
        paddingHorizontalDp: Float = 18f,
        paddingVerticalDp: Float = 14f,
        cornerModeId: String = "concentric_proximity",
        cornerMaskId: String = "all",
        proximityThresholdDp: Float = SeaGlassCornerAdaptationConfig.DEFAULT_PROXIMITY_THRESHOLD_DP,
        allowAsymmetricCorners: Boolean = false,
        onClick: (() -> Unit)? = null,
        content: @Composable () -> Unit
    ) {
        val palette = LocalSeaGlassPalette.current
        val baseMat = when (role.lowercase(Locale.US)) {
            "primary" -> palette.primaryGlass
            "secondary" -> palette.secondaryGlass
            "tertiary" -> palette.tertiaryGlass
            "elevated" -> palette.elevatedGlass
            else -> palette.surfaceGlass
        }
        val mat = if (cornerRadiusDp >= 0f) {
            baseMat.copy(cornerRadiusDp = cornerRadiusDp)
        } else {
            baseMat
        }

        SeaGlassSurface(
            stageState = LocalSeaGlassStageState.current,
            material = mat,
            onClick = onClick,
            contentPadding = PaddingValues(
                horizontal = paddingHorizontalDp.coerceAtLeast(0f).dp,
                vertical = paddingVerticalDp.coerceAtLeast(0f).dp
            ),
            cornerAdaptationMode = resolveCornerMode(cornerModeId),
            cornerMask = resolveCornerMask(cornerMaskId),
            cornerProximityThresholdDp = proximityThresholdDp.coerceAtLeast(0f),
            allowAsymmetricCorners = allowAsymmetricCorners,
            modifier = modifier
        ) {
            content()
        }
    }

    /**
     * Skip-friendly Corner-Embedded Pod (`FLUSH_CORNER_EMBED`) that curves its outer bezel-facing
     * corner(s) concentrically with the device or enclosing page while keeping inner corners crisp
     * and automatically padding content away from the corner arc.
     */
    @Composable
    fun CornerPod(
        modifier: Modifier = Modifier,
        role: String = "primary",
        baseCornerRadiusDp: Float = 18f,
        paddingHorizontalDp: Float = 14f,
        paddingVerticalDp: Float = 10f,
        cornerMaskId: String = "all",
        proximityThresholdDp: Float = 48f,
        onClick: (() -> Unit)? = null,
        content: @Composable () -> Unit
    ) {
        val palette = LocalSeaGlassPalette.current
        val baseMat = when (role.lowercase(Locale.US)) {
            "tertiary" -> palette.tertiaryGlass
            "surface" -> palette.surfaceGlass
            "elevated" -> palette.elevatedGlass
            else -> palette.primaryGlass
        }.copy(cornerRadiusDp = baseCornerRadiusDp.coerceAtLeast(4f))

        SeaGlassCornerEmbeddedPod(
            stageState = LocalSeaGlassStageState.current,
            material = baseMat,
            onClick = onClick,
            contentPadding = PaddingValues(
                horizontal = paddingHorizontalDp.coerceAtLeast(0f).dp,
                vertical = paddingVerticalDp.coerceAtLeast(0f).dp
            ),
            cornerMask = resolveCornerMask(cornerMaskId),
            cornerProximityThresholdDp = proximityThresholdDp.coerceAtLeast(0f),
            modifier = modifier
        ) {
            content()
        }
    }

    /**
     * Skip-friendly 2-element Liquid Glass morph container with in-flight Gaussian content blur
     * and corner-anchored `sminCubic` teardrop neck.
     */
    @Composable
    fun TwoElementMorph(
        expanded: Boolean,
        onToggle: (() -> Unit)? = null,
        anchor: String = "top_end",
        reserveCollapsedLayoutSpace: Boolean = true,
        collapsedContent: @Composable () -> Unit,
        expandedContent: @Composable () -> Unit
    ) {
        val alignment = when (anchor.trim().lowercase(Locale.US)) {
            "top_start", "top_leading" -> Alignment.TopStart
            "top_center" -> Alignment.TopCenter
            "top_end", "top_trailing" -> Alignment.TopEnd
            "bottom_start", "bottom_leading" -> Alignment.BottomStart
            "bottom_center" -> Alignment.BottomCenter
            "bottom_end", "bottom_trailing" -> Alignment.BottomEnd
            else -> Alignment.Center
        }

        SeaGlassTwoElementMorph(
            expanded = expanded,
            onToggle = onToggle,
            stageState = LocalSeaGlassStageState.current,
            anchorAlignment = alignment,
            reserveCollapsedLayoutSpace = reserveCollapsedLayoutSpace,
            collapsedContent = { collapsedContent() },
            expandedContent = { expandedContent() }
        )
    }

    /**
     * Skip-friendly iOS-style Floating Concentric SeaGlass Detent Sheet overlay with
     * device-bezel corner matching, temporal de-aging during drag/morph, and optional
     * morph animation.
     */
    @Composable
    fun FloatingSheet(
        visible: Boolean,
        onDismissRequest: () -> Unit,
        outerInsetDp: Float = 8f,
        enableMorphAnimation: Boolean = true,
        content: @Composable () -> Unit
    ) {
        SeaGlassFloatingSheet(
            visible = visible,
            onDismissRequest = onDismissRequest,
            stageState = LocalSeaGlassStageState.current,
            outerInsetDp = outerInsetDp.coerceAtLeast(0f).dp,
            enableMorphAnimation = enableMorphAnimation
        ) {
            content()
        }
    }

    /**
     * Skip-friendly turnkey Floating Concentric SeaGlass Session Inspector Sheet with
     * 6-action strip (`Pin`, `Unread`, `Mute`, `Compact`, `Fork`, `Export`), in-place
     * morphing `Model` selector row, `Always Notify` toggle, and `Delete Session` action.
     */
    @Composable
    fun FloatingInspectorSheet(
        visible: Boolean,
        onDismissRequest: () -> Unit,
        title: String = "Windows UI Redesign for SeaGlass Elements",
        subtitle: String = "C:\\Users\\maxw6\\Projects\\SeaGlass ...\\projects\\seaglass",
        initialModel: String = "gemini-4-argon-eap",
        contextTokensText: String = "199,692 tokens",
        costText: String = "$0.96",
        enableMorphAnimation: Boolean = true
    ) {
        SeaGlassFloatingSheet(
            visible = visible,
            onDismissRequest = onDismissRequest,
            stageState = LocalSeaGlassStageState.current,
            enableMorphAnimation = enableMorphAnimation
        ) {
            SeaGlassInspectorSheetContent(
                stageState = LocalSeaGlassStageState.current,
                title = title,
                subtitle = subtitle,
                initialModel = initialModel,
                contextTokensText = contextTokensText,
                costText = costText,
                onDeleteSession = onDismissRequest
            )
        }
    }

    /**
     * Skip-friendly iOS 27 Context Menu Overlay with lifted target preview platter,
     * quick-reaction pill, and in-place submenu morphing.
     */
    @Composable
    fun ContextMenuOverlay(
        visible: Boolean,
        onDismissRequest: () -> Unit,
        previewTitle: String = "Pacific Surf & Sea Glass",
        previewSubtitle: String = "Sand-Frosted Continuum · 64% Clear · 1.54 IOR",
        previewMeta: String = "pinned · gemini-4-argon-eap · 199K tokens",
        onItemSelectedId: (String) -> Unit = {}
    ) {
        SeaGlassContextMenuOverlay(
            visible = visible,
            onDismissRequest = onDismissRequest,
            stageState = LocalSeaGlassStageState.current,
            previewTitle = previewTitle,
            previewSubtitle = previewSubtitle,
            previewMeta = previewMeta,
            onItemSelected = { onItemSelectedId(it.id) }
        )
    }

    /**
     * Skip-friendly iOS 27 Optical Magnifier Loupe Scrubber (`SeaGlassMagnifierScrubber`).
     */
    @Composable
    fun MagnifierScrubber(
        value: Float,
        onValueChange: (Float) -> Unit,
        label: String = "OPTICAL TIMELINE & LOUPE SCRUBBER"
    ) {
        SeaGlassMagnifierScrubber(
            value = value,
            onValueChange = onValueChange,
            stageState = LocalSeaGlassStageState.current,
            label = label
        )
    }

    /**
     * Skip-friendly iOS 27 3D Cylindrical Refractive Drum-Wheel Picker (`SeaGlassWheelPicker`).
     */
    @Composable
    fun WheelPicker(
        items: List<String>,
        selectedIndex: Int,
        onSelectIndex: (Int) -> Unit,
        title: String = "IOS 27 REFRACTIVE DRUM-WHEEL PICKER"
    ) {
        SeaGlassWheelPicker(
            items = items,
            selectedIndex = selectedIndex,
            onSelectIndex = onSelectIndex,
            stageState = LocalSeaGlassStageState.current,
            title = title
        )
    }

    /**
     * Skip-friendly iOS 27 Concentric Notification Rollup Stack (`SeaGlassNotificationStack`).
     */
    @Composable
    fun NotificationStack(
        expanded: Boolean,
        onExpandedChange: (Boolean) -> Unit
    ) {
        SeaGlassNotificationStack(
            expanded = expanded,
            onExpandedChange = onExpandedChange,
            stageState = LocalSeaGlassStageState.current
        )
    }

    /**
     * Skip-friendly iOS 27 Control Center Modular Platter Grid & Twin Vertical Pill Sliders.
     */
    @Composable
    fun ControlCenterGrid(
        frostFraction: Float,
        onFrostFractionChange: (Float) -> Unit,
        refractFraction: Float,
        onRefractFractionChange: (Float) -> Unit
    ) {
        SeaGlassControlCenterGrid(
            frostFraction = frostFraction,
            onFrostFractionChange = onFrostFractionChange,
            refractFraction = refractFraction,
            onRefractFractionChange = onRefractFractionChange,
            stageState = LocalSeaGlassStageState.current
        )
    }
}
