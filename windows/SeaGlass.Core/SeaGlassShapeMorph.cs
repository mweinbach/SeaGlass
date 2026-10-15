using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SeaGlass.Core;

/// <summary>
/// Full Material 3 Expressive × Liquid Sea Glass shape catalog (20 preset expressive geometries).
/// Exact 1:1 parity with <c>dev.seaglass.core.SeaGlassShapeKind</c> on Android.
/// </summary>
public enum SeaGlassShapeKind
{
    Circle,
    Capsule,
    Squircle,
    Arch,
    Sunny,
    Cookie4,
    Cookie6,
    Cookie7,
    Cookie9,
    Cookie12,
    Clover4,
    Clover8,
    Flower,
    SoftBurst,
    Gem,
    Heart,
    Diamond,
    Pentagon,
    Triangle,
    Slanted
}

public static class SeaGlassShapeKindExtensions
{
    public static string DisplayName(this SeaGlassShapeKind kind) => kind switch
    {
        SeaGlassShapeKind.Circle => "Circle Droplet",
        SeaGlassShapeKind.Capsule => "Liquid Capsule",
        SeaGlassShapeKind.Squircle => "Expressive Squircle",
        SeaGlassShapeKind.Arch => "Architectural Arch",
        SeaGlassShapeKind.Sunny => "8-Ray Sunny",
        SeaGlassShapeKind.Cookie4 => "4-Lobe Cookie",
        SeaGlassShapeKind.Cookie6 => "6-Lobe Scallop",
        SeaGlassShapeKind.Cookie7 => "7-Lobe Heptagon",
        SeaGlassShapeKind.Cookie9 => "9-Lobe Expressive",
        SeaGlassShapeKind.Cookie12 => "12-Lobe Medallion",
        SeaGlassShapeKind.Clover4 => "Deep 4-Leaf Clover",
        SeaGlassShapeKind.Clover8 => "8-Petal Rosette",
        SeaGlassShapeKind.Flower => "Organic Flower",
        SeaGlassShapeKind.SoftBurst => "Soft Starburst",
        SeaGlassShapeKind.Gem => "Octagonal Gem",
        SeaGlassShapeKind.Heart => "Expressive Heart",
        SeaGlassShapeKind.Diamond => "Puffy Diamond",
        SeaGlassShapeKind.Pentagon => "Soft Pentagon",
        SeaGlassShapeKind.Triangle => "Rounded Triangle",
        SeaGlassShapeKind.Slanted => "Expressive Slanted",
        _ => kind.ToString()
    };

    public static string ShortLabel(this SeaGlassShapeKind kind) => kind switch
    {
        SeaGlassShapeKind.Circle => "Circle",
        SeaGlassShapeKind.Capsule => "Pill",
        SeaGlassShapeKind.Squircle => "Squircle",
        SeaGlassShapeKind.Arch => "Arch",
        SeaGlassShapeKind.Sunny => "Sunny",
        SeaGlassShapeKind.Cookie4 => "Cookie 4",
        SeaGlassShapeKind.Cookie6 => "Cookie 6",
        SeaGlassShapeKind.Cookie7 => "Cookie 7",
        SeaGlassShapeKind.Cookie9 => "Cookie 9",
        SeaGlassShapeKind.Cookie12 => "Cookie 12",
        SeaGlassShapeKind.Clover4 => "Clover 4",
        SeaGlassShapeKind.Clover8 => "Clover 8",
        SeaGlassShapeKind.Flower => "Flower",
        SeaGlassShapeKind.SoftBurst => "Burst",
        SeaGlassShapeKind.Gem => "Gem",
        SeaGlassShapeKind.Heart => "Heart",
        SeaGlassShapeKind.Diamond => "Diamond",
        SeaGlassShapeKind.Pentagon => "Pentagon",
        SeaGlassShapeKind.Triangle => "Triangle",
        SeaGlassShapeKind.Slanted => "Slanted",
        _ => kind.ToString()
    };
}

