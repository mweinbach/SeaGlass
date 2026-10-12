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
                // 1. High-contrast 1px-2px isotropic salt/sand micro-facet stipple (zero large blotches)
                val hFineA = hash2d(x, y, 101)
                val hFineB = hash2d((x + 59) and (TILE_SIZE - 1), (y + 113) and (TILE_SIZE - 1), 179)
                val hFineC = hash2d((x * 3 + 17) and (TILE_SIZE - 1), (y * 3 + 43) and (TILE_SIZE - 1), 263)
                val hMed = bilinearPeriodicHash(x * 0.5f, y * 0.5f, 128, 211)
                val baseStipple = (hFineA * 0.42f + hFineB * 0.28f + hFineC * 0.14f + hMed * 0.16f) - 0.50f

                // 2. Discrete pinpoint sea-glass pits & frost crystal specks (1.5px..3.2px physical pixel radius):
                // Each pit has a smooth radial alpha falloff and pure RGB polarity (0x000000 or 0xFFFFFF)
                // so nearest-neighbor sampling at 1:1 physical pixel scale renders anti-aliased 1dp..2.2dp
                // circular sea-glass dimples with zero bilinear S=0.5 OVERLAY cancellation!
                val pinpointPits = evaluateToroidalSeaGlassPits(
                    x = x.toFloat(),
                    y = y.toFloat(),
                    gridCount = 28,
                    minRadiusPx = 1.35f,
                    maxRadiusPx = 2.55f,
                    spawnThreshold = 0.30f,
                    seed = 347
                )
                val sparsePits = evaluateToroidalSeaGlassPits(
                    x = x.toFloat(),
                    y = y.toFloat(),
                    gridCount = 16,
                    minRadiusPx = 1.95f,
                    maxRadiusPx = 3.35f,
                    spawnThreshold = 0.48f,
                    seed = 587
                )

                // Strong pit dominance inside pit cores so a dark mineral pit is never flipped by fine noise
                val pitSignal = (pinpointPits * 0.72f + sparsePits * 0.68f).coerceIn(-1f, 1f)
                val delta = if (kotlin.math.abs(pitSignal) > 0.12f) {
                    pitSignal * 0.78f + baseStipple * 0.22f
                } else {
                    baseStipple * 0.56f + pitSignal * 0.44f
                }
                val combined = (0.50f + delta).coerceIn(0f, 1f)

                val argb = if (combined >= 0.49f) {
                    // Powdery chalk-white sand-abraded micro-facet stipple (screens glass tint under OVERLAY)
                    val norm = ((combined - 0.49f) / 0.51f).coerceIn(0f, 1f)
                    val sparkle = norm.pow(0.64f)
                    val alpha = (sparkle * 220f).toInt().coerceIn(0, 232)
                    (alpha shl 24) or 0x00FFFFFF
                } else {
                    // Pinpoint mineral pit valley (multiplies glass tint to deeper tonal chroma under OVERLAY)
                    val valley = ((0.49f - combined) / 0.49f).coerceIn(0f, 1f)
                    val pore = valley.pow(0.68f)
                    val alpha = (pore * 185f).toInt().coerceIn(0, 200)
                    (alpha shl 24) or 0x00000000
                }

                pixels[y * TILE_SIZE + x] = argb
            }
        }

        return Bitmap.createBitmap(pixels, TILE_SIZE, TILE_SIZE, Bitmap.Config.ARGB_8888)
    }

    /**
     * Evaluates toroidal cellular Worley/Poisson sea-glass pits in `[-1f..+1f]`:
     * negative values represent deeper tonal mineral pits; positive values represent
     * chalky frosted crystal facets.
     */
    private fun evaluateToroidalSeaGlassPits(
        x: Float,
        y: Float,
        gridCount: Int,
        minRadiusPx: Float,
        maxRadiusPx: Float,
        spawnThreshold: Float,
        seed: Int
    ): Float {
        val cellSize = TILE_SIZE.toFloat() / gridCount.toFloat()
        val gx = x / cellSize
        val gy = y / cellSize
        val baseCellX = gx.toInt()
        val baseCellY = gy.toInt()

        var accum = 0f
        for (dy in -1..1) {
            for (dx in -1..1) {
                val cx = (baseCellX + dx + gridCount) % gridCount
                val cy = (baseCellY + dy + gridCount) % gridCount
                val spawn = hash2d(cx, cy, seed)
                if (spawn < spawnThreshold) continue

                val jitterX = 0.18f + 0.64f * hash2d(cx, cy, seed + 31)
                val jitterY = 0.18f + 0.64f * hash2d(cx, cy, seed + 67)
                val pitX = (baseCellX + dx + jitterX) * cellSize
                val pitY = (baseCellY + dy + jitterY) * cellSize

                val distSq = (x - pitX) * (x - pitX) + (y - pitY) * (y - pitY)
                val r = minRadiusPx + (maxRadiusPx - minRadiusPx) * hash2d(cx, cy, seed + 103)
                val rSq = r * r
                if (distSq < rSq) {
                    val u = kotlin.math.sqrt(distSq) / r
                    val smooth = (1f - u * u * (3f - 2f * u)).coerceIn(0f, 1f)
                    val polarityHash = hash2d(cx, cy, seed + 151)
                    // ~54% deeper tonal mineral pits, ~46% frosty crystal facets
                    val sign = if (polarityHash < 0.54f) -1f else 0.92f
                    val strength = 0.55f + 0.45f * hash2d(cx, cy, seed + 199)
                    accum += sign * smooth * strength
                }
            }
        }
        return accum.coerceIn(-1f, 1f)
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
