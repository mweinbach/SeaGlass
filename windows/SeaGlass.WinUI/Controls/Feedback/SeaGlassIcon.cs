using System;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Renders a crisp vector SeaGlassGlyph harmonized with the active SeaGlass content color.
/// Can be used as a lookless WinUI 3 control or via static helpers.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Viewbox))]
[TemplatePart(Name = "PART_SymbolPresenter", Type = typeof(TextBlock))]
public class SeaGlassIcon : SeaGlassSurface
{
    public static readonly DependencyProperty GlyphProperty =
        DependencyProperty.Register(
            nameof(Glyph),
            typeof(SeaGlassGlyph),
            typeof(SeaGlassIcon),
            new PropertyMetadata(SeaGlassGlyph.Sparkles, OnGlyphChanged));

    public static readonly DependencyProperty GlyphSizeDpProperty =
        DependencyProperty.Register(
            nameof(GlyphSizeDp),
            typeof(double),
            typeof(SeaGlassIcon),
            new PropertyMetadata(24.0));

    public static readonly DependencyProperty CustomSymbolProperty =
        DependencyProperty.Register(
            nameof(CustomSymbol),
            typeof(string),
            typeof(SeaGlassIcon),
            new PropertyMetadata(null));

    public SeaGlassGlyph Glyph
    {
        get => (SeaGlassGlyph)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public double GlyphSizeDp
    {
        get => (double)GetValue(GlyphSizeDpProperty);
        set => SetValue(GlyphSizeDpProperty, value);
    }

    public string? CustomSymbol
    {
        get => (string?)GetValue(CustomSymbolProperty);
        set => SetValue(CustomSymbolProperty, value);
    }

    public SeaGlassIcon()
    {
        DefaultStyleKey = typeof(SeaGlassIcon);
        Size = SeaGlassControlSize.Icon;
        Content = ToUnicodeSymbol(Glyph);
        SizePx = new Vector2(24f, 24f);
        BasePaddingPx = Vector4.Zero;
    }

    public SeaGlassIcon(
        SeaGlassGlyph glyph = SeaGlassGlyph.Sparkles,
        float sizeDp = 24f,
        SeaGlassStageState? stage = null,
        SeaGlassMaterial? material = null)
        : base(stage, material)
    {
        DefaultStyleKey = typeof(SeaGlassIcon);
        Glyph = glyph;
        GlyphSizeDp = sizeDp;
        Content = ToUnicodeSymbol(glyph);
        SizePx = new Vector2(sizeDp, sizeDp);
        BasePaddingPx = Vector4.Zero;
    }

    private static void OnGlyphChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SeaGlassIcon icon && e.NewValue is SeaGlassGlyph g)
        {
            icon.Content = ToUnicodeSymbol(g);
            if (icon._symbolPresenter != null)
            {
                icon._symbolPresenter.Text = icon.CustomSymbol ?? ToUnicodeSymbol(g);
            }
        }
    }

    private Viewbox? _root;
    private TextBlock? _symbolPresenter;

    public TextBlock? SymbolPresenter => _symbolPresenter;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _root = GetTemplateChild("PART_Root") as Viewbox;
        _symbolPresenter = GetTemplateChild("PART_SymbolPresenter") as TextBlock;
        if (_symbolPresenter != null)
        {
            _symbolPresenter.Text = CustomSymbol ?? ToUnicodeSymbol(Glyph);
        }
    }

    public static string ToUnicodeSymbol(SeaGlassGlyph glyph) => glyph switch
    {
        SeaGlassGlyph.Home => "⌂",
        SeaGlassGlyph.Search => "⌕",
        SeaGlassGlyph.Tune => "⊶",
        SeaGlassGlyph.Sparkles => "✦",
        SeaGlassGlyph.Play => "▶",
        SeaGlassGlyph.Pause => "❚❚",
        SeaGlassGlyph.Next => "▶▶",
        SeaGlassGlyph.Check => "✓",
        SeaGlassGlyph.Close => "✕",
        SeaGlassGlyph.Plus => "＋",
        SeaGlassGlyph.Heart => "♡",
        SeaGlassGlyph.Share => "⤴",
        SeaGlassGlyph.Bookmark => "⚑",
        SeaGlassGlyph.Bell => "🔔",
        SeaGlassGlyph.Settings => "⚙",
        SeaGlassGlyph.Back => "‹",
        SeaGlassGlyph.Menu => "☰",
        SeaGlassGlyph.Info => "ⓘ",
        SeaGlassGlyph.Layers => "❖",
        SeaGlassGlyph.AudioWave => "ılı",
        SeaGlassGlyph.PhotoMountain => "⛰",
        SeaGlassGlyph.Microphone => "🎤",
        SeaGlassGlyph.BrandG => "G",
        _ => "✦"
    };

    public static string BuildGlyphXamlPathData(SeaGlassGlyph glyph, float w = 20f, float h = 20f)
    {
        SeaGlassVectorPath2D p = new();
        switch (glyph)
        {
            case SeaGlassGlyph.Home:
                p.MoveTo(w * 0.50f, h * 0.12f);
                p.LineTo(w * 0.88f, h * 0.45f);
                p.LineTo(w * 0.78f, h * 0.45f);
                p.LineTo(w * 0.78f, h * 0.86f);
                p.LineTo(w * 0.22f, h * 0.86f);
                p.LineTo(w * 0.22f, h * 0.45f);
                p.LineTo(w * 0.12f, h * 0.45f);
                p.Close();
                break;

            case SeaGlassGlyph.Play:
                p.MoveTo(w * 0.24f, h * 0.16f);
                p.LineTo(w * 0.84f, h * 0.50f);
                p.LineTo(w * 0.24f, h * 0.84f);
                p.Close();
                break;

            case SeaGlassGlyph.Check:
                p.MoveTo(w * 0.18f, h * 0.52f);
                p.LineTo(w * 0.42f, h * 0.76f);
                p.LineTo(w * 0.84f, h * 0.26f);
                break;

            case SeaGlassGlyph.Plus:
                p.MoveTo(w * 0.50f, h * 0.18f);
                p.LineTo(w * 0.50f, h * 0.82f);
                p.MoveTo(w * 0.18f, h * 0.50f);
                p.LineTo(w * 0.82f, h * 0.50f);
                break;

            case SeaGlassGlyph.Close:
                p.MoveTo(w * 0.22f, h * 0.22f);
                p.LineTo(w * 0.78f, h * 0.78f);
                p.MoveTo(w * 0.78f, h * 0.22f);
                p.LineTo(w * 0.22f, h * 0.78f);
                break;

            case SeaGlassGlyph.Back:
                p.MoveTo(w * 0.64f, h * 0.18f);
                p.LineTo(w * 0.28f, h * 0.50f);
                p.LineTo(w * 0.64f, h * 0.82f);
                break;

            default:
                p.MoveTo(w * 0.50f, h * 0.10f);
                p.LineTo(w * 0.90f, h * 0.50f);
                p.LineTo(w * 0.50f, h * 0.90f);
                p.LineTo(w * 0.10f, h * 0.50f);
                p.Close();
                break;
        }
        return p.ToXamlPathData();
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new SeaGlass.WinUI.Automation.SeaGlassIconAutomationPeer(this);
}
