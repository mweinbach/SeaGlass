using System;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// High-chroma SeaGlass Badge for notification counts or status callouts.
/// </summary>
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_ContentPresenter", Type = typeof(ContentPresenter))]
[TemplatePart(Name = "PART_TextPresenter", Type = typeof(TextBlock))]
public class SeaGlassBadge : SeaGlassSurface
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(SeaGlassBadge),
            new PropertyMetadata(string.Empty, OnTextChanged));

    public static readonly DependencyProperty CountProperty =
        DependencyProperty.Register(
            nameof(Count),
            typeof(int),
            typeof(SeaGlassBadge),
            new PropertyMetadata(0, OnCountChanged));

    public static readonly DependencyProperty IsDotProperty =
        DependencyProperty.Register(
            nameof(IsDot),
            typeof(bool),
            typeof(SeaGlassBadge),
            new PropertyMetadata(false));

    public static readonly DependencyProperty MaxCountProperty =
        DependencyProperty.Register(
            nameof(MaxCount),
            typeof(int),
            typeof(SeaGlassBadge),
            new PropertyMetadata(99));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public int Count
    {
        get => (int)GetValue(CountProperty);
        set => SetValue(CountProperty, value);
    }

    public bool IsDot
    {
        get => (bool)GetValue(IsDotProperty);
        set => SetValue(IsDotProperty, value);
    }

    public int MaxCount
    {
        get => (int)GetValue(MaxCountProperty);
        set => SetValue(MaxCountProperty, value);
    }

    public SeaGlassBadge()
    {
        DefaultStyleKey = typeof(SeaGlassBadge);
        CornerAdaptationMode = SeaGlassCornerAdaptationMode.Fixed;
        ContentRole = SeaGlassContentRole.TextAndIcons;
        SizePx = new Vector2(56f, 24f);
        BasePaddingPx = new Vector4(8f, 2f, 8f, 2f);
    }

    public SeaGlassBadge(
        string text,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? material = null)
        : base(
            stageState: stageState,
            material: (material ?? SeaGlassTheme.Current.Palette.TertiaryGlass) with
            {
                CornerRadiusDp = 12f,
                ThicknessDp = 14f
            },
            contentRole: SeaGlassContentRole.TextAndIcons)
    {
        DefaultStyleKey = typeof(SeaGlassBadge);
        Text = text;
        Content = text;
        SizePx = new Vector2(56f, 24f);
        BasePaddingPx = new Vector4(8f, 2f, 8f, 2f);
    }

    public string Label
    {
        get => Text;
        set => Text = value;
    }

    public static SeaGlassBadge Create(string label = "") =>
        new SeaGlassBadge(label);

    public static SeaGlassBadge Create(int count) =>
        new SeaGlassBadge { Count = count };

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassBadge b && e.NewValue != null)
        {
            b.Content = e.NewValue;
            if (b.GetTemplateChild("PART_TextPresenter") is TextBlock tb)
            {
                tb.Text = e.NewValue.ToString() ?? string.Empty;
            }
        }
    }

    private static void OnCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassBadge b && e.NewValue is int count)
        {
            string val = count > b.MaxCount ? $"{b.MaxCount}+" : count.ToString();
            b.Text = val;
            b.Content = val;
            if (b.GetTemplateChild("PART_TextPresenter") is TextBlock tb)
            {
                tb.Text = val;
            }
        }
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        string val = !string.IsNullOrEmpty(Text) ? Text : (Count > 0 ? (Count > MaxCount ? $"{MaxCount}+" : Count.ToString()) : "");
        if (string.IsNullOrEmpty(Text) && !string.IsNullOrEmpty(val))
        {
            Text = val;
        }
        if (Content == null && !string.IsNullOrEmpty(val))
        {
            Content = val;
        }
        if (GetTemplateChild("PART_TextPresenter") is TextBlock tb && !string.IsNullOrEmpty(val))
        {
            tb.Text = val;
        }
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new SeaGlass.WinUI.Automation.SeaGlassBadgeAutomationPeer(this);
}
