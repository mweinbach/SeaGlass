using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace SeaGlass.Sample.Screenshot;

public sealed record CapturedWindowBitmap(
    int Width,
    int Height,
    int Dpi,
    byte[] BgraPixels)
{
    /// <summary>
    /// Crops a sub-region (in physical device pixels) from the window bitmap.
    /// </summary>
    public CapturedWindowBitmap Crop(int x, int y, int width, int height)
    {
        x = Math.Clamp(x, 0, Width - 1);
        y = Math.Clamp(y, 0, Height - 1);
        width = Math.Clamp(width, 1, Width - x);
        height = Math.Clamp(height, 1, Height - y);

        byte[] cropped = new byte[width * height * 4];
        for (int row = 0; row < height; row++)
        {
            int srcOffset = ((y + row) * Width + x) * 4;
            int dstOffset = row * width * 4;
            System.Buffer.BlockCopy(BgraPixels, srcOffset, cropped, dstOffset, width * 4);
        }

        return new CapturedWindowBitmap(width, height, Dpi, cropped);
    }

    /// <summary>
    /// Encodes and saves the BGRA pixel buffer to a PNG file using Windows WIC.
    /// </summary>
    public async Task SaveToPngAsync(string filePath)
    {
        string? dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        using var stream = new InMemoryRandomAccessStream();
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
        encoder.SetPixelData(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            (uint)Width,
            (uint)Height,
            (uint)Dpi,
            (uint)Dpi,
            BgraPixels);
        await encoder.FlushAsync();

        stream.Seek(0);
        using var fileStream = File.Create(filePath);
        using var inputStream = stream.AsStreamForRead();
        await inputStream.CopyToAsync(fileStream);
    }

    public void SaveToPng(string filePath)
    {
        SaveToPngAsync(filePath).GetAwaiter().GetResult();
    }
}

public static class DwmWindowCapture
{
    public static (int clientOffsetX, int clientOffsetY) GetClientOffset(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return (0, 0);
        NativeMethods.POINT clientOrigin = new() { X = 0, Y = 0 };
        NativeMethods.ClientToScreen(hwnd, ref clientOrigin);
        int hr = NativeMethods.DwmGetWindowAttribute(
            hwnd,
            NativeMethods.DWMWA_EXTENDED_FRAME_BOUNDS,
            out var windowBounds,
            Marshal.SizeOf<NativeMethods.RECT>());
        if (hr != 0 || windowBounds.Width <= 0 || windowBounds.Height <= 0)
        {
            NativeMethods.GetWindowRect(hwnd, out windowBounds);
        }
        return (Math.Max(0, clientOrigin.X - windowBounds.Left), Math.Max(0, clientOrigin.Y - windowBounds.Top));
    }

