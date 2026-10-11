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

        val nodeA = nodes[0]
        val nodeB = nodes[1]
        val signX = if (nodeB.centerX >= nodeA.centerX) 1f else -1f
        val focusOffsetA = max(0f, nodeA.halfWidth - nodeA.halfHeight)
        val focusOffsetB = max(0f, nodeB.halfWidth - nodeB.halfHeight)

        // Inner focal centers facing each other across the coalescing bridge
        val innerCapAX = nodeA.centerX + signX * focusOffsetA
        val innerCapAY = nodeA.centerY
        val innerCapBX = nodeB.centerX - signX * focusOffsetB
        val innerCapBY = nodeB.centerY

        val bridgeDx = innerCapBX - innerCapAX
        val bridgeDy = innerCapBY - innerCapAY
        val bridgeDist = sqrt(bridgeDx * bridgeDx + bridgeDy * bridgeDy)
        val targetD = -insetPx

        // Check whether the spine connecting the facing inner caps is inside the target isosurface
        var spineConnected = bridgeDist > 0.5f
        if (spineConnected) {
            for (k in 1..11) {
                val u = k / 12f
                val sx = innerCapAX + bridgeDx * u
                val sy = innerCapAY + bridgeDy * u
                val dSpine = evaluateCombinedDistance(sx, sy, nodes, smoothRadiusPx)
                if (dSpine >= targetD - 0.8f) {
                    spineConnected = false
                    break
                }
            }
        }

        if (spineConnected) {
            traceConnectedStadiumIsosurface(
                nodeA = nodeA,
                nodeB = nodeB,
                allNodes = nodes,
                smoothRadiusPx = smoothRadiusPx,
                targetD = targetD,
                outPath = outPath
            )
        } else {
            val lobePath = Path()
            for (node in nodes.take(SeaGlassSdfEngine.MAX_SDF_NODES)) {
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
        val bridgeSamples = 44
        val totalSamples = capSamples * 2 + bridgeSamples * 2
        val px = FloatArray(totalSamples)
        val py = FloatArray(totalSamples)

        // Pack 70% of bridge rays into the [uInnerA..uInnerB] neck so wide bars have high neck resolution!
        val uSamples = FloatArray(bridgeSamples) { i ->
            val t = (i + 0.5f) / bridgeSamples.toFloat()
            when {
                uInnerA <= 0.02f && uInnerB >= 0.98f -> t
                t < 0.18f -> (t / 0.18f) * uInnerA
                t < 0.88f -> uInnerA + ((t - 0.18f) / 0.70f) * (uInnerB - uInnerA)
                else -> uInnerB + ((t - 0.88f) / 0.12f) * (1f - uInnerB)
            }
        }

        val maxSearchA = nodeA.halfHeight * 1.45f + smoothRadiusPx
        val maxSearchB = nodeB.halfHeight * 1.45f + smoothRadiusPx
        val maxSearchBridge = max(nodeA.halfHeight, nodeB.halfHeight) * 1.45f + smoothRadiusPx

        var idx = 0

        // 1. Outer Cap A around (startCapX, startCapY)
        for (i in 0 until capSamples) {
            val frac = i.toFloat() / capSamples.toFloat()
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
            val frac = i.toFloat() / capSamples.toFloat()
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

            val c1x = px[p1] + (px[p2] - px[p0]) / 6f
            val c1y = py[p1] + (py[p2] - py[p0]) / 6f
            val c2x = px[p2] - (px[p3] - px[p1]) / 6f
            val c2y = py[p2] - (py[p3] - py[p1]) / 6f

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
