using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using SeaGlass.Core;
using SeaGlass.WinUI;

namespace SeaGlass.ConsumerSample.Views;

/// <summary>
/// Dedicated page authored 100% in fluent C# using SeaGlass controls and method chaining.
/// Demonstrates drop-in SwiftUI-style ergonomics for code-only applications.
/// </summary>
public sealed class FluentPage : Page
{
    public FluentPage()
    {
        var stage = new SeaGlassStage
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        // Oceanic backdrop with gradient & test patterns
        var backdropGrid = new Grid();
        var gradient = new LinearGradientBrush
        {
            StartPoint = new Windows.Foundation.Point(0, 0),
            EndPoint = new Windows.Foundation.Point(1, 1),
            GradientStops =
            {
                new GradientStop { Color = Windows.UI.Color.FromArgb(255, 13, 30, 45), Offset = 0.0 },
                new GradientStop { Color = Windows.UI.Color.FromArgb(255, 20, 61, 78), Offset = 0.4 },
                new GradientStop { Color = Windows.UI.Color.FromArgb(255, 30, 92, 102), Offset = 0.75 },
                new GradientStop { Color = Windows.UI.Color.FromArgb(255, 44, 126, 131), Offset = 1.0 }
            }
        };
        backdropGrid.Background = gradient;

        var circle1 = new Ellipse
        {
            Width = 340,
            Height = 340,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(60, 40, 0, 0),
            Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(64, 85, 179, 166)),
            StrokeThickness = 4
        };
        var circle2 = new Ellipse
        {
            Width = 460,
            Height = 460,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 80, 60),
            Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(37, 20, 128, 118))
        };
        var circle3 = new Ellipse
        {
            Width = 220,
            Height = 220,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 80, 140, 0),
            Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(48, 244, 162, 97)),
            StrokeThickness = 3
        };
        backdropGrid.Children.Add(circle1);
        backdropGrid.Children.Add(circle2);
        backdropGrid.Children.Add(circle3);
        stage.BackdropContent = backdropGrid;

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        var root = new StackPanel
        {
            Spacing = 20,
            Padding = new Thickness(28),
            Width = 780,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        // Header TopAppBar
        var topBar = new SeaGlassTopAppBar
        {
            Title = "SeaGlass Fluent C# Consumer",
            Subtitle = "Authored 100% via C# Object Hierarchies & Method Chaining",
            Tone = SeaGlassTone.CobaltTide,
            Variant = SeaGlassVariant.Primary
        };
        AutomationProperties.SetAutomationId(topBar, "Fluent_SeaGlassTopAppBar");
        root.Children.Add(topBar);

        // Volumetric Card
        var card = new SeaGlassCard
        {
            Title = "Fluent Optical Components",
            Subtitle = "All properties configured programmatically with zero XAML dependencies",
            Tone = SeaGlassTone.SeafoamMint,
            Variant = SeaGlassVariant.Elevated
        };
        AutomationProperties.SetAutomationId(card, "Fluent_SeaGlassCard");

        var cardStack = new StackPanel { Spacing = 16, Padding = new Thickness(4) };

        // Buttons & Chips row
        var buttonRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        var btn = new SeaGlassButton
        {
            Label = "Tactile Fluent Action",
            Tone = SeaGlassTone.SeafoamMint,
            Variant = SeaGlassVariant.Primary
        };
        AutomationProperties.SetAutomationId(btn, "Fluent_SeaGlassButton");
        buttonRow.Children.Add(btn);

        var pill = new SeaGlassPill("Active Filter", true)
        {
            Tone = SeaGlassTone.SeaLavender,
            Variant = SeaGlassVariant.Tonal
        };
        AutomationProperties.SetAutomationId(pill, "Fluent_SeaGlassPill");
        buttonRow.Children.Add(pill);

        var badge = new SeaGlassBadge
        {
            Count = 42,
            Tone = SeaGlassTone.RoseQuartzShell
        };
        AutomationProperties.SetAutomationId(badge, "Fluent_SeaGlassBadge");
        buttonRow.Children.Add(badge);
        cardStack.Children.Add(buttonRow);

        // Switch and Label
        var switchRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16 };
        var sw = new SeaGlassSwitch { IsOn = true, Tone = SeaGlassTone.BeachAmber };
        AutomationProperties.SetAutomationId(sw, "Fluent_SeaGlassSwitch");
        switchRow.Children.Add(sw);

        switchRow.Children.Add(new TextBlock
        {
            Text = "Ambient Caustic Volumetric Scattering",
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 13
        });
        cardStack.Children.Add(switchRow);

        // Liquid Slider
        var slider = new SeaGlassLiquidSlider
        {
            Title = "Optical Refraction Index (IOR)",
            Minimum = 1.0,
            Maximum = 2.0,
            Value = 1.52,
            Tone = SeaGlassTone.CobaltTide
        };
        AutomationProperties.SetAutomationId(slider, "Fluent_SeaGlassLiquidSlider");
        cardStack.Children.Add(slider);

        // Linear Progress Indicator
        var prog = new SeaGlassLinearProgressIndicator
        {
            Progress = 0.72,
            Tone = SeaGlassTone.KelpForest
        };
        AutomationProperties.SetAutomationId(prog, "Fluent_SeaGlassLinearProgressIndicator");
        cardStack.Children.Add(prog);

        // Text Field
        var tf = new SeaGlassTextField
        {
            Placeholder = "Enter volumetric notes...",
            Tone = SeaGlassTone.ObsidianTide
        };
        AutomationProperties.SetAutomationId(tf, "Fluent_SeaGlassTextField");
        cardStack.Children.Add(tf);

        card.Content = cardStack;
        root.Children.Add(card);

        scroll.Content = root;
        stage.Content = scroll;
        Content = stage;
    }
}
