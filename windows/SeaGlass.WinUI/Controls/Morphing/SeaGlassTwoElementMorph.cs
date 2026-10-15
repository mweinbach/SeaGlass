using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

public enum SeaGlassMorphMode
{
    LiquidCoalesce,
    PolygonShapeMorph
}

public enum SeaGlassAnchorAlignment
{
    TopStart,
    TopCenter,
    TopEnd,
    CenterStart,
    Center,
    CenterEnd,
    BottomStart,
    BottomCenter,
    BottomEnd
}

/// <summary>
/// Dual-element shape-morphing glass container evaluating circle-midpoint transitions
/// and temporal de-aging across state changes.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_GlassCanvas", Type = typeof(Canvas))]
[TemplatePart(Name = "PART_CollapsedHost", Type = typeof(ContentPresenter))]
[TemplatePart(Name = "PART_ExpandedHost", Type = typeof(ContentPresenter))]
public class SeaGlassTwoElementMorph : SeaGlassSurface
{
    private float _spatialWidthProgress;
    private float _spatialWidthVel;
    private float _spatialHeightProgress;
    private float _spatialHeightVel;
    private float _effectsProgress;
    private float _effectsVel;

    public static readonly DependencyProperty ExpandedProperty =
        DependencyProperty.Register(
            nameof(Expanded),
            typeof(bool),
            typeof(SeaGlassTwoElementMorph),
            new PropertyMetadata(false, OnExpandedChanged));

    public static readonly DependencyProperty MorphProgressProperty =
        DependencyProperty.Register(
            nameof(MorphProgress),
            typeof(double),
            typeof(SeaGlassTwoElementMorph),
            new PropertyMetadata(0.0));

    public bool Expanded
    {
        get => (bool)GetValue(ExpandedProperty);
        set => SetValue(ExpandedProperty, value);
    }

    public double MorphProgress
    {
        get => (double)GetValue(MorphProgressProperty);
        set => SetValue(MorphProgressProperty, value);
    }

    public Vector2 CollapsedSizePx { get; set; }
    public Vector2 ExpandedSizePx { get; set; }
    public SeaGlassMaterial CollapsedMaterial { get; set; }
    public SeaGlassMaterial ExpandedMaterial { get; set; }
    public SeaGlassShapeKind CollapsedShape { get; set; }
    public SeaGlassShapeKind ExpandedShape { get; set; }
    public SeaGlassMorphMode MorphMode { get; set; }
    public SeaGlassAnchorAlignment AnchorAlignment { get; set; }

    public float SpatialWidthProgress => _spatialWidthProgress;
    public float SpatialHeightProgress => _spatialHeightProgress;
    public float SpatialProgress => (_spatialWidthProgress + _spatialHeightProgress) * 0.5f;
    public float EffectsProgress => _effectsProgress;
    public float InFlightEnergy => Math.Clamp(4f * _effectsProgress * (1f - _effectsProgress), 0f, 1f);
    public float CollapsedContentAlpha => Math.Clamp(1f - _effectsProgress * 1.55f, 0f, 1f);
    public float ExpandedContentAlpha => Math.Clamp((_effectsProgress - 0.06f) * 1.12f, 0f, 1f);
    public float CollapsedBlurRadiusPx => MathF.Max(0f, _effectsProgress * 13f);
    public float ExpandedBlurRadiusPx => MathF.Max(0f, (1f - _effectsProgress) * (1f - _effectsProgress) * 16f);

    public event EventHandler<bool>? ExpandedChanged;

    public SeaGlassTwoElementMorph()
    {
        DefaultStyleKey = typeof(SeaGlassTwoElementMorph);
        IsTabStop = true;
        CollapsedSizePx = new Vector2(44f, 44f);
        ExpandedSizePx = new Vector2(276f, 290f);
        CollapsedMaterial = SeaGlassTheme.Current.Palette.PrimaryGlass;
        ExpandedMaterial = SeaGlassTheme.Current.Palette.SurfaceGlass;
        CollapsedShape = SeaGlassShapeKind.Capsule;
        ExpandedShape = SeaGlassShapeKind.Squircle;
        MorphMode = SeaGlassMorphMode.LiquidCoalesce;
        AnchorAlignment = SeaGlassAnchorAlignment.TopEnd;
        SizePx = CollapsedSizePx;
    }

