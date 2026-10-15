using System;
using ICommand = global::System.Windows.Input.ICommand;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Modal and inline presentation dialog card with header glyph, title, message body, and action buttons.
/// </summary>
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_TitlePresenter", Type = typeof(TextBlock))]
[TemplatePart(Name = "PART_SubtitlePresenter", Type = typeof(TextBlock))]
[TemplatePart(Name = "PART_BodyPresenter", Type = typeof(TextBlock))]
[TemplatePart(Name = "PART_ConfirmButton", Type = typeof(Button))]
[TemplatePart(Name = "PART_DismissButton", Type = typeof(Button))]
public class SeaGlassDialogCard : SeaGlassCard
{
    public static readonly DependencyProperty BodyProperty =
        DependencyProperty.Register(
            nameof(Body),
            typeof(string),
            typeof(SeaGlassDialogCard),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty GlyphProperty =
        DependencyProperty.Register(
            nameof(Glyph),
            typeof(SeaGlassGlyph),
            typeof(SeaGlassDialogCard),
            new PropertyMetadata(SeaGlassGlyph.Sparkles));

    public static readonly DependencyProperty ConfirmLabelProperty =
        DependencyProperty.Register(
            nameof(ConfirmLabel),
            typeof(string),
            typeof(SeaGlassDialogCard),
            new PropertyMetadata("OK"));

    public static readonly DependencyProperty DismissLabelProperty =
        DependencyProperty.Register(
            nameof(DismissLabel),
            typeof(string),
            typeof(SeaGlassDialogCard),
            new PropertyMetadata("Cancel"));

    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.Register(
            nameof(IsOpen),
            typeof(bool),
            typeof(SeaGlassDialogCard),
            new PropertyMetadata(false, OnIsOpenChanged));

    public static readonly DependencyProperty IsBackdropDimmedProperty =
        DependencyProperty.Register(
            nameof(IsBackdropDimmed),
            typeof(bool),
            typeof(SeaGlassDialogCard),
            new PropertyMetadata(true));

    public static readonly DependencyProperty ConfirmCommandProperty =
        DependencyProperty.Register(
            nameof(ConfirmCommand),
            typeof(ICommand),
            typeof(SeaGlassDialogCard),
            new PropertyMetadata(null));

    public static readonly DependencyProperty DismissCommandProperty =
        DependencyProperty.Register(
            nameof(DismissCommand),
            typeof(ICommand),
            typeof(SeaGlassDialogCard),
            new PropertyMetadata(null));

    public string Body
    {
        get => (string)GetValue(BodyProperty);
        set => SetValue(BodyProperty, value);
    }

    public SeaGlassGlyph Glyph
    {
        get => (SeaGlassGlyph)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public string ConfirmLabel
    {
        get => (string)GetValue(ConfirmLabelProperty);
        set => SetValue(ConfirmLabelProperty, value);
    }

    public string DismissLabel
    {
        get => (string)GetValue(DismissLabelProperty);
        set => SetValue(DismissLabelProperty, value);
    }

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public bool IsBackdropDimmed
    {
        get => (bool)GetValue(IsBackdropDimmedProperty);
        set => SetValue(IsBackdropDimmedProperty, value);
    }

    public ICommand? ConfirmCommand
    {
        get => (ICommand?)GetValue(ConfirmCommandProperty);
        set => SetValue(ConfirmCommandProperty, value);
    }

    public ICommand? DismissCommand
    {
        get => (ICommand?)GetValue(DismissCommandProperty);
        set => SetValue(DismissCommandProperty, value);
    }

    public event EventHandler? Confirmed;
    public event EventHandler? Dismissed;
    public event EventHandler? Opened;
    public event EventHandler? Closed;

    public SeaGlassDialogCard()
    {
        DefaultStyleKey = typeof(SeaGlassDialogCard);
    }

    public SeaGlassDialogCard(
        string title,
        string body,
        SeaGlassStageState? stage = null,
        SeaGlassMaterial? material = null,
        Action? onConfirm = null,
        Action? onDismiss = null)
        : base(title, null, stage, material)
    {
        DefaultStyleKey = typeof(SeaGlassDialogCard);
        Body = body;
        if (onConfirm != null) Confirmed += (_, _) => onConfirm();
        if (onDismiss != null) Dismissed += (_, _) => onDismiss();
    }

    public SeaGlassDialogCard(
        string title,
        string message,
        string confirmActionLabel,
        Action onConfirm,
        SeaGlassStageState? stage = null,
        string? dismissActionLabel = null,
        Action? onDismiss = null)
        : base(title, null, stage, null)
    {
        DefaultStyleKey = typeof(SeaGlassDialogCard);
        Body = message;
        ConfirmLabel = confirmActionLabel;
        if (dismissActionLabel != null) DismissLabel = dismissActionLabel;
        if (onConfirm != null) Confirmed += (_, _) => onConfirm();
        if (onDismiss != null) Dismissed += (_, _) => onDismiss();
    }

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassDialogCard dialog)
        {
            if ((bool)e.NewValue) dialog.Opened?.Invoke(dialog, EventArgs.Empty);
            else dialog.Closed?.Invoke(dialog, EventArgs.Empty);
        }
    }

    private TextBlock? _titlePresenter;
    private TextBlock? _bodyPresenter;
    private Button? _confirmButton;
    private Button? _dismissButton;

    public new TextBlock? TitlePresenter => _titlePresenter;
    public TextBlock? BodyPresenter => _bodyPresenter;
    public Button? ConfirmButton => _confirmButton;
    public Button? DismissButton => _dismissButton;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        if (_confirmButton != null) _confirmButton.Click -= OnConfirmButtonClick;
        if (_dismissButton != null) _dismissButton.Click -= OnDismissButtonClick;

        _titlePresenter = GetTemplateChild("PART_TitlePresenter") as TextBlock;
        _bodyPresenter = GetTemplateChild("PART_BodyPresenter") as TextBlock;
        _confirmButton = GetTemplateChild("PART_ConfirmButton") as Button;
        _dismissButton = GetTemplateChild("PART_DismissButton") as Button;

        if (_confirmButton != null)
        {
            _confirmButton.Content = ConfirmLabel;
            _confirmButton.Click += OnConfirmButtonClick;
        }
        if (_dismissButton != null)
        {
            _dismissButton.Content = DismissLabel;
            _dismissButton.Click += OnDismissButtonClick;
        }

        if (_titlePresenter != null && !string.IsNullOrEmpty(Title)) _titlePresenter.Text = Title;
        if (_bodyPresenter != null && !string.IsNullOrEmpty(Body)) _bodyPresenter.Text = Body;
    }

    private void OnConfirmButtonClick(object sender, RoutedEventArgs e) => OnConfirm();
    private void OnDismissButtonClick(object sender, RoutedEventArgs e) => OnDismiss();

    public void OnConfirm()
    {
        Confirmed?.Invoke(this, EventArgs.Empty);
        if (ConfirmCommand?.CanExecute(null) == true) ConfirmCommand.Execute(null);
        IsOpen = false;
    }

    public void OnDismiss()
    {
        Dismissed?.Invoke(this, EventArgs.Empty);
        if (DismissCommand?.CanExecute(null) == true) DismissCommand.Execute(null);
        IsOpen = false;
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new SeaGlass.WinUI.Automation.SeaGlassDialogCardAutomationPeer(this);
}
