package dev.seaglass.view

import android.content.Context
import android.graphics.Canvas
import android.graphics.RenderEffect
import android.graphics.RenderNode
import android.graphics.RuntimeShader
import android.graphics.Shader
import android.os.Build
import android.util.AttributeSet
import android.view.View
import android.widget.FrameLayout
import dev.seaglass.core.SeaGlassDeviceTier
import dev.seaglass.core.SeaGlassMaterial
import dev.seaglass.core.SeaGlassPalette
import dev.seaglass.core.SeaGlassSdfEngine
import dev.seaglass.core.backend.SeaGlassAgslBinding
import dev.seaglass.core.backend.SeaGlassShaderCompiler
import dev.seaglass.core.backend.ShaderDialect
import kotlin.math.min

/**
 * Classic Android [FrameLayout] container that renders a GPU-accelerated SeaGlass backdrop
 * and tumbled bevel contour behind its child views.
 *
 * Works seamlessly across API 26–36+ using the same [SeaGlassDeviceTier] and 256-byte `std140`
 * uniform block as `:seaglass-compose`.
 */
class SeaGlassFrameLayout @JvmOverloads constructor(
    context: Context,
    attrs: AttributeSet? = null,
    defStyleAttr: Int = 0
) : FrameLayout(context, attrs, defStyleAttr) {

    private val density = resources.displayMetrics.density
    private val seaGlassDrawable = SeaGlassDrawable(
        material = SeaGlassPalette.SeafoamMint.surfaceGlass,
        density = density
    )

    private var backdropTargetView: View? = null
    private val locationBuffer = IntArray(2)
    private val targetLocationBuffer = IntArray(2)

    private val backdropRenderNode: RenderNode? =
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
            RenderNode("SeaGlassViewNode")
        } else {
            null
        }

    private val agslRuntimeShader: RuntimeShader? =
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            val bundle = SeaGlassShaderCompiler.compileForDialect(ShaderDialect.AGSL_100)
            RuntimeShader(bundle.seaGlassCompositeFragmentSource)
        } else {
            null
        }

    var material: SeaGlassMaterial
        get() = seaGlassDrawable.material
        set(value) {
            seaGlassDrawable.material = value
            invalidate()
        }

    init {
        setWillNotDraw(false)
    }

    /**
     * Binds a background [View] (such as an artwork container or scrollable content view)
     * to be refracted and frosted behind this [SeaGlassFrameLayout].
     */
    fun bindBackdropTarget(target: View?) {
        backdropTargetView = target
        invalidate()
    }

    override fun onSizeChanged(w: Int, h: Int, oldw: Int, oldh: Int) {
        super.onSizeChanged(w, h, oldw, oldh)
        seaGlassDrawable.setBounds(0, 0, w, h)
    }

    override fun dispatchDraw(canvas: Canvas) {
        val w = width.toFloat()
        val h = height.toFloat()
        if (w > 1f && h > 1f) {
            val target = backdropTargetView
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU &&
                canvas.isHardwareAccelerated &&
                target != null &&
                backdropRenderNode != null &&
                agslRuntimeShader != null
            ) {
                getLocationInWindow(locationBuffer)
                target.getLocationInWindow(targetLocationBuffer)
                val relX = (locationBuffer[0] - targetLocationBuffer[0]).toFloat()
                val relY = (locationBuffer[1] - targetLocationBuffer[1]).toFloat()

                val cornerPx = min(material.cornerRadiusDp * density, min(w, h) * 0.5f)
                val node = SeaGlassSdfEngine.SdfNode(
                    centerX = w * 0.5f,
                    centerY = h * 0.5f,
                    halfWidth = w * 0.5f,
                    halfHeight = h * 0.5f,
                    uniformCornerRadius = cornerPx,
                    cornerSmoothing = material.cornerSmoothing,
                    tintColorArgb = material.tintColorArgb,
                    thicknessPx = material.thicknessDp * density
                )

                seaGlassDrawable.uniformBlock.pack(
                    widthPx = w,
                    heightPx = h,
                    backdropOffsetX = relX,
                    backdropOffsetY = relY,
                    material = material,
                    density = density,
                    nodes = listOf(node),
                    smoothRadiusPx = 0f
                )
                SeaGlassAgslBinding.bindCompositeUniforms(
                    agslRuntimeShader,
                    seaGlassDrawable.uniformBlock
                )

                val frostPx = (material.frostRadiusDp * density).coerceAtLeast(1f)
                val runtimeEffect = RenderEffect.createRuntimeShaderEffect(
                    agslRuntimeShader,
                    "backdrop"
                )
                val blurEffect = RenderEffect.createBlurEffect(
                    frostPx,
                    frostPx,
                    Shader.TileMode.CLAMP
                )
                backdropRenderNode.setPosition(0, 0, width, height)
                backdropRenderNode.setRenderEffect(
                    RenderEffect.createChainEffect(runtimeEffect, blurEffect)
                )

                val recCanvas = backdropRenderNode.beginRecording(width, height)
                try {
                    recCanvas.translate(-relX, -relY)
                    target.draw(recCanvas)
                } finally {
                    backdropRenderNode.endRecording()
                }
                canvas.drawRenderNode(backdropRenderNode)
            } else {
                seaGlassDrawable.draw(canvas)
            }
        }
        super.dispatchDraw(canvas)
    }
}
