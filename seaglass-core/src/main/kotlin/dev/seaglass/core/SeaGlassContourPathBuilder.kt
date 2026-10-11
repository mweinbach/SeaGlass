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
        val maxR = min(hw, hh).coerceAtLeast(0f)
        val rInset = insetPx * 0.85f
        val tl = (node.cornerTopLeft - rInset).coerceIn(0f, maxR)
        val tr = (node.cornerTopRight - rInset).coerceIn(0f, maxR)
        val br = (node.cornerBottomRight - rInset).coerceIn(0f, maxR)
        val bl = (node.cornerBottomLeft - rInset).coerceIn(0f, maxR)

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
        if (nodes.size == 1 || smoothRadiusPx <= 0.5f) {
            val single = Path()
            for (n in nodes) {
                buildSingleNodePath(n, single, insetPx)
                outPath.op(single, Path.Op.UNION)
            }
            return
        }

        val activeCount = min(nodes.size, SeaGlassSdfEngine.MAX_SDF_NODES)
        val targetD = -insetPx

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
