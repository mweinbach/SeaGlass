package dev.seaglass.core

/**
 * Physical optical specification of a SeaGlass surface.
 *
 * Models the three defining characteristics of tumbled ocean sea glass married to
 * Material 3 Expressive design:
 * 1. **Sanded Micro-Coarseness ([coarseness], [frostRadiusDp])**:
 *    Surface micro-facet roughness etched by sand and surf, diffusing both background
 *    refraction and surface highlights into a velvety matte finish.
 * 2. **Volumetric Pastel Body Absorption ([tintColorArgb], [tintDensity], [thicknessDp])**:
 *    Beer-Lambert spectral transmittance in perceptual Oklab space rather than a flat alpha fill.
 * 3. **Expressive Tumbled Rim & Geometry ([ior], [rimChalkiness], [cornerRadiusDp], [cornerSmoothing])**:
 *    Soft edge refraction, chalky Oren-Nayar / rough-Fresnel rim glow, and superellipse squircle contours.
 */
data class SeaGlassMaterial(
    val tintColorArgb: Int,
    val tintDensity: Float = 0.54f,
    val coarseness: Float = 0.40f,
    val frostRadiusDp: Float = 22f,
    val ior: Float = 1.49f,
    val chromaticDispersion: Float = 0.24f,
    val rimChalkiness: Float = 0.68f,
    val thicknessDp: Float = 14f,
    val cornerRadiusDp: Float = 28f,
    val cornerSmoothing: Float = 2.45f,
    val ambientAdaptation: Float = 0.35f
) {
    init {
        require(tintDensity in 0f..1f) { "tintDensity must be in [0f, 1f], was $tintDensity" }
        require(coarseness in 0f..1f) { "coarseness must be in [0f, 1f], was $coarseness" }
        require(frostRadiusDp >= 0f) { "frostRadiusDp must be >= 0f, was $frostRadiusDp" }
        require(ior in 1.0f..2.0f) { "ior must be in [1.0f, 2.0f], was $ior" }
        require(chromaticDispersion in 0f..1f) {
            "chromaticDispersion must be in [0f, 1f], was $chromaticDispersion"
        }
        require(rimChalkiness in 0f..1f) { "rimChalkiness must be in [0f, 1f], was $rimChalkiness" }
        require(thicknessDp >= 1f) { "thicknessDp must be >= 1f, was $thicknessDp" }
        require(cornerRadiusDp >= 0f) { "cornerRadiusDp must be >= 0f, was $cornerRadiusDp" }
        require(cornerSmoothing in 2.0f..4.0f) {
            "cornerSmoothing must be in [2.0f, 4.0f], was $cornerSmoothing"
        }
        require(ambientAdaptation in 0f..1f) {
            "ambientAdaptation must be in [0f, 1f], was $ambientAdaptation"
        }
    }

    /**
     * Returns a physically compressed state of this material for touch/press interactions:
     * pressing the glass increases effective volumetric tint density, widens the chalky
     * tumbled rim glow, and subtly sharpens corner smoothing.
     */
    fun pressed(pressProgress: Float = 1f): SeaGlassMaterial {
        val p = pressProgress.coerceIn(0f, 1f)
        if (p == 0f) return this
        return copy(
            tintDensity = (tintDensity + 0.11f * p).coerceIn(0f, 1f),
            coarseness = (coarseness + 0.06f * p).coerceIn(0f, 1f),
            rimChalkiness = (rimChalkiness + 0.18f * p).coerceIn(0f, 1f),
            thicknessDp = (thicknessDp * (1f + 0.18f * p)).coerceAtMost(36f),
            cornerRadiusDp = (cornerRadiusDp * (1f - 0.12f * p)).coerceAtLeast(8f)
        )
    }

    /**
     * Returns an elevated/expanded state of this material with deeper subsurface frost
     * and wider tumbled bevel thickness.
     */
    fun elevated(elevationProgress: Float = 1f): SeaGlassMaterial {
        val e = elevationProgress.coerceIn(0f, 1f)
        if (e == 0f) return this
        return copy(
            frostRadiusDp = frostRadiusDp + 8f * e,
            thicknessDp = (thicknessDp + 5f * e).coerceAtMost(36f),
            rimChalkiness = (rimChalkiness + 0.10f * e).coerceIn(0f, 1f)
        )
    }

    companion object {
        /**
         * Perceptually interpolates two [SeaGlassMaterial] instances using Oklab color space
         * for [tintColorArgb] and linear interpolation for optical scalar parameters.
         */
        fun lerp(start: SeaGlassMaterial, stop: SeaGlassMaterial, fraction: Float): SeaGlassMaterial {
            val t = fraction.coerceIn(0f, 1f)
            if (t <= 0f) return start
            if (t >= 1f) return stop
            return SeaGlassMaterial(
                tintColorArgb = SeaGlassColorMath.lerpOklab(start.tintColorArgb, stop.tintColorArgb, t),
                tintDensity = start.tintDensity + (stop.tintDensity - start.tintDensity) * t,
                coarseness = start.coarseness + (stop.coarseness - start.coarseness) * t,
                frostRadiusDp = start.frostRadiusDp + (stop.frostRadiusDp - start.frostRadiusDp) * t,
                ior = start.ior + (stop.ior - start.ior) * t,
                chromaticDispersion = start.chromaticDispersion +
                    (stop.chromaticDispersion - start.chromaticDispersion) * t,
                rimChalkiness = start.rimChalkiness + (stop.rimChalkiness - start.rimChalkiness) * t,
                thicknessDp = start.thicknessDp + (stop.thicknessDp - start.thicknessDp) * t,
                cornerRadiusDp = start.cornerRadiusDp + (stop.cornerRadiusDp - start.cornerRadiusDp) * t,
                cornerSmoothing = start.cornerSmoothing + (stop.cornerSmoothing - start.cornerSmoothing) * t,
                ambientAdaptation = start.ambientAdaptation +
                    (stop.ambientAdaptation - start.ambientAdaptation) * t
            )
        }

        fun seafoamMint(
            coarseness: Float = 0.38f,
            frostRadiusDp: Float = 22f,
            tintDensity: Float = 0.54f
        ): SeaGlassMaterial = SeaGlassPalette.SeafoamMint.primaryGlass.copy(
            coarseness = coarseness,
            frostRadiusDp = frostRadiusDp,
            tintDensity = tintDensity
        )

        fun cobaltTide(
            coarseness: Float = 0.34f,
            frostRadiusDp: Float = 24f,
            tintDensity: Float = 0.58f
        ): SeaGlassMaterial = SeaGlassPalette.CobaltTide.primaryGlass.copy(
            coarseness = coarseness,
            frostRadiusDp = frostRadiusDp,
            tintDensity = tintDensity
        )

        fun beachAmber(
            coarseness: Float = 0.42f,
            frostRadiusDp: Float = 21f,
            tintDensity: Float = 0.52f
        ): SeaGlassMaterial = SeaGlassPalette.BeachAmber.primaryGlass.copy(
            coarseness = coarseness,
            frostRadiusDp = frostRadiusDp,
            tintDensity = tintDensity
        )

        fun seaLavender(
            coarseness: Float = 0.40f,
            frostRadiusDp: Float = 23f,
            tintDensity: Float = 0.54f
        ): SeaGlassMaterial = SeaGlassPalette.SeaLavender.primaryGlass.copy(
            coarseness = coarseness,
            frostRadiusDp = frostRadiusDp,
            tintDensity = tintDensity
        )
    }
}
