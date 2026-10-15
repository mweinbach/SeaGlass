using System;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Frosted SeaGlass TextField with optional label, leading icon, and focus-reactive corner & rim morphing.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_TextBox", Type = typeof(TextBox))]
[TemplatePart(Name = "PART_LabelPresenter", Type = typeof(TextBlock))]
[TemplatePart(Name = "PART_LeadingGlyph", Type = typeof(ContentPresenter))]
[TemplatePart(Name = "PART_TrailingGlyph", Type = typeof(ContentPresenter))]
[TemplatePart(Name = "PART_ErrorMessagePresenter", Type = typeof(TextBlock))]
[TemplatePart(Name = "PART_FocusVisual", Type = typeof(Border))]
public class SeaGlassTextField : SeaGlassSurface
{
    private float _focusProgress;
    private float _focusVel;
    private float _cornerRadius = 18f;
    private float _cornerVel;
    private readonly SeaGlassMaterial _baseMaterial;

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(
            nameof(Value),
            typeof(string),
            typeof(SeaGlassTextField),
            new PropertyMetadata(string.Empty, OnValueChanged));

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(SeaGlassTextField),
            new PropertyMetadata(string.Empty, OnTextChanged));

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(
            nameof(Label),
            typeof(string),
            typeof(SeaGlassTextField),
            new PropertyMetadata(null));

    public static readonly DependencyProperty PlaceholderProperty =
        DependencyProperty.Register(
            nameof(Placeholder),
            typeof(string),
            typeof(SeaGlassTextField),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty LeadingGlyphProperty =
        DependencyProperty.Register(
            nameof(LeadingGlyph),
            typeof(SeaGlassGlyph?),
            typeof(SeaGlassTextField),
            new PropertyMetadata(null));

    public static readonly DependencyProperty HasErrorProperty =
        DependencyProperty.Register(
            nameof(HasError),
            typeof(bool),
            typeof(SeaGlassTextField),
            new PropertyMetadata(false));

    public static readonly DependencyProperty ErrorMessageProperty =
        DependencyProperty.Register(
            nameof(ErrorMessage),
            typeof(string),
            typeof(SeaGlassTextField),
            new PropertyMetadata(null));

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string Text
    {
        get => Value;
        set => Value = value;
    }

    public string? Label
    {
        get => (string?)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public SeaGlassGlyph? LeadingGlyph
    {
        get => (SeaGlassGlyph?)GetValue(LeadingGlyphProperty);
        set => SetValue(LeadingGlyphProperty, value);
    }

    public bool HasError
    {
        get => (bool)GetValue(HasErrorProperty);
        set => SetValue(HasErrorProperty, value);
    }

    public string? ErrorMessage
    {
        get => (string?)GetValue(ErrorMessageProperty);
        set => SetValue(ErrorMessageProperty, value);
    }

    public Action<string> OnValueChange { get; set; } = _ => { };
    public bool IsFocused { get; set; }

    public event EventHandler<string>? TextChanged;

    public SeaGlassTextField()
    {
        DefaultStyleKey = typeof(SeaGlassTextField);
        _baseMaterial = SeaGlassTheme.Current.Palette.SurfaceGlass;
        ContentRole = SeaGlassContentRole.TextAndIcons;
        SizePx = new Vector2(360f, 58f);
        BasePaddingPx = new Vector4(16f, 10f, 16f, 10f);
        IsTabStop = true;
    }

    public SeaGlassTextField(
        string value,
        Action<string>? onValueChange = null,
        SeaGlassStageState? stageState = null,
        string? label = null,
        string placeholder = "",
        SeaGlassGlyph? leadingGlyph = null,
        SeaGlassMaterial? material = null)
        : base(
            stageState: stageState,
            material: material ?? SeaGlassTheme.Current.Palette.SurfaceGlass,
            contentRole: SeaGlassContentRole.TextAndIcons)
    {
        DefaultStyleKey = typeof(SeaGlassTextField);
        _baseMaterial = material ?? SeaGlassTheme.Current.Palette.SurfaceGlass;
        Value = value;
        Text = value;
        OnValueChange = onValueChange ?? (_ => { });
        Label = label;
        Placeholder = placeholder;
        LeadingGlyph = leadingGlyph;
        SizePx = new Vector2(360f, 58f);
        BasePaddingPx = new Vector4(16f, 10f, 16f, 10f);
    }

    public string PlaceholderText
    {
        get => Placeholder;
        set => Placeholder = value;
    }

    public static SeaGlassTextField Create(string placeholder = "", string initialValue = "") =>
        new SeaGlassTextField(initialValue, placeholder: placeholder);

    private Grid? _root;
    private Border? _glassHost;
    private TextBox? _textBox;
    private TextBlock? _labelPresenter;
    private ContentPresenter? _leadingGlyph;
    private ContentPresenter? _trailingGlyph;
    private TextBlock? _errorMessagePresenter;
    private Border? _focusVisual;

    public TextBox? TextBox => _textBox;
    public Border? GlassHost => _glassHost;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        if (_textBox != null)
        {
            _textBox.TextChanged -= OnInnerTextBoxTextChanged;
            _textBox.GotFocus -= OnInnerTextBoxGotFocus;
            _textBox.LostFocus -= OnInnerTextBoxLostFocus;
        }

        _root = GetTemplateChild("PART_Root") as Grid;
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _textBox = GetTemplateChild("PART_TextBox") as TextBox;
        _labelPresenter = GetTemplateChild("PART_LabelPresenter") as TextBlock;
        _leadingGlyph = GetTemplateChild("PART_LeadingGlyph") as ContentPresenter;
        _trailingGlyph = GetTemplateChild("PART_TrailingGlyph") as ContentPresenter;
        _errorMessagePresenter = GetTemplateChild("PART_ErrorMessagePresenter") as TextBlock;
        _focusVisual = GetTemplateChild("PART_FocusVisual") as Border;

        if (_textBox != null)
        {
            _textBox.Text = Value ?? string.Empty;
            _textBox.TextChanged += OnInnerTextBoxTextChanged;
            _textBox.GotFocus += OnInnerTextBoxGotFocus;
            _textBox.LostFocus += OnInnerTextBoxLostFocus;
        }

        UpdateVisualState(false);
    }

    private void OnInnerTextBoxTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_textBox != null && Value != _textBox.Text)
        {
            Value = _textBox.Text;
            Text = _textBox.Text;
        }
    }

    private void OnInnerTextBoxGotFocus(object sender, RoutedEventArgs e)
    {
        IsFocused = true;
        if (_focusVisual != null) _focusVisual.Visibility = Visibility.Visible;
        VisualStateManager.GoToState(this, "Focused", true);
    }

    private void OnInnerTextBoxLostFocus(object sender, RoutedEventArgs e)
    {
        IsFocused = false;
        if (_focusVisual != null) _focusVisual.Visibility = Visibility.Collapsed;
        VisualStateManager.GoToState(this, "Normal", true);
    }

    private void UpdateVisualState(bool useTransitions)
    {
        if (HasError)
        {
            VisualStateManager.GoToState(this, "HasError", useTransitions);
        }
        else
        {
            VisualStateManager.GoToState(this, "NoError", useTransitions);
        }

        if (IsFocused)
        {
            VisualStateManager.GoToState(this, "Focused", useTransitions);
        }
        else
        {
            VisualStateManager.GoToState(this, "Normal", useTransitions);
        }
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassTextField tf && e.NewValue is string s)
        {
            if (tf._textBox != null && tf._textBox.Text != s)
            {
                tf._textBox.Text = s;
            }
            if ((string)tf.GetValue(TextProperty) != s)
            {
                tf.SetValue(TextProperty, s);
            }
            tf.OnValueChange?.Invoke(s);
            tf.TextChanged?.Invoke(tf, s);
        }
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassTextField tf && e.NewValue is string s)
        {
            if ((string)tf.GetValue(ValueProperty) != s)
            {
                tf.SetValue(ValueProperty, s);
            }
        }
    }

    public void SetValue(string newValue)
    {
        Value = newValue ?? string.Empty;
        Text = Value;
    }

    public SeaGlassSurfaceFrameState StepFocusAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        activeTheme.MotionScheme.FastEffects.Step(ref _focusProgress, ref _focusVel, IsFocused ? 1f : 0f, dtSeconds);
        activeTheme.MotionScheme.FastSpatial.Step(ref _cornerRadius, ref _cornerVel, IsFocused ? 24f : 18f, dtSeconds);

        float fp = Math.Clamp(_focusProgress, 0f, 1f);
        Material = _baseMaterial with
        {
            CornerRadiusDp = MathF.Max(8f, _cornerRadius),
            ThicknessDp = 14f + 4f * fp,
            RimChalkiness = MathF.Min(0.98f, _baseMaterial.RimChalkiness + 0.18f * fp)
        };

        return EvaluateFrame(dtSeconds, activeTheme);
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new Automation.SeaGlassTextFieldAutomationPeer(this);
}
