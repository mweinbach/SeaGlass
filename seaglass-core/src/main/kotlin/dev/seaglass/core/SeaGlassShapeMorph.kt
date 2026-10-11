package dev.seaglass.core

import android.graphics.Matrix
import android.graphics.Path
import android.graphics.PointF
import android.graphics.RectF
import androidx.graphics.shapes.CornerRounding
import androidx.graphics.shapes.Cubic
import androidx.graphics.shapes.Morph
import androidx.graphics.shapes.RoundedPolygon
import androidx.graphics.shapes.circle
import androidx.graphics.shapes.pill
import androidx.graphics.shapes.rectangle
import androidx.graphics.shapes.star
import androidx.graphics.shapes.toPath
import java.util.concurrent.ConcurrentHashMap
import kotlin.math.PI
import kotlin.math.abs
import kotlin.math.cos
import kotlin.math.floor
import kotlin.math.max
import kotlin.math.min
import kotlin.math.roundToInt
import kotlin.math.sin
import kotlin.math.sqrt

/**
 * Full Material 3 Expressive × Liquid Sea Glass shape catalog (`20` preset expressive geometries).
 *
 * Every shape in [SeaGlassShapeKind] supports:
 * 1. **Pairwise & Multi-Step Sequence Morphing** (`Morph` with cached feature matching and
 *    true under-damped spatial spring overshoot beyond `[0..1]`).
 * 2. **Polar Signed Distance Field Coalescing** ([evaluatePolarRadiusScale]), allowing any two
 *    morphing expressive shapes (e.g., `Cookie9` + `Clover4` or `Flower` + `Heart`) to form
 *    `sminCubic` liquid sea-glass bridges while their lobes morph in real time.
 */
enum class SeaGlassShapeKind(val displayName: String, val shortLabel: String) {
    Circle("Circle Droplet", "Circle"),
    Capsule("Liquid Capsule", "Pill"),
    Squircle("Expressive Squircle", "Squircle"),
    Arch("Architectural Arch", "Arch"),
    Sunny("8-Ray Sunny", "Sunny"),
    Cookie4("4-Lobe Cookie", "Cookie 4"),
    Cookie6("6-Lobe Scallop", "Cookie 6"),
    Cookie7("7-Lobe Heptagon", "Cookie 7"),
    Cookie9("9-Lobe Expressive", "Cookie 9"),
    Cookie12("12-Lobe Medallion", "Cookie 12"),
    Clover4("Deep 4-Leaf Clover", "Clover 4"),
    Clover8("8-Petal Rosette", "Clover 8"),
    Flower("Organic Flower", "Flower"),
    SoftBurst("Soft Starburst", "Burst"),
    Gem("Octagonal Gem", "Gem"),
    Heart("Expressive Heart", "Heart"),
    Diamond("Puffy Diamond", "Diamond"),
    Pentagon("Soft Pentagon", "Pentagon"),
    Triangle("Rounded Triangle", "Triangle"),
    Slanted("Expressive Slanted", "Slanted")
}

object SeaGlassShapeMorph {

    private data class PolygonCacheKey(
        val kind: SeaGlassShapeKind,
        val quantizedAspect: Int
    )

    private data class MorphCacheKey(
        val startKind: SeaGlassShapeKind,
        val endKind: SeaGlassShapeKind,
        val quantizedAspect: Int
    )

    private data class CachedMorphEntry(
        val morph: Morph,
        val startBounds: RectF,
        val endBounds: RectF
    )

    private const val POLAR_LUT_SIZE = 128
    private val polygonCache = ConcurrentHashMap<PolygonCacheKey, RoundedPolygon>()
    private val morphCache = ConcurrentHashMap<MorphCacheKey, CachedMorphEntry>()
    private val polarLutCache = ConcurrentHashMap<SeaGlassShapeKind, FloatArray>()

    private val scratchMatrix = Matrix()
    private val scratchBounds = RectF()
    private val targetBounds = RectF()

