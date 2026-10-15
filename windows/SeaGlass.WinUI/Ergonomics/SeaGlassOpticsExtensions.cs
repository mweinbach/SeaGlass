using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Generic SwiftUI-style fluent modifiers for SeaGlass optics, theming, metrics, and accessibility.
/// Every modifier is generic: <c>T Modifier&lt;T&gt;(this T element, ...) where T : FrameworkElement</c>
/// so it chains on EVERY control catalog-wide and returns the concrete control type.
/// </summary>
public static class SeaGlassOpticsExtensions
{
    // =========================================================================
    // Core Optical & Theming Modifiers (Bare Nouns / Verbs)
    // =========================================================================

    /// <summary>Applies a curated SeaGlass optical material.</summary>
    public static T Material<T>(this T element, SeaGlassMaterial material) where T : FrameworkElement
    {
        if (element is SeaGlassSurface surface)
            surface.Material = material;
        else
            SeaGlassTheme.SetMaterial(element, material);
        return element;
    }

    /// <summary>Alias for Material(material).</summary>
    public static T WithMaterial<T>(this T element, SeaGlassMaterial material) where T : FrameworkElement =>
        element.Material(material);

    /// <summary>Applies a curated Material 3 Expressive palette tone and optional variant.</summary>
    public static T Tone<T>(this T element, SeaGlassTone tone, SeaGlassVariant? variant = null) where T : FrameworkElement
    {
        if (element is SeaGlassSurface surface)
        {
            surface.Tone = tone;
            if (variant.HasValue) surface.Variant = variant.Value;
        }
        else
        {
            SeaGlassTheme.SetTone(element, tone);
            if (variant.HasValue) SeaGlassTheme.SetVariant(element, variant.Value);
        }
        return element;
    }

    /// <summary>Alias for Tone(tone, variant).</summary>
    public static T WithTone<T>(this T element, SeaGlassTone tone, SeaGlassVariant? variant = null) where T : FrameworkElement =>
        element.Tone(tone, variant);

    /// <summary>Applies a semantic variant role (Filled, Elevated, Outlined, Text, Tonal, Destructive).</summary>
    public static T Variant<T>(this T element, SeaGlassVariant variant) where T : FrameworkElement
    {
        if (element is SeaGlassSurface surface)
            surface.Variant = variant;
        else
            SeaGlassTheme.SetVariant(element, variant);
        return element;
    }

    /// <summary>Alias for Variant(variant).</summary>
    public static T WithVariant<T>(this T element, SeaGlassVariant variant) where T : FrameworkElement =>
        element.Variant(variant);

    /// <summary>Applies a metric sizing token (Compact, Small, Medium, Large, Icon).</summary>
    public static T Size<T>(this T element, SeaGlassControlSize size) where T : FrameworkElement
    {
        if (element is SeaGlassSurface surface)
            surface.Size = size;
        else
            SeaGlassTheme.SetSize(element, size);
        return element;
    }

    /// <summary>Alias for Size(size).</summary>
    public static T WithSize<T>(this T element, SeaGlassControlSize size) where T : FrameworkElement =>
        element.Size(size);

    /// <summary>Applies a custom tint color and optional density.</summary>
    public static T Tint<T>(this T element, Windows.UI.Color color, double? density = null) where T : FrameworkElement
    {
        if (element is SeaGlassSurface surface)
        {
            surface.Tint = color;
            if (density.HasValue) surface.TintDensity = density.Value;
        }
        else
        {
            SeaGlassTheme.SetTint(element, color);
            if (density.HasValue) SeaGlassTheme.SetTintDensity(element, density.Value);
        }
        return element;
    }

    /// <summary>Alias for Tint(color, density).</summary>
    public static T WithTint<T>(this T element, Windows.UI.Color color, double? density = null) where T : FrameworkElement =>
        element.Tint(color, density);

    /// <summary>Applies a custom ARGB tint color integer and optional density.</summary>
    public static T Tint<T>(this T element, int argb, float? density = null) where T : FrameworkElement
    {
        Windows.UI.Color c = Windows.UI.Color.FromArgb(
            (byte)((argb >> 24) & 0xFF),
            (byte)((argb >> 16) & 0xFF),
            (byte)((argb >> 8) & 0xFF),
            (byte)(argb & 0xFF));
        return element.Tint(c, density.HasValue ? (double)density.Value : null);
    }

    /// <summary>Alias for Tint(argb, density).</summary>
    public static T WithTint<T>(this T element, int argb, float? density = null) where T : FrameworkElement =>
        element.Tint(argb, density);

    /// <summary>Overrides surface coarseness (0.0 = clear water, 1.0 = heavy sand frosting).</summary>
    public static T Coarseness<T>(this T element, double coarseness) where T : FrameworkElement
    {
        double clamped = Math.Clamp(coarseness, 0.0, 1.0);
        if (element is SeaGlassSurface surface)
            surface.Coarseness = clamped;
        else
            SeaGlassTheme.SetCoarseness(element, clamped);
        return element;
    }

    /// <summary>Alias for Coarseness(coarseness).</summary>
    public static T WithCoarseness<T>(this T element, double coarseness) where T : FrameworkElement =>
        element.Coarseness(coarseness);

