using System;
using System.Numerics;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Damped harmonic oscillator spring specification shared between WinUI 3
/// <c>SpringScalarNaturalMotionAnimation</c> / <c>SpringVector3NaturalMotionAnimation</c>
/// and frame-accurate spatial/effects spring evaluation.
///
/// Exact 1:1 parity with <c>androidx.compose.animation.core.SpringSpec</c> in <c>SeaGlassTheme.kt</c>.
/// </summary>
public readonly record struct SeaGlassSpringSpec(float DampingRatio, float Stiffness)
{
    /// <summary>
    /// Natural undamped angular frequency omega_n = sqrt(stiffness) in rad/s.
    /// </summary>
    public float AngularFrequency => MathF.Sqrt(MathF.Max(1f, Stiffness));

    /// <summary>
    /// WinUI <c>Microsoft.UI.Composition.SpringNaturalMotionAnimation.Period</c> = 2 * PI / sqrt(stiffness).
    /// </summary>
    public TimeSpan CompositionPeriod =>
        TimeSpan.FromSeconds(2.0 * Math.PI / Math.Sqrt(Math.Max(1.0, Stiffness)));

    /// <summary>
    /// Whether this spring is under-damped (allows organic M3 Expressive spatial bounce).
    /// </summary>
    public bool IsUnderDamped => DampingRatio < 0.999f;

    /// <summary>
    /// Whether this spring is critically damped (zero overshoot for optical effects).
    /// </summary>
    public bool IsCriticallyDamped => MathF.Abs(DampingRatio - 1.0f) <= 0.001f;

    /// <summary>
    /// Advances a 1D spring state (<paramref name="current"/>, <paramref name="velocity"/>) toward
    /// <paramref name="target"/> over <paramref name="dtSeconds"/> using semi-implicit Euler integration
    /// with sub-stepping for unconditional stability.
    /// </summary>
    public void Step(ref float current, ref float velocity, float target, float dtSeconds)
    {
        if (MathF.Abs(current - target) < 1e-4f && MathF.Abs(velocity) < 1e-4f)
        {
            current = target;
            velocity = 0f;
            return;
        }

        float dt = Math.Clamp(dtSeconds, 0.0001f, 0.05f);
        const int subSteps = 4;
        float h = dt / subSteps;
        float k = MathF.Max(1f, Stiffness);
        float c = 2f * MathF.Max(0.05f, DampingRatio) * MathF.Sqrt(k);

        for (int i = 0; i < subSteps; i++)
        {
            float displacement = current - target;
            float accel = -k * displacement - c * velocity;
            velocity += accel * h;
            current += velocity * h;
        }

        // Critically damped effects springs never overshoot target
        if (IsCriticallyDamped)
        {
            if ((velocity > 0f && current > target) || (velocity < 0f && current < target))
            {
                current = target;
                velocity = 0f;
                return;
            }
        }

        if (MathF.Abs(current - target) < 1e-4f && MathF.Abs(velocity) < 1e-3f)
        {
            current = target;
            velocity = 0f;
        }
    }
}

/// <summary>
/// Material 3 Expressive dual-spring motion scheme for SeaGlass on Windows.
///
/// Separates Spatial springs (under-damped, allowing lively organic bounce for geometry,
/// corner morphing, scale squish, and SDF coalescing bridges) from Effects springs
/// (critically damped <c>DampingRatio = 1.0f</c> for tint, coarseness, frost radius, and rim glow
/// so optical properties never overshoot).
/// </summary>
public sealed record SeaGlassMotionScheme(
    SeaGlassSpringSpec FastSpatial,
    SeaGlassSpringSpec DefaultSpatial,
    SeaGlassSpringSpec SlowSpatial,
    SeaGlassSpringSpec FastEffects,
    SeaGlassSpringSpec DefaultEffects)
{
    public SeaGlassMotionScheme()
        : this(
            FastSpatial: new SeaGlassSpringSpec(DampingRatio: 0.62f, Stiffness: 680f),
            DefaultSpatial: new SeaGlassSpringSpec(DampingRatio: 0.66f, Stiffness: 410f),
            SlowSpatial: new SeaGlassSpringSpec(DampingRatio: 0.72f, Stiffness: 240f),
            FastEffects: new SeaGlassSpringSpec(DampingRatio: 1.00f, Stiffness: 1100f),
            DefaultEffects: new SeaGlassSpringSpec(DampingRatio: 1.00f, Stiffness: 720f))
    {
    }
}

