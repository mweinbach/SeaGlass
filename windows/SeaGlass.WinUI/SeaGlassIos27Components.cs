using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

// =============================================================================================
// 1. iOS-Style Floating Concentric Sheet (`SeaGlassFloatingSheet`)
// =============================================================================================

/// <summary>
/// Quick-action button inside a <see cref="SeaGlassFloatingSheet"/> action strip.
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassSheetQuickAction</c> on Android.
/// </summary>
public sealed record SeaGlassSheetQuickAction(
    string Id,
    string Label,
    SeaGlassGlyph Glyph,
    bool Active = false);

/// <summary>
/// iOS-Style Floating Concentric SeaGlass Detent Sheet &amp; Page Overlay (<c>SeaGlassFloatingSheet</c>).
/// Matches device bezel corners concentrically (<c>R_sheet = R_bezel - inset</c>) and applies
/// Temporal De-Aging (<see cref="SeaGlassMaterial.Deaged"/>) during sheet morph and drag motion.
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassFloatingSheet</c> on Android.
/// </summary>
public sealed class SeaGlassFloatingSheet : SeaGlassSurface
{
    private float _visibilityProgress;
    private float _visibilityVel;
    private float _pageMorphProgress;
    private float _pageMorphVel;
    private readonly SeaGlassMaterial _baseMaterial;

    public bool Visible { get; private set; }
    public Action OnDismissRequest { get; }
    public float OuterInsetDp { get; set; }
    public bool EnableMorphAnimation { get; set; }
    public int ActiveSubPage { get; private set; }
    public float DragOffsetPx { get; private set; }
    public IReadOnlyList<SeaGlassSheetQuickAction> QuickActions { get; set; }

    public float VisibilityProgress => Math.Clamp(_visibilityProgress, 0f, 1.15f);
    public float PageMorphProgress => Math.Clamp(_pageMorphProgress, 0f, 1.15f);

    public SeaGlassFloatingSheet(
        bool visible,
        Action onDismissRequest,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? material = null,
        float outerInsetDp = 8f,
        bool enableMorphAnimation = true,
        IReadOnlyList<SeaGlassSheetQuickAction>? quickActions = null)
        : base(
            stageState: stageState ?? SeaGlassStageHost.AmbientStageState,
            material: material ?? (SeaGlassTheme.Current.Palette.SurfaceGlass with
            {
                CornerRadiusDp = 42f,
                Transparency = 0.56f,
                Coarseness = 0.44f,
                FrostRadiusDp = 30f,
                ThicknessDp = 22f,
                RimChalkiness = 0.88f
            }),
            cornerAdaptationMode: SeaGlassCornerAdaptationMode.ConcentricProximity)
    {
        _baseMaterial = Material;
        Visible = visible;
        _visibilityProgress = visible ? 1f : 0f;
        OnDismissRequest = onDismissRequest;
        OuterInsetDp = outerInsetDp;
        EnableMorphAnimation = enableMorphAnimation;
        QuickActions = quickActions ??
        [
            new("pin", "Pin", SeaGlassGlyph.Bookmark, Active: true),
            new("unread", "Unread", SeaGlassGlyph.Bell),
            new("fork", "Fork", SeaGlassGlyph.Layers),
            new("export", "Export", SeaGlassGlyph.Share)
        ];
        SizePx = new Vector2(460f, 520f);
    }

    public void SetVisible(bool visible) => Visible = visible;

    public void SwitchSubPage(int pageIndex) => ActiveSubPage = Math.Clamp(pageIndex, 0, 1);

    public void SetDragOffset(float dragOffsetPx)
    {
        DragOffsetPx = MathF.Max(-48f, dragOffsetPx);
        if (DragOffsetPx > 140f)
        {
            Visible = false;
            DragOffsetPx = 0f;
            OnDismissRequest();
        }
    }

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        SeaGlassSpringSpec spatial = new(DampingRatio: 0.64f, Stiffness: 390f);
        spatial.Step(ref _visibilityProgress, ref _visibilityVel, Visible ? 1f : 0f, dtSeconds);
        spatial.Step(ref _pageMorphProgress, ref _pageMorphVel, ActiveSubPage, dtSeconds);

        float vp = Math.Clamp(_visibilityProgress, 0f, 1f);
        float pmp = Math.Clamp(_pageMorphProgress, 0f, 1f);
        float inFlightEnergy = MathF.Max(
            4f * vp * (1f - vp),
            MathF.Max(4f * pmp * (1f - pmp), Math.Clamp(MathF.Abs(DragOffsetPx) / 140f, 0f, 1f)));

