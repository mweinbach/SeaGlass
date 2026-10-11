package dev.seaglass.core

/**
 * Curated Sea Glass × Material 3 Expressive color & optical palettes.
 *
 * Each palette pairs authentic ocean-tumbled glass chemistry (cobalt, seafoam iron-soda,
 * sun-purpled manganese lavender, beach-sand amber, rose quartz, kelp jade, milk glass,
 * and volcanic obsidian) with Material 3 Expressive pastel chroma and contrast-safe
 * foreground tokens.
 */
enum class SeaGlassPalette(
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
    SeafoamMint(
        id = "seafoam_mint",
        displayName = "Seafoam Mint",
        tagline = "Surf-tumbled turquoise soda-lime glass with chalky tidal frost",
        primaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFF6EE7C8.toInt(),
            tintDensity = 0.54f,
            coarseness = 0.38f,
            frostRadiusDp = 22f,
            ior = 1.49f,
            chromaticDispersion = 0.24f,
            rimChalkiness = 0.72f,
            thicknessDp = 14f,
            cornerRadiusDp = 28f,
            cornerSmoothing = 2.5f
        ),
        secondaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFF9BF2DF.toInt(),
            tintDensity = 0.44f,
            coarseness = 0.44f,
            frostRadiusDp = 20f,
            ior = 1.48f,
            chromaticDispersion = 0.18f,
            rimChalkiness = 0.68f,
            thicknessDp = 11f,
            cornerRadiusDp = 24f,
            cornerSmoothing = 2.4f
        ),
        tertiaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFF7FD8FF.toInt(),
            tintDensity = 0.50f,
            coarseness = 0.36f,
            frostRadiusDp = 24f,
            ior = 1.51f,
            chromaticDispersion = 0.28f,
            rimChalkiness = 0.70f,
            thicknessDp = 14f,
            cornerRadiusDp = 26f,
            cornerSmoothing = 2.5f
        ),
        surfaceGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFD9F9F1.toInt(),
            tintDensity = 0.34f,
            coarseness = 0.48f,
            frostRadiusDp = 26f,
            ior = 1.47f,
            chromaticDispersion = 0.16f,
            rimChalkiness = 0.75f,
            thicknessDp = 12f,
            cornerRadiusDp = 32f,
            cornerSmoothing = 2.6f
        ),
        elevatedGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFAEF6E4.toInt(),
            tintDensity = 0.48f,
            coarseness = 0.42f,
            frostRadiusDp = 30f,
            ior = 1.50f,
            chromaticDispersion = 0.22f,
            rimChalkiness = 0.80f,
            thicknessDp = 18f,
            cornerRadiusDp = 32f,
            cornerSmoothing = 2.6f
        ),
        accentRimArgb = 0xFFE8FFF9.toInt(),
        onPrimaryGlassArgb = 0xFF0A2E26.toInt(),
        onSurfaceGlassArgb = 0xFF0F352D.toInt()
    ),

    CobaltTide(
        id = "cobalt_tide",
        displayName = "Cobalt Tide",
        tagline = "Vintage apothecary ocean bottle blue softened to expressive periwinkle",
        primaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFF6898FF.toInt(),
            tintDensity = 0.58f,
            coarseness = 0.34f,
            frostRadiusDp = 24f,
            ior = 1.52f,
            chromaticDispersion = 0.30f,
            rimChalkiness = 0.68f,
            thicknessDp = 16f,
            cornerRadiusDp = 28f,
            cornerSmoothing = 2.5f
        ),
        secondaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFF9ABBFF.toInt(),
            tintDensity = 0.46f,
            coarseness = 0.40f,
            frostRadiusDp = 21f,
            ior = 1.49f,
            chromaticDispersion = 0.22f,
            rimChalkiness = 0.66f,
            thicknessDp = 12f,
            cornerRadiusDp = 24f,
            cornerSmoothing = 2.4f
        ),
        tertiaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFF6AE2FF.toInt(),
            tintDensity = 0.52f,
            coarseness = 0.36f,
            frostRadiusDp = 24f,
            ior = 1.51f,
            chromaticDispersion = 0.28f,
            rimChalkiness = 0.72f,
            thicknessDp = 14f,
            cornerRadiusDp = 26f,
            cornerSmoothing = 2.5f
        ),
        surfaceGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFDCE8FF.toInt(),
            tintDensity = 0.36f,
            coarseness = 0.46f,
            frostRadiusDp = 28f,
            ior = 1.48f,
            chromaticDispersion = 0.18f,
            rimChalkiness = 0.74f,
            thicknessDp = 13f,
            cornerRadiusDp = 32f,
            cornerSmoothing = 2.6f
        ),
        elevatedGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFB5CEFF.toInt(),
            tintDensity = 0.50f,
            coarseness = 0.38f,
            frostRadiusDp = 32f,
            ior = 1.51f,
            chromaticDispersion = 0.25f,
            rimChalkiness = 0.78f,
            thicknessDp = 18f,
            cornerRadiusDp = 32f,
            cornerSmoothing = 2.6f
        ),
        accentRimArgb = 0xFFEEF5FF.toInt(),
        onPrimaryGlassArgb = 0xFF0A1E46.toInt(),
        onSurfaceGlassArgb = 0xFF10254F.toInt()
    ),

    BeachAmber(
        id = "beach_amber",
        displayName = "Beach Amber",
        tagline = "Sun-warmed honey and driftwood amber glass with fine sand grit",
        primaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFFFB86B.toInt(),
            tintDensity = 0.54f,
            coarseness = 0.42f,
            frostRadiusDp = 21f,
            ior = 1.50f,
            chromaticDispersion = 0.22f,
            rimChalkiness = 0.74f,
            thicknessDp = 15f,
            cornerRadiusDp = 28f,
            cornerSmoothing = 2.5f
        ),
        secondaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFFFD29D.toInt(),
            tintDensity = 0.44f,
            coarseness = 0.46f,
            frostRadiusDp = 19f,
            ior = 1.48f,
            chromaticDispersion = 0.18f,
            rimChalkiness = 0.70f,
            thicknessDp = 12f,
            cornerRadiusDp = 24f,
            cornerSmoothing = 2.4f
        ),
        tertiaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFFF967D.toInt(),
            tintDensity = 0.50f,
            coarseness = 0.40f,
            frostRadiusDp = 22f,
            ior = 1.50f,
            chromaticDispersion = 0.24f,
            rimChalkiness = 0.72f,
            thicknessDp = 14f,
            cornerRadiusDp = 26f,
            cornerSmoothing = 2.5f
        ),
        surfaceGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFFFEFE0.toInt(),
            tintDensity = 0.35f,
            coarseness = 0.50f,
            frostRadiusDp = 26f,
            ior = 1.47f,
            chromaticDispersion = 0.15f,
            rimChalkiness = 0.76f,
            thicknessDp = 12f,
            cornerRadiusDp = 32f,
            cornerSmoothing = 2.6f
        ),
        elevatedGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFFFDFBA.toInt(),
            tintDensity = 0.48f,
            coarseness = 0.44f,
            frostRadiusDp = 30f,
            ior = 1.49f,
            chromaticDispersion = 0.20f,
            rimChalkiness = 0.80f,
            thicknessDp = 18f,
            cornerRadiusDp = 32f,
            cornerSmoothing = 2.6f
        ),
        accentRimArgb = 0xFFFFF8F0.toInt(),
        onPrimaryGlassArgb = 0xFF381F05.toInt(),
        onSurfaceGlassArgb = 0xFF3E2408.toInt()
    ),

    SeaLavender(
        id = "sea_lavender",
        displayName = "Sea Lavender",
        tagline = "Sun-purpled manganese amethyst glass with velvety matte diffusion",
        primaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFC59BFF.toInt(),
            tintDensity = 0.54f,
            coarseness = 0.40f,
            frostRadiusDp = 23f,
            ior = 1.50f,
            chromaticDispersion = 0.26f,
            rimChalkiness = 0.72f,
            thicknessDp = 14f,
            cornerRadiusDp = 28f,
            cornerSmoothing = 2.5f
        ),
        secondaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFDCC2FF.toInt(),
            tintDensity = 0.44f,
            coarseness = 0.45f,
            frostRadiusDp = 20f,
            ior = 1.48f,
            chromaticDispersion = 0.20f,
            rimChalkiness = 0.68f,
            thicknessDp = 12f,
            cornerRadiusDp = 24f,
            cornerSmoothing = 2.4f
        ),
        tertiaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFFF9ED8.toInt(),
            tintDensity = 0.50f,
            coarseness = 0.38f,
            frostRadiusDp = 23f,
            ior = 1.50f,
            chromaticDispersion = 0.26f,
            rimChalkiness = 0.72f,
            thicknessDp = 14f,
            cornerRadiusDp = 26f,
            cornerSmoothing = 2.5f
        ),
        surfaceGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFF2E8FF.toInt(),
            tintDensity = 0.35f,
            coarseness = 0.48f,
            frostRadiusDp = 27f,
            ior = 1.47f,
            chromaticDispersion = 0.16f,
            rimChalkiness = 0.76f,
            thicknessDp = 12f,
            cornerRadiusDp = 32f,
            cornerSmoothing = 2.6f
        ),
        elevatedGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFE3CFFF.toInt(),
            tintDensity = 0.48f,
            coarseness = 0.42f,
            frostRadiusDp = 31f,
            ior = 1.49f,
            chromaticDispersion = 0.22f,
            rimChalkiness = 0.80f,
            thicknessDp = 18f,
            cornerRadiusDp = 32f,
            cornerSmoothing = 2.6f
        ),
        accentRimArgb = 0xFFFBF6FF.toInt(),
        onPrimaryGlassArgb = 0xFF281147.toInt(),
        onSurfaceGlassArgb = 0xFF2F1650.toInt()
    ),

    RoseQuartzShell(
        id = "rose_quartz_shell",
        displayName = "Rose Quartz Shell",
        tagline = "Rare coral-pink and cranberry tumbled glass with pearlescent bevel",
        primaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFFF96B8.toInt(),
            tintDensity = 0.53f,
            coarseness = 0.36f,
            frostRadiusDp = 22f,
            ior = 1.50f,
            chromaticDispersion = 0.25f,
            rimChalkiness = 0.72f,
            thicknessDp = 14f,
            cornerRadiusDp = 28f,
            cornerSmoothing = 2.5f
        ),
        secondaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFFFBDCE.toInt(),
            tintDensity = 0.43f,
            coarseness = 0.42f,
            frostRadiusDp = 20f,
            ior = 1.48f,
            chromaticDispersion = 0.19f,
            rimChalkiness = 0.68f,
            thicknessDp = 12f,
            cornerRadiusDp = 24f,
            cornerSmoothing = 2.4f
        ),
        tertiaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFFFB598.toInt(),
            tintDensity = 0.48f,
            coarseness = 0.38f,
            frostRadiusDp = 22f,
            ior = 1.49f,
            chromaticDispersion = 0.22f,
            rimChalkiness = 0.70f,
            thicknessDp = 14f,
            cornerRadiusDp = 26f,
            cornerSmoothing = 2.5f
        ),
        surfaceGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFFFE8F0.toInt(),
            tintDensity = 0.34f,
            coarseness = 0.48f,
            frostRadiusDp = 26f,
            ior = 1.47f,
            chromaticDispersion = 0.16f,
            rimChalkiness = 0.75f,
            thicknessDp = 12f,
            cornerRadiusDp = 32f,
            cornerSmoothing = 2.6f
        ),
        elevatedGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFFFD0E0.toInt(),
            tintDensity = 0.47f,
            coarseness = 0.40f,
            frostRadiusDp = 30f,
            ior = 1.49f,
            chromaticDispersion = 0.21f,
            rimChalkiness = 0.79f,
            thicknessDp = 18f,
            cornerRadiusDp = 32f,
            cornerSmoothing = 2.6f
        ),
        accentRimArgb = 0xFFFFF5F8.toInt(),
        onPrimaryGlassArgb = 0xFF3E0F21.toInt(),
        onSurfaceGlassArgb = 0xFF451427.toInt()
    ),

    KelpForest(
        id = "kelp_forest",
        displayName = "Kelp Forest",
        tagline = "Deep botanical forest-emerald sea glass with heavy surf grit",
        primaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFF58C991.toInt(),
            tintDensity = 0.58f,
            coarseness = 0.44f,
            frostRadiusDp = 23f,
            ior = 1.50f,
            chromaticDispersion = 0.22f,
            rimChalkiness = 0.74f,
            thicknessDp = 15f,
            cornerRadiusDp = 28f,
            cornerSmoothing = 2.5f
        ),
        secondaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFF8CE2B6.toInt(),
            tintDensity = 0.46f,
            coarseness = 0.48f,
            frostRadiusDp = 20f,
            ior = 1.48f,
            chromaticDispersion = 0.18f,
            rimChalkiness = 0.70f,
            thicknessDp = 12f,
            cornerRadiusDp = 24f,
            cornerSmoothing = 2.4f
        ),
        tertiaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFB8E97A.toInt(),
            tintDensity = 0.50f,
            coarseness = 0.42f,
            frostRadiusDp = 22f,
            ior = 1.49f,
            chromaticDispersion = 0.20f,
            rimChalkiness = 0.72f,
            thicknessDp = 14f,
            cornerRadiusDp = 26f,
            cornerSmoothing = 2.5f
        ),
        surfaceGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFDCF7E8.toInt(),
            tintDensity = 0.36f,
            coarseness = 0.52f,
            frostRadiusDp = 27f,
            ior = 1.47f,
            chromaticDispersion = 0.15f,
            rimChalkiness = 0.76f,
            thicknessDp = 12f,
            cornerRadiusDp = 32f,
            cornerSmoothing = 2.6f
        ),
        elevatedGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFACECC9.toInt(),
            tintDensity = 0.50f,
            coarseness = 0.46f,
            frostRadiusDp = 31f,
            ior = 1.49f,
            chromaticDispersion = 0.20f,
            rimChalkiness = 0.80f,
            thicknessDp = 18f,
            cornerRadiusDp = 32f,
            cornerSmoothing = 2.6f
        ),
        accentRimArgb = 0xFFF0FFF6.toInt(),
        onPrimaryGlassArgb = 0xFF092B1A.toInt(),
        onSurfaceGlassArgb = 0xFF0E3320.toInt()
    ),

    MilkGlassFrost(
        id = "milk_glass_frost",
        displayName = "Milk Glass Frost",
        tagline = "High-scatter alabaster milk glass with dense sand-frosted diffusion",
        primaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFECF4F8.toInt(),
            tintDensity = 0.52f,
            coarseness = 0.58f,
            frostRadiusDp = 28f,
            ior = 1.46f,
            chromaticDispersion = 0.14f,
            rimChalkiness = 0.86f,
            thicknessDp = 15f,
            cornerRadiusDp = 28f,
            cornerSmoothing = 2.5f
        ),
        secondaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFDDEAF2.toInt(),
            tintDensity = 0.44f,
            coarseness = 0.54f,
            frostRadiusDp = 24f,
            ior = 1.46f,
            chromaticDispersion = 0.12f,
            rimChalkiness = 0.82f,
            thicknessDp = 12f,
            cornerRadiusDp = 24f,
            cornerSmoothing = 2.4f
        ),
        tertiaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFCFE6F5.toInt(),
            tintDensity = 0.48f,
            coarseness = 0.52f,
            frostRadiusDp = 26f,
            ior = 1.47f,
            chromaticDispersion = 0.16f,
            rimChalkiness = 0.84f,
            thicknessDp = 14f,
            cornerRadiusDp = 26f,
            cornerSmoothing = 2.5f
        ),
        surfaceGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFF5FAFC.toInt(),
            tintDensity = 0.40f,
            coarseness = 0.62f,
            frostRadiusDp = 30f,
            ior = 1.45f,
            chromaticDispersion = 0.10f,
            rimChalkiness = 0.88f,
            thicknessDp = 13f,
            cornerRadiusDp = 32f,
            cornerSmoothing = 2.6f
        ),
        elevatedGlass = SeaGlassMaterial(
            tintColorArgb = 0xFFE6F1F7.toInt(),
            tintDensity = 0.54f,
            coarseness = 0.58f,
            frostRadiusDp = 34f,
            ior = 1.47f,
            chromaticDispersion = 0.14f,
            rimChalkiness = 0.90f,
            thicknessDp = 18f,
            cornerRadiusDp = 32f,
            cornerSmoothing = 2.6f
        ),
        accentRimArgb = 0xFFFFFFFF.toInt(),
        onPrimaryGlassArgb = 0xFF14232E.toInt(),
        onSurfaceGlassArgb = 0xFF182834.toInt()
    ),

    ObsidianTide(
        id = "obsidian_tide",
        displayName = "Obsidian Tide",
        tagline = "Volcanic smoked sea glass for dark-mode interfaces with moonlight frost",
        primaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFF284664.toInt(),
            tintDensity = 0.64f,
            coarseness = 0.36f,
            frostRadiusDp = 25f,
            ior = 1.52f,
            chromaticDispersion = 0.26f,
            rimChalkiness = 0.62f,
            thicknessDp = 16f,
            cornerRadiusDp = 28f,
            cornerSmoothing = 2.5f
        ),
        secondaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFF1F354C.toInt(),
            tintDensity = 0.58f,
            coarseness = 0.40f,
            frostRadiusDp = 22f,
            ior = 1.50f,
            chromaticDispersion = 0.20f,
            rimChalkiness = 0.58f,
            thicknessDp = 13f,
            cornerRadiusDp = 24f,
            cornerSmoothing = 2.4f
        ),
        tertiaryGlass = SeaGlassMaterial(
            tintColorArgb = 0xFF33657A.toInt(),
            tintDensity = 0.60f,
            coarseness = 0.38f,
            frostRadiusDp = 24f,
            ior = 1.51f,
            chromaticDispersion = 0.24f,
            rimChalkiness = 0.64f,
            thicknessDp = 15f,
            cornerRadiusDp = 26f,
            cornerSmoothing = 2.5f
        ),
        surfaceGlass = SeaGlassMaterial(
            tintColorArgb = 0xFF162433.toInt(),
            tintDensity = 0.56f,
            coarseness = 0.44f,
            frostRadiusDp = 28f,
            ior = 1.49f,
            chromaticDispersion = 0.18f,
            rimChalkiness = 0.60f,
            thicknessDp = 14f,
            cornerRadiusDp = 32f,
            cornerSmoothing = 2.6f
        ),
        elevatedGlass = SeaGlassMaterial(
            tintColorArgb = 0xFF22384F.toInt(),
            tintDensity = 0.64f,
            coarseness = 0.40f,
            frostRadiusDp = 32f,
            ior = 1.51f,
            chromaticDispersion = 0.22f,
            rimChalkiness = 0.68f,
            thicknessDp = 18f,
            cornerRadiusDp = 32f,
            cornerSmoothing = 2.6f
        ),
        accentRimArgb = 0xFF8CC4FF.toInt(),
        onPrimaryGlassArgb = 0xFFECF5FF.toInt(),
        onSurfaceGlassArgb = 0xFFE2EEFA.toInt()
    );
}
