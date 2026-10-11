package dev.seaglass.core.backend

import android.opengl.GLES30
import dev.seaglass.core.SeaGlassMaterial
import dev.seaglass.core.SeaGlassSdfEngine

/**
 * Identifies the GPU rendering backend family executing the SeaGlass render graph.
 */
enum class BackendType(
    val id: String,
    val displayName: String,
    val shaderDialect: ShaderDialect?
) {
    /**
     * Vulkan 1.1+ backend consuming `ShaderDialect.VULKAN_GLSL_450` (SPIR-V) and
     * the 256-byte `std140` `SeaGlassUniformBlock` via `VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER`.
     */
    VULKAN_1_1(
        id = "vulkan_1_1",
        displayName = "Vulkan 1.1+ (SPIR-V + std140 UBO)",
        shaderDialect = ShaderDialect.VULKAN_GLSL_450
    ),

    /**
     * Android 13+ (API 33+) zero-copy `RenderNode` + `RenderEffect` + `RuntimeShader` (AGSL) backend.
     */
    AGSL_RENDER_NODE(
        id = "agsl_render_node",
        displayName = "Android 13+ AGSL RenderNode",
        shaderDialect = ShaderDialect.AGSL_100
    ),

    /**
     * Android 12–12L (API 31–32) hardware `RenderEffect` blur + analytical SDF & Oklab Canvas composite.
     */
    HYBRID_RENDER_EFFECT(
        id = "hybrid_render_effect",
        displayName = "Android 12 Hybrid RenderEffect",
        shaderDialect = null
    ),

    /**
     * OpenGL ES 3.0+ backend consuming `ShaderDialect.GLSL_ES_300` and the 256-byte `std140` UBO.
     */
    OPENGLES_3_0(
        id = "opengles_3_0",
        displayName = "OpenGL ES 3.0 (std140 UBO + Dual-Kawase)",
        shaderDialect = ShaderDialect.GLSL_ES_300
    ),

    /**
     * Universal Canvas / CPU reference backend for API 26–30 UI views without a dedicated GL context,
     * Compose Previews, or accessibility Reduce-Transparency mode.
     */
    CANVAS_FALLBACK(
        id = "canvas_fallback",
        displayName = "Hardware Canvas SDF + Oklab Fallback",
        shaderDialect = null
    )
}

/**
 * Backend-agnostic specification of the passes required to render a SeaGlass frame.
 */
data class SeaGlassRenderGraphDescriptor(
    val backendType: BackendType,
    val kawasePyramidLevels: Int,
    val uboBindingIndex: Int = 0,
    val backdropSamplerBindingIndex: Int = 1,
    val uboByteSize: Int = SeaGlassUniformBlock.BYTE_SIZE,
    val shaderBundle: CompiledShaderBundle?
)

/**
 * Vulkan 1.1+ descriptor set and pipeline layout specification so any Vulkan engine,
 * `SurfaceView`/`TextureView` Vulkan swapchain, or `AHardwareBuffer` renderer can bind
 * SeaGlass shaders and `std140` uniform buffers without custom glue logic.
 */
data class VulkanPipelineContract(
    val descriptorSetIndex: Int = 0,
    val uboBinding: Int = 0,
    val backdropCombinedImageSamplerBinding: Int = 1,
    val uboSizeBytes: Int = SeaGlassUniformBlock.BYTE_SIZE,
    val kawasePushConstantSizeBytes: Int = 16,
    val shaderBundle: CompiledShaderBundle =
        SeaGlassShaderCompiler.compileForDialect(ShaderDialect.VULKAN_GLSL_450)
)

/**
 * Pluggable GPU backend interface for SeaGlass.
 *
 * UI layers (`:seaglass-compose` and `:seaglass-view`) populate a [SeaGlassUniformBlock]
 * and query [SeaGlassBackendRegistry] for the active backend descriptor.
 */
interface SeaGlassRendererBackend {
    val type: BackendType
    fun describeRenderGraph(frostRadiusPx: Float): SeaGlassRenderGraphDescriptor
    fun syncUniforms(uniformBlock: SeaGlassUniformBlock)
}

/**
 * Vulkan 1.1+ backend implementation providing the SPIR-V ready GLSL 450 shader bundle,
 * `std140` 256-byte staging buffer, and descriptor set layout contract.
 */
class VulkanSeaGlassBackend : SeaGlassRendererBackend {
    override val type: BackendType = BackendType.VULKAN_1_1

    val pipelineContract: VulkanPipelineContract = VulkanPipelineContract()
    val stagingBuffer = java.nio.ByteBuffer
        .allocateDirect(SeaGlassUniformBlock.BYTE_SIZE)
        .order(java.nio.ByteOrder.nativeOrder())

    private var lastSyncedRevision: Long = -1L

    override fun describeRenderGraph(frostRadiusPx: Float): SeaGlassRenderGraphDescriptor {
        val levels = when {
            frostRadiusPx <= 2f -> 0
            frostRadiusPx <= 12f -> 2
            frostRadiusPx <= 28f -> 3
            else -> 4
        }
        return SeaGlassRenderGraphDescriptor(
            backendType = type,
            kawasePyramidLevels = levels,
            uboBindingIndex = pipelineContract.uboBinding,
            backdropSamplerBindingIndex = pipelineContract.backdropCombinedImageSamplerBinding,
            uboByteSize = pipelineContract.uboSizeBytes,
            shaderBundle = pipelineContract.shaderBundle
        )
    }

