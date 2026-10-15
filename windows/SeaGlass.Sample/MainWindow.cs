using Microsoft.UI.Xaml.Automation;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Sample.Catalog;
using SeaGlass.Sample.Common;
using SeaGlass.Sample.Pages;

namespace SeaGlass.Sample;

/// <summary>
/// Main showcase window for SeaGlass WinUI 3 hosting the full 58-control gallery hierarchy,
/// category navigation, search suggestions, and responsive frame presenter.
/// </summary>
public sealed class MainWindow : Window
{
    private readonly Grid _rootGrid;
    private readonly NavigationView _navView;
    private readonly Frame _contentFrame;
    private readonly AutoSuggestBox _searchBox;
    private readonly Dictionary<NavigationViewItem, object> _itemPageMap = new();

    public MainWindow()
    {
        Title = "SeaGlass Optical Lab & Expressive Showcase (WinUI 3)";

        _rootGrid = new Grid();
        _rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        _contentFrame = new Frame();

        _searchBox = new AutoSuggestBox
        {
            PlaceholderText = "Search controls (e.g. Button, Morph, Slider)...",
            QueryIcon = new SymbolIcon(Symbol.Find),
            Width = 240
        };
        _searchBox.TextChanged += OnSearchTextChanged;
        _searchBox.SuggestionChosen += OnSearchSuggestionChosen;

        _navView = new NavigationView
        {
            PaneTitle = "SeaGlass Gallery",
            OpenPaneLength = 280,
            IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed,
            IsSettingsVisible = true,
            AutoSuggestBox = _searchBox,
            Content = _contentFrame
        };

        PopulateNavigationItems();

        _navView.SelectionChanged += OnNavSelectionChanged;

                AutomationProperties.SetAutomationId(_navView, "GalleryNavView");
        AutomationProperties.SetAutomationId(_contentFrame, "GalleryContentFrame");
        AutomationProperties.SetAutomationId(_searchBox, "GallerySearchBox");

        var navTrigger = new TextBox
        {
            Name = "UiaNavigationTrigger",
            Width = 1,
            Height = 1,
            Opacity = 0.01,
            IsTabStop = false
        };
        AutomationProperties.SetAutomationId(navTrigger, "UiaNavigationTrigger");
        AutomationProperties.SetName(navTrigger, "UiaNavigationTrigger");
        navTrigger.TextChanged += (s, e) =>
        {
            string target = navTrigger.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(target)) return;
            if (string.Equals(target, "Settings", StringComparison.OrdinalIgnoreCase))
            {
                _contentFrame.Content = new SettingsPage();
                return;
            }
            if (string.Equals(target, "Home", StringComparison.OrdinalIgnoreCase))
            {
                _contentFrame.Content = new HomePage();
                return;
            }
            var desc = ControlShowcaseCatalog.FindByName(target);
            if (desc != null)
            {
                NavigateTo(desc);
            }
        };
        _rootGrid.Children.Add(navTrigger);
        _rootGrid.Children.Add(_navView);
        Content = _rootGrid;