        Material = EnableMorphAnimation
            ? _baseMaterial.Deaged(inFlightEnergy)
            : _baseMaterial;

        return EvaluateFrame(dtSeconds, activeTheme);
    }
}

// =============================================================================================
// 2. iOS Context Menu, Action Sheet, and Live Activity Pill (`SeaGlassIosMenus`)
// =============================================================================================

/// <summary>
/// Single item inside an iOS-style <see cref="SeaGlassContextMenuOverlay"/>.
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassContextMenuItem</c> on Android.
/// </summary>
public sealed record SeaGlassContextMenuItem(
    string Id,
    string Title,
    SeaGlassGlyph Glyph,
    string? Subtitle = null,
    bool IsDestructive = false,
    bool StartsNewSection = false,
    bool Checked = false,
    string? SubmenuTitle = null,
    IReadOnlyList<SeaGlassContextMenuItem>? SubmenuItems = null)
{
    public bool HasSubmenu => SubmenuItems is { Count: > 0 };
}

/// <summary>
/// iOS-Style Context Menu with Target Preview Lift, Quick-Reaction Pill Bar,
/// and In-Place Submenu Morphing (<c>SeaGlassContextMenuOverlay</c>).
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassContextMenuOverlay</c> on Android.
/// </summary>
public sealed class SeaGlassContextMenuOverlay : SeaGlassSurface
{
    private float _openProgress;
    private float _openVel;
    private float _submenuProgress;
    private float _submenuVel;
    private readonly SeaGlassMaterial _baseMaterial;

    public bool Visible { get; private set; }
    public Action OnDismissRequest { get; }
    public Action<SeaGlassContextMenuItem> OnItemSelected { get; }
    public IReadOnlyList<SeaGlassContextMenuItem> Items { get; }
    public SeaGlassContextMenuItem? ActiveSubmenuParent { get; private set; }

    public float OpenProgress => Math.Clamp(_openProgress, 0f, 1.15f);
    public float SubmenuProgress => Math.Clamp(_submenuProgress, 0f, 1.15f);
    public float TargetPreviewLiftScale => 1f + 0.035f * Math.Clamp(_openProgress, 0f, 1f);

    public SeaGlassContextMenuOverlay(
        bool visible,
        Action onDismissRequest,
        Action<SeaGlassContextMenuItem> onItemSelected,
        IReadOnlyList<SeaGlassContextMenuItem>? items = null,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? material = null)
        : base(
            stageState: stageState ?? SeaGlassStageHost.AmbientStageState,
            material: material ?? (SeaGlassTheme.Current.Palette.SurfaceGlass with
            {
                CornerRadiusDp = 26f,
                Transparency = 0.54f,
                Coarseness = 0.44f,
                FrostRadiusDp = 30f,
                ThicknessDp = 20f,
                RimChalkiness = 0.88f
            }))
    {
        _baseMaterial = Material;
        Visible = visible;
        _openProgress = visible ? 1f : 0f;
        OnDismissRequest = onDismissRequest;
        OnItemSelected = onItemSelected;
        Items = items ?? DefaultIosContextMenuItems();
        SizePx = new Vector2(280f, 340f);
    }

    public void SetVisible(bool visible)
    {
        Visible = visible;
        if (!visible) ActiveSubmenuParent = null;
    }

    public void ActivateItem(SeaGlassContextMenuItem item)
    {
        if (item.HasSubmenu)
        {
            ActiveSubmenuParent = item;
        }
        else
        {
            OnItemSelected(item);
            SetVisible(false);
        }
    }

    public void PopSubmenu() => ActiveSubmenuParent = null;

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        SeaGlassSpringSpec spatial = new(DampingRatio: 0.64f, Stiffness: 410f);
        spatial.Step(ref _openProgress, ref _openVel, Visible ? 1f : 0f, dtSeconds);
        spatial.Step(ref _submenuProgress, ref _submenuVel, ActiveSubmenuParent is not null ? 1f : 0f, dtSeconds);

        float op = Math.Clamp(_openProgress, 0f, 1f);
        float sp = Math.Clamp(_submenuProgress, 0f, 1f);
        float energy = MathF.Max(4f * op * (1f - op), 4f * sp * (1f - sp));
        Material = _baseMaterial.Deaged(energy);

