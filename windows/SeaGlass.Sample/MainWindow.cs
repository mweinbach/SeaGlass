using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using SeaGlass.Core;
using SeaGlass.Core.Backend;
using SeaGlass.WinUI;
using SeaGlass.WinUI.Rendering;

namespace SeaGlass.Sample;

/// <summary>
/// Fixed desktop window metrics shared by the WPF layout tree and the stage backdrop so that
/// glass surfaces sample exactly the window layer that is visually beneath them.
/// </summary>
internal static class StudioLayout
{
    public const double TitleBarHeight = 48;
    public const double SidebarWidth = 232;
    public const double InspectorWidth = 316;
    public const double StatusBarHeight = 28;
    public const double ContentCornerRadius = 8;
}

public enum StudioCanvasMode
{
    Artwork,
    PorcelainStudio,
    CodeEditor
}

/// <summary>
/// Desktop window backdrop for the SeaGlass Studio stage. Renders the real window layer stack —
/// a Mica-style tinted window material, the edge-to-edge content layer (top-left 8px corner), and
/// the document canvas (a sea-glass beach artwork, porcelain studio daylight stage, or code editor surface) —
/// so every SeaGlass surface refracts the exact pixels that sit under it. Static layers are cached into a
/// <see cref="DrawingGroup"/> and only the shoreline wave layer animates via a reused transform.
/// </summary>
public sealed class StudioWindowBackdrop : FrameworkElement
{
    private readonly TranslateTransform _waveShift = new();
    private readonly TranslateTransform _foamShift = new();
    private DrawingGroup? _staticScene;
    private DrawingGroup? _waveLayer;
    private DrawingGroup? _foamLayer;
    private Geometry? _canvasClip;
    private RectangleGeometry? _boundsClip;
    private (int PaletteId, StudioCanvasMode Mode, int W, int H) _sceneKey;

    public double Phase { get; set; }
    public StudioCanvasMode CanvasMode { get; set; } = StudioCanvasMode.Artwork;

    public static Rect CanvasRectFor(double w, double h) => new(
        StudioLayout.SidebarWidth,
        StudioLayout.TitleBarHeight,
        Math.Max(10, w - StudioLayout.SidebarWidth - StudioLayout.InspectorWidth - 1),
        Math.Max(10, h - StudioLayout.TitleBarHeight - StudioLayout.StatusBarHeight));

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth > 1 ? ActualWidth : 1440;
        double h = ActualHeight > 1 ? ActualHeight : 920;
        SeaGlassPalette palette = SeaGlassTheme.Current.Palette;

        var key = (palette.Id.GetHashCode(), CanvasMode, (int)w, (int)h);
        if (_staticScene is null || key != _sceneKey)
        {
            _sceneKey = key;
            BuildScene(w, h, palette);
        }

        if (_boundsClip is null || _boundsClip.Rect.Width != w || _boundsClip.Rect.Height != h)
        {
            _boundsClip = new RectangleGeometry(new Rect(0, 0, w, h));
            _boundsClip.Freeze();
        }

        // Content bounds must equal the stage rect exactly: glass samples this visual through a
        // VisualBrush whose Viewbox is relative to the bounding box, so any overdraw would misregister refraction.
        dc.PushClip(_boundsClip);
        dc.DrawDrawing(_staticScene);

        if (CanvasMode == StudioCanvasMode.Artwork && _waveLayer is not null && _foamLayer is not null && _canvasClip is not null)
        {
            _waveShift.X = Math.Sin(Phase) * 14.0;
            _foamShift.X = Math.Sin(Phase + 0.9) * 9.0;
            _foamShift.Y = Math.Cos(Phase) * 2.0;
            dc.PushClip(_canvasClip);
            dc.PushTransform(_waveShift);
            dc.DrawDrawing(_waveLayer);
            dc.Pop();
            dc.PushTransform(_foamShift);
            dc.DrawDrawing(_foamLayer);
            dc.Pop();
            dc.Pop();
        }
        dc.Pop();
    }

    private void BuildScene(double w, double h, SeaGlassPalette palette)
    {
        bool isDark = palette.IsDark;
        int primary = palette.PrimaryGlass.TintColorArgb;
        int secondary = palette.SecondaryGlass.TintColorArgb;
        int tertiary = palette.TertiaryGlass.TintColorArgb;

        DrawingGroup scene = new();
        using (DrawingContext dc = scene.Open())
        {
            // 1. Mica window material: neutral base with a faint, heavily-diffused wallpaper bleed.
            int micaBase = isDark
                ? SeaGlassColorMath.LerpOklab(primary, unchecked((int)0xFF1C1F24), 0.93f)
                : SeaGlassColorMath.LerpOklab(primary, unchecked((int)0xFFEEF1F4), 0.90f);
            dc.DrawRectangle(Frozen(new SolidColorBrush(SeaGlassXamlRenderer.ToMediaColor(micaBase))), null, new Rect(0, 0, w, h));
            DrawSoftPool(dc, new Point(w * 0.10, h * 0.08), Math.Max(w, h) * 0.55, primary, isDark ? 0.07 : 0.10);
            DrawSoftPool(dc, new Point(w * 0.95, h * 0.02), Math.Max(w, h) * 0.45, tertiary, isDark ? 0.05 : 0.07);

            // 2. Content layer: edge-to-edge to the right/bottom window edges, rounded only at top-left.
            Geometry layerGeom = TopLeftRoundedRect(new Rect(StudioLayout.SidebarWidth, StudioLayout.TitleBarHeight,
                w - StudioLayout.SidebarWidth + 1, h - StudioLayout.TitleBarHeight + 1), StudioLayout.ContentCornerRadius);
            Color layerFill = isDark ? Color.FromRgb(0x25, 0x28, 0x2E) : Color.FromRgb(0xFA, 0xFB, 0xFC);
            Color layerStroke = isDark ? Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x16, 0x00, 0x00, 0x00);
            dc.DrawGeometry(Frozen(new SolidColorBrush(layerFill)), Frozen(new Pen(Frozen(new SolidColorBrush(layerStroke)), 1.0)), layerGeom);

            // 3. Document canvas (clipped to the content layer's rounded corner).
            Rect canvas = CanvasRectFor(w, h);
            Geometry canvasClip = TopLeftRoundedRect(canvas, StudioLayout.ContentCornerRadius);
            canvasClip.Freeze();
            _canvasClip = canvasClip;
            dc.PushClip(canvasClip);
            if (CanvasMode == StudioCanvasMode.Artwork)
            {
                DrawBeachArtwork(dc, canvas, isDark, primary, secondary, tertiary);
            }
            else if (CanvasMode == StudioCanvasMode.PorcelainStudio)
            {
                DrawPorcelainStudio(dc, canvas, primary, secondary, tertiary);
            }
            else
            {
                Color editorBg = isDark ? Color.FromRgb(0x1A, 0x1C, 0x21) : Color.FromRgb(0x1E, 0x22, 0x2A);
                dc.DrawRectangle(Frozen(new SolidColorBrush(editorBg)), null, canvas);
                dc.DrawRectangle(Frozen(new SolidColorBrush(Color.FromArgb(0x10, 0xFF, 0xFF, 0xFF))), null,
                    new Rect(canvas.X, canvas.Y, canvas.Width, 38));
            }
            dc.Pop();

            // 4. Pane separators (inspector divider + status bar divider).
            SolidColorBrush divider = Frozen(new SolidColorBrush(isDark ? Color.FromArgb(0x2A, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x1C, 0x00, 0x00, 0x00)));
            dc.DrawRectangle(divider, null, new Rect(canvas.Right, StudioLayout.TitleBarHeight, 1, h - StudioLayout.TitleBarHeight));
            dc.DrawRectangle(divider, null, new Rect(StudioLayout.SidebarWidth, h - StudioLayout.StatusBarHeight, w - StudioLayout.SidebarWidth, 1));
        }
        scene.Freeze();
        _staticScene = scene;

        Rect c = CanvasRectFor(w, h);
        _waveLayer = BuildWaveLayer(c, isDark);
        _foamLayer = BuildFoamLayer(c, isDark);
    }

    private static void DrawPorcelainStudio(DrawingContext dc, Rect c, int primary, int secondary, int tertiary)
    {
        LinearGradientBrush studioBg = new()
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1)
        };
        studioBg.GradientStops.Add(new GradientStop(Color.FromRgb(0xFB, 0xFD, 0xFF), 0.0));
        studioBg.GradientStops.Add(new GradientStop(Color.FromRgb(0xF6, 0xF9, 0xFD), 0.52));
        studioBg.GradientStops.Add(new GradientStop(Color.FromRgb(0xF1, 0xF5, 0xFB), 1.0));
        studioBg.Freeze();
        dc.DrawRectangle(studioBg, null, c);

        // Soft diffused daylight pastel pools so refraction and frosted bevel rinds pop cleanly
        DrawSoftPool(dc, new Point(c.X + c.Width * 0.24, c.Y + c.Height * 0.22), c.Width * 0.48, primary, 0.18);
        DrawSoftPool(dc, new Point(c.X + c.Width * 0.78, c.Y + c.Height * 0.34), c.Width * 0.44, unchecked((int)0xFFFF8E6E), 0.16);
        DrawSoftPool(dc, new Point(c.X + c.Width * 0.28, c.Y + c.Height * 0.64), c.Width * 0.48, tertiary, 0.17);
        DrawSoftPool(dc, new Point(c.X + c.Width * 0.74, c.Y + c.Height * 0.80), c.Width * 0.42, secondary, 0.15);
    }

    private static void DrawBeachArtwork(DrawingContext dc, Rect c, bool isDark, int primary, int secondary, int tertiary)
    {
        double horizon = c.Y + c.Height * 0.44;
        double shore = c.Y + c.Height * 0.72;

        // Sky
        int skyTop = isDark ? SeaGlassColorMath.LerpOklab(primary, unchecked((int)0xFF0A1220), 0.82f) : SeaGlassColorMath.LerpOklab(primary, unchecked((int)0xFFFFFFFF), 0.38f);
        int skyLow = isDark ? SeaGlassColorMath.LerpOklab(tertiary, unchecked((int)0xFF14203A), 0.62f) : SeaGlassColorMath.LerpOklab(tertiary, unchecked((int)0xFFFFF3E4), 0.62f);
        dc.DrawRectangle(Frozen(new LinearGradientBrush(SeaGlassXamlRenderer.ToMediaColor(skyTop), SeaGlassXamlRenderer.ToMediaColor(skyLow), 90)), null,
            new Rect(c.X, c.Y, c.Width, horizon - c.Y + 1));

        // Sun / moon with halo
        Point sun = new(c.X + c.Width * 0.72, c.Y + c.Height * 0.20);
        double sunR = Math.Min(c.Width, c.Height) * 0.075;
        Color sunCol = isDark ? Color.FromRgb(0xE6, 0xEE, 0xFF) : Color.FromRgb(0xFF, 0xE4, 0xAE);
        dc.DrawEllipse(Frozen(new RadialGradientBrush(Color.FromArgb(isDark ? (byte)0x40 : (byte)0x70, sunCol.R, sunCol.G, sunCol.B), Color.FromArgb(0, sunCol.R, sunCol.G, sunCol.B))), null, sun, sunR * 3.4, sunR * 3.4);
        dc.DrawEllipse(Frozen(new SolidColorBrush(sunCol)), null, sun, sunR, sunR);

        // Distant headland silhouettes
        int landCol = isDark ? SeaGlassColorMath.LerpOklab(secondary, unchecked((int)0xFF0B1424), 0.70f) : SeaGlassColorMath.LerpOklab(secondary, unchecked((int)0xFF6E8FA0), 0.55f);
        StreamGeometry land = new();
        using (StreamGeometryContext g = land.Open())
        {
            g.BeginFigure(new Point(c.X, horizon), true, true);
            g.BezierTo(new Point(c.X + c.Width * 0.10, horizon - c.Height * 0.09), new Point(c.X + c.Width * 0.22, horizon - c.Height * 0.06), new Point(c.X + c.Width * 0.34, horizon), true, true);
        }
        land.Freeze();
        dc.DrawGeometry(Frozen(new SolidColorBrush(SeaGlassXamlRenderer.ToMediaColor(landCol, 0.85f))), null, land);

        // Sea
        int seaTop = isDark ? SeaGlassColorMath.LerpOklab(primary, unchecked((int)0xFF06223A), 0.55f) : SeaGlassColorMath.LerpOklab(primary, unchecked((int)0xFF1C6E8C), 0.42f);
        int seaLow = isDark ? SeaGlassColorMath.LerpOklab(primary, unchecked((int)0xFF0C3550), 0.40f) : SeaGlassColorMath.LerpOklab(primary, unchecked((int)0xFF3AA6B4), 0.22f);
        dc.DrawRectangle(Frozen(new LinearGradientBrush(SeaGlassXamlRenderer.ToMediaColor(seaTop), SeaGlassXamlRenderer.ToMediaColor(seaLow), 90)), null,
            new Rect(c.X, horizon, c.Width, shore - horizon + 2));

        // Sun glitter path on the water
        SolidColorBrush glitter = Frozen(new SolidColorBrush(Color.FromArgb(isDark ? (byte)0x55 : (byte)0x8C, sunCol.R, sunCol.G, sunCol.B)));
        for (int i = 0; i < 14; i++)
        {
            double t = i / 13.0;
            double y = horizon + 6 + t * (shore - horizon - 30);
            double half = 8 + t * 46;
            double jitter = Math.Sin(i * 2.7) * half * 0.35;
            dc.DrawRoundedRectangle(glitter, null, new Rect(sun.X - half + jitter, y, half * 2 * (0.4 + 0.3 * Math.Abs(Math.Sin(i * 1.3))), 2.2), 1.1, 1.1);
        }

        // Sand
        int sandTop = isDark ? unchecked((int)0xFF4B4136) : SeaGlassColorMath.LerpOklab(unchecked((int)0xFFF1DFBE), secondary, 0.08f);
        int sandLow = isDark ? unchecked((int)0xFF362E26) : SeaGlassColorMath.LerpOklab(unchecked((int)0xFFE2C79C), secondary, 0.06f);
        dc.DrawRectangle(Frozen(new LinearGradientBrush(SeaGlassXamlRenderer.ToMediaColor(sandTop), SeaGlassXamlRenderer.ToMediaColor(sandLow), 90)), null,
            new Rect(c.X, shore, c.Width, c.Bottom - shore));

        // Wet-sand band
        dc.DrawRectangle(Frozen(new LinearGradientBrush(Color.FromArgb(0x38, 0x5A, 0x48, 0x30), Color.FromArgb(0, 0x5A, 0x48, 0x30), 90)), null,
            new Rect(c.X, shore, c.Width, 34));

        // Scattered tumbled sea-glass pebbles (deterministic layout)
        int[] gems = [primary, secondary, tertiary, unchecked((int)0xFFFFFFFF)];
        Pen pebbleRim = Frozen(new Pen(Frozen(new SolidColorBrush(Color.FromArgb(0x60, 0xFF, 0xFF, 0xFF))), 1.0));
        SolidColorBrush pebbleShadow = Frozen(new SolidColorBrush(Color.FromArgb(0x26, 0x3A, 0x2A, 0x18)));
        uint seed = 0x9E3779B9;
        for (int i = 0; i < 26; i++)
        {
            seed = seed * 1664525u + 1013904223u;
            double fx = (seed >> 8) / (double)(1 << 24);
            seed = seed * 1664525u + 1013904223u;
            double fy = (seed >> 8) / (double)(1 << 24);
            seed = seed * 1664525u + 1013904223u;
            double fs = (seed >> 8) / (double)(1 << 24);

            double px = c.X + 18 + fx * (c.Width - 36);
            double py = shore + 42 + fy * Math.Max(10, c.Bottom - shore - 60);
            double pw = 9 + fs * 16;
            double ph = pw * (0.55 + 0.3 * fy);
            Color gc = SeaGlassXamlRenderer.ToMediaColor(gems[i % gems.Length], isDark ? 0.55f : 0.72f);
            dc.DrawEllipse(pebbleShadow, null, new Point(px + 1.5, py + 2.5), pw * 0.5, ph * 0.5);
            dc.DrawEllipse(Frozen(new SolidColorBrush(gc)), pebbleRim, new Point(px, py), pw * 0.5, ph * 0.5);
        }
    }

    private static DrawingGroup BuildWaveLayer(Rect c, bool isDark)
    {
        DrawingGroup g = new();
        using (DrawingContext dc = g.Open())
        {
            double horizon = c.Y + c.Height * 0.44;
            double shore = c.Y + c.Height * 0.72;
            for (int i = 0; i < 7; i++)
            {
                double t = (i + 1) / 8.0;
                double y = horizon + t * (shore - horizon - 10);
                double amp = 2 + t * 5;
                double period = 90 + t * 120;
                byte a = (byte)((isDark ? 0x20 : 0x3A) + t * 0x30);
                Pen wavePen = Frozen(new Pen(Frozen(new SolidColorBrush(Color.FromArgb(a, 0xFF, 0xFF, 0xFF))), 1.0 + t * 1.2)
                {
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round
                });
                StreamGeometry wave = new();
                using (StreamGeometryContext s = wave.Open())
                {
                    double x0 = c.X - 40;
                    s.BeginFigure(new Point(x0, y), false, false);
                    for (double x = x0; x < c.Right + 40; x += period)
                    {
                        s.BezierTo(new Point(x + period * 0.25, y - amp), new Point(x + period * 0.75, y + amp), new Point(x + period, y), true, true);
                    }
                }
                wave.Freeze();
                dc.DrawGeometry(null, wavePen, wave);
            }
        }
        g.Freeze();
        return g;
    }

    private static DrawingGroup BuildFoamLayer(Rect c, bool isDark)
    {
        DrawingGroup g = new();
        using (DrawingContext dc = g.Open())
        {
            double shore = c.Y + c.Height * 0.72;
            StreamGeometry foam = new();
            using (StreamGeometryContext s = foam.Open())
            {
                double x0 = c.X - 40;
                s.BeginFigure(new Point(x0, shore - 6), true, true);
                for (double x = x0; x < c.Right + 40; x += 140)
                {
                    s.BezierTo(new Point(x + 35, shore - 14), new Point(x + 95, shore + 8), new Point(x + 140, shore - 4), true, true);
                }
                s.LineTo(new Point(c.Right + 40, shore + 10), true, true);
                for (double x = c.Right + 40; x > x0; x -= 140)
                {
                    s.BezierTo(new Point(x - 40, shore + 18), new Point(x - 100, shore + 4), new Point(x - 140, shore + 12), true, true);
                }
            }
            foam.Freeze();
            dc.DrawGeometry(Frozen(new SolidColorBrush(Color.FromArgb(isDark ? (byte)0x50 : (byte)0xB0, 0xFF, 0xFF, 0xFF))), null, foam);
        }
        g.Freeze();
        return g;
    }

    private static void DrawSoftPool(DrawingContext dc, Point center, double radius, int argb, double alpha)
    {
        Color col = SeaGlassXamlRenderer.ToMediaColor(argb);
        RadialGradientBrush b = new(Color.FromArgb((byte)Math.Clamp(alpha * 255, 0, 255), col.R, col.G, col.B), Color.FromArgb(0, col.R, col.G, col.B));
        b.Freeze();
        dc.DrawEllipse(b, null, center, radius, radius);
    }

    internal static Geometry TopLeftRoundedRect(Rect r, double radius)
    {
        StreamGeometry g = new();
        using (StreamGeometryContext s = g.Open())
        {
            s.BeginFigure(new Point(r.X, r.Y + radius), true, true);
            s.ArcTo(new Point(r.X + radius, r.Y), new System.Windows.Size(radius, radius), 0, false, SweepDirection.Clockwise, true, true);
            s.LineTo(new Point(r.Right, r.Y), true, true);
            s.LineTo(new Point(r.Right, r.Bottom), true, true);
            s.LineTo(new Point(r.X, r.Bottom), true, true);
        }
        g.Freeze();
        return g;
    }

    private static T Frozen<T>(T f) where T : Freezable
    {
        f.Freeze();
        return f;
    }
}

