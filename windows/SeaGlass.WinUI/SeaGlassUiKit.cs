using System;
using System.Collections.Generic;
using System.Numerics;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Built-in crisp vector glyphs for SeaGlass UI components, toolbars, and navigation controls.
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassGlyph</c> on Android (all 20 glyphs).
/// </summary>
public enum SeaGlassGlyph
{
    Home,
    Search,
    Tune,
    Sparkles,
    Play,
    Pause,
    Next,
    Check,
    Close,
    Plus,
    Heart,
    Share,
    Bookmark,
    Bell,
    Settings,
    Back,
    Menu,
    Info,
    Layers,
    AudioWave,
    PhotoMountain,
    Microphone,
    BrandG
}

/// <summary>
/// Renders a crisp vector <see cref="SeaGlassGlyph"/> harmonized with the active SeaGlass content color.
/// </summary>
public static class SeaGlassIcon
{
    public static string ToUnicodeSymbol(SeaGlassGlyph glyph) => glyph switch
    {
        SeaGlassGlyph.Home => "⌂",
        SeaGlassGlyph.Search => "⌕",
        SeaGlassGlyph.Tune => "⊶",
        SeaGlassGlyph.Sparkles => "✦",
        SeaGlassGlyph.Play => "▶",
        SeaGlassGlyph.Pause => "❚❚",
        SeaGlassGlyph.Next => "▶▶",
        SeaGlassGlyph.Check => "✓",
        SeaGlassGlyph.Close => "✕",
        SeaGlassGlyph.Plus => "＋",
        SeaGlassGlyph.Heart => "♡",
        SeaGlassGlyph.Share => "⤴",
        SeaGlassGlyph.Bookmark => "⚑",
        SeaGlassGlyph.Bell => "🔔",
        SeaGlassGlyph.Settings => "⚙",
        SeaGlassGlyph.Back => "‹",
        SeaGlassGlyph.Menu => "☰",
        SeaGlassGlyph.Info => "ⓘ",
        SeaGlassGlyph.Layers => "❖",
        SeaGlassGlyph.AudioWave => "ılı",
        SeaGlassGlyph.PhotoMountain => "⛰",
        SeaGlassGlyph.Microphone => "🎤",
        SeaGlassGlyph.BrandG => "G",
        _ => "✦"
    };

    /// <summary>
    /// Generates normalized [0..w, 0..h] XAML vector path data for any <see cref="SeaGlassGlyph"/>.
    /// </summary>
    public static string BuildGlyphXamlPathData(SeaGlassGlyph glyph, float w = 20f, float h = 20f)
    {
        SeaGlassVectorPath2D p = new();
        switch (glyph)
        {
            case SeaGlassGlyph.Home:
                p.MoveTo(w * 0.50f, h * 0.12f);
                p.LineTo(w * 0.88f, h * 0.45f);
                p.LineTo(w * 0.78f, h * 0.45f);
                p.LineTo(w * 0.78f, h * 0.86f);
                p.LineTo(w * 0.22f, h * 0.86f);
                p.LineTo(w * 0.22f, h * 0.45f);
                p.LineTo(w * 0.12f, h * 0.45f);
                p.Close();
                break;

            case SeaGlassGlyph.Play:
                p.MoveTo(w * 0.24f, h * 0.16f);
                p.LineTo(w * 0.84f, h * 0.50f);
                p.LineTo(w * 0.24f, h * 0.84f);
                p.Close();
                break;

            case SeaGlassGlyph.Check:
                p.MoveTo(w * 0.18f, h * 0.52f);
                p.LineTo(w * 0.42f, h * 0.76f);
                p.LineTo(w * 0.84f, h * 0.26f);
                break;

            case SeaGlassGlyph.Plus:
                p.MoveTo(w * 0.50f, h * 0.18f);
                p.LineTo(w * 0.50f, h * 0.82f);
                p.MoveTo(w * 0.18f, h * 0.50f);
                p.LineTo(w * 0.82f, h * 0.50f);
                break;

            case SeaGlassGlyph.Close:
                p.MoveTo(w * 0.22f, h * 0.22f);
                p.LineTo(w * 0.78f, h * 0.78f);
                p.MoveTo(w * 0.78f, h * 0.22f);
                p.LineTo(w * 0.22f, h * 0.78f);
                break;

            case SeaGlassGlyph.Back:
                p.MoveTo(w * 0.64f, h * 0.18f);
                p.LineTo(w * 0.28f, h * 0.50f);
                p.LineTo(w * 0.64f, h * 0.82f);
                break;

            default:
                p.MoveTo(w * 0.50f, h * 0.10f);
                p.LineTo(w * 0.90f, h * 0.50f);
                p.LineTo(w * 0.50f, h * 0.90f);
                p.LineTo(w * 0.10f, h * 0.50f);
                p.Close();
                break;
        }
        return p.ToXamlPathData();
    }
}

/// <summary>
/// Tactile circular/squircle SeaGlass icon button with 3D press tilt and HDR caustic bloom.
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassIconButton</c> on Android.
/// </summary>
public sealed class SeaGlassIconButton : SeaGlassButton
{
    public SeaGlassGlyph Glyph { get; set; }
    public bool Selected { get; private set; }
    public float ButtonSizeDp { get; }

    public SeaGlassIconButton(
        SeaGlassGlyph glyph,
        Action onClick,
        SeaGlassStageState? stageState = null,
        bool selected = false,
        float sizeDp = 44f,
        SeaGlassMaterial? material = null)
        : base(
            label: SeaGlassIcon.ToUnicodeSymbol(glyph),
            onClick: onClick,
            stageState: stageState,
            material: material ?? (selected
                ? SeaGlassTheme.Current.Palette.PrimaryGlass
                : SeaGlassTheme.Current.Palette.SurfaceGlass),
            leadingGlyph: glyph)
    {
        Glyph = glyph;
        ButtonSizeDp = sizeDp;
        SizePx = new Vector2(sizeDp, sizeDp);
        BasePaddingPx = Vector4.Zero;
        SetSelected(selected, material);
    }

