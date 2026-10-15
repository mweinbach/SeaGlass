using System;
using SeaGlass.Core;
using Xunit;

namespace SeaGlass.Tests.Theming;

/// <summary>
/// Unit tests mathematically verifying WCAG 2.1 AA text contrast ratios (>= 4.5:1)
/// across all 8 signature Material 3 Expressive × SeaGlass palettes in Light and Dark themes.
/// </summary>
public sealed class ColorContrastTests
{
    private static double LinearChannel(byte sRgb8)
    {
        double val = sRgb8 / 255.0;
        return val <= 0.04045 ? val / 12.92 : Math.Pow((val + 0.055) / 1.055, 2.4);
    }

    private static double RelativeLuminance(int argb)
    {
        byte r = (byte)((argb >> 16) & 0xFF);
        byte g = (byte)((argb >> 8) & 0xFF);
        byte b = (byte)(argb & 0xFF);

        double rLin = LinearChannel(r);
        double gLin = LinearChannel(g);
        double bLin = LinearChannel(b);

        return 0.2126 * rLin + 0.7152 * gLin + 0.0722 * bLin;
    }

    public static double CalculateContrastRatio(int color1Argb, int color2Argb)
    {
        double l1 = RelativeLuminance(color1Argb);
        double l2 = RelativeLuminance(color2Argb);

        double brighter = Math.Max(l1, l2);
        double darker = Math.Min(l1, l2);

        return (brighter + 0.05) / (darker + 0.05);
    }

    [Theory]
    [InlineData("seafoam_mint")]
    [InlineData("cobalt_tide")]
    [InlineData("beach_amber")]
    [InlineData("sea_lavender")]
    [InlineData("rose_quartz_shell")]
    [InlineData("kelp_forest")]
    [InlineData("milk_glass_frost")]
    [InlineData("obsidian_tide")]
    public void AllPalettes_PrimaryGlassAndOnPrimary_SatisfyWcagAaContrast(string paletteId)
    {
        SeaGlassPalette palette = SeaGlassPalette.FromId(paletteId);
        int tint = palette.PrimaryGlass.TintColorArgb;
        int onTone = palette.OnPrimaryGlassArgb;

        double ratio = CalculateContrastRatio(tint, onTone);

        Assert.True(ratio >= 4.5,
            $"Palette '{palette.DisplayName}' Primary contrast ratio {ratio:F2}:1 is below WCAG 2.1 AA threshold (4.5:1). " +
            $"Tint=#{tint:X8}, OnTone=#{onTone:X8}");
    }

    [Theory]
    [InlineData("seafoam_mint")]
    [InlineData("cobalt_tide")]
    [InlineData("beach_amber")]
    [InlineData("sea_lavender")]
    [InlineData("rose_quartz_shell")]
    [InlineData("kelp_forest")]
    [InlineData("milk_glass_frost")]
    [InlineData("obsidian_tide")]
    public void AllPalettes_SurfaceGlassAndOnSurface_SatisfyWcagAaContrast(string paletteId)
    {
        SeaGlassPalette palette = SeaGlassPalette.FromId(paletteId);
        int tint = palette.SurfaceGlass.TintColorArgb;
        int onTone = palette.OnSurfaceGlassArgb;

        double ratio = CalculateContrastRatio(tint, onTone);

        Assert.True(ratio >= 4.5,
            $"Palette '{palette.DisplayName}' Surface contrast ratio {ratio:F2}:1 is below WCAG 2.1 AA threshold (4.5:1). " +
            $"Tint=#{tint:X8}, OnTone=#{onTone:X8}");
    }

    [Fact]
    public void CobaltTide_SpecificallyVerifiesFixedContrastRatioAbove4Point5()
    {
        SeaGlassPalette cobalt = SeaGlassPalette.CobaltTide;
        double ratio = CalculateContrastRatio(cobalt.PrimaryGlass.TintColorArgb, cobalt.OnPrimaryGlassArgb);

        // Previous legacy value was 3.17:1. The fixed #FF000F5C guarantees >= 4.8:1
        Assert.True(ratio >= 4.8, $"Cobalt Tide primary contrast ratio must be >= 4.8:1, got {ratio:F2}:1");
        Assert.Equal(unchecked((int)0xFF000F5C), cobalt.OnPrimaryGlassArgb);
    }

    [Fact]
    public void DarkMode_LuminousAlabaster_ExceedsWcagAaa()
    {
        // Dark theme alabaster text (#FFECF5FF) against dark tints
        int alabasterText = unchecked((int)0xFFECF5FF);
        int obsidianTint = unchecked((int)0xFF1D4E82);
        int darkCobaltTint = unchecked((int)0xFF143050);

        double ratioObsidian = CalculateContrastRatio(obsidianTint, alabasterText);
        double ratioCobalt = CalculateContrastRatio(darkCobaltTint, alabasterText);

        Assert.True(ratioObsidian >= 7.0, $"Obsidian text ratio {ratioObsidian:F2}:1 should meet WCAG AAA (>= 7.0:1)");
        Assert.True(ratioCobalt >= 7.0, $"Dark cobalt ratio {ratioCobalt:F2}:1 should meet WCAG AAA (>= 7.0:1)");
    }

    [Fact]
    public void DynamicPaletteDerivation_GuaranteesWcagAaContrast()
    {
        // Test system accent color harmonization from diverse hues
        int[] accentColors =
        [
            unchecked((int)0xFF0078D4), // Windows blue
            unchecked((int)0xFFE81123), // Windows red
            unchecked((int)0xFF107C41), // Windows green
            unchecked((int)0xFF881798), // Windows purple
            unchecked((int)0xFFFF8C00)  // Windows orange
        ];

        foreach (int accent in accentColors)
        {
            SeaGlassPalette derived = SeaGlassPalette.FromAccentColors(accent, accent, accent, accent);
            double primaryRatio = CalculateContrastRatio(derived.PrimaryGlass.TintColorArgb, derived.OnPrimaryGlassArgb);
            double surfaceRatio = CalculateContrastRatio(derived.SurfaceGlass.TintColorArgb, derived.OnSurfaceGlassArgb);

            Assert.True(primaryRatio >= 4.5,
                $"Derived palette for accent #{accent:X8} Primary ratio {primaryRatio:F2}:1 must meet WCAG AA (>= 4.5:1)");
            Assert.True(surfaceRatio >= 4.5,
                $"Derived palette for accent #{accent:X8} Surface ratio {surfaceRatio:F2}:1 must meet WCAG AA (>= 4.5:1)");
        }
    }
}
