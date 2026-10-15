using System;
using ICommand = global::System.Windows.Input.ICommand;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Custom Windows 11 XAML title bar with app icon, title, drag regions, and search integration.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_IconPresenter", Type = typeof(Image))]
[TemplatePart(Name = "PART_TitlePresenter", Type = typeof(TextBlock))]
[TemplatePart(Name = "PART_DragRegion", Type = typeof(Border))]
[TemplatePart(Name = "PART_SearchHost", Type = typeof(ContentPresenter))]
[TemplatePart(Name = "PART_CaptionButtonsPlaceholder", Type = typeof(Border))]
public class SeaGlassTitleBar : SeaGlassSurface
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(SeaGlassTitleBar),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(ImageSource), typeof(SeaGlassTitleBar),
            new PropertyMetadata(null));

    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(SeaGlassTitleBar),
            new PropertyMetadata(true));

    public static readonly DependencyProperty IsCustomizationSupportedProperty =
        DependencyProperty.Register(nameof(IsCustomizationSupported), typeof(bool), typeof(SeaGlassTitleBar),
            new PropertyMetadata(true));

    public static readonly DependencyProperty HeaderContentProperty =
        DependencyProperty.Register(nameof(HeaderContent), typeof(object), typeof(SeaGlassTitleBar),
            new PropertyMetadata(null));

    public static readonly DependencyProperty FooterContentProperty =
        DependencyProperty.Register(nameof(FooterContent), typeof(object), typeof(SeaGlassTitleBar),
            new PropertyMetadata(null));

    public static readonly DependencyProperty SearchContentProperty =
        DependencyProperty.Register(nameof(SearchContent), typeof(object), typeof(SeaGlassTitleBar),
            new PropertyMetadata(null));

    public static readonly DependencyProperty ExtendsContentIntoTitleBarProperty =
        DependencyProperty.Register(nameof(ExtendsContentIntoTitleBar), typeof(bool), typeof(SeaGlassTitleBar),
            new PropertyMetadata(true));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public ImageSource? Icon
    {
        get => (ImageSource?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public bool IsCustomizationSupported
    {
        get => (bool)GetValue(IsCustomizationSupportedProperty);
        private set => SetValue(IsCustomizationSupportedProperty, value);
    }

    public object? HeaderContent
    {
        get => GetValue(HeaderContentProperty);
        set => SetValue(HeaderContentProperty, value);
    }

    public object? FooterContent
    {
        get => GetValue(FooterContentProperty);
        set => SetValue(FooterContentProperty, value);
    }

    public object? SearchContent
    {
        get => GetValue(SearchContentProperty);
        set => SetValue(SearchContentProperty, value);
    }

    public bool ExtendsContentIntoTitleBar
    {
        get => (bool)GetValue(ExtendsContentIntoTitleBarProperty);
        set => SetValue(ExtendsContentIntoTitleBarProperty, value);
    }

    public event RoutedEventHandler? BackRequested;

    public SeaGlassTitleBar()
    {
        DefaultStyleKey = typeof(SeaGlassTitleBar);
        Height = 48.0;
        Material = SeaGlassTheme.Current.Palette.SurfaceGlass with { Coarseness = 0f, ThicknessDp = 12f };
    }

    public SeaGlassTitleBar(string title = "") : this()
    {
        Title = title;
    }

    private Grid? _root;
    private Border? _glassHost;
    private Image? _iconPresenter;
    private TextBlock? _titlePresenter;
    private Border? _dragRegion;
    private ContentPresenter? _searchHost;
    private Border? _captionButtonsPlaceholder;
    private UIElement? _customDragRegion;

    public Border? GlassHost => _glassHost;
    public TextBlock? TitlePresenter => _titlePresenter;
    public Image? IconPresenter => _iconPresenter;
    public Border? DragRegion => _dragRegion;
    public ContentPresenter? SearchHost => _searchHost;
    public Border? CaptionButtonsPlaceholder => _captionButtonsPlaceholder;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _root = GetTemplateChild("PART_Root") as Grid;
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _iconPresenter = GetTemplateChild("PART_IconPresenter") as Image;
        _titlePresenter = GetTemplateChild("PART_TitlePresenter") as TextBlock;
        _dragRegion = GetTemplateChild("PART_DragRegion") as Border;
        _searchHost = GetTemplateChild("PART_SearchHost") as ContentPresenter;
        _captionButtonsPlaceholder = GetTemplateChild("PART_CaptionButtonsPlaceholder") as Border;
    }

    public void SetDragRegion(UIElement element)
    {
        _customDragRegion = element;
    }

    public void BindToAppWindow(Microsoft.UI.Windowing.AppWindow appWindow)
    {
        if (appWindow is not null && appWindow.TitleBar is not null)
        {
            appWindow.TitleBar.ExtendsContentIntoTitleBar = ExtendsContentIntoTitleBar;
        }
    }

    public void RaiseBackRequested()
    {
        BackRequested?.Invoke(this, new RoutedEventArgs());
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new SeaGlass.WinUI.Automation.SeaGlassTitleBarAutomationPeer(this);
}
