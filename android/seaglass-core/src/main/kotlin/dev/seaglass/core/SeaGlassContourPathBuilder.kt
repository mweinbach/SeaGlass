package dev.seaglass.core

import android.graphics.Path
import android.graphics.RectF
import kotlin.math.PI
import kotlin.math.abs
import kotlin.math.atan2
import kotlin.math.cos
import kotlin.math.max
import kotlin.math.min
import kotlin.math.sin
import kotlin.math.sqrt

/**
 * Generates exact, $C^2$-continuous analytical contours (`android.graphics.Path`) for single
 * or coalescing [SeaGlassSdfEngine.SdfNode] shapes.
 *
 * Uses **Stadium Spine-Normal & Radial 1D Bisection** on the exact [SeaGlassSdfEngine.sminCubic]
 * Signed Distance Field, packing 70% of bridge rays into the `[innerCapA -> innerCapB]` neck zone
 * so wide capsules (`SeaGlassFluidBar`) and circular droplets (`OpticsLabSection`, `FluidCoalescingSection`)
 * both trace razor-sharp, $C^2$-smooth concave hourglass necks.
 */
object SeaGlassContourPathBuilder {

    private val scratchRect = RectF()
    private val scratchRadii = FloatArray(8)

    fun buildSingleNodePath(
        node: SeaGlassSdfEngine.SdfNode,
        outPath: Path,
        insetPx: Float = 0f
    ) {
        outPath.reset()
        val hw = (node.halfWidth - insetPx).coerceAtLeast(2f)
        val hh = (node.halfHeight - insetPx).coerceAtLeast(2f)
        scratchRect.set(
            node.centerX - hw,
            node.centerY - hh,
            node.centerX + hw,
            node.centerY + hh
        )
        val fullW = (hw * 2f).coerceAtLeast(1f)
        val fullH = (hh * 2f).coerceAtLeast(1f)
        val maxSingleR = min(fullW, fullH).coerceAtLeast(0f)
        val rInset = insetPx * 0.85f

        var tl = (node.cornerTopLeft - rInset).coerceIn(0f, maxSingleR)
        var tr = (node.cornerTopRight - rInset).coerceIn(0f, maxSingleR)
        var br = (node.cornerBottomRight - rInset).coerceIn(0f, maxSingleR)
        var bl = (node.cornerBottomLeft - rInset).coerceIn(0f, maxSingleR)

        // Normalize adjacent corner pairs only if their sum exceeds the shared edge length.
        // This allows an asymmetric outer corner (e.g. 52dp inside a 55dp bezel) to use up to
        // (H - r_inner) on a 70dp pod when the opposite inner corner is small (e.g. 14dp)!
        val normScale = min(
            min(
                if (tl + tr > fullW) fullW / (tl + tr) else 1f,
                if (bl + br > fullW) fullW / (bl + br) else 1f
            ),
            min(
                if (tl + bl > fullH) fullH / (tl + bl) else 1f,
                if (tr + br > fullH) fullH / (tr + br) else 1f
            )
        ).coerceAtMost(1f)

        tl *= normScale
        tr *= normScale
        br *= normScale
        bl *= normScale

        scratchRadii[0] = tl
        scratchRadii[1] = tl
        scratchRadii[2] = tr
        scratchRadii[3] = tr
        scratchRadii[4] = br
        scratchRadii[5] = br
        scratchRadii[6] = bl
        scratchRadii[7] = bl

        outPath.addRoundRect(scratchRect, scratchRadii, Path.Direction.CW)
    }