    public void SetSelected(bool selected, SeaGlassMaterial? customMaterial = null)
    {
        Selected = selected;
        SeaGlassMaterial baseMat = customMaterial ?? (selected
            ? SeaGlassTheme.Current.Palette.PrimaryGlass
            : SeaGlassTheme.Current.Palette.SurfaceGlass);
        Material = baseMat with
        {
            CornerRadiusDp = selected ? 16f : ButtonSizeDp * 0.5f,
            ThicknessDp = selected ? 18f : 13f
        };
    }
}

/// <summary>
/// Floating Liquid SeaGlass Top App Bar with navigation icon, title/subtitle, and trailing actions.
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassTopAppBar</c> on Android.
/// </summary>
public sealed class SeaGlassTopAppBar : SeaGlassSurface
{
    public string Title { get; set; }
    public string? Subtitle { get; set; }
    public SeaGlassGlyph? NavigationGlyph { get; set; }
    public Action? OnNavigationClick { get; set; }
    public IReadOnlyList<SeaGlassIconButton> Actions { get; set; }

    public SeaGlassTopAppBar(
        string title,
        SeaGlassStageState? stageState = null,
        string? subtitle = null,
        SeaGlassGlyph? navigationGlyph = SeaGlassGlyph.Back,
        Action? onNavigationClick = null,
        SeaGlassMaterial? material = null,
        IReadOnlyList<SeaGlassIconButton>? actions = null)
        : base(
            stageState: stageState,
            material: (material ?? SeaGlassTheme.Current.Palette.SurfaceGlass) with
            {
                CornerRadiusDp = 30f,
                ThicknessDp = 18f,
                RimChalkiness = 0.86f
            },
            contentRole: SeaGlassContentRole.NavigationChrome)
    {
        Title = title;
        Subtitle = subtitle;
        NavigationGlyph = navigationGlyph;
        OnNavigationClick = onNavigationClick;
        Actions = actions ?? [];
        SizePx = new Vector2(560f, 60f);
        BasePaddingPx = new Vector4(12f, 8f, 12f, 8f);
    }
}

/// <summary>
/// Interactive Liquid SeaGlass Search Bar with focus-reactive rim brightening, live text editing,
/// instant clear button, and optional trailing action button.
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassSearchBar</c> on Android.
/// </summary>
public sealed class SeaGlassSearchBar : SeaGlassSurface
{
    private float _focusProgress;
    private float _focusVel;
    private readonly SeaGlassMaterial _baseMaterial;

    public string Query { get; private set; }
    public Action<string> OnQueryChange { get; }
    public string Placeholder { get; set; }
    public SeaGlassGlyph? TrailingGlyph { get; set; }
    public Action? OnTrailingClick { get; set; }
    public bool IsFocused { get; set; }
    public float FocusProgress => _focusProgress;

    public SeaGlassSearchBar(
        string query,
        Action<string> onQueryChange,
        SeaGlassStageState? stageState = null,
        string placeholder = "Search glass components, shaders, palettes…",
        SeaGlassMaterial? material = null,
        SeaGlassGlyph? trailingGlyph = SeaGlassGlyph.Tune,
        Action? onTrailingClick = null)
        : base(
            stageState: stageState,
            material: material ?? SeaGlassTheme.Current.Palette.SurfaceGlass,
            contentRole: SeaGlassContentRole.NavigationChrome)
    {
        _baseMaterial = material ?? SeaGlassTheme.Current.Palette.SurfaceGlass;
        Query = query;
        OnQueryChange = onQueryChange;
        Placeholder = placeholder;
        TrailingGlyph = trailingGlyph;
        OnTrailingClick = onTrailingClick;
        SizePx = new Vector2(460f, 52f);
        BasePaddingPx = new Vector4(16f, 0f, 16f, 0f);
    }

    public void SetQuery(string query)
    {
        Query = query ?? string.Empty;
        OnQueryChange(Query);
    }

    public void ClearQuery() => SetQuery(string.Empty);

    public SeaGlassSurfaceFrameState StepFocusAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        activeTheme.MotionScheme.FastEffects.Step(ref _focusProgress, ref _focusVel, IsFocused ? 1f : 0f, dtSeconds);
        float fp = Math.Clamp(_focusProgress, 0f, 1f);

        Material = _baseMaterial with
        {
            CornerRadiusDp = 28f,
            ThicknessDp = 16f + 4f * fp,
            RimChalkiness = MathF.Min(0.98f, _baseMaterial.RimChalkiness + 0.16f * fp),
            TintDensity = MathF.Min(0.92f, _baseMaterial.TintDensity + 0.08f * fp)
        };

        return EvaluateFrame(dtSeconds, activeTheme);
    }
}

/// <summary>
/// Frosted SeaGlass TextField with optional label, leading icon, and focus-reactive corner &amp; rim morphing.
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassTextField</c> on Android.
/// </summary>
public sealed class SeaGlassTextField : SeaGlassSurface
{
    private float _focusProgress;
    private float _focusVel;
    private float _cornerRadius = 18f;
    private float _cornerVel;
    private readonly SeaGlassMaterial _baseMaterial;

    public string Value { get; private set; }
    public Action<string> OnValueChange { get; }
    public string? Label { get; set; }
    public string Placeholder { get; set; }
    public SeaGlassGlyph? LeadingGlyph { get; set; }
    public bool IsFocused { get; set; }

