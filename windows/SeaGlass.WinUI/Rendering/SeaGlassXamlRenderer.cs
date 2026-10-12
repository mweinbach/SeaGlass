using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SeaGlass.Core;

namespace SeaGlass.WinUI.Rendering;

/// <summary>
/// Shared GPU stage backdrop visual references (<c>SharpBackdropVisual</c> and <c>FrostedBackdropVisual</c>)
/// sampled in real time by every foreground <see cref="SeaGlassXamlRenderer.DrawSeaGlassSurface"/> call.
/// </summary>
public sealed class SeaGlassStageBackdropVisuals
{
    public Visual? SharpBackdropVisual { get; set; }
    public Visual? FrostedBackdropVisual { get; set; }
    public double StageWidth { get; set; } = 1420.0;
    public double StageHeight { get; set; } = 940.0;
}

/// <summary>
/// Hardware-accelerated Windows Visual / DrawingContext renderer for SeaGlass surfaces,
/// coalescing <c>sminCubic</c> SDF clusters, 20-shape Material 3 Expressive polygons,
/// and all 20 vector glyphs.
///
/// Exact 1:1 port of <c>SeaGlassStage.kt</c>, <c>SeaGlassComponents.kt</c>, and <c>SeaGlassUiKit.kt</c> on Android.
/// </summary>
public static class SeaGlassXamlRenderer
{
    /// <summary>
    /// Enables pointer-reactive 3D skew tilt, cursor-tracked refraction/bevel light, and the press
    /// caustic glow. Off by default: desktop glass stays optically still under click &amp; drag
    /// (hover lift and press squash remain).
    /// </summary>
    public static bool PointerTiltAndGlowEnabled { get; set; }
    private static readonly Typeface ExtraBoldTypeface = new(
        new FontFamily("Segoe UI Variable Display, Segoe UI"),
        FontStyles.Normal,
        FontWeights.ExtraBold,
        FontStretches.Normal);

    private static readonly Typeface BoldTypeface = new(
        new FontFamily("Segoe UI Variable Display, Segoe UI"),
        FontStyles.Normal,
        FontWeights.Bold,
        FontStretches.Normal);

    private static readonly Typeface SemiBoldTypeface = new(
        new FontFamily("Segoe UI Variable Text, Segoe UI"),
        FontStyles.Normal,
        FontWeights.SemiBold,
        FontStretches.Normal);

    private static readonly Typeface MonoTypeface = new(
        new FontFamily("Cascadia Mono, Consolas"),
        FontStyles.Normal,
        FontWeights.Bold,
        FontStretches.Normal);

    private static ImageBrush? _cachedSandGrainBrush;

    public static ImageBrush ObtainSandGrainBrush()
    {
        if (_cachedSandGrainBrush is not null) return _cachedSandGrainBrush;

        byte[] bgra = SeaGlassSandGrainTexture.ObtainBgra32Pixels();
        int size = SeaGlassSandGrainTexture.TileSize;
        WriteableBitmap bmp = new(size, size, 96, 96, PixelFormats.Pbgra32, null);
        bmp.WritePixels(new Int32Rect(0, 0, size, size), bgra, size * 4, 0);
        bmp.Freeze();

        ImageBrush brush = new(bmp)
        {
            TileMode = TileMode.Tile,
            Viewport = new Rect(0, 0, size * 0.65, size * 0.65),
            ViewportUnits = BrushMappingMode.Absolute,
            Stretch = Stretch.Fill
        };
        brush.Freeze();
        _cachedSandGrainBrush = brush;
        return brush;
    }

    public static Color ToMediaColor(int argb, float alphaMultiplier = 1f)
    {
        uint u = unchecked((uint)argb);
        byte a = (byte)Math.Clamp((int)MathF.Round(((u >> 24) & 0xFFu) * alphaMultiplier), 0, 255);
        byte r = (byte)((u >> 16) & 0xFFu);
        byte g = (byte)((u >> 8) & 0xFFu);
        byte b = (byte)(u & 0xFFu);
        return Color.FromArgb(a, r, g, b);
    }

    public static Color ToRgbColor(int rgb, float alpha)
    {
        uint u = unchecked((uint)rgb);
        byte a = (byte)Math.Clamp((int)MathF.Round(Math.Clamp(alpha, 0f, 1f) * 255f), 0, 255);
        byte r = (byte)((u >> 16) & 0xFFu);
        byte g = (byte)((u >> 8) & 0xFFu);
        byte b = (byte)(u & 0xFFu);
        return Color.FromArgb(a, r, g, b);
    }

    public static Geometry BuildGeometryFromPath(SeaGlassVectorPath2D path)
    {
        StreamGeometry geom = new();
        using (StreamGeometryContext ctx = geom.Open())
        {
            IReadOnlyList<SeaGlassVectorPath2D.PathCommand> cmds = path.Commands;
            int count = cmds.Count;
            for (int i = 0; i < count; i++)
            {
                SeaGlassVectorPath2D.PathCommand cmd = cmds[i];
                switch (cmd.Kind)
                {
                    case SeaGlassVectorPath2D.CommandKind.MoveTo:
                        ctx.BeginFigure(new Point(cmd.X1, cmd.Y1), true, true);
                        break;
                    case SeaGlassVectorPath2D.CommandKind.LineTo:
                        ctx.LineTo(new Point(cmd.X1, cmd.Y1), true, true);
                        break;
                    case SeaGlassVectorPath2D.CommandKind.CubicTo:
                        ctx.BezierTo(
                            new Point(cmd.X1, cmd.Y1),
                            new Point(cmd.X2, cmd.Y2),
                            new Point(cmd.X3, cmd.Y3),
                            true,
                            true);
                        break;
                }
            }
        }
        geom.Freeze();
        return geom;
    }

    public static Geometry BuildContourGeometry(
        SeaGlassSurfaceFrameState frameState,
        double width,
        double height)
    {
        if (frameState.IsUniformSingleNode)
        {
            double r = Math.Clamp(frameState.SolvedCornerRadiiPx.TopLeftPx, 0.0, Math.Min(width, height) * 0.5);
            RectangleGeometry rect = new(new Rect(0, 0, Math.Max(1, width), Math.Max(1, height)), r, r);
            rect.Freeze();
            return rect;
        }

        if (frameState.ContourPath is { IsEmpty: false } path)
        {
            return BuildGeometryFromPath(path);
        }

        double fallbackR = Math.Clamp(frameState.EffectiveMaterial.CornerRadiusDp, 0.0, Math.Min(width, height) * 0.5);
        RectangleGeometry fallback = new(new Rect(0, 0, Math.Max(1, width), Math.Max(1, height)), fallbackR, fallbackR);
        fallback.Freeze();
        return fallback;
    }

    public static Geometry ParseContourGeometry(
        string xamlPathData,
        double fallbackWidth,
        double fallbackHeight,
        double fallbackRadius = 24.0)
    {
        if (!string.IsNullOrWhiteSpace(xamlPathData))
        {
            try
            {
                Geometry geom = Geometry.Parse(xamlPathData);
                if (geom.CanFreeze) geom.Freeze();
                return geom;
            }
            catch
            {
                // Fall back to rounded rectangle geometry
            }
        }
        RectangleGeometry rect = new(
            new Rect(0, 0, Math.Max(1, fallbackWidth), Math.Max(1, fallbackHeight)),
            fallbackRadius,
            fallbackRadius);
        if (rect.CanFreeze) rect.Freeze();
        return rect;
    }

    private sealed record CachedBevelAndShadowRig(
        SolidColorBrush CoreShadowBrush,
        Pen[] ShadowFeatherPens,
        Pen[] ProgressiveBevelPens);

    private sealed record CachedSingleNodeShader(
        Geometry OuterGeometry,
        DrawingGroup ShadowDrawing,
        DrawingGroup OpticsAndBevelDrawing);

    private static readonly Dictionary<int, CachedBevelAndShadowRig> BevelRigCache = new(64);
    private static readonly Dictionary<int, CachedSingleNodeShader> SingleNodeShaderCache = new(256);

    private static int ComputeMaterialOpticsKey(SeaGlassMaterial mat, int primaryTintArgb, int secondaryTintArgb)
    {
        HashCode hc = new();
        hc.Add(primaryTintArgb);
        hc.Add(secondaryTintArgb);
        hc.Add((int)MathF.Round(mat.TintDensity * 200f));
        hc.Add((int)MathF.Round(mat.Coarseness * 200f));
        hc.Add((int)MathF.Round(mat.Ior * 200f));
        hc.Add((int)MathF.Round(mat.ChromaticDispersion * 200f));
        hc.Add((int)MathF.Round(mat.RimChalkiness * 200f));
        hc.Add((int)MathF.Round(mat.Transparency * 200f));
        return hc.ToHashCode();
    }

