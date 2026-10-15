using System;

namespace SeaGlass.Core;

/// <summary>
/// Physical optical specification of a SeaGlass surface.
///
/// Direct 1:1 parity with <c>dev.seaglass.core.SeaGlassMaterial</c> on Android:
/// 1. Frosted Glass Transparency (<see cref="Transparency"/>)
/// 2. Sanded Micro-Coarseness (<see cref="Coarseness"/>, <see cref="FrostRadiusDp"/>)
/// 3. Volumetric Pastel Body Absorption (<see cref="TintColorArgb"/>, <see cref="TintDensity"/>, <see cref="ThicknessDp"/>)
/// 4. Liquid Glass Edge Refraction (<see cref="Ior"/>, <see cref="ChromaticDispersion"/>, <see cref="RimChalkiness"/>)
/// </summary>
public sealed record SeaGlassMaterial
{
    public int TintColorArgb { get; set; }
    public float TintDensity { get; set; }
    public float Coarseness { get; set; }
    public float FrostRadiusDp { get; set; }
    public float Ior { get; set; }
    public float ChromaticDispersion { get; set; }
    public float RimChalkiness { get; set; }
    public float ThicknessDp { get; set; }
    public float CornerRadiusDp { get; set; }
    public float CornerSmoothing { get; set; }
    public float AmbientAdaptation { get; set; }
    public float Transparency { get; set; }

    public void CopyFrom(SeaGlassMaterial other)
    {
        TintColorArgb = other.TintColorArgb;
        TintDensity = other.TintDensity;
        Coarseness = other.Coarseness;
        FrostRadiusDp = other.FrostRadiusDp;
        Ior = other.Ior;
        ChromaticDispersion = other.ChromaticDispersion;
        RimChalkiness = other.RimChalkiness;
        ThicknessDp = other.ThicknessDp;
        CornerRadiusDp = other.CornerRadiusDp;
        CornerSmoothing = other.CornerSmoothing;
        AmbientAdaptation = other.AmbientAdaptation;
        Transparency = other.Transparency;
    }

    public SeaGlassMaterial(
        int tintColorArgb,
        float tintDensity = 0.48f,
        float coarseness = 0.38f,
        float frostRadiusDp = 20f,
        float ior = 1.54f,
        float chromaticDispersion = 0.28f,
        float rimChalkiness = 0.65f,
        float thicknessDp = 16f,
        float cornerRadiusDp = 28f,
        float cornerSmoothing = 2.45f,
        float ambientAdaptation = 0.35f,
        float transparency = 0.72f)
    {
        if (tintDensity is < 0f or > 1f)
            throw new ArgumentOutOfRangeException(nameof(tintDensity), $"tintDensity must be in [0f, 1f], was {tintDensity}");
        if (coarseness is < 0f or > 1f)
            throw new ArgumentOutOfRangeException(nameof(coarseness), $"coarseness must be in [0f, 1f], was {coarseness}");
        if (frostRadiusDp < 0f)
            throw new ArgumentOutOfRangeException(nameof(frostRadiusDp), $"frostRadiusDp must be >= 0f, was {frostRadiusDp}");
        if (ior is < 1.0f or > 2.0f)
            throw new ArgumentOutOfRangeException(nameof(ior), $"ior must be in [1.0f, 2.0f], was {ior}");
        if (chromaticDispersion is < 0f or > 1f)
            throw new ArgumentOutOfRangeException(nameof(chromaticDispersion), $"chromaticDispersion must be in [0f, 1f], was {chromaticDispersion}");
        if (rimChalkiness is < 0f or > 1f)
            throw new ArgumentOutOfRangeException(nameof(rimChalkiness), $"rimChalkiness must be in [0f, 1f], was {rimChalkiness}");
        if (thicknessDp < 1f)
            throw new ArgumentOutOfRangeException(nameof(thicknessDp), $"thicknessDp must be >= 1f, was {thicknessDp}");
        if (cornerRadiusDp < 0f)
            throw new ArgumentOutOfRangeException(nameof(cornerRadiusDp), $"cornerRadiusDp must be >= 0f, was {cornerRadiusDp}");
        if (cornerSmoothing is < 2.0f or > 4.0f)
            throw new ArgumentOutOfRangeException(nameof(cornerSmoothing), $"cornerSmoothing must be in [2.0f, 4.0f], was {cornerSmoothing}");
        if (ambientAdaptation is < 0f or > 1f)
            throw new ArgumentOutOfRangeException(nameof(ambientAdaptation), $"ambientAdaptation must be in [0f, 1f], was {ambientAdaptation}");
        if (transparency is < 0f or > 1f)
            throw new ArgumentOutOfRangeException(nameof(transparency), $"transparency must be in [0f, 1f], was {transparency}");

        TintColorArgb = tintColorArgb;
        TintDensity = tintDensity;
        Coarseness = coarseness;
        FrostRadiusDp = frostRadiusDp;
        Ior = ior;
        ChromaticDispersion = chromaticDispersion;
        RimChalkiness = rimChalkiness;
        ThicknessDp = thicknessDp;
        CornerRadiusDp = cornerRadiusDp;
        CornerSmoothing = cornerSmoothing;
        AmbientAdaptation = ambientAdaptation;
        Transparency = transparency;
    }