    /// <summary>Overrides volumetric tint density (0.0 = clear, 1.0 = saturated pigment).</summary>
    public static T TintDensity<T>(this T element, double tintDensity) where T : FrameworkElement
    {
        double clamped = Math.Clamp(tintDensity, 0.0, 1.0);
        if (element is SeaGlassSurface surface)
            surface.TintDensity = clamped;
        else
            SeaGlassTheme.SetTintDensity(element, clamped);
        return element;
    }

    /// <summary>Alias for TintDensity(tintDensity).</summary>
    public static T WithTintDensity<T>(this T element, double tintDensity) where T : FrameworkElement =>
        element.TintDensity(tintDensity);

    /// <summary>Overrides glass slab physical thickness in DP (Beer-Lambert optical path length).</summary>
    public static T ThicknessDp<T>(this T element, double thicknessDp) where T : FrameworkElement
    {
        double clamped = Math.Clamp(thicknessDp, 1.0, 64.0);
        if (element is SeaGlassSurface surface)
            surface.ThicknessDp = clamped;
        else
            SeaGlassTheme.SetThicknessDp(element, clamped);
        return element;
    }

    /// <summary>Alias for ThicknessDp(thicknessDp).</summary>
    public static T WithThicknessDp<T>(this T element, double thicknessDp) where T : FrameworkElement =>
        element.ThicknessDp(thicknessDp);

    /// <summary>Alias for ThicknessDp(thicknessDp).</summary>
    public static T WithThickness<T>(this T element, double thicknessDp) where T : FrameworkElement =>
        element.ThicknessDp(thicknessDp);

    /// <summary>Overrides index of refraction (1.0 = air, 1.54 = sea glass, 2.0 = flint glass).</summary>
    public static T Ior<T>(this T element, double ior) where T : FrameworkElement
    {
        double clamped = Math.Clamp(ior, 1.0, 2.5);
        if (element is SeaGlassSurface surface)
            surface.Ior = clamped;
        return element;
    }

    /// <summary>Alias for Ior(ior).</summary>
    public static T WithIor<T>(this T element, double ior) where T : FrameworkElement =>
        element.Ior(ior);

    /// <summary>Overrides chalky rough-Fresnel rim illumination scatter (0.0 = dark, 1.0 = chalky glow).</summary>
    public static T RimChalkiness<T>(this T element, double rimChalkiness) where T : FrameworkElement
    {
        double clamped = Math.Clamp(rimChalkiness, 0.0, 1.0);
        if (element is SeaGlassSurface surface)
            surface.RimChalkiness = clamped;
        return element;
    }

    /// <summary>Alias for RimChalkiness(rimChalkiness).</summary>
    public static T WithRimChalkiness<T>(this T element, double rimChalkiness) where T : FrameworkElement =>
        element.RimChalkiness(rimChalkiness);

    /// <summary>Overrides uniform corner radius in DP.</summary>
    public static T CornerRadius<T>(this T element, double uniformRadiusDp) where T : FrameworkElement
    {
        CornerRadius cr = new(uniformRadiusDp);
        if (element is Control ctrl) ctrl.CornerRadius = cr;
        else if (element is Border border) border.CornerRadius = cr;
        else if (element is Grid grid) grid.CornerRadius = cr;
        else if (element is StackPanel sp) sp.CornerRadius = cr;
        return element;
    }

    /// <summary>Alias for CornerRadius(uniformRadiusDp).</summary>
    public static T WithCornerRadius<T>(this T element, double uniformRadiusDp) where T : FrameworkElement =>
        element.CornerRadius(uniformRadiusDp);

    /// <summary>Overrides asymmetric 4-corner radii in DP.</summary>
    public static T CornerRadius<T>(this T element, CornerRadius cornerRadius) where T : FrameworkElement
    {
        if (element is Control ctrl) ctrl.CornerRadius = cornerRadius;
        else if (element is Border border) border.CornerRadius = cornerRadius;
        else if (element is Grid grid) grid.CornerRadius = cornerRadius;
        else if (element is StackPanel sp) sp.CornerRadius = cornerRadius;
        return element;
    }

    /// <summary>Alias for CornerRadius(cornerRadius).</summary>
    public static T WithCornerRadius<T>(this T element, CornerRadius cornerRadius) where T : FrameworkElement =>
        element.CornerRadius(cornerRadius);

    /// <summary>Configures adaptive concentric corner optics.</summary>
    public static T CornerAdaptation<T>(
        this T element,
        SeaGlassCornerAdaptationMode mode,
        SeaGlassCornerMask? mask = null,
        float proximityThresholdDp = 36f,
        float fullMatchDistanceDp = 6f) where T : FrameworkElement
    {
        if (element is SeaGlassSurface surface)
        {
            surface.CornerAdaptationMode = mode;
            surface.CornerMask = mask ?? SeaGlassCornerMask.All;
            surface.CornerProximityThresholdDp = proximityThresholdDp;
            surface.CornerFullMatchDistanceDp = fullMatchDistanceDp;
        }
        return element;
    }

