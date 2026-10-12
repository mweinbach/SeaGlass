using System;

namespace SeaGlass.Core;

/// <summary>
/// Perceptual Oklab / Oklch color space conversions and Beer-Lambert volumetric
/// optical absorption math for SeaGlass.
///
/// Direct 1:1 mathematical parity with <c>dev.seaglass.core.SeaGlassColorMath</c> on Android.
/// </summary>
public static class SeaGlassColorMath
{
    /// <summary>
    /// Packed perceptual Oklab color with alpha.
    /// </summary>
    public readonly record struct OklabColor(float L, float A, float B, float Alpha = 1f);

    /// <summary>
    /// Converts an 8-bit sRGB channel [0..255] to linear light [0f..1f].
    /// </summary>
    public static float SrgbChannelToLinear(int channel)
    {
        float c = Math.Clamp(channel, 0, 255) / 255f;
        return c <= 0.04045f
            ? c / 12.92f
            : MathF.Pow((c + 0.055f) / 1.055f, 2.4f);
    }

    /// <summary>
    /// Converts a linear light channel [0f..1f] to an 8-bit sRGB channel [0..255].
    /// </summary>
    public static int LinearChannelToSrgb(float linear)
    {
        float clamped = Math.Clamp(linear, 0f, 1f);
        float srgb = clamped <= 0.0031308f
            ? 12.92f * clamped
            : 1.055f * MathF.Pow(clamped, 1f / 2.4f) - 0.055f;
        return Math.Clamp((int)MathF.Round(srgb * 255f), 0, 255);
    }

    /// <summary>
    /// Converts a packed 0xAARRGGBB color integer to perceptual Oklab.
    /// </summary>
    public static OklabColor ArgbToOklab(int argb)
    {
        uint u = unchecked((uint)argb);
        float alpha = ((u >> 24) & 0xFFu) / 255f;
        float r = SrgbChannelToLinear((int)((u >> 16) & 0xFFu));
        float g = SrgbChannelToLinear((int)((u >> 8) & 0xFFu));
        float b = SrgbChannelToLinear((int)(u & 0xFFu));

        float l = 0.4122214708f * r + 0.5363325363f * g + 0.0514459929f * b;
        float m = 0.2119034982f * r + 0.6806995451f * g + 0.1073969566f * b;
        float s = 0.0883024619f * r + 0.2817188376f * g + 0.6299787005f * b;

        float lCbrt = MathF.Cbrt(MathF.Max(0f, l));
        float mCbrt = MathF.Cbrt(MathF.Max(0f, m));
        float sCbrt = MathF.Cbrt(MathF.Max(0f, s));

        return new OklabColor(
            L: 0.2104542553f * lCbrt + 0.7936177850f * mCbrt - 0.0040720468f * sCbrt,
            A: 1.9779984951f * lCbrt - 2.4285922050f * mCbrt + 0.4505937099f * sCbrt,
            B: 0.0259040371f * lCbrt + 0.7827717662f * mCbrt - 0.8086757660f * sCbrt,
            Alpha: alpha
        );
    }

    /// <summary>
    /// Converts a perceptual Oklab color back to a packed 0xAARRGGBB integer.
    /// </summary>
    public static int OklabToArgb(OklabColor color)
    {
        float lCbrt = color.L + 0.3963377774f * color.A + 0.2158037573f * color.B;
        float mCbrt = color.L - 0.1055613458f * color.A - 0.0638541728f * color.B;
        float sCbrt = color.L - 0.0894841775f * color.A - 1.2914855480f * color.B;

        float l = lCbrt * lCbrt * lCbrt;
        float m = mCbrt * mCbrt * mCbrt;
        float s = sCbrt * sCbrt * sCbrt;

        float rLinear = +4.0767416621f * l - 3.3077115913f * m + 0.2309699292f * s;
        float gLinear = -1.2684380046f * l + 2.6097574011f * m - 0.3413193965f * s;
        float bLinear = -0.0041960863f * l - 0.7034186147f * m + 1.7076147010f * s;

        int aInt = Math.Clamp((int)MathF.Round(Math.Clamp(color.Alpha, 0f, 1f) * 255f), 0, 255);
        int rInt = LinearChannelToSrgb(rLinear);
        int gInt = LinearChannelToSrgb(gLinear);
        int bInt = LinearChannelToSrgb(bLinear);

        return (aInt << 24) | (rInt << 16) | (gInt << 8) | bInt;
    }

