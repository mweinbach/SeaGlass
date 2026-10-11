using System;
using System.Collections.Generic;
using System.Numerics;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Defines how <see cref="SeaGlassTwoElementMorph"/> transitions between two arbitrary UI elements.
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassMorphMode</c> on Android.
/// </summary>
public enum SeaGlassMorphMode
{
    /// <summary>
    /// Synthesizes a multi-node <c>sminCubic</c> liquid sea-glass bridge during the transition so
    /// the departing element stretches a liquid neck as it morphs into the arriving element.
    /// </summary>
    LiquidCoalesce,

    /// <summary>
    /// Morphs between two Material 3 Expressive <see cref="SeaGlassShapeKind"/> silhouettes
    /// while flexing glass thickness, IOR lensing, and chromatic dispersion.
    /// </summary>
    PolygonShapeMorph
}

/// <summary>
/// Anchor alignment for morphing popovers, context menus, and droplets.
/// </summary>
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
/// Automatic 2-Element Dynamic Liquid SeaGlass Morph Primitive.
///
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassTwoElementMorph</c> on Android:
/// 1. Animates container bounds <c>(width, height)</c> with a bouncy spatial spring (<c>zeta = 0.68, k = 360</c>).
/// 2. Animates optical material and cross-fade with a critically damped effects spring (<c>zeta = 1.0, k = 540</c>).
/// 3. Flexes IOR lensing, chromatic dispersion, and thickness at peak transition velocity via
///    <see cref="SeaGlassShapeMorph.ComputeDynamicFlexMaterial"/>.
/// 4. Synthesizes a 2-node <c>sminCubic</c> liquid teardrop bridge anchored to <see cref="AnchorAlignment"/>.
/// </summary>
public class SeaGlassTwoElementMorph : SeaGlassSurface
{
    private float _spatialWidthProgress;
    private float _spatialWidthVel;
    private float _spatialHeightProgress;
    private float _spatialHeightVel;
    private float _effectsProgress;
    private float _effectsVel;

    public bool Expanded { get; private set; }
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

    public void SetExpanded(bool expanded) => Expanded = expanded;
    public void ToggleExpanded() => Expanded = !Expanded;

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        float target = Expanded ? 1f : 0f;

        // Lead-lag dual spatial springs: horizontal width leads (k = 420) while vertical height
        // follows (k = 340, zeta = 0.64) so the glass stretches organically into its target frame.
        SeaGlassSpringSpec widthSpring = new(DampingRatio: 0.68f, Stiffness: 420f);
        SeaGlassSpringSpec heightSpring = new(DampingRatio: 0.64f, Stiffness: 340f);
        widthSpring.Step(ref _spatialWidthProgress, ref _spatialWidthVel, target, dtSeconds);
        heightSpring.Step(ref _spatialHeightProgress, ref _spatialHeightVel, target, dtSeconds);
        activeTheme.MotionScheme.DefaultEffects.Step(ref _effectsProgress, ref _effectsVel, target, dtSeconds);

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

            // Node 1 covers the full current container envelope (curW * 0.5f, curH * 0.5f) so
            // both entry (energy = 0) and exit (energy = 0) match the resting single-node silhouette
            // with zero contour jump.
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
}

/// <summary>
/// Corner-Anchored Liquid Teardrop Context Menu Morph (<c>SeaGlassMorphingContextMenu</c>).
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassMorphingContextMenu</c> on Android.
/// </summary>
public sealed class SeaGlassMorphingContextMenu : SeaGlassTwoElementMorph
{
    public IReadOnlyList<(SeaGlassGlyph Glyph, string Title)> QuickHeaderActions { get; } =
    [
        (SeaGlassGlyph.Bookmark, "Pin"),
        (SeaGlassGlyph.Check, "Settle"),
        (SeaGlassGlyph.Sparkles, "Rename…")
    ];

