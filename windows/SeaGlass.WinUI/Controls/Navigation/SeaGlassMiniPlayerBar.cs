using System;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Mini player bar with track title, artist, progress, and play/next controls.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_ArtworkHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_TitlePresenter", Type = typeof(TextBlock))]
[TemplatePart(Name = "PART_SubtitlePresenter", Type = typeof(TextBlock))]
[TemplatePart(Name = "PART_PlayPauseButton", Type = typeof(Button))]
[TemplatePart(Name = "PART_NextButton", Type = typeof(Button))]
public class SeaGlassMiniPlayerBar : SeaGlassSurface
{
    public static readonly DependencyProperty TrackTitleProperty =
        DependencyProperty.Register(
            nameof(TrackTitle),
            typeof(string),
            typeof(SeaGlassMiniPlayerBar),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ArtistProperty =
        DependencyProperty.Register(
            nameof(Artist),
            typeof(string),
            typeof(SeaGlassMiniPlayerBar),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IsPlayingProperty =
        DependencyProperty.Register(
            nameof(IsPlaying),
            typeof(bool),
            typeof(SeaGlassMiniPlayerBar),
            new PropertyMetadata(false));

    public static readonly DependencyProperty ProgressProperty =
        DependencyProperty.Register(
            nameof(Progress),
            typeof(double),
            typeof(SeaGlassMiniPlayerBar),
            new PropertyMetadata(0.0));

    public string TrackTitle
    {
        get => (string)GetValue(TrackTitleProperty);
        set => SetValue(TrackTitleProperty, value);
    }

    public string Artist
    {
        get => (string)GetValue(ArtistProperty);
        set => SetValue(ArtistProperty, value);
    }

    public bool IsPlaying
    {
        get => (bool)GetValue(IsPlayingProperty);
        set => SetValue(IsPlayingProperty, value);
    }

    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public string Title
    {
        get => TrackTitle;
        set => TrackTitle = value;
    }

    public string Subtitle
    {
        get => Artist;
        set => Artist = value;
    }

    public Action? OnTogglePlay { get; set; }
    public Action? OnNextTrack { get; set; }

    public event EventHandler? PlayPauseClicked;
    public event EventHandler? NextClicked;

    public SeaGlassMiniPlayerBar()
    {
        DefaultStyleKey = typeof(SeaGlassMiniPlayerBar);
        IsTabStop = true;
        ContentRole = SeaGlassContentRole.NavigationChrome;
        SizePx = new Vector2(460f, 56f);
        BasePaddingPx = new Vector4(12f, 7f, 12f, 7f);
    }

    public SeaGlassMiniPlayerBar(
        string title,
        string subtitle,
        bool isPlaying,
        Action? onTogglePlay = null,
        Action? onNextTrack = null,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? material = null)
        : base(
            stageState: stageState,
            material: (material ?? SeaGlassTheme.Current.Palette.SurfaceGlass) with
            {
                CornerRadiusDp = 28f,
                ThicknessDp = 18f,
                RimChalkiness = 0.88f
            },
            contentRole: SeaGlassContentRole.NavigationChrome,
            onClick: onTogglePlay)
    {
        DefaultStyleKey = typeof(SeaGlassMiniPlayerBar);
        TrackTitle = title;
        Artist = subtitle;
        IsPlaying = isPlaying;
        OnTogglePlay = onTogglePlay;
        OnNextTrack = onNextTrack;
        SizePx = new Vector2(460f, 56f);
        BasePaddingPx = new Vector4(12f, 7f, 12f, 7f);
    }

    private Grid? _root;
    private Border? _glassHost;
    private Border? _artworkHost;
    private TextBlock? _titlePresenter;
    private TextBlock? _subtitlePresenter;
    private Button? _playPauseButton;
    private Button? _nextButton;

    public Border? ArtworkHost => _artworkHost;
    public TextBlock? TitlePresenter => _titlePresenter;
    public TextBlock? SubtitlePresenter => _subtitlePresenter;
    public Button? PlayPauseButton => _playPauseButton;
    public Button? NextButton => _nextButton;
    public Border? GlassHost => _glassHost;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        if (_playPauseButton != null) _playPauseButton.Click -= OnPlayPauseButtonClick;
        if (_nextButton != null) _nextButton.Click -= OnNextButtonClick;

        _root = GetTemplateChild("PART_Root") as Grid;
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _artworkHost = GetTemplateChild("PART_ArtworkHost") as Border;
        _titlePresenter = GetTemplateChild("PART_TitlePresenter") as TextBlock;
        _subtitlePresenter = GetTemplateChild("PART_SubtitlePresenter") as TextBlock;
        _playPauseButton = GetTemplateChild("PART_PlayPauseButton") as Button;
        _nextButton = GetTemplateChild("PART_NextButton") as Button;

        if (_playPauseButton != null) _playPauseButton.Click += OnPlayPauseButtonClick;
        if (_nextButton != null) _nextButton.Click += OnNextButtonClick;
    }

    private void OnPlayPauseButtonClick(object sender, RoutedEventArgs e) => TogglePlay();
    private void OnNextButtonClick(object sender, RoutedEventArgs e) => Next();

    public void TogglePlay()
    {
        IsPlaying = !IsPlaying;
        OnTogglePlay?.Invoke();
        PlayPauseClicked?.Invoke(this, EventArgs.Empty);
    }

    public void Next()
    {
        OnNextTrack?.Invoke();
        NextClicked?.Invoke(this, EventArgs.Empty);
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new SeaGlass.WinUI.Automation.SeaGlassMiniPlayerBarAutomationPeer(this);
}
