using System;
using System.Collections.Generic;
using System.Numerics;
using ICommand = global::System.Windows.Input.ICommand;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SeaGlass.Core;
using SeaGlass.Core.Backend;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Hosting;
using SeaGlass.WinUI.Rendering;

namespace SeaGlass.WinUI;

/// <summary>
/// Foundational lookless SeaGlass container surface for WinUI 3 with Material 3 Expressive state morphing,
/// adaptive pointer 3D perspective tilt, HDR caustic flare, concentric corner adaptation,
/// and automatic Oklab contrast-safe foreground content color.
///
/// Base class for all SeaGlass lookless controls.
/// </summary>
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_ContentPresenter", Type = typeof(ContentPresenter))]
[TemplatePart(Name = "PART_FocusVisual", Type = typeof(Border))]
public class SeaGlassSurface : ContentControl
{
    private Border? _glassHost;
    private ContentPresenter? _contentPresenter;
    private Border? _focusVisual;
    private ContainerVisual? _glassContainerVisual;
    private SpriteVisual? _blurVisual;
    private SpriteVisual? _grainVisual;
    private CompositionGeometricClip? _geometricClip;
    private CompositionRoundedRectangleGeometry? _roundedRectGeometry;
    private CompositionEffectBrush? _blurEffectBrush;
    private CompositionColorBrush? _tintColorBrush;

    private readonly Microsoft.UI.Xaml.Media.SolidColorBrush _transparentBrush = new(Microsoft.UI.Colors.Transparent);
    private readonly Microsoft.UI.Xaml.Media.SolidColorBrush _rimBorderBrush = new(Microsoft.UI.Colors.Transparent);
    private readonly Microsoft.UI.Xaml.Media.SolidColorBrush _fallbackTintBrush = new(Microsoft.UI.Colors.Transparent);

    public SeaGlassDeviceTier ActiveRenderTier => SeaGlassTieredRenderEngine.Instance.ActiveTier;
    public ContainerVisual? GlassContainerVisual => _glassContainerVisual;
    public SpriteVisual? BlurVisual => _blurVisual;
    public SpriteVisual? GrainVisual => _grainVisual;
    public CompositionEffectBrush? GlassEffectBrush => _blurEffectBrush;
    public Microsoft.UI.Xaml.Media.SolidColorBrush RimBorderBrush => _rimBorderBrush;


    private SeaGlassMaterialAnimator _materialAnimator;
    private readonly SeaGlassUniformBlock _uniformBlock = new();
    private readonly SeaGlassVectorPath2D _contourPath = new();
    private readonly SeaGlassSdfEngine.SdfNode[] _singleNodeBuffer = new SeaGlassSdfEngine.SdfNode[1];
    private float _pressFraction;
    private float _pressVelocity;
    private float _tiltRotX, _tiltVelX;
    private float _tiltRotY, _tiltVelY;
    private readonly SeaGlassMaterial _pressedMaterialInstance = new(0);

    private SeaGlassMaterial GetPressedMaterial(float press)
    {
        if (press <= 0.0001f)
            return Material;

        Material.PressedInto(press, _pressedMaterialInstance);
        return _pressedMaterialInstance;
    }

    #region Universal Dependency Properties

    public static readonly DependencyProperty VariantProperty =
        DependencyProperty.Register(
            nameof(Variant),
            typeof(SeaGlassVariant),
            typeof(SeaGlassSurface),
            new PropertyMetadata(SeaGlassVariant.Primary, OnVariantChanged));

    public static readonly DependencyProperty SizeProperty =
        DependencyProperty.Register(
            nameof(Size),
            typeof(SeaGlassControlSize),
            typeof(SeaGlassSurface),
            new PropertyMetadata(SeaGlassControlSize.Md, OnSizeTokenChanged));

    public static readonly DependencyProperty ToneProperty =
        DependencyProperty.Register(
            nameof(Tone),
            typeof(SeaGlassTone),
            typeof(SeaGlassSurface),
            new PropertyMetadata(SeaGlassTone.SeafoamMint, OnToneChanged));

    public static readonly DependencyProperty MaterialProperty =
        DependencyProperty.Register(
            nameof(Material),
            typeof(SeaGlassMaterial),
            typeof(SeaGlassSurface),
            new PropertyMetadata(null, OnMaterialPropertyChanged));

    public static readonly DependencyProperty TintProperty =
        DependencyProperty.Register(
            nameof(Tint),
            typeof(Windows.UI.Color?),
            typeof(SeaGlassSurface),
            new PropertyMetadata(null, OnTintPropertyChanged));

    public static readonly DependencyProperty CoarsenessProperty =
        DependencyProperty.Register(
            nameof(Coarseness),
            typeof(double),
            typeof(SeaGlassSurface),
            new PropertyMetadata(0.42, OnOpticalParameterChanged));

    public static readonly DependencyProperty TintDensityProperty =
        DependencyProperty.Register(
            nameof(TintDensity),
            typeof(double),
            typeof(SeaGlassSurface),
            new PropertyMetadata(0.68, OnOpticalParameterChanged));

    public static readonly DependencyProperty ThicknessDpProperty =
        DependencyProperty.Register(
            nameof(ThicknessDp),
            typeof(double),
            typeof(SeaGlassSurface),
            new PropertyMetadata(16.0, OnOpticalParameterChanged));

