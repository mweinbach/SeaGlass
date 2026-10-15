using System;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Interactive Liquid SeaGlass Search Bar with focus-reactive rim brightening, live text editing,
/// and instant clear button.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_LeadingGlyph", Type = typeof(ContentPresenter))]
[TemplatePart(Name = "PART_TextBox", Type = typeof(TextBox))]
[TemplatePart(Name = "PART_ClearButton", Type = typeof(Button))]
[TemplatePart(Name = "PART_TrailingButton", Type = typeof(Button))]
public class SeaGlassSearchBar : SeaGlassSurface
{
    private float _focusProgress;
    private float _focusVel;
    private readonly SeaGlassMaterial _baseMaterial;

    public static readonly DependencyProperty QueryProperty =
        DependencyProperty.Register(
            nameof(Query),
            typeof(string),
            typeof(SeaGlassSearchBar),
            new PropertyMetadata(string.Empty, OnQueryChanged));

    public static readonly DependencyProperty QueryTextProperty =
        DependencyProperty.Register(
            nameof(QueryText),
            typeof(string),
            typeof(SeaGlassSearchBar),
            new PropertyMetadata(string.Empty, OnQueryTextChanged));

    public static readonly DependencyProperty PlaceholderProperty =
        DependencyProperty.Register(
            nameof(Placeholder),
            typeof(string),
            typeof(SeaGlassSearchBar),
            new PropertyMetadata("Search glass components, shaders, palettes…"));

    public static readonly DependencyProperty PlaceholderTextProperty =
        DependencyProperty.Register(
            nameof(PlaceholderText),
            typeof(string),
            typeof(SeaGlassSearchBar),
            new PropertyMetadata("Search glass components, shaders, palettes…"));

    public static readonly DependencyProperty TrailingGlyphProperty =
        DependencyProperty.Register(
            nameof(TrailingGlyph),
            typeof(SeaGlassGlyph?),
            typeof(SeaGlassSearchBar),
            new PropertyMetadata(SeaGlassGlyph.Tune));

    public static readonly DependencyProperty IsSearchActiveProperty =
        DependencyProperty.Register(
            nameof(IsSearchActive),
            typeof(bool),
            typeof(SeaGlassSearchBar),
            new PropertyMetadata(false));

    public string Query
    {
        get => (string)GetValue(QueryProperty);
        set => SetValue(QueryProperty, value);
    }

