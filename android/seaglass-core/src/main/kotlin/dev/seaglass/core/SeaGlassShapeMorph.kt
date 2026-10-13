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

/**
 * Defines a single stage in a Material 3 Expressive × Liquid Sea Glass shape choreography.
 *
 * @property shape Target [SeaGlassShapeKind] geometry at this stage.
 * @property scale Relative size multiplier (e.g. `0.70f` for a compact small circle, `1.28f` for a large expanded circle/flower).
 * @property aspectRatio Width-to-height ratio (`1.0f` equiaxed, `1.55f` wide capsule/squircle).
 * @property rotationDegrees Cumulative angular spin in degrees at this stage.
 * @property viaCircleBridge When true and morphing between two non-circular lobed shapes (e.g. `Flower -> Cookie9`),
 *   morphs down into a taut surface-tension `Circle` in the first half and blooms back out into [shape]
 *   with elastic spring overshoot in the second half.
 * @property label Optional human-readable label for UI display.
 */
data class SeaGlassShapeStage(
    val shape: SeaGlassShapeKind,
    val scale: Float = 1.0f,
    val aspectRatio: Float = 1.0f,
    val rotationDegrees: Float = 0f,
    val viaCircleBridge: Boolean = true,
    val label: String = shape.shortLabel
)

/**
 * Instantaneous spatial transform (scale, aspect, rotation, and optical energy) evaluated
 * during a choreographed shape transition.
 */
