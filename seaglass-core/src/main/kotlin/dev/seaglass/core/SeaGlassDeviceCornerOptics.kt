package dev.seaglass.core

import android.content.Context
import android.content.res.Resources
import android.os.Build
import android.view.RoundedCorner
import android.view.WindowInsets
import android.view.WindowManager
import java.util.Locale
import kotlin.math.max
import kotlin.math.min
import kotlin.math.sqrt

/**
 * Source from which the physical device display corner radii were resolved.
 */
enum class SeaGlassCornerDetectionSource(val label: String) {
    /** Pulled live from Android 12+ (API 31+) `WindowInsets.getRoundedCorner(position)`. */
    WINDOW_INSETS_LIVE("WindowInsets.getRoundedCorner (Live OS)"),

    /** Pulled live from Android 12+ (API 31+) `Display.getRoundedCorner(position)`. */
    DISPLAY_HARDWARE_API("Display.getRoundedCorner (Hardware API)"),

    /** Pulled from AOSP SystemUI internal `android:dimen/rounded_corner_radius*` resources. */
    SYSTEM_RESOURCE_DIMEN("SystemUI dimen/rounded_corner_radius"),

    /** Matched against the calibrated `Build.MODEL` / `Build.DEVICE` hardware database. */
    DEVICE_MODEL_CATALOG("Calibrated Device Hardware Catalog"),

    /** Explicitly selected hardware simulation profile. */
    PROFILE_OVERRIDE("Simulated Hardware Bezel Profile")
}

/**
 * Calibrated physical display corner-radius profiles for real-world flagship devices and form factors.
 */
enum class SeaGlassDeviceHardwareProfile(
    val id: String,
    val displayName: String,
    val shortLabel: String,
    val topLeftDp: Float,
    val topRightDp: Float,
    val bottomRightDp: Float,
    val bottomLeftDp: Float
) {
    AUTO_DETECT(
        id = "auto",
        displayName = "Live Hardware Auto-Detect",
        shortLabel = "Auto",
        topLeftDp = 0f,
        topRightDp = 0f,
        bottomRightDp = 0f,
        bottomLeftDp = 0f
    ),
    PIXEL_9_PRO(
        id = "pixel_9_pro",
        displayName = "Pixel 9 Pro / 10 Pro (46dp Oval Bezel)",
        shortLabel = "Pixel 46dp",
        topLeftDp = 46f,
        topRightDp = 46f,
        bottomRightDp = 46f,
        bottomLeftDp = 46f
    ),
    IPHONE_16_PRO(
        id = "iphone_16_pro",
        displayName = "iPhone 16 Pro / 17 Pro Reference (55dp Squircle)",
        shortLabel = "55dp Pro",
        topLeftDp = 55f,
        topRightDp = 55f,
        bottomRightDp = 55f,
        bottomLeftDp = 55f
    ),
    GALAXY_S25_ULTRA(
        id = "galaxy_s25_ultra",
        displayName = "Galaxy S25 Ultra (18dp Architectural)",
        shortLabel = "Ultra 18dp",
        topLeftDp = 18f,
        topRightDp = 18f,
        bottomRightDp = 18f,
        bottomLeftDp = 18f
    ),
    GALAXY_S25_PLUS(
        id = "galaxy_s25_plus",
        displayName = "Galaxy S25+ / OnePlus 13 (38dp Rounded)",
        shortLabel = "S25+ 38dp",
        topLeftDp = 38f,
        topRightDp = 38f,
        bottomRightDp = 38f,
        bottomLeftDp = 38f
    ),
    FOLD_ASYMMETRIC(
        id = "fold_asymmetric",
        displayName = "Pixel Fold Cover (14dp Hinge / 44dp Outer)",
        shortLabel = "Fold 14/44",
        topLeftDp = 14f,
        topRightDp = 44f,
        bottomRightDp = 44f,
        bottomLeftDp = 14f
    );
}

