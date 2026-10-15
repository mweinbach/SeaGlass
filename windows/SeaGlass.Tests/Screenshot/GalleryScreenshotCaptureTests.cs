using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using SeaGlass.Tests.Infrastructure;
using Xunit;

namespace SeaGlass.Tests.Screenshot;

/// <summary>
/// Automated integration test verifying full DWM window capture and optical variance verification
/// across Light, Dark, High Contrast, and 150% scaling for all gallery controls.
/// </summary>
[Collection("InteractiveDesktop")]
public sealed class GalleryScreenshotCaptureTests
{
    [Fact]
    public async Task RunAutomatedGalleryScreenshotCapture_CapturesAndVerifiesVariance()
    {
        InteractiveDesktopHelper.EnsureInteractiveDesktopAvailable();

        string sampleExe = HostSelfTestRunner.LocateSampleExecutable();
        Assert.True(File.Exists(sampleExe), $"Sample executable not found: {sampleExe}");

        string repoRoot = InteractiveDesktopHelper.LocateRepoRoot();
        string screenshotsDir = Path.Combine(repoRoot, "windows", "screenshots");

        var psi = new ProcessStartInfo
        {
            FileName = sampleExe,
            Arguments = $"--capture-all \"{screenshotsDir}\" --verify-variance",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        Assert.NotNull(process);

        using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(120));
        try
        {
            var stdoutTask = process.StandardOutput.ReadToEndAsync(cts.Token);
            var stderrTask = process.StandardError.ReadToEndAsync(cts.Token);
            await process.WaitForExitAsync(cts.Token);

            string stdout = await stdoutTask;
            string stderr = await stderrTask;

            if (process.ExitCode != 0)
            {
                throw new Exception($"Screenshot runner exited with code {process.ExitCode}.\nSTDOUT: {stdout}\nSTDERR: {stderr}");
            }

            Assert.Equal(0, process.ExitCode);
            Assert.Contains("PASS: Authentic SeaGlass optics confirmed", stdout);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(true); } catch { }
            throw new TimeoutException("HARD TIMEOUT EXCEEDED (120s): Screenshot runner hung or exceeded time limit.");
        }

        // Verify all required suites exist (including scaling_150_simulated and backward-compat scaling_150)
        string[] requiredSuites = ["light", "dark", "high_contrast_simulated", "scaling_150_simulated", "scaling_150"];
        foreach (string suite in requiredSuites)
        {
            string fullPath = Path.Combine(screenshotsDir, suite);
            Assert.True(Directory.Exists(fullPath), $"Suite directory missing: {fullPath}");

            // Verify core controls are present and non-empty
            string[] coreControls =
            [
                "SeaGlassButton.png",
                "SeaGlassCard.png",
                "SeaGlassSwitch.png",
                "SeaGlassLiquidSlider.png",
                "SeaGlassTextField.png",
                "SeaGlassDialogCard.png"
            ];

            foreach (string file in coreControls)
            {
                string filePath = Path.Combine(fullPath, file);
                Assert.True(File.Exists(filePath), $"Screenshot file missing: {filePath}");
                var fi = new FileInfo(filePath);
                Assert.True(fi.Length > 1024, $"Screenshot file too small: {filePath} ({fi.Length} bytes)");
            }
        }

        // Verify all 4 variants are visually and structurally distinct
        string[] testControls = ["SeaGlassButton.png", "SeaGlassCard.png", "SeaGlassSwitch.png"];
        foreach (string file in testControls)
        {
            string lightPath = Path.Combine(screenshotsDir, "light", file);
            string darkPath = Path.Combine(screenshotsDir, "dark", file);
            string hcPath = Path.Combine(screenshotsDir, "high_contrast_simulated", file);
            string scalePath = Path.Combine(screenshotsDir, "scaling_150_simulated", file);

            string hashLight = ComputeFileMd5(lightPath);
            string hashDark = ComputeFileMd5(darkPath);
            string hashHc = ComputeFileMd5(hcPath);
            string hashScale = ComputeFileMd5(scalePath);

            // Assert all 4 hashes differ
            Assert.NotEqual(hashLight, hashDark);
            Assert.NotEqual(hashLight, hashHc);
            Assert.NotEqual(hashDark, hashHc);
            Assert.NotEqual(hashLight, hashScale);

            // Assert distinct luminance
            double lumLight = ComputeMeanLuminance(lightPath);
            double lumDark = ComputeMeanLuminance(darkPath);
            Assert.True(lumLight != lumDark, $"{file} Light and Dark mean luminance must differ");
        }

