using System;
using System.Numerics;
using Microsoft.UI.Composition;
using SeaGlass.Core;
using SeaGlass.WinUI;
using SeaGlass.WinUI.Rendering;
using SeaGlass.Tests.Controls;
using Xunit;

namespace SeaGlass.Tests.Performance;

public sealed class ZeroAllocationRenderingTests
{
    [Theory]
    [InlineData(SeaGlassDeviceTier.D2D1HlslFull)]
    [InlineData(SeaGlassDeviceTier.CompositionBlurHybrid)]
    [InlineData(SeaGlassDeviceTier.D3D11NativePyramid)]
    [InlineData(SeaGlassDeviceTier.LightweightTint)]
    public void SteadyState_RenderLoop_Produces_Zero_GC_Heap_Allocations(SeaGlassDeviceTier tier)
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSurface surface = new();
            SeaGlassRenderScratchArena scratchArena = new();
            using SeaGlassTieredRenderEngine engine = new();
            engine.SetTierOverride(tier);

            // 1. Warm-up phase (JIT compilation and one-time lazy setup)
            for (int i = 0; i < 100; i++)
            {
                SeaGlassSurfaceFrameState frameState = surface.EvaluateFrame(1f / 60f);
                SeaGlassRenderContext context = new()
                {
                    FrameState = frameState,
                    WidthPx = 300f,
                    HeightPx = 100f,
                    LocalBackdropOffsetPx = Vector2.Zero,
                    ActiveTier = engine.ActiveTier,
                    ScratchArena = scratchArena,
                    SmoothRadiusPx = 20f
                };
                engine.Render(context, null);
            }

