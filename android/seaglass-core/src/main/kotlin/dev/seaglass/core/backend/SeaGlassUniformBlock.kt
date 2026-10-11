package dev.seaglass.core.backend

import dev.seaglass.core.SeaGlassColorMath
import dev.seaglass.core.SeaGlassMaterial
import dev.seaglass.core.SeaGlassSdfEngine
import java.nio.ByteBuffer
import java.nio.ByteOrder
import kotlin.math.min
import kotlin.math.sqrt

/**
 * Backend-agnostic 256-byte (`16 × vec4` = `64 × Float`) `std140` / `std430` Uniform Buffer Object (UBO)
 * layout shared across **Vulkan 1.1+ (SPIR-V)**, **OpenGL ES 3.0+**, **Android AGSL (`RuntimeShader`)**,
 * and **C++20 (`SeaGlassUniformBlockStd140`)**.
 *
 * Every field is packed into 16-byte (`vec4`) aligned slots so there is zero padding divergence
 * between `std140`, `std430`, and native C/C++ structs:
 *
 * | Vec4 Slot | Byte Offset | Float Indices | Contents (`x, y, z, w`) |
 * | :--- | :--- | :--- | :--- |
 * | `0` | `0..15` | `0..3` | `uResolutionAndOrigin`: `(widthPx, heightPx, backdropOffsetX, backdropOffsetY)` |
 * | `1` | `16..31` | `4..7` | `uOpticsPrimary`: `(tintDensity, coarseness, ior, chromaticDispersion)` |
 * | `2` | `32..47` | `8..11` | `uOpticsSecondary`: `(rimChalkiness, thicknessPx, cornerSmoothing, smoothRadiusPx)` |
 * | `3` | `48..63` | `12..15` | `uLightAndMeta`: `(lightDirX, lightDirY, lightDirZ, activeNodeCount)` |
 * | `4..7` | `64..127` | `16..31` | `uNodeBounds[0..3]`: `(centerX, centerY, halfWidth, halfHeight)` |
 * | `8..11` | `128..191` | `32..47` | `uNodeCorners[0..3]`: `(rTopRight, rBottomRight, rBottomLeft, rTopLeft)` |
 * | `12..15` | `192..255` | `48..63` | `uNodeTints[0..3]`: `(linearR, linearG, linearB, nodeThicknessPx)` |
 */
class SeaGlassUniformBlock {

    /**
     * Raw 64-float backing array (256 bytes). Mutated in-place to guarantee zero per-frame allocations.
     */
    val data: FloatArray = FloatArray(FLOAT_COUNT)

    /**
     * 16-bit bitmask (`bit k` = `vec4` slot `k` in `0..15`) indicating which `vec4` uniform slots
     * changed during the most recent [pack] call. Allows GPU backends (AGSL, GLES3, Vulkan) to
     * skip redundant JNI uniform uploads or UBO transfers when `dirtySlotsMask == 0`.
     */
    var dirtySlotsMask: Int = ALL_SLOTS_MASK
        private set

    /**
     * Monotonically increasing revision counter incremented only when at least one uniform slot changes.
     */
    var contentRevision: Long = 0L
        private set

    private var initialized: Boolean = false
    private val scratchRgb = FloatArray(3)

    private fun writeVec4Slot(slotIndex: Int, x: Float, y: Float, z: Float, w: Float): Int {
        val base = slotIndex shl 2
        if (initialized &&
            data[base] == x &&
            data[base + 1] == y &&
            data[base + 2] == z &&
            data[base + 3] == w
        ) {
            return 0
        }
        data[base] = x
        data[base + 1] = y
        data[base + 2] = z
        data[base + 3] = w
        return 1 shl slotIndex
    }

    /**
     * Marks all 16 `vec4` slots clean after a backend has synced them to the GPU.
     */
    fun clearDirtyMask() {
        dirtySlotsMask = 0
    }

