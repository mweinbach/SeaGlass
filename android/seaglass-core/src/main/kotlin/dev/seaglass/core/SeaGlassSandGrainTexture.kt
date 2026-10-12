package dev.seaglass.core

import android.graphics.Bitmap
import android.graphics.BitmapShader
import android.graphics.Shader
import kotlin.math.cos
import kotlin.math.pow
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

    private const val TILE_SIZE = 256

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
                // Multi-frequency toroidal stipple hash (periodic mod TILE_SIZE):
                // Combines razor-crisp 1px salt-crystal micro-facets with 2px-3px sand-abrasion clusters
                val hFineA = hash2d(x, y, 101)
                val hFineB = hash2d((x + 59) and (TILE_SIZE - 1), (y + 113) and (TILE_SIZE - 1), 179)
                val hFineC = hash2d((x * 3 + 17) and (TILE_SIZE - 1), (y * 3 + 43) and (TILE_SIZE - 1), 263)
                val hMed = bilinearPeriodicHash(x * 0.5f, y * 0.5f, 128, 211)

                // High-frequency isotropic particulate stipple distribution
                val combined = hFineA * 0.44f + hFineB * 0.28f + hFineC * 0.14f + hMed * 0.14f

                val argb = if (combined >= 0.48f) {
                    // Powdery chalk-white sand-abraded micro-facet stipple (screens glass tint to milky frost under OVERLAY)
                    val norm = ((combined - 0.48f) / 0.52f).coerceIn(0f, 1f)
                    val sparkle = norm.pow(0.72f)
                    val alpha = (sparkle * 195f).toInt().coerceIn(0, 210)
                    (alpha shl 24) or 0x00FFFFFF
                } else {
                    // Fine micro-pore valley (multiplies glass tint to deeper tonal chroma under OVERLAY, zero grey mud!)
                    val valley = ((0.48f - combined) / 0.48f).coerceIn(0f, 1f)
                    val pore = valley.pow(0.78f)
                    val alpha = (pore * 135f).toInt().coerceIn(0, 148)
                    (alpha shl 24) or 0x00000000
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