    override fun syncUniforms(uniformBlock: SeaGlassUniformBlock) {
        if (lastSyncedRevision == uniformBlock.contentRevision && stagingBuffer.remaining() == SeaGlassUniformBlock.BYTE_SIZE) {
            return
        }
        lastSyncedRevision = uniformBlock.contentRevision
        stagingBuffer.clear()
        uniformBlock.writeToStd140ByteBuffer(stagingBuffer)
        stagingBuffer.flip()
        uniformBlock.clearDirtyMask()
    }
}

/**
 * OpenGL ES 3.0 backend implementation consuming the exact same 256-byte `std140` UBO
 * and compiling `ShaderDialect.GLSL_ES_300` once on an active EGL/GLES3 thread.
 */
class Gles3SeaGlassBackend : SeaGlassRendererBackend {
    override val type: BackendType = BackendType.OPENGLES_3_0

    val shaderBundle: CompiledShaderBundle =
        SeaGlassShaderCompiler.compileForDialect(ShaderDialect.GLSL_ES_300)

    val stagingBuffer = java.nio.ByteBuffer
        .allocateDirect(SeaGlassUniformBlock.BYTE_SIZE)
        .order(java.nio.ByteOrder.nativeOrder())

    private var uboHandle: Int = 0
    private var compositeProgramHandle: Int = 0
    private var lastSyncedRevision: Long = -1L

    override fun describeRenderGraph(frostRadiusPx: Float): SeaGlassRenderGraphDescriptor {
        val levels = when {
            frostRadiusPx <= 2f -> 0
            frostRadiusPx <= 12f -> 2
            frostRadiusPx <= 28f -> 3
            else -> 4
        }
        return SeaGlassRenderGraphDescriptor(
            backendType = type,
            kawasePyramidLevels = levels,
            uboBindingIndex = 0,
            backdropSamplerBindingIndex = 1,
            uboByteSize = SeaGlassUniformBlock.BYTE_SIZE,
            shaderBundle = shaderBundle
        )
    }

    override fun syncUniforms(uniformBlock: SeaGlassUniformBlock) {
        if (lastSyncedRevision == uniformBlock.contentRevision && stagingBuffer.remaining() == SeaGlassUniformBlock.BYTE_SIZE) {
            return
        }
        lastSyncedRevision = uniformBlock.contentRevision
        stagingBuffer.clear()
        uniformBlock.writeToStd140ByteBuffer(stagingBuffer)
        stagingBuffer.flip()
        uniformBlock.clearDirtyMask()
        if (uboHandle != 0) {
            GLES30.glBindBuffer(GLES30.GL_UNIFORM_BUFFER, uboHandle)
            GLES30.glBufferSubData(
                GLES30.GL_UNIFORM_BUFFER,
                0,
                SeaGlassUniformBlock.BYTE_SIZE,
                stagingBuffer
            )
            GLES30.glBindBuffer(GLES30.GL_UNIFORM_BUFFER, 0)
        }
    }

    /**
     * Compiles the GLSL ES 3.00 SeaGlass composite shader program once and allocates the
     * 256-byte `std140` Uniform Buffer Object when called on an active EGL/GLES3 context.
     */
    fun initializeGlResourcesIfContextActive(): Boolean {
        if (compositeProgramHandle != 0) return true
        val vShader = compileShader(GLES30.GL_VERTEX_SHADER, shaderBundle.vertexShaderSource ?: return false)
        val fShader = compileShader(GLES30.GL_FRAGMENT_SHADER, shaderBundle.seaGlassCompositeFragmentSource)
        if (vShader == 0 || fShader == 0) return false

        val program = GLES30.glCreateProgram()
        GLES30.glAttachShader(program, vShader)
        GLES30.glAttachShader(program, fShader)
        GLES30.glLinkProgram(program)

        val linkStatus = IntArray(1)
        GLES30.glGetProgramiv(program, GLES30.GL_LINK_STATUS, linkStatus, 0)
        GLES30.glDeleteShader(vShader)
        GLES30.glDeleteShader(fShader)

        if (linkStatus[0] == 0) {
            GLES30.glDeleteProgram(program)
            return false
        }

        compositeProgramHandle = program
        val blockIndex = GLES30.glGetUniformBlockIndex(program, "SeaGlassUniforms")
        if (blockIndex != GLES30.GL_INVALID_INDEX) {
            GLES30.glUniformBlockBinding(program, blockIndex, 0)
        }

        val buffers = IntArray(1)
        GLES30.glGenBuffers(1, buffers, 0)
        uboHandle = buffers[0]
        GLES30.glBindBuffer(GLES30.GL_UNIFORM_BUFFER, uboHandle)
        GLES30.glBufferData(
            GLES30.GL_UNIFORM_BUFFER,
            SeaGlassUniformBlock.BYTE_SIZE,
            stagingBuffer,
            GLES30.GL_DYNAMIC_DRAW
        )
        GLES30.glBindBufferBase(GLES30.GL_UNIFORM_BUFFER, 0, uboHandle)
        GLES30.glBindBuffer(GLES30.GL_UNIFORM_BUFFER, 0)
        return true
    }

    private fun compileShader(type: Int, source: String): Int {
        val shader = GLES30.glCreateShader(type)
        if (shader == 0) return 0
        GLES30.glShaderSource(shader, source)
        GLES30.glCompileShader(shader)
        val compiled = IntArray(1)
        GLES30.glGetShaderiv(shader, GLES30.GL_COMPILE_STATUS, compiled, 0)
        if (compiled[0] == 0) {
            GLES30.glDeleteShader(shader)
            return 0
        }
        return shader
    }
}