        return EvaluateFrame(dtSeconds, activeTheme);
    }

    public static IReadOnlyList<SeaGlassContextMenuItem> DefaultIosContextMenuItems() =>
    [
        new("share", "Share Optical Preset…", SeaGlassGlyph.Share),
        new("palette", "Assign Glass Palette", SeaGlassGlyph.Sparkles,
            SubmenuTitle: "Choose SeaGlass Palette",
            SubmenuItems:
            [
                new("pal_mint", "Seafoam Mint", SeaGlassGlyph.Check, Checked: true),
                new("pal_cobalt", "Cobalt Tide", SeaGlassGlyph.Sparkles),
                new("pal_amber", "Beach Amber", SeaGlassGlyph.Sparkles),
                new("pal_lavender", "Sea Lavender", SeaGlassGlyph.Sparkles)
            ]),
        new("pin", "Pin to Control Platter", SeaGlassGlyph.Bookmark),
        new("inspect", "Inspect std140 UBO Slots", SeaGlassGlyph.Info, StartsNewSection: true),
        new("reset", "Reset Surface Optics", SeaGlassGlyph.Close, IsDestructive: true, StartsNewSection: true)
    ];
}

/// <summary>
/// iOS-Style Frosted Action Sheet with grouped actions and a detached concentric Cancel pill.
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassActionSheet</c> on Android.
/// </summary>
public sealed class SeaGlassActionSheet : SeaGlassSurface
{
    private float _progress;
    private float _vel;
    private readonly SeaGlassMaterial _baseMaterial;

    public string Title { get; set; }
    public string? Message { get; set; }
    public bool Visible { get; set; }
    public Action OnDismissRequest { get; }
    public IReadOnlyList<SeaGlassContextMenuItem> Actions { get; }
    public float OpenProgress => Math.Clamp(_progress, 0f, 1.15f);

    public SeaGlassActionSheet(
        string title,
        bool visible,
        Action onDismissRequest,
        IReadOnlyList<SeaGlassContextMenuItem>? actions = null,
        string? message = null,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? material = null)
        : base(
            stageState: stageState ?? SeaGlassStageHost.AmbientStageState,
            material: material ?? (SeaGlassTheme.Current.Palette.SurfaceGlass with
            {
                CornerRadiusDp = 32f,
                ThicknessDp = 20f,
                RimChalkiness = 0.88f
            }),
            cornerAdaptationMode: SeaGlassCornerAdaptationMode.ConcentricProximity)
    {
        _baseMaterial = Material;
        Title = title;
        Message = message;
        Visible = visible;
        _progress = visible ? 1f : 0f;
        OnDismissRequest = onDismissRequest;
        Actions = actions ?? SeaGlassContextMenuOverlay.DefaultIosContextMenuItems();
        SizePx = new Vector2(420f, 310f);
    }

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        activeTheme.MotionScheme.DefaultSpatial.Step(ref _progress, ref _vel, Visible ? 1f : 0f, dtSeconds);
        float p = Math.Clamp(_progress, 0f, 1f);
        Material = _baseMaterial.Deaged(4f * p * (1f - p));
        return EvaluateFrame(dtSeconds, activeTheme);
    }
}

/// <summary>
/// iOS-Style Live Activity / Dynamic Island Expandable SeaGlass Capsule (<c>SeaGlassLiveActivityPill</c>).
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassLiveActivityPill</c> on Android.
/// </summary>
public sealed class SeaGlassLiveActivityPill : SeaGlassSurface
{
    private float _expandWidthProgress;
    private float _expandWidthVel;
    private float _expandHeightProgress;
    private float _expandHeightVel;
    private readonly SeaGlassMaterial _baseMaterial;

    public string Title { get; set; }
    public string Subtitle { get; set; }
    public stringTrailingMetric MetricText { get; set; }
    public bool Expanded { get; private set; }
    public float ExpandWidthProgress => Math.Clamp(_expandWidthProgress, 0f, 1.18f);
    public float ExpandHeightProgress => Math.Clamp(_expandHeightProgress, 0f, 1.18f);
    public float ExpandProgress => Math.Clamp((_expandWidthProgress + _expandHeightProgress) * 0.5f, 0f, 1.18f);

