using System;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Ocean-tumbled circular/pebble SeaGlass medallion button.
/// </summary>
[TemplatePart(Name = "PART_Root", Type = typeof(Grid))]
[TemplatePart(Name = "PART_GlassHost", Type = typeof(Border))]
[TemplatePart(Name = "PART_GlyphPresenter", Type = typeof(TextBlock))]
[TemplatePart(Name = "PART_ContentPresenter", Type = typeof(ContentPresenter))]
public class SeaGlassPebbleOrb : SeaGlassSurface
{
    private float _expandScale = 1f;
    private float _expandVel;

    public static readonly DependencyProperty GlyphProperty =
        DependencyProperty.Register(
            nameof(Glyph),
            typeof(SeaGlassGlyph),
            typeof(SeaGlassPebbleOrb),
            new PropertyMetadata(SeaGlassGlyph.BrandG));

    public static readonly DependencyProperty OrbSizeDpProperty =
        DependencyProperty.Register(
            nameof(OrbSizeDp),
            typeof(double),
            typeof(SeaGlassPebbleOrb),
            new PropertyMetadata(64.0));

    public SeaGlassGlyph Glyph
    {
        get => (SeaGlassGlyph)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public double OrbSizeDp
    {
        get => (double)GetValue(OrbSizeDpProperty);
        set => SetValue(OrbSizeDpProperty, value);
    }

    public float ExpandScale => _expandScale;
    public SeaGlassMaterial BaseMaterial
    {
        get => Material;
        set => Material = value;
    }

    public SeaGlassPebbleOrb()
    {
        DefaultStyleKey = typeof(SeaGlassPebbleOrb);
        CornerAdaptationMode = SeaGlassCornerAdaptationMode.Fixed;
        ContentRole = SeaGlassContentRole.TextAndIcons;
        SizePx = new Vector2(64f, 64f);
    }

    public SeaGlassPebbleOrb(
        Action? onClick = null,
        SeaGlassStageState? stageState = null,
        float sizeDp = 64f,
        SeaGlassGlyph glyph = SeaGlassGlyph.BrandG,
        SeaGlassMaterial? material = null)
        : base(
            stageState: stageState,
            material: material ?? new SeaGlassMaterial(
                tintColorArgb: unchecked((int)0xFFF7A89A),
                tintDensity: 0.72f,
                coarseness: 0.58f,
                frostRadiusDp: 26f,
                ior: 1.54f,
                chromaticDispersion: 0.22f,
                rimChalkiness: 0.90f,
                thicknessDp: 18f,
                cornerRadiusDp: sizeDp * 0.5f,
                cornerSmoothing: 2.4f,
                transparency: 0.52f),
            cornerAdaptationMode: SeaGlassCornerAdaptationMode.Fixed,
            contentRole: SeaGlassContentRole.TextAndIcons,
            onClick: onClick)
    {
        DefaultStyleKey = typeof(SeaGlassPebbleOrb);
        Glyph = glyph;
        OrbSizeDp = sizeDp;
        SizePx = new Vector2(sizeDp, sizeDp);
    }

    public SeaGlassSurfaceFrameState StepAndEvaluate(float dtSeconds = 1f / 60f, SeaGlassTheme? theme = null)
    {
        SeaGlassSpringSpec spatialSpring = new(DampingRatio: 0.68f, Stiffness: 460f);
        spatialSpring.Step(ref _expandScale, ref _expandVel, 1.0f, dtSeconds);
        return EvaluateFrame(dtSeconds, theme ?? SeaGlassTheme.Current);
    }

    private Grid? _root;
    private Border? _glassHost;
    private TextBlock? _glyphPresenter;
    private ContentPresenter? _contentPresenter;

    public TextBlock? GlyphPresenter => _glyphPresenter;
    public ContentPresenter? ContentPresenterChild => _contentPresenter;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _root = GetTemplateChild("PART_Root") as Grid;
        _glassHost = GetTemplateChild("PART_GlassHost") as Border;
        _glyphPresenter = GetTemplateChild("PART_GlyphPresenter") as TextBlock;
        _contentPresenter = GetTemplateChild("PART_ContentPresenter") as ContentPresenter;

        if (_glyphPresenter != null)
        {
            _glyphPresenter.Text = SeaGlassIcon.ToUnicodeSymbol(Glyph);
        }
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new SeaGlass.WinUI.Automation.SeaGlassPebbleOrbAutomationPeer(this);
}

