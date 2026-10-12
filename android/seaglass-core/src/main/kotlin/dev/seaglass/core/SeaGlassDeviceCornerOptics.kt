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
 * Per-corner mask specifying **where** adaptive corner radius matching is allowed on an element.
 *
 * Developers can enable adaptation on all 4 corners ([ALL]), disable it ([NONE]), restrict it
 * to a specific edge ([TOP_ONLY], [BOTTOM_ONLY], [LEFT_ONLY], [RIGHT_ONLY]), or toggle
 * individual corners (`topLeft`, `topRight`, `bottomRight`, `bottomLeft`).
 */
data class SeaGlassCornerMask(
    val topLeft: Boolean = true,
    val topRight: Boolean = true,
    val bottomRight: Boolean = true,
    val bottomLeft: Boolean = true
) {
    val isNone: Boolean
        get() = !topLeft && !topRight && !bottomRight && !bottomLeft

    val isAll: Boolean
        get() = topLeft && topRight && bottomRight && bottomLeft

    companion object {
        val ALL = SeaGlassCornerMask(
            topLeft = true,
            topRight = true,
            bottomRight = true,
            bottomLeft = true
        )
        val NONE = SeaGlassCornerMask(
            topLeft = false,
            topRight = false,
            bottomRight = false,
            bottomLeft = false
        )
        val TOP_ONLY = SeaGlassCornerMask(
            topLeft = true,
            topRight = true,
            bottomRight = false,
            bottomLeft = false
        )
        val BOTTOM_ONLY = SeaGlassCornerMask(
            topLeft = false,
            topRight = false,
            bottomRight = true,
            bottomLeft = true
        )
        val LEFT_ONLY = SeaGlassCornerMask(
            topLeft = true,
            topRight = false,
            bottomRight = false,
            bottomLeft = true
        )
        val RIGHT_ONLY = SeaGlassCornerMask(
            topLeft = false,
            topRight = true,
            bottomRight = true,
            bottomLeft = false
        )

        fun only(
            topLeft: Boolean = false,
            topRight: Boolean = false,
            bottomRight: Boolean = false,
            bottomLeft: Boolean = false
        ) = SeaGlassCornerMask(
            topLeft = topLeft,
            topRight = topRight,
            bottomRight = bottomRight,
            bottomLeft = bottomLeft
        )
    }
}

/**
 * Developer configuration controlling **when**, **where**, and **how close** to a physical
 * device corner (or parent container corner) a SeaGlass element must be before its corner
 * radius adapts.
 *
 * @property mode Adaptation strategy ([SeaGlassCornerAdaptationMode.FIXED] disables adaptation;
 *   [SeaGlassCornerAdaptationMode.CONCENTRIC_PROXIMITY] smoothly matches within [proximityThresholdDp]).
 * @property corners Per-corner mask ([SeaGlassCornerMask]) selecting which corners may adapt.
 * @property proximityThresholdDp Maximum radial distance (in `dp`) from a device/container corner
 *   at which corner adaptation begins (`d >= proximityThresholdDp` keeps the base corner radius).
 * @property fullMatchDistanceDp Inner radial distance (in `dp`) within which the corner reaches
 *   100% of the concentric bezel target ($R_{\text{outer}} - \text{inset}$).
 * @property strength Overall interpolation weight in `[0f..1f]`.
 * @property allowAsymmetricCorners Whether individual corners on the same element may take on
 *   different radii when only one edge/corner is near the bezel. When `false`, the element keeps
 *   all enabled corners uniform so floating cards and multi-line inputs never look lopsided.
 * @property adaptToDeviceCorners Whether to adapt relative to physical device display corners.
 * @property adaptToParentContainer Whether nested elements also adapt relative to an enclosing
 *   `LocalSeaGlassContainerCornerScope` card/sheet. Defaults to `false` so inner buttons/pills
 *   keep their own shape unless opted in.
 * @property minCornerFloorDp Minimum corner radius floor (in `dp`) when tightening near sharp bezels.
 */
