using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace SeaGlass.Tests.Infrastructure;

public static class InteractiveDesktopHelper
{
    public static string LocateRepoRoot()
    {
        string current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            if (File.Exists(Path.Combine(current, "windows", "SeaGlass.sln")) ||
                File.Exists(Path.Combine(current, "SeaGlass.sln")) ||
                Directory.Exists(Path.Combine(current, ".git")))
            {
                return current;
            }
            string? parent = Directory.GetParent(current)?.FullName;
            if (parent == null || parent == current) break;
            current = parent;
        }
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    }
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr OpenInputDesktop(uint dwFlags, bool fInherit, uint dwDesiredAccess);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseDesktop(IntPtr hDesktop);

    private const uint DESKTOP_SWITCHDESKTOP = 0x0100;

    /// <summary>
    /// Checks if the interactive Windows desktop is available and unlocked.
    /// Returns false if the session is locked (LogonUI running), display is off/sleeping,
    /// or executing in a non-interactive service/CI window station.
    /// </summary>
    public static bool IsInteractiveDesktopAvailable()
    {
        try
        {
            // 1. Check if LogonUI (lock screen) process is active
            if (Process.GetProcessesByName("LogonUI").Length > 0)
            {
                return false;
            }

            // 2. Query input desktop handle with DESKTOP_SWITCHDESKTOP access
            IntPtr hDesk = OpenInputDesktop(0, false, DESKTOP_SWITCHDESKTOP);
            if (hDesk == IntPtr.Zero)
            {
                return false;
            }
            CloseDesktop(hDesk);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Enforces the interactive desktop precondition. Fails fast with:
    /// "INTERACTIVE DESKTOP UNAVAILABLE — PENDING"
    /// when the screen is locked or unavailable.
    /// </summary>
    public static void EnsureInteractiveDesktopAvailable()
    {
        if (!IsInteractiveDesktopAvailable())
        {
            throw new InvalidOperationException("INTERACTIVE DESKTOP UNAVAILABLE — PENDING: Windows session is locked, display is off, or LogonUI is active.");
        }
    }
}