    public SeaGlassTextField(
        string value,
        Action<string> onValueChange,
        SeaGlassStageState? stageState = null,
        string? label = null,
        string placeholder = "",
        SeaGlassGlyph? leadingGlyph = null,
        SeaGlassMaterial? material = null)
        : base(
            stageState: stageState,
            material: material ?? SeaGlassTheme.Current.Palette.SurfaceGlass,
            contentRole: SeaGlassContentRole.TextAndIcons)
    {
        _baseMaterial = material ?? SeaGlassTheme.Current.Palette.SurfaceGlass;
        Value = value;
        OnValueChange = onValueChange;
        Label = label;
        Placeholder = placeholder;
        LeadingGlyph = leadingGlyph;
        SizePx = new Vector2(360f, 58f);
        BasePaddingPx = new Vector4(16f, 10f, 16f, 10f);
    }

    public void SetValue(string newValue)
    {
        Value = newValue ?? string.Empty;
        OnValueChange(Value);
    }

    public SeaGlassSurfaceFrameState StepFocusAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        activeTheme.MotionScheme.FastEffects.Step(ref _focusProgress, ref _focusVel, IsFocused ? 1f : 0f, dtSeconds);
        activeTheme.MotionScheme.FastSpatial.Step(ref _cornerRadius, ref _cornerVel, IsFocused ? 24f : 18f, dtSeconds);

        float fp = Math.Clamp(_focusProgress, 0f, 1f);
        Material = _baseMaterial with
        {
            CornerRadiusDp = MathF.Max(8f, _cornerRadius),
            ThicknessDp = 14f + 4f * fp,
            RimChalkiness = MathF.Min(0.98f, _baseMaterial.RimChalkiness + 0.18f * fp)
        };

        return EvaluateFrame(dtSeconds, activeTheme);
    }
}

/// <summary>
/// Tactile Liquid SeaGlass Toggle Switch whose thumb is a 3D water-drop lens that glides
/// across the frosted capsule track with velocity stretch.
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassSwitch</c> on Android.
/// </summary>
public sealed class SeaGlassSwitch : SeaGlassSurface
{
    private float _thumbProgress;
    private float _thumbVel;
    private float _tintProgress;
    private float _tintVel;

    public bool Checked { get; private set; }
    public Action<bool> OnCheckedChange { get; }
    public SeaGlassMaterial CheckedMaterial { get; set; }
    public SeaGlassMaterial UncheckedMaterial { get; set; }

    public float ThumbProgress => _thumbProgress;
    public float MotionStretch => Math.Clamp(MathF.Abs((Checked ? 1f : 0f) - _thumbProgress), 0f, 1f);
    public float ThumbScaleX => 1f + 0.28f * MotionStretch;
    public float ThumbScaleY => 1f - 0.14f * MotionStretch;

    public SeaGlassSwitch(
        bool isChecked,
        Action<bool> onCheckedChange,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? checkedMaterial = null,
        SeaGlassMaterial? uncheckedMaterial = null)
        : base(
            stageState: stageState,
            material: isChecked
                ? (checkedMaterial ?? SeaGlassTheme.Current.Palette.PrimaryGlass)
                : (uncheckedMaterial ?? SeaGlassTheme.Current.Palette.SurfaceGlass),
            contentRole: SeaGlassContentRole.TextAndIcons)
    {
        Checked = isChecked;
        _thumbProgress = isChecked ? 1f : 0f;
        _tintProgress = isChecked ? 1f : 0f;
        OnCheckedChange = onCheckedChange;
        CheckedMaterial = checkedMaterial ?? SeaGlassTheme.Current.Palette.PrimaryGlass;
        UncheckedMaterial = uncheckedMaterial ?? SeaGlassTheme.Current.Palette.SurfaceGlass;
        SizePx = new Vector2(56f, 32f);
        OnClick = () => Toggle();
    }

    public void Toggle()
    {
        Checked = !Checked;
        OnCheckedChange(Checked);
    }

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        float target = Checked ? 1f : 0f;
        activeTheme.MotionScheme.FastSpatial.Step(ref _thumbProgress, ref _thumbVel, target, dtSeconds);
        activeTheme.MotionScheme.DefaultEffects.Step(ref _tintProgress, ref _tintVel, target, dtSeconds);

        float tp = Math.Clamp(_tintProgress, 0f, 1f);
        float energy = Math.Clamp(4f * tp * (1f - tp) + MotionStretch * 0.65f, 0f, 1f);
        Material = (SeaGlassMaterial.Lerp(UncheckedMaterial, CheckedMaterial, tp) with
        {
            CornerRadiusDp = 18f,
            ThicknessDp = 12f
        }).Deaged(energy);

        return EvaluateFrame(dtSeconds, activeTheme);
    }
}

/// <summary>
/// Material 3 Expressive SeaGlass Checkbox that morphs its contour between a rounded squircle
/// (unchecked) and an expressive 4-lobe Clover (<see cref="SeaGlassShapeKind.Cookie4"/>, checked).
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassCheckbox</c> on Android.
/// </summary>
public sealed class SeaGlassCheckbox : SeaGlassSurface
{
    private float _morphProgress;
    private float _morphVel;
    private float _tintProgress;
    private float _tintVel;

    public bool Checked { get; private set; }
    public Action<bool> OnCheckedChange { get; }
    public SeaGlassMaterial CheckedMaterial { get; set; }
    public SeaGlassMaterial UncheckedMaterial { get; set; }
    public float MorphProgress => _morphProgress;

