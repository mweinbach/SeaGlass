package dev.seaglass.view

import android.graphics.Canvas
import android.graphics.ColorFilter
import android.graphics.LinearGradient
import android.graphics.Matrix
import android.graphics.Paint
import android.graphics.Path
import android.graphics.PixelFormat
import android.graphics.Rect
import android.graphics.Shader
import android.graphics.SweepGradient
import android.graphics.drawable.Drawable
import dev.seaglass.core.SeaGlassColorMath
import dev.seaglass.core.SeaGlassContourPathBuilder
import dev.seaglass.core.SeaGlassMaterial
import dev.seaglass.core.SeaGlassPalette
import dev.seaglass.core.SeaGlassSandGrainTexture
import dev.seaglass.core.SeaGlassSdfEngine
import dev.seaglass.core.SeaGlassShapeKind
import dev.seaglass.core.SeaGlassShapeMorph
import dev.seaglass.core.backend.SeaGlassUniformBlock
import kotlin.math.min

/**
 * Hardware-accelerated Android [Drawable] that renders a SeaGlass surface with
 * analytical SDF contours (including multi-node `sminCubic` coalescing and 20-shape
 * M3 Expressive polygon morphing), Oklab Beer-Lambert volumetric body tinting,
 * toroidal sand-grain micro-facet texture, and a prismatic meniscus rim bevel.
 */
