using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using SeaGlass.WinUI;
using Xunit;

namespace SeaGlass.Tests.Platform;

/// <summary>
/// Automated unit tests verifying platform settings interop,
/// injectable simulation, live event notification, and system accessibility states.
/// </summary>
public sealed class PlatformSettingsTests
{
    [Fact]
    public void InjectablePlatformSettings_PropertyMutations_RaiseSettingsChangedEvent()
    {
        var settings = new InjectablePlatformSettings();
        var recordedEvents = new List<string>();

        settings.SettingsChanged += (sender, args) =>
        {
            Assert.Same(settings, sender);
            recordedEvents.Add(args.SettingName);
        };

        // Mutate each setting
        settings.IsAnimationsEnabled = false;
        settings.IsTransparencyEnabled = false;
        settings.IsHighContrast = true;
        settings.IsEnergySaverActive = true;
        settings.TextScaleFactor = 1.75f;
        settings.PreferredTheme = ElementTheme.Dark;

        Assert.False(settings.IsAnimationsEnabled);
        Assert.False(settings.IsTransparencyEnabled);
        Assert.True(settings.IsHighContrast);
        Assert.True(settings.IsEnergySaverActive);
        Assert.Equal(1.75f, settings.TextScaleFactor);
        Assert.Equal(ElementTheme.Dark, settings.PreferredTheme);

        Assert.Contains(nameof(ISeaGlassPlatformSettings.IsAnimationsEnabled), recordedEvents);
        Assert.Contains(nameof(ISeaGlassPlatformSettings.IsTransparencyEnabled), recordedEvents);
        Assert.Contains(nameof(ISeaGlassPlatformSettings.IsHighContrast), recordedEvents);
        Assert.Contains(nameof(ISeaGlassPlatformSettings.IsEnergySaverActive), recordedEvents);
        Assert.Contains(nameof(ISeaGlassPlatformSettings.TextScaleFactor), recordedEvents);
        Assert.Contains(nameof(ISeaGlassPlatformSettings.PreferredTheme), recordedEvents);
    }

    [Fact]
    public void WindowsPlatformSettings_Instance_InitializesSafelyWithoutCrashing()
    {
        var settings = WindowsPlatformSettings.Instance;
        Assert.NotNull(settings);

        // Safe query of all properties
        bool anim = settings.IsAnimationsEnabled;
        bool trans = settings.IsTransparencyEnabled;
        bool hc = settings.IsHighContrast;
        bool energy = settings.IsEnergySaverActive;
        float scale = settings.TextScaleFactor;
        ElementTheme theme = settings.PreferredTheme;

        Assert.True(scale >= 1.0f);

        // Event listener registration works cleanly
        bool eventFired = false;
        EventHandler<PlatformSettingsChangedEventArgs> handler = (s, e) => eventFired = true;
        settings.SettingsChanged += handler;
        settings.SettingsChanged -= handler;
        Assert.False(eventFired);
    }

    [Fact]
    public void SeaGlassTheme_PlatformSettingsPluggable_PropagatesToContext()
    {
        var theme = new SeaGlassTheme();
        var injectable = new InjectablePlatformSettings
        {
            IsAnimationsEnabled = false,
            IsHighContrast = true
        };

        theme.PlatformSettings = injectable;

        Assert.Same(injectable, theme.PlatformSettings);
        Assert.False(theme.PlatformSettings.IsAnimationsEnabled);
        Assert.True(theme.PlatformSettings.IsHighContrast);
    }
}
