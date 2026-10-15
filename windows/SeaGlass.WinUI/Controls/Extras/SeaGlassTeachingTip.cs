using System;
using ICommand = global::System.Windows.Input.ICommand;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Shapes;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Educational callout bubble targeting an anchor element with beak/pointer, title, subtitle,
/// action buttons, and animated entrance spring.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_Popup", Type = typeof(Popup))]
[TemplatePart(Name = "PART_BeakPointer", Type = typeof(Microsoft.UI.Xaml.Shapes.Path))]
[TemplatePart(Name = "PART_TitlePresenter", Type = typeof(TextBlock))]
[TemplatePart(Name = "PART_SubtitlePresenter", Type = typeof(TextBlock))]
[TemplatePart(Name = "PART_HeroPresenter", Type = typeof(ContentPresenter))]
[TemplatePart(Name = "PART_ContentPresenter", Type = typeof(ContentPresenter))]
[TemplatePart(Name = "PART_ActionButton", Type = typeof(Button))]
[TemplatePart(Name = "PART_CloseButton", Type = typeof(Button))]
public class SeaGlassTeachingTip : SeaGlassSurface
{
    public static readonly DependencyProperty TargetProperty =
        DependencyProperty.Register(nameof(Target), typeof(FrameworkElement), typeof(SeaGlassTeachingTip),
            new PropertyMetadata(null));

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(SeaGlassTeachingTip),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SubtitleProperty =
        DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(SeaGlassTeachingTip),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(SeaGlassTeachingTip),
            new PropertyMetadata(false, (d, e) =>
            {
                var tt = (SeaGlassTeachingTip)d;
                if ((bool)e.NewValue) tt.Opened?.Invoke(tt, EventArgs.Empty);
                else tt.Closed?.Invoke(tt, EventArgs.Empty);
            }));

    public static readonly DependencyProperty ActionButtonContentProperty =
        DependencyProperty.Register(nameof(ActionButtonContent), typeof(object), typeof(SeaGlassTeachingTip),
            new PropertyMetadata(null));

    public static readonly DependencyProperty CloseButtonContentProperty =
        DependencyProperty.Register(nameof(CloseButtonContent), typeof(object), typeof(SeaGlassTeachingTip),
            new PropertyMetadata(null));

    public static readonly DependencyProperty PreferredPlacementProperty =
        DependencyProperty.Register(nameof(PreferredPlacement), typeof(TeachingTipPlacementMode), typeof(SeaGlassTeachingTip),
            new PropertyMetadata(TeachingTipPlacementMode.Auto));

    public static readonly DependencyProperty HeroContentProperty =
        DependencyProperty.Register(nameof(HeroContent), typeof(UIElement), typeof(SeaGlassTeachingTip),
            new PropertyMetadata(null));

    public static readonly DependencyProperty ActionButtonCommandProperty =
        DependencyProperty.Register(nameof(ActionButtonCommand), typeof(ICommand), typeof(SeaGlassTeachingTip),
            new PropertyMetadata(null));

    public static readonly DependencyProperty CloseButtonCommandProperty =
        DependencyProperty.Register(nameof(CloseButtonCommand), typeof(ICommand), typeof(SeaGlassTeachingTip),
            new PropertyMetadata(null));

    public FrameworkElement? Target
    {
        get => (FrameworkElement?)GetValue(TargetProperty);
        set => SetValue(TargetProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public object? ActionButtonContent
    {
        get => GetValue(ActionButtonContentProperty);
        set => SetValue(ActionButtonContentProperty, value);
    }

    public object? CloseButtonContent
    {
        get => GetValue(CloseButtonContentProperty);
        set => SetValue(CloseButtonContentProperty, value);
    }

    public TeachingTipPlacementMode PreferredPlacement
    {
        get => (TeachingTipPlacementMode)GetValue(PreferredPlacementProperty);
        set => SetValue(PreferredPlacementProperty, value);
    }

    public UIElement? HeroContent
    {
        get => (UIElement?)GetValue(HeroContentProperty);
        set => SetValue(HeroContentProperty, value);
    }

    public ICommand? ActionButtonCommand
    {
        get => (ICommand?)GetValue(ActionButtonCommandProperty);
        set => SetValue(ActionButtonCommandProperty, value);
    }

    public ICommand? CloseButtonCommand
    {
        get => (ICommand?)GetValue(CloseButtonCommandProperty);
        set => SetValue(CloseButtonCommandProperty, value);
    }

    public event TypedEventHandler<SeaGlassTeachingTip, EventArgs>? ActionButtonClick;
    public event TypedEventHandler<SeaGlassTeachingTip, EventArgs>? CloseButtonClick;
    public event TypedEventHandler<SeaGlassTeachingTip, EventArgs>? Closed;
    public event TypedEventHandler<SeaGlassTeachingTip, EventArgs>? Opened;

    public SeaGlassTeachingTip()
    {
        DefaultStyleKey = typeof(SeaGlassTeachingTip);
        Material = SeaGlassTheme.Current.Palette.ElevatedGlass;
    }

    public SeaGlassTeachingTip(string title = "", string subtitle = "") : this()
    {
        Title = title;
        Subtitle = subtitle;
    }

    private Grid? _root;
    private Border? _glassHost;
    private Popup? _popup;
    private Microsoft.UI.Xaml.Shapes.Path? _beakPointer;
    private TextBlock? _titlePresenter;
    private TextBlock? _subtitlePresenter;
    private ContentPresenter? _heroPresenter;
    private ContentPresenter? _contentPresenter;
    private Button? _actionButton;
    private Button? _closeButton;

    public Popup? Popup => _popup;
    public Microsoft.UI.Xaml.Shapes.Path? BeakPointer => _beakPointer;
    public TextBlock? TitlePresenter => _titlePresenter;
    public TextBlock? SubtitlePresenter => _subtitlePresenter;
    public ContentPresenter? HeroPresenter => _heroPresenter;
    public ContentPresenter? ContentPresenterChild => _contentPresenter;
    public Button? ActionButton => _actionButton;
    public Button? CloseButton => _closeButton;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        if (_actionButton != null) _actionButton.Click -= OnActionButtonClick;
        if (_closeButton != null) _closeButton.Click -= OnCloseButtonClick;

        _root = GetTemplateChild("PART_Root") as Grid;
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _popup = GetTemplateChild("PART_Popup") as Popup;
        _beakPointer = GetTemplateChild("PART_BeakPointer") as Microsoft.UI.Xaml.Shapes.Path;
        _titlePresenter = GetTemplateChild("PART_TitlePresenter") as TextBlock;
        _subtitlePresenter = GetTemplateChild("PART_SubtitlePresenter") as TextBlock;
        _heroPresenter = GetTemplateChild("PART_HeroPresenter") as ContentPresenter;
        _contentPresenter = GetTemplateChild("PART_ContentPresenter") as ContentPresenter;
        _actionButton = GetTemplateChild("PART_ActionButton") as Button;
        _closeButton = GetTemplateChild("PART_CloseButton") as Button;

        if (_actionButton != null) _actionButton.Click += OnActionButtonClick;
        if (_closeButton != null) _closeButton.Click += OnCloseButtonClick;
    }

    private void OnActionButtonClick(object sender, RoutedEventArgs e) => RequestAction();
    private void OnCloseButtonClick(object sender, RoutedEventArgs e) => RequestClose();

    public void RequestAction()
    {
        ActionButtonClick?.Invoke(this, EventArgs.Empty);
        ActionButtonCommand?.Execute(null);
    }

    public void RequestClose()
    {
        CloseButtonClick?.Invoke(this, EventArgs.Empty);
        CloseButtonCommand?.Execute(null);
        IsOpen = false;
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new SeaGlass.WinUI.Automation.SeaGlassTeachingTipAutomationPeer(this);
}
