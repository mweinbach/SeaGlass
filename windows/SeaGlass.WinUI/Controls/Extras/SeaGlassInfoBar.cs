using System;
using ICommand = global::System.Windows.Input.ICommand;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Contextual status banner with severity states (Informational, Success, Warning, Error)
/// and Oklab tonal styling.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_SeverityIcon", Type = typeof(ContentPresenter))]
[TemplatePart(Name = "PART_TitlePresenter", Type = typeof(TextBlock))]
[TemplatePart(Name = "PART_MessagePresenter", Type = typeof(TextBlock))]
[TemplatePart(Name = "PART_ActionButtonHost", Type = typeof(ContentPresenter))]
[TemplatePart(Name = "PART_CloseButton", Type = typeof(Button))]
public class SeaGlassInfoBar : SeaGlassSurface
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(SeaGlassInfoBar),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(SeaGlassInfoBar),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SeverityProperty =
        DependencyProperty.Register(nameof(Severity), typeof(InfoBarSeverity), typeof(SeaGlassInfoBar),
            new PropertyMetadata(InfoBarSeverity.Informational, OnSeverityChanged));

    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(SeaGlassInfoBar),
            new PropertyMetadata(true, (d, e) =>
            {
                var ib = (SeaGlassInfoBar)d;
                if ((bool)e.NewValue) ib.Opened?.Invoke(ib, EventArgs.Empty);
                else ib.Closed?.Invoke(ib, EventArgs.Empty);
            }));

    public static readonly DependencyProperty IsClosableProperty =
        DependencyProperty.Register(nameof(IsClosable), typeof(bool), typeof(SeaGlassInfoBar),
            new PropertyMetadata(true));

    public static readonly DependencyProperty ActionButtonProperty =
        DependencyProperty.Register(nameof(ActionButton), typeof(ButtonBase), typeof(SeaGlassInfoBar),
            new PropertyMetadata(null));

    public static readonly DependencyProperty IconSourceProperty =
        DependencyProperty.Register(nameof(IconSource), typeof(IconSource), typeof(SeaGlassInfoBar),
            new PropertyMetadata(null));

    public static readonly DependencyProperty CloseButtonCommandProperty =
        DependencyProperty.Register(nameof(CloseButtonCommand), typeof(ICommand), typeof(SeaGlassInfoBar),
            new PropertyMetadata(null));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public InfoBarSeverity Severity
    {
        get => (InfoBarSeverity)GetValue(SeverityProperty);
        set => SetValue(SeverityProperty, value);
    }

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public bool IsClosable
    {
        get => (bool)GetValue(IsClosableProperty);
        set => SetValue(IsClosableProperty, value);
    }

    public ButtonBase? ActionButton
    {
        get => (ButtonBase?)GetValue(ActionButtonProperty);
        set => SetValue(ActionButtonProperty, value);
    }

    public IconSource? IconSource
    {
        get => (IconSource?)GetValue(IconSourceProperty);
        set => SetValue(IconSourceProperty, value);
    }

    public ICommand? CloseButtonCommand
    {
        get => (ICommand?)GetValue(CloseButtonCommandProperty);
        set => SetValue(CloseButtonCommandProperty, value);
    }

    public event TypedEventHandler<SeaGlassInfoBar, EventArgs>? CloseButtonClick;
    public event TypedEventHandler<SeaGlassInfoBar, EventArgs>? Closed;
    public event TypedEventHandler<SeaGlassInfoBar, EventArgs>? Opened;

    public SeaGlassInfoBar()
    {
        DefaultStyleKey = typeof(SeaGlassInfoBar);
    }

    public SeaGlassInfoBar(string title = "", string message = "", InfoBarSeverity severity = InfoBarSeverity.Informational) : this()
    {
        Title = title;
        Message = message;
        Severity = severity;
    }

    private Grid? _root;
    private Border? _glassHost;
    private ContentPresenter? _severityIcon;
    private TextBlock? _titlePresenter;
    private TextBlock? _messagePresenter;
    private ContentPresenter? _actionButtonHost;
    private Button? _closeButton;

    public ContentPresenter? SeverityIcon => _severityIcon;
    public TextBlock? TitlePresenter => _titlePresenter;
    public TextBlock? MessagePresenter => _messagePresenter;
    public ContentPresenter? ActionButtonHost => _actionButtonHost;
    public Button? CloseButton => _closeButton;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        if (_closeButton != null)
        {
            _closeButton.Click -= OnCloseButtonClick;
        }

        _root = GetTemplateChild("PART_Root") as Grid;
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _severityIcon = GetTemplateChild("PART_SeverityIcon") as ContentPresenter;
        _titlePresenter = GetTemplateChild("PART_TitlePresenter") as TextBlock;
        _messagePresenter = GetTemplateChild("PART_MessagePresenter") as TextBlock;
        _actionButtonHost = GetTemplateChild("PART_ActionButtonHost") as ContentPresenter;
        _closeButton = GetTemplateChild("PART_CloseButton") as Button;

        if (_closeButton != null)
        {
            _closeButton.Click += OnCloseButtonClick;
        }
    }

    private void OnCloseButtonClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    public void Close()
    {
        CloseButtonClick?.Invoke(this, EventArgs.Empty);
        CloseButtonCommand?.Execute(null);
        IsOpen = false;
        Visibility = Visibility.Collapsed;
    }

    private static void OnSeverityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ib = (SeaGlassInfoBar)d;
        var severity = (InfoBarSeverity)e.NewValue;
        ib.Tone = severity switch
        {
            InfoBarSeverity.Success => SeaGlassTone.KelpForest,
            InfoBarSeverity.Warning => SeaGlassTone.BeachAmber,
            InfoBarSeverity.Error => SeaGlassTone.RoseQuartzShell,
            _ => SeaGlassTone.CobaltTide
        };
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new SeaGlass.WinUI.Automation.SeaGlassInfoBarAutomationPeer(this);
}