    public IReadOnlyList<(SeaGlassGlyph Glyph, string Label, string Meta)> MenuRows { get; } =
    [
        (SeaGlassGlyph.Layers, "Files", ""),
        (SeaGlassGlyph.Tune, "Terminal", ""),
        (SeaGlassGlyph.Share, "Git", "main"),
        (SeaGlassGlyph.Bell, "Snooze", "›"),
        (SeaGlassGlyph.Sparkles, "Regenerate Title", ""),
        (SeaGlassGlyph.Close, "Delete…", "destructive")
    ];

    public Action<string>? OnActionSelected { get; set; }

    public SeaGlassMorphingContextMenu(
        bool expanded = false,
        SeaGlassStageState? stageState = null,
        Action<string>? onActionSelected = null)
        : base(
            expanded: expanded,
            stageState: stageState,
            collapsedSizePx: new Vector2(44f, 44f),
            expandedSizePx: new Vector2(276f, 292f),
            morphMode: SeaGlassMorphMode.LiquidCoalesce,
            anchorAlignment: SeaGlassAnchorAlignment.TopEnd)
    {
        OnActionSelected = onActionSelected;
    }

    public void SelectMenuAction(string actionLabel)
    {
        OnActionSelected?.Invoke(actionLabel);
        SetExpanded(false);
    }
}

/// <summary>
/// 3-State Cascaded Composer Model &amp; Effort Picker Morph (<c>SeaGlassCascadedModelMorph</c>).
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassCascadedModelMorph</c> on Android.
/// </summary>
public sealed class SeaGlassCascadedModelMorph : SeaGlassSurface
{
    private float _spatialStage;
    private float _spatialVel;
    private float _effectsStage;
    private float _effectsVel;

    public int Stage { get; private set; }
    public string SelectedModel { get; set; }
    public float EffortFraction { get; set; }
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
        < 0.25f => "Low",
        < 0.60f => "Medium",
        < 0.88f => "High",
        _ => "Max"
    };

    public float SpatialStage => _spatialStage;
    public float EffectsStage => _effectsStage;

    public SeaGlassCascadedModelMorph(
        int stage = 0,
        string selectedModel = "Gemini 4 Argon EAP",
        float effortFraction = 0.75f,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? pillMaterial = null,
        SeaGlassMaterial? popoverMaterial = null)
        : base(stageState: stageState)
    {
        Stage = Math.Clamp(stage, 0, 2);
        _spatialStage = Stage;
        _effectsStage = Stage;
        SelectedModel = selectedModel;
        EffortFraction = Math.Clamp(effortFraction, 0f, 1f);
        PillMaterial = pillMaterial ?? SeaGlassTheme.Current.Palette.SurfaceGlass;
        PopoverMaterial = popoverMaterial ?? SeaGlassTheme.Current.Palette.ElevatedGlass;
    }

    public void SetStage(int stage) => Stage = Math.Clamp(stage, 0, 2);

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        SeaGlassSpringSpec spatialSpring = new(DampingRatio: 0.68f, Stiffness: 360f);
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
}

/// <summary>
/// Circular Progress Ring &lt;-&gt; Context Window Popover Morph (<c>SeaGlassContextRingMorph</c>).
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassContextRingMorph</c> on Android.
/// </summary>
public sealed class SeaGlassContextRingMorph : SeaGlassTwoElementMorph
{
    public float Progress { get; set; }
    public string UsedLabel { get; set; }
    public string RemainingLabel { get; set; }

    public SeaGlassContextRingMorph(
        bool expanded = false,
        float progress = 0.35f,
        string usedLabel = "366K of 1.05M tokens",
        string remainingLabel = "683K left",
        SeaGlassStageState? stageState = null)
        : base(
            expanded: expanded,
            stageState: stageState,
            collapsedSizePx: new Vector2(38f, 38f),
            expandedSizePx: new Vector2(236f, 128f),
            morphMode: SeaGlassMorphMode.LiquidCoalesce,
            anchorAlignment: SeaGlassAnchorAlignment.TopCenter)
    {
        Progress = Math.Clamp(progress, 0.05f, 1f);
        UsedLabel = usedLabel;
        RemainingLabel = remainingLabel;
    }
}