/// <summary>
/// Defines a single stage in a Material 3 Expressive × Liquid Sea Glass shape choreography.
/// </summary>
public sealed record SeaGlassShapeStage(
    SeaGlassShapeKind Shape,
    float Scale = 1.0f,
    float AspectRatio = 1.0f,
    float RotationDegrees = 0f,
    bool ViaCircleBridge = true,
    string? Label = null)
{
    public string EffectiveLabel => Label ?? Shape.ShortLabel();
}

/// <summary>
/// Instantaneous spatial transform evaluated during a choreographed shape transition.
/// </summary>
public readonly record struct ChoreographedShapeTransform(
    float ScaleX,
    float ScaleY,
    float RotationDegrees,
    float MorphEnergy,
    SeaGlassShapeKind ActiveStartShape,
    SeaGlassShapeKind ActiveEndShape,
    float ActiveSubProgress);

/// <summary>
/// Normalized cubic Bezier segment in [0..1] space used by <see cref="SeaGlassNormalizedPolygon"/>.
/// </summary>
public readonly record struct CubicBezierSegment(
    float Anchor0X, float Anchor0Y,
    float Control0X, float Control0Y,
    float Control1X, float Control1Y,
    float Anchor1X, float Anchor1Y)
{
    public static CubicBezierSegment Lerp(CubicBezierSegment a, CubicBezierSegment b, float t) => new(
        Anchor0X: a.Anchor0X + (b.Anchor0X - a.Anchor0X) * t,
        Anchor0Y: a.Anchor0Y + (b.Anchor0Y - a.Anchor0Y) * t,
        Control0X: a.Control0X + (b.Control0X - a.Control0X) * t,
        Control0Y: a.Control0Y + (b.Control0Y - a.Control0Y) * t,
        Control1X: a.Control1X + (b.Control1X - a.Control1X) * t,
        Control1Y: a.Control1Y + (b.Control1Y - a.Control1Y) * t,
        Anchor1X: a.Anchor1X + (b.Anchor1X - a.Anchor1X) * t,
        Anchor1Y: a.Anchor1Y + (b.Anchor1Y - a.Anchor1Y) * t);
}

/// <summary>
/// Normalized Material 3 Expressive rounded polygon contour represented by a fixed-count ring
/// of cubic Bezier segments so any two shapes morph seamlessly with under-damped spring overshoot.
/// </summary>
public sealed class SeaGlassNormalizedPolygon
{
    public SeaGlassShapeKind Kind { get; }
    public float AspectRatio { get; }
    public IReadOnlyList<CubicBezierSegment> Cubics { get; }

    public SeaGlassNormalizedPolygon(SeaGlassShapeKind kind, float aspectRatio, IReadOnlyList<CubicBezierSegment> cubics)
    {
        Kind = kind;
        AspectRatio = aspectRatio;
        Cubics = cubics;
    }
}

/// <summary>
/// Material 3 Expressive 20-shape polygon morph engine, polar SDF LUT evaluator,
/// and dynamic Liquid Glass flex-material solver.
///
/// Exact 1:1 parity with <c>dev.seaglass.core.SeaGlassShapeMorph</c> on Android.
/// </summary>
public static class SeaGlassShapeMorph
{
    private const int PolarLutSize = 128;
    private const int CanonicalCubicCount = 24;

    private readonly record struct PolygonCacheKey(SeaGlassShapeKind Kind, int QuantizedAspect);

    private static readonly ConcurrentDictionary<PolygonCacheKey, SeaGlassNormalizedPolygon> PolygonCache = new();
    private static readonly ConcurrentDictionary<SeaGlassShapeKind, float[]> PolarLutCache = new();

