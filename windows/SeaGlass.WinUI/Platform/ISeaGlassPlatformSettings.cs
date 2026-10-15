using System;
using Microsoft.UI.Xaml;

namespace SeaGlass.WinUI;

/// <summary>
/// Platform and operating system settings adapter for SeaGlass on Windows.
///
/// Encapsulates Windows 11 system-wide accessibility and display preferences
/// (reduced motion, transparency effects, high contrast, battery/energy saver, and text scaling)
/// so that both live Windows runtime environments and automated test harnesses
/// can observe and control adaptive optical fallbacks.
/// </summary>
public interface ISeaGlassPlatformSettings
{
    /// <summary>
    /// Whether system animations are enabled (Windows Settings > Accessibility > Visual effects > Animation effects).
    /// When false, SeaGlass transitions snap instantaneously with zero spatial overshoot.
    /// </summary>
    bool IsAnimationsEnabled { get; }

    /// <summary>
    /// Whether system transparency effects are enabled (Windows Settings > Accessibility > Visual effects > Transparency effects).
    /// When false, SeaGlass falls back to the LightweightTint tier.
    /// </summary>
    bool IsTransparencyEnabled { get; }

    /// <summary>
    /// Whether Windows High Contrast theme is currently active.
    /// When true, glass diffusion and refraction are disabled, and high-contrast system colors with solid borders are used.
    /// </summary>
    bool IsHighContrast { get; }

    /// <summary>
    /// Whether Windows Energy Saver / Battery Saver is currently active.
    /// When true, SeaGlass rendering downgrades to LightweightTint and pauses continuous background physics.
    /// </summary>
    bool IsEnergySaverActive { get; }

    /// <summary>
    /// Windows text scaling factor (1.0 = 100%, up to 2.25 = 225%).
    /// </summary>
    float TextScaleFactor { get; }

    /// <summary>
    /// Preferred element theme (Default, Light, or Dark).
    /// </summary>
    ElementTheme PreferredTheme { get; }

    /// <summary>
    /// Fired whenever any platform setting changes at runtime.
    /// </summary>
    event EventHandler<PlatformSettingsChangedEventArgs>? SettingsChanged;
}

/// <summary>
/// Event arguments delivered when an OS setting is modified.
/// </summary>
public sealed class PlatformSettingsChangedEventArgs : EventArgs
{
    public string SettingName { get; init; } = string.Empty;
}
