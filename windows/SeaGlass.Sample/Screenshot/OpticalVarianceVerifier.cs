using System;

namespace SeaGlass.Sample.Screenshot;

public sealed record OpticalVarianceReport(
    double SpatialStdDev,
    double RimContrast,
    double BackdropDiffusionRatio,
    bool PassesCoarseness,
    bool PassesRimHighlight,
    bool IsAuthenticGlass,
    string Summary);

/// <summary>
/// Verifies authentic sea-glass optical rendering in captured framebuffers.
/// Fails if the captured region is indistinguishable from a flat alpha fill.
/// </summary>
public static class OpticalVarianceVerifier
{
    public const double MinSpatialStdDev = 1.5;     // 8-bit [0..255] luminance units (micro-facet noise)
    public const double MinRimContrast = 4.0;        // 8-bit [0..255] luminance units (rough-Fresnel rim)
    public const double MaxDiffusionRatio = 0.60;    // Maximum edge sharpness ratio under glass vs backdrop

    /// <summary>
    /// Analyzes a captured glass region in a bitmap and verifies authentic optical modulation.
    /// </summary>
    public static OpticalVarianceReport VerifyRegion(
        CapturedWindowBitmap bitmap,
        int left, int top, int width, int height,
        CapturedWindowBitmap? unglassedBackdrop = null)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        left = Math.Clamp(left, 0, bitmap.Width - 1);
        top = Math.Clamp(top, 0, bitmap.Height - 1);
        width = Math.Clamp(width, 10, bitmap.Width - left);
        height = Math.Clamp(height, 10, bitmap.Height - top);

        // 1. Compute Rec. 709 perceptual luminance values for the region
        double[] lum = new double[width * height];
        for (int y = 0; y < height; y++)
        {
            int srcRow = (top + y) * bitmap.Width + left;
            int dstRow = y * width;
            for (int x = 0; x < width; x++)
            {
                int p = (srcRow + x) * 4;
                byte b = bitmap.BgraPixels[p];
                byte g = bitmap.BgraPixels[p + 1];
                byte r = bitmap.BgraPixels[p + 2];
                lum[dstRow + x] = 0.2126 * r + 0.7152 * g + 0.0722 * b;
            }
        }

        // 2. Compute Core Standard Deviation (inset by 20% to isolate interior volumetric core)
        int insetX = Math.Max(2, (int)(width * 0.20));
        int insetY = Math.Max(2, (int)(height * 0.20));
        int coreW = width - 2 * insetX;
        int coreH = height - 2 * insetY;

        double sum = 0;
        int coreCount = coreW * coreH;
        for (int y = insetY; y < height - insetY; y++)
        {
            int row = y * width;
            for (int x = insetX; x < width - insetX; x++)
            {
                sum += lum[row + x];
            }
        }
        double meanCore = coreCount > 0 ? sum / coreCount : 0.0;

        double sumSqDiff = 0;
        for (int y = insetY; y < height - insetY; y++)
        {
            int row = y * width;
            for (int x = insetX; x < width - insetX; x++)
            {
                double diff = lum[row + x] - meanCore;
                sumSqDiff += diff * diff;
            }
        }
        double stdDev = coreCount > 0 ? Math.Sqrt(sumSqDiff / coreCount) : 0.0;

        // 3. Compute Rim-to-Core Contrast (outer perimeter band)
        double rimSum = 0;
        int rimCount = 0;
        for (int y = 1; y < Math.Min(4, height - 1); y++)
        {
            int rowTop = y * width;
            int rowBottom = (height - 1 - y) * width;
            for (int x = 1; x < width - 1; x++)
            {
                rimSum += lum[rowTop + x] + lum[rowBottom + x];
                rimCount += 2;
            }
        }
        double meanRim = rimCount > 0 ? rimSum / rimCount : meanCore;
        double rimContrast = Math.Abs(meanRim - meanCore);