    /**
     * Returns a cached, normalized `[0..1]` [RoundedPolygon] for [kind] and [aspectRatio].
     */
    fun obtainNormalizedPolygon(
        kind: SeaGlassShapeKind,
        aspectRatio: Float = 1f
    ): RoundedPolygon {
        val qAspect = (aspectRatio.coerceIn(0.4f, 4.0f) * 20f).roundToInt()
        val key = PolygonCacheKey(kind, qAspect)
        polygonCache[key]?.let { return it }

        val aspect = qAspect / 20f
        val built = createNormalizedPolygonInternal(kind, aspect)
        polygonCache[key] = built
        return built
    }

    private fun createNormalizedPolygonInternal(
        kind: SeaGlassShapeKind,
        aspectRatio: Float
    ): RoundedPolygon {
        val w = aspectRatio.coerceIn(0.4f, 4.0f)
        return when (kind) {
            SeaGlassShapeKind.Circle -> RoundedPolygon.circle(numVertices = 10).normalized()

            SeaGlassShapeKind.Capsule -> RoundedPolygon.pill(
                width = w,
                height = 1f
            ).normalized()

            SeaGlassShapeKind.Squircle -> RoundedPolygon.rectangle(
                width = w,
                height = 1f,
                rounding = CornerRounding(radius = 0.42f, smoothing = 0.68f)
            ).normalized()

            SeaGlassShapeKind.Arch -> RoundedPolygon(
                numVertices = 4,
                radius = 1f,
                perVertexRounding = listOf(
                    CornerRounding(radius = 0.48f, smoothing = 0.55f),
                    CornerRounding(radius = 0.48f, smoothing = 0.55f),
                    CornerRounding(radius = 0.16f, smoothing = 0.25f),
                    CornerRounding(radius = 0.16f, smoothing = 0.25f)
                )
            ).normalized()

            SeaGlassShapeKind.Sunny -> RoundedPolygon.star(
                numVerticesPerRadius = 8,
                innerRadius = 0.78f,
                rounding = CornerRounding(radius = 0.18f, smoothing = 0.45f),
                innerRounding = CornerRounding(radius = 0.18f, smoothing = 0.45f)
            ).normalized()

            SeaGlassShapeKind.Cookie4 -> RoundedPolygon.star(
                numVerticesPerRadius = 4,
                innerRadius = 0.66f,
                rounding = CornerRounding(radius = 0.34f, smoothing = 0.55f),
                innerRounding = CornerRounding(radius = 0.24f, smoothing = 0.50f)
            ).normalized()

            SeaGlassShapeKind.Cookie6 -> RoundedPolygon.star(
                numVerticesPerRadius = 6,
                innerRadius = 0.74f,
                rounding = CornerRounding(radius = 0.26f, smoothing = 0.55f),
                innerRounding = CornerRounding(radius = 0.20f, smoothing = 0.50f)
            ).normalized()

            SeaGlassShapeKind.Cookie7 -> RoundedPolygon.star(
                numVerticesPerRadius = 7,
                innerRadius = 0.76f,
                rounding = CornerRounding(radius = 0.23f, smoothing = 0.52f),
                innerRounding = CornerRounding(radius = 0.18f, smoothing = 0.50f)
            ).normalized()

            SeaGlassShapeKind.Cookie9 -> RoundedPolygon.star(
                numVerticesPerRadius = 9,
                innerRadius = 0.80f,
                rounding = CornerRounding(radius = 0.20f, smoothing = 0.50f),
                innerRounding = CornerRounding(radius = 0.16f, smoothing = 0.50f)
            ).normalized()

            SeaGlassShapeKind.Cookie12 -> RoundedPolygon.star(
                numVerticesPerRadius = 12,
                innerRadius = 0.84f,
                rounding = CornerRounding(radius = 0.15f, smoothing = 0.48f),
                innerRounding = CornerRounding(radius = 0.14f, smoothing = 0.48f)
            ).normalized()

            SeaGlassShapeKind.Clover4 -> RoundedPolygon.star(
                numVerticesPerRadius = 4,
                innerRadius = 0.48f,
                rounding = CornerRounding(radius = 0.42f, smoothing = 0.65f),
                innerRounding = CornerRounding(radius = 0.14f, smoothing = 0.35f)
            ).normalized()

            SeaGlassShapeKind.Clover8 -> RoundedPolygon.star(
                numVerticesPerRadius = 8,
                innerRadius = 0.64f,
                rounding = CornerRounding(radius = 0.26f, smoothing = 0.58f),
                innerRounding = CornerRounding(radius = 0.12f, smoothing = 0.35f)
            ).normalized()

            SeaGlassShapeKind.Flower -> RoundedPolygon.star(
                numVerticesPerRadius = 6,
                innerRadius = 0.58f,
                rounding = CornerRounding(radius = 0.34f, smoothing = 0.62f),
                innerRounding = CornerRounding(radius = 0.12f, smoothing = 0.30f)
            ).normalized()

            SeaGlassShapeKind.SoftBurst -> RoundedPolygon.star(
                numVerticesPerRadius = 10,
                innerRadius = 0.72f,
                rounding = CornerRounding(radius = 0.12f, smoothing = 0.38f),
                innerRounding = CornerRounding(radius = 0.12f, smoothing = 0.38f)
            ).normalized()

            SeaGlassShapeKind.Gem -> RoundedPolygon(
                numVertices = 8,
                rounding = CornerRounding(radius = 0.20f, smoothing = 0.32f)
            ).normalized()

            SeaGlassShapeKind.Heart -> buildHeartPolygon().normalized()

            SeaGlassShapeKind.Diamond -> RoundedPolygon.star(
                numVerticesPerRadius = 4,
                innerRadius = 0.78f,
                rounding = CornerRounding(radius = 0.24f, smoothing = 0.50f),
                innerRounding = CornerRounding(radius = 0.32f, smoothing = 0.55f)
            ).normalized()

            SeaGlassShapeKind.Pentagon -> RoundedPolygon(
                numVertices = 5,
                rounding = CornerRounding(radius = 0.28f, smoothing = 0.52f)
            ).normalized()

            SeaGlassShapeKind.Triangle -> RoundedPolygon(
                numVertices = 3,
                rounding = CornerRounding(radius = 0.26f, smoothing = 0.48f)
            ).normalized()

            SeaGlassShapeKind.Slanted -> RoundedPolygon(
                vertices = floatArrayOf(
                    -0.78f, -0.95f,
                    0.96f, -0.82f,
                    0.78f, 0.95f,
                    -0.96f, 0.82f
                ),
                rounding = CornerRounding(radius = 0.32f, smoothing = 0.55f)
            ).normalized()
        }
    }

