using System;
using System.Collections.Generic;
using ICommand = global::System.Windows.Input.ICommand;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Document/browser tab strip with tab addition, close buttons, and active tab glass pill indicator.
/// Integrates asymmetric lead-lag lozenge motion (k_lead=540, k_trail=310).
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_TabStrip", Type = typeof(ItemsControl))]
[TemplatePart(Name = "PART_AddTabButton", Type = typeof(Button))]
[TemplatePart(Name = "PART_TabLozenge", Type = typeof(Border))]
[TemplatePart(Name = "PART_ContentHost", Type = typeof(ContentPresenter))]
public class SeaGlassTabView : SeaGlassSurface
{
    private readonly SeaGlassLeadLagLozengeTravel _lozengeTravel = new(500f, 390f);

    public static readonly DependencyProperty TabItemsProperty =
        DependencyProperty.Register(nameof(TabItems), typeof(IList<object>), typeof(SeaGlassTabView),
            new PropertyMetadata(null));

    public static readonly DependencyProperty SelectedIndexProperty =
        DependencyProperty.Register(nameof(SelectedIndex), typeof(int), typeof(SeaGlassTabView),
            new PropertyMetadata(-1, (d, e) =>
            {
                var tv = (SeaGlassTabView)d;
                tv.SelectionChanged?.Invoke(tv, null!);
            }));

    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(SeaGlassTabView),
            new PropertyMetadata(null));

    public static readonly DependencyProperty CanAddTabsProperty =
        DependencyProperty.Register(nameof(CanAddTabs), typeof(bool), typeof(SeaGlassTabView),
            new PropertyMetadata(true));

    public static readonly DependencyProperty CanCloseTabsProperty =
        DependencyProperty.Register(nameof(CanCloseTabs), typeof(bool), typeof(SeaGlassTabView),
            new PropertyMetadata(true));

    public static readonly DependencyProperty TabStripHeaderProperty =
        DependencyProperty.Register(nameof(TabStripHeader), typeof(object), typeof(SeaGlassTabView),
            new PropertyMetadata(null));

    public static readonly DependencyProperty TabStripFooterProperty =
        DependencyProperty.Register(nameof(TabStripFooter), typeof(object), typeof(SeaGlassTabView),
            new PropertyMetadata(null));

    public static readonly DependencyProperty AddTabButtonCommandProperty =
        DependencyProperty.Register(nameof(AddTabButtonCommand), typeof(ICommand), typeof(SeaGlassTabView),
            new PropertyMetadata(null));

    public IList<object> TabItems
    {
        get => (IList<object>)GetValue(TabItemsProperty);
        set => SetValue(TabItemsProperty, value);
    }

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public bool CanAddTabs
    {
        get => (bool)GetValue(CanAddTabsProperty);
        set => SetValue(CanAddTabsProperty, value);
    }

    public bool CanCloseTabs
    {
        get => (bool)GetValue(CanCloseTabsProperty);
        set => SetValue(CanCloseTabsProperty, value);
    }

    public object? TabStripHeader
    {
        get => GetValue(TabStripHeaderProperty);
        set => SetValue(TabStripHeaderProperty, value);
    }

    public object? TabStripFooter
    {
        get => GetValue(TabStripFooterProperty);
        set => SetValue(TabStripFooterProperty, value);
    }

    public ICommand? AddTabButtonCommand
    {
        get => (ICommand?)GetValue(AddTabButtonCommandProperty);
        set => SetValue(AddTabButtonCommandProperty, value);
    }

    public SeaGlassLeadLagLozengeTravel LozengeTravel => _lozengeTravel;

    public event SelectionChangedEventHandler? SelectionChanged;
    public event TypedEventHandler<SeaGlassTabView, EventArgs>? AddTabButtonClick;
    public event TypedEventHandler<SeaGlassTabView, TabViewTabCloseRequestedEventArgs>? TabCloseRequested;
    public event TypedEventHandler<SeaGlassTabView, TabViewTabDragStartingEventArgs>? TabDragStarting;
    public event TypedEventHandler<SeaGlassTabView, TabViewTabDroppedEventArgs>? TabDropped;

    public SeaGlassTabView()
    {
        DefaultStyleKey = typeof(SeaGlassTabView);
        IsTabStop = true;
        TabItems = new List<object>();
    }

    public SeaGlassTabView(int selectedIndex = 0) : this()
    {
        SelectedIndex = selectedIndex;
    }

    private Grid? _root;
    private Border? _glassHost;
    private ItemsControl? _tabStrip;
    private Button? _addTabButton;
    private Border? _tabLozenge;
    private ContentPresenter? _contentHost;

    public ItemsControl? TabStrip => _tabStrip;
    public Button? AddTabButton => _addTabButton;
    public Border? TabLozenge => _tabLozenge;
    public ContentPresenter? ContentHost => _contentHost;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        if (_addTabButton != null)
        {
            _addTabButton.Click -= OnAddTabButtonClick;
        }

        _root = GetTemplateChild("PART_Root") as Grid;
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _tabStrip = GetTemplateChild("PART_TabStrip") as ItemsControl;
        _addTabButton = GetTemplateChild("PART_AddTabButton") as Button;
        _tabLozenge = GetTemplateChild("PART_TabLozenge") as Border;
        _contentHost = GetTemplateChild("PART_ContentHost") as ContentPresenter;

        if (_addTabButton != null)
        {
            _addTabButton.Click += OnAddTabButtonClick;
        }
    }

    private void OnAddTabButtonClick(object sender, RoutedEventArgs e)
    {
        RequestAddTab();
    }

    public void RequestAddTab()
    {
        AddTabButtonClick?.Invoke(this, EventArgs.Empty);
        AddTabButtonCommand?.Execute(null);
    }

    public void RaiseTabCloseRequested(object item)
    {
        TabItems?.Remove(item);
        TabCloseRequested?.Invoke(this, null!);
    }

    public void RaiseTabDragStarting(object item)
    {
        TabDragStarting?.Invoke(this, null!);
    }

    public void RaiseTabDropped(object item)
    {
        TabDropped?.Invoke(this, new TabViewTabDroppedEventArgs { Item = item });
    }

    public (float Left, float Right, float Stretch) StepLozenge(float targetLeft, float targetRight, float dtSeconds)
    {
        return _lozengeTravel.Step(targetLeft, targetRight, dtSeconds);
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new SeaGlass.WinUI.Automation.SeaGlassTabViewAutomationPeer(this);
}

public class TabViewTabDroppedEventArgs : EventArgs
{
    public object? Item { get; set; }
    public int DropIndex { get; set; }
}
