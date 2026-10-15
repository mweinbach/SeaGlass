using System;
using System.Collections.Generic;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Command bar with primary and secondary action buttons, overflow button, and sea-glass popover.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_PrimaryItemsPresenter", Type = typeof(ItemsControl))]
[TemplatePart(Name = "PART_MoreButton", Type = typeof(Button))]
[TemplatePart(Name = "PART_SecondaryItemsHost", Type = typeof(ItemsControl))]
[TemplatePart(Name = "PART_OverflowPopup", Type = typeof(Popup))]
public class SeaGlassCommandBar : SeaGlassSurface
{
    public static readonly DependencyProperty PrimaryCommandsProperty =
        DependencyProperty.Register(nameof(PrimaryCommands), typeof(IList<ICommandBarElement>), typeof(SeaGlassCommandBar),
            new PropertyMetadata(null));

    public static readonly DependencyProperty SecondaryCommandsProperty =
        DependencyProperty.Register(nameof(SecondaryCommands), typeof(IList<ICommandBarElement>), typeof(SeaGlassCommandBar),
            new PropertyMetadata(null));

    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(SeaGlassCommandBar),
            new PropertyMetadata(false, (d, e) =>
            {
                var cb = (SeaGlassCommandBar)d;
                if ((bool)e.NewValue) cb.Opened?.Invoke(cb, EventArgs.Empty);
                else cb.Closed?.Invoke(cb, EventArgs.Empty);
            }));

    public static readonly DependencyProperty DefaultLabelPositionProperty =
        DependencyProperty.Register(nameof(DefaultLabelPosition), typeof(CommandBarDefaultLabelPosition), typeof(SeaGlassCommandBar),
            new PropertyMetadata(CommandBarDefaultLabelPosition.Right));

    public static readonly DependencyProperty OverflowButtonVisibilityProperty =
        DependencyProperty.Register(nameof(OverflowButtonVisibility), typeof(CommandBarOverflowButtonVisibility), typeof(SeaGlassCommandBar),
            new PropertyMetadata(CommandBarOverflowButtonVisibility.Auto));

    public IList<ICommandBarElement> PrimaryCommands
    {
        get => (IList<ICommandBarElement>)GetValue(PrimaryCommandsProperty);
        set => SetValue(PrimaryCommandsProperty, value);
    }

    public IList<ICommandBarElement> SecondaryCommands
    {
        get => (IList<ICommandBarElement>)GetValue(SecondaryCommandsProperty);
        set => SetValue(SecondaryCommandsProperty, value);
    }

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public CommandBarDefaultLabelPosition DefaultLabelPosition
    {
        get => (CommandBarDefaultLabelPosition)GetValue(DefaultLabelPositionProperty);
        set => SetValue(DefaultLabelPositionProperty, value);
    }

    public CommandBarOverflowButtonVisibility OverflowButtonVisibility
    {
        get => (CommandBarOverflowButtonVisibility)GetValue(OverflowButtonVisibilityProperty);
        set => SetValue(OverflowButtonVisibilityProperty, value);
    }

    public event TypedEventHandler<SeaGlassCommandBar, EventArgs>? Opening;
    public event TypedEventHandler<SeaGlassCommandBar, EventArgs>? Opened;
    public event TypedEventHandler<SeaGlassCommandBar, EventArgs>? Closing;
    public event TypedEventHandler<SeaGlassCommandBar, EventArgs>? Closed;

    public SeaGlassCommandBar()
    {
        DefaultStyleKey = typeof(SeaGlassCommandBar);
        PrimaryCommands = new List<ICommandBarElement>();
        SecondaryCommands = new List<ICommandBarElement>();
    }

    private Grid? _root;
    private Border? _glassHost;
    private ItemsControl? _primaryItemsPresenter;
    private Button? _moreButton;
    private ItemsControl? _secondaryItemsHost;
    private Popup? _overflowPopup;

    public ItemsControl? PrimaryItemsPresenter => _primaryItemsPresenter;
    public Button? MoreButton => _moreButton;
    public ItemsControl? SecondaryItemsHost => _secondaryItemsHost;
    public Popup? OverflowPopup => _overflowPopup;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        if (_moreButton != null)
        {
            _moreButton.Click -= OnMoreButtonClick;
        }

        _root = GetTemplateChild("PART_Root") as Grid;
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _primaryItemsPresenter = GetTemplateChild("PART_PrimaryItemsPresenter") as ItemsControl;
        _moreButton = GetTemplateChild("PART_MoreButton") as Button;
        _secondaryItemsHost = GetTemplateChild("PART_SecondaryItemsHost") as ItemsControl;
        _overflowPopup = GetTemplateChild("PART_OverflowPopup") as Popup;

        if (_moreButton != null)
        {
            _moreButton.Click += OnMoreButtonClick;
        }
    }

    private void OnMoreButtonClick(object sender, RoutedEventArgs e)
    {
        ToggleOverflow();
    }

    public void ToggleOverflow()
    {
        if (!IsOpen) Opening?.Invoke(this, EventArgs.Empty);
        else Closing?.Invoke(this, EventArgs.Empty);
        IsOpen = !IsOpen;
        if (_overflowPopup != null)
        {
            _overflowPopup.IsOpen = IsOpen;
        }
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new SeaGlass.WinUI.Automation.SeaGlassCommandBarAutomationPeer(this);
}