    private fun buildHeartPolygon(): RoundedPolygon {
        val vertices = floatArrayOf(
            0.0f, -0.58f,   // Top center cleft
            0.55f, -0.98f,  // Top-right lobe peak
            0.98f, -0.35f,  // Right outer flank
            0.0f, 0.96f,    // Bottom tip
            -0.98f, -0.35f, // Left outer flank
            -0.55f, -0.98f  // Top-left lobe peak
        )
        val roundings = listOf(
            CornerRounding(radius = 0.14f, smoothing = 0.30f),
            CornerRounding(radius = 0.44f, smoothing = 0.65f),
            CornerRounding(radius = 0.36f, smoothing = 0.55f),
            CornerRounding(radius = 0.18f, smoothing = 0.35f),
            CornerRounding(radius = 0.36f, smoothing = 0.55f),
            CornerRounding(radius = 0.44f, smoothing = 0.65f)
        )
        return RoundedPolygon(
            vertices = vertices,
            perVertexRounding = roundings,
            centerX = 0f,
            centerY = 0f
        )
    }

    private fun obtainCachedMorph(
        startKind: SeaGlassShapeKind,
        endKind: SeaGlassShapeKind,
        aspectRatio: Float
    ): CachedMorphEntry {
        val qAspect = (aspectRatio.coerceIn(0.4f, 4.0f) * 20f).roundToInt()
        val key = MorphCacheKey(startKind, endKind, qAspect)
        morphCache[key]?.let { return it }

        val aspect = qAspect / 20f
        val startPoly = obtainNormalizedPolygon(startKind, aspect)
        val endPoly = obtainNormalizedPolygon(endKind, aspect)
        val morph = Morph(startPoly, endPoly)

        val p0 = Path()
        morph.toPath(progress = 0f, path = p0)
        val b0 = RectF()
        p0.computeBounds(b0, true)

        val p1 = Path()
        morph.toPath(progress = 1f, path = p1)
        val b1 = RectF()
        p1.computeBounds(b1, true)

        val entry = CachedMorphEntry(morph = morph, startBounds = b0, endBounds = b1)
        morphCache[key] = entry
        return entry
    }