    public static readonly DependencyProperty IorProperty =
        DependencyProperty.Register(
            nameof(Ior),
            typeof(double),
            typeof(SeaGlassSurface),
            new PropertyMetadata(1.54, OnOpticalParameterChanged));

    public static readonly DependencyProperty RimChalkinessProperty =
        DependencyProperty.Register(
            nameof(RimChalkiness),
            typeof(double),
            typeof(SeaGlassSurface),
            new PropertyMetadata(0.78, OnOpticalParameterChanged));

    public static readonly DependencyProperty CornerAdaptationModeProperty =
        DependencyProperty.Register(
            nameof(CornerAdaptationMode),
            typeof(SeaGlassCornerAdaptationMode),
            typeof(SeaGlassSurface),
            new PropertyMetadata(SeaGlassCornerAdaptationMode.ConcentricProximity));

    public static readonly DependencyProperty CornerMaskProperty =
        DependencyProperty.Register(
            nameof(CornerMask),
            typeof(SeaGlassCornerMask),
            typeof(SeaGlassSurface),
            new PropertyMetadata(SeaGlassCornerMask.All));

    public static readonly DependencyProperty CornerProximityThresholdDpProperty =
        DependencyProperty.Register(
            nameof(CornerProximityThresholdDp),
            typeof(float),
            typeof(SeaGlassSurface),
            new PropertyMetadata(36f));

    public static readonly DependencyProperty CornerFullMatchDistanceDpProperty =
        DependencyProperty.Register(
            nameof(CornerFullMatchDistanceDp),
            typeof(float),
            typeof(SeaGlassSurface),
            new PropertyMetadata(6f));

    public static readonly DependencyProperty CornerAdaptationStrengthProperty =
        DependencyProperty.Register(
            nameof(CornerAdaptationStrength),
            typeof(float),
            typeof(SeaGlassSurface),
            new PropertyMetadata(1.0f));

    public static readonly DependencyProperty AllowAsymmetricCornersProperty =
        DependencyProperty.Register(
            nameof(AllowAsymmetricCorners),
            typeof(bool),
            typeof(SeaGlassSurface),
            new PropertyMetadata(true));

    public static readonly DependencyProperty AdaptToParentContainerProperty =
        DependencyProperty.Register(
            nameof(AdaptToParentContainer),
            typeof(bool),
            typeof(SeaGlassSurface),
            new PropertyMetadata(true));

    public static readonly DependencyProperty ContentRoleProperty =
        DependencyProperty.Register(
            nameof(ContentRole),
            typeof(SeaGlassContentRole),
            typeof(SeaGlassSurface),
            new PropertyMetadata(SeaGlassContentRole.TextAndIcons));

    public static readonly DependencyProperty TiltEnabledProperty =
        DependencyProperty.Register(
            nameof(TiltEnabled),
            typeof(bool),
            typeof(SeaGlassSurface),
            new PropertyMetadata(true));

    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(
            nameof(Command),
            typeof(ICommand),
            typeof(SeaGlassSurface),
            new PropertyMetadata(null));

    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.Register(
            nameof(CommandParameter),
            typeof(object),
            typeof(SeaGlassSurface),
            new PropertyMetadata(null));

    #endregion

    #region Properties

