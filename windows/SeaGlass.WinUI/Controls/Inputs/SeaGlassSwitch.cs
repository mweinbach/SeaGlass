using System;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Tactile Liquid SeaGlass Toggle Switch with 3D water-drop lens thumb.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_TrackWell", Type = typeof(Border))]
[TemplatePart(Name = "PART_ThumbLens", Type = typeof(Border))]
[TemplatePart(Name = "PART_FocusVisual", Type = typeof(Border))]
public class SeaGlassSwitch : SeaGlassSurface
{
    private float _thumbProgress;
    private float _thumbVel;
    private float _tintProgress;
    private float _tintVel;

    public static readonly DependencyProperty CheckedProperty =
        DependencyProperty.Register(
            nameof(Checked),
            typeof(bool),
            typeof(SeaGlassSwitch),
            new PropertyMetadata(false, OnCheckedChanged));

    public static readonly DependencyProperty IsOnProperty =
        DependencyProperty.Register(
            nameof(IsOn),
            typeof(bool),
            typeof(SeaGlassSwitch),
            new PropertyMetadata(false, OnIsOnChanged));

    public bool Checked
    {
        get => (bool)GetValue(CheckedProperty);
        set => SetValue(CheckedProperty, value);
    }

    public bool IsOn
    {
        get => (bool)GetValue(IsOnProperty);
        set => SetValue(IsOnProperty, value);
    }

    public Action<bool>? OnCheckedChange { get; set; }
    public SeaGlassMaterial CheckedMaterial { get; set; }
    public SeaGlassMaterial UncheckedMaterial { get; set; }

    public float ThumbProgress => _thumbProgress;
    public float MotionStretch => Math.Clamp(MathF.Abs((Checked ? 1f : 0f) - _thumbProgress), 0f, 1f);
    public float ThumbScaleX => 1f + 0.28f * MotionStretch;
    public float ThumbScaleY => 1f - 0.14f * MotionStretch;

    public event EventHandler<bool>? Toggled;

    public SeaGlassSwitch()
    {
        DefaultStyleKey = typeof(SeaGlassSwitch);
        CheckedMaterial = SeaGlassTheme.Current.Palette.PrimaryGlass;
        UncheckedMaterial = SeaGlassTheme.Current.Palette.SurfaceGlass;
        SizePx = new Vector2(56f, 32f);
        OnClick = () => Toggle();
        IsTabStop = true;
    }

    public SeaGlassSwitch(
        bool isChecked,
        Action<bool>? onCheckedChange = null,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? checkedMaterial = null,
        SeaGlassMaterial? uncheckedMaterial = null)
        : base(
            stageState: stageState,
            material: isChecked
                ? (checkedMaterial ?? SeaGlassTheme.Current.Palette.PrimaryGlass)
                : (uncheckedMaterial ?? SeaGlassTheme.Current.Palette.SurfaceGlass),
            contentRole: SeaGlassContentRole.TextAndIcons)
    {
        DefaultStyleKey = typeof(SeaGlassSwitch);
        Checked = isChecked;
        IsOn = isChecked;
        _thumbProgress = isChecked ? 1f : 0f;
        _tintProgress = isChecked ? 1f : 0f;
        OnCheckedChange = onCheckedChange;
        CheckedMaterial = checkedMaterial ?? SeaGlassTheme.Current.Palette.PrimaryGlass;
        UncheckedMaterial = uncheckedMaterial ?? SeaGlassTheme.Current.Palette.SurfaceGlass;
        SizePx = new Vector2(56f, 32f);
        OnClick = () => Toggle();
    }

    public static SeaGlassSwitch Create(bool isOn = false, Action<bool>? onCheckedChange = null) =>
        new SeaGlassSwitch(isOn, onCheckedChange);

    private Grid? _root;
    private Border? _glassHost;
    private Border? _trackWell;
    private Border? _thumbLens;
    private Border? _focusVisual;

    public Border? TrackWell => _trackWell;
    public Border? ThumbLens => _thumbLens;
    public Border? GlassHost => _glassHost;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _root = GetTemplateChild("PART_Root") as Grid;
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _trackWell = GetTemplateChild("PART_TrackWell") as Border;
        _thumbLens = GetTemplateChild("PART_ThumbLens") as Border;
        _focusVisual = GetTemplateChild("PART_FocusVisual") as Border;

        UpdateVisualState(false);
    }

    private void UpdateVisualState(bool useTransitions)
    {
        VisualStateManager.GoToState(this, Checked ? "Checked" : "Unchecked", useTransitions);
        if (_thumbLens != null)
        {
            _thumbLens.HorizontalAlignment = Checked ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            _thumbLens.Margin = Checked ? new Thickness(0, 0, 4, 0) : new Thickness(4, 0, 0, 0);
        }
    }

    private static void OnCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassSwitch sw && e.NewValue is bool b)
        {
            sw.IsOn = b;
            sw.UpdateVisualState(true);
            sw.OnCheckedChange?.Invoke(b);
            sw.Toggled?.Invoke(sw, b);
        }
    }

    private static void OnIsOnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassSwitch sw && e.NewValue is bool b)
        {
            sw.Checked = b;
        }
    }

    public void Toggle()
    {
        Checked = !Checked;
    }

    protected override void OnKeyDown(Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Windows.System.VirtualKey.Space || e.Key == Windows.System.VirtualKey.Enter)
        {
            Toggle();
            e.Handled = true;
        }
    }

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        float target = Checked ? 1f : 0f;
        activeTheme.MotionScheme.FastSpatial.Step(ref _thumbProgress, ref _thumbVel, target, dtSeconds);
        activeTheme.MotionScheme.DefaultEffects.Step(ref _tintProgress, ref _tintVel, target, dtSeconds);

        float tp = Math.Clamp(_tintProgress, 0f, 1f);
        float energy = Math.Clamp(4f * tp * (1f - tp) + MotionStretch * 0.65f, 0f, 1f);
        Material = (SeaGlassMaterial.Lerp(UncheckedMaterial, CheckedMaterial, tp) with
        {
            CornerRadiusDp = 18f,
            ThicknessDp = 12f
        }).Deaged(energy);

        return EvaluateFrame(dtSeconds, activeTheme);
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new Automation.SeaGlassSwitchAutomationPeer(this);
}
