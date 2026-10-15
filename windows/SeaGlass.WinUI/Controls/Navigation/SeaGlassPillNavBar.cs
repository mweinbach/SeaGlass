using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Tumbled SeaGlass Inline Pill Navigation Bar with dual-edge lead-lag lozenge travel.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_LozengeIndicator", Type = typeof(Border))]
[TemplatePart(Name = "PART_ItemsPresenter", Type = typeof(ItemsControl))]
public class SeaGlassPillNavBar : SeaGlassSurface
{
    private readonly SeaGlassLeadLagLozengeTravel _lozengeTravel = new();

    public static readonly DependencyProperty SelectedIndexProperty =
        DependencyProperty.Register(
            nameof(SelectedIndex),
            typeof(int),
            typeof(SeaGlassPillNavBar),
            new PropertyMetadata(0, OnSelectedIndexChanged));

    public static readonly DependencyProperty LeadEdgeProgressProperty =
        DependencyProperty.Register(
            nameof(LeadEdgeProgress),
            typeof(double),
            typeof(SeaGlassPillNavBar),
            new PropertyMetadata(0.0));

    public static readonly DependencyProperty TrailEdgeProgressProperty =
        DependencyProperty.Register(
            nameof(TrailEdgeProgress),
            typeof(double),
            typeof(SeaGlassPillNavBar),
            new PropertyMetadata(0.0));

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public double LeadEdgeProgress
    {
        get => (double)GetValue(LeadEdgeProgressProperty);
        set => SetValue(LeadEdgeProgressProperty, value);
    }

    public double TrailEdgeProgress
    {
        get => (double)GetValue(TrailEdgeProgressProperty);
        set => SetValue(TrailEdgeProgressProperty, value);
    }

    public IReadOnlyList<(SeaGlassGlyph? Glyph, string Title)> Items { get; set; } = Array.Empty<(SeaGlassGlyph?, string)>();
    public Action<int>? OnSelect { get; set; }

    public float LeadIndex => _lozengeTravel.LeadIndex;
    public float TrailIndex => _lozengeTravel.TrailIndex;
    public float LeftEdgeSlot => MathF.Min(_lozengeTravel.LeadIndex, _lozengeTravel.TrailIndex);
    public float RightEdgeSlot => MathF.Max(_lozengeTravel.LeadIndex, _lozengeTravel.TrailIndex) + 1f;
    public float LeftEdgeTab => LeftEdgeSlot;
    public float LozengeSpan => RightEdgeSlot - LeftEdgeSlot;
    public float StretchSpan => Math.Clamp(MathF.Abs(_lozengeTravel.LeadIndex - _lozengeTravel.TrailIndex), 0f, 1.5f);
    public SeaGlassMaterial BarMaterial
    {
        get => Material;
        set => Material = value;
    }
    public SeaGlassMaterial ActiveIndicatorMaterial => BarMaterial.Deaged(_lozengeTravel.MotionEnergy * 0.92f);

    public event EventHandler<int>? SelectionChanged;

    public SeaGlassPillNavBar()
    {
        DefaultStyleKey = typeof(SeaGlassPillNavBar);
        IsTabStop = true;
        CornerAdaptationMode = SeaGlassCornerAdaptationMode.Fixed;
        ContentRole = SeaGlassContentRole.NavigationChrome;
        SizePx = new Vector2(440f, 60f);
    }

    public SeaGlassPillNavBar(
        IReadOnlyList<(SeaGlassGlyph? Glyph, string Title)> items,
        int selectedIndex,
        Action<int>? onSelect = null,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? material = null)
        : base(
            stageState: stageState,
            material: (material ?? new SeaGlassMaterial(
                tintColorArgb: unchecked((int)0xFFEA96FF),
                tintDensity: 0.72f,
                coarseness: 0.58f,
                frostRadiusDp: 28f,
                ior: 1.54f,
                chromaticDispersion: 0.22f,
                rimChalkiness: 0.90f,
                thicknessDp: 18f,
                cornerRadiusDp: 30f,
                cornerSmoothing: 2.5f,
                transparency: 0.52f)) with { CornerRadiusDp = 30f },
            cornerAdaptationMode: SeaGlassCornerAdaptationMode.Fixed,
            contentRole: SeaGlassContentRole.NavigationChrome)
    {
        DefaultStyleKey = typeof(SeaGlassPillNavBar);
        Items = items;
        SelectedIndex = Math.Clamp(selectedIndex, 0, Math.Max(0, items.Count - 1));
        _lozengeTravel.SnapTo(SelectedIndex);
        OnSelect = onSelect;
        SizePx = new Vector2(440f, 60f);
    }

    private static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassPillNavBar bar && e.NewValue is int idx)
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

    public (int TopArgb, float TopA, int BotArgb, float BotA, float RimA) ComputeInlayColors()
    {
        int frostedRindArgb = SeaGlassColorMath.ComputeFrostedRindColor(Material.TintColorArgb, Material.Coarseness, Material.RimChalkiness);
        int topArgb = SeaGlassColorMath.LerpOklab(frostedRindArgb, unchecked((int)0xFFFFFFFF), 0.70f);
        int botArgb = SeaGlassColorMath.LerpOklab(frostedRindArgb, Material.TintColorArgb, 0.22f);
        return (topArgb, 0.86f, botArgb, 0.72f, 0.65f);
    }

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        _lozengeTravel.Step(dtSeconds);
        LeadEdgeProgress = _lozengeTravel.LeadIndex;
        TrailEdgeProgress = _lozengeTravel.TrailIndex;
        Material = BarMaterial.Deaged(_lozengeTravel.MotionEnergy * 0.92f);
        return EvaluateFrame(dtSeconds, theme ?? SeaGlassTheme.Current);
    }

    private Grid? _root;
    private Border? _glassHost;
    private Border? _lozengeIndicator;
    private ItemsControl? _itemsPresenter;

    public Border? LozengeIndicator => _lozengeIndicator;
    public ItemsControl? ItemsPresenter => _itemsPresenter;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _root = GetTemplateChild("PART_Root") as Grid;
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _lozengeIndicator = GetTemplateChild("PART_LozengeIndicator") as Border;
        _itemsPresenter = GetTemplateChild("PART_ItemsPresenter") as ItemsControl;
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new SeaGlass.WinUI.Automation.SeaGlassPillNavBarAutomationPeer(this);
}