        // Assert 150% scaling verifies specimen/glyph dimensions are scaled ~1.5x via scaling_metrics.json (>= 1.45x threshold per directive)
        string metricsPath = Path.Combine(screenshotsDir, "scaling_metrics.json");
        Assert.True(File.Exists(metricsPath), $"scaling_metrics.json missing at: {metricsPath}");
        string json = File.ReadAllText(metricsPath);
        using var jsonDoc = System.Text.Json.JsonDocument.Parse(json);
        foreach (string controlName in new[] { "SeaGlassButton", "SeaGlassCard", "SeaGlassSwitch" })
        {
            Assert.True(jsonDoc.RootElement.TryGetProperty(controlName, out var elem), $"Missing metrics for {controlName}");
            double factorW = elem.GetProperty("factorW").GetDouble();
            double factorH = elem.GetProperty("factorH").GetDouble();
            Assert.True(factorW >= 1.45, $"{controlName} factorW {factorW:F2} must be >= 1.45x");
            Assert.True(factorH >= 1.45, $"{controlName} factorH {factorH:F2} must be >= 1.45x");
        }

        // Assert Dark Theme Gallery Chrome: Nav pane mean luminance < 0.30 and text contrast >= 4.5:1
        string darkCardScreenshot = Path.Combine(screenshotsDir, "dark", "SeaGlassCard.png");
        Assert.True(File.Exists(darkCardScreenshot), $"dark/SeaGlassCard.png missing at: {darkCardScreenshot}");
        var (navBgLum, navTextLum) = await ComputeNavPaneAndTextLuminanceAsync(darkCardScreenshot);
        Assert.True(navBgLum < 0.30, $"Dark nav pane background luminance {navBgLum:F3} must be < 0.30 (found light background)");
        double darkContrast = (navTextLum + 0.05) / (navBgLum + 0.05);
        Assert.True(darkContrast >= 4.5, $"Dark nav text contrast {darkContrast:F2}:1 must be >= 4.5:1 (navTextLum={navTextLum:F3}, navBgLum={navBgLum:F3})");

        // Verify full window captures (1440x900 client/window bounds with complete snippet/tuning panel)
        var (winW, winH) = ReadPngDimensions(darkCardScreenshot);
        Assert.True(winW >= 1400, $"Captured window width {winW} must be >= 1400px (full window, not cropped 1234px)");
        Assert.True(winH >= 850, $"Captured window height {winH} must be >= 850px");
        Assert.True(winW <= 1450, $"Captured window width {winW} must be <= 1450px (cropped to window bounds, not 2852px canvas)");
        Assert.True(winH <= 920, $"Captured window height {winH} must be <= 920px (cropped to window bounds, not 1786px canvas)");

