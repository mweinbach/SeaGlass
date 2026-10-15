using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Top-level declarative layout functions (VStack, HStack, ZStack, Spacer).
/// Enables single-expression SwiftUI-style page and hierarchy construction.
/// </summary>
public static partial class SeaGlassLayout
{
    public static StackPanel VStack(params UIElement[] children)
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical };
        foreach (var child in children)
            panel.Children.Add(child);
        return panel;
    }

    public static StackPanel VStack(double spacing, params UIElement[] children)
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical, Spacing = spacing };
        foreach (var child in children)
            panel.Children.Add(child);
        return panel;
    }

    public static StackPanel HStack(params UIElement[] children)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (var child in children)
            panel.Children.Add(child);
        return panel;
    }

    public static StackPanel HStack(double spacing, params UIElement[] children)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = spacing };
        foreach (var child in children)
            panel.Children.Add(child);
        return panel;
    }

    public static Grid ZStack(params UIElement[] children)
    {
        var grid = new Grid();
        foreach (var child in children)
            grid.Children.Add(child);
        return grid;
    }

    public static Border Spacer() =>
        new Border
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

    public static Border Spacer(double minWidth, double minHeight) =>
        new Border
        {
            MinWidth = minWidth,
            MinHeight = minHeight,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
}

/// <summary>
/// Fluent C# builder and extension methods for SeaGlass layout hierarchies and FrameworkElements.
/// </summary>
public static partial class SeaGlassControlBuilders
{
    // Layout builder forwards
    public static StackPanel VStack(params UIElement[] children) => SeaGlassLayout.VStack(children);
    public static StackPanel VStack(double spacing, params UIElement[] children) => SeaGlassLayout.VStack(spacing, children);
    public static StackPanel HStack(params UIElement[] children) => SeaGlassLayout.HStack(children);
    public static StackPanel HStack(double spacing, params UIElement[] children) => SeaGlassLayout.HStack(spacing, children);
    public static Grid ZStack(params UIElement[] children) => SeaGlassLayout.ZStack(children);
    public static Border Spacer() => SeaGlassLayout.Spacer();
    public static Border Spacer(double minWidth, double minHeight) => SeaGlassLayout.Spacer(minWidth, minHeight);

    // ==========================================
    // Composition & Layout Extensions
    // ==========================================
    public static T Content<T>(this T control, object content) where T : ContentControl
    {
        control.Content = content;
        return control;
    }

    public static T WithContent<T>(this T control, object content) where T : ContentControl =>
        control.Content(content);

    public static T Children<T>(this T panel, params UIElement[] children) where T : Panel
    {
        foreach (var child in children)
            panel.Children.Add(child);
        return panel;
    }

    public static T WithChildren<T>(this T panel, params UIElement[] children) where T : Panel =>
        panel.Children(children);

    public static StackPanel Horizontal(this StackPanel stack, double spacing = 0)
    {
        stack.Orientation = Orientation.Horizontal;
        stack.Spacing = spacing;
        return stack;
    }

    public static StackPanel Vertical(this StackPanel stack, double spacing = 0)
    {
        stack.Orientation = Orientation.Vertical;
        stack.Spacing = spacing;
        return stack;
    }

    public static StackPanel Spacing(this StackPanel stack, double spacing)
    {
        stack.Spacing = spacing;
        return stack;
    }

    public static T Size<T>(this T element, double width, double height) where T : FrameworkElement
    {
        element.Width = width;
        element.Height = height;
        return element;
    }

    public static T Align<T>(this T element, HorizontalAlignment horizontal, VerticalAlignment vertical) where T : FrameworkElement
    {
        element.HorizontalAlignment = horizontal;
        element.VerticalAlignment = vertical;
        return element;
    }

    public static T Center<T>(this T element) where T : FrameworkElement =>
        element.Align(HorizontalAlignment.Center, VerticalAlignment.Center);

    public static T Stretch<T>(this T element) where T : FrameworkElement =>
        element.Align(HorizontalAlignment.Stretch, VerticalAlignment.Stretch);

    public static T GridRow<T>(this T element, int row) where T : FrameworkElement
    {
        Grid.SetRow(element, row);
        return element;
    }

    public static T GridColumn<T>(this T element, int column) where T : FrameworkElement
    {
        Grid.SetColumn(element, column);
        return element;
    }

    public static T GridRowSpan<T>(this T element, int span) where T : FrameworkElement
    {
        Grid.SetRowSpan(element, span);
        return element;
    }

    public static T GridColumnSpan<T>(this T element, int span) where T : FrameworkElement
    {
        Grid.SetColumnSpan(element, span);
        return element;
    }

    /// <summary>Wraps any FrameworkElement into a SeaGlassSurface container (SwiftUI-like).</summary>
    public static SeaGlassSurface InSeaGlassSurface(
        this FrameworkElement content,
        SeaGlassVariant variant = SeaGlassVariant.Filled,
        SeaGlassTone tone = SeaGlassTone.SeafoamMint,
        double? cornerRadius = null)
    {
        var surface = new SeaGlassSurface
        {
            Content = content,
            Variant = variant,
            Tone = tone
        };
        if (cornerRadius.HasValue)
            surface.CornerRadius = new CornerRadius(cornerRadius.Value);
        return surface;
    }
}
