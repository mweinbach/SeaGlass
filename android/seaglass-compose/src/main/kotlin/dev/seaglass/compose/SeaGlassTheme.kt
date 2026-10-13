package dev.seaglass.compose

import androidx.compose.animation.core.Easing
import androidx.compose.animation.core.FiniteAnimationSpec
import androidx.compose.animation.core.SpringSpec
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.spring
import androidx.compose.material3.ColorScheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Typography
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.ui.graphics.Shadow
import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.Immutable
import androidx.compose.runtime.State
import androidx.compose.runtime.derivedStateOf
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.runtime.staticCompositionLocalOf
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.toArgb
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.LocalView
import dev.seaglass.core.SeaGlassColorMath
import dev.seaglass.core.SeaGlassCornerAdaptationConfig
import dev.seaglass.core.SeaGlassCornerAdaptationSolver
import dev.seaglass.core.SeaGlassCornerDetectionSource
import dev.seaglass.core.SeaGlassCornerRadiiPx
import dev.seaglass.core.SeaGlassDeviceCornerSpec
import dev.seaglass.core.SeaGlassDeviceHardwareProfile
import dev.seaglass.core.SeaGlassDeviceTier
import dev.seaglass.core.SeaGlassMaterial
import dev.seaglass.core.SeaGlassPalette
import dev.seaglass.core.SeaGlassShapeMorph
import dev.seaglass.core.backend.SeaGlassShaderProgramCache

/**
 * Physics-based C2 S-curve (ease-in / ease-out) [Easing] backed by [SeaGlassShapeMorph.physicsSCurve].
 */
val SeaGlassPhysicsSCurveEasing: Easing = Easing { fraction ->
    SeaGlassShapeMorph.physicsSCurve(fraction, overshootGain = 0f)
}

/**
 * Material 3 Expressive dual-spring motion scheme for SeaGlass.
 *
 * Separates **Spatial** springs (under-damped, allowing lively organic bounce for geometry,
 * corner morphing, scale squish, and SDF coalescing bridges) from **Effects** springs
 * (critically damped `dampingRatio = 1.0f` for tint, coarseness, frost radius, and rim glow
 * so optical properties never overshoot), and provides dedicated **Morph** springs paired
 * with [SeaGlassShapeMorph.physicsSCurve] for trigger-to-card, sheet, and sub-page morphs.
 */
@Immutable
data class SeaGlassMotionScheme(
    val fastSpatial: SpringSpec<Float> = spring(dampingRatio = 0.66f, stiffness = 540f),
    val defaultSpatial: SpringSpec<Float> = spring(dampingRatio = 0.70f, stiffness = 360f),
    val slowSpatial: SpringSpec<Float> = spring(dampingRatio = 0.74f, stiffness = 240f),
    val morphOpenSpatial: SpringSpec<Float> = spring(dampingRatio = 0.82f, stiffness = 290f),
    val morphCloseSpatial: SpringSpec<Float> = spring(dampingRatio = 0.88f, stiffness = 380f),
    val subPageSpatial: SpringSpec<Float> = spring(dampingRatio = 0.84f, stiffness = 360f),
    val fastEffects: SpringSpec<Float> = spring(dampingRatio = 1.0f, stiffness = 800f),
    val defaultEffects: SpringSpec<Float> = spring(dampingRatio = 1.0f, stiffness = 420f)
) {
    fun <T> subPageSpatialSpec(): SpringSpec<T> = spring(dampingRatio = 0.84f, stiffness = 360f)
}

/**
 * Enclosing page or card container bounds and resolved 4 corner radii in root coordinates,
 * allowing nested child SeaGlass elements to adapt their corner radii concentrically to the
 * page/card corners in addition to the physical device bezel.
 */
@Immutable
data class SeaGlassContainerCornerScope(
    val originInRoot: Offset,
    val widthPx: Float,
    val heightPx: Float,
    val cornerRadiiPx: SeaGlassCornerRadiiPx
)

val LocalSeaGlassPalette = staticCompositionLocalOf { SeaGlassPalette.SeafoamMint }
val LocalSeaGlassMotionScheme = staticCompositionLocalOf { SeaGlassMotionScheme() }
val LocalSeaGlassTierOverride = staticCompositionLocalOf<SeaGlassDeviceTier?> { null }
val LocalSeaGlassDeviceCornerProfile = staticCompositionLocalOf { SeaGlassDeviceHardwareProfile.AUTO_DETECT }
val LocalSeaGlassDeviceCornerSpec = staticCompositionLocalOf {
    SeaGlassDeviceCornerSpec(
        topLeftPx = 120f,
        topRightPx = 120f,
        bottomRightPx = 120f,
        bottomLeftPx = 120f,
        density = 2.75f,
        source = SeaGlassCornerDetectionSource.DEVICE_MODEL_CATALOG,
        deviceDescription = "Default Flagship Display"
    )
}
val LocalSeaGlassContainerCornerScope = staticCompositionLocalOf<SeaGlassContainerCornerScope?> { null }
val LocalSeaGlassCornerAdaptationConfig = staticCompositionLocalOf {
    SeaGlassCornerAdaptationConfig.Disabled
}