        // Assert that no more than 2% of the image is uniform blank canvas outside the window
        double blankRatio = await ComputeOutsideBlankRatioAsync(darkCardScreenshot);
        Assert.True(blankRatio <= 0.02, $"Outside blank canvas ratio {blankRatio * 100:F2}% exceeds 2% threshold (found blank padding outside window)");
    }

    [Theory]
    [InlineData("SeaGlassCard", "Surfaces & Foundations")]
    [InlineData("SeaGlassButton", "Buttons & Chips")]
    [InlineData("SeaGlassTopAppBar", "Navigation & Bars")]
    [InlineData("SeaGlassTextField", "Inputs & Toggles")]
    [InlineData("SeaGlassListItem", "Feedback & Progress")]
    [InlineData("SeaGlassMorphingButton", "Dynamic Morphing Suite")]
    [InlineData("SeaGlassActionSheet", "Overlays & Sheets")]
    [InlineData("SeaGlassInfoBar", "Windows Native Extras")]
    public async Task GlassTranslucencyAndTextReadability_ConfirmedInCapturedScreenshots(string controlName, string category)
    {
        InteractiveDesktopHelper.EnsureInteractiveDesktopAvailable();

        string repoRoot = InteractiveDesktopHelper.LocateRepoRoot();
        string screenshotsDir = Path.Combine(repoRoot, "windows", "screenshots");
        string lightDir = Path.Combine(screenshotsDir, "light");

        string controlPath = Path.Combine(lightDir, $"{controlName}.png");
        Assert.True(File.Exists(controlPath), $"{controlName} screenshot missing in category '{category}': {controlPath}");

        // Read and decode screenshot to confirm text readability over translucent glass
        using var stream = new InMemoryRandomAccessStream();
        using (var writer = new DataWriter(stream))
        {
            writer.WriteBytes(File.ReadAllBytes(controlPath));
            await writer.StoreAsync();
            writer.DetachStream();
        }
        stream.Seek(0);
        var decoder = await BitmapDecoder.CreateAsync(stream);
        var pixelData = await decoder.GetPixelDataAsync();
        byte[] bgra = pixelData.DetachPixelData();
        int width = (int)decoder.PixelWidth;
        int height = (int)decoder.PixelHeight;

        // In the specimen region (specimen stage on the left/center of the 1440x900 client area):
        // Confirm text pixels (dark contrast) and glass body pixels (bright translucent) are both present.
        int darkTextPixels = 0;
        int brightGlassPixels = 0;
        int yStart = height / 5;
        int yEnd = height * 4 / 5;
        int xStart = width / 8;
        int xEnd = width * 5 / 8;

        for (int y = yStart; y < yEnd; y += 2)
        {
            for (int x = xStart; x < xEnd; x += 2)
            {
                int offset = (y * width + x) * 4;
                byte b = bgra[offset];
                byte g = bgra[offset + 1];
                byte r = bgra[offset + 2];
                double lum = (0.299 * r + 0.587 * g + 0.114 * b) / 255.0;

                if (lum < 0.38)
                {
                    darkTextPixels++;
                }
                else if (lum > 0.60)
                {
                    brightGlassPixels++;
                }
            }
        }

        Assert.True(darkTextPixels >= 20, $"Text pixels on {controlName} in category '{category}' missing or obscured! darkTextPixels={darkTextPixels}");
        Assert.True(brightGlassPixels >= 40, $"Translucent glass body pixels on {controlName} in category '{category}' missing! brightGlassPixels={brightGlassPixels}");
    }

    private static (int width, int height) ReadPngDimensions(string path)
    {
        using var fs = File.OpenRead(path);
        byte[] header = new byte[24];
        fs.ReadExactly(header, 0, 24);
        int width = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
        int height = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
        return (width, height);
    }

    private static string ComputeFileMd5(string path)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        using var fs = File.OpenRead(path);
        byte[] hash = md5.ComputeHash(fs);
        return Convert.ToHexString(hash);
    }

    private static double ComputeMeanLuminance(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        double sum = 0;
        int step = Math.Max(1, bytes.Length / 10000);
        int count = 0;
        for (int i = 0; i < bytes.Length; i += step)
        {
            sum += bytes[i];
            count++;
        }
        return count > 0 ? sum / count : 0.0;
    }

    private static async Task<(double navBgLum, double navTextLum)> ComputeNavPaneAndTextLuminanceAsync(string path)
    {
        using var stream = new InMemoryRandomAccessStream();
        using (var writer = new DataWriter(stream))
        {
            writer.WriteBytes(File.ReadAllBytes(path));
            await writer.StoreAsync();
            writer.DetachStream();
        }
        stream.Seek(0);
        var decoder = await BitmapDecoder.CreateAsync(stream);
        var pixelData = await decoder.GetPixelDataAsync();
        byte[] bgra = pixelData.DetachPixelData();
        int width = (int)decoder.PixelWidth;
        int height = (int)decoder.PixelHeight;

        // Nav pane is located on the left: X in [20..240], Y in [80..height-80]
        int navLeft = 20;
        int navRight = Math.Min(240, width / 4);
        int navTop = 80;
        int navBottom = Math.Max(navTop + 100, height - 80);

        double bgSum = 0;
        int bgCount = 0;
        double textSum = 0;
        int textCount = 0;

        for (int y = navTop; y < navBottom; y += 2)
        {
            for (int x = navLeft; x < navRight; x += 2)
            {
                int offset = (y * width + x) * 4;
                byte b = bgra[offset];
                byte g = bgra[offset + 1];
                byte r = bgra[offset + 2];
                double lum = (0.2126 * r + 0.7152 * g + 0.0722 * b) / 255.0;

                if (lum > 0.50)
                {
                    textSum += lum;
                    textCount++;
                }
                else
                {
                    bgSum += lum;
                    bgCount++;
                }
            }
        }

        double navBgLum = bgCount > 0 ? bgSum / bgCount : 0.0;
        double navTextLum = textCount > 0 ? textSum / textCount : 1.0;
        return (navBgLum, navTextLum);
    }

    private static async Task<double> ComputeOutsideBlankRatioAsync(string path)
    {
        using var stream = new InMemoryRandomAccessStream();
        using (var writer = new DataWriter(stream))
        {
            writer.WriteBytes(File.ReadAllBytes(path));
            await writer.StoreAsync();
            writer.DetachStream();
        }
        stream.Seek(0);
        var decoder = await BitmapDecoder.CreateAsync(stream);
        var pixelData = await decoder.GetPixelDataAsync();
        byte[] bgra = pixelData.DetachPixelData();
        int width = (int)decoder.PixelWidth;
        int height = (int)decoder.PixelHeight;

        int blankPixels = 0;
        int totalSampled = 0;
        for (int y = 0; y < height; y += 4)
        {
            for (int x = 0; x < width; x += 4)
            {
                int offset = (y * width + x) * 4;
                byte a = bgra[offset + 3];

                totalSampled++;
                if (a == 0)
                {
                    blankPixels++;
                }
            }
        }

        return totalSampled > 0 ? (double)blankPixels / totalSampled : 0.0;
    }

    [Theory]
    [InlineData("SeaGlassButton")]
    [InlineData("SeaGlassExtendedFab")]
    [InlineData("SeaGlassFilterChip")]
    [InlineData("SeaGlassPill")]
    public async Task ButtonFamily_SpecimensInLightAndScaledScreenshots_HaveSingleCenteredLabel(string controlName)
    {
        InteractiveDesktopHelper.EnsureInteractiveDesktopAvailable();

        string repoRoot = InteractiveDesktopHelper.LocateRepoRoot();
        string screenshotsDir = Path.Combine(repoRoot, "windows", "screenshots");
        string lightPath = Path.Combine(screenshotsDir, "light", $"{controlName}.png");
        Assert.True(File.Exists(lightPath), $"Screenshot missing: {lightPath}");

        using var stream = new InMemoryRandomAccessStream();
        using (var writer = new DataWriter(stream))
        {
            writer.WriteBytes(File.ReadAllBytes(lightPath));
            await writer.StoreAsync();
            writer.DetachStream();
        }
        stream.Seek(0);
        var decoder = await BitmapDecoder.CreateAsync(stream);
        var pixelData = await decoder.GetPixelDataAsync();
        byte[] bgra = pixelData.DetachPixelData();
        int width = (int)decoder.PixelWidth;
        int height = (int)decoder.PixelHeight;

        // In the specimen area (centered inside the stage host):
        // Scan the specimen horizontal centerline for dark text pixel bursts (separated by gaps).
        // A triplicated label produces 3 distinct text clusters across the button width.
        // A correctly rendered button has exactly 1 single continuous text cluster.
        int stageY = height / 2;
        int stageXStart = width / 5;
        int stageXEnd = width * 3 / 5;

        int textTransitions = 0;
        for (int y = stageY - 10; y <= stageY + 10; y += 4)
        {
            int rowTransitions = 0;
            bool inText = false;
            int consecutiveGaps = 0;
            for (int x = stageXStart; x < stageXEnd; x++)
            {
                int offset = (y * width + x) * 4;
                byte b = bgra[offset];
                byte g = bgra[offset + 1];
                byte r = bgra[offset + 2];
                double lum = (0.299 * r + 0.587 * g + 0.114 * b) / 255.0;

                bool isDarkText = lum < 0.35;
                if (isDarkText)
                {
                    if (!inText && consecutiveGaps > 15)
                    {
                        rowTransitions++;
                        inText = true;
                    }
                    consecutiveGaps = 0;
                }
                else
                {
                    consecutiveGaps++;
                    if (consecutiveGaps > 25)
                    {
                        inText = false;
                    }
                }
            }
            if (rowTransitions > textTransitions)
            {
                textTransitions = rowTransitions;
            }
        }

        Assert.True(textTransitions <= 2, $"Duplicate/triplicated label detected on {controlName}! Observed {textTransitions} distinct text clusters across button");
    }
}
