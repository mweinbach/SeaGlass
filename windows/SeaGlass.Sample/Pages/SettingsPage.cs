using Microsoft.UI.Xaml.Automation;
using System;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SeaGlass.Core.Backend;
using SeaGlass.WinUI;
using Windows.UI;

namespace SeaGlass.Sample.Pages;

/// <summary>
/// Settings & Diagnostics page displaying system telemetry, theme toggles, and GPU optical tier.
/// </summary>
public sealed class SettingsPage : Page
{
    public SettingsPage()
    {
        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        var root = new StackPanel
        {
            Spacing = 24,
            Padding = new Thickness(36),
            MaxWidth = 1000,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        root.Children.Add(new TextBlock
        {
            Text = "Settings & Optical Telemetry",
            FontSize = 28,
            FontWeight = FontWeights.Bold
        });

        // Theme Configuration Card
        var themeCard = new SeaGlassCard
        {
            Title = "Application Theme & Appearance",
            Subtitle = "Switch theme mode to observe chromatic adaptation",
            Tone = SeaGlassTone.SeafoamMint,
            Padding = new Thickness(20)
        };

        var themeStack = new StackPanel { Spacing = 14 };
        var themeRadioLight = new RadioButton { Content = "Light Theme", IsChecked = true, GroupName = "ThemeGroup" };
        var themeRadioDark = new RadioButton { Content = "Dark Theme", GroupName = "ThemeGroup" };
        var themeRadioHighContrast = new RadioButton { Content = "High Contrast Mode (Simulated)", GroupName = "ThemeGroup" };

        themeRadioLight.Checked += (_, _) =>
        {
            if (XamlRoot?.Content is FrameworkElement fe) fe.RequestedTheme = ElementTheme.Light;
        };
        themeRadioDark.Checked += (_, _) =>
        {
            if (XamlRoot?.Content is FrameworkElement fe) fe.RequestedTheme = ElementTheme.Dark;
        };
        themeRadioHighContrast.Checked += (_, _) =>
        {
            if (XamlRoot?.Content is FrameworkElement fe) fe.RequestedTheme = ElementTheme.Dark;
        };

        themeStack.Children.Add(themeRadioLight);
        themeStack.Children.Add(themeRadioDark);
        themeStack.Children.Add(themeRadioHighContrast);
        themeCard.Content = themeStack;
        AutomationProperties.SetAutomationId(themeRadioLight, "DebugRadio_LightTheme");
        AutomationProperties.SetName(themeRadioLight, "Light Theme");

        AutomationProperties.SetAutomationId(themeRadioDark, "DebugRadio_DarkTheme");
        AutomationProperties.SetName(themeRadioDark, "Dark Theme");

        AutomationProperties.SetAutomationId(themeRadioHighContrast, "DebugRadio_HighContrast");
        AutomationProperties.SetName(themeRadioHighContrast, "High Contrast Mode");

        var motionCard = new SeaGlassCard
        {
            Title = "Motion Dynamics & Settings",
            Subtitle = "Injectable platform motion and accessibility modes",
            Tone = SeaGlassTone.BeachAmber,
            Padding = new Thickness(20)
        };
        var motionStack = new StackPanel { Spacing = 14 };
        var motionSwitch = new SeaGlassSwitch
        {
            Content = "Reduced Motion Mode",
            IsOn = false
        };
        AutomationProperties.SetAutomationId(motionSwitch, "DebugSwitch_ReducedMotion");
        AutomationProperties.SetName(motionSwitch, "Reduced Motion Debug Switch");
        motionStack.Children.Add(motionSwitch);
        motionCard.Content = motionStack;
        root.Children.Add(motionCard);
        root.Children.Add(themeCard);

        // Optical Pipeline Telemetry Card
        var telemetryCard = new SeaGlassCard
        {
            Title = "GPU Graphics & Shader Telemetry",
            Subtitle = "Active DirectComposition & Win2D optical pipeline status",
            Tone = SeaGlassTone.CobaltTide,
            Padding = new Thickness(20)
        };

        var telemetryStack = new StackPanel { Spacing = 10 };
        telemetryStack.Children.Add(new TextBlock
        {
            Text = $"Active Optical Tier: D2D1_HLSL_FULL (Direct2D 1.1 + HLSL SM 5.0)",
            FontWeight = FontWeights.SemiBold
        });
        telemetryStack.Children.Add(new TextBlock
        {
            Text = "Backdrop Sampling: Acyclic CompositionVisualSurface zero-copy graph",
            Opacity = 0.85
        });
        telemetryStack.Children.Add(new TextBlock
        {
            Text = "Uniform Block ABI: 256-byte std140 layout parity with Android Vulkan/AGSL",
            Opacity = 0.85
        });
        telemetryStack.Children.Add(new TextBlock
        {
            Text = "Spring Integrator: Semi-implicit Euler sub-stepped dual-spring physics",
            Opacity = 0.85
        });

        telemetryCard.Content = telemetryStack;
        root.Children.Add(telemetryCard);

        scroll.Content = root;
        Content = scroll;
    }
}
