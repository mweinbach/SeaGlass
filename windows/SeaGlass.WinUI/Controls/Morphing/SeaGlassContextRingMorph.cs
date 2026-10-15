using System;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Circular Progress Ring & Context Window Popover Morph.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_RingPresenter", Type = typeof(Canvas))]
[TemplatePart(Name = "PART_DetailsPresenter", Type = typeof(ContentPresenter))]
public class SeaGlassContextRingMorph : SeaGlassTwoElementMorph
{
    public static readonly DependencyProperty ProgressProperty =
        DependencyProperty.Register(
            nameof(Progress),
            typeof(double),
            typeof(SeaGlassContextRingMorph),
            new PropertyMetadata(0.35));

    public static readonly DependencyProperty RingRadiusDpProperty =
        DependencyProperty.Register(
            nameof(RingRadiusDp),
            typeof(double),
            typeof(SeaGlassContextRingMorph),
            new PropertyMetadata(19.0));

    public static readonly DependencyProperty SegmentCountProperty =
        DependencyProperty.Register(
            nameof(SegmentCount),
            typeof(int),
            typeof(SeaGlassContextRingMorph),
            new PropertyMetadata(1));

    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public double RingRadiusDp
    {
        get => (double)GetValue(RingRadiusDpProperty);
        set => SetValue(RingRadiusDpProperty, value);
    }

    public int SegmentCount
    {
        get => (int)GetValue(SegmentCountProperty);
        set => SetValue(SegmentCountProperty, value);
    }

    public string UsedLabel { get; set; } = "366K of 1.05M tokens";
    public string RemainingLabel { get; set; } = "683K left";

    public SeaGlassContextRingMorph()
        : base(
            expanded: false,
            collapsedSizePx: new Vector2(38f, 38f),
            expandedSizePx: new Vector2(236f, 128f),
            morphMode: SeaGlassMorphMode.LiquidCoalesce,
            anchorAlignment: SeaGlassAnchorAlignment.TopCenter)
    {
        DefaultStyleKey = typeof(SeaGlassContextRingMorph);
    }

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
        DefaultStyleKey = typeof(SeaGlassContextRingMorph);
        Progress = Math.Clamp(progress, 0.05f, 1f);
        UsedLabel = usedLabel;
        RemainingLabel = remainingLabel;
    }

    private Grid? _root;
    private Border? _glassHost;
    private Canvas? _ringPresenter;
    private ContentPresenter? _detailsPresenter;

    public Canvas? RingPresenter => _ringPresenter;
    public ContentPresenter? DetailsPresenter => _detailsPresenter;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _root = GetTemplateChild("PART_Root") as Grid;
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _ringPresenter = GetTemplateChild("PART_RingPresenter") as Canvas;
        _detailsPresenter = GetTemplateChild("PART_DetailsPresenter") as ContentPresenter;
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new SeaGlass.WinUI.Automation.SeaGlassContextRingMorphAutomationPeer(this);
}

