using System;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Circular or square expressive SeaGlass icon button.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_IconPresenter", Type = typeof(ContentPresenter))]
[TemplatePart(Name = "PART_ContentPresenter", Type = typeof(ContentPresenter))]
public class SeaGlassIconButton : SeaGlassButton
{
    public static readonly DependencyProperty GlyphProperty =
        DependencyProperty.Register(
            nameof(Glyph),
            typeof(SeaGlassGlyph),
            typeof(SeaGlassIconButton),
            new PropertyMetadata(SeaGlassGlyph.Sparkles, OnGlyphChanged));

    public static readonly DependencyProperty SelectedProperty =
        DependencyProperty.Register(
            nameof(Selected),
            typeof(bool),
            typeof(SeaGlassIconButton),
            new PropertyMetadata(false, OnSelectedChanged));

    public SeaGlassGlyph Glyph
    {
        get => (SeaGlassGlyph)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public bool Selected
    {
        get => (bool)GetValue(SelectedProperty);
        set => SetValue(SelectedProperty, value);
    }

    public float ButtonSizeDp { get; } = 44f;

    public SeaGlassIconButton()
    {
        DefaultStyleKey = typeof(SeaGlassIconButton);
        Size = SeaGlassControlSize.Icon;
        SizePx = new Vector2(44f, 44f);
        BasePaddingPx = Vector4.Zero;
    }

    public SeaGlassIconButton(
        SeaGlassGlyph glyph,
        Action? onClick = null,
        SeaGlassStageState? stageState = null,
        bool selected = false,
        float sizeDp = 44f,
        SeaGlassMaterial? material = null)
        : base(
            label: SeaGlassIcon.ToUnicodeSymbol(glyph),
            onClick: onClick,
            stageState: stageState,
            material: material ?? (selected
                ? SeaGlassTheme.Current.Palette.PrimaryGlass
                : SeaGlassTheme.Current.Palette.SurfaceGlass),
            leadingGlyph: glyph)
    {
        DefaultStyleKey = typeof(SeaGlassIconButton);
        Glyph = glyph;
        ButtonSizeDp = sizeDp;
        SizePx = new Vector2(sizeDp, sizeDp);
        BasePaddingPx = Vector4.Zero;
        SetSelected(selected, material);
    }

    public static SeaGlassIconButton Create(SeaGlassGlyph glyph, Action? onClick = null) =>
        new SeaGlassIconButton(glyph, onClick);

    private static void OnGlyphChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassIconButton btn && e.NewValue is SeaGlassGlyph g)
        {
            btn.LeadingGlyph = g;
            btn.IconGlyph = g;
            btn.Label = SeaGlassIcon.ToUnicodeSymbol(g);
        }
    }

    private static void OnSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassIconButton btn && e.NewValue is bool sel)
        {
            btn.SetSelected(sel);
        }
    }

    public void SetSelected(bool selected, SeaGlassMaterial? customMaterial = null)
    {
        Selected = selected;
        SeaGlassMaterial baseMat = customMaterial ?? (selected
            ? SeaGlassTheme.Current.Palette.PrimaryGlass
            : SeaGlassTheme.Current.Palette.SurfaceGlass);
        Material = baseMat with
        {
            CornerRadiusDp = selected ? 16f : ButtonSizeDp * 0.5f,
            ThicknessDp = selected ? 18f : 13f
        };
    }

    private Grid? _root;
    public Grid? RootGrid => _root;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _root = GetTemplateChild("PART_Root") as Grid;
    }
}

