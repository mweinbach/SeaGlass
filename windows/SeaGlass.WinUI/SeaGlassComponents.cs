using System;
using System.Collections.Generic;
using System.Numerics;
using SeaGlass.Core;
using SeaGlass.Core.Backend;

namespace SeaGlass.WinUI;

/// <summary>
/// Evaluated per-frame optical, geometric, and uniform state for any <see cref="SeaGlassSurface"/>
/// or derived WinUI SeaGlass control.
/// </summary>
public sealed record SeaGlassSurfaceFrameState(
    SeaGlassMaterial EffectiveMaterial,
    int OptimalContentColorArgb,
    SeaGlassCornerRadiiPx SolvedCornerRadiiPx,
    SeaGlassSafePaddingPx SafePaddingPx,
    float ScaleX,
    float ScaleY,
    float TiltRotationXDegrees,
    float TiltRotationYDegrees,
    SeaGlassUniformBlock UniformBlock,
    SeaGlassVectorPath2D? ContourPath = null,
    bool IsUniformSingleNode = false,
    float WidthPx = 0f,
    float HeightPx = 0f)
{
    private string? _cachedXamlContourPathData;

    /// <summary>
    /// Lazily serialized XAML / SVG path string (<c>F1 M ... C ... Z</c>).
    /// Live Direct2D / WPF rendering consumes <see cref="ContourPath"/> and <see cref="IsUniformSingleNode"/>
    /// directly without string formatting or <c>Geometry.Parse</c>.
    /// </summary>
    public string XamlContourPathData
    {
        get
        {
            if (_cachedXamlContourPathData is not null) return _cachedXamlContourPathData;
            if (ContourPath is { IsEmpty: false })
            {
                _cachedXamlContourPathData = ContourPath.ToXamlPathData();
                return _cachedXamlContourPathData;
            }
            SeaGlassVectorPath2D fallback = new();
            fallback.AddRoundRect(
                0f, 0f, MathF.Max(1f, WidthPx), MathF.Max(1f, HeightPx),
                SolvedCornerRadiiPx.TopLeftPx,
                SolvedCornerRadiiPx.TopRightPx,
                SolvedCornerRadiiPx.BottomRightPx,
                SolvedCornerRadiiPx.BottomLeftPx);
            _cachedXamlContourPathData = fallback.ToXamlPathData();
            return _cachedXamlContourPathData;
        }
    }
}

/// <summary>
/// Foundational SeaGlass container surface for WinUI with Material 3 Expressive state morphing,
/// adaptive touch/pointer 3D perspective tilt, HDR caustic flare, concentric corner adaptation,
/// and automatic Oklab contrast-safe foreground content color.
///
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassSurface</c> on Android.
/// </summary>
public class SeaGlassSurface
{
    private SeaGlassMaterialAnimator _materialAnimator;
    private readonly SeaGlassUniformBlock _uniformBlock = new();
    private readonly SeaGlassVectorPath2D _contourPath = new();
    private readonly SeaGlassSdfEngine.SdfNode[] _singleNodeBuffer = new SeaGlassSdfEngine.SdfNode[1];
    private float _pressFraction;
    private float _pressVelocity;
    private float _tiltRotX, _tiltVelX;
    private float _tiltRotY, _tiltVelY;

    public SeaGlassStageState? StageState { get; set; }
    public SeaGlassMaterial Material { get; set; }
    public SeaGlassCornerAdaptationMode CornerAdaptationMode { get; set; }
    public SeaGlassCornerMask CornerMask { get; set; } = SeaGlassCornerMask.All;
    public float CornerProximityThresholdDp { get; set; } = 36f;
    public float CornerFullMatchDistanceDp { get; set; } = 6f;
    public float CornerAdaptationStrength { get; set; } = 1.0f;
    public bool AllowAsymmetricCorners { get; set; } = true;
    public bool AdaptToParentContainer { get; set; } = true;
    public SeaGlassContentRole ContentRole { get; set; }
    public Vector2 OriginInRoot { get; set; }
    public Vector2 SizePx { get; set; } = new(320f, 140f);
    public Vector4 BasePaddingPx { get; set; } = new(16f, 12f, 16f, 12f); // Left, Top, Right, Bottom
    public Vector2? LastTouchOffsetInLocal { get; private set; }
    public bool IsPointerDown { get; private set; }
    public Action? OnClick { get; set; }

