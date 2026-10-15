using System;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Tactile Liquid SeaGlass Slider with fluid dragging boost and velocity stretch.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_TitlePresenter", Type = typeof(TextBlock))]
[TemplatePart(Name = "PART_ValuePresenter", Type = typeof(TextBlock))]
[TemplatePart(Name = "PART_Track", Type = typeof(Border))]
[TemplatePart(Name = "PART_ActiveTrackBar", Type = typeof(Border))]
[TemplatePart(Name = "PART_Thumb", Type = typeof(Microsoft.UI.Xaml.Controls.Primitives.Thumb))]
[TemplatePart(Name = "PART_FocusVisual", Type = typeof(Border))]
public class SeaGlassLiquidSlider : SeaGlassSurface
{
    private float _dragBoost;
    private float _dragVel;

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(SeaGlassLiquidSlider),
            new PropertyMetadata(0.0, OnValueChanged));

    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.Register(
            nameof(Minimum),
            typeof(double),
            typeof(SeaGlassLiquidSlider),
            new PropertyMetadata(0.0));

    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register(
            nameof(Maximum),
            typeof(double),
            typeof(SeaGlassLiquidSlider),
            new PropertyMetadata(100.0));

    public static readonly DependencyProperty StepProperty =
        DependencyProperty.Register(
            nameof(Step),
            typeof(double),
            typeof(SeaGlassLiquidSlider),
            new PropertyMetadata(1.0));

    public static readonly DependencyProperty IsScrubbingProperty =
        DependencyProperty.Register(
            nameof(IsScrubbing),
            typeof(bool),
            typeof(SeaGlassLiquidSlider),
            new PropertyMetadata(false));

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(SeaGlassLiquidSlider),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ValueTextProperty =
        DependencyProperty.Register(
            nameof(ValueText),
            typeof(string),
            typeof(SeaGlassLiquidSlider),
            new PropertyMetadata(string.Empty));

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double Step
    {
        get => (double)GetValue(StepProperty);
        set => SetValue(StepProperty, value);
    }

    public bool IsScrubbing
    {
        get => (bool)GetValue(IsScrubbingProperty);
        set => SetValue(IsScrubbingProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string ValueText
    {
        get => (string)GetValue(ValueTextProperty);
        set => SetValue(ValueTextProperty, value);
    }

    public float MinValue => (float)Minimum;
    public float MaxValue => (float)Maximum;
    public Action<float>? OnValueChange { get; set; }
    public SeaGlassMaterial ThumbMaterial { get; set; }
    public bool IsDragging
    {
        get => IsScrubbing;
        set => IsScrubbing = value;
    }

    public float Fraction => Math.Clamp(((float)Value - MinValue) / MathF.Max(1e-4f, MaxValue - MinValue), 0f, 1f);
    public float DragBoost => _dragBoost;
    public float ThumbScaleX => 1f + 0.24f * _dragBoost;
    public float ThumbScaleY => 1f + 0.12f * _dragBoost;

    public event EventHandler<double>? ValueChanged;

    public SeaGlassLiquidSlider()
    {
        DefaultStyleKey = typeof(SeaGlassLiquidSlider);
        ThumbMaterial = SeaGlassTheme.Current.Palette.PrimaryGlass;
        SizePx = new Vector2(34f, 22f);
        IsTabStop = true;
    }

    public SeaGlassLiquidSlider(
        string title,
        string valueText,
        float value,
        float minValue,
        float maxValue,
        Action<float>? onValueChange = null,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? thumbMaterial = null)
        : base(stageState: stageState)
    {
        DefaultStyleKey = typeof(SeaGlassLiquidSlider);
        Title = title;
        ValueText = valueText;
        Minimum = minValue;
        Maximum = MathF.Max(minValue + 1e-4f, maxValue);
        Value = Math.Clamp(value, minValue, maxValue);
        OnValueChange = onValueChange;
        ThumbMaterial = thumbMaterial ?? SeaGlassTheme.Current.Palette.PrimaryGlass;
        SizePx = new Vector2(34f, 22f);
    }

    public double StepFrequency
    {
        get => Step;
        set => Step = value;
    }

    public static SeaGlassLiquidSlider Create(double value = 0, double min = 0, double max = 100) =>
        new SeaGlassLiquidSlider { Minimum = min, Maximum = max, Value = value };

    private Grid? _root;
    private Border? _glassHost;
    private TextBlock? _titlePresenter;
    private TextBlock? _valuePresenter;
    private Border? _track;
    private Border? _activeTrackBar;
    private Microsoft.UI.Xaml.Controls.Primitives.Thumb? _thumb;
    private Border? _focusVisual;

    public Border? Track => _track;
    public Border? ActiveTrackBar => _activeTrackBar;
    public Microsoft.UI.Xaml.Controls.Primitives.Thumb? Thumb => _thumb;
    public Border? GlassHost => _glassHost;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        if (_thumb != null)
        {
            _thumb.DragStarted -= OnThumbDragStarted;
            _thumb.DragDelta -= OnThumbDragDelta;
            _thumb.DragCompleted -= OnThumbDragCompleted;
        }
        if (_track != null)
        {
            _track.PointerPressed -= OnTrackPointerPressed;
            _track.SizeChanged -= OnTrackSizeChanged;
        }

        _root = GetTemplateChild("PART_Root") as Grid;
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _titlePresenter = GetTemplateChild("PART_TitlePresenter") as TextBlock;
        _valuePresenter = GetTemplateChild("PART_ValuePresenter") as TextBlock;
        _track = GetTemplateChild("PART_Track") as Border;
        _activeTrackBar = GetTemplateChild("PART_ActiveTrackBar") as Border;
        _thumb = GetTemplateChild("PART_Thumb") as Microsoft.UI.Xaml.Controls.Primitives.Thumb;
        _focusVisual = GetTemplateChild("PART_FocusVisual") as Border;

        if (_thumb != null)
        {
            _thumb.DragStarted += OnThumbDragStarted;
            _thumb.DragDelta += OnThumbDragDelta;
            _thumb.DragCompleted += OnThumbDragCompleted;
        }
        if (_track != null)
        {
            _track.PointerPressed += OnTrackPointerPressed;
            _track.SizeChanged += OnTrackSizeChanged;
        }

        UpdateVisualState();
    }

    private void OnTrackPointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (_track == null) return;
        var pt = e.GetCurrentPoint(_track).Position;
        double trackW = _track.ActualWidth > 0 ? _track.ActualWidth : 200.0;
        double ratio = Math.Clamp(pt.X / trackW, 0.0, 1.0);
        Value = Minimum + ratio * (Maximum - Minimum);
        e.Handled = true;
    }

    private void OnTrackSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateVisualState();
    }

    private void OnThumbDragStarted(object sender, Microsoft.UI.Xaml.Controls.Primitives.DragStartedEventArgs e)
    {
        IsDragging = true;
    }

    private void OnThumbDragDelta(object sender, Microsoft.UI.Xaml.Controls.Primitives.DragDeltaEventArgs e)
    {
        double trackW = _track != null && _track.ActualWidth > 0 ? _track.ActualWidth : 200.0;
        double deltaVal = (e.HorizontalChange / trackW) * (Maximum - Minimum);
        Value = Math.Clamp(Value + deltaVal, Minimum, Maximum);
    }

    private void OnThumbDragCompleted(object sender, Microsoft.UI.Xaml.Controls.Primitives.DragCompletedEventArgs e)
    {
        IsDragging = false;
    }

    private void UpdateVisualState()
    {
        if (string.IsNullOrEmpty(ValueText))
        {
            ValueText = Value.ToString("0.0");
        }
        if (_valuePresenter != null)
        {
            _valuePresenter.Text = ValueText;
        }
        if (_titlePresenter != null && !string.IsNullOrEmpty(Title))
        {
            _titlePresenter.Text = Title;
        }

        double trackW = _track != null && _track.ActualWidth > 0 ? _track.ActualWidth : 200.0;
        double thumbW = _thumb != null && _thumb.ActualWidth > 0 ? _thumb.ActualWidth : 22.0;
        double usableW = Math.Max(0.0, trackW - thumbW);
        double thumbLeft = Fraction * usableW;

        if (_thumb != null)
        {
            _thumb.Margin = new Thickness(thumbLeft, 0, 0, 0);
        }
        if (_activeTrackBar != null)
        {
            _activeTrackBar.Width = Math.Clamp(Fraction * trackW, 0.0, trackW);
        }
    }

    protected override void OnKeyDown(Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        base.OnKeyDown(e);
        switch (e.Key)
        {
            case Windows.System.VirtualKey.Left:
            case Windows.System.VirtualKey.Down:
                Value = Math.Max(Minimum, Value - Step);
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.Right:
            case Windows.System.VirtualKey.Up:
                Value = Math.Min(Maximum, Value + Step);
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.Home:
                Value = Minimum;
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.End:
                Value = Maximum;
                e.Handled = true;
                break;
        }
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassLiquidSlider slider && e.NewValue is double val)
        {
            slider.UpdateVisualState();
            slider.OnValueChange?.Invoke((float)val);
            slider.ValueChanged?.Invoke(slider, val);
        }
    }

    public void SetValue(float newValue)
    {
        Value = Math.Clamp(newValue, MinValue, MaxValue);
    }

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        activeTheme.MotionScheme.FastSpatial.Step(ref _dragBoost, ref _dragVel, IsDragging ? 1f : 0f, dtSeconds);
        float db = Math.Clamp(_dragBoost, 0f, 1.2f);

        Material = ThumbMaterial.Deaged(db) with { CornerRadiusDp = 14f };

        return EvaluateFrame(dtSeconds, activeTheme);
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new Automation.SeaGlassLiquidSliderAutomationPeer(this);
}