/**
 * Controls how a SeaGlass element adapts its 4 corner radii relative to the physical device
 * bezel corners and/or its enclosing page/card container corners.
 */
enum class SeaGlassCornerAdaptationMode {
    /**
     * Smoothly morphs each individual corner `(TL, TR, BR, BL)` toward the concentric target
     * $R_{\text{concentric}} = \max(r_{\min}, R_{\text{outer}} - \text{inset})$ as that corner
     * approaches a device or page corner ($d < D_{\text{influence}}$).
     */
    CONCENTRIC_PROXIMITY,

    /**
     * Explicitly embeds the element into the nearest corner(s): outer corner(s) lock to the
     * concentric bezel/page radius $R_{\text{outer}} - \text{inset}$ while inner page-facing
     * corners maintain a tighter architectural radius.
     */
    FLUSH_CORNER_EMBED,

    /**
     * Applies the concentric corner radius uniformly to all 4 corners when the element is near
     * a top or bottom screen edge.
     */
    UNIFORM_CONCENTRIC,

    /**
     * Disables device/page corner adaptation and uses raw [SeaGlassMaterial.cornerRadiusDp].
     */
    FIXED
}

/**
 * Per-corner radius quad in pixels (`topLeft`, `topRight`, `bottomRight`, `bottomLeft`).
 */
data class SeaGlassCornerRadiiPx(
    val topLeftPx: Float,
    val topRightPx: Float,
    val bottomRightPx: Float,
    val bottomLeftPx: Float
) {
    val maxRadiusPx: Float
        get() = max(max(topLeftPx, topRightPx), max(bottomRightPx, bottomLeftPx))

    val minRadiusPx: Float
        get() = min(min(topLeftPx, topRightPx), min(bottomRightPx, bottomLeftPx))

    val isAsymmetric: Boolean
        get() = (maxRadiusPx - minRadiusPx) > 1.5f

    companion object {
        fun uniform(radiusPx: Float) = SeaGlassCornerRadiiPx(radiusPx, radiusPx, radiusPx, radiusPx)
    }
}

/**
 * Resolved physical display corner geometry for the active Android device or window.
 */
data class SeaGlassDeviceCornerSpec(
    val topLeftPx: Float,
    val topRightPx: Float,
    val bottomRightPx: Float,
    val bottomLeftPx: Float,
    val density: Float,
    val source: SeaGlassCornerDetectionSource,
    val deviceDescription: String
) {
    val topLeftDp: Float get() = topLeftPx / max(density, 0.1f)
    val topRightDp: Float get() = topRightPx / max(density, 0.1f)
    val bottomRightDp: Float get() = bottomRightPx / max(density, 0.1f)
    val bottomLeftDp: Float get() = bottomLeftPx / max(density, 0.1f)

    fun toRadiiPx(): SeaGlassCornerRadiiPx = SeaGlassCornerRadiiPx(
        topLeftPx = topLeftPx,
        topRightPx = topRightPx,
        bottomRightPx = bottomRightPx,
        bottomLeftPx = bottomLeftPx
    )
}

/**
 * Multi-tier physical display corner radius detector and concentric corner solver.
 *
 * Detection cascade:
 * 1. **Live OS `WindowInsets.getRoundedCorner(position)` (API 31+)**: Exact per-corner pixel radius
 *    accounting for current window posture, rotation, and split-screen state.
 * 2. **Live OS `Display.getRoundedCorner(position)` (API 31+)**: Direct hardware display query
 *    when `WindowInsets` is not yet attached or reports `0`.
 * 3. **AOSP SystemUI Internal Resource (`android:dimen/rounded_corner_radius*`)**: Queries the
 *    same framework resource IDs (`rounded_corner_radius`, `rounded_corner_radius_top`,
 *    `rounded_corner_radius_bottom`) used by Android SystemUI to draw screen corners.
 * 4. **Calibrated Hardware Device Catalog (`Build.MODEL` / `Build.DEVICE` + Aspect Ratio)**:
 *    Provides accurate physical bezel radii on emulators (`radius=0` in `dumpsys window`) and
 *    older API 26–30 devices.
 */
