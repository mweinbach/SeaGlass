using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Floating Liquid SeaGlass Top App Bar with navigation icon, title/subtitle, and trailing actions.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_NavigationButton", Type = typeof(Button))]
[TemplatePart(Name = "PART_TitlePresenter", Type = typeof(TextBlock))]
[TemplatePart(Name = "PART_SubtitlePresenter", Type = typeof(TextBlock))]
[TemplatePart(Name = "PART_ActionsPresenter", Type = typeof(ItemsControl))]
public class SeaGlassTopAppBar : SeaGlassSurface
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(SeaGlassTopAppBar),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SubtitleProperty =
        DependencyProperty.Register(
            nameof(Subtitle),
            typeof(string),
            typeof(SeaGlassTopAppBar),
            new PropertyMetadata(null));

    public static readonly DependencyProperty NavigationGlyphProperty =
        DependencyProperty.Register(
            nameof(NavigationGlyph),
            typeof(SeaGlassGlyph?),
            typeof(SeaGlassTopAppBar),
            new PropertyMetadata(SeaGlassGlyph.Back));

    public static readonly DependencyProperty ActionGlyphProperty =
        DependencyProperty.Register(
            nameof(ActionGlyph),
            typeof(SeaGlassGlyph?),
            typeof(SeaGlassTopAppBar),
            new PropertyMetadata(null));

    public static readonly DependencyProperty IsScrolledOverProperty =
        DependencyProperty.Register(
            nameof(IsScrolledOver),
            typeof(bool),
            typeof(SeaGlassTopAppBar),
            new PropertyMetadata(false));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Subtitle
    {
        get => (string?)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public SeaGlassGlyph? NavigationGlyph
    {
        get => (SeaGlassGlyph?)GetValue(NavigationGlyphProperty);
        set => SetValue(NavigationGlyphProperty, value);
    }

    public SeaGlassGlyph? ActionGlyph
    {
        get => (SeaGlassGlyph?)GetValue(ActionGlyphProperty);
        set => SetValue(ActionGlyphProperty, value);
    }

    public bool IsScrolledOver
    {
        get => (bool)GetValue(IsScrolledOverProperty);
        set => SetValue(IsScrolledOverProperty, value);
    }

    public Action? OnNavigationClick { get; set; }
    public IReadOnlyList<SeaGlassIconButton> Actions { get; set; } = Array.Empty<SeaGlassIconButton>();

    public event EventHandler? BackRequested;
    public event EventHandler? ActionRequested;

    public SeaGlassTopAppBar()
    {
        DefaultStyleKey = typeof(SeaGlassTopAppBar);
        ContentRole = SeaGlassContentRole.NavigationChrome;
        SizePx = new Vector2(560f, 60f);
        BasePaddingPx = new Vector4(12f, 8f, 12f, 8f);
    }

    public SeaGlassTopAppBar(
        string title,
        SeaGlassStageState? stageState = null,
        string? subtitle = null,
        SeaGlassGlyph? navigationGlyph = SeaGlassGlyph.Back,
        Action? onNavigationClick = null,
        SeaGlassMaterial? material = null,
        IReadOnlyList<SeaGlassIconButton>? actions = null)
        : base(
            stageState: stageState,
            material: (material ?? SeaGlassTheme.Current.Palette.SurfaceGlass) with
            {
                CornerRadiusDp = 30f,
                ThicknessDp = 18f,
                RimChalkiness = 0.86f
            },
            contentRole: SeaGlassContentRole.NavigationChrome)
    {
        DefaultStyleKey = typeof(SeaGlassTopAppBar);
        Title = title;
        Subtitle = subtitle;
        NavigationGlyph = navigationGlyph;
        OnNavigationClick = onNavigationClick;
        Actions = actions ?? Array.Empty<SeaGlassIconButton>();
        SizePx = new Vector2(560f, 60f);
        BasePaddingPx = new Vector4(12f, 8f, 12f, 8f);
    }

    private Grid? _root;
    private Border? _glassHost;
    private Button? _navigationButton;
    private TextBlock? _titlePresenter;
    private TextBlock? _subtitlePresenter;
    private ItemsControl? _actionsPresenter;

    public Button? NavigationButton => _navigationButton;
    public TextBlock? TitlePresenter => _titlePresenter;
    public TextBlock? SubtitlePresenter => _subtitlePresenter;
    public ItemsControl? ActionsPresenter => _actionsPresenter;
    public Border? GlassHost => _glassHost;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        if (_navigationButton != null)
        {
            _navigationButton.Click -= OnNavigationButtonClick;
        }

        _root = GetTemplateChild("PART_Root") as Grid;
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _navigationButton = GetTemplateChild("PART_NavigationButton") as Button;
        _titlePresenter = GetTemplateChild("PART_TitlePresenter") as TextBlock;
        _subtitlePresenter = GetTemplateChild("PART_SubtitlePresenter") as TextBlock;
        _actionsPresenter = GetTemplateChild("PART_ActionsPresenter") as ItemsControl;

        if (_navigationButton != null)
        {
            _navigationButton.Click += OnNavigationButtonClick;
        }
    }

    private void OnNavigationButtonClick(object sender, RoutedEventArgs e)
    {
        OnBack();
    }

    public void OnBack()
    {
        OnNavigationClick?.Invoke();
        BackRequested?.Invoke(this, EventArgs.Empty);
    }

    public void OnAction()
    {
        ActionRequested?.Invoke(this, EventArgs.Empty);
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new SeaGlass.WinUI.Automation.SeaGlassTopAppBarAutomationPeer(this);
}