    public SeaGlassSurface(
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? material = null,
        SeaGlassCornerAdaptationMode cornerAdaptationMode = SeaGlassCornerAdaptationMode.ConcentricProximity,
        SeaGlassContentRole contentRole = SeaGlassContentRole.TextAndIcons,
        Action? onClick = null,
        SeaGlassCornerMask? cornerMask = null,
        float cornerProximityThresholdDp = 36f,
        float cornerFullMatchDistanceDp = 6f,
        float cornerAdaptationStrength = 1.0f,
        bool allowAsymmetricCorners = true,
        bool adaptToParentContainer = true)
    {
        StageState = stageState ?? SeaGlassStageHost.AmbientStageState;
        Material = material ?? SeaGlassTheme.Current.Palette.SurfaceGlass;
        CornerAdaptationMode = cornerAdaptationMode;
        CornerMask = cornerMask ?? SeaGlassCornerMask.All;
        CornerProximityThresholdDp = cornerProximityThresholdDp;
        CornerFullMatchDistanceDp = cornerFullMatchDistanceDp;
        CornerAdaptationStrength = cornerAdaptationStrength;
        AllowAsymmetricCorners = allowAsymmetricCorners;
        AdaptToParentContainer = adaptToParentContainer;
        ContentRole = contentRole;
        OnClick = onClick;
        _materialAnimator = new SeaGlassMaterialAnimator(Material);
    }

    /// <summary>
    /// Immediately snaps both the target <see cref="Material"/> and internal spring animator to <paramref name="material"/>.
    /// </summary>
    public void SnapMaterial(SeaGlassMaterial material)
    {
        Material = material;
        _materialAnimator = new SeaGlassMaterialAnimator(material);
    }

    public void OnPointerStateChanged(Vector2? localPosition, bool isDown)
    {
        if (localPosition is not null) LastTouchOffsetInLocal = localPosition;
        bool wasDown = IsPointerDown;
        IsPointerDown = isDown;
        if (wasDown && !isDown)
        {
            OnClick?.Invoke();
        }
    }