    public SeaGlassCheckbox(
        bool isChecked,
        Action<bool> onCheckedChange,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? checkedMaterial = null,
        SeaGlassMaterial? uncheckedMaterial = null)
        : base(stageState: stageState)
    {
        Checked = isChecked;
        _morphProgress = isChecked ? 1f : 0f;
        _tintProgress = isChecked ? 1f : 0f;
        OnCheckedChange = onCheckedChange;
        CheckedMaterial = checkedMaterial ?? SeaGlassTheme.Current.Palette.PrimaryGlass;
        UncheckedMaterial = uncheckedMaterial ?? SeaGlassTheme.Current.Palette.SurfaceGlass;
        SizePx = new Vector2(28f, 28f);
        OnClick = () => Toggle();
    }

    public void Toggle()
    {
        Checked = !Checked;
        OnCheckedChange(Checked);
    }

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        float target = Checked ? 1f : 0f;
        activeTheme.MotionScheme.FastSpatial.Step(ref _morphProgress, ref _morphVel, target, dtSeconds);
        activeTheme.MotionScheme.DefaultEffects.Step(ref _tintProgress, ref _tintVel, target, dtSeconds);

        float tp = Math.Clamp(_tintProgress, 0f, 1f);
        float energy = Math.Clamp(4f * tp * (1f - tp), 0f, 1f);
        Material = (SeaGlassMaterial.Lerp(UncheckedMaterial, CheckedMaterial, tp) with
        {
            ThicknessDp = 12f + 4f * tp
        }).Deaged(energy);

        float mp = _morphProgress;
        return EvaluateFrame(
            dtSeconds: dtSeconds,
            theme: activeTheme,
            customContourBuilder: (wPx, hPx, outPath) =>
                SeaGlassShapeMorph.BuildMorphedPolygonPath(
                    startKind: SeaGlassShapeKind.Squircle,
                    endKind: SeaGlassShapeKind.Cookie4,
                    progress: mp,
                    widthPx: wPx,
                    heightPx: hPx,
                    outPath: outPath));
    }
}

/// <summary>
/// Frosted SeaGlass RadioButton with an animated inner droplet that blooms when selected.
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassRadioButton</c> on Android.
/// </summary>
public sealed class SeaGlassRadioButton : SeaGlassSurface
{
    private float _spatialProgress;
    private float _spatialVel;
    private float _tintProgress;
    private float _tintVel;

    public bool Selected { get; private set; }
    public Action OnSelect { get; }
    public SeaGlassMaterial SelectedMaterial { get; set; }
    public SeaGlassMaterial UnselectedMaterial { get; set; }
    public float InnerDropletScale => MathF.Max(0f, _spatialProgress);

    public SeaGlassRadioButton(
        bool selected,
        Action onSelect,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? selectedMaterial = null,
        SeaGlassMaterial? unselectedMaterial = null)
        : base(stageState: stageState, onClick: onSelect)
    {
        Selected = selected;
        _spatialProgress = selected ? 1f : 0f;
        _tintProgress = selected ? 1f : 0f;
        OnSelect = onSelect;
        SelectedMaterial = selectedMaterial ?? SeaGlassTheme.Current.Palette.PrimaryGlass;
        UnselectedMaterial = unselectedMaterial ?? SeaGlassTheme.Current.Palette.SurfaceGlass;
        SizePx = new Vector2(28f, 28f);
    }

    public void SetSelected(bool selected) => Selected = selected;

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        float target = Selected ? 1f : 0f;
        activeTheme.MotionScheme.FastSpatial.Step(ref _spatialProgress, ref _spatialVel, target, dtSeconds);
        activeTheme.MotionScheme.DefaultEffects.Step(ref _tintProgress, ref _tintVel, target, dtSeconds);

        float tp = Math.Clamp(_tintProgress, 0f, 1f);
        float energy = Math.Clamp(4f * tp * (1f - tp), 0f, 1f);
        Material = (SeaGlassMaterial.Lerp(UnselectedMaterial, SelectedMaterial, tp) with
        {
            CornerRadiusDp = 14f,
            ThicknessDp = 12f + 4f * tp
        }).Deaged(energy);

        return EvaluateFrame(dtSeconds, activeTheme);
    }
}

/// <summary>
/// Multi-Segment Liquid SeaGlass Segmented Button Row with a gliding frosted lozenge indicator
/// whose leading and trailing edges follow independent spatial springs (<c>LeadIndex</c> at
/// <c>k = 540</c>, <c>TrailIndex</c> at <c>k = 310</c>) so the indicator stretches into a
/// liquid capsule across intermediate segments before snapping into the target slot.
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassSegmentedButtonRow</c> on Android.
/// </summary>
public sealed class SeaGlassSegmentedButtonRow : SeaGlassSurface
{
    private float _leadIndex;
    private float _leadVel;
    private float _trailIndex;
    private float _trailVel;

    public IReadOnlyList<string> Options { get; }
    public int SelectedIndex { get; private set; }
    public Action<int> OnSelect { get; }
    public SeaGlassMaterial ContainerMaterial { get; set; }
    public SeaGlassMaterial IndicatorMaterial { get; set; }

    public float LeadIndex => _leadIndex;
    public float TrailIndex => _trailIndex;
    public float AnimatedIndex => (_leadIndex + _trailIndex) * 0.5f;
    public float LeftEdgeSlot => MathF.Min(_leadIndex, _trailIndex);
    public float RightEdgeSlot => MathF.Max(_leadIndex, _trailIndex) + 1f;
    public float LozengeSlotSpan => MathF.Max(1f, RightEdgeSlot - LeftEdgeSlot);
    public float MotionStretch => Math.Clamp(MathF.Abs(_leadIndex - _trailIndex), 0f, 2f);
    public float MotionEnergy => Math.Clamp(MotionStretch * 0.85f, 0f, 1f);
    public float IndicatorScaleX => LozengeSlotSpan;
    public float IndicatorScaleY => 1f - Math.Clamp(MotionStretch, 0f, 1.2f) * 0.08f;

