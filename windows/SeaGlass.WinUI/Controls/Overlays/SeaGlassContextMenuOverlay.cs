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
/// iOS-Style Context Menu with Target Preview Lift, Quick-Reaction Pill Bar,
/// and In-Place Submenu Morphing (<see cref="SeaGlassContextMenuOverlay"/>).
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_BackdropDimmer", Type = typeof(Border))]
[TemplatePart(Name = "PART_MenuCard", Type = typeof(Border))]
[TemplatePart(Name = "PART_ItemsPresenter", Type = typeof(ItemsControl))]
public class SeaGlassContextMenuOverlay : SeaGlassSurface
{
    private float _openProgress;
    private float _openVel;
    private float _submenuProgress;
    private float _submenuVel;
    private readonly SeaGlassMaterial _baseMaterial;

    public static readonly DependencyProperty VisibleProperty =
        DependencyProperty.Register(nameof(Visible), typeof(bool), typeof(SeaGlassContextMenuOverlay),
            new PropertyMetadata(false, (d, e) => ((SeaGlassContextMenuOverlay)d)._openProgress = (bool)e.NewValue ? 1f : 0f));

    public static readonly DependencyProperty ItemsProperty =
        DependencyProperty.Register(nameof(Items), typeof(IReadOnlyList<SeaGlassContextMenuItem>), typeof(SeaGlassContextMenuOverlay),
            new PropertyMetadata(null));

    public static readonly DependencyProperty ActiveSubmenuParentProperty =
        DependencyProperty.Register(nameof(ActiveSubmenuParent), typeof(SeaGlassContextMenuItem), typeof(SeaGlassContextMenuOverlay),
            new PropertyMetadata(null));

    public static readonly DependencyProperty DismissCommandProperty =
        DependencyProperty.Register(nameof(DismissCommand), typeof(ICommand), typeof(SeaGlassContextMenuOverlay),
            new PropertyMetadata(null));

    public static readonly DependencyProperty ItemCommandProperty =
        DependencyProperty.Register(nameof(ItemCommand), typeof(ICommand), typeof(SeaGlassContextMenuOverlay),
            new PropertyMetadata(null));

    public bool Visible
    {
        get => (bool)GetValue(VisibleProperty);
        set => SetValue(VisibleProperty, value);
    }

