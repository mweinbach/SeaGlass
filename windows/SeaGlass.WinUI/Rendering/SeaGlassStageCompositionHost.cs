using System;
using System.Numerics;
using Microsoft.UI.Composition;

namespace SeaGlass.WinUI.Rendering;

/// <summary>
/// Manages zero-copy acyclic backdrop sampling via Windows Composition (<see cref="CompositionVisualSurface"/>).
/// Enforces Rule #4 in AGENTS.md: records background visual content into a dedicated visual surface
/// before foreground glass surfaces sample from it, preventing recursive feedback loops.
/// </summary>
public sealed class SeaGlassStageCompositionHost : IDisposable
{
    private readonly Compositor _compositor;
    private readonly Visual _backdropVisual;
    private CompositionVisualSurface? _visualSurface;
    private CompositionSurfaceBrush? _surfaceBrush;

    public CompositionVisualSurface VisualSurface => _visualSurface ??= CreateVisualSurface();
    public CompositionSurfaceBrush SurfaceBrush => _surfaceBrush ??= CreateSurfaceBrush();

    public SeaGlassStageCompositionHost(Visual backdropVisual)
    {
        _backdropVisual = backdropVisual ?? throw new ArgumentNullException(nameof(backdropVisual));
        _compositor = backdropVisual.Compositor;
    }

    private CompositionVisualSurface CreateVisualSurface()
    {
        CompositionVisualSurface surface = _compositor.CreateVisualSurface();
        surface.SourceVisual = _backdropVisual;
        return surface;
    }

    private CompositionSurfaceBrush CreateSurfaceBrush()
    {
        CompositionSurfaceBrush brush = _compositor.CreateSurfaceBrush(VisualSurface);
        brush.HorizontalAlignmentRatio = 0f;
        brush.VerticalAlignmentRatio = 0f;
        brush.Stretch = CompositionStretch.None;
        return brush;
    }

    /// <summary>
    /// Updates the backdrop capture dimension in pixels.
    /// </summary>
    public void UpdateStageSize(Vector2 sizePx)
    {
        if (_visualSurface is not null)
        {
            _visualSurface.SourceSize = sizePx;
        }
    }

    /// <summary>
    /// Binds the shared zero-copy backdrop brush to a foreground glass visual,
    /// translating its UV coordinates so the glass samples the exact underlying backdrop region.
    /// </summary>
    public void BindSurfaceBackdrop(SpriteVisual glassVisual, Vector2 originInStagePx)
    {
        SurfaceBrush.TransformMatrix = Matrix3x2.CreateTranslation(-originInStagePx.X, -originInStagePx.Y);
        glassVisual.Brush = SurfaceBrush;
    }

    public void Dispose()
    {
        _surfaceBrush?.Dispose();
        _surfaceBrush = null;
        _visualSurface?.Dispose();
        _visualSurface = null;
    }
}