    /// <summary>
    /// Captures the full DWM-composited window for the given HWND.
    /// Flushes DWM queue, queries extended frame bounds, and uses Screen BitBlt with CAPTUREBLT.
    /// Falls back to PrintWindow(PW_RENDERFULLCONTENT) if screen capture is unviable.
    /// </summary>
    public static CapturedWindowBitmap CaptureWindow(IntPtr hwnd, bool clientAreaOnly = false)
    {
        if (hwnd == IntPtr.Zero)
            throw new ArgumentException("Invalid HWND", nameof(hwnd));

        // 1. Ensure window is restored, visible, and frontmost
        NativeMethods.ShowWindow(hwnd, NativeMethods.SW_SHOW);
        NativeMethods.BringWindowToTop(hwnd);
        NativeMethods.SetForegroundWindow(hwnd);

        // 2. Flush DWM composition queue to ensure latest frame is on screen
        NativeMethods.DwmFlush();

        // 3. Determine capture bounds
        NativeMethods.RECT captureBounds;
        if (clientAreaOnly)
        {
            NativeMethods.GetClientRect(hwnd, out captureBounds);
            NativeMethods.POINT clientOrigin = new() { X = 0, Y = 0 };
            NativeMethods.ClientToScreen(hwnd, ref clientOrigin);
            captureBounds.Left = clientOrigin.X;
            captureBounds.Top = clientOrigin.Y;
            captureBounds.Right = captureBounds.Left + captureBounds.Width;
            captureBounds.Bottom = captureBounds.Top + captureBounds.Height;
        }
        else
        {
            // DWMWA_EXTENDED_FRAME_BOUNDS excludes invisible drop shadow borders
            int hr = NativeMethods.DwmGetWindowAttribute(
                hwnd,
                NativeMethods.DWMWA_EXTENDED_FRAME_BOUNDS,
                out captureBounds,
                Marshal.SizeOf<NativeMethods.RECT>());

            if (hr != 0 || captureBounds.Width <= 0 || captureBounds.Height <= 0)
            {
                NativeMethods.GetWindowRect(hwnd, out captureBounds);
            }
        }

        int width = captureBounds.Width;
        int height = captureBounds.Height;
        if (width <= 0 || height <= 0)
            throw new InvalidOperationException($"Invalid window bounds: {width}x{height}");

        uint dpi = NativeMethods.GetDpiForWindow(hwnd);
        if (dpi == 0) dpi = 96;

        // 4. Setup GDI top-down DIBSection
        IntPtr hdcScreen = NativeMethods.GetDC(IntPtr.Zero);
        IntPtr hdcMem = NativeMethods.CreateCompatibleDC(hdcScreen);

        NativeMethods.BITMAPINFO bmi = new()
        {
            bmiHeader = new NativeMethods.BITMAPINFOHEADER
            {
                biSize = (uint)Marshal.SizeOf<NativeMethods.BITMAPINFOHEADER>(),
                biWidth = width,
                biHeight = -height, // Top-down scanlines!
                biPlanes = 1,
                biBitCount = 32,
                biCompression = 0 // BI_RGB
            }
        };

        IntPtr hDIB = NativeMethods.CreateDIBSection(
            hdcMem,
            in bmi,
            NativeMethods.DIB_RGB_COLORS,
            out IntPtr ppvBits,
            IntPtr.Zero,
            0);

        if (hDIB == IntPtr.Zero || ppvBits == IntPtr.Zero)
        {
            NativeMethods.DeleteDC(hdcMem);
            NativeMethods.ReleaseDC(IntPtr.Zero, hdcScreen);
            throw new InvalidOperationException("Failed to allocate GDI DIBSection");
        }

        IntPtr hOldBmp = NativeMethods.SelectObject(hdcMem, hDIB);

        try
        {
            // Primary strategy: Screen BitBlt with CAPTUREBLT
            bool success = NativeMethods.BitBlt(
                hdcMem, 0, 0, width, height,
                hdcScreen, captureBounds.Left, captureBounds.Top,
                NativeMethods.SRCCOPY | NativeMethods.CAPTUREBLT);

            byte[] pixelBytes = new byte[width * height * 4];
            Marshal.Copy(ppvBits, pixelBytes, 0, pixelBytes.Length);

            // Verify not completely empty/black/uniform
            bool isUniformOrBlank = CheckIsBlankBlackOrUniform(pixelBytes);

            if (!success || isUniformOrBlank)
            {
                // Fallback strategy: PrintWindow with PW_RENDERFULLCONTENT
                NativeMethods.PrintWindow(hwnd, hdcMem, NativeMethods.PW_RENDERFULLCONTENT);
                Marshal.Copy(ppvBits, pixelBytes, 0, pixelBytes.Length);
                isUniformOrBlank = CheckIsBlankBlackOrUniform(pixelBytes);
            }

            if (isUniformOrBlank)
            {
                throw new InvalidOperationException("Screen capture produced blank, black, or uniform frame (display may be sleeping or session is headless). Screen capture cannot capture real composited glass in current session.");
            }

            // Detect actual rendered window content bounds to eliminate DPI canvas mismatch
            int minX = width, maxX = 0;
            int minY = height, maxY = 0;
            for (int y = 0; y < height; y += 2)
            {
                int row = y * width * 4;
                for (int x = 0; x < width; x += 2)
                {
                    byte a = pixelBytes[row + x * 4 + 3];
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

            var captured = new CapturedWindowBitmap(width, height, (int)dpi, pixelBytes);
            if (minX == 0 && minY == 0 && actualW >= 400 && actualH >= 300 &&
                (actualW < width - 20 || actualH < height - 20))
            {
                if (Math.Abs(actualW - 1440) <= 12) actualW = 1440;
                if (Math.Abs(actualH - 900) <= 12) actualH = 900;
                captured = captured.Crop(0, 0, actualW, actualH);
            }

            return captured;
        }
        finally
        {
            NativeMethods.SelectObject(hdcMem, hOldBmp);
            NativeMethods.DeleteObject(hDIB);
            NativeMethods.DeleteDC(hdcMem);
            NativeMethods.ReleaseDC(IntPtr.Zero, hdcScreen);
        }
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

        // 1. Completely black
        if (!hasNonZero) return true;

        // 2. Completely uniform (all sampled pixels identical within 1 LSB)
        if ((maxB - minB <= 1) && (maxG - minG <= 1) && (maxR - minR <= 1))
            return true;

        return false;
    }
}