    public static IReadOnlyList<SeaGlassShapeStage> DefaultPlayfulChoreography { get; } =
    [
        new(SeaGlassShapeKind.Flower, Scale: 1.06f, RotationDegrees: 0f, ViaCircleBridge: true, Label: "Flower Bloom"),
        new(SeaGlassShapeKind.Circle, Scale: 0.72f, RotationDegrees: 45f, ViaCircleBridge: false, Label: "Small Droplet"),
        new(SeaGlassShapeKind.Cookie9, Scale: 1.14f, RotationDegrees: 95f, ViaCircleBridge: true, Label: "Cookie 9"),
        new(SeaGlassShapeKind.Circle, Scale: 1.24f, RotationDegrees: 140f, ViaCircleBridge: false, Label: "Large Orb"),
        new(SeaGlassShapeKind.Clover4, Scale: 1.08f, RotationDegrees: 195f, ViaCircleBridge: true, Label: "4-Leaf Clover"),
        new(SeaGlassShapeKind.Capsule, Scale: 0.88f, AspectRatio: 1.48f, RotationDegrees: 180f, ViaCircleBridge: false, Label: "Liquid Pill"),
        new(SeaGlassShapeKind.Sunny, Scale: 1.15f, RotationDegrees: 240f, ViaCircleBridge: true, Label: "Sunny Star"),
        new(SeaGlassShapeKind.Heart, Scale: 1.10f, RotationDegrees: 360f, ViaCircleBridge: true, Label: "Heart Gem")
    ];

    /// <summary>
    /// Cubic Hermite S-curve (3u^2 - 2u^3) on [0..1], providing zero initial and terminal slope
    /// for smooth ease-in / ease-out transitions.
    /// </summary>
    public static float SCurve(float t)
    {
        float u = Math.Clamp(t, 0f, 1f);
        return u * u * (3f - 2f * u);
    }

    /// <summary>
    /// Quintic C2-continuous S-curve (6u^5 - 15u^4 + 10u^3) on [0..1], with zero first and second
    /// derivatives at both endpoints so inertia builds and settles without initial jerk.
    /// </summary>
    public static float SmootherStep(float t)
    {
        float u = Math.Clamp(t, 0f, 1f);
        return u * u * u * (u * (u * 6f - 15f) + 10f);
    }

    /// <summary>
    /// Physics-based S-curve (ease-in / ease-out) transfer function:
    /// Inside [0..1], blends linear momentum (0.52 * u) with Cubic Hermite smoothstep (0.48 * u^2 * (3 - 2u)).
    /// Preserves damped physical spring overshoot/undershoot outside [0..1] scaled by <paramref name="overshootGain"/>.
    /// </summary>
    public static float PhysicsSCurve(float progress, float overshootGain = 0.52f)
    {
        if (progress < 0f) return progress * overshootGain;
        if (progress > 1f) return 1f + (progress - 1f) * overshootGain;
        float h3 = progress * progress * (3f - 2f * progress);
        return 0.52f * progress + 0.48f * h3;
    }

    /// <summary>
    /// Evaluates a physics-based S-curve over the sub-window [start..end] of <paramref name="progress"/>.
    /// </summary>
    public static float SCurveWindow(float progress, float start, float end, float overshootGain = 0f)
    {
        float span = MathF.Max(end - start, 1e-4f);
        float raw = (progress - start) / span;
        return PhysicsSCurve(raw, overshootGain);
    }

    /// <summary>
    /// Eases from 0 to 1 over [0..until] along a physics S-curve and holds at 1 while carrying overshoot.
    /// </summary>
    public static float SCurveLead(float progress, float until, float overshootGain = 0.45f)
    {
        float baseVal = SCurveWindow(progress, 0f, MathF.Max(until, 1e-4f), overshootGain: 0f);
        float excess = progress > 1f ? (progress - 1f) * overshootGain : 0f;
        return baseVal + excess;
    }

    /// <summary>
    /// Smooth C2 bell curve peaking at 1.0 when progress == 0.5 and easing to 0.0 at both endpoints (0.0 and 1.0).
    /// </summary>
    public static float SCurveBell(float progress)
    {
        float s = SCurve(progress);
        return Math.Clamp(4f * s * (1f - s), 0f, 1f);
    }

