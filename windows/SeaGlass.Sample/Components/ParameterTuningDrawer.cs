using System;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SeaGlass.Core;
using SeaGlass.Sample.Common;
using SeaGlass.WinUI;
using Windows.UI;

namespace SeaGlass.Sample.Components;

/// <summary>
/// Interactive drawer panel with live sliders and pickers for real-time optical parameter tuning.
/// </summary>
public sealed class ParameterTuningDrawer : StackPanel
{
    private readonly TuningViewModel _viewModel;
    private readonly Slider _coarsenessSlider;
    private readonly Slider _thicknessSlider;
    private readonly Slider _iorSlider;
    private readonly Slider _rimSlider;
    private readonly Slider _tintSlider;
    private readonly ComboBox _toneCombo;
    private readonly ComboBox _variantCombo;
    private readonly ComboBox _sizeCombo;

    public ParameterTuningDrawer(TuningViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        Spacing = 16;
        Padding = new Thickness(18);
        Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255));
        CornerRadius = new CornerRadius(12);
        BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
        BorderThickness = new Thickness(1);

        // Header
        var headerPanel = new StackPanel { Spacing = 4 };
        headerPanel.Children.Add(new TextBlock
        {
            Text = "Optical & M3 Expressive Parameters",
            FontSize = 16,
            FontWeight = FontWeights.Bold
        });
        headerPanel.Children.Add(new TextBlock
        {
            Text = "Adjust parameters to observe volumetric Beer-Lambert absorption and micro-facet coarseness.",
            FontSize = 12,
            Opacity = 0.7,
            TextWrapping = TextWrapping.Wrap
        });
        Children.Add(headerPanel);

        // Sliders
        _coarsenessSlider = AddSliderControl("Coarseness (Sand Micro-facets)", 0.0, 1.0, 0.01, _viewModel.Coarseness,
            val => _viewModel.Coarseness = val, "F2");
        _thicknessSlider = AddSliderControl("Thickness (Volumetric Absorption Depth)", 1.0, 32.0, 0.5, _viewModel.ThicknessDp,
            val => _viewModel.ThicknessDp = val, "F1", " dp");
        _iorSlider = AddSliderControl("Index of Refraction (IOR)", 1.0, 2.0, 0.01, _viewModel.Ior,
            val => _viewModel.Ior = val, "F2");
        _rimSlider = AddSliderControl("Rim Chalkiness (Rough-Fresnel Glow)", 0.0, 1.0, 0.01, _viewModel.RimChalkiness,
            val => _viewModel.RimChalkiness = val, "F2");
        _tintSlider = AddSliderControl("Tint Density (Oklab Absorption Sigma)", 0.0, 2.0, 0.05, _viewModel.TintDensity,
            val => _viewModel.TintDensity = val, "F2");

        // ComboBoxes Grid
        var comboGrid = new Grid { ColumnSpacing = 12 };
        comboGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        comboGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        comboGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _toneCombo = new ComboBox
        {
            Header = "Tone Palette",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = Enum.GetValues<SeaGlassTone>(),
            SelectedItem = _viewModel.Tone
        };
        _toneCombo.SelectionChanged += (_, _) =>
        {
            if (_toneCombo.SelectedItem is SeaGlassTone t) _viewModel.Tone = t;
        };
        Grid.SetColumn(_toneCombo, 0);

        _variantCombo = new ComboBox
        {
            Header = "Variant",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = Enum.GetValues<SeaGlassVariant>(),
            SelectedItem = _viewModel.Variant
        };
        _variantCombo.SelectionChanged += (_, _) =>
        {
            if (_variantCombo.SelectedItem is SeaGlassVariant v) _viewModel.Variant = v;
        };
        Grid.SetColumn(_variantCombo, 1);

        _sizeCombo = new ComboBox
        {
            Header = "Control Size",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = Enum.GetValues<SeaGlassControlSize>(),
            SelectedItem = _viewModel.Size
        };
        _sizeCombo.SelectionChanged += (_, _) =>
        {
            if (_sizeCombo.SelectedItem is SeaGlassControlSize s) _viewModel.Size = s;
        };
        Grid.SetColumn(_sizeCombo, 2);

        comboGrid.Children.Add(_toneCombo);
        comboGrid.Children.Add(_variantCombo);
        comboGrid.Children.Add(_sizeCombo);
        Children.Add(comboGrid);

        // Reset Button
        var resetButton = new Button
        {
            Content = "Reset to Defaults",
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 4, 0, 0)
        };
        resetButton.Click += (_, _) =>
        {
            _viewModel.ResetToDefaults();
            SyncSlidersFromModel();
        };
        Children.Add(resetButton);

        _viewModel.PropertyChanged += (_, _) => SyncSlidersFromModel();
    }

    private void SyncSlidersFromModel()
    {
        _coarsenessSlider.Value = _viewModel.Coarseness;
        _thicknessSlider.Value = _viewModel.ThicknessDp;
        _iorSlider.Value = _viewModel.Ior;
        _rimSlider.Value = _viewModel.RimChalkiness;
        _tintSlider.Value = _viewModel.TintDensity;
        _toneCombo.SelectedItem = _viewModel.Tone;
        _variantCombo.SelectedItem = _viewModel.Variant;
        _sizeCombo.SelectedItem = _viewModel.Size;
    }

    private Slider AddSliderControl(string header, double min, double max, double step, double current,
        Action<double> onValueChanged, string format, string unit = "")
    {
        var label = new TextBlock
        {
            Text = $"{header}: {current.ToString(format)}{unit}",
            FontSize = 13,
            FontWeight = FontWeights.Medium
        };

        var slider = new Slider
        {
            Minimum = min,
            Maximum = max,
            StepFrequency = step,
            Value = current,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        slider.ValueChanged += (_, e) =>
        {
            label.Text = $"{header}: {e.NewValue.ToString(format)}{unit}";
            onValueChanged(e.NewValue);
        };

        var container = new StackPanel { Spacing = 2 };
        container.Children.Add(label);
        container.Children.Add(slider);
        Children.Add(container);

        return slider;
    }
}
