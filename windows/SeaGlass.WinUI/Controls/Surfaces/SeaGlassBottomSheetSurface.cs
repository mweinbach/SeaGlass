using System;
using System.Numerics;
using ICommand = global::System.Windows.Input.ICommand;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

public enum SeaGlassBottomSheetState
{
    Hidden,
    Peek,
    Expanded
}

/// <summary>
/// Frosted SeaGlass Bottom Sheet surface with a centered drag-handle pill and structured sheet content.
/// </summary>
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_DragHandle", Type = typeof(Border))]
[TemplatePart(Name = "PART_HeaderPresenter", Type = typeof(StackPanel))]
[TemplatePart(Name = "PART_ContentPresenter", Type = typeof(ContentPresenter))]
public class SeaGlassBottomSheetSurface : SeaGlassSurface
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(SeaGlassBottomSheetSurface),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SubtitleProperty =
        DependencyProperty.Register(
            nameof(Subtitle),
            typeof(string),
            typeof(SeaGlassBottomSheetSurface),
            new PropertyMetadata(null));

    public static readonly DependencyProperty PeekHeightDpProperty =
        DependencyProperty.Register(
            nameof(PeekHeightDp),
            typeof(double),
            typeof(SeaGlassBottomSheetSurface),
            new PropertyMetadata(120.0));

    public static readonly DependencyProperty ExpandedHeightDpProperty =
        DependencyProperty.Register(
            nameof(ExpandedHeightDp),
            typeof(double),
            typeof(SeaGlassBottomSheetSurface),
            new PropertyMetadata(480.0));

    public static readonly DependencyProperty CurrentStateProperty =
        DependencyProperty.Register(
            nameof(CurrentState),
            typeof(SeaGlassBottomSheetState),
            typeof(SeaGlassBottomSheetSurface),
            new PropertyMetadata(SeaGlassBottomSheetState.Peek, OnCurrentStateChanged));

    public static readonly DependencyProperty DragOffsetPxProperty =
        DependencyProperty.Register(
            nameof(DragOffsetPx),
            typeof(double),
            typeof(SeaGlassBottomSheetSurface),
            new PropertyMetadata(0.0));

    public static readonly DependencyProperty IsDraggableProperty =
        DependencyProperty.Register(
            nameof(IsDraggable),
            typeof(bool),
            typeof(SeaGlassBottomSheetSurface),
            new PropertyMetadata(true));

    public static readonly DependencyProperty DismissCommandProperty =
        DependencyProperty.Register(
            nameof(DismissCommand),
            typeof(ICommand),
            typeof(SeaGlassBottomSheetSurface),
            new PropertyMetadata(null));

    public static readonly DependencyProperty ExpandCommandProperty =
        DependencyProperty.Register(
            nameof(ExpandCommand),
            typeof(ICommand),
            typeof(SeaGlassBottomSheetSurface),
            new PropertyMetadata(null));

    public static readonly DependencyProperty CollapseCommandProperty =
        DependencyProperty.Register(
            nameof(CollapseCommand),
            typeof(ICommand),
            typeof(SeaGlassBottomSheetSurface),
            new PropertyMetadata(null));

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

    public double PeekHeightDp
    {
        get => (double)GetValue(PeekHeightDpProperty);
        set => SetValue(PeekHeightDpProperty, value);
    }

    public double ExpandedHeightDp
    {
        get => (double)GetValue(ExpandedHeightDpProperty);
        set => SetValue(ExpandedHeightDpProperty, value);
    }

    public SeaGlassBottomSheetState CurrentState
    {
        get => (SeaGlassBottomSheetState)GetValue(CurrentStateProperty);
        set => SetValue(CurrentStateProperty, value);
    }

    public double DragOffsetPx
    {
        get => (double)GetValue(DragOffsetPxProperty);
        set => SetValue(DragOffsetPxProperty, value);
    }

    public bool IsDraggable
    {
        get => (bool)GetValue(IsDraggableProperty);
        set => SetValue(IsDraggableProperty, value);
    }

    public ICommand? DismissCommand
    {
        get => (ICommand?)GetValue(DismissCommandProperty);
        set => SetValue(DismissCommandProperty, value);
    }

    public ICommand? ExpandCommand
    {
        get => (ICommand?)GetValue(ExpandCommandProperty);
        set => SetValue(ExpandCommandProperty, value);
    }

    public ICommand? CollapseCommand
    {
        get => (ICommand?)GetValue(CollapseCommandProperty);
        set => SetValue(CollapseCommandProperty, value);
    }

    public event EventHandler<SeaGlassBottomSheetState>? StateChanged;
    public event EventHandler? DismissRequested;

    public SeaGlassBottomSheetSurface()
    {
        DefaultStyleKey = typeof(SeaGlassBottomSheetSurface);
        SizePx = new Vector2(480f, 240f);
        BasePaddingPx = new Vector4(20f, 16f, 20f, 16f);
    }

    public SeaGlassBottomSheetSurface(
        string title,
        SeaGlassStageState? stageState = null,
        string? subtitle = null,
        SeaGlassMaterial? material = null)
        : base(
            stageState: stageState,
            material: (material ?? SeaGlassTheme.Current.Palette.ElevatedGlass) with
            {
                CornerRadiusDp = 34f,
                ThicknessDp = 22f,
                RimChalkiness = 0.88f
            })
    {
        DefaultStyleKey = typeof(SeaGlassBottomSheetSurface);
        Title = title;
        Subtitle = subtitle;
        SizePx = new Vector2(480f, 240f);
        BasePaddingPx = new Vector4(20f, 16f, 20f, 16f);
    }

    private static void OnCurrentStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassBottomSheetSurface sheet && e.NewValue is SeaGlassBottomSheetState st)
        {
            sheet.StateChanged?.Invoke(sheet, st);
            if (st == SeaGlassBottomSheetState.Hidden)
            {
                sheet.DismissRequested?.Invoke(sheet, EventArgs.Empty);
            }
        }
    }

    private Border? _dragHandle;
    private StackPanel? _headerPresenter;

    public Border? DragHandle => _dragHandle;
    public StackPanel? HeaderPresenter => _headerPresenter;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        if (_dragHandle != null)
        {
            _dragHandle.PointerPressed -= OnDragHandlePointerPressed;
        }

        _dragHandle = GetTemplateChild("PART_DragHandle") as Border;
        _headerPresenter = GetTemplateChild("PART_HeaderPresenter") as StackPanel;

        if (_dragHandle != null)
        {
            _dragHandle.PointerPressed += OnDragHandlePointerPressed;
        }
    }

    private void OnDragHandlePointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        CurrentState = CurrentState switch
        {
            SeaGlassBottomSheetState.Peek => SeaGlassBottomSheetState.Expanded,
            SeaGlassBottomSheetState.Expanded => SeaGlassBottomSheetState.Peek,
            _ => SeaGlassBottomSheetState.Peek
        };
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new Automation.SeaGlassBottomSheetSurfaceAutomationPeer(this);
}
