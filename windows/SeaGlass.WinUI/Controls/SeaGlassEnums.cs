using System;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// shadcn-style component variant role.
/// </summary>
public enum SeaGlassVariant
{
    // Canonical Semantic Variants
    Filled = 0,
    Elevated = 1,
    Outlined = 2,
    Text = 3,
    Tonal = 4,
    Destructive = 5,
    Tertiary = 6,

    // Backward-Compatible & Palette Aliases
    Primary = Filled,
    Secondary = Tonal,
    Ghost = Text,
    Outline = Outlined,
    Subtle = 7
}

/// <summary>
/// Metric sizing tokens for SeaGlass controls.
/// </summary>
public enum SeaGlassControlSize
{
    Compact = 0,
    Small = 1,
    Medium = 2,
    Large = 3,
    Icon = 4,

    // Aliases
    Sm = Small,
    Md = Medium,
    Lg = Large
}

/// <summary>
/// Anchor corner for flush-embedded corner pods.
/// </summary>
public enum SeaGlassAnchorCorner
{
    TopLeft,
    TopRight,
    BottomRight,
    BottomLeft
}

/// <summary>
/// Curated Material 3 Expressive palette tones for SeaGlass on Windows.
/// </summary>
public enum SeaGlassTone
{
    SeafoamMint,      // #56B89D - Classic coastal sea glass (green-cyan tint)
    CobaltTide,       // #2854C5 - Deep ocean bottle glass (vivid cobalt blue)
    BeachAmber,       // #D89832 - Sun-baked beer bottle glass (golden amber)
    SeaLavender,      // #9670D6 - Delicate tumbled lilac glass (soft purple)
    RoseQuartzShell,  // #E07A8B - Coral-peach tumbled glass (warm pink/peach)
    KelpForest,       // #3E7C4A - Deep coastal seaweed glass (rich olive green)
    MilkGlassFrost,   // #E4ECE8 - Opaque white tumbled glass (chalky milky white)
    ObsidianTide      // #2A3236 - Volcanic dark sea glass (smoky charcoal)
}

/// <summary>
/// Helper extensions for SeaGlass tones, variants, and sizes.
/// </summary>
public static class SeaGlassToneExtensions
{
    public static SeaGlassPalette ToPalette(this SeaGlassTone tone) => tone switch
    {
        SeaGlassTone.SeafoamMint => SeaGlassPalette.SeafoamMint,
        SeaGlassTone.CobaltTide => SeaGlassPalette.CobaltTide,
        SeaGlassTone.BeachAmber => SeaGlassPalette.BeachAmber,
        SeaGlassTone.SeaLavender => SeaGlassPalette.SeaLavender,
        SeaGlassTone.RoseQuartzShell => SeaGlassPalette.RoseQuartzShell,
        SeaGlassTone.KelpForest => SeaGlassPalette.KelpForest,
        SeaGlassTone.MilkGlassFrost => SeaGlassPalette.MilkGlassFrost,
        SeaGlassTone.ObsidianTide => SeaGlassPalette.ObsidianTide,
        _ => SeaGlassPalette.SeafoamMint
    };

    public static SeaGlassMaterial ResolveMaterial(SeaGlassTone tone, SeaGlassVariant variant)
    {
        SeaGlassPalette palette = tone.ToPalette();
        return variant switch
        {
            SeaGlassVariant.Filled => palette.PrimaryGlass,
            SeaGlassVariant.Elevated => palette.ElevatedGlass with
            {
                ThicknessDp = 20f,
                FrostRadiusDp = 28f,
                Coarseness = 0.32f,
                RimChalkiness = 0.82f
            },
            SeaGlassVariant.Outlined => palette.SurfaceGlass with
            {
                TintDensity = 0.05f,
                RimChalkiness = 0.95f,
                Transparency = 0.96f,
                Coarseness = 0.28f
            },
            SeaGlassVariant.Text => palette.SurfaceGlass with
            {
                TintDensity = 0.04f,
                RimChalkiness = 0.15f,
                Transparency = 0.98f,
                Coarseness = 0.10f
            },
            SeaGlassVariant.Tonal => palette.SecondaryGlass,
            SeaGlassVariant.Tertiary => palette.TertiaryGlass,
            SeaGlassVariant.Destructive => new SeaGlassMaterial(
                tintColorArgb: unchecked((int)0xFFFF5C5C),
                tintDensity: 0.72f,
                coarseness: 0.40f,
                frostRadiusDp: 24f,
                ior: 1.55f,
                rimChalkiness: 0.85f),
            (SeaGlassVariant)7 => palette.SurfaceGlass with { TintDensity = 0.22f, Transparency = 0.88f },
            _ => palette.PrimaryGlass
        };
    }
}