    /// <summary>Alias for CornerAdaptation(...).</summary>
    public static T WithCornerAdaptation<T>(
        this T element,
        SeaGlassCornerAdaptationMode mode,
        SeaGlassCornerMask? mask = null,
        float proximityThresholdDp = 36f,
        float fullMatchDistanceDp = 6f) where T : FrameworkElement =>
        element.CornerAdaptation(mode, mask, proximityThresholdDp, fullMatchDistanceDp);

    // =========================================================================
    // Common Universal Modifiers (Padding, Margin, Sizing, Accessibility)
    // =========================================================================

    public static T Padding<T>(this T element, Thickness padding) where T : FrameworkElement
    {
        if (element is Control ctrl) ctrl.Padding = padding;
        else if (element is StackPanel sp) sp.Padding = padding;
        else if (element is Grid grid) grid.Padding = padding;
        else if (element is Border border) border.Padding = padding;
        return element;
    }

    public static T Padding<T>(this T element, double uniform) where T : FrameworkElement =>
        element.Padding(new Thickness(uniform));

    public static T Padding<T>(this T element, double horizontal, double vertical) where T : FrameworkElement =>
        element.Padding(new Thickness(horizontal, vertical, horizontal, vertical));

    public static T Margin<T>(this T element, Thickness margin) where T : FrameworkElement
    {
        element.Margin = margin;
        return element;
    }

    public static T Margin<T>(this T element, double uniform) where T : FrameworkElement =>
        element.Margin(new Thickness(uniform));

    public static T Margin<T>(this T element, double horizontal, double vertical) where T : FrameworkElement =>
        element.Margin(new Thickness(horizontal, vertical, horizontal, vertical));

    public static T Width<T>(this T element, double width) where T : FrameworkElement
    {
        element.Width = width;
        return element;
    }

    public static T Height<T>(this T element, double height) where T : FrameworkElement
    {
        element.Height = height;
        return element;
    }

    public static T Frame<T>(this T element, double width, double height) where T : FrameworkElement
    {
        element.Width = width;
        element.Height = height;
        return element;
    }

    public static T Enabled<T>(this T element, bool isEnabled = true) where T : Control
    {
        element.IsEnabled = isEnabled;
        return element;
    }

    public static T Visible<T>(this T element, bool isVisible = true) where T : UIElement
    {
        element.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        return element;
    }

    public static T AccessibleName<T>(this T element, string name) where T : DependencyObject
    {
        AutomationProperties.SetName(element, name);
        return element;
    }

    public static T ToolTip<T>(this T element, object tooltip) where T : FrameworkElement
    {
        ToolTipService.SetToolTip(element, tooltip);
        return element;
    }

    // =========================================================================
    // Semantic Shorthand Variant Helpers
    // =========================================================================
    public static T Filled<T>(this T element) where T : FrameworkElement => element.Variant(SeaGlassVariant.Filled);
    public static T Elevated<T>(this T element) where T : FrameworkElement => element.Variant(SeaGlassVariant.Elevated);
    public static T Outlined<T>(this T element) where T : FrameworkElement => element.Variant(SeaGlassVariant.Outlined);
    public static T TextVariant<T>(this T element) where T : FrameworkElement => element.Variant(SeaGlassVariant.Text);
    public static T Tonal<T>(this T element) where T : FrameworkElement => element.Variant(SeaGlassVariant.Tonal);
    public static T Destructive<T>(this T element) where T : FrameworkElement => element.Variant(SeaGlassVariant.Destructive);

    // =========================================================================
    // Semantic Shorthand Sizing Helpers
    // =========================================================================
    public static T Compact<T>(this T element) where T : FrameworkElement => element.Size(SeaGlassControlSize.Compact);
    public static T Small<T>(this T element) where T : FrameworkElement => element.Size(SeaGlassControlSize.Small);
    public static T Medium<T>(this T element) where T : FrameworkElement => element.Size(SeaGlassControlSize.Medium);
    public static T Large<T>(this T element) where T : FrameworkElement => element.Size(SeaGlassControlSize.Large);

    // =========================================================================
    // Semantic Shorthand Tone Helpers
    // =========================================================================
    public static T Mint<T>(this T element) where T : FrameworkElement => element.Tone(SeaGlassTone.SeafoamMint);
    public static T Cobalt<T>(this T element) where T : FrameworkElement => element.Tone(SeaGlassTone.CobaltTide);
    public static T Amber<T>(this T element) where T : FrameworkElement => element.Tone(SeaGlassTone.BeachAmber);
    public static T Lavender<T>(this T element) where T : FrameworkElement => element.Tone(SeaGlassTone.SeaLavender);
    public static T Rose<T>(this T element) where T : FrameworkElement => element.Tone(SeaGlassTone.RoseQuartzShell);
    public static T Kelp<T>(this T element) where T : FrameworkElement => element.Tone(SeaGlassTone.KelpForest);
    public static T MilkGlass<T>(this T element) where T : FrameworkElement => element.Tone(SeaGlassTone.MilkGlassFrost);
    public static T Obsidian<T>(this T element) where T : FrameworkElement => element.Tone(SeaGlassTone.ObsidianTide);
}
