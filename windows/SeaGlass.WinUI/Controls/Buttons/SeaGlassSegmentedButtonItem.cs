using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SeaGlass.WinUI;

/// <summary>
/// Child item inside a SeaGlassSegmentedButtonRow.
/// </summary>
public class SeaGlassSegmentedButtonItem : SeaGlassButton
{
    public static readonly DependencyProperty IndexProperty =
        DependencyProperty.Register(
            nameof(Index),
            typeof(int),
            typeof(SeaGlassSegmentedButtonItem),
            new PropertyMetadata(0));

    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register(
            nameof(IsSelected),
            typeof(bool),
            typeof(SeaGlassSegmentedButtonItem),
            new PropertyMetadata(false));

    public int Index
    {
        get => (int)GetValue(IndexProperty);
        set => SetValue(IndexProperty, value);
    }

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public SeaGlassSegmentedButtonItem()
    {
        DefaultStyleKey = typeof(SeaGlassSegmentedButtonItem);
    }

    public SeaGlassSegmentedButtonItem(string label, int index, bool isSelected = false)
        : base(label)
    {
        DefaultStyleKey = typeof(SeaGlassSegmentedButtonItem);
        Index = index;
        IsSelected = isSelected;
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new SeaGlass.WinUI.Automation.SeaGlassSegmentedButtonItemAutomationPeer(this);
}