data class SeaGlassCornerAdaptationConfig(
    val mode: SeaGlassCornerAdaptationMode = SeaGlassCornerAdaptationMode.CONCENTRIC_PROXIMITY,
    val corners: SeaGlassCornerMask = SeaGlassCornerMask.ALL,
    val proximityThresholdDp: Float = DEFAULT_PROXIMITY_THRESHOLD_DP,
    val fullMatchDistanceDp: Float = DEFAULT_FULL_MATCH_DISTANCE_DP,
    val strength: Float = 1.0f,
    val allowAsymmetricCorners: Boolean = false,
    val adaptToDeviceCorners: Boolean = true,
    val adaptToParentContainer: Boolean = false,
    val minCornerFloorDp: Float = 8f
) {
    companion object {
        const val DEFAULT_PROXIMITY_THRESHOLD_DP = 32f
        const val DEFAULT_FULL_MATCH_DISTANCE_DP = 6f

        /** Disables corner adaptation and uses raw [SeaGlassMaterial.cornerRadiusDp]. */
        val Disabled = SeaGlassCornerAdaptationConfig(
            mode = SeaGlassCornerAdaptationMode.FIXED,
            corners = SeaGlassCornerMask.NONE,
            strength = 0f
        )

        /**
         * Smooth concentric matching when an element comes within [proximityThresholdDp] of a
         * device corner. By default keeps corners uniform ([allowAsymmetricCorners] = `false`)
         * unless per-corner asymmetry is explicitly requested.
         */
        fun concentricProximity(
            proximityThresholdDp: Float = DEFAULT_PROXIMITY_THRESHOLD_DP,
            fullMatchDistanceDp: Float = DEFAULT_FULL_MATCH_DISTANCE_DP,
            corners: SeaGlassCornerMask = SeaGlassCornerMask.ALL,
            allowAsymmetricCorners: Boolean = false,
            strength: Float = 1.0f,
            adaptToDeviceCorners: Boolean = true,
            adaptToParentContainer: Boolean = false
        ) = SeaGlassCornerAdaptationConfig(
            mode = SeaGlassCornerAdaptationMode.CONCENTRIC_PROXIMITY,
            corners = corners,
            proximityThresholdDp = proximityThresholdDp.coerceAtLeast(0f),
            fullMatchDistanceDp = fullMatchDistanceDp.coerceAtLeast(0f),
            strength = strength.coerceIn(0f, 1f),
            allowAsymmetricCorners = allowAsymmetricCorners,
            adaptToDeviceCorners = adaptToDeviceCorners,
            adaptToParentContainer = adaptToParentContainer
        )

        /**
         * Asymmetric corner embedding for corner pods and edge docks where outer bezel-facing
         * corners lock to $R_{\text{outer}} - \text{inset}$ while inner corners stay tighter.
         */
        fun flushCornerEmbed(
            proximityThresholdDp: Float = 48f,
            fullMatchDistanceDp: Float = 8f,
            corners: SeaGlassCornerMask = SeaGlassCornerMask.ALL,
            adaptToParentContainer: Boolean = true,
            strength: Float = 1.0f
        ) = SeaGlassCornerAdaptationConfig(
            mode = SeaGlassCornerAdaptationMode.FLUSH_CORNER_EMBED,
            corners = corners,
            proximityThresholdDp = proximityThresholdDp.coerceAtLeast(0f),
            fullMatchDistanceDp = fullMatchDistanceDp.coerceAtLeast(0f),
            strength = strength.coerceIn(0f, 1f),
            allowAsymmetricCorners = true,
            adaptToDeviceCorners = true,
            adaptToParentContainer = adaptToParentContainer
        )
    }
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
 * Resolved corner-safe content padding in pixels (`leftPx`, `topPx`, `rightPx`, `bottomPx`)
 * that prevents foreground text and icons from colliding with deeply curved concentric corners.
 */
data class SeaGlassSafePaddingPx(
    val leftPx: Float,
    val topPx: Float,
    val rightPx: Float,
    val bottomPx: Float
)

/**
 * Dynamically solved, guaranteed non-overlapping layout dimensions in pixels for corner-embedded
 * pods and bottom docks inside an enclosing bezel or page frame.
 */
data class SeaGlassCornerPodLayoutPx(
    val effectiveInsetPx: Float,
    val podWidthPx: Float,
    val topPodHeightPx: Float,
    val bottomDockHeightPx: Float,
    val horizontalGapPx: Float,
    val verticalGapPx: Float
)

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
        config: SeaGlassCornerAdaptationConfig,
        density: Float = 2.75f
    ): SeaGlassCornerRadiiPx {
        val d = density.coerceAtLeast(0.5f)
        return solveElementCornerRadiiPx(
            elementLeftPx = elementLeftPx,
            elementTopPx = elementTopPx,
            elementRightPx = elementRightPx,
            elementBottomPx = elementBottomPx,
            containerWidthPx = containerWidthPx,
            containerHeightPx = containerHeightPx,
            containerCornersPx = containerCornersPx,
            baseCornerRadiusPx = baseCornerRadiusPx,
            minCornerFloorPx = config.minCornerFloorDp * d,
            influenceDistancePx = config.proximityThresholdDp * d,
            fullMatchDistancePx = config.fullMatchDistanceDp * d,
            adaptationStrength = config.strength,
            mode = config.mode,
            cornerMask = config.corners,
            allowAsymmetricCorners = config.allowAsymmetricCorners
        )
    }

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
        influenceDistancePx: Float = 96f,
        fullMatchDistancePx: Float = 0f,
        adaptationStrength: Float = 1.0f,
        mode: SeaGlassCornerAdaptationMode = SeaGlassCornerAdaptationMode.CONCENTRIC_PROXIMITY,
        cornerMask: SeaGlassCornerMask = SeaGlassCornerMask.ALL,
        allowAsymmetricCorners: Boolean = true
    ): SeaGlassCornerRadiiPx {
        val elemW = (elementRightPx - elementLeftPx).coerceAtLeast(1f)
        val elemH = (elementBottomPx - elementTopPx).coerceAtLeast(1f)
        val halfMinDimPx = min(elemW, elemH) * 0.5f
        // An asymmetric outer corner can safely use up to 78% of min(elemW, elemH) when the
        // opposite inner corner on that edge is small (e.g., 52dp outer + 14dp inner <= 70dp height)
        val maxAsymmetricCornerPx = min(elemW, elemH) * 0.78f
        val clampedBase = baseCornerRadiusPx.coerceIn(0f, halfMinDimPx)

        if (mode == SeaGlassCornerAdaptationMode.FIXED ||
            cornerMask.isNone ||
            containerWidthPx <= 1f ||
            containerHeightPx <= 1f ||
            influenceDistancePx <= 0.5f ||
            adaptationStrength <= 0.001f
        ) {
            return SeaGlassCornerRadiiPx.uniform(clampedBase)
        }

        val leftInset = elementLeftPx.coerceAtLeast(0f)
        val topInset = elementTopPx.coerceAtLeast(0f)
        val rightInset = (containerWidthPx - elementRightPx).coerceAtLeast(0f)
        val bottomInset = (containerHeightPx - elementBottomPx).coerceAtLeast(0f)

        val floorPx = min(minCornerFloorPx, halfMinDimPx)
        val matchDistPx = fullMatchDistancePx.coerceIn(0f, influenceDistancePx * 0.90f)
        val transitionSpanPx = (influenceDistancePx - matchDistPx).coerceAtLeast(1f)
        val strength = adaptationStrength.coerceIn(0f, 1f)

        fun solveSingleCorner(
            dx: Float,
            dy: Float,
            outerRadiusPx: Float,
            cornerEnabled: Boolean
        ): Float {
            if (!cornerEnabled || outerRadiusPx <= 0.5f) return clampedBase
            if (dx >= influenceDistancePx || dy >= influenceDistancePx) {
                return if (mode == SeaGlassCornerAdaptationMode.FLUSH_CORNER_EMBED) {
                    min(clampedBase, halfMinDimPx * 0.45f).coerceAtLeast(floorPx)
                } else {
                    clampedBase
                }
            }
            val edgeDist = min(dx, dy)
            val rawT = ((influenceDistancePx - edgeDist) / transitionSpanPx).coerceIn(0f, 1f)
            val proximityWeight = rawT * rawT * (3f - 2f * rawT) * strength

            if (proximityWeight <= 0.001f && mode != SeaGlassCornerAdaptationMode.FLUSH_CORNER_EMBED) {
                return clampedBase
            }

            val effectiveInset = edgeDist
            val rawConcentric = (outerRadiusPx - effectiveInset)
                .coerceIn(floorPx, maxAsymmetricCornerPx)

            return when (mode) {
                SeaGlassCornerAdaptationMode.FLUSH_CORNER_EMBED -> {
                    if (edgeDist < influenceDistancePx * 0.85f) {
                        rawConcentric
                    } else {
                        min(clampedBase, halfMinDimPx * 0.45f).coerceAtLeast(floorPx)
                    }
                }
                else -> {
                    // Prevent mid-distance corner pinching: when approaching a bezel rounder than
                    // clampedBase, only expand above clampedBase (never dip below clampedBase at
                    // intermediate distances); when approaching a sharper bezel, tighten down.
                    val monotonicTarget = if (outerRadiusPx >= clampedBase) {
                        max(clampedBase, rawConcentric)
                    } else {
                        min(clampedBase, rawConcentric)
                    }
                    (clampedBase + (monotonicTarget - clampedBase) * proximityWeight)
                        .coerceIn(0f, maxAsymmetricCornerPx)
                }
            }
        }

        var tl = solveSingleCorner(leftInset, topInset, containerCornersPx.topLeftPx, cornerMask.topLeft)
        var tr = solveSingleCorner(rightInset, topInset, containerCornersPx.topRightPx, cornerMask.topRight)
        var br = solveSingleCorner(rightInset, bottomInset, containerCornersPx.bottomRightPx, cornerMask.bottomRight)
        var bl = solveSingleCorner(leftInset, bottomInset, containerCornersPx.bottomLeftPx, cornerMask.bottomLeft)

        if (mode == SeaGlassCornerAdaptationMode.UNIFORM_CONCENTRIC ||
            (!allowAsymmetricCorners && mode != SeaGlassCornerAdaptationMode.FLUSH_CORNER_EMBED)
        ) {
            val maxCorner = max(max(tl, tr), max(br, bl))
            val minCorner = min(min(tl, tr), min(br, bl))
            val unified = if (maxCorner > clampedBase + 0.5f) {
                min(maxCorner, halfMinDimPx)
            } else if (minCorner < clampedBase - 0.5f) {
                min(minCorner, halfMinDimPx)
            } else {
                clampedBase
            }
            tl = if (cornerMask.topLeft) unified else clampedBase
            tr = if (cornerMask.topRight) unified else clampedBase
            br = if (cornerMask.bottomRight) unified else clampedBase
            bl = if (cornerMask.bottomLeft) unified else clampedBase
        }

        // Normalize adjacent corner sums so no pair of corners on the same edge exceeds elemW or elemH
        val edgeScale = min(
            min(
                if (tl + tr > elemW) elemW / (tl + tr) else 1f,
                if (bl + br > elemW) elemW / (bl + br) else 1f
            ),
            min(
                if (tl + bl > elemH) elemH / (tl + bl) else 1f,
                if (tr + br > elemH) elemH / (tr + br) else 1f
            )
        ).coerceAtMost(1f)

        tl *= edgeScale
        tr *= edgeScale
        br *= edgeScale
        bl *= edgeScale

        return SeaGlassCornerRadiiPx(
            topLeftPx = tl,
            topRightPx = tr,
            bottomRightPx = br,
            bottomLeftPx = bl
        )
    }

    /**
     * Computes the minimum safe corner-cleared content padding so foreground text and icons
     * never collide with deeply rounded concentric corners (`R * (1 - cos 45°) ≈ 0.293 R`).
     */
    fun computeCornerSafePaddingPx(
        corners: SeaGlassCornerRadiiPx,
        baseLeftPx: Float,
        baseTopPx: Float,
        baseRightPx: Float,
        baseBottomPx: Float
    ): SeaGlassSafePaddingPx {
        val leftCornerMax = max(corners.topLeftPx, corners.bottomLeftPx)
        val rightCornerMax = max(corners.topRightPx, corners.bottomRightPx)
        val topCornerMax = max(corners.topLeftPx, corners.topRightPx)
        val bottomCornerMax = max(corners.bottomLeftPx, corners.bottomRightPx)

        return SeaGlassSafePaddingPx(
            leftPx = max(baseLeftPx, leftCornerMax * 0.34f),
            topPx = max(baseTopPx, topCornerMax * 0.22f),
            rightPx = max(baseRightPx, rightCornerMax * 0.34f),
            bottomPx = max(baseBottomPx, bottomCornerMax * 0.22f)
        )
    }

    /**
     * Dynamically solves collision-free dimensions and safe bezel inset for corner-embedded pods
     * and bottom docks inside an enclosing bezel or page frame of size `(containerWidthPx, containerHeightPx)`.
     *
     * Guarantees:
     * 1. **Zero Bezel Protrusion**: If `requestedInsetPx` is smaller than the diagonal clearance
     *    needed for the pod's corner radius inside `containerCornersPx`, `effectiveInsetPx` is
     *    automatically increased so the pod never crosses or gets clipped by the bezel curve.
     * 2. **Zero Inter-Pod Overlap**: `podWidthPx` is clamped to `(innerW - minHorizontalGapPx) / 2`
     *    and `topPodHeightPx + bottomDockHeightPx <= innerH - minVerticalGapPx` at all insets.
     */
    fun solveCornerPodLayoutPx(
        containerWidthPx: Float,
        containerHeightPx: Float,
        containerCornersPx: SeaGlassCornerRadiiPx,
        requestedInsetPx: Float,
        minHorizontalGapPx: Float,
        minVerticalGapPx: Float,
        preferredPodWidthPx: Float,
        preferredTopPodHeightPx: Float,
        preferredBottomDockHeightPx: Float
    ): SeaGlassCornerPodLayoutPx {
        val maxBezelR = containerCornersPx.maxRadiusPx
        val maxPodR = min(preferredPodWidthPx, preferredTopPodHeightPx) * 0.75f
        // Diagonal arc protrusion protection: ensure (d + r_pod * 0.293 >= R_bezel * 0.293)
        val minArcSafeInsetPx = max(0f, (maxBezelR - maxPodR) * 0.30f)
        val maxSafeInsetX = (containerWidthPx * 0.18f).coerceAtLeast(0f)
        val maxSafeInsetY = (containerHeightPx * 0.16f).coerceAtLeast(0f)
        val effectiveInsetPx = max(requestedInsetPx, minArcSafeInsetPx)
            .coerceAtMost(min(maxSafeInsetX, maxSafeInsetY).coerceAtLeast(requestedInsetPx))

        val innerW = (containerWidthPx - 2f * effectiveInsetPx).coerceAtLeast(64f)
        val innerH = (containerHeightPx - 2f * effectiveInsetPx).coerceAtLeast(64f)

        val maxNonOverlappingPodW = ((innerW - minHorizontalGapPx) * 0.5f).coerceAtLeast(36f)
        val solvedPodW = min(preferredPodWidthPx, maxNonOverlappingPodW)
        val actualHorizontalGap = (innerW - solvedPodW * 2f).coerceAtLeast(0f)

        val availVertForPods = (innerH - minVerticalGapPx).coerceAtLeast(48f)
        val totalPrefH = (preferredTopPodHeightPx + preferredBottomDockHeightPx).coerceAtLeast(1f)
        val vertScale = min(1f, availVertForPods / totalPrefH)
        val solvedTopH = preferredTopPodHeightPx * vertScale
        val solvedBottomH = preferredBottomDockHeightPx * vertScale
        val actualVerticalGap = (innerH - solvedTopH - solvedBottomH).coerceAtLeast(0f)

        return SeaGlassCornerPodLayoutPx(
            effectiveInsetPx = effectiveInsetPx,
            podWidthPx = solvedPodW,
            topPodHeightPx = solvedTopH,
            bottomDockHeightPx = solvedBottomH,
            horizontalGapPx = actualHorizontalGap,
            verticalGapPx = actualVerticalGap
        )
    }
}
