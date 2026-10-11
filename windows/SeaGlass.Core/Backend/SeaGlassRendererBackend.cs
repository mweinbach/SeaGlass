using System;
using System.Collections.Concurrent;

namespace SeaGlass.Core.Backend;

/// <summary>
/// Process-wide compile-once GPU shader &amp; effect program cache for the active Windows device.
/// Exact 1:1 parity with <c>dev.seaglass.core.backend.SeaGlassShaderProgramCache</c> on Android.
/// </summary>
public static class SeaGlassShaderProgramCache
{
    private const int MaxBlurBucket = 320; // 320 * 0.5px = 160px max quantized radius
    private static readonly object SyncRoot = new();
    private static readonly float[] QuantizedBlurRadii = new float[MaxBlurBucket + 1];

    public static bool IsWarmedUp { get; private set; }
    public static SeaGlassDeviceTier? CachedDeviceTier { get; private set; }
    public static ShaderDialect? CachedDeviceDialect { get; private set; }
    public static CompiledShaderBundle? CachedShaderBundle { get; private set; }

    /// <summary>
    /// Compiles and caches the active device's target shader bundle, sand-grain micro-facet texture,
    /// and quantized blur radius buckets once per process.
    /// </summary>
    public static CompiledShaderBundle WarmUpForDevice(SeaGlassDeviceTier? tierOverride = null)
    {
        if (IsWarmedUp && CachedShaderBundle is not null)
        {
            return CachedShaderBundle;
        }

        lock (SyncRoot)
        {
            if (IsWarmedUp && CachedShaderBundle is not null)
            {
                return CachedShaderBundle;
            }

            SeaGlassDeviceTier tier = SeaGlassDeviceTierResolver.ResolveRecommendedTier(manualOverride: tierOverride);
            CachedDeviceTier = tier;

            ShaderDialect dialect = tier switch
            {
                SeaGlassDeviceTier.D2D1HlslFull => ShaderDialect.HlslSm50,
                SeaGlassDeviceTier.CompositionBlurHybrid => ShaderDialect.D2D1Hlsl,
                SeaGlassDeviceTier.D3D11NativePyramid => ShaderDialect.HlslSm50,
                _ => ShaderDialect.HlslSm50
            };
            CachedDeviceDialect = dialect;

            CompiledShaderBundle bundle = SeaGlassShaderCompiler.CompileForDialect(dialect);
            CachedShaderBundle = bundle;

            for (int i = 1; i <= MaxBlurBucket; i++)
            {
                QuantizedBlurRadii[i] = i * 0.5f;
            }

            _ = SeaGlassSandGrainTexture.ObtainBgra32Pixels();
            IsWarmedUp = true;
            return bundle;
        }
    }

    /// <summary>
    /// Quantizes a continuous blur radius in pixels to 0.5px buckets in [0.5px..160.0px]
    /// so animations and slider drags never allocate new Composition/Direct2D blur effects per frame.
    /// </summary>
    public static float ObtainQuantizedBlurRadiusPx(float radiusPx)
    {
        int bucket = Math.Clamp((int)MathF.Round(Math.Clamp(radiusPx, 0.5f, 160.0f) * 2.0f), 1, MaxBlurBucket);
        return bucket * 0.5f;
    }
}

/// <summary>
/// Render-graph telemetry and pass descriptor for a SeaGlass GPU backend.
/// </summary>
public sealed record SeaGlassRenderGraphDescriptor(
    string BackendName,
    ShaderDialect Dialect,
    int UboByteSize,
    int KawasePyramidLevels,
    float FrostRadiusPx,
    bool ZeroCopyBackdropEnabled);

/// <summary>
/// Pluggable GPU backend contract for SeaGlass across Direct2D/Direct3D 11, Vulkan 1.1+, and OpenGL ES 3.0.
/// </summary>
public interface ISeaGlassRendererBackend
{
    string BackendName { get; }
    ShaderDialect Dialect { get; }
    ReadOnlyMemory<byte> StagingBuffer { get; }
    CompiledShaderBundle ShaderBundle { get; }

    void SyncUniforms(SeaGlassUniformBlock uniformBlock);
    SeaGlassRenderGraphDescriptor DescribeRenderGraph(float frostRadiusPx);
}

/// <summary>
/// Primary Windows 11 / WinUI 3 GPU backend consuming <see cref="ShaderDialect.HlslSm50"/>
/// and the 256-byte <c>cbuffer SeaGlassUniforms : register(b0)</c>.
/// </summary>
public sealed class Direct2DHlslSeaGlassBackend : ISeaGlassRendererBackend
{
    private readonly byte[] _stagingBuffer = new byte[SeaGlassUniformBlock.ByteSize];
    private long _lastSyncedRevision = -1L;

    public string BackendName => "Direct2D 1.3 / D3D11.1 HLSL SM 5.0";
    public ShaderDialect Dialect => ShaderDialect.HlslSm50;
    public ReadOnlyMemory<byte> StagingBuffer => _stagingBuffer;
    public CompiledShaderBundle ShaderBundle { get; } =
        SeaGlassShaderCompiler.CompileForDialect(ShaderDialect.HlslSm50);

