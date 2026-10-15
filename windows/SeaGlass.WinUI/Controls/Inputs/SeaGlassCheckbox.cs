using System;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Material 3 Expressive SeaGlass Checkbox that morphs between squircle and Clover.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_BoxContour", Type = typeof(Border))]
[TemplatePart(Name = "PART_Checkmark", Type = typeof(Microsoft.UI.Xaml.Shapes.Path))]
[TemplatePart(Name = "PART_ContentPresenter", Type = typeof(ContentPresenter))]
[TemplatePart(Name = "PART_FocusVisual", Type = typeof(Border))]
public class SeaGlassCheckbox : SeaGlassSurface
{
    private float _morphProgress;
    private float _morphVel;
    private float _tintProgress;
    private float _tintVel;

    public static readonly DependencyProperty CheckedProperty =
        DependencyProperty.Register(
            nameof(Checked),
            typeof(bool),
            typeof(SeaGlassCheckbox),
            new PropertyMetadata(false, OnCheckedChanged));

    public static readonly DependencyProperty IsCheckedProperty =
        DependencyProperty.Register(
            nameof(IsChecked),
            typeof(bool?),
            typeof(SeaGlassCheckbox),
            new PropertyMetadata(false, OnIsCheckedChanged));

    public static readonly DependencyProperty IsThreeStateProperty =
        DependencyProperty.Register(
            nameof(IsThreeState),
            typeof(bool),
            typeof(SeaGlassCheckbox),
            new PropertyMetadata(false));

    public bool Checked
    {
        get => (bool)GetValue(CheckedProperty);
        set => SetValue(CheckedProperty, value);
    }

    public bool? IsChecked
    {
        get => (bool?)GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    public bool IsThreeState
    {
        get => (bool)GetValue(IsThreeStateProperty);
        set => SetValue(IsThreeStateProperty, value);
    }

    public Action<bool>? OnCheckedChange { get; set; }
    public SeaGlassMaterial CheckedMaterial { get; set; }
    public SeaGlassMaterial UncheckedMaterial { get; set; }
    public bool UseDesktopSquareContour { get; set; }
    public float MorphProgress => _morphProgress;

    public event EventHandler<bool?>? CheckStateChanged;

    public SeaGlassCheckbox()
    {
        DefaultStyleKey = typeof(SeaGlassCheckbox);
        CheckedMaterial = SeaGlassTheme.Current.Palette.PrimaryGlass;
        UncheckedMaterial = SeaGlassTheme.Current.Palette.SurfaceGlass;
        SizePx = new Vector2(28f, 28f);
        OnClick = () => Toggle();
        IsTabStop = true;
    }

    public SeaGlassCheckbox(
        bool isChecked,
        Action<bool>? onCheckedChange = null,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? checkedMaterial = null,
        SeaGlassMaterial? uncheckedMaterial = null)
        : base(stageState: stageState)
    {
        DefaultStyleKey = typeof(SeaGlassCheckbox);
        Checked = isChecked;
        IsChecked = isChecked;
        _morphProgress = isChecked ? 1f : 0f;
        _tintProgress = isChecked ? 1f : 0f;
        OnCheckedChange = onCheckedChange;
        CheckedMaterial = checkedMaterial ?? SeaGlassTheme.Current.Palette.PrimaryGlass;
        UncheckedMaterial = uncheckedMaterial ?? SeaGlassTheme.Current.Palette.SurfaceGlass;
        SizePx = new Vector2(28f, 28f);
        OnClick = () => Toggle();
    }

    private Grid? _root;
    private Border? _glassHost;
    private Border? _boxContour;
    private Microsoft.UI.Xaml.Shapes.Path? _checkmark;
    private ContentPresenter? _contentPresenter;
    private Border? _focusVisual;

    public Border? BoxContour => _boxContour;
    public Microsoft.UI.Xaml.Shapes.Path? Checkmark => _checkmark;
    public Border? GlassHost => _glassHost;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _root = GetTemplateChild("PART_Root") as Grid;
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _boxContour = GetTemplateChild("PART_BoxContour") as Border;
        _checkmark = GetTemplateChild("PART_Checkmark") as Microsoft.UI.Xaml.Shapes.Path;
        _contentPresenter = GetTemplateChild("PART_ContentPresenter") as ContentPresenter;
        _focusVisual = GetTemplateChild("PART_FocusVisual") as Border;

        UpdateVisualState(false);
    }

    private void UpdateVisualState(bool useTransitions)
    {
        VisualStateManager.GoToState(this, Checked ? "Checked" : "Unchecked", useTransitions);
        if (_checkmark != null)
        {
            _checkmark.Visibility = Checked ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private static void OnCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassCheckbox cb && e.NewValue is bool b)
        {
            cb.IsChecked = b;
            cb.UpdateVisualState(true);
            cb.OnCheckedChange?.Invoke(b);
            cb.CheckStateChanged?.Invoke(cb, b);
        }
    }

    private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassCheckbox cb && e.NewValue is bool b)
        {
            cb.Checked = b;
        }
    }

    public void Toggle()
    {
        Checked = !Checked;
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
            CornerRadiusDp = UseDesktopSquareContour ? 6f : CheckedMaterial.CornerRadiusDp,
            ThicknessDp = 12f + 4f * tp
        }).Deaged(energy);

        if (UseDesktopSquareContour)
        {
            return EvaluateFrame(dtSeconds: dtSeconds, theme: activeTheme);
        }

        float mp = _morphProgress;
        return EvaluateFrame(
            dtSeconds: dtSeconds,
            theme: activeTheme,
            customContourBuilder: (wPx, hPx, outPath) =>
                SeaGlassShapeMorph.BuildMorphedPolygonPath(
                    startKind: SeaGlassShapeKind.Squircle,
                    endKind: SeaGlassShapeKind.Clover4,
                    progress: mp,
                    widthPx: wPx,
                    heightPx: hPx,
                    outPath: outPath));
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new Automation.SeaGlassCheckboxAutomationPeer(this);
}