/// <summary>
/// Enclosing page, window, or card container bounds and resolved 4 corner radii in root coordinates,
/// allowing nested child SeaGlass elements to adapt their corner radii concentrically.
/// </summary>
public sealed record SeaGlassContainerCornerScope(
    Vector2 OriginInRoot,
    float WidthPx,
    float HeightPx,
    SeaGlassCornerRadiiPx CornerRadiiPx);

/// <summary>
/// Stateful dual-spring material animator that enforces Rule #5 in <c>AGENTS.md</c>:
/// perceptual Oklab interpolation for <see cref="SeaGlassMaterial.TintColorArgb"/> and optical
/// scalars via critically-damped <c>Effects</c> springs, and geometry (<c>ThicknessDp</c>,
/// <c>CornerRadiusDp</c>, <c>CornerSmoothing</c>) via bouncy <c>Spatial</c> springs.
/// </summary>
public sealed class SeaGlassMaterialAnimator
{
    private float _l, _vL;
    private float _a, _vA;
    private float _b, _vB;
    private float _tintDensity, _vTintDensity;
    private float _coarseness, _vCoarseness;
    private float _frostRadiusDp, _vFrostRadiusDp;
    private float _ior, _vIor;
    private float _dispersion, _vDispersion;
    private float _rimChalkiness, _vRimChalkiness;
    private float _thicknessDp, _vThicknessDp;
    private float _cornerRadiusDp, _vCornerRadiusDp;
    private float _cornerSmoothing, _vCornerSmoothing;
    private float _transparency, _vTransparency;
    private float _ambientAdaptation;
    private bool _isAtRest = true;

    private readonly SeaGlassMaterial _currentInstance = new(0);
    private readonly SeaGlassMaterial _targetInstance = new(0);

    public SeaGlassMaterial Current { get; private set; }
    public SeaGlassMaterial Target { get; private set; }

    public SeaGlassMaterialAnimator(SeaGlassMaterial initial)
    {
        _currentInstance.CopyFrom(initial);
        _targetInstance.CopyFrom(initial);
        Current = _currentInstance;
        Target = _targetInstance;
        SnapTo(initial);
    }

    public void SnapTo(SeaGlassMaterial material)
    {
        SeaGlassColorMath.OklabColor lab = SeaGlassColorMath.ArgbToOklab(material.TintColorArgb);
        _l = lab.L; _vL = 0f;
        _a = lab.A; _vA = 0f;
        _b = lab.B; _vB = 0f;
        _tintDensity = material.TintDensity; _vTintDensity = 0f;
        _coarseness = material.Coarseness; _vCoarseness = 0f;
        _frostRadiusDp = material.FrostRadiusDp; _vFrostRadiusDp = 0f;
        _ior = material.Ior; _vIor = 0f;
        _dispersion = material.ChromaticDispersion; _vDispersion = 0f;
        _rimChalkiness = material.RimChalkiness; _vRimChalkiness = 0f;
        _thicknessDp = material.ThicknessDp; _vThicknessDp = 0f;
        _cornerRadiusDp = material.CornerRadiusDp; _vCornerRadiusDp = 0f;
        _cornerSmoothing = material.CornerSmoothing; _vCornerSmoothing = 0f;
        _transparency = material.Transparency; _vTransparency = 0f;
        _ambientAdaptation = material.AmbientAdaptation;
        _isAtRest = true;
        _currentInstance.CopyFrom(material);
        _targetInstance.CopyFrom(material);
        Current = _currentInstance;
        Target = _targetInstance;
    }

