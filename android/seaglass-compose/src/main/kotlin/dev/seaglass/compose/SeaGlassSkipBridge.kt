package dev.seaglass.compose

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import dev.seaglass.core.SeaGlassCornerAdaptationMode
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
            it.id.lowercase(Locale.US) == clean || it.name.lowercase(Locale.US) == clean
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
            "fixed", "none" -> SeaGlassCornerAdaptationMode.FIXED
            else -> SeaGlassCornerAdaptationMode.CONCENTRIC_PROXIMITY
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
     * Skip-friendly SeaGlass surface container with automatic device & page corner adaptation.
     */
    @Composable
    fun Surface(
        modifier: Modifier = Modifier,
        role: String = "surface",
        cornerRadiusDp: Float = -1f,
        paddingHorizontalDp: Float = 18f,
        paddingVerticalDp: Float = 14f,
        cornerModeId: String = "concentric_proximity",
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
}
