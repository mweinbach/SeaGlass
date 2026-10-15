using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Tumbled squircle container card with title, subtitle, header template, and elevation depth.
/// </summary>
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_ContentPresenter", Type = typeof(ContentPresenter))]
[TemplatePart(Name = "PART_TitlePresenter", Type = typeof(TextBlock))]
[TemplatePart(Name = "PART_SubtitlePresenter", Type = typeof(TextBlock))]
public class SeaGlassCard : SeaGlassSurface
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(SeaGlassCard),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SubtitleProperty =
        DependencyProperty.Register(
            nameof(Subtitle),
            typeof(string),
            typeof(SeaGlassCard),
            new PropertyMetadata(null));

    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(object),
            typeof(SeaGlassCard),
            new PropertyMetadata(null));

    public static readonly DependencyProperty HeaderTemplateProperty =
        DependencyProperty.Register(
            nameof(HeaderTemplate),
            typeof(DataTemplate),
            typeof(SeaGlassCard),
            new PropertyMetadata(null));

    public static readonly DependencyProperty ElevationDepthDpProperty =
        DependencyProperty.Register(
            nameof(ElevationDepthDp),
            typeof(double),
            typeof(SeaGlassCard),
            new PropertyMetadata(16.0));

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

    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public DataTemplate? HeaderTemplate
    {
        get => (DataTemplate?)GetValue(HeaderTemplateProperty);
        set => SetValue(HeaderTemplateProperty, value);
    }

    public double ElevationDepthDp
    {
        get => (double)GetValue(ElevationDepthDpProperty);
        set => SetValue(ElevationDepthDpProperty, value);
    }

    public SeaGlassCard()
    {
        DefaultStyleKey = typeof(SeaGlassCard);
    }

    public SeaGlassCard(
        string title = "",
        string? subtitle = null,
        SeaGlassStageState? stage = null,
        SeaGlassMaterial? material = null)
        : base(stage, material)
    {
        DefaultStyleKey = typeof(SeaGlassCard);
        Title = title;
        Subtitle = subtitle;
    }

    public static SeaGlassCard Create(string title = "", string? subtitle = null) =>
        new SeaGlassCard(title, subtitle);

    private TextBlock? _titlePresenter;
    private TextBlock? _subtitlePresenter;

    public TextBlock? TitlePresenter => _titlePresenter;
    public TextBlock? SubtitlePresenter => _subtitlePresenter;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _titlePresenter = GetTemplateChild("PART_TitlePresenter") as TextBlock;
        _subtitlePresenter = GetTemplateChild("PART_SubtitlePresenter") as TextBlock;

        if (_titlePresenter != null && !string.IsNullOrEmpty(Title)) _titlePresenter.Text = Title;
        if (_subtitlePresenter != null && !string.IsNullOrEmpty(Subtitle)) _subtitlePresenter.Text = Subtitle;
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new SeaGlass.WinUI.Automation.SeaGlassCardAutomationPeer(this);
}
