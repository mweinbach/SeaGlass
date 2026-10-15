using System;
using System.Collections.Generic;
using System.Numerics;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// iOS 27 Cylindrical 3D Drum-Wheel Picker with a de-aged refractive selection lens bar.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_DrumWheel", Type = typeof(ItemsPresenter))]
[TemplatePart(Name = "PART_CenterMagnifierLens", Type = typeof(Border))]
public class SeaGlassWheelPicker : SeaGlassSurface
{
    private float _animatedIndex;
    private float _indexVel;

    public static readonly DependencyProperty ItemsProperty =
        DependencyProperty.Register(nameof(Items), typeof(IReadOnlyList<string>), typeof(SeaGlassWheelPicker),
            new PropertyMetadata(null));

    public static readonly DependencyProperty SelectedIndexProperty =
        DependencyProperty.Register(nameof(SelectedIndex), typeof(int), typeof(SeaGlassWheelPicker),
            new PropertyMetadata(0, (d, e) =>
            {
                var p = (SeaGlassWheelPicker)d;
                int idx = (int)e.NewValue;
                p.SelectionChanged?.Invoke(p, idx);
                p.OnSelectedIndexChange?.Invoke(idx);
            }));

    public IReadOnlyList<string> Items
    {
        get => (IReadOnlyList<string>)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public Action<int>? OnSelectedIndexChange { get; set; }
    public float AnimatedIndex => _animatedIndex;

    public event TypedEventHandler<SeaGlassWheelPicker, int>? SelectionChanged;

    public SeaGlassWheelPicker()
    {
        DefaultStyleKey = typeof(SeaGlassWheelPicker);
        IsTabStop = true;
        Items = [];
        SizePx = new Vector2(360f, 180f);
    }

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
        DefaultStyleKey = typeof(SeaGlassWheelPicker);
        Items = items;
        SelectedIndex = Math.Clamp(selectedIndex, 0, Math.Max(0, items.Count - 1));
        _animatedIndex = SelectedIndex;
        OnSelectedIndexChange = onSelectedIndexChange;
        SizePx = new Vector2(360f, 180f);
    }

    public void SelectItem(int index)
    {
        SelectedIndex = Math.Clamp(index, 0, Math.Max(0, (Items?.Count ?? 1) - 1));
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

    private Grid? _root;
    private Border? _glassHost;
    private ItemsPresenter? _drumWheel;
    private Border? _centerMagnifierLens;

    public ItemsPresenter? DrumWheel => _drumWheel;
    public Border? CenterMagnifierLens => _centerMagnifierLens;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _root = GetTemplateChild("PART_Root") as Grid;
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _drumWheel = GetTemplateChild("PART_DrumWheel") as ItemsPresenter;
        _centerMagnifierLens = GetTemplateChild("PART_CenterMagnifierLens") as Border;
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new SeaGlass.WinUI.Automation.SeaGlassWheelPickerAutomationPeer(this);
}

