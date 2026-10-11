package dev.seaglass.core

import android.graphics.Path
import android.graphics.RectF
import kotlin.math.atan2
import kotlin.math.cos
import kotlin.math.max
import kotlin.math.min
import kotlin.math.sin
import kotlin.math.sqrt

/**
 * Generates analytical contours (`android.graphics.Path`) for single or coalescing
 * [SeaGlassSdfEngine.SdfNode] shapes when rendering on Hybrid (API 31–32) or Canvas Fallback tiers.
 */
object SeaGlassContourPathBuilder {

    private val scratchRect = RectF()
    private val scratchRadii = FloatArray(8)

    /**
     * Populates [outPath] with the contour for a single [SeaGlassSdfEngine.SdfNode].
     */
    fun buildSingleNodePath(node: SeaGlassSdfEngine.SdfNode, outPath: Path) {
        outPath.reset()
        scratchRect.set(
            node.centerX - node.halfWidth,
            node.centerY - node.halfHeight,
            node.centerX + node.halfWidth,
            node.centerY + node.halfHeight
        )
        val maxR = min(node.halfWidth, node.halfHeight).coerceAtLeast(0f)
        val tl = node.cornerTopLeft.coerceIn(0f, maxR)
        val tr = node.cornerTopRight.coerceIn(0f, maxR)
        val br = node.cornerBottomRight.coerceIn(0f, maxR)
        val bl = node.cornerBottomLeft.coerceIn(0f, maxR)

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

    /**
     * Populates [outPath] with a smooth coalescing contour bridging [nodeA] and [nodeB]
     * when their boundaries are within [smoothRadiusPx] of each other.
     */
    fun buildCoalescingPairPath(
        nodeA: SeaGlassSdfEngine.SdfNode,
        nodeB: SeaGlassSdfEngine.SdfNode,
        smoothRadiusPx: Float,
        outPath: Path
    ) {
        outPath.reset()
        val tempPathB = Path()
        buildSingleNodePath(nodeA, outPath)
        buildSingleNodePath(nodeB, tempPathB)

        val dx = nodeB.centerX - nodeA.centerX
        val dy = nodeB.centerY - nodeA.centerY
        val centerDist = sqrt(dx * dx + dy * dy)
        val rA = min(nodeA.halfWidth, nodeA.halfHeight)
        val rB = min(nodeB.halfWidth, nodeB.halfHeight)
        val gap = centerDist - (rA + rB)

        // When the two shapes are close enough for smin to form a liquid bridge, add a metaball neck
        if (smoothRadiusPx > 1f && gap < smoothRadiusPx * 1.35f && centerDist > 1f) {
            val bridgeFactor = (1f - (gap / (smoothRadiusPx * 1.35f))).coerceIn(0f, 1f)
            val neckHalfThickness = min(rA, rB) * 0.72f * bridgeFactor * bridgeFactor
            if (neckHalfThickness > 1.5f) {
                val angle = atan2(dy, dx)
                val nx = -sin(angle)
                val ny = cos(angle)

                val pA1x = nodeA.centerX + nx * rA * 0.68f
                val pA1y = nodeA.centerY + ny * rA * 0.68f
                val pA2x = nodeA.centerX - nx * rA * 0.68f
                val pA2y = nodeA.centerY - ny * rA * 0.68f

                val pB1x = nodeB.centerX + nx * rB * 0.68f
                val pB1y = nodeB.centerY + ny * rB * 0.68f
                val pB2x = nodeB.centerX - nx * rB * 0.68f
                val pB2y = nodeB.centerY - ny * rB * 0.68f

                val midX = (nodeA.centerX + nodeB.centerX) * 0.5f
                val midY = (nodeA.centerY + nodeB.centerY) * 0.5f
                val ctrl1x = midX + nx * neckHalfThickness
                val ctrl1y = midY + ny * neckHalfThickness
                val ctrl2x = midX - nx * neckHalfThickness
                val ctrl2y = midY - ny * neckHalfThickness

                val bridgePath = Path().apply {
                    moveTo(pA1x, pA1y)
                    quadTo(ctrl1x, ctrl1y, pB1x, pB1y)
                    lineTo(pB2x, pB2y)
                    quadTo(ctrl2x, ctrl2y, pA2x, pA2y)
                    close()
                }
                outPath.op(bridgePath, Path.Op.UNION)
            }
        }

        outPath.op(tempPathB, Path.Op.UNION)
    }
}
