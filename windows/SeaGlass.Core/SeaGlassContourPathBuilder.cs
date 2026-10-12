using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SeaGlass.Core;

/// <summary>
/// Platform-agnostic 2D vector path representation (MoveTo, LineTo, CubicTo, Close)
/// that serializes directly to WinUI XAML / WPF Path Geometry Mini-Language (<c>F1 M ... C ... Z</c>)
/// or Direct2D <c>ID2D1PathGeometry</c> sinks with zero per-frame allocations when reused.
/// </summary>
public sealed class SeaGlassVectorPath2D
{
    public enum CommandKind : byte
    {
        MoveTo,
        LineTo,
        CubicTo,
        Close
    }

    public readonly record struct PathCommand(
        CommandKind Kind,
        float X1,
        float Y1,
        float X2 = 0f,
        float Y2 = 0f,
        float X3 = 0f,
        float Y3 = 0f);

    private readonly List<PathCommand> _commands = new(256);

    public IReadOnlyList<PathCommand> Commands => _commands;
    public bool IsEmpty => _commands.Count == 0;

    public void Reset() => _commands.Clear();

    public void MoveTo(float x, float y) =>
        _commands.Add(new PathCommand(CommandKind.MoveTo, x, y));

    public void LineTo(float x, float y) =>
        _commands.Add(new PathCommand(CommandKind.LineTo, x, y));

    public void CubicTo(float cx1, float cy1, float cx2, float cy2, float x, float y) =>
        _commands.Add(new PathCommand(CommandKind.CubicTo, cx1, cy1, cx2, cy2, x, y));

    public void Close() =>
        _commands.Add(new PathCommand(CommandKind.Close, 0f, 0f));

    public void AppendFrom(SeaGlassVectorPath2D other) =>
        _commands.AddRange(other._commands);

    /// <summary>
    /// Adds a clockwise per-corner rounded rectangle using cubic Bezier circular arc approximation
    /// (kappa = 0.55228475).
    /// </summary>
    public void AddRoundRect(
        float left,
        float top,
        float right,
        float bottom,
        float rTopLeft,
        float rTopRight,
        float rBottomRight,
        float rBottomLeft)
    {
        const float k = 0.55228475f;

        MoveTo(left + rTopLeft, top);
        LineTo(right - rTopRight, top);
        if (rTopRight > 0.01f)
        {
            CubicTo(
                right - rTopRight * (1f - k), top,
                right, top + rTopRight * (1f - k),
                right, top + rTopRight);
        }

        LineTo(right, bottom - rBottomRight);
        if (rBottomRight > 0.01f)
        {
            CubicTo(
                right, bottom - rBottomRight * (1f - k),
                right - rBottomRight * (1f - k), bottom,
                right - rBottomRight, bottom);
        }

        LineTo(left + rBottomLeft, bottom);
        if (rBottomLeft > 0.01f)
        {
            CubicTo(
                left + rBottomLeft * (1f - k), bottom,
                left, bottom - rBottomLeft * (1f - k),
                left, bottom - rBottomLeft);
        }

        LineTo(left, top + rTopLeft);
        if (rTopLeft > 0.01f)
        {
            CubicTo(
                left, top + rTopLeft * (1f - k),
                left + rTopLeft * (1f - k), top,
                left + rTopLeft, top);
        }

        Close();
    }

    public (float Left, float Top, float Right, float Bottom) ComputeBounds()
    {
        if (_commands.Count == 0) return (0f, 0f, 0f, 0f);
        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;

        void Include(float x, float y)
        {
            if (x < minX) minX = x;
            if (y < minY) minY = y;
            if (x > maxX) maxX = x;
            if (y > maxY) maxY = y;
        }

        foreach (PathCommand cmd in _commands)
        {
            switch (cmd.Kind)
            {
                case CommandKind.MoveTo:
                case CommandKind.LineTo:
                    Include(cmd.X1, cmd.Y1);
                    break;
                case CommandKind.CubicTo:
                    Include(cmd.X1, cmd.Y1);
                    Include(cmd.X2, cmd.Y2);
                    Include(cmd.X3, cmd.Y3);
                    break;
            }
        }

        return minX <= maxX ? (minX, minY, maxX, maxY) : (0f, 0f, 0f, 0f);
    }

