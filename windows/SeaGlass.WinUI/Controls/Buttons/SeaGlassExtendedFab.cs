using System;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Expandable Liquid SeaGlass Floating Action Button (SeaGlassExtendedFab) that morphs
/// between a compact 56dp expressive icon orb and a wide pill with icon + label.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_IconPresenter", Type = typeof(ContentPresenter))]
[TemplatePart(Name = "PART_LabelPresenter", Type = typeof(TextBlock))]
[TemplatePart(Name = "PART_ContentPresenter", Type = typeof(ContentPresenter))]
public class SeaGlassExtendedFab : SeaGlassButton
{
    public static readonly DependencyProperty ExpandedProperty =
        DependencyProperty.Register(
            nameof(Expanded),
            typeof(bool),
            typeof(SeaGlassExtendedFab),
            new PropertyMetadata(false, OnExpandedChanged));

    public static readonly DependencyProperty IsExpandedProperty =
        DependencyProperty.Register(
            nameof(IsExpanded),
            typeof(bool),
            typeof(SeaGlassExtendedFab),
            new PropertyMetadata(false, OnIsExpandedChanged));

    public static readonly DependencyProperty GlyphProperty =
        DependencyProperty.Register(
            nameof(Glyph),
            typeof(SeaGlassGlyph),
            typeof(SeaGlassExtendedFab),
            new PropertyMetadata(SeaGlassGlyph.Plus, OnGlyphChanged));

    public bool Expanded
    {
        get => (bool)GetValue(ExpandedProperty);
        set => SetValue(ExpandedProperty, value);
    }

    public bool IsExpanded
    {
        get => (bool)GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    public SeaGlassGlyph Glyph
    {
        get => (SeaGlassGlyph)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public SeaGlassExtendedFab()
    {
        DefaultStyleKey = typeof(SeaGlassExtendedFab);
        SetExpanded(false);
    }

    public SeaGlassExtendedFab(
        string text,
        SeaGlassGlyph glyph,
        bool expanded,
        Action? onClick = null,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? material = null)
        : base(text, onClick, stageState, material, glyph)
    {
        DefaultStyleKey = typeof(SeaGlassExtendedFab);
        Glyph = glyph;
        SetExpanded(expanded, material);
    }

    private static void OnExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassExtendedFab fab && e.NewValue is bool exp)
        {
            fab.IsExpanded = exp;
            fab.SetExpanded(exp);
        }
    }

    private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassExtendedFab fab && e.NewValue is bool exp)
        {
            fab.Expanded = exp;
            fab.SetExpanded(exp);
        }
    }

    private static void OnGlyphChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassExtendedFab fab && e.NewValue is SeaGlassGlyph g)
        {
            fab.LeadingGlyph = g;
            fab.IconGlyph = g;
        }
    }

    public void SetExpanded(bool expanded, SeaGlassMaterial? baseMaterial = null)
    {
        Expanded = expanded;
        SeaGlassMaterial mat = baseMaterial ?? Material;
        float defaultRadius = expanded ? 28f : 20f;
        Material = mat with
        {
            CornerRadiusDp = mat.CornerRadiusDp < 16f ? mat.CornerRadiusDp : defaultRadius,
            ThicknessDp = 20f,
            RimChalkiness = 0.88f
        };
        SizePx = expanded ? new Vector2(172f, 56f) : new Vector2(56f, 56f);
    }

    private Grid? _root;
    public Grid? RootGrid => _root;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _root = GetTemplateChild("PART_Root") as Grid;
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new SeaGlass.WinUI.Automation.SeaGlassExtendedFabAutomationPeer(this);
}