    public SeaGlassMaterial ActiveIndicatorMaterial => (IndicatorMaterial with
    {
        ThicknessDp = IndicatorMaterial.ThicknessDp + 4f * MotionEnergy,
        ChromaticDispersion = Math.Clamp(IndicatorMaterial.ChromaticDispersion + 0.015f * MotionEnergy, 0f, 1f)
    }).Deaged(MotionEnergy);

    public SeaGlassSegmentedButtonRow(
        IReadOnlyList<string> options,
        int selectedIndex,
        Action<int> onSelect,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? containerMaterial = null,
        SeaGlassMaterial? indicatorMaterial = null)
        : base(
            stageState: stageState,
            material: (containerMaterial ?? SeaGlassTheme.Current.Palette.SurfaceGlass) with
            {
                CornerRadiusDp = 23f,
                ThicknessDp = 15f
            },
            contentRole: SeaGlassContentRole.NavigationChrome)
    {
        Options = options;
        SelectedIndex = Math.Clamp(selectedIndex, 0, Math.Max(0, options.Count - 1));
        _leadIndex = SelectedIndex;
        _trailIndex = SelectedIndex;
        OnSelect = onSelect;
        ContainerMaterial = containerMaterial ?? SeaGlassTheme.Current.Palette.SurfaceGlass;
        IndicatorMaterial = indicatorMaterial ?? SeaGlassTheme.Current.Palette.PrimaryGlass;
        SizePx = new Vector2(420f, 46f);
    }

    public void SelectOption(int index)
    {
        SelectedIndex = Math.Clamp(index, 0, Math.Max(0, Options.Count - 1));
        OnSelect(SelectedIndex);
    }

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        SeaGlassSpringSpec leadSpring = new(DampingRatio: 0.68f, Stiffness: 540f);
        SeaGlassSpringSpec trailSpring = new(DampingRatio: 0.72f, Stiffness: 310f);
        leadSpring.Step(ref _leadIndex, ref _leadVel, SelectedIndex, dtSeconds);
        trailSpring.Step(ref _trailIndex, ref _trailVel, SelectedIndex, dtSeconds);
        return EvaluateFrame(dtSeconds, activeTheme);
    }
}

/// <summary>
/// Material 3 Expressive Filter Chip that smoothly expands to reveal a leading checkmark when selected.
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassFilterChip</c> on Android.
/// </summary>
public sealed class SeaGlassFilterChip : SeaGlassButton
{
    public bool Selected { get; private set; }
    public SeaGlassMaterial SelectedMaterial { get; set; }
    public SeaGlassMaterial UnselectedMaterial { get; set; }

    public SeaGlassFilterChip(
        string label,
        bool selected,
        Action onClick,
        SeaGlassStageState? stageState = null,
        SeaGlassGlyph? leadingGlyph = null,
        SeaGlassMaterial? selectedMaterial = null,
        SeaGlassMaterial? unselectedMaterial = null)
        : base(label, onClick, stageState, leadingGlyph: selected ? SeaGlassGlyph.Check : leadingGlyph)
    {
        SelectedMaterial = selectedMaterial ?? SeaGlassTheme.Current.Palette.PrimaryGlass;
        UnselectedMaterial = unselectedMaterial ?? SeaGlassTheme.Current.Palette.SurfaceGlass;
        SizePx = new Vector2(124f, 38f);
        BasePaddingPx = new Vector4(14f, 9f, 14f, 9f);
        SetSelected(selected, leadingGlyph);
    }

    public void SetSelected(bool selected, SeaGlassGlyph? fallbackGlyph = null)
    {
        Selected = selected;
        LeadingGlyph = selected ? SeaGlassGlyph.Check : fallbackGlyph;
        Material = selected
            ? SelectedMaterial with { CornerRadiusDp = 22f, ThicknessDp = 16f }
            : UnselectedMaterial with { CornerRadiusDp = 14f, ThicknessDp = 10f };
    }
}

/// <summary>
/// High-chroma SeaGlass Badge for notification counts or status callouts.
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassBadge</c> on Android.
/// </summary>
public sealed class SeaGlassBadge : SeaGlassSurface
{
    public string Text { get; set; }

    public SeaGlassBadge(
        string text,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? material = null)
        : base(
            stageState: stageState,
            material: (material ?? SeaGlassTheme.Current.Palette.TertiaryGlass) with
            {
                CornerRadiusDp = 12f,
                ThicknessDp = 14f
            },
            contentRole: SeaGlassContentRole.TextAndIcons)
    {
        Text = text;
        SizePx = new Vector2(56f, 24f);
        BasePaddingPx = new Vector4(8f, 2f, 8f, 2f);
    }
}

/// <summary>
/// Expandable Liquid SeaGlass Floating Action Button (<c>SeaGlassExtendedFab</c>) that morphs
/// between a compact 56dp expressive icon orb and a wide pill with icon + label.
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassExtendedFab</c> on Android.
/// </summary>
public sealed class SeaGlassExtendedFab : SeaGlassButton
{
    public bool Expanded { get; private set; }
    public SeaGlassGlyph Glyph { get; set; }

    public SeaGlassExtendedFab(
        string text,
        SeaGlassGlyph glyph,
        bool expanded,
        Action onClick,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? material = null)
        : base(text, onClick, stageState, material, glyph)
    {
        Glyph = glyph;
        SetExpanded(expanded, material);
    }

    public void SetExpanded(bool expanded, SeaGlassMaterial? baseMaterial = null)
    {
        Expanded = expanded;
        SeaGlassMaterial mat = baseMaterial ?? Material;
        Material = mat with
        {
            CornerRadiusDp = expanded ? 28f : 20f,
            ThicknessDp = 20f,
            RimChalkiness = 0.88f
        };
        SizePx = expanded ? new Vector2(172f, 56f) : new Vector2(56f, 56f);
    }
}

