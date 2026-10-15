using System;
using System.Collections.Generic;
using System.Numerics;
using ICommand = global::System.Windows.Input.ICommand;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// iOS-Style Frosted Action Sheet with grouped actions and a detached concentric Cancel pill.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_HeaderPresenter", Type = typeof(StackPanel))]
[TemplatePart(Name = "PART_ActionsHost", Type = typeof(ItemsControl))]
[TemplatePart(Name = "PART_CancelButton", Type = typeof(Button))]
public class SeaGlassActionSheet : SeaGlassSurface
{
    private float _progress;
    private float _vel;
    private readonly SeaGlassMaterial _baseMaterial;

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(SeaGlassActionSheet),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(SeaGlassActionSheet),
            new PropertyMetadata(null));

    public static readonly DependencyProperty VisibleProperty =
        DependencyProperty.Register(nameof(Visible), typeof(bool), typeof(SeaGlassActionSheet),
            new PropertyMetadata(false, (d, e) => ((SeaGlassActionSheet)d)._progress = (bool)e.NewValue ? 1f : 0f));

    public static readonly DependencyProperty ActionsProperty =
        DependencyProperty.Register(nameof(Actions), typeof(IReadOnlyList<SeaGlassContextMenuItem>), typeof(SeaGlassActionSheet),
            new PropertyMetadata(null));

    public static readonly DependencyProperty DismissCommandProperty =
        DependencyProperty.Register(nameof(DismissCommand), typeof(ICommand), typeof(SeaGlassActionSheet),
            new PropertyMetadata(null));

    public static readonly DependencyProperty CancelLabelProperty =
        DependencyProperty.Register(nameof(CancelLabel), typeof(string), typeof(SeaGlassActionSheet),
            new PropertyMetadata("Cancel"));

    public string CancelLabel
    {
        get => (string)GetValue(CancelLabelProperty);
        set => SetValue(CancelLabelProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Message
    {
        get => (string?)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public bool Visible
    {
        get => (bool)GetValue(VisibleProperty);
        set => SetValue(VisibleProperty, value);
    }

    public IReadOnlyList<SeaGlassContextMenuItem> Actions
    {
        get => (IReadOnlyList<SeaGlassContextMenuItem>)GetValue(ActionsProperty);
        set => SetValue(ActionsProperty, value);
    }

    public ICommand? DismissCommand
    {
        get => (ICommand?)GetValue(DismissCommandProperty);
        set => SetValue(DismissCommandProperty, value);
    }

    public Action? OnDismissRequest { get; set; }

    public float OpenProgress => Math.Clamp(_progress, 0f, 1.15f);

    public event TypedEventHandler<SeaGlassActionSheet, EventArgs>? DismissRequested;
    public event TypedEventHandler<SeaGlassActionSheet, SeaGlassContextMenuItem>? ActionSelected;

    public SeaGlassActionSheet()
    {
        DefaultStyleKey = typeof(SeaGlassActionSheet);
        _baseMaterial = Material;
        CornerAdaptationMode = SeaGlassCornerAdaptationMode.ConcentricProximity;
        Actions = SeaGlassContextMenuOverlay.DefaultIosContextMenuItems();
        SizePx = new Vector2(420f, 310f);
    }

    public SeaGlassActionSheet(
        string title,
        bool visible,
        Action onDismissRequest,
        IReadOnlyList<SeaGlassContextMenuItem>? actions = null,
        string? message = null,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? material = null)
        : base(
            stageState: stageState ?? SeaGlassStageHost.AmbientStageState,
            material: material ?? (SeaGlassTheme.Current.Palette.SurfaceGlass with
            {
                CornerRadiusDp = 32f,
                ThicknessDp = 20f,
                RimChalkiness = 0.88f
            }),
            cornerAdaptationMode: SeaGlassCornerAdaptationMode.ConcentricProximity)
    {
        DefaultStyleKey = typeof(SeaGlassActionSheet);
        _baseMaterial = Material;
        Title = title;
        Message = message;
        Visible = visible;
        _progress = visible ? 1f : 0f;
        OnDismissRequest = onDismissRequest;
        Actions = actions ?? SeaGlassContextMenuOverlay.DefaultIosContextMenuItems();
        SizePx = new Vector2(420f, 310f);
    }

    public void Dismiss()
    {
        Visible = false;
        OnDismissRequest?.Invoke();
        DismissRequested?.Invoke(this, EventArgs.Empty);
        DismissCommand?.Execute(null);
    }

    public void SelectAction(SeaGlassContextMenuItem action)
    {
        ActionSelected?.Invoke(this, action);
        Dismiss();
    }

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        activeTheme.MotionScheme.DefaultSpatial.Step(ref _progress, ref _vel, Visible ? 1f : 0f, dtSeconds);
        float p = Math.Clamp(_progress, 0f, 1f);
        Material = _baseMaterial.Deaged(4f * p * (1f - p));
        return EvaluateFrame(dtSeconds, activeTheme);
    }

    private Grid? _root;
    private Border? _glassHost;
    private StackPanel? _headerPresenter;
    private ItemsControl? _actionsHost;
    private Button? _cancelButton;

    public StackPanel? HeaderPresenter => _headerPresenter;
    public ItemsControl? ActionsHost => _actionsHost;
    public Button? CancelButton => _cancelButton;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        if (_cancelButton != null)
        {
            _cancelButton.Click -= OnCancelButtonClick;
        }

        _root = GetTemplateChild("PART_Root") as Grid;
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _headerPresenter = GetTemplateChild("PART_HeaderPresenter") as StackPanel;
        _actionsHost = GetTemplateChild("PART_ActionsHost") as ItemsControl;
        _cancelButton = GetTemplateChild("PART_CancelButton") as Button;

        if (_cancelButton != null)
        {
            _cancelButton.Click += OnCancelButtonClick;
        }
    }

    private void OnCancelButtonClick(object sender, RoutedEventArgs e)
    {
        Dismiss();
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new SeaGlass.WinUI.Automation.SeaGlassActionSheetAutomationPeer(this);
}

