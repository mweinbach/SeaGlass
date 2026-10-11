using System;
using System.Collections.Generic;
using SeaGlass.Core.Backend;

namespace SeaGlass.Core;

/// <summary>
/// Multi-tier GPU rendering capability tiers for SeaGlass across Windows and Android.
/// </summary>
public enum SeaGlassDeviceTier
{
    /// <summary>
    /// Tier 1 (Windows 11 / Direct2D 1.1+ HLSL Shader Model 5.0 / Android 13+ AGSL):
    /// Zero-copy CompositionVisualSurface / RenderNode backdrop capture + Gaussian/Kawase blur
    /// pyramid + single-pass HLSL/AGSL fragment shader evaluating 4-node sminCubic SDF coalescing,
    /// meniscus bevel refraction, sand-grain micro-coarseness, and Oklab Beer-Lambert absorption.
    /// </summary>
    D2D1HlslFull,

    /// <summary>
    /// Tier 2 (Windows 10 Composition GaussianBlurEffect / Android 12 RenderEffect Hybrid):
    /// Hardware Composition blur backdrop paired with analytical SDF Catmull-Rom contour clipping,
    /// procedural sand-grain texture pass, and Oklab Beer-Lambert meniscus lighting.
    /// </summary>
    CompositionBlurHybrid,

    /// <summary>
    /// Tier 3 (Direct3D 11 WARP / Vulkan / OpenGL ES 3.0 Offscreen Pipeline):
    /// Dual-Kawase downsample/upsample pyramid + 256-byte cbuffer/std140 composite shader.
    /// </summary>
    D3D11NativePyramid,

    /// <summary>
    /// Tier 4 (Accessibility Reduce-Transparency / Battery Energy Saver / Headless Previews):
    /// Analytical SDF smooth-union contour + Oklab Beer-Lambert translucent fill + chalky bevel
    /// rim with zero offscreen backdrop passes.
    /// </summary>
    LightweightTint
}

/// <summary>
/// Diagnostic telemetry snapshot for the active GPU tier and shader pipeline.
/// </summary>
public sealed record SeaGlassTierDiagnostics(
    SeaGlassDeviceTier ActiveTier,
    string TierDisplayName,
    string OsDescription,
    bool SupportsZeroCopyVisualSurface,
    bool SupportsSinglePassHlslSdf,
    int UniformBlockBytes,
    IReadOnlyList<ShaderDialect> SupportedDialects);

public static class SeaGlassDeviceTierResolver
{
    public static SeaGlassDeviceTier ResolveRecommendedTier(
        bool transparencyEffectsEnabled = true,
        bool batterySaverActive = false,
        SeaGlassDeviceTier? manualOverride = null)
    {
        if (manualOverride is { } over) return over;
        if (!transparencyEffectsEnabled || batterySaverActive)
        {
            return SeaGlassDeviceTier.LightweightTint;
        }
        return SeaGlassDeviceTier.D2D1HlslFull;
    }

    public static SeaGlassTierDiagnostics GetDiagnostics(
        SeaGlassDeviceTier? tierOverride = null,
        bool transparencyEffectsEnabled = true,
        bool batterySaverActive = false)
    {
        SeaGlassDeviceTier tier = ResolveRecommendedTier(
            transparencyEffectsEnabled,
            batterySaverActive,
            tierOverride);

        string displayName = tier switch
        {
            SeaGlassDeviceTier.D2D1HlslFull => "Tier 1 · D2D1_HLSL_FULL (Direct2D SM 5.0 + CompositionVisualSurface)",
            SeaGlassDeviceTier.CompositionBlurHybrid => "Tier 2 · COMPOSITION_BLUR_HYBRID (Windows.UI.Composition Blur + SDF Clip)",
            SeaGlassDeviceTier.D3D11NativePyramid => "Tier 3 · D3D11_NATIVE_PYRAMID (Dual-Kawase + 256B cbuffer)",
            SeaGlassDeviceTier.LightweightTint => "Tier 4 · LIGHTWEIGHT_TINT (Zero-Offscreen Oklab Contrast Surface)",
            _ => tier.ToString()
        };

        return new SeaGlassTierDiagnostics(
            ActiveTier: tier,
            TierDisplayName: displayName,
            OsDescription: $"{Environment.OSVersion.VersionString} ({System.Runtime.InteropServices.RuntimeInformation.OSArchitecture})",
            SupportsZeroCopyVisualSurface: tier != SeaGlassDeviceTier.LightweightTint,
            SupportsSinglePassHlslSdf: tier is SeaGlassDeviceTier.D2D1HlslFull or SeaGlassDeviceTier.D3D11NativePyramid,
            UniformBlockBytes: SeaGlassUniformBlock.ByteSize,
            SupportedDialects:
            [
                ShaderDialect.HlslSm50,
                ShaderDialect.D2D1Hlsl,
                ShaderDialect.VulkanGlsl450,
                ShaderDialect.GlslEs300,
                ShaderDialect.Agsl100
            ]);
    }
}
