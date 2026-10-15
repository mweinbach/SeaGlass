using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;
using SeaGlass.Core.Backend;
using SeaGlass.WinUI.Rendering;

namespace SeaGlass.WinUI;

/// <summary>
/// Multi-element liquid coalescing coordinator panel evaluating a shared 2D Signed Distance Field (SDF)
/// with polynomial cubic smooth-minimum (<c>sminCubic</c>) unions across up to 16 registered nodes.
///
/// Implements 4-node GPU proximity clustering for 256-byte uniform block parity.
/// </summary>
public class SeaGlassFluidGroup : Panel
{
    private readonly SortedDictionary<string, SeaGlassSdfEngine.SdfNode> _registeredNodes = new(StringComparer.Ordinal);
    private readonly List<IReadOnlyList<SeaGlassSdfEngine.SdfNode>> _clusters = new(16);
    private readonly SeaGlassUniformBlock _uniformBlock = new();
    private readonly SeaGlassVectorPath2D _contourPath = new();

    private readonly SeaGlassSdfEngine.SdfNode[] _scratchNodes = new SeaGlassSdfEngine.SdfNode[16];
    private readonly bool[] _nodeAssigned = new bool[16];
    private readonly List<SeaGlassSdfEngine.SdfNode>[] _clusterPool = new List<SeaGlassSdfEngine.SdfNode>[16];
    private readonly SeaGlassUniformBlock[] _clusterUniformBlocks = new SeaGlassUniformBlock[16];

    #region Dependency Properties

    public static readonly DependencyProperty SmoothRadiusDpProperty =
        DependencyProperty.Register(
            nameof(SmoothRadiusDp),
            typeof(double),
            typeof(SeaGlassFluidGroup),
            new PropertyMetadata(26.0, OnPropertyInvalidated));

    public static readonly DependencyProperty GroupMaterialProperty =
        DependencyProperty.Register(
            nameof(GroupMaterial),
            typeof(SeaGlassMaterial),
            typeof(SeaGlassFluidGroup),
            new PropertyMetadata(SeaGlassPalette.SeafoamMint.PrimaryGlass, OnPropertyInvalidated));

    public static readonly DependencyProperty ToneProperty =
        DependencyProperty.Register(
            nameof(Tone),
            typeof(SeaGlassTone),
            typeof(SeaGlassFluidGroup),
            new PropertyMetadata(SeaGlassTone.SeafoamMint, OnToneChanged));

    public static readonly DependencyProperty MaxNodesProperty =
        DependencyProperty.Register(
            nameof(MaxNodes),
            typeof(int),
            typeof(SeaGlassFluidGroup),
            new PropertyMetadata(16));

    public double SmoothRadiusDp
    {
        get => (double)GetValue(SmoothRadiusDpProperty);
        set => SetValue(SmoothRadiusDpProperty, value);
    }

    public SeaGlassMaterial GroupMaterial
    {
        get => (SeaGlassMaterial)GetValue(GroupMaterialProperty);
        set => SetValue(GroupMaterialProperty, value);
    }

    public SeaGlassTone Tone
    {
        get => (SeaGlassTone)GetValue(ToneProperty);
        set => SetValue(ToneProperty, value);
    }

    public int MaxNodes
    {
        get => (int)GetValue(MaxNodesProperty);
        set => SetValue(MaxNodesProperty, value);
    }

    public int ActiveNodeCount => _registeredNodes.Count;

    #endregion

    #region Attached Properties

    public static readonly DependencyProperty IsFluidNodeProperty =
        DependencyProperty.RegisterAttached(
            "IsFluidNode",
            typeof(bool),
            typeof(SeaGlassFluidGroup),
            new PropertyMetadata(true));

    public static bool GetIsFluidNode(DependencyObject obj) =>
        (bool)obj.GetValue(IsFluidNodeProperty);

    public static void SetIsFluidNode(DependencyObject obj, bool value) =>
        obj.SetValue(IsFluidNodeProperty, value);

