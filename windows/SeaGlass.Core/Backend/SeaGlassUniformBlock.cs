using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace SeaGlass.Core.Backend;

/// <summary>
/// Byte-exact 256-byte blittable C# structure matching <c>seaglass::SeaGlassUniformBlockStd140</c>
/// in <c>seaglass_backend_abi.h</c> and HLSL <c>cbuffer SeaGlassUniforms : register(b0)</c>.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 256)]
public struct SeaGlassUniformBlockStd140
{
    [FieldOffset(0)] public Vector4 ResolutionAndOrigin;
    [FieldOffset(16)] public Vector4 OpticsPrimary;
    [FieldOffset(32)] public Vector4 OpticsSecondary;
    [FieldOffset(48)] public Vector4 LightAndMeta;

    [FieldOffset(64)] public Vector4 Node0Bounds;
    [FieldOffset(80)] public Vector4 Node1Bounds;
    [FieldOffset(96)] public Vector4 Node2Bounds;
    [FieldOffset(112)] public Vector4 Node3Bounds;

    [FieldOffset(128)] public Vector4 Node0Corners;
    [FieldOffset(144)] public Vector4 Node1Corners;
    [FieldOffset(160)] public Vector4 Node2Corners;
    [FieldOffset(176)] public Vector4 Node3Corners;

    [FieldOffset(192)] public Vector4 Node0Tint;
    [FieldOffset(208)] public Vector4 Node1Tint;
    [FieldOffset(224)] public Vector4 Node2Tint;
    [FieldOffset(240)] public Vector4 Node3Tint;
}

/// <summary>
/// Backend-agnostic 256-byte (16 × vec4 = 64 × float) <c>cbuffer</c> / <c>std140</c> Uniform Buffer Object (UBO)
/// layout shared across Direct3D 11 / Direct2D HLSL, Vulkan 1.1+ (SPIR-V), OpenGL ES 3.0+,
/// Android AGSL (<c>RuntimeShader</c>), and C++20 (<c>SeaGlassUniformBlockStd140</c>).
///
/// Exact 1:1 parity with <c>dev.seaglass.core.backend.SeaGlassUniformBlock</c> on Android.
/// </summary>
public sealed class SeaGlassUniformBlock
{
    public const int Vec4Count = 16;
    public const int FloatCount = Vec4Count * 4; // 64 floats
    public const int ByteSize = FloatCount * 4;  // 256 bytes
    public const int AllSlotsMask = 0xFFFF;

    public const int OffsetResolutionAndOrigin = 0;
    public const int OffsetOpticsPrimary = 4;
    public const int OffsetOpticsSecondary = 8;
    public const int OffsetLightAndMeta = 12;
    public const int OffsetNodeBounds = 16;
    public const int OffsetNodeCorners = 32;
    public const int OffsetNodeTints = 48;

    private const float DefaultLightX = -0.52f;
    private const float DefaultLightY = -0.70f;
    private const float DefaultLightZ = 0.48f;

    /// <summary>
    /// Raw 64-float backing array (256 bytes). Mutated in-place to guarantee zero per-frame heap allocations.
    /// </summary>
    public float[] Data { get; } = new float[FloatCount];

    /// <summary>
    /// 16-bit bitmask (<c>bit k</c> = <c>vec4</c> slot <c>k</c> in <c>0..15</c>) indicating which <c>vec4</c>
    /// uniform slots changed during the most recent <see cref="Pack"/> call. Allows GPU backends to skip
    /// redundant cbuffer/UBO uploads when <c>DirtySlotsMask == 0</c>.
    /// </summary>
    public int DirtySlotsMask { get; private set; } = AllSlotsMask;

    /// <summary>
    /// Monotonically increasing revision counter incremented only when at least one uniform slot changes.
    /// </summary>
    public long ContentRevision { get; private set; }

    private bool _initialized;
    private readonly float[] _scratchRgb = new float[3];

    private int WriteVec4Slot(int slotIndex, float x, float y, float z, float w)
    {
        int baseIdx = slotIndex << 2;
        if (_initialized &&
            Data[baseIdx] == x &&
            Data[baseIdx + 1] == y &&
            Data[baseIdx + 2] == z &&
            Data[baseIdx + 3] == w)
        {
            return 0;
        }
        Data[baseIdx] = x;
        Data[baseIdx + 1] = y;
        Data[baseIdx + 2] = z;
        Data[baseIdx + 3] = w;
        return 1 << slotIndex;
    }

    /// <summary>
    /// Marks all 16 <c>vec4</c> slots clean after a backend has synced them to the GPU.
    /// </summary>
    public void ClearDirtyMask() => DirtySlotsMask = 0;

