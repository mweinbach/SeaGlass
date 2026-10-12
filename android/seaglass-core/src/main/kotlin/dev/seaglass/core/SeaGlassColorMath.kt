package dev.seaglass.core

import kotlin.math.abs
import kotlin.math.cbrt
import kotlin.math.cos
import kotlin.math.exp
import kotlin.math.ln
import kotlin.math.max
import kotlin.math.min
import kotlin.math.pow
import kotlin.math.roundToInt
import kotlin.math.sin
import kotlin.math.sqrt

/**
 * Perceptual Oklab / Oklch color space conversions and Beer-Lambert volumetric
 * optical absorption math for SeaGlass.
 *
 * Real sea glass derives its color from transition-metal oxides dissolved inside
 * the glass volume (iron for seafoam/kelp green, cobalt for deep blue, selenium/gold
 * for rose quartz, manganese for lavender, sulfur/carbon for beach amber).
 * Unlike a flat alpha overlay, volumetric absorption transmits high-luminance
 * highlights while shifting midtones in perceptual Oklab chroma space and adding
 * an isotropic sand-frosted subsurface scatter floor.
 */
object SeaGlassColorMath {

    /**
     * Packed perceptual Oklab color with alpha.
     */
    data class OklabColor(
        val l: Float,
        val a: Float,
        val b: Float,
        val alpha: Float = 1f
    )

    /**
     * Converts an 8-bit sRGB channel [0..255] to linear light [0f..1f].
     */
    fun srgbChannelToLinear(channel: Int): Float {
        val c = (channel.coerceIn(0, 255)) / 255f
        return if (c <= 0.04045f) {
            c / 12.92f
        } else {
            ((c + 0.055f) / 1.055f).pow(2.4f)
        }
    }

    /**
     * Converts a linear light channel [0f..1f] to an 8-bit sRGB channel [0..255].
     */
    fun linearChannelToSrgb(linear: Float): Int {
        val clamped = linear.coerceIn(0f, 1f)
        val srgb = if (clamped <= 0.0031308f) {
            12.92f * clamped
        } else {
            1.055f * clamped.pow(1f / 2.4f) - 0.055f
        }
        return (srgb * 255f).roundToInt().coerceIn(0, 255)
    }

    /**
     * Converts a packed 0xAARRGGBB color integer to perceptual Oklab.
     */
    fun argbToOklab(argb: Int): OklabColor {
        val alpha = ((argb ushr 24) and 0xFF) / 255f
        val r = srgbChannelToLinear((argb ushr 16) and 0xFF)
        val g = srgbChannelToLinear((argb ushr 8) and 0xFF)
        val b = srgbChannelToLinear(argb and 0xFF)

        val l = 0.4122214708f * r + 0.5363325363f * g + 0.0514459929f * b
        val m = 0.2119034982f * r + 0.6806995451f * g + 0.1073969566f * b
        val s = 0.0883024619f * r + 0.2817188376f * g + 0.6299787005f * b

        val lCbrt = cbrt(max(0f, l))
        val mCbrt = cbrt(max(0f, m))
        val sCbrt = cbrt(max(0f, s))

        return OklabColor(
            l = 0.2104542553f * lCbrt + 0.7936177850f * mCbrt - 0.0040720468f * sCbrt,
            a = 1.9779984951f * lCbrt - 2.4285922050f * mCbrt + 0.4505937099f * sCbrt,
            b = 0.0259040371f * lCbrt + 0.7827717662f * mCbrt - 0.8086757660f * sCbrt,
            alpha = alpha
        )
    }

    /**
     * Converts a perceptual Oklab color back to a packed 0xAARRGGBB integer.
     */
    fun oklabToArgb(color: OklabColor): Int {
        val lCbrt = color.l + 0.3963377774f * color.a + 0.2158037573f * color.b
        val mCbrt = color.l - 0.1055613458f * color.a - 0.0638541728f * color.b
        val sCbrt = color.l - 0.0894841775f * color.a - 1.2914855480f * color.b

        val l = lCbrt * lCbrt * lCbrt
        val m = mCbrt * mCbrt * mCbrt
        val s = sCbrt * sCbrt * sCbrt

        val rLinear = +4.0767416621f * l - 3.3077115913f * m + 0.2309699292f * s
        val gLinear = -1.2684380046f * l + 2.6097574011f * m - 0.3413193965f * s
        val bLinear = -0.0041960863f * l - 0.7034186147f * m + 1.7076147010f * s

        val aInt = (color.alpha.coerceIn(0f, 1f) * 255f).roundToInt().coerceIn(0, 255)
        val rInt = linearChannelToSrgb(rLinear)
        val gInt = linearChannelToSrgb(gLinear)
        val bInt = linearChannelToSrgb(bLinear)

        return (aInt shl 24) or (rInt shl 16) or (gInt shl 8) or bInt
    }

