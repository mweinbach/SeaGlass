using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Liquid-Glass-inspired adaptive navigation bar in frosted SeaGlass with lead-lag lozenge motion.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_BarTrack", Type = typeof(Border))]
[TemplatePart(Name = "PART_IndicatorLozenge", Type = typeof(Border))]
[TemplatePart(Name = "PART_ItemsPresenter", Type = typeof(ItemsControl))]
[TemplatePart(Name = "PART_ActionOrb", Type = typeof(Button))]
public class SeaGlassFluidBar : SeaGlassSurface
{
    private readonly SeaGlassLeadLagLozengeTravel _lozengeTravel = new();
    private float _actionSeparationDp;
    private float _sepVel;

    public static readonly DependencyProperty SelectedIndexProperty =
        DependencyProperty.Register(
            nameof(SelectedIndex),
            typeof(int),
            typeof(SeaGlassFluidBar),
            new PropertyMetadata(0, OnSelectedIndexChanged));

    public static readonly DependencyProperty ItemsProperty =
        DependencyProperty.Register(
            nameof(Items),
            typeof(IReadOnlyList<string>),
            typeof(SeaGlassFluidBar),
            new PropertyMetadata(Array.Empty<string>()));

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public IReadOnlyList<string> Items
    {
        get => (IReadOnlyList<string>)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public Action<int>? OnSelect { get; set; }
    public SeaGlassMaterial BarMaterial { get; set; }
    public SeaGlassMaterial IndicatorMaterial { get; set; }
    public string ActionLabel { get; set; } = "✦";
    public bool ActionDetached { get; set; }
    public bool ShowActionOrb { get; set; }
    public Action? OnActionClick { get; set; }

    public float LeadIndex => _lozengeTravel.LeadIndex;
    public float TrailIndex => _lozengeTravel.TrailIndex;
    public float AnimatedIndex => _lozengeTravel.CenterIndex;
    public float LeftEdgeSlot => MathF.Min(_lozengeTravel.LeadIndex, _lozengeTravel.TrailIndex);
    public float RightEdgeSlot => MathF.Max(_lozengeTravel.LeadIndex, _lozengeTravel.TrailIndex);
    public float LozengeSlotSpan => MathF.Max(1.0f, 1.0f + (RightEdgeSlot - LeftEdgeSlot) * 0.92f);
    public float MotionEnergy => Math.Clamp(MathF.Abs(_lozengeTravel.LeadIndex - _lozengeTravel.TrailIndex) * 1.45f, 0f, 1f);
    public float ActionSeparationDp => _actionSeparationDp;
    public float MotionStretch => Math.Clamp(MathF.Abs(_lozengeTravel.LeadIndex - _lozengeTravel.TrailIndex), 0f, 1.2f);
    public SeaGlassMaterial ActiveIndicatorMaterial => IndicatorMaterial.Deaged(MotionEnergy * 0.92f);

    public event EventHandler<int>? SelectionChanged;

    public SeaGlassFluidBar()
    {
        DefaultStyleKey = typeof(SeaGlassFluidBar);
        IsTabStop = true;
        BarMaterial = SeaGlassTheme.Current.Palette.SurfaceGlass;
        IndicatorMaterial = SeaGlassTheme.Current.Palette.PrimaryGlass;
        SizePx = new Vector2(520f, 68f);
    }

    public SeaGlassFluidBar(
        IReadOnlyList<string> items,
        int selectedIndex,
        Action<int>? onSelect = null,
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
        DefaultStyleKey = typeof(SeaGlassFluidBar);
        Items = items;
        SelectedIndex = Math.Clamp(selectedIndex, 0, Math.Max(0, items.Count - 1));
        _lozengeTravel.SnapTo(SelectedIndex);
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

    private static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassFluidBar bar && e.NewValue is int idx)
        {
            bar._lozengeTravel.SetTarget(idx);
            bar.OnSelect?.Invoke(idx);
            bar.SelectionChanged?.Invoke(bar, idx);
        }
    }

    public void SelectTab(int index)
    {
        SelectedIndex = Math.Clamp(index, 0, Math.Max(0, (Items?.Count ?? 1) - 1));
    }

    private Grid? _root;
    private Border? _glassHost;
    private Border? _barTrack;
    private Border? _indicatorLozenge;
    private ItemsControl? _itemsPresenter;
    private Button? _actionOrb;

    public Border? BarTrack => _barTrack;
    public Border? IndicatorLozenge => _indicatorLozenge;
    public ItemsControl? ItemsPresenter => _itemsPresenter;
    public Button? ActionOrb => _actionOrb;
    public Border? GlassHost => _glassHost;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        if (_actionOrb != null)
        {
            _actionOrb.Click -= OnActionOrbClick;
        }

        _root = GetTemplateChild("PART_Root") as Grid;
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _barTrack = GetTemplateChild("PART_BarTrack") as Border;
        _indicatorLozenge = GetTemplateChild("PART_IndicatorLozenge") as Border;
        _itemsPresenter = GetTemplateChild("PART_ItemsPresenter") as ItemsControl;
        _actionOrb = GetTemplateChild("PART_ActionOrb") as Button;

        if (_actionOrb != null)
        {
            _actionOrb.Click += OnActionOrbClick;
        }
    }

    private void OnActionOrbClick(object sender, RoutedEventArgs e)
    {
        OnActionClick?.Invoke();
    }

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        float density = MathF.Max(0.5f, activeTheme.DpiScale);

        _lozengeTravel.Step(dtSeconds);

        SeaGlassSpringSpec orbSpring = new(DampingRatio: 0.64f, Stiffness: 400f);
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

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new SeaGlass.WinUI.Automation.SeaGlassFluidBarAutomationPeer(this);
}