    public void SyncUniforms(SeaGlassUniformBlock uniformBlock)
    {
        if (_lastSyncedRevision == uniformBlock.ContentRevision)
        {
            return;
        }
        _lastSyncedRevision = uniformBlock.ContentRevision;
        uniformBlock.WriteToStd140ByteBuffer(_stagingBuffer);
        uniformBlock.ClearDirtyMask();
    }

    public SeaGlassRenderGraphDescriptor DescribeRenderGraph(float frostRadiusPx)
    {
        int levels = ComputeKawaseLevels(frostRadiusPx);
        return new SeaGlassRenderGraphDescriptor(
            BackendName: BackendName,
            Dialect: Dialect,
            UboByteSize: SeaGlassUniformBlock.ByteSize,
            KawasePyramidLevels: levels,
            FrostRadiusPx: frostRadiusPx,
            ZeroCopyBackdropEnabled: true);
    }

    internal static int ComputeKawaseLevels(float frostRadiusPx) => frostRadiusPx switch
    {
        <= 1f => 0,
        <= 8f => 2,
        <= 18f => 3,
        <= 36f => 4,
        _ => 5
    };
}

/// <summary>
/// Cross-platform Vulkan 1.1+ backend consuming <see cref="ShaderDialect.VulkanGlsl450"/>
/// and the 256-byte <c>std140</c> UBO. Matches <c>VulkanSeaGlassBackend</c> on Android.
/// </summary>
public sealed class VulkanSeaGlassBackend : ISeaGlassRendererBackend
{
    private readonly byte[] _stagingBuffer = new byte[SeaGlassUniformBlock.ByteSize];
    private long _lastSyncedRevision = -1L;

    public string BackendName => "Vulkan 1.1+ (SPIR-V std140)";
    public ShaderDialect Dialect => ShaderDialect.VulkanGlsl450;
    public ReadOnlyMemory<byte> StagingBuffer => _stagingBuffer;
    public CompiledShaderBundle ShaderBundle { get; } =
        SeaGlassShaderCompiler.CompileForDialect(ShaderDialect.VulkanGlsl450);

    public void SyncUniforms(SeaGlassUniformBlock uniformBlock)
    {
        if (_lastSyncedRevision == uniformBlock.ContentRevision)
        {
            return;
        }
        _lastSyncedRevision = uniformBlock.ContentRevision;
        uniformBlock.WriteToStd140ByteBuffer(_stagingBuffer);
        uniformBlock.ClearDirtyMask();
    }

    public SeaGlassRenderGraphDescriptor DescribeRenderGraph(float frostRadiusPx) =>
        new(
            BackendName: BackendName,
            Dialect: Dialect,
            UboByteSize: SeaGlassUniformBlock.ByteSize,
            KawasePyramidLevels: Direct2DHlslSeaGlassBackend.ComputeKawaseLevels(frostRadiusPx),
            FrostRadiusPx: frostRadiusPx,
            ZeroCopyBackdropEnabled: true);
}

/// <summary>
/// Cross-platform OpenGL ES 3.0+ backend consuming <see cref="ShaderDialect.GlslEs300"/>
/// and the 256-byte <c>std140</c> UBO. Matches <c>Gles3SeaGlassBackend</c> on Android.
/// </summary>
public sealed class Gles3SeaGlassBackend : ISeaGlassRendererBackend
{
    private readonly byte[] _stagingBuffer = new byte[SeaGlassUniformBlock.ByteSize];
    private long _lastSyncedRevision = -1L;

    public string BackendName => "OpenGL ES 3.00 (Dual-Kawase + std140 UBO)";
    public ShaderDialect Dialect => ShaderDialect.GlslEs300;
    public ReadOnlyMemory<byte> StagingBuffer => _stagingBuffer;
    public CompiledShaderBundle ShaderBundle { get; } =
        SeaGlassShaderCompiler.CompileForDialect(ShaderDialect.GlslEs300);

    public void SyncUniforms(SeaGlassUniformBlock uniformBlock)
    {
        if (_lastSyncedRevision == uniformBlock.ContentRevision)
        {
            return;
        }
        _lastSyncedRevision = uniformBlock.ContentRevision;
        uniformBlock.WriteToStd140ByteBuffer(_stagingBuffer);
        uniformBlock.ClearDirtyMask();
    }

    public SeaGlassRenderGraphDescriptor DescribeRenderGraph(float frostRadiusPx) =>
        new(
            BackendName: BackendName,
            Dialect: Dialect,
            UboByteSize: SeaGlassUniformBlock.ByteSize,
            KawasePyramidLevels: Direct2DHlslSeaGlassBackend.ComputeKawaseLevels(frostRadiusPx),
            FrostRadiusPx: frostRadiusPx,
            ZeroCopyBackdropEnabled: false);
}
