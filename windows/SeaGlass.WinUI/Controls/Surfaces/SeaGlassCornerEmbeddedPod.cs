using System;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Flush-embedded corner dock/pod surface utilizing FlushCornerEmbed adaptation
/// to conform concentrically to device/window corners.
/// </summary>
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_ContentPresenter", Type = typeof(ContentPresenter))]
public class SeaGlassCornerEmbeddedPod : SeaGlassSurface
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(
            nameof(Label),
            typeof(string),
            typeof(SeaGlassCornerEmbeddedPod),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SubLabelProperty =
        DependencyProperty.Register(
            nameof(SubLabel),
            typeof(string),
            typeof(SeaGlassCornerEmbeddedPod),
            new PropertyMetadata(null));

    public static readonly DependencyProperty AnchorCornerProperty =
        DependencyProperty.Register(
            nameof(AnchorCorner),
            typeof(SeaGlassAnchorCorner),
            typeof(SeaGlassCornerEmbeddedPod),
            new PropertyMetadata(SeaGlassAnchorCorner.TopLeft));

    public static readonly DependencyProperty EffectiveInsetDpProperty =
        DependencyProperty.Register(
            nameof(EffectiveInsetDp),
            typeof(double),
            typeof(SeaGlassCornerEmbeddedPod),
            new PropertyMetadata(8.0));

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string? SubLabel
    {
        get => (string?)GetValue(SubLabelProperty);
        set => SetValue(SubLabelProperty, value);
    }

    public SeaGlassAnchorCorner AnchorCorner
    {
        get => (SeaGlassAnchorCorner)GetValue(AnchorCornerProperty);
        set => SetValue(AnchorCornerProperty, value);
    }

    public double EffectiveInsetDp
    {
        get => (double)GetValue(EffectiveInsetDpProperty);
        set => SetValue(EffectiveInsetDpProperty, value);
    }

    public SeaGlassCornerEmbeddedPod()
    {
        DefaultStyleKey = typeof(SeaGlassCornerEmbeddedPod);
        CornerAdaptationMode = SeaGlassCornerAdaptationMode.FlushCornerEmbed;
        ContentRole = SeaGlassContentRole.TextAndIcons;
        SizePx = new Vector2(148f, 78f);
        BasePaddingPx = new Vector4(14f, 10f, 14f, 10f);
    }

    public SeaGlassCornerEmbeddedPod(
        string label,
        string? subLabel = null,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? material = null,
        Action? onClick = null)
        : base(
            stageState: stageState,
            material: material ?? SeaGlassTheme.Current.Palette.PrimaryGlass,
            cornerAdaptationMode: SeaGlassCornerAdaptationMode.FlushCornerEmbed,
            contentRole: SeaGlassContentRole.TextAndIcons,
            onClick: onClick)
    {
        DefaultStyleKey = typeof(SeaGlassCornerEmbeddedPod);
        Label = label;
        SubLabel = subLabel;
        SizePx = new Vector2(148f, 78f);
        BasePaddingPx = new Vector4(14f, 10f, 14f, 10f);
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new SeaGlass.WinUI.Automation.SeaGlassCornerEmbeddedPodAutomationPeer(this);
}
