using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using SeaGlass.Core;
using SeaGlass.Core.Backend;
using SeaGlass.WinUI;
using SeaGlass.WinUI.Rendering;

namespace SeaGlass.Sample;

/// <summary>
/// Exact 1:1 port of <c>ExpressiveTidalBackdrop</c> from <c>android/sample/.../MainActivity.kt</c> (lines 2191–2487):
/// Luminous, palette-harmonized Material 3 Expressive coastal lightscape with rotated silk ribbons,
/// sunlit coral/amber bokeh spheres, luminous pebbles &amp; rings, cubic Bezier caustic wave filaments,
/// and editorial media cards behind the Mini-Player &amp; Bottom Fluid Bar.
/// </summary>
public sealed class ExpressiveTidalBackdropCanvas : FrameworkElement
{
    public double Phase { get; set; }

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth > 1 ? ActualWidth : 1420;
        double h = ActualHeight > 1 ? ActualHeight : 940;

        // Strictly clip to (0, 0, w, h) so VisualBrush RelativeToBoundingBox maps [0..1] to the exact stage viewport!
        dc.PushClip(new RectangleGeometry(new Rect(0, 0, w, h)));

        SeaGlassPalette palette = SeaGlassTheme.Current.Palette;
        bool isDark = palette.IsDark;

        Color bgTop = isDark
            ? Color.FromRgb(0x07, 0x12, 0x1E)
            : SeaGlassXamlRenderer.ToMediaColor(SeaGlassColorMath.LerpOklab(palette.PrimaryGlass.TintColorArgb, unchecked((int)0xFF102A44), 0.52f));
        Color bgMid = isDark
            ? Color.FromRgb(0x0C, 0x1F, 0x34)
            : SeaGlassXamlRenderer.ToMediaColor(SeaGlassColorMath.LerpOklab(palette.TertiaryGlass.TintColorArgb, unchecked((int)0xFF163858), 0.46f));
        Color bgBottom = isDark
            ? Color.FromRgb(0x11, 0x2A, 0x44)
            : SeaGlassXamlRenderer.ToMediaColor(SeaGlassColorMath.LerpOklab(palette.SecondaryGlass.TintColorArgb, unchecked((int)0xFF0E2942), 0.50f));

        LinearGradientBrush bgBrush = new(
            new GradientStopCollection
            {
                new GradientStop(bgTop, 0.0),
                new GradientStop(bgMid, 0.5),
                new GradientStop(bgBottom, 1.0)
            },
            new Point(0, 0),
            new Point(0, 1));
        bgBrush.Freeze();
        dc.DrawRectangle(bgBrush, null, new Rect(0, 0, w, h));

        double dx = Math.Cos(Phase) * w * 0.08;
        double dy = Math.Sin(Phase) * h * 0.04;

        Color primaryGlow = SeaGlassXamlRenderer.ToMediaColor(palette.PrimaryGlass.TintColorArgb);
        Color secondaryGlow = SeaGlassXamlRenderer.ToMediaColor(palette.SecondaryGlass.TintColorArgb);
        Color tertiaryGlow = SeaGlassXamlRenderer.ToMediaColor(palette.TertiaryGlass.TintColorArgb);
        Color sunlitPeach = Color.FromRgb(0xFF, 0x8E, 0x6E);
        Color goldenSand = Color.FromRgb(0xFF, 0xCA, 0x58);
        Color aquaCaustic = Color.FromRgb(0x68, 0xF4, 0xFF);

        // 1. Soft glowing atmospheric bokeh spheres
        DrawRadialSphere(dc, new Point(w * 0.20 + dx, h * 0.18 + dy), w * 0.45, primaryGlow, 0.78);
        DrawRadialSphere(dc, new Point(w * 0.82 - dx * 0.7, h * 0.34 - dy), w * 0.42, sunlitPeach, 0.72);
        DrawRadialSphere(dc, new Point(w * 0.24 - dy, h * 0.62 + dx * 0.5), w * 0.44, tertiaryGlow, 0.76);
        DrawRadialSphere(dc, new Point(w * 0.78 + dy, h * 0.84 - dx * 0.4), w * 0.40, goldenSand, 0.70);

        // 2. Flowing Material 3 Expressive Silk Ribbons (-14°, +12°, -10°)
        DrawRotatedSilkRibbon(
            dc,
            angleDeg: -14.0,
            pivot: new Point(w * 0.5, h * 0.22),
            rect: new Rect(-w * 0.25 + dx * 0.35, h * 0.15, w * 1.50, 96),
            c0: WithAlpha(primaryGlow, 0.88),
            c1: WithAlpha(goldenSand, 0.86),
            c2: WithAlpha(tertiaryGlow, 0.88));

        DrawRotatedSilkRibbon(
            dc,
            angleDeg: 12.0,
            pivot: new Point(w * 0.5, h * 0.44),
            rect: new Rect(-w * 0.25 - dx * 0.30, h * 0.38, w * 1.50, 104),
            c0: WithAlpha(tertiaryGlow, 0.86),
            c1: WithAlpha(sunlitPeach, 0.84),
            c2: WithAlpha(secondaryGlow, 0.88));

        DrawRotatedSilkRibbon(
            dc,
            angleDeg: -10.0,
            pivot: new Point(w * 0.5, h * 0.68),
            rect: new Rect(-w * 0.25 + dx * 0.25, h * 0.62, w * 1.50, 98),
            c0: WithAlpha(sunlitPeach, 0.82),
            c1: WithAlpha(primaryGlow, 0.88),
            c2: WithAlpha(aquaCaustic, 0.84));

        // 3. Crisp Luminous Pebbles & Concentric Rings for Meniscus Refraction Calibration
        Pen whiteRingPen = new(new SolidColorBrush(Color.FromArgb(115, 255, 255, 255)), 3.0);
        whiteRingPen.Freeze();

        Point p1 = new(w * 0.26 - dx * 0.4, h * 0.29 + dy);
        dc.DrawEllipse(new SolidColorBrush(WithAlpha(goldenSand, 0.92)), null, p1, 48, 48);
        dc.DrawEllipse(null, whiteRingPen, p1, 60, 60);

        Point p2 = new(w * 0.74 + dx * 0.35, h * 0.48 - dy);
        dc.DrawEllipse(new SolidColorBrush(WithAlpha(sunlitPeach, 0.90)), null, p2, 54, 54);
        dc.DrawEllipse(null, whiteRingPen, p2, 68, 68);

        Point p3 = new(w * 0.32 + dy, h * 0.74);
        dc.DrawEllipse(new SolidColorBrush(WithAlpha(aquaCaustic, 0.88)), null, p3, 48, 48);

        // 4. Luminous Caustic Wave Filaments
        LinearGradientBrush waveBrush = new(
            new GradientStopCollection
            {
                new GradientStop(Color.FromArgb(40, 255, 255, 255), 0.0),
                new GradientStop(Color.FromArgb(160, 255, 255, 255), 0.35),
                new GradientStop(WithAlpha(aquaCaustic, 0.58), 0.68),
                new GradientStop(Color.FromArgb(40, 255, 255, 255), 1.0)
            },
            new Point(0, 0),
            new Point(1, 0));
        waveBrush.Freeze();
        Pen wavePen = new(waveBrush, 3.0);
        wavePen.Freeze();

        for (int i = 0; i <= 3; i++)
        {
            double yBase = h * (0.20 + i * 0.21);
            StreamGeometry waveGeom = new();
            using (StreamGeometryContext ctx = waveGeom.Open())
            {
                ctx.BeginFigure(new Point(0, yBase), false, false);
                ctx.BezierTo(
                    new Point(w * 0.33, yBase - 34.0 * Math.Sin(Phase + i * 0.9)),
                    new Point(w * 0.66, yBase + 34.0 * Math.Cos(Phase + i * 0.9)),
                    new Point(w, yBase),
                    true,
                    true);
            }
            waveGeom.Freeze();
            dc.DrawGeometry(null, wavePen, waveGeom);
        }

        // 5. High-Contrast Editorial Media Cards & Headlines directly behind the Mini-Player & Bottom Tab Bar
        double driftX = Math.Sin(Phase * 0.7) * 18.0;
        LinearGradientBrush card1 = new(Color.FromRgb(0xFF, 0x6B, 0x98), Color.FromRgb(0x5A, 0x98, 0xFF), new Point(0, 0), new Point(1, 1));
        card1.Freeze();
        dc.DrawRoundedRectangle(card1, null, new Rect(w * 0.05 + driftX, h * 0.77, w * 0.42, 62), 14, 14);

        LinearGradientBrush card2 = new(Color.FromRgb(0x7A, 0xF5, 0xC8), Color.FromRgb(0x14, 0xA8, 0x78), new Point(0, 0), new Point(1, 1));
        card2.Freeze();
        dc.DrawRoundedRectangle(card2, null, new Rect(w * 0.51 + driftX, h * 0.77, w * 0.44, 62), 14, 14);

        SeaGlassXamlRenderer.DrawLabel(dc, "Chill House Beats", new Point(w * 0.05 + driftX, h * 0.842), unchecked((int)0xEBFFFFFF), 17, bold: true, extraBold: true);
        SeaGlassXamlRenderer.DrawLabel(dc, "Coastal Lounge", new Point(w * 0.05 + driftX, h * 0.868), unchecked((int)0xCCE0F4FF), 14, bold: false);

        SeaGlassXamlRenderer.DrawLabel(dc, "Tropical House Essentials", new Point(w * 0.50 + driftX, h * 0.842), unchecked((int)0xEBFFFFFF), 17, bold: true, extraBold: true);
        SeaGlassXamlRenderer.DrawLabel(dc, "SeaGlass Dance", new Point(w * 0.50 + driftX, h * 0.868), unchecked((int)0xCCE0F4FF), 14, bold: false);

        LinearGradientBrush card3 = new(Color.FromRgb(0x3E, 0x8E, 0xFF), Color.FromRgb(0xA8, 0xDC, 0xFF), new Point(0, 0), new Point(1, 1));
        card3.Freeze();
        dc.DrawRoundedRectangle(card3, null, new Rect(w * 0.05 - driftX * 0.6, h * 0.905, w * 0.42, 84), 14, 14);

        LinearGradientBrush card4 = new(Color.FromRgb(0xFF, 0xB2, 0x48), Color.FromRgb(0x32, 0xD8, 0xB4), new Point(0, 0), new Point(1, 1));
        card4.Freeze();
        dc.DrawRoundedRectangle(card4, null, new Rect(w * 0.51 - driftX * 0.6, h * 0.905, w * 0.44, 84), 14, 14);

        dc.Pop(); // Pop stage viewport clip
    }

    private static Color WithAlpha(Color c, double alpha) =>
        Color.FromArgb((byte)Math.Clamp((int)(alpha * 255), 0, 255), c.R, c.G, c.B);

    private static void DrawRadialSphere(DrawingContext dc, Point center, double radius, Color color, double alpha)
    {
        RadialGradientBrush b = new(
            WithAlpha(color, alpha),
            Color.FromArgb(0, color.R, color.G, color.B));
        b.Freeze();
        dc.DrawEllipse(b, null, center, radius, radius);
    }

    private static void DrawRotatedSilkRibbon(
        DrawingContext dc,
        double angleDeg,
        Point pivot,
        Rect rect,
        Color c0,
        Color c1,
        Color c2)
    {
        dc.PushTransform(new RotateTransform(angleDeg, pivot.X, pivot.Y));
        LinearGradientBrush b = new(
            new GradientStopCollection
            {
                new GradientStop(c0, 0.0),
                new GradientStop(c1, 0.5),
                new GradientStop(c2, 1.0)
            },
            new Point(0, 0),
            new Point(1, 0));
        b.Freeze();
        double r = rect.Height * 0.5;
        dc.DrawRoundedRectangle(b, null, rect, r, r);
        dc.Pop();
    }
}

/// <summary>
/// Hardware-accelerated SeaGlass visual control that samples the live blurred &amp; sharp
/// <see cref="SeaGlassStageBackdropVisuals"/> and renders authentic Water-Drop + Sanded SeaGlass optics.
/// </summary>
public sealed class SeaGlassVisualControl : Border
{
    private Point? _localPointerPos;

    public SeaGlassSurface SurfaceModel { get; set; }
    public SeaGlassStageBackdropVisuals? StageBackdrop { get; set; }
    public UIElement? StageCoordinateRoot { get; set; }
    public Func<float, SeaGlassSurfaceFrameState>? CustomStepEvaluator { get; set; }
    public Func<IReadOnlyList<SeaGlassSdfEngine.SdfNode>?>? ActiveNodesProvider { get; set; }
    public float SmoothRadiusDp { get; set; } = 28f;
    public Action<DrawingContext, SeaGlassSurfaceFrameState, double, double>? CustomOverlayPainter { get; set; }
    public SeaGlassSurfaceFrameState? LastFrameState { get; private set; }

