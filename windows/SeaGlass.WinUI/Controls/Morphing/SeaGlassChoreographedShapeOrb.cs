using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Autonomous orb choreographing 20 shapes through normalized circle midpoints.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_ShapeCanvas", Type = typeof(Canvas))]
public class SeaGlassChoreographedShapeOrb : SeaGlassSurface
{
    private int _prevIndex;
    private int _targetIndex;
    private float _transitionProgress = 1f;
    private float _transitionVel;

    public static readonly DependencyProperty ChoreographyProgressProperty =
        DependencyProperty.Register(
            nameof(ChoreographyProgress),
            typeof(double),
            typeof(SeaGlassChoreographedShapeOrb),
            new PropertyMetadata(0.0));

    public double ChoreographyProgress
    {
        get => (double)GetValue(ChoreographyProgressProperty);
        set => SetValue(ChoreographyProgressProperty, value);
    }

    public IReadOnlyList<SeaGlassShapeStage> Stages { get; set; } = SeaGlassShapeMorph.DefaultPlayfulChoreography;
    public int TargetStageIndex => _targetIndex;
    public SeaGlassShapeStage CurrentTargetStage => Stages[_targetIndex];
    public ChoreographedShapeTransform CurrentTransform { get; private set; }
    public SeaGlassMaterial PrimaryMaterial { get; set; }
    public SeaGlassMaterial SecondaryMaterial { get; set; }

    public SeaGlassChoreographedShapeOrb()
    {
        DefaultStyleKey = typeof(SeaGlassChoreographedShapeOrb);
        IsTabStop = true;
        PrimaryMaterial = SeaGlassTheme.Current.Palette.PrimaryGlass;
        SecondaryMaterial = SeaGlassTheme.Current.Palette.TertiaryGlass;
        SizePx = new Vector2(116f, 116f);
        OnClick = () => AdvanceTo((_targetIndex + 1) % Stages.Count);
    }

    public SeaGlassChoreographedShapeOrb(
        SeaGlassStageState? stageState = null,
        IReadOnlyList<SeaGlassShapeStage>? stages = null,
        SeaGlassMaterial? primaryMaterial = null,
        SeaGlassMaterial? secondaryMaterial = null)
        : base(stageState: stageState)
    {
        DefaultStyleKey = typeof(SeaGlassChoreographedShapeOrb);
        Stages = stages ?? SeaGlassShapeMorph.DefaultPlayfulChoreography;
        if (Stages.Count < 2)
            throw new ArgumentException("stages must contain at least 2 keyframes", nameof(stages));

        PrimaryMaterial = primaryMaterial ?? SeaGlassTheme.Current.Palette.PrimaryGlass;
        SecondaryMaterial = secondaryMaterial ?? SeaGlassTheme.Current.Palette.TertiaryGlass;
        SizePx = new Vector2(116f, 116f);
        OnClick = () => AdvanceTo((_targetIndex + 1) % Stages.Count);
    }

    public void AdvanceTo(int nextIdx)
    {
        int count = Stages.Count;
        int clampedNext = ((nextIdx % count) + count) % count;
        if (clampedNext == _targetIndex && _transitionProgress > 0.9f)
        {
            _prevIndex = _targetIndex;
            _targetIndex = (_targetIndex + 1) % count;
        }
        else
        {
            _prevIndex = _targetIndex;
            _targetIndex = clampedNext;
        }
        _transitionProgress = 0f;
        _transitionVel = 0f;
    }

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        SeaGlassSpringSpec orbSpring = new(DampingRatio: 0.64f, Stiffness: 400f);
        orbSpring.Step(ref _transitionProgress, ref _transitionVel, 1f, dtSeconds);

        ChoreographyProgress = _transitionProgress;

        SeaGlassShapeStage startStage = Stages[_prevIndex];
        SeaGlassShapeStage endStage = Stages[_targetIndex];
        float rawP = _transitionProgress;

        ChoreographedShapeTransform choreo = SeaGlassShapeMorph.EvaluateChoreographedTransform(startStage, endStage, rawP);
        CurrentTransform = choreo;

        bool useAlt = (_targetIndex % 2) == 1;
        SeaGlassMaterial fromMat = useAlt ? PrimaryMaterial : SecondaryMaterial;
        SeaGlassMaterial toMat = useAlt ? SecondaryMaterial : PrimaryMaterial;

        Material = SeaGlassShapeMorph.ComputeDynamicFlexMaterial(
            startMaterial: fromMat,
            endMaterial: toMat,
            progress: Math.Clamp(rawP, 0f, 1f),
            velocityPxPerSec: choreo.MorphEnergy * 1500f,
            areaExpansionRatio: choreo.ScaleX * choreo.ScaleY);

        return EvaluateFrame(
            dtSeconds: dtSeconds,
            theme: activeTheme,
            customContourBuilder: (wPx, hPx, outPath) =>
                SeaGlassShapeMorph.BuildChoreographedPolygonPath(startStage, endStage, rawP, wPx, hPx, outPath));
    }

    private Grid? _root;
    private Border? _glassHost;
    private Canvas? _shapeCanvas;

    public Canvas? ShapeCanvas => _shapeCanvas;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _root = GetTemplateChild("PART_Root") as Grid;
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _shapeCanvas = GetTemplateChild("PART_ShapeCanvas") as Canvas;
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new SeaGlass.WinUI.Automation.SeaGlassChoreographedShapeOrbAutomationPeer(this);
}