    /**
     * Packs the current frame's optical material and SDF coalescing nodes into the `std140` layout,
     * updating [dirtySlotsMask] and [contentRevision] only for slots whose values changed.
     */
    fun pack(
        widthPx: Float,
        heightPx: Float,
        backdropOffsetX: Float,
        backdropOffsetY: Float,
        material: SeaGlassMaterial,
        density: Float,
        nodes: List<SeaGlassSdfEngine.SdfNode>,
        smoothRadiusPx: Float,
        lightDirX: Float = DEFAULT_LIGHT_X,
        lightDirY: Float = DEFAULT_LIGHT_Y,
        lightDirZ: Float = DEFAULT_LIGHT_Z
    ) {
        val d = density.coerceAtLeast(0.5f)
        val thicknessPx = material.thicknessDp * d

        // Normalize key light direction
        val lLen = sqrt(lightDirX * lightDirX + lightDirY * lightDirY + lightDirZ * lightDirZ)
            .coerceAtLeast(1e-4f)

        var mask = 0

        // Slot 0: uResolutionAndOrigin
        mask = mask or writeVec4Slot(
            0,
            widthPx.coerceAtLeast(1f),
            heightPx.coerceAtLeast(1f),
            backdropOffsetX,
            backdropOffsetY
        )

        // Slot 1: uOpticsPrimary
        mask = mask or writeVec4Slot(
            1,
            material.tintDensity,
            material.coarseness,
            material.ior,
            material.chromaticDispersion
        )

        // Slot 2: uOpticsSecondary
        mask = mask or writeVec4Slot(
            2,
            material.rimChalkiness,
            thicknessPx,
            material.cornerSmoothing,
            smoothRadiusPx.coerceAtLeast(0f)
        )

        // Slot 3: uLightAndMeta
        val activeCount = min(nodes.size, SeaGlassSdfEngine.MAX_SDF_NODES).coerceAtLeast(1)
        mask = mask or writeVec4Slot(
            3,
            lightDirX / lLen,
            lightDirY / lLen,
            lightDirZ / lLen,
            activeCount.toFloat()
        )

        // Slots 4..15: Node arrays (up to 4 nodes)
        for (i in 0 until SeaGlassSdfEngine.MAX_SDF_NODES) {
            val boundsSlot = 4 + i
            val cornersSlot = 8 + i
            val tintsSlot = 12 + i

            if (i < nodes.size) {
                val node = nodes[i]
                mask = mask or writeVec4Slot(
                    boundsSlot,
                    node.centerX,
                    node.centerY,
                    node.halfWidth,
                    node.halfHeight
                )
                mask = mask or writeVec4Slot(
                    cornersSlot,
                    node.cornerTopRight,
                    node.cornerBottomRight,
                    node.cornerBottomLeft,
                    node.cornerTopLeft
                )
                SeaGlassColorMath.argbToLinearRgbFloats(node.tintColorArgb, scratchRgb, 0)
                mask = mask or writeVec4Slot(
                    tintsSlot,
                    scratchRgb[0],
                    scratchRgb[1],
                    scratchRgb[2],
                    node.thicknessPx
                )
            } else if (i == 0) {
                // Default single node covering the full viewport if empty list passed
                val cornerPx = material.cornerRadiusDp * d
                mask = mask or writeVec4Slot(
                    boundsSlot,
                    widthPx * 0.5f,
                    heightPx * 0.5f,
                    widthPx * 0.5f,
                    heightPx * 0.5f
                )
                mask = mask or writeVec4Slot(
                    cornersSlot,
                    cornerPx,
                    cornerPx,
                    cornerPx,
                    cornerPx
                )
                SeaGlassColorMath.argbToLinearRgbFloats(material.tintColorArgb, scratchRgb, 0)
                mask = mask or writeVec4Slot(
                    tintsSlot,
                    scratchRgb[0],
                    scratchRgb[1],
                    scratchRgb[2],
                    thicknessPx
                )
            } else {
                // Inactive slot pushed far outside the field
                mask = mask or writeVec4Slot(boundsSlot, -1e5f, -1e5f, 0f, 0f)
                mask = mask or writeVec4Slot(cornersSlot, 0f, 0f, 0f, 0f)
                mask = mask or writeVec4Slot(tintsSlot, 1f, 1f, 1f, thicknessPx)
            }
        }

        initialized = true
        dirtySlotsMask = dirtySlotsMask or mask
        if (mask != 0) {
            contentRevision++
        }
    }

    /**
     * Writes the 256-byte `std140` UBO payload into a little-endian [ByteBuffer] for
     * Vulkan (`vkMapMemory` / push constants) or OpenGL ES 3.0 (`glBufferSubData`).
     */
    fun writeToStd140ByteBuffer(target: ByteBuffer) {
        require(target.remaining() >= BYTE_SIZE) {
            "Target ByteBuffer requires at least $BYTE_SIZE bytes remaining, had ${target.remaining()}"
        }
        target.order(ByteOrder.nativeOrder())
        val startPos = target.position()
        target.asFloatBuffer().put(data, 0, FLOAT_COUNT)
        target.position(startPos + BYTE_SIZE)
    }

    /**
     * Allocates a direct native-ordered [ByteBuffer] sized to [BYTE_SIZE] (256 bytes)
     * suitable for persistent Vulkan / OpenGL ES UBO staging.
     */
    fun createDirectStagingBuffer(): ByteBuffer {
        val buffer = ByteBuffer.allocateDirect(BYTE_SIZE).order(ByteOrder.nativeOrder())
        writeToStd140ByteBuffer(buffer)
        buffer.flip()
        return buffer
    }

    companion object {
        const val VEC4_COUNT = 16
        const val FLOAT_COUNT = VEC4_COUNT * 4 // 64 floats
        const val BYTE_SIZE = FLOAT_COUNT * 4   // 256 bytes
        const val ALL_SLOTS_MASK = 0xFFFF

        const val OFFSET_RESOLUTION_AND_ORIGIN = 0
        const val OFFSET_OPTICS_PRIMARY = 4
        const val OFFSET_OPTICS_SECONDARY = 8
        const val OFFSET_LIGHT_AND_META = 12
        const val OFFSET_NODE_BOUNDS = 16
        const val OFFSET_NODE_CORNERS = 32
        const val OFFSET_NODE_TINTS = 48

        private const val DEFAULT_LIGHT_X = -0.52f
        private const val DEFAULT_LIGHT_Y = -0.70f
        private const val DEFAULT_LIGHT_Z = 0.48f
    }
}