    /// <summary>
    /// Perceptually uniform interpolation between two ARGB colors in Oklab space.
    /// </summary>
    public static int LerpOklab(int startArgb, int stopArgb, float fraction)
    {
        float t = Math.Clamp(fraction, 0f, 1f);
        if (t <= 0f) return startArgb;
        if (t >= 1f) return stopArgb;

        OklabColor c0 = ArgbToOklab(startArgb);
        OklabColor c1 = ArgbToOklab(stopArgb);
        return OklabToArgb(new OklabColor(
            L: c0.L + (c1.L - c0.L) * t,
            A: c0.A + (c1.A - c0.A) * t,
            B: c0.B + (c1.B - c0.B) * t,
            Alpha: c0.Alpha + (c1.Alpha - c0.Alpha) * t
        ));
    }

    /// <summary>
    /// Evaluates the Beer-Lambert volumetric sea-glass transmission of a backdrop color
    /// through a tumbled sea-glass slab.
    /// </summary>
    public static int ComputeBeerLambertTransmission(
        int backdropArgb,
        int tintArgb,
        float tintDensity,
        float coarseness,
        float normalizedThickness = 1.0f)
    {
        float density = Math.Clamp(tintDensity, 0f, 1f);
        float grit = Math.Clamp(coarseness, 0f, 1f);
        float thickness = Math.Clamp(normalizedThickness, 0.05f, 2.5f);
        float opticalDepth = density * (0.65f + 0.35f * thickness);

        uint uBg = unchecked((uint)backdropArgb);
        uint uTint = unchecked((uint)tintArgb);

        float bgR = MathF.Max(0.001f, SrgbChannelToLinear((int)((uBg >> 16) & 0xFFu)));
        float bgG = MathF.Max(0.001f, SrgbChannelToLinear((int)((uBg >> 8) & 0xFFu)));
        float bgB = MathF.Max(0.001f, SrgbChannelToLinear((int)(uBg & 0xFFu)));

        float tintR = MathF.Max(0.04f, SrgbChannelToLinear((int)((uTint >> 16) & 0xFFu)));
        float tintG = MathF.Max(0.04f, SrgbChannelToLinear((int)((uTint >> 8) & 0xFFu)));
        float tintB = MathF.Max(0.04f, SrgbChannelToLinear((int)(uTint & 0xFFu)));

        // Beer-Lambert spectral absorption: sigma = -ln(tintLinear), T = exp(-sigma * opticalDepth)
        float absorbR = MathF.Exp(MathF.Log(tintR) * opticalDepth * 1.35f);
        float absorbG = MathF.Exp(MathF.Log(tintG) * opticalDepth * 1.35f);
        float absorbB = MathF.Exp(MathF.Log(tintB) * opticalDepth * 1.35f);

        float transmittedR = bgR * absorbR;
        float transmittedG = bgG * absorbG;
        float transmittedB = bgB * absorbB;

        int transmittedArgb = unchecked((int)0xFF000000u)
            | (LinearChannelToSrgb(transmittedR) << 16)
            | (LinearChannelToSrgb(transmittedG) << 8)
            | LinearChannelToSrgb(transmittedB);

        OklabColor labTransmitted = ArgbToOklab(transmittedArgb);
        OklabColor labTint = ArgbToOklab(tintArgb);

        // Sand-tumbled sea glass develops a velvet subsurface scatter in the glass's own Oklab tint
        // (from 0 at untreated clear glass up to dense velvet glass at grit = 1f) without bleaching
        // the center toward white as depth increases.
        float velvetScatterWeight = Math.Clamp(density * 0.34f + grit * 0.56f, 0f, 0.90f);
        float lightnessBlend = Math.Clamp(velvetScatterWeight * 0.62f, 0f, 0.78f);

        float outL = Math.Clamp(
            labTransmitted.L * (1f - lightnessBlend) + labTint.L * lightnessBlend,
            0.08f,
            0.95f
        );
        float outA = labTransmitted.A * (1f - velvetScatterWeight) + labTint.A * velvetScatterWeight;
        float outB = labTransmitted.B * (1f - velvetScatterWeight) + labTint.B * velvetScatterWeight;

        return OklabToArgb(new OklabColor(outL, outA, outB, 1f));
    }