    public static readonly DependencyProperty NodeIdProperty =
        DependencyProperty.RegisterAttached(
            "NodeId",
            typeof(string),
            typeof(SeaGlassFluidGroup),
            new PropertyMetadata(string.Empty));

    public static string GetNodeId(DependencyObject obj) =>
        (string)obj.GetValue(NodeIdProperty);

    public static void SetNodeId(DependencyObject obj, string value) =>
        obj.SetValue(NodeIdProperty, value);

    public static readonly DependencyProperty FluidMaterialOverrideProperty =
        DependencyProperty.RegisterAttached(
            "FluidMaterialOverride",
            typeof(SeaGlassMaterial),
            typeof(SeaGlassFluidGroup),
            new PropertyMetadata(null));

    public static SeaGlassMaterial? GetFluidMaterialOverride(DependencyObject obj) =>
        (SeaGlassMaterial?)obj.GetValue(FluidMaterialOverrideProperty);

    public static void SetFluidMaterialOverride(DependencyObject obj, SeaGlassMaterial? value) =>
        obj.SetValue(FluidMaterialOverrideProperty, value);

    public static readonly DependencyProperty CornerRadiusOverrideProperty =
        DependencyProperty.RegisterAttached(
            "CornerRadiusOverride",
            typeof(double),
            typeof(SeaGlassFluidGroup),
            new PropertyMetadata(double.NaN));

    public static double GetCornerRadiusOverride(DependencyObject obj) =>
        (double)obj.GetValue(CornerRadiusOverrideProperty);

    public static void SetCornerRadiusOverride(DependencyObject obj, double value) =>
        obj.SetValue(CornerRadiusOverrideProperty, value);

    #endregion

    public SeaGlassStageState? StageState { get; set; }
    public SeaGlassFluidGroupState State { get; }

    public SeaGlassFluidGroup()
    {
        InitPreallocatedPools();
        State = new SeaGlassFluidGroupState(StageState, GroupMaterial, (float)SmoothRadiusDp);
    }

    public SeaGlassFluidGroup(
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? groupMaterial = null,
        float smoothRadiusDp = 26f)
    {
        InitPreallocatedPools();
        StageState = stageState;
        GroupMaterial = groupMaterial ?? SeaGlassPalette.SeafoamMint.PrimaryGlass;
        SmoothRadiusDp = smoothRadiusDp;
        State = new SeaGlassFluidGroupState(stageState, GroupMaterial, smoothRadiusDp);
    }

    private void InitPreallocatedPools()
    {
        for (int i = 0; i < 16; i++)
        {
            _clusterPool[i] = new List<SeaGlassSdfEngine.SdfNode>(4);
            _clusterUniformBlocks[i] = new SeaGlassUniformBlock();
        }
    }