    public SeaGlassMaterial StepToward(SeaGlassMaterial target, SeaGlassMotionScheme motion, float dtSeconds)
    {
        if (_isAtRest && _targetInstance.Equals(target))
        {
            return Current;
        }

        _targetInstance.CopyFrom(target);
        Target = _targetInstance;
        SeaGlassColorMath.OklabColor labTarget = SeaGlassColorMath.ArgbToOklab(target.TintColorArgb);

        // Effects springs (critically damped, zero overshoot)
        motion.DefaultEffects.Step(ref _l, ref _vL, labTarget.L, dtSeconds);
        motion.DefaultEffects.Step(ref _a, ref _vA, labTarget.A, dtSeconds);
        motion.DefaultEffects.Step(ref _b, ref _vB, labTarget.B, dtSeconds);
        motion.DefaultEffects.Step(ref _tintDensity, ref _vTintDensity, target.TintDensity, dtSeconds);
        motion.DefaultEffects.Step(ref _coarseness, ref _vCoarseness, target.Coarseness, dtSeconds);
        motion.DefaultEffects.Step(ref _frostRadiusDp, ref _vFrostRadiusDp, target.FrostRadiusDp, dtSeconds);
        motion.DefaultEffects.Step(ref _ior, ref _vIor, target.Ior, dtSeconds);
        motion.DefaultEffects.Step(ref _dispersion, ref _vDispersion, target.ChromaticDispersion, dtSeconds);
        motion.FastEffects.Step(ref _rimChalkiness, ref _vRimChalkiness, target.RimChalkiness, dtSeconds);
        motion.DefaultEffects.Step(ref _transparency, ref _vTransparency, target.Transparency, dtSeconds);

        // Spatial springs (expressive bounce)
        motion.DefaultSpatial.Step(ref _thicknessDp, ref _vThicknessDp, target.ThicknessDp, dtSeconds);
        motion.FastSpatial.Step(ref _cornerRadiusDp, ref _vCornerRadiusDp, target.CornerRadiusDp, dtSeconds);
        motion.DefaultSpatial.Step(ref _cornerSmoothing, ref _vCornerSmoothing, target.CornerSmoothing, dtSeconds);
        _ambientAdaptation = target.AmbientAdaptation;

        bool settled = MathF.Abs(_vL) < 1e-4f && MathF.Abs(_vA) < 1e-4f && MathF.Abs(_vB) < 1e-4f &&
                       MathF.Abs(_vTintDensity) < 1e-4f && MathF.Abs(_vCoarseness) < 1e-4f && MathF.Abs(_vFrostRadiusDp) < 1e-4f &&
                       MathF.Abs(_vIor) < 1e-4f && MathF.Abs(_vDispersion) < 1e-4f && MathF.Abs(_vRimChalkiness) < 1e-4f &&
                       MathF.Abs(_vTransparency) < 1e-4f && MathF.Abs(_vThicknessDp) < 1e-4f &&
                       MathF.Abs(_vCornerRadiusDp) < 1e-4f && MathF.Abs(_vCornerSmoothing) < 1e-4f &&
                       MathF.Abs(_l - labTarget.L) < 1e-3f && MathF.Abs(_a - labTarget.A) < 1e-3f && MathF.Abs(_b - labTarget.B) < 1e-3f &&
                       MathF.Abs(_tintDensity - target.TintDensity) < 1e-3f &&
                       MathF.Abs(_coarseness - target.Coarseness) < 1e-3f &&
                       MathF.Abs(_frostRadiusDp - target.FrostRadiusDp) < 1e-3f &&
                       MathF.Abs(_ior - target.Ior) < 1e-3f &&
                       MathF.Abs(_dispersion - target.ChromaticDispersion) < 1e-3f &&
                       MathF.Abs(_rimChalkiness - target.RimChalkiness) < 1e-3f &&
                       MathF.Abs(_transparency - target.Transparency) < 1e-3f &&
                       MathF.Abs(_thicknessDp - target.ThicknessDp) < 1e-3f &&
                       MathF.Abs(_cornerRadiusDp - target.CornerRadiusDp) < 1e-3f &&
                       MathF.Abs(_cornerSmoothing - target.CornerSmoothing) < 1e-3f;

        if (settled)
        {
            _isAtRest = true;
            _vL = 0f; _vA = 0f; _vB = 0f;
            _vTintDensity = 0f; _vCoarseness = 0f; _vFrostRadiusDp = 0f;
            _vIor = 0f; _vDispersion = 0f; _vRimChalkiness = 0f;
            _vTransparency = 0f; _vThicknessDp = 0f;
            _vCornerRadiusDp = 0f; _vCornerSmoothing = 0f;
            _currentInstance.CopyFrom(target);
            Current = _currentInstance;
            return Current;
        }

        _isAtRest = false;
        int animatedArgb = SeaGlassColorMath.OklabToArgb(
            new SeaGlassColorMath.OklabColor(Math.Clamp(_l, 0.04f, 0.99f), _a, _b, 1f));

        _currentInstance.TintColorArgb = animatedArgb;
        _currentInstance.TintDensity = Math.Clamp(_tintDensity, 0f, 1f);
        _currentInstance.Coarseness = Math.Clamp(_coarseness, 0f, 1f);
        _currentInstance.FrostRadiusDp = MathF.Max(0f, _frostRadiusDp);
        _currentInstance.Ior = Math.Clamp(_ior, 1.0f, 2.0f);
        _currentInstance.ChromaticDispersion = Math.Clamp(_dispersion, 0f, 1f);
        _currentInstance.RimChalkiness = Math.Clamp(_rimChalkiness, 0f, 1f);
        _currentInstance.ThicknessDp = Math.Clamp(_thicknessDp, 1f, 36f);
        _currentInstance.CornerRadiusDp = MathF.Max(0f, _cornerRadiusDp);
        _currentInstance.CornerSmoothing = Math.Clamp(_cornerSmoothing, 2.0f, 4.0f);
        _currentInstance.AmbientAdaptation = Math.Clamp(_ambientAdaptation, 0f, 1f);
        _currentInstance.Transparency = Math.Clamp(_transparency, 0f, 1f);

        Current = _currentInstance;
        return Current;
    }
}

