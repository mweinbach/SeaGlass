using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace SeaGlass.ConsumerSample;

public static class Program
{
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

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; public int Width => Right - Left; public int Height => Bottom - Top; }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFOHEADER { public uint biSize; public int biWidth, biHeight; public ushort biPlanes, biBitCount; public uint biCompression, biSizeImage; public int biXPelsPerMeter, biYPelsPerMeter, biClrUsed, biClrImportant; }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFO { public BITMAPINFOHEADER bmiHeader; public uint bmiColors; }

    private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;
    private const uint PW_RENDERFULLCONTENT = 2;
    private const uint SRCCOPY = 0x00CC0020;
    private const uint CAPTUREBLT = 0x40000000;

    [STAThread]
    public static int Main(string[] args)
    {
        var minVersion = new Microsoft.Windows.ApplicationModel.DynamicDependency.PackageVersion(0x0002000500010000);
        Microsoft.Windows.ApplicationModel.DynamicDependency.Bootstrap.TryInitialize(
            0x00020005, "", minVersion,
            Microsoft.Windows.ApplicationModel.DynamicDependency.Bootstrap.InitializeOptions.None,
            out _);

        WinRT.ComWrappersSupport.InitializeComWrappers();

        bool isScreenshot = args.Contains("--screenshot", StringComparer.OrdinalIgnoreCase);

        if (isScreenshot)
        {
            string outPath = "windows/screenshots/consumer_sample_launch.png";
            int idx = Array.FindIndex(args, a => string.Equals(a, "--screenshot", StringComparison.OrdinalIgnoreCase));
            if (idx >= 0 && idx + 1 < args.Length)
            {
                outPath = args[idx + 1];
            }

            int exitCode = 0;
            try
            {
                Application.Start(p =>
                {
                    try
                    {
                        var queue = DispatcherQueue.GetForCurrentThread();
                        var context = new DispatcherQueueSynchronizationContext(queue);
                        SynchronizationContext.SetSynchronizationContext(context);
                        _ = new App();

                        queue.TryEnqueue(async () =>
                        {
                            try
                            {
                                var window = App.MainWindowInstance ?? new MainWindow();
                                window.Activate();
                                IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

                                ShowWindow(hwnd, 5);
                                BringWindowToTop(hwnd);
                                SetForegroundWindow(hwnd);
                                await Task.Delay(600);
                                DwmFlush();

                                // 1. Capture XAML controls launch frame
                                await CaptureAndSaveWindowAsync(hwnd, outPath);
                                Console.WriteLine($"[CONSUMER_SAMPLE] Launch screenshot saved to: {outPath}");

                                // 2. If outPath matches standard launch, also capture fluent page
                                string fluentOutPath = Path.Combine(Path.GetDirectoryName(outPath) ?? "", "consumer_sample_fluent_page.png");
                                window.SelectPage("fluent");
                                await Task.Delay(500);
                                DwmFlush();
                                await CaptureAndSaveWindowAsync(hwnd, fluentOutPath);
                                Console.WriteLine($"[CONSUMER_SAMPLE] Fluent page screenshot saved to: {fluentOutPath}");
                            }
                            catch (Exception ex)
                            {
                                Console.Error.WriteLine($"FATAL screenshot capture: {ex}");
                                exitCode = 1;
                            }
                            finally
                            {
                                try { Application.Current?.Exit(); } catch { }
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"FATAL callback: {ex}");
                        exitCode = 1;
                        try { Application.Current?.Exit(); } catch { }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"FATAL outer: {ex}");
                exitCode = 1;
            }
            return exitCode;
        }

        Application.Start(p =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            _ = new App();
        });
        return 0;
    }

    public static async Task CaptureAndSaveWindowAsync(IntPtr hwnd, string outPath)
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
                throw new InvalidOperationException("Consumer window capture produced a blank, black, or uniform frame.");
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

    public static bool CheckIsBlankBlackOrUniform(byte[] pixels)
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
}