    /// <summary>
    /// Advances dual-spring physics and evaluates the 256-byte <see cref="SeaGlassUniformBlock"/>,
    /// concentric per-corner radii, and C^2-continuous contour path with zero per-frame buffer allocations.
    /// </summary>
    public SeaGlassSurfaceFrameState EvaluateFrame(
        float dtSeconds = 1f / 60f,
        SeaGlassTheme? theme = null,
        IReadOnlyList<SeaGlassSdfEngine.SdfNode>? nodesOverride = null,
        float smoothRadiusDp = 26f,
        Action<float, float, SeaGlassVectorPath2D>? customContourBuilder = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        SeaGlassMotionScheme motion = activeTheme.MotionScheme;
        float density = MathF.Max(0.5f, activeTheme.DpiScale);
        SeaGlassStageState? activeStage = StageState ?? SeaGlassStageHost.AmbientStageState;

        float targetPress = IsPointerDown ? 1f : 0f;
        motion.FastSpatial.Step(ref _pressFraction, ref _pressVelocity, targetPress, dtSeconds);
        float clampedPress = Math.Clamp(_pressFraction, 0f, 1.25f);

        SeaGlassMaterial targetMaterial = Material.Pressed(Math.Clamp(clampedPress, 0f, 1f));
        if (!SeaGlass.WinUI.Rendering.SeaGlassXamlRenderer.PointerTiltAndGlowEnabled)
        {
            targetMaterial = targetMaterial with { RimChalkiness = Material.RimChalkiness };
        }
        SeaGlassMaterial effectiveMaterial = _materialAnimator.StepToward(targetMaterial, motion, dtSeconds);

        float wPx = MathF.Max(1f, SizePx.X);
        float hPx = MathF.Max(1f, SizePx.Y);
        float cx = MathF.Max(1f, wPx * 0.5f);
        float cy = MathF.Max(1f, hPx * 0.5f);
        float normTouchX = LastTouchOffsetInLocal is { } ptX ? Math.Clamp((ptX.X - cx) / cx, -1f, 1f) : 0f;
        float normTouchY = LastTouchOffsetInLocal is { } ptY ? Math.Clamp((ptY.Y - cy) / cy, -1f, 1f) : 0f;

        float tiltGain = SeaGlass.WinUI.Rendering.SeaGlassXamlRenderer.PointerTiltAndGlowEnabled ? 4.8f * clampedPress : 0f;
        motion.FastSpatial.Step(ref _tiltRotX, ref _tiltVelX, -normTouchY * tiltGain, dtSeconds);
        motion.FastSpatial.Step(ref _tiltRotY, ref _tiltVelY, normTouchX * tiltGain, dtSeconds);

        float scaleX = 1f + 0.022f * clampedPress;
        float scaleY = 1f - 0.018f * clampedPress;

        int contentColor = SeaGlassColorMath.OptimalContentColorFor(
            effectiveMaterial.TintColorArgb,
            effectiveMaterial.TintDensity);

        // Solve concentric device & container corner radii
        float baseCornerPx = MathF.Min(effectiveMaterial.CornerRadiusDp * density, MathF.Min(wPx, hPx) * 0.5f);
        Vector2 stageSize = activeStage?.BackdropSize ?? new Vector2(1280f, 800f);
        Vector2 stageOrigin = activeStage?.BackdropOriginInRoot ?? Vector2.Zero;
        float leftInStage = OriginInRoot.X - stageOrigin.X;
        float topInStage = OriginInRoot.Y - stageOrigin.Y;
        float proximityPx = MathF.Max(1f, CornerProximityThresholdDp * density);
        float fullMatchPx = MathF.Max(0f, CornerFullMatchDistanceDp * density);

        SeaGlassCornerRadiiPx deviceSolved = (CornerAdaptationMode == SeaGlassCornerAdaptationMode.Fixed || CornerMask == SeaGlassCornerMask.None)
            ? SeaGlassCornerRadiiPx.Uniform(baseCornerPx)
            : SeaGlassCornerAdaptationSolver.SolveElementCornerRadiiPx(
                elementLeftPx: leftInStage,
                elementTopPx: topInStage,
                elementRightPx: leftInStage + wPx,
                elementBottomPx: topInStage + hPx,
                containerWidthPx: stageSize.X,
                containerHeightPx: stageSize.Y,
                containerCornersPx: activeTheme.DeviceCornerSpec.ToRadiiPx(),
                baseCornerRadiusPx: baseCornerPx,
                minCornerFloorPx: 8f * density,
                influenceDistancePx: proximityPx,
                adaptationStrength: CornerAdaptationStrength,
                mode: CornerAdaptationMode,
                cornerMask: CornerMask,
                fullMatchDistancePx: fullMatchPx,
                allowAsymmetricCorners: AllowAsymmetricCorners);

        SeaGlassCornerRadiiPx solvedRadii = deviceSolved;
        if (AdaptToParentContainer &&
            activeTheme.ContainerCornerScope is { } parentScope &&
            parentScope.WidthPx > wPx + 4f &&
            parentScope.HeightPx > hPx + 4f &&
            CornerAdaptationMode != SeaGlassCornerAdaptationMode.Fixed &&
            CornerMask != SeaGlassCornerMask.None)
        {
            float relLeft = OriginInRoot.X - parentScope.OriginInRoot.X;
            float relTop = OriginInRoot.Y - parentScope.OriginInRoot.Y;
            solvedRadii = SeaGlassCornerAdaptationSolver.SolveElementCornerRadiiPx(
                elementLeftPx: relLeft,
                elementTopPx: relTop,
                elementRightPx: relLeft + wPx,
                elementBottomPx: relTop + hPx,
                containerWidthPx: parentScope.WidthPx,
                containerHeightPx: parentScope.HeightPx,
                containerCornersPx: parentScope.CornerRadiiPx,
                baseCornerRadiusPx: deviceSolved.MaxRadiusPx,
                minCornerFloorPx: 7f * density,
                influenceDistancePx: MathF.Min(proximityPx, 48f * density),
                adaptationStrength: CornerAdaptationStrength,
                mode: CornerAdaptationMode,
                cornerMask: CornerMask,
                fullMatchDistancePx: fullMatchPx,
                allowAsymmetricCorners: AllowAsymmetricCorners);
        }

        SeaGlassSafePaddingPx safePadding = CornerAdaptationMode == SeaGlassCornerAdaptationMode.FlushCornerEmbed
            ? SeaGlassCornerAdaptationSolver.ComputeCornerSafePaddingPx(
                solvedRadii,
                BasePaddingPx.X,
                BasePaddingPx.Y,
                BasePaddingPx.Z,
                BasePaddingPx.W)
            : new SeaGlassSafePaddingPx(BasePaddingPx.X, BasePaddingPx.Y, BasePaddingPx.Z, BasePaddingPx.W);

        IReadOnlyList<SeaGlassSdfEngine.SdfNode> activeNodes;
        if (nodesOverride is not null)
        {
            activeNodes = nodesOverride;
        }
        else
        {
            _singleNodeBuffer[0] = new SeaGlassSdfEngine.SdfNode(
                CenterX: wPx * 0.5f,
                CenterY: hPx * 0.5f,
                HalfWidth: wPx * 0.5f,
                HalfHeight: hPx * 0.5f,
                CornerTopRight: solvedRadii.TopRightPx,
                CornerBottomRight: solvedRadii.BottomRightPx,
                CornerBottomLeft: solvedRadii.BottomLeftPx,
                CornerTopLeft: solvedRadii.TopLeftPx,
                CornerSmoothing: effectiveMaterial.CornerSmoothing,
                TintColorArgb: effectiveMaterial.TintColorArgb,
                ThicknessPx: effectiveMaterial.ThicknessDp * density);
            activeNodes = _singleNodeBuffer;
        }

        bool isUniformSingleNode = customContourBuilder is null &&
                                   nodesOverride is null &&
                                   !solvedRadii.IsAsymmetric;

        if (customContourBuilder is not null)
        {
            customContourBuilder(wPx, hPx, _contourPath);
        }
        else if (!isUniformSingleNode)
        {
            SeaGlassContourPathBuilder.BuildCoalescingScenePath(
                nodes: activeNodes,
                smoothRadiusPx: smoothRadiusDp * density,
                outPath: _contourPath);
        }
        else
        {
            _contourPath.Reset();
        }

        Vector2 backdropOffset = activeStage?.ComputeLocalBackdropOffset(OriginInRoot) ?? OriginInRoot;
        Vector3 lightDir = activeTheme.TiltState.LightDirection;

        _uniformBlock.Pack(
            widthPx: wPx,
            heightPx: hPx,
            backdropOffsetX: backdropOffset.X,
            backdropOffsetY: backdropOffset.Y,
            material: effectiveMaterial,
            density: density,
            nodes: activeNodes,
            smoothRadiusPx: smoothRadiusDp * density,
            lightDirX: lightDir.X,
            lightDirY: lightDir.Y,
            lightDirZ: lightDir.Z);

        return new SeaGlassSurfaceFrameState(
            EffectiveMaterial: effectiveMaterial,
            OptimalContentColorArgb: contentColor,
            SolvedCornerRadiiPx: solvedRadii,
            SafePaddingPx: safePadding,
            ScaleX: scaleX,
            ScaleY: scaleY,
            TiltRotationXDegrees: _tiltRotX,
            TiltRotationYDegrees: _tiltRotY,
            UniformBlock: _uniformBlock,
            ContourPath: _contourPath,
            IsUniformSingleNode: isUniformSingleNode,
            WidthPx: wPx,
            HeightPx: hPx);
    }
}

