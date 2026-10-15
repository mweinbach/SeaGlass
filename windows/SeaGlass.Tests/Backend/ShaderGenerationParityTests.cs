using System;
using System.Diagnostics;
using SeaGlass.Core;
using SeaGlass.Core.Backend;
using SeaGlass.WinUI;
using SeaGlass.WinUI.Rendering;
using Xunit;

namespace SeaGlass.Tests.Backend;

public sealed class ShaderGenerationParityTests
{
    [Theory]
    [InlineData(ShaderDialect.HlslSm50)]
    [InlineData(ShaderDialect.D2D1Hlsl)]
    [InlineData(ShaderDialect.VulkanGlsl450)]
    [InlineData(ShaderDialect.GlslEs300)]
    [InlineData(ShaderDialect.Agsl100)]
    public void ShaderCompiler_Generates_Valid_CompositeSource_For_All_Dialects(ShaderDialect dialect)
    {
        CompiledShaderBundle bundle = SeaGlassShaderCompiler.CompileForDialect(dialect);
        Assert.NotNull(bundle);
        Assert.NotEmpty(bundle.SeaGlassCompositeFragmentSource);

        string src = bundle.SeaGlassCompositeFragmentSource;

        // Core optical mathematics presence
        Assert.Contains("sgRoundedBoxSdf", src);
        Assert.Contains("sgLinearSrgbToOklab", src);
        Assert.Contains("sgOklabToLinearSrgb", src);
        Assert.Contains("sgSandMicroFacet", src);
        if (dialect == ShaderDialect.Agsl100)
        {
            Assert.Contains("sgSminDist", src);
        }
        else
        {
            Assert.Contains("sgSminCubic", src);
        }

        // Branching & optimization guards
        Assert.Contains("coarseness > 0.005", src);
        Assert.Contains("dispersion > 0.004", src);
        Assert.Contains("frostScatter", src);
        Assert.Contains("meniscus > 0.001", src);
    }

    [Fact]
    public void HlslSm50_Matches_Cbuffer_Register_Contract()
    {
        CompiledShaderBundle bundle = SeaGlassShaderCompiler.CompileForDialect(ShaderDialect.HlslSm50);
        string src = bundle.SeaGlassCompositeFragmentSource;

        Assert.Contains("cbuffer SeaGlassUniforms : register(b0)", src);
        Assert.Contains("Texture2D<float4> uBackdropTex : register(t0)", src);
        Assert.Contains("SamplerState uLinearClamp : register(s0)", src);
        Assert.NotNull(bundle.KawaseDownsampleFragmentSource);
        Assert.NotNull(bundle.KawaseUpsampleFragmentSource);
    }

    [Fact]
    public void D2D1Hlsl_Matches_Direct2D_PixelShader_Contract()
    {
        CompiledShaderBundle bundle = SeaGlassShaderCompiler.CompileForDialect(ShaderDialect.D2D1Hlsl);
        string src = bundle.SeaGlassCompositeFragmentSource;

        Assert.Contains("D2D_PS_ENTRY(PSMain)", src);
        Assert.Contains("D2DSampleInputAtOffset(0,", src);
        Assert.NotNull(bundle.SandGrainOverlayFragmentSource);
        Assert.NotNull(bundle.SdfMaskFragmentSource);
    }

    [Fact]
    public void ShaderProgramCache_Is_Deterministic_And_SubMillisecond()
    {
        Stopwatch sw = Stopwatch.StartNew();
        CompiledShaderBundle cold = SeaGlassShaderCompiler.CompileForDialect(ShaderDialect.D2D1Hlsl);
        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds < 50, "Cold compilation should be sub-50ms");