    public SeaGlassTwoElementMorph(
        bool expanded = false,
        SeaGlassStageState? stageState = null,
        Vector2? collapsedSizePx = null,
        Vector2? expandedSizePx = null,
        SeaGlassMaterial? collapsedMaterial = null,
        SeaGlassMaterial? expandedMaterial = null,
        SeaGlassShapeKind collapsedShape = SeaGlassShapeKind.Capsule,
        SeaGlassShapeKind expandedShape = SeaGlassShapeKind.Squircle,
        SeaGlassMorphMode morphMode = SeaGlassMorphMode.LiquidCoalesce,
        SeaGlassAnchorAlignment anchorAlignment = SeaGlassAnchorAlignment.TopEnd,
        Action? onToggle = null)
        : base(stageState: stageState, onClick: onToggle)
    {
        DefaultStyleKey = typeof(SeaGlassTwoElementMorph);
        Expanded = expanded;
        _spatialWidthProgress = expanded ? 1f : 0f;
        _spatialHeightProgress = expanded ? 1f : 0f;
        _effectsProgress = expanded ? 1f : 0f;
        CollapsedSizePx = collapsedSizePx ?? new Vector2(44f, 44f);
        ExpandedSizePx = expandedSizePx ?? new Vector2(276f, 290f);
        CollapsedMaterial = collapsedMaterial ?? SeaGlassTheme.Current.Palette.PrimaryGlass;
        ExpandedMaterial = expandedMaterial ?? SeaGlassTheme.Current.Palette.SurfaceGlass;
        CollapsedShape = collapsedShape;
        ExpandedShape = expandedShape;
        MorphMode = morphMode;
        AnchorAlignment = anchorAlignment;
        SizePx = expanded ? ExpandedSizePx : CollapsedSizePx;
    }

