using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// iOS 27 Optical Magnifier Loupe &amp; Precision Scrubbing Bar.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_TrackWell", Type = typeof(Border))]
[TemplatePart(Name = "PART_LoupeLens", Type = typeof(Border))]
[TemplatePart(Name = "PART_TimecodePresenter", Type = typeof(TextBlock))]
public class SeaGlassMagnifierScrubber : SeaGlassSurface
{
    private float _loupeLiftProgress = 1f;
    private float _loupeLiftVel;

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(float), typeof(SeaGlassMagnifierScrubber),
            new PropertyMetadata(0.0f, (d, e) =>
            {
                var s = (SeaGlassMagnifierScrubber)d;
                float v = (float)e.NewValue;
                s.ValueChanged?.Invoke(s, v);
                s.OnValueChange?.Invoke(v);
            }));

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(SeaGlassMagnifierScrubber),
            new PropertyMetadata("OPTICAL TIMELINE & LOUPE SCRUBBER"));

    public static readonly DependencyProperty LoupeActiveProperty =
        DependencyProperty.Register(nameof(LoupeActive), typeof(bool), typeof(SeaGlassMagnifierScrubber),
            new PropertyMetadata(true));

    public static readonly DependencyProperty TrackMaterialProperty =
        DependencyProperty.Register(nameof(TrackMaterial), typeof(SeaGlassMaterial), typeof(SeaGlassMagnifierScrubber),
            new PropertyMetadata(null));

    public static readonly DependencyProperty LoupeMaterialProperty =
        DependencyProperty.Register(nameof(LoupeMaterial), typeof(SeaGlassMaterial), typeof(SeaGlassMagnifierScrubber),
            new PropertyMetadata(null));

    public float Value
    {
        get => (float)GetValue(ValueProperty);
        set => SetValue(ValueProperty, Math.Clamp(value, 0f, 1f));
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public bool LoupeActive
    {
        get => (bool)GetValue(LoupeActiveProperty);
        set => SetValue(LoupeActiveProperty, value);
    }

    public SeaGlassMaterial TrackMaterial
    {
        get => (SeaGlassMaterial)GetValue(TrackMaterialProperty);
        set => SetValue(TrackMaterialProperty, value);
    }

    public SeaGlassMaterial LoupeMaterial
    {
        get => (SeaGlassMaterial)GetValue(LoupeMaterialProperty);
        set => SetValue(LoupeMaterialProperty, value);
    }

    public Action<float>? OnValueChange { get; set; }
    public float LoupeLiftProgress => Math.Clamp(_loupeLiftProgress, 0f, 1.15f);

    public event TypedEventHandler<SeaGlassMagnifierScrubber, float>? ValueChanged;

    public SeaGlassMagnifierScrubber()
    {
        DefaultStyleKey = typeof(SeaGlassMagnifierScrubber);
        IsTabStop = true;
        TrackMaterial = SeaGlassTheme.Current.Palette.SurfaceGlass;
        LoupeMaterial = SeaGlassTheme.Current.Palette.PrimaryGlass.Deaged(1.0f);
        SizePx = new Vector2(460f, 112f);
    }

    public SeaGlassMagnifierScrubber(
        float value,
        Action<float> onValueChange,
        SeaGlassStageState? stageState = null,
        string label = "OPTICAL TIMELINE & LOUPE SCRUBBER",
        SeaGlassMaterial? trackMaterial = null,
        SeaGlassMaterial? loupeMaterial = null)
        : base(
            stageState: stageState ?? SeaGlassStageHost.AmbientStageState,
            material: trackMaterial ?? SeaGlassTheme.Current.Palette.SurfaceGlass)
    {
        DefaultStyleKey = typeof(SeaGlassMagnifierScrubber);
        Value = Math.Clamp(value, 0f, 1f);
        OnValueChange = onValueChange;
        Label = label;
        TrackMaterial = trackMaterial ?? SeaGlassTheme.Current.Palette.SurfaceGlass;
        LoupeMaterial = (loupeMaterial ?? SeaGlassTheme.Current.Palette.PrimaryGlass).Deaged(1.0f);
        _loupeLiftProgress = 1f;
        SizePx = new Vector2(460f, 112f);
    }

    public void SetValue(float value)
    {
        Value = Math.Clamp(value, 0f, 1f);
    }

    public string FormatValueLabel() =>
        string.Create(CultureInfo.InvariantCulture, $"Frame {(int)MathF.Round(Value * 240f):D3} · {Value * 100f:F0}%");

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        float density = MathF.Max(0.5f, activeTheme.DpiScale);
        SeaGlassSpringSpec liftSpring = new(DampingRatio: 0.62f, Stiffness: 420f);
        liftSpring.Step(ref _loupeLiftProgress, ref _loupeLiftVel, LoupeActive ? 1f : 0f, dtSeconds);

        float lift = Math.Clamp(_loupeLiftProgress, 0f, 1.15f);
        float w = MathF.Max(180f, SizePx.X);
        float h = MathF.Max(80f, SizePx.Y);
        float trackHalfH = 15f * density;
        float trackCenterY = h - 22f * density;
        float loupeHalfW = 46f * density * (0.45f + 0.55f * Math.Clamp(lift, 0f, 1f));
        float loupeHalfH = 22f * density * (0.45f + 0.55f * Math.Clamp(lift, 0f, 1f));
        float thumbX = loupeHalfW + (w - 2f * loupeHalfW) * Value;
        float loupeCenterY = trackCenterY - (trackHalfH + loupeHalfH + 8f * density) * lift;

        SeaGlassMaterial activeLoupe = LoupeMaterial.Deaged(Math.Clamp(lift, 0f, 1f));
        List<SeaGlassSdfEngine.SdfNode> nodes =
        [
            new SeaGlassSdfEngine.SdfNode(
                centerX: w * 0.5f,
                centerY: trackCenterY,
                halfWidth: w * 0.5f,
                halfHeight: trackHalfH,
                uniformCornerRadius: trackHalfH,
                cornerSmoothing: 2.5f,
                tintColorArgb: TrackMaterial.TintColorArgb,
                thicknessPx: TrackMaterial.ThicknessDp * density),
            new SeaGlassSdfEngine.SdfNode(
                centerX: thumbX,
                centerY: loupeCenterY,
                halfWidth: loupeHalfW,
                halfHeight: loupeHalfH,
                uniformCornerRadius: loupeHalfH,
                cornerSmoothing: 2.5f,
                tintColorArgb: activeLoupe.TintColorArgb,
                thicknessPx: activeLoupe.ThicknessDp * density)
        ];

        return EvaluateFrame(dtSeconds, activeTheme, nodes, smoothRadiusDp: 24f);
    }

    private Grid? _root;
    private Border? _glassHost;
    private Border? _trackWell;
    private Border? _loupeLens;
    private TextBlock? _timecodePresenter;

    public Border? TrackWell => _trackWell;
    public Border? LoupeLens => _loupeLens;
    public TextBlock? TimecodePresenter => _timecodePresenter;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _root = GetTemplateChild("PART_Root") as Grid;
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _trackWell = GetTemplateChild("PART_TrackWell") as Border;
        _loupeLens = GetTemplateChild("PART_LoupeLens") as Border;
        _timecodePresenter = GetTemplateChild("PART_TimecodePresenter") as TextBlock;
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new SeaGlass.WinUI.Automation.SeaGlassMagnifierScrubberAutomationPeer(this);
}