object SeaGlassCornerAdaptationSolver {

    /**
     * Detects the physical display corner radii for the given [context] and optional [rootWindowInsets].
     */
    fun detectDeviceCorners(
        context: Context?,
        rootWindowInsets: WindowInsets? = null,
        overrideProfile: SeaGlassDeviceHardwareProfile = SeaGlassDeviceHardwareProfile.AUTO_DETECT
    ): SeaGlassDeviceCornerSpec {
        val density = context?.resources?.displayMetrics?.density ?: 2.75f
        val modelDesc = "${Build.MANUFACTURER} ${Build.MODEL}".trim()

        // 0. Explicit hardware profile override (for design previews & multi-device testing)
        if (overrideProfile != SeaGlassDeviceHardwareProfile.AUTO_DETECT) {
            return SeaGlassDeviceCornerSpec(
                topLeftPx = overrideProfile.topLeftDp * density,
                topRightPx = overrideProfile.topRightDp * density,
                bottomRightPx = overrideProfile.bottomRightDp * density,
                bottomLeftPx = overrideProfile.bottomLeftDp * density,
                density = density,
                source = SeaGlassCornerDetectionSource.PROFILE_OVERRIDE,
                deviceDescription = overrideProfile.displayName
            )
        }

        // 1. Live Android 12+ (API 31+) WindowInsets.getRoundedCorner(position)
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S && rootWindowInsets != null) {
            val tl = rootWindowInsets.getRoundedCorner(RoundedCorner.POSITION_TOP_LEFT)?.radius ?: 0
            val tr = rootWindowInsets.getRoundedCorner(RoundedCorner.POSITION_TOP_RIGHT)?.radius ?: 0
            val br = rootWindowInsets.getRoundedCorner(RoundedCorner.POSITION_BOTTOM_RIGHT)?.radius ?: 0
            val bl = rootWindowInsets.getRoundedCorner(RoundedCorner.POSITION_BOTTOM_LEFT)?.radius ?: 0
            if (tl > 0 || tr > 0 || br > 0 || bl > 0) {
                val fallback = max(max(tl, tr), max(br, bl)).toFloat()
                return SeaGlassDeviceCornerSpec(
                    topLeftPx = if (tl > 0) tl.toFloat() else fallback,
                    topRightPx = if (tr > 0) tr.toFloat() else fallback,
                    bottomRightPx = if (br > 0) br.toFloat() else fallback,
                    bottomLeftPx = if (bl > 0) bl.toFloat() else fallback,
                    density = density,
                    source = SeaGlassCornerDetectionSource.WINDOW_INSETS_LIVE,
                    deviceDescription = modelDesc
                )
            }
        }