data class ChoreographedShapeTransform(
    val scaleX: Float,
    val scaleY: Float,
    val rotationDegrees: Float,
    val morphEnergy: Float,
    val activeStartShape: SeaGlassShapeKind,
    val activeEndShape: SeaGlassShapeKind,
    val activeSubProgress: Float
)

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
     * Cubic Hermite S-curve (`3u^2 - 2u^3`) on `[0..1]`, providing zero initial and terminal
     * slope (`f'(0) = 0`, `f'(1) = 0`) for smooth ease-in / ease-out transitions.
     */
    @JvmStatic
    fun sCurve(t: Float): Float {
        val u = t.coerceIn(0f, 1f)
        return u * u * (3f - 2f * u)
    }

    /**
     * Quintic C2-continuous S-curve (`6u^5 - 15u^4 + 10u^3`) on `[0..1]`, with zero first AND
     * second derivatives at both endpoints (`f'(0) = f''(0) = f'(1) = f''(1) = 0`) so inertia
     * builds and settles without initial jerk.
     */
    @JvmStatic
    fun smootherStep(t: Float): Float {
        val u = t.coerceIn(0f, 1f)
        return u * u * u * (u * (u * 6f - 15f) + 10f)
    }

    /**
     * Physics-based S-curve (ease-in / ease-out) transfer function:
     * - Inside `[0..1]`, blends linear momentum (`0.52 * u`) with Cubic Hermite smoothstep
     *   (`0.48 * u^2 * (3 - 2u)`). Because [physicsSCurve] is driven by a 2nd-order physical
     *   spring `s(t)` (which already starts at `O(t^2)` from rest), a pure cubic/quintic curve
     *   would compound to `O(t^4..t^6)` at the start and compress the middle into a 3-frame cliff.
     *   This damped Hermite S-curve keeps initial motion truly quadratic (`O(t^2)` ease-in from
     *   Frame 1), limits peak midpoint slope to `1.24x` (~12% per 60Hz frame / 6% per 120Hz frame),
     *   and smoothly cushions the ease-out landing.
     * - Symmetric under reversal (`physicsSCurve(1 - u) == 1 - physicsSCurve(u)`), so closing
     *   morphs (`1 -> 0`) get the exact same ease-in detachment and ease-out landing as opening
     *   morphs (`0 -> 1`).
     * - Outside `[0..1]`, preserves damped physical spring overshoot/undershoot scaled by
     *   [overshootGain].
     */
    @JvmStatic
    @JvmOverloads
    fun physicsSCurve(progress: Float, overshootGain: Float = 0.52f): Float {
        return when {
            progress < 0f -> progress * overshootGain
            progress > 1f -> 1f + (progress - 1f) * overshootGain
            else -> {
                val h3 = progress * progress * (3f - 2f * progress)
                0.52f * progress + 0.48f * h3
            }
        }
    }

    /**
     * Evaluates a physics-based S-curve over the sub-window `[start..end]` of [progress],
     * eliminating the C1 velocity kinks of linear window slicing `((progress - start) / (end - start)).coerceIn(0f, 1f)`.
     */
    @JvmStatic
    @JvmOverloads
    fun sCurveWindow(
        progress: Float,
        start: Float,
        end: Float,
        overshootGain: Float = 0f
    ): Float {
        val span = (end - start).coerceAtLeast(1e-4f)
        val raw = (progress - start) / span
        return physicsSCurve(raw, overshootGain = overshootGain)
    }

    /**
     * Eases from `0` to `1` over `[0..until]` along a physics S-curve and holds at `1`
     * while still carrying any spring overshoot past `progress > 1f` scaled by [overshootGain].
     */
    @JvmStatic
    @JvmOverloads
    fun sCurveLead(
        progress: Float,
        until: Float,
        overshootGain: Float = 0.45f
    ): Float {
        val base = sCurveWindow(progress, 0f, until.coerceAtLeast(1e-4f), overshootGain = 0f)
        val excess = if (progress > 1f) (progress - 1f) * overshootGain else 0f
        return base + excess
    }

    /**
     * Smooth C2 bell curve peaking at `1.0` when `progress == 0.5` and easing to `0.0` at both
     * endpoints (`0.0` and `1.0`), used for in-flight optical energy, liquid meniscus swell,
     * and temporal de-aging.
     */
    @JvmStatic
    fun sCurveBell(progress: Float): Float {
        val s = sCurve(progress)
        return (4f * s * (1f - s)).coerceIn(0f, 1f)
    }

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

        // Shape progress with a physics S-curve while preserving elastic spatial spring overshoot (-0.25..+1.25)!
        val elasticProgress = physicsSCurve(progress, overshootGain = 0.72f).coerceIn(-0.25f, 1.25f)
        outPath.reset()
        cached.morph.toPath(progress = elasticProgress, path = outPath)

        // Use the S-curve [0..1] rest reference bounds for the scale-to-fit transform so
        // elastic vertex overshoot (> 1.0 or < 0.0) genuinely expands/flexes past the rest frame!
        val refT = sCurve(progress)
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
     * Evaluates a two-phase Material 3 Expressive **Morph-In (to Circle) -> Morph-Out (to Target)**
     * choreography between [startStage] and [endStage] at spring [progress] (supporting elastic
     * overshoot beyond `[0..1]`).
     *
     * When [SeaGlassShapeStage.viaCircleBridge] is enabled and both stages are lobed/polygonal
     * (`startStage.shape != Circle && endStage.shape != Circle && startStage.shape != endStage.shape`):
     * - **Phase 1 (`p in 0.0..0.42`)**: Pulls the starting petals (`Flower`, `Cookie9`, etc.)
     *   inward into a taut, compact `Circle` droplet while dipping scale by `-14%`.
     * - **Phase 2 (`p > 0.42`, including spring overshoot `p > 1.0`)**: Blooms outward from the
     *   `Circle` into `endStage.shape` and `endStage.scale`, so all new petals/lobes emerge
     *   symmetrically and overshoot with lively M3 Expressive bounce!
     */
    fun evaluateChoreographedTransform(
        startStage: SeaGlassShapeStage,
        endStage: SeaGlassShapeStage,
        progress: Float
    ): ChoreographedShapeTransform {
        val clamped = progress.coerceIn(0f, 1f)
        val overshoot = progress - clamped

        val useCircleMidpoint = endStage.viaCircleBridge &&
            startStage.shape != endStage.shape &&
            startStage.shape != SeaGlassShapeKind.Circle &&
            endStage.shape != SeaGlassShapeKind.Circle

        val activeStart: SeaGlassShapeKind
        val activeEnd: SeaGlassShapeKind
        val subProgress: Float

        if (useCircleMidpoint) {
            val pivot = 0.42f
            if (clamped < pivot && progress <= pivot) {
                activeStart = startStage.shape
                activeEnd = SeaGlassShapeKind.Circle
                val rawU = (progress / pivot).coerceIn(-0.25f, 1.0f)
                // Physics S-curve ease-in/ease-out toward the circle pinch point
                subProgress = physicsSCurve(rawU, overshootGain = 0.55f).coerceIn(-0.25f, 1.0f)
            } else {
                activeStart = SeaGlassShapeKind.Circle
                activeEnd = endStage.shape
                // Physics S-curve bloom out from Circle while preserving spring overshoot
                val rawOut = (progress - pivot) / (1f - pivot)
                subProgress = physicsSCurve(rawOut, overshootGain = 0.75f).coerceIn(0f, 1.28f)
            }
        } else {
            activeStart = startStage.shape
            activeEnd = endStage.shape
            subProgress = physicsSCurve(progress, overshootGain = 0.72f).coerceIn(-0.25f, 1.28f)
        }

        val shapedProgress = physicsSCurve(progress, overshootGain = 0.65f)
        val clampedSCurve = sCurve(clamped)

        // Base scale interpolation + mid-morph "gather & bloom" pulse + elastic spring overshoot
        val baseScale = startStage.scale + (endStage.scale - startStage.scale) * shapedProgress
        val gatherDip = if (useCircleMidpoint) {
            -0.15f * sin(clampedSCurve * PI).toFloat()
        } else {
            -0.07f * sin(clampedSCurve * PI).toFloat()
        }
        val effectiveScale = (baseScale + gatherDip + overshoot * 0.18f).coerceIn(0.42f, 1.65f)

        val currentAspect = (startStage.aspectRatio +
            (endStage.aspectRatio - startStage.aspectRatio) * shapedProgress).coerceIn(0.55f, 2.2f)

        val scaleX = effectiveScale * sqrt(currentAspect)
        val scaleY = effectiveScale / sqrt(currentAspect)

        val rotation = startStage.rotationDegrees +
            (endStage.rotationDegrees - startStage.rotationDegrees) * shapedProgress

        val energy = (sin(clampedSCurve * PI).toFloat() + abs(overshoot) * 2.2f).coerceIn(0f, 1f)

        return ChoreographedShapeTransform(
            scaleX = scaleX,
            scaleY = scaleY,
            rotationDegrees = rotation,
            morphEnergy = energy,
            activeStartShape = activeStart,
            activeEndShape = activeEnd,
            activeSubProgress = subProgress
        )
    }

    /**
     * Populates [outPath] with the choreographed contour between [startStage] and [endStage]
     * at spring [progress].
     */
    fun buildChoreographedPolygonPath(
        startStage: SeaGlassShapeStage,
        endStage: SeaGlassShapeStage,
        progress: Float,
        widthPx: Float,
        heightPx: Float,
        outPath: Path
    ) {
        val transform = evaluateChoreographedTransform(startStage, endStage, progress)
        buildMorphedPolygonPath(
            startKind = transform.activeStartShape,
            endKind = transform.activeEndShape,
            progress = transform.activeSubProgress,
            widthPx = widthPx,
            heightPx = heightPx,
            outPath = outPath
        )
    }

    /**
     * Default 8-stage Material 3 Expressive playful shape choreography tour:
     * Morphs from `Flower` -> `Small Circle` -> `Cookie 9` -> `Large Circle` -> `Clover 4` ->
     * `Wide Capsule` -> `Sunny 8` -> `Heart` -> `Squircle`!
     */
    val DefaultPlayfulChoreography: List<SeaGlassShapeStage> = listOf(
        SeaGlassShapeStage(
            shape = SeaGlassShapeKind.Flower,
            scale = 1.06f,
            rotationDegrees = 0f,
            viaCircleBridge = true,
            label = "Flower Bloom"
        ),
        SeaGlassShapeStage(
            shape = SeaGlassShapeKind.Circle,
            scale = 0.72f,
            rotationDegrees = 45f,
            viaCircleBridge = false,
            label = "Small Droplet"
        ),
        SeaGlassShapeStage(
            shape = SeaGlassShapeKind.Cookie9,
            scale = 1.14f,
            rotationDegrees = 95f,
            viaCircleBridge = true,
            label = "Cookie 9"
        ),
        SeaGlassShapeStage(
            shape = SeaGlassShapeKind.Circle,
            scale = 1.24f,
            rotationDegrees = 140f,
            viaCircleBridge = false,
            label = "Large Orb"
        ),
        SeaGlassShapeStage(
            shape = SeaGlassShapeKind.Clover4,
            scale = 1.08f,
            rotationDegrees = 195f,
            viaCircleBridge = true,
            label = "4-Leaf Clover"
        ),
        SeaGlassShapeStage(
            shape = SeaGlassShapeKind.Capsule,
            scale = 0.88f,
            aspectRatio = 1.48f,
            rotationDegrees = 180f,
            viaCircleBridge = false,
            label = "Liquid Pill"
        ),
        SeaGlassShapeStage(
            shape = SeaGlassShapeKind.Sunny,
            scale = 1.15f,
            rotationDegrees = 240f,
            viaCircleBridge = true,
            label = "Sunny Star"
        ),
        SeaGlassShapeStage(
            shape = SeaGlassShapeKind.Heart,
            scale = 1.10f,
            rotationDegrees = 360f,
            viaCircleBridge = true,
            label = "Heart Gem"
        )
    )

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
        val shapedP = physicsSCurve(progress, overshootGain = 0.65f).coerceIn(-0.25f, 1.25f)
        return (rA + (rB - rA) * shapedP).coerceIn(0.35f, 1.15f)
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
     * Computes the dynamic **Liquid Glass Flex & Temporal De-Aging Material** during an animation or transition:
     * - While in flight (`totalEnergy > 0`), the material **de-ages** (`deaged(totalEnergy)`): it sheds
     *   its coarse sand-frosted texture (`coarseness` dissolves toward `0`), clarifies into malleable
     *   liquid glass (`transparency` rises, `frostRadiusDp` tightens), and flexes its refractive lens
     *   (`ior`, `chromaticDispersion`, `thicknessDp`).
     * - As the animation settles (`progress -> 0` or `progress -> 1`), the glass solidifies and
     *   regains its full sand-frosted surface treatment.
     */
    fun computeDynamicFlexMaterial(
        startMaterial: SeaGlassMaterial,
        endMaterial: SeaGlassMaterial,
        progress: Float,
        velocityPxPerSec: Float = 0f,
        areaExpansionRatio: Float = 1f
    ): SeaGlassMaterial {
        val p = sCurve(progress)
        val base = SeaGlassMaterial.lerp(startMaterial, endMaterial, p)

        val morphEnergy = sCurveBell(progress)
        val velocityEnergy = (abs(velocityPxPerSec) / 1800f).coerceIn(0f, 1f)
        val totalEnergy = max(morphEnergy, velocityEnergy * 0.75f).coerceIn(0f, 1f)

        val scaleFactor = sqrt(areaExpansionRatio.coerceIn(0.5f, 4.0f))
        val melt = (totalEnergy * (1.35f - 0.35f * totalEnergy)).coerceIn(0f, 1f)
        val targetClearTransparency = max(base.transparency, 0.92f)

        return base.copy(
            coarseness = (base.coarseness * (1f - 0.88f * melt)).coerceIn(0f, 1f),
            transparency = (base.transparency + (targetClearTransparency - base.transparency) * 0.80f * melt)
                .coerceIn(0f, 1f),
            frostRadiusDp = (base.frostRadiusDp * (1f - 0.82f * melt) + 2.0f * melt).coerceAtLeast(0f),
            ior = (base.ior + 0.10f * totalEnergy + 0.04f * (scaleFactor - 1f)).coerceIn(1.0f, 1.95f),
            chromaticDispersion = (base.chromaticDispersion + 0.22f * totalEnergy).coerceIn(0f, 1f),
            rimChalkiness = (base.rimChalkiness + 0.16f * totalEnergy).coerceIn(0f, 1f),
            thicknessDp = (base.thicknessDp * (1f + 0.20f * totalEnergy) * scaleFactor.coerceIn(0.85f, 1.35f))
                .coerceIn(4f, 36f)
        )
    }
}
