using System;
using System.Collections.Generic;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Lightweight popup flyout container and context menu with optical sea-glass backdrop.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_Popup", Type = typeof(Popup))]
[TemplatePart(Name = "PART_ContentPresenter", Type = typeof(ContentPresenter))]
public class SeaGlassFlyout : SeaGlassSurface
{
    public static readonly DependencyProperty FlyoutContentProperty =
        DependencyProperty.Register(nameof(FlyoutContent), typeof(UIElement), typeof(SeaGlassFlyout),
            new PropertyMetadata(null));

    public static readonly DependencyProperty PlacementProperty =
        DependencyProperty.Register(nameof(Placement), typeof(FlyoutPlacementMode), typeof(SeaGlassFlyout),
            new PropertyMetadata(FlyoutPlacementMode.Bottom));

    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(SeaGlassFlyout),
            new PropertyMetadata(false, (d, e) =>
            {
                var f = (SeaGlassFlyout)d;
                if ((bool)e.NewValue) f.Opened?.Invoke(f, EventArgs.Empty);
                else f.Closed?.Invoke(f, EventArgs.Empty);
            }));

    public static readonly DependencyProperty LightDismissOverlayModeProperty =
        DependencyProperty.Register(nameof(LightDismissOverlayMode), typeof(LightDismissOverlayMode), typeof(SeaGlassFlyout),
            new PropertyMetadata(LightDismissOverlayMode.Auto));

    public UIElement? FlyoutContent
    {
        get => (UIElement?)GetValue(FlyoutContentProperty);
        set => SetValue(FlyoutContentProperty, value);
    }

    public FlyoutPlacementMode Placement
    {
        get => (FlyoutPlacementMode)GetValue(PlacementProperty);
        set => SetValue(PlacementProperty, value);
    }

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public LightDismissOverlayMode LightDismissOverlayMode
    {
        get => (LightDismissOverlayMode)GetValue(LightDismissOverlayModeProperty);
        set => SetValue(LightDismissOverlayModeProperty, value);
    }

    public event TypedEventHandler<SeaGlassFlyout, EventArgs>? Opening;
    public event TypedEventHandler<SeaGlassFlyout, EventArgs>? Opened;
    public event TypedEventHandler<SeaGlassFlyout, EventArgs>? Closing;
    public event TypedEventHandler<SeaGlassFlyout, EventArgs>? Closed;

    public SeaGlassFlyout()
    {
        DefaultStyleKey = typeof(SeaGlassFlyout);
        Material = SeaGlassTheme.Current.Palette.ElevatedGlass;
    }

    private Grid? _root;
    private Border? _glassHost;
    private Popup? _popup;
    private ContentPresenter? _contentPresenter;

    public Popup? Popup => _popup;
    public ContentPresenter? ContentPresenterChild => _contentPresenter;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _root = GetTemplateChild("PART_Root") as Grid;
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _popup = GetTemplateChild("PART_Popup") as Popup;
        _contentPresenter = GetTemplateChild("PART_ContentPresenter") as ContentPresenter;
    }

    public void ShowAt(FrameworkElement targetElement)
    {
        Opening?.Invoke(this, EventArgs.Empty);
        IsOpen = true;
    }

    public void Hide()
    {
        Closing?.Invoke(this, EventArgs.Empty);
        IsOpen = false;
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new SeaGlass.WinUI.Automation.SeaGlassFlyoutAutomationPeer(this);
}