    /**
     * Perceptually uniform interpolation between two ARGB colors in Oklab space.
     * Prevents the muddy desaturation trough caused by naive sRGB linear interpolation.
     */
    fun lerpOklab(startArgb: Int, stopArgb: Int, fraction: Float): Int {
        val t = fraction.coerceIn(0f, 1f)
        if (t <= 0f) return startArgb
        if (t >= 1f) return stopArgb

        val c0 = argbToOklab(startArgb)
        val c1 = argbToOklab(stopArgb)
        return oklabToArgb(
            OklabColor(
                l = c0.l + (c1.l - c0.l) * t,
                a = c0.a + (c1.a - c0.a) * t,
                b = c0.b + (c1.b - c0.b) * t,
                alpha = c0.alpha + (c1.alpha - c0.alpha) * t
            )
        )
    }

    /**
     * Evaluates the Beer-Lambert volumetric sea-glass transmission of a backdrop color
     * through a tumbled sea-glass slab.
     *
     * @param backdropArgb Packed ARGB color behind the sea glass.
     * @param tintArgb Packed ARGB intrinsic tint of the sea glass.
     * @param tintDensity Volumetric absorption coefficient in [0f..1f].
     * @param coarseness Sand-tumbled surface roughness in [0f..1f] (adds isotropic milky frost).
     * @param normalizedThickness Local slab thickness factor in [0f..2f] (1f at interior, tapering at bevel).
     * @return Packed ARGB transmitted color before rim specular lighting.
     */
    fun computeBeerLambertTransmission(
        backdropArgb: Int,
        tintArgb: Int,
        tintDensity: Float,
        coarseness: Float,
        normalizedThickness: Float = 1.0f
    ): Int {
        val density = tintDensity.coerceIn(0f, 1f)
        val grit = coarseness.coerceIn(0f, 1f)
        val thickness = normalizedThickness.coerceIn(0.05f, 2.5f)
        val opticalDepth = density * (0.65f + 0.35f * thickness)

        val bgR = max(0.001f, srgbChannelToLinear((backdropArgb ushr 16) and 0xFF))
        val bgG = max(0.001f, srgbChannelToLinear((backdropArgb ushr 8) and 0xFF))
        val bgB = max(0.001f, srgbChannelToLinear(backdropArgb and 0xFF))

        val tintR = max(0.04f, srgbChannelToLinear((tintArgb ushr 16) and 0xFF))
        val tintG = max(0.04f, srgbChannelToLinear((tintArgb ushr 8) and 0xFF))
        val tintB = max(0.04f, srgbChannelToLinear(tintArgb and 0xFF))

        // Beer-Lambert spectral absorption: sigma = -ln(tintLinear), T = exp(-sigma * opticalDepth)
        val absorbR = exp(ln(tintR) * opticalDepth * 1.35f)
        val absorbG = exp(ln(tintG) * opticalDepth * 1.35f)
        val absorbB = exp(ln(tintB) * opticalDepth * 1.35f)

        val transmittedR = bgR * absorbR
        val transmittedG = bgG * absorbG
        val transmittedB = bgB * absorbB

        // Convert to Oklab to apply Material 3 Expressive pastel body scatter and velvet diffusion
        val transmittedArgb = (0xFF shl 24) or
            (linearChannelToSrgb(transmittedR) shl 16) or
            (linearChannelToSrgb(transmittedG) shl 8) or
            linearChannelToSrgb(transmittedB)

        val labTransmitted = argbToOklab(transmittedArgb)
        val labTint = argbToOklab(tintArgb)

        // Sand-tumbled sea glass develops a velvet subsurface scatter in the glass's own Oklab tint
        // (from 0 at untreated clear glass up to dense velvet glass at grit = 1f) without bleaching
        // the center toward white as depth increases.
        val velvetScatterWeight = (density * 0.34f + grit * 0.56f).coerceIn(0f, 0.90f)
        val lightnessBlend = (velvetScatterWeight * 0.62f).coerceIn(0f, 0.78f)

        val outL = (labTransmitted.l * (1f - lightnessBlend) + labTint.l * lightnessBlend)
            .coerceIn(0.08f, 0.95f)
        val outA = labTransmitted.a * (1f - velvetScatterWeight) + labTint.a * velvetScatterWeight
        val outB = labTransmitted.b * (1f - velvetScatterWeight) + labTint.b * velvetScatterWeight

        return oklabToArgb(OklabColor(l = outL, a = outA, b = outB, alpha = 1f))
    }