/**
 * Smoothly animates a [SeaGlassMaterial] between states using Material 3 Expressive dual-spring
 * discipline: perceptual Oklab interpolation for [SeaGlassMaterial.tintColorArgb], critically
 * damped effects springs for optical properties, and expressive spatial springs for geometry.
 */
@Composable
fun animateSeaGlassMaterialAsState(
    targetMaterial: SeaGlassMaterial,
    motionScheme: SeaGlassMotionScheme = LocalSeaGlassMotionScheme.current
): State<SeaGlassMaterial> {
    val labTarget = remember(targetMaterial.tintColorArgb) {
        SeaGlassColorMath.argbToOklab(targetMaterial.tintColorArgb)
    }

    val l by animateFloatAsState(labTarget.l, motionScheme.defaultEffects, label = "oklabL")
    val a by animateFloatAsState(labTarget.a, motionScheme.defaultEffects, label = "oklabA")
    val b by animateFloatAsState(labTarget.b, motionScheme.defaultEffects, label = "oklabB")

    val tintDensity by animateFloatAsState(
        targetMaterial.tintDensity,
        motionScheme.defaultEffects,
        label = "tintDensity"
    )
    val coarseness by animateFloatAsState(
        targetMaterial.coarseness,
        motionScheme.defaultEffects,
        label = "coarseness"
    )
    val frostRadiusDp by animateFloatAsState(
        targetMaterial.frostRadiusDp,
        motionScheme.defaultEffects,
        label = "frostRadiusDp"
    )
    val ior by animateFloatAsState(
        targetMaterial.ior,
        motionScheme.defaultEffects,
        label = "ior"
    )
    val dispersion by animateFloatAsState(
        targetMaterial.chromaticDispersion,
        motionScheme.defaultEffects,
        label = "chromaticDispersion"
    )
    val rimChalkiness by animateFloatAsState(
        targetMaterial.rimChalkiness,
        motionScheme.fastEffects,
        label = "rimChalkiness"
    )
    val thicknessDp by animateFloatAsState(
        targetMaterial.thicknessDp,
        motionScheme.defaultSpatial,
        label = "thicknessDp"
    )
    val cornerRadiusDp by animateFloatAsState(
        targetMaterial.cornerRadiusDp,
        motionScheme.fastSpatial,
        label = "cornerRadiusDp"
    )
    val cornerSmoothing by animateFloatAsState(
        targetMaterial.cornerSmoothing,
        motionScheme.defaultSpatial,
        label = "cornerSmoothing"
    )
    val transparency by animateFloatAsState(
        targetMaterial.transparency,
        motionScheme.defaultEffects,
        label = "transparency"
    )

    return remember(targetMaterial) {
        derivedStateOf {
            val animatedArgb = SeaGlassColorMath.oklabToArgb(
                SeaGlassColorMath.OklabColor(l = l, a = a, b = b, alpha = 1f)
            )
            // Compute in-flight malleability from active spatial/state transitions:
            // while geometry (cornerRadius, thickness) or state tint is moving toward its target,
            // the glass de-ages (loses its coarse sand texture and clarifies into malleable liquid
            // glass) and solidifies back to its sand-frosted finish as the animation settles!
            val cornerDelta = kotlin.math.abs(cornerRadiusDp - targetMaterial.cornerRadiusDp) / 7f
            val thicknessDelta = kotlin.math.abs(thicknessDp - targetMaterial.thicknessDp) / 4.5f
            val dl = l - labTarget.l
            val da = a - labTarget.a
            val db = b - labTarget.b
            val colorDelta = kotlin.math.sqrt(dl * dl + da * da + db * db) * 4.5f
            val inFlightMalleability = SeaGlassShapeMorph.sCurve(
                maxOf(cornerDelta, thicknessDelta, colorDelta)
            )

            SeaGlassMaterial(
                tintColorArgb = animatedArgb,
                tintDensity = tintDensity.coerceIn(0f, 1f),
                coarseness = coarseness.coerceIn(0f, 1f),
                frostRadiusDp = frostRadiusDp.coerceAtLeast(0f),
                ior = ior.coerceIn(1.0f, 2.0f),
                chromaticDispersion = dispersion.coerceIn(0f, 1f),
                rimChalkiness = rimChalkiness.coerceIn(0f, 1f),
                thicknessDp = thicknessDp.coerceAtLeast(1f),
                cornerRadiusDp = cornerRadiusDp.coerceAtLeast(0f),
                cornerSmoothing = cornerSmoothing.coerceIn(2.0f, 4.0f),
                ambientAdaptation = targetMaterial.ambientAdaptation,
                transparency = transparency.coerceIn(0f, 1f)
            ).deaged(inFlightMalleability)
        }
    }
}

/**
 * Root theme wrapper that bridges [SeaGlassPalette] and [SeaGlassMotionScheme] with Material 3.
 */
