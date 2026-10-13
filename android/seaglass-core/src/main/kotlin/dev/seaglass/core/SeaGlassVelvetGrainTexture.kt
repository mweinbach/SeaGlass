package dev.seaglass.core

import android.graphics.Bitmap
import android.graphics.BitmapShader
import android.graphics.Shader

/**
 * Soft velvet grain for app chrome: faint SrcOver highlights and valleys with no pitting.
 * A seamless 128x128 tile cached as a repeating [BitmapShader].
 */
object SeaGlassVelvetGrainTexture {

    private const val TILE_SIZE = 128

    @Volatile
    private var cachedShader: BitmapShader? = null

    /** Returns the shared tileable velvet grain [BitmapShader]. */
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
                // Toroidal multi-octave hash (periodic mod TILE_SIZE) for an isotropic velvet finish
                val hFineA = hash2d(x, y, 101)
                val hFineB = hash2d((x + 37) and (TILE_SIZE - 1), (y + 73) and (TILE_SIZE - 1), 179)
                val hMed = bilinearPeriodicHash(x * 0.5f, y * 0.5f, 64, 211)

                // High-frequency isotropic velvet micro-facet distribution (82% fine velvet + 18% sub-grain)
                val combined = hFineA * 0.46f + hFineB * 0.36f + hMed * 0.18f

                val argb = if (combined >= 0.50f) {
                    // Velvety sand-abraded micro-facet highlight (soft pearlescent sheen)
                    val intensity = ((combined - 0.50f) * 2.0f).coerceIn(0f, 1f)
                    val softVelvet = intensity * (1.15f - 0.15f * intensity)
                    val alpha = (softVelvet * 76f).toInt().coerceIn(0, 82)
                    (alpha shl 24) or 0x00F6FAFD
                } else {
                    // Subtle micro-facet valley (gives tactile velvet depth without dark blotches)
                    val valley = ((0.50f - combined) * 2.0f).coerceIn(0f, 1f)
                    val softValley = valley * valley
                    val alpha = (softValley * 36f).toInt().coerceIn(0, 42)
                    (alpha shl 24) or 0x000E2434
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
