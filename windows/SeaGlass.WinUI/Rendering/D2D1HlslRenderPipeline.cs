using System;
using System.Numerics;
using Microsoft.UI.Composition;
using SeaGlass.Core;
using SeaGlass.Core.Backend;

namespace SeaGlass.WinUI.Rendering;

/// <summary>
/// Tier 1 Render Pipeline: D2D1_HLSL_FULL.
/// Primary Windows 11 GPU engine consuming HLSL SM 5.0 (d2d1) shader dialect,
/// the 256-byte cbuffer ABI, single-pass analytical squircle SDF, micro-facet sand coarseness,
/// and perceptual Oklab Beer-Lambert volumetric absorption.
///
/// In Milestone 1, validates D2D1 HLSL shader bundle compilation, ABI register alignments,
/// stages the 256-byte constant buffer per frame without heap allocations, and aligns zero-copy backdrop sampling.
/// Runtime HLSL pixel shader execution via Win2D PixelShaderEffect is scheduled for Milestone 2 control integration.
/// </summary>
public sealed class D2D1HlslRenderPipeline : ISeaGlassRenderPipeline
{
    private Compositor? _compositor;

    public SeaGlassDeviceTier Tier => SeaGlassDeviceTier.D2D1HlslFull;

    public CompiledShaderBundle ShaderBundle { get; private set; }

    public int StagedByteCount { get; private set; }

    public bool IsBackdropBound { get; private set; }

    public D2D1HlslRenderPipeline()
    {
        CompiledShaderBundle bundle = SeaGlassShaderCompiler.CompileForDialect(ShaderDialect.D2D1Hlsl);
        ValidateShaderBundle(bundle);
        ShaderBundle = bundle;
    }

    public void Initialize(Compositor? compositor)
    {
        _compositor = compositor;
    }

    public void BindShaderBundle(CompiledShaderBundle bundle)
    {
        ArgumentNullException.ThrowIfNull(bundle);
        ValidateShaderBundle(bundle);
        ShaderBundle = bundle;
    }

    private static void ValidateShaderBundle(CompiledShaderBundle bundle)
    {
        if (bundle.Dialect != ShaderDialect.D2D1Hlsl)
        {
            throw new InvalidOperationException($"Expected D2D1Hlsl dialect, but got {bundle.Dialect}");
        }
        if (!bundle.SeaGlassCompositeFragmentSource.Contains("cbuffer SeaGlassUniforms : register(b0)"))
        {
            throw new InvalidOperationException("Shader bundle missing 256-byte cbuffer SeaGlassUniforms register(b0) binding.");
        }
        if (!bundle.SeaGlassCompositeFragmentSource.Contains("D2D_PS_ENTRY(PSMain)"))
        {
            throw new InvalidOperationException("Shader bundle missing D2D_PS_ENTRY(PSMain) entry point.");
        }
    }

    public void RenderSurface(SeaGlassRenderContext context, SpriteVisual? targetVisual)
    {
        // 1. Pack 256-byte constant buffer into preallocated scratch staging buffer (zero heap allocations)
        context.FrameState.UniformBlock.WriteToStd140ByteBuffer(context.ScratchArena.StagingBuffer);
        StagedByteCount = SeaGlassUniformBlock.ByteSize;

        // 2. Align zero-copy backdrop sampling if target visual is hosted
        if (targetVisual is not null)
        {
            targetVisual.Size = new Vector2(context.WidthPx, context.HeightPx);
            if (targetVisual.Brush is CompositionSurfaceBrush surfaceBrush)
            {
                surfaceBrush.TransformMatrix = Matrix3x2.CreateTranslation(
                    -context.LocalBackdropOffsetPx.X,
                    -context.LocalBackdropOffsetPx.Y);
                IsBackdropBound = true;
            }
        }
    }

    public void Dispose()
    {
        _compositor = null;
    }
}
