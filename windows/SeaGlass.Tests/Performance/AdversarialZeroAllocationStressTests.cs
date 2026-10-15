using System;
using System.Numerics;
using SeaGlass.Core;
using SeaGlass.Core.Backend;
using SeaGlass.WinUI;
using SeaGlass.WinUI.Rendering;
using SeaGlass.Tests.Controls;
using Xunit;

namespace SeaGlass.Tests.Performance;

public sealed class AdversarialZeroAllocationStressTests
{
    [Theory]
    [InlineData(SeaGlassDeviceTier.D2D1HlslFull)]
    [InlineData(SeaGlassDeviceTier.CompositionBlurHybrid)]
    [InlineData(SeaGlassDeviceTier.D3D11NativePyramid)]
    [InlineData(SeaGlassDeviceTier.LightweightTint)]
    public void StressTest_10000_Frames_ConstantGeometry_Allocates_Zero_Bytes(SeaGlassDeviceTier tier)
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSurface surface = new();
            SeaGlassRenderScratchArena scratchArena = new();
            SeaGlassTieredRenderEngine engine = new();
            engine.SetTierOverride(tier);

            // Warm-up phase (JIT & internal initialization)
            for (int i = 0; i < 200; i++)
            {
                SeaGlassSurfaceFrameState frameState = surface.EvaluateFrame(1f / 60f);
                SeaGlassRenderContext warmContext = new()
                {
                    FrameState = frameState,
                    WidthPx = 300f,
                    HeightPx = 100f,
                    LocalBackdropOffsetPx = Vector2.Zero,
                    ActiveTier = engine.ActiveTier,
                    ScratchArena = scratchArena,
                    SmoothRadiusPx = 20f
                };
                engine.Render(warmContext, null);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long bytesBefore = GC.GetAllocatedBytesForCurrentThread();

            // 10,000 continuous frames
            for (int i = 0; i < 10000; i++)
            {
                SeaGlassSurfaceFrameState frameState = surface.EvaluateFrame(1f / 60f);
                SeaGlassRenderContext loopContext = new()
                {
                    FrameState = frameState,
                    WidthPx = 300f,
                    HeightPx = 100f,
                    LocalBackdropOffsetPx = Vector2.Zero,
                    ActiveTier = engine.ActiveTier,
                    ScratchArena = scratchArena,
                    SmoothRadiusPx = 20f
                };
                engine.Render(loopContext, null);
            }

            long bytesAfter = GC.GetAllocatedBytesForCurrentThread();
            long totalAllocated = bytesAfter - bytesBefore;

            Assert.True(totalAllocated == 0,
                $"Expected 0 bytes allocated over 10,000 frames on tier {tier}, but allocated {totalAllocated} bytes.");
        });
    }

    [Theory]
    [InlineData(SeaGlassDeviceTier.D2D1HlslFull)]
    [InlineData(SeaGlassDeviceTier.CompositionBlurHybrid)]
    [InlineData(SeaGlassDeviceTier.D3D11NativePyramid)]
    [InlineData(SeaGlassDeviceTier.LightweightTint)]
    public void StressTest_10000_Frames_DynamicGeometryAndBackdropOffsets_Allocates_Zero_Bytes(SeaGlassDeviceTier tier)
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSurface surface = new();
            SeaGlassRenderScratchArena scratchArena = new();
            SeaGlassTieredRenderEngine engine = new();
            engine.SetTierOverride(tier);

            // Warm up
            for (int i = 0; i < 200; i++)
            {
                surface.SizePx = new Vector2(200f + (i % 50), 100f + (i % 30));
                SeaGlassSurfaceFrameState frameState = surface.EvaluateFrame(1f / 60f);
                SeaGlassRenderContext warmContext = new()
                {
                    FrameState = frameState,
                    WidthPx = surface.SizePx.X,
                    HeightPx = surface.SizePx.Y,
                    LocalBackdropOffsetPx = new Vector2(i % 100, (i * 2) % 100),
                    ActiveTier = engine.ActiveTier,
                    ScratchArena = scratchArena,
                    SmoothRadiusPx = 10f + (i % 20)
                };
                engine.Render(warmContext, null);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long bytesBefore = GC.GetAllocatedBytesForCurrentThread();

            // 10,000 frames with varying dimensions and backdrop offsets
            for (int i = 0; i < 10000; i++)
            {
                surface.SizePx = new Vector2(100f + (i % 600), 50f + (i % 300));
                SeaGlassSurfaceFrameState frameState = surface.EvaluateFrame(1f / 60f);
                SeaGlassRenderContext dynamicContext = new()
                {
                    FrameState = frameState,
                    WidthPx = surface.SizePx.X,
                    HeightPx = surface.SizePx.Y,
                    LocalBackdropOffsetPx = new Vector2(i % 1920, (i * 3) % 1080),
                    ActiveTier = engine.ActiveTier,
                    ScratchArena = scratchArena,
                    SmoothRadiusPx = 5f + (i % 40)
                };

                engine.Render(dynamicContext, null);
            }

            long bytesAfter = GC.GetAllocatedBytesForCurrentThread();
            long totalAllocated = bytesAfter - bytesBefore;

            Assert.True(totalAllocated == 0,
                $"Expected 0 bytes allocated over 10,000 dynamically oscillating frames on tier {tier}, but allocated {totalAllocated} bytes.");
        });
    }

    [Fact]
    public void StressTest_10000_Frames_EvaluateFrame_Allocates_Zero_Bytes()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSurface surface = new();

            // Warm up with active animation
            for (int i = 0; i < 200; i++)
            {
                surface.SizePx = new Vector2(200f + (i % 100), 100f + (i % 50));
                surface.OriginInRoot = new Vector2(i % 50, (i * 2) % 50);
                surface.OnPointerStateChanged(new Vector2(100f + (i % 50), 50f + (i % 30)), isDown: (i % 20) < 10);
                _ = surface.EvaluateFrame(1f / 60f);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long bytesBefore = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < 10000; i++)
            {
                surface.SizePx = new Vector2(200f + (i % 100), 100f + (i % 50));
                surface.OriginInRoot = new Vector2(i % 50, (i * 2) % 50);
                surface.OnPointerStateChanged(new Vector2(100f + (i % 50), 50f + (i % 30)), isDown: (i % 20) < 10);
                _ = surface.EvaluateFrame(1f / 60f);
            }

            long bytesAfter = GC.GetAllocatedBytesForCurrentThread();
            long totalAllocated = bytesAfter - bytesBefore;

            Assert.True(totalAllocated == 0,
                $"Expected 0 bytes allocated over 10,000 EvaluateFrame calls under active animation, but allocated {totalAllocated} bytes.");
        });
    }

    [Fact]
    public void StressTest_10000_Frames_MultiNodeUniformPackingAndBlitting_Allocates_Zero_Bytes()
    {
        SeaGlassUniformBlock uniformBlock = new();
        byte[] stagingBuffer = new byte[256];

        SeaGlassSdfEngine.SdfNode[] nodes = new SeaGlassSdfEngine.SdfNode[4];
        for (int n = 0; n < 4; n++)
        {
            nodes[n] = new SeaGlassSdfEngine.SdfNode(
                CenterX: 100f * n,
                CenterY: 50f * n,
                HalfWidth: 50f,
                HalfHeight: 25f,
                CornerTopRight: 10f,
                CornerBottomRight: 10f,
                CornerBottomLeft: 10f,
                CornerTopLeft: 10f,
                CornerSmoothing: 0.2f,
                TintColorArgb: unchecked((int)0xFF00AAFF),
                ThicknessPx: 12f
            );
        }

        SeaGlassMaterial mat = SeaGlassPalette.CobaltTide.SurfaceGlass;

        // Warmup
        for (int i = 0; i < 200; i++)
        {
            uniformBlock.Pack(
                widthPx: 800f,
                heightPx: 600f,
                backdropOffsetX: 10f,
                backdropOffsetY: 20f,
                material: mat,
                density: 1.5f,
                nodes: nodes,
                smoothRadiusPx: 24f,
                lightDirX: 0.3f,
                lightDirY: 0.5f,
                lightDirZ: 0.8f
            );
            uniformBlock.WriteToStd140ByteBuffer(stagingBuffer);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long bytesBefore = GC.GetAllocatedBytesForCurrentThread();

        for (int i = 0; i < 10000; i++)
        {
            uniformBlock.Pack(
                widthPx: 800f + (i % 100),
                heightPx: 600f + (i % 100),
                backdropOffsetX: (float)(i % 50),
                backdropOffsetY: (float)((i * 2) % 50),
                material: mat,
                density: 1.0f + (i % 3) * 0.5f,
                nodes: nodes,
                smoothRadiusPx: 20f + (i % 10),
                lightDirX: 0.3f,
                lightDirY: 0.5f,
                lightDirZ: 0.8f
            );
            uniformBlock.WriteToStd140ByteBuffer(stagingBuffer);
        }

        long bytesAfter = GC.GetAllocatedBytesForCurrentThread();
        long totalAllocated = bytesAfter - bytesBefore;

        Assert.True(totalAllocated == 0,
            $"Expected 0 bytes allocated over 10,000 multi-node uniform packing and blitting cycles, but allocated {totalAllocated} bytes.");
    }
}
