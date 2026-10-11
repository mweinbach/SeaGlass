package dev.seaglass.view

import android.content.Context
import android.graphics.Canvas
import android.graphics.RenderEffect
import android.graphics.RenderNode
import android.graphics.RuntimeShader
import android.graphics.Shader
import android.graphics.Typeface
import android.os.Build
import android.util.AttributeSet
import android.util.TypedValue
import android.view.Gravity
import android.view.MotionEvent
import android.view.View
import android.widget.FrameLayout
import android.widget.TextView
import dev.seaglass.core.SeaGlassColorMath
import dev.seaglass.core.SeaGlassMaterial
import dev.seaglass.core.SeaGlassPalette
import dev.seaglass.core.SeaGlassSdfEngine
import dev.seaglass.core.SeaGlassShapeKind
import dev.seaglass.core.backend.SeaGlassAgslBinding
import dev.seaglass.core.backend.SeaGlassShaderCompiler
import dev.seaglass.core.backend.ShaderDialect
import kotlin.math.min
import kotlin.math.roundToInt

/**
 * Classic Android [FrameLayout] container that renders a GPU-accelerated SeaGlass backdrop
 * and tumbled bevel contour behind its child views.
 *
 * Works seamlessly across API 26–36+ using the same `std140` uniform block as `:seaglass-compose`.
 */
open class SeaGlassFrameLayout @JvmOverloads constructor(
    context: Context,
    attrs: AttributeSet? = null,
    defStyleAttr: Int = 0
) : FrameLayout(context, attrs, defStyleAttr) {

    protected val density: Float = resources.displayMetrics.density
    protected val seaGlassDrawable: SeaGlassDrawable = SeaGlassDrawable(
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
            onMaterialUpdated(value)
            invalidate()
        }

    init {
        setWillNotDraw(false)
    }

    protected open fun onMaterialUpdated(newMaterial: SeaGlassMaterial) {}

    /**
     * Binds a background [View] (such as an artwork container or scrollable content view)
     * to be refracted and frosted behind this [SeaGlassFrameLayout].
     */
    fun bindBackdropTarget(target: View?) {
        backdropTargetView = target
        invalidate()
    }

    /**
     * Configures multi-node SDF smooth-union coalescing (`sminCubic`) for this view.
     */
    fun setSdfNodes(nodes: List<SeaGlassSdfEngine.SdfNode>?, smoothRadiusDp: Float = 28f) {
        seaGlassDrawable.setSdfNodes(nodes, smoothRadiusDp)
        invalidate()
    }

    /**
     * Configures Material 3 Expressive 20-shape polygon morphing for this view.
     */
    fun setShapeMorph(
        startShape: SeaGlassShapeKind?,
        endShape: SeaGlassShapeKind? = startShape,
        progress: Float = 0f
    ) {
        seaGlassDrawable.setShapeMorph(startShape, endShape, progress)
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

/**
 * Classic Android View card container with default 20dp sea-glass padding and squircle contour.
 */
class SeaGlassCardView @JvmOverloads constructor(
    context: Context,
    attrs: AttributeSet? = null,
    defStyleAttr: Int = 0
) : SeaGlassFrameLayout(context, attrs, defStyleAttr) {

    init {
        val pad = (20f * density).roundToInt()
        setPadding(pad, pad, pad, pad)
        material = SeaGlassPalette.SeafoamMint.surfaceGlass
    }
}

/**
 * Tactile classic Android View button backed by [SeaGlassFrameLayout] that physically compresses
 * on touch and automatically synchronizes its label color via [SeaGlassColorMath.optimalContentColorFor].
 */
class SeaGlassButtonView @JvmOverloads constructor(
    context: Context,
    attrs: AttributeSet? = null,
    defStyleAttr: Int = 0
) : SeaGlassFrameLayout(context, attrs, defStyleAttr) {

    val labelView: TextView = TextView(context).apply {
        gravity = Gravity.CENTER
        setTextSize(TypedValue.COMPLEX_UNIT_SP, 14f)
        typeface = Typeface.create(Typeface.DEFAULT, Typeface.BOLD)
    }

    private var restingMaterial: SeaGlassMaterial = SeaGlassPalette.SeafoamMint.primaryGlass

    var text: CharSequence
        get() = labelView.text
        set(value) {
            labelView.text = value
        }

    init {
        val padH = (22f * density).roundToInt()
        val padV = (13f * density).roundToInt()
        setPadding(padH, padV, padH, padV)
        isClickable = true
        isFocusable = true
        addView(
            labelView,
            LayoutParams(LayoutParams.WRAP_CONTENT, LayoutParams.WRAP_CONTENT, Gravity.CENTER)
        )
        material = restingMaterial
    }

    override fun onMaterialUpdated(newMaterial: SeaGlassMaterial) {
        val contentColor = SeaGlassColorMath.optimalContentColorFor(
            newMaterial.tintColorArgb,
            newMaterial.tintDensity
        )
        labelView.setTextColor(contentColor)
    }

    override fun onTouchEvent(event: MotionEvent): Boolean {
        when (event.actionMasked) {
            MotionEvent.ACTION_DOWN -> {
                restingMaterial = material
                seaGlassDrawable.material = restingMaterial.pressed(1f)
                animate().scaleX(1.04f).scaleY(0.96f).setDuration(110).start()
                invalidate()
            }
            MotionEvent.ACTION_UP, MotionEvent.ACTION_CANCEL -> {
                seaGlassDrawable.material = restingMaterial
                animate().scaleX(1f).scaleY(1f).setDuration(150).start()
                invalidate()
            }
        }
        return super.onTouchEvent(event)
    }
}
