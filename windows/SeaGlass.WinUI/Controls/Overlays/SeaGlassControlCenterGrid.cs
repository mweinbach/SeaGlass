using System;
using System.Numerics;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// iOS 27 Expandable Control Center Platter Grid.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_Tile0", Type = typeof(Button))]
[TemplatePart(Name = "PART_Tile1", Type = typeof(Button))]
[TemplatePart(Name = "PART_Tile2", Type = typeof(Button))]
[TemplatePart(Name = "PART_Tile3", Type = typeof(Button))]
public class SeaGlassControlCenterGrid : SeaGlassSurface
{
    private float _expandProgress;
    private float _expandVel;
    private readonly SeaGlassMaterial _baseMaterial;

    public static readonly DependencyProperty ExpandedPlatterIndexProperty =
        DependencyProperty.Register(nameof(ExpandedPlatterIndex), typeof(int), typeof(SeaGlassControlCenterGrid),
            new PropertyMetadata(-1, (d, e) =>
            {
                var g = (SeaGlassControlCenterGrid)d;
                int idx = (int)e.NewValue;
                g.PlatterExpansionChanged?.Invoke(g, idx);
            }));

    public int ExpandedPlatterIndex
    {
        get => (int)GetValue(ExpandedPlatterIndexProperty);
        set => SetValue(ExpandedPlatterIndexProperty, value);
    }

    public float ExpandProgress => Math.Clamp(_expandProgress, 0f, 1.15f);

    public event TypedEventHandler<SeaGlassControlCenterGrid, int>? PlatterExpansionChanged;

    public SeaGlassControlCenterGrid()
    {
        DefaultStyleKey = typeof(SeaGlassControlCenterGrid);
        _baseMaterial = Material;
        SizePx = new Vector2(460f, 220f);
    }

    public SeaGlassControlCenterGrid(
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? material = null)
        : base(
            stageState: stageState ?? SeaGlassStageHost.AmbientStageState,
            material: material ?? SeaGlassTheme.Current.Palette.SurfaceGlass)
    {
        DefaultStyleKey = typeof(SeaGlassControlCenterGrid);
        _baseMaterial = Material;
        SizePx = new Vector2(460f, 220f);
    }

    public void TogglePlatterExpansion(int platterIndex)
    {
        ExpandedPlatterIndex = ExpandedPlatterIndex == platterIndex ? -1 : Math.Clamp(platterIndex, 0, 3);
    }

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        SeaGlassSpringSpec spatial = new(DampingRatio: 0.64f, Stiffness: 400f);
        spatial.Step(ref _expandProgress, ref _expandVel, ExpandedPlatterIndex >= 0 ? 1f : 0f, dtSeconds);
        float p = Math.Clamp(_expandProgress, 0f, 1f);
        Material = _baseMaterial.Deaged(4f * p * (1f - p));
        return EvaluateFrame(dtSeconds, activeTheme);
    }

    private Grid? _root;
    private Border? _glassHost;
    private Button? _tile0;
    private Button? _tile1;
    private Button? _tile2;
    private Button? _tile3;

    public Button? Tile0 => _tile0;
    public Button? Tile1 => _tile1;
    public Button? Tile2 => _tile2;
    public Button? Tile3 => _tile3;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        if (_tile0 != null) _tile0.Click -= OnTile0Click;
        if (_tile1 != null) _tile1.Click -= OnTile1Click;
        if (_tile2 != null) _tile2.Click -= OnTile2Click;
        if (_tile3 != null) _tile3.Click -= OnTile3Click;

        _root = GetTemplateChild("PART_Root") as Grid;
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _tile0 = GetTemplateChild("PART_Tile0") as Button;
        _tile1 = GetTemplateChild("PART_Tile1") as Button;
        _tile2 = GetTemplateChild("PART_Tile2") as Button;
        _tile3 = GetTemplateChild("PART_Tile3") as Button;

        if (_tile0 != null) _tile0.Click += OnTile0Click;
        if (_tile1 != null) _tile1.Click += OnTile1Click;
        if (_tile2 != null) _tile2.Click += OnTile2Click;
        if (_tile3 != null) _tile3.Click += OnTile3Click;
    }

    private void OnTile0Click(object sender, RoutedEventArgs e) => TogglePlatterExpansion(0);
    private void OnTile1Click(object sender, RoutedEventArgs e) => TogglePlatterExpansion(1);
    private void OnTile2Click(object sender, RoutedEventArgs e) => TogglePlatterExpansion(2);
    private void OnTile3Click(object sender, RoutedEventArgs e) => TogglePlatterExpansion(3);

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new SeaGlass.WinUI.Automation.SeaGlassControlCenterGridAutomationPeer(this);
}