    public SeaGlassVisualControl(
        SeaGlassSurface surfaceModel,
        SeaGlassStageBackdropVisuals? stageBackdrop = null,
        UIElement? stageCoordinateRoot = null,
        Func<float, SeaGlassSurfaceFrameState>? customStepEvaluator = null,
        Action<DrawingContext, SeaGlassSurfaceFrameState, double, double>? customOverlayPainter = null,
        Func<IReadOnlyList<SeaGlassSdfEngine.SdfNode>?>? activeNodesProvider = null,
        float smoothRadiusDp = 28f)
    {
        SurfaceModel = surfaceModel;
        StageBackdrop = stageBackdrop;
        StageCoordinateRoot = stageCoordinateRoot;
        CustomStepEvaluator = customStepEvaluator;
        CustomOverlayPainter = customOverlayPainter;
        ActiveNodesProvider = activeNodesProvider;
        SmoothRadiusDp = smoothRadiusDp;
        Background = Brushes.Transparent;
        Cursor = Cursors.Hand;

        MouseDown += (_, e) =>
        {
            Point pt = e.GetPosition(this);
            _localPointerPos = pt;
            SurfaceModel.OnPointerStateChanged(new Vector2((float)pt.X, (float)pt.Y), true);
            InvalidateVisual();
        };
        MouseMove += (_, e) =>
        {
            Point pt = e.GetPosition(this);
            _localPointerPos = pt;
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                SurfaceModel.OnPointerStateChanged(new Vector2((float)pt.X, (float)pt.Y), true);
            }
            InvalidateVisual();
        };
        MouseUp += (_, e) =>
        {
            Point pt = e.GetPosition(this);
            _localPointerPos = pt;
            SurfaceModel.OnPointerStateChanged(new Vector2((float)pt.X, (float)pt.Y), false);
            InvalidateVisual();
        };
        MouseLeave += (_, _) =>
        {
            _localPointerPos = null;
            if (SurfaceModel.IsPointerDown)
            {
                SurfaceModel.OnPointerStateChanged(null, false);
            }
            InvalidateVisual();
        };
    }

    public void StepPhysics(float dtSeconds)
    {
        if (ActualWidth > 1 && ActualHeight > 1)
        {
            SurfaceModel.SizePx = new Vector2((float)ActualWidth, (float)ActualHeight);
        }

        if (StageCoordinateRoot is not null)
        {
            try
            {
                if (StageBackdrop is not null && StageCoordinateRoot.RenderSize.Width > 1)
                {
                    StageBackdrop.StageWidth = StageCoordinateRoot.RenderSize.Width;
                    StageBackdrop.StageHeight = StageCoordinateRoot.RenderSize.Height;
                }
                Point ptInStage = TransformToVisual(StageCoordinateRoot).Transform(new Point(0, 0));
                SurfaceModel.OriginInRoot = new Vector2((float)ptInStage.X, (float)ptInStage.Y);
            }
            catch
            {
                // Ignore if visual tree is mid-rebuild
            }
        }

        LastFrameState = CustomStepEvaluator is not null
            ? CustomStepEvaluator(dtSeconds)
            : SurfaceModel.EvaluateFrame(dtSeconds, SeaGlassTheme.Current);

        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth > 1 ? ActualWidth : SurfaceModel.SizePx.X;
        double h = ActualHeight > 1 ? ActualHeight : SurfaceModel.SizePx.Y;

        LastFrameState ??= CustomStepEvaluator is not null
            ? CustomStepEvaluator(1f / 60f)
            : SurfaceModel.EvaluateFrame(1f / 60f, SeaGlassTheme.Current);

        Point originInStage = new(SurfaceModel.OriginInRoot.X, SurfaceModel.OriginInRoot.Y);
        if (StageCoordinateRoot is not null)
        {
            try
            {
                if (StageBackdrop is not null && StageCoordinateRoot.RenderSize.Width > 1)
                {
                    StageBackdrop.StageWidth = StageCoordinateRoot.RenderSize.Width;
                    StageBackdrop.StageHeight = StageCoordinateRoot.RenderSize.Height;
                }
                originInStage = TransformToVisual(StageCoordinateRoot).Transform(new Point(0, 0));
            }
            catch
            {
                // Use cached OriginInRoot
            }
        }

        IReadOnlyList<SeaGlassSdfEngine.SdfNode>? nodes = ActiveNodesProvider?.Invoke();

        SeaGlassXamlRenderer.DrawSeaGlassSurface(
            dc: dc,
            frameState: LastFrameState,
            width: w,
            height: h,
            backdropSampleArgb: unchecked((int)0xFF143854),
            localPointerPos: _localPointerPos,
            isPointerDown: SurfaceModel.IsPointerDown,
            stageBackdrop: StageBackdrop,
            elementOriginInStage: originInStage,
            activeNodes: nodes,
            smoothRadiusDp: SmoothRadiusDp,
            contentRole: SurfaceModel.ContentRole);

        CustomOverlayPainter?.Invoke(dc, LastFrameState, w, h);
    }
}

/// <summary>
/// Interactive SeaGlass Optical Lab, Expressive Showcase &amp; Multi-Column Gallery Window.
/// Exact 1:1 visual and functional parity with <c>android/sample/.../MainActivity.kt</c>.
/// </summary>
public sealed class MainWindow : Window
{
    private readonly SeaGlassStageState _stageState = new();
    private readonly SeaGlassStageBackdropVisuals _stageBackdropVisuals = new();
    private readonly ExpressiveTidalBackdropCanvas _sharpBackdropCanvas = new();
    private readonly ExpressiveTidalBackdropCanvas _frostedBackdropCanvas = new();
    private readonly Border _frostedBackdropHost;
    private readonly Grid _stageRootGrid = new();
    private readonly List<SeaGlassVisualControl> _activeControls = new();
    private readonly ContentControl _mainContentHost = new();
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private double _lastTimeSeconds;
    private double _autoCycleTimer;

    // Showcase state matching MainActivity.kt 1:1
    private int _selectedTab; // 0=Home(Optics), 1=Morph(Fluid), 2=Palettes, 3=Library(UI Kit), 4=Search(Backends), 5=All-in-One Gallery
    private bool _widescreenGalleryMode = true; // Shows all sections side-by-side in a rich gallery view by default!
    private bool _autoAnimateStates = true;

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
    private float _dropletSepDp = 100f;
    private float _dropletSepVel;
    private float _smoothBridgeDp = 56f;
    private Vector2 _dragOffsetPx = Vector2.Zero;

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
    private readonly SeaGlassAdaptiveCornerLabCard _cornerLabCard;
    private readonly SeaGlassMiniPlayerBar _miniPlayerBar;
    private readonly SeaGlassFluidBar _bottomFluidBar;

    private int _miniTrackIndex;
    private readonly (string Title, string Artist)[] _miniTracks =
    [
        ("A Good Song Never Dies", "Saint Motel"),
        ("Pacific Surf & Driftwood", "SeaGlass Spatial"),
        ("Tropical House Essentials", "Coastal Tide Mix")
    ];
    private ShaderDialect _selectedDialect = ShaderDialect.HlslSm50;