/// <summary>
/// Tactile tumbled sea-glass button that physically compresses on press with adaptive
/// touch-point HDR caustic flare and 3D perspective tilt.
///
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassButton</c> on Android.
/// </summary>
public class SeaGlassButton : SeaGlassSurface
{
    public string Label { get; set; }
    public SeaGlassGlyph? LeadingGlyph { get; set; }

    public SeaGlassButton(
        string label,
        Action onClick,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? material = null,
        SeaGlassGlyph? leadingGlyph = null,
        SeaGlassCornerAdaptationMode cornerAdaptationMode = SeaGlassCornerAdaptationMode.Fixed,
        SeaGlassCornerMask? cornerMask = null,
        float cornerProximityThresholdDp = 24f)
        : base(
            stageState: stageState,
            material: material ?? SeaGlassTheme.Current.Palette.PrimaryGlass,
            cornerAdaptationMode: cornerAdaptationMode,
            contentRole: SeaGlassContentRole.TextAndIcons,
            onClick: onClick,
            cornerMask: cornerMask ?? SeaGlassCornerMask.None,
            cornerProximityThresholdDp: cornerProximityThresholdDp)
    {
        Label = label;
        LeadingGlyph = leadingGlyph;
        SizePx = new Vector2(148f, 46f);
        BasePaddingPx = new Vector4(22f, 13f, 22f, 13f);
    }
}