/// <summary>
/// Root theme context for WinUI / Windows applications that bridges <see cref="SeaGlassPalette"/>,
/// <see cref="SeaGlassMotionScheme"/>, <see cref="SeaGlassTiltState"/>, and <see cref="SeaGlassDeviceCornerSpec"/>.
///
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassTheme</c> on Android.
/// </summary>
public sealed class SeaGlassTheme
{
    public static SeaGlassTheme Current { get; set; } = new();

    #region Attached Dependency Properties

    public static Microsoft.UI.Xaml.DependencyProperty? ToneProperty;
    public static Microsoft.UI.Xaml.DependencyProperty? VariantProperty;
    public static Microsoft.UI.Xaml.DependencyProperty? SizeProperty;
    public static Microsoft.UI.Xaml.DependencyProperty? MaterialProperty;
    public static Microsoft.UI.Xaml.DependencyProperty? TintProperty;
    public static Microsoft.UI.Xaml.DependencyProperty? TintDensityProperty;
    public static Microsoft.UI.Xaml.DependencyProperty? CoarsenessProperty;
    public static Microsoft.UI.Xaml.DependencyProperty? ThicknessDpProperty;
    public static Microsoft.UI.Xaml.DependencyProperty? SpatialSpringProperty;
    public static Microsoft.UI.Xaml.DependencyProperty? EffectsSpringProperty;
    public static Microsoft.UI.Xaml.DependencyProperty? MotionSchemeProperty;

    private static readonly object _dpRegistrationLock = new();