        sw.Restart();
        CompiledShaderBundle warm = SeaGlassShaderCompiler.CompileForDialect(ShaderDialect.D2D1Hlsl);
        sw.Stop();
        Assert.Same(cold, warm);
        Assert.True(sw.ElapsedTicks < Stopwatch.Frequency / 1000, "Cache hit must be sub-millisecond");
    }

    private static SeaGlassRenderContext CreateTestContext(float thicknessDp = 24f, float widthPx = 300f, float heightPx = 100f)
    {
        SeaGlassSurfaceFrameState frameState = default;
        SeaGlass.Tests.Controls.WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassMaterial mat = SeaGlassPalette.CobaltTide.PrimaryGlass with { ThicknessDp = thicknessDp };
            SeaGlassSurface surface = new(material: mat) { SizePx = new System.Numerics.Vector2(widthPx, heightPx) };
            frameState = surface.EvaluateFrame(1f / 60f);
        });

        return new SeaGlassRenderContext
        {
            FrameState = frameState,
            WidthPx = widthPx,
            HeightPx = heightPx,
            LocalBackdropOffsetPx = new System.Numerics.Vector2(16f, 32f),
            ActiveTier = SeaGlassDeviceTier.D2D1HlslFull,
            ScratchArena = new SeaGlassRenderScratchArena(),
            SmoothRadiusPx = 20f
        };
    }

    [Fact]
    public void D2D1HlslRenderPipeline_Exposes_Valid_ShaderBundle_And_Stages_ConstantBuffer()
    {
        using SeaGlass.WinUI.Rendering.D2D1HlslRenderPipeline pipeline = new();
        Assert.Equal(SeaGlassDeviceTier.D2D1HlslFull, pipeline.Tier);

        Assert.NotNull(pipeline.ShaderBundle);
        Assert.Equal(ShaderDialect.D2D1Hlsl, pipeline.ShaderBundle.Dialect);
        Assert.Contains("D2D_PS_ENTRY(PSMain)", pipeline.ShaderBundle.SeaGlassCompositeFragmentSource);
        Assert.Contains("cbuffer SeaGlassUniforms : register(b0)", pipeline.ShaderBundle.SeaGlassCompositeFragmentSource);

        var context = CreateTestContext();
        pipeline.RenderSurface(context, null);

        Assert.Equal(256, pipeline.StagedByteCount);
        Assert.True(context.ScratchArena.StagingBuffer.Length >= 256);
    }

    [Theory]
    [InlineData(0f, 0.5f)]
    [InlineData(8f, 12f)]
    [InlineData(16f, 24f)]
    [InlineData(32f, 48f)]
    public void CompositionHybridRenderPipeline_Calculates_And_Exposes_QuantizedBlurRadius(float thicknessDp, float expectedBlurPx)
    {
        using SeaGlass.WinUI.Rendering.CompositionHybridRenderPipeline pipeline = new();
        Assert.Equal(SeaGlassDeviceTier.CompositionBlurHybrid, pipeline.Tier);

        var context = CreateTestContext(thicknessDp: thicknessDp);
        pipeline.RenderSurface(context, null);

        Assert.Equal(expectedBlurPx, pipeline.LastQuantizedBlurRadius, precision: 1);
    }

    [Theory]
    [InlineData(0.5f, 0)]
    [InlineData(5.0f, 2)]
    [InlineData(12.0f, 3)]
    [InlineData(25.0f, 4)]
    [InlineData(50.0f, 5)]
    public void D3D11WarpRenderPipeline_Calculates_And_Exposes_KawaseLevels_And_Descriptor(float thicknessDp, int expectedLevels)
    {
        using SeaGlass.WinUI.Rendering.D3D11WarpRenderPipeline pipeline = new();
        Assert.Equal(SeaGlassDeviceTier.D3D11NativePyramid, pipeline.Tier);

        var context = CreateTestContext(thicknessDp: thicknessDp);
        pipeline.RenderSurface(context, null);

        Assert.Equal(expectedLevels, pipeline.LastKawaseLevels);
        Assert.NotNull(pipeline.LastRenderGraph);
        Assert.Equal(expectedLevels, pipeline.LastRenderGraph.KawasePyramidLevels);
        Assert.Equal(256, pipeline.LastRenderGraph.UboByteSize);
    }

    [Fact]
    public void Win2D_Assembly_And_Types_Are_Loadable_At_Runtime()
    {
        var canvasDeviceType = typeof(Microsoft.Graphics.Canvas.CanvasDevice);
        Assert.NotNull(canvasDeviceType);
        Assert.StartsWith("Microsoft.Graphics.Canvas", canvasDeviceType.Assembly.GetName().Name);

        var blurEffectType = typeof(Microsoft.Graphics.Canvas.Effects.GaussianBlurEffect);
        Assert.NotNull(blurEffectType);
        Assert.StartsWith("Microsoft.Graphics.Canvas", blurEffectType.Assembly.GetName().Name);

        var winUiAssembly = typeof(SeaGlass.WinUI.Rendering.SeaGlassTieredRenderEngine).Assembly;
        var referencedAssemblies = winUiAssembly.GetReferencedAssemblies();
        Assert.Contains(referencedAssemblies, r => r.Name != null && r.Name.StartsWith("Microsoft.Graphics.Canvas"));
    }
}