/// <summary>
/// Material 3 Expressive Floating Split Toolbar whose action pill and primary FAB orb coalesce
/// across a liquid <c>sminCubic</c> SDF bridge when docked and split apart when detached.
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassFloatingToolbar</c> on Android.
/// </summary>
public sealed class SeaGlassFloatingToolbar : SeaGlassSurface
{
    private float _gapDp;
    private float _gapVel;

    public IReadOnlyList<(SeaGlassGlyph Glyph, string Label)> Actions { get; }
    public int SelectedActionIndex { get; set; }
    public Action<int> OnActionSelect { get; }
    public SeaGlassGlyph FabGlyph { get; set; }
    public bool FabDetached { get; set; }
    public Action OnFabClick { get; }
    public SeaGlassMaterial BarMaterial { get; set; }
    public SeaGlassMaterial FabMaterial { get; set; }
    public float CurrentGapDp => _gapDp;

    public SeaGlassFloatingToolbar(
        IReadOnlyList<(SeaGlassGlyph Glyph, string Label)> actions,
        int selectedActionIndex,
        Action<int> onActionSelect,
        SeaGlassGlyph fabGlyph,
        bool fabDetached,
        Action onFabClick,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? barMaterial = null,
        SeaGlassMaterial? fabMaterial = null)
        : base(
            stageState: stageState,
            material: barMaterial ?? SeaGlassTheme.Current.Palette.SurfaceGlass,
            contentRole: SeaGlassContentRole.NavigationChrome)
    {
        Actions = actions;
        SelectedActionIndex = selectedActionIndex;
        OnActionSelect = onActionSelect;
        FabGlyph = fabGlyph;
        FabDetached = fabDetached;
        _gapDp = fabDetached ? 18f : -6f;
        OnFabClick = onFabClick;
        BarMaterial = barMaterial ?? SeaGlassTheme.Current.Palette.SurfaceGlass;
        FabMaterial = fabMaterial ?? SeaGlassTheme.Current.Palette.PrimaryGlass;
        SizePx = new Vector2(460f, 58f);
    }

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        float density = MathF.Max(0.5f, activeTheme.DpiScale);
        SeaGlassSpringSpec gapSpring = new(DampingRatio: 0.62f, Stiffness: 380f);
        gapSpring.Step(ref _gapDp, ref _gapVel, FabDetached ? 18f : -6f, dtSeconds);

        float gapNorm = Math.Clamp((_gapDp + 6f) / 24f, 0f, 1f);
        float energy = Math.Clamp(4f * gapNorm * (1f - gapNorm), 0f, 1f);
        Material = BarMaterial.Deaged(energy);

        float totalW = MathF.Max(200f, SizePx.X);
        float heightPx = MathF.Max(40f, SizePx.Y);
        float gapPx = _gapDp * density;
        float mainBarWidthPx = MathF.Max(heightPx * 2f, totalW - heightPx - MathF.Max(0f, gapPx));
        float fabLeftPx = MathF.Min(totalW - heightPx, mainBarWidthPx + gapPx);

        List<SeaGlassSdfEngine.SdfNode> sdfNodes =
        [
            new SeaGlassSdfEngine.SdfNode(
                centerX: mainBarWidthPx * 0.5f,
                centerY: heightPx * 0.5f,
                halfWidth: mainBarWidthPx * 0.5f,
                halfHeight: heightPx * 0.5f,
                uniformCornerRadius: heightPx * 0.5f,
                cornerSmoothing: 2.5f,
                tintColorArgb: BarMaterial.TintColorArgb,
                thicknessPx: BarMaterial.ThicknessDp * density),
            new SeaGlassSdfEngine.SdfNode(
                centerX: fabLeftPx + heightPx * 0.5f,
                centerY: heightPx * 0.5f,
                halfWidth: heightPx * 0.5f,
                halfHeight: heightPx * 0.5f,
                uniformCornerRadius: heightPx * 0.5f,
                cornerSmoothing: 2.5f,
                tintColorArgb: FabMaterial.TintColorArgb,
                thicknessPx: FabMaterial.ThicknessDp * density)
        ];

        return EvaluateFrame(dtSeconds, activeTheme, sdfNodes, smoothRadiusDp: 32f);
    }
}

/// <summary>
/// Frosted SeaGlass Linear Progress Indicator with an Oklab gradient liquid fill bar.
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassLinearProgressIndicator</c> on Android.
/// </summary>
public sealed class SeaGlassLinearProgressIndicator : SeaGlassSurface
{
    private float _animatedProgress;
    private float _vel;

    public float Progress { get; set; }
    public SeaGlassMaterial TrackMaterial { get; set; }
    public SeaGlassMaterial FillMaterial { get; set; }
    public float AnimatedProgress => Math.Clamp(_animatedProgress, 0.04f, 1f);

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
        Progress = Math.Clamp(progress, 0.04f, 1f);
        _animatedProgress = Progress;
        TrackMaterial = trackMaterial ?? SeaGlassTheme.Current.Palette.SurfaceGlass;
        FillMaterial = fillMaterial ?? SeaGlassTheme.Current.Palette.PrimaryGlass;
        SizePx = new Vector2(360f, 14f);
    }

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        activeTheme.MotionScheme.DefaultSpatial.Step(ref _animatedProgress, ref _vel, Math.Clamp(Progress, 0.04f, 1f), dtSeconds);
        return EvaluateFrame(dtSeconds, activeTheme);
    }
}

