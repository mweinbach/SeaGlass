using System;
using SeaGlass.Core;
using SeaGlass.Core.Backend;

namespace SeaGlass.WinUI.Rendering;

/// <summary>
/// Pre-allocated scratch buffers ensuring zero heap allocations during steady-state rendering.
/// Reusable per-frame across the 4 GPU device tiers.
/// </summary>
public sealed class SeaGlassRenderScratchArena
{
    public byte[] StagingBuffer { get; } = new byte[SeaGlassUniformBlock.ByteSize];
    public float[] FloatScratch { get; } = new float[SeaGlassUniformBlock.FloatCount];
    public SeaGlassVectorPath2D PathScratch { get; } = new();
    public SeaGlassSdfEngine.SdfNode[] SingleNodeBuffer { get; } = new SeaGlassSdfEngine.SdfNode[1];
    public float[] ScratchRgb { get; } = new float[3];

    /// <summary>
    /// Resets the scratch structures for the next frame without releasing memory.
    /// </summary>
    public void Reset()
    {
        PathScratch.Reset();
    }
}
