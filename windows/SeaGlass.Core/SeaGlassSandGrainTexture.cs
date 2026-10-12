using System;

namespace SeaGlass.Core;

/// <summary>
/// Generates and caches a seamless, toroidally tileable 128×128 micro-facet sand-grain frost
/// BGRA32 pixel buffer that models the microscopic pits and chalky crystalline frost of
/// ocean-tumbled sea glass.
///
/// Exact 1:1 parity with <c>dev.seaglass.core.SeaGlassSandGrainTexture</c> on Android.
/// </summary>
public static class SeaGlassSandGrainTexture
{
    public const int TileSize = 256;

    private static byte[]? _cachedBgraPixels;
    private static readonly object SyncRoot = new();

    /// <summary>
    /// Returns the shared 256×256×4 premultiplied BGRA32 pixel array for hardware texture/brush upload.
    /// </summary>
    public static byte[] ObtainBgra32Pixels()
    {
        if (_cachedBgraPixels is not null) return _cachedBgraPixels;
        lock (SyncRoot)
        {
            if (_cachedBgraPixels is not null) return _cachedBgraPixels;
            _cachedBgraPixels = GenerateTileableSandGrainBgra32();
            return _cachedBgraPixels;
        }
    }

    private static byte[] GenerateTileableSandGrainBgra32()
    {
        byte[] pixels = new byte[TileSize * TileSize * 4];

        for (int y = 0; y < TileSize; y++)
        {
            for (int x = 0; x < TileSize; x++)
            {
                // Multi-frequency toroidal stipple hash (periodic mod TileSize)
                float hFineA = Hash2D(x, y, 101);
                float hFineB = Hash2D((x + 59) & (TileSize - 1), (y + 113) & (TileSize - 1), 179);
                float hFineC = Hash2D((x * 3 + 17) & (TileSize - 1), (y * 3 + 43) & (TileSize - 1), 263);
                float hMed = BilinearPeriodicHash(x * 0.5f, y * 0.5f, 128, 211);

                float combined = hFineA * 0.44f + hFineB * 0.28f + hFineC * 0.14f + hMed * 0.14f;

                byte r, g, b, a;
                if (combined >= 0.46f)
                {
                    // Powdery chalk-white sand-abraded micro-facet stipple (0xFFFFFF)
                    float norm = Math.Clamp((combined - 0.46f) / 0.54f, 0f, 1f);
                    float sparkle = norm * (0.80f + 0.55f * norm);
                    a = (byte)Math.Clamp((int)(sparkle * 168f), 0, 182);
                    r = 0xFF;
                    g = 0xFF;
                    b = 0xFF;
                }
                else
                {
                    // Subtle micro-facet pore relief (0x142638)
                    float valley = Math.Clamp((0.46f - combined) / 0.46f, 0f, 1f);
                    float pore = valley * valley;
                    a = (byte)Math.Clamp((int)(pore * 36f), 0, 40);
                    r = 0x14;
                    g = 0x26;
                    b = 0x38;
                }

                // Premultiply for WPF / Direct2D Pbgra32
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