    /**
     * Harmonizes an arbitrary Material 3 accent color into the SeaGlass pastel gamut
     * by lifting Oklab lightness into the translucent sea-glass band [0.68..0.88]
     * while preserving hue angle and expressive chroma.
     */
    fun harmonizeToSeaGlassPastel(sourceArgb: Int, targetLightness: Float = 0.79f): Int {
        val lab = argbToOklab(sourceArgb)
        val chroma = sqrt(lab.a * lab.a + lab.b * lab.b)
        val hue = kotlin.math.atan2(lab.b, lab.a)
        val pastelChroma = chroma.coerceIn(0.035f, 0.145f)
        return oklabToArgb(
            OklabColor(
                l = targetLightness.coerceIn(0.22f, 0.92f),
                a = pastelChroma * cos(hue),
                b = pastelChroma * sin(hue),
                alpha = 1f
            )
        )
    }

    /**
     * Computes a contrast-safe, hue-harmonized deep tonal ink color (for light/pastel sea glass)
     * or luminous frosted chalk-alabaster color (for dark smoked sea glass) for icons and typography.
     *
     * Models the rich tonal ink of ocean-tumbled sea-glass iconography (e.g. deep terracotta-maroon
     * on coral-peach glass, deep royal plum on orchid-lavender glass, deep indigo-navy on periwinkle
     * blue glass, and deep emerald-teal on seafoam mint glass) rather than desaturated charcoal black.
     */
    fun optimalContentColorFor(tintArgb: Int, tintDensity: Float = 0.55f): Int {
        val lab = argbToOklab(tintArgb)
        val chroma = sqrt(lab.a * lab.a + lab.b * lab.b)
        val hue = kotlin.math.atan2(lab.b, lab.a)
        return if (lab.l >= 0.48f || tintDensity < 0.35f) {
            val tonalChroma = if (chroma < 0.025f) {
                0.046f
            } else {
                (chroma * 1.08f).coerceIn(0.084f, 0.142f)
            }
            oklabToArgb(
                OklabColor(
                    l = 0.255f,
                    a = tonalChroma * cos(hue),
                    b = tonalChroma * sin(hue),
                    alpha = 1f
                )
            )
        } else {
            val subtleChroma = min(0.048f, chroma * 0.45f)
            oklabToArgb(
                OklabColor(
                    l = 0.96f,
                    a = subtleChroma * 0.5f * cos(hue),
                    b = subtleChroma * sin(hue),
                    alpha = 1f
                )
            )
        }
    }

    /**
     * Computes the luminous frosted catch-light color that gleams along the bottom-right lip
     * of text and iconography etched (intaglio-carved) into a sea-glass surface.
     */
    fun computeEtchedCatchLightColor(tintArgb: Int, coarseness: Float = 0.45f): Int {
        val lab = argbToOklab(tintArgb)
        return if (lab.l >= 0.48f) {
            lerpOklab(tintArgb, 0xFFFFFFFF.toInt(), (0.86f + 0.08f * coarseness).coerceIn(0.82f, 0.96f))
        } else {
            lerpOklab(tintArgb, 0xFF040C16.toInt(), 0.78f)
        }
    }

    /**
     * Computes the richer, deeper Beer-Lambert volumetric core color for the interior of a tumbled
     * sea-glass piece where physical slab thickness is greatest (never bleaching the center to white).
     */
    fun computeDeepVolumetricCoreColor(tintArgb: Int, tintDensity: Float = 0.65f): Int {
        val lab = argbToOklab(tintArgb)
        val chroma = sqrt(lab.a * lab.a + lab.b * lab.b)
        val hue = kotlin.math.atan2(lab.b, lab.a)
        val density = tintDensity.coerceIn(0f, 1f)
        val coreL = if (lab.l >= 0.48f) {
            (lab.l - 0.045f * density).coerceIn(0.42f, 0.88f)
        } else {
            (lab.l - 0.035f * density).coerceIn(0.12f, 0.46f)
        }
        val coreChroma = (chroma * (1.08f + 0.18f * density)).coerceIn(0.02f, 0.20f)
        return oklabToArgb(
            OklabColor(
                l = coreL,
                a = coreChroma * cos(hue),
                b = coreChroma * sin(hue),
                alpha = 1f
            )
        )
    }