/// <summary>
/// Material 3 Expressive Shape-Morphing SeaGlass Button.
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassMorphingButton</c> on Android.
/// </summary>
public sealed class SeaGlassMorphingButton : SeaGlassSurface
{
    private float _morphProgress;
    private float _morphVel;

    public bool Active { get; private set; }
    public SeaGlassShapeKind RestingShape { get; set; }
    public SeaGlassShapeKind ActiveShape { get; set; }
    public float RestingScale { get; set; }
    public float ActiveScale { get; set; }
    public float SpinDegreesOnMorph { get; set; }
    public bool ViaCircleBridge { get; set; }
    public SeaGlassMaterial RestingMaterial { get; set; }
    public SeaGlassMaterial ActiveMaterial { get; set; }
    public ChoreographedShapeTransform LastChoreoTransform { get; private set; }

    public SeaGlassMorphingButton(
        bool active,
        Action onClick,
        SeaGlassStageState? stageState = null,
        SeaGlassShapeKind restingShape = SeaGlassShapeKind.Cookie9,
        SeaGlassShapeKind activeShape = SeaGlassShapeKind.Squircle,
        float restingScale = 1.0f,
        float activeScale = 1.06f,
        float spinDegreesOnMorph = 45f,
        bool viaCircleBridge = true,
        SeaGlassMaterial? restingMaterial = null,
        SeaGlassMaterial? activeMaterial = null)
        : base(stageState: stageState, onClick: onClick)
    {
        Active = active;
        _morphProgress = active ? 1f : 0f;
        RestingShape = restingShape;
        ActiveShape = activeShape;
        RestingScale = restingScale;
        ActiveScale = activeScale;
        SpinDegreesOnMorph = spinDegreesOnMorph;
        ViaCircleBridge = viaCircleBridge;
        RestingMaterial = restingMaterial ?? SeaGlassTheme.Current.Palette.PrimaryGlass;
        ActiveMaterial = activeMaterial ?? SeaGlassTheme.Current.Palette.TertiaryGlass;
        SizePx = new Vector2(96f, 96f);
    }

    public void SetActive(bool active) => Active = active;
    public void ToggleActive() => Active = !Active;

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        float target = IsPointerDown ? (Active ? 0.58f : 0.42f) : (Active ? 1f : 0f);
        SeaGlassSpringSpec morphSpring = new(DampingRatio: 0.50f, Stiffness: 340f);
        morphSpring.Step(ref _morphProgress, ref _morphVel, target, dtSeconds);

        SeaGlassShapeStage startStage = new(RestingShape, RestingScale, 1f, 0f, ViaCircleBridge);
        SeaGlassShapeStage endStage = new(ActiveShape, ActiveScale, 1f, SpinDegreesOnMorph, ViaCircleBridge);
        ChoreographedShapeTransform choreo = SeaGlassShapeMorph.EvaluateChoreographedTransform(startStage, endStage, _morphProgress);
        LastChoreoTransform = choreo;

        Material = SeaGlassShapeMorph.ComputeDynamicFlexMaterial(
            startMaterial: RestingMaterial,
            endMaterial: ActiveMaterial,
            progress: Math.Clamp(_morphProgress, 0f, 1f),
            velocityPxPerSec: choreo.MorphEnergy * 1400f);

        float p = _morphProgress;
        return EvaluateFrame(
            dtSeconds: dtSeconds,
            theme: activeTheme,
            customContourBuilder: (wPx, hPx, outPath) =>
                SeaGlassShapeMorph.BuildChoreographedPolygonPath(startStage, endStage, p, wPx, hPx, outPath));
    }
}

