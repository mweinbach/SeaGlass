/*
 * SeaGlass Backend-Agnostic C++20 & Direct3D 11 / Direct2D HLSL Uniform ABI Header
 *
 * Defines the byte-exact 256-byte `cbuffer` / `std140` / `std430` Uniform Buffer Object
 * (`SeaGlassUniformBlockStd140`) shared across:
 *   - Direct3D 11 / Direct2D HLSL (`cbuffer SeaGlassUniforms : register(b0)`)
 *   - Vulkan 1.1+ (`layout(set = 0, binding = 0, std140) uniform SeaGlassUniforms`)
 *   - OpenGL ES 3.0 (`layout(std140) uniform SeaGlassUniforms`)
 *   - Android AGSL (`RuntimeShader`)
 *   - C# `SeaGlass.Core.Backend.SeaGlassUniformBlock`
 *   - Kotlin `dev.seaglass.core.backend.SeaGlassUniformBlock`
 */
#ifndef SEAGLASS_WINDOWS_BACKEND_ABI_H
#define SEAGLASS_WINDOWS_BACKEND_ABI_H

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
 * 256-byte `cbuffer` / `std140` Uniform Buffer Block matching
 * `SeaGlass.Core.Backend.SeaGlassUniformBlock` and `dev.seaglass.core.backend.SeaGlassUniformBlock`
 * byte-for-byte. Can be uploaded directly via `ID3D11DeviceContext::UpdateSubresource`,
 * `ID2D1Effect::SetValue`, `vkCmdPushConstants`, or `glBufferSubData`.
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
              "SeaGlassUniformBlockStd140 must be exactly 256 bytes for HLSL cbuffer & Vulkan/GLES3 std140 parity");

inline float EvaluateRoundedBoxSdf(
    float px,
    float py,
    const Vec4f& bounds,
    const Vec4f& corners,
    float smoothingExp
) noexcept {
    (void)smoothingExp;
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

#endif  // SEAGLASS_WINDOWS_BACKEND_ABI_H
