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
            DefaultEffects: new SeaGlassSpringSpec(DampingRatio: 1.00f, Stiffness: 540f))
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

    public SeaGlassMaterial Current { get; private set; }
    public SeaGlassMaterial Target { get; private set; }

    public SeaGlassMaterialAnimator(SeaGlassMaterial initial)
    {
        SnapTo(initial);
        Current = initial;
        Target = initial;
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
        Current = material;
        Target = material;
    }

    public SeaGlassMaterial StepToward(SeaGlassMaterial target, SeaGlassMotionScheme motion, float dtSeconds)
    {
        if (_isAtRest && Target.Equals(target))
        {
            return Current;
        }

        Target = target;
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

        bool settled = _vL == 0f && _vA == 0f && _vB == 0f &&
                       _vTintDensity == 0f && _vCoarseness == 0f && _vFrostRadiusDp == 0f &&
                       _vIor == 0f && _vDispersion == 0f && _vRimChalkiness == 0f &&
                       _vTransparency == 0f && _vThicknessDp == 0f &&
                       _vCornerRadiusDp == 0f && _vCornerSmoothing == 0f;

        if (settled)
        {
            _isAtRest = true;
            Current = target;
            return Current;
        }

        _isAtRest = false;
        int animatedArgb = SeaGlassColorMath.OklabToArgb(
            new SeaGlassColorMath.OklabColor(Math.Clamp(_l, 0.04f, 0.99f), _a, _b, 1f));

        Current = new SeaGlassMaterial(
            tintColorArgb: animatedArgb,
            tintDensity: Math.Clamp(_tintDensity, 0f, 1f),
            coarseness: Math.Clamp(_coarseness, 0f, 1f),
            frostRadiusDp: MathF.Max(0f, _frostRadiusDp),
            ior: Math.Clamp(_ior, 1.0f, 2.0f),
            chromaticDispersion: Math.Clamp(_dispersion, 0f, 1f),
            rimChalkiness: Math.Clamp(_rimChalkiness, 0f, 1f),
            thicknessDp: Math.Clamp(_thicknessDp, 1f, 36f),
            cornerRadiusDp: MathF.Max(0f, _cornerRadiusDp),
            cornerSmoothing: Math.Clamp(_cornerSmoothing, 2.0f, 4.0f),
            ambientAdaptation: Math.Clamp(_ambientAdaptation, 0f, 1f),
            transparency: Math.Clamp(_transparency, 0f, 1f));

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

    public SeaGlassPalette Palette { get; set; }
    public SeaGlassMotionScheme MotionScheme { get; set; }
    public SeaGlassTiltState TiltState { get; set; }
    public SeaGlassDeviceTier? TierOverride { get; set; }
    public SeaGlassDeviceHardwareProfile DeviceCornerProfile { get; private set; }
    public SeaGlassDeviceCornerSpec DeviceCornerSpec { get; private set; }
    public SeaGlassContainerCornerScope? ContainerCornerScope { get; set; }
    public float DpiScale { get; set; } = 1.0f;

    public event Action<SeaGlassPalette>? PaletteChanged;

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
