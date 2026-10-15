using System;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Floating Liquid SeaGlass Snackbar / Toast Banner with leading icon, message, and action button.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_LeadingGlyph", Type = typeof(ContentPresenter))]
[TemplatePart(Name = "PART_MessagePresenter", Type = typeof(TextBlock))]
[TemplatePart(Name = "PART_ActionButton", Type = typeof(Button))]
public class SeaGlassSnackbar : SeaGlassSurface
{
    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(
            nameof(Message),
            typeof(string),
            typeof(SeaGlassSnackbar),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ActionLabelProperty =
        DependencyProperty.Register(
            nameof(ActionLabel),
            typeof(string),
            typeof(SeaGlassSnackbar),
            new PropertyMetadata(null));

    public static readonly DependencyProperty LeadingGlyphProperty =
        DependencyProperty.Register(
            nameof(LeadingGlyph),
            typeof(SeaGlassGlyph),
            typeof(SeaGlassSnackbar),
            new PropertyMetadata(SeaGlassGlyph.Sparkles));

    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.Register(
            nameof(IsOpen),
            typeof(bool),
            typeof(SeaGlassSnackbar),
            new PropertyMetadata(false));

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public string? ActionLabel
    {
        get => (string?)GetValue(ActionLabelProperty);
        set => SetValue(ActionLabelProperty, value);
    }

    public SeaGlassGlyph LeadingGlyph
    {
        get => (SeaGlassGlyph)GetValue(LeadingGlyphProperty);
        set => SetValue(LeadingGlyphProperty, value);
    }

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public Action? OnActionClick { get; set; }

    public event EventHandler? ActionClicked;

    public SeaGlassSnackbar()
    {
        DefaultStyleKey = typeof(SeaGlassSnackbar);
        ContentRole = SeaGlassContentRole.NavigationChrome;
        SizePx = new Vector2(440f, 52f);
        BasePaddingPx = new Vector4(16f, 12f, 16f, 12f);
    }

    public SeaGlassSnackbar(
        string message,
        SeaGlassStageState? stageState = null,
        string? actionLabel = null,
        Action? onActionClick = null,
        SeaGlassGlyph leadingGlyph = SeaGlassGlyph.Sparkles,
        SeaGlassMaterial? material = null)
        : base(
            stageState: stageState,
            material: (material ?? SeaGlassTheme.Current.Palette.ElevatedGlass) with
            {
                CornerRadiusDp = 26f,
                ThicknessDp = 18f
            },
            contentRole: SeaGlassContentRole.NavigationChrome)
    {
        DefaultStyleKey = typeof(SeaGlassSnackbar);
        Message = message;
        ActionLabel = actionLabel;
        OnActionClick = onActionClick;
        LeadingGlyph = leadingGlyph;
        SizePx = new Vector2(440f, 52f);
        BasePaddingPx = new Vector4(16f, 12f, 16f, 12f);
    }

    private Grid? _root;
    private Border? _glassHost;
    private ContentPresenter? _leadingGlyph;
    private TextBlock? _messagePresenter;
    private Button? _actionButton;

    public ContentPresenter? LeadingGlyphPresenter => _leadingGlyph;
    public TextBlock? MessagePresenter => _messagePresenter;
    public Button? ActionButton => _actionButton;
    public Border? GlassHost => _glassHost;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        if (_actionButton != null)
        {
            _actionButton.Click -= OnActionButtonClick;
        }

        _root = GetTemplateChild("PART_Root") as Grid;
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _leadingGlyph = GetTemplateChild("PART_LeadingGlyph") as ContentPresenter;
        _messagePresenter = GetTemplateChild("PART_MessagePresenter") as TextBlock;
        _actionButton = GetTemplateChild("PART_ActionButton") as Button;

        if (_actionButton != null)
        {
            _actionButton.Click += OnActionButtonClick;
        }
    }

    private void OnActionButtonClick(object sender, RoutedEventArgs e)
    {
        InvokeAction();
    }

    public void InvokeAction()
    {
        OnActionClick?.Invoke();
        ActionClicked?.Invoke(this, EventArgs.Empty);
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new SeaGlass.WinUI.Automation.SeaGlassSnackbarAutomationPeer(this);
}
