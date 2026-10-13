package dev.seaglass.core

/**
 * Curated Sea Glass × Material 3 Expressive color & optical palettes.
 *
 * Each palette pairs authentic ocean-tumbled glass chemistry (cobalt, seafoam iron-soda,
 * sun-purpled manganese lavender, beach-sand amber, rose quartz, kelp jade, milk glass,
 * and volcanic obsidian) with saturated Material 3 Expressive pastel chroma and contrast-safe
 * foreground tokens.
 */
data class SeaGlassPalette(
    val id: String,
    val displayName: String,
    val tagline: String,
    val primaryGlass: SeaGlassMaterial,
    val secondaryGlass: SeaGlassMaterial,
    val tertiaryGlass: SeaGlassMaterial,
    val surfaceGlass: SeaGlassMaterial,
    val elevatedGlass: SeaGlassMaterial,
    val accentRimArgb: Int,
    val onPrimaryGlassArgb: Int,
    val onSurfaceGlassArgb: Int
) {
    /** Backwards-compatible name property matching the former enum constant name/displayName. */
    val name: String get() = displayName

    /** Backwards-compatible ordinal index in [entries]. */
    val ordinal: Int get() = entries.indexOfFirst { it.id == id }.coerceAtLeast(0)

    companion object {
    val SeafoamMint = SeaGlassPalette(
        id = "seafoam_mint",
        displayName = "Seafoam Mint",
        tagline = "Surf-tumbled turquoise soda-lime glass with chalky tidal frost",
        primaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFF2EE6B8.toInt(),
            tintDensity = 0.68f,
            coarseness = 0.42f,
            frostRadiusDp = 22f,
            ior = 1.54f,
            chromaticDispersion = 0.28f,
            rimChalkiness = 0.78f,
            thicknessDp = 16f,
            cornerRadiusDp = 28f,
            cornerSmoothing = 2.5f,
            transparency = 0.64f
        ),
        secondaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFF5CF0CB.toInt(),
            tintDensity = 0.58f,
            coarseness = 0.46f,
            frostRadiusDp = 20f,
            ior = 1.50f,
            chromaticDispersion = 0.22f,
            rimChalkiness = 0.72f,
            thicknessDp = 13f,
            cornerRadiusDp = 24f,
            cornerSmoothing = 2.4f,
            transparency = 0.66f
        ),
        tertiaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFF429BFF.toInt(),
            tintDensity = 0.72f,
            coarseness = 0.36f,
            frostRadiusDp = 22f,
            ior = 1.58f,
            chromaticDispersion = 0.36f,
            rimChalkiness = 0.82f,
            thicknessDp = 16f,
            cornerRadiusDp = 26f,
            cornerSmoothing = 2.5f,
            transparency = 0.68f
        ),
        surfaceGlass = SeaGlassMaterial(
            tintColorArgb = 0xFF7AF2D5.toInt(),
            tintDensity = 0.52f,
            coarseness = 0.48f,
            frostRadiusDp = 26f,
            ior = 1.50f,
            chromaticDispersion = 0.20f,
            rimChalkiness = 0.78f,
            thicknessDp = 14f,
            cornerRadiusDp = 32f,
            cornerSmoothing = 2.6f,
            transparency = 0.68f
        ),
        elevatedGlass = SeaGlassMaterial(
            tintColorArgb = 0xFF4CECC3.toInt(),
            tintDensity = 0.64f,
            coarseness = 0.44f,
            frostRadiusDp = 30f,
            ior = 1.54f,
            chromaticDispersion = 0.26f,
            rimChalkiness = 0.84f,
            thicknessDp = 18f,
            cornerRadiusDp = 32f,
            cornerSmoothing = 2.6f,
            transparency = 0.62f
        ),
        accentRimArgb = 0xFFE8FFF9.toInt(),
        onPrimaryGlassArgb = 0xFF06261E.toInt(),
        onSurfaceGlassArgb = 0xFF092E25.toInt()
    )

    val CobaltTide = SeaGlassPalette(
        id = "cobalt_tide",
        displayName = "Cobalt Tide",
        tagline = "Vintage apothecary ocean bottle blue softened to expressive periwinkle",
        primaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFF4A82FF.toInt(),
            tintDensity = 0.70f,
            coarseness = 0.44f,
            frostRadiusDp = 24f,
            ior = 1.56f,
            chromaticDispersion = 0.32f,
            rimChalkiness = 0.82f,
            thicknessDp = 16f,
            cornerRadiusDp = 28f,
            cornerSmoothing = 2.5f,
            transparency = 0.62f
        ),
        secondaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFF76A4FF.toInt(),
            tintDensity = 0.58f,
            coarseness = 0.46f,
            frostRadiusDp = 21f,
            ior = 1.51f,
            chromaticDispersion = 0.24f,
            rimChalkiness = 0.78f,
            thicknessDp = 13f,
            cornerRadiusDp = 24f,
            cornerSmoothing = 2.4f,
            transparency = 0.64f
        ),
        tertiaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFF9E6BFF.toInt(),
            tintDensity = 0.72f,
            coarseness = 0.42f,
            frostRadiusDp = 24f,
            ior = 1.58f,
            chromaticDispersion = 0.34f,
            rimChalkiness = 0.84f,
            thicknessDp = 16f,
            cornerRadiusDp = 26f,
            cornerSmoothing = 2.5f,
            transparency = 0.64f
        ),
        surfaceGlass = SeaGlassMaterial(
            tintColorArgb = 0xFF94B8FF.toInt(),
            tintDensity = 0.56f,
            coarseness = 0.48f,
            frostRadiusDp = 28f,
            ior = 1.50f,
            chromaticDispersion = 0.20f,
            rimChalkiness = 0.84f,
            thicknessDp = 14f,
            cornerRadiusDp = 32f,
            cornerSmoothing = 2.6f,
            transparency = 0.64f
        ),
        elevatedGlass = SeaGlassMaterial(
            tintColorArgb = 0xFF6498FF.toInt(),
            tintDensity = 0.66f,
            coarseness = 0.44f,
            frostRadiusDp = 32f,
            ior = 1.54f,
            chromaticDispersion = 0.26f,
            rimChalkiness = 0.86f,
            thicknessDp = 18f,
            cornerRadiusDp = 32f,
            cornerSmoothing = 2.6f,
            transparency = 0.60f
        ),
        accentRimArgb = 0xFFEEF5FF.toInt(),
        onPrimaryGlassArgb = 0xFF1A3876.toInt(),
        onSurfaceGlassArgb = 0xFF1D3E80.toInt()
    )

    val BeachAmber = SeaGlassPalette(
        id = "beach_amber",
        displayName = "Beach Amber",
        tagline = "Sun-warmed coral peach and driftwood amber glass with fine sand grit",
        primaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFFFA038.toInt(),
            tintDensity = 0.68f,
            coarseness = 0.46f,
            frostRadiusDp = 22f,
            ior = 1.54f,
            chromaticDispersion = 0.26f,
            rimChalkiness = 0.82f,
            thicknessDp = 16f,
            cornerRadiusDp = 28f,
            cornerSmoothing = 2.5f,
            transparency = 0.62f
        ),
        secondaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFFFBC68.toInt(),
            tintDensity = 0.58f,
            coarseness = 0.48f,
            frostRadiusDp = 20f,
            ior = 1.50f,
            chromaticDispersion = 0.20f,
            rimChalkiness = 0.78f,
            thicknessDp = 13f,
            cornerRadiusDp = 24f,
            cornerSmoothing = 2.4f,
            transparency = 0.64f
        ),
        tertiaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFF79C8E.toInt(),
            tintDensity = 0.66f,
            coarseness = 0.46f,
            frostRadiusDp = 22f,
            ior = 1.54f,
            chromaticDispersion = 0.28f,
            rimChalkiness = 0.82f,
            thicknessDp = 16f,
            cornerRadiusDp = 26f,
            cornerSmoothing = 2.5f,
            transparency = 0.62f
        ),
        surfaceGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFFFC982.toInt(),
            tintDensity = 0.54f,
            coarseness = 0.50f,
            frostRadiusDp = 26f,
            ior = 1.49f,
            chromaticDispersion = 0.18f,
            rimChalkiness = 0.84f,
            thicknessDp = 14f,
            cornerRadiusDp = 32f,
            cornerSmoothing = 2.6f,
            transparency = 0.64f
        ),
        elevatedGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFFFB254.toInt(),
            tintDensity = 0.64f,
            coarseness = 0.46f,
            frostRadiusDp = 30f,
            ior = 1.52f,
            chromaticDispersion = 0.22f,
            rimChalkiness = 0.86f,
            thicknessDp = 18f,
            cornerRadiusDp = 32f,
            cornerSmoothing = 2.6f,
            transparency = 0.60f
        ),
        accentRimArgb = 0xFFFFF8F0.toInt(),
        onPrimaryGlassArgb = 0xFF6C2418.toInt(),
        onSurfaceGlassArgb = 0xFF722A1C.toInt()
    )

    val SeaLavender = SeaGlassPalette(
        id = "sea_lavender",
        displayName = "Sea Lavender",
        tagline = "Sun-purpled manganese orchid-amethyst glass with powdery matte frost",
        primaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFD98EFF.toInt(),
            tintDensity = 0.68f,
            coarseness = 0.46f,
            frostRadiusDp = 23f,
            ior = 1.54f,
            chromaticDispersion = 0.28f,
            rimChalkiness = 0.84f,
            thicknessDp = 16f,
            cornerRadiusDp = 28f,
            cornerSmoothing = 2.5f,
            transparency = 0.62f
        ),
        secondaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFE6ADFF.toInt(),
            tintDensity = 0.58f,
            coarseness = 0.48f,
            frostRadiusDp = 20f,
            ior = 1.50f,
            chromaticDispersion = 0.22f,
            rimChalkiness = 0.80f,
            thicknessDp = 13f,
            cornerRadiusDp = 24f,
            cornerSmoothing = 2.4f,
            transparency = 0.64f
        ),
        tertiaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFF9EB8FF.toInt(),
            tintDensity = 0.66f,
            coarseness = 0.44f,
            frostRadiusDp = 23f,
            ior = 1.54f,
            chromaticDispersion = 0.28f,
            rimChalkiness = 0.82f,
            thicknessDp = 16f,
            cornerRadiusDp = 26f,
            cornerSmoothing = 2.5f,
            transparency = 0.62f
        ),
        surfaceGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFECB8FF.toInt(),
            tintDensity = 0.56f,
            coarseness = 0.50f,
            frostRadiusDp = 27f,
            ior = 1.49f,
            chromaticDispersion = 0.18f,
            rimChalkiness = 0.86f,
            thicknessDp = 14f,
            cornerRadiusDp = 32f,
            cornerSmoothing = 2.6f,
            transparency = 0.64f
        ),
        elevatedGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFE29EFF.toInt(),
            tintDensity = 0.64f,
            coarseness = 0.46f,
            frostRadiusDp = 31f,
            ior = 1.52f,
            chromaticDispersion = 0.24f,
            rimChalkiness = 0.88f,
            thicknessDp = 18f,
            cornerRadiusDp = 32f,
            cornerSmoothing = 2.6f,
            transparency = 0.60f
        ),
        accentRimArgb = 0xFFFDF8FF.toInt(),
        onPrimaryGlassArgb = 0xFF561A74.toInt(),
        onSurfaceGlassArgb = 0xFF5C1E7A.toInt()
    )

    val RoseQuartzShell = SeaGlassPalette(
        id = "rose_quartz_shell",
        displayName = "Rose Quartz Shell",
        tagline = "Rare coral-pink and cranberry tumbled glass with pearlescent bevel",
        primaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFFF82AA.toInt(),
            tintDensity = 0.68f,
            coarseness = 0.44f,
            frostRadiusDp = 22f,
            ior = 1.54f,
            chromaticDispersion = 0.28f,
            rimChalkiness = 0.82f,
            thicknessDp = 16f,
            cornerRadiusDp = 28f,
            cornerSmoothing = 2.5f,
            transparency = 0.62f
        ),
        secondaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFFFA2C0.toInt(),
            tintDensity = 0.58f,
            coarseness = 0.46f,
            frostRadiusDp = 20f,
            ior = 1.50f,
            chromaticDispersion = 0.22f,
            rimChalkiness = 0.78f,
            thicknessDp = 13f,
            cornerRadiusDp = 24f,
            cornerSmoothing = 2.4f,
            transparency = 0.64f
        ),
        tertiaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFF8A698.toInt(),
            tintDensity = 0.64f,
            coarseness = 0.44f,
            frostRadiusDp = 22f,
            ior = 1.52f,
            chromaticDispersion = 0.24f,
            rimChalkiness = 0.82f,
            thicknessDp = 16f,
            cornerRadiusDp = 26f,
            cornerSmoothing = 2.5f,
            transparency = 0.62f
        ),
        surfaceGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFFFB4CE.toInt(),
            tintDensity = 0.54f,
            coarseness = 0.48f,
            frostRadiusDp = 26f,
            ior = 1.49f,
            chromaticDispersion = 0.18f,
            rimChalkiness = 0.84f,
            thicknessDp = 14f,
            cornerRadiusDp = 32f,
            cornerSmoothing = 2.6f,
            transparency = 0.64f
        ),
        elevatedGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFFF92B6.toInt(),
            tintDensity = 0.64f,
            coarseness = 0.44f,
            frostRadiusDp = 30f,
            ior = 1.52f,
            chromaticDispersion = 0.24f,
            rimChalkiness = 0.86f,
            thicknessDp = 18f,
            cornerRadiusDp = 32f,
            cornerSmoothing = 2.6f,
            transparency = 0.60f
        ),
        accentRimArgb = 0xFFFFF5F8.toInt(),
        onPrimaryGlassArgb = 0xFF72162E.toInt(),
        onSurfaceGlassArgb = 0xFF781B34.toInt()
    )

    val KelpForest = SeaGlassPalette(
        id = "kelp_forest",
        displayName = "Kelp Forest",
        tagline = "Deep botanical forest-emerald sea glass with heavy surf grit",
        primaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFF2EC47A.toInt(),
            tintDensity = 0.70f,
            coarseness = 0.46f,
            frostRadiusDp = 23f,
            ior = 1.54f,
            chromaticDispersion = 0.26f,
            rimChalkiness = 0.78f,
            thicknessDp = 16f,
            cornerRadiusDp = 28f,
            cornerSmoothing = 2.5f,
            transparency = 0.64f
        ),
        secondaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFF62DC9D.toInt(),
            tintDensity = 0.58f,
            coarseness = 0.50f,
            frostRadiusDp = 20f,
            ior = 1.50f,
            chromaticDispersion = 0.20f,
            rimChalkiness = 0.74f,
            thicknessDp = 13f,
            cornerRadiusDp = 24f,
            cornerSmoothing = 2.4f,
            transparency = 0.66f
        ),
        tertiaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFF9CE452.toInt(),
            tintDensity = 0.64f,
            coarseness = 0.42f,
            frostRadiusDp = 22f,
            ior = 1.52f,
            chromaticDispersion = 0.24f,
            rimChalkiness = 0.76f,
            thicknessDp = 16f,
            cornerRadiusDp = 26f,
            cornerSmoothing = 2.5f,
            transparency = 0.64f
        ),
        surfaceGlass = SeaGlassMaterial(
            tintColorArgb = 0xFF7CE6B0.toInt(),
            tintDensity = 0.54f,
            coarseness = 0.52f,
            frostRadiusDp = 27f,
            ior = 1.49f,
            chromaticDispersion = 0.18f,
            rimChalkiness = 0.80f,
            thicknessDp = 14f,
            cornerRadiusDp = 32f,
            cornerSmoothing = 2.6f,
            transparency = 0.68f
        ),
        elevatedGlass = SeaGlassMaterial(
            tintColorArgb = 0xFF4CD690.toInt(),
            tintDensity = 0.66f,
            coarseness = 0.46f,
            frostRadiusDp = 31f,
            ior = 1.52f,
            chromaticDispersion = 0.22f,
            rimChalkiness = 0.82f,
            thicknessDp = 18f,
            cornerRadiusDp = 32f,
            cornerSmoothing = 2.6f,
            transparency = 0.62f
        ),
        accentRimArgb = 0xFFF0FFF6.toInt(),
        onPrimaryGlassArgb = 0xFF104426.toInt(),
        onSurfaceGlassArgb = 0xFF144B2B.toInt()
    )

    val MilkGlassFrost = SeaGlassPalette(
        id = "milk_glass_frost",
        displayName = "Milk Glass Frost",
        tagline = "High-scatter alabaster-opal milk glass with dense sand-frosted diffusion",
        primaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFB8E2FA.toInt(),
            tintDensity = 0.66f,
            coarseness = 0.58f,
            frostRadiusDp = 28f,
            ior = 1.50f,
            chromaticDispersion = 0.22f,
            rimChalkiness = 0.88f,
            thicknessDp = 16f,
            cornerRadiusDp = 28f,
            cornerSmoothing = 2.5f,
            transparency = 0.58f
        ),
        secondaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFCCEAFD.toInt(),
            tintDensity = 0.58f,
            coarseness = 0.54f,
            frostRadiusDp = 24f,
            ior = 1.48f,
            chromaticDispersion = 0.18f,
            rimChalkiness = 0.84f,
            thicknessDp = 13f,
            cornerRadiusDp = 24f,
            cornerSmoothing = 2.4f,
            transparency = 0.62f
        ),
        tertiaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFF8CD2FF.toInt(),
            tintDensity = 0.64f,
            coarseness = 0.52f,
            frostRadiusDp = 26f,
            ior = 1.52f,
            chromaticDispersion = 0.24f,
            rimChalkiness = 0.86f,
            thicknessDp = 16f,
            cornerRadiusDp = 26f,
            cornerSmoothing = 2.5f,
            transparency = 0.60f
        ),
        surfaceGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFD6EEFC.toInt(),
            tintDensity = 0.56f,
            coarseness = 0.60f,
            frostRadiusDp = 30f,
            ior = 1.48f,
            chromaticDispersion = 0.16f,
            rimChalkiness = 0.90f,
            thicknessDp = 14f,
            cornerRadiusDp = 32f,
            cornerSmoothing = 2.6f,
            transparency = 0.62f
        ),
        elevatedGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFC2E6FC.toInt(),
            tintDensity = 0.64f,
            coarseness = 0.58f,
            frostRadiusDp = 34f,
            ior = 1.50f,
            chromaticDispersion = 0.20f,
            rimChalkiness = 0.92f,
            thicknessDp = 18f,
            cornerRadiusDp = 32f,
            cornerSmoothing = 2.6f,
            transparency = 0.58f
        ),
        accentRimArgb = 0xFFFFFFFF.toInt(),
        onPrimaryGlassArgb = 0xFF183652.toInt(),
        onSurfaceGlassArgb = 0xFF1C3E5C.toInt()
    )

    val ObsidianTide = SeaGlassPalette(
        id = "obsidian_tide",
        displayName = "Obsidian Tide",
        tagline = "Volcanic smoked sapphire sea glass for dark-mode interfaces",
        primaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFF1D4E82.toInt(),
            tintDensity = 0.74f,
            coarseness = 0.38f,
            frostRadiusDp = 25f,
            ior = 1.56f,
            chromaticDispersion = 0.30f,
            rimChalkiness = 0.72f,
            thicknessDp = 16f,
            cornerRadiusDp = 28f,
            cornerSmoothing = 2.5f,
            transparency = 0.60f
        ),
        secondaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFF173E68.toInt(),
            tintDensity = 0.66f,
            coarseness = 0.42f,
            frostRadiusDp = 22f,
            ior = 1.52f,
            chromaticDispersion = 0.24f,
            rimChalkiness = 0.68f,
            thicknessDp = 13f,
            cornerRadiusDp = 24f,
            cornerSmoothing = 2.4f,
            transparency = 0.64f
        ),
        tertiaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFF22789A.toInt(),
            tintDensity = 0.70f,
            coarseness = 0.38f,
            frostRadiusDp = 24f,
            ior = 1.54f,
            chromaticDispersion = 0.28f,
            rimChalkiness = 0.74f,
            thicknessDp = 16f,
            cornerRadiusDp = 26f,
            cornerSmoothing = 2.5f,
            transparency = 0.62f
        ),
        surfaceGlass = SeaGlassMaterial(
            tintColorArgb = 0xFF143050.toInt(),
            tintDensity = 0.66f,
            coarseness = 0.44f,
            frostRadiusDp = 28f,
            ior = 1.50f,
            chromaticDispersion = 0.20f,
            rimChalkiness = 0.70f,
            thicknessDp = 14f,
            cornerRadiusDp = 32f,
            cornerSmoothing = 2.6f,
            transparency = 0.64f
        ),
        elevatedGlass = SeaGlassMaterial(
            tintColorArgb = 0xFF1A4574.toInt(),
            tintDensity = 0.72f,
            coarseness = 0.40f,
            frostRadiusDp = 32f,
            ior = 1.54f,
            chromaticDispersion = 0.26f,
            rimChalkiness = 0.76f,
            thicknessDp = 18f,
            cornerRadiusDp = 32f,
            cornerSmoothing = 2.6f,
            transparency = 0.60f
        ),
        accentRimArgb = 0xFF8CC4FF.toInt(),
        onPrimaryGlassArgb = 0xFFECF5FF.toInt(),
        onSurfaceGlassArgb = 0xFFE2EEFA.toInt()
    )

    val entries: List<SeaGlassPalette> = listOf(
        SeafoamMint,
        CobaltTide,
        BeachAmber,
        SeaLavender,
        RoseQuartzShell,
        KelpForest,
        MilkGlassFrost,
        ObsidianTide
    )
    }
}