    public IReadOnlyList<SeaGlassContextMenuItem> Items
    {
        get => (IReadOnlyList<SeaGlassContextMenuItem>)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public SeaGlassContextMenuItem? ActiveSubmenuParent
    {
        get => (SeaGlassContextMenuItem?)GetValue(ActiveSubmenuParentProperty);
        set => SetValue(ActiveSubmenuParentProperty, value);
    }

    public ICommand? DismissCommand
    {
        get => (ICommand?)GetValue(DismissCommandProperty);
        set => SetValue(DismissCommandProperty, value);
    }

    public ICommand? ItemCommand
    {
        get => (ICommand?)GetValue(ItemCommandProperty);
        set => SetValue(ItemCommandProperty, value);
    }

    public Action? OnDismissRequest { get; set; }
    public Action<SeaGlassContextMenuItem>? OnItemSelected { get; set; }

    public float OpenProgress => Math.Clamp(_openProgress, 0f, 1.15f);
    public float SubmenuProgress => Math.Clamp(_submenuProgress, 0f, 1.15f);
    public float TargetPreviewLiftScale => 1f + 0.035f * Math.Clamp(_openProgress, 0f, 1f);

    public event TypedEventHandler<SeaGlassContextMenuOverlay, EventArgs>? DismissRequested;
    public event TypedEventHandler<SeaGlassContextMenuOverlay, SeaGlassContextMenuItem>? ItemSelected;

    public SeaGlassContextMenuOverlay()
    {
        DefaultStyleKey = typeof(SeaGlassContextMenuOverlay);
        _baseMaterial = Material;
        Items = DefaultIosContextMenuItems();
        SizePx = new Vector2(280f, 340f);
    }

    public SeaGlassContextMenuOverlay(
        bool visible,
        Action onDismissRequest,
        Action<SeaGlassContextMenuItem> onItemSelected,
        IReadOnlyList<SeaGlassContextMenuItem>? items = null,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? material = null)
        : base(
            stageState: stageState ?? SeaGlassStageHost.AmbientStageState,
            material: material ?? (SeaGlassTheme.Current.Palette.SurfaceGlass with
            {
                CornerRadiusDp = 26f,
                Transparency = 0.54f,
                Coarseness = 0.44f,
                FrostRadiusDp = 30f,
                ThicknessDp = 20f,
                RimChalkiness = 0.88f
            }))
    {
        DefaultStyleKey = typeof(SeaGlassContextMenuOverlay);
        _baseMaterial = Material;
        Visible = visible;
        _openProgress = visible ? 1f : 0f;
        OnDismissRequest = onDismissRequest;
        OnItemSelected = onItemSelected;
        Items = items ?? DefaultIosContextMenuItems();
        SizePx = new Vector2(280f, 340f);
    }

    public void SetVisible(bool visible)
    {
        Visible = visible;
        if (!visible)
        {
            ActiveSubmenuParent = null;
            OnDismissRequest?.Invoke();
            DismissRequested?.Invoke(this, EventArgs.Empty);
            DismissCommand?.Execute(null);
        }
    }

    public void ActivateItem(SeaGlassContextMenuItem item)
    {
        if (item.HasSubmenu)
        {
            ActiveSubmenuParent = item;
        }
        else
        {
            OnItemSelected?.Invoke(item);
            ItemSelected?.Invoke(this, item);
            ItemCommand?.Execute(item);
            SetVisible(false);
        }
    }

    public void PopSubmenu() => ActiveSubmenuParent = null;

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        SeaGlassSpringSpec spatial = new(DampingRatio: 0.64f, Stiffness: 410f);
        spatial.Step(ref _openProgress, ref _openVel, Visible ? 1f : 0f, dtSeconds);
        spatial.Step(ref _submenuProgress, ref _submenuVel, ActiveSubmenuParent is not null ? 1f : 0f, dtSeconds);

        float op = Math.Clamp(_openProgress, 0f, 1f);
        float sp = Math.Clamp(_submenuProgress, 0f, 1f);
        float energy = MathF.Max(4f * op * (1f - op), 4f * sp * (1f - sp));
        Material = _baseMaterial.Deaged(energy);

        return EvaluateFrame(dtSeconds, activeTheme);
    }

    public static IReadOnlyList<SeaGlassContextMenuItem> DefaultIosContextMenuItems() =>
    [
        new("share", "Share Optical Preset…", SeaGlassGlyph.Share),
        new("palette", "Assign Glass Palette", SeaGlassGlyph.Sparkles,
            SubmenuTitle: "Choose SeaGlass Palette",
            SubmenuItems:
            [
                new("pal_mint", "Seafoam Mint", SeaGlassGlyph.Check, Checked: true),
                new("pal_cobalt", "Cobalt Tide", SeaGlassGlyph.Sparkles),
                new("pal_amber", "Beach Amber", SeaGlassGlyph.Sparkles),
                new("pal_lavender", "Sea Lavender", SeaGlassGlyph.Sparkles)
            ]),
        new("pin", "Pin to Control Platter", SeaGlassGlyph.Bookmark),
        new("inspect", "Inspect std140 UBO Slots", SeaGlassGlyph.Info, StartsNewSection: true),
        new("reset", "Reset Surface Optics", SeaGlassGlyph.Close, IsDestructive: true, StartsNewSection: true)
    ];

    private Grid? _root;
    private Border? _glassHost;
    private Border? _backdropDimmer;
    private Border? _menuCard;
    private ItemsControl? _itemsPresenter;

    public Border? BackdropDimmer => _backdropDimmer;
    public Border? MenuCard => _menuCard;
    public ItemsControl? ItemsPresenter => _itemsPresenter;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _root = GetTemplateChild("PART_Root") as Grid;
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _backdropDimmer = GetTemplateChild("PART_BackdropDimmer") as Border;
        _menuCard = GetTemplateChild("PART_MenuCard") as Border;
        _itemsPresenter = GetTemplateChild("PART_ItemsPresenter") as ItemsControl;
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new SeaGlass.WinUI.Automation.SeaGlassContextMenuOverlayAutomationPeer(this);
}