    public MainWindow()
    {
        Title = "SeaGlass Optical Lab & Material 3 Expressive Gallery (WinUI / Windows)";
        Width = 1420;
        Height = 940;
        MinWidth = 920;
        MinHeight = 720;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        _frostedBackdropHost = new Border
        {
            Child = _frostedBackdropCanvas,
            Effect = new BlurEffect
            {
                Radius = 38.0,
                KernelType = KernelType.Gaussian,
                RenderingBias = RenderingBias.Quality
            },
            IsHitTestVisible = false
        };

        _stageBackdropVisuals.SharpBackdropVisual = _sharpBackdropCanvas;
        _stageBackdropVisuals.FrostedBackdropVisual = _frostedBackdropHost;
        _stageState.BindBackdropTarget("ExpressiveTidalBackdrop", Vector2.Zero, new Vector2(1420f, 940f));

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
            restingShape: SeaGlassShapeKind.Cookie9,
            activeShape: SeaGlassShapeKind.Squircle);
        _morphBtn1 = new SeaGlassMorphingButton(
            active: true,
            onClick: () => _morphBtn1!.ToggleActive(),
            stageState: _stageState,
            restingShape: SeaGlassShapeKind.SoftBurst,
            activeShape: SeaGlassShapeKind.Circle);
        _morphBtn2 = new SeaGlassMorphingButton(
            active: false,
            onClick: () => _morphBtn2!.ToggleActive(),
            stageState: _stageState,
            restingShape: SeaGlassShapeKind.Cookie4,
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
        _cornerLabCard = new SeaGlassAdaptiveCornerLabCard(SeaGlassDeviceHardwareProfile.Pixel9Pro, _stageState);

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
                int nextPal = (IndexOfPalette(SeaGlassTheme.Current.Palette) + 1) % SeaGlassPalette.Entries.Count;
                SelectPalette(SeaGlassPalette.Entries[nextPal]);
            },
            stageState: _stageState);

        _bottomFluidBar = new SeaGlassFluidBar(
            items: ["Home", "Morph", "Palettes", "Library", "Search"],
            selectedIndex: 0,
            onSelect: idx =>
            {
                _selectedTab = Math.Clamp(idx, 0, 4);
                _widescreenGalleryMode = false;
                RebuildUI();
            },
            stageState: _stageState);

        BuildStageRoot();
        Content = _stageRootGrid;
        SelectPalette(SeaGlassPalette.SeafoamMint);

        CompositionTarget.Rendering += OnFrameTick;
    }

    public void SelectTabForPreview(int tabIndex, bool widescreenGallery)
    {
        _selectedTab = Math.Clamp(tabIndex, 0, 4);
        _bottomFluidBar.SelectTab(_selectedTab);
        _widescreenGalleryMode = widescreenGallery;
        RebuildUI();
    }

    private void OnFrameTick(object? sender, EventArgs e)
    {
        double now = _clock.Elapsed.TotalSeconds;
        float dt = (float)Math.Clamp(now - _lastTimeSeconds, 0.004, 0.05);
        _lastTimeSeconds = now;

        double phase = (now * (2.0 * Math.PI / 16.0)) % (2.0 * Math.PI);
        _sharpBackdropCanvas.Phase = phase;
        _frostedBackdropCanvas.Phase = phase;
        _sharpBackdropCanvas.InvalidateVisual();
        _frostedBackdropCanvas.InvalidateVisual();

        SeaGlassTheme.Current.TiltState.UpdateFrame(dt);

        // Step spring-animated gap & droplet separation
        SeaGlassSpringSpec spring54 = new(DampingRatio: 0.54f, Stiffness: 300f);
        float targetGap = _heroFlowState switch { 0 => -2f, 1 => 9f, _ => 22f };
        spring54.Step(ref _playOrbGapDp, ref _playOrbGapVel, targetGap, dt);
        spring54.Step(ref _dropletSepDp, ref _dropletSepVel, _coalescedPreset ? 100f : 168f, dt);

        if (_autoAnimateStates)
        {
            _autoCycleTimer += dt;
            if (_autoCycleTimer >= 2.2)
            {
                _autoCycleTimer = 0;
                _heroFlowState = (_heroFlowState + 1) % 3;
                _coalescedPreset = !_coalescedPreset;
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

    private SeaGlassMaterial CustomSurfaceMaterial =>
        SeaGlassTheme.Current.Palette.SurfaceGlass with
        {
            Transparency = Math.Clamp(_transparency + 0.04f, 0f, 0.95f),
            Coarseness = Math.Clamp(_coarseness + 0.05f, 0f, 1f),
            FrostRadiusDp = MathF.Min(56f, _frostRadiusDp + 3f),
            TintDensity = Math.Clamp(_tintDensity * 0.84f, 0.24f, 0.92f),
            ThicknessDp = _thicknessDp,
            Ior = _ior,
            ChromaticDispersion = _chromaticDispersion,
            RimChalkiness = Math.Clamp(_rimChalkiness + 0.06f, 0f, 1f)
        };

    private SeaGlassMaterial CustomTertiaryMaterial =>
        SeaGlassTheme.Current.Palette.TertiaryGlass with
        {
            Transparency = _transparency,
            Coarseness = _coarseness,
            FrostRadiusDp = _frostRadiusDp,
            Ior = _ior,
            ChromaticDispersion = _chromaticDispersion,
            ThicknessDp = _thicknessDp
        };

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

        _frostedBackdropHost.Effect = new BlurEffect
        {
            Radius = Math.Max(28.0, _frostRadiusDp * 1.65),
            KernelType = KernelType.Gaussian,
            RenderingBias = RenderingBias.Quality
        };

        SyncComponentMaterials();
        RebuildUI();
    }

    private void SyncComponentMaterials()
    {
        SeaGlassMaterial prim = CustomPrimaryMaterial;
        SeaGlassMaterial surf = CustomSurfaceMaterial;
        SeaGlassMaterial tert = CustomTertiaryMaterial;

        _shapeOrb.PrimaryMaterial = prim;
        _shapeOrb.SecondaryMaterial = tert;
        _buddingCluster.AnchorMaterial = prim;
        _buddingCluster.SatelliteMaterial = tert;
        _morphBtn0.RestingMaterial = prim; _morphBtn0.ActiveMaterial = tert;
        _morphBtn1.RestingMaterial = tert; _morphBtn1.ActiveMaterial = prim;
        _morphBtn2.RestingMaterial = prim; _morphBtn2.ActiveMaterial = tert;
        _twoElementMorph.CollapsedMaterial = prim with { CornerRadiusDp = 28f };
        _twoElementMorph.ExpandedMaterial = surf with { CornerRadiusDp = 32f };
        _contextMenuMorph.CollapsedMaterial = prim;
        _contextMenuMorph.ExpandedMaterial = surf;
        _cascadedModelMorph.PillMaterial = surf;
        _cascadedModelMorph.PopoverMaterial = surf;
        _contextRingMorph.CollapsedMaterial = surf;
        _contextRingMorph.ExpandedMaterial = surf;

        _segmentedRow.ContainerMaterial = surf;
        _segmentedRow.IndicatorMaterial = prim;
        _floatingToolbar.BarMaterial = surf;
        _floatingToolbar.FabMaterial = prim;
        _extendedFab.SetExpanded(_extendedFab.Expanded, prim);
        _switchA.CheckedMaterial = prim; _switchA.UncheckedMaterial = surf;
        _switchB.CheckedMaterial = tert; _switchB.UncheckedMaterial = surf;
        _checkA.CheckedMaterial = prim; _checkA.UncheckedMaterial = surf;
        _checkB.CheckedMaterial = tert; _checkB.UncheckedMaterial = surf;
        _radioA.SelectedMaterial = prim; _radioA.UnselectedMaterial = surf;
        _radioB.SelectedMaterial = prim; _radioB.UnselectedMaterial = surf;
        _circularProgress.Material = surf with { CornerRadiusDp = 26f, ThicknessDp = 15f };
        _linearProgress.TrackMaterial = surf;
        _linearProgress.FillMaterial = prim;
        _miniPlayerBar.Material = surf with { CornerRadiusDp = 28f, ThicknessDp = 18f, RimChalkiness = 0.88f };
        _bottomFluidBar.BarMaterial = surf;
        _bottomFluidBar.IndicatorMaterial = prim;
    }

    private void BuildStageRoot()
    {
        _stageRootGrid.Children.Clear();
        // 1. Frosted offscreen backdrop layer (sampled via VisualBrush by foreground glass interiors)
        _stageRootGrid.Children.Add(_frostedBackdropHost);
        // 2. Sharp visible backdrop layer (drawn directly to screen and sampled by meniscus rims)
        _stageRootGrid.Children.Add(_sharpBackdropCanvas);
        // 3. Foreground SeaGlass UI hierarchy
        _stageRootGrid.Children.Add(_mainContentHost);
    }

    public void RebuildUI()
    {
        _activeControls.Clear();
        SyncComponentMaterials();

        Grid rootOverlay = new();
        rootOverlay.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        rootOverlay.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        ScrollViewer scroll = new()
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Padding = new Thickness(18, 12, 18, 12)
        };

        if (_widescreenGalleryMode)
        {
            // Multi-Column Widescreen Gallery View showing Optics Lab, Fluid Morphs, and UI Kit Library side-by-side!
            StackPanel galleryRoot = new();
            galleryRoot.Children.Add(BuildTopGalleryModeBar());
            galleryRoot.Children.Add( Spacer(10) );

            Grid columns = new();
            columns.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            columns.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
            columns.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            columns.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
            columns.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Column 0: Header Banner + Palette Strip + Optics Lab & Corner Bezel Lab
            StackPanel col0 = new();
            col0.Children.Add(BuildHeaderSpecimenBanner());
            col0.Children.Add(Spacer(10));
            col0.Children.Add(BuildPaletteQuickStrip());
            col0.Children.Add(Spacer(10));
            col0.Children.Add(BuildOpticsLabSection());
            Grid.SetColumn(col0, 0);
            columns.Children.Add(col0);

            // Column 1: Fluid SDF Coalescing + 20-ShapeChoreographed Orb + Budding Cluster + Popover Morphs
            StackPanel col1 = new();
            col1.Children.Add(BuildFluidCoalescingSection());
            Grid.SetColumn(col1, 2);
            columns.Children.Add(col1);

            // Column 2: Full UI Kit Component Library + Palettes & HLSL Telemetry
            StackPanel col2 = new();
            col2.Children.Add(BuildUiComponentLibrarySection());
            Grid.SetColumn(col2, 4);
            columns.Children.Add(col2);

            galleryRoot.Children.Add(columns);
            scroll.Content = galleryRoot;
        }
        else
        {
            // Single-Column Mobile/Tablet Flagship View matching Android MainActivity.kt 1:1
            StackPanel singleCol = new()
            {
                MaxWidth = 540,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            singleCol.Children.Add(BuildTopGalleryModeBar());
            singleCol.Children.Add(Spacer(10));
            singleCol.Children.Add(BuildHeaderSpecimenBanner());
            singleCol.Children.Add(Spacer(10));
            singleCol.Children.Add(BuildPaletteQuickStrip());
            singleCol.Children.Add(Spacer(10));

            UIElement activeSection = _selectedTab switch
            {
                0 => BuildOpticsLabSection(),
                1 => BuildFluidCoalescingSection(),
                2 => BuildPaletteGallerySection(),
                3 => BuildUiComponentLibrarySection(),
                _ => BuildBackendArchitectureSection()
            };
            singleCol.Children.Add(activeSection);
            scroll.Content = singleCol;
        }

        Grid.SetRow(scroll, 0);
        rootOverlay.Children.Add(scroll);

        // Bottom Stacked iOS 26 Apple Music Style Liquid SeaGlass Mini-Player + 5-Tab Fluid Bar
        StackPanel bottomChrome = new()
        {
            MaxWidth = _widescreenGalleryMode ? 760 : 540,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(18, 4, 18, 12)
        };
        bottomChrome.Children.Add(BuildMiniPlayerBarControl());
        bottomChrome.Children.Add(Spacer(9));
        bottomChrome.Children.Add(BuildBottomFluidBarControl());

        Grid.SetRow(bottomChrome, 1);
        rootOverlay.Children.Add(bottomChrome);

        _mainContentHost.Content = rootOverlay;
    }

    private UIElement BuildTopGalleryModeBar()
    {
        Grid bar = new();
        bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
        bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        SeaGlassPill modePill = new(
            label: _widescreenGalleryMode ? "❖ 3-Column Gallery Mode" : "📱 Mobile Flagship Mode",
            selected: _widescreenGalleryMode,
            onClick: () =>
            {
                _widescreenGalleryMode = !_widescreenGalleryMode;
                RebuildUI();
            },
            stageState: _stageState,
            selectedMaterial: CustomPrimaryMaterial,
            unselectedMaterial: CustomSurfaceMaterial);
        UIElement modeCtrl = CreatePillControl(modePill, height: 38, width: 210);
        Grid.SetColumn(modeCtrl, 1);
        bar.Children.Add(modeCtrl);

        SeaGlassPill autoPill = new(
            label: _autoAnimateStates ? "⏸ Auto-Play States: ON" : "▶ Auto-Play States: OFF",
            selected: _autoAnimateStates,
            onClick: () =>
            {
                _autoAnimateStates = !_autoAnimateStates;
                RebuildUI();
            },
            stageState: _stageState,
            selectedMaterial: CustomTertiaryMaterial,
            unselectedMaterial: CustomSurfaceMaterial);
        UIElement autoCtrl = CreatePillControl(autoPill, height: 38, width: 200);
        Grid.SetColumn(autoCtrl, 3);
        bar.Children.Add(autoCtrl);

        return bar;
    }

    /// <summary>
    /// Exact 1:1 port of <c>HeaderSpecimenBanner</c> in <c>MainActivity.kt</c>.
    /// </summary>
    private UIElement BuildHeaderSpecimenBanner()
    {
        SeaGlassPalette palette = SeaGlassTheme.Current.Palette;
        SeaGlassSurface bannerSurface = new(
            stageState: _stageState,
            material: CustomPrimaryMaterial with { CornerRadiusDp = 30f },
            onClick: () =>
            {
                int next = (IndexOfPalette(palette) + 1) % SeaGlassPalette.Entries.Count;
                SelectPalette(SeaGlassPalette.Entries[next]);
            });

        SeaGlassVisualControl bannerCtrl = CreateGlassControl(
            bannerSurface,
            height: 98,
            overlay: (dc, frame, w, h) =>
            {
                int c = frame.OptimalContentColorArgb;
                SeaGlassXamlRenderer.DrawLabel(dc, "HDR 1.65× · TILT & TAP REFRACTION", new Point(18, 14), c, 10.5, bold: true, extraBold: true, alpha: 0.78f);
                SeaGlassXamlRenderer.DrawLabel(dc, palette.DisplayName, new Point(18, 30), c, 22.0, bold: true, extraBold: true);
                SeaGlassXamlRenderer.DrawLabel(dc, palette.Tagline, new Point(18, 64), c, 12.0, bold: false, alpha: 0.90f, maxWidth: Math.Max(120, w - 150));
            });

        // Trailing "Next Tide ↻" SeaGlassButton
        SeaGlassButton nextBtnModel = new(
            label: "Next Tide ↻",
            onClick: () =>
            {
                int next = (IndexOfPalette(palette) + 1) % SeaGlassPalette.Entries.Count;
                SelectPalette(SeaGlassPalette.Entries[next]);
            },
            stageState: _stageState,
            material: CustomTertiaryMaterial with { CornerRadiusDp = 22f, ThicknessDp = 18f, RimChalkiness = 0.90f });

        SeaGlassVisualControl nextBtnCtrl = CreateGlassControl(
            nextBtnModel,
            width: 118,
            height: 40,
            overlay: (dc, frame, w, h) =>
            {
                SeaGlassXamlRenderer.DrawLabel(dc, "Next Tide ↻", new Point(18, 10), frame.OptimalContentColorArgb, 13.0, bold: true, extraBold: true);
            });
        nextBtnCtrl.HorizontalAlignment = HorizontalAlignment.Right;
        nextBtnCtrl.VerticalAlignment = VerticalAlignment.Top;
        nextBtnCtrl.Margin = new Thickness(0, 16, 16, 0);
        bannerCtrl.Child = nextBtnCtrl;

        return bannerCtrl;
    }

    /// <summary>
    /// Exact 1:1 port of <c>PaletteQuickStrip</c> in <c>MainActivity.kt</c>.
    /// </summary>
    private UIElement BuildPaletteQuickStrip()
    {
        (SeaGlassPalette Palette, string ShortLabel)[] featured =
        [
            (SeaGlassPalette.SeafoamMint, "Mint"),
            (SeaGlassPalette.CobaltTide, "Cobalt"),
            (SeaGlassPalette.BeachAmber, "Amber"),
            (SeaGlassPalette.SeaLavender, "Lavender")
        ];

        UniformGrid row = new() { Rows = 1, Columns = 4 };
        foreach (var (pal, shortLabel) in featured)
        {
            bool selected = pal.Id == SeaGlassTheme.Current.Palette.Id;
            SeaGlassMaterial targetMat = selected
                ? pal.PrimaryGlass with { CornerRadiusDp = 22f, ThicknessDp = 18f }
                : pal.SurfaceGlass with { CornerRadiusDp = 18f, ThicknessDp = 11f };

            SeaGlassButton btnModel = new(shortLabel, () => SelectPalette(pal), _stageState, targetMat);
            SeaGlassVisualControl btnCtrl = CreateGlassControl(
                btnModel,
                height: 38,
                overlay: (dc, frame, w, h) =>
                {
                    SolidColorBrush gem = new(SeaGlassXamlRenderer.ToMediaColor(pal.PrimaryGlass.TintColorArgb));
                    gem.Freeze();
                    Pen gemPen = new(Brushes.White, 1.2);
                    gemPen.Freeze();
                    dc.DrawEllipse(gem, gemPen, new Point(22, h * 0.5), 5.5, 5.5);
                    SeaGlassXamlRenderer.DrawLabel(dc, shortLabel, new Point(34, h * 0.24), frame.OptimalContentColorArgb, 12.5, bold: true, extraBold: selected);
                });
            btnCtrl.Margin = new Thickness(3, 0, 3, 0);
            row.Children.Add(btnCtrl);
        }
        return row;
    }

    /// <summary>
    /// Exact 1:1 port of <c>OpticsLabSection</c> in <c>MainActivity.kt</c>.
    /// </summary>
    private UIElement BuildOpticsLabSection()
    {
        StackPanel stack = new();

        // 0. Adaptive Device & Page Corner Optics Lab Card
        stack.Children.Add(BuildAdaptiveCornerLabCard());
        stack.Children.Add(Spacer(10));

        // 1. Flagship Coalescing Media & Soundscape Capsule + Play Droplet (sminCubic)
        SeaGlassSurface heroDeckSurface = new(
            stageState: _stageState,
            material: CustomPrimaryMaterial,
            onClick: () => _heroFlowState = (_heroFlowState + 1) % 3);

        IReadOnlyList<SeaGlassSdfEngine.SdfNode> ComputeHeroNodes(double w, double h)
        {
            float deckW = (float)Math.Max(260.0, w);
            float deckH = (float)Math.Max(80.0, h);
            float orbR = 44f;
            float gap = _playOrbGapDp;
            float capW = MathF.Max(orbR * 3f, deckW - orbR * 2f - MathF.Max(0f, gap) - 8f);
            float capHalfW = capW * 0.5f;
            float capHalfH = deckH * 0.46f;
            float cy = deckH * 0.50f;
            float orbCX = MathF.Min(deckW - orbR - 4f, capW + gap + orbR);

            return
            [
                new SeaGlassSdfEngine.SdfNode(
                    centerX: capHalfW,
                    centerY: cy,
                    halfWidth: capHalfW,
                    halfHeight: capHalfH,
                    uniformCornerRadius: capHalfH,
                    cornerSmoothing: 2.2f,
                    tintColorArgb: CustomPrimaryMaterial.TintColorArgb,
                    thicknessPx: CustomPrimaryMaterial.ThicknessDp * 2.8f),
                new SeaGlassSdfEngine.SdfNode(
                    centerX: orbCX,
                    centerY: cy,
                    halfWidth: orbR,
                    halfHeight: orbR,
                    uniformCornerRadius: orbR,
                    cornerSmoothing: 2.2f,
                    tintColorArgb: CustomTertiaryMaterial.TintColorArgb,
                    thicknessPx: CustomTertiaryMaterial.ThicknessDp * 2.8f)
            ];
        }

        SeaGlassVisualControl heroDeckCtrl = new(
            surfaceModel: heroDeckSurface,
            stageBackdrop: _stageBackdropVisuals,
            stageCoordinateRoot: _stageRootGrid,
            customStepEvaluator: dt =>
            {
                double w = heroDeckSurface.SizePx.X > 10 ? heroDeckSurface.SizePx.X : 440;
                double h = heroDeckSurface.SizePx.Y > 10 ? heroDeckSurface.SizePx.Y : 108;
                return heroDeckSurface.EvaluateFrame(dt, SeaGlassTheme.Current, ComputeHeroNodes(w, h), smoothRadiusDp: 44f);
            },
            customOverlayPainter: (dc, frame, w, h) =>
            {
                int c = frame.OptimalContentColorArgb;
                string stateTag = _heroFlowState switch
                {
                    0 => "COALESCED SOUNDSCAPE · 432 HZ",
                    1 => "SURFACE TENSION NECK",
                    _ => "DETACHED ORB MODE"
                };
                SeaGlassXamlRenderer.DrawLabel(dc, stateTag, new Point(22, 18), c, 9.5, bold: true, extraBold: true, alpha: 0.78f);
                SeaGlassXamlRenderer.DrawLabel(dc, "Pacific Surf & Sea Glass", new Point(22, 34), c, 17.0, bold: true, extraBold: true);
                SeaGlassXamlRenderer.DrawLabel(dc, $"{_transparency * 100f:F0}% Clear · {_ior:F2} IOR · Tap to flow ⇄", new Point(22, 62), c, 11.5, bold: false, alpha: 0.86f);

                float orbR = 44f;
                float capW = MathF.Max(orbR * 3f, (float)w - orbR * 2f - MathF.Max(0f, _playOrbGapDp) - 8f);
                float orbCX = MathF.Min((float)w - orbR - 4f, capW + _playOrbGapDp + orbR);

                // 5-bar waveform inside right of capsule
                double waveBaseX = capW - 44;
                double[] barHeights = [12, 24, 18, 28, 15];
                SolidColorBrush waveBrush = new(SeaGlassXamlRenderer.ToMediaColor(c, 0.82f));
                waveBrush.Freeze();
                for (int i = 0; i < barHeights.Length; i++)
                {
                    double bh = _heroFlowState != 2 ? barHeights[i] : 8;
                    dc.DrawRoundedRectangle(waveBrush, null, new Rect(waveBaseX + i * 7, (h - bh) * 0.5, 4, bh), 2, 2);
                }

                // Play/Pause + FLOW/SPLIT inside coalescing right orb
                SeaGlassXamlRenderer.DrawGlyph(
                    dc,
                    _heroFlowState != 2 ? SeaGlassGlyph.Pause : SeaGlassGlyph.Play,
                    new Rect(orbCX - 10, h * 0.5 - 16, 20, 20),
                    c);
                SeaGlassXamlRenderer.DrawLabel(
                    dc,
                    _heroFlowState == 2 ? "SPLIT" : "FLOW",
                    new Point(orbCX - 15, h * 0.5 + 8),
                    c,
                    9.0,
                    bold: true,
                    extraBold: true,
                    alpha: 0.82f);
            },
            activeNodesProvider: () => ComputeHeroNodes(heroDeckSurface.SizePx.X, heroDeckSurface.SizePx.Y),
            smoothRadiusDp: 44f)
        {
            Height = 108
        };
        _activeControls.Add(heroDeckCtrl);
        stack.Children.Add(heroDeckCtrl);
        stack.Children.Add(Spacer(10));

        // 2. 1-Tap Optical Finish Presets (Sanded Beach / Liquid Lens / Heavy Frost)
        UniformGrid presetsRow = new() { Rows = 1, Columns = 3 };
        void ApplyOpticalPreset(float tr, float c, float f, float d, float t, float ior, float disp)
        {
            _transparency = tr; _coarseness = c; _frostRadiusDp = f;
            _tintDensity = d; _thicknessDp = t; _ior = ior; _chromaticDispersion = disp;
            SyncComponentMaterials();
            RebuildUI();
        }
        presetsRow.Children.Add(CreatePillControl(new SeaGlassPill(
            "Sanded Beach",
            _transparency is >= 0.55f and <= 0.75f,
            () => ApplyOpticalPreset(0.64f, 0.44f, 22f, 0.68f, 16f, 1.54f, 0.80f),
            _stageState, CustomPrimaryMaterial, CustomSurfaceMaterial), 38, new Thickness(0, 0, 4, 0)));
        presetsRow.Children.Add(CreatePillControl(new SeaGlassPill(
            "Liquid Lens",
            _transparency > 0.75f,
            () => ApplyOpticalPreset(0.86f, 0.16f, 12f, 0.62f, 22f, 1.72f, 0.88f),
            _stageState, CustomPrimaryMaterial, CustomSurfaceMaterial), 38, new Thickness(2, 0, 2, 0)));
        presetsRow.Children.Add(CreatePillControl(new SeaGlassPill(
            "Heavy Frost",
            _transparency < 0.55f,
            () => ApplyOpticalPreset(0.36f, 0.78f, 34f, 0.74f, 16f, 1.48f, 0.92f),
            _stageState, CustomPrimaryMaterial, CustomSurfaceMaterial), 38, new Thickness(4, 0, 0, 0)));
        stack.Children.Add(presetsRow);
        stack.Children.Add(Spacer(10));

        // 3. Hero "FROSTED GLASS TRANSPARENCY BAR" Card + "SEA GLASS & LIQUID MENISCUS OPTICS" Sliders Card
        SeaGlassSurface opticsCardModel = new(_stageState, CustomSurfaceMaterial with { CornerRadiusDp = 28f });
        SeaGlassVisualControl opticsCard = CreateGlassControl(
            opticsCardModel,
            height: 248,
            overlay: (dc, frame, w, h) =>
            {
                int c = frame.OptimalContentColorArgb;
                SeaGlassXamlRenderer.DrawLabel(dc, "SEA GLASS & LIQUID MENISCUS OPTICS (DRAG ANY BAR TO TUNE)", new Point(18, 12), c, 10.5, bold: true, extraBold: true, alpha: 0.78f);

                DrawCustomOpticalBar(dc, "Frosted Glass Transparency", $"{_transparency * 100f:F0}%", (_transparency - 0.08f) / 0.87f, 18, 34, w - 36, c, CustomPrimaryMaterial.TintColorArgb);
                DrawCustomOpticalBar(dc, "Beer-Lambert Color Tint Strength", $"{_tintDensity * 100f:F0}%", (_tintDensity - 0.18f) / 0.77f, 18, 76, w - 36, c, CustomPrimaryMaterial.TintColorArgb);
                DrawCustomOpticalBar(dc, "Sand Coarseness (Micro-Grit)", $"{_coarseness * 100f:F0}%", _coarseness, 18, 118, w - 36, c, CustomPrimaryMaterial.TintColorArgb);
                DrawCustomOpticalBar(dc, "Edge Lens Refraction (IOR)", $"{_ior:F2}", (_ior - 1.08f) / 0.72f, 18, 160, w - 36, c, CustomPrimaryMaterial.TintColorArgb);
                DrawCustomOpticalBar(dc, "Chromatic Aberration (Prismatic)", $"{_chromaticDispersion * 100f:F0}%", _chromaticDispersion, 18, 202, w - 36, c, CustomPrimaryMaterial.TintColorArgb);
            });

        void HandleOpticsBarPointer(Point pt, double w)
        {
            float frac = (float)Math.Clamp((pt.X - 18.0) / Math.Max(1.0, w - 36.0), 0.0, 1.0);
            if (pt.Y is >= 28 and < 70) _transparency = 0.08f + frac * 0.87f;
            else if (pt.Y is >= 70 and < 112) _tintDensity = 0.18f + frac * 0.77f;
            else if (pt.Y is >= 112 and < 154) _coarseness = frac;
            else if (pt.Y is >= 154 and < 196) _ior = 1.08f + frac * 0.72f;
            else if (pt.Y >= 196) _chromaticDispersion = frac;
            SyncComponentMaterials();
        }

        opticsCard.MouseDown += (_, e) => HandleOpticsBarPointer(e.GetPosition(opticsCard), opticsCard.ActualWidth);
        opticsCard.MouseMove += (_, e) =>
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                HandleOpticsBarPointer(e.GetPosition(opticsCard), opticsCard.ActualWidth);
            }
        };

        stack.Children.Add(opticsCard);
        return stack;
    }

    private UIElement BuildAdaptiveCornerLabCard()
    {
        SeaGlassSurface cardSurface = new(_stageState, CustomSurfaceMaterial with { CornerRadiusDp = 30f });
        SeaGlassVisualControl cardCtrl = CreateGlassControl(
            cardSurface,
            height: 274,
            overlay: (dc, frame, w, h) =>
            {
                int c = frame.OptimalContentColorArgb;
                var (layout, tlPod, trPod, dock) = _cornerLabCard.SolveBezelFrame((float)Math.Max(280, w - 32), 146f, 1.0f);
                var (tlDp, trDp, brDp, blDp) = _cornerLabCard.ActiveProfile.RadiiDp();

                SeaGlassXamlRenderer.DrawLabel(dc, "ADAPTIVE DEVICE & PAGE CORNER OPTICS", new Point(18, 12), c, 10.0, bold: true, extraBold: true, alpha: 0.76f);
                SeaGlassXamlRenderer.DrawLabel(dc, "Concentric Bezel & Page Corner Matching", new Point(18, 26), c, 15.0, bold: true, extraBold: true);
                SeaGlassXamlRenderer.DrawLabel(
                    dc,
                    $"Profile: {_cornerLabCard.ActiveProfile.ShortLabel()} · Bezel TL {tlDp:F0}dp · TR {trDp:F0}dp · BR {brDp:F0}dp · BL {blDp:F0}dp",
                    new Point(18, 50), c, 10.5, bold: false, alpha: 0.84f);

                // Simulated Bezel Frame with 3 Concentric Corner-Embedded Glass Pods
                Rect bezelRect = new(16, 74, Math.Max(200, w - 32), 184);
                SolidColorBrush bezelBg = new(Color.FromArgb(42, 0, 0, 0));
                bezelBg.Freeze();
                Pen bezelPen = new(new SolidColorBrush(Color.FromArgb(185, 255, 255, 255)), 2.0);
                bezelPen.Freeze();
                dc.DrawRoundedRectangle(bezelBg, bezelPen, bezelRect, tlDp * 0.75, trDp * 0.75);

                double inset = Math.Clamp(layout.EffectiveInsetPx, 6, 18);
                double podW = Math.Min(148, (bezelRect.Width - inset * 2 - 14) * 0.5);
                double podH = 66;

                // TL Pod
                Rect tlRect = new(bezelRect.X + inset, bezelRect.Y + inset, podW, podH);
                DrawMiniGlassPod(dc, tlRect, tlPod.TopLeftPx * 0.65, CustomPrimaryMaterial.TintColorArgb, "↖ TL Corner Pod", $"Outer {tlPod.TopLeftPx:F0}dp · Inner {tlPod.BottomRightPx:F0}dp", c);

                // TR Pod
                Rect trRect = new(bezelRect.Right - inset - podW, bezelRect.Y + inset, podW, podH);
                DrawMiniGlassPod(dc, trRect, trPod.TopRightPx * 0.65, CustomTertiaryMaterial.TintColorArgb, "TR Corner Pod ↗", $"Outer {trPod.TopRightPx:F0}dp · Inner {trPod.BottomLeftPx:F0}dp", c);

                // Bottom Concentric Dock
                Rect dockRect = new(bezelRect.X + inset, bezelRect.Bottom - inset - 54, bezelRect.Width - inset * 2, 54);
                DrawMiniGlassPod(dc, dockRect, dock.BottomLeftPx * 0.55, CustomSurfaceMaterial.TintColorArgb, "↙ Bottom Concentric Page Dock ↘", $"BL {dock.BottomLeftPx:F0}dp · BR {dock.BottomRightPx:F0}dp · Zero Bezel Protrusion", c);
            });

        cardCtrl.MouseUp += (_, _) =>
        {
            SeaGlassDeviceHardwareProfile[] profiles =
            [
                SeaGlassDeviceHardwareProfile.Pixel9Pro,
                SeaGlassDeviceHardwareProfile.IPhone16Pro,
                SeaGlassDeviceHardwareProfile.GalaxyS25Ultra,
                SeaGlassDeviceHardwareProfile.FoldAsymmetric,
                SeaGlassDeviceHardwareProfile.Windows11Rounded
            ];
            int idx = Array.IndexOf(profiles, _cornerLabCard.ActiveProfile);
            _cornerLabCard.SelectProfile(profiles[(idx + 1) % profiles.Length]);
        };

        return cardCtrl;
    }

    /// <summary>
    /// Exact 1:1 port of <c>FluidCoalescingSection</c> in <c>MainActivity.kt</c>.
    /// </summary>
    private UIElement BuildFluidCoalescingSection()
    {
        StackPanel stack = new();

        // 1. Top Row: 3-State Cascaded Model Morph + Teardrop (•••) Context Menu Morph
        Grid topMorphRow = new() { Height = 104 };
        topMorphRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.15, GridUnitType.Star) });
        topMorphRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
        topMorphRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

        SeaGlassVisualControl cascadedCtrl = new(
            _cascadedModelMorph,
            _stageBackdropVisuals,
            _stageRootGrid,
            dt => _cascadedModelMorph.StepAndEvaluate(dt, SeaGlassTheme.Current),
            (dc, frame, w, h) =>
            {
                int c = frame.OptimalContentColorArgb;
                SeaGlassXamlRenderer.DrawGlyph(dc, SeaGlassGlyph.Layers, new Rect(14, 14, 16, 16), c);
                SeaGlassXamlRenderer.DrawLabel(dc, $"{_cascadedModelMorph.SelectedModel} ›", new Point(36, 12), c, 12.0, bold: true, extraBold: true);
                SeaGlassXamlRenderer.DrawLabel(dc, $"Stage {_cascadedModelMorph.Stage} · {_cascadedModelMorph.EffortLabel}", new Point(14, 36), unchecked((int)0xFFC41238), 11.5, bold: true, extraBold: true);

                // Indigo-violet-to-coral gradient slider bar
                LinearGradientBrush effortGrad = new(
                    new GradientStopCollection
                    {
                        new GradientStop(Color.FromRgb(0x4B, 0x44, 0xF6), 0.0),
                        new GradientStop(Color.FromRgb(0x9C, 0x3C, 0xF8), 0.5),
                        new GradientStop(Color.FromRgb(0xF2, 0x5C, 0x78), 1.0)
                    },
                    new Point(0, 0), new Point(1, 0));
                effortGrad.Freeze();
                dc.DrawRoundedRectangle(effortGrad, null, new Rect(14, 62, Math.Max(40, w - 28), 22), 11, 11);
                double thumbX = 16 + Math.Max(10, (w - 60) * _cascadedModelMorph.EffortFraction);
                dc.DrawRoundedRectangle(Brushes.White, null, new Rect(thumbX, 65, 28, 16), 8, 8);
            });
        _activeControls.Add(cascadedCtrl);
        Grid.SetColumn(cascadedCtrl, 0);
        topMorphRow.Children.Add(cascadedCtrl);

        SeaGlassVisualControl menuMorphCtrl = new(
            _contextMenuMorph,
            _stageBackdropVisuals,
            _stageRootGrid,
            dt => _contextMenuMorph.StepAndEvaluate(dt, SeaGlassTheme.Current),
            (dc, frame, w, h) =>
            {
                int c = frame.OptimalContentColorArgb;
                SeaGlassXamlRenderer.DrawLabel(dc, "••• Teardrop Menu", new Point(14, 12), c, 12.0, bold: true, extraBold: true);
                SeaGlassXamlRenderer.DrawLabel(dc, "⚑ Pin · ✓ Settle · ✦ Rename", new Point(14, 34), c, 10.5, bold: true, alpha: 0.88f);
                SeaGlassXamlRenderer.DrawLabel(dc, "❖ Files · ⊶ Terminal · ⤴ Git", new Point(14, 54), c, 10.5, bold: false, alpha: 0.82f);
                SeaGlassXamlRenderer.DrawLabel(dc, "✕ Delete… (Tap to Morph)", new Point(14, 74), unchecked((int)0xFFD92644), 10.5, bold: true);
            });
        _activeControls.Add(menuMorphCtrl);
        Grid.SetColumn(menuMorphCtrl, 2);
        topMorphRow.Children.Add(menuMorphCtrl);

        stack.Children.Add(topMorphRow);
        stack.Children.Add(Spacer(10));

        // 2. M3 Expressive Choreographed Shape Shifter Orb + 5 Stage Pills
        Grid orbSection = new() { Height = 132 };
        SeaGlassVisualControl orbCtrl = new(
            _shapeOrb,
            _stageBackdropVisuals,
            _stageRootGrid,
            dt => _shapeOrb.StepAndEvaluate(dt, SeaGlassTheme.Current),
            (dc, frame, w, h) =>
            {
                int c = frame.OptimalContentColorArgb;
                SeaGlassShapeStage st = _shapeOrb.CurrentTargetStage;
                SeaGlassXamlRenderer.DrawCenteredLabel(dc, st.EffectiveLabel.ToUpperInvariant(), new Point(w * 0.5, h * 0.44), c, 11.0, bold: true, extraBold: true);
                SeaGlassXamlRenderer.DrawCenteredLabel(dc, $"{st.Scale:F2}× · Tap ↻", new Point(w * 0.5, h * 0.60), c, 9.5, bold: true, alpha: 0.82f);
            })
        {
            Width = 122,
            Height = 122,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        _activeControls.Add(orbCtrl);
        orbSection.Children.Add(orbCtrl);
        stack.Children.Add(orbSection);

        UniformGrid stagePills = new() { Rows = 1, Columns = 5, Margin = new Thickness(0, 4, 0, 10) };
        for (int i = 0; i < 5; i++)
        {
            int idx = i;
            SeaGlassShapeStage st = _shapeOrb.Stages[idx];
            string shortLbl = st.EffectiveLabel.Split(' ')[0];
            stagePills.Children.Add(CreatePillControl(new SeaGlassPill(
                shortLbl,
                _shapeOrb.TargetStageIndex == idx,
                () => _shapeOrb.AdvanceTo(idx),
                _stageState, CustomPrimaryMaterial, CustomSurfaceMaterial), 34, new Thickness(2, 0, 2, 0)));
        }
        stack.Children.Add(stagePills);

        // 3. Budding Action Cluster (SeaGlassBuddingCluster)
        SeaGlassVisualControl budCtrl = new(
            _buddingCluster,
            _stageBackdropVisuals,
            _stageRootGrid,
            dt => _buddingCluster.StepAndEvaluate(dt, SeaGlassTheme.Current),
            (dc, frame, w, h) =>
            {
                int c = frame.OptimalContentColorArgb;
                SeaGlassXamlRenderer.DrawLabel(dc, _buddingCluster.Expanded ? "✕" : "✦", new Point(38, 26), c, 16, bold: true, extraBold: true);
                SeaGlassXamlRenderer.DrawLabel(dc, _buddingCluster.Expanded ? "FOLD" : "BUD", new Point(32, 48), c, 8.5, bold: true, extraBold: true, alpha: 0.80f);

                if (_buddingCluster.ExpandProgress > 0.15f)
                {
                    double anchorX = 48.0;
                    double step = Math.Max(78.0, (w - anchorX - 48.0) / 3.0);
                    for (int i = 0; i < _buddingCluster.Items.Count; i++)
                    {
                        var (sym, lbl) = _buddingCluster.Items[i];
                        double cx = anchorX + step * (i + 1) * _buddingCluster.ExpandProgress;
                        SeaGlassXamlRenderer.DrawLabel(dc, sym, new Point(cx - 8, 24), c, 14, bold: true, extraBold: true);
                        SeaGlassXamlRenderer.DrawLabel(dc, lbl, new Point(cx - 16, 46), c, 9.5, bold: true, alpha: 0.85f);
                    }
                }
            },
            smoothRadiusDp: 46f)
        {
            Height = 88
        };
        _activeControls.Add(budCtrl);
        stack.Children.Add(budCtrl);
        stack.Children.Add(Spacer(10));

        // 4. 3 M3 Expressive Polygon Shape-Morphing Buttons (Cookie9/Squircle, Burst12/Droplet, Clover4/Gem8)
        UniformGrid morphBtnRow = new() { Rows = 1, Columns = 3, Margin = new Thickness(0, 0, 0, 10) };
        morphBtnRow.Children.Add(BuildMorphingShapeButtonControl(_morphBtn0, "Squircle", "Cookie9"));
        morphBtnRow.Children.Add(BuildMorphingShapeButtonControl(_morphBtn1, "Droplet", "Burst12"));
        morphBtnRow.Children.Add(BuildMorphingShapeButtonControl(_morphBtn2, "Gem8", "Clover4"));
        stack.Children.Add(morphBtnRow);

        // 5. Hero Interactive Multi-Node SDF Arena (Cookie 9 + Clover 4 Coalescing)
        SeaGlassSurface arenaSurface = new(_stageState, CustomPrimaryMaterial);
        IReadOnlyList<SeaGlassSdfEngine.SdfNode> ComputeArenaNodes(double w, double h)
        {
            float aw = (float)Math.Max(280, w);
            float ah = (float)Math.Max(140, h);
            float cAX = aw * 0.32f;
            float cAY = ah * 0.50f;
            float cBX = Math.Clamp(cAX + _dropletSepDp + _dragOffsetPx.X, 75f, aw - 75f);
            float cBY = Math.Clamp(cAY + _dragOffsetPx.Y, 55f, ah - 55f);

            return
            [
                new SeaGlassSdfEngine.SdfNode(
                    centerX: cAX, centerY: cAY, halfWidth: 52f, halfHeight: 52f, uniformCornerRadius: 52f,
                    cornerSmoothing: 2.2f, tintColorArgb: CustomPrimaryMaterial.TintColorArgb, thicknessPx: 34f,
                    startShapeKind: SeaGlassShapeKind.Cookie9, endShapeKind: SeaGlassShapeKind.Clover4, shapeMorphProgress: 0.25f),
                new SeaGlassSdfEngine.SdfNode(
                    centerX: cBX, centerY: cBY, halfWidth: 46f, halfHeight: 46f, uniformCornerRadius: 46f,
                    cornerSmoothing: 2.2f, tintColorArgb: CustomTertiaryMaterial.TintColorArgb, thicknessPx: 32f,
                    startShapeKind: SeaGlassShapeKind.Flower, endShapeKind: SeaGlassShapeKind.Heart, shapeMorphProgress: 0.25f)
            ];
        }

        SeaGlassVisualControl arenaCtrl = new(
            surfaceModel: arenaSurface,
            stageBackdrop: _stageBackdropVisuals,
            stageCoordinateRoot: _stageRootGrid,
            customStepEvaluator: dt =>
            {
                double w = arenaSurface.SizePx.X > 10 ? arenaSurface.SizePx.X : 440;
                double h = arenaSurface.SizePx.Y > 10 ? arenaSurface.SizePx.Y : 168;
                return arenaSurface.EvaluateFrame(dt, SeaGlassTheme.Current, ComputeArenaNodes(w, h), smoothRadiusDp: _smoothBridgeDp);
            },
            customOverlayPainter: (dc, frame, w, h) =>
            {
                int c = frame.OptimalContentColorArgb;
                double cAX = w * 0.32;
                double cAY = h * 0.50;
                double cBX = Math.Clamp(cAX + _dropletSepDp + _dragOffsetPx.X, 75, w - 75);
                double cBY = Math.Clamp(cAY + _dragOffsetPx.Y, 55, h - 55);

                SeaGlassXamlRenderer.DrawCenteredLabel(dc, "COOKIE 9", new Point(cAX, cAY - 8), c, 12.0, bold: true, extraBold: true);
                SeaGlassXamlRenderer.DrawCenteredLabel(dc, "Tap / Merge", new Point(cAX, cAY + 10), c, 10.0, bold: false, alpha: 0.82f);

                SeaGlassXamlRenderer.DrawCenteredLabel(dc, "FLOWER", new Point(cBX, cBY - 8), c, 12.0, bold: true, extraBold: true);
                SeaGlassXamlRenderer.DrawCenteredLabel(dc, "Drag Droplet", new Point(cBX, cBY + 10), c, 10.0, bold: false, alpha: 0.82f);
            },
            activeNodesProvider: () => ComputeArenaNodes(arenaSurface.SizePx.X, arenaSurface.SizePx.Y),
            smoothRadiusDp: _smoothBridgeDp)
        {
            Height = 168
        };

        Point? lastDragPt = null;
        arenaCtrl.MouseDown += (_, e) => lastDragPt = e.GetPosition(arenaCtrl);
        arenaCtrl.MouseMove += (_, e) =>
        {
            if (e.LeftButton == MouseButtonState.Pressed && lastDragPt is { } prev)
            {
                Point cur = e.GetPosition(arenaCtrl);
                _dragOffsetPx += new Vector2((float)(cur.X - prev.X), (float)(cur.Y - prev.Y));
                lastDragPt = cur;
            }
        };
        arenaCtrl.MouseUp += (_, _) =>
        {
            if (_dragOffsetPx.Length() < 4f) _coalescedPreset = !_coalescedPreset;
            lastDragPt = null;
        };

        _activeControls.Add(arenaCtrl);
        stack.Children.Add(arenaCtrl);
        stack.Children.Add(Spacer(10));

        // 6. Controls & Bridge Smoothness Card below the interactive arena (Matches MainActivity.kt lines 1446-1546)
        SeaGlassSurface bridgeCardModel = new(_stageState, CustomSurfaceMaterial with { CornerRadiusDp = 28f });
        SeaGlassVisualControl bridgeCard = CreateGlassControl(
            bridgeCardModel,
            height: 184,
            overlay: (dc, frame, w, h) =>
            {
                int c = frame.OptimalContentColorArgb;
                SeaGlassXamlRenderer.DrawLabel(dc, "LIQUID SDF COALESCING (SMIN)", new Point(18, 14), c, 10.5, bold: true, extraBold: true, alpha: 0.78f);
                SeaGlassXamlRenderer.DrawLabel(
                    dc,
                    "Drag the right sea-glass droplet or tap Merge/Split. Both droplets evaluate a shared 256-byte UBO and blend Oklab tints across the bridge.",
                    new Point(18, 32), c, 11.5, bold: false, alpha: 0.88f, maxWidth: Math.Max(140, w - 36));

                DrawCustomOpticalBar(dc, "Smooth-Min Bridge Radius (k)", $"{_smoothBridgeDp:F0} dp", (_smoothBridgeDp - 8f) / 52f, 18, 128, w - 36, c, CustomPrimaryMaterial.TintColorArgb);
            });

        UniformGrid mergeSplitRow = new()
        {
            Rows = 1,
            Columns = 2,
            Height = 40,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(18, 76, 18, 0)
        };
        mergeSplitRow.Children.Add(CreatePillControl(new SeaGlassPill(
            "Merge Bridge",
            _coalescedPreset,
            () => { _coalescedPreset = true; _dragOffsetPx = Vector2.Zero; RebuildUI(); },
            _stageState, CustomPrimaryMaterial, CustomSurfaceMaterial), 40, new Thickness(0, 0, 5, 0)));
        mergeSplitRow.Children.Add(CreatePillControl(new SeaGlassPill(
            "Split Teardrops",
            !_coalescedPreset,
            () => { _coalescedPreset = false; _dragOffsetPx = Vector2.Zero; RebuildUI(); },
            _stageState, CustomTertiaryMaterial, CustomSurfaceMaterial), 40, new Thickness(5, 0, 0, 0)));
        bridgeCard.Child = mergeSplitRow;

        stack.Children.Add(bridgeCard);
        return stack;
    }

    private UIElement BuildMorphingShapeButtonControl(SeaGlassMorphingButton btn, string activeText, string restingText)
    {
        SeaGlassVisualControl ctrl = new(
            btn,
            _stageBackdropVisuals,
            _stageRootGrid,
            dt => btn.StepAndEvaluate(dt, SeaGlassTheme.Current),
            (dc, frame, w, h) =>
            {
                string label = btn.Active ? activeText : restingText;
                SeaGlassXamlRenderer.DrawCenteredLabel(dc, label, new Point(w * 0.5, h * 0.5), frame.OptimalContentColorArgb, 12.5, bold: true, extraBold: true);
            })
        {
            Height = 62,
            Margin = new Thickness(3, 0, 3, 0)
        };
        _activeControls.Add(ctrl);
        return ctrl;
    }

    /// <summary>
    /// Exact 1:1 port of <c>PaletteGallerySection</c> in <c>MainActivity.kt</c>.
    /// </summary>
    private UIElement BuildPaletteGallerySection()
    {
        UniformGrid grid = new() { Columns = 2 };
        foreach (SeaGlassPalette pal in SeaGlassPalette.Entries)
        {
            bool isSelected = pal.Id == SeaGlassTheme.Current.Palette.Id;
            SeaGlassSurface cardModel = new(
                stageState: _stageState,
                material: pal.PrimaryGlass with
                {
                    CornerRadiusDp = isSelected ? 34f : 26f,
                    ThicknessDp = isSelected ? 20f : 15f,
                    RimChalkiness = isSelected ? 0.94f : pal.PrimaryGlass.RimChalkiness
                },
                onClick: () => SelectPalette(pal));

            SeaGlassVisualControl cardCtrl = CreateGlassControl(
                cardModel,
                height: 132,
                overlay: (dc, frame, w, h) =>
                {
                    int c = frame.OptimalContentColorArgb;
                    int[] gems = [pal.PrimaryGlass.TintColorArgb, pal.SecondaryGlass.TintColorArgb, pal.TertiaryGlass.TintColorArgb];
                    Pen gemBorder = new(Brushes.White, 1.3);
                    gemBorder.Freeze();
                    for (int i = 0; i < gems.Length; i++)
                    {
                        SolidColorBrush b = new(SeaGlassXamlRenderer.ToMediaColor(gems[i]));
                        b.Freeze();
                        dc.DrawEllipse(b, gemBorder, new Point(26 + i * 14, 24), 9, 9);
                    }

                    if (isSelected)
                    {
                        SolidColorBrush badgeBg = new(SeaGlassXamlRenderer.ToMediaColor(c, 0.16f));
                        badgeBg.Freeze();
                        dc.DrawRoundedRectangle(badgeBg, null, new Rect(w - 68, 14, 52, 20), 8, 8);
                        SeaGlassXamlRenderer.DrawLabel(dc, "ACTIVE", new Point(w - 60, 17), c, 9.5, bold: true, extraBold: true);
                    }

                    SeaGlassXamlRenderer.DrawLabel(dc, pal.DisplayName, new Point(18, h - 50), c, 16.0, bold: true, extraBold: true);
                    SeaGlassXamlRenderer.DrawLabel(
                        dc,
                        $"{pal.PrimaryGlass.Transparency * 100f:F0}% Clear · {pal.PrimaryGlass.Coarseness * 100f:F0}% Grit · {pal.PrimaryGlass.Ior:F2} IOR",
                        new Point(18, h - 26), c, 10.5, bold: false, alpha: 0.85f);
                });
            cardCtrl.Margin = new Thickness(5);
            grid.Children.Add(cardCtrl);
        }
        return grid;
    }

    /// <summary>
    /// Exact 1:1 port of <c>UiComponentLibrarySection</c> in <c>MainActivity.kt</c>.
    /// </summary>
    private UIElement BuildUiComponentLibrarySection()
    {
        StackPanel stack = new();

        // 1. Floating SeaGlassTopAppBar
        SeaGlassSurface topBarSurface = new(
            stageState: _stageState,
            material: CustomSurfaceMaterial with { CornerRadiusDp = 30f, ThicknessDp = 18f, RimChalkiness = 0.86f },
            contentRole: SeaGlassContentRole.NavigationChrome,
            onClick: () => _extendedFab.SetExpanded(!_extendedFab.Expanded));
        SeaGlassVisualControl topBarCtrl = CreateGlassControl(
            topBarSurface,
            height: 60,
            overlay: (dc, frame, w, h) =>
            {
                int c = frame.OptimalContentColorArgb;
                SeaGlassXamlRenderer.DrawGlyph(dc, SeaGlassGlyph.Menu, new Rect(16, 20, 20, 20), c);
                SeaGlassXamlRenderer.DrawLabel(dc, "SeaGlass App UI Kit", new Point(48, 12), c, 15.5, bold: true, extraBold: true);
                SeaGlassXamlRenderer.DrawLabel(dc, "Full Material 3 Expressive × Liquid Glass Parity", new Point(48, 32), c, 11.0, bold: false, alpha: 0.78f);

                // Trailing "26 UI" badge + Bell + Settings icons
                SolidColorBrush badgeBg = new(SeaGlassXamlRenderer.ToMediaColor(CustomTertiaryMaterial.TintColorArgb, 0.82f));
                badgeBg.Freeze();
                dc.DrawRoundedRectangle(badgeBg, null, new Rect(w - 138, 18, 46, 24), 12, 12);
                int bc = SeaGlassColorMath.OptimalContentColorFor(CustomTertiaryMaterial.TintColorArgb, CustomTertiaryMaterial.TintDensity);
                SeaGlassXamlRenderer.DrawLabel(dc, "26 UI", new Point(w - 130, 22), bc, 10.5, bold: true, extraBold: true);

                SeaGlassXamlRenderer.DrawGlyph(dc, SeaGlassGlyph.Bell, new Rect(w - 80, 20, 20, 20), c);
                SeaGlassXamlRenderer.DrawGlyph(dc, SeaGlassGlyph.Settings, new Rect(w - 42, 20, 20, 20), c);
            });
        stack.Children.Add(topBarCtrl);
        stack.Children.Add(Spacer(10));

        // 2. Segmented Button Row (Controls / Surfaces / Overlays)
        SeaGlassVisualControl segCtrl = new(
            _segmentedRow,
            _stageBackdropVisuals,
            _stageRootGrid,
            dt => _segmentedRow.StepAndEvaluate(dt, SeaGlassTheme.Current),
            (dc, frame, w, h) =>
            {
                int c = frame.OptimalContentColorArgb;
                double slotW = (w - 8.0) / 3.0;
                double x = 4.0 + _segmentedRow.LeftEdgeSlot * slotW;
                double pillW = Math.Max(slotW * 0.85, _segmentedRow.LozengeSlotSpan * slotW);
                double yInset = 4.0 + _segmentedRow.MotionEnergy * 2.5;
                double pillH = Math.Max(20.0, h - yInset * 2.0);

                SolidColorBrush pillBrush = new(SeaGlassXamlRenderer.ToMediaColor(CustomPrimaryMaterial.TintColorArgb, 0.62f));
                pillBrush.Freeze();
                Pen pillPen = new(new SolidColorBrush(Color.FromArgb(160, 255, 255, 255)), 1.0);
                pillPen.Freeze();
                dc.DrawRoundedRectangle(pillBrush, pillPen, new Rect(x, yInset, pillW, pillH), pillH * 0.5, pillH * 0.5);

                for (int i = 0; i < 3; i++)
                {
                    bool sel = i == _segmentedRow.SelectedIndex;
                    SeaGlassXamlRenderer.DrawCenteredLabel(dc, _segmentedRow.Options[i], new Point(4 + (i + 0.5) * slotW, h * 0.5), c, 13.0, bold: true, extraBold: sel, alpha: sel ? 1f : 0.78f);
                }
            })
        {
            Height = 46
        };
        _activeControls.Add(segCtrl);
        stack.Children.Add(segCtrl);
        stack.Children.Add(Spacer(10));

        // 3. Coalescing Floating Split Toolbar (sminCubic)
        IReadOnlyList<SeaGlassSdfEngine.SdfNode> ComputeToolbarNodes(double w, double h)
        {
            float tw = (float)Math.Max(240, w);
            float th = 58f;
            float gap = _floatingToolbar.CurrentGapDp;
            float mainW = MathF.Max(th * 2f, tw - th - MathF.Max(0f, gap));
            float fabLeft = MathF.Min(tw - th, mainW + gap);
            return
            [
                new SeaGlassSdfEngine.SdfNode(mainW * 0.5f, th * 0.5f, mainW * 0.5f, th * 0.5f, th * 0.5f, 2.5f, CustomSurfaceMaterial.TintColorArgb, CustomSurfaceMaterial.ThicknessDp * 1.5f),
                new SeaGlassSdfEngine.SdfNode(fabLeft + th * 0.5f, th * 0.5f, th * 0.5f, th * 0.5f, th * 0.5f, 2.5f, CustomPrimaryMaterial.TintColorArgb, CustomPrimaryMaterial.ThicknessDp * 1.5f)
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
                double mainW = Math.Max(140, w - 58 - Math.Max(0, _floatingToolbar.CurrentGapDp));
                double slotW = mainW / 4.0;
                for (int i = 0; i < _floatingToolbar.Actions.Count; i++)
                {
                    var (glyph, lbl) = _floatingToolbar.Actions[i];
                    bool sel = i == _floatingToolbar.SelectedActionIndex;
                    double sx = i * slotW + 14;
                    SeaGlassXamlRenderer.DrawGlyph(dc, glyph, new Rect(sx, 20, 18, 18), c, sel ? 1f : 0.76f);
                    if (sel)
                    {
                        SeaGlassXamlRenderer.DrawLabel(dc, lbl, new Point(sx + 22, 20), c, 12.0, bold: true, extraBold: true);
                    }
                }
                double fabLeft = Math.Min(w - 58, mainW + _floatingToolbar.CurrentGapDp);
                int fc = SeaGlassColorMath.OptimalContentColorFor(CustomPrimaryMaterial.TintColorArgb, CustomPrimaryMaterial.TintDensity);
                SeaGlassXamlRenderer.DrawGlyph(
                    dc,
                    _floatingToolbar.FabDetached ? SeaGlassGlyph.Close : SeaGlassGlyph.Plus,
                    new Rect(fabLeft + 18, 18, 22, 22),
                    fc);
            },
            activeNodesProvider: () => ComputeToolbarNodes(_floatingToolbar.SizePx.X, _floatingToolbar.SizePx.Y),
            smoothRadiusDp: 32f)
        {
            Height = 58
        };
        _activeControls.Add(toolbarCtrl);
        stack.Children.Add(toolbarCtrl);
        stack.Children.Add(Spacer(10));

        // 4. Selection, Input & Progress Card (Matches docs/assets/ui-kit-library.png 1:1!)
        SeaGlassSurface selCardModel = new(_stageState, CustomSurfaceMaterial with { CornerRadiusDp = 28f });
        SeaGlassVisualControl selCardCtrl = CreateGlassControl(
            selCardModel,
            height: 220,
            overlay: (dc, frame, w, h) =>
            {
                int c = frame.OptimalContentColorArgb;
                SeaGlassXamlRenderer.DrawLabel(dc, "SELECTION, INPUT & PROGRESS", new Point(18, 16), c, 10.0, bold: true, extraBold: true, alpha: 0.76f);
                SeaGlassXamlRenderer.DrawLabel(dc, "Switches, Checkboxes & Radios", new Point(18, 32), c, 14.5, bold: true, extraBold: true);

                // Linear Progress Bar
                SolidColorBrush trackBg = new(SeaGlassXamlRenderer.ToMediaColor(c, 0.16f));
                trackBg.Freeze();
                dc.DrawRoundedRectangle(trackBg, null, new Rect(18, 134, Math.Max(100, w - 36), 14), 7, 7);

                LinearGradientBrush fillGrad = new(
                    SeaGlassXamlRenderer.ToMediaColor(CustomPrimaryMaterial.TintColorArgb),
                    SeaGlassXamlRenderer.ToMediaColor(SeaGlassColorMath.LerpOklab(CustomPrimaryMaterial.TintColorArgb, unchecked((int)0xFFFFFFFF), 0.35f)),
                    new Point(0, 0), new Point(1, 0));
                fillGrad.Freeze();
                dc.DrawRoundedRectangle(fillGrad, null, new Rect(20, 136, Math.Max(20, (w - 40) * _linearProgress.AnimatedProgress), 10), 5, 5);

                // Frosted TextField Preview
                SolidColorBrush tfBg = new(SeaGlassXamlRenderer.ToMediaColor(c, 0.10f));
                tfBg.Freeze();
                dc.DrawRoundedRectangle(tfBg, null, new Rect(18, 160, Math.Max(100, w - 36), 44), 18, 18);
                SeaGlassXamlRenderer.DrawLabel(dc, "PRESET SCENE NAME", new Point(32, 165), c, 9.0, bold: true, extraBold: true, alpha: 0.70f);
                SeaGlassXamlRenderer.DrawGlyph(dc, SeaGlassGlyph.Tune, new Rect(32, 181, 16, 16), c, 0.82f);
                SeaGlassXamlRenderer.DrawLabel(dc, "Coastal Acoustic Suite", new Point(54, 180), c, 13.0, bold: true);
            });

        // Interactive live child controls inside Selection Card (FAB at top-right, Toggles + Ring on row 2)
        Grid selCardLayout = new() { Margin = new Thickness(18, 12, 18, 0) };
        selCardLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(52) });
        selCardLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(60) });

        UIElement fabCtrl = BuildInteractiveExtendedFabControl();
        if (fabCtrl is FrameworkElement feFab)
        {
            feFab.HorizontalAlignment = HorizontalAlignment.Right;
            feFab.VerticalAlignment = VerticalAlignment.Center;
        }
        Grid.SetRow(fabCtrl, 0);
        selCardLayout.Children.Add(fabCtrl);

        Grid togglesRow = new();
        StackPanel leftToggles = new() { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        leftToggles.Children.Add(BuildInteractiveSwitchControl(_switchA));
        leftToggles.Children.Add(SpacerW(10));
        leftToggles.Children.Add(BuildInteractiveCheckboxControl(_checkA));
        leftToggles.Children.Add(SpacerW(10));
        leftToggles.Children.Add(BuildInteractiveCheckboxControl(_checkB));
        leftToggles.Children.Add(SpacerW(10));
        leftToggles.Children.Add(BuildInteractiveRadioControl(_radioA));
        leftToggles.Children.Add(SpacerW(10));
        leftToggles.Children.Add(BuildInteractiveRadioControl(_radioB));
        togglesRow.Children.Add(leftToggles);

        UIElement ringCtrl = BuildInteractiveCircularProgressControl();
        if (ringCtrl is FrameworkElement feRing)
        {
            feRing.HorizontalAlignment = HorizontalAlignment.Right;
            feRing.VerticalAlignment = VerticalAlignment.Center;
        }
        togglesRow.Children.Add(ringCtrl);

        Grid.SetRow(togglesRow, 1);
        selCardLayout.Children.Add(togglesRow);

        selCardCtrl.Child = selCardLayout;
        stack.Children.Add(selCardCtrl);
        stack.Children.Add(Spacer(10));

        // 5. Dialog Card & Snackbar
        SeaGlassSurface snackbarModel = new(
            stageState: _stageState,
            material: CustomPrimaryMaterial with { CornerRadiusDp = 24f, ThicknessDp = 18f },
            contentRole: SeaGlassContentRole.NavigationChrome);
        SeaGlassVisualControl snackbarCtrl = CreateGlassControl(
            snackbarModel,
            height: 46,
            overlay: (dc, frame, w, h) =>
            {
                int c = frame.OptimalContentColorArgb;
                SeaGlassXamlRenderer.DrawGlyph(dc, SeaGlassGlyph.Sparkles, new Rect(16, 14, 18, 18), c);
                SeaGlassXamlRenderer.DrawLabel(dc, "Oklab Screen + SrcOver volumetric tint calibrated", new Point(42, 14), c, 12.5, bold: true);
            });
        stack.Children.Add(snackbarCtrl);
        stack.Children.Add(Spacer(10));

        // 6. Frosted List Items + Export SeaGlass Design Tokens Dialog Card (Matches MainActivity.kt lines 1870-1932)
        SeaGlassSurface listItem1Model = new(
            stageState: _stageState,
            material: CustomSurfaceMaterial with { CornerRadiusDp = 22f, ThicknessDp = 14f },
            onClick: () => _switchA.Toggle());
        SeaGlassVisualControl listItem1Ctrl = CreateGlassControl(
            listItem1Model,
            height: 62,
            overlay: (dc, frame, w, h) =>
            {
                int c = frame.OptimalContentColorArgb;
                SolidColorBrush iconBox = new(SeaGlassXamlRenderer.ToMediaColor(c, 0.12f));
                iconBox.Freeze();
                dc.DrawRoundedRectangle(iconBox, null, new Rect(14, 12, 38, 38), 12, 12);
                SeaGlassXamlRenderer.DrawGlyph(dc, SeaGlassGlyph.Layers, new Rect(23, 21, 20, 20), c);
                SeaGlassXamlRenderer.DrawLabel(dc, "Adaptive Subsurface Diffusion", new Point(64, 13), c, 13.5, bold: true, extraBold: true);
                SeaGlassXamlRenderer.DrawLabel(dc, "Maintains >= 28dp Gaussian interior frost behind text & icons", new Point(64, 33), c, 11.0, bold: false, alpha: 0.78f);
            });
        stack.Children.Add(listItem1Ctrl);
        stack.Children.Add(Spacer(10));

        SeaGlassSurface dialogModel = new(
            stageState: _stageState,
            material: CustomSurfaceMaterial with { CornerRadiusDp = 30f, ThicknessDp = 20f, RimChalkiness = 0.88f });
        SeaGlassVisualControl dialogCtrl = CreateGlassControl(
            dialogModel,
            height: 146,
            overlay: (dc, frame, w, h) =>
            {
                int c = frame.OptimalContentColorArgb;
                SolidColorBrush iconBox = new(SeaGlassXamlRenderer.ToMediaColor(c, 0.14f));
                iconBox.Freeze();
                dc.DrawRoundedRectangle(iconBox, null, new Rect(18, 16, 40, 40), 14, 14);
                SeaGlassXamlRenderer.DrawGlyph(dc, SeaGlassGlyph.Sparkles, new Rect(27, 25, 22, 22), c);
                SeaGlassXamlRenderer.DrawLabel(dc, "Export SeaGlass Design Tokens", new Point(70, 24), c, 15.5, bold: true, extraBold: true);
                SeaGlassXamlRenderer.DrawLabel(
                    dc,
                    "Generates WinUI 3 XAML tokens, Compose themes, and 256-byte cbuffer / std140 shader uniforms for Direct2D HLSL, Vulkan 1.1, GLES 3.0, and AGSL.",
                    new Point(18, 66), c, 11.5, bold: false, alpha: 0.86f, maxWidth: Math.Max(140, w - 36));
            });
        stack.Children.Add(dialogCtrl);

        return stack;
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
                double thumbW = 26.0 * sw.ThumbScaleX;
                double thumbH = 24.0 * sw.ThumbScaleY;
                double travel = Math.Max(4.0, w - 32.0);
                double tx = 3.0 + travel * Math.Clamp(sw.ThumbProgress, -0.1f, 1.1f);
                double ty = (h - thumbH) * 0.5;

                LinearGradientBrush thumbBrush = new(
                    Color.FromArgb(240, 255, 255, 255),
                    SeaGlassXamlRenderer.ToMediaColor(sw.CheckedMaterial.TintColorArgb, sw.Checked ? 0.58f : 0.28f),
                    new Point(0, 0), new Point(0, 1));
                thumbBrush.Freeze();
                Pen border = new(new SolidColorBrush(Color.FromArgb(215, 255, 255, 255)), 1.2);
                border.Freeze();
                dc.DrawRoundedRectangle(thumbBrush, border, new Rect(tx, ty, thumbW, thumbH), 12, 12);

                if (sw.Checked)
                {
                    SeaGlassXamlRenderer.DrawGlyph(dc, SeaGlassGlyph.Check, new Rect(tx + thumbW * 0.5 - 6.5, ty + thumbH * 0.5 - 6.5, 13, 13), frame.OptimalContentColorArgb);
                }
            })
        {
            Width = 56,
            Height = 32
        };
        _activeControls.Add(ctrl);
        return ctrl;
    }

    private UIElement BuildInteractiveCheckboxControl(SeaGlassCheckbox cb)
    {
        SeaGlassVisualControl ctrl = new(
            cb,
            _stageBackdropVisuals,
            _stageRootGrid,
            dt => cb.StepAndEvaluate(dt, SeaGlassTheme.Current),
            (dc, frame, w, h) =>
            {
                if (cb.MorphProgress > 0.15f)
                {
                    SeaGlassXamlRenderer.DrawGlyph(dc, SeaGlassGlyph.Check, new Rect(w * 0.5 - 8, h * 0.5 - 8, 16, 16), frame.OptimalContentColorArgb);
                }
            })
        {
            Width = 30,
            Height = 30
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
                double r = 6.0 * rb.InnerDropletScale;
                if (r > 0.5)
                {
                    SolidColorBrush b = new(SeaGlassXamlRenderer.ToMediaColor(frame.OptimalContentColorArgb));
                    b.Freeze();
                    dc.DrawEllipse(b, null, new Point(w * 0.5, h * 0.5), r, r);
                }
            })
        {
            Width = 30,
            Height = 30
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
                Pen trackPen = new(new SolidColorBrush(SeaGlassXamlRenderer.ToMediaColor(c, 0.18f)), 4.0);
                trackPen.Freeze();
                Pen arcPen = new(new SolidColorBrush(SeaGlassXamlRenderer.ToMediaColor(CustomPrimaryMaterial.TintColorArgb)), 4.0)
                {
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round
                };
                arcPen.Freeze();
                dc.DrawEllipse(null, trackPen, center, radius, radius);
                DrawArcGeometry(dc, arcPen, center, radius, -90, _circularProgress.SweepAngleDegrees);

                int pct = (int)MathF.Round(_circularProgress.AnimatedProgress * 100f);
                SeaGlassXamlRenderer.DrawLabel(dc, $"{pct}%", new Point(center.X - 11, center.Y - 7), c, 10.0, bold: true, extraBold: true);
            })
        {
            Width = 52,
            Height = 52
        };
        _activeControls.Add(ctrl);
        return ctrl;
    }

    private UIElement BuildInteractiveExtendedFabControl()
    {
        SeaGlassVisualControl ctrl = new(
            _extendedFab,
            _stageBackdropVisuals,
            _stageRootGrid,
            dt => _extendedFab.EvaluateFrame(dt, SeaGlassTheme.Current),
            (dc, frame, w, h) =>
            {
                int c = frame.OptimalContentColorArgb;
                SeaGlassXamlRenderer.DrawGlyph(dc, SeaGlassGlyph.Sparkles, new Rect(16, h * 0.5 - 10, 20, 20), c);
                if (_extendedFab.Expanded)
                {
                    SeaGlassXamlRenderer.DrawLabel(dc, "Create", new Point(42, h * 0.5 - 9), c, 13.5, bold: true, extraBold: true);
                }
            })
        {
            Width = 104,
            Height = 48
        };
        _activeControls.Add(ctrl);
        return ctrl;
    }

    /// <summary>
    /// Exact 1:1 port of <c>BackendArchitectureSection</c> in <c>MainActivity.kt</c>.
    /// </summary>
    private UIElement BuildBackendArchitectureSection()
    {
        StackPanel stack = new();

        SeaGlassTierDiagnostics diag = SeaGlassDeviceTierResolver.GetDiagnostics(SeaGlassTheme.Current.TierOverride);
        CompiledShaderBundle bundle = SeaGlassShaderCompiler.CompileForDialect(_selectedDialect);

        SeaGlassSurface gpuCardModel = new(_stageState, CustomSurfaceMaterial with { CornerRadiusDp = 28f });
        SeaGlassVisualControl gpuCard = CreateGlassControl(
            gpuCardModel,
            height: 118,
            overlay: (dc, frame, w, h) =>
            {
                int c = frame.OptimalContentColorArgb;
                SeaGlassXamlRenderer.DrawLabel(dc, "BACKEND-AGNOSTIC GPU ENGINE", new Point(18, 14), c, 10.5, bold: true, extraBold: true, alpha: 0.75f);
                SeaGlassXamlRenderer.DrawLabel(dc, diag.TierDisplayName, new Point(18, 32), c, 15.0, bold: true, extraBold: true);
                SeaGlassXamlRenderer.DrawLabel(dc, $"OS: {diag.OsDescription} · cbuffer / std140 UBO: {diag.UniformBlockBytes} bytes (16 × float4)", new Point(18, 58), c, 11.5, bold: false, alpha: 0.85f);
                SeaGlassXamlRenderer.DrawLabel(dc, "Dialects: HLSL SM 5.0 · Direct2D D2D1 · Vulkan GLSL 450 · OpenGL ES 3.00 · Android AGSL 1.0", new Point(18, 82), c, 11.0, bold: true, alpha: 0.80f);
            });
        stack.Children.Add(gpuCard);
        stack.Children.Add(Spacer(10));

        UniformGrid dialectRow = new() { Rows = 1, Columns = 5 };
        foreach (ShaderDialect d in Enum.GetValues<ShaderDialect>())
        {
            ShaderDialect captured = d;
            string shortLbl = d switch
            {
                ShaderDialect.HlslSm50 => "HLSL 5.0",
                ShaderDialect.D2D1Hlsl => "D2D1 HLSL",
                ShaderDialect.VulkanGlsl450 => "Vulkan 450",
                ShaderDialect.GlslEs300 => "GLES 3.00",
                _ => "AGSL 1.0"
            };
            dialectRow.Children.Add(CreatePillControl(new SeaGlassPill(
                shortLbl,
                _selectedDialect == d,
                () =>
                {
                    _selectedDialect = captured;
                    RebuildUI();
                },
                _stageState, CustomPrimaryMaterial, CustomSurfaceMaterial), 36, new Thickness(2, 0, 2, 0)));
        }
        stack.Children.Add(dialectRow);
        stack.Children.Add(Spacer(10));

        string snippet = string.Join("\n", bundle.SeaGlassCompositeFragmentSource.Split('\n')[..Math.Min(18, bundle.SeaGlassCompositeFragmentSource.Split('\n').Length)]);
        SeaGlassSurface shaderCardModel = new(_stageState, CustomSurfaceMaterial with { CornerRadiusDp = 28f });
        SeaGlassVisualControl shaderCard = CreateGlassControl(
            shaderCardModel,
            height: 290,
            overlay: (dc, frame, w, h) =>
            {
                int c = frame.OptimalContentColorArgb;
                SeaGlassXamlRenderer.DrawLabel(dc, $"PORTABLE SHADER SOURCE · {_selectedDialect.DisplayName().ToUpperInvariant()}", new Point(18, 14), c, 10.5, bold: true, extraBold: true, alpha: 0.78f);
                SolidColorBrush codeBg = new(Color.FromArgb(42, 0, 0, 0));
                codeBg.Freeze();
                dc.DrawRoundedRectangle(codeBg, null, new Rect(14, 36, Math.Max(100, w - 28), h - 50), 16, 16);
                SeaGlassXamlRenderer.DrawLabel(dc, snippet, new Point(24, 46), c, 10.5, bold: true, mono: true, alpha: 0.92f);
            });
        stack.Children.Add(shaderCard);

        return stack;
    }

    /// <summary>
    /// Exact 1:1 port of <c>SeaGlassMiniPlayerBar</c> in <c>SeaGlassComponents.kt</c>.
    /// </summary>
    private UIElement BuildMiniPlayerBarControl()
    {
        SeaGlassVisualControl ctrl = CreateGlassControl(
            _miniPlayerBar,
            height: 56,
            overlay: (dc, frame, w, h) =>
            {
                int c = frame.OptimalContentColorArgb;

                // Coral/Orange Vinyl Album Artwork Thumbnail (40x40 rounded 10dp)
                LinearGradientBrush artBrush = new(Color.FromRgb(0xE8, 0x6A, 0x50), Color.FromRgb(0xD8, 0x4E, 0x38), new Point(0, 0), new Point(1, 1));
                artBrush.Freeze();
                dc.DrawRoundedRectangle(artBrush, null, new Rect(12, 8, 40, 40), 10, 10);
                Pen groovePen = new(new SolidColorBrush(Color.FromArgb(210, 40, 18, 12)), 2.0);
                groovePen.Freeze();
                dc.DrawEllipse(null, groovePen, new Point(32, 28), 9.5, 9.5);
                dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(0xFF, 0xDF, 0xC8)), null, new Point(32, 28), 3.2, 3.2);

                // Track Title & Subtitle
                SeaGlassXamlRenderer.DrawLabel(dc, _miniPlayerBar.Title, new Point(64, 11), c, 14.0, bold: true, extraBold: true);
                SeaGlassXamlRenderer.DrawLabel(dc, _miniPlayerBar.Subtitle, new Point(64, 30), c, 12.0, bold: false, alpha: 0.78f);

                // Play/Pause + Next Vector Controls
                SeaGlassXamlRenderer.DrawGlyph(
                    dc,
                    _miniPlayerBar.IsPlaying ? SeaGlassGlyph.Pause : SeaGlassGlyph.Play,
                    new Rect(w - 84, 19, 18, 18),
                    c);
                SeaGlassXamlRenderer.DrawGlyph(
                    dc,
                    SeaGlassGlyph.Next,
                    new Rect(w - 42, 17, 22, 22),
                    c);
            });

        ctrl.MouseUp += (_, e) =>
        {
            if (e.GetPosition(ctrl).X > ctrl.ActualWidth - 54)
            {
                _miniPlayerBar.OnNextTrack();
            }
        };

        return ctrl;
    }

    /// <summary>
    /// Exact 1:1 port of <c>SeaGlassFluidBar</c> in <c>SeaGlassComponents.kt</c>.
    /// </summary>
    private UIElement BuildBottomFluidBarControl()
    {
        SeaGlassGlyph[] navGlyphs =
        [
            SeaGlassGlyph.Home,
            SeaGlassGlyph.Sparkles,
            SeaGlassGlyph.Layers,
            SeaGlassGlyph.Tune,
            SeaGlassGlyph.Search
        ];

        SeaGlassVisualControl ctrl = new(
            _bottomFluidBar,
            _stageBackdropVisuals,
            _stageRootGrid,
            dt => _bottomFluidBar.StepAndEvaluate(dt, SeaGlassTheme.Current),
            (dc, frame, w, h) =>
            {
                int baseInk = frame.OptimalContentColorArgb;
                bool isDarkBar = SeaGlassColorMath.ArgbToOklab(_bottomFluidBar.BarMaterial.TintColorArgb).L < 0.48f;
                int activeAccent = isDarkBar
                    ? unchecked((int)0xFFFF6482)
                    : SeaGlassColorMath.LerpOklab(unchecked((int)0xFFC41238), CustomPrimaryMaterial.TintColorArgb, 0.16f);

                int count = _bottomFluidBar.Items.Count;
                double innerW = Math.Max(100, w - 8.0);
                double slotW = innerW / count;
                double pillX = 4.0 + _bottomFluidBar.LeftEdgeSlot * slotW;
                double pillW = Math.Max(slotW * 0.85, _bottomFluidBar.LozengeSlotSpan * slotW);
                double yInset = 4.0 + _bottomFluidBar.MotionEnergy * 3.0;
                double pillH = Math.Max(20.0, h - yInset * 2.0);

                // Plump Liquid Glass Active Tab Lozenge Pill with dual-edge lead-lag stretch
                Color pillTop = isDarkBar
                    ? SeaGlassXamlRenderer.ToMediaColor(CustomPrimaryMaterial.TintColorArgb, 0.48f)
                    : SeaGlassXamlRenderer.ToMediaColor(SeaGlassColorMath.LerpOklab(CustomPrimaryMaterial.TintColorArgb, unchecked((int)0xFFFFFFFF), 0.48f), 0.56f);
                Color pillBottom = isDarkBar
                    ? Color.FromArgb(88, 10, 30, 48)
                    : SeaGlassXamlRenderer.ToMediaColor(SeaGlassColorMath.LerpOklab(CustomPrimaryMaterial.TintColorArgb, unchecked((int)0xFFFFFFFF), 0.28f), 0.42f);

                LinearGradientBrush lozengeBrush = new(pillTop, pillBottom, new Point(0, 0), new Point(0, 1));
                lozengeBrush.Freeze();
                dc.DrawRoundedRectangle(lozengeBrush, null, new Rect(pillX, yInset, pillW, pillH), pillH * 0.5, pillH * 0.5);

                for (int i = 0; i < count; i++)
                {
                    bool selected = !_widescreenGalleryMode && i == _selectedTab;
                    int itemColor = selected ? activeAccent : baseInk;
                    float alpha = selected ? 1.0f : 0.86f;
                    double cx = 4.0 + (i + 0.5) * slotW;

                    SeaGlassXamlRenderer.DrawGlyph(dc, navGlyphs[i % navGlyphs.Length], new Rect(cx - 10.5, 12, 21, 21), itemColor, alpha);
                    SeaGlassXamlRenderer.DrawCenteredLabel(dc, _bottomFluidBar.Items[i], new Point(cx, 46), itemColor, 11.0, bold: true, extraBold: selected, alpha: alpha);
                }
            })
        {
            Height = 68
        };

        ctrl.MouseUp += (_, e) =>
        {
            double u = e.GetPosition(ctrl).X / Math.Max(1.0, ctrl.ActualWidth);
            int idx = Math.Clamp((int)(u * _bottomFluidBar.Items.Count), 0, _bottomFluidBar.Items.Count - 1);
            _bottomFluidBar.SelectTab(idx);
        };

        _activeControls.Add(ctrl);
        return ctrl;
    }

    private SeaGlassVisualControl CreateGlassControl(
        SeaGlassSurface model,
        double? width = null,
        double? height = null,
        Action<DrawingContext, SeaGlassSurfaceFrameState, double, double>? overlay = null)
    {
        SeaGlassVisualControl ctrl = new(
            surfaceModel: model,
            stageBackdrop: _stageBackdropVisuals,
            stageCoordinateRoot: _stageRootGrid,
            customOverlayPainter: overlay);
        if (width is { } w) ctrl.Width = w;
        if (height is { } h) ctrl.Height = h;
        _activeControls.Add(ctrl);
        return ctrl;
    }

    private UIElement CreatePillControl(SeaGlassPill pill, double height = 38, Thickness? margin = null, double? width = null)
    {
        SeaGlassVisualControl ctrl = CreateGlassControl(
            pill,
            width: width,
            height: height,
            overlay: (dc, frame, w, h) =>
            {
                SeaGlassXamlRenderer.DrawCenteredLabel(
                    dc,
                    pill.Label,
                    new Point(w * 0.5, h * 0.5),
                    frame.OptimalContentColorArgb,
                    12.0,
                    bold: true,
                    extraBold: pill.Selected);
            });
        if (margin is { } m) ctrl.Margin = m;
        return ctrl;
    }

    private static void DrawCustomOpticalBar(
        DrawingContext dc,
        string label,
        string valueBadge,
        float normalizedValue,
        double x,
        double y,
        double width,
        int contentColorArgb,
        int accentArgb)
    {
        SeaGlassXamlRenderer.DrawLabel(dc, label, new Point(x, y), contentColorArgb, 12.0, bold: true);

        SolidColorBrush badgeBg = new(SeaGlassXamlRenderer.ToMediaColor(contentColorArgb, 0.14f));
        badgeBg.Freeze();
        dc.DrawRoundedRectangle(badgeBg, null, new Rect(x + width - 58, y - 1, 58, 18), 7, 7);
        SeaGlassXamlRenderer.DrawLabel(dc, valueBadge, new Point(x + width - 50, y + 1), contentColorArgb, 10.5, bold: true, mono: true);

        double trackY = y + 23;
        SolidColorBrush trackBg = new(SeaGlassXamlRenderer.ToMediaColor(contentColorArgb, 0.22f));
        trackBg.Freeze();
        dc.DrawRoundedRectangle(trackBg, null, new Rect(x, trackY, width, 6), 3, 3);

        double fillW = Math.Clamp(normalizedValue, 0.02f, 1.0f) * width;
        SolidColorBrush activeTrack = new(SeaGlassXamlRenderer.ToMediaColor(contentColorArgb, 0.84f));
        activeTrack.Freeze();
        dc.DrawRoundedRectangle(activeTrack, null, new Rect(x, trackY, fillW, 6), 3, 3);

        // Thumb
        SolidColorBrush thumb = new(SeaGlassXamlRenderer.ToMediaColor(contentColorArgb));
        thumb.Freeze();
        dc.DrawEllipse(thumb, null, new Point(x + fillW, trackY + 3), 7.5, 7.5);
    }

    private static void DrawMiniGlassPod(
        DrawingContext dc,
        Rect rect,
        double cornerRadius,
        int tintArgb,
        string title,
        string subtitle,
        int contentColorArgb)
    {
        int richArgb = SeaGlassColorMath.LerpOklab(tintArgb, unchecked((int)0xFFFFFFFF), 0.30f);
        SolidColorBrush fill = new(SeaGlassXamlRenderer.ToMediaColor(richArgb, 0.72f));
        fill.Freeze();
        Pen rim = new(new SolidColorBrush(Color.FromArgb(215, 255, 255, 255)), 1.5);
        rim.Freeze();
        double r = Math.Clamp(cornerRadius, 8, Math.Min(rect.Width, rect.Height) * 0.65);
        dc.DrawRoundedRectangle(fill, rim, rect, r, r);

        SeaGlassXamlRenderer.DrawLabel(dc, title, new Point(rect.X + 12, rect.Y + 12), contentColorArgb, 11.0, bold: true, extraBold: true);
        SeaGlassXamlRenderer.DrawLabel(dc, subtitle, new Point(rect.X + 12, rect.Y + 30), contentColorArgb, 9.5, bold: false, alpha: 0.86f);
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

        Measure(new System.Windows.Size(Width, Height));
        Arrange(new Rect(0, 0, Width, Height));
        UpdateLayout();

        if (Content is UIElement rootVisual)
        {
            rootVisual.Measure(new System.Windows.Size(Width, Height));
            rootVisual.Arrange(new Rect(0, 0, Width, Height));
            rootVisual.UpdateLayout();

            foreach (SeaGlassVisualControl ctrl in _activeControls)
            {
                ctrl.StepPhysics(1f / 60f);
            }
            rootVisual.UpdateLayout();

            RenderTargetBitmap rtb = new((int)Width, (int)Height, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(rootVisual);

            PngBitmapEncoder encoder = new();
            encoder.Frames.Add(BitmapFrame.Create(rtb));
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
            using FileStream fs = File.Create(outputPath);
            encoder.Save(fs);
        }
    }
}