    static SeaGlassTheme()
    {
        RegisterDependencyProperties();
    }

    public static void RegisterDependencyProperties()
    {
        if (ToneProperty != null) return;
        lock (_dpRegistrationLock)
        {
            if (ToneProperty != null) return;
            try
            {
                ToneProperty = Microsoft.UI.Xaml.DependencyProperty.RegisterAttached(
                    "Tone",
                    typeof(SeaGlassTone),
                    typeof(SeaGlassTheme),
                    new Microsoft.UI.Xaml.PropertyMetadata(SeaGlassTone.SeafoamMint));

                VariantProperty = Microsoft.UI.Xaml.DependencyProperty.RegisterAttached(
                    "Variant",
                    typeof(SeaGlassVariant),
                    typeof(SeaGlassTheme),
                    new Microsoft.UI.Xaml.PropertyMetadata(SeaGlassVariant.Primary));

                SizeProperty = Microsoft.UI.Xaml.DependencyProperty.RegisterAttached(
                    "Size",
                    typeof(SeaGlassControlSize),
                    typeof(SeaGlassTheme),
                    new Microsoft.UI.Xaml.PropertyMetadata(SeaGlassControlSize.Medium));

                MaterialProperty = Microsoft.UI.Xaml.DependencyProperty.RegisterAttached(
                    "Material",
                    typeof(SeaGlassMaterial),
                    typeof(SeaGlassTheme),
                    new Microsoft.UI.Xaml.PropertyMetadata(null));

                TintProperty = Microsoft.UI.Xaml.DependencyProperty.RegisterAttached(
                    "Tint",
                    typeof(Windows.UI.Color?),
                    typeof(SeaGlassTheme),
                    new Microsoft.UI.Xaml.PropertyMetadata(null));

                TintDensityProperty = Microsoft.UI.Xaml.DependencyProperty.RegisterAttached(
                    "TintDensity",
                    typeof(double),
                    typeof(SeaGlassTheme),
                    new Microsoft.UI.Xaml.PropertyMetadata(0.68));

                CoarsenessProperty = Microsoft.UI.Xaml.DependencyProperty.RegisterAttached(
                    "Coarseness",
                    typeof(double),
                    typeof(SeaGlassTheme),
                    new Microsoft.UI.Xaml.PropertyMetadata(0.42));

                ThicknessDpProperty = Microsoft.UI.Xaml.DependencyProperty.RegisterAttached(
                    "ThicknessDp",
                    typeof(double),
                    typeof(SeaGlassTheme),
                    new Microsoft.UI.Xaml.PropertyMetadata(16.0));

                SpatialSpringProperty = Microsoft.UI.Xaml.DependencyProperty.RegisterAttached(
                    "SpatialSpring",
                    typeof(SeaGlassSpringSpec?),
                    typeof(SeaGlassTheme),
                    new Microsoft.UI.Xaml.PropertyMetadata(null));

                EffectsSpringProperty = Microsoft.UI.Xaml.DependencyProperty.RegisterAttached(
                    "EffectsSpring",
                    typeof(SeaGlassSpringSpec?),
                    typeof(SeaGlassTheme),
                    new Microsoft.UI.Xaml.PropertyMetadata(null));

                MotionSchemeProperty = Microsoft.UI.Xaml.DependencyProperty.RegisterAttached(
                    "MotionScheme",
                    typeof(SeaGlassMotionScheme),
                    typeof(SeaGlassTheme),
                    new Microsoft.UI.Xaml.PropertyMetadata(null));
            }
            catch
            {
                // If executed outside an initialized WinUI Xaml context (e.g. unbootstrapped MTA unit test),
                // suppress exception so .cctor() does not crash. Properties will register when XamlCore initializes.
            }
        }
    }

    public static SeaGlassTone GetTone(Microsoft.UI.Xaml.DependencyObject obj)
    {
        RegisterDependencyProperties();
        return ToneProperty != null ? (SeaGlassTone)obj.GetValue(ToneProperty) : SeaGlassTone.SeafoamMint;
    }