    /// <summary>
    /// Evaluates the unified Sand-Frosted Continuum (<c>0f..1f</c>) for this material:
    /// <list type="bullet">
    /// <item><description><c>0.0f</c>: Completely Clear Untreated Glass (<c>Coarseness == 0f</c> and <c>Transparency == 1f</c>).</description></item>
    /// <item><description><c>0.0f..1.0f</c>: Progressive ocean sand abrasion over time.</description></item>
    /// <item><description><c>1.0f</c>: Heavily Sand-Frosted Opaque Glass (<c>Coarseness == 1f</c> / <c>Transparency == 0f</c>).</description></item>
    /// </list>
    /// </summary>
    public float EffectiveSandWear
    {
        get
        {
            float frostOpacity = Math.Clamp(1f - Transparency, 0f, 1f);
            float clearGate = Math.Clamp(frostOpacity * 4.5f + Coarseness * 3.5f, 0f, 1f);
            return Math.Clamp((Coarseness * 0.62f + frostOpacity * 0.58f) * clearGate, 0f, 1f);
        }
    }

    /// <summary>
    /// Generates a <see cref="SeaGlassMaterial"/> at any point along the continuous
    /// Sand-Frosted Coarseness Continuum (<paramref name="sandWear"/> in [0, 1]).
    /// </summary>
    public SeaGlassMaterial WithSandWear(float sandWear)
    {
        float w = Math.Clamp(sandWear, 0f, 1f);
        return this with
        {
            Coarseness = w,
            Transparency = Math.Clamp(1f - w * 0.92f, 0.04f, 1.0f),
            FrostRadiusDp = MathF.Max(0f, 1.5f + w * 42f),
            RimChalkiness = Math.Clamp(0.32f + w * 0.60f, 0f, 1f),
            ChromaticDispersion = Math.Clamp(ChromaticDispersion * (1f - 0.55f * w), 0f, 1f)
        };
    }

    /// <summary>
    /// Evaluates Temporal De-Aging &amp; Solidification into a pre-allocated target material instance.
    /// </summary>
    public void DeagedInto(float malleability, SeaGlassMaterial target)
    {
        target.CopyFrom(this);
        float m = Math.Clamp(malleability, 0f, 1f);
        if (m <= 0.005f) return;
        float melt = Math.Clamp(m * (1.35f - 0.35f * m), 0f, 1f);
        float targetClear = MathF.Max(Transparency, 0.92f);
        target.Coarseness = Math.Clamp(Coarseness * (1f - 0.88f * melt), 0f, 1f);
        target.Transparency = Math.Clamp(Transparency + (targetClear - Transparency) * 0.80f * melt, 0f, 1f);
        target.FrostRadiusDp = MathF.Max(0f, FrostRadiusDp * (1f - 0.82f * melt) + 2.0f * melt);
        target.Ior = Math.Clamp(Ior + 0.10f * melt, 1.0f, 2.0f);
        target.ChromaticDispersion = Math.Clamp(ChromaticDispersion + 0.22f * melt, 0f, 1f);
        target.RimChalkiness = Math.Clamp(RimChalkiness + 0.16f * melt, 0f, 1f);
        target.ThicknessDp = Math.Clamp(ThicknessDp * (1f + 0.20f * melt), 1f, 36f);
    }

    /// <summary>
    /// Evaluates Temporal De-Aging &amp; Solidification on this material during motion or morphing:
    /// while the glass is malleable and moving (<paramref name="malleability"/> -&gt; 1.0), hydrodynamic
    /// friction melts surface micro-pits toward zero coarseness, clarifies body transparency toward 0.92+,
    /// and flexes refractive thickness. Settling (<paramref name="malleability"/> -&gt; 0.0) solidifies
    /// and restores the resting frosted texture.
    /// </summary>
    public SeaGlassMaterial Deaged(float malleability)
    {
        float m = Math.Clamp(malleability, 0f, 1f);
        if (m <= 0.005f) return this;
        var result = new SeaGlassMaterial(0);
        DeagedInto(m, result);
        return result;
    }

    /// <summary>
    /// Evaluates touch/pointer press physical compression into a pre-allocated target material instance.
    /// </summary>
    public void PressedInto(float pressProgress, SeaGlassMaterial target)
    {
        target.CopyFrom(this);
        float p = Math.Clamp(pressProgress, 0f, 1f);
        if (p <= 0f) return;
        target.TintDensity = Math.Clamp(TintDensity + 0.08f * p, 0f, 1f);
        target.RimChalkiness = Math.Clamp(RimChalkiness + 0.16f * p, 0f, 1f);
        target.ThicknessDp = MathF.Min(ThicknessDp * (1f + 0.18f * p), 36f);
        target.CornerRadiusDp = MathF.Max(CornerRadiusDp * (1f - 0.10f * p), 8f);
    }

