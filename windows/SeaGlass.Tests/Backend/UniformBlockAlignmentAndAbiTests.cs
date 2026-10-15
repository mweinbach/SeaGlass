using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using SeaGlass.Core;
using SeaGlass.Core.Backend;
using Xunit;

namespace SeaGlass.Tests.Backend;

public sealed class UniformBlockAlignmentAndAbiTests
{
    [Fact]
    public void UniformBlockStd140_Size_Is_Exactly_256_Bytes()
    {
        Assert.Equal(256, Marshal.SizeOf<SeaGlassUniformBlockStd140>());
        Assert.Equal(256, SeaGlassUniformBlock.ByteSize);
        Assert.Equal(64, SeaGlassUniformBlock.FloatCount);
        Assert.Equal(16, SeaGlassUniformBlock.Vec4Count);
    }

    [Fact]
    public void UniformBlockStd140_RegisterOffsets_Are_16Byte_Aligned()
    {
        (string name, int expectedOffset)[] expectedLayout =
        [
            (nameof(SeaGlassUniformBlockStd140.ResolutionAndOrigin), 0),
            (nameof(SeaGlassUniformBlockStd140.OpticsPrimary), 16),
            (nameof(SeaGlassUniformBlockStd140.OpticsSecondary), 32),
            (nameof(SeaGlassUniformBlockStd140.LightAndMeta), 48),
            (nameof(SeaGlassUniformBlockStd140.Node0Bounds), 64),
            (nameof(SeaGlassUniformBlockStd140.Node1Bounds), 80),
            (nameof(SeaGlassUniformBlockStd140.Node2Bounds), 96),
            (nameof(SeaGlassUniformBlockStd140.Node3Bounds), 112),
            (nameof(SeaGlassUniformBlockStd140.Node0Corners), 128),
            (nameof(SeaGlassUniformBlockStd140.Node1Corners), 144),
            (nameof(SeaGlassUniformBlockStd140.Node2Corners), 160),
            (nameof(SeaGlassUniformBlockStd140.Node3Corners), 176),
            (nameof(SeaGlassUniformBlockStd140.Node0Tint), 192),
            (nameof(SeaGlassUniformBlockStd140.Node1Tint), 208),
            (nameof(SeaGlassUniformBlockStd140.Node2Tint), 224),
            (nameof(SeaGlassUniformBlockStd140.Node3Tint), 240)
        ];

        foreach (var (fieldName, expectedOffset) in expectedLayout)
        {
            int actualOffset = (int)Marshal.OffsetOf<SeaGlassUniformBlockStd140>(fieldName);
            Assert.Equal(expectedOffset, actualOffset);
            Assert.True(actualOffset % 16 == 0, $"Register {fieldName} offset ({actualOffset}) must be 16-byte aligned");
        }
    }

    [Fact]
    public void UniformBlock_StagingBuffer_Blits_Cleanly_Into_Std140_Struct()
    {
        SeaGlassUniformBlock block = new();
        SeaGlassMaterial mat = SeaGlassPalette.CobaltTide.PrimaryGlass;

        block.Pack(
            widthPx: 800f,
            heightPx: 400f,
            backdropOffsetX: 12f,
            backdropOffsetY: 34f,
            material: mat,
            density: 1.5f,
            nodes:
            [
                new SeaGlassSdfEngine.SdfNode(
                    centerX: 200f,
                    centerY: 150f,
                    halfWidth: 60f,
                    halfHeight: 40f,
                    uniformCornerRadius: 20f,
                    tintColorArgb: mat.TintColorArgb,
                    thicknessPx: 25f)
            ],
            smoothRadiusPx: 30f,
            lightDirX: 0.1f,
            lightDirY: -0.8f,
            lightDirZ: 0.5f);

        byte[] staging = block.CreateDirectStagingBuffer();
        Assert.Equal(256, staging.Length);

        GCHandle pin = GCHandle.Alloc(staging, GCHandleType.Pinned);
        try
        {
            var structCopy = Marshal.PtrToStructure<SeaGlassUniformBlockStd140>(pin.AddrOfPinnedObject());
            Assert.Equal(800f, structCopy.ResolutionAndOrigin.X);
            Assert.Equal(400f, structCopy.ResolutionAndOrigin.Y);
            Assert.Equal(12f, structCopy.ResolutionAndOrigin.Z);
            Assert.Equal(34f, structCopy.ResolutionAndOrigin.W);

            Assert.Equal(mat.TintDensity, structCopy.OpticsPrimary.X, 4);
            Assert.Equal(mat.Coarseness, structCopy.OpticsPrimary.Y, 4);
            Assert.Equal(mat.Ior, structCopy.OpticsPrimary.Z, 4);
            Assert.Equal(mat.ChromaticDispersion, structCopy.OpticsPrimary.W, 4);

            float lLen = MathF.Sqrt(0.1f * 0.1f + (-0.8f) * (-0.8f) + 0.5f * 0.5f);
            Assert.Equal(0.1f / lLen, structCopy.LightAndMeta.X, 3);
            Assert.Equal(-0.8f / lLen, structCopy.LightAndMeta.Y, 3);
            Assert.Equal(0.5f / lLen, structCopy.LightAndMeta.Z, 3);
            Assert.Equal(1.0f, structCopy.LightAndMeta.W, 3); // 1 active node
        }
        finally
        {
            pin.Free();
        }
    }

    [Fact]
    public void AbiHeader_Maintains_ByteExact_Parity_With_CSharp_Struct()
    {
        string? dir = AppContext.BaseDirectory;
        string? windowsRoot = null;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "SeaGlass.sln")))
            {
                windowsRoot = dir;
                break;
            }
            if (File.Exists(Path.Combine(dir, "windows", "SeaGlass.sln")))
            {
                windowsRoot = Path.Combine(dir, "windows");
                break;
            }
            dir = Path.GetDirectoryName(dir);
        }

        windowsRoot ??= Directory.GetCurrentDirectory();
        if (File.Exists(Path.Combine(windowsRoot, "windows", "SeaGlass.sln")))
        {
            windowsRoot = Path.Combine(windowsRoot, "windows");
        }

        string headerPath = Path.Combine(windowsRoot, "SeaGlass.Core", "include", "seaglass_backend_abi.h");
        Assert.True(File.Exists(headerPath), $"seaglass_backend_abi.h must exist at {headerPath}");

        string headerContent = File.ReadAllText(headerPath);
        Assert.Contains("struct alignas(16) Vec4f", headerContent);
        Assert.Contains("struct alignas(16) SeaGlassUniformBlockStd140", headerContent);
        Assert.Contains("constexpr std::size_t kUniformBlockByteSize = 256", headerContent);
        Assert.Contains("static_assert(sizeof(SeaGlassUniformBlockStd140) == kUniformBlockByteSize", headerContent);
    }
}