/// <summary>
/// Material 3 Expressive Choreographed Shape Shifter Orb (<c>Flower -&gt; Small Circle -&gt; Cookie9 -&gt;
/// Large Circle -&gt; Clover4 -&gt; Liquid Pill -&gt; Sunny -&gt; Heart</c>).
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassChoreographedShapeOrb</c> on Android.
/// </summary>
public sealed class SeaGlassChoreographedShapeOrb : SeaGlassSurface
{
    private int _prevIndex;
    private int _targetIndex;
    private float _transitionProgress = 1f;
    private float _transitionVel;

    public IReadOnlyList<SeaGlassShapeStage> Stages { get; }
    public int TargetStageIndex => _targetIndex;
    public SeaGlassShapeStage CurrentTargetStage => Stages[_targetIndex];
    public ChoreographedShapeTransform CurrentTransform { get; private set; }
    public SeaGlassMaterial PrimaryMaterial { get; set; }
    public SeaGlassMaterial SecondaryMaterial { get; set; }

    public SeaGlassChoreographedShapeOrb(
        SeaGlassStageState? stageState = null,
        IReadOnlyList<SeaGlassShapeStage>? stages = null,
        SeaGlassMaterial? primaryMaterial = null,
        SeaGlassMaterial? secondaryMaterial = null)
        : base(stageState: stageState)
    {
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
        SeaGlassSpringSpec orbSpring = new(DampingRatio: 0.48f, Stiffness: 290f);
        orbSpring.Step(ref _transitionProgress, ref _transitionVel, 1f, dtSeconds);

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
}

/// <summary>
/// Apple Liquid Glass Budding Action Cluster (<c>SeaGlassBuddingCluster</c>).
/// Satellite droplets bud out of the anchor's center, stretching live <c>sminCubic</c> liquid bridges.
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassBuddingCluster</c> on Android.
/// </summary>
public sealed class SeaGlassBuddingCluster : SeaGlassSurface
{
    private float _expandProgress;
    private float _expandVel;

    public bool Expanded { get; private set; }
    public IReadOnlyList<(string Symbol, string Label)> Items { get; }
    public Action<int> OnItemClick { get; }
    public SeaGlassMaterial AnchorMaterial { get; set; }
    public SeaGlassMaterial SatelliteMaterial { get; set; }
    public float ExpandProgress => _expandProgress;
    public IReadOnlyList<SeaGlassSdfEngine.SdfNode> LastEvaluatedNodes { get; private set; } = [];

    public SeaGlassBuddingCluster(
        bool expanded,
        IReadOnlyList<(string Symbol, string Label)> items,
        Action<int> onItemClick,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? anchorMaterial = null,
        SeaGlassMaterial? satelliteMaterial = null)
        : base(stageState: stageState)
    {
        Expanded = expanded;
        _expandProgress = expanded ? 1f : 0f;
        Items = items;
        OnItemClick = onItemClick;
        AnchorMaterial = anchorMaterial ?? SeaGlassTheme.Current.Palette.PrimaryGlass;
        SatelliteMaterial = satelliteMaterial ?? SeaGlassTheme.Current.Palette.TertiaryGlass;
        SizePx = new Vector2(460f, 82f);
        OnClick = () => ToggleExpanded();
    }

    public void ToggleExpanded() => Expanded = !Expanded;
    public void SetExpanded(bool expanded) => Expanded = expanded;

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        float density = MathF.Max(0.5f, activeTheme.DpiScale);
        SeaGlassSpringSpec budSpring = new(DampingRatio: 0.56f, Stiffness: 320f);
        budSpring.Step(ref _expandProgress, ref _expandVel, Expanded ? 1f : 0f, dtSeconds);

        float containerW = MathF.Max(240f, SizePx.X);
        float clusterH = MathF.Max(56f, SizePx.Y);
        float centerY = clusterH * 0.5f;
        float anchorR = MathF.Min(25f * density, clusterH * 0.36f);
        float satR = MathF.Min(22f * density, clusterH * 0.32f);
        int count = Math.Clamp(Items.Count, 1, 3);