    public static SeaGlassNormalizedPolygon ObtainNormalizedPolygon(SeaGlassShapeKind kind, float aspectRatio = 1f)
    {
        int qAspect = (int)MathF.Round(Math.Clamp(aspectRatio, 0.4f, 4.0f) * 20f);
        PolygonCacheKey key = new(kind, qAspect);
        return PolygonCache.GetOrAdd(key, static k => BuildNormalizedPolygonInternal(k.Kind, k.QuantizedAspect / 20f));
    }

    public static SeaGlassNormalizedPolygon CreateNormalizedPolygon(SeaGlassShapeKind kind, float aspectRatio = 1f) =>
        ObtainNormalizedPolygon(kind, aspectRatio);

    /// <summary>
    /// Evaluates the normalized polar radius scale r(theta) in [0.35, 1.15] of <paramref name="startKind"/>
    /// (or a morphed blend toward <paramref name="endKind"/>) at <paramref name="angleRad"/> in [-PI, PI].
    /// </summary>
    public static float EvaluatePolarRadiusScale(
        SeaGlassShapeKind startKind,
        SeaGlassShapeKind endKind,
        float progress,
        float angleRad)
    {
        float[] lutA = ObtainPolarLut(startKind);
        float rA = SamplePolarLut(lutA, angleRad);
        if (startKind == endKind || MathF.Abs(progress) < 1e-3f)
        {
            return rA;
        }
        float[] lutB = ObtainPolarLut(endKind);
        float rB = SamplePolarLut(lutB, angleRad);
        float shapedP = Math.Clamp(PhysicsSCurve(progress, overshootGain: 0.65f), -0.25f, 1.25f);
        return Math.Clamp(rA + (rB - rA) * shapedP, 0.35f, 1.15f);
    }

    public static float EvaluatePolarRadiusScale(SeaGlassShapeKind kind, float angleRad) =>
        EvaluatePolarRadiusScale(kind, kind, 0f, angleRad);

    /// <summary>
    /// Populates <paramref name="outPath"/> with the morphed contour between <paramref name="startKind"/>
    /// and <paramref name="endKind"/> at <paramref name="progress"/> (supporting under-damped spring
    /// overshoot in [-0.25..1.25]).
    /// </summary>
    public static void BuildMorphedPolygonPath(
        SeaGlassShapeKind startKind,
        SeaGlassShapeKind endKind,
        float progress,
        float widthPx,
        float heightPx,
        SeaGlassVectorPath2D outPath)
    {
        float aspect = Math.Clamp(widthPx / MathF.Max(heightPx, 1f), 0.4f, 4.0f);
        SeaGlassNormalizedPolygon startPoly = ObtainNormalizedPolygon(startKind, aspect);
        SeaGlassNormalizedPolygon endPoly = ObtainNormalizedPolygon(endKind, aspect);

        float elasticProgress = Math.Clamp(PhysicsSCurve(progress, overshootGain: 0.72f), -0.25f, 1.25f);
        float refT = SCurve(progress);
        float overshootMarginX = widthPx * 0.04f * MathF.Abs(elasticProgress - refT);
        float overshootMarginY = heightPx * 0.04f * MathF.Abs(elasticProgress - refT);
        float availW = MathF.Max(1f, widthPx - 2f * overshootMarginX);
        float availH = MathF.Max(1f, heightPx - 2f * overshootMarginY);

        // Preserve 1:1 radial symmetry for floral/cookie/gem/star shapes while allowing
        // capsules and squircles to expand across wide aspect ratios.
        float startStretch = IsAspectFlexibleShape(startKind) ? 1f : 0f;
        float endStretch = IsAspectFlexibleShape(endKind) ? 1f : 0f;
        float stretchBlend = Math.Clamp(startStretch + (endStretch - startStretch) * refT, 0f, 1f);
        float minSide = MathF.Min(availW, availH);
        float targetW = minSide + (availW - minSide) * stretchBlend;
        float targetH = minSide + (availH - minSide) * stretchBlend;
        float originX = (widthPx - targetW) * 0.5f;
        float originY = (heightPx - targetH) * 0.5f;

        outPath.Reset();
        IReadOnlyList<CubicBezierSegment> cA = startPoly.Cubics;
        IReadOnlyList<CubicBezierSegment> cB = endPoly.Cubics;
        int count = Math.Min(cA.Count, cB.Count);
        if (count == 0) return;

        CubicBezierSegment first = CubicBezierSegment.Lerp(cA[0], cB[0], elasticProgress);
        outPath.MoveTo(
            originX + first.Anchor0X * targetW,
            originY + first.Anchor0Y * targetH);

        for (int i = 0; i < count; i++)
        {
            CubicBezierSegment seg = CubicBezierSegment.Lerp(cA[i], cB[i], elasticProgress);
            outPath.CubicTo(
                originX + seg.Control0X * targetW,
                originY + seg.Control0Y * targetH,
                originX + seg.Control1X * targetW,
                originY + seg.Control1Y * targetH,
                originX + seg.Anchor1X * targetW,
                originY + seg.Anchor1Y * targetH);
        }

        outPath.Close();
    }