    fun buildCoalescingScenePath(
        nodes: List<SeaGlassSdfEngine.SdfNode>,
        smoothRadiusPx: Float,
        outPath: Path,
        insetPx: Float = 0f
    ) {
        outPath.reset()
        if (nodes.isEmpty()) return
        if (nodes.size == 1) {
            buildSingleNodePath(nodes[0], outPath, insetPx)
            return
        }
        if (smoothRadiusPx <= 0.5f) {
            val single = Path()
            for (n in nodes) {
                buildSingleNodePath(n, single, insetPx)
                outPath.op(single, Path.Op.UNION)
            }
            return
        }

        val activeCount = min(nodes.size, SeaGlassSdfEngine.MAX_SDF_NODES)
        val targetD = -insetPx

        // Vertical T-junction fast path: when Node 0 is a wide horizontal track/bar and Node 1
        // is a magnifier loupe or popover rising vertically out of an interior X coordinate along
        // the track, union the full-width track capsule with the vertical sminCubic bridge between
        // the track anchor at (nB.centerX, nA.centerY) and nB so the track's full width is preserved!
        if (activeCount == 2) {
            val nA = nodes[0]
            val nB = nodes[1]
            if (nA.halfWidth > nB.halfWidth * 1.65f &&
                abs(nB.centerY - nA.centerY) > nA.halfHeight * 0.55f
            ) {
                buildSingleNodePath(nA, outPath, insetPx)
                val anchorX = nB.centerX.coerceIn(
                    nA.centerX - nA.halfWidth + nA.halfHeight,
                    nA.centerX + nA.halfWidth - nA.halfHeight
                )
                val anchorHalfW = min(nB.halfWidth * 0.82f, nA.halfHeight * 1.45f)
                val anchorNode = SeaGlassSdfEngine.SdfNode(
                    centerX = anchorX,
                    centerY = nA.centerY,
                    halfWidth = anchorHalfW,
                    halfHeight = nA.halfHeight,
                    uniformCornerRadius = nA.halfHeight,
                    cornerSmoothing = nA.cornerSmoothing,
                    tintColorArgb = nA.tintColorArgb,
                    thicknessPx = nA.thicknessPx
                )
                val verticalPair = listOf(anchorNode, nB)
                val midX = (anchorNode.centerX + nB.centerX) * 0.5f
                val midY = (anchorNode.centerY + nB.centerY) * 0.5f
                val dMid = evaluateCombinedDistance(midX, midY, verticalPair, smoothRadiusPx)
                val bridgePath = Path()
                if (dMid < targetD - 0.5f) {
                    val maxSpan = max(nB.halfWidth, abs(nB.centerY - anchorNode.centerY) * 0.5f + nB.halfHeight) * 1.45f + smoothRadiusPx
                    traceStarShapedLobeIsosurface(
                        centerX = midX,
                        centerY = midY,
                        maxSpan = maxSpan,
                        allNodes = verticalPair,
                        smoothRadiusPx = smoothRadiusPx,
                        targetD = targetD,
                        outPath = bridgePath
                    )
                } else {
                    buildSingleNodePath(nB, bridgePath, insetPx)
                }
                outPath.op(bridgePath, Path.Op.UNION)
                return
            }
        }

        // Check whether every adjacent pair in the chain [0 .. activeCount - 1] is connected across its spine
        var chainConnected = activeCount >= 2
        if (chainConnected) {
            for (pairIdx in 0 until activeCount - 1) {
                val nA = nodes[pairIdx]
                val nB = nodes[pairIdx + 1]
                val sX = if (nB.centerX >= nA.centerX) 1f else -1f
                val fA = max(0f, nA.halfWidth - nA.halfHeight)
                val fB = max(0f, nB.halfWidth - nB.halfHeight)
                val inAX = nA.centerX + sX * fA
                val inAY = nA.centerY
                val inBX = nB.centerX - sX * fB
                val inBY = nB.centerY
                val bDx = inBX - inAX
                val bDy = inBY - inAY
                val bDist = sqrt(bDx * bDx + bDy * bDy)
                if (bDist <= 0.5f) continue
                for (k in 1..11) {
                    val u = k / 12f
                    val sx = inAX + bDx * u
                    val sy = inAY + bDy * u
                    val dSpine = evaluateCombinedDistance(sx, sy, nodes, smoothRadiusPx)
                    if (dSpine >= targetD - 0.8f) {
                        chainConnected = false
                        break
                    }
                }
                if (!chainConnected) break
            }
        }

        if (chainConnected) {
            traceConnectedStadiumIsosurface(
                nodeA = nodes[0],
                nodeB = nodes[activeCount - 1],
                allNodes = nodes,
                smoothRadiusPx = smoothRadiusPx,
                targetD = targetD,
                outPath = outPath
            )
        } else {
            val lobePath = Path()
            for (node in nodes.take(activeCount)) {
                val centerD = evaluateCombinedDistance(node.centerX, node.centerY, nodes, smoothRadiusPx)
                if (centerD < targetD - 1.0f) {
                    traceStarShapedLobeIsosurface(
                        centerX = node.centerX,
                        centerY = node.centerY,
                        maxSpan = max(node.halfWidth, node.halfHeight) * 1.65f + smoothRadiusPx,
                        allNodes = nodes,
                        smoothRadiusPx = smoothRadiusPx,
                        targetD = targetD,
                        outPath = lobePath
                    )
                    outPath.op(lobePath, Path.Op.UNION)
                } else {
                    buildSingleNodePath(node, lobePath, insetPx)
                    outPath.op(lobePath, Path.Op.UNION)
                }
            }
        }
    }

