using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Corner-Anchored Liquid Teardrop Context Menu Morph.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_QuickActionsHost", Type = typeof(StackPanel))]
[TemplatePart(Name = "PART_MenuRowsHost", Type = typeof(ItemsControl))]
public class SeaGlassMorphingContextMenu : SeaGlassTwoElementMorph
{
    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.Register(
            nameof(IsOpen),
            typeof(bool),
            typeof(SeaGlassMorphingContextMenu),
            new PropertyMetadata(false, OnIsOpenChanged));

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public IReadOnlyList<(SeaGlassGlyph Glyph, string Title)> QuickHeaderActions { get; } =
    [
        (SeaGlassGlyph.Bookmark, "Pin"),
        (SeaGlassGlyph.Check, "Settle"),
        (SeaGlassGlyph.Sparkles, "Rename…")
    ];

    public IReadOnlyList<(SeaGlassGlyph Glyph, string Label, string Meta)> MenuRows { get; } =
    [
        (SeaGlassGlyph.Layers, "Files", ""),
        (SeaGlassGlyph.Tune, "Terminal", ""),
        (SeaGlassGlyph.Share, "Git", "main"),
        (SeaGlassGlyph.Bell, "Snooze", "›"),
        (SeaGlassGlyph.Sparkles, "Regenerate Title", ""),
        (SeaGlassGlyph.Close, "Delete…", "destructive")
    ];

    public Action<string>? OnActionSelected { get; set; }

    public event EventHandler<string>? ActionSelected;

    public SeaGlassMorphingContextMenu()
        : base(
            expanded: false,
            collapsedSizePx: new Vector2(44f, 44f),
            expandedSizePx: new Vector2(276f, 292f),
            morphMode: SeaGlassMorphMode.LiquidCoalesce,
            anchorAlignment: SeaGlassAnchorAlignment.TopEnd)
    {
        DefaultStyleKey = typeof(SeaGlassMorphingContextMenu);
    }

    public SeaGlassMorphingContextMenu(
        bool expanded = false,
        SeaGlassStageState? stageState = null,
        Action<string>? onActionSelected = null)
        : base(
            expanded: expanded,
            stageState: stageState,
            collapsedSizePx: new Vector2(44f, 44f),
            expandedSizePx: new Vector2(276f, 292f),
            morphMode: SeaGlassMorphMode.LiquidCoalesce,
            anchorAlignment: SeaGlassAnchorAlignment.TopEnd)
    {
        DefaultStyleKey = typeof(SeaGlassMorphingContextMenu);
        OnActionSelected = onActionSelected;
        IsOpen = expanded;
    }

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassMorphingContextMenu menu && e.NewValue is bool open)
        {
            menu.SetExpanded(open);
        }
    }

    public void SelectMenuAction(string actionLabel)
    {
        OnActionSelected?.Invoke(actionLabel);
        ActionSelected?.Invoke(this, actionLabel);
        IsOpen = false;
        SetExpanded(false);
    }

    private Grid? _root;
    private Border? _glassHost;
    private StackPanel? _quickActionsHost;
    private ItemsControl? _menuRowsHost;

    public StackPanel? QuickActionsHost => _quickActionsHost;
    public ItemsControl? MenuRowsHost => _menuRowsHost;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _root = GetTemplateChild("PART_Root") as Grid;
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _quickActionsHost = GetTemplateChild("PART_QuickActionsHost") as StackPanel;
        _menuRowsHost = GetTemplateChild("PART_MenuRowsHost") as ItemsControl;
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new SeaGlass.WinUI.Automation.SeaGlassMorphingContextMenuAutomationPeer(this);
}