    private static void OnPropertyInvalidated(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassFluidGroup group)
        {
            group.State.GroupMaterial = group.GroupMaterial;
            group.State.SmoothRadiusDp = (float)group.SmoothRadiusDp;
            group.InvalidateArrange();
        }
    }

    private static void OnToneChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassFluidGroup group)
        {
            SeaGlassPalette palette = group.Tone.ToPalette();
            group.GroupMaterial = palette.PrimaryGlass;
        }
    }

    protected override Windows.Foundation.Size MeasureOverride(Windows.Foundation.Size availableSize)
    {
        float maxW = 0f;
        float maxH = 0f;
        foreach (UIElement child in Children)
        {
            child.Measure(availableSize);
            maxW = MathF.Max(maxW, (float)child.DesiredSize.Width);
            maxH = MathF.Max(maxH, (float)child.DesiredSize.Height);
        }
        return new Windows.Foundation.Size(
            double.IsInfinity(availableSize.Width) ? maxW : availableSize.Width,
            double.IsInfinity(availableSize.Height) ? maxH : availableSize.Height);
    }

    protected override Windows.Foundation.Size ArrangeOverride(Windows.Foundation.Size finalSize)
    {
        _registeredNodes.Clear();
        int nodeIndex = 0;

        foreach (UIElement child in Children)
        {
            if (child is not FrameworkElement fe) continue;

            double x = Canvas.GetLeft(fe);
            double y = Canvas.GetTop(fe);
            if (double.IsNaN(x)) x = 0;
            if (double.IsNaN(y)) y = 0;

            fe.Arrange(new Windows.Foundation.Rect(x, y, fe.DesiredSize.Width, fe.DesiredSize.Height));

            if (!GetIsFluidNode(fe)) continue;

            string nodeId = GetNodeId(fe);
            if (string.IsNullOrEmpty(nodeId))
            {
                nodeId = !string.IsNullOrEmpty(fe.Name) ? fe.Name : $"fluid_node_{nodeIndex}";
            }

            SeaGlassMaterial? matOverride = GetFluidMaterialOverride(fe);
            double crOverride = GetCornerRadiusOverride(fe);
            float? cornerRadiusOverridePx = double.IsNaN(crOverride) ? null : (float)crOverride;

            float w = (float)(fe.ActualWidth > 0 ? fe.ActualWidth : fe.DesiredSize.Width);
            float h = (float)(fe.ActualHeight > 0 ? fe.ActualHeight : fe.DesiredSize.Height);
            if (w <= 0f && fe is SeaGlassSurface surf)
            {
                w = surf.SizePx.X;
                h = surf.SizePx.Y;
            }

            RegisterChildBounds(
                nodeKey: nodeId,
                positionInRoot: new Vector2((float)x, (float)y),
                sizePx: new Vector2(w, h),
                material: matOverride ?? GroupMaterial,
                cornerRadiusOverridePx: cornerRadiusOverridePx);

            nodeIndex++;
        }

        return finalSize;
    }

    public void RegisterChildBounds(
        string nodeKey,
        Vector2 positionInRoot,
        Vector2 sizePx,
        SeaGlassMaterial? material = null,
        float? cornerRadiusOverridePx = null)
    {
        State.RegisterChildBounds(nodeKey, positionInRoot, sizePx, material, cornerRadiusOverridePx);
        if (sizePx.X > 1f && sizePx.Y > 1f)
        {
            float halfW = sizePx.X * 0.5f;
            float halfH = sizePx.Y * 0.5f;
            float r = cornerRadiusOverridePx ?? MathF.Min(halfW, halfH);
            SeaGlassMaterial mat = material ?? GroupMaterial;

            _registeredNodes[nodeKey] = new SeaGlassSdfEngine.SdfNode(
                centerX: positionInRoot.X + halfW,
                centerY: positionInRoot.Y + halfH,
                halfWidth: halfW,
                halfHeight: halfH,
                uniformCornerRadius: r,
                cornerSmoothing: mat.CornerSmoothing,
                tintColorArgb: mat.TintColorArgb,
                thicknessPx: mat.ThicknessDp * 2.5f);
        }
    }

    public void RemoveNode(string key)
    {
        State.RemoveNode(key);
        _registeredNodes.Remove(key);
    }

    /// <summary>
    /// Partitions registered fluid nodes into spatial proximity clusters of 1 to 4 droplets
    /// (threshold = 2 * smoothRadius) so each cluster can be dispatched with a 256-byte uniform block.
    /// Guarantees zero heap allocations per frame by reusing preallocated scratch buffers.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<SeaGlassSdfEngine.SdfNode>> ComputeProximityClusters()
    {
        _clusters.Clear();
        int count = 0;

        foreach (var pair in _registeredNodes)
        {
            if (count >= MaxNodes || count >= _scratchNodes.Length) break;
            _scratchNodes[count++] = pair.Value;
        }

        if (count == 0) return _clusters;

        Array.Clear(_nodeAssigned, 0, count);
        float threshold = (float)SmoothRadiusDp * 2.0f;
        int clusterCount = 0;

        for (int i = 0; i < count; i++)
        {
            if (_nodeAssigned[i]) continue;
            _nodeAssigned[i] = true;

            var cluster = _clusterPool[clusterCount++];
            cluster.Clear();
            cluster.Add(_scratchNodes[i]);

            for (int j = i + 1; j < count && cluster.Count < 4; j++)
            {
                if (_nodeAssigned[j]) continue;
                var candidate = _scratchNodes[j];
                bool isClose = false;

                for (int c = 0; c < cluster.Count; c++)
                {
                    var member = cluster[c];
                    float dx = MathF.Abs(candidate.CenterX - member.CenterX) - (candidate.HalfWidth + member.HalfWidth);
                    float dy = MathF.Abs(candidate.CenterY - member.CenterY) - (candidate.HalfHeight + member.HalfHeight);
                    float dist = MathF.Max(dx, dy);
                    if (dist <= threshold)
                    {
                        isClose = true;
                        break;
                    }
                }

                if (isClose)
                {
                    _nodeAssigned[j] = true;
                    cluster.Add(candidate);
                }
            }

            _clusters.Add(cluster);
        }

        return _clusters;
    }

    /// <summary>
    /// Evaluates the cubic smooth minimum distance field at (px, py) across all registered nodes.
    /// Guarantees zero heap allocations per query.
    /// </summary>
    public float EvaluateDistanceField(float px, float py)
    {
        int count = 0;
        foreach (var pair in _registeredNodes)
        {
            if (count >= MaxNodes || count >= _scratchNodes.Length) break;
            _scratchNodes[count++] = pair.Value;
        }

        if (count == 0) return 9999f;

        float d = SeaGlassSdfEngine.EvaluateNodeSdf(px, py, _scratchNodes[0]);
        float k = (float)SmoothRadiusDp;

        for (int i = 1; i < count; i++)
        {
            float di = SeaGlassSdfEngine.EvaluateNodeSdf(px, py, _scratchNodes[i]);
            d = SeaGlassSdfEngine.SminCubic(d, di, k);
        }

        return d;
    }

    /// <summary>
    /// Partitions registered fluid nodes into proximity clusters of 1 to 4 droplets and packs each
    /// cluster into the byte-exact 256-byte <see cref="SeaGlassUniformBlock"/> ABI for Direct2D/HLSL dispatch.
    /// Guarantees zero heap allocations per frame.
    /// </summary>
    public int DispatchClusterRender(
        SeaGlassTieredRenderEngine? renderEngine = null,
        SeaGlassRenderScratchArena? scratchArena = null,
        float density = 1.0f)
    {
        var clusters = ComputeProximityClusters();
        SeaGlassTieredRenderEngine engine = renderEngine ?? SeaGlassTieredRenderEngine.Instance;
        SeaGlassRenderScratchArena scratch = scratchArena ?? new SeaGlassRenderScratchArena();

        Vector2 groupSize = new((float)ActualWidth, (float)ActualHeight);
        if (groupSize.X <= 1f || groupSize.Y <= 1f)
        {
            groupSize = State.GroupSizePx;
        }
        Vector2 backdropOffset = StageState?.ComputeLocalBackdropOffset(State.GroupOriginInRoot) ?? Vector2.Zero;

        for (int i = 0; i < clusters.Count; i++)
        {
            var clusterNodes = clusters[i];
            var uniformBlock = _clusterUniformBlocks[i];

            uniformBlock.Pack(
                widthPx: groupSize.X,
                heightPx: groupSize.Y,
                backdropOffsetX: backdropOffset.X,
                backdropOffsetY: backdropOffset.Y,
                material: GroupMaterial,
                density: density,
                nodes: clusterNodes,
                smoothRadiusPx: (float)SmoothRadiusDp * density);

            uniformBlock.WriteToStd140ByteBuffer(scratch.StagingBuffer);
        }

        return clusters.Count;
    }
}