    public SeaGlassVariant Variant
    {
        get => (SeaGlassVariant)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public SeaGlassControlSize Size
    {
        get => (SeaGlassControlSize)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public SeaGlassTone Tone
    {
        get => (SeaGlassTone)GetValue(ToneProperty);
        set => SetValue(ToneProperty, value);
    }

    public SeaGlassMaterial Material
    {
        get => (SeaGlassMaterial)(GetValue(MaterialProperty) ?? SeaGlassToneExtensions.ResolveMaterial(Tone, Variant));
        set => SetValue(MaterialProperty, value);
    }

    public Windows.UI.Color? Tint
    {
        get => (Windows.UI.Color?)GetValue(TintProperty);
        set => SetValue(TintProperty, value);
    }

    public double Coarseness
    {
        get => (double)GetValue(CoarsenessProperty);
        set => SetValue(CoarsenessProperty, value);
    }

    public double TintDensity
    {
        get => (double)GetValue(TintDensityProperty);
        set => SetValue(TintDensityProperty, value);
    }

    public double ThicknessDp
    {
        get => (double)GetValue(ThicknessDpProperty);
        set => SetValue(ThicknessDpProperty, value);
    }

    public double Ior
    {
        get => (double)GetValue(IorProperty);
        set => SetValue(IorProperty, value);
    }

    public double RimChalkiness
    {
        get => (double)GetValue(RimChalkinessProperty);
        set => SetValue(RimChalkinessProperty, value);
    }

    public SeaGlassCornerAdaptationMode CornerAdaptationMode
    {
        get => (SeaGlassCornerAdaptationMode)GetValue(CornerAdaptationModeProperty);
        set => SetValue(CornerAdaptationModeProperty, value);
    }

    public SeaGlassCornerMask CornerMask
    {
        get => (SeaGlassCornerMask)GetValue(CornerMaskProperty);
        set => SetValue(CornerMaskProperty, value);
    }

    public float CornerProximityThresholdDp
    {
        get => (float)GetValue(CornerProximityThresholdDpProperty);
        set => SetValue(CornerProximityThresholdDpProperty, value);
    }

    public float CornerFullMatchDistanceDp
    {
        get => (float)GetValue(CornerFullMatchDistanceDpProperty);
        set => SetValue(CornerFullMatchDistanceDpProperty, value);
    }

    public float CornerAdaptationStrength
    {
        get => (float)GetValue(CornerAdaptationStrengthProperty);
        set => SetValue(CornerAdaptationStrengthProperty, value);
    }

    public bool AllowAsymmetricCorners
    {
        get => (bool)GetValue(AllowAsymmetricCornersProperty);
        set => SetValue(AllowAsymmetricCornersProperty, value);
    }

    public bool AdaptToParentContainer
    {
        get => (bool)GetValue(AdaptToParentContainerProperty);
        set => SetValue(AdaptToParentContainerProperty, value);
    }

    public SeaGlassContentRole ContentRole
    {
        get => (SeaGlassContentRole)GetValue(ContentRoleProperty);
        set => SetValue(ContentRoleProperty, value);
    }

    public bool TiltEnabled
    {
        get => (bool)GetValue(TiltEnabledProperty);
        set => SetValue(TiltEnabledProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public SeaGlassStageState? StageState { get; set; }
    public Vector2 OriginInRoot { get; set; }
    public Vector2 SizePx { get; set; } = new(320f, 140f);
    public Vector4 BasePaddingPx { get; set; } = new(16f, 12f, 16f, 12f);
    public Vector2? LastTouchOffsetInLocal { get; private set; }
    public bool IsPointerDown { get; private set; }
    public Action? OnClick { get; set; }

    public event RoutedEventHandler? Click;

    #endregion

    public SeaGlassSurface()
    {
        DefaultStyleKey = typeof(SeaGlassSurface);
        SeaGlassMaterial initial = SeaGlassToneExtensions.ResolveMaterial(Tone, Variant);
        _materialAnimator = new SeaGlassMaterialAnimator(initial);
    }

    public SeaGlassSurface(
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? material = null,
        SeaGlassCornerAdaptationMode cornerAdaptationMode = SeaGlassCornerAdaptationMode.ConcentricProximity,
        SeaGlassContentRole contentRole = SeaGlassContentRole.TextAndIcons,
        Action? onClick = null,
        SeaGlassCornerMask? cornerMask = null,
        float cornerProximityThresholdDp = 36f,
        float cornerFullMatchDistanceDp = 6f,
        float cornerAdaptationStrength = 1.0f,
        bool allowAsymmetricCorners = true,
        bool adaptToParentContainer = true)
        : this()
    {
        StageState = stageState ?? SeaGlassStageHost.AmbientStageState;
        if (material != null) Material = material;
        CornerAdaptationMode = cornerAdaptationMode;
        CornerMask = cornerMask ?? SeaGlassCornerMask.All;
        CornerProximityThresholdDp = cornerProximityThresholdDp;
        CornerFullMatchDistanceDp = cornerFullMatchDistanceDp;
        CornerAdaptationStrength = cornerAdaptationStrength;
        AllowAsymmetricCorners = allowAsymmetricCorners;
        AdaptToParentContainer = adaptToParentContainer;
        ContentRole = contentRole;
        OnClick = onClick;
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _contentPresenter = GetTemplateChild("PART_ContentPresenter") as ContentPresenter;
        _focusVisual = GetTemplateChild("PART_FocusVisual") as Border;

        if (_glassHost != null)
        {
            _glassHost.BorderBrush = _rimBorderBrush;
            _glassHost.Background = _transparentBrush;
            _glassHost.Loaded -= OnGlassHostLoaded;
            _glassHost.Loaded += OnGlassHostLoaded;
            _glassHost.SizeChanged -= OnGlassHostSizeChanged;
            _glassHost.SizeChanged += OnGlassHostSizeChanged;
            if (_glassHost.IsLoaded)
            {
                AttachGlassCompositionVisual(_glassHost);
            }
        }

        ActualThemeChanged -= OnSurfaceActualThemeChanged;
        ActualThemeChanged += OnSurfaceActualThemeChanged;

        if (ReadLocalValue(CornerRadiusProperty) != DependencyProperty.UnsetValue && _glassHost != null)
        {
            _glassHost.CornerRadius = CornerRadius;
        }

        InheritAmbientThemeProperties();
        UpdateVisualStates(false);
    }

    private void OnGlassHostLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is Border host)
        {
            AttachGlassCompositionVisual(host);
        }
    }

    private void OnGlassHostSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is Border host)
        {
            UpdateGlassCompositionVisualSize(host);
        }
    }