/// <summary>
/// Frosted SeaGlass Circular Medallion Progress Indicator with animated arc sweep.
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassCircularProgressIndicator</c> on Android.
/// </summary>
public sealed class SeaGlassCircularProgressIndicator : SeaGlassSurface
{
    private float _animatedProgress;
    private float _vel;

    public float Progress { get; set; }
    public float DiameterDp { get; }
    public float AnimatedProgress => Math.Clamp(_animatedProgress, 0.05f, 1f);
    public float SweepAngleDegrees => 360f * AnimatedProgress;

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
        Progress = Math.Clamp(progress, 0.05f, 1f);
        _animatedProgress = Progress;
        DiameterDp = sizeDp;
        SizePx = new Vector2(sizeDp, sizeDp);
    }

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        activeTheme.MotionScheme.DefaultSpatial.Step(ref _animatedProgress, ref _vel, Math.Clamp(Progress, 0.05f, 1f), dtSeconds);
        return EvaluateFrame(dtSeconds, activeTheme);
    }
}

/// <summary>
/// Expressive Frosted SeaGlass List / Setting Row with leading icon, headline, supporting
/// subtitle, and trailing control slot.
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassListItem</c> on Android.
/// </summary>
public sealed class SeaGlassListItem : SeaGlassSurface
{
    public string HeadlineText { get; set; }
    public string? SupportingText { get; set; }
    public SeaGlassGlyph? LeadingGlyph { get; set; }

    public SeaGlassListItem(
        string headlineText,
        SeaGlassStageState? stageState = null,
        string? supportingText = null,
        SeaGlassGlyph? leadingGlyph = null,
        Action? onClick = null,
        SeaGlassMaterial? material = null)
        : base(
            stageState: stageState,
            material: (material ?? SeaGlassTheme.Current.Palette.SurfaceGlass) with
            {
                CornerRadiusDp = 22f,
                ThicknessDp = 14f
            },
            onClick: onClick)
    {
        HeadlineText = headlineText;
        SupportingText = supportingText;
        LeadingGlyph = leadingGlyph;
        SizePx = new Vector2(420f, 64f);
        BasePaddingPx = new Vector4(16f, 12f, 16f, 12f);
    }
}

/// <summary>
/// Floating Liquid SeaGlass Snackbar / Toast Banner with leading icon, message, and action button.
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassSnackbar</c> on Android.
/// </summary>
public sealed class SeaGlassSnackbar : SeaGlassSurface
{
    public string Message { get; set; }
    public string? ActionLabel { get; set; }
    public Action? OnActionClick { get; set; }
    public SeaGlassGlyph LeadingGlyph { get; set; }

    public SeaGlassSnackbar(
        string message,
        SeaGlassStageState? stageState = null,
        string? actionLabel = null,
        Action? onActionClick = null,
        SeaGlassGlyph leadingGlyph = SeaGlassGlyph.Sparkles,
        SeaGlassMaterial? material = null)
        : base(
            stageState: stageState,
            material: (material ?? SeaGlassTheme.Current.Palette.ElevatedGlass) with
            {
                CornerRadiusDp = 26f,
                ThicknessDp = 18f
            },
            contentRole: SeaGlassContentRole.NavigationChrome)
    {
        Message = message;
        ActionLabel = actionLabel;
        OnActionClick = onActionClick;
        LeadingGlyph = leadingGlyph;
        SizePx = new Vector2(440f, 52f);
        BasePaddingPx = new Vector4(16f, 12f, 16f, 12f);
    }
}

/// <summary>
/// Modal or Inline SeaGlass Dialog Card with header glyph, title, body text, and confirm/dismiss buttons.
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassDialogCard</c> on Android.
/// </summary>
public sealed class SeaGlassDialogCard : SeaGlassCard
{
    public string Body { get; set; }
    public string ConfirmLabel { get; set; }
    public Action OnConfirm { get; set; }
    public string? DismissLabel { get; set; }
    public Action? OnDismiss { get; set; }
    public SeaGlassGlyph Glyph { get; set; }

    public SeaGlassDialogCard(
        string title,
        string body,
        string confirmLabel,
        Action onConfirm,
        SeaGlassStageState? stageState = null,
        string? dismissLabel = "Cancel",
        Action? onDismiss = null,
        SeaGlassGlyph glyph = SeaGlassGlyph.Sparkles,
        SeaGlassMaterial? material = null)
        : base(
            title: title,
            subtitle: null,
            stageState: stageState,
            material: (material ?? SeaGlassTheme.Current.Palette.ElevatedGlass) with
            {
                CornerRadiusDp = 32f,
                ThicknessDp = 22f,
                RimChalkiness = 0.88f
            })
    {
        Body = body;
        ConfirmLabel = confirmLabel;
        OnConfirm = onConfirm;
        DismissLabel = dismissLabel;
        OnDismiss = onDismiss;
        Glyph = glyph;
        SizePx = new Vector2(420f, 220f);
    }
}

/// <summary>
/// Frosted SeaGlass Bottom Sheet surface with a centered drag-handle pill and structured sheet content.
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassBottomSheetSurface</c> on Android.
/// </summary>
public sealed class SeaGlassBottomSheetSurface : SeaGlassSurface
{
    public string Title { get; set; }
    public string? Subtitle { get; set; }

    public SeaGlassBottomSheetSurface(
        string title,
        SeaGlassStageState? stageState = null,
        string? subtitle = null,
        SeaGlassMaterial? material = null)
        : base(
            stageState: stageState,
            material: (material ?? SeaGlassTheme.Current.Palette.ElevatedGlass) with
            {
                CornerRadiusDp = 34f,
                ThicknessDp = 22f,
                RimChalkiness = 0.88f
            })
    {
        Title = title;
        Subtitle = subtitle;
        SizePx = new Vector2(480f, 240f);
        BasePaddingPx = new Vector4(20f, 16f, 20f, 16f);
    }
}