    private static bool IsAspectFlexibleShape(SeaGlassShapeKind kind) => kind switch
    {
        SeaGlassShapeKind.Capsule => true,
        SeaGlassShapeKind.Squircle => true,
        SeaGlassShapeKind.Arch => true,
        SeaGlassShapeKind.Slanted => true,
        _ => false
    };

    /// <summary>
    /// Morphs smoothly through an ordered sequence of N >= 2 <paramref name="shapes"/> driven by
    /// a continuous float <paramref name="continuousIndex"/>.
    /// </summary>
    public static void BuildSequenceMorphedPolygonPath(
        IReadOnlyList<SeaGlassShapeKind> shapes,
        float continuousIndex,
        float widthPx,
        float heightPx,
        SeaGlassVectorPath2D outPath)
    {
        if (shapes is null || shapes.Count == 0)
            throw new ArgumentException("shapes list must not be empty", nameof(shapes));

        if (shapes.Count == 1)
        {
            BuildMorphedPolygonPath(shapes[0], shapes[0], 0f, widthPx, heightPx, outPath);
            return;
        }

        int maxIdx = shapes.Count - 1;
        int baseIdx = Math.Clamp((int)MathF.Floor(Math.Clamp(continuousIndex, 0f, maxIdx - 1)), 0, maxIdx - 1);
        float localProgress = continuousIndex - baseIdx;

        BuildMorphedPolygonPath(
            startKind: shapes[baseIdx],
            endKind: shapes[baseIdx + 1],
            progress: localProgress,
            widthPx: widthPx,
            heightPx: heightPx,
            outPath: outPath);
    }