@Composable
fun SeaGlassTheme(
    palette: SeaGlassPalette = SeaGlassPalette.SeafoamMint,
    motionScheme: SeaGlassMotionScheme = remember { SeaGlassMotionScheme() },
    tiltState: SeaGlassTiltState = rememberSeaGlassTiltState(),
    tierOverride: SeaGlassDeviceTier? = null,
    deviceCornerProfile: SeaGlassDeviceHardwareProfile = SeaGlassDeviceHardwareProfile.AUTO_DETECT,
    cornerAdaptationConfig: SeaGlassCornerAdaptationConfig = LocalSeaGlassCornerAdaptationConfig.current,
    content: @Composable () -> Unit
) {
    val context = LocalContext.current
    val view = LocalView.current
    val rootInsets = view.rootWindowInsets
    remember(context) {
        SeaGlassShaderProgramCache.warmUpForDevice(context)
    }
    val detectedCornerSpec = remember(context, rootInsets, deviceCornerProfile) {
        SeaGlassCornerAdaptationSolver.detectDeviceCorners(
            context = context,
            rootWindowInsets = rootInsets,
            overrideProfile = deviceCornerProfile
        )
    }

    val isDark = palette == SeaGlassPalette.ObsidianTide
    val m3ColorScheme: ColorScheme = remember(palette, isDark) {
        val primary = Color(palette.primaryGlass.tintColorArgb)
        val secondary = Color(palette.secondaryGlass.tintColorArgb)
        val tertiary = Color(palette.tertiaryGlass.tintColorArgb)
        val surface = Color(palette.surfaceGlass.tintColorArgb)
        val onPrimary = Color(palette.onPrimaryGlassArgb)
        val onSurface = Color(palette.onSurfaceGlassArgb)

        if (isDark) {
            darkColorScheme(
                primary = primary,
                onPrimary = onPrimary,
                secondary = secondary,
                tertiary = tertiary,
                surface = surface,
                onSurface = onSurface
            )
        } else {
            lightColorScheme(
                primary = primary,
                onPrimary = onPrimary,
                secondary = secondary,
                tertiary = tertiary,
                surface = surface,
                onSurface = onSurface
            )
        }
    }

    CompositionLocalProvider(
        LocalSeaGlassPalette provides palette,
        LocalSeaGlassMotionScheme provides motionScheme,
        LocalSeaGlassTiltState provides tiltState,
        LocalSeaGlassTierOverride provides tierOverride,
        LocalSeaGlassDeviceCornerProfile provides deviceCornerProfile,
        LocalSeaGlassDeviceCornerSpec provides detectedCornerSpec,
        LocalSeaGlassCornerAdaptationConfig provides cornerAdaptationConfig
    ) {
        MaterialTheme(
            colorScheme = m3ColorScheme,
            content = content
        )
    }
}

/**
 * Convenience extension to convert a Compose [Color] into a harmonized SeaGlass pastel [SeaGlassMaterial].
 */
fun Color.toSeaGlassMaterial(
    coarseness: Float = 0.40f,
    frostRadiusDp: Float = 22f,
    tintDensity: Float = 0.54f
): SeaGlassMaterial {
    val harmonized = SeaGlassColorMath.harmonizeToSeaGlassPastel(this.toArgb())
    return SeaGlassMaterial(
        tintColorArgb = harmonized,
        coarseness = coarseness,
        frostRadiusDp = frostRadiusDp,
        tintDensity = tintDensity
    )
}

/**
 * Harmonized set of [SeaGlassMaterial] tokens derived dynamically from any Material 3 [ColorScheme]
 * (including Android 12+ `dynamicLightColorScheme` / `dynamicDarkColorScheme`).
 */
@Immutable
data class SeaGlassColorSchemeMaterials(
    val primaryGlass: SeaGlassMaterial,
    val secondaryGlass: SeaGlassMaterial,
    val tertiaryGlass: SeaGlassMaterial,
    val surfaceGlass: SeaGlassMaterial,
    val elevatedGlass: SeaGlassMaterial
)

/**
 * Derives a complete [SeaGlassColorSchemeMaterials] bundle from any Material 3 [ColorScheme]
 * by harmonizing its tonal accents in perceptual Oklab space.
 */
fun ColorScheme.toSeaGlassMaterials(): SeaGlassColorSchemeMaterials {
    return SeaGlassColorSchemeMaterials(
        primaryGlass = primary.toSeaGlassMaterial(coarseness = 0.38f, frostRadiusDp = 24f, tintDensity = 0.64f),
        secondaryGlass = secondary.toSeaGlassMaterial(coarseness = 0.44f, frostRadiusDp = 22f, tintDensity = 0.56f),
        tertiaryGlass = tertiary.toSeaGlassMaterial(coarseness = 0.36f, frostRadiusDp = 24f, tintDensity = 0.66f),
        surfaceGlass = surfaceVariant.toSeaGlassMaterial(coarseness = 0.48f, frostRadiusDp = 28f, tintDensity = 0.50f),
        elevatedGlass = primaryContainer.toSeaGlassMaterial(coarseness = 0.42f, frostRadiusDp = 30f, tintDensity = 0.60f)
    )
}
