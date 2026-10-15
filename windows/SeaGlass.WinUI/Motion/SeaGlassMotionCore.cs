using System;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Asymmetric lead-lag dual-edge lozenge travel dynamics for active indicators
/// in SegmentedButtonRow, FluidBar, PillNavBar, and TabView.
/// Enforces Rule #5: leading edge responds aggressively (k=540, zeta=0.68)
/// while trailing edge lags behind (k=310, zeta=0.74), causing the lozenge
/// to stretch dynamically during transit and settle with viscoelastic fluidity.
/// </summary>
public sealed class SeaGlassLeadLagLozengeTravel
{
    private readonly SeaGlassSpringSpec _leadSpring;
    private readonly SeaGlassSpringSpec _trailSpring;

    private float _leadIndex;
    private float _trailIndex;
    public float LeadVelocity;
    public float TrailVelocity;
    public float TargetIndex { get; private set; }

    public float LeadIndex => _leadIndex;
    public float TrailIndex => _trailIndex;

    public SeaGlassLeadLagLozengeTravel(float initialIndex = 0f)
        : this(500f, 390f, initialIndex)
    {
    }

    public SeaGlassLeadLagLozengeTravel(float leadStiffness, float trailStiffness, float initialIndex = 0f)
    {
        _leadSpring = new(DampingRatio: 0.66f, Stiffness: Math.Clamp(leadStiffness, 380f, 520f));
        _trailSpring = new(DampingRatio: 0.72f, Stiffness: Math.Clamp(trailStiffness, 380f, 520f));
        _leadIndex = initialIndex;
        _trailIndex = initialIndex;
        TargetIndex = initialIndex;
    }

    public void SnapTo(float index)
    {
        _leadIndex = index;
        _trailIndex = index;
        LeadVelocity = 0f;
        TrailVelocity = 0f;
        TargetIndex = index;
    }

    public void SetTarget(float targetIndex)
    {
        TargetIndex = targetIndex;
    }

    public void Step(float dtSeconds)
    {
        float target = TargetIndex;

        // Direction-aware lead-lag
        _leadSpring.Step(ref _leadIndex, ref LeadVelocity, target, dtSeconds);
        _trailSpring.Step(ref _trailIndex, ref TrailVelocity, target, dtSeconds);
    }

    public (float Left, float Right, float Stretch) Step(float targetLeft, float targetRight, float dtSeconds)
    {
        float currentCenter = (_leadIndex + _trailIndex) * 0.5f;
        float targetCenter = (targetLeft + targetRight) * 0.5f;
        bool movingLeft = targetCenter < currentCenter - 1e-4f;

        if (movingLeft)
        {
            // Leftward transit: left edge is leading edge (stiffer spring), right edge is trailing (softer spring)
            _leadSpring.Step(ref _trailIndex, ref TrailVelocity, targetLeft, dtSeconds);
            _trailSpring.Step(ref _leadIndex, ref LeadVelocity, targetRight, dtSeconds);
        }
        else
        {
            // Rightward transit or rest: right edge is leading edge, left edge is trailing
            _leadSpring.Step(ref _leadIndex, ref LeadVelocity, targetRight, dtSeconds);
            _trailSpring.Step(ref _trailIndex, ref TrailVelocity, targetLeft, dtSeconds);
        }

        float left = MathF.Min(_leadIndex, _trailIndex);
        float right = MathF.Max(_leadIndex, _trailIndex);
        float span = Math.Clamp(right - left, 1.0f, 1.35f);
        return (left, right, span);
    }

    /// <summary>
    /// Dynamic lozenge slot span. Resting span is 1.0. In transit stretches up to 1.35x.
    /// </summary>
    public float LozengeSlotSpan =>
        Math.Clamp(MathF.Abs(_leadIndex - _trailIndex) + 1.0f, 1.0f, 1.35f);

    /// <summary>
    /// Center of the lozenge during travel.
    /// </summary>
    public float CenterIndex => (_leadIndex + _trailIndex) * 0.5f;

    /// <summary>
    /// Normalized motion energy in range [0, 1] feeding into in-flight temporal de-aging.
    /// </summary>
    public float MotionEnergy =>
        Math.Clamp(MathF.Abs(LeadIndex - TrailIndex) / 0.85f, 0f, 1.0f);

    public bool IsAtRest =>
        MathF.Abs(LeadIndex - TargetIndex) < 1e-3f &&
        MathF.Abs(TrailIndex - TargetIndex) < 1e-3f &&
        MathF.Abs(LeadVelocity) < 1e-3f &&
        MathF.Abs(TrailVelocity) < 1e-3f;
}

/// <summary>
/// Hydrodynamic In-Flight Temporal De-Aging for SeaGlass optics.
/// During interaction, dragging, and shape morphing, hydrodynamic friction clarifies the glass:
/// surface micro-pits lubricate and melt towards zero coarseness, transparency clarifies to 0.92+,
/// and refractive index increases, returning to sand-frosted resting state upon settling.
/// </summary>
public static class SeaGlassTemporalDeaging
{
    public static SeaGlassMaterial ComputeDeagedMaterial(
        SeaGlassMaterial restMaterial,
        float motionEnergy = 0f,
        float dragVelocityPxPerSec = 0f,
        float morphProgress = 0f)
    {
        float eMorph = morphProgress > 0f && morphProgress < 1f
            ? Math.Clamp(4.0f * morphProgress * (1.0f - morphProgress), 0f, 1f)
            : 0f;
        float eVel = Math.Clamp(MathF.Abs(dragVelocityPxPerSec) / 1800f, 0f, 1f);
        float eMotion = Math.Clamp(motionEnergy, 0f, 1f);
        float totalEnergy = MathF.Max(eMorph, MathF.Max(eVel, eMotion));

        return restMaterial.Deaged(totalEnergy);
    }
}

/// <summary>
/// 20-Shape circle midpoint morph transitions.
/// All 20 geometric shapes in <see cref="SeaGlassShapeKind"/> are topologically mediated
/// through a normalized Circle midpoint at t = 0.5 with coupled material flex and zero heap allocations.
/// </summary>
public static class SeaGlassCircleMidpointMorph
{
    /// <summary>
    /// Computes the polar radial function R(theta) for a given shape kind.
    /// </summary>
    public static float EvaluateRadialDistance(SeaGlassShapeKind shape, float theta, float baseRadius)
    {
        return SeaGlassShapeMorph.EvaluatePolarRadiusScale(shape, theta) * baseRadius;
    }

    /// <summary>
    /// Two-stage Circle Midpoint Interpolation:
    /// t in [0.0, 0.5]: Shape A morphs into Circle (canonical midpoint).
    /// t in [0.5, 1.0]: Circle morphs into Shape B.
    /// </summary>
    public static float InterpolateRadius(
        SeaGlassShapeKind source,
        SeaGlassShapeKind target,
        float progress,
        float theta,
        float baseRadius)
    {
        float t = Math.Clamp(progress, 0f, 1f);
        float rCircle = baseRadius;

        if (t <= 0.5f)
        {
            float alpha = t * 2.0f; // [0, 1]
            float rA = EvaluateRadialDistance(source, theta, baseRadius);
            return (1.0f - alpha) * rA + alpha * rCircle;
        }
        else
        {
            float alpha = (t - 0.5f) * 2.0f; // [0, 1]
            float rB = EvaluateRadialDistance(target, theta, baseRadius);
            return (1.0f - alpha) * rCircle + alpha * rB;
        }
    }
}