/// <summary>
/// Expressive SeaGlass pill chip that smoothly morphs its tint, coarseness, corner radius,
/// and thickness between unselected and selected states.
///
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassPill</c> on Android.
/// </summary>
public sealed class SeaGlassPill : SeaGlassButton
{
    public bool Selected { get; private set; }
    public SeaGlassMaterial SelectedMaterial { get; set; }
    public SeaGlassMaterial UnselectedMaterial { get; set; }

    public SeaGlassPill(
        string label,
        bool selected,
        Action onClick,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? selectedMaterial = null,
        SeaGlassMaterial? unselectedMaterial = null)
        : base(label, onClick, stageState)
    {
        SelectedMaterial = selectedMaterial ?? SeaGlassTheme.Current.Palette.PrimaryGlass;
        UnselectedMaterial = unselectedMaterial ?? SeaGlassTheme.Current.Palette.SurfaceGlass;
        SizePx = new Vector2(112f, 40f);
        BasePaddingPx = new Vector4(18f, 10f, 18f, 10f);
        SetSelected(selected);
        SnapMaterial(Material);
    }

    public void SetSelected(bool selected)
    {
        Selected = selected;
        Material = selected
            ? SelectedMaterial with
            {
                CornerRadiusDp = SelectedMaterial.CornerRadiusDp < 16f ? SelectedMaterial.CornerRadiusDp : 26f,
                ThicknessDp = 16f
            }
            : UnselectedMaterial with
            {
                CornerRadiusDp = UnselectedMaterial.CornerRadiusDp < 16f ? UnselectedMaterial.CornerRadiusDp : 18f,
                ThicknessDp = 10f
            };
    }
}

/// <summary>
/// Expressive SeaGlass card with a tumbled squircle silhouette and adaptive frost depth.
///
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassCard</c> on Android.
/// </summary>
public class SeaGlassCard : SeaGlassSurface
{
    public string Title { get; set; }
    public string? Subtitle { get; set; }

    public SeaGlassCard(
        string title = "",
        string? subtitle = null,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? material = null,
        Action? onClick = null)
        : base(
            stageState: stageState,
            material: material ?? SeaGlassTheme.Current.Palette.SurfaceGlass,
            cornerAdaptationMode: SeaGlassCornerAdaptationMode.ConcentricProximity,
            contentRole: SeaGlassContentRole.TextAndIcons,
            onClick: onClick)
    {
        Title = title;
        Subtitle = subtitle;
        SizePx = new Vector2(420f, 180f);
        BasePaddingPx = new Vector4(20f, 20f, 20f, 20f);
    }
}

/// <summary>
/// Floating Liquid SeaGlass Mini-Player Capsule with meniscus bevels, album artwork,
/// track title/subtitle, and Play/Pause + Next controls.
///
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassMiniPlayerBar</c> on Android.
/// </summary>
public sealed class SeaGlassMiniPlayerBar : SeaGlassSurface
{
    public string Title { get; set; }
    public string Subtitle { get; set; }
    public bool IsPlaying { get; set; }
    public Action OnTogglePlay { get; set; }
    public Action OnNextTrack { get; set; }

    public SeaGlassMiniPlayerBar(
        string title,
        string subtitle,
        bool isPlaying,
        Action onTogglePlay,
        Action onNextTrack,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? material = null)
        : base(
            stageState: stageState,
            material: (material ?? SeaGlassTheme.Current.Palette.SurfaceGlass) with
            {
                CornerRadiusDp = 28f,
                ThicknessDp = 18f,
                RimChalkiness = 0.88f
            },
            contentRole: SeaGlassContentRole.NavigationChrome,
            onClick: onTogglePlay)
    {
        Title = title;
        Subtitle = subtitle;
        IsPlaying = isPlaying;
        OnTogglePlay = onTogglePlay;
        OnNextTrack = onNextTrack;
        SizePx = new Vector2(460f, 56f);
        BasePaddingPx = new Vector4(12f, 7f, 12f, 7f);
    }
}