    public string QueryText
    {
        get => (string)GetValue(QueryTextProperty);
        set => SetValue(QueryTextProperty, value);
    }

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public string PlaceholderText
    {
        get => (string)GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    public SeaGlassGlyph? TrailingGlyph
    {
        get => (SeaGlassGlyph?)GetValue(TrailingGlyphProperty);
        set => SetValue(TrailingGlyphProperty, value);
    }

    public bool IsSearchActive
    {
        get => (bool)GetValue(IsSearchActiveProperty);
        set => SetValue(IsSearchActiveProperty, value);
    }

    public Action<string> OnQueryChange { get; set; } = _ => { };
    public Action? OnTrailingClick { get; set; }
    public bool IsFocused { get; set; }
    public float FocusProgress => _focusProgress;

    public event EventHandler<string>? QuerySubmitted;

    public SeaGlassSearchBar()
    {
        DefaultStyleKey = typeof(SeaGlassSearchBar);
        _baseMaterial = SeaGlassTheme.Current.Palette.SurfaceGlass;
        ContentRole = SeaGlassContentRole.NavigationChrome;
        SizePx = new Vector2(460f, 52f);
        BasePaddingPx = new Vector4(16f, 0f, 16f, 0f);
        IsTabStop = true;
    }

    public SeaGlassSearchBar(
        string query,
        Action<string>? onQueryChange = null,
        SeaGlassStageState? stageState = null,
        string placeholder = "Search glass components, shaders, palettes…",
        SeaGlassMaterial? material = null,
        SeaGlassGlyph? trailingGlyph = SeaGlassGlyph.Tune,
        Action? onTrailingClick = null)
        : base(
            stageState: stageState,
            material: material ?? SeaGlassTheme.Current.Palette.SurfaceGlass,
            contentRole: SeaGlassContentRole.NavigationChrome)
    {
        DefaultStyleKey = typeof(SeaGlassSearchBar);
        _baseMaterial = material ?? SeaGlassTheme.Current.Palette.SurfaceGlass;
        Query = query;
        QueryText = query;
        OnQueryChange = onQueryChange ?? (_ => { });
        Placeholder = placeholder;
        PlaceholderText = placeholder;
        TrailingGlyph = trailingGlyph;
        OnTrailingClick = onTrailingClick;
        SizePx = new Vector2(460f, 52f);
        BasePaddingPx = new Vector4(16f, 0f, 16f, 0f);
    }

    private static void OnQueryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassSearchBar bar && e.NewValue is string q)
        {
            if (bar.QueryText != q)
            {
                bar.QueryText = q;
            }
            if (bar._textBox != null && bar._textBox.Text != q)
            {
                bar._textBox.Text = q;
            }
            bar.OnQueryChange?.Invoke(q);
        }
    }

    private static void OnQueryTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassSearchBar bar && e.NewValue is string q)
        {
            if (bar.Query != q)
            {
                bar.Query = q;
            }
        }
    }

    public void SetQuery(string query)
    {
        var text = query ?? string.Empty;
        Query = text;
        QueryText = text;
        if (_textBox != null && _textBox.Text != text)
        {
            _textBox.Text = text;
        }
        OnQueryChange?.Invoke(text);
    }

    public void ClearQuery() => SetQuery(string.Empty);

    public void SubmitQuery()
    {
        QuerySubmitted?.Invoke(this, Query);
    }

    public SeaGlassSurfaceFrameState StepFocusAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        activeTheme.MotionScheme.FastEffects.Step(ref _focusProgress, ref _focusVel, IsFocused ? 1f : 0f, dtSeconds);
        float fp = Math.Clamp(_focusProgress, 0f, 1f);

        Material = _baseMaterial with
        {
            CornerRadiusDp = 28f,
            ThicknessDp = 16f + 4f * fp,
            RimChalkiness = MathF.Min(0.98f, _baseMaterial.RimChalkiness + 0.16f * fp),
            TintDensity = MathF.Min(0.92f, _baseMaterial.TintDensity + 0.08f * fp)
        };

        return EvaluateFrame(dtSeconds, activeTheme);
    }

    private Grid? _root;
    private Border? _glassHost;
    private ContentPresenter? _leadingGlyph;
    private TextBox? _textBox;
    private Button? _clearButton;
    private Button? _trailingButton;

    public TextBox? TextBox => _textBox;
    public Button? ClearButton => _clearButton;
    public Button? TrailingButton => _trailingButton;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        if (_textBox != null)
        {
            _textBox.TextChanged -= OnInnerTextBoxTextChanged;
            _textBox.KeyDown -= OnInnerTextBoxKeyDown;
        }
        if (_clearButton != null) _clearButton.Click -= OnClearButtonClick;
        if (_trailingButton != null) _trailingButton.Click -= OnTrailingButtonClick;

        _root = GetTemplateChild("PART_Root") as Grid;
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _leadingGlyph = GetTemplateChild("PART_LeadingGlyph") as ContentPresenter;
        _textBox = GetTemplateChild("PART_TextBox") as TextBox;
        _clearButton = GetTemplateChild("PART_ClearButton") as Button;
        _trailingButton = GetTemplateChild("PART_TrailingButton") as Button;

        if (_textBox != null)
        {
            _textBox.Text = Query ?? string.Empty;
            _textBox.PlaceholderText = Placeholder ?? string.Empty;
            _textBox.TextChanged += OnInnerTextBoxTextChanged;
            _textBox.KeyDown += OnInnerTextBoxKeyDown;
        }
        if (_clearButton != null) _clearButton.Click += OnClearButtonClick;
        if (_trailingButton != null) _trailingButton.Click += OnTrailingButtonClick;
    }

    private void OnInnerTextBoxTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_textBox != null)
        {
            var text = _textBox.Text ?? string.Empty;
            if (Query != text)
            {
                SetQuery(text);
            }
        }
    }

    private void OnInnerTextBoxKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            SubmitQuery();
            e.Handled = true;
        }
    }

    private void OnClearButtonClick(object sender, RoutedEventArgs e) => ClearQuery();
    private void OnTrailingButtonClick(object sender, RoutedEventArgs e) => OnTrailingClick?.Invoke();

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new Automation.SeaGlassSearchBarAutomationPeer(this);
}

