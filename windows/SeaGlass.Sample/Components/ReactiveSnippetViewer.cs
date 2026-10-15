using System;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SeaGlass.Sample.Common;
using Windows.UI;

namespace SeaGlass.Sample.Components;

/// <summary>
/// Reusable code snippet viewer displaying synchronized XAML markup and Fluent C# code
/// with live reactive updates and one-click clipboard copying.
/// </summary>
public sealed class ReactiveSnippetViewer : StackPanel
{
    private string _xamlCode = string.Empty;
    private string _fluentCode = string.Empty;
    private bool _isXamlTab = true;

    private readonly Button _xamlTabButton;
    private readonly Button _fluentTabButton;
    private readonly Button _copyButton;
    private readonly TextBox _codeTextBox;
    private readonly DispatcherTimer _copyTimer;

    public ReactiveSnippetViewer()
    {
        Spacing = 12;
        Padding = new Thickness(16);
        Background = new SolidColorBrush(Color.FromArgb(35, 15, 23, 42)); // Deep slate
        CornerRadius = new CornerRadius(12);
        BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
        BorderThickness = new Thickness(1);

        // Header Grid: Tabs on left, Copy button on right
        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var tabsPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };

        _xamlTabButton = new Button
        {
            Content = "XAML",
            FontWeight = FontWeights.Bold
        };
        _xamlTabButton.Click += (_, _) => SelectTab(true);

        _fluentTabButton = new Button
        {
            Content = "Fluent C#",
            FontWeight = FontWeights.Normal
        };
        _fluentTabButton.Click += (_, _) => SelectTab(false);

        tabsPanel.Children.Add(_xamlTabButton);
        tabsPanel.Children.Add(_fluentTabButton);
        Grid.SetColumn(tabsPanel, 0);

        _copyButton = new Button
        {
            Content = "📋 Copy Code",
            HorizontalAlignment = HorizontalAlignment.Right
        };

        _copyTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _copyTimer.Tick += (_, _) =>
        {
            _copyButton.Content = "📋 Copy Code";
            _copyTimer.Stop();
        };

        _copyButton.Click += (_, _) =>
        {
            string currentCode = _isXamlTab ? _xamlCode : _fluentCode;
            if (ClipboardHelper.CopyText(currentCode))
            {
                _copyButton.Content = "Copied! ✓";
                _copyTimer.Stop();
                _copyTimer.Start();
            }
        };
        Grid.SetColumn(_copyButton, 1);

        headerGrid.Children.Add(tabsPanel);
        headerGrid.Children.Add(_copyButton);
        Children.Add(headerGrid);

        // Code Box
        _codeTextBox = new TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            FontFamily = new FontFamily("Cascadia Code, Consolas, Courier New"),
            FontSize = 12,
            Background = new SolidColorBrush(Color.FromArgb(20, 0, 0, 0)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            MinHeight = 140,
            MaxHeight = 280
        };
        Children.Add(_codeTextBox);

        SelectTab(true);
    }

    public void UpdateSnippets(string xamlCode, string fluentCode)
    {
        _xamlCode = xamlCode ?? string.Empty;
        _fluentCode = fluentCode ?? string.Empty;
        _codeTextBox.Text = _isXamlTab ? _xamlCode : _fluentCode;
    }

    private void SelectTab(bool isXaml)
    {
        _isXamlTab = isXaml;
        _xamlTabButton.FontWeight = isXaml ? FontWeights.Bold : FontWeights.Normal;
        _fluentTabButton.FontWeight = isXaml ? FontWeights.Normal : FontWeights.Bold;
        _codeTextBox.Text = isXaml ? _xamlCode : _fluentCode;
    }
}
