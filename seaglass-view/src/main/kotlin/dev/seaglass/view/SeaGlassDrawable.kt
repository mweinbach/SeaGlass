package dev.seaglass.view

import android.graphics.Canvas
import android.graphics.ColorFilter
import android.graphics.LinearGradient
import android.graphics.Paint
import android.graphics.Path
import android.graphics.PixelFormat
import android.graphics.Rect
import android.graphics.Shader
import android.graphics.drawable.Drawable
import dev.seaglass.core.SeaGlassColorMath
import dev.seaglass.core.SeaGlassContourPathBuilder
import dev.seaglass.core.SeaGlassMaterial
import dev.seaglass.core.SeaGlassPalette
import dev.seaglass.core.SeaGlassSdfEngine
import dev.seaglass.core.backend.SeaGlassUniformBlock
import kotlin.math.min

/**
 * Hardware-accelerated Android [Drawable] that renders a SeaGlass surface with
 * analytical SDF contours, Oklab Beer-Lambert volumetric body tinting, and a
 * chalky Oren-Nayar / rough-Fresnel tumbled rim bevel.
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

    val uniformBlock: SeaGlassUniformBlock = SeaGlassUniformBlock()

    private val contourPath = Path()
    private val bodyPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        style = Paint.Style.FILL
    }
    private val rimPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        style = Paint.Style.STROKE
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

        val node = SeaGlassSdfEngine.SdfNode(
            centerX = bounds.left + w * 0.5f,
            centerY = bounds.top + h * 0.5f,
            halfWidth = w * 0.5f,
            halfHeight = h * 0.5f,
            uniformCornerRadius = cornerPx,
            cornerSmoothing = material.cornerSmoothing,
            tintColorArgb = material.tintColorArgb,
            thicknessPx = thicknessPx
        )

        SeaGlassContourPathBuilder.buildSingleNodePath(node, contourPath)
        uniformBlock.pack(
            widthPx = w,
            heightPx = h,
            backdropOffsetX = bounds.left.toFloat(),
            backdropOffsetY = bounds.top.toFloat(),
            material = material,
            density = density,
            nodes = listOf(node),
            smoothRadiusPx = 0f
        )

        val transmittedTop = SeaGlassColorMath.computeBeerLambertTransmission(
            backdropArgb = 0xFFEAF3FA.toInt(),
            tintArgb = material.tintColorArgb,
            tintDensity = material.tintDensity,
            coarseness = material.coarseness,
            normalizedThickness = 0.85f
        )
        val transmittedBottom = SeaGlassColorMath.computeBeerLambertTransmission(
            backdropArgb = 0xFFCCE0F0.toInt(),
            tintArgb = material.tintColorArgb,
            tintDensity = material.tintDensity,
            coarseness = material.coarseness,
            normalizedThickness = 1.25f
        )

        val bodyAlpha = ((0.46f + material.tintDensity * 0.32f + material.coarseness * 0.14f)
            .coerceIn(0.30f, 0.90f) * 255f).toInt()
        val topWithAlpha = (bodyAlpha shl 24) or (transmittedTop and 0x00FFFFFF)
        val bottomWithAlpha = (bodyAlpha shl 24) or (transmittedBottom and 0x00FFFFFF)

        bodyPaint.shader = LinearGradient(
            bounds.left.toFloat(),
            bounds.top.toFloat(),
            bounds.right.toFloat(),
            bounds.bottom.toFloat(),
            topWithAlpha,
            bottomWithAlpha,
            Shader.TileMode.CLAMP
        )

        val windwardRim = SeaGlassColorMath.lerpOklab(
            material.tintColorArgb,
            0xFFFFFFFF.toInt(),
            (0.70f + material.coarseness * 0.20f).coerceIn(0f, 0.95f)
        )
        val leewardRim = SeaGlassColorMath.lerpOklab(
            material.tintColorArgb,
            0xFFFFFFFF.toInt(),
            0.30f
        )
        val rimAlpha = ((0.42f + material.rimChalkiness * 0.48f).coerceIn(0.25f, 0.92f) * 255f).toInt()

        rimPaint.strokeWidth = (1.4f + material.coarseness * 0.85f) * density
        rimPaint.shader = LinearGradient(
            bounds.left.toFloat(),
            bounds.top.toFloat(),
            bounds.right.toFloat(),
            bounds.bottom.toFloat(),
            (rimAlpha shl 24) or (windwardRim and 0x00FFFFFF),
            ((rimAlpha * 0.65f).toInt() shl 24) or (leewardRim and 0x00FFFFFF),
            Shader.TileMode.CLAMP
        )
    }

    override fun draw(canvas: Canvas) {
        canvas.drawPath(contourPath, bodyPaint)
        canvas.drawPath(contourPath, rimPaint)
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