    public static void SetTone(Microsoft.UI.Xaml.DependencyObject obj, SeaGlassTone value)
    {
        RegisterDependencyProperties();
        if (ToneProperty != null) obj.SetValue(ToneProperty, value);
    }

    public static SeaGlassVariant GetVariant(Microsoft.UI.Xaml.DependencyObject obj)
    {
        RegisterDependencyProperties();
        return VariantProperty != null ? (SeaGlassVariant)obj.GetValue(VariantProperty) : SeaGlassVariant.Primary;
    }

    public static void SetVariant(Microsoft.UI.Xaml.DependencyObject obj, SeaGlassVariant value)
    {
        RegisterDependencyProperties();
        if (VariantProperty != null) obj.SetValue(VariantProperty, value);
    }

    public static SeaGlassControlSize GetSize(Microsoft.UI.Xaml.DependencyObject obj)
    {
        RegisterDependencyProperties();
        return SizeProperty != null ? (SeaGlassControlSize)obj.GetValue(SizeProperty) : SeaGlassControlSize.Medium;
    }

    public static void SetSize(Microsoft.UI.Xaml.DependencyObject obj, SeaGlassControlSize value)
    {
        RegisterDependencyProperties();
        if (SizeProperty != null) obj.SetValue(SizeProperty, value);
    }

    public static SeaGlassMaterial? GetMaterial(Microsoft.UI.Xaml.DependencyObject obj)
    {
        RegisterDependencyProperties();
        return MaterialProperty != null ? (SeaGlassMaterial?)obj.GetValue(MaterialProperty) : null;
    }

    public static void SetMaterial(Microsoft.UI.Xaml.DependencyObject obj, SeaGlassMaterial? value)
    {
        RegisterDependencyProperties();
        if (MaterialProperty != null) obj.SetValue(MaterialProperty, value);
    }

    public static Windows.UI.Color? GetTint(Microsoft.UI.Xaml.DependencyObject obj)
    {
        RegisterDependencyProperties();
        return TintProperty != null ? (Windows.UI.Color?)obj.GetValue(TintProperty) : null;
    }

    public static void SetTint(Microsoft.UI.Xaml.DependencyObject obj, Windows.UI.Color? value)
    {
        RegisterDependencyProperties();
        if (TintProperty != null) obj.SetValue(TintProperty, value);
    }

    public static double GetTintDensity(Microsoft.UI.Xaml.DependencyObject obj)
    {
        RegisterDependencyProperties();
        return TintDensityProperty != null ? (double)obj.GetValue(TintDensityProperty) : 0.68;
    }

    public static void SetTintDensity(Microsoft.UI.Xaml.DependencyObject obj, double value)
    {
        RegisterDependencyProperties();
        if (TintDensityProperty != null) obj.SetValue(TintDensityProperty, value);
    }

    public static double GetCoarseness(Microsoft.UI.Xaml.DependencyObject obj)
    {
        RegisterDependencyProperties();
        return CoarsenessProperty != null ? (double)obj.GetValue(CoarsenessProperty) : 0.42;
    }

    public static void SetCoarseness(Microsoft.UI.Xaml.DependencyObject obj, double value)
    {
        RegisterDependencyProperties();
        if (CoarsenessProperty != null) obj.SetValue(CoarsenessProperty, value);
    }

    public static double GetThicknessDp(Microsoft.UI.Xaml.DependencyObject obj)
    {
        RegisterDependencyProperties();
        return ThicknessDpProperty != null ? (double)obj.GetValue(ThicknessDpProperty) : 16.0;
    }

    public static void SetThicknessDp(Microsoft.UI.Xaml.DependencyObject obj, double value)
    {
        RegisterDependencyProperties();
        if (ThicknessDpProperty != null) obj.SetValue(ThicknessDpProperty, value);
    }

