using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Material 3 Expressive Floating Split Toolbar whose action pill and primary FAB orb
/// coalesce across a liquid sminCubic SDF bridge when docked and split apart when detached.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_ToolbarBar", Type = typeof(Border))]
[TemplatePart(Name = "PART_ItemsPresenter", Type = typeof(ItemsControl))]
[TemplatePart(Name = "PART_FabOrb", Type = typeof(Button))]
public class SeaGlassFloatingToolbar : SeaGlassSurface
{
    private float _gapDp;
    private float _gapVel;

    public static readonly DependencyProperty SelectedIndexProperty =
        DependencyProperty.Register(
            nameof(SelectedIndex),
            typeof(int),
            typeof(SeaGlassFloatingToolbar),
            new PropertyMetadata(0));

    public static readonly DependencyProperty OrientationProperty =
        DependencyProperty.Register(
            nameof(Orientation),
            typeof(Orientation),
            typeof(SeaGlassFloatingToolbar),
            new PropertyMetadata(Orientation.Horizontal));

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public IReadOnlyList<(SeaGlassGlyph Glyph, string Label)> Actions { get; set; } = Array.Empty<(SeaGlassGlyph, string)>();
    public int SelectedActionIndex
    {
        get => SelectedIndex;
        set => SelectedIndex = value;
    }
    public Action<int>? OnActionSelect { get; set; }
    public SeaGlassGlyph FabGlyph { get; set; } = SeaGlassGlyph.Sparkles;
    public bool FabDetached { get; set; }
    public Action? OnFabClick { get; set; }
    public SeaGlassMaterial BarMaterial { get; set; }
    public SeaGlassMaterial FabMaterial { get; set; }
    public float CurrentGapDp => _gapDp;

    public event EventHandler<int>? ItemSelected;
    public event EventHandler? FabClicked;

    public SeaGlassFloatingToolbar()
    {
        DefaultStyleKey = typeof(SeaGlassFloatingToolbar);
        IsTabStop = true;
        ContentRole = SeaGlassContentRole.NavigationChrome;
        BarMaterial = SeaGlassTheme.Current.Palette.SurfaceGlass;
        FabMaterial = SeaGlassTheme.Current.Palette.PrimaryGlass;
        SizePx = new Vector2(460f, 58f);
    }

    public SeaGlassFloatingToolbar(
        IReadOnlyList<(SeaGlassGlyph Glyph, string Label)> actions,
        int selectedActionIndex,
        Action<int>? onActionSelect = null,
        SeaGlassGlyph fabGlyph = SeaGlassGlyph.Sparkles,
        bool fabDetached = false,
        Action? onFabClick = null,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? barMaterial = null,
        SeaGlassMaterial? fabMaterial = null)
        : base(
            stageState: stageState,
            material: barMaterial ?? SeaGlassTheme.Current.Palette.SurfaceGlass,
            contentRole: SeaGlassContentRole.NavigationChrome)
    {
        DefaultStyleKey = typeof(SeaGlassFloatingToolbar);
        Actions = actions;
        SelectedIndex = selectedActionIndex;
        OnActionSelect = onActionSelect;
        FabGlyph = fabGlyph;
        FabDetached = fabDetached;
        _gapDp = fabDetached ? 18f : -6f;
        OnFabClick = onFabClick;
        BarMaterial = barMaterial ?? SeaGlassTheme.Current.Palette.SurfaceGlass;
        FabMaterial = fabMaterial ?? SeaGlassTheme.Current.Palette.PrimaryGlass;
        SizePx = new Vector2(460f, 58f);
    }

    private Grid? _root;
    private Border? _glassHost;
    private Border? _toolbarBar;
    private ItemsControl? _itemsPresenter;
    private Button? _fabOrb;

    public Border? ToolbarBar => _toolbarBar;
    public ItemsControl? ItemsPresenter => _itemsPresenter;
    public Button? FabOrb => _fabOrb;
    public Border? GlassHost => _glassHost;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        if (_fabOrb != null)
        {
            _fabOrb.Click -= OnFabOrbClick;
        }

        _root = GetTemplateChild("PART_Root") as Grid;
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _toolbarBar = GetTemplateChild("PART_ToolbarBar") as Border;
        _itemsPresenter = GetTemplateChild("PART_ItemsPresenter") as ItemsControl;
        _fabOrb = GetTemplateChild("PART_FabOrb") as Button;

        if (_fabOrb != null)
        {
            _fabOrb.Click += OnFabOrbClick;
        }
    }

    private void OnFabOrbClick(object sender, RoutedEventArgs e)
    {
        InvokeFab();
    }

    public void SelectItem(int index)
    {
        SelectedIndex = index;
        OnActionSelect?.Invoke(index);
        ItemSelected?.Invoke(this, index);
    }

    public void InvokeFab()
    {
        OnFabClick?.Invoke();
        FabClicked?.Invoke(this, EventArgs.Empty);
    }

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        float density = MathF.Max(0.5f, activeTheme.DpiScale);
        SeaGlassSpringSpec gapSpring = new(DampingRatio: 0.62f, Stiffness: 380f);
        gapSpring.Step(ref _gapDp, ref _gapVel, FabDetached ? 18f : -6f, dtSeconds);

        float gapNorm = Math.Clamp((_gapDp + 6f) / 24f, 0f, 1f);
        float energy = Math.Clamp(4f * gapNorm * (1f - gapNorm), 0f, 1f);
        Material = BarMaterial.Deaged(energy);

        float totalW = MathF.Max(200f, SizePx.X);
        float heightPx = MathF.Max(40f, SizePx.Y);
        float gapPx = _gapDp * density;
        float mainBarWidthPx = MathF.Max(heightPx * 2f, totalW - heightPx - MathF.Max(0f, gapPx));
        float fabLeftPx = MathF.Min(totalW - heightPx, mainBarWidthPx + gapPx);

        List<SeaGlassSdfEngine.SdfNode> sdfNodes =
        [
            new SeaGlassSdfEngine.SdfNode(
                centerX: mainBarWidthPx * 0.5f,
                centerY: heightPx * 0.5f,
                halfWidth: mainBarWidthPx * 0.5f,
                halfHeight: heightPx * 0.5f,
                uniformCornerRadius: heightPx * 0.5f,
                cornerSmoothing: 2.5f,
                tintColorArgb: BarMaterial.TintColorArgb,
                thicknessPx: BarMaterial.ThicknessDp * density),
            new SeaGlassSdfEngine.SdfNode(
                centerX: fabLeftPx + heightPx * 0.5f,
                centerY: heightPx * 0.5f,
                halfWidth: heightPx * 0.5f,
                halfHeight: heightPx * 0.5f,
                uniformCornerRadius: heightPx * 0.5f,
                cornerSmoothing: 2.5f,
                tintColorArgb: FabMaterial.TintColorArgb,
                thicknessPx: FabMaterial.ThicknessDp * density)
        ];

        return EvaluateFrame(dtSeconds, activeTheme, sdfNodes, smoothRadiusDp: 32f);
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new SeaGlass.WinUI.Automation.SeaGlassFloatingToolbarAutomationPeer(this);
}