    private fun traceConnectedStadiumIsosurface(
        nodeA: SeaGlassSdfEngine.SdfNode,
        nodeB: SeaGlassSdfEngine.SdfNode,
        allNodes: List<SeaGlassSdfEngine.SdfNode>,
        smoothRadiusPx: Float,
        targetD: Float,
        outPath: Path
    ) {
        val focusOffsetA = max(0f, nodeA.halfWidth - nodeA.halfHeight)
        val focusOffsetB = max(0f, nodeB.halfWidth - nodeB.halfHeight)
        val signX = if (nodeB.centerX >= nodeA.centerX) 1f else -1f

        val startCapX = nodeA.centerX - signX * focusOffsetA
        val startCapY = nodeA.centerY
        val endCapX = nodeB.centerX + signX * focusOffsetB
        val endCapY = nodeB.centerY

        val dx = endCapX - startCapX
        val dy = endCapY - startCapY
        val len = sqrt(dx * dx + dy * dy).coerceAtLeast(1e-3f)
        val tx = dx / len
        val ty = dy / len
        val nx = -ty
        val ny = tx
        val baseAngle = atan2(dy, dx)

        // Fractional spine positions of the facing inner cap centers where the liquid neck forms
        val uInnerA = ((focusOffsetA * 2f) / len).coerceIn(0f, 0.86f)
        val uInnerB = (1f - (focusOffsetB * 2f) / len).coerceIn((uInnerA + 0.05f).coerceAtMost(0.98f), 1f)

        val capSamples = 24
        val bridgeSamples = if (allNodes.size > 2) 64 else 44
        val totalSamples = capSamples * 2 + bridgeSamples * 2
        val px = FloatArray(totalSamples)
        val py = FloatArray(totalSamples)

        // Pack 70% of bridge rays into the [uInnerA..uInnerB] neck while explicitly anchoring u = 0f and u = 1f
        val uSamples = FloatArray(bridgeSamples) { i ->
            val t = i.toFloat() / (bridgeSamples - 1).toFloat()
            when {
                allNodes.size > 2 -> t
                uInnerA <= 0.02f && uInnerB >= 0.98f -> t
                t <= 0.22f -> (t / 0.22f) * uInnerA
                t <= 0.88f -> uInnerA + ((t - 0.22f) / 0.66f) * (uInnerB - uInnerA)
                else -> uInnerB + ((t - 0.88f) / 0.12f) * (1f - uInnerB)
            }
        }

        val maxSearchA = nodeA.halfHeight * 1.45f + smoothRadiusPx
        val maxSearchB = nodeB.halfHeight * 1.45f + smoothRadiusPx
        val maxSearchBridge = allNodes.maxOf { max(it.halfHeight, it.halfWidth * 0.6f) } * 1.45f + smoothRadiusPx

        var idx = 0

        // 1. Outer Cap A around (startCapX, startCapY)
        for (i in 0 until capSamples) {
            val frac = (i + 1).toFloat() / (capSamples + 1).toFloat()
            val angle = baseAngle + (PI * 0.5 + PI * frac).toFloat()
            val dirX = cos(angle)
            val dirY = sin(angle)
            val t = solveRayIsosurface(
                ox = startCapX,
                oy = startCapY,
                dirX = dirX,
                dirY = dirY,
                maxDist = maxSearchA,
                nodes = allNodes,
                smoothRadiusPx = smoothRadiusPx,
                targetD = targetD
            )
            px[idx] = startCapX + dirX * t
            py[idx] = startCapY + dirY * t
            idx++
        }

        // 2. Side 1 Bridge (from startCap -> endCap along -normal direction)
        for (i in 0 until bridgeSamples) {
            val u = uSamples[i]
            val ox = startCapX + dx * u
            val oy = startCapY + dy * u
            val dirX = -nx
            val dirY = -ny
            val t = solveRayIsosurface(
                ox = ox,
                oy = oy,
                dirX = dirX,
                dirY = dirY,
                maxDist = maxSearchBridge,
                nodes = allNodes,
                smoothRadiusPx = smoothRadiusPx,
                targetD = targetD
            )
            px[idx] = ox + dirX * t
            py[idx] = oy + dirY * t
            idx++
        }

        // 3. Outer Cap B around (endCapX, endCapY)
        for (i in 0 until capSamples) {
            val frac = (i + 1).toFloat() / (capSamples + 1).toFloat()
            val angle = baseAngle + (-PI * 0.5 + PI * frac).toFloat()
            val dirX = cos(angle)
            val dirY = sin(angle)
            val t = solveRayIsosurface(
                ox = endCapX,
                oy = endCapY,
                dirX = dirX,
                dirY = dirY,
                maxDist = maxSearchB,
                nodes = allNodes,
                smoothRadiusPx = smoothRadiusPx,
                targetD = targetD
            )
            px[idx] = endCapX + dirX * t
            py[idx] = endCapY + dirY * t
            idx++
        }

        // 4. Side 2 Bridge (from endCap -> startCap along +normal direction)
        for (i in 0 until bridgeSamples) {
            val u = uSamples[bridgeSamples - 1 - i]
            val ox = startCapX + dx * u
            val oy = startCapY + dy * u
            val dirX = nx
            val dirY = ny
            val t = solveRayIsosurface(
                ox = ox,
                oy = oy,
                dirX = dirX,
                dirY = dirY,
                maxDist = maxSearchBridge,
                nodes = allNodes,
                smoothRadiusPx = smoothRadiusPx,
                targetD = targetD
            )
            px[idx] = ox + dirX * t
            py[idx] = oy + dirY * t
            idx++
        }

        appendSmoothClosedCatmullRom(px, py, totalSamples, outPath)
    }