            // 2. Steady-state assertion phase across 1,000 frames
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long bytesBefore = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < 1000; i++)
            {
                SeaGlassSurfaceFrameState frameState = surface.EvaluateFrame(1f / 60f);
                SeaGlassRenderContext context = new()
                {
                    FrameState = frameState,
                    WidthPx = 300f,
                    HeightPx = 100f,
                    LocalBackdropOffsetPx = Vector2.Zero,
                    ActiveTier = engine.ActiveTier,
                    ScratchArena = scratchArena,
                    SmoothRadiusPx = 20f
                };
                engine.Render(context, null);
            }

            long bytesAfter = GC.GetAllocatedBytesForCurrentThread();

            Assert.Equal(0, bytesAfter - bytesBefore);
        });
    }

    [Fact]
    public void ScratchArena_Reuses_Preallocated_Buffers_Without_Reallocation()
    {
        SeaGlassRenderScratchArena arena = new();

        byte[] staging1 = arena.StagingBuffer;
        float[] float1 = arena.FloatScratch;
        float[] rgb1 = arena.ScratchRgb;

        arena.Reset();

        Assert.Same(staging1, arena.StagingBuffer);
        Assert.Same(float1, arena.FloatScratch);
        Assert.Same(rgb1, arena.ScratchRgb);
        Assert.Equal(256, arena.StagingBuffer.Length);
        Assert.Equal(64, arena.FloatScratch.Length);
        Assert.Equal(3, arena.ScratchRgb.Length);
    }

    [Fact]
    public void SteadyState_EvaluateFrame_Over1000Frames_Produces_Zero_GC_Heap_Allocations()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSurface surface = new();

            // Warm-up
            for (int i = 0; i < 100; i++)
            {
                _ = surface.EvaluateFrame(1f / 60f);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long bytesBefore = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < 1000; i++)
            {
                _ = surface.EvaluateFrame(1f / 60f);
            }

            long bytesAfter = GC.GetAllocatedBytesForCurrentThread();
            Assert.Equal(0, bytesAfter - bytesBefore);
        });
    }

    [Fact]
    public void SteadyState_EvaluateFrameInPlace_Over1000Frames_Produces_Zero_GC_Heap_Allocations()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSurface surface = new();
            SeaGlassSurfaceFrameState targetState = surface.EvaluateFrame(1f / 60f);

            // Warm-up
            for (int i = 0; i < 100; i++)
            {
                surface.EvaluateFrameInPlace(ref targetState, 1f / 60f);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long bytesBefore = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < 1000; i++)
            {
                surface.EvaluateFrameInPlace(ref targetState, 1f / 60f);
            }

            long bytesAfter = GC.GetAllocatedBytesForCurrentThread();
            Assert.Equal(0, bytesAfter - bytesBefore);
        });
    }

    [Fact]
    public void SteadyState_EvaluateFrame_UnderActiveAnimation_Produces_Zero_GC_Heap_Allocations()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSurface surface = new();
            surface.OnPointerStateChanged(new Vector2(120f, 45f), isDown: true);

            // Warm-up (200 frames for spring dynamics)
            for (int i = 0; i < 200; i++)
            {
                surface.EvaluateFrame(1f / 60f);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long bytesBefore = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < 1000; i++)
            {
                surface.EvaluateFrame(1f / 60f);
            }

            long bytesAfter = GC.GetAllocatedBytesForCurrentThread();
            Assert.Equal(0, bytesAfter - bytesBefore);
        });
    }

    [Theory]
    [InlineData(SeaGlassDeviceTier.D2D1HlslFull)]
    [InlineData(SeaGlassDeviceTier.CompositionBlurHybrid)]
    [InlineData(SeaGlassDeviceTier.D3D11NativePyramid)]
    [InlineData(SeaGlassDeviceTier.LightweightTint)]
    public void SteadyState_RenderLoop_WithTargetVisual_Produces_Zero_GC_Heap_Allocations(SeaGlassDeviceTier tier)
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            Compositor? compositor = null;
            SpriteVisual? visual = null;
            try
            {
                if (Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread() == null)
                {
                    Microsoft.UI.Dispatching.DispatcherQueueController.CreateOnCurrentThread();
                }
                compositor = new Compositor();
                visual = compositor.CreateSpriteVisual();
                var surfaceBrush = compositor.CreateSurfaceBrush();
                visual.Brush = surfaceBrush;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to activate Compositor on STA thread: {ex.Message}", ex);
            }

            Assert.NotNull(compositor);
            Assert.NotNull(visual);

            SeaGlassSurface surface = new();
            SeaGlassRenderScratchArena scratchArena = new();
            using SeaGlassTieredRenderEngine engine = new();
            if (compositor is not null)
            {
                engine.Initialize(compositor);
            }
            engine.SetTierOverride(tier);

            // Warm-up
            for (int i = 0; i < 100; i++)
            {
                SeaGlassSurfaceFrameState frameState = surface.EvaluateFrame(1f / 60f);
                SeaGlassRenderContext context = new()
                {
                    FrameState = frameState,
                    WidthPx = 300f + (i % 20),
                    HeightPx = 100f + (i % 10),
                    LocalBackdropOffsetPx = new Vector2(i % 50, (i * 2) % 50),
                    ActiveTier = engine.ActiveTier,
                    ScratchArena = scratchArena,
                    SmoothRadiusPx = 20f
                };
                engine.Render(context, visual);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var props = visual.Brush is CompositionEffectBrush eb ? eb.Properties : null;
            long bytesBefore = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < 1000; i++)
            {
                SeaGlassSurfaceFrameState frameState = surface.EvaluateFrame(1f / 60f);
                SeaGlassRenderContext context = new()
                {
                    FrameState = frameState,
                    WidthPx = 300f + (i % 20),
                    HeightPx = 100f + (i % 10),
                    LocalBackdropOffsetPx = new Vector2(i % 50, (i * 2) % 50),
                    ActiveTier = engine.ActiveTier,
                    ScratchArena = scratchArena,
                    SmoothRadiusPx = 20f
                };
                engine.Render(context, visual);
            }

            long bytesAfter = GC.GetAllocatedBytesForCurrentThread();
            Assert.Equal(0, bytesAfter - bytesBefore);
        });
    }
}
