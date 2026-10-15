using System;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SeaGlass.WinUI;
using Windows.UI;

namespace SeaGlass.Sample.Pages;

/// <summary>
/// Gallery home page introducing Sanded Sea Glass optics, M3 Expressive physics, and catalog navigation.
/// </summary>
public sealed class HomePage : Page
{
    public HomePage()
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
            MaxWidth = 1200,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        // Hero Banner
        var heroCard = new SeaGlassCard
        {
            Title = "SeaGlass Optical Lab & UI Foundation",
            Subtitle = "High-Performance GPU Graphics for Windows (WinUI 3) & Android",
            Tone = SeaGlassTone.CobaltTide,
            Variant = SeaGlassVariant.Elevated,
            Padding = new Thickness(24)
        };

        var heroContent = new StackPanel { Spacing = 12 };
        heroContent.Children.Add(new TextBlock
        {
            Text = "SeaGlass fuses ocean-tumbled sanded glass optics, Material 3 Expressive pastels & dual-spring physics, and 2D SDF liquid coalescing into a unified cross-platform UI ecosystem.",
            FontSize = 14,
            Opacity = 0.9,
            TextWrapping = TextWrapping.Wrap
        });

        heroCard.Content = heroContent;
        root.Children.Add(heroCard);

        // Pillars Grid
        var pillarsGrid = new Grid { ColumnSpacing = 16, RowSpacing = 16 };
        pillarsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pillarsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pillarsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var pillar1 = new SeaGlassCard
        {
            Title = "1. Sanded Sea Glass Optics",
            Subtitle = "Volumetric Physics",
            Tone = SeaGlassTone.SeafoamMint,
            Content = new TextBlock
            {
                Text = "Isotropic micro-facet coarseness, volumetric Beer-Lambert absorption in perceptual Oklab space, softened edge refraction, and chalky rough-Fresnel rim illumination — never flat alpha or glossy plastic.",
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap
            }
        };
        Grid.SetColumn(pillar1, 0);

        var pillar2 = new SeaGlassCard
        {
            Title = "2. M3 Expressive & Motion",
            Subtitle = "Dual-Spring Discipline",
            Tone = SeaGlassTone.SeaLavender,
            Content = new TextBlock
            {
                Text = "8 signature pastel palettes, squircle geometry, asymmetric corner morphing, and strict dual springs: spatial springs with organic bounce vs. critically damped effects springs.",
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap
            }
        };
        Grid.SetColumn(pillar2, 1);

        var pillar3 = new SeaGlassCard
        {
            Title = "3. Liquid SDF Coalescing",
            Subtitle = "Continuous Surfaces",
            Tone = SeaGlassTone.BeachAmber,
            Content = new TextBlock
            {
                Text = "Shared 2D Signed Distance Fields with polynomial smooth-minimum (smin) unions, flowing surfaces into and out of each other like tidal droplets during expansion and drag gestures.",
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap
            }
        };
        Grid.SetColumn(pillar3, 2);

        pillarsGrid.Children.Add(pillar1);
        pillarsGrid.Children.Add(pillar2);
        pillarsGrid.Children.Add(pillar3);
        root.Children.Add(pillarsGrid);

        // Catalog Summary Card
        var catalogSummary = new SeaGlassCard
        {
            Title = "58-Control Component Inventory",
            Subtitle = "Full parity across Android Compose and Windows WinUI 3",
            Tone = SeaGlassTone.MilkGlassFrost,
            Content = new TextBlock
            {
                Text = "Explore the complete catalog in the left navigation pane: Surfaces, Buttons & Chips, Navigation & Bars, Inputs & Toggles, Feedback & Progress, Dynamic Morphing Suite, Overlays & Sheets, and Windows Native Extras.",
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap
            }
        };
        root.Children.Add(catalogSummary);

        scroll.Content = root;
        Content = scroll;
    }
}