    /// <summary>
    /// Harmonizes an arbitrary accent color into the SeaGlass pastel gamut by lifting
    /// Oklab lightness into the translucent sea-glass band [0.68..0.88].
    /// </summary>
    public static int HarmonizeToSeaGlassPastel(int sourceArgb, float targetLightness = 0.79f)
    {
        OklabColor lab = ArgbToOklab(sourceArgb);
        float chroma = MathF.Sqrt(lab.A * lab.A + lab.B * lab.B);
        float hue = MathF.Atan2(lab.B, lab.A);
        float pastelChroma = Math.Clamp(chroma, 0.035f, 0.145f);
        return OklabToArgb(new OklabColor(
            L: Math.Clamp(targetLightness, 0.22f, 0.92f),
            A: pastelChroma * MathF.Cos(hue),
            B: pastelChroma * MathF.Sin(hue),
            Alpha: 1f
        ));
    }

    /// <summary>
    /// Computes a contrast-safe, hue-harmonized deep tonal ink color (for light/pastel sea glass)
    /// or luminous frosted chalk-alabaster color (for dark smoked sea glass) for icons and typography.
    /// </summary>
    public static int OptimalContentColorFor(int tintArgb, float tintDensity = 0.55f)
    {
        OklabColor lab = ArgbToOklab(tintArgb);
        float chroma = MathF.Sqrt(lab.A * lab.A + lab.B * lab.B);
        float hue = MathF.Atan2(lab.B, lab.A);

        if (lab.L >= 0.48f || tintDensity < 0.35f)
        {
            float tonalChroma = chroma < 0.025f
                ? 0.042f
                : Math.Clamp(chroma * 0.98f, 0.076f, 0.136f);
            return OklabToArgb(new OklabColor(
                L: 0.235f,
                A: tonalChroma * MathF.Cos(hue),
                B: tonalChroma * MathF.Sin(hue),
                Alpha: 1f
            ));
        }

        float subtleChroma = MathF.Min(0.048f, chroma * 0.45f);
        return OklabToArgb(new OklabColor(
            L: 0.96f,
            A: subtleChroma * 0.5f * MathF.Cos(hue),
            B: subtleChroma * MathF.Sin(hue),
            Alpha: 1f
        ));
    }

    /// <summary>
    /// Computes the luminous frosted catch-light color that gleams along the bottom-right lip
    /// of text and iconography etched (intaglio-carved) into a sea-glass surface.
    /// </summary>
    public static int ComputeEtchedCatchLightColor(int tintArgb, float coarseness = 0.45f)
    {
        OklabColor lab = ArgbToOklab(tintArgb);
        return lab.L >= 0.48f
            ? LerpOklab(tintArgb, unchecked((int)0xFFFFFFFF), Math.Clamp(0.86f + 0.08f * coarseness, 0.82f, 0.96f))
            : LerpOklab(tintArgb, unchecked((int)0xFF040C16), 0.78f);
    }

    /// <summary>
    /// Computes the richer, deeper Beer-Lambert volumetric core color for the interior of a tumbled
    /// sea-glass piece where physical slab thickness is greatest (never bleaching the center to white).
    /// </summary>
    public static int ComputeDeepVolumetricCoreColor(int tintArgb, float tintDensity = 0.65f)
    {
        OklabColor lab = ArgbToOklab(tintArgb);
        float chroma = MathF.Sqrt(lab.A * lab.A + lab.B * lab.B);
        float hue = MathF.Atan2(lab.B, lab.A);
        float density = Math.Clamp(tintDensity, 0f, 1f);
        float coreL = lab.L >= 0.48f
            ? Math.Clamp(lab.L - 0.045f * density, 0.42f, 0.88f)
            : Math.Clamp(lab.L - 0.035f * density, 0.12f, 0.46f);
        float coreChroma = Math.Clamp(chroma * (1.08f + 0.18f * density), 0.02f, 0.20f);
        return OklabToArgb(new OklabColor(
            L: coreL,
            A: coreChroma * MathF.Cos(hue),
            B: coreChroma * MathF.Sin(hue),
            Alpha: 1f
        ));
    }

