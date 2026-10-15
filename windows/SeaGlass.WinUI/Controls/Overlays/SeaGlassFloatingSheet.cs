using System;
using System.Collections.Generic;
using System.Numerics;
using ICommand = global::System.Windows.Input.ICommand;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// iOS-Style Floating Concentric SeaGlass Detent Sheet &amp; Page Overlay.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_DragHandle", Type = typeof(Border))]
[TemplatePart(Name = "PART_QuickActionStrip", Type = typeof(ItemsControl))]
[TemplatePart(Name = "PART_PageHost", Type = typeof(ContentPresenter))]
public class SeaGlassFloatingSheet : SeaGlassSurface
{
    private float _visibilityProgress;
    private float _visibilityVel;
    private float _pageMorphProgress;
    private float _pageMorphVel;
    private readonly SeaGlassMaterial _baseMaterial;

    public static readonly DependencyProperty VisibleProperty =
        DependencyProperty.Register(nameof(Visible), typeof(bool), typeof(SeaGlassFloatingSheet),
            new PropertyMetadata(false, (d, e) => ((SeaGlassFloatingSheet)d)._visibilityProgress = (bool)e.NewValue ? 1f : 0f));

    public static readonly DependencyProperty OuterInsetDpProperty =
        DependencyProperty.Register(nameof(OuterInsetDp), typeof(float), typeof(SeaGlassFloatingSheet),
            new PropertyMetadata(8.0f));

    public static readonly DependencyProperty EnableMorphAnimationProperty =
        DependencyProperty.Register(nameof(EnableMorphAnimation), typeof(bool), typeof(SeaGlassFloatingSheet),
            new PropertyMetadata(true));

    public static readonly DependencyProperty ActiveSubPageProperty =
        DependencyProperty.Register(nameof(ActiveSubPage), typeof(int), typeof(SeaGlassFloatingSheet),
            new PropertyMetadata(0, (d, e) => ((SeaGlassFloatingSheet)d).SubPageChanged?.Invoke((SeaGlassFloatingSheet)d, (int)e.NewValue)));

    public static readonly DependencyProperty DragOffsetPxProperty =
        DependencyProperty.Register(nameof(DragOffsetPx), typeof(float), typeof(SeaGlassFloatingSheet),
            new PropertyMetadata(0.0f));

    public static readonly DependencyProperty QuickActionsProperty =
        DependencyProperty.Register(nameof(QuickActions), typeof(IReadOnlyList<SeaGlassSheetQuickAction>), typeof(SeaGlassFloatingSheet),
            new PropertyMetadata(null));

    public static readonly DependencyProperty DismissCommandProperty =
        DependencyProperty.Register(nameof(DismissCommand), typeof(ICommand), typeof(SeaGlassFloatingSheet),
            new PropertyMetadata(null));

    public bool Visible
    {
        get => (bool)GetValue(VisibleProperty);
        set => SetValue(VisibleProperty, value);
    }

    public float OuterInsetDp
    {
        get => (float)GetValue(OuterInsetDpProperty);
        set => SetValue(OuterInsetDpProperty, value);
    }

    public bool EnableMorphAnimation
    {
        get => (bool)GetValue(EnableMorphAnimationProperty);
        set => SetValue(EnableMorphAnimationProperty, value);
    }

    public int ActiveSubPage
    {
        get => (int)GetValue(ActiveSubPageProperty);
        set => SetValue(ActiveSubPageProperty, value);
    }

    public float DragOffsetPx
    {
        get => (float)GetValue(DragOffsetPxProperty);
        set => SetValue(DragOffsetPxProperty, value);
    }

    public IReadOnlyList<SeaGlassSheetQuickAction> QuickActions
    {
        get => (IReadOnlyList<SeaGlassSheetQuickAction>)GetValue(QuickActionsProperty);
        set => SetValue(QuickActionsProperty, value);
    }

    public ICommand? DismissCommand
    {
        get => (ICommand?)GetValue(DismissCommandProperty);
        set => SetValue(DismissCommandProperty, value);
    }

    public Action? OnDismissRequest { get; set; }

    public float VisibilityProgress => Math.Clamp(_visibilityProgress, 0f, 1.15f);
    public float PageMorphProgress => Math.Clamp(_pageMorphProgress, 0f, 1.15f);

    public event TypedEventHandler<SeaGlassFloatingSheet, EventArgs>? DismissRequested;
    public event TypedEventHandler<SeaGlassFloatingSheet, int>? SubPageChanged;

    public SeaGlassFloatingSheet()
    {
        DefaultStyleKey = typeof(SeaGlassFloatingSheet);
        _baseMaterial = Material;
        CornerAdaptationMode = SeaGlassCornerAdaptationMode.ConcentricProximity;
        SizePx = new Vector2(460f, 520f);
        QuickActions =
        [
            new("pin", "Pin", SeaGlassGlyph.Bookmark, Active: true),
            new("unread", "Unread", SeaGlassGlyph.Bell),
            new("fork", "Fork", SeaGlassGlyph.Layers),
            new("export", "Export", SeaGlassGlyph.Share)
        ];
    }

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
        DefaultStyleKey = typeof(SeaGlassFloatingSheet);
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

    public void SwitchSubPage(int pageIndex)
    {
        ActiveSubPage = Math.Clamp(pageIndex, 0, 1);
    }

    public void SetDragOffset(float dragOffsetPx)
    {
        DragOffsetPx = MathF.Max(-48f, dragOffsetPx);
        if (DragOffsetPx > 140f)
        {
            Visible = false;
            DragOffsetPx = 0f;
            OnDismissRequest?.Invoke();
            DismissRequested?.Invoke(this, EventArgs.Empty);
            DismissCommand?.Execute(null);
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

    private Grid? _root;
    private Border? _glassHost;
    private Border? _dragHandle;
    private ItemsControl? _quickActionStrip;
    private ContentPresenter? _pageHost;

    public Border? DragHandle => _dragHandle;
    public ItemsControl? QuickActionStrip => _quickActionStrip;
    public ContentPresenter? PageHost => _pageHost;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _root = GetTemplateChild("PART_Root") as Grid;
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _dragHandle = GetTemplateChild("PART_DragHandle") as Border;
        _quickActionStrip = GetTemplateChild("PART_QuickActionStrip") as ItemsControl;
        _pageHost = GetTemplateChild("PART_PageHost") as ContentPresenter;
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new Automation.SeaGlassFloatingSheetAutomationPeer(this);
}

