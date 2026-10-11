package dev.seaglass.compose

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.BoxScope
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.Stable
import androidx.compose.runtime.mutableStateMapOf
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.layout.onGloballyPositioned
import androidx.compose.ui.layout.positionInRoot
import dev.seaglass.core.SeaGlassMaterial
import dev.seaglass.core.SeaGlassSdfEngine

/**
 * Coordinates multiple sibling SeaGlass shapes inside a shared 2D Signed Distance Field (SDF)
 * so neighboring elements smoothly coalesce (`sminCubic`) and split like liquid sea-glass droplets.
 */
@Stable
class SeaGlassFluidGroupState internal constructor(
    val stageState: SeaGlassStageState?,
    val groupMaterial: SeaGlassMaterial,
    val smoothRadiusDp: Float
) {
    internal var groupOriginInRoot: Offset = Offset.Zero
    internal val registeredNodes = mutableStateMapOf<String, SeaGlassSdfEngine.SdfNode>()

    fun updateNode(key: String, node: SeaGlassSdfEngine.SdfNode) {
        registeredNodes[key] = node
    }

    fun removeNode(key: String) {
        registeredNodes.remove(key)
    }

    fun snapshotNodes(): List<SeaGlassSdfEngine.SdfNode> {
        return registeredNodes.entries
            .sortedBy { it.key }
            .map { it.value }
            .take(SeaGlassSdfEngine.MAX_SDF_NODES)
    }
}

/**
 * Scope inside a [SeaGlassFluidGroup] that allows child elements to register as coalescing
 * SDF droplets via [Modifier.seaGlassFluidNode].
 */
class SeaGlassFluidGroupScope internal constructor(
    val groupState: SeaGlassFluidGroupState,
    private val boxScope: BoxScope
) : BoxScope by boxScope {

    /**
     * Registers this child composable's bounds and optical material into the enclosing
     * [SeaGlassFluidGroup] so it smoothly coalesces (`smin`) with neighboring nodes.
     */
    @Composable
    fun Modifier.seaGlassFluidNode(
        nodeKey: String,
        material: SeaGlassMaterial = groupState.groupMaterial,
        cornerRadiusOverridePx: Float? = null
    ): Modifier {
        DisposableEffect(nodeKey) {
            onDispose {
                groupState.removeNode(nodeKey)
            }
        }

        return this.onGloballyPositioned { coordinates ->
            val posInRoot = coordinates.positionInRoot()
            val localTopLeft = posInRoot - groupState.groupOriginInRoot
            val w = coordinates.size.width.toFloat()
            val h = coordinates.size.height.toFloat()
            if (w > 1f && h > 1f) {
                val halfW = w * 0.5f
                val halfH = h * 0.5f
                val radiusPx = cornerRadiusOverridePx
                    ?: kotlin.math.min(halfW, halfH)
                groupState.updateNode(
                    key = nodeKey,
                    node = SeaGlassSdfEngine.SdfNode(
                        centerX = localTopLeft.x + halfW,
                        centerY = localTopLeft.y + halfH,
                        halfWidth = halfW,
                        halfHeight = halfH,
                        uniformCornerRadius = radiusPx,
                        cornerSmoothing = material.cornerSmoothing,
                        tintColorArgb = material.tintColorArgb,
                        thicknessPx = material.thicknessDp * 2.5f
                    )
                )
            }
        }
    }
}

/**
 * Container that renders a unified multi-node coalescing SeaGlass SDF field (`sminCubic`)
 * for all children registered with [SeaGlassFluidGroupScope.seaGlassFluidNode].
 */
@Composable
fun SeaGlassFluidGroup(
    stageState: SeaGlassStageState?,
    material: SeaGlassMaterial = LocalSeaGlassPalette.current.primaryGlass,
    smoothRadiusDp: Float = 26f,
    modifier: Modifier = Modifier,
    content: @Composable SeaGlassFluidGroupScope.() -> Unit
) {
    val groupState = remember(stageState, material, smoothRadiusDp) {
        SeaGlassFluidGroupState(
            stageState = stageState,
            groupMaterial = material,
            smoothRadiusDp = smoothRadiusDp
        )
    }

    val nodesSnapshot = groupState.snapshotNodes()

    Box(
        modifier = modifier
            .onGloballyPositioned { coordinates ->
                groupState.groupOriginInRoot = coordinates.positionInRoot()
            }
            .then(
                if (nodesSnapshot.isNotEmpty()) {
                    Modifier.seaGlass(
                        stageState = stageState,
                        material = material,
                        nodesOptional = nodesSnapshot,
                        smoothRadiusDp = smoothRadiusDp
                    )
                } else {
                    Modifier
                }
            )
    ) {
        val scope = remember(groupState, this) {
            SeaGlassFluidGroupScope(groupState = groupState, boxScope = this)
        }
        scope.content()
    }
}