    /// <summary>
    /// Evaluates a two-phase Material 3 Expressive Morph-In (to Circle) -> Morph-Out (to Target)
    /// choreography between <paramref name="startStage"/> and <paramref name="endStage"/>.
    /// </summary>
    public static ChoreographedShapeTransform EvaluateChoreographedTransform(
        SeaGlassShapeStage startStage,
        SeaGlassShapeStage endStage,
        float progress)
    {
        float clamped = Math.Clamp(progress, 0f, 1f);
        float overshoot = progress - clamped;

        bool useCircleMidpoint = endStage.ViaCircleBridge &&
            startStage.Shape != endStage.Shape &&
            startStage.Shape != SeaGlassShapeKind.Circle &&
            endStage.Shape != SeaGlassShapeKind.Circle;

        SeaGlassShapeKind activeStart;
        SeaGlassShapeKind activeEnd;
        float subProgress;

        if (useCircleMidpoint)
        {
            const float pivot = 0.42f;
            if (clamped < pivot && progress <= pivot)
            {
                activeStart = startStage.Shape;
                activeEnd = SeaGlassShapeKind.Circle;
                float rawU = Math.Clamp(progress / pivot, -0.25f, 1.0f);
                subProgress = Math.Clamp(PhysicsSCurve(rawU, overshootGain: 0.55f), -0.25f, 1.0f);
            }
            else
            {
                activeStart = SeaGlassShapeKind.Circle;
                activeEnd = endStage.Shape;
                float rawOut = (progress - pivot) / (1f - pivot);
                subProgress = Math.Clamp(PhysicsSCurve(rawOut, overshootGain: 0.75f), 0f, 1.28f);
            }
        }
        else
        {
            activeStart = startStage.Shape;
            activeEnd = endStage.Shape;
            subProgress = Math.Clamp(PhysicsSCurve(progress, overshootGain: 0.72f), -0.25f, 1.28f);
        }

        float shapedProgress = PhysicsSCurve(progress, overshootGain: 0.65f);
        float clampedSCurve = SCurve(clamped);

        float baseScale = startStage.Scale + (endStage.Scale - startStage.Scale) * shapedProgress;
        float gatherDip = (useCircleMidpoint ? -0.15f : -0.07f) * MathF.Sin(clampedSCurve * MathF.PI);
        float effectiveScale = Math.Clamp(baseScale + gatherDip + overshoot * 0.18f, 0.42f, 1.65f);

        float currentAspect = Math.Clamp(
            startStage.AspectRatio + (endStage.AspectRatio - startStage.AspectRatio) * shapedProgress,
            0.55f,
            2.2f);

        float scaleX = effectiveScale * MathF.Sqrt(currentAspect);
        float scaleY = effectiveScale / MathF.Sqrt(currentAspect);
        float rotation = startStage.RotationDegrees + (endStage.RotationDegrees - startStage.RotationDegrees) * shapedProgress;
        float energy = Math.Clamp(MathF.Sin(clampedSCurve * MathF.PI) + MathF.Abs(overshoot) * 2.2f, 0f, 1f);

        return new ChoreographedShapeTransform(
            ScaleX: scaleX,
            ScaleY: scaleY,
            RotationDegrees: rotation,
            MorphEnergy: energy,
            ActiveStartShape: activeStart,
            ActiveEndShape: activeEnd,
            ActiveSubProgress: subProgress);
    }

    public static void BuildChoreographedPolygonPath(
        SeaGlassShapeStage startStage,
        SeaGlassShapeStage endStage,
        float progress,
        float widthPx,
        float heightPx,
        SeaGlassVectorPath2D outPath)
    {
        ChoreographedShapeTransform transform = EvaluateChoreographedTransform(startStage, endStage, progress);
        BuildMorphedPolygonPath(
            startKind: transform.ActiveStartShape,
            endKind: transform.ActiveEndShape,
            progress: transform.ActiveSubProgress,
            widthPx: widthPx,
            heightPx: heightPx,
            outPath: outPath);
    }

    /// <summary>
    /// Computes the dynamic Liquid Glass Flex &amp; Temporal De-Aging Material during an animation or transition:
    /// while in flight (<c>totalEnergy &gt; 0</c>), the material de-ages (<see cref="SeaGlassMaterial.Deaged"/>),
    /// shedding its coarse sand-frosted texture, clarifying into malleable liquid glass, and flexing its
    /// refractive lens (<c>Ior</c>, <c>ChromaticDispersion</c>, <c>ThicknessDp</c>). As the animation settles,
    /// the glass solidifies and regains its full sand-frosted surface treatment.
    /// </summary>
    public static SeaGlassMaterial ComputeDynamicFlexMaterial(
        SeaGlassMaterial startMaterial,
        SeaGlassMaterial endMaterial,
        float progress,
        float velocityPxPerSec = 0f,
        float areaExpansionRatio = 1f)
    {
        float p = SCurve(progress);
        SeaGlassMaterial baseMat = SeaGlassMaterial.Lerp(startMaterial, endMaterial, p);

        float morphEnergy = SCurveBell(progress);
        float velocityEnergy = Math.Clamp(MathF.Abs(velocityPxPerSec) / 1800f, 0f, 1f);
        float totalEnergy = Math.Clamp(MathF.Max(morphEnergy, velocityEnergy * 0.75f), 0f, 1f);
        float scaleFactor = MathF.Sqrt(Math.Clamp(areaExpansionRatio, 0.5f, 4.0f));
        float melt = Math.Clamp(totalEnergy * (1.35f - 0.35f * totalEnergy), 0f, 1f);
        float targetClearTransparency = MathF.Max(baseMat.Transparency, 0.92f);

        return baseMat with
        {
            Coarseness = Math.Clamp(baseMat.Coarseness * (1f - 0.88f * melt), 0f, 1f),
            Transparency = Math.Clamp(baseMat.Transparency + (targetClearTransparency - baseMat.Transparency) * 0.80f * melt, 0f, 1f),
            FrostRadiusDp = MathF.Max(0f, baseMat.FrostRadiusDp * (1f - 0.82f * melt) + 2.0f * melt),
            Ior = Math.Clamp(baseMat.Ior + 0.10f * totalEnergy + 0.04f * (scaleFactor - 1f), 1.0f, 1.95f),
            ChromaticDispersion = Math.Clamp(baseMat.ChromaticDispersion + 0.22f * totalEnergy, 0f, 1f),
            RimChalkiness = Math.Clamp(baseMat.RimChalkiness + 0.16f * totalEnergy, 0f, 1f),
            ThicknessDp = Math.Clamp(
                baseMat.ThicknessDp * (1f + 0.20f * totalEnergy) * Math.Clamp(scaleFactor, 0.85f, 1.35f),
                4f,
                36f)
        };
    }