    /**
     * Populates [outPath] with the morphed contour between [startKind] and [endKind] at
     * [progress] (supporting **under-damped spring overshoot** `progress in -0.28f..1.28f`!).
     *
     * Uses cached [Morph] instances so zero feature-matching allocations occur during animation.
     */
    fun buildMorphedPolygonPath(
        startKind: SeaGlassShapeKind,
        endKind: SeaGlassShapeKind,
        progress: Float,
        widthPx: Float,
        heightPx: Float,
        outPath: Path
    ) {
        val aspect = (widthPx / max(heightPx, 1f)).coerceIn(0.4f, 4.0f)
        val cached = obtainCachedMorph(startKind, endKind, aspect)

        // Allow elastic spatial spring overshoot (-0.25..+1.25) so lobes/vertices physically bounce!
        val elasticProgress = progress.coerceIn(-0.25f, 1.25f)
        outPath.reset()
        cached.morph.toPath(progress = elasticProgress, path = outPath)

        // Use the clamped [0..1] rest reference bounds for the scale-to-fit transform so
        // elastic vertex overshoot (> 1.0 or < 0.0) genuinely expands/flexes past the rest frame!
        val refT = progress.coerceIn(0f, 1f)
        val refLeft = cached.startBounds.left + (cached.endBounds.left - cached.startBounds.left) * refT
        val refTop = cached.startBounds.top + (cached.endBounds.top - cached.startBounds.top) * refT
        val refRight = cached.startBounds.right + (cached.endBounds.right - cached.startBounds.right) * refT
        val refBottom = cached.startBounds.bottom + (cached.endBounds.bottom - cached.startBounds.bottom) * refT

        scratchBounds.set(refLeft, refTop, refRight, refBottom)
        if (scratchBounds.width() > 1e-4f && scratchBounds.height() > 1e-4f) {
            // Inset by 4% when overshooting so bouncy lobes have headroom inside the view box
            val overshootMarginX = widthPx * 0.04f * abs(elasticProgress - refT)
            val overshootMarginY = heightPx * 0.04f * abs(elasticProgress - refT)
            targetBounds.set(
                overshootMarginX,
                overshootMarginY,
                widthPx - overshootMarginX,
                heightPx - overshootMarginY
            )
            scratchMatrix.reset()
            scratchMatrix.setRectToRect(scratchBounds, targetBounds, Matrix.ScaleToFit.FILL)
            outPath.transform(scratchMatrix)
        }
    }

    /**
     * Morphs smoothly through an ordered sequence of $N \ge 2$ [shapes] driven by a single
     * continuous float [continuousIndex] (e.g. `0.0 -> 1.0 -> 2.0 -> 3.0`), preserving
     * spring overshoot at the target shape index.
     */
    fun buildSequenceMorphedPolygonPath(
        shapes: List<SeaGlassShapeKind>,
        continuousIndex: Float,
        widthPx: Float,
        heightPx: Float,
        outPath: Path
    ) {
        require(shapes.isNotEmpty()) { "shapes list must not be empty" }
        if (shapes.size == 1) {
            buildMorphedPolygonPath(shapes[0], shapes[0], 0f, widthPx, heightPx, outPath)
            return
        }

        val maxIdx = shapes.size - 1
        val baseIdx = floor(continuousIndex.coerceIn(0f, (maxIdx - 1).toFloat())).toInt()
            .coerceIn(0, maxIdx - 1)
        val localProgress = continuousIndex - baseIdx.toFloat()

        buildMorphedPolygonPath(
            startKind = shapes[baseIdx],
            endKind = shapes[baseIdx + 1],
            progress = localProgress,
            widthPx = widthPx,
            heightPx = heightPx,
            outPath = outPath
        )
    }