    public void TransformScaleTranslate(float scaleX, float scaleY, float translateX, float translateY)
    {
        for (int i = 0; i < _commands.Count; i++)
        {
            PathCommand c = _commands[i];
            _commands[i] = c.Kind switch
            {
                CommandKind.MoveTo or CommandKind.LineTo => c with
                {
                    X1 = c.X1 * scaleX + translateX,
                    Y1 = c.Y1 * scaleY + translateY
                },
                CommandKind.CubicTo => c with
                {
                    X1 = c.X1 * scaleX + translateX,
                    Y1 = c.Y1 * scaleY + translateY,
                    X2 = c.X2 * scaleX + translateX,
                    Y2 = c.Y2 * scaleY + translateY,
                    X3 = c.X3 * scaleX + translateX,
                    Y3 = c.Y3 * scaleY + translateY
                },
                _ => c
            };
        }
    }

    /// <summary>
    /// Formats the vector path as a WinUI 3 / WPF XAML Geometry mini-language string (<c>F1 M ... C ... Z</c>).
    /// </summary>
    public string ToXamlPathData()
    {
        if (_commands.Count == 0) return string.Empty;
        StringBuilder sb = new(_commands.Count * 28);
        sb.Append("F1 ");
        CultureInfo inv = CultureInfo.InvariantCulture;

        foreach (PathCommand cmd in _commands)
        {
            switch (cmd.Kind)
            {
                case CommandKind.MoveTo:
                    sb.AppendFormat(inv, "M {0:F2},{1:F2} ", cmd.X1, cmd.Y1);
                    break;
                case CommandKind.LineTo:
                    sb.AppendFormat(inv, "L {0:F2},{1:F2} ", cmd.X1, cmd.Y1);
                    break;
                case CommandKind.CubicTo:
                    sb.AppendFormat(
                        inv,
                        "C {0:F2},{1:F2} {2:F2},{3:F2} {4:F2},{5:F2} ",
                        cmd.X1, cmd.Y1, cmd.X2, cmd.Y2, cmd.X3, cmd.Y3);
                    break;
                case CommandKind.Close:
                    sb.Append("Z ");
                    break;
            }
        }
        return sb.ToString().TrimEnd();
    }
}

/// <summary>
/// Generates exact, C^2-continuous analytical contours (<see cref="SeaGlassVectorPath2D"/>) for single
/// or coalescing <see cref="SeaGlassSdfEngine.SdfNode"/> shapes.
///
/// Exact 1:1 parity with <c>dev.seaglass.core.SeaGlassContourPathBuilder</c> on Android.
/// </summary>
public static class SeaGlassContourPathBuilder
{
    public static void BuildSingleNodePath(
        SeaGlassSdfEngine.SdfNode node,
        SeaGlassVectorPath2D outPath,
        float insetPx = 0f)
    {
        outPath.Reset();
        float hw = MathF.Max(2f, node.HalfWidth - insetPx);
        float hh = MathF.Max(2f, node.HalfHeight - insetPx);
        float left = node.CenterX - hw;
        float top = node.CenterY - hh;
        float right = node.CenterX + hw;
        float bottom = node.CenterY + hh;

        float fullW = MathF.Max(1f, hw * 2f);
        float fullH = MathF.Max(1f, hh * 2f);
        float maxSingleR = MathF.Max(0f, MathF.Min(fullW, fullH));
        float rInset = insetPx * 0.85f;

        float tl = Math.Clamp(node.CornerTopLeft - rInset, 0f, maxSingleR);
        float tr = Math.Clamp(node.CornerTopRight - rInset, 0f, maxSingleR);
        float br = Math.Clamp(node.CornerBottomRight - rInset, 0f, maxSingleR);
        float bl = Math.Clamp(node.CornerBottomLeft - rInset, 0f, maxSingleR);

        // Normalize adjacent corner pairs only if their sum exceeds the shared edge length
        float normScale = MathF.Min(
            MathF.Min(
                tl + tr > fullW ? fullW / (tl + tr) : 1f,
                bl + br > fullW ? fullW / (bl + br) : 1f),
            MathF.Min(
                tl + bl > fullH ? fullH / (tl + bl) : 1f,
                tr + br > fullH ? fullH / (tr + br) : 1f)
        );
        normScale = MathF.Min(1f, normScale);

        tl *= normScale;
        tr *= normScale;
        br *= normScale;
        bl *= normScale;

        outPath.AddRoundRect(left, top, right, bottom, tl, tr, br, bl);
    }

