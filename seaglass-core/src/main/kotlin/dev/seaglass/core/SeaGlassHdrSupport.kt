package dev.seaglass.core

import android.app.Activity
import android.content.Context
import android.content.pm.ActivityInfo
import android.os.Build
import android.view.Display
import android.view.Window
import android.view.WindowManager

/**
 * Hardware HDR (High Dynamic Range) & Wide-Color-Gamut display controller for SeaGlass.
 *
 * Enables `COLOR_MODE_HDR` (`Extended sRGB` / `FP16` framebuffer) and requests HDR specular
 * headroom (`Window.setDesiredHdrHeadroom` on Android 14 / API 34+) so Liquid Sea Glass
 * specular rim glints, internal caustics, and adaptive touch flares can exceed `1.0` SDR
 * white luminance on HDR-capable OLED/Mini-LED displays.
 */
object SeaGlassHdrSupport {

    data class HdrStatus(
        val isHdrDisplaySupported: Boolean,
        val isWideColorGamutSupported: Boolean,
        val targetHdrHeadroom: Float,
        val reportedMaxHdrSdrRatio: Float
    )

    /**
     * Enables `COLOR_MODE_HDR` and configures [desiredHdrHeadroom] (default `1.65f` = +65%
     * specular headroom above SDR paper white) on the host [Activity] window.
     */
    fun enableWindowHdr(
        activity: Activity,
        desiredHdrHeadroom: Float = 1.65f
    ): HdrStatus {
        val window: Window = activity.window
        window.colorMode = ActivityInfo.COLOR_MODE_HDR

        val clampedHeadroom = desiredHdrHeadroom.coerceIn(1.0f, 3.0f)
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.UPSIDE_DOWN_CAKE) {
            window.desiredHdrHeadroom = clampedHeadroom
        }

        return queryHdrStatus(activity, clampedHeadroom)
    }

    /**
     * Queries the current display's HDR and Wide-Color-Gamut capability status.
     */
    fun queryHdrStatus(
        context: Context,
        configuredHeadroom: Float = 1.65f
    ): HdrStatus {
        val display: Display? = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.R) {
            try {
                context.display
            } catch (_: UnsupportedOperationException) {
                val wm = context.getSystemService(Context.WINDOW_SERVICE) as? WindowManager
                @Suppress("DEPRECATION")
                wm?.defaultDisplay
            }
        } else {
            val wm = context.getSystemService(Context.WINDOW_SERVICE) as? WindowManager
            @Suppress("DEPRECATION")
            wm?.defaultDisplay
        }

        val isHdr = display?.isHdr == true
        val isWideGamut = display?.isWideColorGamut == true
        val maxRatio = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.UPSIDE_DOWN_CAKE && display != null) {
            display.hdrSdrRatio.coerceAtLeast(1.0f)
        } else if (isHdr) {
            2.0f
        } else {
            1.0f
        }

        return HdrStatus(
            isHdrDisplaySupported = isHdr,
            isWideColorGamutSupported = isWideGamut,
            targetHdrHeadroom = configuredHeadroom,
            reportedMaxHdrSdrRatio = maxRatio
        )
    }
}