    private static CachedBevelAndShadowRig ObtainBevelAndShadowRig(
        SeaGlassMaterial mat,
        int primaryTintArgb,
        int secondaryTintArgb,
        double minDim,
        float effectiveHover,
        float touchNormX,
        float touchNormY)
    {
        int minDimQ = (int)Math.Round(minDim);
        int hoverQ = (int)MathF.Round(effectiveHover * 25f);
        int tiltQ = HashCode.Combine((int)MathF.Round(touchNormX * 25f), (int)MathF.Round(touchNormY * 25f));
        int matKey = ComputeMaterialOpticsKey(mat, primaryTintArgb, secondaryTintArgb);
        int rigKey = HashCode.Combine(matKey, minDimQ, hoverQ, tiltQ);

        if (BevelRigCache.TryGetValue(rigKey, out CachedBevelAndShadowRig? cached))
        {
            return cached;
        }

        if (BevelRigCache.Count >= 96)
        {
            BevelRigCache.Clear();
        }

        float sandWear = mat.EffectiveSandWear;
        int shadowColorArgb = SeaGlassColorMath.LerpOklab(mat.TintColorArgb, unchecked((int)0xFF0A1624), 0.68f);
        float baseShadowAlpha = Math.Clamp(0.11f + sandWear * 0.12f + effectiveHover * 0.03f, 0.07f, 0.22f);

        SolidColorBrush coreShadowBrush = new(ToRgbColor(shadowColorArgb, baseShadowAlpha * 0.22f));
        coreShadowBrush.Freeze();

        const int shadowSteps = 10;
        Pen[] shadowPens = new Pen[shadowSteps];
        for (int s = 1; s <= shadowSteps; s++)
        {
            double st = (double)s / shadowSteps;
            double penW = 1.4 + st * st * 15.0;
            float featherA = baseShadowAlpha * 0.068f * (float)Math.Pow(1.0 - st * 0.85, 1.4);
            SolidColorBrush sb = new(ToRgbColor(shadowColorArgb, featherA));
            sb.Freeze();
            Pen sp = new(sb, penW) { LineJoin = PenLineJoin.Round };
            sp.Freeze();
            shadowPens[s - 1] = sp;
        }

        float rimLight = Math.Clamp(0.44f + mat.RimChalkiness * 0.50f, 0.28f, 0.96f);
        float effectiveDispersion = Math.Clamp(mat.ChromaticDispersion * (1.0f - 0.72f * mat.Coarseness), 0.0f, 0.85f);

        int windwardCrestRgb = SeaGlassColorMath.ComputeFrostedRindColor(primaryTintArgb, mat.Coarseness, 0.70f) & 0x00FFFFFF;
        int leewardCausticRgb = SeaGlassColorMath.ComputeFrostedRindColor(secondaryTintArgb, mat.Coarseness, 0.62f) & 0x00FFFFFF;
        int coolDispersionRgb = SeaGlassColorMath.LerpOklab(primaryTintArgb, unchecked((int)0xFFEAF7FF), 0.56f + 0.18f * effectiveDispersion) & 0x00FFFFFF;
        int warmDispersionRgb = SeaGlassColorMath.LerpOklab(secondaryTintArgb, unchecked((int)0xFFFFF4EC), 0.56f + 0.18f * effectiveDispersion) & 0x00FFFFFF;
        int milkyRindRgb = SeaGlassColorMath.ComputeFrostedRindColor(primaryTintArgb, mat.Coarseness, 0.78f) & 0x00FFFFFF;

        Point bevelStart = new(
            Math.Clamp(0.12 + touchNormX * 0.30, 0.0, 0.5),
            Math.Clamp(0.04 + touchNormY * 0.30, 0.0, 0.5));
        Point bevelEnd = new(
            Math.Clamp(0.88 - touchNormX * 0.25, 0.5, 1.0),
            Math.Clamp(0.96 - touchNormY * 0.25, 0.5, 1.0));

        float slopeScale = Math.Clamp(0.32f + 0.52f * MathF.Max(sandWear, mat.RimChalkiness * 0.52f), 0.28f, 0.82f);
        double maxBevelStrokeW = Math.Clamp(minDim * 0.66, 14.0, 42.0);
        const int bevelSteps = 18;
        float totalBevelEnergy = rimLight * slopeScale;

        Pen[] bevelPens = new Pen[bevelSteps];
        for (int i = 0; i < bevelSteps; i++)
        {
            double t = (i + 0.5) / bevelSteps;
            double radialFraction = Math.Pow(1.0 - t, 1.35);
            double strokeW = Math.Max(1.2, maxBevelStrokeW * radialFraction);
            float bellWeight = MathF.Sin((float)(t * Math.PI * 0.85));
            float layerAlpha = Math.Clamp(totalBevelEnergy * 0.062f * (0.45f + 0.55f * bellWeight), 0.008f, 0.048f);
            float rimBlend = (float)(t * t);

            int topCrest = SeaGlassColorMath.LerpOklab(windwardCrestRgb | unchecked((int)0xFF000000), milkyRindRgb | unchecked((int)0xFF000000), rimBlend) & 0x00FFFFFF;
            int botCaustic = SeaGlassColorMath.LerpOklab(leewardCausticRgb | unchecked((int)0xFF000000), milkyRindRgb | unchecked((int)0xFF000000), rimBlend * 0.65f) & 0x00FFFFFF;
            LinearGradientBrush b = new(
                new GradientStopCollection
                {
                    new GradientStop(ToRgbColor(topCrest, layerAlpha * 0.92f), 0.00),
                    new GradientStop(ToRgbColor(warmDispersionRgb, layerAlpha * 0.86f), 0.22),
                    new GradientStop(ToRgbColor(topCrest, layerAlpha * 0.68f), 0.48),
                    new GradientStop(ToRgbColor(coolDispersionRgb, layerAlpha * 0.88f), 0.72),
                    new GradientStop(ToRgbColor(warmDispersionRgb, layerAlpha * 0.86f), 0.88),
                    new GradientStop(ToRgbColor(botCaustic, layerAlpha * 0.95f), 1.00)
                },
                bevelStart,
                bevelEnd);
            b.Freeze();

            Pen bp = new(b, strokeW) { LineJoin = PenLineJoin.Round };
            bp.Freeze();
            bevelPens[i] = bp;
        }

        CachedBevelAndShadowRig created = new(coreShadowBrush, shadowPens, bevelPens);
        BevelRigCache[rigKey] = created;
        return created;
    }

