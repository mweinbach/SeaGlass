using System;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Tumbled SeaGlass Prompt & Voice Capsule Bar with voice activation wave.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_PlusButton", Type = typeof(Button))]
[TemplatePart(Name = "PART_PromptInput", Type = typeof(TextBox))]
[TemplatePart(Name = "PART_MicButton", Type = typeof(Button))]
[TemplatePart(Name = "PART_VoiceWaveIndicator", Type = typeof(Border))]
public class SeaGlassPromptBar : SeaGlassSurface
{
    private float _waveScale = 1f;
    private float _waveVel;

    public static readonly DependencyProperty PromptTextProperty =
        DependencyProperty.Register(
            nameof(PromptText),
            typeof(string),
            typeof(SeaGlassPromptBar),
            new PropertyMetadata("Ask Gemini"));

    public static readonly DependencyProperty PlaceholderTextProperty =
        DependencyProperty.Register(
            nameof(PlaceholderText),
            typeof(string),
            typeof(SeaGlassPromptBar),
            new PropertyMetadata("Ask anything…"));

    public static readonly DependencyProperty IsSendingProperty =
        DependencyProperty.Register(
            nameof(IsSending),
            typeof(bool),
            typeof(SeaGlassPromptBar),
            new PropertyMetadata(false));

    public static readonly DependencyProperty IsVoiceActiveProperty =
        DependencyProperty.Register(
            nameof(IsVoiceActive),
            typeof(bool),
            typeof(SeaGlassPromptBar),
            new PropertyMetadata(false));

    public string PromptText
    {
        get => (string)GetValue(PromptTextProperty);
        set => SetValue(PromptTextProperty, value);
    }

    public string PlaceholderText
    {
        get => (string)GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    public bool IsSending
    {
        get => (bool)GetValue(IsSendingProperty);
        set => SetValue(IsSendingProperty, value);
    }

    public bool IsVoiceActive
    {
        get => (bool)GetValue(IsVoiceActiveProperty);
        set => SetValue(IsVoiceActiveProperty, value);
    }

    public Action OnPromptClick { get; set; } = () => { };
    public Action OnPlusClick { get; set; } = () => { };
    public Action OnMicClick { get; set; } = () => { };
    public Action OnVoiceWaveClick { get; set; } = () => { };
    public float WaveScale => _waveScale;
    public SeaGlassMaterial BarMaterial
    {
        get => Material;
        set => Material = value;
    }

    public event EventHandler<string>? SendSubmitted;
    public event EventHandler? VoiceToggled;

    public SeaGlassPromptBar()
    {
        DefaultStyleKey = typeof(SeaGlassPromptBar);
        IsTabStop = true;
        CornerAdaptationMode = SeaGlassCornerAdaptationMode.Fixed;
        ContentRole = SeaGlassContentRole.NavigationChrome;
        SizePx = new Vector2(440f, 62f);
    }

    public SeaGlassPromptBar(
        string promptText = "Ask Gemini",
        Action? onPromptClick = null,
        Action? onPlusClick = null,
        Action? onMicClick = null,
        Action? onVoiceWaveClick = null,
        bool isVoiceActive = false,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? material = null)
        : base(
            stageState: stageState,
            material: (material ?? new SeaGlassMaterial(
                tintColorArgb: unchecked((int)0xFF9AB8FF),
                tintDensity: 0.72f,
                coarseness: 0.58f,
                frostRadiusDp: 28f,
                ior: 1.54f,
                chromaticDispersion: 0.22f,
                rimChalkiness: 0.90f,
                thicknessDp: 18f,
                cornerRadiusDp: 31f,
                cornerSmoothing: 2.5f,
                transparency: 0.52f)) with { CornerRadiusDp = 31f },
            cornerAdaptationMode: SeaGlassCornerAdaptationMode.Fixed,
            contentRole: SeaGlassContentRole.NavigationChrome,
            onClick: onPromptClick)
    {
        DefaultStyleKey = typeof(SeaGlassPromptBar);
        PromptText = promptText;
        IsVoiceActive = isVoiceActive;
        OnPromptClick = onPromptClick ?? (() => { });
        OnPlusClick = onPlusClick ?? (() => { });
        OnMicClick = onMicClick ?? (() => { });
        OnVoiceWaveClick = onVoiceWaveClick ?? (() => { });
        SizePx = new Vector2(440f, 62f);
    }

    public void ToggleVoiceActive()
    {
        IsVoiceActive = !IsVoiceActive;
        OnVoiceWaveClick();
        VoiceToggled?.Invoke(this, EventArgs.Empty);
    }

    public void SubmitSend()
    {
        SendSubmitted?.Invoke(this, PromptText);
    }

    public (int TopArgb, float TopA, int BotArgb, float BotA, float RimA) ComputeMedallionColors()
    {
        int frostedRindArgb = SeaGlassColorMath.ComputeFrostedRindColor(Material.TintColorArgb, Material.Coarseness, Material.RimChalkiness);
        int topArgb = SeaGlassColorMath.LerpOklab(frostedRindArgb, unchecked((int)0xFFFFFFFF), IsVoiceActive ? 0.78f : 0.68f);
        int botArgb = SeaGlassColorMath.LerpOklab(frostedRindArgb, Material.TintColorArgb, 0.24f);
        return (topArgb, 0.88f, botArgb, 0.72f, 0.68f);
    }

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassSpringSpec pulseSpring = new(DampingRatio: 0.66f, Stiffness: 480f);
        pulseSpring.Step(ref _waveScale, ref _waveVel, IsVoiceActive ? 1.07f : 1.0f, dtSeconds);
        return EvaluateFrame(dtSeconds, theme ?? SeaGlassTheme.Current);
    }

