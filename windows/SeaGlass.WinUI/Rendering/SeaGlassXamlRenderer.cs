using System;
using System.Numerics;
using SeaGlass.Core;

namespace SeaGlass.WinUI.Rendering;

/// <summary>
/// Global rendering options and optical math helpers for SeaGlass WinUI surfaces.
/// Decoupled from legacy WPF DrawingContext rendering to support WinUI 3 and Direct2D/Composition.
/// </summary>
public static class SeaGlassXamlRenderer
{
    /// <summary>
    /// Enables pointer-reactive 3D skew tilt, cursor-tracked refraction/bevel light, and the press
    /// caustic glow. Off by default: desktop glass stays optically still under click &amp; drag
    /// (hover lift and press squash remain).
    /// </summary>
    public static bool PointerTiltAndGlowEnabled { get; set; } = false;

    /// <summary>
    /// Computes intaglio glass etching parameters for text/icons/glyphs.
    /// An Oklab-L–driven 'lip' pass: light ink (L > 0.55) gets a dark lip offset up-left (-0.75px, -0.75px);
    /// dark ink (L &lt;= 0.55) gets a white/catch-light lip offset down-right (+0.75px, +0.75px).
    /// </summary>
    public static (int LipColorArgb, Vector2 LipOffsetPx) ComputeIntaglioLip(int inkArgb, float coarseness = 0.45f)
    {
        SeaGlassColorMath.OklabColor lab = SeaGlassColorMath.ArgbToOklab(inkArgb);
        if (lab.L > 0.55f)
        {
            // Light ink on frosted glass: dark etched shadow lip offset up-left
            int darkLip = SeaGlassColorMath.ComputeTonalPitColor(inkArgb, 0.70f);
            return (darkLip, new Vector2(-0.75f, -0.75f));
        }
        else
        {
            // Dark ink on frosted glass: luminous catch-light lip offset down-right
            int lightLip = SeaGlassColorMath.ComputeEtchedCatchLightColor(inkArgb, coarseness);
            return (lightLip, new Vector2(0.75f, 0.75f));
        }
    }

    /// <summary>
    /// Computes the luminous internal subsurface caustic color that pools along the lower bevel
    /// and curved bottom edge of a convex sea glass surface.
    /// </summary>
    public static int ComputeSubsurfaceCausticColor(int tintArgb, float coarseness = 0.45f)
        => SeaGlassColorMath.ComputeSubsurfaceCausticColor(tintArgb, coarseness);

    /// <summary>
    /// Computes the chroma-enriched tonal mineral pit color for micro-abrasions and sand pits.
    /// </summary>
    public static int ComputeTonalPitColor(int tintArgb, float tintDensity = 0.65f)
        => SeaGlassColorMath.ComputeTonalPitColor(tintArgb, tintDensity);

    /// <summary>
    /// Computes the frosted rind catch light color for etched bevels.
    /// </summary>
    public static int ComputeEtchedCatchLightColor(int tintArgb, float coarseness = 0.45f)
        => SeaGlassColorMath.ComputeEtchedCatchLightColor(tintArgb, coarseness);
}