    private fun traceStarShapedLobeIsosurface(
        centerX: Float,
        centerY: Float,
        maxSpan: Float,
        allNodes: List<SeaGlassSdfEngine.SdfNode>,
        smoothRadiusPx: Float,
        targetD: Float,
        outPath: Path
    ) {
        outPath.reset()
        val samples = 64
        val px = FloatArray(samples)
        val py = FloatArray(samples)

        for (i in 0 until samples) {
            val angle = (2.0 * PI * i / samples).toFloat()
            val dirX = cos(angle)
            val dirY = sin(angle)
            val t = solveRayIsosurface(
                ox = centerX,
                oy = centerY,
                dirX = dirX,
                dirY = dirY,
                maxDist = maxSpan,
                nodes = allNodes,
                smoothRadiusPx = smoothRadiusPx,
                targetD = targetD
            )
            px[i] = centerX + dirX * t
            py[i] = centerY + dirY * t
        }

        appendSmoothClosedCatmullRom(px, py, samples, outPath)
    }

    private fun solveRayIsosurface(
        ox: Float,
        oy: Float,
        dirX: Float,
        dirY: Float,
        maxDist: Float,
        nodes: List<SeaGlassSdfEngine.SdfNode>,
        smoothRadiusPx: Float,
        targetD: Float
    ): Float {
        var lo = 0f
        var hi = maxDist
        val steps = 8
        val stepSize = maxDist / steps
        var prevT = 0f
        for (s in 1..steps) {
            val currT = s * stepSize
            val fCurr = evaluateCombinedDistance(
                ox + dirX * currT,
                oy + dirY * currT,
                nodes,
                smoothRadiusPx
            ) - targetD
            if (fCurr >= 0f) {
                lo = prevT
                hi = currT
                break
            }
            prevT = currT
        }

        for (iter in 0 until 8) {
            val mid = (lo + hi) * 0.5f
            val fMid = evaluateCombinedDistance(
                ox + dirX * mid,
                oy + dirY * mid,
                nodes,
                smoothRadiusPx
            ) - targetD
            if (fMid < 0f) {
                lo = mid
            } else {
                hi = mid
            }
        }

        val fLo = evaluateCombinedDistance(ox + dirX * lo, oy + dirY * lo, nodes, smoothRadiusPx) - targetD
        val fHi = evaluateCombinedDistance(ox + dirX * hi, oy + dirY * hi, nodes, smoothRadiusPx) - targetD
        val denom = (fHi - fLo)
        return if (abs(denom) > 1e-4f) {
            (lo - fLo * (hi - lo) / denom).coerceIn(lo, hi)
        } else {
            (lo + hi) * 0.5f
        }
    }

