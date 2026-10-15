using System;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Frosted SeaGlass Linear Progress Indicator with an Oklab gradient liquid fill bar.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_Track", Type = typeof(Border))]
[TemplatePart(Name = "PART_FillBar", Type = typeof(Border))]
[TemplatePart(Name = "PART_IndeterminateShimmer", Type = typeof(Border))]
public class SeaGlassLinearProgressIndicator : SeaGlassSurface
{
    private float _animatedProgress;
    private float _vel;

    public static readonly DependencyProperty ProgressProperty =
        DependencyProperty.Register(
            nameof(Progress),
            typeof(double),
            typeof(SeaGlassLinearProgressIndicator),
            new PropertyMetadata(0.0));

    public static readonly DependencyProperty IsIndeterminateProperty =
        DependencyProperty.Register(
            nameof(IsIndeterminate),
            typeof(bool),
            typeof(SeaGlassLinearProgressIndicator),
            new PropertyMetadata(false, (d, e) => ((SeaGlassLinearProgressIndicator)d).UpdateIndeterminateVisibility()));

    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public bool IsIndeterminate
    {
        get => (bool)GetValue(IsIndeterminateProperty);
        set => SetValue(IsIndeterminateProperty, value);
    }

    public SeaGlassMaterial TrackMaterial { get; set; }
    public SeaGlassMaterial FillMaterial { get; set; }
    public float AnimatedProgress => Math.Clamp(_animatedProgress, 0.04f, 1f);

    public SeaGlassLinearProgressIndicator()
    {
        DefaultStyleKey = typeof(SeaGlassLinearProgressIndicator);
        TrackMaterial = SeaGlassTheme.Current.Palette.SurfaceGlass;
        FillMaterial = SeaGlassTheme.Current.Palette.PrimaryGlass;
        SizePx = new Vector2(360f, 14f);
    }

    public SeaGlassLinearProgressIndicator(
        float progress,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? trackMaterial = null,
        SeaGlassMaterial? fillMaterial = null)
        : base(
            stageState: stageState,
            material: (trackMaterial ?? SeaGlassTheme.Current.Palette.SurfaceGlass) with
            {
                CornerRadiusDp = 7f,
                ThicknessDp = 10f
            })
    {
        DefaultStyleKey = typeof(SeaGlassLinearProgressIndicator);
        Progress = Math.Clamp(progress, 0.04f, 1f);
        _animatedProgress = (float)Progress;
        TrackMaterial = trackMaterial ?? SeaGlassTheme.Current.Palette.SurfaceGlass;
        FillMaterial = fillMaterial ?? SeaGlassTheme.Current.Palette.PrimaryGlass;
        SizePx = new Vector2(360f, 14f);
    }

    private Grid? _root;
    private Border? _glassHost;
    private Border? _track;
    private Border? _fillBar;
    private Border? _indeterminateShimmer;

    public Border? Track => _track;
    public Border? FillBar => _fillBar;
    public Border? IndeterminateShimmer => _indeterminateShimmer;
    public Border? GlassHost => _glassHost;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _root = GetTemplateChild("PART_Root") as Grid;
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _track = GetTemplateChild("PART_Track") as Border;
        _fillBar = GetTemplateChild("PART_FillBar") as Border;
        _indeterminateShimmer = GetTemplateChild("PART_IndeterminateShimmer") as Border;
        UpdateIndeterminateVisibility();
    }

    private void UpdateIndeterminateVisibility()
    {
        if (_indeterminateShimmer != null)
        {
            _indeterminateShimmer.Visibility = IsIndeterminate ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        activeTheme.MotionScheme.DefaultSpatial.Step(ref _animatedProgress, ref _vel, Math.Clamp((float)Progress, 0.04f, 1f), dtSeconds);
        return EvaluateFrame(dtSeconds, activeTheme);
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new SeaGlass.WinUI.Automation.SeaGlassLinearProgressIndicatorAutomationPeer(this);
}
