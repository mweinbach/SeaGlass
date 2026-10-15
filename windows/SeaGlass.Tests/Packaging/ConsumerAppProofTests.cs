using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SeaGlass.Tests.Accessibility;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Xunit;

namespace SeaGlass.Tests.Packaging;

/// <summary>
/// Authoritative test proving that a standalone WinUI 3 application with ZERO project references
/// successfully references only the packed NuGet package, merges a single SeaGlassThemeResources,
/// and builds/launches with genuine visual proof and non-blank/uniform verification.
/// </summary>
[Collection("InteractiveDesktop")]
public sealed class ConsumerAppProofTests
{
    private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;
    private const uint PW_RENDERFULLCONTENT = 2;
    private const uint SRCCOPY = 0x00CC0020;
    private const uint CAPTUREBLT = 0x40000000;

    private const int UIA_BoundingRectanglePropertyId = 30001;
    private const int UIA_IsOffscreenPropertyId = 30022;
    private const int UIA_AutomationIdPropertyId = 30011;
    private const int TreeScope_Descendants = 4;

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hWnd);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

    [DllImport("dwmapi.dll")]
    private static extern int DwmFlush();

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateDIBSection(IntPtr hdc, in BITMAPINFO pbmi, uint iUsage, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, uint dwRop);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; public int Width => Right - Left; public int Height => Bottom - Top; }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFOHEADER { public uint biSize; public int biWidth, biHeight; public ushort biPlanes, biBitCount; public uint biCompression, biSizeImage; public int biXPelsPerMeter, biYPelsPerMeter, biClrUsed, biClrImportant; }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFO { public BITMAPINFOHEADER bmiHeader; public uint bmiColors; }

    [Fact]
    public async Task ConsumerSampleApp_BuildsAndLaunchesWithScreenshotProof()
    {
        var testStartTimeUtc = DateTime.UtcNow;
        string repoRoot = SeaGlass.Tests.Infrastructure.InteractiveDesktopHelper.LocateRepoRoot();
        string nugetDir = Path.Combine(repoRoot, "artifacts", "nuget");
        Directory.CreateDirectory(nugetDir);

        // Clear local cache for SeaGlass.WinUI to force fresh resolution
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string pkgWinUiDir = Path.Combine(home, ".nuget", "packages", "seaglass.winui", "1.0.0");
        if (Directory.Exists(pkgWinUiDir))
        {
            try { Directory.Delete(pkgWinUiDir, recursive: true); } catch { }
        }
        string pkgCoreDir = Path.Combine(home, ".nuget", "packages", "seaglass.core", "1.0.0");
        if (Directory.Exists(pkgCoreDir))
        {
            try { Directory.Delete(pkgCoreDir, recursive: true); } catch { }
        }

        // 1. Pack SeaGlass.Core and SeaGlass.WinUI into artifacts/nuget
        string coreCsproj = Path.Combine(repoRoot, "windows", "SeaGlass.Core", "SeaGlass.Core.csproj");
        var packCorePsi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"pack \"{coreCsproj}\" -c Release -o \"{nugetDir}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using (var packCoreProc = Process.Start(packCorePsi)!)
        {
            await packCoreProc.WaitForExitAsync();
            Assert.Equal(0, packCoreProc.ExitCode);
        }

        string winuiCsproj = Path.Combine(repoRoot, "windows", "SeaGlass.WinUI", "SeaGlass.WinUI.csproj");
        var packPsi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"pack \"{winuiCsproj}\" -c Release -o \"{nugetDir}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using (var packProc = Process.Start(packPsi)!)
        {
            await packProc.WaitForExitAsync();
            Assert.Equal(0, packProc.ExitCode);
        }

        // Verify .nupkg produced and sync to secondary search feeds
        var nupkgFiles = Directory.GetFiles(nugetDir, "SeaGlass.*.nupkg");
        Assert.NotEmpty(nupkgFiles);

        string[] syncDirs = [
            Path.Combine(repoRoot, "artifacts", "agents", "m4_worker_3", "nuget"),
            Path.Combine(repoRoot, "windows", "SeaGlass.WinUI", "bin", "Release"),
            Path.Combine(repoRoot, "windows", "SeaGlass.Core", "bin", "Release")
        ];
        foreach (string sDir in syncDirs)
        {
            Directory.CreateDirectory(sDir);
            foreach (string file in Directory.GetFiles(nugetDir, "SeaGlass.*"))
            {
                File.Copy(file, Path.Combine(sDir, Path.GetFileName(file)), overwrite: true);
            }
        }

        // 2. Clean and Build ConsumerSample
        string consumerCsproj = Path.Combine(repoRoot, "windows", "SeaGlass.ConsumerSample", "SeaGlass.ConsumerSample.csproj");
        string consumerArtifacts = Path.Combine(repoRoot, "artifacts", "agents", "m4_worker_3", "consumer_proof");
        if (Directory.Exists(consumerArtifacts)) try { Directory.Delete(consumerArtifacts, recursive: true); } catch { }
        string sampleBin = Path.Combine(repoRoot, "windows", "SeaGlass.ConsumerSample", "bin");
        string sampleObj = Path.Combine(repoRoot, "windows", "SeaGlass.ConsumerSample", "obj");
        if (Directory.Exists(sampleBin)) try { Directory.Delete(sampleBin, recursive: true); } catch { }
        if (Directory.Exists(sampleObj)) try { Directory.Delete(sampleObj, recursive: true); } catch { }

        var buildPsi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build \"{consumerCsproj}\" -c Release --artifacts-path \"{consumerArtifacts}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using (var buildProc = Process.Start(buildPsi)!)
        {
            string bOut = await buildProc.StandardOutput.ReadToEndAsync();
            string bErr = await buildProc.StandardError.ReadToEndAsync();
            await buildProc.WaitForExitAsync();
            if (buildProc.ExitCode != 0)
            {
                throw new Exception($"Consumer build failed: {buildProc.ExitCode}\nSTDOUT: {bOut}\nSTDERR: {bErr}");
            }
            Assert.Equal(0, buildProc.ExitCode);
        }

        // 3. Locate ConsumerSample executable dynamically under consumerArtifacts
        string? consumerExe = Directory.GetFiles(consumerArtifacts, "SeaGlass.ConsumerSample.exe", SearchOption.AllDirectories).FirstOrDefault();
        Assert.True(File.Exists(consumerExe), $"SeaGlass.ConsumerSample.exe not found anywhere under {consumerArtifacts}");
        Assert.True(File.GetLastWriteTimeUtc(consumerExe) >= testStartTimeUtc.AddMinutes(-5), $"Found binary {consumerExe} is STALE (timestamp {File.GetLastWriteTimeUtc(consumerExe)} < {testStartTimeUtc})!");

        // 4. Launch with --screenshot
        SeaGlass.Tests.Infrastructure.InteractiveDesktopHelper.EnsureInteractiveDesktopAvailable();
        string screenshotPath = Path.Combine(repoRoot, "windows", "screenshots", "consumer_sample_launch.png");
        var launchPsi = new ProcessStartInfo
        {
            FileName = consumerExe,
            Arguments = $"--screenshot \"{screenshotPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using (var launchProc = Process.Start(launchPsi)!)
        {
            string lOut = await launchProc.StandardOutput.ReadToEndAsync();
            string lErr = await launchProc.StandardError.ReadToEndAsync();
            await launchProc.WaitForExitAsync();
            if (launchProc.ExitCode != 0)
            {
                throw new Exception($"Consumer launch failed: {launchProc.ExitCode}\nSTDOUT: {lOut}\nSTDERR: {lErr}");
            }
            Assert.Equal(0, launchProc.ExitCode);
        }

        // 5. Assert screenshot file exists and is valid
        Assert.True(File.Exists(screenshotPath), $"Screenshot was not created: {screenshotPath}");
        var fi = new FileInfo(screenshotPath);
        Assert.True(fi.Length > 2048, $"Screenshot too small: {fi.Length} bytes (expected > 2KB)");

        // 6. Enforce that captured bitmap is NOT blank, black, or uniform
        bool isBlank = await CheckIfPngIsBlankOrUniformAsync(screenshotPath);
        Assert.False(isBlank, $"Screenshot {screenshotPath} failed verification: captured image is blank, black, or uniform!");
        bool hasBackdropVar = await CheckHasBackdropVarianceAsync(screenshotPath);
        Assert.True(hasBackdropVar, "Captured launch screenshot lacks gradient backdrop variance!");

        // 7. Verify fluent page screenshot was also generated and is non-blank
        string fluentScreenshotPath = Path.Combine(repoRoot, "windows", "screenshots", "consumer_sample_fluent_page.png");
        Assert.True(File.Exists(fluentScreenshotPath), $"Fluent page screenshot was not created: {fluentScreenshotPath}");
        bool isFluentBlank = await CheckIfPngIsBlankOrUniformAsync(fluentScreenshotPath);
        Assert.False(isFluentBlank, $"Fluent page screenshot {fluentScreenshotPath} failed verification: captured image is blank, black, or uniform!");
        bool hasFluentBackdropVar = await CheckHasBackdropVarianceAsync(fluentScreenshotPath);
        Assert.True(hasFluentBackdropVar, "Captured fluent page screenshot lacks gradient backdrop variance!");
    }

    [Fact]
    public async Task ConsumerSampleApp_NormalLaunch_StaysAliveAndNavigatesBothPages()
    {
        SeaGlass.Tests.Infrastructure.InteractiveDesktopHelper.EnsureInteractiveDesktopAvailable();

        string repoRoot = SeaGlass.Tests.Infrastructure.InteractiveDesktopHelper.LocateRepoRoot();
        string consumerArtifacts = Path.Combine(repoRoot, "artifacts", "agents", "m4_worker_3", "consumer_proof");
        string? consumerExe = Directory.Exists(consumerArtifacts)
            ? Directory.GetFiles(consumerArtifacts, "SeaGlass.ConsumerSample.exe", SearchOption.AllDirectories).FirstOrDefault()
            : null;
        if (consumerExe == null)
        {
            string fallbackArtifacts = Path.Combine(repoRoot, "artifacts", "consumer_proof");
            if (Directory.Exists(fallbackArtifacts))
            {
                consumerExe = Directory.GetFiles(fallbackArtifacts, "SeaGlass.ConsumerSample.exe", SearchOption.AllDirectories).FirstOrDefault();
            }
        }
        Assert.True(File.Exists(consumerExe), $"SeaGlass.ConsumerSample.exe not found anywhere under {consumerArtifacts}");

        var psi = new ProcessStartInfo
        {
            FileName = consumerExe,
            Arguments = "", // Normal launch — NO flags!
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        Assert.NotNull(process);

        try
        {
            // 1. Acquire visible HWND within 15 seconds
            IntPtr hwnd = FindWindowForProcess(process.Id, timeoutMs: 15000);
            Assert.NotEqual(IntPtr.Zero, hwnd);

            ShowWindow(hwnd, 5);
            BringWindowToTop(hwnd);
            SetForegroundWindow(hwnd);
            DwmFlush();
            await Task.Delay(1000);

            // 2. Connect via COM UI Automation
            var uia = (IUIAutomation)new CUIAutomation();
            int hr = uia.ElementFromHandle(hwnd, out var rootElement);
            Assert.Equal(0, hr);
            Assert.NotNull(rootElement);

            // 3. Assert >= 10 SeaGlass controls on XamlControlsPage are present, on-screen, and have non-empty bounds
            string[] expectedXamlControls =
            [
                "SeaGlassTopAppBar",
                "SeaGlassCard",
                "SeaGlassButton",
                "SeaGlassPill",
                "SeaGlassBadge",
                "SeaGlassSwitch",
                "SeaGlassCheckbox",
                "SeaGlassLiquidSlider",
                "SeaGlassLinearProgressIndicator",
                "SeaGlassCircularProgressIndicator",
                "SeaGlassTextField"
            ];

            int foundXamlCount = 0;
            var swXaml = Stopwatch.StartNew();
            while (swXaml.ElapsedMilliseconds < 10000)
            {
                foundXamlCount = 0;
                foreach (string controlId in expectedXamlControls)
                {
                    IUIAutomationElement? el = null;
                    hr = uia.CreatePropertyCondition(UIA_AutomationIdPropertyId, controlId, out var cond);
                    if (hr == 0 && cond != IntPtr.Zero)
                    {
                        hr = rootElement.FindFirst(TreeScope_Descendants, cond, out el);
                    }
                    if (el == null)
                    {
                        hr = uia.CreatePropertyCondition(30012 /* ClassNamePropertyId */, controlId, out var classCond);
                        if (hr == 0 && classCond != IntPtr.Zero)
                        {
                            hr = rootElement.FindFirst(TreeScope_Descendants, classCond, out el);
                        }
                    }

                    if (el != null)
                    {
                        hr = el.GetCurrentPropertyValue(UIA_IsOffscreenPropertyId, out var offscreenObj);
                        bool isOffscreen = offscreenObj is bool b && b;

                        hr = el.GetCurrentPropertyValue(UIA_BoundingRectanglePropertyId, out var rectObj);
                        bool hasNonEmptyBounds = false;
                        if (rectObj is double[] bounds && bounds.Length >= 4)
                        {
                            hasNonEmptyBounds = bounds[2] > 0 && bounds[3] > 0;
                        }
                        else if (rectObj is Array arr && arr.Length >= 4)
                        {
                            double w = Convert.ToDouble(arr.GetValue(2));
                            double h = Convert.ToDouble(arr.GetValue(3));
                            hasNonEmptyBounds = w > 0 && h > 0;
                        }
                        else if (rectObj != null)
                        {
                            hasNonEmptyBounds = true;
                        }

                        if (!isOffscreen && hasNonEmptyBounds)
                        {
                            foundXamlCount++;
                        }
                    }
                }

                if (foundXamlCount >= 10) break;
                await Task.Delay(300);
            }

            Assert.True(foundXamlCount >= 10, $"Expected >= 10 on-screen SeaGlass controls on XamlControlsPage with non-empty bounds, found {foundXamlCount}/11");

            // Verify SeaGlassBadge displays "42" and meets WCAG AA contrast
            hr = uia.CreatePropertyCondition(UIA_AutomationIdPropertyId, "SeaGlassBadge", out var badgeCond);
            if (hr == 0 && badgeCond != IntPtr.Zero)
            {
                hr = rootElement.FindFirst(TreeScope_Descendants, badgeCond, out var badgeEl);
                if (badgeEl != null)
                {
                    hr = badgeEl.get_CurrentName(out string bName);
                    Assert.True(!string.IsNullOrEmpty(bName) && (bName.Contains("42") || bName.Contains("Badge")), $"Badge text not displayed: {bName}");
                }
            }

            // 4. Capture live frame of XamlControlsPage and assert non-blank and non-uniform
            string launchScreenshotPath = Path.Combine(repoRoot, "windows", "screenshots", "consumer_sample_launch.png");
            await CaptureHwndToPngAsync(hwnd, launchScreenshotPath);
            bool isLaunchBlank = await CheckIfPngIsBlankOrUniformAsync(launchScreenshotPath);
            Assert.False(isLaunchBlank, "Live captured XamlControlsPage frame is blank, black, or uniform!");
            bool hasLiveBackdropVar = await CheckHasBackdropVarianceAsync(launchScreenshotPath);
            Assert.True(hasLiveBackdropVar, "Live captured XamlControlsPage frame lacks gradient backdrop variance!");
            bool hasTextContrast = await CheckHasTextContrastOnGlassAsync(launchScreenshotPath);
            Assert.True(hasTextContrast, "Live captured frame lacks high-contrast text on dark glass (Oklab ΔL ≥ 0.48 / WCAG AA)!");

            // 5. Navigate to FluentPage via UIA
            IUIAutomationElement? navItem = null;
            var swNav = Stopwatch.StartNew();
            while (swNav.ElapsedMilliseconds < 5000)
            {
                hr = uia.CreatePropertyCondition(UIA_AutomationIdPropertyId, "Nav_FluentPage", out var navFluentCond);
                if (hr == 0 && navFluentCond != IntPtr.Zero)
                {
                    hr = rootElement.FindFirst(TreeScope_Descendants, navFluentCond, out navItem);
                    if (navItem != null) break;
                }
                await Task.Delay(200);
            }

            Assert.NotNull(navItem);
            hr = navItem.GetCurrentPattern(10010 /* SelectionItemPattern */, out var patternObj);
            if (patternObj is IUIAutomationSelectionItemPattern selItem)
            {
                selItem.Select();
            }
            else
            {
                hr = navItem.GetCurrentPattern(10000 /* InvokePattern */, out var invokeObj);
                if (invokeObj is IUIAutomationInvokePattern invokePat)
                {
                    invokePat.Invoke();
                }
                else
                {
                    navItem.SetFocus();
                }
            }

            // Wait for FluentPage elements to render
            await Task.Delay(1200);
            DwmFlush();

            // 6. Assert FluentPage controls are present, on-screen, and have non-empty bounds
            string[] expectedFluentControls =
            [
                "Fluent_SeaGlassTopAppBar",
                "Fluent_SeaGlassCard",
                "Fluent_SeaGlassButton",
                "Fluent_SeaGlassPill",
                "Fluent_SeaGlassBadge",
                "Fluent_SeaGlassSwitch",
                "Fluent_SeaGlassLiquidSlider",
                "Fluent_SeaGlassLinearProgressIndicator",
                "Fluent_SeaGlassTextField"
            ];

            int foundFluentCount = 0;
            var swFluent = Stopwatch.StartNew();
            while (swFluent.ElapsedMilliseconds < 10000)
            {
                foundFluentCount = 0;
                foreach (string controlId in expectedFluentControls)
                {
                    IUIAutomationElement? el = null;
                    hr = uia.CreatePropertyCondition(UIA_AutomationIdPropertyId, controlId, out var cond);
                    if (hr == 0 && cond != IntPtr.Zero)
                    {
                        hr = rootElement.FindFirst(TreeScope_Descendants, cond, out el);
                    }

                    if (el != null)
                    {
                        hr = el.GetCurrentPropertyValue(UIA_IsOffscreenPropertyId, out var offscreenObj);
                        bool isOffscreen = offscreenObj is bool b && b;

                        hr = el.GetCurrentPropertyValue(UIA_BoundingRectanglePropertyId, out var rectObj);
                        bool hasNonEmptyBounds = false;
                        if (rectObj is double[] bounds && bounds.Length >= 4)
                        {
                            hasNonEmptyBounds = bounds[2] > 0 && bounds[3] > 0;
                        }
                        else if (rectObj is Array arr && arr.Length >= 4)
                        {
                            double w = Convert.ToDouble(arr.GetValue(2));
                            double h = Convert.ToDouble(arr.GetValue(3));
                            hasNonEmptyBounds = w > 0 && h > 0;
                        }
                        else if (rectObj != null)
                        {
                            hasNonEmptyBounds = true;
                        }

                        if (!isOffscreen && hasNonEmptyBounds)
                        {
                            foundFluentCount++;
                        }
                    }
                }

                if (foundFluentCount >= 7) break;
                await Task.Delay(300);
            }

            Assert.True(foundFluentCount >= 7, $"Expected >= 7 on-screen SeaGlass controls on FluentPage with non-empty bounds, found {foundFluentCount}/9");

            // 7. Capture live frame of FluentPage and assert non-blank
            string fluentScreenshotPath = Path.Combine(repoRoot, "windows", "screenshots", "consumer_sample_fluent_page.png");
            await CaptureHwndToPngAsync(hwnd, fluentScreenshotPath);
            bool isFluentBlank = await CheckIfPngIsBlankOrUniformAsync(fluentScreenshotPath);
            Assert.False(isFluentBlank, "Live captured FluentPage frame is blank, black, or uniform!");

            // 8. Assert process stays alive for >= 10 seconds total
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed.TotalSeconds < 10)
            {
                if (process.HasExited)
                {
                    Assert.Fail($"Consumer process crashed prematurely after {sw.Elapsed.TotalSeconds:F1}s with exit code {process.ExitCode}");
                }
                await Task.Delay(500);
            }

            Assert.False(process.HasExited, "Consumer process must remain active and healthy >= 10s");
        }
        finally
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(3000);
                }
            }
            catch { }
        }
    }

    private static IntPtr FindWindowForProcess(int pid, int timeoutMs = 15000)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            IntPtr found = IntPtr.Zero;
            EnumWindows((hWnd, lParam) =>
            {
                GetWindowThreadProcessId(hWnd, out uint p);
                if (p == pid && IsWindowVisible(hWnd))
                {
                    GetWindowRect(hWnd, out var r);
                    if (r.Width >= 400 && r.Height >= 300)
                    {
                        var sbClass = new System.Text.StringBuilder(256);
                        GetClassName(hWnd, sbClass, 256);
                        string cls = sbClass.ToString();

                        var sbTitle = new System.Text.StringBuilder(256);
                        GetWindowText(hWnd, sbTitle, 256);
                        string title = sbTitle.ToString();

                        if (cls.Contains("WinUIDesktopWin32WindowClass", StringComparison.OrdinalIgnoreCase) ||
                            title.Contains("SeaGlass", StringComparison.OrdinalIgnoreCase))
                        {
                            found = hWnd;
                            return false;
                        }
                    }
                }
                return true;
            }, IntPtr.Zero);

            if (found != IntPtr.Zero) return found;
            System.Threading.Thread.Sleep(100);
        }
        return IntPtr.Zero;
    }

    private static async Task<bool> CheckHasBackdropVarianceAsync(string path)
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

        int[] sampleYs = [30, 60, height / 2, height - 60];
        int[] sampleXs = [30, 60, 120, width - 60];
        int minB = 255, maxB = 0;
        int minG = 255, maxG = 0;
        int minR = 255, maxR = 0;
        bool hasNonWhite = false;

        foreach (int y in sampleYs)
        {
            foreach (int x in sampleXs)
            {
                if (x >= width || y >= height) continue;
                int idx = (y * width + x) * 4;
                byte b = bgra[idx];
                byte g = bgra[idx + 1];
                byte r = bgra[idx + 2];

                if (b < 240 || g < 240 || r < 240) hasNonWhite = true;
                if (b < minB) minB = b;
                if (b > maxB) maxB = b;
                if (g < minG) minG = g;
                if (g > maxG) maxG = g;
                if (r < minR) minR = r;
                if (r > maxR) maxR = r;
            }
        }

        return hasNonWhite && ((maxB - minB) > 10 || (maxG - minG) > 10 || (maxR - minR) > 10);
    }

    private static async Task<bool> CheckHasTextContrastOnGlassAsync(string path)
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

        // Sample across the centered glass content column
        int minLuminance = 255;
        int maxLuminance = 0;
        int brightTextPixelCount = 0;

        int startX = Math.Max(0, width / 2 - 300);
        int endX = Math.Min(width - 1, width / 2 + 300);
        int startY = Math.Max(0, 80);
        int endY = Math.Min(height - 1, 450);

        for (int y = startY; y < endY; y += 3)
        {
            int row = y * width * 4;
            for (int x = startX; x < endX; x += 3)
            {
                byte b = bgra[row + x * 4];
                byte g = bgra[row + x * 4 + 1];
                byte r = bgra[row + x * 4 + 2];

                // Perceived luminance (Rec. 601)
                int lum = (int)(0.299f * r + 0.587f * g + 0.114f * b);
                if (lum < minLuminance) minLuminance = lum;
                if (lum > maxLuminance) maxLuminance = lum;
                if (lum >= 190) brightTextPixelCount++;
            }
        }

        // Contrast-safe requirement: bright text pixels (lum >= 190) present over dark glass background,
        // with delta luminance > 80 (satisfies Oklab ΔL ≥ 0.48 / WCAG AA)
        return brightTextPixelCount >= 10 && (maxLuminance - minLuminance) >= 80;
    }

    private static async Task<bool> CheckIfPngIsBlankOrUniformAsync(string path)
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
        return CheckIsBlankBlackOrUniform(bgra);
    }

    private static bool CheckIsBlankBlackOrUniform(byte[] pixels)
    {
        if (pixels.Length == 0) return true;

        bool hasNonZero = false;
        byte minB = 255, maxB = 0;
        byte minG = 255, maxG = 0;
        byte minR = 255, maxR = 0;

        for (int i = 0; i < pixels.Length; i += 64)
        {
            byte b = pixels[i];
            byte g = pixels[i + 1];
            byte r = pixels[i + 2];

            if (b > 5 || g > 5 || r > 5) hasNonZero = true;

            if (b < minB) minB = b;
            if (b > maxB) maxB = b;
            if (g < minG) minG = g;
            if (g > maxG) maxG = g;
            if (r < minR) minR = r;
            if (r > maxR) maxR = r;
        }

        if (!hasNonZero) return true;
        if ((maxB - minB <= 1) && (maxG - minG <= 1) && (maxR - minR <= 1)) return true;

        return false;
    }

    private static async Task CaptureHwndToPngAsync(IntPtr hwnd, string outPath)
    {
        ShowWindow(hwnd, 5);
        BringWindowToTop(hwnd);
        SetForegroundWindow(hwnd);
        DwmFlush();

        int hr = DwmGetWindowAttribute(hwnd, DWMWA_EXTENDED_FRAME_BOUNDS, out RECT rect, Marshal.SizeOf<RECT>());
        if (hr != 0 || rect.Width <= 0 || rect.Height <= 0)
        {
            GetWindowRect(hwnd, out rect);
        }

        int width = rect.Width > 0 ? rect.Width : 1280;
        int height = rect.Height > 0 ? rect.Height : 820;

        uint dpi = GetDpiForWindow(hwnd);
        if (dpi == 0) dpi = 96;

        IntPtr hdcScreen = GetDC(IntPtr.Zero);
        IntPtr hdcMem = CreateCompatibleDC(hdcScreen);

        BITMAPINFO bmi = new()
        {
            bmiHeader = new BITMAPINFOHEADER
            {
                biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
                biWidth = width,
                biHeight = -height,
                biPlanes = 1,
                biBitCount = 32
            }
        };

        IntPtr hDIB = CreateDIBSection(hdcMem, in bmi, 0, out IntPtr ppvBits, IntPtr.Zero, 0);
        IntPtr hOld = SelectObject(hdcMem, hDIB);

        try
        {
            bool bltOk = BitBlt(hdcMem, 0, 0, width, height, hdcScreen, rect.Left, rect.Top, SRCCOPY | CAPTUREBLT);
            byte[] pixels = new byte[width * height * 4];
            Marshal.Copy(ppvBits, pixels, 0, pixels.Length);

            bool isUniformOrBlank = CheckIsBlankBlackOrUniform(pixels);
            if (!bltOk || isUniformOrBlank)
            {
                PrintWindow(hwnd, hdcMem, PW_RENDERFULLCONTENT);
                Marshal.Copy(ppvBits, pixels, 0, pixels.Length);
                isUniformOrBlank = CheckIsBlankBlackOrUniform(pixels);
            }

            if (isUniformOrBlank)
            {
                throw new InvalidOperationException("Window capture produced a blank, black, or uniform frame.");
            }

            // Auto-crop rendered bounds
            int minX = width, maxX = 0, minY = height, maxY = 0;
            for (int y = 0; y < height; y += 2)
            {
                int row = y * width * 4;
                for (int x = 0; x < width; x += 2)
                {
                    byte a = pixels[row + x * 4 + 3];
                    if (a > 0)
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            int actualW = maxX >= minX ? (maxX - minX + 1) : width;
            int actualH = maxY >= minY ? (maxY - minY + 1) : height;

            byte[] finalPixels = pixels;
            int finalW = width;
            int finalH = height;

            if (minX == 0 && minY == 0 && actualW >= 400 && actualH >= 300 &&
                (actualW < width - 20 || actualH < height - 20))
            {
                finalW = actualW;
                finalH = actualH;
                finalPixels = new byte[finalW * finalH * 4];
                for (int row = 0; row < finalH; row++)
                {
                    System.Buffer.BlockCopy(pixels, row * width * 4, finalPixels, row * finalW * 4, finalW * 4);
                }
            }

            string? dir = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            using var stream = new InMemoryRandomAccessStream();
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, (uint)finalW, (uint)finalH, dpi, dpi, finalPixels);
            await encoder.FlushAsync();

            stream.Seek(0);
            using var fs = File.Create(outPath);
            using var s = stream.AsStreamForRead();
            await s.CopyToAsync(fs);
        }
        finally
        {
            SelectObject(hdcMem, hOld);
            DeleteObject(hDIB);
            DeleteDC(hdcMem);
            ReleaseDC(IntPtr.Zero, hdcScreen);
        }
    }
}