    private fun appendSmoothClosedCatmullRom(
        px: FloatArray,
        py: FloatArray,
        count: Int,
        outPath: Path
    ) {
        if (count < 3) return
        outPath.moveTo(px[0], py[0])
        for (i in 0 until count) {
            val p0 = (i - 1 + count) % count
            val p1 = i
            val p2 = (i + 1) % count
            val p3 = (i + 2) % count

            val dx01 = px[p1] - px[p0]
            val dy01 = py[p1] - py[p0]
            val d01 = sqrt(dx01 * dx01 + dy01 * dy01).coerceAtLeast(1e-3f)

            val dx12 = px[p2] - px[p1]
            val dy12 = py[p2] - py[p1]
            val d12 = sqrt(dx12 * dx12 + dy12 * dy12).coerceAtLeast(1e-3f)

            val dx23 = px[p3] - px[p2]
            val dy23 = py[p3] - py[p2]
            val d23 = sqrt(dx23 * dx23 + dy23 * dy23).coerceAtLeast(1e-3f)

            // Chord-length-weighted unit tangents scaled by current segment length d12 / 3f
            val t1x = ((dx12 / d12) * d01 + (dx01 / d01) * d12) / (d01 + d12)
            val t1y = ((dy12 / d12) * d01 + (dy01 / d01) * d12) / (d01 + d12)

            val t2x = ((dx23 / d23) * d12 + (dx12 / d12) * d23) / (d12 + d23)
            val t2y = ((dy23 / d23) * d12 + (dy12 / d12) * d23) / (d12 + d23)

            val segScale = d12 / 3f
            val c1x = px[p1] + t1x * segScale
            val c1y = py[p1] + t1y * segScale
            val c2x = px[p2] - t2x * segScale
            val c2y = py[p2] - t2y * segScale

            outPath.cubicTo(c1x, c1y, c2x, c2y, px[p2], py[p2])
        }
        outPath.close()
    }

    fun buildCoalescingPairPath(
        nodeA: SeaGlassSdfEngine.SdfNode,
        nodeB: SeaGlassSdfEngine.SdfNode,
        smoothRadiusPx: Float,
        outPath: Path,
        insetPx: Float = 0f
    ) {
        buildCoalescingScenePath(
            nodes = listOf(nodeA, nodeB),
            smoothRadiusPx = smoothRadiusPx,
            outPath = outPath,
            insetPx = insetPx
        )
    }

    /**
     * Builds a smooth circular/capsule sea-glass medallion contour with zero edge waviness.
     */
    fun buildOrganicPebblePath(
        widthPx: Float,
        heightPx: Float,
        outPath: Path,
        organicIrregularity: Float = 0.0f,
        phaseSeedRad: Float = 0.0f
    ) {
        outPath.reset()
        val w = widthPx.coerceAtLeast(2f)
        val h = heightPx.coerceAtLeast(2f)
        val r = min(w, h) * 0.5f
        scratchRect.set(0f, 0f, w, h)
        outPath.addRoundRect(scratchRect, r, r, Path.Direction.CW)
    }

    /**
     * Builds a smooth stadium capsule/pill contour with zero edge waviness.
     */
    fun buildTumbledCapsulePath(
        widthPx: Float,
        heightPx: Float,
        outPath: Path,
        organicWavePx: Float = 0.0f,
        phaseSeedRad: Float = 0.0f
    ) {
        outPath.reset()
        val w = widthPx.coerceAtLeast(2f)
        val h = heightPx.coerceAtLeast(2f)
        val r = min(w, h) * 0.5f
        scratchRect.set(0f, 0f, w, h)
        outPath.addRoundRect(scratchRect, r, r, Path.Direction.CW)
    }

    private fun evaluateCombinedDistance(
        px: Float,
        py: Float,
        nodes: List<SeaGlassSdfEngine.SdfNode>,
        smoothRadiusPx: Float
    ): Float {
        var d = SeaGlassSdfEngine.evaluateNodeSdf(px, py, nodes[0])
        val count = min(nodes.size, SeaGlassSdfEngine.MAX_SDF_NODES)
        for (i in 1 until count) {
            val di = SeaGlassSdfEngine.evaluateNodeSdf(px, py, nodes[i])
            d = SeaGlassSdfEngine.sminCubic(d, di, smoothRadiusPx)
        }
        return d
    }
}