/// <summary>
/// Liquid-Glass-inspired adaptive Material 3 Expressive navigation bar rendered in frosted SeaGlass.
/// Features a plump active-tab glass lozenge pill driven by dual-edge lead-lag spatial springs
/// (<c>LeadIndex</c> at <c>zeta=0.68, k=540</c> and <c>TrailIndex</c> at <c>zeta=0.72, k=310</c>)
/// so the leading edge reaches the target tab first while the trailing edge stretches a continuous
/// liquid lozenge bridge behind it before settling, plus Temporal De-Aging (<c>Deaged</c>) in flight.
///
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassFluidBar</c> on Android.
/// </summary>
public sealed class SeaGlassFluidBar : SeaGlassSurface
{
    private float _leadIndex;
    private float _leadVel;
    private float _trailIndex;
    private float _trailVel;
    private float _actionSeparationDp;
    private float _sepVel;

    public IReadOnlyList<string> Items { get; }
    public int SelectedIndex { get; private set; }
    public Action<int> OnSelect { get; }
    public SeaGlassMaterial BarMaterial { get; set; }
    public SeaGlassMaterial IndicatorMaterial { get; set; }
    public string ActionLabel { get; set; }
    public bool ActionDetached { get; set; }
    public bool ShowActionOrb { get; set; }
    public Action? OnActionClick { get; set; }

    public float LeadIndex => _leadIndex;
    public float TrailIndex => _trailIndex;
    public float AnimatedIndex => (_leadIndex + _trailIndex) * 0.5f;
    public float LeftEdgeSlot => MathF.Min(_leadIndex, _trailIndex);
    public float RightEdgeSlot => MathF.Max(_leadIndex, _trailIndex);
    public float LozengeSlotSpan => MathF.Max(1.0f, 1.0f + (RightEdgeSlot - LeftEdgeSlot) * 0.92f);
    public float MotionEnergy => Math.Clamp(MathF.Abs(_leadIndex - _trailIndex) * 1.45f, 0f, 1f);
    public float ActionSeparationDp => _actionSeparationDp;
    public float MotionStretch => Math.Clamp(MathF.Abs(_leadIndex - _trailIndex), 0f, 1.2f);
    public SeaGlassMaterial ActiveIndicatorMaterial => IndicatorMaterial.Deaged(MotionEnergy * 0.92f);

    public SeaGlassFluidBar(
        IReadOnlyList<string> items,
        int selectedIndex,
        Action<int> onSelect,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? barMaterial = null,
        SeaGlassMaterial? indicatorMaterial = null,
        string actionLabel = "✦",
        bool actionDetached = false,
        bool showActionOrb = false,
        Action? onActionClick = null)
        : base(
            stageState: stageState,
            material: (barMaterial ?? SeaGlassTheme.Current.Palette.SurfaceGlass) with
            {
                CornerRadiusDp = 34f,
                ThicknessDp = 20f,
                RimChalkiness = 0.90f
            },
            contentRole: SeaGlassContentRole.NavigationChrome)
    {
        Items = items;
        SelectedIndex = Math.Clamp(selectedIndex, 0, Math.Max(0, items.Count - 1));
        _leadIndex = SelectedIndex;
        _trailIndex = SelectedIndex;
        OnSelect = onSelect;
        BarMaterial = barMaterial ?? SeaGlassTheme.Current.Palette.SurfaceGlass;
        IndicatorMaterial = indicatorMaterial ?? SeaGlassTheme.Current.Palette.PrimaryGlass;
        ActionLabel = actionLabel;
        ActionDetached = actionDetached;
        ShowActionOrb = showActionOrb;
        OnActionClick = onActionClick;
        _actionSeparationDp = actionDetached ? 18f : 0f;
        SizePx = new Vector2(520f, 68f);
    }

    public void SelectTab(int index)
    {
        SelectedIndex = Math.Clamp(index, 0, Math.Max(0, Items.Count - 1));
        OnSelect(SelectedIndex);
    }

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        float density = MathF.Max(0.5f, activeTheme.DpiScale);

        SeaGlassSpringSpec leadSpring = new(DampingRatio: 0.68f, Stiffness: 540f);
        SeaGlassSpringSpec trailSpring = new(DampingRatio: 0.72f, Stiffness: 310f);
        leadSpring.Step(ref _leadIndex, ref _leadVel, SelectedIndex, dtSeconds);
        trailSpring.Step(ref _trailIndex, ref _trailVel, SelectedIndex, dtSeconds);

        SeaGlassSpringSpec orbSpring = new(DampingRatio: 0.54f, Stiffness: 300f);
        orbSpring.Step(ref _actionSeparationDp, ref _sepVel, ActionDetached ? 18f : 0f, dtSeconds);

