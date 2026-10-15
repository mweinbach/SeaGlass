using System;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Detached corner pod surface.
/// </summary>
public class SeaGlassDetachedCornerPod : SeaGlassCornerEmbeddedPod
{
    public SeaGlassDetachedCornerPod()
    {
        DefaultStyleKey = typeof(SeaGlassDetachedCornerPod);
    }
}

/// <summary>
/// Concentric corner anchor surface.
/// </summary>
public class SeaGlassConcentricCornerAnchor : SeaGlassSurface
{
    public SeaGlassConcentricCornerAnchor()
    {
        DefaultStyleKey = typeof(SeaGlassConcentricCornerAnchor);
        CornerAdaptationMode = SeaGlassCornerAdaptationMode.ConcentricProximity;
    }
}

/// <summary>
/// Spatial glow plate with rough-Fresnel halo.
/// </summary>
public class SeaGlassSpatialGlowPlate : SeaGlassSurface
{
    public SeaGlassSpatialGlowPlate()
    {
        DefaultStyleKey = typeof(SeaGlassSpatialGlowPlate);
    }
}

/// <summary>
/// Fluid island pill overlay.
/// </summary>
public class SeaGlassFluidIslandPill : SeaGlassLiveActivityPill
{
    public SeaGlassFluidIslandPill()
    {
        DefaultStyleKey = typeof(SeaGlassFluidIslandPill);
    }
}

/// <summary>
/// Liquid split pill interactive control.
/// </summary>
public class SeaGlassLiquidSplitPill : SeaGlassPill
{
    public SeaGlassLiquidSplitPill()
    {
        DefaultStyleKey = typeof(SeaGlassLiquidSplitPill);
    }
}

/// <summary>
/// Interactive squircle plate surface.
/// </summary>
public class SeaGlassInteractiveSquirclePlate : SeaGlassSurface
{
    public SeaGlassInteractiveSquirclePlate()
    {
        DefaultStyleKey = typeof(SeaGlassInteractiveSquirclePlate);
    }
}