    public static SeaGlassSpringSpec? GetSpatialSpring(Microsoft.UI.Xaml.DependencyObject obj)
    {
        RegisterDependencyProperties();
        return SpatialSpringProperty != null ? (SeaGlassSpringSpec?)obj.GetValue(SpatialSpringProperty) : null;
    }

    public static void SetSpatialSpring(Microsoft.UI.Xaml.DependencyObject obj, SeaGlassSpringSpec? value)
    {
        RegisterDependencyProperties();
        if (SpatialSpringProperty != null) obj.SetValue(SpatialSpringProperty, value);
    }

    public static SeaGlassSpringSpec? GetEffectsSpring(Microsoft.UI.Xaml.DependencyObject obj)
    {
        RegisterDependencyProperties();
        return EffectsSpringProperty != null ? (SeaGlassSpringSpec?)obj.GetValue(EffectsSpringProperty) : null;
    }

    public static void SetEffectsSpring(Microsoft.UI.Xaml.DependencyObject obj, SeaGlassSpringSpec? value)
    {
        RegisterDependencyProperties();
        if (EffectsSpringProperty != null) obj.SetValue(EffectsSpringProperty, value);
    }

    public static SeaGlassMotionScheme? GetMotionScheme(Microsoft.UI.Xaml.DependencyObject obj)
    {
        RegisterDependencyProperties();
        return MotionSchemeProperty != null ? (SeaGlassMotionScheme?)obj.GetValue(MotionSchemeProperty) : null;
    }

    public static void SetMotionScheme(Microsoft.UI.Xaml.DependencyObject obj, SeaGlassMotionScheme? value)
    {
        RegisterDependencyProperties();
        if (MotionSchemeProperty != null) obj.SetValue(MotionSchemeProperty, value);
    }

    #endregion

    public ISeaGlassPlatformSettings PlatformSettings { get; set; } = WindowsPlatformSettings.Instance;
    public SeaGlassPalette Palette { get; set; }
    public SeaGlassMotionScheme MotionScheme { get; set; }
    public SeaGlassTiltState TiltState { get; set; }
    public SeaGlassDeviceTier? TierOverride { get; set; }
    public SeaGlassDeviceHardwareProfile DeviceCornerProfile { get; private set; }
    public SeaGlassDeviceCornerSpec DeviceCornerSpec { get; private set; }
    public SeaGlassContainerCornerScope? ContainerCornerScope { get; set; }
    public float DpiScale { get; set; } = 1.0f;

    public event Action<SeaGlassPalette>? PaletteChanged;

    public SeaGlassTheme() : this(null) { }

    public SeaGlassTheme(
        SeaGlassPalette? palette = null,
        SeaGlassMotionScheme? motionScheme = null,
        SeaGlassTiltState? tiltState = null,
        SeaGlassDeviceTier? tierOverride = null,
        SeaGlassDeviceHardwareProfile deviceCornerProfile = SeaGlassDeviceHardwareProfile.AutoDetect,
        float dpiScale = 1.0f)
    {
        Palette = palette ?? SeaGlassPalette.SeafoamMint;
        MotionScheme = motionScheme ?? new SeaGlassMotionScheme();
        TiltState = tiltState ?? new SeaGlassTiltState();
        TierOverride = tierOverride;
        DpiScale = MathF.Max(0.5f, dpiScale);
        DeviceCornerProfile = deviceCornerProfile;
        DeviceCornerSpec = SeaGlassCornerAdaptationSolver.DetectDeviceCorners(DpiScale, deviceCornerProfile);
    }

    public void SetPalette(SeaGlassPalette palette)
    {
        Palette = palette;
        PaletteChanged?.Invoke(palette);
    }

    public void SetDeviceCornerProfile(SeaGlassDeviceHardwareProfile profile)
    {
        DeviceCornerProfile = profile;
        DeviceCornerSpec = SeaGlassCornerAdaptationSolver.DetectDeviceCorners(DpiScale, profile);
    }
}
