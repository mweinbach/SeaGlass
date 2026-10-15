using System;

namespace SeaGlass.Core;

/// <summary>
/// Generates and caches a seamless, toroidally tileable 128×128 soft velvet grain
/// BGRA32 pixel buffer for app chrome: faint SrcOver highlights and valleys with no pitting.
///
/// Exact 1:1 parity with <c>dev.seaglass.core.SeaGlassVelvetGrainTexture</c> on Android.
/// </summary>
public static class SeaGlassVelvetGrainTexture
{
    public const int TileSize = 128;

    private static byte[]? _cachedBgraPixels;
    private static readonly object SyncRoot = new();

    /// <summary>
    /// Returns the shared 128×128×4 premultiplied BGRA32 pixel array for hardware texture/brush upload.
    /// </summary>
    public static byte[] ObtainBgra32Pixels()
    {
        if (_cachedBgraPixels is not null) return _cachedBgraPixels;
        lock (SyncRoot)
        {
            if (_cachedBgraPixels is not null) return _cachedBgraPixels;
            _cachedBgraPixels = GenerateTileableVelvetGrainBgra32();
            return _cachedBgraPixels;
        }
    }

    private static byte[] GenerateTileableVelvetGrainBgra32()
    {
        byte[] pixels = new byte[TileSize * TileSize * 4];

        for (int y = 0; y < TileSize; y++)
        {
            for (int x = 0; x < TileSize; x++)
            {
                // Toroidal multi-octave hash (periodic mod TileSize) for an isotropic velvet finish
                float hFineA = Hash2D(x, y, 101);
                float hFineB = Hash2D((x + 37) & (TileSize - 1), (y + 73) & (TileSize - 1), 179);
                float hMed = BilinearPeriodicHash(x * 0.5f, y * 0.5f, 64, 211);

                // High-frequency isotropic velvet micro-facet distribution (82% fine velvet + 18% sub-grain)
                float combined = hFineA * 0.46f + hFineB * 0.36f + hMed * 0.18f;

                byte r, g, b, a;
                if (combined >= 0.50f)
                {
                    // Velvety sand-abraded micro-facet highlight (soft pearlescent sheen 0xF6FAFD)
                    float intensity = Math.Clamp((combined - 0.50f) * 2.0f, 0f, 1f);
                    float softVelvet = intensity * (1.15f - 0.15f * intensity);
                    a = (byte)Math.Clamp((int)(softVelvet * 76f), 0, 82);
                    r = 0xF6;
                    g = 0xFA;
                    b = 0xFD;
                }
                else
                {
                    // Subtle micro-facet valley (gives tactile velvet depth without dark blotches 0x0E2434)
                    float valley = Math.Clamp((0.50f - combined) * 2.0f, 0f, 1f);
                    float softValley = valley * valley;
                    a = (byte)Math.Clamp((int)(softValley * 36f), 0, 42);
                    r = 0x0E;
                    g = 0x24;
                    b = 0x34;
                }

                // Premultiply for Direct2D / Win2D PBGRA32
                int idx = (y * TileSize + x) * 4;
                pixels[idx + 0] = (byte)((b * a) / 255);
                pixels[idx + 1] = (byte)((g * a) / 255);
                pixels[idx + 2] = (byte)((r * a) / 255);
                pixels[idx + 3] = a;
            }
        }

        return pixels;
    }

    private static float Hash2D(int x, int y, int seed)
    {
        int wx = x & (TileSize - 1);
        int wy = y & (TileSize - 1);
        int n = unchecked(wx * 374761393 + wy * 668265263 + seed * 1442695041);
        n = unchecked((n ^ (int)((uint)n >> 13)) * 1274126177);
        n ^= (int)((uint)n >> 16);
        return (n & 0x7FFFFFFF) / (float)int.MaxValue;
    }

    private static float BilinearPeriodicHash(float fx, float fy, int period, int seed)
    {
        int ix = (int)fx;
        int iy = (int)fy;
        int x0 = ix % period;
        int y0 = iy % period;
        int x1 = (x0 + 1) % period;
        int y1 = (y0 + 1) % period;
        float tx = fx - ix;
        float ty = fy - iy;
        float ux = tx * tx * (3f - 2f * tx);
        float uy = ty * ty * (3f - 2f * ty);

        float v00 = Hash2D(x0, y0, seed);
        float v10 = Hash2D(x1, y0, seed);
        float v01 = Hash2D(x0, y1, seed);
        float v11 = Hash2D(x1, y1, seed);

        float top = v00 + (v10 - v00) * ux;
        float bottom = v01 + (v11 - v01) * ux;
        return top + (bottom - top) * uy;
    }
}