    public readonly record struct stringTrailingMetric(string Value, string Unit);

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
        _baseMaterial = Material;
        Title = title;
        Subtitle = subtitle;
        MetricText = new stringTrailingMetric("0.42", "ms");
        Expanded = expanded;
        _expandWidthProgress = expanded ? 1f : 0f;
        _expandHeightProgress = expanded ? 1f : 0f;
        SizePx = expanded ? new Vector2(380f, 128f) : new Vector2(236f, 44f);
        OnClick = () => SetExpanded(!Expanded);
    }

    public void SetExpanded(bool expanded) => Expanded = expanded;

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        float target = Expanded ? 1f : 0f;
        SeaGlassSpringSpec widthSpring = new(DampingRatio: 0.66f, Stiffness: 440f);
        SeaGlassSpringSpec heightSpring = new(DampingRatio: 0.62f, Stiffness: 340f);
        widthSpring.Step(ref _expandWidthProgress, ref _expandWidthVel, target, dtSeconds);
        heightSpring.Step(ref _expandHeightProgress, ref _expandHeightVel, target, dtSeconds);

        float pw = Math.Clamp(_expandWidthProgress, 0f, 1.15f);
        float ph = Math.Clamp(_expandHeightProgress, 0f, 1.15f);
        float p = Math.Clamp((pw + ph) * 0.5f, 0f, 1f);
        SizePx = new Vector2(236f + (380f - 236f) * pw, 44f + (128f - 44f) * ph);
        Material = (_baseMaterial with { CornerRadiusDp = 22f + 12f * p }).Deaged(4f * p * (1f - p));

        return EvaluateFrame(dtSeconds, activeTheme);
    }
}

// =============================================================================================
// 3. iOS 27 Component Suite (`SeaGlassIos27Components`)
// =============================================================================================

/// <summary>
/// iOS 27 Optical Magnifier Loupe &amp; Precision Scrubbing Bar (<c>SeaGlassMagnifierScrubber</c>).
/// Rises out of the horizontal track across a vertical T-junction <c>sminCubic</c> liquid bridge
/// while de-aging the loupe lens to crystal-clear glass (<c>Deaged(1.0f)</c>).
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassMagnifierScrubber</c> on Android.
/// </summary>
public sealed class SeaGlassMagnifierScrubber : SeaGlassSurface
{
    private float _loupeLiftProgress;
    private float _loupeLiftVel;

    public float Value { get; private set; }
    public Action<float> OnValueChange { get; }
    public string Label { get; set; }
    public bool LoupeActive { get; set; } = true;
    public SeaGlassMaterial TrackMaterial { get; set; }
    public SeaGlassMaterial LoupeMaterial { get; set; }
    public float LoupeLiftProgress => Math.Clamp(_loupeLiftProgress, 0f, 1.15f);

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
        OnValueChange(Value);
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
}

/// <summary>
/// Single notification item inside a <see cref="SeaGlassNotificationStack"/>.
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassNotificationItem</c> on Android.
/// </summary>
public sealed record SeaGlassNotificationItem(
    string Id,
    string AppName,
    string TimeLabel,
    string Title,
    string Body,
    SeaGlassGlyph Glyph);

/// <summary>
/// iOS 27 Depth-Stacked Notification Rollup Stack (<c>SeaGlassNotificationStack</c>) that fans
/// out from a 3-layer perspective deck into individual frosted cards with Temporal De-Aging.
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassNotificationStack</c> on Android.
/// </summary>
public sealed class SeaGlassNotificationStack : SeaGlassSurface
{
    private float _fanProgress;
    private float _fanVel;
    private readonly SeaGlassMaterial _baseMaterial;

    public IReadOnlyList<SeaGlassNotificationItem> Notifications { get; }
    public bool Expanded { get; private set; }
    public float FanProgress => Math.Clamp(_fanProgress, 0f, 1.15f);

    public SeaGlassNotificationStack(
        bool expanded = false,
        IReadOnlyList<SeaGlassNotificationItem>? notifications = null,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? material = null)
        : base(
            stageState: stageState ?? SeaGlassStageHost.AmbientStageState,
            material: material ?? SeaGlassTheme.Current.Palette.SurfaceGlass)
    {
        _baseMaterial = Material;
        Expanded = expanded;
        _fanProgress = expanded ? 1f : 0f;
        Notifications = notifications ?? DefaultNotifications();
        SizePx = new Vector2(440f, expanded ? 260f : 112f);
        OnClick = () => ToggleExpanded();
    }

    public void ToggleExpanded() => Expanded = !Expanded;

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        SeaGlassSpringSpec spatial = new(DampingRatio: 0.64f, Stiffness: 400f);
        spatial.Step(ref _fanProgress, ref _fanVel, Expanded ? 1f : 0f, dtSeconds);