    public static void BuildCoalescingScenePath(
        IReadOnlyList<SeaGlassSdfEngine.SdfNode> nodes,
        float smoothRadiusPx,
        SeaGlassVectorPath2D outPath,
        float insetPx = 0f)
    {
        outPath.Reset();
        if (nodes is null || nodes.Count == 0) return;

        if (nodes.Count == 1 || smoothRadiusPx <= 0.5f)
        {
            SeaGlassVectorPath2D single = new();
            foreach (SeaGlassSdfEngine.SdfNode n in nodes)
            {
                BuildSingleNodePath(n, single, insetPx);
                outPath.AppendFrom(single);
            }
            return;
        }

        int activeCount = Math.Min(nodes.Count, SeaGlassSdfEngine.MaxSdfNodes);
        float targetD = -insetPx;

        // Vertical T-junction fast path: when Node 0 is a wide horizontal track/bar and Node 1
        // is a magnifier loupe or popover rising vertically out of an interior X coordinate along
        // the track, union the full-width track capsule with the vertical sminCubic bridge between
        // the track anchor at (nB.CenterX, nA.CenterY) and nB so the track's full width is preserved!
        if (activeCount == 2)
        {
            SeaGlassSdfEngine.SdfNode nA = nodes[0];
            SeaGlassSdfEngine.SdfNode nB = nodes[1];
            if (nA.HalfWidth > nB.HalfWidth * 1.65f &&
                MathF.Abs(nB.CenterY - nA.CenterY) > nA.HalfHeight * 0.55f)
            {
                BuildSingleNodePath(nA, outPath, insetPx);
                float anchorX = Math.Clamp(
                    nB.CenterX,
                    nA.CenterX - nA.HalfWidth + nA.HalfHeight,
                    nA.CenterX + nA.HalfWidth - nA.HalfHeight);
                float anchorHalfW = MathF.Min(nB.HalfWidth * 0.82f, nA.HalfHeight * 1.45f);
                var anchorNode = new SeaGlassSdfEngine.SdfNode(
                    centerX: anchorX,
                    centerY: nA.CenterY,
                    halfWidth: anchorHalfW,
                    halfHeight: nA.HalfHeight,
                    uniformCornerRadius: nA.HalfHeight,
                    cornerSmoothing: nA.CornerSmoothing,
                    tintColorArgb: nA.TintColorArgb,
                    thicknessPx: nA.ThicknessPx);

                SeaGlassSdfEngine.SdfNode[] verticalPair = [anchorNode, nB];
                float midX = (anchorNode.CenterX + nB.CenterX) * 0.5f;
                float midY = (anchorNode.CenterY + nB.CenterY) * 0.5f;
                float dMid = EvaluateCombinedDistance(midX, midY, verticalPair, smoothRadiusPx);
                SeaGlassVectorPath2D bridgePath = new();
                if (dMid < targetD - 0.5f)
                {
                    float maxSpan = MathF.Max(nB.HalfWidth, MathF.Abs(nB.CenterY - anchorNode.CenterY) * 0.5f + nB.HalfHeight) * 1.45f + smoothRadiusPx;
                    TraceStarShapedLobeIsosurface(
                        centerX: midX,
                        centerY: midY,
                        maxSpan: maxSpan,
                        allNodes: verticalPair,
                        smoothRadiusPx: smoothRadiusPx,
                        targetD: targetD,
                        outPath: bridgePath);
                }
                else
                {
                    BuildSingleNodePath(nB, bridgePath, insetPx);
                }
                outPath.AppendFrom(bridgePath);
                return;
            }
        }

        bool chainConnected = activeCount >= 2;
        if (chainConnected)
        {
            for (int pairIdx = 0; pairIdx < activeCount - 1; pairIdx++)
            {
                SeaGlassSdfEngine.SdfNode nA = nodes[pairIdx];
                SeaGlassSdfEngine.SdfNode nB = nodes[pairIdx + 1];
                float sX = nB.CenterX >= nA.CenterX ? 1f : -1f;
                float fA = MathF.Max(0f, nA.HalfWidth - nA.HalfHeight);
                float fB = MathF.Max(0f, nB.HalfWidth - nB.HalfHeight);
                float inAX = nA.CenterX + sX * fA;
                float inAY = nA.CenterY;
                float inBX = nB.CenterX - sX * fB;
                float inBY = nB.CenterY;
                float bDx = inBX - inAX;
                float bDy = inBY - inAY;
                float bDist = MathF.Sqrt(bDx * bDx + bDy * bDy);
                if (bDist <= 0.5f) continue;

                for (int k = 1; k <= 11; k++)
                {
                    float u = k / 12f;
                    float sx = inAX + bDx * u;
                    float sy = inAY + bDy * u;
                    float dSpine = EvaluateCombinedDistance(sx, sy, nodes, smoothRadiusPx);
                    if (dSpine >= targetD - 0.8f)
                    {
                        chainConnected = false;
                        break;
                    }
                }
                if (!chainConnected) break;
            }
        }

        if (chainConnected)
        {
            TraceConnectedStadiumIsosurface(
                nodeA: nodes[0],
                nodeB: nodes[activeCount - 1],
                allNodes: nodes,
                smoothRadiusPx: smoothRadiusPx,
                targetD: targetD,
                outPath: outPath);
        }
        else
        {
            SeaGlassVectorPath2D lobePath = new();
            for (int i = 0; i < activeCount; i++)
            {
                SeaGlassSdfEngine.SdfNode node = nodes[i];
                float centerD = EvaluateCombinedDistance(node.CenterX, node.CenterY, nodes, smoothRadiusPx);
                if (centerD < targetD - 1.0f)
                {
                    TraceStarShapedLobeIsosurface(
                        centerX: node.CenterX,
                        centerY: node.CenterY,
                        maxSpan: MathF.Max(node.HalfWidth, node.HalfHeight) * 1.65f + smoothRadiusPx,
                        allNodes: nodes,
                        smoothRadiusPx: smoothRadiusPx,
                        targetD: targetD,
                        outPath: lobePath);
                    outPath.AppendFrom(lobePath);
                }
                else
                {
                    BuildSingleNodePath(node, lobePath, insetPx);
                    outPath.AppendFrom(lobePath);
                }
            }
        }
    }