        // 2. Live Android 12+ (API 31+) Display.getRoundedCorner(position)
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S && context != null) {
            val display = try {
                context.display
            } catch (_: Throwable) {
                @Suppress("DEPRECATION")
                (context.getSystemService(Context.WINDOW_SERVICE) as? WindowManager)?.defaultDisplay
            }
            if (display != null) {
                val tl = display.getRoundedCorner(RoundedCorner.POSITION_TOP_LEFT)?.radius ?: 0
                val tr = display.getRoundedCorner(RoundedCorner.POSITION_TOP_RIGHT)?.radius ?: 0
                val br = display.getRoundedCorner(RoundedCorner.POSITION_BOTTOM_RIGHT)?.radius ?: 0
                val bl = display.getRoundedCorner(RoundedCorner.POSITION_BOTTOM_LEFT)?.radius ?: 0
                if (tl > 0 || tr > 0 || br > 0 || bl > 0) {
                    val fallback = max(max(tl, tr), max(br, bl)).toFloat()
                    return SeaGlassDeviceCornerSpec(
                        topLeftPx = if (tl > 0) tl.toFloat() else fallback,
                        topRightPx = if (tr > 0) tr.toFloat() else fallback,
                        bottomRightPx = if (br > 0) br.toFloat() else fallback,
                        bottomLeftPx = if (bl > 0) bl.toFloat() else fallback,
                        density = density,
                        source = SeaGlassCornerDetectionSource.DISPLAY_HARDWARE_API,
                        deviceDescription = modelDesc
                    )
                }
            }
        }

        // 3. AOSP SystemUI framework resources (android:dimen/rounded_corner_radius*)
        val sysRes = try {
            context?.resources ?: Resources.getSystem()
        } catch (_: Throwable) {
            null
        }
        if (sysRes != null) {
            val basePx = readSystemDimenPx(sysRes, "rounded_corner_radius")
            val topPx = readSystemDimenPx(sysRes, "rounded_corner_radius_top").let {
                if (it > 0f) it else basePx
            }
            val bottomPx = readSystemDimenPx(sysRes, "rounded_corner_radius_bottom").let {
                if (it > 0f) it else basePx
            }
            val adjPx = readSystemDimenPx(sysRes, "rounded_corner_radius_adjustment")
            val finalTop = (topPx - adjPx).coerceAtLeast(0f)
            val finalBottom = (bottomPx - adjPx).coerceAtLeast(0f)
            if (finalTop > 2f * density || finalBottom > 2f * density) {
                val t = if (finalTop > 0f) finalTop else finalBottom
                val b = if (finalBottom > 0f) finalBottom else finalTop
                return SeaGlassDeviceCornerSpec(
                    topLeftPx = t,
                    topRightPx = t,
                    bottomRightPx = b,
                    bottomLeftPx = b,
                    density = density,
                    source = SeaGlassCornerDetectionSource.SYSTEM_RESOURCE_DIMEN,
                    deviceDescription = modelDesc
                )
            }
        }

        // 4. Calibrated Hardware Model Catalog & Form-Factor Heuristic
        val catalogRadiiDp = lookupDeviceModelCornerRadiiDp(
            manufacturer = Build.MANUFACTURER ?: "",
            model = Build.MODEL ?: "",
            device = Build.DEVICE ?: "",
            screenWidthDp = context?.resources?.configuration?.screenWidthDp ?: 411,
            screenHeightDp = context?.resources?.configuration?.screenHeightDp ?: 891
        )
        return SeaGlassDeviceCornerSpec(
            topLeftPx = catalogRadiiDp[0] * density,
            topRightPx = catalogRadiiDp[1] * density,
            bottomRightPx = catalogRadiiDp[2] * density,
            bottomLeftPx = catalogRadiiDp[3] * density,
            density = density,
            source = SeaGlassCornerDetectionSource.DEVICE_MODEL_CATALOG,
            deviceDescription = modelDesc
        )
    }

    @Suppress("DiscouragedApi")
    private fun readSystemDimenPx(res: Resources, name: String): Float {
        return try {
            val id = res.getIdentifier(name, "dimen", "android")
            if (id != 0) res.getDimension(id).coerceAtLeast(0f) else 0f
        } catch (_: Throwable) {
            0f
        }
    }

    /**
     * Calibrated lookup table mapping device model strings & form factors to `[TL, TR, BR, BL]` in `dp`.
     */
    internal fun lookupDeviceModelCornerRadiiDp(
        manufacturer: String,
        model: String,
        device: String,
        screenWidthDp: Int = 411,
        screenHeightDp: Int = 891
    ): FloatArray {
        val key = "$manufacturer $model $device".lowercase(Locale.US)
        return when {
            // Google Pixel 9 / 10 series (deep 46dp oval corners)
            key.contains("pixel 9") || key.contains("pixel 10") ||
                key.contains("caiman") || key.contains("komodo") || key.contains("tokay") ->
                if (key.contains("fold") && screenWidthDp < 580) {
                    // Fold outer cover screen: sharper left hinge edge (14dp), rounded right bezel (44dp)
                    floatArrayOf(14f, 44f, 44f, 14f)
                } else {
                    floatArrayOf(46f, 46f, 46f, 46f)
                }

            // Google Pixel 8 series (38dp corners)
            key.contains("pixel 8") || key.contains("husky") || key.contains("shiba") ->
                floatArrayOf(38f, 38f, 38f, 38f)

            // Google Pixel 6 / 7 series (28dp corners)
            key.contains("pixel 7") || key.contains("pixel 6") ->
                floatArrayOf(28f, 28f, 28f, 28f)

            // Samsung Galaxy S Ultra series (architectural 18dp corners on S25U, 8dp on S24U)
            key.contains("ultra") || key.contains("s928") || key.contains("s938") ->
                if (key.contains("s25") || key.contains("s938")) {
                    floatArrayOf(18f, 18f, 18f, 18f)
                } else {
                    floatArrayOf(12f, 12f, 12f, 12f)
                }

            // Samsung Galaxy S24 / S25 / Plus (38dp rounded corners)
            key.contains("sm-s92") || key.contains("sm-s93") || key.contains("galaxy s2") ->
                floatArrayOf(38f, 38f, 38f, 38f)

            // Tablets / Large unfolded displays (sw >= 600dp)
            min(screenWidthDp, screenHeightDp) >= 600 ->
                floatArrayOf(28f, 28f, 28f, 28f)

            // Default modern 20:9 flagship Android phone / emulator bezel (44dp)
            else -> floatArrayOf(44f, 44f, 44f, 44f)
        }
    }

    /**
     * Solves the 4 per-corner radii (`topLeftPx`, `topRightPx`, `bottomRightPx`, `bottomLeftPx`)
     * for a UI element located at `[elementLeftPx, elementTopPx, elementRightPx, elementBottomPx]`
     * inside an enclosing container/screen of size `(containerWidthPx, containerHeightPx)` whose
     * own 4 outer corners have radii [containerCornersPx].
     *
     * ### Mathematical Model
     * 1. **Concentric Curvature Law**:
     *    When a corner of the element is inset by $(d_x, d_y)$ from the corresponding corner of
     *    the device screen (or parent page card), the concentric radius that shares the exact
     *    same center of curvature as the outer corner $R_{\text{outer}}$ is:
     *    $$R_{\text{concentric}}(d_x, d_y) = \max\!\big(r_{\text{minFloor}},\; R_{\text{outer}} - \text{effectiveInset}(d_x, d_y)\big)$$
     *    When the element is embedded flush in the corner ($d_x \to 0, d_y \to 0$),
     *    $R_{\text{concentric}} \to R_{\text{outer}}$, matching the exact curvature of the device bezel!
     * 2. **Per-Corner Proximity Field ($\alpha_c \in [0, 1]$)**:
     *    Each of the 4 corners (`TL`, `TR`, `BR`, `BL`) independently measures its radial distance
     *    $d_c = \sqrt{d_x^2 + d_y^2}$ to the corresponding container/device corner. Inside the
     *    influence radius $D_{\text{influence}}$, $\alpha_c = \text{smoothstep}(1 - d_c / D_{\text{influence}})$
     *    smoothly transitions that corner from `baseCornerRadiusPx` to $R_{\text{concentric}}$.
     */
    fun solveElementCornerRadiiPx(
        elementLeftPx: Float,
        elementTopPx: Float,
        elementRightPx: Float,
        elementBottomPx: Float,
        containerWidthPx: Float,
        containerHeightPx: Float,
        containerCornersPx: SeaGlassCornerRadiiPx,
        baseCornerRadiusPx: Float,
        minCornerFloorPx: Float = 8f,
        influenceDistancePx: Float = 320f,
        adaptationStrength: Float = 1.0f,
        mode: SeaGlassCornerAdaptationMode = SeaGlassCornerAdaptationMode.CONCENTRIC_PROXIMITY
    ): SeaGlassCornerRadiiPx {
        val elemW = (elementRightPx - elementLeftPx).coerceAtLeast(1f)
        val elemH = (elementBottomPx - elementTopPx).coerceAtLeast(1f)
        val maxAllowedRadiusPx = min(elemW, elemH) * 0.5f
        val clampedBase = baseCornerRadiusPx.coerceIn(0f, maxAllowedRadiusPx)

        if (mode == SeaGlassCornerAdaptationMode.FIXED ||
            containerWidthPx <= 1f ||
            containerHeightPx <= 1f ||
            adaptationStrength <= 0.001f
        ) {
            return SeaGlassCornerRadiiPx.uniform(clampedBase)
        }

        val leftInset = elementLeftPx.coerceAtLeast(0f)
        val topInset = elementTopPx.coerceAtLeast(0f)
        val rightInset = (containerWidthPx - elementRightPx).coerceAtLeast(0f)
        val bottomInset = (containerHeightPx - elementBottomPx).coerceAtLeast(0f)

        val floorPx = min(minCornerFloorPx, maxAllowedRadiusPx)
        val invInfluence = 1f / influenceDistancePx.coerceAtLeast(1f)
        val strength = adaptationStrength.coerceIn(0f, 1f)

        fun solveSingleCorner(
            dx: Float,
            dy: Float,
            outerRadiusPx: Float
        ): Float {
            if (outerRadiusPx <= 0.5f) return clampedBase
            // Diagonal distance from this element corner to the corresponding screen/page corner
            val dist = sqrt(dx * dx + dy * dy)
            val rawT = (1f - dist * invInfluence).coerceIn(0f, 1f)
            // C^1 smoothstep weight: 1.0 at the corner, 0.0 outside influenceDistancePx
            val proximityWeight = rawT * rawT * (3f - 2f * rawT) * strength

            if (proximityWeight <= 0.001f && mode != SeaGlassCornerAdaptationMode.FLUSH_CORNER_EMBED) {
                return clampedBase
            }

            // Concentric offset: R_concentric = max(floor, R_outer - inset)
            // Using harmonic/balanced inset so both flush-corner (dx=0, dy=0 -> R_outer)
            // and padded-inset (dx=16dp, dy=16dp -> R_outer - 16dp) are geometrically exact!
            val effectiveInset = (min(dx, dy) * 0.65f + max(dx, dy) * 0.35f)
            val concentricTarget = (outerRadiusPx - effectiveInset)
                .coerceIn(floorPx, maxAllowedRadiusPx)

            return when (mode) {
                SeaGlassCornerAdaptationMode.FLUSH_CORNER_EMBED -> {
                    if (dist < influenceDistancePx * 0.85f) {
                        concentricTarget
                    } else {
                        // Inner page-facing corner stays crisper when outer corner is embedded
                        min(clampedBase, maxAllowedRadiusPx * 0.55f).coerceAtLeast(floorPx)
                    }
                }
                else -> {
                    (clampedBase + (concentricTarget - clampedBase) * proximityWeight)
                        .coerceIn(0f, maxAllowedRadiusPx)
                }
            }
        }

        val tl = solveSingleCorner(leftInset, topInset, containerCornersPx.topLeftPx)
        val tr = solveSingleCorner(rightInset, topInset, containerCornersPx.topRightPx)
        val br = solveSingleCorner(rightInset, bottomInset, containerCornersPx.bottomRightPx)
        val bl = solveSingleCorner(leftInset, bottomInset, containerCornersPx.bottomLeftPx)

        if (mode == SeaGlassCornerAdaptationMode.UNIFORM_CONCENTRIC) {
            val avg = (tl + tr + br + bl) * 0.25f
            return SeaGlassCornerRadiiPx.uniform(avg)
        }

        return SeaGlassCornerRadiiPx(
            topLeftPx = tl,
            topRightPx = tr,
            bottomRightPx = br,
            bottomLeftPx = bl
        )
    }
}
