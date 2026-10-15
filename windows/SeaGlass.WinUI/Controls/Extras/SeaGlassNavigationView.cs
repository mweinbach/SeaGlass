using System;
using System.Collections.Generic;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Hierarchical navigation control matching Windows 11 Fluent guidelines with collapsible pane
/// and sea-glass selection indicator.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_PaneRoot", Type = typeof(Border))]
[TemplatePart(Name = "PART_HeaderHost", Type = typeof(ContentPresenter))]
[TemplatePart(Name = "PART_MenuItemsHost", Type = typeof(ItemsControl))]
[TemplatePart(Name = "PART_FooterItemsHost", Type = typeof(ItemsControl))]
[TemplatePart(Name = "PART_ContentFrame", Type = typeof(Frame))]
[TemplatePart(Name = "PART_SelectionLozenge", Type = typeof(Border))]
public class SeaGlassNavigationView : SeaGlassSurface
{
    public static readonly DependencyProperty MenuItemsProperty =
        DependencyProperty.Register(nameof(MenuItems), typeof(IList<object>), typeof(SeaGlassNavigationView),
            new PropertyMetadata(null));

    public static readonly DependencyProperty FooterMenuItemsProperty =
        DependencyProperty.Register(nameof(FooterMenuItems), typeof(IList<object>), typeof(SeaGlassNavigationView),
            new PropertyMetadata(null));

    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(SeaGlassNavigationView),
            new PropertyMetadata(null, (d, e) =>
            {
                var nv = (SeaGlassNavigationView)d;
                nv.UpdateSelectionLozenge();
                nv.SelectionChanged?.Invoke(nv, null!);
            }));

    public static readonly DependencyProperty PaneDisplayModeProperty =
        DependencyProperty.Register(nameof(PaneDisplayMode), typeof(NavigationViewPaneDisplayMode), typeof(SeaGlassNavigationView),
            new PropertyMetadata(NavigationViewPaneDisplayMode.Auto));

    public static readonly DependencyProperty IsPaneOpenProperty =
        DependencyProperty.Register(nameof(IsPaneOpen), typeof(bool), typeof(SeaGlassNavigationView),
            new PropertyMetadata(true, (d, e) =>
            {
                var nv = (SeaGlassNavigationView)d;
                if ((bool)e.NewValue) nv.PaneOpened?.Invoke(nv, EventArgs.Empty);
                else nv.PaneClosed?.Invoke(nv, EventArgs.Empty);
            }));

    public static readonly DependencyProperty PaneTitleProperty =
        DependencyProperty.Register(nameof(PaneTitle), typeof(string), typeof(SeaGlassNavigationView),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(nameof(Header), typeof(object), typeof(SeaGlassNavigationView),
            new PropertyMetadata(null));

    public static readonly DependencyProperty AutoSuggestBoxProperty =
        DependencyProperty.Register(nameof(AutoSuggestBox), typeof(AutoSuggestBox), typeof(SeaGlassNavigationView),
            new PropertyMetadata(null));

    public IList<object> MenuItems
    {
        get => (IList<object>)GetValue(MenuItemsProperty);
        set => SetValue(MenuItemsProperty, value);
    }

    public IList<object> FooterMenuItems
    {
        get => (IList<object>)GetValue(FooterMenuItemsProperty);
        set => SetValue(FooterMenuItemsProperty, value);
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public NavigationViewPaneDisplayMode PaneDisplayMode
    {
        get => (NavigationViewPaneDisplayMode)GetValue(PaneDisplayModeProperty);
        set => SetValue(PaneDisplayModeProperty, value);
    }

    public bool IsPaneOpen
    {
        get => (bool)GetValue(IsPaneOpenProperty);
        set => SetValue(IsPaneOpenProperty, value);
    }

    public string PaneTitle
    {
        get => (string)GetValue(PaneTitleProperty);
        set => SetValue(PaneTitleProperty, value);
    }

    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public AutoSuggestBox? AutoSuggestBox
    {
        get => (AutoSuggestBox?)GetValue(AutoSuggestBoxProperty);
        set => SetValue(AutoSuggestBoxProperty, value);
    }

    public event TypedEventHandler<SeaGlassNavigationView, NavigationViewSelectionChangedEventArgs>? SelectionChanged;
    public event TypedEventHandler<SeaGlassNavigationView, NavigationViewItemInvokedEventArgs>? ItemInvoked;
    public event TypedEventHandler<SeaGlassNavigationView, EventArgs>? PaneOpened;
    public event TypedEventHandler<SeaGlassNavigationView, EventArgs>? PaneClosed;

    public SeaGlassNavigationView()
    {
        DefaultStyleKey = typeof(SeaGlassNavigationView);
        IsTabStop = true;
        MenuItems = new List<object>();
        FooterMenuItems = new List<object>();
    }

    public SeaGlassNavigationView(object? selectedItem = null) : this()
    {
        SelectedItem = selectedItem;
    }

    private Grid? _root;
    private Border? _glassHost;
    private Border? _paneRoot;
    private ContentPresenter? _headerHost;
    private ItemsControl? _menuItemsHost;
    private ItemsControl? _footerItemsHost;
    private Frame? _contentFrame;
    private Border? _selectionLozenge;

    public Border? PaneRoot => _paneRoot;
    public ItemsControl? MenuItemsHost => _menuItemsHost;
    public Frame? ContentFrame => _contentFrame;
    public Border? SelectionLozenge => _selectionLozenge;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _root = GetTemplateChild("PART_Root") as Grid;
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _paneRoot = GetTemplateChild("PART_PaneRoot") as Border;
        _headerHost = GetTemplateChild("PART_HeaderHost") as ContentPresenter;
        _menuItemsHost = GetTemplateChild("PART_MenuItemsHost") as ItemsControl;
        _footerItemsHost = GetTemplateChild("PART_FooterItemsHost") as ItemsControl;
        _contentFrame = GetTemplateChild("PART_ContentFrame") as Frame;
        _selectionLozenge = GetTemplateChild("PART_SelectionLozenge") as Border;
        UpdateSelectionLozenge();
    }

    public void UpdateSelectionLozenge()
    {
        if (_selectionLozenge == null) return;

        if (SelectedItem != null)
        {
            int index = -1;
            if (MenuItems != null)
            {
                index = MenuItems.IndexOf(SelectedItem);
            }
            if (index < 0) index = 0;

            _selectionLozenge.Margin = new Thickness(0, index * 40.0, 0, 0);
            _selectionLozenge.Visibility = Visibility.Visible;
        }
        else
        {
            _selectionLozenge.Visibility = Visibility.Collapsed;
        }
    }

    public void InvokeItem(object item)
    {
        SelectedItem = item;
        UpdateSelectionLozenge();
        ItemInvoked?.Invoke(this, null!);
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new SeaGlass.WinUI.Automation.SeaGlassNavigationViewAutomationPeer(this);
}