    private static void RecordOpticsAndBevelPass(
        DrawingContext dc,
        SeaGlassMaterial mat,
        Geometry outerGeometry,
        double width,
        double height,
        int primaryTintArgb,
        int secondaryTintArgb,
        Point gradStart,
        Point gradEnd,
        SeaGlassContentRole contentRole,
        CachedBevelAndShadowRig rig)
    {
        float sandWear = mat.EffectiveSandWear;
        Rect fullRect = new(0, 0, width, height);

        SeaGlassColorMath.OklabColor primaryLab = SeaGlassColorMath.ArgbToOklab(primaryTintArgb);
        bool isDarkGlass = primaryLab.L < 0.48f;

        if (isDarkGlass)
        {
            float darkBodyAlpha = Math.Clamp(sandWear * 0.66f + mat.TintDensity * (0.14f + 0.26f * sandWear), 0.02f, 0.90f);
            int topDarkTint = SeaGlassColorMath.LerpOklab(primaryTintArgb, unchecked((int)0xFF2B3E55), 0.18f);
            int botDarkTint = SeaGlassColorMath.LerpOklab(secondaryTintArgb, unchecked((int)0xFF060C14), 0.24f);
            LinearGradientBrush darkBodyBrush = new(
                new GradientStopCollection
                {
                    new GradientStop(ToRgbColor(topDarkTint, darkBodyAlpha * 0.94f), 0.0),
                    new GradientStop(ToRgbColor(primaryTintArgb, darkBodyAlpha), 0.50),
                    new GradientStop(ToRgbColor(botDarkTint, Math.Min(0.92f, darkBodyAlpha * 1.04f)), 1.0)
                },
                gradStart,
                gradEnd)
            {
                MappingMode = BrushMappingMode.Absolute
            };
            darkBodyBrush.Freeze();
            dc.DrawRectangle(darkBodyBrush, null, fullRect);
        }
        else
        {
            float screenAlpha = Math.Clamp(sandWear * 0.34f + mat.TintDensity * 0.22f * (0.20f + 0.80f * sandWear), 0.0f, 0.54f);
            if (screenAlpha > 0.01f)
            {
                int liftStart = SeaGlassColorMath.LerpOklab(primaryTintArgb, unchecked((int)0xFFFFFFFF), 0.28f);
                int liftEnd = SeaGlassColorMath.LerpOklab(secondaryTintArgb, unchecked((int)0xFFFFFFFF), 0.24f);
                LinearGradientBrush liftBrush = new(
                    ToRgbColor(liftStart, screenAlpha * 0.55f),
                    ToRgbColor(liftEnd, screenAlpha * 0.55f),
                    gradStart,
                    gradEnd)
                {
                    MappingMode = BrushMappingMode.Absolute
                };
                liftBrush.Freeze();
                dc.DrawRectangle(liftBrush, null, fullRect);
            }

            float roleWeight = contentRole switch
            {
                SeaGlassContentRole.NavigationChrome => 1.06f,
                SeaGlassContentRole.PureOpticalLens => 0.84f,
                _ => 1.00f
            };
            float bodyAlpha = Math.Clamp((sandWear * 0.74f + mat.TintDensity * (0.16f + 0.24f * sandWear)) * roleWeight, 0.0f, 0.92f);

            if (bodyAlpha > 0.01f)
            {
                int milkyPrimary = SeaGlassColorMath.ComputeFrostedRindColor(primaryTintArgb, mat.Coarseness, 0.34f * sandWear);
                int milkySecondary = SeaGlassColorMath.ComputeFrostedRindColor(secondaryTintArgb, mat.Coarseness, 0.38f * sandWear);

                LinearGradientBrush milkyBaseBrush = new(
                    ToRgbColor(milkyPrimary, bodyAlpha),
                    ToRgbColor(milkySecondary, bodyAlpha),
                    gradStart,
                    gradEnd)
                {
                    MappingMode = BrushMappingMode.Absolute
                };
                milkyBaseBrush.Freeze();
                dc.DrawRectangle(milkyBaseBrush, null, fullRect);

                float coreDepthAlpha = Math.Clamp(bodyAlpha * (0.42f + 0.28f * mat.TintDensity), 0.0f, 0.64f);
                if (coreDepthAlpha > 0.01f)
                {
                    int deepCoreArgb = SeaGlassColorMath.ComputeDeepVolumetricCoreColor(primaryTintArgb, mat.TintDensity);

                    LinearGradientBrush verticalCoreBrush = new(
                        new GradientStopCollection
                        {
                            new GradientStop(ToRgbColor(primaryTintArgb, coreDepthAlpha * 0.18f), 0.00),
                            new GradientStop(ToRgbColor(deepCoreArgb, coreDepthAlpha * 0.74f), 0.28),
                            new GradientStop(ToRgbColor(primaryTintArgb, coreDepthAlpha * 0.62f), 0.56),
                            new GradientStop(ToRgbColor(primaryTintArgb, coreDepthAlpha * 0.22f), 0.84),
                            new GradientStop(Color.FromArgb(0, 255, 255, 255), 1.00)
                        },
                        new Point(0, 0),
                        new Point(0, height))
                    {
                        MappingMode = BrushMappingMode.Absolute
                    };
                    verticalCoreBrush.Freeze();
                    dc.DrawRectangle(verticalCoreBrush, null, fullRect);

                    double coreRx = Math.Max(16.0, width * 0.56);
                    double coreRy = Math.Max(16.0, height * 0.68);
                    Point corePt = new(width * 0.47, height * 0.42);
                    RadialGradientBrush radialCoreBrush = new(
                        new GradientStopCollection
                        {
                            new GradientStop(ToRgbColor(deepCoreArgb, coreDepthAlpha * 0.52f), 0.00),
                            new GradientStop(ToRgbColor(primaryTintArgb, coreDepthAlpha * 0.26f), 0.55),
                            new GradientStop(Color.FromArgb(0, 255, 255, 255), 1.00)
                        })
                    {
                        Center = corePt,
                        GradientOrigin = corePt,
                        RadiusX = coreRx,
                        RadiusY = coreRy,
                        MappingMode = BrushMappingMode.Absolute
                    };
                    radialCoreBrush.Freeze();
                    dc.DrawRectangle(radialCoreBrush, null, fullRect);
                }
            }
        }

        // Pass 3A: 18-step progressive Gaussian bevel integration (using cached frozen pens)
        Pen[] bevelPens = rig.ProgressiveBevelPens;
        for (int i = 0; i < bevelPens.Length; i++)
        {
            dc.DrawGeometry(null, bevelPens[i], outerGeometry);
        }

        // Pass 3B: Crisp powdery salt/sand micro-facet stipple across the entire glass piece
        float velvetProgress = MathF.Pow(Math.Clamp(mat.Coarseness, 0f, 1f), 0.75f);
        float grainOpacity = Math.Clamp(velvetProgress * (0.52f + 0.48f * sandWear) * 0.82f, 0f, 0.82f);
        if (grainOpacity > 0.01f)
        {
            dc.PushOpacity(grainOpacity);
            dc.DrawRectangle(ObtainSandGrainBrush(), null, fullRect);
            dc.Pop();
        }
    }

