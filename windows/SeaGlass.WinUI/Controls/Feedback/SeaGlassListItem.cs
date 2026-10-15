using System;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Expressive Frosted SeaGlass List / Setting Row with leading icon, headline, and supporting subtitle.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_LeadingHost", Type = typeof(ContentPresenter))]
[TemplatePart(Name = "PART_HeadlinePresenter", Type = typeof(TextBlock))]
[TemplatePart(Name = "PART_SupportingPresenter", Type = typeof(TextBlock))]
[TemplatePart(Name = "PART_TrailingHost", Type = typeof(ContentPresenter))]
public class SeaGlassListItem : SeaGlassSurface
{
    public static readonly DependencyProperty HeadlineTextProperty =
        DependencyProperty.Register(
            nameof(HeadlineText),
            typeof(string),
            typeof(SeaGlassListItem),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty PrimaryTextProperty =
        DependencyProperty.Register(
            nameof(PrimaryText),
            typeof(string),
            typeof(SeaGlassListItem),
            new PropertyMetadata(string.Empty, OnPrimaryTextChanged));

    public static readonly DependencyProperty SupportingTextProperty =
        DependencyProperty.Register(
            nameof(SupportingText),
            typeof(string),
            typeof(SeaGlassListItem),
            new PropertyMetadata(null));

    public static readonly DependencyProperty SecondaryTextProperty =
        DependencyProperty.Register(
            nameof(SecondaryText),
            typeof(string),
            typeof(SeaGlassListItem),
            new PropertyMetadata(null, OnSecondaryTextChanged));

    public static readonly DependencyProperty LeadingGlyphProperty =
        DependencyProperty.Register(
            nameof(LeadingGlyph),
            typeof(SeaGlassGlyph?),
            typeof(SeaGlassListItem),
            new PropertyMetadata(null));

    public static readonly DependencyProperty TrailingGlyphProperty =
        DependencyProperty.Register(
            nameof(TrailingGlyph),
            typeof(SeaGlassGlyph?),
            typeof(SeaGlassListItem),
            new PropertyMetadata(null));

    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register(
            nameof(IsSelected),
            typeof(bool),
            typeof(SeaGlassListItem),
            new PropertyMetadata(false));

    public string HeadlineText
    {
        get => (string)GetValue(HeadlineTextProperty);
        set => SetValue(HeadlineTextProperty, value);
    }

    public string PrimaryText
    {
        get => HeadlineText;
        set => HeadlineText = value;
    }

    public string? SupportingText
    {
        get => (string?)GetValue(SupportingTextProperty);
        set => SetValue(SupportingTextProperty, value);
    }

    public string? SecondaryText
    {
        get => SupportingText;
        set => SupportingText = value;
    }

    public SeaGlassGlyph? LeadingGlyph
    {
        get => (SeaGlassGlyph?)GetValue(LeadingGlyphProperty);
        set => SetValue(LeadingGlyphProperty, value);
    }

    public SeaGlassGlyph? TrailingGlyph
    {
        get => (SeaGlassGlyph?)GetValue(TrailingGlyphProperty);
        set => SetValue(TrailingGlyphProperty, value);
    }

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public SeaGlassListItem()
    {
        DefaultStyleKey = typeof(SeaGlassListItem);
        IsTabStop = true;
        SizePx = new Vector2(420f, 64f);
        BasePaddingPx = new Vector4(16f, 12f, 16f, 12f);
    }

    public SeaGlassListItem(
        string headlineText,
        SeaGlassStageState? stageState = null,
        string? supportingText = null,
        SeaGlassGlyph? leadingGlyph = null,
        Action? onClick = null,
        SeaGlassMaterial? material = null)
        : base(
            stageState: stageState,
            material: (material ?? SeaGlassTheme.Current.Palette.SurfaceGlass) with
            {
                CornerRadiusDp = 22f,
                ThicknessDp = 14f
            },
            onClick: onClick)
    {
        DefaultStyleKey = typeof(SeaGlassListItem);
        HeadlineText = headlineText;
        PrimaryText = headlineText;
        SupportingText = supportingText;
        SecondaryText = supportingText;
        LeadingGlyph = leadingGlyph;
        SizePx = new Vector2(420f, 64f);
        BasePaddingPx = new Vector4(16f, 12f, 16f, 12f);
    }

    private static void OnPrimaryTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassListItem item && e.NewValue is string s)
        {
            item.HeadlineText = s;
        }
    }

    private static void OnSecondaryTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassListItem item && e.NewValue is string s)
        {
            item.SupportingText = s;
        }
    }

    private Grid? _root;
    private Border? _glassHost;
    private ContentPresenter? _leadingHost;
    private TextBlock? _headlinePresenter;
    private TextBlock? _supportingPresenter;
    private ContentPresenter? _trailingHost;

    public ContentPresenter? LeadingHost => _leadingHost;
    public TextBlock? HeadlinePresenter => _headlinePresenter;
    public TextBlock? SupportingPresenter => _supportingPresenter;
    public ContentPresenter? TrailingHost => _trailingHost;
    public Border? GlassHost => _glassHost;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _root = GetTemplateChild("PART_Root") as Grid;
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _leadingHost = GetTemplateChild("PART_LeadingHost") as ContentPresenter;
        _headlinePresenter = GetTemplateChild("PART_HeadlinePresenter") as TextBlock;
        _supportingPresenter = GetTemplateChild("PART_SupportingPresenter") as TextBlock;
        _trailingHost = GetTemplateChild("PART_TrailingHost") as ContentPresenter;
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new SeaGlass.WinUI.Automation.SeaGlassListItemAutomationPeer(this);
}
