using System;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Frosted SeaGlass RadioButton with an animated inner droplet that blooms when selected.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_OuterRing", Type = typeof(Border))]
[TemplatePart(Name = "PART_InnerDroplet", Type = typeof(Border))]
[TemplatePart(Name = "PART_ContentPresenter", Type = typeof(ContentPresenter))]
[TemplatePart(Name = "PART_FocusVisual", Type = typeof(Border))]
public class SeaGlassRadioButton : SeaGlassSurface
{
    private float _spatialProgress;
    private float _spatialVel;
    private float _tintProgress;
    private float _tintVel;

    public static readonly DependencyProperty SelectedProperty =
        DependencyProperty.Register(
            nameof(Selected),
            typeof(bool),
            typeof(SeaGlassRadioButton),
            new PropertyMetadata(false, OnSelectedChanged));

    public static readonly DependencyProperty IsCheckedProperty =
        DependencyProperty.Register(
            nameof(IsChecked),
            typeof(bool),
            typeof(SeaGlassRadioButton),
            new PropertyMetadata(false, OnIsCheckedChanged));

    public static readonly DependencyProperty GroupNameProperty =
        DependencyProperty.Register(
            nameof(GroupName),
            typeof(string),
            typeof(SeaGlassRadioButton),
            new PropertyMetadata(string.Empty));

    public bool Selected
    {
        get => (bool)GetValue(SelectedProperty);
        set => SetValue(SelectedProperty, value);
    }

    public bool IsChecked
    {
        get => (bool)GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    public string GroupName
    {
        get => (string)GetValue(GroupNameProperty);
        set => SetValue(GroupNameProperty, value);
    }

    public Action? OnSelect { get; set; }
    public SeaGlassMaterial SelectedMaterial { get; set; }
    public SeaGlassMaterial UnselectedMaterial { get; set; }
    public float InnerDropletScale => MathF.Max(0f, _spatialProgress);

    public event EventHandler<bool>? Checked;

    public SeaGlassRadioButton()
    {
        DefaultStyleKey = typeof(SeaGlassRadioButton);
        SelectedMaterial = SeaGlassTheme.Current.Palette.PrimaryGlass;
        UnselectedMaterial = SeaGlassTheme.Current.Palette.SurfaceGlass;
        SizePx = new Vector2(28f, 28f);
        OnClick = () => SetSelected(true);
        IsTabStop = true;
    }

    public SeaGlassRadioButton(
        bool selected,
        Action? onSelect = null,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? selectedMaterial = null,
        SeaGlassMaterial? unselectedMaterial = null)
        : base(stageState: stageState, onClick: onSelect)
    {
        DefaultStyleKey = typeof(SeaGlassRadioButton);
        Selected = selected;
        IsChecked = selected;
        _spatialProgress = selected ? 1f : 0f;
        _tintProgress = selected ? 1f : 0f;
        OnSelect = onSelect;
        SelectedMaterial = selectedMaterial ?? SeaGlassTheme.Current.Palette.PrimaryGlass;
        UnselectedMaterial = unselectedMaterial ?? SeaGlassTheme.Current.Palette.SurfaceGlass;
        SizePx = new Vector2(28f, 28f);
        OnClick = () => SetSelected(true);
    }

    private Grid? _root;
    private Border? _glassHost;
    private Border? _outerRing;
    private Border? _innerDroplet;
    private ContentPresenter? _contentPresenter;
    private Border? _focusVisual;

    public Border? OuterRing => _outerRing;
    public Border? InnerDroplet => _innerDroplet;
    public Border? GlassHost => _glassHost;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _root = GetTemplateChild("PART_Root") as Grid;
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _outerRing = GetTemplateChild("PART_OuterRing") as Border;
        _innerDroplet = GetTemplateChild("PART_InnerDroplet") as Border;
        _contentPresenter = GetTemplateChild("PART_ContentPresenter") as ContentPresenter;
        _focusVisual = GetTemplateChild("PART_FocusVisual") as Border;

        UpdateVisualState(false);
    }

    private void UpdateVisualState(bool useTransitions)
    {
        VisualStateManager.GoToState(this, Selected ? "Checked" : "Unchecked", useTransitions);
        if (_innerDroplet != null)
        {
            _innerDroplet.Visibility = Selected ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private static void OnSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassRadioButton rb && e.NewValue is bool b)
        {
            rb.IsChecked = b;
            rb.UpdateVisualState(true);
            if (b) rb.OnSelect?.Invoke();
            rb.Checked?.Invoke(rb, b);
        }
    }

    private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassRadioButton rb && e.NewValue is bool b)
        {
            rb.Selected = b;
        }
    }

    public void SetSelected(bool selected)
    {
        Selected = selected;
    }

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        float target = Selected ? 1f : 0f;
        activeTheme.MotionScheme.FastSpatial.Step(ref _spatialProgress, ref _spatialVel, target, dtSeconds);
        activeTheme.MotionScheme.DefaultEffects.Step(ref _tintProgress, ref _tintVel, target, dtSeconds);

        float tp = Math.Clamp(_tintProgress, 0f, 1f);
        float energy = Math.Clamp(4f * tp * (1f - tp), 0f, 1f);
        Material = (SeaGlassMaterial.Lerp(UnselectedMaterial, SelectedMaterial, tp) with
        {
            CornerRadiusDp = 14f,
            ThicknessDp = 12f + 4f * tp
        }).Deaged(energy);

        return EvaluateFrame(dtSeconds, activeTheme);
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new Automation.SeaGlassRadioButtonAutomationPeer(this);
}