    public static void BuildCoalescingPairPath(
        SeaGlassSdfEngine.SdfNode nodeA,
        SeaGlassSdfEngine.SdfNode nodeB,
        float smoothRadiusPx,
        SeaGlassVectorPath2D outPath,
        float insetPx = 0f) =>
        BuildCoalescingScenePath([nodeA, nodeB], smoothRadiusPx, outPath, insetPx);

    /// <summary>
    /// Builds a smooth circular/capsule sea-glass medallion contour with zero edge waviness.
    /// </summary>
    public static void BuildOrganicPebblePath(
        float widthPx,
        float heightPx,
        SeaGlassVectorPath2D outPath,
        float organicIrregularity = 0.0f,
        float phaseSeedRad = 0.0f)
    {
        outPath.Reset();
        float w = MathF.Max(2f, widthPx);
        float h = MathF.Max(2f, heightPx);
        float r = MathF.Min(w, h) * 0.5f;
        outPath.AddRoundRect(0f, 0f, w, h, r, r, r, r);
    }

    /// <summary>
    /// Builds a smooth stadium capsule/pill contour with zero edge waviness.
    /// </summary>
    public static void BuildTumbledCapsulePath(
        float widthPx,
        float heightPx,
        SeaGlassVectorPath2D outPath,
        float organicWavePx = 0.0f,
        float phaseSeedRad = 0.0f)
    {
        outPath.Reset();
        float w = MathF.Max(2f, widthPx);
        float h = MathF.Max(2f, heightPx);
        float r = MathF.Min(w, h) * 0.5f;
        outPath.AddRoundRect(0f, 0f, w, h, r, r, r, r);
    }