    /**
     * Computes the milky, chalk-frosted perimeter rind color produced by ocean sand and pebble
     * abrasion along the curved edges of tumbled sea glass.
     */
    fun computeFrostedRindColor(
        tintArgb: Int,
        coarseness: Float = 0.45f,
        rindWhiteness: Float = 0.64f
    ): Int {
        val grit = coarseness.coerceIn(0f, 1f)
        val blend = (rindWhiteness + 0.16f * grit).coerceIn(0.25f, 0.92f)
        return lerpOklab(tintArgb, 0xFFFAFCFF.toInt(), blend)
    }

    /**
     * Computes the luminous internal subsurface caustic color that pools along the lower bevel
     * and curved bottom edge of a thick, convex piece of tumbled sea glass.
     *
     * Models how overhead light refracting through a thick frosted glass slab concentrates into a
     * high-luminance, slightly hue-shifted caustic glow at the bottom bevel:
     * - Periwinkle / cobalt / sky-blue glass shifts toward luminous icy aqua-cyan (`0xFFB8FAFF`).
     * - Coral / rose / amber / peach glass shifts toward warm sunlit peach-ivory (`0xFFFFF0DC`).
     * - Lavender / orchid glass shifts toward opalescent pink-alabaster (`0xFFFEEBFF`).
     * - Seafoam / mint / kelp glass shifts toward luminous aqua-mint pearl (`0xFFD6FFF6`).
     */
    fun computeSubsurfaceCausticColor(
        tintArgb: Int,
        coarseness: Float = 0.45f
    ): Int {
        val lab = argbToOklab(tintArgb)
        val grit = coarseness.coerceIn(0f, 1f)
        // Determine warm vs cool vs orchid hue family in Oklab (a = green..red, b = blue..yellow)
        val causticTargetArgb = when {
            // Blue / Periwinkle / Cobalt (b < -0.02f): shift toward luminous icy aqua-cyan
            lab.b < -0.02f && lab.a < 0.06f -> 0xFFBCFAFF.toInt()
            // Lavender / Orchid (b < 0f and a >= 0.06f): shift toward opalescent pink-lilac
            lab.b < 0.0f && lab.a >= 0.06f -> 0xFFFEE8FF.toInt()
            // Coral / Rose / Amber / Peach (a > 0.02f or b > 0.03f): shift toward warm sunlit peach-cream
            lab.a > 0.02f || lab.b > 0.03f -> 0xFFFFF1DF.toInt()
            // Seafoam / Mint / Kelp: shift toward luminous icy aqua-mint
            else -> 0xFFDCFFF6.toInt()
        }
        val blend = if (lab.l < 0.48f) {
            (0.34f + 0.14f * grit).coerceIn(0.25f, 0.55f)
        } else {
            (0.46f + 0.18f * grit).coerceIn(0.35f, 0.72f)
        }
        return lerpOklab(tintArgb, causticTargetArgb, blend)
    }

    /**
     * Computes the deeper, chroma-enriched tonal mineral pit color for sand-abraded dimples
     * on the surface of tumbled sea glass (never grey or black mud).
     */
    fun computeTonalPitColor(tintArgb: Int, tintDensity: Float = 0.65f): Int {
        val lab = argbToOklab(tintArgb)
        val chroma = sqrt(lab.a * lab.a + lab.b * lab.b)
        val hue = kotlin.math.atan2(lab.b, lab.a)
        val density = tintDensity.coerceIn(0f, 1f)
        val pitL = (lab.l - (0.11f + 0.06f * density)).coerceIn(0.18f, 0.78f)
        val pitChroma = (chroma * (1.28f + 0.22f * density)).coerceIn(0.04f, 0.22f)
        return oklabToArgb(
            OklabColor(
                l = pitL,
                a = pitChroma * cos(hue),
                b = pitChroma * sin(hue),
                alpha = 1f
            )
        )
    }

    /**
     * Extracts linear sRGB float components [r, g, b] in [0f..1f] for GPU shader uniforms.
     */
    fun argbToLinearRgbFloats(argb: Int, outArray: FloatArray, offset: Int = 0) {
        outArray[offset] = srgbChannelToLinear((argb ushr 16) and 0xFF)
        outArray[offset + 1] = srgbChannelToLinear((argb ushr 8) and 0xFF)
        outArray[offset + 2] = srgbChannelToLinear(argb and 0xFF)
    }
}
