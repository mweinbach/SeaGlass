using System;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Material 3 Expressive Shape-Morphing SeaGlass Button with circle-midpoint transitions.
/// Note: Morphing geometry is evaluated and rendered dynamically via the Direct2D/Composition contour builder in EvaluateFrame;
/// static PART_MorphedPath contract has been reconciled and removed.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_ContentPresenter", Type = typeof(ContentPresenter))]
public class SeaGlassMorphingButton : SeaGlassSurface
{
    private float _morphProgress;
    private float _morphVel;

    public static readonly DependencyProperty ActiveProperty =
        DependencyProperty.Register(
            nameof(Active),
            typeof(bool),
            typeof(SeaGlassMorphingButton),
            new PropertyMetadata(false, OnActiveChanged));

    public static readonly DependencyProperty IsExpandedProperty =
        DependencyProperty.Register(
            nameof(IsExpanded),
            typeof(bool),
            typeof(SeaGlassMorphingButton),
            new PropertyMetadata(false, OnIsExpandedChanged));

    public static readonly DependencyProperty MorphProgressProperty =
        DependencyProperty.Register(
            nameof(MorphProgress),
            typeof(double),
            typeof(SeaGlassMorphingButton),
            new PropertyMetadata(0.0));

    public static readonly DependencyProperty ButtonLabelProperty =
        DependencyProperty.Register(
            nameof(ButtonLabel),
            typeof(string),
            typeof(SeaGlassMorphingButton),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ExpandedLabelProperty =
        DependencyProperty.Register(
            nameof(ExpandedLabel),
            typeof(string),
            typeof(SeaGlassMorphingButton),
            new PropertyMetadata(string.Empty));

    public bool Active
    {
        get => (bool)GetValue(ActiveProperty);
        set => SetValue(ActiveProperty, value);
    }

    public bool IsExpanded
    {
        get => (bool)GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    public double MorphProgress
    {
        get => (double)GetValue(MorphProgressProperty);
        set => SetValue(MorphProgressProperty, value);
    }

    public string ButtonLabel
    {
        get => (string)GetValue(ButtonLabelProperty);
        set => SetValue(ButtonLabelProperty, value);
    }

    public string ExpandedLabel
    {
        get => (string)GetValue(ExpandedLabelProperty);
        set => SetValue(ExpandedLabelProperty, value);
    }

    public SeaGlassShapeKind RestingShape { get; set; } = SeaGlassShapeKind.Cookie9;
    public SeaGlassShapeKind ActiveShape { get; set; } = SeaGlassShapeKind.Squircle;
    public float RestingScale { get; set; } = 1.0f;
    public float ActiveScale { get; set; } = 1.06f;
    public float SpinDegreesOnMorph { get; set; } = 45f;
    public bool ViaCircleBridge { get; set; } = true;
    public SeaGlassMaterial RestingMaterial { get; set; }
    public SeaGlassMaterial ActiveMaterial { get; set; }
    public ChoreographedShapeTransform LastChoreoTransform { get; private set; }

    public event EventHandler<bool>? ActiveChanged;

    public SeaGlassMorphingButton()
    {
        DefaultStyleKey = typeof(SeaGlassMorphingButton);
        IsTabStop = true;
        RestingMaterial = SeaGlassTheme.Current.Palette.PrimaryGlass;
        ActiveMaterial = SeaGlassTheme.Current.Palette.TertiaryGlass;
        SizePx = new Vector2(96f, 96f);
    }

    public SeaGlassMorphingButton(
        bool active,
        Action? onClick = null,
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
        DefaultStyleKey = typeof(SeaGlassMorphingButton);
        Active = active;
        IsExpanded = active;
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

    private static void OnActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassMorphingButton btn && e.NewValue is bool act)
        {
            btn.IsExpanded = act;
            btn.ActiveChanged?.Invoke(btn, act);
        }
    }

    private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassMorphingButton btn && e.NewValue is bool act)
        {
            btn.Active = act;
        }
    }

    public void SetActive(bool active) => Active = active;
    public void ToggleActive() => Active = !Active;

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        float target = IsPointerDown ? (Active ? 0.58f : 0.42f) : (Active ? 1f : 0f);
        SeaGlassSpringSpec morphSpring = new(DampingRatio: 0.64f, Stiffness: 420f);
        morphSpring.Step(ref _morphProgress, ref _morphVel, target, dtSeconds);

        MorphProgress = _morphProgress;

        SeaGlassShapeStage startStage = new(RestingShape, RestingScale, 1f, 0f, ViaCircleBridge);
        SeaGlassShapeStage endStage = new(ActiveShape, ActiveScale, 1f, SpinDegreesOnMorph, ViaCircleBridge);
        ChoreographedShapeTransform choreo = SeaGlassShapeMorph.EvaluateChoreographedTransform(startStage, endStage, _morphProgress);
        LastChoreoTransform = choreo;

        var blended = SeaGlassShapeMorph.ComputeDynamicFlexMaterial(
            startMaterial: RestingMaterial,
            endMaterial: ActiveMaterial,
            progress: Math.Clamp(_morphProgress, 0f, 1f),
            velocityPxPerSec: choreo.MorphEnergy * 1400f);
        Material = blended.Deaged(choreo.MorphEnergy * 0.90f);

        float p = _morphProgress;
        return EvaluateFrame(
            dtSeconds: dtSeconds,
            theme: activeTheme,
            customContourBuilder: (wPx, hPx, outPath) =>
                SeaGlassShapeMorph.BuildChoreographedPolygonPath(startStage, endStage, p, wPx, hPx, outPath));
    }

    private Grid? _root;
    private Border? _glassHost;
    private ContentPresenter? _contentPresenter;

    public ContentPresenter? ContentPresenterChild => _contentPresenter;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _root = GetTemplateChild("PART_Root") as Grid;
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _contentPresenter = GetTemplateChild("PART_ContentPresenter") as ContentPresenter;
    }

    public override void PerformClick()
    {
        ToggleActive();
        base.PerformClick();
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new SeaGlass.WinUI.Automation.SeaGlassMorphingButtonAutomationPeer(this);
}

