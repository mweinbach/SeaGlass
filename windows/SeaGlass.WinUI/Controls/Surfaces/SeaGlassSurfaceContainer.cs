using System;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// General-purpose container surface with SeaGlass optics and concentric corner adaptation.
/// </summary>
public class SeaGlassSurfaceContainer : SeaGlassSurface
{
    public SeaGlassSurfaceContainer()
    {
        DefaultStyleKey = typeof(SeaGlassSurfaceContainer);
    }

    public SeaGlassSurfaceContainer(
        SeaGlassStageState? stage = null,
        SeaGlassMaterial? material = null)
        : base(stage, material)
    {
        DefaultStyleKey = typeof(SeaGlassSurfaceContainer);
    }
}