/// <summary>
/// Lightweight retained-mode painter for flat desktop chrome (sidebar, inspector rows, status bar,
/// caption buttons). Re-renders only when its <see cref="Signature"/> changes.
/// </summary>
public sealed class StudioPaintHost : FrameworkElement
{
    private int _lastSignature = int.MinValue;

    public Action<DrawingContext, double, double>? Painter { get; set; }
    public Func<int>? Signature { get; set; }
    public bool IsHovered { get; private set; }
    public Point? PointerPos { get; private set; }

    public StudioPaintHost()
    {
        MouseEnter += (_, e) => { IsHovered = true; PointerPos = e.GetPosition(this); InvalidateVisual(); };
        MouseMove += (_, e) => { PointerPos = e.GetPosition(this); InvalidateVisual(); };
        MouseLeave += (_, _) => { IsHovered = false; PointerPos = null; InvalidateVisual(); };
    }

    public void Tick()
    {
        if (Signature is null) return;
        int sig = Signature();
        if (sig != _lastSignature)
        {
            InvalidateVisual();
        }
    }

    protected override void OnRender(DrawingContext dc)
    {
        _lastSignature = Signature?.Invoke() ?? 0;
        dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, ActualWidth, ActualHeight));
        Painter?.Invoke(dc, ActualWidth, ActualHeight);
    }
}