    /// <summary>
    /// Renders an authentic multi-pass Water-Drop &amp; Sanded SeaGlass optical surface into <paramref name="dc"/>,
    /// matching <c>Modifier.seaGlass</c> and <c>drawWaterDropSeaGlassOptics</c> in <c>SeaGlassStage.kt</c> 1:1.
    /// Uses pre-compiled frozen <see cref="DrawingGroup"/> shader layers for single-node surfaces and cached
    /// progressive bevel/shadow rigs for multi-node coalescing SDF surfaces.
    /// </summary>
    public static void DrawSeaGlassSurface(
        DrawingContext dc,
        SeaGlassSurfaceFrameState frameState,
        double width,
        double height,
        int backdropSampleArgb = unchecked((int)0xFF143854),
        Point? localPointerPos = null,
        bool isPointerDown = false,
        SeaGlassStageBackdropVisuals? stageBackdrop = null,
        Point? elementOriginInStage = null,
        IReadOnlyList<SeaGlassSdfEngine.SdfNode>? activeNodes = null,
        float smoothRadiusDp = 28f,
        SeaGlassContentRole contentRole = SeaGlassContentRole.TextAndIcons,
        float hoverProgress = 0f)
    {
        if (width <= 1 || height <= 1) return;

        SeaGlassMaterial mat = frameState.EffectiveMaterial;
        float sandWear = mat.EffectiveSandWear;

        double cx = width * 0.5;
        double cy = height * 0.5;
        bool pointerFx = PointerTiltAndGlowEnabled;
        float tiltX = pointerFx ? SeaGlassTheme.Current.TiltState.TiltX : 0f;
        float tiltY = pointerFx ? SeaGlassTheme.Current.TiltState.TiltY : 0f;
        float effectiveHover = Math.Clamp(hoverProgress, 0f, 1f);

        Point? fxPointer = pointerFx ? localPointerPos : null;
        float touchIntensity = !pointerFx ? 0f : isPointerDown ? 1.0f : effectiveHover * 0.42f;

        float touchNormX = (fxPointer is { } lp && touchIntensity > 0.01f)
            ? Math.Clamp((float)((lp.X - cx) / Math.Max(cx, 1.0)), -1f, 1f) * touchIntensity * 0.65f
            : 0f;
        float touchNormY = (fxPointer is { } lp2 && touchIntensity > 0.01f)
            ? Math.Clamp((float)((lp2.Y - cy) / Math.Max(cy, 1.0)), -1f, 1f) * touchIntensity * 0.65f
            : 0f;

        float combinedTiltX = Math.Clamp(tiltX + touchNormX, -1f, 1f);
        float combinedTiltY = Math.Clamp(tiltY + touchNormY, -1f, 1f);
        double skewY = pointerFx ? frameState.TiltRotationYDegrees * 0.35 : 0.0;
        double skewX = pointerFx ? frameState.TiltRotationXDegrees * 0.35 : 0.0;

        bool hasTransform = Math.Abs(frameState.ScaleX - 1f) > 0.001f ||
                            Math.Abs(frameState.ScaleY - 1f) > 0.001f ||
                            Math.Abs(skewX) > 0.02 ||
                            Math.Abs(skewY) > 0.02;
        if (hasTransform)
        {
            TransformGroup tg = new();
            tg.Children.Add(new ScaleTransform(frameState.ScaleX, frameState.ScaleY, cx, cy));
            if (Math.Abs(skewX) > 0.02 || Math.Abs(skewY) > 0.02)
            {
                tg.Children.Add(new SkewTransform(skewY, skewX, cx, cy));
            }
            dc.PushTransform(tg);
        }

        bool isMultiNode = activeNodes is { Count: > 1 };
        double minDim = Math.Min(width, height);
        int primaryTintArgb = (activeNodes is { Count: > 0 }) ? activeNodes[0].TintColorArgb : mat.TintColorArgb;
        int secondaryTintArgb = (activeNodes is { Count: >= 2 }) ? activeNodes[1].TintColorArgb : primaryTintArgb;

        CachedBevelAndShadowRig rig = ObtainBevelAndShadowRig(
            mat, primaryTintArgb, secondaryTintArgb, minDim, effectiveHover, touchNormX, touchNormY);

        Geometry outerGeometry;
        DrawingGroup? cachedOpticsDrawing = null;

        if (!isMultiNode && frameState.IsUniformSingleNode)
        {
            int wQ = (int)Math.Round(width * 2.0);
            int hQ = (int)Math.Round(height * 2.0);
            int rQ = (int)MathF.Round(frameState.SolvedCornerRadiiPx.TopLeftPx * 2.0f);
            int hoverQ = (int)MathF.Round(effectiveHover * 25f);
            int tiltQ = HashCode.Combine((int)MathF.Round(combinedTiltX * 25f), (int)MathF.Round(combinedTiltY * 25f));
            int matKey = ComputeMaterialOpticsKey(mat, primaryTintArgb, secondaryTintArgb);
            int shaderKey = HashCode.Combine(wQ, hQ, rQ, matKey, (int)contentRole, hoverQ, tiltQ, isPointerDown);

            if (!SingleNodeShaderCache.TryGetValue(shaderKey, out CachedSingleNodeShader? cachedShader))
            {
                if (SingleNodeShaderCache.Count >= 256)
                {
                    SingleNodeShaderCache.Clear();
                }

                Geometry geom = BuildContourGeometry(frameState, width, height);

                double shadowOffsetX = -combinedTiltX * 3.5;
                double shadowOffsetY = (3.5 - combinedTiltY * 2.5) + effectiveHover * 1.2 - (isPointerDown ? 1.0 : 0.0);
                DrawingGroup shadowGroup = new();
                using (DrawingContext sdc = shadowGroup.Open())
                {
                    sdc.PushTransform(new TranslateTransform(shadowOffsetX, shadowOffsetY));
                    sdc.DrawGeometry(rig.CoreShadowBrush, null, geom);
                    Pen[] sPens = rig.ShadowFeatherPens;
                    for (int s = 0; s < sPens.Length; s++)
                    {
                        sdc.DrawGeometry(null, sPens[s], geom);
                    }
                    sdc.Pop();
                }
                shadowGroup.Freeze();

                DrawingGroup opticsGroup = new();
                using (DrawingContext odc = opticsGroup.Open())
                {
                    RecordOpticsAndBevelPass(
                        odc, mat, geom, width, height, primaryTintArgb, secondaryTintArgb,
                        new Point(width * 0.15, 0), new Point(width * 0.85, height), contentRole, rig);
                }
                opticsGroup.Freeze();

                cachedShader = new CachedSingleNodeShader(geom, shadowGroup, opticsGroup);
                SingleNodeShaderCache[shaderKey] = cachedShader;
            }

            outerGeometry = cachedShader.OuterGeometry;
            cachedOpticsDrawing = cachedShader.OpticsAndBevelDrawing;
            dc.DrawDrawing(cachedShader.ShadowDrawing);
        }
        else
        {
            outerGeometry = isMultiNode
                ? BuildMultiNodeGeometry(activeNodes!, smoothRadiusDp)
                : BuildContourGeometry(frameState, width, height);

            double shadowOffsetX = -combinedTiltX * 3.5;
            double shadowOffsetY = (3.5 - combinedTiltY * 2.5) + effectiveHover * 1.2 - (isPointerDown ? 1.0 : 0.0);
            dc.PushTransform(new TranslateTransform(shadowOffsetX, shadowOffsetY));
            dc.DrawGeometry(rig.CoreShadowBrush, null, outerGeometry);
            Pen[] sPens = rig.ShadowFeatherPens;
            for (int s = 0; s < sPens.Length; s++)
            {
                dc.DrawGeometry(null, sPens[s], outerGeometry);
            }
            dc.Pop();
        }

        // Pass 1: Continuous Glass Refraction & Sand-Diffused Subsurface Transmission
        dc.PushClip(outerGeometry);

        Point relOrigin = elementOriginInStage ?? new Point(0, 0);
        if (stageBackdrop?.FrostedBackdropVisual is not null &&
            stageBackdrop.SharpBackdropVisual is not null &&
            SeaGlassTheme.Current.TierOverride != SeaGlassDeviceTier.LightweightTint)
        {
            double stageW = Math.Max(100.0, stageBackdrop.StageWidth);
            double stageH = Math.Max(100.0, stageBackdrop.StageHeight);

            double coreLensScale = 1.0 + (mat.Ior - 1.0) * 0.055 * (1.0 - 0.45 * sandWear);
            double maxRefractPx = (mat.Ior - 1.0) * 18.0 * (1.0 - 0.42 * sandWear);
            double coreRefractX = -combinedTiltX * maxRefractPx * 0.36;
            double coreRefractY = -combinedTiltY * maxRefractPx * 0.36;

            double vw = width / coreLensScale;
            double vh = height / coreLensScale;
            double vx = relOrigin.X - coreRefractX + (width - vw) * 0.5;
            double vy = relOrigin.Y - coreRefractY + (height - vh) * 0.5;
            Rect viewbox = new(vx / stageW, vy / stageH, vw / stageW, vh / stageH);

            float clearLensFactor = Math.Clamp((mat.Transparency - 0.72f) / 0.23f, 0f, 1f) *
                                    Math.Clamp((0.30f - mat.Coarseness) / 0.30f, 0f, 1f);
            float frostLayerOpacity = Math.Clamp(1.0f - clearLensFactor * 0.65f, 0.25f, 1.0f);

            if (frostLayerOpacity < 0.99f)
            {
                VisualBrush sharpBrush = new(stageBackdrop.SharpBackdropVisual)
                {
                    Viewbox = viewbox,
                    ViewboxUnits = BrushMappingMode.RelativeToBoundingBox,
                    Stretch = Stretch.Fill
                };
                dc.DrawRectangle(sharpBrush, null, new Rect(0, 0, width, height));
            }

            VisualBrush frostedBrush = new(stageBackdrop.FrostedBackdropVisual)
            {
                Viewbox = viewbox,
                ViewboxUnits = BrushMappingMode.RelativeToBoundingBox,
                Stretch = Stretch.Fill,
                Opacity = frostLayerOpacity
            };
            dc.DrawRectangle(frostedBrush, null, new Rect(0, 0, width, height));
        }

        // Pass 2 & Pass 3: Draw pre-compiled frozen shader DrawingGroup on single-node surfaces,
        // or evaluate using cached frozen BevelAndShadowRig pens on dynamic multi-node SDF surfaces.
        if (cachedOpticsDrawing is not null)
        {
            dc.DrawDrawing(cachedOpticsDrawing);
        }
        else
        {
            Point gradStart = (activeNodes is { Count: >= 2 })
                ? new Point(activeNodes[0].CenterX - activeNodes[0].HalfWidth * 0.55, activeNodes[0].CenterY)
                : new Point(width * 0.15, 0);
            Point gradEnd = (activeNodes is { Count: >= 2 })
                ? new Point(activeNodes[^1].CenterX + activeNodes[^1].HalfWidth * 0.55, activeNodes[^1].CenterY)
                : new Point(width * 0.85, height);

            RecordOpticsAndBevelPass(
                dc, mat, outerGeometry, width, height, primaryTintArgb, secondaryTintArgb,
                gradStart, gradEnd, contentRole, rig);
        }

        // Pass 4: Optional Cursor-Tracked Hover Illumination & Touch Compression Caustic Wave
        if (fxPointer is { } pt && touchIntensity > 0.01f)
        {
            int hdrTintArgb = SeaGlassColorMath.LerpOklab(primaryTintArgb, unchecked((int)0xFFFFFFFF), 0.45f);
            double waveRadius = Math.Max(width, height) * (isPointerDown ? 0.52 : 0.65);
            float centerAlpha = isPointerDown ? 0.22f : 0.11f * effectiveHover;
            RadialGradientBrush waveBrush = new(
                new GradientStopCollection
                {
                    new GradientStop(ToRgbColor(hdrTintArgb & 0x00FFFFFF, centerAlpha), 0.0),
                    new GradientStop(ToRgbColor(primaryTintArgb & 0x00FFFFFF, centerAlpha * 0.40f), 0.48),
                    new GradientStop(Color.FromArgb(0, 255, 255, 255), 1.0)
                })
            {
                Center = pt,
                GradientOrigin = pt,
                RadiusX = waveRadius,
                RadiusY = waveRadius,
                MappingMode = BrushMappingMode.Absolute
            };
            waveBrush.Freeze();
            dc.DrawEllipse(waveBrush, null, pt, waveRadius, waveRadius);
        }

        dc.Pop(); // Pop outerGeometry clip

        if (hasTransform)
        {
            dc.Pop();
        }
    }

