using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.ConsumerSample.Views;

namespace SeaGlass.ConsumerSample;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();

        var appWindow = this.AppWindow;
        if (appWindow != null)
        {
            appWindow.Resize(new Windows.Graphics.SizeInt32(1280, 820));
        }

        NavView.SelectionChanged += OnNavSelectionChanged;
        ContentFrame.Content = new XamlControlsPage();
    }

    public void SelectPage(string tag)
    {
        if (tag == "xaml" && NavView.MenuItems.Count > 0)
        {
            NavView.SelectedItem = NavView.MenuItems[0];
        }
        else if (tag == "fluent" && NavView.MenuItems.Count > 1)
        {
            NavView.SelectedItem = NavView.MenuItems[1];
        }
    }

    private void OnNavSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item)
        {
            string? tag = item.Tag as string;
            if (tag == "xaml")
            {
                ContentFrame.Content = new XamlControlsPage();
            }
            else if (tag == "fluent")
            {
                ContentFrame.Content = new FluentPage();
            }
        }
    }
}
