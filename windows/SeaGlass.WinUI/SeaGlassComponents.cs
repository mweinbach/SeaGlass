using System;
using System.Numerics;
using SeaGlass.Core;
using SeaGlass.Core.Backend;

namespace SeaGlass.WinUI;

/// <summary>
/// Evaluated per-frame optical, geometric, and uniform state for any <see cref="SeaGlassSurface"/>
/// or derived WinUI SeaGlass control.
/// </summary>
public readonly record struct SeaGlassSurfaceFrameState(
    SeaGlassMaterial EffectiveMaterial,
    int OptimalContentColorArgb,
    SeaGlassCornerRadiiPx SolvedCornerRadiiPx,
    SeaGlassSafePaddingPx SafePaddingPx,
    float ScaleX,
    float ScaleY,
    float TiltRotationXDegrees,
    float TiltRotationYDegrees,
    SeaGlassUniformBlock UniformBlock,
    SeaGlassVectorPath2D? ContourPath = null,
    bool IsUniformSingleNode = false,
    float WidthPx = 0f,
    float HeightPx = 0f)
{
    /// <summary>
    /// Lazily serialized XAML / SVG path string (<c>F1 M ... C ... Z</c>).
    /// Live Direct2D / Composition rendering consumes <see cref="ContourPath"/> and <see cref="IsUniformSingleNode"/>
    /// directly without string formatting or <c>Geometry.Parse</c>.
    /// </summary>
    public string XamlContourPathData
    {
        get
        {
            if (ContourPath is { IsEmpty: false })
            {
                return ContourPath.ToXamlPathData();
            }
            SeaGlassVectorPath2D fallback = new();
            fallback.AddRoundRect(
                0f, 0f, MathF.Max(1f, WidthPx), MathF.Max(1f, HeightPx),
                SolvedCornerRadiiPx.TopLeftPx,
                SolvedCornerRadiiPx.TopRightPx,
                SolvedCornerRadiiPx.BottomRightPx,
                SolvedCornerRadiiPx.BottomLeftPx);
            return fallback.ToXamlPathData();
        }
    }
}
