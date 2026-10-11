package dev.seaglass.core

/**
 * Physical optical specification of a SeaGlass surface.
 *
 * Models the defining characteristics of tumbled ocean sea glass fused with
 * Apple Liquid Glass edge refraction and Material 3 Expressive design:
 * 1. **Frosted Glass Transparency ([transparency])**:
 *    Controls how see-through the frosted sea-glass body is (`0.0f` = dense milky sea glass,
 *    `1.0f` = crystal-clear liquid glass). Edges remain even clearer so background light
 *    refracts cleanly through the curved rim.
 * 2. **Sanded Micro-Coarseness ([coarseness], [frostRadiusDp])**:
 *    Surface micro-facet roughness etched by sand and surf, diffusing subsurface light
 *    into a velvety matte finish.
 * 3. **Volumetric Pastel Body Absorption ([tintColorArgb], [tintDensity], [thicknessDp])**:
 *    Beer-Lambert spectral transmittance in perceptual Oklab space rather than a flat alpha fill.
 * 4. **Liquid Glass Edge Refraction ([ior], [chromaticDispersion], [rimChalkiness])**:
 *    Crystal-clear lens bending around the curved perimeter with additive refractive light.
 */
data class SeaGlassMaterial(
    val tintColorArgb: Int,
    val tintDensity: Float = 0.48f,
    val coarseness: Float = 0.38f,
    val frostRadiusDp: Float = 20f,
    val ior: Float = 1.54f,
    val chromaticDispersion: Float = 0.28f,
    val rimChalkiness: Float = 0.65f,
    val thicknessDp: Float = 16f,
    val cornerRadiusDp: Float = 28f,
    val cornerSmoothing: Float = 2.45f,
    val ambientAdaptation: Float = 0.35f,
    val transparency: Float = 0.72f
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
        require(transparency in 0f..1f) {
            "transparency must be in [0f, 1f], was $transparency"
        }
    }

    /**
     * Returns a physically compressed state of this material for touch/press interactions:
     * pressing the glass increases refractive rim light and volumetric tint saturation.
     */
    fun pressed(pressProgress: Float = 1f): SeaGlassMaterial {
        val p = pressProgress.coerceIn(0f, 1f)
        if (p == 0f) return this
        return copy(
            tintDensity = (tintDensity + 0.08f * p).coerceIn(0f, 1f),
            rimChalkiness = (rimChalkiness + 0.16f * p).coerceIn(0f, 1f),
            thicknessDp = (thicknessDp * (1f + 0.18f * p)).coerceAtMost(36f),
            cornerRadiusDp = (cornerRadiusDp * (1f - 0.10f * p)).coerceAtLeast(8f)
        )
    }

    /**
     * Returns an elevated/expanded state of this material with deeper subsurface frost
     * and wider refractive lens thickness.
     */
    fun elevated(elevationProgress: Float = 1f): SeaGlassMaterial {
        val e = elevationProgress.coerceIn(0f, 1f)
        if (e == 0f) return this
        return copy(
            frostRadiusDp = frostRadiusDp + 6f * e,
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
                    (stop.ambientAdaptation - start.ambientAdaptation) * t,
                transparency = start.transparency + (stop.transparency - start.transparency) * t
            )
        }

        fun seafoamMint(
            coarseness: Float = 0.38f,
            frostRadiusDp: Float = 20f,
            tintDensity: Float = 0.48f,
            transparency: Float = 0.72f
        ): SeaGlassMaterial = SeaGlassPalette.SeafoamMint.primaryGlass.copy(
            coarseness = coarseness,
            frostRadiusDp = frostRadiusDp,
            tintDensity = tintDensity,
            transparency = transparency
        )

        fun cobaltTide(
            coarseness: Float = 0.34f,
            frostRadiusDp: Float = 22f,
            tintDensity: Float = 0.52f,
            transparency: Float = 0.72f
        ): SeaGlassMaterial = SeaGlassPalette.CobaltTide.primaryGlass.copy(
            coarseness = coarseness,
            frostRadiusDp = frostRadiusDp,
            tintDensity = tintDensity,
            transparency = transparency
        )

        fun beachAmber(
            coarseness: Float = 0.42f,
            frostRadiusDp: Float = 20f,
            tintDensity: Float = 0.48f,
            transparency: Float = 0.72f
        ): SeaGlassMaterial = SeaGlassPalette.BeachAmber.primaryGlass.copy(
            coarseness = coarseness,
            frostRadiusDp = frostRadiusDp,
            tintDensity = tintDensity,
            transparency = transparency
        )

        fun seaLavender(
            coarseness: Float = 0.40f,
            frostRadiusDp: Float = 21f,
            tintDensity: Float = 0.48f,
            transparency: Float = 0.72f
        ): SeaGlassMaterial = SeaGlassPalette.SeaLavender.primaryGlass.copy(
            coarseness = coarseness,
            frostRadiusDp = frostRadiusDp,
            tintDensity = tintDensity,
            transparency = transparency
        )
    }
}