    /// <summary>
    /// Computes the milky, chalk-frosted perimeter rind color produced by ocean sand and pebble
    /// abrasion along the curved edges of tumbled sea glass.
    /// </summary>
    public static int ComputeFrostedRindColor(
        int tintArgb,
        float coarseness = 0.45f,
        float rindWhiteness = 0.64f)
    {
        float grit = Math.Clamp(coarseness, 0f, 1f);
        float blend = Math.Clamp(rindWhiteness + 0.16f * grit, 0.25f, 0.92f);
        return LerpOklab(tintArgb, unchecked((int)0xFFFAFCFF), blend);
    }

    /// <summary>
    /// Computes the luminous internal subsurface caustic color that pools along the lower bevel
    /// and curved bottom edge of a thick, convex piece of tumbled sea glass.
    /// </summary>
    public static int ComputeSubsurfaceCausticColor(
        int tintArgb,
        float coarseness = 0.45f)
    {
        OklabColor lab = ArgbToOklab(tintArgb);
        float grit = Math.Clamp(coarseness, 0f, 1f);
        int causticTargetArgb = (lab.B < -0.02f && lab.A < 0.06f)
            ? unchecked((int)0xFFBCFAFF)
            : (lab.B < 0.0f && lab.A >= 0.06f)
                ? unchecked((int)0xFFFEE8FF)
                : (lab.A > 0.02f || lab.B > 0.03f)
                    ? unchecked((int)0xFFFFF1DF)
                    : unchecked((int)0xFFDCFFF6);
        float blend = lab.L < 0.48f
            ? Math.Clamp(0.34f + 0.14f * grit, 0.25f, 0.55f)
            : Math.Clamp(0.46f + 0.18f * grit, 0.35f, 0.72f);
        return LerpOklab(tintArgb, causticTargetArgb, blend);
    }

    /// <summary>
    /// Computes the deeper, chroma-enriched tonal mineral pit color for sand-abraded dimples
    /// on the surface of tumbled sea glass (never grey or black mud).
    /// </summary>
    public static int ComputeTonalPitColor(int tintArgb, float tintDensity = 0.65f)
    {
        OklabColor lab = ArgbToOklab(tintArgb);
        float chroma = MathF.Sqrt(lab.A * lab.A + lab.B * lab.B);
        float hue = MathF.Atan2(lab.B, lab.A);
        float density = Math.Clamp(tintDensity, 0f, 1f);
        float pitL = Math.Clamp(lab.L - (0.11f + 0.06f * density), 0.18f, 0.78f);
        float pitChroma = Math.Clamp(chroma * (1.28f + 0.22f * density), 0.04f, 0.22f);
        return OklabToArgb(new OklabColor(
            L: pitL,
            A: pitChroma * MathF.Cos(hue),
            B: pitChroma * MathF.Sin(hue),
            Alpha: 1f
        ));
    }

    /// <summary>
    /// Extracts linear sRGB float components [r, g, b] in [0f..1f] into <paramref name="outArray"/>
    /// at <paramref name="offset"/> with zero allocations.
    /// </summary>
    public static void ArgbToLinearRgbFloats(int argb, float[] outArray, int offset = 0)
    {
        uint u = unchecked((uint)argb);
        outArray[offset] = SrgbChannelToLinear((int)((u >> 16) & 0xFFu));
        outArray[offset + 1] = SrgbChannelToLinear((int)((u >> 8) & 0xFFu));
        outArray[offset + 2] = SrgbChannelToLinear((int)(u & 0xFFu));
    }
}