    private Grid? _root;
    private Border? _glassHost;
    private Button? _plusButton;
    private TextBox? _promptInput;
    private Button? _micButton;
    private Border? _voiceWaveIndicator;

    public TextBox? PromptInput => _promptInput;
    public Button? PlusButton => _plusButton;
    public Button? MicButton => _micButton;
    public Border? VoiceWaveIndicator => _voiceWaveIndicator;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        if (_plusButton != null) _plusButton.Click -= OnPlusButtonClick;
        if (_micButton != null) _micButton.Click -= OnMicButtonClick;
        if (_promptInput != null)
        {
            _promptInput.TextChanged -= OnPromptInputTextChanged;
            _promptInput.KeyDown -= OnPromptInputKeyDown;
        }
        if (_voiceWaveIndicator != null) _voiceWaveIndicator.PointerPressed -= OnVoiceWavePointerPressed;

        _root = GetTemplateChild("PART_Root") as Grid;
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _plusButton = GetTemplateChild("PART_PlusButton") as Button;
        _promptInput = GetTemplateChild("PART_PromptInput") as TextBox;
        _micButton = GetTemplateChild("PART_MicButton") as Button;
        _voiceWaveIndicator = GetTemplateChild("PART_VoiceWaveIndicator") as Border;

        if (_plusButton != null) _plusButton.Click += OnPlusButtonClick;
        if (_micButton != null) _micButton.Click += OnMicButtonClick;
        if (_promptInput != null)
        {
            _promptInput.Text = PromptText ?? string.Empty;
            _promptInput.PlaceholderText = PlaceholderText ?? string.Empty;
            _promptInput.TextChanged += OnPromptInputTextChanged;
            _promptInput.KeyDown += OnPromptInputKeyDown;
        }
        if (_voiceWaveIndicator != null) _voiceWaveIndicator.PointerPressed += OnVoiceWavePointerPressed;
    }

    private void OnPlusButtonClick(object sender, RoutedEventArgs e) => OnPlusClick();
    private void OnMicButtonClick(object sender, RoutedEventArgs e) => OnMicClick();
    private void OnVoiceWavePointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) => ToggleVoiceActive();

    private void OnPromptInputTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_promptInput != null && PromptText != _promptInput.Text)
        {
            PromptText = _promptInput.Text;
        }
    }

    private void OnPromptInputKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            SubmitSend();
            e.Handled = true;
        }
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new SeaGlass.WinUI.Automation.SeaGlassPromptBarAutomationPeer(this);
}

