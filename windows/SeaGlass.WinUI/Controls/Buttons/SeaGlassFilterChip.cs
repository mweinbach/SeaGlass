using System;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Material 3 Expressive Filter Chip that smoothly expands to reveal a leading checkmark when selected.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_CheckmarkPresenter", Type = typeof(ContentPresenter))]
[TemplatePart(Name = "PART_IconPresenter", Type = typeof(ContentPresenter))]
[TemplatePart(Name = "PART_LabelPresenter", Type = typeof(TextBlock))]
[TemplatePart(Name = "PART_ContentPresenter", Type = typeof(ContentPresenter))]
public class SeaGlassFilterChip : SeaGlassButton
{
    public static readonly DependencyProperty SelectedProperty =
        DependencyProperty.Register(
            nameof(Selected),
            typeof(bool),
            typeof(SeaGlassFilterChip),
            new PropertyMetadata(false, OnSelectedChanged));

    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register(
            nameof(IsSelected),
            typeof(bool),
            typeof(SeaGlassFilterChip),
            new PropertyMetadata(false, OnIsSelectedChanged));

    public static readonly DependencyProperty ShowLeadingCheckProperty =
        DependencyProperty.Register(
            nameof(ShowLeadingCheck),
            typeof(bool),
            typeof(SeaGlassFilterChip),
            new PropertyMetadata(true));

    public bool Selected
    {
        get => (bool)GetValue(SelectedProperty);
        set => SetValue(SelectedProperty, value);
    }

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public bool ShowLeadingCheck
    {
        get => (bool)GetValue(ShowLeadingCheckProperty);
        set => SetValue(ShowLeadingCheckProperty, value);
    }

    public SeaGlassMaterial SelectedMaterial { get; set; }
    public SeaGlassMaterial UnselectedMaterial { get; set; }

    public event EventHandler<bool>? SelectedChanged;

    public SeaGlassFilterChip()
    {
        DefaultStyleKey = typeof(SeaGlassFilterChip);
        SelectedMaterial = SeaGlassTheme.Current.Palette.PrimaryGlass;
        UnselectedMaterial = SeaGlassTheme.Current.Palette.SurfaceGlass;
        SizePx = new Vector2(124f, 38f);
        BasePaddingPx = new Vector4(14f, 9f, 14f, 9f);
        IsTabStop = true;
    }

    public SeaGlassFilterChip(
        string label,
        bool selected,
        Action? onClick = null,
        SeaGlassStageState? stageState = null,
        SeaGlassGlyph? leadingGlyph = null,
        SeaGlassMaterial? selectedMaterial = null,
        SeaGlassMaterial? unselectedMaterial = null)
        : base(label, onClick, stageState, leadingGlyph: selected ? SeaGlassGlyph.Check : leadingGlyph)
    {
        DefaultStyleKey = typeof(SeaGlassFilterChip);
        SelectedMaterial = selectedMaterial ?? SeaGlassTheme.Current.Palette.PrimaryGlass;
        UnselectedMaterial = unselectedMaterial ?? SeaGlassTheme.Current.Palette.SurfaceGlass;
        SizePx = new Vector2(124f, 38f);
        BasePaddingPx = new Vector4(14f, 9f, 14f, 9f);
        SetSelected(selected, leadingGlyph);
    }

    public static SeaGlassFilterChip Create(string label = "", bool selected = false) =>
        new SeaGlassFilterChip(label, selected);

    private static void OnSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassFilterChip chip && e.NewValue is bool sel)
        {
            chip.IsSelected = sel;
            chip.SetSelected(sel);
            chip.SelectedChanged?.Invoke(chip, sel);
        }
    }

    private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassFilterChip chip && e.NewValue is bool sel)
        {
            chip.Selected = sel;
            chip.SetSelected(sel);
            chip.SelectedChanged?.Invoke(chip, sel);
        }
    }

    public void SetSelected(bool selected, SeaGlassGlyph? fallbackGlyph = null)
    {
        Selected = selected;
        LeadingGlyph = selected ? (ShowLeadingCheck ? SeaGlassGlyph.Check : null) : fallbackGlyph;
        Material = selected
            ? SelectedMaterial with { CornerRadiusDp = 22f, ThicknessDp = 16f }
            : UnselectedMaterial with { CornerRadiusDp = 14f, ThicknessDp = 10f };
        UpdateCheckmarkVisibility();
    }

    private Grid? _root;
    private ContentPresenter? _checkmarkPresenter;

    public ContentPresenter? CheckmarkPresenter => _checkmarkPresenter;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _root = GetTemplateChild("PART_Root") as Grid;
        _checkmarkPresenter = GetTemplateChild("PART_CheckmarkPresenter") as ContentPresenter;
        UpdateCheckmarkVisibility();
    }

    private void UpdateCheckmarkVisibility()
    {
        if (_checkmarkPresenter != null)
        {
            _checkmarkPresenter.Visibility = Selected ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new Automation.SeaGlassFilterChipAutomationPeer(this);
}

