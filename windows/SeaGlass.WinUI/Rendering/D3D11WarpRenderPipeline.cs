using System;
using System.Numerics;
using Microsoft.UI.Composition;
using SeaGlass.Core;
using SeaGlass.Core.Backend;

namespace SeaGlass.WinUI.Rendering;

/// <summary>
/// Tier 3 Render Pipeline: D3D11_WARP / D3D11_NATIVE_PYRAMID.
/// Software rasterizer fallback for VMs, remote desktop sessions, or basic display adapters.
///
/// In Milestone 1, computes required Dual-Kawase pyramid levels (0..5) based on frost thickness,
/// stages the 256-byte constant buffer per frame without heap allocations, and exposes render-graph telemetry.
/// Full software texture downsampling and staging readback is scheduled for Milestone 2 control integration.
/// </summary>
public sealed class D3D11WarpRenderPipeline : ISeaGlassRenderPipeline
{
    private static readonly SeaGlassRenderGraphDescriptor[] CachedDescriptors =
    [
        new("D3D11_WARP_Software", ShaderDialect.HlslSm50, 256, 0, 0f, true),
        new("D3D11_WARP_Software", ShaderDialect.HlslSm50, 256, 1, 4f, true),
        new("D3D11_WARP_Software", ShaderDialect.HlslSm50, 256, 2, 8f, true),
        new("D3D11_WARP_Software", ShaderDialect.HlslSm50, 256, 3, 18f, true),
        new("D3D11_WARP_Software", ShaderDialect.HlslSm50, 256, 4, 36f, true),
        new("D3D11_WARP_Software", ShaderDialect.HlslSm50, 256, 5, 50f, true),
    ];

    private Compositor? _compositor;

    public SeaGlassDeviceTier Tier => SeaGlassDeviceTier.D3D11NativePyramid;

    public int LastKawaseLevels { get; private set; }

    public SeaGlassRenderGraphDescriptor? LastRenderGraph { get; private set; }

    public void Initialize(Compositor? compositor)
    {
        _compositor = compositor;
    }

    public void RenderSurface(SeaGlassRenderContext context, SpriteVisual? targetVisual)
    {
        // 1. Pack 256-byte constant buffer into preallocated scratch staging buffer
        context.FrameState.UniformBlock.WriteToStd140ByteBuffer(context.ScratchArena.StagingBuffer);

        // 2. Compute Dual-Kawase pyramid levels for software downsampling
        int kawaseLevels = ComputeKawaseLevels(context.FrameState.EffectiveMaterial.ThicknessDp);
        LastKawaseLevels = kawaseLevels;
        LastRenderGraph = CachedDescriptors[Math.Clamp(kawaseLevels, 0, 5)];

        // 3. Update target visual bounds and backdrop translation
        if (targetVisual is not null)
        {
            targetVisual.Size = new Vector2(context.WidthPx, context.HeightPx);
            if (targetVisual.Brush is CompositionSurfaceBrush surfaceBrush)
            {
                surfaceBrush.TransformMatrix = Matrix3x2.CreateTranslation(
                    -context.LocalBackdropOffsetPx.X,
                    -context.LocalBackdropOffsetPx.Y);
            }
        }
    }

    private static int ComputeKawaseLevels(float frostRadiusPx) => frostRadiusPx switch
    {
        <= 1f => 0,
        <= 8f => 2,
        <= 18f => 3,
        <= 36f => 4,
        _ => 5
    };

    public void Dispose()
    {
        _compositor = null;
    }
}
