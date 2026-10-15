using System;
using Microsoft.UI.Xaml;

namespace SeaGlass.WinUI;

/// <summary>
/// Injectable and mutable implementation of <see cref="ISeaGlassPlatformSettings"/>
/// for test fixtures and interactive simulation without touching Windows system registry or OS settings.
/// </summary>
public sealed class InjectablePlatformSettings : ISeaGlassPlatformSettings
{
    private bool _isAnimationsEnabled = true;
    private bool _isTransparencyEnabled = true;
    private bool _isHighContrast = false;
    private bool _isEnergySaverActive = false;
    private float _textScaleFactor = 1.0f;
    private ElementTheme _preferredTheme = ElementTheme.Default;

    public bool IsAnimationsEnabled
    {
        get => _isAnimationsEnabled;
        set
        {
            if (_isAnimationsEnabled != value)
            {
                _isAnimationsEnabled = value;
                NotifyChanged(nameof(IsAnimationsEnabled));
            }
        }
    }

    public bool IsTransparencyEnabled
    {
        get => _isTransparencyEnabled;
        set
        {
            if (_isTransparencyEnabled != value)
            {
                _isTransparencyEnabled = value;
                NotifyChanged(nameof(IsTransparencyEnabled));
            }
        }
    }

    public bool IsHighContrast
    {
        get => _isHighContrast;
        set
        {
            if (_isHighContrast != value)
            {
                _isHighContrast = value;
                NotifyChanged(nameof(IsHighContrast));
            }
        }
    }

    public bool IsEnergySaverActive
    {
        get => _isEnergySaverActive;
        set
        {
            if (_isEnergySaverActive != value)
            {
                _isEnergySaverActive = value;
                NotifyChanged(nameof(IsEnergySaverActive));
            }
        }
    }

    public float TextScaleFactor
    {
        get => _textScaleFactor;
        set
        {
            if (Math.Abs(_textScaleFactor - value) > 0.001f)
            {
                _textScaleFactor = value;
                NotifyChanged(nameof(TextScaleFactor));
            }
        }
    }

    public ElementTheme PreferredTheme
    {
        get => _preferredTheme;
        set
        {
            if (_preferredTheme != value)
            {
                _preferredTheme = value;
                NotifyChanged(nameof(PreferredTheme));
            }
        }
    }

    public event EventHandler<PlatformSettingsChangedEventArgs>? SettingsChanged;

    public void NotifyChanged(string settingName)
    {
        SettingsChanged?.Invoke(this, new PlatformSettingsChangedEventArgs { SettingName = settingName });
    }
}
