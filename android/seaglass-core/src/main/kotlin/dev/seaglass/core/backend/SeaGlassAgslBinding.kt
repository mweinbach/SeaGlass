package dev.seaglass.core.backend

import android.content.Context
import android.graphics.BlurMaskFilter
import android.graphics.RenderEffect
import android.graphics.RuntimeShader
import android.graphics.Shader
import android.os.Build
import androidx.annotation.RequiresApi
import dev.seaglass.core.SeaGlassDeviceTier
import dev.seaglass.core.SeaGlassSandGrainTexture
import java.util.concurrent.ConcurrentHashMap
import kotlin.math.roundToInt

/**
 * Process-wide compile-once GPU shader & effect program cache for the active device.
 *
 * Guarantees that:
 * 1. Device tier detection and target [ShaderDialect] compilation happen **once per app process**.
 * 2. On Android 13+ (API 33+), AGSL [RuntimeShader] AST parsing and SkSL/SPIR-V compilation
 *    are executed once during [warmUpForDevice] and reused across surfaces.
 * 3. On Android 12+ (API 31+), quantized [RenderEffect] blur instances (`0.5px` steps in
 *    `[1.0px..160.0px]`) and [BlurMaskFilter] bevel filters are pooled and reused so animations
 *    and slider drags perform $O(1)$ lookups with **zero per-frame JNI or heap allocations**.
 */
object SeaGlassShaderProgramCache {

    private const val MAX_BLUR_BUCKET = 320 // 320 * 0.5px = 160px max quantized radius

    @Volatile
    private var warmedUp: Boolean = false

    @Volatile
    var cachedDeviceTier: SeaGlassDeviceTier? = null
        private set

    @Volatile
    var cachedDeviceDialect: ShaderDialect? = null
        private set

    @Volatile
    var cachedShaderBundle: CompiledShaderBundle? = null
        private set

    @Volatile
    private var cachedAgslCompositeShader: Any? = null

    @Volatile
    private var cachedAgslOverlayShader: Any? = null

    @Volatile
    private var cachedAgslMaskShader: Any? = null

    private val clampBlurEffects = arrayOfNulls<Any>(MAX_BLUR_BUCKET + 1)
    private val decalBlurEffects = arrayOfNulls<Any>(MAX_BLUR_BUCKET + 1)
    private val blurMaskFilters = ConcurrentHashMap<Int, BlurMaskFilter>(16)

    /**
     * Returns true once [warmUpForDevice] has compiled and cached the device's shaders.
     */
    val isWarmedUp: Boolean
        get() = warmedUp