    public void AttachGlassCompositionVisual(Border host)
    {
        try
        {
            var hostVisual = ElementCompositionPreview.GetElementVisual(host);
            var compositor = hostVisual.Compositor;

            _glassHost ??= host;
            host.Background = _transparentBrush;
            host.BorderBrush = _rimBorderBrush;


            if (_glassContainerVisual == null)
            {
                _glassContainerVisual = compositor.CreateContainerVisual();

                // 1. Squircle / Corner Shape Geometric Clip (Rule 4)
                _roundedRectGeometry = compositor.CreateRoundedRectangleGeometry();
                _geometricClip = compositor.CreateGeometricClip(_roundedRectGeometry);
                _glassContainerVisual.Clip = _geometricClip;

                // 2. Hardware Composition Backdrop Blur + Beer-Lambert Volumetric Absorption (Rule 1)
                try
                {
                    var effectFactory = CompositionHybridRenderPipeline.CreateGlassEffectFactory(compositor);
                    _blurEffectBrush = effectFactory.CreateBrush();
                    _blurEffectBrush.SetSourceParameter("Backdrop", compositor.CreateBackdropBrush());
                    _tintColorBrush = compositor.CreateColorBrush();
                    _blurEffectBrush.SetSourceParameter("TintSource", _tintColorBrush);

                    _blurVisual = compositor.CreateSpriteVisual();
                    _blurVisual.Brush = _blurEffectBrush;
                    _glassContainerVisual.Children.InsertAtBottom(_blurVisual);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SeaGlass] Composition effect factory: {ex.Message}");
                }

                // 3. Chalky Sand-Grain Micro-Facet Texture Overlay (Rule 3)
                try
                {
                    var grainBrush = CompositionHybridRenderPipeline.GetOrCreateSandGrainBrush(compositor);
                    if (grainBrush != null)
                    {
                        _grainVisual = compositor.CreateSpriteVisual();
                        _grainVisual.Brush = grainBrush;
                        _grainVisual.Opacity = Math.Clamp((float)Coarseness * 0.15f, 0f, 0.25f);
                        _glassContainerVisual.Children.InsertAtTop(_grainVisual);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SeaGlass] Sand grain visual attach: {ex.Message}");
                }

                ElementCompositionPreview.SetElementChildVisual(host, _glassContainerVisual);
            }

            UpdateGlassCompositionVisualSize(host);
            UpdateGlassCompositionParameters();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SeaGlass] Composition visual attach: {ex.Message}");
        }
    }

    private void UpdateGlassCompositionVisualSize(Border host)
    {
        if (_glassContainerVisual == null) return;
        float w = (float)host.ActualWidth;
        float h = (float)host.ActualHeight;
        if ((w <= 0 || float.IsNaN(w)) && host.Width > 0 && !double.IsNaN(host.Width))
        {
            w = (float)host.Width;
        }
        if ((h <= 0 || float.IsNaN(h)) && host.Height > 0 && !double.IsNaN(host.Height))
        {
            h = (float)host.Height;
        }
        if (w > 0 && h > 0)
        {
            Vector2 size = new(w, h);
            _glassContainerVisual.Size = size;
            if (_blurVisual != null) _blurVisual.Size = size;
            if (_grainVisual != null) _grainVisual.Size = size;
            if (_roundedRectGeometry != null)
            {
                _roundedRectGeometry.Size = size;
                float cr = (float)Math.Max(host.CornerRadius.TopLeft, CornerRadius.TopLeft);
                _roundedRectGeometry.CornerRadius = new Vector2(cr, cr);
            }
        }
    }

    public void UpdateGlassCompositionParameters()
    {
        bool isLightweight = ActiveRenderTier == SeaGlassDeviceTier.LightweightTint;
        if (_blurVisual != null)
        {
            _blurVisual.IsVisible = !isLightweight;
        }
        if (_grainVisual != null)
        {
            _grainVisual.IsVisible = !isLightweight;
        }

        int tintArgb = Material?.TintColorArgb ?? Tone.ToPalette().PrimaryGlass.TintColorArgb;
        float coarsenessVal = (float)Coarseness;
        float thicknessVal = (float)ThicknessDp;
        float densityVal = (float)TintDensity;

        if (isLightweight)
        {
            if (_glassHost != null)
            {
                byte fr = (byte)((tintArgb >> 16) & 0xFF);
                byte fg = (byte)((tintArgb >> 8) & 0xFF);
                byte fb = (byte)(tintArgb & 0xFF);
                byte fa = (byte)Math.Clamp((int)(densityVal * 120f + 40f), 40, 200);
                _fallbackTintBrush.Color = Windows.UI.Color.FromArgb(fa, fr, fg, fb);
                _glassHost.Background = _fallbackTintBrush;
            }
        }
        else
        {
            if (_glassHost != null && _glassHost.Background != _transparentBrush)
            {
                _glassHost.Background = _transparentBrush;
            }
        }

        // Automatic Oklab contrast-safe foreground content theme adaptation:
        // When glass tint is dark (Oklab L < 0.48, e.g. ObsidianTide), adapt requested theme
        // to Dark if not explicitly overridden so text is bright, contrast-safe white.
        var tintOklab = SeaGlassColorMath.ArgbToOklab(tintArgb);
        if (tintOklab.L < 0.48f && RequestedTheme == ElementTheme.Default)
        {
            RequestedTheme = ElementTheme.Dark;
        }

        bool isLight = (ActualTheme == ElementTheme.Light) || (RequestedTheme == ElementTheme.Light);

        // 1. Compute Beer-Lambert spectral absorption & transmittance (Rule 1)
        float depthFactor = isLight
            ? (0.35f + 0.20f * Math.Clamp(thicknessVal / 24f, 0.05f, 2.0f))
            : (0.65f + 0.35f * Math.Clamp(thicknessVal / 24f, 0.05f, 2.5f));
        float opticalDepth = densityVal * depthFactor;
        float absorbExp = isLight ? 0.75f : 1.35f;

        float tintR = MathF.Max(0.04f, SeaGlassColorMath.SrgbChannelToLinear((tintArgb >> 16) & 0xFF));
        float tintG = MathF.Max(0.04f, SeaGlassColorMath.SrgbChannelToLinear((tintArgb >> 8) & 0xFF));
        float tintB = MathF.Max(0.04f, SeaGlassColorMath.SrgbChannelToLinear(tintArgb & 0xFF));

        float absorbR = MathF.Exp(MathF.Log(tintR) * opticalDepth * absorbExp);
        float absorbG = MathF.Exp(MathF.Log(tintG) * opticalDepth * absorbExp);
        float absorbB = MathF.Exp(MathF.Log(tintB) * opticalDepth * absorbExp);

        byte trSrgb = (byte)SeaGlassColorMath.LinearChannelToSrgb(absorbR);
        byte tgSrgb = (byte)SeaGlassColorMath.LinearChannelToSrgb(absorbG);
        byte tbSrgb = (byte)SeaGlassColorMath.LinearChannelToSrgb(absorbB);

        if (_tintColorBrush != null)
        {
            _tintColorBrush.Color = Windows.UI.Color.FromArgb(255, trSrgb, tgSrgb, tbSrgb);
        }

        float rawBlur = isLight
            ? Math.Clamp(thicknessVal * 0.75f + coarsenessVal * 12f, 4f, 20f)
            : MathF.Max(6f, thicknessVal * 1.5f + coarsenessVal * 20f);
        float quantizedBlur = SeaGlassShaderProgramCache.ObtainQuantizedBlurRadiusPx(rawBlur);

        float velvetScatter = isLight
            ? Math.Clamp(densityVal * 0.05f + coarsenessVal * 0.05f, 0.02f, 0.08f)
            : Math.Clamp(densityVal * 0.15f + coarsenessVal * 0.15f, 0.04f, 0.20f);
        float multiplyAmount = isLight
            ? Math.Clamp(0.88f - velvetScatter, 0.80f, 0.88f)
            : Math.Clamp(0.85f - velvetScatter, 0.65f, 0.85f);
        float scatterAmount = velvetScatter;
        float preserveAmount = isLight
            ? Math.Clamp(0.24f * (1.0f - densityVal * 0.3f), 0.18f, 0.30f)
            : Math.Clamp(0.18f * (1.0f - densityVal * 0.5f), 0.10f, 0.25f);

        if (_blurEffectBrush != null)
        {
            try
            {
                _blurEffectBrush.Properties.InsertScalar("Blur.BlurAmount", quantizedBlur);
                _blurEffectBrush.Properties.InsertScalar("BeerLambert.MultiplyAmount", multiplyAmount);
                _blurEffectBrush.Properties.InsertScalar("BeerLambert.Source1Amount", preserveAmount);
                _blurEffectBrush.Properties.InsertScalar("BeerLambert.Source2Amount", scatterAmount);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SeaGlass] BlurEffectBrush properties: {ex.Message}");
            }
        }

        // 2. Sand-grain micro-facet coarseness modulation (Rule 3)
        if (_grainVisual != null)
        {
            _grainVisual.Opacity = Math.Clamp(coarsenessVal * 0.15f, 0f, 0.25f);
        }

        // 3. Chalky rough-Fresnel rim illumination with zero heap allocations (Rules 2 & 3)
        float chalkiness = (float)RimChalkiness;
        byte baseR = (byte)((tintArgb >> 16) & 0xFF);
        byte baseG = (byte)((tintArgb >> 8) & 0xFF);
        byte baseB = (byte)(tintArgb & 0xFF);

        float chalkLift = Math.Clamp(chalkiness * 0.55f, 0f, 0.75f);
        byte rimR = (byte)Math.Clamp((int)(baseR * (1f - chalkLift) + 255f * chalkLift), 0, 255);
        byte rimG = (byte)Math.Clamp((int)(baseG * (1f - chalkLift) + 255f * chalkLift), 0, 255);
        byte rimB = (byte)Math.Clamp((int)(baseB * (1f - chalkLift) + 255f * chalkLift), 0, 255);
        byte rimA = (byte)Math.Clamp((int)(chalkiness * 140f + 80f), 80, 245);

        _rimBorderBrush.Color = Windows.UI.Color.FromArgb(rimA, rimR, rimG, rimB);
        if (_glassHost != null)
        {
            _glassHost.BorderThickness = new Thickness(1.5);
        }
    }



    private void OnSurfaceActualThemeChanged(FrameworkElement sender, object args)
    {
        UpdateResolvedMaterial();
    }

    private void InheritAmbientThemeProperties()
    {
        if (ReadLocalValue(ToneProperty) == DependencyProperty.UnsetValue)
        {
            var ambientTone = FindAncestorAttached<SeaGlassTone>(SeaGlassTheme.ToneProperty);
            if (ambientTone.HasValue) Tone = ambientTone.Value;
        }

        if (ReadLocalValue(VariantProperty) == DependencyProperty.UnsetValue)
        {
            var ambientVariant = FindAncestorAttached<SeaGlassVariant>(SeaGlassTheme.VariantProperty);
            if (ambientVariant.HasValue) Variant = ambientVariant.Value;
        }

        if (ReadLocalValue(SizeProperty) == DependencyProperty.UnsetValue)
        {
            var ambientSize = FindAncestorAttached<SeaGlassControlSize>(SeaGlassTheme.SizeProperty);
            if (ambientSize.HasValue) Size = ambientSize.Value;
        }
    }

    private T? FindAncestorAttached<T>(DependencyProperty? dp) where T : struct
    {
        if (dp == null) return null;
        DependencyObject? current = this;
        while (current != null)
        {
            current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current);
            if (current != null)
            {
                object val = current.GetValue(dp);
                if (val is T typedVal)
                {
                    return typedVal;
                }
            }
        }
        return null;
    }

    private static void OnVariantChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassSurface s)
        {
            s.UpdateResolvedMaterial();
        }
    }

    private static void OnToneChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassSurface s)
        {
            s.UpdateResolvedMaterial();
        }
    }

    private static void OnTintPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassSurface s)
        {
            s.UpdateResolvedMaterial();
        }
    }

    private static void OnSizeTokenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassSurface s)
        {
            bool isRigidHeightControl = s is SeaGlassButton || s is SeaGlassPill || s is SeaGlassFilterChip || s is SeaGlassIconButton || s is SeaGlassPebbleOrb;

            switch (s.Size)
            {
                case SeaGlassControlSize.Compact:
                    if (isRigidHeightControl) s.Height = 28;
                    s.Padding = new Thickness(10, 4, 10, 4);
                    break;
                case SeaGlassControlSize.Small:
                    if (isRigidHeightControl) s.Height = 32;
                    s.Padding = new Thickness(12, 6, 12, 6);
                    break;
                case SeaGlassControlSize.Medium:
                    if (isRigidHeightControl) s.Height = 40;
                    s.Padding = new Thickness(18, 10, 18, 10);
                    break;
                case SeaGlassControlSize.Large:
                    if (isRigidHeightControl) s.Height = 48;
                    s.Padding = new Thickness(24, 14, 24, 14);
                    break;
                case SeaGlassControlSize.Icon:
                    if (isRigidHeightControl) { s.Width = 40; s.Height = 40; }
                    s.Padding = new Thickness(8);
                    break;
            }
        }
    }

    private static void OnMaterialPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassSurface s && e.NewValue is SeaGlassMaterial mat)
        {
            s._materialAnimator = new SeaGlassMaterialAnimator(mat);
            s.UpdateGlassCompositionParameters();
        }
    }

    private static void OnOpticalParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassSurface s)
        {
            SeaGlassMaterial current = s.Material;
            s.Material = current with
            {
                Coarseness = (float)s.Coarseness,
                TintDensity = (float)s.TintDensity,
                ThicknessDp = (float)s.ThicknessDp,
                Ior = (float)s.Ior,
                RimChalkiness = (float)s.RimChalkiness
            };
            s.UpdateGlassCompositionParameters();
        }
    }

    public void UpdateResolvedMaterial()
    {
        SeaGlassMaterial resolved = SeaGlassToneExtensions.ResolveMaterial(Tone, Variant);
        if (Tint.HasValue)
        {
            Windows.UI.Color c = Tint.Value;
            int argb = (c.A << 24) | (c.R << 16) | (c.G << 8) | c.B;
            resolved = resolved with { TintColorArgb = argb };
        }
        Material = resolved;
        if (_materialAnimator == null)
        {
            _materialAnimator = new SeaGlassMaterialAnimator(resolved);
        }
        UpdateGlassCompositionParameters();
    }

    public void SnapMaterial(SeaGlassMaterial material)
    {
        Material = material;
        _materialAnimator = new SeaGlassMaterialAnimator(material);
        UpdateGlassCompositionParameters();
    }

    public virtual void PerformClick()
    {
        OnClick?.Invoke();
        Click?.Invoke(this, new RoutedEventArgs());
        if (Command?.CanExecute(CommandParameter) == true)
        {
            Command.Execute(CommandParameter);
        }
    }

    public void OnPointerStateChanged(Vector2? localPosition, bool isDown)
    {
        if (localPosition is not null) LastTouchOffsetInLocal = localPosition;
        bool wasDown = IsPointerDown;
        IsPointerDown = isDown;
        if (wasDown && !isDown)
        {
            PerformClick();
        }
    }

    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);
        CapturePointer(e.Pointer);
        var pt = e.GetCurrentPoint(this).Position;
        OnPointerStateChanged(new Vector2((float)pt.X, (float)pt.Y), true);
        VisualStateManager.GoToState(this, "Pressed", true);
    }

    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        base.OnPointerReleased(e);
        ReleasePointerCapture(e.Pointer);
        var pt = e.GetCurrentPoint(this).Position;
        OnPointerStateChanged(new Vector2((float)pt.X, (float)pt.Y), false);
        VisualStateManager.GoToState(this, "Normal", true);
    }

    protected override void OnPointerMoved(PointerRoutedEventArgs e)
    {
        base.OnPointerMoved(e);
        var pt = e.GetCurrentPoint(this).Position;
        LastTouchOffsetInLocal = new Vector2((float)pt.X, (float)pt.Y);
    }

    protected override void OnPointerEntered(PointerRoutedEventArgs e)
    {
        base.OnPointerEntered(e);
        VisualStateManager.GoToState(this, "PointerOver", true);
    }

    protected override void OnPointerExited(PointerRoutedEventArgs e)
    {
        base.OnPointerExited(e);
        VisualStateManager.GoToState(this, "Normal", true);
    }

    protected override void OnGotFocus(RoutedEventArgs e)
    {
        base.OnGotFocus(e);
        VisualStateManager.GoToState(this, "Focused", true);
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);
        VisualStateManager.GoToState(this, "Unfocused", true);
    }

    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Windows.System.VirtualKey.Space || e.Key == Windows.System.VirtualKey.Enter)
        {
            PerformClick();
            e.Handled = true;
        }
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new Automation.SeaGlassSurfaceAutomationPeer(this);

    private void UpdateVisualStates(bool useTransitions)
    {
        if (!IsEnabled)
        {
            VisualStateManager.GoToState(this, "Disabled", useTransitions);
        }
        else if (IsPointerDown)
        {
            VisualStateManager.GoToState(this, "Pressed", useTransitions);
        }
        else
        {
            VisualStateManager.GoToState(this, "Normal", useTransitions);
        }
    }

    public SeaGlassSurfaceFrameState EvaluateFrame(
        float dtSeconds = 1f / 60f,
        SeaGlassTheme? theme = null,
        IReadOnlyList<SeaGlassSdfEngine.SdfNode>? nodesOverride = null,
        float smoothRadiusDp = 26f,
        Action<float, float, SeaGlassVectorPath2D>? customContourBuilder = null)
    {
        SeaGlassTheme activeTheme = theme ?? SeaGlassTheme.Current;
        SeaGlassMotionScheme motion = activeTheme.MotionScheme;
        float density = MathF.Max(0.5f, activeTheme.DpiScale);
        SeaGlassStageState? activeStage = StageState ?? SeaGlassStageHost.AmbientStageState;

        float targetPress = IsPointerDown ? 1f : 0f;
        motion.FastSpatial.Step(ref _pressFraction, ref _pressVelocity, targetPress, dtSeconds);
        float clampedPress = Math.Clamp(_pressFraction, 0f, 1.25f);

        SeaGlassMaterial targetMaterial = GetPressedMaterial(Math.Clamp(clampedPress, 0f, 1f));
        SeaGlassMaterial effectiveMaterial = _materialAnimator.StepToward(targetMaterial, motion, dtSeconds);

        float wPx = MathF.Max(1f, SizePx.X);
        float hPx = MathF.Max(1f, SizePx.Y);
        float cx = MathF.Max(1f, wPx * 0.5f);
        float cy = MathF.Max(1f, hPx * 0.5f);
        float normTouchX = LastTouchOffsetInLocal is { } ptX ? Math.Clamp((ptX.X - cx) / cx, -1f, 1f) : 0f;
        float normTouchY = LastTouchOffsetInLocal is { } ptY ? Math.Clamp((ptY.Y - cy) / cy, -1f, 1f) : 0f;

        float tiltGain = TiltEnabled ? 4.8f * clampedPress : 0f;
        motion.FastSpatial.Step(ref _tiltRotX, ref _tiltVelX, -normTouchY * tiltGain, dtSeconds);
        motion.FastSpatial.Step(ref _tiltRotY, ref _tiltVelY, normTouchX * tiltGain, dtSeconds);

        float scaleX = 1f + 0.022f * clampedPress;
        float scaleY = 1f - 0.018f * clampedPress;

        int contentColor = SeaGlassColorMath.OptimalContentColorFor(
            effectiveMaterial.TintColorArgb,
            effectiveMaterial.TintDensity);

        float effectiveCornerDp = CornerRadius.TopLeft > 0.001
            ? (float)CornerRadius.TopLeft
            : effectiveMaterial.CornerRadiusDp;
        float baseCornerPx = MathF.Min(effectiveCornerDp * density, MathF.Min(wPx, hPx) * 0.5f);
        Vector2 stageSize = activeStage?.BackdropSize ?? new Vector2(1280f, 800f);
        Vector2 stageOrigin = activeStage?.BackdropOriginInRoot ?? Vector2.Zero;
        float leftInStage = OriginInRoot.X - stageOrigin.X;
        float topInStage = OriginInRoot.Y - stageOrigin.Y;
        float proximityPx = MathF.Max(1f, CornerProximityThresholdDp * density);
        float fullMatchPx = MathF.Max(0f, CornerFullMatchDistanceDp * density);

        SeaGlassCornerRadiiPx deviceSolved = (CornerAdaptationMode == SeaGlassCornerAdaptationMode.Fixed || CornerMask == SeaGlassCornerMask.None)
            ? SeaGlassCornerRadiiPx.Uniform(baseCornerPx)
            : SeaGlassCornerAdaptationSolver.SolveElementCornerRadiiPx(
                elementLeftPx: leftInStage,
                elementTopPx: topInStage,
                elementRightPx: leftInStage + wPx,
                elementBottomPx: topInStage + hPx,
                containerWidthPx: stageSize.X,
                containerHeightPx: stageSize.Y,
                containerCornersPx: activeTheme.DeviceCornerSpec.ToRadiiPx(),
                baseCornerRadiusPx: baseCornerPx,
                minCornerFloorPx: 8f * density,
                influenceDistancePx: proximityPx,
                adaptationStrength: CornerAdaptationStrength,
                mode: CornerAdaptationMode,
                cornerMask: CornerMask,
                fullMatchDistancePx: fullMatchPx,
                allowAsymmetricCorners: AllowAsymmetricCorners);

        SeaGlassCornerRadiiPx solvedRadii = deviceSolved;
        if (AdaptToParentContainer &&
            activeTheme.ContainerCornerScope is { } parentScope &&
            parentScope.WidthPx > wPx + 4f &&
            parentScope.HeightPx > hPx + 4f &&
            CornerAdaptationMode != SeaGlassCornerAdaptationMode.Fixed &&
            CornerMask != SeaGlassCornerMask.None)
        {
            float relLeft = OriginInRoot.X - parentScope.OriginInRoot.X;
            float relTop = OriginInRoot.Y - parentScope.OriginInRoot.Y;
            solvedRadii = SeaGlassCornerAdaptationSolver.SolveElementCornerRadiiPx(
                elementLeftPx: relLeft,
                elementTopPx: relTop,
                elementRightPx: relLeft + wPx,
                elementBottomPx: relTop + hPx,
                containerWidthPx: parentScope.WidthPx,
                containerHeightPx: parentScope.HeightPx,
                containerCornersPx: parentScope.CornerRadiiPx,
                baseCornerRadiusPx: deviceSolved.MaxRadiusPx,
                minCornerFloorPx: 7f * density,
                influenceDistancePx: MathF.Min(proximityPx, 48f * density),
                adaptationStrength: CornerAdaptationStrength,
                mode: CornerAdaptationMode,
                cornerMask: CornerMask,
                fullMatchDistancePx: fullMatchPx,
                allowAsymmetricCorners: AllowAsymmetricCorners);
        }

        SeaGlassSafePaddingPx safePadding = CornerAdaptationMode == SeaGlassCornerAdaptationMode.FlushCornerEmbed
            ? SeaGlassCornerAdaptationSolver.ComputeCornerSafePaddingPx(
                solvedRadii,
                BasePaddingPx.X,
                BasePaddingPx.Y,
                BasePaddingPx.Z,
                BasePaddingPx.W)
            : new SeaGlassSafePaddingPx(BasePaddingPx.X, BasePaddingPx.Y, BasePaddingPx.Z, BasePaddingPx.W);

        IReadOnlyList<SeaGlassSdfEngine.SdfNode> activeNodes;
        if (nodesOverride is not null)
        {
            activeNodes = nodesOverride;
        }
        else
        {
            _singleNodeBuffer[0] = new SeaGlassSdfEngine.SdfNode(
                CenterX: wPx * 0.5f,
                CenterY: hPx * 0.5f,
                HalfWidth: wPx * 0.5f,
                HalfHeight: hPx * 0.5f,
                CornerTopRight: solvedRadii.TopRightPx,
                CornerBottomRight: solvedRadii.BottomRightPx,
                CornerBottomLeft: solvedRadii.BottomLeftPx,
                CornerTopLeft: solvedRadii.TopLeftPx,
                CornerSmoothing: effectiveMaterial.CornerSmoothing,
                TintColorArgb: effectiveMaterial.TintColorArgb,
                ThicknessPx: effectiveMaterial.ThicknessDp * density);
            activeNodes = _singleNodeBuffer;
        }

        bool isUniformSingleNode = customContourBuilder is null &&
                                   nodesOverride is null &&
                                   !solvedRadii.IsAsymmetric;

        if (customContourBuilder is not null)
        {
            customContourBuilder(wPx, hPx, _contourPath);
        }
        else if (!isUniformSingleNode)
        {
            SeaGlassContourPathBuilder.BuildCoalescingScenePath(
                nodes: activeNodes,
                smoothRadiusPx: smoothRadiusDp * density,
                outPath: _contourPath);
        }
        else
        {
            _contourPath.Reset();
        }

        Vector2 backdropOffset = activeStage?.ComputeLocalBackdropOffset(OriginInRoot) ?? OriginInRoot;
        Vector3 lightDir = activeTheme.TiltState.LightDirection;

        _uniformBlock.Pack(
            widthPx: wPx,
            heightPx: hPx,
            backdropOffsetX: backdropOffset.X,
            backdropOffsetY: backdropOffset.Y,
            material: effectiveMaterial,
            density: density,
            nodes: activeNodes,
            smoothRadiusPx: smoothRadiusDp * density,
            lightDirX: lightDir.X,
            lightDirY: lightDir.Y,
            lightDirZ: lightDir.Z);

        return new SeaGlassSurfaceFrameState(
            EffectiveMaterial: effectiveMaterial,
            OptimalContentColorArgb: contentColor,
            SolvedCornerRadiiPx: solvedRadii,
            SafePaddingPx: safePadding,
            ScaleX: scaleX,
            ScaleY: scaleY,
            TiltRotationXDegrees: _tiltRotX,
            TiltRotationYDegrees: _tiltRotY,
            UniformBlock: _uniformBlock,
            ContourPath: _contourPath,
            IsUniformSingleNode: isUniformSingleNode,
            WidthPx: wPx,
            HeightPx: hPx);
    }

    public void EvaluateFrameInPlace(
        ref SeaGlassSurfaceFrameState targetState,
        float dtSeconds = 1f / 60f,
        SeaGlassTheme? theme = null,
        IReadOnlyList<SeaGlassSdfEngine.SdfNode>? nodesOverride = null,
        float smoothRadiusDp = 26f,
        Action<float, float, SeaGlassVectorPath2D>? customContourBuilder = null)
    {
        targetState = EvaluateFrame(
            dtSeconds: dtSeconds,
            theme: theme,
            nodesOverride: nodesOverride,
            smoothRadiusDp: smoothRadiusDp,
            customContourBuilder: customContourBuilder);
    }
}