    /// <summary>
    /// Packs the current frame's optical material and SDF coalescing nodes into the 256-byte cbuffer / std140 layout,
    /// updating <see cref="DirtySlotsMask"/> and <see cref="ContentRevision"/> only for slots whose values changed.
    /// </summary>
    public void Pack(
        float widthPx,
        float heightPx,
        float backdropOffsetX,
        float backdropOffsetY,
        SeaGlassMaterial material,
        float density,
        IReadOnlyList<SeaGlassSdfEngine.SdfNode> nodes,
        float smoothRadiusPx,
        float lightDirX = DefaultLightX,
        float lightDirY = DefaultLightY,
        float lightDirZ = DefaultLightZ)
    {
        float d = MathF.Max(0.5f, density);
        float thicknessPx = material.ThicknessDp * d;

        float lLen = MathF.Max(1e-4f, MathF.Sqrt(lightDirX * lightDirX + lightDirY * lightDirY + lightDirZ * lightDirZ));
        int mask = 0;

        // Slot 0: uResolutionAndOrigin
        mask |= WriteVec4Slot(
            0,
            MathF.Max(1f, widthPx),
            MathF.Max(1f, heightPx),
            backdropOffsetX,
            backdropOffsetY);

        // Slot 1: uOpticsPrimary
        mask |= WriteVec4Slot(
            1,
            material.TintDensity,
            material.Coarseness,
            material.Ior,
            material.ChromaticDispersion);

        // Slot 2: uOpticsSecondary
        mask |= WriteVec4Slot(
            2,
            material.RimChalkiness,
            thicknessPx,
            material.CornerSmoothing,
            MathF.Max(0f, smoothRadiusPx));

        // Slot 3: uLightAndMeta
        int nodeCount = nodes?.Count ?? 0;
        int activeCount = Math.Max(1, Math.Min(nodeCount, SeaGlassSdfEngine.MaxSdfNodes));
        mask |= WriteVec4Slot(
            3,
            lightDirX / lLen,
            lightDirY / lLen,
            lightDirZ / lLen,
            activeCount);

        // Slots 4..15: Node arrays (up to 4 nodes)
        for (int i = 0; i < SeaGlassSdfEngine.MaxSdfNodes; i++)
        {
            int boundsSlot = 4 + i;
            int cornersSlot = 8 + i;
            int tintsSlot = 12 + i;

            if (nodes is not null && i < nodes.Count)
            {
                SeaGlassSdfEngine.SdfNode node = nodes[i];
                mask |= WriteVec4Slot(
                    boundsSlot,
                    node.CenterX,
                    node.CenterY,
                    node.HalfWidth,
                    node.HalfHeight);
                mask |= WriteVec4Slot(
                    cornersSlot,
                    node.CornerTopRight,
                    node.CornerBottomRight,
                    node.CornerBottomLeft,
                    node.CornerTopLeft);
                SeaGlassColorMath.ArgbToLinearRgbFloats(node.TintColorArgb, _scratchRgb, 0);
                mask |= WriteVec4Slot(
                    tintsSlot,
                    _scratchRgb[0],
                    _scratchRgb[1],
                    _scratchRgb[2],
                    node.ThicknessPx);
            }
            else if (i == 0)
            {
                float cornerPx = material.CornerRadiusDp * d;
                mask |= WriteVec4Slot(
                    boundsSlot,
                    widthPx * 0.5f,
                    heightPx * 0.5f,
                    widthPx * 0.5f,
                    heightPx * 0.5f);
                mask |= WriteVec4Slot(
                    cornersSlot,
                    cornerPx,
                    cornerPx,
                    cornerPx,
                    cornerPx);
                SeaGlassColorMath.ArgbToLinearRgbFloats(material.TintColorArgb, _scratchRgb, 0);
                mask |= WriteVec4Slot(
                    tintsSlot,
                    _scratchRgb[0],
                    _scratchRgb[1],
                    _scratchRgb[2],
                    thicknessPx);
            }
            else
            {
                mask |= WriteVec4Slot(boundsSlot, -1e5f, -1e5f, 0f, 0f);
                mask |= WriteVec4Slot(cornersSlot, 0f, 0f, 0f, 0f);
                mask |= WriteVec4Slot(tintsSlot, 1f, 1f, 1f, thicknessPx);
            }
        }

        _initialized = true;
        DirtySlotsMask |= mask;
        if (mask != 0)
        {
            ContentRevision++;
        }
    }

    /// <summary>
    /// Writes the 256-byte <c>cbuffer</c> / <c>std140</c> payload into a destination <see cref="Span{Byte}"/>
    /// with zero heap allocations.
    /// </summary>
    public void WriteToStd140ByteBuffer(Span<byte> target)
    {
        if (target.Length < ByteSize)
        {
            throw new ArgumentException($"Target Span<byte> requires at least {ByteSize} bytes, had {target.Length}", nameof(target));
        }
        ReadOnlySpan<byte> srcBytes = MemoryMarshal.AsBytes(Data.AsSpan());
        srcBytes.CopyTo(target);
    }

    /// <summary>
    /// Allocates a 256-byte staging buffer populated with the current <c>std140</c> / <c>cbuffer</c> data.
    /// </summary>
    public byte[] CreateDirectStagingBuffer()
    {
        byte[] buffer = new byte[ByteSize];
        WriteToStd140ByteBuffer(buffer);
        return buffer;
    }

    /// <summary>
    /// Reinterprets the 64-float backing buffer as a byte-exact <see cref="SeaGlassUniformBlockStd140"/> struct.
    /// </summary>
    public SeaGlassUniformBlockStd140 ToStd140Struct()
    {
        ReadOnlySpan<byte> srcBytes = MemoryMarshal.AsBytes(Data.AsSpan());
        return MemoryMarshal.Read<SeaGlassUniformBlockStd140>(srcBytes);
    }
}
