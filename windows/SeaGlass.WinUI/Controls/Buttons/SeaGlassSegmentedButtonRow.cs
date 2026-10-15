using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Segmented button row coordinating multiple options with viscoelastic lead-lag lozenge travel.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_IndicatorLozenge", Type = typeof(Border))]
[TemplatePart(Name = "PART_ItemsHost", Type = typeof(ItemsControl))]
public class SeaGlassSegmentedButtonRow : SeaGlassSurface
{
    private readonly SeaGlassLeadLagLozengeTravel _lozengeTravel = new();

    public static readonly DependencyProperty SelectedIndexProperty =
        DependencyProperty.Register(
            nameof(SelectedIndex),
            typeof(int),
            typeof(SeaGlassSegmentedButtonRow),
            new PropertyMetadata(0, OnSelectedIndexChanged));

    public static readonly DependencyProperty OptionsProperty =
        DependencyProperty.Register(
            nameof(Options),
            typeof(IReadOnlyList<string>),
            typeof(SeaGlassSegmentedButtonRow),
            new PropertyMetadata(Array.Empty<string>()));

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public IReadOnlyList<string> Options
    {
        get => (IReadOnlyList<string>)GetValue(OptionsProperty);
        set => SetValue(OptionsProperty, value);
    }

    public Action<int>? OnSelect { get; set; }
    public SeaGlassMaterial ContainerMaterial { get; set; }
    public SeaGlassMaterial IndicatorMaterial { get; set; }

    public float LeadIndex => _lozengeTravel.LeadIndex;
    public float TrailIndex => _lozengeTravel.TrailIndex;
    public float AnimatedIndex => _lozengeTravel.CenterIndex;
    public float LeftEdgeSlot => MathF.Min(_lozengeTravel.LeadIndex, _lozengeTravel.TrailIndex);
    public float RightEdgeSlot => MathF.Max(_lozengeTravel.LeadIndex, _lozengeTravel.TrailIndex) + 1f;
    public float LozengeSlotSpan => _lozengeTravel.LozengeSlotSpan;
    public float MotionStretch => Math.Clamp(MathF.Abs(_lozengeTravel.LeadIndex - _lozengeTravel.TrailIndex), 0f, 2f);
    public float MotionEnergy => _lozengeTravel.MotionEnergy;
    public float IndicatorScaleX => LozengeSlotSpan;
    public float IndicatorScaleY => 1f - Math.Clamp(MotionStretch, 0f, 1.2f) * 0.08f;

    public SeaGlassMaterial ActiveIndicatorMaterial => (IndicatorMaterial with
    {
        ThicknessDp = IndicatorMaterial.ThicknessDp + 4f * MotionEnergy,
        ChromaticDispersion = Math.Clamp(IndicatorMaterial.ChromaticDispersion + 0.015f * MotionEnergy, 0f, 1f)
    }).Deaged(MotionEnergy);

    public event EventHandler<int>? SelectionChanged;

    public SeaGlassSegmentedButtonRow()
    {
        DefaultStyleKey = typeof(SeaGlassSegmentedButtonRow);
        ContainerMaterial = SeaGlassTheme.Current.Palette.SurfaceGlass;
        IndicatorMaterial = SeaGlassTheme.Current.Palette.PrimaryGlass;
        SizePx = new Vector2(420f, 46f);
        IsTabStop = true;
    }

    public SeaGlassSegmentedButtonRow(
        IReadOnlyList<string> options,
        int selectedIndex,
        Action<int>? onSelect = null,
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
        DefaultStyleKey = typeof(SeaGlassSegmentedButtonRow);
        Options = options;
        SelectedIndex = Math.Clamp(selectedIndex, 0, Math.Max(0, options.Count - 1));
        _lozengeTravel.SnapTo(SelectedIndex);
        OnSelect = onSelect;
        ContainerMaterial = containerMaterial ?? SeaGlassTheme.Current.Palette.SurfaceGlass;
        IndicatorMaterial = indicatorMaterial ?? SeaGlassTheme.Current.Palette.PrimaryGlass;
        SizePx = new Vector2(420f, 46f);
    }

    private static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassSegmentedButtonRow row && e.NewValue is int idx)
        {
            row._lozengeTravel.SetTarget(idx);
            row.OnSelect?.Invoke(idx);
            row.SelectionChanged?.Invoke(row, idx);
        }
    }

    public void SelectOption(int index)
    {
        SelectedIndex = Math.Clamp(index, 0, Math.Max(0, (Options?.Count ?? 1) - 1));
    }

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        if (ContainerMaterial.CornerRadiusDp < 16f)
        {
            Material = ContainerMaterial;
        }
        _lozengeTravel.Step(dtSeconds);
        return EvaluateFrame(dtSeconds, activeTheme);
    }

    private Grid? _root;
    private Border? _glassHost;
    private Border? _indicatorLozenge;
    private ItemsControl? _itemsHost;

    public Border? IndicatorLozenge => _indicatorLozenge;
    public ItemsControl? ItemsHost => _itemsHost;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _root = GetTemplateChild("PART_Root") as Grid;
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _indicatorLozenge = GetTemplateChild("PART_IndicatorLozenge") as Border;
        _itemsHost = GetTemplateChild("PART_ItemsHost") as ItemsControl;
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new Automation.SeaGlassSegmentedButtonRowAutomationPeer(this);
}