    private static readonly SeaGlassVectorPath2D SharedMultiNodePath = new();

    private static Geometry BuildMultiNodeGeometry(
        IReadOnlyList<SeaGlassSdfEngine.SdfNode> nodes,
        float smoothRadiusDp)
    {
        SeaGlassContourPathBuilder.BuildCoalescingScenePath(nodes, smoothRadiusDp, SharedMultiNodePath);
        return BuildGeometryFromPath(SharedMultiNodePath);
    }

    /// <summary>
    /// Draws crisp vector glyphs matching <c>SeaGlassIcon</c> in <c>SeaGlassUiKit.kt</c>.
    /// </summary>
    public static void DrawGlyph(
        DrawingContext dc,
        SeaGlassGlyph glyph,
        Rect bounds,
        int colorArgb,
        float alpha = 1f)
    {
        double w = bounds.Width;
        double h = bounds.Height;
        double ox = bounds.X;
        double oy = bounds.Y;
        double sw = Math.Max(1.8, Math.Min(w, h) * 0.105);

        SolidColorBrush fill = new(ToMediaColor(colorArgb, alpha));
        fill.Freeze();
        Pen stroke = new(fill, sw)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round
        };
        stroke.Freeze();

        Point P(double nx, double ny) => new(ox + w * nx, oy + h * ny);