        float anchorCenterX = anchorR + 12f * density;
        float stepSpanPx = MathF.Max(satR * 2.35f, (containerW - anchorCenterX - satR * 1.3f - 14f * density) / count);

        List<SeaGlassSdfEngine.SdfNode> activeNodes = new(count + 1)
        {
            new SeaGlassSdfEngine.SdfNode(
                centerX: anchorCenterX,
                centerY: centerY,
                halfWidth: anchorR,
                halfHeight: anchorR,
                uniformCornerRadius: anchorR,
                cornerSmoothing: 2.2f,
                tintColorArgb: AnchorMaterial.TintColorArgb,
                thicknessPx: anchorR * 0.68f)
        };

        if (_expandProgress > 0.02f)
        {
            for (int i = 0; i < count; i++)
            {
                float stagger = Math.Clamp((_expandProgress - i * 0.08f) / (1f - i * 0.08f), 0f, 1f);
                float r = satR * (0.48f + 0.52f * stagger);
                float cx = anchorCenterX + stepSpanPx * (i + 1) * stagger;
                activeNodes.Add(new SeaGlassSdfEngine.SdfNode(
                    centerX: cx,
                    centerY: centerY,
                    halfWidth: r * 1.18f,
                    halfHeight: r,
                    uniformCornerRadius: r,
                    cornerSmoothing: 2.2f,
                    tintColorArgb: (i % 2 == 0) ? SatelliteMaterial.TintColorArgb : AnchorMaterial.TintColorArgb,
                    thicknessPx: r * 0.68f));
            }
        }

        LastEvaluatedNodes = activeNodes;
        Material = AnchorMaterial;
        return EvaluateFrame(dtSeconds, activeTheme, activeNodes, smoothRadiusDp: 34f);
    }
}

/// <summary>
/// Interactive Liquid SeaGlass Slider whose thumb is a live 3D water-drop sea-glass lens
/// that magnifies the track underneath, stretches elastically with drag velocity, and blooms
/// with HDR light on touch.
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassLiquidSlider</c> on Android.
/// </summary>
public sealed class SeaGlassLiquidSlider : SeaGlassSurface
{
    private float _dragBoost;
    private float _dragVel;

    public string Title { get; set; }
    public string ValueText { get; set; }
    public float Value { get; private set; }
    public float MinValue { get; }
    public float MaxValue { get; }
    public Action<float> OnValueChange { get; }
    public SeaGlassMaterial ThumbMaterial { get; set; }
    public bool IsDragging { get; set; }

    public float Fraction => Math.Clamp((Value - MinValue) / MathF.Max(1e-4f, MaxValue - MinValue), 0f, 1f);
    public float DragBoost => _dragBoost;
    public float ThumbScaleX => 1f + 0.24f * _dragBoost;
    public float ThumbScaleY => 1f + 0.12f * _dragBoost;

    public SeaGlassLiquidSlider(
        string title,
        string valueText,
        float value,
        float minValue,
        float maxValue,
        Action<float> onValueChange,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? thumbMaterial = null)
        : base(stageState: stageState)
    {
        Title = title;
        ValueText = valueText;
        MinValue = minValue;
        MaxValue = MathF.Max(minValue + 1e-4f, maxValue);
        Value = Math.Clamp(value, MinValue, MaxValue);
        OnValueChange = onValueChange;
        ThumbMaterial = thumbMaterial ?? SeaGlassTheme.Current.Palette.PrimaryGlass;
        SizePx = new Vector2(34f, 22f);
    }

    public void SetValue(float newValue)
    {
        Value = Math.Clamp(newValue, MinValue, MaxValue);
        OnValueChange(Value);
    }

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        activeTheme.MotionScheme.FastSpatial.Step(ref _dragBoost, ref _dragVel, IsDragging ? 1f : 0f, dtSeconds);
        float db = Math.Clamp(_dragBoost, 0f, 1.2f);

