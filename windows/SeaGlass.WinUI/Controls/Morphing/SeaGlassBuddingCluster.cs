using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Apple Liquid Glass Budding Action Cluster (SeaGlassBuddingCluster).
/// Satellite droplets bud out of the anchor's center, stretching live sminCubic liquid bridges.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_AnchorOrb", Type = typeof(Button))]
[TemplatePart(Name = "PART_SatellitesHost", Type = typeof(ItemsControl))]
public class SeaGlassBuddingCluster : SeaGlassSurface
{
    private float _expandProgress;
    private float _expandVel;

    public static readonly DependencyProperty ExpandedProperty =
        DependencyProperty.Register(
            nameof(Expanded),
            typeof(bool),
            typeof(SeaGlassBuddingCluster),
            new PropertyMetadata(false, OnExpandedChanged));

    public static readonly DependencyProperty BudProgressProperty =
        DependencyProperty.Register(
            nameof(BudProgress),
            typeof(double),
            typeof(SeaGlassBuddingCluster),
            new PropertyMetadata(0.0));

    public bool Expanded
    {
        get => (bool)GetValue(ExpandedProperty);
        set => SetValue(ExpandedProperty, value);
    }

    public double BudProgress
    {
        get => (double)GetValue(BudProgressProperty);
        set => SetValue(BudProgressProperty, value);
    }

    public IReadOnlyList<(string Symbol, string Label)> Items { get; set; } = Array.Empty<(string, string)>();
    public Action<int>? OnItemClick { get; set; }
    public SeaGlassMaterial AnchorMaterial { get; set; }
    public SeaGlassMaterial SatelliteMaterial { get; set; }
    public float ExpandProgress => _expandProgress;
    public IReadOnlyList<SeaGlassSdfEngine.SdfNode> LastEvaluatedNodes { get; private set; } = Array.Empty<SeaGlassSdfEngine.SdfNode>();

    public event EventHandler<int>? ItemClicked;

    public SeaGlassBuddingCluster()
    {
        DefaultStyleKey = typeof(SeaGlassBuddingCluster);
        IsTabStop = true;
        AnchorMaterial = SeaGlassTheme.Current.Palette.PrimaryGlass;
        SatelliteMaterial = SeaGlassTheme.Current.Palette.TertiaryGlass;
        SizePx = new Vector2(460f, 82f);
        OnClick = () => ToggleExpanded();
    }

    public SeaGlassBuddingCluster(
        bool expanded,
        IReadOnlyList<(string Symbol, string Label)> items,
        Action<int>? onItemClick = null,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? anchorMaterial = null,
        SeaGlassMaterial? satelliteMaterial = null)
        : base(stageState: stageState)
    {
        DefaultStyleKey = typeof(SeaGlassBuddingCluster);
        Expanded = expanded;
        _expandProgress = expanded ? 1f : 0f;
        Items = items;
        OnItemClick = onItemClick;
        AnchorMaterial = anchorMaterial ?? SeaGlassTheme.Current.Palette.PrimaryGlass;
        SatelliteMaterial = satelliteMaterial ?? SeaGlassTheme.Current.Palette.TertiaryGlass;
        SizePx = new Vector2(460f, 82f);
        OnClick = () => ToggleExpanded();
    }

    private static void OnExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassBuddingCluster cluster && e.NewValue is bool exp)
        {
            cluster.SetExpanded(exp);
        }
    }

    public void ToggleExpanded() => Expanded = !Expanded;
    public void SetExpanded(bool expanded) => Expanded = expanded;

    public void ClickItem(int index)
    {
        OnItemClick?.Invoke(index);
        ItemClicked?.Invoke(this, index);
    }

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        float density = MathF.Max(0.5f, activeTheme.DpiScale);
        SeaGlassSpringSpec budSpring = new(DampingRatio: 0.65f, Stiffness: 410f);
        budSpring.Step(ref _expandProgress, ref _expandVel, Expanded ? 1f : 0f, dtSeconds);

        BudProgress = _expandProgress;

        float containerW = MathF.Max(240f, SizePx.X);
        float clusterH = MathF.Max(56f, SizePx.Y);
        float centerY = clusterH * 0.5f;
        float anchorR = MathF.Min(25f * density, clusterH * 0.36f);
        float satR = MathF.Min(22f * density, clusterH * 0.32f);
        int count = Math.Clamp(Items?.Count ?? 0, 1, 3);

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
        Material = AnchorMaterial.Deaged((float)BudProgress * 0.85f);
        return EvaluateFrame(dtSeconds, activeTheme, activeNodes, smoothRadiusDp: 34f);
    }

    private Grid? _root;
    private Border? _glassHost;
    private Button? _anchorOrb;
    private ItemsControl? _satellitesHost;

    public Button? AnchorOrb => _anchorOrb;
    public ItemsControl? SatellitesHost => _satellitesHost;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        if (_anchorOrb != null) _anchorOrb.Click -= OnAnchorOrbClick;

        _root = GetTemplateChild("PART_Root") as Grid;
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _anchorOrb = GetTemplateChild("PART_AnchorOrb") as Button;
        _satellitesHost = GetTemplateChild("PART_SatellitesHost") as ItemsControl;

        if (_anchorOrb != null) _anchorOrb.Click += OnAnchorOrbClick;
    }

    private void OnAnchorOrbClick(object sender, RoutedEventArgs e) => ToggleExpanded();

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new SeaGlass.WinUI.Automation.SeaGlassBuddingClusterAutomationPeer(this);
}

