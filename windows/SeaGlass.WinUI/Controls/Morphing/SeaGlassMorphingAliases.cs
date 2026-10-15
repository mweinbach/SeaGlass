using System;
using Microsoft.UI.Xaml.Controls;

namespace SeaGlass.WinUI;

/// <summary>
/// Organic liquid blob morph control.
/// </summary>
public class SeaGlassLiquidBlobMorph : SeaGlassMultiShapeMorph
{
    public SeaGlassLiquidBlobMorph()
    {
        DefaultStyleKey = typeof(SeaGlassLiquidBlobMorph);
    }
}

/// <summary>
/// State-driven morphing surface.
/// </summary>
public class SeaGlassStateDrivenMorph : SeaGlassTwoElementMorph
{
    public SeaGlassStateDrivenMorph()
    {
        DefaultStyleKey = typeof(SeaGlassStateDrivenMorph);
    }
}

/// <summary>
/// Fluid shape bridge morph control.
/// </summary>
public class SeaGlassFluidShapeBridge : SeaGlassTwoElementMorph
{
    public SeaGlassFluidShapeBridge()
    {
        DefaultStyleKey = typeof(SeaGlassFluidShapeBridge);
    }
}
