using System;
using System.Collections.Generic;
using System.Numerics;
using SeaGlass.Core;
using SeaGlass.Core.Backend;
using SeaGlass.WinUI;
using Xunit;

namespace SeaGlass.Tests.E2E.Infrastructure;

/// <summary>
/// Injectable mock system settings provider for verifying R4 live Windows settings interop
/// (AnimationsEnabled, TransparencyEnabled, HighContrastEnabled, EnergySaverStatus, TextScaleFactor).
/// </summary>
public sealed class MockSystemSettingsProvider
{
    public bool AnimationsEnabled { get; set; } = true;
    public bool TransparencyEffectsEnabled { get; set; } = true;
    public bool HighContrastEnabled { get; set; } = false;
    public bool EnergySaverActive { get; set; } = false;
    public float TextScaleFactor { get; set; } = 1.0f;
    public float RasterizationScale { get; set; } = 1.0f;

    public SeaGlassDeviceTier ResolveEffectiveTier(SeaGlassDeviceTier baseTier = SeaGlassDeviceTier.D2D1HlslFull)
    {
        if (HighContrastEnabled)
        {
            return SeaGlassDeviceTier.LightweightTint;
        }

        if (!TransparencyEffectsEnabled || EnergySaverActive)
        {
            return SeaGlassDeviceTier.LightweightTint;
        }

        return baseTier;
    }

    public float AdjustDeltaTimeForAnimations(float dtSeconds)
    {
        // When animations are disabled, motion springs should snap immediately to target
        return AnimationsEnabled ? dtSeconds : 1000f;
    }

    public float ScaleText(float baseSizeSp)
    {
        return baseSizeSp * Math.Clamp(TextScaleFactor, 1.0f, 2.5f);
    }
}

/// <summary>
/// Mathematical oracle for 2D squircle Signed Distance Fields and polynomial cubic smooth-min
/// derived independently from Quilez and Superellipse mathematics (authoritative expected output source).
/// </summary>
public static class SdfOracle
{
    public static float SuperellipseDistance(float px, float py, float cx, float cy, float halfW, float halfH, float r, float p)
    {
        float qx = MathF.Abs(px - cx) - (halfW - r);
        float qy = MathF.Abs(py - cy) - (halfH - r);

        float maxQx0 = MathF.Max(qx, 0f);
        float maxQy0 = MathF.Max(qy, 0f);

        float cornerDist = MathF.Pow(MathF.Pow(maxQx0, p) + MathF.Pow(maxQy0, p), 1f / p);
        float insideDist = MathF.Min(MathF.Max(qx, qy), 0f);

        return insideDist + cornerDist - r;
    }

    public static float SminCubic(float d1, float d2, float k)
    {
        if (k <= 0f) return MathF.Min(d1, d2);
        float h = MathF.Max(k - MathF.Abs(d1 - d2), 0f) / k;
        return MathF.Min(d1, d2) - (1f / 6f) * h * h * h * k;
    }
}

/// <summary>
/// Mathematical oracle for perceptual Oklab color conversions and Beer-Lambert volumetric absorption
/// derived from the authoritative Björn Ottosson specification (authoritative expected output source).
/// </summary>
public static class ColorOracle
{
    public static (float L, float A, float B) SrgbToOklab(float r, float g, float b)
    {
        // Linear sRGB to LMS
        float l = 0.4122214708f * r + 0.5363325363f * g + 0.0514459929f * b;
        float m = 0.2119034982f * r + 0.6806995451f * g + 0.1073969566f * b;
        float s = 0.0883024619f * r + 0.2817188376f * g + 0.6299787005f * b;

        float l_ = MathF.Cbrt(MathF.Max(0f, l));
        float m_ = MathF.Cbrt(MathF.Max(0f, m));
        float s_ = MathF.Cbrt(MathF.Max(0f, s));

        return (
            0.2104542553f * l_ + 0.7936177850f * m_ - 0.0040720468f * s_,
            1.9779984951f * l_ - 2.4285922050f * m_ + 0.4505937099f * s_,
            0.0259040371f * l_ + 0.7827717662f * m_ - 0.8086757660f * s_
        );
    }

    public static (float R, float G, float B) BeerLambertTransmittance(
        (float R, float G, float B) backdrop,
        (float R, float G, float B) tint,
        float density,
        float thickness)
    {
        // T = exp(-sigma * thickness)
        float sigmaR = (1f - tint.R) * density * 2.2f;
        float sigmaG = (1f - tint.G) * density * 2.2f;
        float sigmaB = (1f - tint.B) * density * 2.2f;

        float transR = backdrop.R * MathF.Exp(-sigmaR * thickness);
        float transG = backdrop.G * MathF.Exp(-sigmaG * thickness);
        float transB = backdrop.B * MathF.Exp(-sigmaB * thickness);

        return (Math.Clamp(transR, 0f, 1f), Math.Clamp(transG, 0f, 1f), Math.Clamp(transB, 0f, 1f));
    }
}

/// <summary>
/// Mathematical oracle for harmonic oscillator dual springs (authoritative expected output source).
/// </summary>
public static class SpringOracle
{
    public static float AngularFrequency(float stiffness) => MathF.Sqrt(MathF.Max(1f, stiffness));

    public static TimeSpan Period(float stiffness) =>
        TimeSpan.FromSeconds(2.0 * Math.PI / Math.Sqrt(Math.Max(1.0, stiffness)));

    public static bool IsUnderDamped(float dampingRatio) => dampingRatio < 0.999f;
    public static bool IsCriticallyDamped(float dampingRatio) => MathF.Abs(dampingRatio - 1.0f) <= 0.001f;
}
