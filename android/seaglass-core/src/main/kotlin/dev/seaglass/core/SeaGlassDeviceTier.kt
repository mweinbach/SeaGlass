package dev.seaglass.core

import android.content.Context
import android.content.pm.PackageManager
import android.os.Build
import android.os.PowerManager
import dev.seaglass.core.backend.BackendType
import dev.seaglass.core.backend.SeaGlassShaderCompiler
import dev.seaglass.core.backend.ShaderDialect

/**
 * Runtime device tier and backend capability detector for SeaGlass.
 *
 * Inspects Android API level, Vulkan 1.1+ hardware feature support, OpenGL ES 3.0 support,
 * and power-save / accessibility flags to select the optimal rendering tier while allowing
 * host apps to explicitly target Vulkan, AGSL, OpenGL ES 3.0, or Canvas fallback.
 */
enum class SeaGlassDeviceTier(
    val id: String,
    val displayName: String,
    val defaultBackendType: BackendType,
    val supportsRealtimeRefraction: Boolean,
    val supportsHardwareBlur: Boolean
) {
    /**
     * Android 13+ (API 33–37+) with hardware `RenderNode` + `RenderEffect` + AGSL `RuntimeShader`
     * (and Vulkan 1.1+ HWUI / offscreen pipeline support).
     */
    AGSL_FULL(
        id = "agsl_full",
        displayName = "Tier 1 · AGSL RuntimeShader + RenderNode (API 33+)",
        defaultBackendType = BackendType.AGSL_RENDER_NODE,
        supportsRealtimeRefraction = true,
        supportsHardwareBlur = true
    ),

    /**
     * Explicit Vulkan 1.1+ SPIR-V + `std140` UBO pipeline for Vulkan surfaces or custom engines.
     */
    VULKAN_NATIVE(
        id = "vulkan_native",
        displayName = "Tier 1V · Vulkan 1.1+ SPIR-V + std140 UBO",
        defaultBackendType = BackendType.VULKAN_1_1,
        supportsRealtimeRefraction = true,
        supportsHardwareBlur = true
    ),

    /**
     * Android 12–12L (API 31–32) with hardware `RenderEffect.createBlurEffect` + analytical SDF
     * contour clip + Oklab Beer-Lambert body tint & bevel lighting.
     */
    RENDER_EFFECT_HYBRID(
        id = "render_effect_hybrid",
        displayName = "Tier 2 · Hardware RenderEffect + SDF Bevel (API 31–32)",
        defaultBackendType = BackendType.HYBRID_RENDER_EFFECT,
        supportsRealtimeRefraction = false,
        supportsHardwareBlur = true
    ),

    /**
     * Android 8.0–11 (API 26–30) or explicit OpenGL ES 3.0 `std140` UBO + Dual-Kawase pipeline.
     */
    GLES3_NATIVE(
        id = "gles3_native",
        displayName = "Tier 3 · OpenGL ES 3.0 std140 UBO + Dual-Kawase (API 26–30)",
        defaultBackendType = BackendType.OPENGLES_3_0,
        supportsRealtimeRefraction = true,
        supportsHardwareBlur = true
    ),

    /**
     * Lightweight Oklab frosted sea-glass surface with analytical SDF smooth-union contours,
     * procedural sand-grain texture, and chalky bevel rim (no backdrop readback).
     */
    LIGHTWEIGHT_TINT(
        id = "lightweight_tint",
        displayName = "Tier 4 · Lightweight Oklab Frost + SDF Rim",
        defaultBackendType = BackendType.CANVAS_FALLBACK,
        supportsRealtimeRefraction = false,
        supportsHardwareBlur = false
    );

    companion object {
        /**
         * Detects the optimal [SeaGlassDeviceTier] for the running Android device.
         */
        fun detect(
            context: Context? = null,
            preferVulkan: Boolean = false,
            respectPowerSaveMode: Boolean = false,
            sdkInt: Int = Build.VERSION.SDK_INT
        ): SeaGlassDeviceTier {
            if (respectPowerSaveMode && context != null) {
                val powerManager = context.getSystemService(Context.POWER_SERVICE) as? PowerManager
                if (powerManager?.isPowerSaveMode == true) {
                    return LIGHTWEIGHT_TINT
                }
            }

            if (preferVulkan && supportsVulkan11(context, sdkInt)) {
                return VULKAN_NATIVE
            }

            return when {
                sdkInt >= Build.VERSION_CODES.TIRAMISU -> AGSL_FULL
                sdkInt >= Build.VERSION_CODES.S -> RENDER_EFFECT_HYBRID
                sdkInt >= Build.VERSION_CODES.O -> GLES3_NATIVE
                else -> LIGHTWEIGHT_TINT
            }
        }

        /**
         * Returns true if the device supports Vulkan 1.1+ (`FEATURE_VULKAN_HARDWARE_VERSION >= 0x00401000`).
         */
        fun supportsVulkan11(context: Context?, sdkInt: Int = Build.VERSION.SDK_INT): Boolean {
            if (sdkInt < Build.VERSION_CODES.N) return false
            val pm = context?.packageManager ?: return sdkInt >= Build.VERSION_CODES.Q
            return pm.hasSystemFeature(PackageManager.FEATURE_VULKAN_HARDWARE_VERSION, 0x00401000)
        }
    }
}

/**
 * Snapshot of active GPU capabilities and shader dialect readiness for diagnostics & telemetry UI.
 */
data class SeaGlassTelemetry(
    val activeTier: SeaGlassDeviceTier,
    val activeBackend: BackendType,
    val sdkInt: Int,
    val deviceModel: String,
    val vulkan11Supported: Boolean,
    val uboByteSize: Int,
    val supportedDialects: List<ShaderDialect>
) {
    companion object {
        fun capture(
            context: Context? = null,
            overrideTier: SeaGlassDeviceTier? = null
        ): SeaGlassTelemetry {
            val tier = overrideTier ?: SeaGlassDeviceTier.detect(context)
            return SeaGlassTelemetry(
                activeTier = tier,
                activeBackend = tier.defaultBackendType,
                sdkInt = Build.VERSION.SDK_INT,
                deviceModel = "${Build.MANUFACTURER} ${Build.MODEL}".trim(),
                vulkan11Supported = SeaGlassDeviceTier.supportsVulkan11(context),
                uboByteSize = dev.seaglass.core.backend.SeaGlassUniformBlock.BYTE_SIZE,
                supportedDialects = ShaderDialect.entries
            )
        }
    }
}
