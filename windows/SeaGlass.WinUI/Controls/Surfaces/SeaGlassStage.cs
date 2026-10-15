using System;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace SeaGlass.WinUI;

/// <summary>
/// Acyclic backdrop capture root container enforcing Rule #4 in AGENTS.md.
/// Captures BackdropContent into a CompositionVisualSurface before any foreground
/// glass surfaces sample from it.
/// </summary>
[TemplatePart(Name = "PART_BackdropHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_ContentPresenter", Type = typeof(ContentPresenter))]
public class SeaGlassStage : ContentControl
{
    public static readonly DependencyProperty BackdropContentProperty =
        DependencyProperty.Register(
            nameof(BackdropContent),
            typeof(UIElement),
            typeof(SeaGlassStage),
            new PropertyMetadata(null, OnBackdropContentChanged));

    public static readonly DependencyProperty BackdropIdProperty =
        DependencyProperty.Register(
            nameof(BackdropId),
            typeof(string),
            typeof(SeaGlassStage),
            new PropertyMetadata("seaglass_stage_backdrop", OnBackdropIdChanged));

    public static readonly DependencyProperty ViewportSizeProperty =
        DependencyProperty.Register(
            nameof(ViewportSize),
            typeof(Size),
            typeof(SeaGlassStage),
            new PropertyMetadata(new Size(1920, 1080), OnViewportSizeChanged));

    public static readonly DependencyProperty IsBackdropCapturedProperty =
        DependencyProperty.Register(
            nameof(IsBackdropCaptured),
            typeof(bool),
            typeof(SeaGlassStage),
            new PropertyMetadata(false));

    public static readonly DependencyProperty ContentThemeProperty =
        DependencyProperty.Register(
            nameof(ContentTheme),
            typeof(SeaGlassContentTheme),
            typeof(SeaGlassStage),
            new PropertyMetadata(SeaGlassContentTheme.Auto, OnContentThemeChanged));

    public static readonly DependencyProperty BackdropLuminanceProperty =
        DependencyProperty.Register(
            nameof(BackdropLuminance),
            typeof(float),
            typeof(SeaGlassStage),
            new PropertyMetadata(0.5f, OnBackdropLuminanceChanged));

    public UIElement? BackdropContent
    {
        get => (UIElement?)GetValue(BackdropContentProperty);
        set => SetValue(BackdropContentProperty, value);
    }

    public string BackdropId
    {
        get => (string)GetValue(BackdropIdProperty);
        set => SetValue(BackdropIdProperty, value);
    }

    public Size ViewportSize
    {
        get => (Size)GetValue(ViewportSizeProperty);
        set => SetValue(ViewportSizeProperty, value);
    }

    public bool IsBackdropCaptured
    {
        get => (bool)GetValue(IsBackdropCapturedProperty);
        private set => SetValue(IsBackdropCapturedProperty, value);
    }

    public SeaGlassContentTheme ContentTheme
    {
        get => (SeaGlassContentTheme)GetValue(ContentThemeProperty);
        set => SetValue(ContentThemeProperty, value);
    }

    public float BackdropLuminance
    {
        get => (float)GetValue(BackdropLuminanceProperty);
        set => SetValue(BackdropLuminanceProperty, value);
    }

    public SeaGlassStageState State { get; }

    public event EventHandler? BackdropCaptured;
    public event EventHandler? BackdropInvalidated;

    public SeaGlassStage()
    {
        DefaultStyleKey = typeof(SeaGlassStage);
        State = new SeaGlassStageState();
        State.BindBackdropTarget(BackdropId, Vector2.Zero, new Vector2((float)ViewportSize.Width, (float)ViewportSize.Height));
        IsBackdropCaptured = State.IsBackdropCaptured;
        ApplyContentTheme();
    }

    public SeaGlassStage(Vector2? viewportSizePx = null, string backdropId = "seaglass_stage_backdrop")
    {
        DefaultStyleKey = typeof(SeaGlassStage);
        BackdropId = backdropId;
        State = new SeaGlassStageState();
        Vector2 size = viewportSizePx ?? new Vector2(1920f, 1080f);
        ViewportSize = new Size(size.X, size.Y);
        State.BindBackdropTarget(backdropId, Vector2.Zero, size);
        IsBackdropCaptured = State.IsBackdropCaptured;
        ApplyContentTheme();
    }