/// <summary>
/// Interactive Device &amp; Page Corner Radius Adaptation Lab (<c>SeaGlassAdaptiveCornerLabCard</c>).
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassAdaptiveCornerLabCard</c> on Android.
/// </summary>
public sealed class SeaGlassAdaptiveCornerLabCard : SeaGlassCard
{
    private float _animatedInsetDp = 3f;
    private float _insetVel;

    public SeaGlassDeviceHardwareProfile ActiveProfile { get; private set; }
    public int InsetPresetIndex { get; private set; }
    public SeaGlassCornerMask CornerMaskPreset { get; private set; } = SeaGlassCornerMask.All;
    public float ProximityThresholdDp { get; private set; } = 96f;
    public float AnimatedInsetDp => _animatedInsetDp;

    public float TargetInsetDp => InsetPresetIndex switch
    {
        0 => 3f,
        1 => 12f,
        _ => 24f
    };

    public SeaGlassAdaptiveCornerLabCard(
        SeaGlassDeviceHardwareProfile activeProfile = SeaGlassDeviceHardwareProfile.Pixel9Pro,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? surfaceMaterial = null)
        : base(
            title: "Concentric Bezel & Page Corner Matching",
            subtitle: "ADAPTIVE DEVICE & PAGE CORNER OPTICS",
            stageState: stageState,
            material: surfaceMaterial ?? SeaGlassTheme.Current.Palette.SurfaceGlass)
    {
        ActiveProfile = activeProfile;
        SizePx = new Vector2(540f, 340f);
    }

    public void SelectProfile(SeaGlassDeviceHardwareProfile profile) => ActiveProfile = profile;
    public void SelectInsetPreset(int presetIndex) => InsetPresetIndex = Math.Clamp(presetIndex, 0, 2);
    public void SelectCornerMask(SeaGlassCornerMask mask) => CornerMaskPreset = mask;
    public void SetProximityThresholdDp(float thresholdDp) => ProximityThresholdDp = Math.Clamp(thresholdDp, 0f, 240f);

    public (SeaGlassCornerPodLayoutPx Layout, SeaGlassCornerRadiiPx TopLeftPodRadii, SeaGlassCornerRadiiPx TopRightPodRadii, SeaGlassCornerRadiiPx BottomDockRadii) SolveBezelFrame(
        float frameWidthPx = 500f,
        float frameHeightPx = 206f,
        float density = 1.0f,
        float dtSeconds = 1f / 60f)
    {
        SeaGlassSpringSpec insetSpring = new(DampingRatio: 0.64f, Stiffness: 380f);
        insetSpring.Step(ref _animatedInsetDp, ref _insetVel, TargetInsetDp, dtSeconds);

        SeaGlassDeviceCornerSpec spec = SeaGlassCornerAdaptationSolver.DetectDeviceCorners(density, ActiveProfile);
        SeaGlassCornerRadiiPx bezelRadii = spec.ToRadiiPx();
        float reqInsetPx = _animatedInsetDp * density;

        SeaGlassCornerPodLayoutPx layout = SeaGlassCornerAdaptationSolver.SolveCornerPodLayoutPx(
            containerWidthPx: frameWidthPx,
            containerHeightPx: frameHeightPx,
            containerCornersPx: bezelRadii,
            requestedInsetPx: reqInsetPx,
            minHorizontalGapPx: 14f * density,
            minVerticalGapPx: 14f * density,
            preferredPodWidthPx: 148f * density,
            preferredTopPodHeightPx: 78f * density,
            preferredBottomDockHeightPx: 68f * density);

        float insetPx = layout.EffectiveInsetPx;
        float podW = layout.PodWidthPx;
        float podH = layout.TopPodHeightPx;
        float dockH = layout.BottomDockHeightPx;
        float basePodCornerPx = 18f * density;
        float influencePx = ProximityThresholdDp * density;

        SeaGlassCornerRadiiPx tlPod = SeaGlassCornerAdaptationSolver.SolveElementCornerRadiiPx(
            insetPx, insetPx, insetPx + podW, insetPx + podH,
            frameWidthPx, frameHeightPx, bezelRadii, basePodCornerPx,
            minCornerFloorPx: 8f * density, influenceDistancePx: influencePx, adaptationStrength: 1f,
            mode: SeaGlassCornerAdaptationMode.FlushCornerEmbed, cornerMask: CornerMaskPreset);

        SeaGlassCornerRadiiPx trPod = SeaGlassCornerAdaptationSolver.SolveElementCornerRadiiPx(
            MathF.Max(0f, frameWidthPx - insetPx - podW), insetPx, MathF.Max(podW, frameWidthPx - insetPx), insetPx + podH,
            frameWidthPx, frameHeightPx, bezelRadii, basePodCornerPx,
            minCornerFloorPx: 8f * density, influenceDistancePx: influencePx, adaptationStrength: 1f,
            mode: SeaGlassCornerAdaptationMode.FlushCornerEmbed, cornerMask: CornerMaskPreset);

        SeaGlassCornerRadiiPx dock = SeaGlassCornerAdaptationSolver.SolveElementCornerRadiiPx(
            insetPx, MathF.Max(0f, frameHeightPx - insetPx - dockH), MathF.Max(podW, frameWidthPx - insetPx), MathF.Max(dockH, frameHeightPx - insetPx),
            frameWidthPx, frameHeightPx, bezelRadii, basePodCornerPx,
            minCornerFloorPx: 8f * density, influenceDistancePx: influencePx, adaptationStrength: 1f,
            mode: SeaGlassCornerAdaptationMode.FlushCornerEmbed, cornerMask: CornerMaskPreset);

        return (layout, tlPod, trPod, dock);
    }
}