        Activated += OnWindowActivated;
    }

    private bool _initialNavDone;

    private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
    {
        if (!_initialNavDone)
        {
            _initialNavDone = true;
            if (!string.IsNullOrEmpty(Program.InitialControlPage))
            {
                if (string.Equals(Program.InitialControlPage, "Settings", StringComparison.OrdinalIgnoreCase))
                {
                    _contentFrame.Content = new SettingsPage();
                    return;
                }
                var descriptor = ControlShowcaseCatalog.FindByName(Program.InitialControlPage);
                if (descriptor != null)
                {
                    NavigateTo(descriptor);
                    return;
                }
            }
            _contentFrame.Content = new HomePage();
        }
    }

    public Frame ContentFrame => _contentFrame;

    public NavigationView NavView => _navView;

    public void NavigateTo(object target)
    {
        if (target is ControlShowcaseDescriptor desc)
        {
            _contentFrame.Content = new ControlShowcasePage(desc);
        }
        else if (target is Type pageType)
        {
            _contentFrame.Content = Activator.CreateInstance(pageType);
        }
    }

    public void SetCaptureMode(ElementTheme theme, bool isHighContrast = false, int scalePercent = 100)
    {
        var targetTheme = isHighContrast ? ElementTheme.Dark : theme;
        _rootGrid.RequestedTheme = targetTheme;
        _navView.RequestedTheme = targetTheme;
        _contentFrame.RequestedTheme = targetTheme;

        Microsoft.UI.Xaml.Media.SolidColorBrush rootBg;
        Microsoft.UI.Xaml.Media.SolidColorBrush navBg;

        if (isHighContrast)
        {
            rootBg = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0));
            navBg = rootBg;
        }
        else if (theme == ElementTheme.Dark)
        {
            rootBg = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 24, 28, 34));
            navBg = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 18, 22, 28));
        }
        else
        {
            rootBg = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 248, 250, 252));
            navBg = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 240, 244, 248));
        }

        _rootGrid.Background = rootBg;
        _navView.Background = navBg;
        _contentFrame.Background = rootBg;

        _navView.Resources["NavigationViewDefaultPaneBackground"] = navBg;
        _navView.Resources["NavigationViewExpandedPaneBackground"] = navBg;
        _navView.Resources["NavigationViewTopPaneBackground"] = navBg;
        _rootGrid.Resources["NavigationViewDefaultPaneBackground"] = navBg;
        _rootGrid.Resources["NavigationViewExpandedPaneBackground"] = navBg;
        _rootGrid.Resources["NavigationViewTopPaneBackground"] = navBg;

        string themeKey = isHighContrast ? "HighContrast" : (theme == ElementTheme.Dark ? "Dark" : "Light");
        if (!_navView.Resources.ThemeDictionaries.TryGetValue(themeKey, out var dictObj) || dictObj is not ResourceDictionary themeDict)
        {
            themeDict = new ResourceDictionary();
            _navView.Resources.ThemeDictionaries[themeKey] = themeDict;
        }
        themeDict["NavigationViewDefaultPaneBackground"] = navBg;
        themeDict["NavigationViewExpandedPaneBackground"] = navBg;
        themeDict["NavigationViewTopPaneBackground"] = navBg;

        ApplyNavThemeRecursively(_navView, targetTheme, navBg);

        foreach (var item in _navView.MenuItems)
        {
            if (item is FrameworkElement fe) fe.RequestedTheme = targetTheme;
        }
        foreach (var item in _navView.FooterMenuItems)
        {
            if (item is FrameworkElement fe) fe.RequestedTheme = targetTheme;
        }

        if (scalePercent != 100)
        {
            double factor = scalePercent / 100.0;
            _rootGrid.RenderTransform = new Microsoft.UI.Xaml.Media.ScaleTransform
            {
                ScaleX = factor,
                ScaleY = factor
            };
        }
        else
        {
            _rootGrid.RenderTransform = null;
        }

        SeaGlass.WinUI.Rendering.SeaGlassTieredRenderEngine.Instance.UpdateSystemSettings(
            advancedEffectsEnabled: true,
            energySaverActive: false,
            highContrast: isHighContrast);

        IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        if (appWindow != null)
        {
            appWindow.MoveAndResize(new Windows.Graphics.RectInt32(0, 0, 1440, 900));
        }

        if (_contentFrame.Content is ControlShowcasePage csp)
        {
            csp.ApplyTheme(theme, isHighContrast);
        }
    }

    private static void ApplyNavThemeRecursively(DependencyObject root, ElementTheme theme, Microsoft.UI.Xaml.Media.Brush paneBg)
    {
        if (root is FrameworkElement fe)
        {
            fe.RequestedTheme = theme;
        }
        if (root is SplitView sv)
        {
            sv.PaneBackground = paneBg;
            sv.RequestedTheme = theme;
        }
        int count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            ApplyNavThemeRecursively(Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, i), theme, paneBg);
        }
    }

    private void PopulateNavigationItems()
    {
        // 1. Home Item
        var homeItem = new NavigationViewItem
        {
            Content = "Home & Overview",
            Icon = new SymbolIcon(Symbol.Home),
            Tag = typeof(HomePage)
        };
        _navView.MenuItems.Add(homeItem);
        _navView.SelectedItem = homeItem;

        // 2. Add Categorized Control Items
        var grouped = ControlShowcaseCatalog.AllDescriptors
            .GroupBy(d => d.Category)
            .ToList();

        foreach (var categoryGroup in grouped)
        {
            var categoryInfo = CategoryDefinitions.AllCategories.FirstOrDefault(c => c.DisplayName == categoryGroup.Key);
            string glyph = categoryInfo?.Glyph ?? "📁";

            var categoryHeader = new NavigationViewItemHeader
            {
                Content = $"{glyph} {categoryGroup.Key}"
            };
            _navView.MenuItems.Add(categoryHeader);

            foreach (var descriptor in categoryGroup)
            {
                var item = new NavigationViewItem
                {
                    Content = descriptor.Name,
                    Icon = new SymbolIcon(Symbol.Document),
                    Tag = descriptor
                };
                _navView.MenuItems.Add(item);
                _itemPageMap[item] = descriptor;
            }
        }
    }

    private void OnNavSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            _contentFrame.Content = new SettingsPage();
            return;
        }

        if (args.SelectedItem is NavigationViewItem item)
        {
            if (item.Tag is Type pageType)
            {
                _contentFrame.Content = Activator.CreateInstance(pageType);
            }
            else if (item.Tag is ControlShowcaseDescriptor descriptor)
            {
                _contentFrame.Content = new ControlShowcasePage(descriptor);
            }
        }
    }

    private void OnSearchTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            string query = sender.Text.Trim();
            if (string.IsNullOrEmpty(query))
            {
                sender.ItemsSource = null;
                return;
            }

            var matches = ControlShowcaseCatalog.AllDescriptors
                .Where(d => d.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                            d.Category.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                            d.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Select(d => d.Name)
                .Take(10)
                .ToList();

            sender.ItemsSource = matches;
        }
    }

    private void OnSearchSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is string controlName)
        {
            var descriptor = ControlShowcaseCatalog.FindByName(controlName);
            if (descriptor != null)
            {
                _contentFrame.Content = new ControlShowcasePage(descriptor);

                // Highlight corresponding item if present
                var navItem = _itemPageMap.FirstOrDefault(kvp => Equals(kvp.Value, descriptor)).Key;
                if (navItem != null)
                {
                    _navView.SelectedItem = navItem;
                }
            }
        }
    }
}
