using System;
using System.Numerics;
using ICommand = global::System.Windows.Input.ICommand;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// iOS-Style Live Activity / Dynamic Island Expandable SeaGlass Capsule.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_CompactHost", Type = typeof(StackPanel))]
[TemplatePart(Name = "PART_ExpandedHost", Type = typeof(Grid))]
public class SeaGlassLiveActivityPill : SeaGlassSurface
{
    private float _expandWidthProgress;
    private float _expandWidthVel;
    private float _expandHeightProgress;
    private float _expandHeightVel;
    private readonly SeaGlassMaterial _baseMaterial;

    public readonly record struct stringTrailingMetric(string Value, string Unit);

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(SeaGlassLiveActivityPill),
            new PropertyMetadata("HLSL SM 5.0 · 60 FPS"));

    public static readonly DependencyProperty SubtitleProperty =
        DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(SeaGlassLiveActivityPill),
            new PropertyMetadata("Zero-Copy CompositionVisualSurface Active"));

    public static readonly DependencyProperty ExpandedProperty =
        DependencyProperty.Register(nameof(Expanded), typeof(bool), typeof(SeaGlassLiveActivityPill),
            new PropertyMetadata(false, (d, e) =>
            {
                var pill = (SeaGlassLiveActivityPill)d;
                bool exp = (bool)e.NewValue;
                pill._expandWidthProgress = exp ? 1f : 0f;
                pill._expandHeightProgress = exp ? 1f : 0f;
                pill.UpdateHostsVisibility();
                pill.ExpandedChanged?.Invoke(pill, exp);
            }));

    public static readonly DependencyProperty MetricValueProperty =
        DependencyProperty.Register(nameof(MetricValue), typeof(string), typeof(SeaGlassLiveActivityPill),
            new PropertyMetadata("0.42"));

    public static readonly DependencyProperty MetricUnitProperty =
        DependencyProperty.Register(nameof(MetricUnit), typeof(string), typeof(SeaGlassLiveActivityPill),
            new PropertyMetadata("ms"));

    public static readonly DependencyProperty ToggleCommandProperty =
        DependencyProperty.Register(nameof(ToggleCommand), typeof(ICommand), typeof(SeaGlassLiveActivityPill),
            new PropertyMetadata(null));

    public static readonly DependencyProperty StatusTextProperty =
        DependencyProperty.Register(nameof(StatusText), typeof(string), typeof(SeaGlassLiveActivityPill),
            new PropertyMetadata("HLSL SM 5.0 · 60 FPS"));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public string StatusText
    {
        get => (string)GetValue(StatusTextProperty);
        set => SetValue(StatusTextProperty, value);
    }

    public bool Expanded
    {
        get => (bool)GetValue(ExpandedProperty);
        set => SetValue(ExpandedProperty, value);
    }

    public string MetricValue
    {
        get => (string)GetValue(MetricValueProperty);
        set => SetValue(MetricValueProperty, value);
    }

    public string MetricUnit
    {
        get => (string)GetValue(MetricUnitProperty);
        set => SetValue(MetricUnitProperty, value);
    }

    public ICommand? ToggleCommand
    {
        get => (ICommand?)GetValue(ToggleCommandProperty);
        set => SetValue(ToggleCommandProperty, value);
    }

    public stringTrailingMetric MetricText
    {
        get => new(MetricValue, MetricUnit);
        set
        {
            MetricValue = value.Value;
            MetricUnit = value.Unit;
        }
    }

    public float ExpandWidthProgress => Math.Clamp(_expandWidthProgress, 0f, 1.18f);
    public float ExpandHeightProgress => Math.Clamp(_expandHeightProgress, 0f, 1.18f);
    public float ExpandProgress => Math.Clamp((_expandWidthProgress + _expandHeightProgress) * 0.5f, 0f, 1.18f);

    public event TypedEventHandler<SeaGlassLiveActivityPill, bool>? ExpandedChanged;

    public SeaGlassLiveActivityPill()
    {
        DefaultStyleKey = typeof(SeaGlassLiveActivityPill);
        IsTabStop = true;
        _baseMaterial = Material;
        SizePx = new Vector2(236f, 44f);
        OnClick = () => SetExpanded(!Expanded);
    }

    public SeaGlassLiveActivityPill(
        string title = "HLSL SM 5.0 · 60 FPS",
        string subtitle = "Zero-Copy CompositionVisualSurface Active",
        bool expanded = false,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? material = null)
        : base(
            stageState: stageState ?? SeaGlassStageHost.AmbientStageState,
            material: material ?? (SeaGlassTheme.Current.Palette.ElevatedGlass with
            {
                CornerRadiusDp = 28f,
                ThicknessDp = 18f,
                RimChalkiness = 0.88f
            }))
    {
        DefaultStyleKey = typeof(SeaGlassLiveActivityPill);
        _baseMaterial = Material;
        Title = title;
        Subtitle = subtitle;
        Expanded = expanded;
        _expandWidthProgress = expanded ? 1f : 0f;
        _expandHeightProgress = expanded ? 1f : 0f;
        SizePx = expanded ? new Vector2(380f, 128f) : new Vector2(236f, 44f);
        OnClick = () => SetExpanded(!Expanded);
    }

    public void SetExpanded(bool expanded)
    {
        Expanded = expanded;
        ToggleCommand?.Execute(expanded);
    }

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        float target = Expanded ? 1f : 0f;
        SeaGlassSpringSpec widthSpring = new(DampingRatio: 0.66f, Stiffness: 440f);
        SeaGlassSpringSpec heightSpring = new(DampingRatio: 0.64f, Stiffness: 390f);
        widthSpring.Step(ref _expandWidthProgress, ref _expandWidthVel, target, dtSeconds);
        heightSpring.Step(ref _expandHeightProgress, ref _expandHeightVel, target, dtSeconds);

        float pw = Math.Clamp(_expandWidthProgress, 0f, 1.15f);
        float ph = Math.Clamp(_expandHeightProgress, 0f, 1.15f);
        float p = Math.Clamp((pw + ph) * 0.5f, 0f, 1f);
        SizePx = new Vector2(236f + (380f - 236f) * pw, 44f + (128f - 44f) * ph);
        Material = (_baseMaterial with { CornerRadiusDp = 22f + 12f * p }).Deaged(4f * p * (1f - p));

        return EvaluateFrame(dtSeconds, activeTheme);
    }

    private Grid? _root;
    private Border? _glassHost;
    private StackPanel? _compactHost;
    private Grid? _expandedHost;

    public StackPanel? CompactHost => _compactHost;
    public Grid? ExpandedHost => _expandedHost;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _root = GetTemplateChild("PART_Root") as Grid;
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _compactHost = GetTemplateChild("PART_CompactHost") as StackPanel;
        _expandedHost = GetTemplateChild("PART_ExpandedHost") as Grid;

        UpdateHostsVisibility();
    }

    private void UpdateHostsVisibility()
    {
        if (_compactHost != null) _compactHost.Visibility = Expanded ? Visibility.Collapsed : Visibility.Visible;
        if (_expandedHost != null) _expandedHost.Visibility = Expanded ? Visibility.Visible : Visibility.Collapsed;
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new SeaGlass.WinUI.Automation.SeaGlassLiveActivityPillAutomationPeer(this);
}

