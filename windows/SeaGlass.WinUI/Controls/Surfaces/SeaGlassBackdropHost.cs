using System;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Acyclic backdrop host surface for embedding background layers without feedback loops.
/// </summary>
public class SeaGlassBackdropHost : SeaGlassSurface
{
    public SeaGlassBackdropHost()
    {
        DefaultStyleKey = typeof(SeaGlassBackdropHost);
    }

    public SeaGlassBackdropHost(
        SeaGlassStageState? stage = null,
        SeaGlassMaterial? material = null)
        : base(stage, material)
    {
        DefaultStyleKey = typeof(SeaGlassBackdropHost);
    }
}
