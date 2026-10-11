package dev.seaglass.core

import android.graphics.Bitmap
import android.graphics.BitmapShader
import android.graphics.Shader
import kotlin.math.cos
import kotlin.math.sin

/**
 * Generates and caches a seamless, toroidally tileable 128×128 micro-facet sand-grain frost
 * [BitmapShader] (`Shader.TileMode.REPEAT`) that models the microscopic pits and chalky
 * crystalline frost of ocean-tumbled sea glass.
 *
 * Works universally across all Android GPU drivers (Vulkan, OpenGL ES, and Emulator `GFXSTREAM`)
 * with zero per-frame allocations.
 */
object SeaGlassSandGrainTexture {

    private const val TILE_SIZE = 128

    @Volatile
    private var cachedShader: BitmapShader? = null

    /**
     * Returns the shared tileable sand-grain [BitmapShader].
     */
    fun obtainShader(): BitmapShader {
        cachedShader?.let { return it }
        synchronized(this) {
            cachedShader?.let { return it }
            val bitmap = generateTileableSandGrainBitmap()
            val shader = BitmapShader(bitmap, Shader.TileMode.REPEAT, Shader.TileMode.REPEAT)
            cachedShader = shader
            return shader
        }
    }

    private fun generateTileableSandGrainBitmap(): Bitmap {
        val pixels = IntArray(TILE_SIZE * TILE_SIZE)

        for (y in 0 until TILE_SIZE) {
            for (x in 0 until TILE_SIZE) {
                // Toroidal multi-octave hash (periodic mod TILE_SIZE) so tile edges are 100% seamless
                val hFine = hash2d(x, y, 101)
                val hMed = bilinearPeriodicHash(x * 0.5f, y * 0.5f, 64, 211)
                val hCoarse = bilinearPeriodicHash(x * 0.25f, y * 0.25f, 32, 307)

                // Combine fine crystalline sparkle (55%), medium sand-grain frost (30%), and coarse surf pit (15%)
                val combined = hFine * 0.55f + hMed * 0.30f + hCoarse * 0.15f

                val argb = if (combined >= 0.50f) {
                    // Chalky frosted micro-facet highlight (luminous alabaster crystal)
                    val intensity = ((combined - 0.50f) * 2.0f).coerceIn(0f, 1f)
                    val sharpened = intensity * intensity * (1.4f - 0.4f * intensity)
                    val alpha = (sharpened * 118f).toInt().coerceIn(0, 125)
                    (alpha shl 24) or 0x00F8FCFF
                } else {
                    // Etched micro-pit shadow (gives tactile dry-sanded depth)
                    val pit = ((0.50f - combined) * 2.0f).coerceIn(0f, 1f)
                    val sharpened = pit * pit
                    val alpha = (sharpened * 64f).toInt().coerceIn(0, 72)
                    (alpha shl 24) or 0x000B2234
                }

                pixels[y * TILE_SIZE + x] = argb
            }
        }

        return Bitmap.createBitmap(pixels, TILE_SIZE, TILE_SIZE, Bitmap.Config.ARGB_8888)
    }

    private fun hash2d(x: Int, y: Int, seed: Int): Float {
        val wx = (x and (TILE_SIZE - 1))
        val wy = (y and (TILE_SIZE - 1))
        var n = wx * 374761393 + wy * 668265263 + seed * 1442695041
        n = (n xor (n ushr 13)) * 1274126177
        n = n xor (n ushr 16)
        return (n and 0x7FFFFFFF).toFloat() / Int.MAX_VALUE.toFloat()
    }

    private fun bilinearPeriodicHash(fx: Float, fy: Float, period: Int, seed: Int): Float {
        val x0 = fx.toInt() % period
        val y0 = fy.toInt() % period
        val x1 = (x0 + 1) % period
        val y1 = (y0 + 1) % period
        val tx = fx - fx.toInt()
        val ty = fy - fy.toInt()
        val ux = tx * tx * (3f - 2f * tx)
        val uy = ty * ty * (3f - 2f * ty)

        val v00 = hash2d(x0, y0, seed)
        val v10 = hash2d(x1, y0, seed)
        val v01 = hash2d(x0, y1, seed)
        val v11 = hash2d(x1, y1, seed)

        val top = v00 + (v10 - v00) * ux
        val bottom = v01 + (v11 - v01) * ux
        return top + (bottom - top) * uy
    }
}
