using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// 3-State Cascaded Composer Model & Effort Picker Morph.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_PillHeader", Type = typeof(Border))]
[TemplatePart(Name = "PART_PopoverBody", Type = typeof(Border))]
[TemplatePart(Name = "PART_EffortSlider", Type = typeof(Slider))]
public class SeaGlassCascadedModelMorph : SeaGlassSurface
{
    private float _spatialStage;
    private float _spatialVel;
    private float _effectsStage;
    private float _effectsVel;

    public static readonly DependencyProperty StageProperty =
        DependencyProperty.Register(
            nameof(Stage),
            typeof(int),
            typeof(SeaGlassCascadedModelMorph),
            new PropertyMetadata(0, OnStageChanged));

    public static readonly DependencyProperty SelectedModelProperty =
        DependencyProperty.Register(
            nameof(SelectedModel),
            typeof(string),
            typeof(SeaGlassCascadedModelMorph),
            new PropertyMetadata("Gemini 4 Argon EAP"));

    public static readonly DependencyProperty EffortFractionProperty =
        DependencyProperty.Register(
            nameof(EffortFraction),
            typeof(double),
            typeof(SeaGlassCascadedModelMorph),
            new PropertyMetadata(0.75));

    public int Stage
    {
        get => (int)GetValue(StageProperty);
        set => SetValue(StageProperty, value);
    }

    public string SelectedModel
    {
        get => (string)GetValue(SelectedModelProperty);
        set => SetValue(SelectedModelProperty, value);
    }

    public double EffortFraction
    {
        get => (double)GetValue(EffortFractionProperty);
        set => SetValue(EffortFractionProperty, value);
    }

    public SeaGlassMaterial PillMaterial { get; set; }
    public SeaGlassMaterial PopoverMaterial { get; set; }

    public IReadOnlyList<(string Name, string Provider)> Models { get; } =
    [
        ("Gemini 4 Argon EAP", "Google · 1M Context"),
        ("Claude Haiku 5.5", "OpenCode Go"),
        ("Exo Free", "OpenCode Zen"),
        ("Ling 3.1 Flash Free", "OpenCode Zen"),
        ("GPT-6.1 Sol", "OpenAI")
    ];

    public string EffortLabel => EffortFraction switch
    {
        < 0.25 => "Low",
        < 0.60 => "Medium",
        < 0.88 => "High",
        _ => "Max"
    };

    public float SpatialStage => _spatialStage;
    public float EffectsStage => _effectsStage;

    public SeaGlassCascadedModelMorph()
    {
        DefaultStyleKey = typeof(SeaGlassCascadedModelMorph);
        IsTabStop = true;
        PillMaterial = SeaGlassTheme.Current.Palette.SurfaceGlass;
        PopoverMaterial = SeaGlassTheme.Current.Palette.ElevatedGlass;
    }

    public SeaGlassCascadedModelMorph(
        int stage = 0,
        string selectedModel = "Gemini 4 Argon EAP",
        float effortFraction = 0.75f,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? pillMaterial = null,
        SeaGlassMaterial? popoverMaterial = null)
        : base(stageState: stageState)
    {
        DefaultStyleKey = typeof(SeaGlassCascadedModelMorph);
        Stage = Math.Clamp(stage, 0, 2);
        _spatialStage = Stage;
        _effectsStage = Stage;
        SelectedModel = selectedModel;
        EffortFraction = Math.Clamp(effortFraction, 0f, 1f);
        PillMaterial = pillMaterial ?? SeaGlassTheme.Current.Palette.SurfaceGlass;
        PopoverMaterial = popoverMaterial ?? SeaGlassTheme.Current.Palette.ElevatedGlass;
    }

    private static void OnStageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassCascadedModelMorph morph && e.NewValue is int st)
        {
            morph.SetStage(st);
        }
    }

    public void SetStage(int stage) => Stage = Math.Clamp(stage, 0, 2);

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        SeaGlassSpringSpec spatialSpring = new(DampingRatio: 0.66f, Stiffness: 400f);
        spatialSpring.Step(ref _spatialStage, ref _spatialVel, Stage, dtSeconds);
        activeTheme.MotionScheme.DefaultEffects.Step(ref _effectsStage, ref _effectsVel, Stage, dtSeconds);

        bool isUpper = _effectsStage > 1.0f || (Stage == 2 && _spatialStage > 0.85f);
        float localSpatial = isUpper ? (_spatialStage - 1f) : _spatialStage;
        float localEffects = Math.Clamp(isUpper ? (_effectsStage - 1f) : _effectsStage, 0f, 1f);

        Vector2 startSize = isUpper ? new Vector2(292f, 132f) : new Vector2(220f, 40f);
        Vector2 endSize = isUpper ? new Vector2(308f, 320f) : new Vector2(292f, 132f);

        SizePx = new Vector2(
            MathF.Max(36f, startSize.X + (endSize.X - startSize.X) * localSpatial),
            MathF.Max(32f, startSize.Y + (endSize.Y - startSize.Y) * localSpatial));

        float energy = Math.Clamp(4f * localEffects * (1f - localEffects), 0f, 1f);
        Material = SeaGlassShapeMorph.ComputeDynamicFlexMaterial(
            startMaterial: isUpper ? PopoverMaterial : PillMaterial,
            endMaterial: PopoverMaterial,
            progress: localEffects,
            areaExpansionRatio: 1f + energy * 0.8f) with
        {
            CornerRadiusDp = (isUpper ? 28f : (20f + 8f * localEffects)) + 24f * energy,
            ThicknessDp = 16f + 6f * localEffects + 4f * energy
        };

        return EvaluateFrame(dtSeconds, activeTheme);
    }

    private Grid? _root;
    private Border? _glassHost;
    private Border? _pillHeader;
    private Border? _popoverBody;
    private Slider? _effortSlider;

    public Border? PillHeader => _pillHeader;
    public Border? PopoverBody => _popoverBody;
    public Slider? EffortSlider => _effortSlider;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        if (_effortSlider != null)
        {
            _effortSlider.ValueChanged -= OnEffortSliderValueChanged;
        }

        _root = GetTemplateChild("PART_Root") as Grid;
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _pillHeader = GetTemplateChild("PART_PillHeader") as Border;
        _popoverBody = GetTemplateChild("PART_PopoverBody") as Border;
        _effortSlider = GetTemplateChild("PART_EffortSlider") as Slider;

        if (_effortSlider != null)
        {
            _effortSlider.Value = EffortFraction;
            _effortSlider.ValueChanged += OnEffortSliderValueChanged;
        }
    }

    private void OnEffortSliderValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        EffortFraction = e.NewValue;
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new SeaGlass.WinUI.Automation.SeaGlassCascadedModelMorphAutomationPeer(this);
}

