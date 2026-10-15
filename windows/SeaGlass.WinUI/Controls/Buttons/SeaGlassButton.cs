using System;
using System.Numerics;
using ICommand = global::System.Windows.Input.ICommand;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Tactile tumbled sea-glass button that physically compresses on press with adaptive
/// touch-point HDR caustic flare and 3D perspective tilt.
/// </summary>
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_ContentPresenter", Type = typeof(ContentPresenter))]
[TemplatePart(Name = "PART_IconPresenter", Type = typeof(ContentPresenter))]
[TemplatePart(Name = "PART_LabelPresenter", Type = typeof(TextBlock))]
public class SeaGlassButton : SeaGlassSurface
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(
            nameof(Label),
            typeof(string),
            typeof(SeaGlassButton),
            new PropertyMetadata(string.Empty, OnLabelChanged));

    public static readonly DependencyProperty LeadingGlyphProperty =
        DependencyProperty.Register(
            nameof(LeadingGlyph),
            typeof(SeaGlassGlyph?),
            typeof(SeaGlassButton),
            new PropertyMetadata(null));

    public static readonly DependencyProperty IconGlyphProperty =
        DependencyProperty.Register(
            nameof(IconGlyph),
            typeof(SeaGlassGlyph?),
            typeof(SeaGlassButton),
            new PropertyMetadata(null, OnIconGlyphChanged));

    public static readonly DependencyProperty IsPressedProperty =
        DependencyProperty.Register(
            nameof(IsPressed),
            typeof(bool),
            typeof(SeaGlassButton),
            new PropertyMetadata(false));

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public SeaGlassGlyph? LeadingGlyph
    {
        get => (SeaGlassGlyph?)GetValue(LeadingGlyphProperty);
        set => SetValue(LeadingGlyphProperty, value);
    }

    public SeaGlassGlyph? IconGlyph
    {
        get => (SeaGlassGlyph?)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    public bool IsPressed
    {
        get => (bool)GetValue(IsPressedProperty);
        set => SetValue(IsPressedProperty, value);
    }

    public SeaGlassButton()
    {
        DefaultStyleKey = typeof(SeaGlassButton);
        CornerAdaptationMode = SeaGlassCornerAdaptationMode.Fixed;
        ContentRole = SeaGlassContentRole.TextAndIcons;
        SizePx = new Vector2(148f, 46f);
        BasePaddingPx = new Vector4(22f, 13f, 22f, 13f);
        IsTabStop = true;
    }

    public SeaGlassButton(
        string label,
        Action? onClick = null,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? material = null,
        SeaGlassGlyph? leadingGlyph = null,
        SeaGlassCornerAdaptationMode cornerAdaptationMode = SeaGlassCornerAdaptationMode.Fixed,
        SeaGlassCornerMask? cornerMask = null,
        float cornerProximityThresholdDp = 24f)
        : base(
            stageState: stageState,
            material: material ?? SeaGlassTheme.Current.Palette.PrimaryGlass,
            cornerAdaptationMode: cornerAdaptationMode,
            contentRole: SeaGlassContentRole.TextAndIcons,
            onClick: onClick,
            cornerMask: cornerMask ?? SeaGlassCornerMask.None,
            cornerProximityThresholdDp: cornerProximityThresholdDp)
    {
        DefaultStyleKey = typeof(SeaGlassButton);
        Label = label;
        LeadingGlyph = leadingGlyph;
        IconGlyph = leadingGlyph;
        Content = label;
        SizePx = new Vector2(148f, 46f);
        BasePaddingPx = new Vector4(22f, 13f, 22f, 13f);
    }

    public static SeaGlassButton Create(string label, Action? onClick = null) =>
        new SeaGlassButton(label, onClick);

    private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassButton btn)
        {
            if (btn.Content == null)
            {
                btn.Content = e.NewValue;
            }
            btn.UpdateContentAndIconVisibility();
        }
    }

    private static void OnIconGlyphChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassButton btn)
        {
            btn.LeadingGlyph = (SeaGlassGlyph?)e.NewValue;
            btn.UpdateContentAndIconVisibility();
        }
    }

    protected ContentPresenter? _iconPresenter;
    protected TextBlock? _labelPresenter;
    protected ContentPresenter? _contentPresenter;

    public ContentPresenter? IconPresenter => _iconPresenter;
    public TextBlock? LabelPresenter => _labelPresenter;
    public ContentPresenter? ContentPresenterChild => _contentPresenter;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _iconPresenter = GetTemplateChild("PART_IconPresenter") as ContentPresenter;
        _labelPresenter = GetTemplateChild("PART_LabelPresenter") as TextBlock;
        _contentPresenter = GetTemplateChild("PART_ContentPresenter") as ContentPresenter;

        UpdateContentAndIconVisibility();
    }

    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);
        UpdateContentAndIconVisibility();
    }

    protected virtual void UpdateContentAndIconVisibility()
    {
        bool hasIcon = (IconGlyph != null || LeadingGlyph != null);
        if (_iconPresenter != null)
        {
            _iconPresenter.Visibility = hasIcon ? Visibility.Visible : Visibility.Collapsed;
        }

        bool hasLabel = !string.IsNullOrEmpty(Label);
        if (_labelPresenter != null)
        {
            _labelPresenter.Text = Label ?? string.Empty;
            _labelPresenter.Visibility = hasLabel ? Visibility.Visible : Visibility.Collapsed;
        }

        if (_contentPresenter != null)
        {
            // If Content is null, or identical to Label (e.g. set by constructor or OnLabelChanged),
            // hide ContentPresenter to ensure only ONE visible text element displays the label.
            bool contentDuplicatesLabel = (Content == null) ||
                                          (hasLabel && Content is string s && s == Label);
            _contentPresenter.Visibility = contentDuplicatesLabel ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    public void SetPresentersForTesting(ContentPresenter? icon, TextBlock? label, ContentPresenter? content)
    {
        _iconPresenter = icon;
        _labelPresenter = label;
        _contentPresenter = content;
        UpdateContentAndIconVisibility();
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new Automation.SeaGlassButtonAutomationPeer(this);
}