        switch (glyph)
        {
            case SeaGlassGlyph.Home:
            {
                StreamGeometry g = new();
                using (StreamGeometryContext ctx = g.Open())
                {
                    ctx.BeginFigure(P(0.50, 0.12), false, true);
                    ctx.LineTo(P(0.88, 0.45), true, true);
                    ctx.LineTo(P(0.78, 0.45), true, true);
                    ctx.LineTo(P(0.78, 0.86), true, true);
                    ctx.LineTo(P(0.22, 0.86), true, true);
                    ctx.LineTo(P(0.22, 0.45), true, true);
                    ctx.LineTo(P(0.12, 0.45), true, true);
                }
                g.Freeze();
                dc.DrawGeometry(null, stroke, g);
                break;
            }
            case SeaGlassGlyph.Search:
                dc.DrawEllipse(null, stroke, P(0.42, 0.42), w * 0.28, h * 0.28);
                dc.DrawLine(stroke, P(0.63, 0.63), P(0.88, 0.88));
                break;

            case SeaGlassGlyph.Tune:
                dc.DrawLine(stroke, P(0.14, 0.30), P(0.86, 0.30));
                dc.DrawEllipse(fill, null, P(0.36, 0.30), w * 0.11, h * 0.11);
                dc.DrawLine(stroke, P(0.14, 0.70), P(0.86, 0.70));
                dc.DrawEllipse(fill, null, P(0.66, 0.70), w * 0.11, h * 0.11);
                break;

            case SeaGlassGlyph.Sparkles:
            {
                StreamGeometry g = new();
                using (StreamGeometryContext ctx = g.Open())
                {
                    ctx.BeginFigure(P(0.50, 0.08), true, true);
                    ctx.QuadraticBezierTo(P(0.53, 0.47), P(0.92, 0.50), true, true);
                    ctx.QuadraticBezierTo(P(0.53, 0.53), P(0.50, 0.92), true, true);
                    ctx.QuadraticBezierTo(P(0.47, 0.53), P(0.08, 0.50), true, true);
                    ctx.QuadraticBezierTo(P(0.47, 0.47), P(0.50, 0.08), true, true);
                }
                g.Freeze();
                dc.DrawGeometry(fill, null, g);
                break;
            }
            case SeaGlassGlyph.Play:
            {
                StreamGeometry g = new();
                using (StreamGeometryContext ctx = g.Open())
                {
                    ctx.BeginFigure(P(0.22, 0.14), true, true);
                    ctx.LineTo(P(0.86, 0.50), true, true);
                    ctx.LineTo(P(0.22, 0.86), true, true);
                }
                g.Freeze();
                dc.DrawGeometry(fill, null, g);
                break;
            }
            case SeaGlassGlyph.Pause:
                dc.DrawRoundedRectangle(fill, null, new Rect(ox + w * 0.20, oy + h * 0.15, w * 0.22, h * 0.70), 2, 2);
                dc.DrawRoundedRectangle(fill, null, new Rect(ox + w * 0.58, oy + h * 0.15, w * 0.22, h * 0.70), 2, 2);
                break;

            case SeaGlassGlyph.Next:
            {
                StreamGeometry g = new();
                using (StreamGeometryContext ctx = g.Open())
                {
                    ctx.BeginFigure(P(0.10, 0.18), true, true);
                    ctx.LineTo(P(0.50, 0.50), true, true);
                    ctx.LineTo(P(0.10, 0.82), true, true);
                    ctx.BeginFigure(P(0.50, 0.18), true, true);
                    ctx.LineTo(P(0.90, 0.50), true, true);
                    ctx.LineTo(P(0.50, 0.82), true, true);
                }
                g.Freeze();
                dc.DrawGeometry(fill, null, g);
                break;
            }
            case SeaGlassGlyph.Check:
            {
                StreamGeometry g = new();
                using (StreamGeometryContext ctx = g.Open())
                {
                    ctx.BeginFigure(P(0.18, 0.52), false, false);
                    ctx.LineTo(P(0.42, 0.76), true, true);
                    ctx.LineTo(P(0.84, 0.26), true, true);
                }
                g.Freeze();
                dc.DrawGeometry(null, stroke, g);
                break;
            }
            case SeaGlassGlyph.Close:
                dc.DrawLine(stroke, P(0.22, 0.22), P(0.78, 0.78));
                dc.DrawLine(stroke, P(0.78, 0.22), P(0.22, 0.78));
                break;

            case SeaGlassGlyph.Plus:
                dc.DrawLine(stroke, P(0.50, 0.18), P(0.50, 0.82));
                dc.DrawLine(stroke, P(0.18, 0.50), P(0.82, 0.50));
                break;

            case SeaGlassGlyph.Heart:
            {
                StreamGeometry g = new();
                using (StreamGeometryContext ctx = g.Open())
                {
                    ctx.BeginFigure(P(0.50, 0.84), false, true);
                    ctx.BezierTo(P(0.12, 0.60), P(0.08, 0.24), P(0.30, 0.18), true, true);
                    ctx.BezierTo(P(0.42, 0.15), P(0.50, 0.26), P(0.50, 0.32), true, true);
                    ctx.BezierTo(P(0.50, 0.26), P(0.58, 0.15), P(0.70, 0.18), true, true);
                    ctx.BezierTo(P(0.92, 0.24), P(0.88, 0.60), P(0.50, 0.84), true, true);
                }
                g.Freeze();
                dc.DrawGeometry(null, stroke, g);
                break;
            }
            case SeaGlassGlyph.Share:
                dc.DrawEllipse(fill, null, P(0.74, 0.22), w * 0.11, h * 0.11);
                dc.DrawEllipse(fill, null, P(0.24, 0.50), w * 0.11, h * 0.11);
                dc.DrawEllipse(fill, null, P(0.74, 0.78), w * 0.11, h * 0.11);
                dc.DrawLine(stroke, P(0.33, 0.45), P(0.65, 0.27));
                dc.DrawLine(stroke, P(0.33, 0.55), P(0.65, 0.73));
                break;

            case SeaGlassGlyph.Bookmark:
            {
                StreamGeometry g = new();
                using (StreamGeometryContext ctx = g.Open())
                {
                    ctx.BeginFigure(P(0.24, 0.14), false, true);
                    ctx.LineTo(P(0.76, 0.14), true, true);
                    ctx.LineTo(P(0.76, 0.86), true, true);
                    ctx.LineTo(P(0.50, 0.68), true, true);
                    ctx.LineTo(P(0.24, 0.86), true, true);
                }
                g.Freeze();
                dc.DrawGeometry(null, stroke, g);
                break;
            }
            case SeaGlassGlyph.Bell:
            {
                StreamGeometry g = new();
                using (StreamGeometryContext ctx = g.Open())
                {
                    ctx.BeginFigure(P(0.22, 0.68), false, true);
                    ctx.LineTo(P(0.78, 0.68), true, true);
                    ctx.LineTo(P(0.72, 0.42), true, true);
                    ctx.BezierTo(P(0.72, 0.22), P(0.28, 0.22), P(0.28, 0.42), true, true);
                }
                g.Freeze();
                dc.DrawGeometry(null, stroke, g);
                dc.DrawEllipse(fill, null, P(0.50, 0.82), w * 0.08, h * 0.08);
                break;
            }
            case SeaGlassGlyph.Settings:
                dc.DrawEllipse(null, stroke, P(0.50, 0.50), w * 0.18, h * 0.18);
                for (int i = 0; i < 6; i++)
                {
                    double a = i * (Math.PI / 3.0);
                    dc.DrawLine(
                        stroke,
                        P(0.50 + Math.Cos(a) * 0.26, 0.50 + Math.Sin(a) * 0.26),
                        P(0.50 + Math.Cos(a) * 0.39, 0.50 + Math.Sin(a) * 0.39));
                }
                break;

            case SeaGlassGlyph.Back:
            {
                StreamGeometry g = new();
                using (StreamGeometryContext ctx = g.Open())
                {
                    ctx.BeginFigure(P(0.64, 0.18), false, false);
                    ctx.LineTo(P(0.28, 0.50), true, true);
                    ctx.LineTo(P(0.64, 0.82), true, true);
                }
                g.Freeze();
                dc.DrawGeometry(null, stroke, g);
                break;
            }
            case SeaGlassGlyph.Menu:
                dc.DrawLine(stroke, P(0.18, 0.28), P(0.82, 0.28));
                dc.DrawLine(stroke, P(0.18, 0.50), P(0.82, 0.50));
                dc.DrawLine(stroke, P(0.18, 0.72), P(0.62, 0.72));
                break;

            case SeaGlassGlyph.Layers:
            {
                StreamGeometry g = new();
                using (StreamGeometryContext ctx = g.Open())
                {
                    ctx.BeginFigure(P(0.50, 0.16), false, true);
                    ctx.LineTo(P(0.86, 0.36), true, true);
                    ctx.LineTo(P(0.50, 0.54), true, true);
                    ctx.LineTo(P(0.14, 0.36), true, true);
                    ctx.BeginFigure(P(0.16, 0.60), false, false);
                    ctx.LineTo(P(0.50, 0.78), true, true);
                    ctx.LineTo(P(0.84, 0.60), true, true);
                }
                g.Freeze();
                dc.DrawGeometry(null, stroke, g);
                break;
            }
            case SeaGlassGlyph.PhotoMountain:
            {
                // Rounded-square photo badge with twin mountain peaks cut out in negative space
                RectangleGeometry frameGeom = new(new Rect(ox + w * 0.10, oy + h * 0.10, w * 0.80, h * 0.80), w * 0.18, h * 0.18);
                frameGeom.Freeze();
                StreamGeometry mountains = new();
                using (StreamGeometryContext ctx = mountains.Open())
                {
                    ctx.BeginFigure(P(0.16, 0.78), true, true);
                    ctx.LineTo(P(0.40, 0.46), true, true);
                    ctx.LineTo(P(0.56, 0.68), true, true);
                    ctx.LineTo(P(0.68, 0.54), true, true);
                    ctx.LineTo(P(0.84, 0.78), true, true);
                }
                mountains.Freeze();
                CombinedGeometry combined = new(GeometryCombineMode.Exclude, frameGeom, mountains);
                combined.Freeze();
                dc.DrawGeometry(fill, null, combined);
                break;
            }
            case SeaGlassGlyph.Microphone:
            {
                // Capsule microphone head + U-shaped lower cradle arc + base stem
                double micR = w * 0.14;
                dc.DrawRoundedRectangle(null, stroke, new Rect(ox + w * 0.50 - micR, oy + h * 0.12, micR * 2.0, h * 0.44), micR, micR);
                StreamGeometry cradle = new();
                using (StreamGeometryContext ctx = cradle.Open())
                {
                    ctx.BeginFigure(P(0.76, 0.49), false, false);
                    ctx.ArcTo(P(0.24, 0.49), new System.Windows.Size(w * 0.26, h * 0.23), 0, false, SweepDirection.Clockwise, true, true);
                }
                cradle.Freeze();
                dc.DrawGeometry(null, stroke, cradle);
                dc.DrawLine(stroke, P(0.50, 0.72), P(0.50, 0.88));
                break;
            }
            case SeaGlassGlyph.BrandG:
            {
                // Bold geometric Google-style "G" emblem clipped to the exact outer circular boundary
                double minWh = Math.Min(w, h);
                double gStrokeW = Math.Max(2.8, minWh * 0.195);
                double radius = minWh * 0.35;
                double outerR = radius + gStrokeW * 0.5;
                EllipseGeometry outerCircle = new(P(0.50, 0.50), outerR, outerR);
                outerCircle.Freeze();
                dc.PushClip(outerCircle);

                Pen gArcPen = new(fill, gStrokeW)
                {
                    StartLineCap = PenLineCap.Flat,
                    EndLineCap = PenLineCap.Flat
                };
                gArcPen.Freeze();

                double startRad = -10.0 * (Math.PI / 180.0);
                double endRad = (-10.0 + 322.0) * (Math.PI / 180.0);
                Point cxCy = P(0.50, 0.50);
                Point startPt = new(cxCy.X + Math.Cos(startRad) * radius, cxCy.Y + Math.Sin(startRad) * radius);
                Point endPt = new(cxCy.X + Math.Cos(endRad) * radius, cxCy.Y + Math.Sin(endRad) * radius);

                StreamGeometry arcGeom = new();
                using (StreamGeometryContext ctx = arcGeom.Open())
                {
                    ctx.BeginFigure(startPt, false, false);
                    ctx.ArcTo(endPt, new System.Windows.Size(radius, radius), 0, true, SweepDirection.Clockwise, true, true);
                }
                arcGeom.Freeze();
                dc.DrawGeometry(null, gArcPen, arcGeom);

                dc.DrawRectangle(
                    fill,
                    null,
                    new Rect(cxCy.X - gStrokeW * 0.08, cxCy.Y - gStrokeW * 0.46, outerR + gStrokeW * 0.2, gStrokeW * 0.92));
                dc.Pop();
                break;
            }
            default:
            {
                double[] heights = [0.32, 0.68, 0.48, 0.80, 0.38];
                for (int i = 0; i < heights.Length; i++)
                {
                    double x = 0.18 + i * 0.16;
                    double half = heights[i] * 0.5;
                    dc.DrawLine(stroke, P(x, 0.5 - half), P(x, 0.5 + half));
                }
                break;
            }
        }
    }

    private readonly record struct FormattedTextCacheKey(
        string Text,
        int ColorArgb,
        int FontSizeTenths,
        byte StyleFlags,
        byte AlphaByte,
        int MaxWidthPx);

    private static readonly Dictionary<FormattedTextCacheKey, FormattedText> FormattedTextCache = new(512);
    private static readonly SeaGlassVectorPath2D SharedPillPath = new();

    public static FormattedText CreateFormattedText(
        string text,
        int colorArgb,
        double fontSize = 13.0,
        bool bold = true,
        bool extraBold = false,
        bool mono = false,
        float alpha = 1f,
        double maxWidth = 0.0)
    {
        byte flags = (byte)((bold ? 1 : 0) | (extraBold ? 2 : 0) | (mono ? 4 : 0));
        byte alphaByte = (byte)Math.Clamp((int)MathF.Round(alpha * 255f), 0, 255);
        int maxW = maxWidth > 4.0 ? (int)Math.Round(maxWidth) : 0;
        FormattedTextCacheKey key = new(text, colorArgb, (int)Math.Round(fontSize * 10.0), flags, alphaByte, maxW);

        if (FormattedTextCache.TryGetValue(key, out FormattedText? cached))
        {
            return cached;
        }

        if (FormattedTextCache.Count >= 1024)
        {
            FormattedTextCache.Clear();
        }

        SolidColorBrush brush = new(ToMediaColor(colorArgb, alpha));
        brush.Freeze();

        Typeface tf = mono ? MonoTypeface : (extraBold ? ExtraBoldTypeface : (bold ? BoldTypeface : SemiBoldTypeface));
        FormattedText ft = new(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            tf,
            fontSize,
            brush,
            1.25);

        if (maxWidth > 4.0)
        {
            ft.MaxTextWidth = maxWidth;
            ft.MaxLineCount = 2;
            ft.Trimming = TextTrimming.CharacterEllipsis;
        }

        FormattedTextCache[key] = ft;
        return ft;
    }

    public static void DrawLabel(
        DrawingContext dc,
        string text,
        Point origin,
        int colorArgb,
        double fontSize = 13.0,
        bool bold = true,
        bool extraBold = false,
        bool mono = false,
        float alpha = 1f,
        double maxWidth = 0.0)
    {
        if (string.IsNullOrEmpty(text)) return;
        SeaGlassColorMath.OklabColor lab = SeaGlassColorMath.ArgbToOklab(colorArgb);
        bool isLightInk = lab.L > 0.55f;
        int lipArgb = isLightInk ? unchecked((int)0xFF040C18) : unchecked((int)0xFFFFFFFF);
        float lipAlpha = (isLightInk ? 0.62f : 0.86f) * alpha;
        Point lipOffset = isLightInk ? new Point(origin.X - 0.35, origin.Y - 0.85) : new Point(origin.X + 0.40, origin.Y + 1.05);
        FormattedText lipFt = CreateFormattedText(text, lipArgb, fontSize, bold, extraBold, mono, lipAlpha, maxWidth);
        dc.DrawText(lipFt, lipOffset);

        FormattedText ft = CreateFormattedText(text, colorArgb, fontSize, bold, extraBold, mono, alpha, maxWidth);
        dc.DrawText(ft, origin);
    }

    public static void DrawCenteredLabel(
        DrawingContext dc,
        string text,
        Point center,
        int colorArgb,
        double fontSize = 13.0,
        bool bold = true,
        bool extraBold = false,
        bool mono = false,
        float alpha = 1f)
    {
        if (string.IsNullOrEmpty(text)) return;
        FormattedText ft = CreateFormattedText(text, colorArgb, fontSize, bold, extraBold, mono, alpha);
        Point origin = new(center.X - ft.Width * 0.5, center.Y - ft.Height * 0.5);

        SeaGlassColorMath.OklabColor lab = SeaGlassColorMath.ArgbToOklab(colorArgb);
        bool isLightInk = lab.L > 0.55f;
        int lipArgb = isLightInk ? unchecked((int)0xFF040C18) : unchecked((int)0xFFFFFFFF);
        float lipAlpha = (isLightInk ? 0.62f : 0.86f) * alpha;
        Point lipOffset = isLightInk ? new Point(origin.X - 0.35, origin.Y - 0.85) : new Point(origin.X + 0.40, origin.Y + 1.05);
        FormattedText lipFt = CreateFormattedText(text, lipArgb, fontSize, bold, extraBold, mono, lipAlpha);
        dc.DrawText(lipFt, lipOffset);

        dc.DrawText(ft, origin);
    }

    /// <summary>
    /// Renders an optically recessed sea-glass well / trough carved into a host glass surface.
    /// Uses Oklab Beer-Lambert depth shading, sand-grain micro-texture, and an inverted bevel rim
    /// (top-left inner shadow + bottom-right chalky catch-light lip).
    /// </summary>
    public static void DrawRecessedGlassWell(
        DrawingContext dc,
        Rect bounds,
        double cornerRadius,
        SeaGlassMaterial hostMaterial,
        float depth = 1.0f)
    {
        if (bounds.Width <= 2 || bounds.Height <= 2) return;
        double r = Math.Clamp(cornerRadius, 2.0, Math.Min(bounds.Width, bounds.Height) * 0.5);
        RectangleGeometry wellGeom = new(bounds, r, r);
        wellGeom.Freeze();

        SeaGlassColorMath.OklabColor lab = SeaGlassColorMath.ArgbToOklab(hostMaterial.TintColorArgb);
        bool isDark = lab.L < 0.48f;

        int topWellArgb = isDark
            ? SeaGlassColorMath.LerpOklab(hostMaterial.TintColorArgb, unchecked((int)0xFF030B14), 0.72f)
            : SeaGlassColorMath.LerpOklab(hostMaterial.TintColorArgb, unchecked((int)0xFF0A2238), 0.24f * depth);
        int bottomWellArgb = isDark
            ? SeaGlassColorMath.LerpOklab(hostMaterial.TintColorArgb, unchecked((int)0xFF081B2E), 0.55f)
            : SeaGlassColorMath.LerpOklab(hostMaterial.TintColorArgb, unchecked((int)0xFFFFFFFF), 0.18f);

        float fillAlpha = isDark ? 0.52f * depth : 0.24f * depth;
        LinearGradientBrush wellFill = new(
            ToRgbColor(topWellArgb, fillAlpha),
            ToRgbColor(bottomWellArgb, fillAlpha * 0.82f),
            new Point(0, 0),
            new Point(0, 1));
        wellFill.Freeze();

        dc.PushClip(wellGeom);
        dc.DrawRectangle(wellFill, null, bounds);

        float grainAlpha = Math.Clamp(hostMaterial.Coarseness * 0.22f * depth, 0f, 0.28f);
        if (grainAlpha > 0.01f)
        {
            dc.PushOpacity(grainAlpha);
            dc.DrawRectangle(ObtainSandGrainBrush(), null, bounds);
            dc.Pop();
        }

        // Inverted bevel: dark top-left inner shadow transitioning to bright bottom-right catch-light
        LinearGradientBrush bevelBrush = new(
            new GradientStopCollection
            {
                new GradientStop(Color.FromArgb((byte)(isDark ? 140 : 68), 4, 16, 28), 0.0),
                new GradientStop(Color.FromArgb(18, 255, 255, 255), 0.48),
                new GradientStop(Color.FromArgb((byte)(isDark ? 120 : 195), 255, 255, 255), 1.0)
            },
            new Point(0, 0),
            new Point(1, 1));
        bevelBrush.Freeze();
        Pen bevelPen = new(bevelBrush, 1.4);
        bevelPen.Freeze();
        dc.DrawGeometry(null, bevelPen, wellGeom);
        dc.Pop();
    }

    /// <summary>
    /// Renders a 3D convex water-drop SeaGlass lens (used for switch thumbs, slider thumbs,
    /// radio inner droplets, and raised glass medallions) using the exact SeaGlass material optics.
    /// </summary>
    public static void DrawWaterDropLens(
        DrawingContext dc,
        Rect bounds,
        double cornerRadius,
        SeaGlassMaterial material,
        bool isActive = false)
    {
        if (bounds.Width <= 2 || bounds.Height <= 2) return;
        double r = Math.Clamp(cornerRadius, 2.0, Math.Min(bounds.Width, bounds.Height) * 0.5);
        RectangleGeometry lensGeom = new(bounds, r, r);
        lensGeom.Freeze();

        // Elevation shadow
        int shadowArgb = SeaGlassColorMath.LerpOklab(material.TintColorArgb, unchecked((int)0xFF04101C), 0.72f);
        dc.PushTransform(new TranslateTransform(0.0, 2.2));
        SolidColorBrush shadowBrush = new(ToRgbColor(shadowArgb, isActive ? 0.28f : 0.20f));
        shadowBrush.Freeze();
        dc.DrawGeometry(shadowBrush, null, lensGeom);
        dc.Pop();

        dc.PushClip(lensGeom);

        // Crystal-clear refractive water-drop interior (transparent center + lower-right focused caustic)
        int causticArgb = SeaGlassColorMath.ComputeSubsurfaceCausticColor(material.TintColorArgb, material.Coarseness);
        LinearGradientBrush bodyBrush = new(
            new GradientStopCollection
            {
                new GradientStop(Color.FromArgb(76, 255, 255, 255), 0.00),
                new GradientStop(Color.FromArgb(0, 255, 255, 255), 0.36),
                new GradientStop(ToRgbColor(causticArgb, 0.28f), 0.68),
                new GradientStop(Color.FromArgb(158, 255, 255, 255), 1.00)
            },
            new Point(0.0, 0.0),
            new Point(1.0, 1.0));
        bodyBrush.Freeze();
        dc.DrawRectangle(bodyBrush, null, bounds);

        // Prismatic Fresnel meniscus inner slope + crisp contact lip
        int rimTint = SeaGlassColorMath.LerpOklab(material.TintColorArgb, unchecked((int)0xFFFFFFFF), 0.88f);
        LinearGradientBrush rimBrush = new(
            new GradientStopCollection
            {
                new GradientStop(Color.FromArgb(248, 255, 255, 255), 0.0),
                new GradientStop(Color.FromArgb(88, 255, 255, 255), 0.42),
                new GradientStop(ToRgbColor(rimTint, 0.92f), 1.0)
            },
            new Point(0, 0),
            new Point(1, 1));
        rimBrush.Freeze();
        Pen rimPen = new(rimBrush, 2.2);
        rimPen.Freeze();
        dc.DrawGeometry(null, rimPen, lensGeom);

        // Upper-left convex dome specular glint
        Point crownPt = new(bounds.X + bounds.Width * 0.32, bounds.Y + bounds.Height * 0.25);
        double crownRx = Math.Max(3.0, bounds.Width * 0.32);
        double crownRy = Math.Max(2.5, bounds.Height * 0.24);
        RadialGradientBrush crownBrush = new(
            Color.FromArgb(235, 255, 255, 255),
            Color.FromArgb(0, 255, 255, 255));
        crownBrush.Freeze();
        dc.DrawEllipse(crownBrush, null, crownPt, crownRx, crownRy);

        dc.Pop();
    }

    /// <summary>
    /// Renders a nested frosted SeaGlass pill, chip, segment indicator, or asymmetric corner pod
    /// with Oklab volumetric tinting, sand-grain texture, and chalky meniscus rim.
    /// </summary>
    public static void DrawGlassPill(
        DrawingContext dc,
        Rect bounds,
        SeaGlassMaterial material,
        SeaGlassCornerRadiiPx? customRadii = null,
        float opacityScale = 1.0f,
        bool elevated = true)
    {
        if (bounds.Width <= 2 || bounds.Height <= 2) return;

        Geometry geom;
        if (customRadii is { } cr && cr.IsAsymmetric)
        {
            SharedPillPath.Reset();
            SharedPillPath.AddRoundRect(
                (float)bounds.Left,
                (float)bounds.Top,
                (float)bounds.Right,
                (float)bounds.Bottom,
                cr.TopLeftPx,
                cr.TopRightPx,
                cr.BottomRightPx,
                cr.BottomLeftPx);
            geom = BuildGeometryFromPath(SharedPillPath);
        }
        else
        {
            double rawR = customRadii is { } uCr ? uCr.TopLeftPx : material.CornerRadiusDp;
            double r = Math.Clamp(rawR, 4.0, Math.Min(bounds.Width, bounds.Height) * 0.5);
            RectangleGeometry rg = new(bounds, r, r);
            rg.Freeze();
            geom = rg;
        }

        if (elevated)
        {
            int shadowArgb = SeaGlassColorMath.LerpOklab(material.TintColorArgb, unchecked((int)0xFF04101C), 0.72f);
            dc.PushTransform(new TranslateTransform(0.0, 2.0));
            SolidColorBrush shadowBrush = new(ToRgbColor(shadowArgb, 0.14f * opacityScale));
            shadowBrush.Freeze();
            dc.DrawGeometry(shadowBrush, null, geom);
            dc.Pop();
        }

        dc.PushClip(geom);

        SeaGlassColorMath.OklabColor lab = SeaGlassColorMath.ArgbToOklab(material.TintColorArgb);
        bool isDark = lab.L < 0.48f;
        int startArgb = isDark
            ? SeaGlassColorMath.LerpOklab(material.TintColorArgb, unchecked((int)0xFFFFFFFF), 0.12f)
            : SeaGlassColorMath.LerpOklab(material.TintColorArgb, unchecked((int)0xFFFFFFFF), 0.36f + material.Coarseness * 0.12f);
        int endArgb = isDark
            ? material.TintColorArgb
            : SeaGlassColorMath.LerpOklab(material.TintColorArgb, unchecked((int)0xFFFFFFFF), 0.16f + material.Coarseness * 0.10f);

        float fillAlpha = Math.Clamp((isDark ? 0.68f : 0.64f) * opacityScale, 0.12f, 0.94f);
        LinearGradientBrush fillBrush = new(
            ToRgbColor(startArgb, fillAlpha),
            ToRgbColor(endArgb, fillAlpha * 0.90f),
            new Point(0, 0),
            new Point(1, 1));
        fillBrush.Freeze();
        dc.DrawRectangle(fillBrush, null, bounds);

        float grainAlpha = Math.Clamp(material.Coarseness * 0.30f * opacityScale, 0f, 0.42f);
        if (grainAlpha > 0.01f)
        {
            dc.PushOpacity(grainAlpha);
            dc.DrawRectangle(ObtainSandGrainBrush(), null, bounds);
            dc.Pop();
        }

        int crestRgb = SeaGlassColorMath.LerpOklab(material.TintColorArgb, unchecked((int)0xFFFFFFFF), 0.88f);
        LinearGradientBrush rimBrush = new(
            new GradientStopCollection
            {
                new GradientStop(ToRgbColor(0x00FFFFFF, 0.88f * opacityScale), 0.0),
                new GradientStop(ToRgbColor(crestRgb, 0.35f * opacityScale), 0.48),
                new GradientStop(ToRgbColor(crestRgb, 0.76f * opacityScale), 1.0)
            },
            new Point(0, 0),
            new Point(1, 1));
        rimBrush.Freeze();
        Pen rimPen = new(rimBrush, 2.0) { LineJoin = PenLineJoin.Round };
        rimPen.Freeze();
        dc.DrawGeometry(null, rimPen, geom);

        dc.Pop();
    }

    /// <summary>
    /// Renders a complete SeaGlass Liquid Slider row with label, frosted value pill badge,
    /// recessed glass trough, Oklab volumetric fill bar, and a 3D water-drop lens thumb.
    /// </summary>
    public static void DrawLiquidSliderRow(
        DrawingContext dc,
        string label,
        string valueBadge,
        float normalizedValue,
        double x,
        double y,
        double width,
        int contentColorArgb,
        SeaGlassMaterial hostMaterial,
        SeaGlassMaterial accentMaterial)
    {
        double clampedWidth = Math.Max(80.0, width);
        float norm = Math.Clamp(normalizedValue, 0.0f, 1.0f);

        // Parameter label on left
        DrawLabel(dc, label, new Point(x, y), contentColorArgb, 12.0, bold: true);

        // Frosted glass value badge on right
        Rect badgeRect = new(x + clampedWidth - 64, y - 2, 64, 20);
        DrawGlassPill(dc, badgeRect, accentMaterial with { CornerRadiusDp = 10f }, opacityScale: 0.72f, elevated: false);
        DrawCenteredLabel(
            dc,
            valueBadge,
            new Point(badgeRect.X + badgeRect.Width * 0.5, badgeRect.Y + badgeRect.Height * 0.5),
            contentColorArgb,
            10.5,
            bold: true,
            mono: true);

        // Recessed glass slider trough
        double trackY = y + 22;
        double trackH = 10.0;
        Rect trackRect = new(x, trackY, clampedWidth, trackH);
        DrawRecessedGlassWell(dc, trackRect, trackH * 0.5, hostMaterial, depth: 0.95f);

        // Luminous Oklab liquid fill bar
        double thumbW = 22.0;
        double thumbH = 16.0;
        double usableSpan = Math.Max(4.0, clampedWidth - thumbW);
        double thumbCenterX = x + thumbW * 0.5 + norm * usableSpan;
        double fillW = Math.Clamp(thumbCenterX - x, trackH, clampedWidth);

        int fillStart = SeaGlassColorMath.LerpOklab(accentMaterial.TintColorArgb, unchecked((int)0xFFFFFFFF), 0.15f);
        int fillEnd = SeaGlassColorMath.LerpOklab(accentMaterial.TintColorArgb, unchecked((int)0xFFFFFFFF), 0.45f);
        LinearGradientBrush fillBrush = new(
            ToRgbColor(fillStart, 0.88f),
            ToRgbColor(fillEnd, 0.94f),
            new Point(0, 0),
            new Point(1, 0));
        fillBrush.Freeze();
        Pen fillRim = new(new SolidColorBrush(Color.FromArgb(150, 255, 255, 255)), 0.8);
        fillRim.Freeze();
        dc.DrawRoundedRectangle(fillBrush, fillRim, new Rect(x, trackY, fillW, trackH), trackH * 0.5, trackH * 0.5);

        // 3D Water-Drop Lens Thumb
        Rect thumbRect = new(thumbCenterX - thumbW * 0.5, trackY + trackH * 0.5 - thumbH * 0.5, thumbW, thumbH);
        DrawWaterDropLens(dc, thumbRect, thumbH * 0.5, accentMaterial, isActive: true);
    }
}
