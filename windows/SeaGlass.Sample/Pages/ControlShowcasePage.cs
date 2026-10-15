using SeaGlass.WinUI;
using Microsoft.UI.Xaml.Automation;
using System;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SeaGlass.Sample.Catalog;
using SeaGlass.Sample.Common;
using SeaGlass.Sample.Components;

namespace SeaGlass.Sample.Pages;

/// <summary>
/// Universal showcase page hosting a live control preview on the oceanic stage host,
/// parameter tuning drawer, and reactive code snippet viewer.
/// </summary>
public class ControlShowcasePage : Page
{
    private readonly ControlShowcaseDescriptor _descriptor;
    private readonly TuningViewModel _tuning = new();
    private readonly ShowcaseStageHost _stageHost;
    private readonly ParameterTuningDrawer _tuningDrawer;
    private readonly ReactiveSnippetViewer _snippetViewer;
    private FrameworkElement _previewElement;

    public ControlShowcasePage(string controlName)
        : this(ControlShowcaseCatalog.FindByName(controlName) ??
               throw new ArgumentException($"Control '{controlName}' not found in catalog", nameof(controlName)))
    {
    }

    public ControlShowcasePage(ControlShowcaseDescriptor descriptor)
    {
        _descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));

        var rootScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        var contentStack = new StackPanel
        {
            Spacing = 24,
            Padding = new Thickness(32),
            MaxWidth = 1200,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        // 1. Header Information
        var headerStack = new StackPanel { Spacing = 6 };
        var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        titleRow.Children.Add(new TextBlock
        {
            Text = _descriptor.Name,
            FontSize = 28,
            FontWeight = FontWeights.Bold
        });
        titleRow.Children.Add(new Border
        {
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(40, 255, 255, 255)),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(10, 4, 10, 4),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = _descriptor.Category,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Opacity = 0.85
            }
        });
        headerStack.Children.Add(titleRow);

        headerStack.Children.Add(new TextBlock
        {
            Text = _descriptor.Description,
            FontSize = 14,
            Opacity = 0.75,
            TextWrapping = TextWrapping.Wrap
        });
        contentStack.Children.Add(headerStack);

        // 2. Main Stage and Controls Layout (2-Column Grid)
        _stageHost = new ShowcaseStageHost();
        _previewElement = _descriptor.PreviewFactory();
        AutomationProperties.SetAutomationId(_previewElement, "Specimen_" + _descriptor.Name);
        if (string.IsNullOrWhiteSpace(AutomationProperties.GetName(_previewElement)))
        {
            AutomationProperties.SetName(_previewElement, _descriptor.Name);
        }

        var invokeCountBlock = new TextBlock
        {
            Text = "0",
            Width = 1,
            Height = 1,
            Opacity = 0.01,
            IsTabStop = false
        };
        AutomationProperties.SetAutomationId(invokeCountBlock, "InvokeCount_" + _descriptor.Name);
        AutomationProperties.SetName(invokeCountBlock, "InvokeCount_" + _descriptor.Name);

        int invokeCount = 0;
        void RegisterClick()
        {
            invokeCount++;
            invokeCountBlock.Text = invokeCount.ToString();
        }

        if (_previewElement is SeaGlassButton btn) btn.Click += (s, e) => RegisterClick();
        else if (_previewElement is SeaGlassIconButton ibtn) ibtn.Click += (s, e) => RegisterClick();
        else if (_previewElement is SeaGlassExtendedFab fab) fab.Click += (s, e) => RegisterClick();
        else if (_previewElement is SeaGlassPebbleOrb orb) orb.Click += (s, e) => RegisterClick();
        else if (_previewElement is SeaGlassMorphingButton mb) mb.Click += (s, e) => RegisterClick();
        else if (_previewElement is SeaGlassListItem li) li.Click += (s, e) => RegisterClick();
        else if (_previewElement is Button sysBtn) sysBtn.Click += (s, e) => RegisterClick();
        else if (_previewElement is SeaGlassSurface surf) surf.Click += (s, e) => RegisterClick();

        contentStack.Children.Add(invokeCountBlock);
        _stageHost.SetPreview(_previewElement);

        var bodyGrid = new Grid { ColumnSpacing = 24 };
        bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(380, GridUnitType.Pixel) });

        var leftStack = new StackPanel { Spacing = 16 };
        leftStack.Children.Add(_stageHost);
        if (_descriptor.ActionFactory != null)
        {
            var actionControl = _descriptor.ActionFactory();
            leftStack.Children.Add(actionControl);
        }
        Grid.SetColumn(leftStack, 0);
        bodyGrid.Children.Add(leftStack);

        var rightStack = new StackPanel { Spacing = 16 };
        _tuningDrawer = new ParameterTuningDrawer(_tuning);
        _snippetViewer = new ReactiveSnippetViewer();
        rightStack.Children.Add(_tuningDrawer);
        rightStack.Children.Add(_snippetViewer);
        Grid.SetColumn(rightStack, 1);
        bodyGrid.Children.Add(rightStack);

        contentStack.Children.Add(bodyGrid);

        rootScroll.Content = contentStack;
        Content = rootScroll;

        // Hook tuning changes
        _tuning.PropertyChanged += (_, _) => OnTuningChanged();
        OnTuningChanged();
    }

    public ControlShowcaseDescriptor Descriptor => _descriptor;

    public FrameworkElement PreviewElement => _previewElement;

    public ShowcaseStageHost StageHost => _stageHost;

    public void ApplyTheme(ElementTheme theme, bool isHighContrast = false)
    {
        RequestedTheme = theme;
        if (isHighContrast)
        {
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0));
        }
        else if (theme == ElementTheme.Dark)
        {
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 24, 28, 34));
        }
        else
        {
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 248, 250, 252));
        }
        _stageHost.ApplyTheme(theme, isHighContrast);
    }

    private void OnTuningChanged()
    {
        _descriptor.ConfigurePreview?.Invoke(_previewElement, _tuning);

        string xaml = SnippetCodeGenerator.GenerateXaml(_descriptor, _tuning);
        string fluent = SnippetCodeGenerator.GenerateFluent(_descriptor, _tuning);
        _snippetViewer.UpdateSnippets(xaml, fluent);
    }
}