    public void ApplyContentTheme()
    {
        switch (ContentTheme)
        {
            case SeaGlassContentTheme.Dark:
                RequestedTheme = ElementTheme.Dark;
                break;
            case SeaGlassContentTheme.Light:
                RequestedTheme = ElementTheme.Light;
                break;
            case SeaGlassContentTheme.Auto:
            default:
                // Auto computes from BackdropLuminance (L < 0.48f -> Dark mode for luminous alabaster text; else Light mode)
                RequestedTheme = BackdropLuminance < 0.48f ? ElementTheme.Dark : ElementTheme.Light;
                break;
        }
    }

    private void AnalyzeBackdropLuminance()
    {
        if (BackdropContent is Panel panel && panel.Background is not null)
        {
            float lum = TryComputeBrushLuminance(panel.Background);
            if (lum >= 0f) BackdropLuminance = lum;
        }
        else if (BackdropContent is Border border && border.Background is not null)
        {
            float lum = TryComputeBrushLuminance(border.Background);
            if (lum >= 0f) BackdropLuminance = lum;
        }
    }

    private static float TryComputeBrushLuminance(Microsoft.UI.Xaml.Media.Brush brush)
    {
        if (brush is Microsoft.UI.Xaml.Media.SolidColorBrush scb)
        {
            var c = scb.Color;
            return (0.299f * c.R + 0.587f * c.G + 0.114f * c.B) / 255f;
        }
        if (brush is Microsoft.UI.Xaml.Media.LinearGradientBrush lgb && lgb.GradientStops.Count > 0)
        {
            float sum = 0f;
            foreach (var stop in lgb.GradientStops)
            {
                var c = stop.Color;
                sum += (0.299f * c.R + 0.587f * c.G + 0.114f * c.B) / 255f;
            }
            return sum / lgb.GradientStops.Count;
        }
        return -1f;
    }

    private static void OnBackdropContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassStage stage)
        {
            stage.UpdateBackdropHost();
            stage.AnalyzeBackdropLuminance();
            stage.ApplyContentTheme();
            stage.InvalidateBackdrop();
        }
    }

    private static void OnContentThemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassStage stage)
        {
            stage.ApplyContentTheme();
        }
    }

    private static void OnBackdropLuminanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassStage stage)
        {
            stage.ApplyContentTheme();
        }
    }

    private static void OnBackdropIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassStage stage)
        {
            stage.State.BindBackdropTarget(
                stage.BackdropId,
                Vector2.Zero,
                new Vector2((float)stage.ViewportSize.Width, (float)stage.ViewportSize.Height));
            stage.InvalidateBackdrop();
        }
    }

    private static void OnViewportSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassStage stage)
        {
            stage.State.BindBackdropTarget(
                stage.BackdropId,
                Vector2.Zero,
                new Vector2((float)stage.ViewportSize.Width, (float)stage.ViewportSize.Height));
            stage.InvalidateBackdrop();
        }
    }

    public void InvalidateBackdrop()
    {
        IsBackdropCaptured = State.IsBackdropCaptured;
        BackdropInvalidated?.Invoke(this, EventArgs.Empty);
    }

    public void MarkBackdropCaptured()
    {
        IsBackdropCaptured = true;
        BackdropCaptured?.Invoke(this, EventArgs.Empty);
    }

    private Border? _backdropHost;
    private ContentPresenter? _contentPresenter;

    public Border? BackdropHost => _backdropHost;
    public ContentPresenter? ContentPresenterChild => _contentPresenter;

    private void UpdateBackdropHost()
    {
        if (_backdropHost != null && _backdropHost.Child != BackdropContent)
        {
            _backdropHost.Child = BackdropContent;
        }
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _backdropHost = GetTemplateChild("PART_BackdropHost") as Border;
        _contentPresenter = GetTemplateChild("PART_ContentPresenter") as ContentPresenter;
        UpdateBackdropHost();
    }
}
