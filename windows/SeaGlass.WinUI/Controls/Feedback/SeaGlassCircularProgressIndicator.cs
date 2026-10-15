using System;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Frosted SeaGlass Circular Medallion Progress Indicator with animated arc sweep.
/// Note: Circular progress arc and indeterminate ring are rendered natively via PART_ProgressRing;
/// PART_TrackArc and PART_SweepArc contracts have been reconciled and removed.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_ProgressRing", Type = typeof(ProgressRing))]
public class SeaGlassCircularProgressIndicator : SeaGlassSurface
{
    private float _animatedProgress;
    private float _vel;

    public static readonly DependencyProperty ProgressProperty =
        DependencyProperty.Register(
            nameof(Progress),
            typeof(double),
            typeof(SeaGlassCircularProgressIndicator),
            new PropertyMetadata(0.0));

    public static readonly DependencyProperty IsIndeterminateProperty =
        DependencyProperty.Register(
            nameof(IsIndeterminate),
            typeof(bool),
            typeof(SeaGlassCircularProgressIndicator),
            new PropertyMetadata(false));

    public static readonly DependencyProperty SweepAngleDegreesProperty =
        DependencyProperty.Register(
            nameof(SweepAngleDegrees),
            typeof(double),
            typeof(SeaGlassCircularProgressIndicator),
            new PropertyMetadata(0.0));

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

    public double SweepAngleDegrees
    {
        get => (double)GetValue(SweepAngleDegreesProperty);
        set => SetValue(SweepAngleDegreesProperty, value);
    }

    public float DiameterDp { get; set; } = 52f;
    public float AnimatedProgress => Math.Clamp(_animatedProgress, 0.05f, 1f);

    public SeaGlassCircularProgressIndicator()
    {
        DefaultStyleKey = typeof(SeaGlassCircularProgressIndicator);
        SizePx = new Vector2(52f, 52f);
    }

    public SeaGlassCircularProgressIndicator(
        float progress,
        SeaGlassStageState? stageState = null,
        float sizeDp = 52f,
        SeaGlassMaterial? material = null)
        : base(
            stageState: stageState,
            material: (material ?? SeaGlassTheme.Current.Palette.SurfaceGlass) with
            {
                CornerRadiusDp = sizeDp * 0.5f,
                ThicknessDp = 15f
            })
    {
        DefaultStyleKey = typeof(SeaGlassCircularProgressIndicator);
        Progress = Math.Clamp(progress, 0.05f, 1f);
        _animatedProgress = (float)Progress;
        DiameterDp = sizeDp;
        SweepAngleDegrees = 360.0 * Progress;
        SizePx = new Vector2(sizeDp, sizeDp);
    }

    private Grid? _root;
    private Border? _glassHost;
    private ProgressRing? _progressRing;

    public ProgressRing? ProgressRing => _progressRing;
    public Border? GlassHost => _glassHost;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _root = GetTemplateChild("PART_Root") as Grid;
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _progressRing = GetTemplateChild("PART_ProgressRing") as ProgressRing;
    }

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        activeTheme.MotionScheme.DefaultSpatial.Step(ref _animatedProgress, ref _vel, Math.Clamp((float)Progress, 0.05f, 1f), dtSeconds);
        SweepAngleDegrees = 360.0 * AnimatedProgress;
        return EvaluateFrame(dtSeconds, activeTheme);
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new SeaGlass.WinUI.Automation.SeaGlassCircularProgressIndicatorAutomationPeer(this);
}