    private static float[] ObtainPolarLut(SeaGlassShapeKind kind)
    {
        return PolarLutCache.GetOrAdd(kind, static k =>
        {
            float[] lut = new float[PolarLutSize];
            for (int i = 0; i < PolarLutSize; i++)
            {
                float angle = -MathF.PI + (2.0f * MathF.PI * i / PolarLutSize);
                lut[i] = EvaluateAnalyticalPolarRadius(k, angle);
            }

            // 2-pass 3-tap circular smoothing filter matching Android
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < PolarLutSize; i++)
                {
                    int prev = (i - 1 + PolarLutSize) % PolarLutSize;
                    int next = (i + 1) % PolarLutSize;
                    lut[i] = lut[i] * 0.5f + (lut[prev] + lut[next]) * 0.25f;
                }
            }
            return lut;
        });
    }

    private static float SamplePolarLut(float[] lut, float angleRad)
    {
        float norm = (angleRad + MathF.PI) / (2.0f * MathF.PI);
        norm -= MathF.Floor(norm);
        float pos = norm * PolarLutSize;
        int i0 = ((int)pos) % PolarLutSize;
        int i1 = (i0 + 1) % PolarLutSize;
        float frac = pos - MathF.Floor(pos);
        return lut[i0] + (lut[i1] - lut[i0]) * frac;
    }

    private static float EvaluateAnalyticalPolarRadius(SeaGlassShapeKind kind, float angle)
    {
        float c = MathF.Cos(angle);
        float s = MathF.Sin(angle);

        float r = kind switch
        {
            SeaGlassShapeKind.Circle => 0.98f,
            SeaGlassShapeKind.Capsule => 0.90f + 0.08f * MathF.Cos(2f * angle),
            SeaGlassShapeKind.Squircle => 1.0f / MathF.Pow(MathF.Pow(MathF.Abs(c), 2.5f) + MathF.Pow(MathF.Abs(s), 2.5f), 1f / 2.5f) * 0.88f,
            SeaGlassShapeKind.Arch => s < 0f
                ? 0.94f
                : 1.0f / MathF.Pow(MathF.Pow(MathF.Abs(c), 3.0f) + MathF.Pow(MathF.Abs(s), 3.0f), 1f / 3.0f) * 0.90f,
            SeaGlassShapeKind.Sunny => 0.88f + 0.11f * MathF.Cos(8f * angle),
            SeaGlassShapeKind.Cookie4 => 0.84f + 0.15f * MathF.Cos(4f * angle),
            SeaGlassShapeKind.Cookie6 => 0.86f + 0.13f * MathF.Cos(6f * angle),
            SeaGlassShapeKind.Cookie7 => 0.87f + 0.12f * MathF.Cos(7f * angle),
            SeaGlassShapeKind.Cookie9 => 0.89f + 0.10f * MathF.Cos(9f * angle),
            SeaGlassShapeKind.Cookie12 => 0.91f + 0.08f * MathF.Cos(12f * angle),
            SeaGlassShapeKind.Clover4 => 0.76f + 0.22f * MathF.Max(-0.6f, MathF.Cos(4f * angle)),
            SeaGlassShapeKind.Clover8 => 0.82f + 0.16f * MathF.Cos(8f * angle),
            SeaGlassShapeKind.Flower => 0.80f + 0.19f * MathF.Cos(6f * angle),
            SeaGlassShapeKind.SoftBurst => 0.86f + 0.13f * MathF.Cos(10f * angle),
            SeaGlassShapeKind.Gem => 0.92f + 0.06f * MathF.Cos(8f * angle),
            SeaGlassShapeKind.Heart => 0.82f - 0.14f * s + 0.12f * MathF.Abs(c) * (1f - s * 0.5f),
            SeaGlassShapeKind.Diamond => 0.88f + 0.11f * MathF.Cos(4f * angle),
            SeaGlassShapeKind.Pentagon => 0.90f + 0.08f * MathF.Cos(5f * angle - MathF.PI * 0.5f),
            SeaGlassShapeKind.Triangle => 0.85f + 0.13f * MathF.Cos(3f * angle - MathF.PI * 0.5f),
            SeaGlassShapeKind.Slanted => 0.90f + 0.08f * MathF.Sin(2f * angle),
            _ => 0.95f
        };

        return Math.Clamp(r, 0.35f, 1.15f);
    }

    private static SeaGlassNormalizedPolygon BuildNormalizedPolygonInternal(SeaGlassShapeKind kind, float aspectRatio)
    {
        float[] px = new float[CanonicalCubicCount];
        float[] py = new float[CanonicalCubicCount];

        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;

        for (int i = 0; i < CanonicalCubicCount; i++)
        {
            float angle = -MathF.PI + (2.0f * MathF.PI * i / CanonicalCubicCount);
            float r = EvaluateAnalyticalPolarRadius(kind, angle);
            float x = MathF.Cos(angle) * r * aspectRatio;
            float y = MathF.Sin(angle) * r;
            px[i] = x;
            py[i] = y;
            if (x < minX) minX = x;
            if (y < minY) minY = y;
            if (x > maxX) maxX = x;
            if (y > maxY) maxY = y;
        }

        float spanX = MathF.Max(1e-4f, maxX - minX);
        float spanY = MathF.Max(1e-4f, maxY - minY);

        for (int i = 0; i < CanonicalCubicCount; i++)
        {
            px[i] = (px[i] - minX) / spanX;
            py[i] = (py[i] - minY) / spanY;
        }

        List<CubicBezierSegment> cubics = new(CanonicalCubicCount);
        for (int i = 0; i < CanonicalCubicCount; i++)
        {
            int p0 = (i - 1 + CanonicalCubicCount) % CanonicalCubicCount;
            int p1 = i;
            int p2 = (i + 1) % CanonicalCubicCount;
            int p3 = (i + 2) % CanonicalCubicCount;

            float t1x = (px[p2] - px[p0]) / 6f;
            float t1y = (py[p2] - py[p0]) / 6f;
            float t2x = (px[p3] - px[p1]) / 6f;
            float t2y = (py[p3] - py[p1]) / 6f;

            cubics.Add(new CubicBezierSegment(
                Anchor0X: px[p1],
                Anchor0Y: py[p1],
                Control0X: px[p1] + t1x,
                Control0Y: py[p1] + t1y,
                Control1X: px[p2] - t2x,
                Control1Y: py[p2] - t2y,
                Anchor1X: px[p2],
                Anchor1Y: py[p2]));
        }

        return new SeaGlassNormalizedPolygon(kind, aspectRatio, cubics);
    }
}