/// <summary>
/// Cached desktop typography (Segoe UI Variable / Cascadia Mono) with left, center, and right alignment.
/// </summary>
internal static class StudioText
{
    private static readonly Typeface Regular = new(new FontFamily("Segoe UI Variable Text, Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
    private static readonly Typeface Semibold = new(new FontFamily("Segoe UI Variable Text, Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
    private static readonly Typeface Display = new(new FontFamily("Segoe UI Variable Display, Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
    private static readonly Typeface Mono = new(new FontFamily("Cascadia Mono, Consolas"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
    private static readonly Dictionary<(string, double, int, int), FormattedText> Cache = new();

    public enum Style { Regular, Semibold, Display, Mono }

    public static FormattedText Get(string text, double size, int argb, Style style = Style.Regular, float alpha = 1f)
    {
        int colorKey = (argb & 0x00FFFFFF) | ((int)(Math.Clamp(alpha, 0f, 1f) * ((argb >>> 24) & 0xFF)) << 24);
        var key = (text, size, colorKey, (int)style);
        if (Cache.TryGetValue(key, out FormattedText? ft)) return ft;
        if (Cache.Count > 4000) Cache.Clear();

        SolidColorBrush brush = new(Color.FromArgb((byte)((colorKey >>> 24) & 0xFF), (byte)((argb >> 16) & 0xFF), (byte)((argb >> 8) & 0xFF), (byte)(argb & 0xFF)));
        brush.Freeze();
        Typeface tf = style switch
        {
            Style.Semibold => Semibold,
            Style.Display => Display,
            Style.Mono => Mono,
            _ => Regular
        };
        ft = new FormattedText(text, System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, size, brush, 1.25);
        Cache[key] = ft;
        return ft;
    }

    public static double Draw(DrawingContext dc, string text, double x, double y, double size, int argb, Style style = Style.Regular, float alpha = 1f)
    {
        FormattedText ft = Get(text, size, argb, style, alpha);
        dc.DrawText(ft, new Point(x, y));
        return ft.Width;
    }

    public static void DrawRight(DrawingContext dc, string text, double right, double y, double size, int argb, Style style = Style.Regular, float alpha = 1f)
    {
        FormattedText ft = Get(text, size, argb, style, alpha);
        dc.DrawText(ft, new Point(right - ft.Width, y));
    }

    public static void DrawCentered(DrawingContext dc, string text, Point center, double size, int argb, Style style = Style.Regular, float alpha = 1f)
    {
        FormattedText ft = Get(text, size, argb, style, alpha);
        dc.DrawText(ft, new Point(center.X - ft.Width * 0.5, center.Y - ft.Height * 0.5));
    }

    /// <summary>Vertically centers single-line text inside a row of the given height (cap-height centered, baseline aligned across fonts).</summary>
    public static void DrawInRow(DrawingContext dc, string text, double x, double rowY, double rowH, double size, int argb, Style style = Style.Regular, float alpha = 1f)
    {
        FormattedText ft = Get(text, size, argb, style, alpha);
        dc.DrawText(ft, new Point(x, RowBaseline(rowY, rowH, size) - ft.Baseline));
    }

    public static void DrawRightInRow(DrawingContext dc, string text, double right, double rowY, double rowH, double size, int argb, Style style = Style.Regular, float alpha = 1f)
    {
        FormattedText ft = Get(text, size, argb, style, alpha);
        dc.DrawText(ft, new Point(right - ft.Width, RowBaseline(rowY, rowH, size) - ft.Baseline));
    }

    /// <summary>Draws text so its baseline sits exactly at <paramref name="baselineY"/>.</summary>
    public static void DrawAtBaseline(DrawingContext dc, string text, double x, double baselineY, double size, int argb, Style style = Style.Regular, float alpha = 1f, bool rightAligned = false)
    {
        FormattedText ft = Get(text, size, argb, style, alpha);
        dc.DrawText(ft, new Point(rightAligned ? x - ft.Width : x, baselineY - ft.Baseline));
    }

    private static double RowBaseline(double rowY, double rowH, double size) => Math.Round(rowY + rowH * 0.5 + size * 0.36);
}

/// <summary>
/// Hardware-accelerated SeaGlass visual control with spring-animated desktop hover &amp; press physics,
/// cursor-tracked rim illumination, and smart dirty-state invalidation.
/// </summary>
public sealed class SeaGlassVisualControl : Border
{
    private Point? _localPointerPos;
    private bool _isHovered;
    private float _hoverProgress;
    private float _hoverVel;
    private int _lastRenderedSignature;
    private bool _originDirty = true;

    public SeaGlassSurface SurfaceModel { get; set; }
    public SeaGlassStageBackdropVisuals? StageBackdrop { get; set; }
    public UIElement? StageCoordinateRoot { get; set; }
    public Func<float, SeaGlassSurfaceFrameState>? CustomStepEvaluator { get; set; }
    public Func<IReadOnlyList<SeaGlassSdfEngine.SdfNode>?>? ActiveNodesProvider { get; set; }
    public Func<int>? CustomStateSignature { get; set; }
    public float SmoothRadiusDp { get; set; } = 28f;
    public bool InteractiveHoverLift { get; set; }
    public float HoverProgress => _hoverProgress;
    public Point? LocalPointerPos => _localPointerPos;
    public Action<DrawingContext, SeaGlassSurfaceFrameState, double, double>? CustomOverlayPainter { get; set; }
    public SeaGlassSurfaceFrameState? LastFrameState { get; private set; }

    public SeaGlassVisualControl(
        SeaGlassSurface surfaceModel,
        SeaGlassStageBackdropVisuals? stageBackdrop = null,
        UIElement? stageCoordinateRoot = null,
        Func<float, SeaGlassSurfaceFrameState>? customStepEvaluator = null,
        Action<DrawingContext, SeaGlassSurfaceFrameState, double, double>? customOverlayPainter = null,
        Func<IReadOnlyList<SeaGlassSdfEngine.SdfNode>?>? activeNodesProvider = null,
        float smoothRadiusDp = 28f,
        Func<int>? customStateSignature = null,
        bool interactiveHoverLift = false)
    {
        SurfaceModel = surfaceModel;
        StageBackdrop = stageBackdrop;
        StageCoordinateRoot = stageCoordinateRoot;
        CustomStepEvaluator = customStepEvaluator;
        CustomOverlayPainter = customOverlayPainter;
        ActiveNodesProvider = activeNodesProvider;
        SmoothRadiusDp = smoothRadiusDp;
        CustomStateSignature = customStateSignature;
        InteractiveHoverLift = interactiveHoverLift;
        Background = Brushes.Transparent;
        Cursor = surfaceModel.OnClick is not null || interactiveHoverLift ? Cursors.Hand : Cursors.Arrow;

        MouseEnter += (_, e) =>
        {
            _isHovered = true;
            _localPointerPos = e.GetPosition(this);
        };
        MouseDown += (_, e) =>
        {
            Point pt = e.GetPosition(this);
            _localPointerPos = pt;
            SurfaceModel.OnPointerStateChanged(new Vector2((float)pt.X, (float)pt.Y), true);
            _lastRenderedSignature = 0;
            InvalidateVisual();
        };
        MouseMove += (_, e) =>
        {
            Point pt = e.GetPosition(this);
            bool movedSignificantly = _localPointerPos is null ||
                                      Math.Abs(pt.X - _localPointerPos.Value.X) + Math.Abs(pt.Y - _localPointerPos.Value.Y) >= 4.0;
            _localPointerPos = pt;
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                SurfaceModel.OnPointerStateChanged(new Vector2((float)pt.X, (float)pt.Y), true);
                _lastRenderedSignature = 0;
                InvalidateVisual();
            }
            else if (SeaGlassXamlRenderer.PointerTiltAndGlowEnabled && movedSignificantly && _isHovered)
            {
                _lastRenderedSignature = 0;
                InvalidateVisual();
            }
        };
        MouseUp += (_, e) =>
        {
            Point pt = e.GetPosition(this);
            _localPointerPos = pt;
            SurfaceModel.OnPointerStateChanged(new Vector2((float)pt.X, (float)pt.Y), false);
            _lastRenderedSignature = 0;
            InvalidateVisual();
        };
        MouseLeave += (_, _) =>
        {
            _isHovered = false;
            _localPointerPos = null;
            if (SurfaceModel.IsPointerDown)
            {
                SurfaceModel.OnPointerStateChanged(null, false);
            }
            _lastRenderedSignature = 0;
            InvalidateVisual();
        };
    }

    public void MarkOriginDirty() => _originDirty = true;

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        _originDirty = true;
        _lastRenderedSignature = 0;
    }

    private void EnsureOriginUpToDate()
    {
        if (!_originDirty || StageCoordinateRoot is null)
        {
            return;
        }

        try
        {
            if (StageBackdrop is not null && StageCoordinateRoot.RenderSize.Width > 1)
            {
                StageBackdrop.StageWidth = StageCoordinateRoot.RenderSize.Width;
                StageBackdrop.StageHeight = StageCoordinateRoot.RenderSize.Height;
            }
            Point ptInStage = TransformToVisual(StageCoordinateRoot).Transform(new Point(0, 0));
            SurfaceModel.OriginInRoot = new Vector2((float)ptInStage.X, (float)ptInStage.Y);
            _originDirty = false;
        }
        catch
        {
            // Ignore if visual tree is mid-rebuild
        }
    }

    private int ComputeSignature(SeaGlassSurfaceFrameState frame)
    {
        SeaGlassMaterial m = frame.EffectiveMaterial;
        HashCode hc = new();
        hc.Add(m.TintColorArgb);
        hc.Add((int)MathF.Round(m.TintDensity * 200f));
        hc.Add((int)MathF.Round(m.Coarseness * 200f));
        hc.Add((int)MathF.Round(m.Ior * 200f));
        hc.Add((int)MathF.Round(m.ChromaticDispersion * 200f));
        hc.Add((int)MathF.Round(m.RimChalkiness * 200f));
        hc.Add((int)MathF.Round(m.Transparency * 200f));
        hc.Add((int)MathF.Round(frame.SolvedCornerRadiiPx.TopLeftPx * 2f));
        hc.Add((int)MathF.Round(frame.ScaleX * 500f));
        hc.Add((int)MathF.Round(frame.ScaleY * 500f));
        hc.Add(InteractiveHoverLift ? (int)MathF.Round(_hoverProgress * 100f) : 0);
        hc.Add((int)Math.Round(ActualWidth));
        hc.Add((int)Math.Round(ActualHeight));
        hc.Add((int)MathF.Round(SurfaceModel.OriginInRoot.X));
        hc.Add((int)MathF.Round(SurfaceModel.OriginInRoot.Y));
        hc.Add(SurfaceModel.IsPointerDown);
        hc.Add(MainWindow.GlobalOpticsRevision);
        if (CustomStateSignature is not null)
        {
            hc.Add(CustomStateSignature());
        }
        return hc.ToHashCode();
    }

    public void StepPhysics(float dtSeconds)
    {
        if (ActualWidth > 1 && ActualHeight > 1)
        {
            SurfaceModel.SizePx = new Vector2((float)ActualWidth, (float)ActualHeight);
        }

        EnsureOriginUpToDate();

        SeaGlassSpringSpec hoverSpring = new(DampingRatio: 0.85f, Stiffness: 460f);
        hoverSpring.Step(ref _hoverProgress, ref _hoverVel, _isHovered ? 1f : 0f, dtSeconds);

        LastFrameState = CustomStepEvaluator is not null
            ? CustomStepEvaluator(dtSeconds)
            : SurfaceModel.EvaluateFrame(dtSeconds, SeaGlassTheme.Current);

        int sig = ComputeSignature(LastFrameState);
        if (sig != _lastRenderedSignature)
        {
            InvalidateVisual();
        }
    }

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth > 1 ? ActualWidth : SurfaceModel.SizePx.X;
        double h = ActualHeight > 1 ? ActualHeight : SurfaceModel.SizePx.Y;

        if (w > 1 && h > 1)
        {
            SurfaceModel.SizePx = new Vector2((float)w, (float)h);
        }

        EnsureOriginUpToDate();

        LastFrameState ??= CustomStepEvaluator is not null
            ? CustomStepEvaluator(1f / 60f)
            : SurfaceModel.EvaluateFrame(1f / 60f, SeaGlassTheme.Current);

        _lastRenderedSignature = ComputeSignature(LastFrameState);

        bool pushHoverLift = InteractiveHoverLift && (_hoverProgress > 0.005f || SurfaceModel.IsPointerDown);
        if (pushHoverLift)
        {
            double liftScale = SurfaceModel.IsPointerDown
                ? 0.976
                : 1.0 + _hoverProgress * 0.012;
            dc.PushTransform(new ScaleTransform(liftScale, liftScale, w * 0.5, h * 0.5));
        }

        Point originInStage = new(SurfaceModel.OriginInRoot.X, SurfaceModel.OriginInRoot.Y);
        IReadOnlyList<SeaGlassSdfEngine.SdfNode>? nodes = ActiveNodesProvider?.Invoke();
        int sampleBackdrop = SeaGlassTheme.Current.Palette.IsDark
            ? unchecked((int)0xFF0C1522)
            : unchecked((int)0xFFEEF4F8);

        SeaGlassXamlRenderer.DrawSeaGlassSurface(
            dc: dc,
            frameState: LastFrameState,
            width: w,
            height: h,
            backdropSampleArgb: sampleBackdrop,
            localPointerPos: _localPointerPos,
            isPointerDown: SurfaceModel.IsPointerDown,
            stageBackdrop: StageBackdrop,
            elementOriginInStage: originInStage,
            activeNodes: nodes,
            smoothRadiusDp: SmoothRadiusDp,
            contentRole: SurfaceModel.ContentRole,
            hoverProgress: _hoverProgress);

        CustomOverlayPainter?.Invoke(dc, LastFrameState, w, h);

        if (pushHoverLift)
        {
            dc.Pop();
        }
    }
}

/// <summary>
/// SeaGlass Studio — a real desktop application window for the SeaGlass material.
/// Mica title bar and sidebar, an edge-to-edge content layer holding a document canvas and a flat
/// inspector, and a status bar. Sea glass is reserved for surfaces that float above content
/// (toolbars, flyouts, HUDs, droplets, and controls) instead of wrapping every section in a card.
/// </summary>
public sealed class MainWindow : Window
{
    public static int GlobalOpticsRevision { get; private set; } = 1;

    private readonly SeaGlassStageState _stageState = new();
    private readonly SeaGlassStageBackdropVisuals _stageBackdropVisuals = new();
    private readonly StudioWindowBackdrop _sharpBackdropCanvas = new();
    private readonly StudioWindowBackdrop _frostedBackdropCanvas = new();
    private readonly Border _frostedBackdropHost;
    private readonly Grid _stageRootGrid = new();
    private readonly List<SeaGlassVisualControl> _activeControls = new();
    private readonly List<StudioPaintHost> _paintHosts = new();
    private readonly List<Action> _materialSyncCallbacks = new();
    private readonly ContentControl _mainContentHost = new();
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private double _lastTimeSeconds;
    private double _lastBackdropUpdateSeconds;
    private double _autoCycleTimer;
    private double _fps = 60;

    // Navigation state: 0 = Material, 1 = Liquid SDF, 2 = Controls, 3 = Shader Code
    private int _page;
    private int _codeScrollLine;
    private bool _autoAnimateStates;

    // Lead-lag vertical springs for the sidebar selection indicator
    private float _sidebarLeadSlot;
    private float _sidebarLeadVel;
    private float _sidebarTrailSlot;
    private float _sidebarTrailVel;
    private float _presetSlot;
    private float _presetSlotVel;
    private int ActivePresetIndex => _transparency > 0.75f ? 1 : _transparency < 0.55f ? 2 : 0;

    private float _transparency = 0.64f;
    private float _coarseness = 0.42f;
    private float _frostRadiusDp = 22f;
    private float _tintDensity = 0.68f;
    private float _thicknessDp = 16f;
    private float _ior = 1.54f;
    private float _chromaticDispersion = 0.58f;
    private float _rimChalkiness = 0.78f;

    // Live interactive component models
    private int _heroFlowState;
    private float _playOrbGapDp = -2f;
    private float _playOrbGapVel;
    private bool _coalescedPreset = true;
    private const float MergedDropletSepDp = 50f;
    private const float SplitDropletSepDp = 150f;
    private static readonly SeaGlassSpringSpec DropletSpring = new(DampingRatio: 0.68f, Stiffness: 420f);
    private float _dropletSepDp = MergedDropletSepDp;
    private float _dropletSepVel;
    private float _smoothBridgeDp = 48f;
    private Vector2 _dragOffsetPx = Vector2.Zero;

    private readonly (string Title, string Artist)[] _miniTracks =
    [
        ("A Good Song Never Dies", "Saint Motel"),
        ("Midnight City", "M83"),
        ("Tidal Glass", "Tycho"),
        ("Oceanic Bloom", "Bonobo")
    ];
    private int _miniTrackIndex;
    private int _conceptPromptIndex;
    private bool _porcelainDaylight;
    private static readonly string[] ConceptPrompts = ["Ask Gemini", "Tumbled Sea Glass Optics", "Sand-Frosted Oklab Rind"];
    private ShaderDialect _selectedDialect = ShaderDialect.HlslSm50;

    // Persistent component instances
    private readonly SeaGlassPebbleOrb _pebbleOrb;
    private readonly SeaGlassPillNavBar _pillNavBar;
    private readonly SeaGlassPromptBar _promptBar;
    private readonly SeaGlassChoreographedShapeOrb _shapeOrb;
    private readonly SeaGlassBuddingCluster _buddingCluster;
    private readonly SeaGlassMorphingButton _morphBtn0;
    private readonly SeaGlassMorphingButton _morphBtn1;
    private readonly SeaGlassMorphingButton _morphBtn2;
    private readonly SeaGlassTwoElementMorph _twoElementMorph;
    private readonly SeaGlassMorphingContextMenu _contextMenuMorph;
    private readonly SeaGlassCascadedModelMorph _cascadedModelMorph;
    private readonly SeaGlassContextRingMorph _contextRingMorph;
    private readonly SeaGlassSegmentedButtonRow _segmentedRow;
    private readonly SeaGlassFloatingToolbar _floatingToolbar;
    private readonly SeaGlassExtendedFab _extendedFab;
    private readonly SeaGlassSwitch _switchA;
    private readonly SeaGlassSwitch _switchB;
    private readonly SeaGlassCheckbox _checkA;
    private readonly SeaGlassCheckbox _checkB;
    private readonly SeaGlassRadioButton _radioA;
    private readonly SeaGlassRadioButton _radioB;
    private readonly SeaGlassCircularProgressIndicator _circularProgress;
    private readonly SeaGlassLinearProgressIndicator _linearProgress;
    private readonly SeaGlassMiniPlayerBar _miniPlayerBar;

    public MainWindow()
    {
        Title = "SeaGlass Studio";
        Width = 1440;
        Height = 920;
        MinWidth = 1120;
        MinHeight = 760;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new SolidColorBrush(Color.FromRgb(0xEE, 0xF1, 0xF4));

        // Integrated title bar: the app draws its own Mica caption row + Windows 11 caption buttons.
        WindowChrome.SetWindowChrome(this, new WindowChrome
        {
            CaptionHeight = StudioLayout.TitleBarHeight,
            ResizeBorderThickness = new Thickness(6),
            GlassFrameThickness = new Thickness(0, 0, 0, 1),
            CornerRadius = new CornerRadius(0),
            UseAeroCaptionButtons = false
        });
        StateChanged += (_, _) =>
        {
            double inset = WindowState == WindowState.Maximized
                ? SystemParameters.WindowResizeBorderThickness.Left + 4
                : 0;
            _stageRootGrid.Margin = new Thickness(inset);
            MarkAllControlsOriginDirty();
        };

        // Hardware GPU BitmapCache Pyramid: blur once at 0.5x resolution
        _sharpBackdropCanvas.CacheMode = new BitmapCache(1.0);
        _frostedBackdropCanvas.Effect = new BlurEffect
        {
            Radius = 38.0,
            KernelType = KernelType.Gaussian,
            RenderingBias = RenderingBias.Performance
        };
        _frostedBackdropHost = new Border
        {
            Child = _frostedBackdropCanvas,
            CacheMode = new BitmapCache(0.5),
            // Clip the blur's padded bounds so the frosted VisualBrush registers 1:1 with the sharp backdrop
            ClipToBounds = true,
            IsHitTestVisible = false
        };
        _sharpBackdropCanvas.IsHitTestVisible = false;

        _stageBackdropVisuals.SharpBackdropVisual = _sharpBackdropCanvas;
        _stageBackdropVisuals.FrostedBackdropVisual = _frostedBackdropHost;
        SeaGlassTheme.Current.DpiScale = 1.0f;
        _stageState.BindBackdropTarget("StudioWindowBackdrop", Vector2.Zero, new Vector2(1440f, 920f));

        SizeChanged += (_, _) => MarkAllControlsOriginDirty();

        // Initialize Concept Reference Triad (Coral-Peach "G" Pebble + Orchid Pill Nav Bar + Periwinkle Prompt Bar)
        _pebbleOrb = new SeaGlassPebbleOrb(
            onClick: () =>
            {
                _porcelainDaylight = !_porcelainDaylight;
                RebuildUI();
            },
            stageState: _stageState,
            sizeDp: 68f,
            glyph: SeaGlassGlyph.BrandG);

        _pillNavBar = new SeaGlassPillNavBar(
            items:
            [
                (SeaGlassGlyph.PhotoMountain, "Photos"),
                (null, "Collections"),
                (null, "Create")
            ],
            selectedIndex: 0,
            onSelect: _ => { },
            stageState: _stageState);

        _promptBar = new SeaGlassPromptBar(
            promptText: ConceptPrompts[0],
            onPromptClick: () =>
            {
                _conceptPromptIndex = (_conceptPromptIndex + 1) % ConceptPrompts.Length;
                _promptBar!.PromptText = ConceptPrompts[_conceptPromptIndex];
            },
            onPlusClick: () =>
            {
                _conceptPromptIndex = (_conceptPromptIndex + 1) % ConceptPrompts.Length;
                _promptBar!.PromptText = ConceptPrompts[_conceptPromptIndex];
            },
            onMicClick: () => _promptBar!.ToggleVoiceActive(),
            onVoiceWaveClick: () => _promptBar!.ToggleVoiceActive(),
            isVoiceActive: false,
            stageState: _stageState);

        // Initialize stateful components
        _shapeOrb = new SeaGlassChoreographedShapeOrb(_stageState);
        _buddingCluster = new SeaGlassBuddingCluster(
            expanded: true,
            items: [("♪", "Surf"), ("☀", "68° UV"), ("◈", "Lens")],
            onItemClick: _ => _coalescedPreset = !_coalescedPreset,
            stageState: _stageState);

        _morphBtn0 = new SeaGlassMorphingButton(
            active: false,
            onClick: () => _morphBtn0!.ToggleActive(),
            stageState: _stageState,
            restingShape: SeaGlassShapeKind.Squircle,
            activeShape: SeaGlassShapeKind.Circle);
        _morphBtn1 = new SeaGlassMorphingButton(
            active: true,
            onClick: () => _morphBtn1!.ToggleActive(),
            stageState: _stageState,
            restingShape: SeaGlassShapeKind.Squircle,
            activeShape: SeaGlassShapeKind.Cookie6);
        _morphBtn2 = new SeaGlassMorphingButton(
            active: false,
            onClick: () => _morphBtn2!.ToggleActive(),
            stageState: _stageState,
            restingShape: SeaGlassShapeKind.Squircle,
            activeShape: SeaGlassShapeKind.Gem);

        _twoElementMorph = new SeaGlassTwoElementMorph(
            expanded: false,
            stageState: _stageState,
            collapsedSizePx: new Vector2(360f, 64f),
            expandedSizePx: new Vector2(420f, 128f),
            morphMode: SeaGlassMorphMode.LiquidCoalesce,
            anchorAlignment: SeaGlassAnchorAlignment.Center,
            onToggle: () => _twoElementMorph!.ToggleExpanded());

        _contextMenuMorph = new SeaGlassMorphingContextMenu(expanded: false, stageState: _stageState);
        _contextMenuMorph.OnClick = () => _contextMenuMorph.ToggleExpanded();

        _cascadedModelMorph = new SeaGlassCascadedModelMorph(stage: 1, stageState: _stageState);
        _cascadedModelMorph.OnClick = () => _cascadedModelMorph.SetStage((_cascadedModelMorph.Stage + 1) % 3);

        _contextRingMorph = new SeaGlassContextRingMorph(expanded: false, progress: 0.35f, stageState: _stageState);
        _contextRingMorph.OnClick = () => _contextRingMorph.ToggleExpanded();

        _segmentedRow = new SeaGlassSegmentedButtonRow(
            options: ["Controls", "Surfaces", "Overlays"],
            selectedIndex: 0,
            onSelect: idx =>
            {
                float p = idx switch { 0 => 0.74f, 1 => 0.48f, _ => 0.92f };
                _linearProgress!.Progress = p;
                _circularProgress!.Progress = p;
            },
            stageState: _stageState);
        _segmentedRow.OnClick = () => _segmentedRow.SelectOption((_segmentedRow.SelectedIndex + 1) % 3);

        _floatingToolbar = new SeaGlassFloatingToolbar(
            actions:
            [
                (SeaGlassGlyph.Play, "Stream"),
                (SeaGlassGlyph.Heart, "Favorite"),
                (SeaGlassGlyph.Share, "Share"),
                (SeaGlassGlyph.Bookmark, "Save")
            ],
            selectedActionIndex: 0,
            onActionSelect: _ => { },
            fabGlyph: SeaGlassGlyph.Plus,
            fabDetached: false,
            onFabClick: () => _floatingToolbar!.FabDetached = !_floatingToolbar.FabDetached,
            stageState: _stageState);
        _floatingToolbar.OnClick = () =>
        {
            _floatingToolbar.SelectedActionIndex = (_floatingToolbar.SelectedActionIndex + 1) % 4;
            _floatingToolbar.FabDetached = !_floatingToolbar.FabDetached;
        };

        _extendedFab = new SeaGlassExtendedFab(
            text: "Create",
            glyph: SeaGlassGlyph.Sparkles,
            expanded: true,
            onClick: () => _extendedFab!.SetExpanded(!_extendedFab.Expanded),
            stageState: _stageState);

        _switchA = new SeaGlassSwitch(true, _ => { }, _stageState);
        _switchB = new SeaGlassSwitch(false, _ => { }, _stageState);
        _checkA = new SeaGlassCheckbox(true, _ => { }, _stageState);
        _checkB = new SeaGlassCheckbox(false, _ => { }, _stageState);
        _radioA = new SeaGlassRadioButton(true, () => { _radioA!.SetSelected(true); _radioB!.SetSelected(false); }, _stageState);
        _radioB = new SeaGlassRadioButton(false, () => { _radioA!.SetSelected(false); _radioB!.SetSelected(true); }, _stageState);
        _circularProgress = new SeaGlassCircularProgressIndicator(0.74f, _stageState, sizeDp: 52f);
        _linearProgress = new SeaGlassLinearProgressIndicator(0.74f, _stageState);

        _miniPlayerBar = new SeaGlassMiniPlayerBar(
            title: _miniTracks[0].Title,
            subtitle: _miniTracks[0].Artist,
            isPlaying: true,
            onTogglePlay: () => _miniPlayerBar!.IsPlaying = !_miniPlayerBar.IsPlaying,
            onNextTrack: () =>
            {
                _miniTrackIndex = (_miniTrackIndex + 1) % _miniTracks.Length;
                _miniPlayerBar!.Title = _miniTracks[_miniTrackIndex].Title;
                _miniPlayerBar.Subtitle = _miniTracks[_miniTrackIndex].Artist;
            },
            stageState: _stageState);

        BuildStageRoot();
        Content = _stageRootGrid;
        SelectPalette(SeaGlassPalette.SeafoamMint);

        CompositionTarget.Rendering += OnFrameTick;
    }

    private int ActiveSidebarSlot => _page;

    private void SelectSidebarSlot(int slot)
    {
        int next = Math.Clamp(slot, 0, PageTitles.Length - 1);
        if (next == _page) return;
        _page = next;
        RebuildUI();
    }

    private void MarkAllControlsOriginDirty()
    {
        foreach (SeaGlassVisualControl ctrl in _activeControls)
        {
            ctrl.MarkOriginDirty();
        }
    }

    public void SelectTabForPreview(int pageIndex)
    {
        _page = Math.Clamp(pageIndex, 0, PageTitles.Length - 1);
        _sidebarLeadSlot = _sidebarTrailSlot = _page;
        RebuildUI();
    }

    private void OnFrameTick(object? sender, EventArgs e)
    {
        double now = _clock.Elapsed.TotalSeconds;
        double rawDt = now - _lastTimeSeconds;
        float dt = (float)Math.Clamp(rawDt, 0.004, 0.05);
        _lastTimeSeconds = now;
        if (rawDt > 0.0005)
        {
            _fps = _fps * 0.94 + Math.Min(240.0, 1.0 / rawDt) * 0.06;
        }

        // Keep the GPU BlurEffect BitmapCache 100% resident in VRAM; only animate sharp shoreline waves when auto-animating
        if (_autoAnimateStates && _sharpBackdropCanvas.CanvasMode == StudioCanvasMode.Artwork && now - _lastBackdropUpdateSeconds >= 1.0 / 15.0)
        {
            _lastBackdropUpdateSeconds = now;
            double phase = (now * (2.0 * Math.PI / 14.0)) % (2.0 * Math.PI);
            _sharpBackdropCanvas.Phase = phase;
            _sharpBackdropCanvas.InvalidateVisual();
        }

        if (SeaGlassXamlRenderer.PointerTiltAndGlowEnabled)
        {
            SeaGlassTheme.Current.TiltState.UpdateFrame(dt);
        }

        // Lead-lag sidebar selection indicator springs
        float targetSidebarSlot = ActiveSidebarSlot;
        SeaGlassSpringSpec leadSpring = new(DampingRatio: 0.68f, Stiffness: 540f);
        SeaGlassSpringSpec trailSpring = new(DampingRatio: 0.72f, Stiffness: 310f);
        leadSpring.Step(ref _sidebarLeadSlot, ref _sidebarLeadVel, targetSidebarSlot, dt);
        trailSpring.Step(ref _sidebarTrailSlot, ref _sidebarTrailVel, targetSidebarSlot, dt);
        new SeaGlassSpringSpec(DampingRatio: 0.74f, Stiffness: 480f).Step(ref _presetSlot, ref _presetSlotVel, ActivePresetIndex, dt);

        // Step spring-animated gap, droplet separation & linear progress
        SeaGlassSpringSpec spring54 = new(DampingRatio: 0.58f, Stiffness: 320f);
        float targetGap = _heroFlowState switch { 0 => -2f, 1 => 9f, _ => 22f };
        spring54.Step(ref _playOrbGapDp, ref _playOrbGapVel, targetGap, dt);
        // Merged: centers 50dp apart (radii 44 + 40dp) so the smin union is a full liquid lens, never a pinched bowtie.
        // Split: 150dp leaves a 66dp gap (> 48dp bridge k) so droplets fully separate. Spatial spring per AGENTS.md rule 5.
        DropletSpring.Step(ref _dropletSepDp, ref _dropletSepVel, _coalescedPreset ? MergedDropletSepDp : SplitDropletSepDp, dt);
        _linearProgress.StepAndEvaluate(dt, SeaGlassTheme.Current);

        if (_autoAnimateStates)
        {
            _autoCycleTimer += dt;
            if (_autoCycleTimer >= 2.2)
            {
                _autoCycleTimer = 0;
                _heroFlowState = (_heroFlowState + 1) % 3;
                _coalescedPreset = !_coalescedPreset;
                _pillNavBar.SelectTab((_pillNavBar.SelectedIndex + 1) % _pillNavBar.Items.Count);
                _promptBar.ToggleVoiceActive();
                _shapeOrb.AdvanceTo(_shapeOrb.TargetStageIndex + 1);
                _morphBtn0.ToggleActive();
                _morphBtn1.ToggleActive();
                _morphBtn2.ToggleActive();
                _buddingCluster.ToggleExpanded();
                _switchA.Toggle();
                _checkA.Toggle();
                _segmentedRow.SelectOption((_segmentedRow.SelectedIndex + 1) % 3);
                _floatingToolbar.FabDetached = !_floatingToolbar.FabDetached;
                _floatingToolbar.SelectedActionIndex = (_floatingToolbar.SelectedActionIndex + 1) % 4;
                _extendedFab.SetExpanded(!_extendedFab.Expanded);
            }
        }

        foreach (SeaGlassVisualControl ctrl in _activeControls)
        {
            ctrl.StepPhysics(dt);
        }
        foreach (StudioPaintHost host in _paintHosts)
        {
            host.Tick();
        }
    }

    private static int IndexOfPalette(SeaGlassPalette palette)
    {
        for (int i = 0; i < SeaGlassPalette.Entries.Count; i++)
        {
            if (SeaGlassPalette.Entries[i].Id == palette.Id) return i;
        }
        return 0;
    }

    private SeaGlassMaterial CustomPrimaryMaterial =>
        SeaGlassTheme.Current.Palette.PrimaryGlass with
        {
            Transparency = _transparency,
            Coarseness = _coarseness,
            FrostRadiusDp = _frostRadiusDp,
            TintDensity = _tintDensity,
            ThicknessDp = _thicknessDp,
            Ior = _ior,
            ChromaticDispersion = _chromaticDispersion,
            RimChalkiness = _rimChalkiness
        };

    /// <summary>
    /// Tumbled SeaGlass Surface Material: preserves full reverse-rim frosted rind,
    /// volumetric Beer-Lambert core tint, and powdery sand-stipple coarseness.
    /// </summary>
    private SeaGlassMaterial DesktopCardMaterial =>
        SeaGlassTheme.Current.Palette.SurfaceGlass with
        {
            CornerRadiusDp = 24f,
            Transparency = _transparency,
            Coarseness = _coarseness,
            FrostRadiusDp = MathF.Min(56f, _frostRadiusDp + 4f),
            TintDensity = _tintDensity,
            ThicknessDp = _thicknessDp,
            Ior = _ior,
            ChromaticDispersion = _chromaticDispersion,
            RimChalkiness = _rimChalkiness
        };

    private SeaGlassMaterial CustomTertiaryMaterial =>
        SeaGlassTheme.Current.Palette.TertiaryGlass with
        {
            Transparency = _transparency,
            Coarseness = _coarseness,
            FrostRadiusDp = _frostRadiusDp,
            Ior = _ior,
            ChromaticDispersion = _chromaticDispersion,
            ThicknessDp = _thicknessDp,
            RimChalkiness = _rimChalkiness
        };

    private SeaGlassMaterial PeachPebbleMaterial => new(
        tintColorArgb: unchecked((int)0xFFF7A89A),
        tintDensity: Math.Clamp(_tintDensity * 1.05f, 0.42f, 0.92f),
        coarseness: Math.Clamp(_coarseness * 1.15f, 0.32f, 0.95f),
        frostRadiusDp: _frostRadiusDp,
        ior: _ior,
        chromaticDispersion: 0.20f,
        rimChalkiness: 0.90f,
        thicknessDp: _thicknessDp,
        cornerRadiusDp: 34f,
        cornerSmoothing: 2.4f,
        transparency: Math.Clamp(_transparency * 0.85f, 0.28f, 0.85f));

    private SeaGlassMaterial OrchidPillMaterial => new(
        tintColorArgb: unchecked((int)0xFFEA96FF),
        tintDensity: Math.Clamp(_tintDensity * 1.04f, 0.42f, 0.92f),
        coarseness: Math.Clamp(_coarseness * 1.15f, 0.32f, 0.95f),
        frostRadiusDp: _frostRadiusDp,
        ior: _ior,
        chromaticDispersion: 0.22f,
        rimChalkiness: 0.90f,
        thicknessDp: _thicknessDp,
        cornerRadiusDp: 30f,
        cornerSmoothing: 2.5f,
        transparency: Math.Clamp(_transparency * 0.85f, 0.28f, 0.85f));

    private SeaGlassMaterial PeriwinklePromptMaterial => new(
        tintColorArgb: unchecked((int)0xFF9AB8FF),
        tintDensity: Math.Clamp(_tintDensity * 1.04f, 0.42f, 0.92f),
        coarseness: Math.Clamp(_coarseness * 1.15f, 0.32f, 0.95f),
        frostRadiusDp: _frostRadiusDp,
        ior: _ior,
        chromaticDispersion: 0.22f,
        rimChalkiness: 0.90f,
        thicknessDp: _thicknessDp,
        cornerRadiusDp: 31f,
        cornerSmoothing: 2.5f,
        transparency: Math.Clamp(_transparency * 0.85f, 0.28f, 0.85f));

    public void SelectPalette(SeaGlassPalette palette)
    {
        SeaGlassTheme.Current.SetPalette(palette);
        _transparency = palette.PrimaryGlass.Transparency;
        _coarseness = palette.PrimaryGlass.Coarseness;
        _frostRadiusDp = palette.PrimaryGlass.FrostRadiusDp;
        _tintDensity = palette.PrimaryGlass.TintDensity;
        _thicknessDp = palette.PrimaryGlass.ThicknessDp;
        _ior = palette.PrimaryGlass.Ior;
        _chromaticDispersion = 0.58f;
        _rimChalkiness = palette.PrimaryGlass.RimChalkiness;

        if (_frostedBackdropCanvas.Effect is BlurEffect blur)
        {
            blur.Radius = Math.Clamp(_frostRadiusDp * 1.65, 28.0, 44.0);
        }

        _sharpBackdropCanvas.InvalidateVisual();
        _frostedBackdropCanvas.InvalidateVisual();

        SyncComponentMaterials();
        RebuildUI();
    }

    private void SyncComponentMaterials()
    {
        GlobalOpticsRevision++;
        SeaGlassMaterial prim = CustomPrimaryMaterial;
        SeaGlassMaterial surf = DesktopCardMaterial;
        SeaGlassMaterial tert = CustomTertiaryMaterial;

        _pebbleOrb.BaseMaterial = PeachPebbleMaterial;
        _pebbleOrb.SnapMaterial(_pebbleOrb.BaseMaterial);
        _pillNavBar.BarMaterial = OrchidPillMaterial;
        _pillNavBar.SnapMaterial(_pillNavBar.BarMaterial);
        _promptBar.BarMaterial = PeriwinklePromptMaterial;
        _promptBar.SnapMaterial(_promptBar.BarMaterial);

        _shapeOrb.PrimaryMaterial = prim;
        _shapeOrb.SecondaryMaterial = tert;
        _buddingCluster.AnchorMaterial = prim;
        _buddingCluster.SatelliteMaterial = tert;
        _morphBtn0.RestingMaterial = prim with { CornerRadiusDp = 16f };
        _morphBtn0.ActiveMaterial = tert with { CornerRadiusDp = 24f };
        _morphBtn1.RestingMaterial = tert with { CornerRadiusDp = 16f };
        _morphBtn1.ActiveMaterial = prim with { CornerRadiusDp = 24f };
        _morphBtn2.RestingMaterial = prim with { CornerRadiusDp = 16f };
        _morphBtn2.ActiveMaterial = tert with { CornerRadiusDp = 24f };
        _twoElementMorph.CollapsedMaterial = prim with { CornerRadiusDp = 28f };
        _twoElementMorph.ExpandedMaterial = surf with { CornerRadiusDp = 24f };
        _contextMenuMorph.CollapsedMaterial = prim;
        _contextMenuMorph.ExpandedMaterial = surf;
        _cascadedModelMorph.PillMaterial = surf;
        _cascadedModelMorph.PopoverMaterial = surf;
        _contextRingMorph.CollapsedMaterial = surf;
        _contextRingMorph.ExpandedMaterial = surf;

        _segmentedRow.ContainerMaterial = surf with { CornerRadiusDp = 18f };
        _segmentedRow.IndicatorMaterial = prim with { CornerRadiusDp = 14f };
        _floatingToolbar.BarMaterial = surf with { CornerRadiusDp = 24f };
        _floatingToolbar.FabMaterial = prim with { CornerRadiusDp = 24f };
        _extendedFab.SetExpanded(_extendedFab.Expanded, prim with { CornerRadiusDp = 19f });
        _switchA.CheckedMaterial = prim; _switchA.UncheckedMaterial = surf;
        _switchB.CheckedMaterial = tert; _switchB.UncheckedMaterial = surf;
        _checkA.CheckedMaterial = prim with { CornerRadiusDp = 8f }; _checkA.UncheckedMaterial = surf with { CornerRadiusDp = 8f };
        _checkB.CheckedMaterial = tert with { CornerRadiusDp = 8f }; _checkB.UncheckedMaterial = surf with { CornerRadiusDp = 8f };
        _radioA.SelectedMaterial = prim; _radioA.UnselectedMaterial = surf;
        _radioB.SelectedMaterial = prim; _radioB.UnselectedMaterial = surf;
        _circularProgress.Material = surf with { CornerRadiusDp = 23f, ThicknessDp = 14f };
        _linearProgress.TrackMaterial = surf;
        _linearProgress.FillMaterial = prim;
        _miniPlayerBar.Material = surf with { CornerRadiusDp = 24f, ThicknessDp = 16f };

        foreach (Action sync in _materialSyncCallbacks)
        {
            sync();
        }
    }

    private void BuildStageRoot()
    {
        _stageRootGrid.Children.Clear();
        _stageRootGrid.Children.Add(_frostedBackdropHost);
        _stageRootGrid.Children.Add(_sharpBackdropCanvas);
        _stageRootGrid.Children.Add(_mainContentHost);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    //  Desktop window composition
    //
    //  ┌ Title bar (Mica) ─ app identity · breadcrumb · finish presets · caption buttons ┐
    //  ├ Sidebar (Mica) ┬ Content layer (edge-to-edge, 8px top-left corner) ──────────────┤
    //  │ navigation     │ Canvas (document: beach artwork / code)  │ Inspector (flat rows) │
    //  │ palettes       │ glass components float over content      │                       │
    //  │ motion toggle  ├──────────────────────── Status bar ─────────────────────────────┤
    //  └────────────────┴──────────────────────────────────────────────────────────────────┘
    //
    //  Glass is reserved for things that float above content (toolbars, flyouts, HUDs,
    //  droplets, controls). Window chrome and panes are flat layers — never cards.
    // ─────────────────────────────────────────────────────────────────────────────

    private static readonly string[] PageTitles = ["Material", "Liquid SDF", "Controls", "Shader Code"];
    private static readonly SeaGlassGlyph[] PageGlyphs = [SeaGlassGlyph.Tune, SeaGlassGlyph.Sparkles, SeaGlassGlyph.Layers, SeaGlassGlyph.Menu];

    private const double NavTop = 10;
    private const double NavItemH = 36;
    private const double PaletteRowH = 32;
    private static double PaletteHeaderTop => NavTop + PageTitles.Length * NavItemH + 18;
    private static double PaletteTop => PaletteHeaderTop + 30;

    private static bool IsDarkTheme => SeaGlassTheme.Current.Palette.IsDark;
    private static int Ink => IsDarkTheme ? unchecked((int)0xFFF1F4F8) : unchecked((int)0xFF171B21);

    private int AccentInk => IsDarkTheme
        ? SeaGlassColorMath.LerpOklab(CustomPrimaryMaterial.TintColorArgb, unchecked((int)0xFFFFFFFF), 0.25f)
        : SeaGlassColorMath.LerpOklab(CustomPrimaryMaterial.TintColorArgb, unchecked((int)0xFF0B2530), 0.48f);

    private int AccentFill => IsDarkTheme
        ? CustomPrimaryMaterial.TintColorArgb
        : SeaGlassColorMath.LerpOklab(CustomPrimaryMaterial.TintColorArgb, unchecked((int)0xFF0B2530), 0.18f);

    private static readonly Dictionary<int, SolidColorBrush> BrushCache = new();
    private static readonly Dictionary<(int, int), Pen> PenCache = new();

    private static int PremulKey(int argb, float alpha)
    {
        int a = (int)MathF.Round(Math.Clamp(alpha, 0f, 1f) * ((argb >>> 24) & 0xFF));
        return (argb & 0x00FFFFFF) | (a << 24);
    }

    private static SolidColorBrush Fill(int argb, float alpha = 1f)
    {
        int key = PremulKey(argb, alpha);
        if (BrushCache.TryGetValue(key, out SolidColorBrush? cached)) return cached;
        if (BrushCache.Count > 2048) BrushCache.Clear();
        SolidColorBrush b = new(SeaGlassXamlRenderer.ToMediaColor(argb, alpha));
        b.Freeze();
        BrushCache[key] = b;
        return b;
    }

    private static Pen Stroke(int argb, float alpha, double thickness)
    {
        var key = (PremulKey(argb, alpha), (int)Math.Round(thickness * 100));
        if (PenCache.TryGetValue(key, out Pen? cached)) return cached;
        if (PenCache.Count > 1024) PenCache.Clear();
        Pen p = new(Fill(argb, alpha), thickness)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round
        };
        p.Freeze();
        PenCache[key] = p;
        return p;
    }

    private StudioPaintHost NewPaintHost(Action<DrawingContext, double, double> painter, Func<int>? signature = null)
    {
        StudioPaintHost host = new() { Painter = painter, Signature = signature };
        _paintHosts.Add(host);
        return host;
    }

    public void RebuildUI()
    {
        _activeControls.Clear();
        _paintHosts.Clear();
        _materialSyncCallbacks.Clear();
        SyncComponentMaterials();

        StudioCanvasMode mode = _page == 3
            ? StudioCanvasMode.CodeEditor
            : (_porcelainDaylight ? StudioCanvasMode.PorcelainStudio : StudioCanvasMode.Artwork);
        if (_sharpBackdropCanvas.CanvasMode != mode)
        {
            _sharpBackdropCanvas.CanvasMode = mode;
            _frostedBackdropCanvas.CanvasMode = mode;
            _sharpBackdropCanvas.InvalidateVisual();
            _frostedBackdropCanvas.InvalidateVisual();
        }

        Grid shell = new();
        shell.RowDefinitions.Add(new RowDefinition { Height = new GridLength(StudioLayout.TitleBarHeight) });
        shell.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        UIElement titleBar = BuildTitleBar();
        Grid.SetRow(titleBar, 0);
        shell.Children.Add(titleBar);

        Grid body = new();
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(StudioLayout.SidebarWidth) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(body, 1);
        shell.Children.Add(body);

        UIElement sidebar = BuildSidebar();
        Grid.SetColumn(sidebar, 0);
        body.Children.Add(sidebar);

        Grid layer = new();
        layer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        layer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(StudioLayout.StatusBarHeight) });
        Grid.SetColumn(layer, 1);
        body.Children.Add(layer);

        Grid panes = new();
        panes.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        panes.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1) });
        panes.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(StudioLayout.InspectorWidth) });
        Grid.SetRow(panes, 0);
        layer.Children.Add(panes);

        UIElement canvas = BuildCanvas();
        Grid.SetColumn(canvas, 0);
        panes.Children.Add(canvas);

        UIElement inspector = BuildInspector();
        Grid.SetColumn(inspector, 2);
        panes.Children.Add(inspector);

        UIElement status = BuildStatusBar();
        Grid.SetRow(status, 1);
        layer.Children.Add(status);

        _mainContentHost.Content = shell;
        MarkAllControlsOriginDirty();
    }

    // ── Title bar ───────────────────────────────────────────────────────────────

    private void ApplyOpticalPreset(float tr, float c, float f, float d, float t, float ior, float disp)
    {
        _transparency = tr; _coarseness = c; _frostRadiusDp = f;
        _tintDensity = d; _thicknessDp = t; _ior = ior; _chromaticDispersion = disp;
        if (_frostedBackdropCanvas.Effect is BlurEffect blur)
        {
            blur.Radius = Math.Clamp(_frostRadiusDp * 1.65, 28.0, 44.0);
        }
        SyncComponentMaterials();
        RebuildUI();
    }

    private UIElement BuildTitleBar()
    {
        SeaGlassPalette palette = SeaGlassTheme.Current.Palette;
        Grid bar = new();

        StudioPaintHost identity = NewPaintHost((dc, w, h) =>
        {
            int ink = Ink;
            LinearGradientBrush gem = new(
                SeaGlassXamlRenderer.ToMediaColor(CustomPrimaryMaterial.TintColorArgb),
                SeaGlassXamlRenderer.ToMediaColor(CustomTertiaryMaterial.TintColorArgb),
                new Point(0, 0), new Point(1, 1));
            gem.Freeze();
            dc.DrawRoundedRectangle(gem, Stroke(unchecked((int)0xFFFFFFFF), 0.6f, 1.0), new Rect(16, 14, 20, 20), 5.5, 5.5);
            SeaGlassXamlRenderer.DrawGlyph(dc, SeaGlassGlyph.Sparkles, new Rect(20, 18, 12, 12), unchecked((int)0xFFFFFFFF));
            StudioText.DrawInRow(dc, "SeaGlass Studio", 46, 0, h, 12.5, ink, StudioText.Style.Semibold);

            double x = StudioLayout.SidebarWidth + 16;
            FormattedText page = StudioText.Get(PageTitles[_page], 14, ink, StudioText.Style.Display);
            dc.DrawText(page, new Point(x, (h - page.Height) * 0.5));
            x += page.Width + 10;
            FormattedText chevron = StudioText.Get("›", 14, ink, StudioText.Style.Regular, 0.4f);
            dc.DrawText(chevron, new Point(x, (h - chevron.Height) * 0.5 - 1));
            x += chevron.Width + 10;
            StudioText.DrawInRow(dc, palette.DisplayName, x, 0, h, 13, ink, StudioText.Style.Regular, 0.62f);
        }, () => HashCode.Combine(_page, palette.Id));
        identity.IsHitTestVisible = false;
        bar.Children.Add(identity);

        StackPanel right = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        StackPanel presets = new()
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 14, 0)
        };

        string[] presetLabels = ["Sanded", "Liquid", "Frosted"];
        Action[] presetApply =
        [
            () => ApplyOpticalPreset(0.64f, 0.44f, 22f, 0.68f, 16f, 1.54f, 0.80f),
            () => ApplyOpticalPreset(0.86f, 0.16f, 12f, 0.62f, 22f, 1.72f, 0.88f),
            () => ApplyOpticalPreset(0.36f, 0.78f, 34f, 0.74f, 16f, 1.48f, 0.92f)
        ];
        StudioPaintHost seg = null!;
        seg = NewPaintHost((dc, w, h) =>
        {
            int ink = Ink;
            double slotW = (w - 4) / presetLabels.Length;
            int hoverIdx = seg.PointerPos is { } p ? Math.Clamp((int)((p.X - 2) / slotW), 0, presetLabels.Length - 1) : -1;
            dc.DrawRoundedRectangle(Fill(ink, IsDarkTheme ? 0.07f : 0.05f), Stroke(ink, IsDarkTheme ? 0.07f : 0.05f, 1.0), new Rect(0.5, 0.5, w - 1, h - 1), 6, 6);
            if (hoverIdx >= 0 && hoverIdx != ActivePresetIndex)
            {
                dc.DrawRoundedRectangle(Fill(ink, 0.04f), null, new Rect(2 + hoverIdx * slotW, 2, slotW, h - 4), 4.5, 4.5);
            }

            Rect chip = new(2 + _presetSlot * slotW, 2, slotW, h - 4);
            dc.DrawRoundedRectangle(Fill(unchecked((int)0xFF000000), IsDarkTheme ? 0.22f : 0.07f), null, new Rect(chip.X, chip.Y + 1, chip.Width, chip.Height), 4.5, 4.5);
            dc.DrawRoundedRectangle(Fill(IsDarkTheme ? unchecked((int)0xFF3B3F47) : unchecked((int)0xFFFFFFFF)), Stroke(ink, IsDarkTheme ? 0.10f : 0.07f, 1.0), chip, 4.5, 4.5);

            for (int i = 0; i < presetLabels.Length; i++)
            {
                bool sel = i == ActivePresetIndex;
                StudioText.DrawCentered(dc, presetLabels[i], new Point(2 + (i + 0.5) * slotW, h * 0.5), 12, ink,
                    sel ? StudioText.Style.Semibold : StudioText.Style.Regular, sel ? 1f : 0.7f);
            }
        }, () => HashCode.Combine((int)(_presetSlot * 400f), ActivePresetIndex, IsDarkTheme, seg.PointerPos.HasValue ? (int)(seg.PointerPos.Value.X / 8) : -1));
        seg.Width = 222;
        seg.Height = 30;
        seg.MouseLeftButtonUp += (_, e) =>
        {
            double slotW = (seg.ActualWidth - 4) / presetLabels.Length;
            int idx = Math.Clamp((int)((e.GetPosition(seg).X - 2) / slotW), 0, presetLabels.Length - 1);
            if (idx != ActivePresetIndex) presetApply[idx]();
        };
        WindowChrome.SetIsHitTestVisibleInChrome(seg, true);
        presets.Children.Add(seg);

        SeaGlassPalette nextPalette = SeaGlassPalette.Entries[(IndexOfPalette(palette) + 1) % SeaGlassPalette.Entries.Count];
        StudioPaintHost nextBtn = null!;
        nextBtn = NewPaintHost((dc, w, h) =>
        {
            int ink = Ink;
            if (nextBtn.IsHovered)
            {
                dc.DrawRoundedRectangle(Fill(ink, nextBtn.IsMouseCaptured ? 0.08f : 0.05f), null, new Rect(0, 0, w, h), 6, 6);
            }
            LinearGradientBrush swatch = new(
                SeaGlassXamlRenderer.ToMediaColor(nextPalette.PrimaryGlass.TintColorArgb),
                SeaGlassXamlRenderer.ToMediaColor(nextPalette.TertiaryGlass.TintColorArgb),
                new Point(0, 0), new Point(1, 1));
            swatch.Freeze();
            dc.DrawRoundedRectangle(swatch, Stroke(ink, 0.16f, 1.0), new Rect(10, (h - 14) * 0.5, 14, 14), 4, 4);
            StudioText.DrawInRow(dc, "Next palette", 32, 0, h, 12, ink, StudioText.Style.Regular, 0.86f);
        }, () => HashCode.Combine(nextBtn.IsHovered, nextBtn.IsMouseCaptured, nextPalette.Id, IsDarkTheme));
        nextBtn.Width = 112;
        nextBtn.Height = 30;
        nextBtn.Margin = new Thickness(8, 0, 0, 0);
        nextBtn.MouseLeftButtonDown += (_, _) => nextBtn.CaptureMouse();
        nextBtn.MouseLeftButtonUp += (_, e) =>
        {
            Point pos = e.GetPosition(nextBtn);
            bool inside = pos.X >= 0 && pos.Y >= 0 && pos.X <= nextBtn.ActualWidth && pos.Y <= nextBtn.ActualHeight;
            nextBtn.ReleaseMouseCapture();
            if (inside) SelectPalette(nextPalette);
        };
        WindowChrome.SetIsHitTestVisibleInChrome(nextBtn, true);
        presets.Children.Add(nextBtn);
        right.Children.Add(presets);

        right.Children.Add(BuildCaptionButton(0));
        right.Children.Add(BuildCaptionButton(1));
        right.Children.Add(BuildCaptionButton(2));
        bar.Children.Add(right);
        return bar;
    }

    /// <summary>Windows 11 caption buttons (minimize / maximize-restore / close), 46 × title-bar height.</summary>
    private UIElement BuildCaptionButton(int kind)
    {
        StudioPaintHost host = null!;
        host = NewPaintHost((dc, w, h) =>
        {
            bool hover = host.IsHovered;
            int ink = Ink;
            if (hover)
            {
                dc.DrawRectangle(kind == 2 ? Fill(unchecked((int)0xFFC42B1C)) : Fill(ink, 0.07f), null, new Rect(0, 0, w, h));
            }
            int glyphInk = hover && kind == 2 ? unchecked((int)0xFFFFFFFF) : ink;
            Pen pen = new(Fill(glyphInk, 0.92f), 1.0);
            pen.Freeze();
            double cx = Math.Round(w * 0.5) + 0.5;
            double cy = Math.Round(h * 0.5) + 0.5;
            switch (kind)
            {
                case 0:
                    dc.DrawLine(pen, new Point(cx - 5, cy), new Point(cx + 5, cy));
                    break;
                case 1 when WindowState == WindowState.Maximized:
                    dc.DrawRectangle(null, pen, new Rect(cx - 5, cy - 3, 8, 8));
                    dc.DrawLine(pen, new Point(cx - 3, cy - 5), new Point(cx + 5, cy - 5));
                    dc.DrawLine(pen, new Point(cx + 5, cy - 5), new Point(cx + 5, cy + 3));
                    break;
                case 1:
                    dc.DrawRoundedRectangle(null, pen, new Rect(cx - 5, cy - 5, 10, 10), 1.5, 1.5);
                    break;
                default:
                    dc.DrawLine(pen, new Point(cx - 5, cy - 5), new Point(cx + 5, cy + 5));
                    dc.DrawLine(pen, new Point(cx + 5, cy - 5), new Point(cx - 5, cy + 5));
                    break;
            }
        }, () => HashCode.Combine(IsDarkTheme, WindowState));
        host.Width = 46;
        host.MouseLeftButtonUp += (_, _) =>
        {
            switch (kind)
            {
                case 0: WindowState = WindowState.Minimized; break;
                case 1: WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized; break;
                default: Close(); break;
            }
        };
        WindowChrome.SetIsHitTestVisibleInChrome(host, true);
        return host;
    }

    // ── Sidebar ─────────────────────────────────────────────────────────────────

    private (int Kind, int Index) SidebarRowAt(double y)
    {
        if (y >= NavTop && y < NavTop + PageTitles.Length * NavItemH)
        {
            return (0, (int)((y - NavTop) / NavItemH));
        }
        if (y >= PaletteTop && y < PaletteTop + SeaGlassPalette.Entries.Count * PaletteRowH)
        {
            return (1, (int)((y - PaletteTop) / PaletteRowH));
        }
        return (-1, -1);
    }

    private UIElement BuildSidebar()
    {
        SeaGlassPalette palette = SeaGlassTheme.Current.Palette;
        Grid side = new();

        StudioPaintHost nav = null!;
        nav = NewPaintHost((dc, w, h) =>
        {
            int ink = Ink;
            float selAlpha = IsDarkTheme ? 0.09f : 0.065f;
            float hoverAlpha = IsDarkTheme ? 0.05f : 0.035f;
            (int hoverKind, int hoverIdx) = nav.PointerPos is { } p ? SidebarRowAt(p.Y) : (-1, -1);

            // Navigation items
            for (int i = 0; i < PageTitles.Length; i++)
            {
                double y = NavTop + i * NavItemH;
                Rect row = new(6, y + 2, w - 12, NavItemH - 4);
                bool sel = i == _page;
                if (sel) dc.DrawRoundedRectangle(Fill(ink, selAlpha), null, row, 5, 5);
                else if (hoverKind == 0 && hoverIdx == i) dc.DrawRoundedRectangle(Fill(ink, hoverAlpha), null, row, 5, 5);

                SeaGlassXamlRenderer.DrawGlyph(dc, PageGlyphs[i], new Rect(19, y + 10, 16, 16), sel ? AccentInk : ink, sel ? 1f : 0.82f);
                StudioText.DrawInRow(dc, PageTitles[i], 46, y, NavItemH, 13, ink, sel ? StudioText.Style.Semibold : StudioText.Style.Regular, sel ? 1f : 0.88f);
            }

            // Lead-lag spring selection indicator (WinUI pill that stretches between items)
            float topSlot = MathF.Min(_sidebarLeadSlot, _sidebarTrailSlot);
            float botSlot = MathF.Max(_sidebarLeadSlot, _sidebarTrailSlot);
            double barTop = NavTop + topSlot * NavItemH + 11;
            double barH = (botSlot - topSlot) * NavItemH + NavItemH - 22;
            dc.DrawRoundedRectangle(Fill(AccentFill), null, new Rect(6, barTop, 3, Math.Max(6, barH)), 1.5, 1.5);

            // Palettes
            StudioText.DrawInRow(dc, "Palettes", 18, PaletteHeaderTop, 30, 12, ink, StudioText.Style.Semibold, 0.6f);
            for (int i = 0; i < SeaGlassPalette.Entries.Count; i++)
            {
                SeaGlassPalette pal = SeaGlassPalette.Entries[i];
                double y = PaletteTop + i * PaletteRowH;
                Rect row = new(6, y + 1, w - 12, PaletteRowH - 2);
                bool sel = pal.Id == palette.Id;
                if (sel) dc.DrawRoundedRectangle(Fill(ink, selAlpha), null, row, 5, 5);
                else if (hoverKind == 1 && hoverIdx == i) dc.DrawRoundedRectangle(Fill(ink, hoverAlpha), null, row, 5, 5);

                LinearGradientBrush chip = new(
                    SeaGlassXamlRenderer.ToMediaColor(pal.PrimaryGlass.TintColorArgb),
                    SeaGlassXamlRenderer.ToMediaColor(pal.TertiaryGlass.TintColorArgb),
                    new Point(0, 0), new Point(1, 1));
                chip.Freeze();
                dc.DrawRoundedRectangle(chip, Stroke(ink, 0.16f, 1.0), new Rect(19, y + 8, 16, 16), 4.5, 4.5);
                StudioText.DrawInRow(dc, pal.DisplayName, 46, y, PaletteRowH, 13, ink, StudioText.Style.Regular, sel ? 1f : 0.82f);
                if (sel)
                {
                    SeaGlassXamlRenderer.DrawGlyph(dc, SeaGlassGlyph.Check, new Rect(w - 34, y + 9, 14, 14), AccentInk);
                }
            }
        }, () =>
        {
            int hover = nav.PointerPos is { } p ? SidebarRowAt(p.Y).GetHashCode() : -1;
            return HashCode.Combine((int)(_sidebarLeadSlot * 400f), (int)(_sidebarTrailSlot * 400f), _page, palette.Id, hover);
        });
        nav.Cursor = Cursors.Arrow;
        nav.MouseLeftButtonUp += (_, e) =>
        {
            var (kind, idx) = SidebarRowAt(e.GetPosition(nav).Y);
            if (kind == 0) SelectSidebarSlot(idx);
            else if (kind == 1) SelectPalette(SeaGlassPalette.Entries[idx]);
        };
        side.Children.Add(nav);

        // Footer: motion preference
        Grid footer = new() { Height = 52, VerticalAlignment = VerticalAlignment.Bottom };
        footer.Children.Add(NewPaintHost((dc, w, h) =>
        {
            dc.DrawRectangle(Fill(Ink, IsDarkTheme ? 0.10f : 0.08f), null, new Rect(14, 0, w - 28, 1));
            StudioText.DrawInRow(dc, "Auto-play motion", 18, 0, h, 13, Ink, StudioText.Style.Regular, 0.88f);
        }, () => HashCode.Combine(IsDarkTheme)));

        SeaGlassSwitch autoSwitch = new(_autoAnimateStates, v => _autoAnimateStates = v, _stageState, CustomPrimaryMaterial, DesktopCardMaterial);
        _materialSyncCallbacks.Add(() => { autoSwitch.CheckedMaterial = CustomPrimaryMaterial; autoSwitch.UncheckedMaterial = DesktopCardMaterial; });
        UIElement autoCtrl = BuildInteractiveSwitchControl(autoSwitch);
        if (autoCtrl is FrameworkElement fe)
        {
            fe.HorizontalAlignment = HorizontalAlignment.Right;
            fe.VerticalAlignment = VerticalAlignment.Center;
            fe.Margin = new Thickness(0, 0, 16, 0);
        }
        footer.Children.Add(autoCtrl);
        side.Children.Add(footer);
        return side;
    }

    // ── Canvas (document) ───────────────────────────────────────────────────────

    private UIElement BuildCanvas()
    {
        Grid canvas = new() { ClipToBounds = true };
        switch (_page)
        {
            case 0:
            {
                UIElement toolbar = BuildFloatingToolbarControl();
                canvas.Children.Add(toolbar);

                // Concept Reference Triad (Coral-Peach "G" Pebble + Orchid Pill Nav Bar + Periwinkle Prompt Bar)
                FrameworkElement triad = BuildConceptReferenceTriad();
                triad.HorizontalAlignment = HorizontalAlignment.Center;
                triad.VerticalAlignment = VerticalAlignment.Top;
                triad.Margin = new Thickness(0, 92, 0, 0);
                canvas.Children.Add(triad);

                // Material-preview specimen locked to the lower artwork horizon so refraction is legible.
                Canvas specimenHost = new();
                FrameworkElement specimen = BuildSpecimenLens(310, 156);
                specimenHost.Children.Add(specimen);
                specimenHost.SizeChanged += (_, e) =>
                {
                    Canvas.SetLeft(specimen, Math.Round(e.NewSize.Width * 0.50 - 155));
                    Canvas.SetTop(specimen, Math.Round(Math.Max(264, e.NewSize.Height * 0.58 - 78)));
                    MarkAllControlsOriginDirty();
                };
                canvas.Children.Add(specimenHost);

                FrameworkElement deck = BuildMediaDeckControl();
                deck.VerticalAlignment = VerticalAlignment.Bottom;
                deck.Margin = new Thickness(0, 0, 0, 28);
                canvas.Children.Add(deck);
                break;
            }
            case 1:
            {
                FrameworkElement arena = BuildDropletArena(720, 400, 1.55f, morph: 0f);
                arena.VerticalAlignment = VerticalAlignment.Center;
                arena.Margin = new Thickness(0, 160, 0, 0); // sit in the sea so waves refract through the lens
                canvas.Children.Add(arena);

                FrameworkElement hud = BuildHudPill("Drag the right droplet to coalesce  ·  click to split", 360);
                hud.VerticalAlignment = VerticalAlignment.Bottom;
                hud.Margin = new Thickness(0, 0, 0, 28);
                canvas.Children.Add(hud);
                break;
            }
            case 2:
            {
                canvas.Children.Add(BuildFloatingToolbarControl());
                canvas.Children.Add(BuildQuickSettingsFlyout());

                UIElement fab = BuildInteractiveExtendedFabControl();
                if (fab is FrameworkElement feFab)
                {
                    feFab.Width = 108;
                    feFab.Height = 40;
                    feFab.HorizontalAlignment = HorizontalAlignment.Right;
                    feFab.VerticalAlignment = VerticalAlignment.Bottom;
                    feFab.Margin = new Thickness(0, 0, 24, 24);
                }
                canvas.Children.Add(fab);
                break;
            }
            default:
                canvas.Children.Add(BuildCodeEditor());
                break;
        }
        return canvas;
    }

    private FrameworkElement BuildConceptReferenceTriad()
    {
        StackPanel root = new()
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        StackPanel topRow = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 12)
        };

        FrameworkElement pebbleCtrl = BuildPebbleOrbControl(68);
        pebbleCtrl.Margin = new Thickness(0, 0, 14, 0);
        pebbleCtrl.VerticalAlignment = VerticalAlignment.Center;
        topRow.Children.Add(pebbleCtrl);

        FrameworkElement pillNavCtrl = BuildPillNavBarControl(420, 60);
        pillNavCtrl.VerticalAlignment = VerticalAlignment.Center;
        topRow.Children.Add(pillNavCtrl);

        root.Children.Add(topRow);

        FrameworkElement promptCtrl = BuildPromptBarControl(502, 62);
        promptCtrl.HorizontalAlignment = HorizontalAlignment.Center;
        root.Children.Add(promptCtrl);

        return root;
    }

    private FrameworkElement BuildPebbleOrbControl(double size)
    {
        SeaGlassVisualControl ctrl = new(
            surfaceModel: _pebbleOrb,
            stageBackdrop: _stageBackdropVisuals,
            stageCoordinateRoot: _stageRootGrid,
            customStepEvaluator: dt => _pebbleOrb.StepAndEvaluate(dt, SeaGlassTheme.Current),
            customOverlayPainter: (dc, frame, w, h) =>
            {
                double gSize = Math.Min(w, h) * 0.48;
                SeaGlassXamlRenderer.DrawGlyph(
                    dc,
                    _pebbleOrb.Glyph,
                    new Rect((w - gSize) * 0.5, (h - gSize) * 0.5, gSize, gSize),
                    frame.OptimalContentColorArgb);
            },
            customStateSignature: () => HashCode.Combine((int)(_pebbleOrb.ExpandScale * 500f), GlobalOpticsRevision),
            interactiveHoverLift: true)
        {
            Width = size,
            Height = size
        };
        _activeControls.Add(ctrl);
        return ctrl;
    }

    private FrameworkElement BuildPillNavBarControl(double width, double height)
    {
        SeaGlassVisualControl ctrl = null!;
        ctrl = new(
            surfaceModel: _pillNavBar,
            stageBackdrop: _stageBackdropVisuals,
            stageCoordinateRoot: _stageRootGrid,
            customStepEvaluator: dt => _pillNavBar.StepAndEvaluate(dt, SeaGlassTheme.Current),
            customOverlayPainter: (dc, frame, w, h) =>
            {
                int c = frame.OptimalContentColorArgb;
                int count = Math.Max(1, _pillNavBar.Items.Count);
                double pad = 6.0;
                double innerW = Math.Max(1.0, w - pad * 2.0);
                double innerH = Math.Max(1.0, h - pad * 2.0);
                double slotW = innerW / count;
                double offsetX = pad + _pillNavBar.LeftEdgeTab * slotW;
                double pillW = Math.Max(slotW * 0.85, _pillNavBar.LozengeSpan * slotW);
                double scaleY = 1.0 - _pillNavBar.StretchSpan * 0.08;
                double pillH = innerH * scaleY;
                double offsetY = pad + (innerH - pillH) * 0.5;

                // Milky-Frosted Sea-Glass Inlay Capsule (matches the active "Photos" pill in reference)
                var (topArgb, topA, botArgb, botA, rimA) = _pillNavBar.ComputeInlayColors();
                LinearGradientBrush inlayBrush = new(
                    SeaGlassXamlRenderer.ToMediaColor(topArgb, topA),
                    SeaGlassXamlRenderer.ToMediaColor(botArgb, botA),
                    new Point(0, 0),
                    new Point(0, 1));
                inlayBrush.Freeze();
                Rect inlayRect = new(offsetX, offsetY, pillW, pillH);
                dc.DrawRoundedRectangle(inlayBrush, null, inlayRect, pillH * 0.5, pillH * 0.5);
                for (int s = 1; s <= 5; s++)
                {
                    double sw = 0.8 + s * 1.15;
                    float sa = rimA * 0.11f * (6 - s) / 5.0f;
                    dc.DrawRoundedRectangle(null, Stroke(topArgb, sa, sw), inlayRect, pillH * 0.5, pillH * 0.5);
                }

                for (int i = 0; i < count; i++)
                {
                    var (glyph, title) = _pillNavBar.Items[i];
                    bool selected = i == _pillNavBar.SelectedIndex;
                    float alpha = selected ? 1.0f : 0.86f;
                    double slotCenterX = pad + (i + 0.5) * slotW;
                    double centerY = h * 0.5;

                    FormattedText ft = StudioText.Get(title, 14.5, c, selected ? StudioText.Style.Semibold : StudioText.Style.Regular, alpha);
                    if (glyph is { } g && (selected || i == 0))
                    {
                        const double iconSz = 18.0;
                        const double gap = 7.0;
                        double totalW = iconSz + gap + ft.Width;
                        double startX = slotCenterX - totalW * 0.5;
                        SeaGlassXamlRenderer.DrawGlyph(dc, g, new Rect(startX, centerY - iconSz * 0.5, iconSz, iconSz), c, alpha);
                        dc.DrawText(ft, new Point(startX + iconSz + gap, centerY - ft.Height * 0.5));
                    }
                    else
                    {
                        dc.DrawText(ft, new Point(slotCenterX - ft.Width * 0.5, centerY - ft.Height * 0.5));
                    }
                }
            },
            customStateSignature: () => HashCode.Combine(
                (int)(_pillNavBar.LeadIndex * 500f),
                (int)(_pillNavBar.TrailIndex * 500f),
                _pillNavBar.SelectedIndex,
                GlobalOpticsRevision),
            interactiveHoverLift: true)
        {
            Width = width,
            Height = height
        };

        ctrl.MouseLeftButtonUp += (_, e) =>
        {
            Point pt = e.GetPosition(ctrl);
            int count = Math.Max(1, _pillNavBar.Items.Count);
            double slotW = Math.Max(1.0, (ctrl.ActualWidth - 12.0) / count);
            int idx = Math.Clamp((int)((pt.X - 6.0) / slotW), 0, count - 1);
            _pillNavBar.SelectTab(idx);
        };

        _activeControls.Add(ctrl);
        return ctrl;
    }

    private FrameworkElement BuildPromptBarControl(double width, double height)
    {
        SeaGlassVisualControl ctrl = null!;
        ctrl = new(
            surfaceModel: _promptBar,
            stageBackdrop: _stageBackdropVisuals,
            stageCoordinateRoot: _stageRootGrid,
            customStepEvaluator: dt => _promptBar.StepAndEvaluate(dt, SeaGlassTheme.Current),
            customOverlayPainter: (dc, frame, w, h) =>
            {
                int c = frame.OptimalContentColorArgb;
                double cy = h * 0.5;

                // 1. Leading "+" button
                SeaGlassXamlRenderer.DrawGlyph(dc, SeaGlassGlyph.Plus, new Rect(22, cy - 10, 20, 20), c);

                // 2. Tonal prompt text ("Ask Gemini")
                StudioText.DrawInRow(dc, _promptBar.PromptText, 54, 0, h, 15.5, c, StudioText.Style.Semibold);

                // 3. Inline Microphone button
                double medallionSize = 48.0 * _promptBar.WaveScale;
                double medallionCenterX = w - 7.0 - 24.0;
                double micCenterX = medallionCenterX - 24.0 - 6.0 - 19.0;
                SeaGlassXamlRenderer.DrawGlyph(dc, SeaGlassGlyph.Microphone, new Rect(micCenterX - 10, cy - 10, 20, 20), c);

                // 4. Nested Circular Milky-Frosted Sea-Glass Voice Waveform Medallion
                var (topArgb, topA, botArgb, botA, rimA) = _promptBar.ComputeMedallionColors();
                LinearGradientBrush medBrush = new(
                    SeaGlassXamlRenderer.ToMediaColor(topArgb, topA),
                    SeaGlassXamlRenderer.ToMediaColor(botArgb, botA),
                    new Point(0, 0),
                    new Point(0, 1));
                medBrush.Freeze();
                double medR = medallionSize * 0.5;
                Point medCenter = new(medallionCenterX, cy);
                dc.DrawEllipse(medBrush, null, medCenter, medR, medR);
                for (int s = 1; s <= 5; s++)
                {
                    double sw = 0.8 + s * 1.15;
                    float sa = rimA * 0.11f * (6 - s) / 5.0f;
                    dc.DrawEllipse(null, Stroke(topArgb, sa, sw), medCenter, medR, medR);
                }
                SeaGlassXamlRenderer.DrawGlyph(dc, SeaGlassGlyph.AudioWave, new Rect(medallionCenterX - 10.5, cy - 10.5, 21, 21), c);
            },
            customStateSignature: () => HashCode.Combine(
                _promptBar.PromptText,
                _promptBar.IsVoiceActive,
                (int)(_promptBar.WaveScale * 500f),
                GlobalOpticsRevision),
            interactiveHoverLift: true)
        {
            Width = width,
            Height = height
        };

        ctrl.MouseLeftButtonUp += (_, e) =>
        {
            Point pt = e.GetPosition(ctrl);
            if (pt.X >= ctrl.ActualWidth - 105)
            {
                _promptBar.ToggleVoiceActive();
            }
            else
            {
                _conceptPromptIndex = (_conceptPromptIndex + 1) % ConceptPrompts.Length;
                _promptBar.PromptText = ConceptPrompts[_conceptPromptIndex];
            }
        };

        _activeControls.Add(ctrl);
        return ctrl;
    }

    private UIElement BuildFloatingToolbarControl()
    {
        IReadOnlyList<SeaGlassSdfEngine.SdfNode> ComputeToolbarNodes(double w, double h)
        {
            float tw = (float)Math.Max(240, w);
            float th = 48f;
            float gap = _floatingToolbar.CurrentGapDp;
            float mainW = MathF.Max(th * 2f, tw - th - MathF.Max(0f, gap));
            float fabLeft = MathF.Min(tw - th, mainW + gap);
            return
            [
                new SeaGlassSdfEngine.SdfNode(mainW * 0.5f, th * 0.5f, mainW * 0.5f, th * 0.5f, th * 0.5f, 2.2f, CustomPrimaryMaterial.TintColorArgb, CustomPrimaryMaterial.ThicknessDp * 1.4f),
                new SeaGlassSdfEngine.SdfNode(fabLeft + th * 0.5f, th * 0.5f, th * 0.5f, th * 0.5f, th * 0.5f, 2.2f, CustomTertiaryMaterial.TintColorArgb, CustomTertiaryMaterial.ThicknessDp * 1.4f)
            ];
        }

        SeaGlassVisualControl toolbarCtrl = new(
            _floatingToolbar,
            _stageBackdropVisuals,
            _stageRootGrid,
            dt => _floatingToolbar.StepAndEvaluate(dt, SeaGlassTheme.Current),
            (dc, frame, w, h) =>
            {
                int c = frame.OptimalContentColorArgb;
                double mainW = Math.Max(140, w - 48 - Math.Max(0, _floatingToolbar.CurrentGapDp));
                double slotW = mainW / 4.0;
                for (int i = 0; i < _floatingToolbar.Actions.Count; i++)
                {
                    var (glyph, lbl) = _floatingToolbar.Actions[i];
                    bool sel = i == _floatingToolbar.SelectedActionIndex;
                    double sx = i * slotW + 16;
                    SeaGlassXamlRenderer.DrawGlyph(dc, glyph, new Rect(sx, 16, 16, 16), c, sel ? 1f : 0.78f);
                    StudioText.DrawInRow(dc, lbl, sx + 22, 0, h, 12, c, sel ? StudioText.Style.Semibold : StudioText.Style.Regular, sel ? 1f : 0.8f);
                }
                double fabLeft = Math.Min(w - 48, mainW + _floatingToolbar.CurrentGapDp);
                int fc = SeaGlassColorMath.OptimalContentColorFor(CustomTertiaryMaterial.TintColorArgb, CustomTertiaryMaterial.TintDensity);
                SeaGlassXamlRenderer.DrawGlyph(dc, _floatingToolbar.FabDetached ? SeaGlassGlyph.Close : SeaGlassGlyph.Plus, new Rect(fabLeft + 15, 15, 18, 18), fc);
            },
            activeNodesProvider: () => ComputeToolbarNodes(_floatingToolbar.SizePx.X, _floatingToolbar.SizePx.Y),
            smoothRadiusDp: 30f,
            customStateSignature: () => HashCode.Combine((int)(_floatingToolbar.CurrentGapDp * 100f), _floatingToolbar.SelectedActionIndex),
            interactiveHoverLift: true)
        {
            Width = 440,
            Height = 48,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 22, 0, 0)
        };
        _activeControls.Add(toolbarCtrl);
        return toolbarCtrl;
    }

    private FrameworkElement BuildDropletArena(double width, double height, float scale, float morph = 0.2f)
    {
        SeaGlassSurface arenaSurface = new(_stageState, CustomPrimaryMaterial, contentRole: SeaGlassContentRole.PureOpticalLens);
        _materialSyncCallbacks.Add(() => arenaSurface.Material = CustomPrimaryMaterial);
        float rA = 44f * scale;
        float rB = 40f * scale;

        IReadOnlyList<SeaGlassSdfEngine.SdfNode> ComputeArenaNodes(double w, double h)
        {
            float aw = (float)Math.Max(rA * 4, w);
            float ah = (float)Math.Max(rA * 2.4f, h);
            float cAX = aw * 0.38f;
            float cAY = ah * 0.5f;
            float cBX = Math.Clamp(cAX + _dropletSepDp * scale + _dragOffsetPx.X, rB + 4f, aw - rB - 4f);
            float cBY = Math.Clamp(cAY + _dragOffsetPx.Y, rB + 4f, ah - rB - 4f);
            return
            [
                new SeaGlassSdfEngine.SdfNode(
                    centerX: cAX, centerY: cAY, halfWidth: rA, halfHeight: rA, uniformCornerRadius: rA,
                    cornerSmoothing: 2.2f, tintColorArgb: CustomPrimaryMaterial.TintColorArgb, thicknessPx: 34f * scale,
                    startShapeKind: SeaGlassShapeKind.Squircle, endShapeKind: SeaGlassShapeKind.Cookie9, shapeMorphProgress: morph),
                new SeaGlassSdfEngine.SdfNode(
                    centerX: cBX, centerY: cBY, halfWidth: rB, halfHeight: rB, uniformCornerRadius: rB,
                    cornerSmoothing: 2.2f, tintColorArgb: CustomTertiaryMaterial.TintColorArgb, thicknessPx: 32f * scale,
                    startShapeKind: SeaGlassShapeKind.Circle, endShapeKind: SeaGlassShapeKind.Flower, shapeMorphProgress: morph)
            ];
        }

        SeaGlassVisualControl arenaCtrl = new(
            surfaceModel: arenaSurface,
            stageBackdrop: _stageBackdropVisuals,
            stageCoordinateRoot: _stageRootGrid,
            customStepEvaluator: dt =>
            {
                double w = arenaSurface.SizePx.X > 10 ? arenaSurface.SizePx.X : width;
                double h = arenaSurface.SizePx.Y > 10 ? arenaSurface.SizePx.Y : height;
                return arenaSurface.EvaluateFrame(dt, SeaGlassTheme.Current, ComputeArenaNodes(w, h), smoothRadiusDp: _smoothBridgeDp * scale);
            },
            activeNodesProvider: () => ComputeArenaNodes(arenaSurface.SizePx.X, arenaSurface.SizePx.Y),
            smoothRadiusDp: _smoothBridgeDp * scale,
            customStateSignature: () => HashCode.Combine((int)(_dropletSepDp * 100f), (int)(_dragOffsetPx.X * 10f), (int)(_dragOffsetPx.Y * 10f), (int)(_smoothBridgeDp * 10f)))
        {
            Width = width,
            Height = height,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        Point? lastDragPt = null;
        double dragTravel = 0;
        arenaCtrl.MouseLeftButtonDown += (_, e) =>
        {
            lastDragPt = e.GetPosition(arenaCtrl);
            dragTravel = 0;
            arenaCtrl.CaptureMouse();
        };
        arenaCtrl.MouseMove += (_, e) =>
        {
            if (arenaCtrl.IsMouseCaptured && lastDragPt is { } prev)
            {
                Point cur = e.GetPosition(arenaCtrl);
                Vector2 delta = new((float)(cur.X - prev.X), (float)(cur.Y - prev.Y));
                _dragOffsetPx += delta;
                dragTravel += delta.Length();
                lastDragPt = cur;
            }
        };
        arenaCtrl.MouseLeftButtonUp += (_, _) =>
        {
            arenaCtrl.ReleaseMouseCapture();
            if (dragTravel < 4.0) _coalescedPreset = !_coalescedPreset;
            lastDragPt = null;
        };
        _materialSyncCallbacks.Add(() => arenaCtrl.SmoothRadiusDp = _smoothBridgeDp * scale);
        _activeControls.Add(arenaCtrl);
        return arenaCtrl;
    }

    private FrameworkElement BuildSpecimenLens(double width, double height)
    {
        SeaGlassSurface specimenSurface = new(_stageState, CustomPrimaryMaterial, contentRole: SeaGlassContentRole.PureOpticalLens);
        _materialSyncCallbacks.Add(() => specimenSurface.Material = CustomPrimaryMaterial);

        IReadOnlyList<SeaGlassSdfEngine.SdfNode> ComputeSpecimenNodes(double w, double h)
        {
            float sw = (float)Math.Max(120, w);
            float sh = (float)Math.Max(80, h);
            float hw = sw * 0.5f - 8f;
            float hh = sh * 0.5f - 8f;
            float thick = _thicknessDp * 2.6f;
            return
            [
                // Tumbled pebble: squircle body
                new SeaGlassSdfEngine.SdfNode(
                    centerX: sw * 0.5f, centerY: sh * 0.5f, halfWidth: hw, halfHeight: hh, uniformCornerRadius: hh * 0.92f,
                    cornerSmoothing: 2.4f, tintColorArgb: CustomPrimaryMaterial.TintColorArgb, thicknessPx: thick),
                // Coincident twin node (union == body) keeps the volumetric multi-node optics path
                new SeaGlassSdfEngine.SdfNode(
                    centerX: sw * 0.5f, centerY: sh * 0.5f, halfWidth: hw, halfHeight: hh, uniformCornerRadius: hh * 0.92f,
                    cornerSmoothing: 2.4f, tintColorArgb: CustomPrimaryMaterial.TintColorArgb, thicknessPx: thick)
            ];
        }

        SeaGlassVisualControl ctrl = new(
            surfaceModel: specimenSurface,
            stageBackdrop: _stageBackdropVisuals,
            stageCoordinateRoot: _stageRootGrid,
            customStepEvaluator: dt =>
            {
                double w = specimenSurface.SizePx.X > 10 ? specimenSurface.SizePx.X : width;
                double h = specimenSurface.SizePx.Y > 10 ? specimenSurface.SizePx.Y : height;
                return specimenSurface.EvaluateFrame(dt, SeaGlassTheme.Current, ComputeSpecimenNodes(w, h), smoothRadiusDp: 18f);
            },
            activeNodesProvider: () => ComputeSpecimenNodes(specimenSurface.SizePx.X, specimenSurface.SizePx.Y),
            smoothRadiusDp: 18f,
            customStateSignature: () => HashCode.Combine((int)(_thicknessDp * 10f), GlobalOpticsRevision),
            interactiveHoverLift: true)
        {
            Width = width,
            Height = height
        };
        _activeControls.Add(ctrl);
        return ctrl;
    }

    private FrameworkElement BuildMediaDeckControl()
    {
        SeaGlassSurface deckSurface = new(
            stageState: _stageState,
            material: CustomPrimaryMaterial,
            onClick: () =>
            {
                _heroFlowState = (_heroFlowState + 1) % 3;
                _miniTrackIndex = (_miniTrackIndex + 1) % _miniTracks.Length;
                _miniPlayerBar.Title = _miniTracks[_miniTrackIndex].Title;
                _miniPlayerBar.Subtitle = _miniTracks[_miniTrackIndex].Artist;
            });
        _materialSyncCallbacks.Add(() => deckSurface.Material = CustomPrimaryMaterial);
        const float orbR = 34f;

        IReadOnlyList<SeaGlassSdfEngine.SdfNode> ComputeDeckNodes(double w, double h)
        {
            float deckW = (float)Math.Max(260.0, w);
            float deckH = (float)Math.Max(64.0, h);
            float capW = MathF.Max(orbR * 3f, deckW - orbR * 2f - MathF.Max(0f, _playOrbGapDp) - 6f);
            float cy = deckH * 0.5f;
            float orbCX = MathF.Min(deckW - orbR - 4f, capW + _playOrbGapDp + orbR);
            return
            [
                new SeaGlassSdfEngine.SdfNode(
                    centerX: capW * 0.5f, centerY: cy, halfWidth: capW * 0.5f, halfHeight: deckH * 0.46f,
                    uniformCornerRadius: deckH * 0.46f, cornerSmoothing: 2.0f,
                    tintColorArgb: CustomPrimaryMaterial.TintColorArgb, thicknessPx: CustomPrimaryMaterial.ThicknessDp * 2.2f),
                new SeaGlassSdfEngine.SdfNode(
                    centerX: orbCX, centerY: cy, halfWidth: orbR, halfHeight: orbR,
                    uniformCornerRadius: orbR, cornerSmoothing: 2.2f,
                    tintColorArgb: CustomTertiaryMaterial.TintColorArgb, thicknessPx: CustomTertiaryMaterial.ThicknessDp * 2.2f)
            ];
        }

        SeaGlassVisualControl deckCtrl = new(
            surfaceModel: deckSurface,
            stageBackdrop: _stageBackdropVisuals,
            stageCoordinateRoot: _stageRootGrid,
            interactiveHoverLift: true,
            customStepEvaluator: dt =>
            {
                double w = deckSurface.SizePx.X > 10 ? deckSurface.SizePx.X : 480;
                double h = deckSurface.SizePx.Y > 10 ? deckSurface.SizePx.Y : 72;
                return deckSurface.EvaluateFrame(dt, SeaGlassTheme.Current, ComputeDeckNodes(w, h), smoothRadiusDp: 36f);
            },
            customOverlayPainter: (dc, frame, w, h) =>
            {
                int c = frame.OptimalContentColorArgb;
                LinearGradientBrush art = new(Color.FromRgb(0xF0, 0x68, 0x4E), Color.FromRgb(0xC8, 0x3E, 0x5B), new Point(0, 0), new Point(1, 1));
                art.Freeze();
                dc.DrawRoundedRectangle(art, Stroke(unchecked((int)0xFFFFFFFF), 0.7f, 1.0), new Rect(16, (h - 42) * 0.5, 42, 42), 9, 9);
                dc.DrawEllipse(Brushes.White, null, new Point(37, h * 0.5), 3.5, 3.5);

                StudioText.Draw(dc, _miniPlayerBar.Title, 70, h * 0.5 - 19, 13.5, c, StudioText.Style.Semibold);
                StudioText.Draw(dc, $"{_miniPlayerBar.Subtitle}  ·  Spatial Audio", 70, h * 0.5 + 1, 11.5, c, StudioText.Style.Regular, 0.78f);

                float capW = MathF.Max(orbR * 3f, (float)w - orbR * 2f - MathF.Max(0f, _playOrbGapDp) - 6f);
                float orbCX = MathF.Min((float)w - orbR - 4f, capW + _playOrbGapDp + orbR);
                double waveX = capW - 50;
                double[] bars = [10, 20, 14, 24, 12];
                SolidColorBrush wave = Fill(c, 0.82f);
                for (int i = 0; i < bars.Length; i++)
                {
                    double bh = _heroFlowState != 2 ? bars[i] : 6;
                    dc.DrawRoundedRectangle(wave, null, new Rect(waveX + i * 6.5, (h - bh) * 0.5, 3.2, bh), 1.6, 1.6);
                }
                SeaGlassXamlRenderer.DrawGlyph(dc, _heroFlowState != 2 ? SeaGlassGlyph.Pause : SeaGlassGlyph.Play, new Rect(orbCX - 9, h * 0.5 - 9, 18, 18), c);
            },
            activeNodesProvider: () => ComputeDeckNodes(deckSurface.SizePx.X, deckSurface.SizePx.Y),
            smoothRadiusDp: 36f,
            customStateSignature: () => HashCode.Combine(_heroFlowState, (int)(_playOrbGapDp * 200f), _miniTrackIndex))
        {
            Width = 470,
            Height = 72,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _activeControls.Add(deckCtrl);
        return deckCtrl;
    }

    private FrameworkElement BuildHudPill(string text, double width)
    {
        SeaGlassSurface hud = new(_stageState, DesktopCardMaterial with { CornerRadiusDp = 18f }, contentRole: SeaGlassContentRole.NavigationChrome)
        {
            CornerAdaptationMode = SeaGlassCornerAdaptationMode.Fixed
        };
        _materialSyncCallbacks.Add(() => hud.SnapMaterial(DesktopCardMaterial with { CornerRadiusDp = 18f }));
        SeaGlassVisualControl ctrl = CreateGlassControl(hud, width, 36, (dc, frame, w, h) =>
            StudioText.DrawCentered(dc, text, new Point(w * 0.5, h * 0.5), 12, frame.OptimalContentColorArgb, StudioText.Style.Regular, 0.9f));
        ctrl.HorizontalAlignment = HorizontalAlignment.Center;
        ctrl.IsHitTestVisible = false;
        return ctrl;
    }

    /// <summary>
    /// A realistic glass flyout (Quick Settings) anchored beneath the floating toolbar — the place
    /// where switches, checkboxes, radios, segmented controls, and progress actually live on desktop.
    /// </summary>
    private UIElement BuildQuickSettingsFlyout()
    {
        const double fw = 372;
        const double fh = 500;
        SeaGlassSurface flyout = new(_stageState, DesktopCardMaterial with { CornerRadiusDp = 26f }, contentRole: SeaGlassContentRole.NavigationChrome)
        {
            CornerAdaptationMode = SeaGlassCornerAdaptationMode.Fixed
        };
        _materialSyncCallbacks.Add(() => flyout.SnapMaterial(DesktopCardMaterial with { CornerRadiusDp = 26f }));

        (string Label, double Y)[] labelRows =
        [
            ("Live refraction", 104), ("Reduce motion", 144),
            ("Surface grain", 196), ("Chalky rim glow", 236),
            ("Follow palette tint", 288), ("Always clear glass", 328)
        ];

        SeaGlassVisualControl panel = CreateGlassControl(flyout, fw, fh, (dc, frame, w, h) =>
        {
            int c = frame.OptimalContentColorArgb;
            StudioText.DrawInRow(dc, "Quick Settings", 18, 0, 48, 14, c, StudioText.Style.Display);
            StudioText.DrawRightInRow(dc, "Esc", w - 18, 0, 48, 11, c, StudioText.Style.Mono, 0.45f);

            for (int i = 0; i < labelRows.Length; i++)
            {
                double x = i < 2 ? 18 : 50;
                StudioText.DrawInRow(dc, labelRows[i].Label, x, labelRows[i].Y, 40, 13, c, StudioText.Style.Regular, 0.92f);
            }

            SolidColorBrush div = Fill(c, 0.10f);
            foreach (double dy in new[] { 98.0, 190.0, 282.0, 374.0, 442.0 })
            {
                dc.DrawRectangle(div, null, new Rect(14, dy, w - 28, 1));
            }

            StudioText.Draw(dc, "Rendering preview", 18, 386, 13, c, StudioText.Style.Regular, 0.92f);
            double barW = w - 18 - 76;
            dc.DrawRoundedRectangle(Fill(c, 0.14f), null, new Rect(18, 416, barW, 4), 2, 2);
            dc.DrawRoundedRectangle(Fill(AccentFill), null, new Rect(18, 416, Math.Max(4, barW * _linearProgress.AnimatedProgress), 4), 2, 2);
        });
        panel.CustomStateSignature = () => (int)(_linearProgress.AnimatedProgress * 500f);
        panel.HorizontalAlignment = HorizontalAlignment.Center;
        panel.VerticalAlignment = VerticalAlignment.Top;
        panel.Margin = new Thickness(0, 84, 0, 0);

        Grid inner = new();
        void Place(UIElement el, HorizontalAlignment ha, double left, double top, double right = 0)
        {
            if (el is FrameworkElement fe)
            {
                fe.HorizontalAlignment = ha;
                fe.VerticalAlignment = VerticalAlignment.Top;
                fe.Margin = new Thickness(left, top, right, 0);
            }
            inner.Children.Add(el);
        }

        Place(BuildSegmentedControl(fw - 36, 32), HorizontalAlignment.Left, 18, 54);
        Place(BuildInteractiveSwitchControl(_switchA), HorizontalAlignment.Right, 0, 104 + 7, 18);
        Place(BuildInteractiveSwitchControl(_switchB), HorizontalAlignment.Right, 0, 144 + 7, 18);
        Place(BuildInteractiveCheckboxControl(_checkA), HorizontalAlignment.Left, 18, 196 + 8);
        Place(BuildInteractiveCheckboxControl(_checkB), HorizontalAlignment.Left, 18, 236 + 8);
        Place(BuildInteractiveRadioControl(_radioA), HorizontalAlignment.Left, 17, 288 + 7);
        Place(BuildInteractiveRadioControl(_radioB), HorizontalAlignment.Left, 17, 328 + 7);
        Place(BuildInteractiveCircularProgressControl(), HorizontalAlignment.Right, 0, 384, 16);

        StackPanel footer = new() { Orientation = Orientation.Horizontal };
        footer.Children.Add(CreateDesktopButtonControl(new SeaGlassPill("Reset", false, () =>
        {
            float p = _segmentedRow.SelectedIndex switch { 0 => 0.74f, 1 => 0.48f, _ => 0.92f };
            _linearProgress.Progress = p;
            _circularProgress.Progress = p;
        }, _stageState), height: 30, margin: new Thickness(0, 0, 8, 0), width: 76));
        footer.Children.Add(CreateDesktopButtonControl(new SeaGlassPill("Apply", true, () =>
        {
            _linearProgress.Progress = 1f;
            _circularProgress.Progress = 1f;
        }, _stageState), height: 30, width: 84));
        Place(footer, HorizontalAlignment.Right, 0, 456, 16);

        panel.Child = inner;
        return panel;
    }

    // ── Code editor (Shader Code page) ──────────────────────────────────────────

    private static readonly HashSet<string> ShaderKeywords =
    [
        "cbuffer", "struct", "uniform", "layout", "in", "out", "inout", "return", "if", "else", "for", "const",
        "static", "register", "precision", "highp", "mediump", "lowp", "binding", "std140", "set", "location"
    ];

    private static readonly HashSet<string> ShaderTypes =
    [
        "float", "float2", "float3", "float4", "float4x4", "half", "half2", "half3", "half4", "vec2", "vec3", "vec4",
        "mat2", "mat3", "mat4", "int", "uint", "bool", "void", "sampler2D", "Texture2D", "SamplerState", "shader"
    ];

    private readonly Dictionary<ShaderDialect, List<FormattedText>> _codeLineCache = new();

    private List<FormattedText> GetHighlightedLines(ShaderDialect dialect)
    {
        if (_codeLineCache.TryGetValue(dialect, out List<FormattedText>? cached)) return cached;

        Typeface mono = new(new FontFamily("Cascadia Mono, Consolas"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        SolidColorBrush Br(uint argb) { SolidColorBrush b = new(Color.FromArgb((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb)); b.Freeze(); return b; }
        SolidColorBrush plain = Br(0xFFD4D7DD), keyword = Br(0xFF7AA2F7), type = Br(0xFF5FD4C4), number = Br(0xFFE0AF68),
            comment = Br(0xFF6E7A8A), preproc = Br(0xFFC792EA), call = Br(0xFFE6D58C);

        string source = SeaGlassShaderCompiler.CompileForDialect(dialect).SeaGlassCompositeFragmentSource.Replace("\r", "");
        List<FormattedText> lines = new();
        foreach (string raw in source.Split('\n'))
        {
            string line = raw.Replace("\t", "    ");
            FormattedText ft = new(line.Length == 0 ? " " : line, System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, mono, 12.5, plain, 1.25);
            int i = 0;
            string trimmed = line.TrimStart();
            if (trimmed.StartsWith('#'))
            {
                ft.SetForegroundBrush(preproc, 0, line.Length);
                lines.Add(ft);
                continue;
            }
            while (i < line.Length)
            {
                char ch = line[i];
                if (ch == '/' && i + 1 < line.Length && line[i + 1] == '/')
                {
                    ft.SetForegroundBrush(comment, i, line.Length - i);
                    break;
                }
                if (char.IsLetter(ch) || ch == '_')
                {
                    int start = i;
                    while (i < line.Length && (char.IsLetterOrDigit(line[i]) || line[i] == '_')) i++;
                    string word = line[start..i];
                    int next = i;
                    while (next < line.Length && line[next] == ' ') next++;
                    if (ShaderKeywords.Contains(word)) ft.SetForegroundBrush(keyword, start, i - start);
                    else if (ShaderTypes.Contains(word)) ft.SetForegroundBrush(type, start, i - start);
                    else if (next < line.Length && line[next] == '(') ft.SetForegroundBrush(call, start, i - start);
                    continue;
                }
                if (char.IsDigit(ch))
                {
                    int start = i;
                    while (i < line.Length && (char.IsLetterOrDigit(line[i]) || line[i] == '.')) i++;
                    ft.SetForegroundBrush(number, start, i - start);
                    continue;
                }
                i++;
            }
            lines.Add(ft);
        }
        _codeLineCache[dialect] = lines;
        return lines;
    }

    private static string DialectTabLabel(ShaderDialect d) => d switch
    {
        ShaderDialect.HlslSm50 => "SeaGlass.hlsl",
        ShaderDialect.D2D1Hlsl => "SeaGlass.d2d1.hlsl",
        ShaderDialect.VulkanGlsl450 => "seaglass.frag (450)",
        ShaderDialect.GlslEs300 => "seaglass.frag (ES 3.0)",
        _ => "seaglass.agsl"
    };

    private UIElement BuildCodeEditor()
    {
        ShaderDialect[] dialects = Enum.GetValues<ShaderDialect>();
        const double tabH = 38;
        const double lineH = 19;
        List<double> tabEdges = new();

        StudioPaintHost editor = null!;
        editor = NewPaintHost((dc, w, h) =>
        {
            int fg = unchecked((int)0xFFE6E9EF);
            tabEdges.Clear();
            double x = 12;
            for (int i = 0; i < dialects.Length; i++)
            {
                FormattedText ft = StudioText.Get(DialectTabLabel(dialects[i]), 12, fg, StudioText.Style.Regular, dialects[i] == _selectedDialect ? 1f : 0.55f);
                double tw = ft.Width + 28;
                bool sel = dialects[i] == _selectedDialect;
                if (sel)
                {
                    dc.DrawRectangle(Fill(unchecked((int)0xFFFFFFFF), 0.07f), null, new Rect(x, 0, tw, tabH));
                    dc.DrawRectangle(Fill(CustomPrimaryMaterial.TintColorArgb), null, new Rect(x + 8, tabH - 2, tw - 16, 2));
                }
                dc.DrawText(ft, new Point(x + 14, (tabH - ft.Height) * 0.5));
                tabEdges.Add(x);
                x += tw;
            }
            tabEdges.Add(x);

            List<FormattedText> lines = GetHighlightedLines(_selectedDialect);
            int visible = (int)((h - tabH - 16) / lineH);
            _codeScrollLine = Math.Clamp(_codeScrollLine, 0, Math.Max(0, lines.Count - visible));
            dc.PushClip(new RectangleGeometry(new Rect(0, tabH, w, h - tabH)));
            for (int row = 0; row < visible && row + _codeScrollLine < lines.Count; row++)
            {
                int ln = row + _codeScrollLine;
                double y = tabH + 12 + row * lineH;
                StudioText.DrawRight(dc, (ln + 1).ToString(), 44, y, 12, fg, StudioText.Style.Mono, 0.32f);
                dc.DrawText(lines[ln], new Point(60, y));
            }
            dc.Pop();
        }, () => HashCode.Combine(_selectedDialect, _codeScrollLine));

        editor.MouseLeftButtonUp += (_, e) =>
        {
            Point p = e.GetPosition(editor);
            if (p.Y > tabH) return;
            for (int i = 0; i + 1 < tabEdges.Count; i++)
            {
                if (p.X >= tabEdges[i] && p.X < tabEdges[i + 1])
                {
                    _selectedDialect = dialects[i];
                    _codeScrollLine = 0;
                    RebuildUI();
                    return;
                }
            }
        };
        editor.MouseWheel += (_, e) =>
        {
            _codeScrollLine = Math.Max(0, _codeScrollLine - Math.Sign(e.Delta) * 3);
            editor.InvalidateVisual();
        };
        editor.Cursor = Cursors.IBeam;
        return editor;
    }

    // ── Inspector (flat property rows) ──────────────────────────────────────────

    private UIElement BuildInspector()
    {
        StackPanel rows = new() { Margin = new Thickness(0, 4, 0, 16) };
        SeaGlassPalette palette = SeaGlassTheme.Current.Palette;

        switch (_page)
        {
            case 0:
                rows.Children.Add(InspectorPaletteSummary(palette));
                SeaGlassSwitch porcelainSwitch = new(_porcelainDaylight, v =>
                {
                    _porcelainDaylight = v;
                    RebuildUI();
                }, _stageState, CustomPrimaryMaterial, DesktopCardMaterial);
                rows.Children.Add(InspectorToggle("Porcelain Studio stage", porcelainSwitch));
                rows.Children.Add(InspectorHeader("Optics"));
                rows.Children.Add(InspectorSlider("Transparency", () => $"{_transparency * 100f:F0}%", () => (_transparency - 0.08f) / 0.87f, f => _transparency = 0.08f + f * 0.87f));
                rows.Children.Add(InspectorSlider("Tint density", () => $"{_tintDensity * 100f:F0}%", () => (_tintDensity - 0.18f) / 0.77f, f => _tintDensity = 0.18f + f * 0.77f));
                rows.Children.Add(InspectorSlider("Sand coarseness", () => $"{_coarseness * 100f:F0}%", () => _coarseness, f => _coarseness = f));
                rows.Children.Add(InspectorSlider("Refraction index", () => $"{_ior:F2}", () => (_ior - 1.08f) / 0.72f, f => _ior = 1.08f + f * 0.72f));
                rows.Children.Add(InspectorSlider("Dispersion", () => $"{_chromaticDispersion * 100f:F0}%", () => _chromaticDispersion, f => _chromaticDispersion = f));
                rows.Children.Add(InspectorSlider("Thickness", () => $"{_thicknessDp:F0} dp", () => (_thicknessDp - 6f) / 22f, f => _thicknessDp = 6f + f * 22f));
                rows.Children.Add(InspectorSlider("Rim chalkiness", () => $"{_rimChalkiness * 100f:F0}%", () => _rimChalkiness, f => _rimChalkiness = f));
                break;

            case 1:
                rows.Children.Add(InspectorHeader("Coalescing", divider: false));
                rows.Children.Add(InspectorSlider("Bridge radius (k)", () => $"{_smoothBridgeDp:F0} dp", () => (_smoothBridgeDp - 8f) / 52f, f => _smoothBridgeDp = 8f + f * 52f));
                SeaGlassSwitch mergeSwitch = new(_coalescedPreset, v => _coalescedPreset = v, _stageState, CustomPrimaryMaterial, DesktopCardMaterial);
                rows.Children.Add(InspectorToggle("Merge droplets", mergeSwitch));
                rows.Children.Add(InspectorButtons(
                    (new SeaGlassPill("Reset position", false, () => _dragOffsetPx = Vector2.Zero, _stageState), 116)));
                rows.Children.Add(InspectorHeader("Nodes"));
                rows.Children.Add(InspectorValue("Anchor", () => "Squircle"));
                rows.Children.Add(InspectorValue("Droplet", () => "Circle"));
                rows.Children.Add(InspectorValue("Separation", () => $"{_dropletSepDp:F0} dp", mono: true));
                rows.Children.Add(InspectorValue("Drag offset", () => $"{_dragOffsetPx.X:F0}, {_dragOffsetPx.Y:F0}", mono: true));
                rows.Children.Add(InspectorHeader("Field"));
                rows.Children.Add(InspectorValue("Union", () => "sminCubic"));
                rows.Children.Add(InspectorValue("Bridge spring", () => "ζ 0.68 · k 420", mono: true));
                break;

            case 2:
                rows.Children.Add(InspectorHeader("Component state", divider: false));
                rows.Children.Add(InspectorValue("Segment", () => _segmentedRow.Options[_segmentedRow.SelectedIndex]));
                rows.Children.Add(InspectorValue("Live refraction", () => _switchA.Checked ? "On" : "Off"));
                rows.Children.Add(InspectorValue("Reduce motion", () => _switchB.Checked ? "On" : "Off"));
                rows.Children.Add(InspectorValue("Surface grain", () => _checkA.Checked ? "Checked" : "Unchecked"));
                rows.Children.Add(InspectorValue("Rim glow", () => _checkB.Checked ? "Checked" : "Unchecked"));
                rows.Children.Add(InspectorValue("Glass tint", () => _radioA.Selected ? "Follow palette" : "Always clear"));
                rows.Children.Add(InspectorSlider("Progress", () => $"{_linearProgress.Progress * 100f:F0}%", () => _linearProgress.Progress, f =>
                {
                    _linearProgress.Progress = Math.Clamp(f, 0.04f, 1f);
                    _circularProgress.Progress = _linearProgress.Progress;
                }, syncMaterials: false));
                rows.Children.Add(InspectorHeader("Motion"));
                rows.Children.Add(InspectorValue("Spatial spring", () => "ζ 0.68 · k 540", mono: true));
                rows.Children.Add(InspectorValue("Effects spring", () => "ζ 1.00 · k 900", mono: true));
                rows.Children.Add(InspectorValue("Hover lift", () => "ζ 0.85 · k 460", mono: true));
                break;

            default:
                rows.Children.Add(InspectorHeader("Dialect", divider: false));
                foreach (ShaderDialect d in Enum.GetValues<ShaderDialect>())
                {
                    rows.Children.Add(InspectorChoice(d.DisplayName(), () => _selectedDialect == d, () =>
                    {
                        _selectedDialect = d;
                        _codeScrollLine = 0;
                        RebuildUI();
                    }));
                }
                rows.Children.Add(InspectorHeader("Uniform block · 256 bytes"));
                (string Name, int Offset, int Size)[] ubo =
                [
                    ("uResolutionAndOrigin", 0, 16), ("uOpticsPrimary", 16, 16), ("uOpticsSecondary", 32, 16),
                    ("uLightAndMeta", 48, 16), ("uNodeBounds[4]", 64, 64), ("uNodeCorners[4]", 128, 64), ("uNodeTints[4]", 192, 64)
                ];
                foreach (var f in ubo)
                {
                    rows.Children.Add(InspectorValue($"+{f.Offset,3}  {f.Name}", () => $"{f.Size} B", mono: true, monoLabel: true));
                }
                rows.Children.Add(InspectorHeader("Backend"));
                rows.Children.Add(InspectorValue("Tier", () => "D2D1_HLSL_FULL", mono: true));
                rows.Children.Add(InspectorValue("Layout", () => "std140 · 16 × float4", mono: true));
                rows.Children.Add(InspectorValue("Lines", () => GetHighlightedLines(_selectedDialect).Count.ToString(), mono: true));
                break;
        }

        ScrollViewer scroll = new()
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = rows
        };
        scroll.ScrollChanged += (_, _) => MarkAllControlsOriginDirty();
        return scroll;
    }

    private FrameworkElement InspectorHeader(string title, bool divider = true)
    {
        StudioPaintHost host = NewPaintHost((dc, w, h) =>
        {
            if (divider) dc.DrawRectangle(Fill(Ink, IsDarkTheme ? 0.10f : 0.08f), null, new Rect(16, 6, w - 32, 1));
            StudioText.Draw(dc, title, 16, h - 26, 13, Ink, StudioText.Style.Semibold);
        }, () => HashCode.Combine(IsDarkTheme));
        host.Height = divider ? 46 : 38;
        return host;
    }

    private FrameworkElement InspectorPaletteSummary(SeaGlassPalette palette)
    {
        StudioPaintHost host = NewPaintHost((dc, w, h) =>
        {
            int ink = Ink;
            StudioText.Draw(dc, palette.DisplayName, 16, 10, 16, ink, StudioText.Style.Display);
            FormattedText tag = new(palette.Tagline, System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI Variable Text, Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                12, Fill(ink, 0.62f), 1.25)
            {
                MaxTextWidth = w - 32,
                MaxLineCount = 2,
                Trimming = TextTrimming.CharacterEllipsis
            };
            dc.DrawText(tag, new Point(16, 36));

            (string Name, int Argb)[] chips = [("Primary", palette.PrimaryGlass.TintColorArgb), ("Secondary", palette.SecondaryGlass.TintColorArgb), ("Tertiary", palette.TertiaryGlass.TintColorArgb)];
            double colW = (w - 32) / 3.0;
            for (int i = 0; i < chips.Length; i++)
            {
                double x = 16 + i * colW;
                dc.DrawRoundedRectangle(Fill(chips[i].Argb), Stroke(ink, 0.14f, 1.0), new Rect(x, 82, 22, 22), 5, 5);
                StudioText.Draw(dc, chips[i].Name, x + 30, 79, 11, ink, StudioText.Style.Regular, 0.6f);
                StudioText.Draw(dc, $"#{chips[i].Argb & 0xFFFFFF:X6}", x + 30, 93, 11, ink, StudioText.Style.Mono, 0.86f);
            }
        }, () => HashCode.Combine(palette.Id));
        host.Height = 118;
        return host;
    }

    private FrameworkElement InspectorSlider(string label, Func<string> valueText, Func<float> getNorm, Action<float> setNorm, bool syncMaterials = true)
    {
        StudioPaintHost host = null!;
        host = NewPaintHost((dc, w, h) =>
        {
            int ink = Ink;
            float v = Math.Clamp(getNorm(), 0f, 1f);
            StudioText.DrawAtBaseline(dc, label, 16, 20, 12.5, ink, StudioText.Style.Regular, 0.9f);
            StudioText.DrawAtBaseline(dc, valueText(), w - 16, 20, 11.5, ink, StudioText.Style.Mono, 0.6f, rightAligned: true);

            double tx = 16, tw = w - 32, ty = 36;
            dc.DrawRoundedRectangle(Fill(ink, IsDarkTheme ? 0.22f : 0.16f), null, new Rect(tx, ty - 2, tw, 4), 2, 2);
            dc.DrawRoundedRectangle(Fill(AccentFill), null, new Rect(tx, ty - 2, Math.Max(4, tw * v), 4), 2, 2);

            Point thumb = new(tx + tw * v, ty);
            bool active = host.IsMouseCaptured || host.IsHovered;
            dc.DrawEllipse(Fill(unchecked((int)0xFF000000), 0.10f), null, new Point(thumb.X, thumb.Y + 1), 10, 10);
            dc.DrawEllipse(Fill(IsDarkTheme ? unchecked((int)0xFF454951) : unchecked((int)0xFFFFFFFF)), Stroke(ink, 0.12f, 1.0), thumb, 9.5, 9.5);
            double inner = host.IsMouseCaptured ? 4.0 : active ? 6.0 : 5.0;
            dc.DrawEllipse(Fill(AccentFill), null, thumb, inner, inner);
        });
        host.Signature = () => HashCode.Combine((int)(getNorm() * 2000f), host.IsMouseCaptured, IsDarkTheme, GlobalOpticsRevision);
        host.Height = 52;

        void Apply(Point p)
        {
            double tw = Math.Max(10, host.ActualWidth - 32);
            setNorm((float)Math.Clamp((p.X - 16) / tw, 0.0, 1.0));
            if (syncMaterials) SyncComponentMaterials();
            host.InvalidateVisual();
        }
        host.MouseLeftButtonDown += (_, e) => { host.CaptureMouse(); Apply(e.GetPosition(host)); e.Handled = true; };
        host.MouseMove += (_, e) => { if (host.IsMouseCaptured) Apply(e.GetPosition(host)); };
        host.MouseLeftButtonUp += (_, _) => { host.ReleaseMouseCapture(); host.InvalidateVisual(); };
        return host;
    }

    private FrameworkElement InspectorValue(string label, Func<string> value, bool mono = false, bool monoLabel = false)
    {
        StudioPaintHost host = NewPaintHost((dc, w, h) =>
        {
            int ink = Ink;
            StudioText.DrawInRow(dc, label, 16, 0, h, monoLabel ? 11.5 : 12.5, ink, monoLabel ? StudioText.Style.Mono : StudioText.Style.Regular, 0.72f);
            StudioText.DrawRightInRow(dc, value(), w - 16, 0, h, mono ? 11.5 : 12.5, ink, mono ? StudioText.Style.Mono : StudioText.Style.Regular, 0.95f);
        }, () => HashCode.Combine(value(), IsDarkTheme));
        host.Height = 30;
        return host;
    }

    private FrameworkElement InspectorChoice(string label, Func<bool> isSelected, Action select)
    {
        StudioPaintHost host = null!;
        host = NewPaintHost((dc, w, h) =>
        {
            int ink = Ink;
            bool sel = isSelected();
            Rect row = new(8, 1, w - 16, h - 2);
            if (sel) dc.DrawRoundedRectangle(Fill(ink, IsDarkTheme ? 0.09f : 0.06f), null, row, 5, 5);
            else if (host.IsHovered) dc.DrawRoundedRectangle(Fill(ink, IsDarkTheme ? 0.05f : 0.035f), null, row, 5, 5);
            StudioText.DrawInRow(dc, label, 16, 0, h, 12.5, ink, sel ? StudioText.Style.Semibold : StudioText.Style.Regular, sel ? 1f : 0.82f);
            if (sel) SeaGlassXamlRenderer.DrawGlyph(dc, SeaGlassGlyph.Check, new Rect(w - 34, (h - 14) * 0.5, 14, 14), AccentInk);
        });
        host.Signature = () => HashCode.Combine(isSelected(), host.IsHovered, IsDarkTheme);
        host.Height = 32;
        host.MouseLeftButtonUp += (_, _) => select();
        return host;
    }

    private FrameworkElement InspectorToggle(string label, SeaGlassSwitch sw)
    {
        _materialSyncCallbacks.Add(() => { sw.CheckedMaterial = CustomPrimaryMaterial; sw.UncheckedMaterial = DesktopCardMaterial; });
        Grid row = new() { Height = 44 };
        row.Children.Add(NewPaintHost((dc, w, h) => StudioText.DrawInRow(dc, label, 16, 0, h, 12.5, Ink, StudioText.Style.Regular, 0.9f), () => HashCode.Combine(IsDarkTheme)));
        UIElement ctrl = BuildInteractiveSwitchControl(sw);
        if (ctrl is FrameworkElement fe)
        {
            fe.HorizontalAlignment = HorizontalAlignment.Right;
            fe.VerticalAlignment = VerticalAlignment.Center;
            fe.Margin = new Thickness(0, 0, 16, 0);
        }
        row.Children.Add(ctrl);
        return row;
    }

    private FrameworkElement InspectorButtons(params (SeaGlassPill Pill, double Width)[] buttons)
    {
        StackPanel row = new() { Orientation = Orientation.Horizontal, Margin = new Thickness(16, 6, 16, 8) };
        foreach (var (pill, width) in buttons)
        {
            row.Children.Add(CreateDesktopButtonControl(pill, height: 30, margin: new Thickness(0, 0, 8, 0), width: width));
        }
        return row;
    }

    // ── Status bar ──────────────────────────────────────────────────────────────

    private UIElement BuildStatusBar()
    {
        SeaGlassPalette palette = SeaGlassTheme.Current.Palette;
        return NewPaintHost((dc, w, h) =>
        {
            int ink = Ink;
            double x = 16;
            dc.DrawEllipse(Fill(AccentFill), null, new Point(x + 3.5, h * 0.5), 3.5, 3.5);
            x += 14;
            string[] left = ["D2D1_HLSL_FULL", "256-byte cbuffer", $"{_activeControls.Count} glass surfaces"];
            foreach (string item in left)
            {
                FormattedText ft = StudioText.Get(item, 11.5, ink, StudioText.Style.Regular, 0.66f);
                dc.DrawText(ft, new Point(x, (h - ft.Height) * 0.5));
                x += ft.Width + 18;
            }

            double r = w - 16;
            string[] right = [$"{_fps:F0} fps", palette.DisplayName, PageTitles[_page]];
            foreach (string item in right)
            {
                FormattedText ft = StudioText.Get(item, 11.5, ink, StudioText.Style.Regular, 0.66f);
                dc.DrawText(ft, new Point(r - ft.Width, (h - ft.Height) * 0.5));
                r -= ft.Width + 18;
            }
        }, () => HashCode.Combine((int)Math.Round(_fps), palette.Id, _page, _activeControls.Count));
    }

    private UIElement BuildSegmentedControl(double width, double height)
    {
        _segmentedRow.CornerAdaptationMode = SeaGlassCornerAdaptationMode.Fixed;
        _segmentedRow.ContainerMaterial = DesktopCardMaterial with { CornerRadiusDp = 16f };
        _segmentedRow.SnapMaterial(_segmentedRow.ContainerMaterial);

        SeaGlassVisualControl segCtrl = new(
            _segmentedRow,
            _stageBackdropVisuals,
            _stageRootGrid,
            dt => _segmentedRow.StepAndEvaluate(dt, SeaGlassTheme.Current),
            (dc, frame, w, h) =>
            {
                int c = frame.OptimalContentColorArgb;
                double slotW = (w - 6.0) / 3.0;
                double x = 3.0 + _segmentedRow.LeftEdgeSlot * slotW;
                double pillW = Math.Max(slotW * 0.85, _segmentedRow.LozengeSlotSpan * slotW);
                double yInset = 3.0 + _segmentedRow.MotionEnergy * 1.8;
                double pillH = Math.Max(18.0, h - yInset * 2.0);

                // Milky-Frosted Sea-Glass Active Segment Lozenge Pill (matching Android 32f354a)
                bool isDarkSeg = SeaGlassColorMath.ArgbToOklab(DesktopCardMaterial.TintColorArgb).L < 0.48f;
                int topArgb = isDarkSeg
                    ? CustomPrimaryMaterial.TintColorArgb
                    : SeaGlassColorMath.ComputeFrostedRindColor(CustomPrimaryMaterial.TintColorArgb, CustomPrimaryMaterial.Coarseness, 0.72f);
                int botArgb = isDarkSeg
                    ? unchecked((int)0xFF0A1E30)
                    : SeaGlassColorMath.ComputeFrostedRindColor(CustomPrimaryMaterial.TintColorArgb, CustomPrimaryMaterial.Coarseness, 0.58f);
                Color pillTop = SeaGlassXamlRenderer.ToMediaColor(topArgb, isDarkSeg ? 0.56f : 0.48f);
                Color pillBot = SeaGlassXamlRenderer.ToMediaColor(botArgb, isDarkSeg ? 0.40f : 0.38f);
                LinearGradientBrush pillBrush = new(pillTop, pillBot, new Point(0, 0), new Point(0, 1));
                pillBrush.Freeze();
                Pen pillPen = Stroke(unchecked((int)0xFFFFFFFF), isDarkSeg ? 0.32f : 0.38f, 1.0);
                dc.DrawRoundedRectangle(pillBrush, pillPen, new Rect(x, yInset, pillW, pillH), pillH * 0.5, pillH * 0.5);

                for (int i = 0; i < 3; i++)
                {
                    bool sel = i == _segmentedRow.SelectedIndex;
                    SeaGlassXamlRenderer.DrawCenteredLabel(dc, _segmentedRow.Options[i], new Point(3 + (i + 0.5) * slotW, h * 0.5), c, 11.0, bold: sel, extraBold: sel, alpha: sel ? 1f : 0.72f);
                }
            },
            customStateSignature: () => HashCode.Combine((int)(_segmentedRow.LeftEdgeSlot * 500f), (int)(_segmentedRow.LozengeSlotSpan * 500f), _segmentedRow.SelectedIndex))
        {
            Width = width,
            Height = height
        };
        _activeControls.Add(segCtrl);
        return segCtrl;
    }

    private UIElement BuildInteractiveSwitchControl(SeaGlassSwitch sw)
    {
        SeaGlassVisualControl ctrl = new(
            sw,
            _stageBackdropVisuals,
            _stageRootGrid,
            dt => sw.StepAndEvaluate(dt, SeaGlassTheme.Current),
            (dc, frame, w, h) =>
            {
                int c = frame.OptimalContentColorArgb;
                Pen trackBorder = new(new SolidColorBrush(SeaGlassXamlRenderer.ToMediaColor(c, sw.Checked ? 0.28f : 0.42f)), 1.2);
                trackBorder.Freeze();
                dc.DrawRoundedRectangle(null, trackBorder, new Rect(1.5, 1.5, w - 3.0, h - 3.0), (h - 3.0) * 0.5, (h - 3.0) * 0.5);

                double thumbW = 20.0 * sw.ThumbScaleX;
                double thumbH = 18.0 * sw.ThumbScaleY;
                double travel = Math.Max(4.0, w - 26.0);
                double tx = 4.0 + travel * Math.Clamp(sw.ThumbProgress, -0.06f, 1.06f);
                double ty = (h - thumbH) * 0.5;

                SolidColorBrush thumbBrush = new(Colors.White);
                thumbBrush.Freeze();
                Pen border = new(new SolidColorBrush(Color.FromArgb(80, 0, 0, 0)), 0.8);
                border.Freeze();
                dc.DrawRoundedRectangle(thumbBrush, border, new Rect(tx, ty, thumbW, thumbH), thumbH * 0.5, thumbH * 0.5);
            },
            customStateSignature: () => HashCode.Combine((int)(sw.ThumbProgress * 500f), (int)(sw.ThumbScaleX * 500f), sw.Checked),
            interactiveHoverLift: true)
        {
            Width = 48,
            Height = 26
        };
        _activeControls.Add(ctrl);
        return ctrl;
    }

    private UIElement BuildInteractiveCheckboxControl(SeaGlassCheckbox cb)
    {
        cb.UseDesktopSquareContour = false;
        cb.CornerAdaptationMode = SeaGlassCornerAdaptationMode.Fixed;
        cb.CheckedMaterial = CustomPrimaryMaterial with { CornerRadiusDp = 8f };
        cb.UncheckedMaterial = DesktopCardMaterial with { CornerRadiusDp = 8f };
        cb.SnapMaterial(cb.Checked ? cb.CheckedMaterial : cb.UncheckedMaterial);

        SeaGlassVisualControl ctrl = new(
            cb,
            _stageBackdropVisuals,
            _stageRootGrid,
            dt => cb.StepAndEvaluate(dt, SeaGlassTheme.Current),
            (dc, frame, w, h) =>
            {
                int c = frame.OptimalContentColorArgb;
                if (cb.MorphProgress <= 0.15f)
                {
                    Pen wellPen = new(new SolidColorBrush(SeaGlassXamlRenderer.ToMediaColor(c, 0.36f)), 1.2);
                    wellPen.Freeze();
                    dc.DrawRoundedRectangle(null, wellPen, new Rect(1.5, 1.5, w - 3, h - 3), 7, 7);
                }
                else
                {
                    SeaGlassXamlRenderer.DrawGlyph(dc, SeaGlassGlyph.Check, new Rect(w * 0.5 - 6.5, h * 0.5 - 6.5, 13, 13), c);
                }
            },
            customStateSignature: () => (int)(cb.MorphProgress * 500f),
            interactiveHoverLift: true)
        {
            Width = 24,
            Height = 24
        };
        _activeControls.Add(ctrl);
        return ctrl;
    }

    private UIElement BuildInteractiveRadioControl(SeaGlassRadioButton rb)
    {
        SeaGlassVisualControl ctrl = new(
            rb,
            _stageBackdropVisuals,
            _stageRootGrid,
            dt => rb.StepAndEvaluate(dt, SeaGlassTheme.Current),
            (dc, frame, w, h) =>
            {
                int c = frame.OptimalContentColorArgb;
                Pen wellPen = new(new SolidColorBrush(SeaGlassXamlRenderer.ToMediaColor(c, rb.Selected ? 0.32f : 0.42f)), 1.3);
                wellPen.Freeze();
                dc.DrawEllipse(null, wellPen, new Point(w * 0.5, h * 0.5), w * 0.5 - 2.0, h * 0.5 - 2.0);

                double r = 4.5 * rb.InnerDropletScale;
                if (r > 0.5)
                {
                    dc.DrawEllipse(Fill(unchecked((int)0xFF000000), 0.14f), null, new Point(w * 0.5, h * 0.5 + 0.6), r + 0.8, r + 0.8);
                    dc.DrawEllipse(Brushes.White, null, new Point(w * 0.5, h * 0.5), r, r);
                }
            },
            customStateSignature: () => (int)(rb.InnerDropletScale * 500f),
            interactiveHoverLift: true)
        {
            Width = 26,
            Height = 26
        };
        _activeControls.Add(ctrl);
        return ctrl;
    }

    private UIElement BuildInteractiveCircularProgressControl()
    {
        SeaGlassVisualControl ctrl = new(
            _circularProgress,
            _stageBackdropVisuals,
            _stageRootGrid,
            dt => _circularProgress.StepAndEvaluate(dt, SeaGlassTheme.Current),
            (dc, frame, w, h) =>
            {
                int c = frame.OptimalContentColorArgb;
                Point center = new(w * 0.5, h * 0.5);
                double radius = Math.Min(w, h) * 0.36;
                Pen trackPen = new(new SolidColorBrush(SeaGlassXamlRenderer.ToMediaColor(c, 0.16f)), 3.8);
                trackPen.Freeze();
                Pen arcPen = new(new SolidColorBrush(SeaGlassXamlRenderer.ToMediaColor(CustomPrimaryMaterial.TintColorArgb)), 3.8)
                {
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round
                };
                arcPen.Freeze();
                dc.DrawEllipse(null, trackPen, center, radius, radius);
                DrawArcGeometry(dc, arcPen, center, radius, -90, _circularProgress.SweepAngleDegrees);

                int pct = (int)MathF.Round(_circularProgress.AnimatedProgress * 100f);
                SeaGlassXamlRenderer.DrawCenteredLabel(dc, $"{pct}%", center, c, 9.5, bold: true, extraBold: true);
            },
            customStateSignature: () => (int)(_circularProgress.SweepAngleDegrees * 20f))
        {
            Width = 46,
            Height = 46
        };
        _activeControls.Add(ctrl);
        return ctrl;
    }

    private UIElement BuildInteractiveExtendedFabControl()
    {
        _extendedFab.CornerAdaptationMode = SeaGlassCornerAdaptationMode.Fixed;
        _extendedFab.SetExpanded(_extendedFab.Expanded, CustomPrimaryMaterial with { CornerRadiusDp = 20f, ThicknessDp = 16f });
        _extendedFab.SnapMaterial(_extendedFab.Material);
        _materialSyncCallbacks.Add(() =>
        {
            _extendedFab.SetExpanded(_extendedFab.Expanded, CustomPrimaryMaterial with { CornerRadiusDp = 20f, ThicknessDp = 16f });
            _extendedFab.SnapMaterial(_extendedFab.Material);
        });
        SeaGlassVisualControl ctrl = new(
            _extendedFab,
            _stageBackdropVisuals,
            _stageRootGrid,
            dt => _extendedFab.EvaluateFrame(dt, SeaGlassTheme.Current),
            (dc, frame, w, h) =>
            {
                int c = frame.OptimalContentColorArgb;
                SeaGlassXamlRenderer.DrawGlyph(dc, SeaGlassGlyph.Sparkles, new Rect(14, h * 0.5 - 7, 14, 14), c);
                if (_extendedFab.Expanded)
                {
                    SeaGlassXamlRenderer.DrawLabel(dc, "Create", new Point(34, h * 0.5 - 7.5), c, 11.5, bold: true, extraBold: true);
                }
            },
            customStateSignature: () => _extendedFab.Expanded ? 1 : 0,
            interactiveHoverLift: true)
        {
            Width = 104,
            Height = 38
        };
        _activeControls.Add(ctrl);
        return ctrl;
    }

    private SeaGlassVisualControl CreateGlassControl(
        SeaGlassSurface model,
        double? width = null,
        double? height = null,
        Action<DrawingContext, SeaGlassSurfaceFrameState, double, double>? overlay = null,
        bool interactiveHoverLift = false)
    {
        model.SnapMaterial(model.Material);
        SeaGlassVisualControl ctrl = new(
            surfaceModel: model,
            stageBackdrop: _stageBackdropVisuals,
            stageCoordinateRoot: _stageRootGrid,
            customOverlayPainter: overlay,
            interactiveHoverLift: interactiveHoverLift);
        if (width is { } w) ctrl.Width = w;
        if (height is { } h) ctrl.Height = h;
        _activeControls.Add(ctrl);
        return ctrl;
    }

    private UIElement CreateDesktopButtonControl(SeaGlassPill pill, double height = 32, Thickness? margin = null, double? width = null)
    {
        float pillRadius = (float)(height * 0.5);
        pill.CornerAdaptationMode = SeaGlassCornerAdaptationMode.Fixed;
        pill.SelectedMaterial = CustomPrimaryMaterial with { CornerRadiusDp = pillRadius };
        pill.UnselectedMaterial = DesktopCardMaterial with { CornerRadiusDp = pillRadius };
        pill.SetSelected(pill.Selected);
        pill.SnapMaterial(pill.Material);

        _materialSyncCallbacks.Add(() =>
        {
            pill.SelectedMaterial = CustomPrimaryMaterial with { CornerRadiusDp = pillRadius };
            pill.UnselectedMaterial = DesktopCardMaterial with { CornerRadiusDp = pillRadius };
            pill.SetSelected(pill.Selected);
            pill.SnapMaterial(pill.Material);
        });
        SeaGlassVisualControl ctrl = CreateGlassControl(
            pill,
            width: width,
            height: height,
            interactiveHoverLift: true,
            overlay: (dc, frame, w, h) =>
            {
                SeaGlassXamlRenderer.DrawCenteredLabel(
                    dc,
                    pill.Label,
                    new Point(w * 0.5, h * 0.5),
                    frame.OptimalContentColorArgb,
                    11.0,
                    bold: pill.Selected,
                    extraBold: pill.Selected);
            });
        if (margin is { } m) ctrl.Margin = m;
        return ctrl;
    }

    private static void DrawArcGeometry(
        DrawingContext dc,
        Pen pen,
        Point center,
        double radius,
        double startAngleDeg,
        double sweepAngleDeg)
    {
        double clampedSweep = Math.Clamp(sweepAngleDeg, 1.0, 359.5);
        double startRad = startAngleDeg * (Math.PI / 180.0);
        double endRad = (startAngleDeg + clampedSweep) * (Math.PI / 180.0);

        Point startPt = new(center.X + Math.Cos(startRad) * radius, center.Y + Math.Sin(startRad) * radius);
        Point endPt = new(center.X + Math.Cos(endRad) * radius, center.Y + Math.Sin(endRad) * radius);

        StreamGeometry arcGeom = new();
        using (StreamGeometryContext ctx = arcGeom.Open())
        {
            ctx.BeginFigure(startPt, false, false);
            ctx.ArcTo(
                endPt,
                new System.Windows.Size(radius, radius),
                0,
                clampedSweep > 180.0,
                SweepDirection.Clockwise,
                true,
                true);
        }
        arcGeom.Freeze();
        dc.DrawGeometry(null, pen, arcGeom);
    }

    private static FrameworkElement Spacer(double height) => new Border { Height = height };
    private static FrameworkElement SpacerW(double width) => new Border { Width = width };

    /// <summary>
    /// Renders the live gallery window to a PNG file for visual inspection.
    /// </summary>
    public void CaptureGallerySnapshotToPng(string outputPath)
    {
        _sharpBackdropCanvas.Phase = 0.85;
        _frostedBackdropCanvas.Phase = 0.85;
        _sharpBackdropCanvas.CacheMode = null;
        _frostedBackdropHost.CacheMode = null;

        Measure(new System.Windows.Size(Width, Height));
        Arrange(new Rect(0, 0, Width, Height));
        UpdateLayout();

        if (Content is UIElement rootVisual)
        {
            rootVisual.Measure(new System.Windows.Size(Width, Height));
            rootVisual.Arrange(new Rect(0, 0, Width, Height));
            rootVisual.UpdateLayout();

            for (int i = 0; i < 90; i++)
            {
                foreach (SeaGlassVisualControl ctrl in _activeControls)
                {
                    ctrl.StepPhysics(1f / 60f);
                }
            }
            foreach (StudioPaintHost host in _paintHosts)
            {
                host.InvalidateVisual();
            }
            rootVisual.UpdateLayout();

            RenderTargetBitmap rtb = new((int)Width, (int)Height, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(rootVisual);

            PngBitmapEncoder encoder = new();
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            string? dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using FileStream fs = File.Create(outputPath);
            encoder.Save(fs);
        }
    }
}
