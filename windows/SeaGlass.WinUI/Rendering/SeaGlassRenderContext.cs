using System.Collections.Generic;
using System.Numerics;
using SeaGlass.Core;

namespace SeaGlass.WinUI.Rendering;

/// <summary>
/// Execution context passed to rendering pipelines per frame.
/// Uses ref struct semantics where applicable to guarantee zero GC heap allocations.
/// </summary>
public readonly ref struct SeaGlassRenderContext
{
    public required SeaGlassSurfaceFrameState FrameState { get; init; }
    public required float WidthPx { get; init; }
    public required float HeightPx { get; init; }
    public required Vector2 LocalBackdropOffsetPx { get; init; }
    public required SeaGlassDeviceTier ActiveTier { get; init; }
    public required SeaGlassRenderScratchArena ScratchArena { get; init; }
    public IReadOnlyList<SeaGlassSdfEngine.SdfNode>? ActiveNodes { get; init; }
    public float SmoothRadiusPx { get; init; }
}
