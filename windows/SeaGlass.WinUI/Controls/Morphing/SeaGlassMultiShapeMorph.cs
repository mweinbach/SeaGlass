using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Multi-Stage Material 3 Expressive Shape Sequence Morpher across 20 canonical shapes.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_MorphedPath", Type = typeof(Microsoft.UI.Xaml.Shapes.Path))]
public class SeaGlassMultiShapeMorph : SeaGlassSurface
{
    private float _animatedIndex;
    private float _indexVel;

    public static readonly DependencyProperty ActiveShapeIndexProperty =
        DependencyProperty.Register(
            nameof(ActiveShapeIndex),
            typeof(int),
            typeof(SeaGlassMultiShapeMorph),
            new PropertyMetadata(0, OnActiveShapeIndexChanged));

    public static readonly DependencyProperty CurrentShapeKindProperty =
        DependencyProperty.Register(
            nameof(CurrentShapeKind),
            typeof(SeaGlassShapeKind),
            typeof(SeaGlassMultiShapeMorph),
            new PropertyMetadata(SeaGlassShapeKind.Circle));

    public int ActiveShapeIndex
    {
        get => (int)GetValue(ActiveShapeIndexProperty);
        set => SetValue(ActiveShapeIndexProperty, value);
    }

    public SeaGlassShapeKind CurrentShapeKind
    {
        get => (SeaGlassShapeKind)GetValue(CurrentShapeKindProperty);
        set => SetValue(CurrentShapeKindProperty, value);
    }

    public IReadOnlyList<SeaGlassShapeKind> Shapes { get; set; } =
    [
        SeaGlassShapeKind.Circle,
        SeaGlassShapeKind.Squircle,
        SeaGlassShapeKind.Clover4,
        SeaGlassShapeKind.Heart,
        SeaGlassShapeKind.Sunny
    ];

    public float? ContinuousIndexOverride { get; set; }
    public SeaGlassMaterial PrimaryMaterial { get; set; }
    public SeaGlassMaterial SecondaryMaterial { get; set; }

    public SeaGlassShapeKind CurrentDisplayShape => Shapes[Math.Clamp(ActiveShapeIndex, 0, Shapes.Count - 1)];

    public SeaGlassMultiShapeMorph()
    {
        DefaultStyleKey = typeof(SeaGlassMultiShapeMorph);
        IsTabStop = true;
        PrimaryMaterial = SeaGlassTheme.Current.Palette.PrimaryGlass;
        SecondaryMaterial = SeaGlassTheme.Current.Palette.TertiaryGlass;
        SizePx = new Vector2(110f, 110f);
    }

    public SeaGlassMultiShapeMorph(
        IReadOnlyList<SeaGlassShapeKind> shapes,
        int activeShapeIndex = 0,
        SeaGlassStageState? stageState = null,
        Action? onClick = null,
        SeaGlassMaterial? primaryMaterial = null,
        SeaGlassMaterial? secondaryMaterial = null)
        : base(stageState: stageState, onClick: onClick)
    {
        DefaultStyleKey = typeof(SeaGlassMultiShapeMorph);
        if (shapes is null || shapes.Count == 0)
            throw new ArgumentException("shapes must not be empty", nameof(shapes));

        Shapes = shapes;
        ActiveShapeIndex = Math.Clamp(activeShapeIndex, 0, shapes.Count - 1);
        _animatedIndex = ActiveShapeIndex;
        CurrentShapeKind = CurrentDisplayShape;
        PrimaryMaterial = primaryMaterial ?? SeaGlassTheme.Current.Palette.PrimaryGlass;
        SecondaryMaterial = secondaryMaterial ?? SeaGlassTheme.Current.Palette.TertiaryGlass;
        SizePx = new Vector2(110f, 110f);
    }

    private static void OnActiveShapeIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassMultiShapeMorph morph && e.NewValue is int idx)
        {
            morph.CurrentShapeKind = morph.CurrentDisplayShape;
        }
    }

    public void SetActiveShapeIndex(int index) =>
        ActiveShapeIndex = Math.Clamp(index, 0, Shapes.Count - 1);

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        SeaGlassSpringSpec seqSpring = new(DampingRatio: 0.64f, Stiffness: 420f);
        seqSpring.Step(ref _animatedIndex, ref _indexVel, ActiveShapeIndex, dtSeconds);

        float effectiveIdx = ContinuousIndexOverride ?? _animatedIndex;
        float fractionalPart = MathF.Abs(effectiveIdx - MathF.Round(effectiveIdx));
        float colorWave = Math.Clamp(MathF.Sin(effectiveIdx * MathF.PI * 0.5f) * 0.5f + 0.5f, 0f, 1f);

        var blended = SeaGlassShapeMorph.ComputeDynamicFlexMaterial(
            startMaterial: PrimaryMaterial,
            endMaterial: SecondaryMaterial,
            progress: colorWave,
            velocityPxPerSec: fractionalPart * 1200f);
        Material = blended.Deaged(fractionalPart * 0.90f);

        return EvaluateFrame(
            dtSeconds: dtSeconds,
            theme: activeTheme,
            customContourBuilder: (wPx, hPx, outPath) =>
                SeaGlassShapeMorph.BuildSequenceMorphedPolygonPath(Shapes, effectiveIdx, wPx, hPx, outPath));
    }

    private Grid? _root;
    private Border? _glassHost;
    private Microsoft.UI.Xaml.Shapes.Path? _morphedPath;

    public Microsoft.UI.Xaml.Shapes.Path? MorphedPath => _morphedPath;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _root = GetTemplateChild("PART_Root") as Grid;
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _morphedPath = GetTemplateChild("PART_MorphedPath") as Microsoft.UI.Xaml.Shapes.Path;
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new SeaGlass.WinUI.Automation.SeaGlassMultiShapeMorphAutomationPeer(this);
}