        float fp = Math.Clamp(_fanProgress, 0f, 1f);
        SizePx = new Vector2(440f, 112f + (260f - 112f) * fp);
        Material = _baseMaterial.Deaged(4f * fp * (1f - fp));
        return EvaluateFrame(dtSeconds, activeTheme);
    }

    public static IReadOnlyList<SeaGlassNotificationItem> DefaultNotifications() =>
    [
        new("n1", "OPTICS LAB", "now", "Analytical Single-Pass SDF Active",
            "sgRoundedBoxSdfAndGrad reduced single-node evaluations from 5 passes to 1.", SeaGlassGlyph.Sparkles),
        new("n2", "TEMPORAL GLASS", "2m ago", "De-Aging & Solidification Enabled",
            "In-flight glass melts into clear liquid and re-crystallizes on settle.", SeaGlassGlyph.Layers),
        new("n3", "DIRECT2D HLSL", "6m ago", "256-Byte cbuffer Delta Tracking",
            "Dirty-slot bitmask skips redundant GPU constant-buffer transfers.", SeaGlassGlyph.Tune)
    ];
}

/// <summary>
/// iOS 27 Cylindrical 3D Drum-Wheel Picker with a de-aged refractive selection lens bar.
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassWheelPicker</c> on Android.
/// </summary>
public sealed class SeaGlassWheelPicker : SeaGlassSurface
{
    private float _animatedIndex;
    private float _indexVel;

    public IReadOnlyList<string> Items { get; }
    public int SelectedIndex { get; private set; }
    public Action<int> OnSelectedIndexChange { get; }
    public float AnimatedIndex => _animatedIndex;

    public SeaGlassWheelPicker(
        IReadOnlyList<string> items,
        int selectedIndex,
        Action<int> onSelectedIndexChange,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? material = null)
        : base(
            stageState: stageState ?? SeaGlassStageHost.AmbientStageState,
            material: material ?? SeaGlassTheme.Current.Palette.SurfaceGlass)
    {
        Items = items;
        SelectedIndex = Math.Clamp(selectedIndex, 0, Math.Max(0, items.Count - 1));
        _animatedIndex = SelectedIndex;
        OnSelectedIndexChange = onSelectedIndexChange;
        SizePx = new Vector2(360f, 180f);
    }

    public void SelectItem(int index)
    {
        SelectedIndex = Math.Clamp(index, 0, Math.Max(0, Items.Count - 1));
        OnSelectedIndexChange(SelectedIndex);
    }

    public (float AngleRadians, float Scale, float Alpha) EvaluateDrumRowTransform(int itemIndex)
    {
        float delta = itemIndex - _animatedIndex;
        float clampedDelta = Math.Clamp(delta, -3f, 3f);
        float angleRad = clampedDelta * 0.42f;
        float cosA = MathF.Cos(angleRad);
        float scale = 0.78f + 0.28f * MathF.Max(0f, cosA);
        float alpha = Math.Clamp(MathF.Pow(MathF.Max(0f, cosA), 1.4f), 0.15f, 1.0f);
        return (angleRad, scale, alpha);
    }

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        SeaGlassSpringSpec wheelSpring = new(DampingRatio: 0.68f, Stiffness: 420f);
        wheelSpring.Step(ref _animatedIndex, ref _indexVel, SelectedIndex, dtSeconds);
        return EvaluateFrame(dtSeconds, activeTheme);
    }
}

/// <summary>
/// iOS 27 Expandable Control Center Platter Grid (<c>SeaGlassControlCenterGrid</c>).
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassControlCenterGrid</c> on Android.
/// </summary>
public sealed class SeaGlassControlCenterGrid : SeaGlassSurface
{
    private float _expandProgress;
    private float _expandVel;
    private readonly SeaGlassMaterial _baseMaterial;

    public int ExpandedPlatterIndex { get; private set; } = -1;
    public float ExpandProgress => Math.Clamp(_expandProgress, 0f, 1.15f);

    public SeaGlassControlCenterGrid(
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? material = null)
        : base(
            stageState: stageState ?? SeaGlassStageHost.AmbientStageState,
            material: material ?? SeaGlassTheme.Current.Palette.SurfaceGlass)
    {
        _baseMaterial = Material;
        SizePx = new Vector2(460f, 220f);
    }

    public void TogglePlatterExpansion(int platterIndex)
    {
        ExpandedPlatterIndex = ExpandedPlatterIndex == platterIndex ? -1 : Math.Clamp(platterIndex, 0, 3);
    }

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        SeaGlassSpringSpec spatial = new(DampingRatio: 0.64f, Stiffness: 400f);
        spatial.Step(ref _expandProgress, ref _expandVel, ExpandedPlatterIndex >= 0 ? 1f : 0f, dtSeconds);
        float p = Math.Clamp(_expandProgress, 0f, 1f);
        Material = _baseMaterial.Deaged(4f * p * (1f - p));
        return EvaluateFrame(dtSeconds, activeTheme);
    }
}