        Material = ThumbMaterial with
        {
            CornerRadiusDp = 14f,
            ThicknessDp = Math.Clamp(18f + 6f * db, 1f, 36f),
            Transparency = Math.Clamp(ThumbMaterial.Transparency + 0.16f, 0f, 0.92f),
            Ior = Math.Clamp(ThumbMaterial.Ior + 0.12f * db, 1.0f, 1.88f),
            ChromaticDispersion = Math.Clamp(ThumbMaterial.ChromaticDispersion + 0.18f * db, 0f, 1f)
        };

        return EvaluateFrame(dtSeconds, activeTheme);
    }
}

/// <summary>
/// Multi-Stage Material 3 Expressive Shape Sequence Morpher (<c>SeaGlassMultiShapeMorph</c>).
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassMultiShapeMorph</c> on Android.
/// </summary>
public sealed class SeaGlassMultiShapeMorph : SeaGlassSurface
{
    private float _animatedIndex;
    private float _indexVel;

    public IReadOnlyList<SeaGlassShapeKind> Shapes { get; }
    public int ActiveShapeIndex { get; private set; }
    public float? ContinuousIndexOverride { get; set; }
    public SeaGlassMaterial PrimaryMaterial { get; set; }
    public SeaGlassMaterial SecondaryMaterial { get; set; }

    public SeaGlassShapeKind CurrentDisplayShape => Shapes[Math.Clamp(ActiveShapeIndex, 0, Shapes.Count - 1)];

    public SeaGlassMultiShapeMorph(
        IReadOnlyList<SeaGlassShapeKind> shapes,
        int activeShapeIndex = 0,
        SeaGlassStageState? stageState = null,
        Action? onClick = null,
        SeaGlassMaterial? primaryMaterial = null,
        SeaGlassMaterial? secondaryMaterial = null)
        : base(stageState: stageState, onClick: onClick)
    {
        if (shapes is null || shapes.Count == 0)
            throw new ArgumentException("shapes must not be empty", nameof(shapes));

        Shapes = shapes;
        ActiveShapeIndex = Math.Clamp(activeShapeIndex, 0, shapes.Count - 1);
        _animatedIndex = ActiveShapeIndex;
        PrimaryMaterial = primaryMaterial ?? SeaGlassTheme.Current.Palette.PrimaryGlass;
        SecondaryMaterial = secondaryMaterial ?? SeaGlassTheme.Current.Palette.TertiaryGlass;
        SizePx = new Vector2(110f, 110f);
    }

    public void SetActiveShapeIndex(int index) =>
        ActiveShapeIndex = Math.Clamp(index, 0, Shapes.Count - 1);

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        SeaGlassSpringSpec seqSpring = new(DampingRatio: 0.54f, Stiffness: 340f);
        seqSpring.Step(ref _animatedIndex, ref _indexVel, ActiveShapeIndex, dtSeconds);

        float effectiveIdx = ContinuousIndexOverride ?? _animatedIndex;
        float fractionalPart = MathF.Abs(effectiveIdx - MathF.Round(effectiveIdx));
        float colorWave = Math.Clamp(MathF.Sin(effectiveIdx * MathF.PI * 0.5f) * 0.5f + 0.5f, 0f, 1f);

        Material = SeaGlassShapeMorph.ComputeDynamicFlexMaterial(
            startMaterial: PrimaryMaterial,
            endMaterial: SecondaryMaterial,
            progress: colorWave,
            velocityPxPerSec: fractionalPart * 1200f);

        return EvaluateFrame(
            dtSeconds: dtSeconds,
            theme: activeTheme,
            customContourBuilder: (wPx, hPx, outPath) =>
                SeaGlassShapeMorph.BuildSequenceMorphedPolygonPath(Shapes, effectiveIdx, wPx, hPx, outPath));
    }
}
