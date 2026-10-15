using System;
using Microsoft.UI.Xaml;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

public static partial class SeaGlassLayout
{
    public static SeaGlassFluidGroup FluidGroup(params UIElement[] children)
    {
        var group = new SeaGlassFluidGroup();
        foreach (var child in children)
            group.Children.Add(child);
        return group;
    }

    public static SeaGlassFluidGroup FluidGroup(double smoothRadiusDp, params UIElement[] children)
    {
        var group = new SeaGlassFluidGroup { SmoothRadiusDp = smoothRadiusDp };
        foreach (var child in children)
            group.Children.Add(child);
        return group;
    }
}

public static partial class SeaGlassControlBuilders
{
    // Layout forwards for FluidGroup
    public static SeaGlassFluidGroup FluidGroup(params UIElement[] children) =>
        SeaGlassLayout.FluidGroup(children);

    public static SeaGlassFluidGroup FluidGroup(double smoothRadiusDp, params UIElement[] children) =>
        SeaGlassLayout.FluidGroup(smoothRadiusDp, children);

    // ==========================================
    // Fluid Coalescing Extensions
    // ==========================================
    public static T FluidNode<T>(
        this T element,
        string? nodeId = null,
        bool isFluid = true,
        SeaGlassMaterial? materialOverride = null,
        double cornerRadiusOverride = double.NaN) where T : FrameworkElement
    {
        SeaGlassFluidGroup.SetIsFluidNode(element, isFluid);
        if (!string.IsNullOrEmpty(nodeId))
            SeaGlassFluidGroup.SetNodeId(element, nodeId);
        if (materialOverride != null)
            SeaGlassFluidGroup.SetFluidMaterialOverride(element, materialOverride);
        if (!double.IsNaN(cornerRadiusOverride))
            SeaGlassFluidGroup.SetCornerRadiusOverride(element, cornerRadiusOverride);
        return element;
    }

    public static T WithFluidNode<T>(
        this T element,
        string? nodeId = null,
        bool isFluid = true,
        SeaGlassMaterial? materialOverride = null,
        double cornerRadiusOverride = double.NaN) where T : FrameworkElement =>
        element.FluidNode(nodeId, isFluid, materialOverride, cornerRadiusOverride);

    public static SeaGlassFluidGroup SmoothRadius(this SeaGlassFluidGroup group, double radiusDp)
    {
        group.SmoothRadiusDp = radiusDp;
        return group;
    }

    public static SeaGlassFluidGroup WithSmoothRadius(this SeaGlassFluidGroup group, double radiusDp) =>
        group.SmoothRadius(radiusDp);

    public static SeaGlassFluidGroup FluidChildren(this SeaGlassFluidGroup group, params UIElement[] children)
    {
        foreach (var child in children)
        {
            group.Children.Add(child);
        }
        return group;
    }

    public static SeaGlassFluidGroup WithFluidChildren(this SeaGlassFluidGroup group, params UIElement[] children) =>
        group.FluidChildren(children);
}