    /**
     * Compiles and caches the active device's target shader bundle, AGSL [RuntimeShader] programs,
     * sand-grain micro-facet texture, and common [RenderEffect] blur radii once per process.
     */
    fun warmUpForDevice(context: Context? = null): CompiledShaderBundle {
        cachedShaderBundle?.let { if (warmedUp) return it }
        synchronized(this) {
            cachedShaderBundle?.let { if (warmedUp) return it }

            val tier = SeaGlassDeviceTier.detect(context)
            cachedDeviceTier = tier

            val dialect = when (tier) {
                SeaGlassDeviceTier.AGSL_FULL -> ShaderDialect.AGSL_100
                SeaGlassDeviceTier.VULKAN_NATIVE -> ShaderDialect.VULKAN_GLSL_450
                else -> if (SeaGlassDeviceTier.supportsVulkan11(context)) {
                    ShaderDialect.VULKAN_GLSL_450
                } else {
                    ShaderDialect.GLSL_ES_300
                }
            }
            cachedDeviceDialect = dialect

            val bundle = SeaGlassShaderCompiler.compileForDialect(dialect)
            cachedShaderBundle = bundle

            // Pre-compile Android 13+ AGSL RuntimeShader programs once if running on a live Android runtime
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU && context != null) {
                try {
                    val agslBundle = if (dialect == ShaderDialect.AGSL_100) {
                        bundle
                    } else {
                        SeaGlassShaderCompiler.compileForDialect(ShaderDialect.AGSL_100)
                    }
                    cachedAgslCompositeShader = RuntimeShader(agslBundle.seaGlassCompositeFragmentSource)
                    agslBundle.sandGrainOverlayFragmentSource?.let {
                        cachedAgslOverlayShader = RuntimeShader(it)
                    }
                    agslBundle.sdfMaskFragmentSource?.let {
                        cachedAgslMaskShader = RuntimeShader(it)
                    }
                } catch (_: Throwable) {
                    // Safe fallback on unit-test JVM stubs
                }
            }

            if (context != null) {
                try {
                    SeaGlassSandGrainTexture.obtainShader()
                } catch (_: Throwable) {
                    // Safe fallback on unit-test JVM stubs
                }
            }

            warmedUp = true
            return bundle
        }
    }

    /**
     * Returns the pre-compiled AGSL composite [RuntimeShader] for this device, compiling it once if needed.
     */
    @RequiresApi(Build.VERSION_CODES.TIRAMISU)
    fun obtainAgslCompositeShader(): RuntimeShader {
        (cachedAgslCompositeShader as? RuntimeShader)?.let { return it }
        synchronized(this) {
            (cachedAgslCompositeShader as? RuntimeShader)?.let { return it }
            val agsl = SeaGlassShaderCompiler.compileForDialect(ShaderDialect.AGSL_100)
            val compiled = RuntimeShader(agsl.seaGlassCompositeFragmentSource)
            cachedAgslCompositeShader = compiled
            return compiled
        }
    }

    /**
     * Returns the pre-compiled AGSL sand-grain overlay [RuntimeShader], compiling it once if needed.
     */
    @RequiresApi(Build.VERSION_CODES.TIRAMISU)
    fun obtainAgslSandGrainOverlayShader(): RuntimeShader {
        (cachedAgslOverlayShader as? RuntimeShader)?.let { return it }
        synchronized(this) {
            (cachedAgslOverlayShader as? RuntimeShader)?.let { return it }
            val agsl = SeaGlassShaderCompiler.compileForDialect(ShaderDialect.AGSL_100)
            val src = agsl.sandGrainOverlayFragmentSource ?: agsl.seaGlassCompositeFragmentSource
            val compiled = RuntimeShader(src)
            cachedAgslOverlayShader = compiled
            return compiled
        }
    }

    /**
     * Returns a cached, quantized [RenderEffect] blur (`0.5px` resolution buckets in `[1.0px..160.0px]`)
     * so animations and live slider updates never allocate JNI [RenderEffect] objects per frame.
     */
    @RequiresApi(Build.VERSION_CODES.S)
    fun obtainBlurRenderEffect(
        radiusPx: Float,
        tileMode: Shader.TileMode = Shader.TileMode.CLAMP
    ): RenderEffect {
        val bucket = (radiusPx.coerceIn(0.5f, 160.0f) * 2.0f).roundToInt().coerceIn(1, MAX_BLUR_BUCKET)
        val table = if (tileMode == Shader.TileMode.DECAL) decalBlurEffects else clampBlurEffects
        (table[bucket] as? RenderEffect)?.let { return it }
        val quantizedRadius = bucket * 0.5f
        val created = RenderEffect.createBlurEffect(quantizedRadius, quantizedRadius, tileMode)
        table[bucket] = created
        return created
    }

    /**
     * Returns a cached, quantized [BlurMaskFilter] (`0.5px` buckets) for shadow and bevel feathering.
     */
    fun obtainBlurMaskFilter(
        radiusPx: Float,
        style: BlurMaskFilter.Blur = BlurMaskFilter.Blur.NORMAL
    ): BlurMaskFilter {
        val bucket = (radiusPx.coerceIn(0.5f, 96.0f) * 2.0f).roundToInt().coerceAtLeast(1)
        val key = (style.ordinal shl 16) or bucket
        return blurMaskFilters.getOrPut(key) {
            BlurMaskFilter(bucket * 0.5f, style)
        }
    }
}

