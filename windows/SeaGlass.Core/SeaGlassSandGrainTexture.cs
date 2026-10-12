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
                float hFineA = Hash2D(x, y, 101);
                float hFineB = Hash2D((x + 59) & (TileSize - 1), (y + 113) & (TileSize - 1), 179);
                float hFineC = Hash2D((x * 3 + 17) & (TileSize - 1), (y * 3 + 43) & (TileSize - 1), 263);
                float hMed = BilinearPeriodicHash(x * 0.5f, y * 0.5f, 128, 211);
                float baseStipple = (hFineA * 0.42f + hFineB * 0.28f + hFineC * 0.14f + hMed * 0.16f) - 0.50f;

                float pinpointPits = EvaluateToroidalSeaGlassPits(
                    x: x,
                    y: y,
                    gridCount: 28,
                    minRadiusPx: 1.35f,
                    maxRadiusPx: 2.55f,
                    spawnThreshold: 0.30f,
                    seed: 347);
                float sparsePits = EvaluateToroidalSeaGlassPits(
                    x: x,
                    y: y,
                    gridCount: 16,
                    minRadiusPx: 1.95f,
                    maxRadiusPx: 3.35f,
                    spawnThreshold: 0.48f,
                    seed: 587);

                float pitSignal = Math.Clamp(pinpointPits * 0.72f + sparsePits * 0.68f, -1f, 1f);
                float delta = MathF.Abs(pitSignal) > 0.12f
                    ? pitSignal * 0.78f + baseStipple * 0.22f
                    : baseStipple * 0.56f + pitSignal * 0.44f;
                float combined = Math.Clamp(0.50f + delta, 0f, 1f);

                byte r, g, b, a;
                if (combined >= 0.49f)
                {
                    // Powdery chalk-white sand-abraded micro-facet stipple (0xFFFFFF)
                    float norm = Math.Clamp((combined - 0.49f) / 0.51f, 0f, 1f);
                    float sparkle = MathF.Pow(norm, 0.64f);
                    a = (byte)Math.Clamp((int)(sparkle * 195f), 0, 210);
                    r = 0xFF;
                    g = 0xFF;
                    b = 0xFF;
                }
                else
                {
                    // Pinpoint tumbled sea-glass mineral pit relief (0x102234)
                    float valley = Math.Clamp((0.49f - combined) / 0.49f, 0f, 1f);
                    float pore = MathF.Pow(valley, 0.72f);
                    a = (byte)Math.Clamp((int)(pore * 72f), 0, 84);
                    r = 0x10;
                    g = 0x22;
                    b = 0x34;
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

    private static float EvaluateToroidalSeaGlassPits(
        float x,
        float y,
        int gridCount,
        float minRadiusPx,
        float maxRadiusPx,
        float spawnThreshold,
        int seed)
    {
        float cellSize = (float)TileSize / gridCount;
        float gx = x / cellSize;
        float gy = y / cellSize;
        int baseCellX = (int)gx;
        int baseCellY = (int)gy;

        float accum = 0f;
        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                int cx = (baseCellX + dx + gridCount) % gridCount;
                int cy = (baseCellY + dy + gridCount) % gridCount;
                float spawn = Hash2D(cx, cy, seed);
                if (spawn < spawnThreshold) continue;

                float jitterX = 0.18f + 0.64f * Hash2D(cx, cy, seed + 31);
                float jitterY = 0.18f + 0.64f * Hash2D(cx, cy, seed + 67);
                float pitX = (baseCellX + dx + jitterX) * cellSize;
                float pitY = (baseCellY + dy + jitterY) * cellSize;

                float distSq = (x - pitX) * (x - pitX) + (y - pitY) * (y - pitY);
                float r = minRadiusPx + (maxRadiusPx - minRadiusPx) * Hash2D(cx, cy, seed + 103);
                float rSq = r * r;
                if (distSq < rSq)
                {
                    float u = MathF.Sqrt(distSq) / r;
                    float smooth = Math.Clamp(1f - u * u * (3f - 2f * u), 0f, 1f);
                    float polarityHash = Hash2D(cx, cy, seed + 151);
                    float sign = polarityHash < 0.54f ? -1f : 0.92f;
                    float strength = 0.55f + 0.45f * Hash2D(cx, cy, seed + 199);
                    accum += sign * smooth * strength;
                }
            }
        }
        return Math.Clamp(accum, -1f, 1f);
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
