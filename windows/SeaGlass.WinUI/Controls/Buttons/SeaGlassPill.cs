using System;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

public enum SeaGlassPillKind
{
    Filter,
    Suggestion,
    Assist,
    Input
}

/// <summary>
/// Expressive SeaGlass pill chip that smoothly morphs its tint, coarseness, corner radius,
/// and thickness between unselected and selected states.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_IconPresenter", Type = typeof(ContentPresenter))]
[TemplatePart(Name = "PART_LabelPresenter", Type = typeof(TextBlock))]
[TemplatePart(Name = "PART_ContentPresenter", Type = typeof(ContentPresenter))]
public class SeaGlassPill : SeaGlassButton
{
    public static readonly DependencyProperty SelectedProperty =
        DependencyProperty.Register(
            nameof(Selected),
            typeof(bool),
            typeof(SeaGlassPill),
            new PropertyMetadata(false, OnSelectedChanged));

    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register(
            nameof(IsSelected),
            typeof(bool),
            typeof(SeaGlassPill),
            new PropertyMetadata(false, OnIsSelectedChanged));

    public static readonly DependencyProperty PillKindProperty =
        DependencyProperty.Register(
            nameof(PillKind),
            typeof(SeaGlassPillKind),
            typeof(SeaGlassPillKind),
            new PropertyMetadata(SeaGlassPillKind.Filter));

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

    public SeaGlassPillKind PillKind
    {
        get => (SeaGlassPillKind)GetValue(PillKindProperty);
        set => SetValue(PillKindProperty, value);
    }

    public SeaGlassMaterial SelectedMaterial { get; set; }
    public SeaGlassMaterial UnselectedMaterial { get; set; }

    public SeaGlassPill()
    {
        DefaultStyleKey = typeof(SeaGlassPill);
        SelectedMaterial = SeaGlassTheme.Current.Palette.PrimaryGlass;
        UnselectedMaterial = SeaGlassTheme.Current.Palette.SurfaceGlass;
        SizePx = new Vector2(112f, 40f);
        BasePaddingPx = new Vector4(18f, 10f, 18f, 10f);
        IsTabStop = true;
    }

    public SeaGlassPill(
        string label,
        bool selected,
        Action? onClick = null,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? selectedMaterial = null,
        SeaGlassMaterial? unselectedMaterial = null)
        : base(label, onClick, stageState)
    {
        DefaultStyleKey = typeof(SeaGlassPill);
        SelectedMaterial = selectedMaterial ?? SeaGlassTheme.Current.Palette.PrimaryGlass;
        UnselectedMaterial = unselectedMaterial ?? SeaGlassTheme.Current.Palette.SurfaceGlass;
        SizePx = new Vector2(112f, 40f);
        BasePaddingPx = new Vector4(18f, 10f, 18f, 10f);
        SetSelected(selected);
        SnapMaterial(Material);
    }

    public SeaGlassPillKind Kind
    {
        get => PillKind;
        set => PillKind = value;
    }

    public event EventHandler<bool>? SelectedChanged;

    public static SeaGlassPill Create(string label = "", bool selected = false) =>
        new SeaGlassPill(label, selected);

    private static void OnSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassPill pill && e.NewValue is bool sel)
        {
            pill.IsSelected = sel;
            pill.SetSelected(sel);
            pill.SelectedChanged?.Invoke(pill, sel);
        }
    }

    private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassPill pill && e.NewValue is bool sel)
        {
            pill.Selected = sel;
            pill.SetSelected(sel);
        }
    }

    public void SetSelected(bool selected)
    {
        Selected = selected;
        Material = selected
            ? SelectedMaterial with
            {
                CornerRadiusDp = SelectedMaterial.CornerRadiusDp < 16f ? SelectedMaterial.CornerRadiusDp : 26f,
                ThicknessDp = 16f
            }
            : UnselectedMaterial with
            {
                CornerRadiusDp = UnselectedMaterial.CornerRadiusDp < 16f ? UnselectedMaterial.CornerRadiusDp : 18f,
                ThicknessDp = 10f
            };
    }

    private Grid? _root;
    public Grid? RootGrid => _root;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _root = GetTemplateChild("PART_Root") as Grid;
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new Automation.SeaGlassPillAutomationPeer(this);
}

