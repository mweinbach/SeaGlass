package dev.seaglass.core

import android.graphics.Matrix
import android.graphics.Path
import android.graphics.RectF
import androidx.graphics.shapes.CornerRounding
import androidx.graphics.shapes.Morph
import androidx.graphics.shapes.RoundedPolygon
import androidx.graphics.shapes.circle
import androidx.graphics.shapes.pill
import androidx.graphics.shapes.rectangle
import androidx.graphics.shapes.star
import androidx.graphics.shapes.toPath
import kotlin.math.abs
import kotlin.math.max
import kotlin.math.sqrt

/**
 * Material 3 Expressive × Liquid Sea Glass shape catalog and dynamic polygon/SDF morph engine.
 *
 * Bridges `androidx.graphics.shapes.RoundedPolygon` & `Morph` with [SeaGlassSdfEngine] and
 * [SeaGlassMaterial] so any two shapes or UI elements can smoothly morph their geometry,
 * topology (`sminCubic` liquid bridge), and optical properties (thickness, lensing, frost, tint).
 */
enum class SeaGlassShapeKind(val displayName: String) {
    Circle("Circle Droplet"),
    Capsule("Liquid Capsule"),
    Squircle("Expressive Squircle"),
    Cookie4("4-Leaf Clover"),
    Cookie6("6-Sided Scallop"),
    Cookie9("9-Sided Expressive Cookie"),
    SoftBurst("Soft Starburst"),
    Gem("Octagonal Gem")
}

object SeaGlassShapeMorph {

    private val scratchMatrix = Matrix()
    private val scratchBounds = RectF()
    private val targetBounds = RectF()

    /**
     * Creates a normalized `[0..1]` [RoundedPolygon] for the given [kind] and aspect ratio.
     */
    fun createNormalizedPolygon(
        kind: SeaGlassShapeKind,
        aspectRatio: Float = 1f
    ): RoundedPolygon {
        return when (kind) {
            SeaGlassShapeKind.Circle -> RoundedPolygon.circle(numVertices = 8).normalized()
            SeaGlassShapeKind.Capsule -> RoundedPolygon.pill(
                width = aspectRatio.coerceIn(0.5f, 4.0f),
                height = 1f
            ).normalized()

            SeaGlassShapeKind.Squircle -> RoundedPolygon.rectangle(
                width = aspectRatio.coerceIn(0.5f, 4.0f),
                height = 1f,
                rounding = CornerRounding(radius = 0.42f, smoothing = 0.65f)
            ).normalized()

            SeaGlassShapeKind.Cookie4 -> RoundedPolygon.star(
                numVerticesPerRadius = 4,
                innerRadius = 0.66f,
                rounding = CornerRounding(radius = 0.32f, smoothing = 0.55f),
                innerRounding = CornerRounding(radius = 0.24f, smoothing = 0.50f)
            ).normalized()

            SeaGlassShapeKind.Cookie6 -> RoundedPolygon.star(
                numVerticesPerRadius = 6,
                innerRadius = 0.74f,
                rounding = CornerRounding(radius = 0.26f, smoothing = 0.55f),
                innerRounding = CornerRounding(radius = 0.20f, smoothing = 0.50f)
            ).normalized()

            SeaGlassShapeKind.Cookie9 -> RoundedPolygon.star(
                numVerticesPerRadius = 9,
                innerRadius = 0.80f,
                rounding = CornerRounding(radius = 0.20f, smoothing = 0.50f),
                innerRounding = CornerRounding(radius = 0.16f, smoothing = 0.50f)
            ).normalized()

            SeaGlassShapeKind.SoftBurst -> RoundedPolygon.star(
                numVerticesPerRadius = 12,
                innerRadius = 0.82f,
                rounding = CornerRounding(radius = 0.14f, smoothing = 0.45f),
                innerRounding = CornerRounding(radius = 0.14f, smoothing = 0.45f)
            ).normalized()

            SeaGlassShapeKind.Gem -> RoundedPolygon(
                numVertices = 8,
                rounding = CornerRounding(radius = 0.22f, smoothing = 0.35f)
            ).normalized()
        }
    }

    /**
     * Populates [outPath] with the smoothly morphed contour between [startKind] and [endKind]
     * at [progress] $\in [0, 1]$, fitted cleanly to `[0..widthPx, 0..heightPx]`.
     */
    fun buildMorphedPolygonPath(
        startKind: SeaGlassShapeKind,
        endKind: SeaGlassShapeKind,
        progress: Float,
        widthPx: Float,
        heightPx: Float,
        outPath: Path
    ) {
        val clampedProgress = progress.coerceIn(0f, 1f)
        val aspect = (widthPx / max(heightPx, 1f)).coerceIn(0.4f, 4.0f)
        val startPoly = createNormalizedPolygon(startKind, aspect)
        val endPoly = createNormalizedPolygon(endKind, aspect)
        val morph = Morph(startPoly, endPoly)

        outPath.reset()
        morph.toPath(progress = clampedProgress, path = outPath)

        outPath.computeBounds(scratchBounds, true)
        if (scratchBounds.width() > 1e-4f && scratchBounds.height() > 1e-4f) {
            targetBounds.set(0f, 0f, widthPx, heightPx)
            scratchMatrix.reset()
            scratchMatrix.setRectToRect(scratchBounds, targetBounds, Matrix.ScaleToFit.FILL)
            outPath.transform(scratchMatrix)
        }
    }

    /**
     * Computes the dynamic **Liquid Glass Flex Material** during an animation or transition.
     */
    fun computeDynamicFlexMaterial(
        startMaterial: SeaGlassMaterial,
        endMaterial: SeaGlassMaterial,
        progress: Float,
        velocityPxPerSec: Float = 0f,
        areaExpansionRatio: Float = 1f
    ): SeaGlassMaterial {
        val p = progress.coerceIn(0f, 1f)
        val base = SeaGlassMaterial.lerp(startMaterial, endMaterial, p)

        val morphEnergy = (4f * p * (1f - p)).coerceIn(0f, 1f)
        val velocityEnergy = (abs(velocityPxPerSec) / 1800f).coerceIn(0f, 1f)
        val totalEnergy = max(morphEnergy, velocityEnergy * 0.75f)

        val scaleFactor = sqrt(areaExpansionRatio.coerceIn(0.5f, 4.0f))

        return base.copy(
            ior = (base.ior + 0.10f * totalEnergy + 0.04f * (scaleFactor - 1f)).coerceIn(1.0f, 1.95f),
            chromaticDispersion = (base.chromaticDispersion + 0.22f * totalEnergy).coerceIn(0f, 1f),
            rimChalkiness = (base.rimChalkiness + 0.16f * totalEnergy).coerceIn(0f, 1f),
            thicknessDp = (base.thicknessDp * (1f + 0.20f * totalEnergy) * scaleFactor.coerceIn(0.85f, 1.35f))
                .coerceIn(4f, 36f)
        )
    }
}