    /**
     * Evaluates the normalized polar radius scale $r(\theta) \in [0.45, 1.05]$ of [kind]
     * at angle [angleRad] $\in [-\pi, \pi]$ (or a morphed blend between [startKind] and [endKind]).
     *
     * Used by [SeaGlassSdfEngine] so **any Material 3 Expressive polygon shape can coalesce
     * with neighboring droplets via `sminCubic` liquid bridges**!
     */
    fun evaluatePolarRadiusScale(
        startKind: SeaGlassShapeKind,
        endKind: SeaGlassShapeKind = startKind,
        progress: Float = 0f,
        angleRad: Float
    ): Float {
        val lutA = obtainPolarLut(startKind)
        val rA = samplePolarLut(lutA, angleRad)
        if (startKind == endKind || abs(progress) < 1e-3f) {
            return rA
        }
        val lutB = obtainPolarLut(endKind)
        val rB = samplePolarLut(lutB, angleRad)
        return (rA + (rB - rA) * progress.coerceIn(-0.25f, 1.25f)).coerceIn(0.35f, 1.15f)
    }

    private fun obtainPolarLut(kind: SeaGlassShapeKind): FloatArray {
        polarLutCache[kind]?.let { return it }
        val poly = obtainNormalizedPolygon(kind, 1f)
        val cubics = poly.cubics

        // Sample 512 points along the normalized polygon boundary [0..1] centered at (0.5, 0.5)
        val lut = FloatArray(POLAR_LUT_SIZE) { 1.0f }
        val weightSum = FloatArray(POLAR_LUT_SIZE)

        for (cubic in cubics) {
            for (s in 0..15) {
                val t = s / 16f
                val pt = pointOnCubic(cubic, t)
                val dx = (pt.x - 0.5f) * 2f
                val dy = (pt.y - 0.5f) * 2f
                val radius = sqrt(dx * dx + dy * dy).coerceIn(0.35f, 1.15f)
                val angle = kotlin.math.atan2(dy, dx) // [-PI..PI]
                val normAngle = ((angle + PI) / (2.0 * PI)).toFloat().coerceIn(0f, 0.9999f)
                val bin = (normAngle * POLAR_LUT_SIZE).toInt().coerceIn(0, POLAR_LUT_SIZE - 1)
                if (weightSum[bin] == 0f) {
                    lut[bin] = radius
                } else {
                    lut[bin] = (lut[bin] * weightSum[bin] + radius) / (weightSum[bin] + 1f)
                }
                weightSum[bin] += 1f
            }
        }

        // Fill any empty bins by circular interpolation and apply a 3-tap circular box filter
        for (pass in 0..1) {
            for (i in 0 until POLAR_LUT_SIZE) {
                val prev = (i - 1 + POLAR_LUT_SIZE) % POLAR_LUT_SIZE
                val next = (i + 1) % POLAR_LUT_SIZE
                lut[i] = lut[i] * 0.5f + (lut[prev] + lut[next]) * 0.25f
            }
        }

        polarLutCache[kind] = lut
        return lut
    }

    private fun samplePolarLut(lut: FloatArray, angleRad: Float): Float {
        var norm = ((angleRad + PI) / (2.0 * PI)).toFloat()
        norm -= floor(norm)
        val pos = norm * POLAR_LUT_SIZE
        val i0 = pos.toInt() % POLAR_LUT_SIZE
        val i1 = (i0 + 1) % POLAR_LUT_SIZE
        val frac = pos - floor(pos)
        return lut[i0] + (lut[i1] - lut[i0]) * frac
    }

    private fun pointOnCubic(c: Cubic, t: Float): PointF {
        val u = 1f - t
        val u2 = u * u
        val u3 = u2 * u
        val t2 = t * t
        val t3 = t2 * t
        val x = u3 * c.anchor0X +
            3f * u2 * t * c.control0X +
            3f * u * t2 * c.control1X +
            t3 * c.anchor1X
        val y = u3 * c.anchor0Y +
            3f * u2 * t * c.control0Y +
            3f * u * t2 * c.control1Y +
            t3 * c.anchor1Y
        return PointF(x, y)
    }

    /**
     * Backwards-compatible alias for [obtainNormalizedPolygon].
     */
    fun createNormalizedPolygon(
        kind: SeaGlassShapeKind,
        aspectRatio: Float = 1f
    ): RoundedPolygon = obtainNormalizedPolygon(kind, aspectRatio)

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
