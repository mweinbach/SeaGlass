using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SeaGlass.Tests.Infrastructure;
using Xunit;

namespace SeaGlass.Tests.Gallery;

/// <summary>
/// Authoritative smoke test suite verifying normal launch (no arguments) stays alive >= 10s without crashing,
/// and that navigating all 58 control pages in MainWindow renders cleanly with zero exceptions.
/// </summary>
[Collection("InteractiveDesktop")]
public sealed class GalleryLaunchAndSmokeTests
{
    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    private static IntPtr FindWindowForProcess(int pid, int timeoutMs = 15000)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            IntPtr found = IntPtr.Zero;
            EnumWindows((hWnd, lParam) =>
            {
                if (IsWindowVisible(hWnd))
                {
                    GetWindowThreadProcessId(hWnd, out uint p);
                    if (p == pid)
                    {
                        var sb = new StringBuilder(256);
                        GetClassName(hWnd, sb, 256);
                        string cls = sb.ToString();
                        if (cls.Contains("WinUIDesktopWin32WindowClass", StringComparison.OrdinalIgnoreCase) ||
                            cls.Contains("Window", StringComparison.OrdinalIgnoreCase))
                        {
                            found = hWnd;
                            return false;
                        }
                    }
                }
                return true;
            }, IntPtr.Zero);

            if (found != IntPtr.Zero) return found;
            Thread.Sleep(100);
        }
        return IntPtr.Zero;
    }

    [Fact]
    public async Task Gallery_NormalLaunch_StaysAliveAndAcquiresHwnd()
    {
        InteractiveDesktopHelper.EnsureInteractiveDesktopAvailable();

        string sampleExe = HostSelfTestRunner.LocateSampleExecutable();
        Assert.True(File.Exists(sampleExe), $"Sample executable not found: {sampleExe}");

        var psi = new ProcessStartInfo
        {
            FileName = sampleExe,
            Arguments = "", // NO arguments — normal launch!
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

            // 2. Assert process stays alive for >= 10 seconds without crashing
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed.TotalSeconds < 10)
            {
                if (process.HasExited)
                {
                    Assert.Fail($"Process crashed prematurely after {sw.Elapsed.TotalSeconds:F1}s with exit code {process.ExitCode}");
                }
                await Task.Delay(500);
            }

            Assert.False(process.HasExited, "Process must remain active and healthy >= 10s");
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

    [Fact]
    public async Task Gallery_SmokeTest_NavigatesAll58ControlPagesWithoutCrashing()
    {
        InteractiveDesktopHelper.EnsureInteractiveDesktopAvailable();

        string sampleExe = HostSelfTestRunner.LocateSampleExecutable();
        Assert.True(File.Exists(sampleExe), $"Sample executable not found: {sampleExe}");

        var psi = new ProcessStartInfo
        {
            FileName = sampleExe,
            Arguments = "--gallery-smoke",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        Assert.NotNull(process);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        try
        {
            var stdoutTask = process.StandardOutput.ReadToEndAsync(cts.Token);
            var stderrTask = process.StandardError.ReadToEndAsync(cts.Token);
            await process.WaitForExitAsync(cts.Token);

            string stdout = await stdoutTask;
            string stderr = await stderrTask;

            if (process.ExitCode != 0)
            {
                throw new Exception($"Gallery smoke test failed with exit code {process.ExitCode}.\nSTDOUT: {stdout}\nSTDERR: {stderr}");
            }

            Assert.Equal(0, process.ExitCode);
            Assert.Contains("--- SEAGLASS GALLERY SMOKE VERIFICATION PASSED: 58/58 pages loaded cleanly ---", stdout);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(true); } catch { }
            throw new TimeoutException("HARD TIMEOUT EXCEEDED (60s): Gallery smoke test hung or exceeded time limit.");
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
}