class SeaGlassDrawable(
    material: SeaGlassMaterial = SeaGlassPalette.SeafoamMint.primaryGlass,
    private val density: Float = 2.75f
) : Drawable() {

    var material: SeaGlassMaterial = material
        set(value) {
            if (field != value) {
                field = value
                rebuildShaders(bounds)
                invalidateSelf()
            }
        }

    private var customNodes: List<SeaGlassSdfEngine.SdfNode>? = null
    private var smoothRadiusPx: Float = 28f * density
    private var morphStartShape: SeaGlassShapeKind? = null
    private var morphEndShape: SeaGlassShapeKind? = null
    private var morphProgress: Float = 0f

    val uniformBlock: SeaGlassUniformBlock = SeaGlassUniformBlock()

    private val contourPath = Path()
    private val bodyPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        style = Paint.Style.FILL
    }
    private val sandGrainPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        style = Paint.Style.FILL
        isFilterBitmap = true
        xfermode = android.graphics.PorterDuffXfermode(android.graphics.PorterDuff.Mode.OVERLAY)
        shader = SeaGlassSandGrainTexture.obtainShader()
    }
    private val grainMatrix = Matrix()
    private val innerSlopePaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        style = Paint.Style.STROKE
        strokeCap = Paint.Cap.ROUND
        strokeJoin = Paint.Join.ROUND
    }
    private val rimPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        style = Paint.Style.STROKE
        strokeCap = Paint.Cap.ROUND
        strokeJoin = Paint.Join.ROUND
    }

    /**
     * Configures multi-node liquid SDF coalescing (`sminCubic`) for this drawable.
     */
    fun setSdfNodes(nodes: List<SeaGlassSdfEngine.SdfNode>?, smoothRadiusDp: Float = 28f) {
        customNodes = nodes
        smoothRadiusPx = smoothRadiusDp * density
        rebuildShaders(bounds)
        invalidateSelf()
    }

    /**
     * Configures Material 3 Expressive 20-shape polygon morphing for this drawable.
     */
    fun setShapeMorph(
        startShape: SeaGlassShapeKind?,
        endShape: SeaGlassShapeKind? = startShape,
        progress: Float = 0f
    ) {
        morphStartShape = startShape
        morphEndShape = endShape
        morphProgress = progress
        rebuildShaders(bounds)
        invalidateSelf()
    }

    override fun onBoundsChange(bounds: Rect) {
        super.onBoundsChange(bounds)
        rebuildShaders(bounds)
    }

    private fun rebuildShaders(bounds: Rect) {
        val w = bounds.width().toFloat().coerceAtLeast(1f)
        val h = bounds.height().toFloat().coerceAtLeast(1f)
        val cornerPx = min(material.cornerRadiusDp * density, min(w, h) * 0.5f)
        val thicknessPx = material.thicknessDp * density

        val activeNodes = customNodes?.takeIf { it.isNotEmpty() } ?: listOf(
            SeaGlassSdfEngine.SdfNode(
                centerX = bounds.left + w * 0.5f,
                centerY = bounds.top + h * 0.5f,
                halfWidth = w * 0.5f,
                halfHeight = h * 0.5f,
                uniformCornerRadius = cornerPx,
                cornerSmoothing = material.cornerSmoothing,
                tintColorArgb = material.tintColorArgb,
                thicknessPx = thicknessPx
            )
        )

        val startKind = morphStartShape
        val endKind = morphEndShape
        if (startKind != null && endKind != null) {
            SeaGlassShapeMorph.buildMorphedPolygonPath(
                startKind = startKind,
                endKind = endKind,
                progress = morphProgress,
                widthPx = w,
                heightPx = h,
                outPath = contourPath
            )
            contourPath.offset(bounds.left.toFloat(), bounds.top.toFloat())
        } else {
            SeaGlassContourPathBuilder.buildCoalescingScenePath(
                nodes = activeNodes,
                smoothRadiusPx = if (activeNodes.size >= 2) smoothRadiusPx else 0f,
                outPath = contourPath,
                insetPx = 0f
            )
        }

        uniformBlock.pack(
            widthPx = w,
            heightPx = h,
            backdropOffsetX = bounds.left.toFloat(),
            backdropOffsetY = bounds.top.toFloat(),
            material = material,
            density = density,
            nodes = activeNodes,
            smoothRadiusPx = smoothRadiusPx
        )

        val primaryTint = activeNodes.first().tintColorArgb
        val secondaryTint = if (activeNodes.size >= 2) activeNodes[1].tintColorArgb else primaryTint

        val transmittedTop = SeaGlassColorMath.computeBeerLambertTransmission(
            backdropArgb = 0xFFF2F8FC.toInt(),
            tintArgb = primaryTint,
            tintDensity = material.tintDensity,
            coarseness = material.coarseness,
            normalizedThickness = 0.85f
        )
        val transmittedBottom = SeaGlassColorMath.computeBeerLambertTransmission(
            backdropArgb = 0xFFDCEBF6.toInt(),
            tintArgb = secondaryTint,
            tintDensity = material.tintDensity,
            coarseness = material.coarseness,
            normalizedThickness = 1.25f
        )

        val sandWear = material.effectiveSandWear
        val bodyAlpha = ((sandWear * 0.76f + material.tintDensity * (0.18f + 0.28f * sandWear))
            .coerceIn(0.08f, 0.92f) * 255f).toInt()
        val causticBottom = SeaGlassColorMath.computeSubsurfaceCausticColor(secondaryTint, material.coarseness)
        val topWithAlpha = (bodyAlpha shl 24) or (transmittedTop and 0x00FFFFFF)
        val midWithAlpha = (bodyAlpha shl 24) or (transmittedBottom and 0x00FFFFFF)
        val bottomWithAlpha = (bodyAlpha shl 24) or (causticBottom and 0x00FFFFFF)

        bodyPaint.shader = LinearGradient(
            bounds.exactCenterX(),
            bounds.top.toFloat(),
            bounds.exactCenterX(),
            bounds.bottom.toFloat(),
            intArrayOf(topWithAlpha, midWithAlpha, bottomWithAlpha),
            floatArrayOf(0.0f, 0.58f, 1.0f),
            Shader.TileMode.CLAMP
        )

        val grainScale = (density * 0.54f).coerceIn(0.85f, 1.65f)
        grainMatrix.reset()
        grainMatrix.setScale(grainScale, grainScale)
        sandGrainPaint.shader?.setLocalMatrix(grainMatrix)
        sandGrainPaint.alpha = (material.coarseness * (0.56f + 0.44f * sandWear) * 230f)
            .toInt()
            .coerceIn(0, 230)

        val cx = bounds.exactCenterX()
        val rimLight = (0.54f + material.rimChalkiness * 0.44f).coerceIn(0.38f, 0.98f)
        val topBevel = SeaGlassColorMath.computeFrostedRindColor(
            primaryTint,
            material.coarseness,
            0.78f
        ) and 0x00FFFFFF
        val sideBevel = SeaGlassColorMath.computeFrostedRindColor(
            secondaryTint,
            material.coarseness,
            0.66f
        ) and 0x00FFFFFF
        val bottomCrest = SeaGlassColorMath.lerpOklab(
            causticBottom,
            0xFFFFFFFF.toInt(),
            0.76f + 0.14f * material.coarseness
        ) and 0x00FFFFFF
        val milkyRind = SeaGlassColorMath.computeFrostedRindColor(
            primaryTint,
            material.coarseness,
            0.84f
        ) and 0x00FFFFFF

        fun argb(rgb: Int, aFloat: Float): Int {
            val a = (aFloat.coerceIn(0f, 1f) * 255f).toInt()
            return (a shl 24) or rgb
        }

        innerSlopePaint.strokeWidth = 17.5f * density
        innerSlopePaint.shader = LinearGradient(
            cx,
            bounds.top.toFloat(),
            cx,
            bounds.bottom.toFloat(),
            intArrayOf(
                argb(topBevel, rimLight * 0.74f),
                argb(sideBevel, rimLight * 0.58f),
                argb(bottomCrest, rimLight * 0.92f)
            ),
            floatArrayOf(0.0f, 0.45f, 1.0f),
            Shader.TileMode.CLAMP
        )

        rimPaint.strokeWidth = 2.4f * density
        rimPaint.shader = LinearGradient(
            cx,
            bounds.top.toFloat(),
            cx,
            bounds.bottom.toFloat(),
            intArrayOf(
                argb(0xFAFDFF, rimLight * 0.76f),
                argb(milkyRind, rimLight * 0.54f),
                argb(0xFFFFFF, rimLight * 0.90f)
            ),
            floatArrayOf(0.0f, 0.50f, 1.0f),
            Shader.TileMode.CLAMP
        )
    }

    override fun draw(canvas: Canvas) {
        canvas.drawPath(contourPath, bodyPaint)
        canvas.save()
        canvas.clipPath(contourPath)
        innerSlopePaint.alpha = 225
        canvas.drawPath(contourPath, innerSlopePaint)
        if (sandGrainPaint.alpha > 4) {
            canvas.drawRect(bounds, sandGrainPaint)
        }
        canvas.drawPath(contourPath, rimPaint)
        canvas.restore()
    }

    override fun setAlpha(alpha: Int) {
        bodyPaint.alpha = alpha
        rimPaint.alpha = alpha
        invalidateSelf()
    }

    override fun setColorFilter(colorFilter: ColorFilter?) {
        bodyPaint.colorFilter = colorFilter
        rimPaint.colorFilter = colorFilter
        invalidateSelf()
    }

    @Deprecated("Deprecated in Java")
    override fun getOpacity(): Int = PixelFormat.TRANSLUCENT
}