    private static void OnExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassTwoElementMorph morph && e.NewValue is bool exp)
        {
            morph.UpdateHostsVisibility();
            morph.ExpandedChanged?.Invoke(morph, exp);
        }
    }

    public void SetExpanded(bool expanded) => Expanded = expanded;
    public void ToggleExpanded() => Expanded = !Expanded;

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        float target = Expanded ? 1f : 0f;

        SeaGlassSpringSpec widthSpring = new(DampingRatio: 0.68f, Stiffness: 420f);
        SeaGlassSpringSpec heightSpring = new(DampingRatio: 0.64f, Stiffness: 390f);
        widthSpring.Step(ref _spatialWidthProgress, ref _spatialWidthVel, target, dtSeconds);
        heightSpring.Step(ref _spatialHeightProgress, ref _spatialHeightVel, target, dtSeconds);
        activeTheme.MotionScheme.DefaultEffects.Step(ref _effectsProgress, ref _effectsVel, target, dtSeconds);

        MorphProgress = _effectsProgress;

        float curW = MathF.Max(32f, CollapsedSizePx.X + (ExpandedSizePx.X - CollapsedSizePx.X) * _spatialWidthProgress);
        float curH = MathF.Max(32f, CollapsedSizePx.Y + (ExpandedSizePx.Y - CollapsedSizePx.Y) * _spatialHeightProgress);
        SizePx = new Vector2(curW, curH);

        float collapsedArea = MathF.Max(1f, CollapsedSizePx.X * CollapsedSizePx.Y);
        float expandedArea = MathF.Max(1f, ExpandedSizePx.X * ExpandedSizePx.Y);
        float currentAreaRatio = 1f + (expandedArea / collapsedArea - 1f) * _effectsProgress;
        float energy = InFlightEnergy;

        SeaGlassMaterial baseFlex = SeaGlassShapeMorph.ComputeDynamicFlexMaterial(
            startMaterial: CollapsedMaterial,
            endMaterial: ExpandedMaterial,
            progress: _effectsProgress,
            areaExpansionRatio: currentAreaRatio);

        Material = baseFlex.Deaged(energy) with
        {
            CornerRadiusDp = baseFlex.CornerRadiusDp + 22f * energy
        };

        List<SeaGlassSdfEngine.SdfNode>? coalescingNodes = null;
        if (MorphMode == SeaGlassMorphMode.LiquidCoalesce && energy > 0.04f)
        {
            float colW = MathF.Max(32f, CollapsedSizePx.X);
            float colH = MathF.Max(32f, CollapsedSizePx.Y);
            float anchorHalfW = MathF.Min(curW * 0.5f, colW * 0.5f * (1f - _effectsProgress * 0.18f));
            float anchorHalfH = MathF.Min(curH * 0.5f, colH * 0.5f * (1f - _effectsProgress * 0.18f));

            float anchorCenterX = AnchorAlignment switch
            {
                SeaGlassAnchorAlignment.TopEnd or SeaGlassAnchorAlignment.BottomEnd or SeaGlassAnchorAlignment.CenterEnd =>
                    MathF.Max(anchorHalfW, curW - anchorHalfW),
                SeaGlassAnchorAlignment.TopStart or SeaGlassAnchorAlignment.BottomStart or SeaGlassAnchorAlignment.CenterStart =>
                    anchorHalfW,
                _ => curW * 0.5f
            };
            float anchorCenterY = AnchorAlignment switch
            {
                SeaGlassAnchorAlignment.TopStart or SeaGlassAnchorAlignment.TopCenter or SeaGlassAnchorAlignment.TopEnd =>
                    anchorHalfH,
                SeaGlassAnchorAlignment.BottomStart or SeaGlassAnchorAlignment.BottomCenter or SeaGlassAnchorAlignment.BottomEnd =>
                    MathF.Max(anchorHalfH, curH - anchorHalfH),
                _ => curH * 0.5f
            };

            float targetHalfW = curW * 0.5f;
            float targetHalfH = curH * 0.5f;
            float ovalMidFlightRadius = MathF.Min(targetHalfW, targetHalfH) * (0.35f + 0.50f * energy);

            coalescingNodes =
            [
                new SeaGlassSdfEngine.SdfNode(
                    centerX: anchorCenterX,
                    centerY: anchorCenterY,
                    halfWidth: anchorHalfW,
                    halfHeight: anchorHalfH,
                    uniformCornerRadius: MathF.Min(anchorHalfW, anchorHalfH) * 0.95f,
                    cornerSmoothing: 2.4f,
                    tintColorArgb: CollapsedMaterial.TintColorArgb,
                    thicknessPx: Material.ThicknessDp * 2.8f),
                new SeaGlassSdfEngine.SdfNode(
                    centerX: curW * 0.5f,
                    centerY: curH * 0.5f,
                    halfWidth: targetHalfW,
                    halfHeight: targetHalfH,
                    uniformCornerRadius: MathF.Max(Material.CornerRadiusDp * 2f, ovalMidFlightRadius),
                    cornerSmoothing: 2.5f,
                    tintColorArgb: Material.TintColorArgb,
                    thicknessPx: Material.ThicknessDp * 2.8f)
            ];
        }

        Action<float, float, SeaGlassVectorPath2D>? polyBuilder = MorphMode == SeaGlassMorphMode.PolygonShapeMorph
            ? (wPx, hPx, outPath) => SeaGlassShapeMorph.BuildMorphedPolygonPath(
                CollapsedShape, ExpandedShape, _effectsProgress, wPx, hPx, outPath)
            : null;

        return EvaluateFrame(
            dtSeconds: dtSeconds,
            theme: activeTheme,
            nodesOverride: coalescingNodes,
            smoothRadiusDp: 38f * energy,
            customContourBuilder: polyBuilder);
    }

    private Grid? _root;
    private Border? _glassHost;
    private Canvas? _glassCanvas;
    private ContentPresenter? _collapsedHost;
    private ContentPresenter? _expandedHost;

    public Canvas? GlassCanvas => _glassCanvas;
    public ContentPresenter? CollapsedHost => _collapsedHost;
    public ContentPresenter? ExpandedHost => _expandedHost;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _root = GetTemplateChild("PART_Root") as Grid;
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _glassCanvas = GetTemplateChild("PART_GlassCanvas") as Canvas;
        _collapsedHost = GetTemplateChild("PART_CollapsedHost") as ContentPresenter;
        _expandedHost = GetTemplateChild("PART_ExpandedHost") as ContentPresenter;

        UpdateHostsVisibility();
    }

    private void UpdateHostsVisibility()
    {
        if (_collapsedHost != null) _collapsedHost.Visibility = Expanded ? Visibility.Collapsed : Visibility.Visible;
        if (_expandedHost != null) _expandedHost.Visibility = Expanded ? Visibility.Visible : Visibility.Collapsed;
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new SeaGlass.WinUI.Automation.SeaGlassTwoElementMorphAutomationPeer(this);
}