        float orbMotionMalleability = ShowActionOrb
            ? Math.Clamp(MathF.Abs(_actionSeparationDp - (ActionDetached ? 18f : 0f)) / 10f, 0f, 1f)
            : 0f;
        float barMalleability = Math.Clamp(MathF.Max(MotionEnergy * 0.55f, orbMotionMalleability), 0f, 1f);
        SeaGlassMaterial activeBarMat = BarMaterial.Deaged(barMalleability) with
        {
            CornerRadiusDp = 34f,
            ThicknessDp = 20f,
            RimChalkiness = 0.90f
        };
        Material = activeBarMat;

        float totalW = MathF.Max(120f, SizePx.X);
        float totalH = MathF.Max(40f, SizePx.Y);
        float barHeightPx = totalH;
        float orbSizePx = MathF.Min(totalH, 62f * density);
        float gapPx = _actionSeparationDp * density;

        List<SeaGlassSdfEngine.SdfNode> nodes = new(2);
        float centerY = totalH * 0.5f;

        if (!ShowActionOrb)
        {
            float barHalfW = totalW * 0.5f;
            float barHalfH = barHeightPx * 0.5f;
            nodes.Add(new SeaGlassSdfEngine.SdfNode(
                centerX: barHalfW,
                centerY: centerY,
                halfWidth: barHalfW,
                halfHeight: barHalfH,
                uniformCornerRadius: barHalfH,
                cornerSmoothing: activeBarMat.CornerSmoothing,
                tintColorArgb: activeBarMat.TintColorArgb,
                thicknessPx: barHeightPx * 0.48f));
        }
        else
        {
            float mainBarW = MathF.Max(orbSizePx * 2f, totalW - orbSizePx - MathF.Max(0f, gapPx) - 4f * density);
            float barHalfW = mainBarW * 0.5f;
            float barHalfH = barHeightPx * 0.5f;
            float orbHalf = orbSizePx * 0.5f;
            float orbCenterX = MathF.Min(totalW - orbHalf, mainBarW + gapPx + orbHalf);

            nodes.Add(new SeaGlassSdfEngine.SdfNode(
                centerX: barHalfW,
                centerY: centerY,
                halfWidth: barHalfW,
                halfHeight: barHalfH,
                uniformCornerRadius: barHalfH,
                cornerSmoothing: activeBarMat.CornerSmoothing,
                tintColorArgb: activeBarMat.TintColorArgb,
                thicknessPx: barHeightPx * 0.48f));

            nodes.Add(new SeaGlassSdfEngine.SdfNode(
                centerX: orbCenterX,
                centerY: centerY,
                halfWidth: orbHalf,
                halfHeight: orbHalf,
                uniformCornerRadius: orbHalf,
                cornerSmoothing: ActiveIndicatorMaterial.CornerSmoothing,
                tintColorArgb: ActiveIndicatorMaterial.TintColorArgb,
                thicknessPx: orbSizePx * 0.52f));
        }

        return EvaluateFrame(
            dtSeconds: dtSeconds,
            theme: activeTheme,
            nodesOverride: nodes,
            smoothRadiusDp: 42f);
    }
}

/// <summary>
/// Corner-Embedded SeaGlass Pod using <see cref="SeaGlassCornerAdaptationMode.FlushCornerEmbed"/>
/// so outer bezel-facing corners match <c>R_concentric = max(r_min, R_outer - inset)</c>.
///
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassCornerEmbeddedPod</c> on Android.
/// </summary>
public sealed class SeaGlassCornerEmbeddedPod : SeaGlassSurface
{
    public string Label { get; set; }
    public string? SubLabel { get; set; }

    public SeaGlassCornerEmbeddedPod(
        string label,
        string? subLabel = null,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? material = null,
        Action? onClick = null)
        : base(
            stageState: stageState,
            material: material ?? SeaGlassTheme.Current.Palette.PrimaryGlass,
            cornerAdaptationMode: SeaGlassCornerAdaptationMode.FlushCornerEmbed,
            contentRole: SeaGlassContentRole.TextAndIcons,
            onClick: onClick)
    {
        Label = label;
        SubLabel = subLabel;
        SizePx = new Vector2(148f, 78f);
        BasePaddingPx = new Vector4(14f, 10f, 14f, 10f);
    }
}
