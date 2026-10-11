package dev.seaglass.compose

import androidx.compose.animation.core.FiniteAnimationSpec
import androidx.compose.animation.core.SpringSpec
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.spring
import androidx.compose.material3.ColorScheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.Immutable
import androidx.compose.runtime.State
import androidx.compose.runtime.derivedStateOf
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.runtime.staticCompositionLocalOf
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.toArgb
import dev.seaglass.core.SeaGlassColorMath
import dev.seaglass.core.SeaGlassDeviceTier
import dev.seaglass.core.SeaGlassMaterial
import dev.seaglass.core.SeaGlassPalette

/**
 * Material 3 Expressive dual-spring motion scheme for SeaGlass.
 *
 * Separates **Spatial** springs (under-damped, allowing lively organic bounce for geometry,
 * corner morphing, scale squish, and SDF coalescing bridges) from **Effects** springs
 * (critically damped `dampingRatio = 1.0f` for tint, coarseness, frost radius, and rim glow
 * so optical properties never overshoot).
 */
@Immutable
data class SeaGlassMotionScheme(
    val fastSpatial: SpringSpec<Float> = spring(dampingRatio = 0.62f, stiffness = 680f),
    val defaultSpatial: SpringSpec<Float> = spring(dampingRatio = 0.66f, stiffness = 410f),
    val slowSpatial: SpringSpec<Float> = spring(dampingRatio = 0.72f, stiffness = 240f),
    val fastEffects: SpringSpec<Float> = spring(dampingRatio = 1.0f, stiffness = 1100f),
    val defaultEffects: SpringSpec<Float> = spring(dampingRatio = 1.0f, stiffness = 540f)
)

val LocalSeaGlassPalette = staticCompositionLocalOf { SeaGlassPalette.SeafoamMint }
val LocalSeaGlassMotionScheme = staticCompositionLocalOf { SeaGlassMotionScheme() }
val LocalSeaGlassTierOverride = staticCompositionLocalOf<SeaGlassDeviceTier?> { null }

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

    return remember {
        derivedStateOf {
            val animatedArgb = SeaGlassColorMath.oklabToArgb(
                SeaGlassColorMath.OklabColor(l = l, a = a, b = b, alpha = 1f)
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
            )
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
    content: @Composable () -> Unit
) {
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
        LocalSeaGlassTierOverride provides tierOverride
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
