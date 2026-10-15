using System;
using Microsoft.UI.Xaml;

namespace SeaGlass.WinUI;

/// <summary>
/// Live Windows 11 platform settings provider that hooks genuine WinRT system APIs:
/// <c>Windows.UI.ViewManagement.UISettings</c>,
/// <c>Windows.UI.ViewManagement.AccessibilitySettings</c>, and
/// <c>Windows.System.Power.PowerManager</c>.
/// </summary>
public sealed class WindowsPlatformSettings : ISeaGlassPlatformSettings
{
    private static readonly Lazy<WindowsPlatformSettings> _instance =
        new(() => new WindowsPlatformSettings());

    public static WindowsPlatformSettings Instance => _instance.Value;

    private readonly Windows.UI.ViewManagement.UISettings? _uiSettings;
    private readonly Windows.UI.ViewManagement.AccessibilitySettings? _accessibilitySettings;

    public WindowsPlatformSettings()
    {
        try
        {
            _uiSettings = new Windows.UI.ViewManagement.UISettings();
            _uiSettings.ColorValuesChanged += (s, e) => RaiseChanged(nameof(PreferredTheme));
            _uiSettings.AdvancedEffectsEnabledChanged += (s, e) => RaiseChanged(nameof(IsTransparencyEnabled));
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041))
            {
                _uiSettings.AnimationsEnabledChanged += (s, e) => RaiseChanged(nameof(IsAnimationsEnabled));
            }
            _uiSettings.TextScaleFactorChanged += (s, e) => RaiseChanged(nameof(TextScaleFactor));
        }
        catch
        {
            // In headless/test or unbootstrapped environment, UISettings constructor may fail gracefully
        }

        try
        {
            _accessibilitySettings = new Windows.UI.ViewManagement.AccessibilitySettings();
            _accessibilitySettings.HighContrastChanged += (s, e) => RaiseChanged(nameof(IsHighContrast));
        }
        catch
        {
        }

        try
        {
            Windows.System.Power.PowerManager.EnergySaverStatusChanged += (s, e) => RaiseChanged(nameof(IsEnergySaverActive));
        }
        catch
        {
        }
    }

    public bool IsAnimationsEnabled
    {
        get
        {
            try { return _uiSettings?.AnimationsEnabled ?? true; }
            catch { return true; }
        }
    }

    public bool IsTransparencyEnabled
    {
        get
        {
            try { return _uiSettings?.AdvancedEffectsEnabled ?? true; }
            catch { return true; }
        }
    }

    public bool IsHighContrast
    {
        get
        {
            try { return _accessibilitySettings?.HighContrast ?? false; }
            catch { return false; }
        }
    }

    public bool IsEnergySaverActive
    {
        get
        {
            try
            {
                return Windows.System.Power.PowerManager.EnergySaverStatus == Windows.System.Power.EnergySaverStatus.On;
            }
            catch
            {
                return false;
            }
        }
    }

    public float TextScaleFactor
    {
        get
        {
            try { return (float)(_uiSettings?.TextScaleFactor ?? 1.0); }
            catch { return 1.0f; }
        }
    }

    public ElementTheme PreferredTheme => ElementTheme.Default;

    public event EventHandler<PlatformSettingsChangedEventArgs>? SettingsChanged;

    private void RaiseChanged(string settingName)
    {
        SettingsChanged?.Invoke(this, new PlatformSettingsChangedEventArgs { SettingName = settingName });
    }
}