        // Headless / display-sleep detection: fail immediately if frame is blank, black, or completely uniform
        if (meanCore <= 5.0 && meanRim <= 5.0)
        {
            return new OpticalVarianceReport(
                SpatialStdDev: stdDev,
                RimContrast: rimContrast,
                BackdropDiffusionRatio: 1.0,
                PassesCoarseness: false,
                PassesRimHighlight: false,
                IsAuthenticGlass: false,
                Summary: "FAIL: Blank or black frame detected (luminance <= 5.0). Screen capture cannot capture real composited glass in current session.");
        }

        if (stdDev < 0.1 && rimContrast < 0.1)
        {
            return new OpticalVarianceReport(
                SpatialStdDev: stdDev,
                RimContrast: rimContrast,
                BackdropDiffusionRatio: 1.0,
                PassesCoarseness: false,
                PassesRimHighlight: false,
                IsAuthenticGlass: false,
                Summary: "FAIL: Completely uniform frame detected (spatial std dev < 0.1). Screen capture cannot capture real composited glass in current session.");
        }

        // 4. Compute Backdrop Diffusion & Transmittance if unglassed baseline provided
        double diffusionRatio = 0.0;
        bool passesTransmittance = true;
        if (unglassedBackdrop != null)
        {
            double gradGlass = ComputeMaxGradient(bitmap, left, top, width, height);
            double gradBackdrop = ComputeMaxGradient(unglassedBackdrop, left, top, width, height);
            diffusionRatio = gradBackdrop > 0.001 ? gradGlass / gradBackdrop : 0.0;

            // When backdrop has high-contrast features (circles, watermark text),
            // authentic glass must transmit modulated structural luminance (never opaque).
            if (gradBackdrop >= 4.0 && gradGlass < 0.3)
            {
                passesTransmittance = false;
            }
        }

        bool passesCoarseness = stdDev >= MinSpatialStdDev;
        bool passesRim = rimContrast >= MinRimContrast;
        bool passesDiffusion = unglassedBackdrop == null || (diffusionRatio <= MaxDiffusionRatio && passesTransmittance);

        bool isAuthentic = (passesCoarseness || passesRim) && passesTransmittance;

        string summary;
        if (!passesTransmittance)
        {
            summary = $"FAIL: Glass is opaque! Backdrop structure behind specimen is blocked (gradGlass < 0.3 under structured backdrop).";
        }
        else if (isAuthentic)
        {
            summary = $"PASS: Authentic SeaGlass optics confirmed (StdDev={stdDev:F2} >= {MinSpatialStdDev})";
        }
        else
        {
            summary = $"FAIL: Captured region is indistinguishable from flat fill! (StdDev={stdDev:F2} < {MinSpatialStdDev} and RimContrast={rimContrast:F2} < {MinRimContrast})";
        }

        return new OpticalVarianceReport(
            SpatialStdDev: stdDev,
            RimContrast: rimContrast,
            BackdropDiffusionRatio: diffusionRatio,
            PassesCoarseness: passesCoarseness,
            PassesRimHighlight: passesRim,
            IsAuthenticGlass: isAuthentic,
            Summary: summary);
    }

    private static double ComputeMaxGradient(CapturedWindowBitmap bmp, int left, int top, int width, int height)
    {
        double maxGrad = 0.0;
        for (int y = top + 1; y < top + height - 1; y++)
        {
            for (int x = left + 1; x < left + width - 1; x++)
            {
                int pRight = (y * bmp.Width + (x + 1)) * 4;
                int pLeft = (y * bmp.Width + (x - 1)) * 4;
                double lumRight = 0.2126 * bmp.BgraPixels[pRight + 2] + 0.7152 * bmp.BgraPixels[pRight + 1] + 0.0722 * bmp.BgraPixels[pRight];
                double lumLeft = 0.2126 * bmp.BgraPixels[pLeft + 2] + 0.7152 * bmp.BgraPixels[pLeft + 1] + 0.0722 * bmp.BgraPixels[pLeft];
                double gradX = Math.Abs(lumRight - lumLeft);
                if (gradX > maxGrad) maxGrad = gradX;
            }
        }
        return maxGrad;
    }
}
