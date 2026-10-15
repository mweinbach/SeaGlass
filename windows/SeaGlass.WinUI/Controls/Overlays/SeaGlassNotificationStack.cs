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
/// iOS 27 Depth-Stacked Notification Rollup Stack that fans out into individual frosted cards.
/// Note: SeaGlassNotificationStack hosts cards exclusively via PART_CardsContainer; it does not host content via PART_ContentPresenter.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_CardsContainer", Type = typeof(Canvas))]
public class SeaGlassNotificationStack : SeaGlassSurface
{
    private float _fanProgress;
    private float _fanVel;
    private readonly SeaGlassMaterial _baseMaterial;

    public static readonly DependencyProperty ExpandedProperty =
        DependencyProperty.Register(nameof(Expanded), typeof(bool), typeof(SeaGlassNotificationStack),
            new PropertyMetadata(false, (d, e) =>
            {
                var s = (SeaGlassNotificationStack)d;
                bool exp = (bool)e.NewValue;
                s._fanProgress = exp ? 1f : 0f;
                s.ExpandedChanged?.Invoke(s, exp);
            }));

    public static readonly DependencyProperty NotificationsProperty =
        DependencyProperty.Register(nameof(Notifications), typeof(IReadOnlyList<SeaGlassNotificationItem>), typeof(SeaGlassNotificationStack),
            new PropertyMetadata(null));

    public static readonly DependencyProperty ToggleCommandProperty =
        DependencyProperty.Register(nameof(ToggleCommand), typeof(ICommand), typeof(SeaGlassNotificationStack),
            new PropertyMetadata(null));

    public bool Expanded
    {
        get => (bool)GetValue(ExpandedProperty);
        set => SetValue(ExpandedProperty, value);
    }

    public IReadOnlyList<SeaGlassNotificationItem> Notifications
    {
        get => (IReadOnlyList<SeaGlassNotificationItem>)GetValue(NotificationsProperty);
        set => SetValue(NotificationsProperty, value);
    }

    public ICommand? ToggleCommand
    {
        get => (ICommand?)GetValue(ToggleCommandProperty);
        set => SetValue(ToggleCommandProperty, value);
    }

    public float FanProgress => Math.Clamp(_fanProgress, 0f, 1.15f);

    public event TypedEventHandler<SeaGlassNotificationStack, bool>? ExpandedChanged;
    public event TypedEventHandler<SeaGlassNotificationStack, SeaGlassNotificationItem>? NotificationClick;

    public SeaGlassNotificationStack()
    {
        DefaultStyleKey = typeof(SeaGlassNotificationStack);
        _baseMaterial = Material;
        Notifications = DefaultNotifications();
        SizePx = new Vector2(440f, 112f);
        OnClick = () => ToggleExpanded();
    }

    public SeaGlassNotificationStack(
        bool expanded = false,
        IReadOnlyList<SeaGlassNotificationItem>? notifications = null,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? material = null)
        : base(
            stageState: stageState ?? SeaGlassStageHost.AmbientStageState,
            material: material ?? SeaGlassTheme.Current.Palette.SurfaceGlass)
    {
        DefaultStyleKey = typeof(SeaGlassNotificationStack);
        _baseMaterial = Material;
        Expanded = expanded;
        _fanProgress = expanded ? 1f : 0f;
        Notifications = notifications ?? DefaultNotifications();
        SizePx = new Vector2(440f, expanded ? 260f : 112f);
        OnClick = () => ToggleExpanded();
    }

    public void ToggleExpanded()
    {
        Expanded = !Expanded;
        ToggleCommand?.Execute(Expanded);
    }

    public void OnItemClicked(SeaGlassNotificationItem item)
    {
        NotificationClick?.Invoke(this, item);
    }

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        SeaGlassSpringSpec spatial = new(DampingRatio: 0.64f, Stiffness: 400f);
        spatial.Step(ref _fanProgress, ref _fanVel, Expanded ? 1f : 0f, dtSeconds);

        float fp = Math.Clamp(_fanProgress, 0f, 1f);
        SizePx = new Vector2(440f, 112f + (260f - 112f) * fp);
        Material = _baseMaterial.Deaged(4f * fp * (1f - fp));
        return EvaluateFrame(dtSeconds, activeTheme);
    }

    public static IReadOnlyList<SeaGlassNotificationItem> DefaultNotifications() =>
    [
        new("n1", "OPTICS LAB", "now", "Analytical Single-Pass SDF Active",
            "sgRoundedBoxSdfAndGrad reduced single-node evaluations from 5 passes to 1.", SeaGlassGlyph.Sparkles),
        new("n2", "TEMPORAL GLASS", "2m ago", "De-Aging & Solidification Enabled",
            "In-flight glass melts into clear liquid and re-crystallizes on settle.", SeaGlassGlyph.Layers),
        new("n3", "DIRECT2D HLSL", "6m ago", "256-Byte cbuffer Delta Tracking",
            "Dirty-slot bitmask skips redundant GPU constant-buffer transfers.", SeaGlassGlyph.Tune)
    ];

    private Grid? _root;
    private Border? _glassHost;
    private Canvas? _cardsContainer;

    public Canvas? CardsContainer => _cardsContainer;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _root = GetTemplateChild("PART_Root") as Grid;
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _cardsContainer = GetTemplateChild("PART_CardsContainer") as Canvas;
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new SeaGlass.WinUI.Automation.SeaGlassNotificationStackAutomationPeer(this);
}

