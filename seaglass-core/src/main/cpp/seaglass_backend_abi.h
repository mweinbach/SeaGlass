/*
 * SeaGlass Backend-Agnostic C++20 Uniform ABI & Optical Reference Header
 *
 * Defines the byte-exact 256-byte `std140` / `std430` Uniform Buffer Object (`SeaGlassUniformBlockStd140`)
 * shared across:
 *   - Vulkan 1.1+ (`layout(set = 0, binding = 0, std140) uniform SeaGlassUniforms`)
 *   - OpenGL ES 3.0 (`layout(std140) uniform SeaGlassUniforms`)
 *   - Android AGSL (`RuntimeShader`)
 *   - Kotlin `dev.seaglass.core.backend.SeaGlassUniformBlock`
 */
#ifndef SEAGLASS_BACKEND_ABI_H
#define SEAGLASS_BACKEND_ABI_H

#include <algorithm>
#include <cmath>
#include <cstdint>

namespace seaglass {

struct alignas(16) Vec4f {
    float x;
    float y;
    float z;
    float w;
};

constexpr std::size_t kMaxSdfNodes = 4;
constexpr std::size_t kUniformBlockByteSize = 256;

/**
 * 256-byte `std140` Uniform Buffer Block matching `SeaGlassUniformBlock.writeToStd140ByteBuffer`
 * byte-for-byte. Can be uploaded directly via `vkCmdPushConstants`, `vkMapMemory` on a
 * `VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER`, or `glBufferSubData(GL_UNIFORM_BUFFER, ...)`.
 */
struct alignas(16) SeaGlassUniformBlockStd140 {
    Vec4f resolutionAndOrigin;          // [widthPx, heightPx, backdropOffsetX, backdropOffsetY]
    Vec4f opticsPrimary;                // [tintDensity, coarseness, ior, chromaticDispersion]
    Vec4f opticsSecondary;              // [rimChalkiness, thicknessPx, cornerSmoothing, smoothRadiusPx]
    Vec4f lightAndMeta;                 // [lightDirX, lightDirY, lightDirZ, activeNodeCount]
    Vec4f nodeBounds[kMaxSdfNodes];     // 4 x [centerX, centerY, halfWidth, halfHeight]
    Vec4f nodeCorners[kMaxSdfNodes];    // 4 x [rTopRight, rBottomRight, rBottomLeft, rTopLeft]
    Vec4f nodeTints[kMaxSdfNodes];      // 4 x [linearR, linearG, linearB, nodeThicknessPx]
};

static_assert(sizeof(SeaGlassUniformBlockStd140) == kUniformBlockByteSize,
              "SeaGlassUniformBlockStd140 must be exactly 256 bytes for std140 Vulkan/GLES3 parity");

inline float EvaluateRoundedBoxSdf(
    float px,
    float py,
    const Vec4f& bounds,
    const Vec4f& corners,
    float smoothingExp
) noexcept {
    const float dx = px - bounds.x;
    const float dy = py - bounds.y;
    const float rRight = (dy > 0.0f) ? corners.y : corners.x;
    const float rLeft  = (dy > 0.0f) ? corners.z : corners.w;
    float r = (dx > 0.0f) ? rRight : rLeft;
    const float maxR = std::max(0.0f, std::min(bounds.z, bounds.w));
    r = std::clamp(r, 0.0f, maxR);

    const float qx = std::abs(dx) - bounds.z + r;
    const float qy = std::abs(dy) - bounds.w + r;
    const float outX = std::max(qx, 0.0f);
    const float outY = std::max(qy, 0.0f);
    const float inside = std::min(std::max(qx, qy), 0.0f);
    return inside + std::sqrt(outX * outX + outY * outY) - r;
}

inline float SminCubic(float a, float b, float k) noexcept {
    if (k <= 0.0001f) return std::min(a, b);
    const float h = std::clamp(k - std::abs(a - b), 0.0f, k) / k;
    return std::min(a, b) - h * h * h * k * (1.0f / 6.0f);
}

}  // namespace seaglass

#endif  // SEAGLASS_BACKEND_ABI_H