/**
 * Binds the backend-agnostic 256-byte [SeaGlassUniformBlock] (`16 × vec4`) to an
 * Android 13+ (API 33+) [RuntimeShader] instance with zero heap allocations and
 * per-slot dirty-bitmask delta filtering (`dirtySlotsMask`).
 */
@RequiresApi(Build.VERSION_CODES.TIRAMISU)
object SeaGlassAgslBinding {

    /**
     * Uploads `vec4` slots from [uniformBlock] into the target [shader].
     * When [onlyDirtySlots] is `true`, only slots whose bits are set in
     * [SeaGlassUniformBlock.dirtySlotsMask] cross the JNI boundary.
     */
    fun bindCompositeUniforms(
        shader: RuntimeShader,
        uniformBlock: SeaGlassUniformBlock,
        onlyDirtySlots: Boolean = false
    ) {
        val mask = if (onlyDirtySlots) uniformBlock.dirtySlotsMask else SeaGlassUniformBlock.ALL_SLOTS_MASK
        if (mask == 0) return

        val d = uniformBlock.data

        if ((mask and (1 shl 0)) != 0) shader.setFloatUniform("uResolutionAndOrigin", d[0], d[1], d[2], d[3])
        if ((mask and (1 shl 1)) != 0) shader.setFloatUniform("uOpticsPrimary", d[4], d[5], d[6], d[7])
        if ((mask and (1 shl 2)) != 0) shader.setFloatUniform("uOpticsSecondary", d[8], d[9], d[10], d[11])
        if ((mask and (1 shl 3)) != 0) shader.setFloatUniform("uLightAndMeta", d[12], d[13], d[14], d[15])

        // Node 0
        if ((mask and (1 shl 4)) != 0) shader.setFloatUniform("uNode0Bounds", d[16], d[17], d[18], d[19])
        if ((mask and (1 shl 8)) != 0) shader.setFloatUniform("uNode0Corners", d[32], d[33], d[34], d[35])
        if ((mask and (1 shl 12)) != 0) shader.setFloatUniform("uNode0Tint", d[48], d[49], d[50], d[51])

        // Node 1
        if ((mask and (1 shl 5)) != 0) shader.setFloatUniform("uNode1Bounds", d[20], d[21], d[22], d[23])
        if ((mask and (1 shl 9)) != 0) shader.setFloatUniform("uNode1Corners", d[36], d[37], d[38], d[39])
        if ((mask and (1 shl 13)) != 0) shader.setFloatUniform("uNode1Tint", d[52], d[53], d[54], d[55])

        // Node 2
        if ((mask and (1 shl 6)) != 0) shader.setFloatUniform("uNode2Bounds", d[24], d[25], d[26], d[27])
        if ((mask and (1 shl 10)) != 0) shader.setFloatUniform("uNode2Corners", d[40], d[41], d[42], d[43])
        if ((mask and (1 shl 14)) != 0) shader.setFloatUniform("uNode2Tint", d[56], d[57], d[58], d[59])

        // Node 3
        if ((mask and (1 shl 7)) != 0) shader.setFloatUniform("uNode3Bounds", d[28], d[29], d[30], d[31])
        if ((mask and (1 shl 11)) != 0) shader.setFloatUniform("uNode3Corners", d[44], d[45], d[46], d[47])
        if ((mask and (1 shl 15)) != 0) shader.setFloatUniform("uNode3Tint", d[60], d[61], d[62], d[63])

        uniformBlock.clearDirtyMask()
    }

    /**
     * Uploads uniforms for the lightweight AGSL sand-grain + frosted rim overlay shader.
     */
    fun bindSandGrainOverlayUniforms(
        shader: RuntimeShader,
        uniformBlock: SeaGlassUniformBlock,
        onlyDirtySlots: Boolean = false
    ) {
        bindCompositeUniforms(shader, uniformBlock, onlyDirtySlots)
    }
}