    /// <summary>
    /// Returns a physically compressed state of this material for touch/pointer press interactions:
    /// pressing the glass increases volumetric body tint saturation, enhances chalky rim light,
    /// and expands optical lens thickness.
    /// </summary>
    public SeaGlassMaterial Pressed(float pressProgress = 1f)
    {
        float p = Math.Clamp(pressProgress, 0f, 1f);
        if (p <= 0f) return this;
        var result = new SeaGlassMaterial(0);
        PressedInto(p, result);
        return result;
    }

    /// <summary>
    /// Returns an elevated/expanded state of this material with deeper subsurface frost
    /// and wider refractive lens thickness.
    /// </summary>
    public SeaGlassMaterial Elevated(float elevationProgress = 1f)
    {
        float e = Math.Clamp(elevationProgress, 0f, 1f);
        if (e == 0f) return this;
        return this with
        {
            FrostRadiusDp = FrostRadiusDp + 6f * e,
            ThicknessDp = MathF.Min(ThicknessDp + 5f * e, 36f),
            RimChalkiness = Math.Clamp(RimChalkiness + 0.10f * e, 0f, 1f)
        };
    }

    /// <summary>
    /// Perceptually interpolates two <see cref="SeaGlassMaterial"/> instances using Oklab color space
    /// for <see cref="TintColorArgb"/> and linear interpolation for optical scalar parameters.
    /// </summary>
    public static SeaGlassMaterial Lerp(SeaGlassMaterial start, SeaGlassMaterial stop, float fraction)
    {
        float t = Math.Clamp(fraction, 0f, 1f);
        if (t <= 0f) return start;
        if (t >= 1f) return stop;

        return new SeaGlassMaterial(
            tintColorArgb: SeaGlassColorMath.LerpOklab(start.TintColorArgb, stop.TintColorArgb, t),
            tintDensity: start.TintDensity + (stop.TintDensity - start.TintDensity) * t,
            coarseness: start.Coarseness + (stop.Coarseness - start.Coarseness) * t,
            frostRadiusDp: start.FrostRadiusDp + (stop.FrostRadiusDp - start.FrostRadiusDp) * t,
            ior: start.Ior + (stop.Ior - start.Ior) * t,
            chromaticDispersion: start.ChromaticDispersion + (stop.ChromaticDispersion - start.ChromaticDispersion) * t,
            rimChalkiness: start.RimChalkiness + (stop.RimChalkiness - start.RimChalkiness) * t,
            thicknessDp: start.ThicknessDp + (stop.ThicknessDp - start.ThicknessDp) * t,
            cornerRadiusDp: start.CornerRadiusDp + (stop.CornerRadiusDp - start.CornerRadiusDp) * t,
            cornerSmoothing: start.CornerSmoothing + (stop.CornerSmoothing - start.CornerSmoothing) * t,
            ambientAdaptation: start.AmbientAdaptation + (stop.AmbientAdaptation - start.AmbientAdaptation) * t,
            transparency: start.Transparency + (stop.Transparency - start.Transparency) * t
        );
    }

    public static SeaGlassMaterial SeafoamMint(
        float coarseness = 0.38f,
        float frostRadiusDp = 20f,
        float tintDensity = 0.48f,
        float transparency = 0.72f) =>
        SeaGlassPalette.SeafoamMint.PrimaryGlass with
        {
            Coarseness = coarseness,
            FrostRadiusDp = frostRadiusDp,
            TintDensity = tintDensity,
            Transparency = transparency
        };

    public static SeaGlassMaterial CobaltTide(
        float coarseness = 0.34f,
        float frostRadiusDp = 22f,
        float tintDensity = 0.52f,
        float transparency = 0.72f) =>
        SeaGlassPalette.CobaltTide.PrimaryGlass with
        {
            Coarseness = coarseness,
            FrostRadiusDp = frostRadiusDp,
            TintDensity = tintDensity,
            Transparency = transparency
        };

    public static SeaGlassMaterial BeachAmber(
        float coarseness = 0.42f,
        float frostRadiusDp = 20f,
        float tintDensity = 0.48f,
        float transparency = 0.72f) =>
        SeaGlassPalette.BeachAmber.PrimaryGlass with
        {
            Coarseness = coarseness,
            FrostRadiusDp = frostRadiusDp,
            TintDensity = tintDensity,
            Transparency = transparency
        };

    public static SeaGlassMaterial SeaLavender(
        float coarseness = 0.40f,
        float frostRadiusDp = 21f,
        float tintDensity = 0.48f,
        float transparency = 0.72f) =>
        SeaGlassPalette.SeaLavender.PrimaryGlass with
        {
            Coarseness = coarseness,
            FrostRadiusDp = frostRadiusDp,
            TintDensity = tintDensity,
            Transparency = transparency
        };
}
