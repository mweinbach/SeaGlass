using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Expressive frosted callout tooltip with micro-coarseness and entrance/exit spring.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_ContentPresenter", Type = typeof(ContentPresenter))]
public class SeaGlassToolTip : SeaGlassSurface
{
    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(SeaGlassToolTip),
            new PropertyMetadata(false, (d, e) =>
            {
                var tt = (SeaGlassToolTip)d;
                if ((bool)e.NewValue) tt.Opened?.Invoke(tt, new RoutedEventArgs());
                else tt.Closed?.Invoke(tt, new RoutedEventArgs());
            }));

    public static readonly DependencyProperty PlacementProperty =
        DependencyProperty.Register(nameof(Placement), typeof(PlacementMode), typeof(SeaGlassToolTip),
            new PropertyMetadata(PlacementMode.Top));

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public PlacementMode Placement
    {
        get => (PlacementMode)GetValue(PlacementProperty);
        set => SetValue(PlacementProperty, value);
    }

    public event RoutedEventHandler? Opened;
    public event RoutedEventHandler? Closed;

    public SeaGlassToolTip()
    {
        DefaultStyleKey = typeof(SeaGlassToolTip);
        Size = SeaGlassControlSize.Sm;
        Material = SeaGlassTheme.Current.Palette.ElevatedGlass with { Coarseness = 0.58f };
    }

    private Grid? _root;
    private Border? _glassHost;
    private ContentPresenter? _contentPresenter;

    public ContentPresenter? ContentPresenterChild => _contentPresenter;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _root = GetTemplateChild("PART_Root") as Grid;
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _contentPresenter = GetTemplateChild("PART_ContentPresenter") as ContentPresenter;
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new SeaGlass.WinUI.Automation.SeaGlassToolTipAutomationPeer(this);
}
