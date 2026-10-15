using System;
using Microsoft.UI.Composition;
using SeaGlass.Core;

namespace SeaGlass.WinUI.Rendering;

/// <summary>
/// Unified contract for a GPU rendering tier pipeline in WinUI 3 / Windows Composition.
/// </summary>
public interface ISeaGlassRenderPipeline : IDisposable
{
    /// <summary>
    /// The device tier handled by this pipeline.
    /// </summary>
    SeaGlassDeviceTier Tier { get; }

    /// <summary>
    /// Initializes compositor resources.
    /// </summary>
    void Initialize(Compositor? compositor);

    /// <summary>
    /// Renders or updates visual representation for a SeaGlass surface.
    /// Steady-state executions must produce zero heap allocations.
    /// </summary>
    void RenderSurface(SeaGlassRenderContext context, SpriteVisual? targetVisual);
}