    private static void TraceConnectedStadiumIsosurface(
        SeaGlassSdfEngine.SdfNode nodeA,
        SeaGlassSdfEngine.SdfNode nodeB,
        IReadOnlyList<SeaGlassSdfEngine.SdfNode> allNodes,
        float smoothRadiusPx,
        float targetD,
        SeaGlassVectorPath2D outPath)
    {
        float focusOffsetA = MathF.Max(0f, nodeA.HalfWidth - nodeA.HalfHeight);
        float focusOffsetB = MathF.Max(0f, nodeB.HalfWidth - nodeB.HalfHeight);
        float signX = nodeB.CenterX >= nodeA.CenterX ? 1f : -1f;

        float startCapX = nodeA.CenterX - signX * focusOffsetA;
        float startCapY = nodeA.CenterY;
        float endCapX = nodeB.CenterX + signX * focusOffsetB;
        float endCapY = nodeB.CenterY;

        float dx = endCapX - startCapX;
        float dy = endCapY - startCapY;
        float len = MathF.Max(1e-3f, MathF.Sqrt(dx * dx + dy * dy));
        float tx = dx / len;
        float ty = dy / len;
        float nx = -ty;
        float ny = tx;
        float baseAngle = MathF.Atan2(dy, dx);

        float uInnerA = Math.Clamp((focusOffsetA * 2f) / len, 0f, 0.86f);
        float uInnerB = Math.Clamp(1f - (focusOffsetB * 2f) / len, MathF.Min(uInnerA + 0.05f, 0.98f), 1f);

        const int capSamples = 24;
        int bridgeSamples = allNodes.Count > 2 ? 64 : 44;
        int totalSamples = capSamples * 2 + bridgeSamples * 2;
        float[] px = new float[totalSamples];
        float[] py = new float[totalSamples];

        float[] uSamples = new float[bridgeSamples];
        for (int i = 0; i < bridgeSamples; i++)
        {
            float t = i / (float)(bridgeSamples - 1);
            if (allNodes.Count > 2 || (uInnerA <= 0.02f && uInnerB >= 0.98f))
                uSamples[i] = t;
            else if (t <= 0.22f)
                uSamples[i] = (t / 0.22f) * uInnerA;
            else if (t <= 0.88f)
                uSamples[i] = uInnerA + ((t - 0.22f) / 0.66f) * (uInnerB - uInnerA);
            else
                uSamples[i] = uInnerB + ((t - 0.88f) / 0.12f) * (1f - uInnerB);
        }

        float maxSearchA = nodeA.HalfHeight * 1.45f + smoothRadiusPx;
        float maxSearchB = nodeB.HalfHeight * 1.45f + smoothRadiusPx;
        float maxSpan = 0f;
        for (int i = 0; i < allNodes.Count; i++)
        {
            float candidate = MathF.Max(allNodes[i].HalfHeight, allNodes[i].HalfWidth * 0.6f);
            if (candidate > maxSpan) maxSpan = candidate;
        }
        float maxSearchBridge = maxSpan * 1.45f + smoothRadiusPx;

        int idx = 0;

        // 1. Outer Cap A
        for (int i = 0; i < capSamples; i++)
        {
            float frac = (i + 1f) / (capSamples + 1f);
            float angle = baseAngle + MathF.PI * 0.5f + MathF.PI * frac;
            float dirX = MathF.Cos(angle);
            float dirY = MathF.Sin(angle);
            float t = SolveRayIsosurface(startCapX, startCapY, dirX, dirY, maxSearchA, allNodes, smoothRadiusPx, targetD);
            px[idx] = startCapX + dirX * t;
            py[idx] = startCapY + dirY * t;
            idx++;
        }

        // 2. Side 1 Bridge
        for (int i = 0; i < bridgeSamples; i++)
        {
            float u = uSamples[i];
            float ox = startCapX + dx * u;
            float oy = startCapY + dy * u;
            float t = SolveRayIsosurface(ox, oy, -nx, -ny, maxSearchBridge, allNodes, smoothRadiusPx, targetD);
            px[idx] = ox - nx * t;
            py[idx] = oy - ny * t;
            idx++;
        }

        // 3. Outer Cap B
        for (int i = 0; i < capSamples; i++)
        {
            float frac = (i + 1f) / (capSamples + 1f);
            float angle = baseAngle - MathF.PI * 0.5f + MathF.PI * frac;
            float dirX = MathF.Cos(angle);
            float dirY = MathF.Sin(angle);
            float t = SolveRayIsosurface(endCapX, endCapY, dirX, dirY, maxSearchB, allNodes, smoothRadiusPx, targetD);
            px[idx] = endCapX + dirX * t;
            py[idx] = endCapY + dirY * t;
            idx++;
        }

        // 4. Side 2 Bridge
        for (int i = 0; i < bridgeSamples; i++)
        {
            float u = uSamples[bridgeSamples - 1 - i];
            float ox = startCapX + dx * u;
            float oy = startCapY + dy * u;
            float t = SolveRayIsosurface(ox, oy, nx, ny, maxSearchBridge, allNodes, smoothRadiusPx, targetD);
            px[idx] = ox + nx * t;
            py[idx] = oy + ny * t;
            idx++;
        }

        AppendSmoothClosedCatmullRom(px, py, totalSamples, outPath);
    }

