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
     * Packs the current frame's optical material and SDF coalescing nodes into the `std140` layout.
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

        // Slot 0: uResolutionAndOrigin
        data[0] = widthPx.coerceAtLeast(1f)
        data[1] = heightPx.coerceAtLeast(1f)
        data[2] = backdropOffsetX
        data[3] = backdropOffsetY

        // Slot 1: uOpticsPrimary
        data[4] = material.tintDensity
        data[5] = material.coarseness
        data[6] = material.ior
        data[7] = material.chromaticDispersion

        // Slot 2: uOpticsSecondary
        data[8] = material.rimChalkiness
        data[9] = thicknessPx
        data[10] = material.cornerSmoothing
        data[11] = smoothRadiusPx.coerceAtLeast(0f)

        // Slot 3: uLightAndMeta
        val activeCount = min(nodes.size, SeaGlassSdfEngine.MAX_SDF_NODES).coerceAtLeast(1)
        data[12] = lightDirX / lLen
        data[13] = lightDirY / lLen
        data[14] = lightDirZ / lLen
        data[15] = activeCount.toFloat()

        // Slots 4..15: Node arrays (up to 4 nodes)
        for (i in 0 until SeaGlassSdfEngine.MAX_SDF_NODES) {
            val boundsOffset = OFFSET_NODE_BOUNDS + i * 4
            val cornersOffset = OFFSET_NODE_CORNERS + i * 4
            val tintsOffset = OFFSET_NODE_TINTS + i * 4

            if (i < nodes.size) {
                val node = nodes[i]
                data[boundsOffset] = node.centerX
                data[boundsOffset + 1] = node.centerY
                data[boundsOffset + 2] = node.halfWidth
                data[boundsOffset + 3] = node.halfHeight

                data[cornersOffset] = node.cornerTopRight
                data[cornersOffset + 1] = node.cornerBottomRight
                data[cornersOffset + 2] = node.cornerBottomLeft
                data[cornersOffset + 3] = node.cornerTopLeft

                SeaGlassColorMath.argbToLinearRgbFloats(node.tintColorArgb, data, tintsOffset)
                data[tintsOffset + 3] = node.thicknessPx
            } else if (i == 0) {
                // Default single node covering the full viewport if empty list passed
                val cornerPx = material.cornerRadiusDp * d
                data[boundsOffset] = widthPx * 0.5f
                data[boundsOffset + 1] = heightPx * 0.5f
                data[boundsOffset + 2] = widthPx * 0.5f
                data[boundsOffset + 3] = heightPx * 0.5f

                data[cornersOffset] = cornerPx
                data[cornersOffset + 1] = cornerPx
                data[cornersOffset + 2] = cornerPx
                data[cornersOffset + 3] = cornerPx

                SeaGlassColorMath.argbToLinearRgbFloats(material.tintColorArgb, data, tintsOffset)
                data[tintsOffset + 3] = thicknessPx
            } else {
                // Inactive slot pushed far outside the field
                data[boundsOffset] = -1e5f
                data[boundsOffset + 1] = -1e5f
                data[boundsOffset + 2] = 0f
                data[boundsOffset + 3] = 0f

                data[cornersOffset] = 0f
                data[cornersOffset + 1] = 0f
                data[cornersOffset + 2] = 0f
                data[cornersOffset + 3] = 0f

                data[tintsOffset] = 1f
                data[tintsOffset + 1] = 1f
                data[tintsOffset + 2] = 1f
                data[tintsOffset + 3] = thicknessPx
            }
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
