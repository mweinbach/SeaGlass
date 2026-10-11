using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SeaGlass.Core;
using SeaGlass.Core.Backend;

namespace SeaGlass.WinUI;

/// <summary>
/// Coordinates multiple sibling SeaGlass shapes inside a shared 2D Signed Distance Field (SDF)
/// so neighboring elements smoothly coalesce (<c>sminCubic</c>) and split like liquid sea-glass droplets.
///
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassFluidGroupState</c> on Android.
/// </summary>
public sealed class SeaGlassFluidGroupState
{
    private readonly SortedDictionary<string, SeaGlassSdfEngine.SdfNode> _registeredNodes = new(StringComparer.Ordinal);
    private readonly SeaGlassUniformBlock _uniformBlock = new();
    private readonly SeaGlassVectorPath2D _cachedContourPath = new();

    public SeaGlassStageState? StageState { get; }
    public SeaGlassMaterial GroupMaterial { get; set; }
    public float SmoothRadiusDp { get; set; }
    public Vector2 GroupOriginInRoot { get; set; }
    public Vector2 GroupSizePx { get; set; } = new(600f, 160f);

    public SeaGlassFluidGroupState(
        SeaGlassStageState? stageState,
        SeaGlassMaterial groupMaterial,
        float smoothRadiusDp = 26f)
    {
        StageState = stageState;
        GroupMaterial = groupMaterial;
        SmoothRadiusDp = smoothRadiusDp;
    }

    public void UpdateNode(string key, SeaGlassSdfEngine.SdfNode node)
    {
        _registeredNodes[key] = node;
    }

    public void RegisterChildBounds(
        string nodeKey,
        Vector2 positionInRoot,
        Vector2 sizePx,
        SeaGlassMaterial? material = null,
        float? cornerRadiusOverridePx = null)
    {
        if (sizePx.X <= 1f || sizePx.Y <= 1f) return;
        SeaGlassMaterial mat = material ?? GroupMaterial;
        Vector2 localTopLeft = positionInRoot - GroupOriginInRoot;
        float halfW = sizePx.X * 0.5f;
        float halfH = sizePx.Y * 0.5f;
        float radiusPx = cornerRadiusOverridePx ?? MathF.Min(halfW, halfH);

        _registeredNodes[nodeKey] = new SeaGlassSdfEngine.SdfNode(
            centerX: localTopLeft.X + halfW,
            centerY: localTopLeft.Y + halfH,
            halfWidth: halfW,
            halfHeight: halfH,
            uniformCornerRadius: radiusPx,
            cornerSmoothing: mat.CornerSmoothing,
            tintColorArgb: mat.TintColorArgb,
            thicknessPx: mat.ThicknessDp * 2.5f);
    }

    public void RemoveNode(string key)
    {
        _registeredNodes.Remove(key);
    }

    public IReadOnlyList<SeaGlassSdfEngine.SdfNode> SnapshotNodes()
    {
        return _registeredNodes.Values
            .Take(SeaGlassSdfEngine.MaxSdfNodes)
            .ToList();
    }

    /// <summary>
    /// Packs the shared 256-byte <see cref="SeaGlassUniformBlock"/> in place for all registered nodes.
    /// </summary>
    public SeaGlassUniformBlock PackUniformBlock(float density = 1.0f)
    {
        IReadOnlyList<SeaGlassSdfEngine.SdfNode> nodes = SnapshotNodes();
        Vector2 backdropOffset = StageState?.ComputeLocalBackdropOffset(GroupOriginInRoot) ?? Vector2.Zero;
        _uniformBlock.Pack(
            widthPx: GroupSizePx.X,
            heightPx: GroupSizePx.Y,
            backdropOffsetX: backdropOffset.X,
            backdropOffsetY: backdropOffset.Y,
            material: GroupMaterial,
            density: density,
            nodes: nodes,
            smoothRadiusPx: SmoothRadiusDp * density);
        return _uniformBlock;
    }

    /// <summary>
    /// Evaluates the C^2-continuous coalesced contour path for all registered droplets in this fluid group.
    /// </summary>
    public SeaGlassVectorPath2D BuildCoalescedContourPath(float density = 1.0f, float insetPx = 0f)
    {
        IReadOnlyList<SeaGlassSdfEngine.SdfNode> nodes = SnapshotNodes();
        SeaGlassContourPathBuilder.BuildCoalescingScenePath(
            nodes: nodes,
            smoothRadiusPx: SmoothRadiusDp * density,
            outPath: _cachedContourPath,
            insetPx: insetPx);
        return _cachedContourPath;
    }
}