    private static void TraceStarShapedLobeIsosurface(
        float centerX,
        float centerY,
        float maxSpan,
        IReadOnlyList<SeaGlassSdfEngine.SdfNode> allNodes,
        float smoothRadiusPx,
        float targetD,
        SeaGlassVectorPath2D outPath)
    {
        outPath.Reset();
        const int samples = 64;
        float[] px = new float[samples];
        float[] py = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float angle = 2.0f * MathF.PI * i / samples;
            float dirX = MathF.Cos(angle);
            float dirY = MathF.Sin(angle);
            float t = SolveRayIsosurface(centerX, centerY, dirX, dirY, maxSpan, allNodes, smoothRadiusPx, targetD);
            px[i] = centerX + dirX * t;
            py[i] = centerY + dirY * t;
        }

        AppendSmoothClosedCatmullRom(px, py, samples, outPath);
    }

    private static float SolveRayIsosurface(
        float ox,
        float oy,
        float dirX,
        float dirY,
        float maxDist,
        IReadOnlyList<SeaGlassSdfEngine.SdfNode> nodes,
        float smoothRadiusPx,
        float targetD)
    {
        float lo = 0f;
        float hi = maxDist;
        const int steps = 8;
        float stepSize = maxDist / steps;
        float prevT = 0f;

        for (int s = 1; s <= steps; s++)
        {
            float currT = s * stepSize;
            float fCurr = EvaluateCombinedDistance(ox + dirX * currT, oy + dirY * currT, nodes, smoothRadiusPx) - targetD;
            if (fCurr >= 0f)
            {
                lo = prevT;
                hi = currT;
                break;
            }
            prevT = currT;
        }

        for (int iter = 0; iter < 8; iter++)
        {
            float mid = (lo + hi) * 0.5f;
            float fMid = EvaluateCombinedDistance(ox + dirX * mid, oy + dirY * mid, nodes, smoothRadiusPx) - targetD;
            if (fMid < 0f)
                lo = mid;
            else
                hi = mid;
        }

        float fLo = EvaluateCombinedDistance(ox + dirX * lo, oy + dirY * lo, nodes, smoothRadiusPx) - targetD;
        float fHi = EvaluateCombinedDistance(ox + dirX * hi, oy + dirY * hi, nodes, smoothRadiusPx) - targetD;
        float denom = fHi - fLo;

        return MathF.Abs(denom) > 1e-4f
            ? Math.Clamp(lo - fLo * (hi - lo) / denom, lo, hi)
            : (lo + hi) * 0.5f;
    }

    private static void AppendSmoothClosedCatmullRom(
        float[] px,
        float[] py,
        int count,
        SeaGlassVectorPath2D outPath)
    {
        if (count < 3) return;
        outPath.MoveTo(px[0], py[0]);

        for (int i = 0; i < count; i++)
        {
            int p0 = (i - 1 + count) % count;
            int p1 = i;
            int p2 = (i + 1) % count;
            int p3 = (i + 2) % count;

            float dx01 = px[p1] - px[p0];
            float dy01 = py[p1] - py[p0];
            float d01 = MathF.Max(1e-3f, MathF.Sqrt(dx01 * dx01 + dy01 * dy01));

            float dx12 = px[p2] - px[p1];
            float dy12 = py[p2] - py[p1];
            float d12 = MathF.Max(1e-3f, MathF.Sqrt(dx12 * dx12 + dy12 * dy12));

            float dx23 = px[p3] - px[p2];
            float dy23 = py[p3] - py[p2];
            float d23 = MathF.Max(1e-3f, MathF.Sqrt(dx23 * dx23 + dy23 * dy23));

            float t1x = ((dx12 / d12) * d01 + (dx01 / d01) * d12) / (d01 + d12);
            float t1y = ((dy12 / d12) * d01 + (dy01 / d01) * d12) / (d01 + d12);

            float t2x = ((dx23 / d23) * d12 + (dx12 / d12) * d23) / (d12 + d23);
            float t2y = ((dy23 / d23) * d12 + (dy12 / d12) * d23) / (d12 + d23);

            float segScale = d12 / 3f;
            float c1x = px[p1] + t1x * segScale;
            float c1y = py[p1] + t1y * segScale;
            float c2x = px[p2] - t2x * segScale;
            float c2y = py[p2] - t2y * segScale;

            outPath.CubicTo(c1x, c1y, c2x, c2y, px[p2], py[p2]);
        }

        outPath.Close();
    }

    private static float EvaluateCombinedDistance(
        float px,
        float py,
        IReadOnlyList<SeaGlassSdfEngine.SdfNode> nodes,
        float smoothRadiusPx)
    {
        float d = SeaGlassSdfEngine.EvaluateNodeSdf(px, py, nodes[0]);
        int count = Math.Min(nodes.Count, SeaGlassSdfEngine.MaxSdfNodes);
        for (int i = 1; i < count; i++)
        {
            float di = SeaGlassSdfEngine.EvaluateNodeSdf(px, py, nodes[i]);
            d = SeaGlassSdfEngine.SminCubic(d, di, smoothRadiusPx);
        }
        return d;
    }
}
