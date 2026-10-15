using System;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace SeaGlass.Sample.Components;

/// <summary>
/// Stage host that provides an authentic high-contrast ocean refraction backdrop
/// for evaluating Beer-Lambert absorption, surface coarseness diffusion, and rim highlights.
/// </summary>
public sealed class ShowcaseStageHost : Grid
{
    private readonly Border _previewContainer;
    private readonly TextBlock _watermarkText;
    private readonly TextBlock _subtitleText;
    private readonly Ellipse _circle1;
    private readonly Ellipse _circle2;

    public ShowcaseStageHost()
    {
        MinHeight = 320;
        CornerRadius = new CornerRadius(16);
        BorderThickness = new Thickness(1);
        BorderBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));
        Clip = new RectangleGeometry { Rect = new Windows.Foundation.Rect(0, 0, 2000, 2000) };

        // 1. Oceanic Background with Multi-stop Gradient
        var gradient = new LinearGradientBrush
        {
            StartPoint = new Windows.Foundation.Point(0, 0),
            EndPoint = new Windows.Foundation.Point(1, 1),
            GradientStops =
            {
                new GradientStop { Color = Color.FromArgb(255, 10, 25, 42), Offset = 0.0 },   // Deep Abyss
                new GradientStop { Color = Color.FromArgb(255, 14, 48, 68), Offset = 0.35 },  // Ocean Depth
                new GradientStop { Color = Color.FromArgb(255, 20, 78, 92), Offset = 0.70 },  // Teal Current
                new GradientStop { Color = Color.FromArgb(255, 34, 112, 118), Offset = 1.0 }  // Shallow Reef
            }
        };
        Background = gradient;

        // 2. High-Contrast Backdrop Optical Test Patterns (Grid Lines + Watermarks + Circles)
        var patternGrid = new Grid
        {
            IsHitTestVisible = false,
            Opacity = 0.85
        };

        // Geometric watermark circles
        _circle1 = new Ellipse
        {
            Width = 260,
            Height = 260,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(40, -40, 0, 0),
            Stroke = new SolidColorBrush(Color.FromArgb(60, 42, 161, 152)),
            StrokeThickness = 3
        };
        _circle2 = new Ellipse
        {
            Width = 380,
            Height = 380,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, -80, -80),
            Fill = new SolidColorBrush(Color.FromArgb(30, 38, 139, 210)),
            Stroke = new SolidColorBrush(Color.FromArgb(80, 108, 194, 204)),
            StrokeThickness = 2
        };

        // Bold Typography Watermark for testing edge diffusion & modulation
        _watermarkText = new TextBlock
        {
            Text = "SEAGLASS OPTICAL LAB",
            FontSize = 38,
            FontWeight = FontWeights.Black,
            Foreground = new SolidColorBrush(Color.FromArgb(45, 255, 255, 255)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 120),
            IsHitTestVisible = false
        };

        _subtitleText = new TextBlock
        {
            Text = "BEER-LAMBERT ABSORPTION • ROUGH-FRESNEL RIM • ISOTROPIC SAND COARSENESS",
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            CharacterSpacing = 120,
            Foreground = new SolidColorBrush(Color.FromArgb(50, 180, 230, 240)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 50),
            IsHitTestVisible = false
        };

        // Subtle diagonal pattern lines
        patternGrid.Children.Add(_circle1);
        patternGrid.Children.Add(_circle2);
        patternGrid.Children.Add(_watermarkText);
        patternGrid.Children.Add(_subtitleText);

        Children.Add(patternGrid);

        // 3. Centered Preview Container
        _previewContainer = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Padding = new Thickness(24)
        };
        Children.Add(_previewContainer);
    }

    public Border PreviewContainer => _previewContainer;

    public void SetPreview(FrameworkElement previewElement)
    {
        _previewContainer.Child = previewElement;
    }

    public void SetPreviewVisibility(bool visible)
    {
        _previewContainer.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
    }

    public void ApplyTheme(ElementTheme theme, bool isHighContrast = false)
    {
        if (isHighContrast)
        {
            Background = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
            BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
            BorderThickness = new Thickness(2);
            _watermarkText.Foreground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255));
            _subtitleText.Foreground = new SolidColorBrush(Color.FromArgb(200, 0, 255, 255));
            _circle1.Stroke = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255));
            _circle2.Stroke = new SolidColorBrush(Color.FromArgb(180, 0, 255, 255));
            _circle2.Fill = new SolidColorBrush(Color.FromArgb(80, 0, 255, 255));
            return;
        }

        if (theme == ElementTheme.Light)
        {
            var lightGradient = new LinearGradientBrush
            {
                StartPoint = new Windows.Foundation.Point(0, 0),
                EndPoint = new Windows.Foundation.Point(1, 1),
                GradientStops =
                {
                    new GradientStop { Color = Color.FromArgb(255, 238, 248, 250), Offset = 0.0 },
                    new GradientStop { Color = Color.FromArgb(255, 218, 240, 244), Offset = 0.35 },
                    new GradientStop { Color = Color.FromArgb(255, 195, 228, 235), Offset = 0.70 },
                    new GradientStop { Color = Color.FromArgb(255, 175, 216, 226), Offset = 1.0 }
                }
            };
            Background = lightGradient;
            BorderBrush = new SolidColorBrush(Color.FromArgb(70, 0, 80, 95));
            BorderThickness = new Thickness(1);
            _watermarkText.Foreground = new SolidColorBrush(Color.FromArgb(70, 15, 50, 60));
            _subtitleText.Foreground = new SolidColorBrush(Color.FromArgb(90, 10, 70, 85));
            _circle1.Stroke = new SolidColorBrush(Color.FromArgb(90, 0, 120, 130));
            _circle2.Stroke = new SolidColorBrush(Color.FromArgb(100, 20, 140, 160));
            _circle2.Fill = new SolidColorBrush(Color.FromArgb(40, 0, 160, 180));
        }
        else
        {
            var darkGradient = new LinearGradientBrush
            {
                StartPoint = new Windows.Foundation.Point(0, 0),
                EndPoint = new Windows.Foundation.Point(1, 1),
                GradientStops =
                {
                    new GradientStop { Color = Color.FromArgb(255, 10, 25, 42), Offset = 0.0 },
                    new GradientStop { Color = Color.FromArgb(255, 14, 48, 68), Offset = 0.35 },
                    new GradientStop { Color = Color.FromArgb(255, 20, 78, 92), Offset = 0.70 },
                    new GradientStop { Color = Color.FromArgb(255, 34, 112, 118), Offset = 1.0 }
                }
            };
            Background = darkGradient;
            BorderBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));
            BorderThickness = new Thickness(1);
            _watermarkText.Foreground = new SolidColorBrush(Color.FromArgb(45, 255, 255, 255));
            _subtitleText.Foreground = new SolidColorBrush(Color.FromArgb(50, 180, 230, 240));
            _circle1.Stroke = new SolidColorBrush(Color.FromArgb(60, 42, 161, 152));
            _circle2.Stroke = new SolidColorBrush(Color.FromArgb(80, 108, 194, 204));
            _circle2.Fill = new SolidColorBrush(Color.FromArgb(30, 38, 139, 210));
        }
    }
}
