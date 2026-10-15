using System;
using System.Numerics;
using Microsoft.UI.Composition;
using SeaGlass.Core;
using SeaGlass.Core.Backend;

namespace SeaGlass.WinUI.Rendering;

/// <summary>
/// Tier 2 Render Pipeline: COMPOSITION_BLUR_HYBRID.
/// Windows 10 (1809+) compatibility engine pairing hardware Composition backdrop blur
/// with analytical SDF Catmull-Rom contour clipping and quantized blur buckets.
///
/// In Milestone 1, computes quantized blur buckets in [0.5..160.0px], stages the 256-byte constant buffer
/// per frame without heap allocations, and updates animatable CompositionEffectBrush scalar properties ('Blur.BlurAmount').
/// Full Catmull-Rom composition mask clipping is scheduled for Milestone 2 control integration.
/// </summary>
public sealed class CompositionHybridRenderPipeline : ISeaGlassRenderPipeline
{
    private Compositor? _compositor;

    public SeaGlassDeviceTier Tier => SeaGlassDeviceTier.CompositionBlurHybrid;

    public float LastQuantizedBlurRadius { get; private set; }

    public void Initialize(Compositor? compositor)
    {
        _compositor = compositor;
    }

    /// <summary>
    /// Creates a Win2D Gaussian blur <see cref="CompositionEffectFactory"/> with an animatable
    /// <c>Blur.BlurAmount</c> scalar property for hardware composition brushes.
    /// </summary>
    public static CompositionEffectFactory CreateBlurEffectFactory(Compositor compositor)
    {
        ArgumentNullException.ThrowIfNull(compositor);
        var blurEffect = new Microsoft.Graphics.Canvas.Effects.GaussianBlurEffect
        {
            Name = "Blur",
            BlurAmount = 0f,
            BorderMode = Microsoft.Graphics.Canvas.Effects.EffectBorderMode.Hard,
            Optimization = Microsoft.Graphics.Canvas.Effects.EffectOptimization.Speed,
            Source = new CompositionEffectSourceParameter("Backdrop")
        };
        return compositor.CreateEffectFactory(blurEffect, new[] { "Blur.BlurAmount" });
    }

    /// <summary>
    /// Creates an authentic SeaGlass <see cref="CompositionEffectFactory"/> chaining Gaussian blur
    /// with Beer-Lambert volumetric spectral absorption and luminance-preserving scatter.
    /// </summary>
    public static CompositionEffectFactory CreateGlassEffectFactory(Compositor compositor)
    {
        ArgumentNullException.ThrowIfNull(compositor);
        var blurEffect = new Microsoft.Graphics.Canvas.Effects.GaussianBlurEffect
        {
            Name = "Blur",
            BlurAmount = 0f,
            BorderMode = Microsoft.Graphics.Canvas.Effects.EffectBorderMode.Hard,
            Optimization = Microsoft.Graphics.Canvas.Effects.EffectOptimization.Speed,
            Source = new CompositionEffectSourceParameter("Backdrop")
        };

        var compositeEffect = new Microsoft.Graphics.Canvas.Effects.ArithmeticCompositeEffect
        {
            Name = "BeerLambert",
            Source1 = blurEffect,
            Source2 = new CompositionEffectSourceParameter("TintSource"),
            MultiplyAmount = 1.0f,
            Source1Amount = 0.0f,
            Source2Amount = 0.2f,
            Offset = 0.0f
        };

        return compositor.CreateEffectFactory(compositeEffect, new[]
        {
            "Blur.BlurAmount",
            "BeerLambert.MultiplyAmount",
            "BeerLambert.Source1Amount",
            "BeerLambert.Source2Amount"
        });
    }

    private static CompositionBrush? _sharedGrainBrush;
    private static readonly object _grainSync = new();

    /// <summary>
    /// Gets or lazily creates the shared sand-grain micro-facet surface brush from <see cref="SeaGlassSandGrainTexture"/>.
    /// Wraps the 256x256 procedural texture with Win2D BorderEffect (ExtendX=Wrap, ExtendY=Wrap)
    /// to tile seamlessly across any surface dimensions without edge clipping or single-tile artifact.
    /// </summary>
    public static CompositionBrush? GetOrCreateSandGrainBrush(Compositor compositor)
    {
        if (_sharedGrainBrush is not null) return _sharedGrainBrush;
        lock (_grainSync)
        {
            if (_sharedGrainBrush is not null) return _sharedGrainBrush;
            try
            {
                var canvasDevice = Microsoft.Graphics.Canvas.CanvasDevice.GetSharedDevice();
                var compGraphicsDevice = Microsoft.Graphics.Canvas.UI.Composition.CanvasComposition.CreateCompositionGraphicsDevice(
                    compositor,
                    canvasDevice);
                var drawingSurface = compGraphicsDevice.CreateDrawingSurface(
                    new Windows.Foundation.Size(256, 256),
                    Microsoft.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized,
                    Microsoft.Graphics.DirectX.DirectXAlphaMode.Premultiplied);

                using (var ds = Microsoft.Graphics.Canvas.UI.Composition.CanvasComposition.CreateDrawingSession(drawingSurface))
                {
                    byte[] grainBytes = SeaGlassSandGrainTexture.ObtainBgra32Pixels();
                    var canvasBitmap = Microsoft.Graphics.Canvas.CanvasBitmap.CreateFromBytes(
                        canvasDevice,
                        grainBytes,
                        256,
                        256,
                        Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized);
                    ds.DrawImage(canvasBitmap);
                }

                var surfaceBrush = compositor.CreateSurfaceBrush(drawingSurface);
                surfaceBrush.Stretch = CompositionStretch.None;

                try
                {
                    var borderEffect = new Microsoft.Graphics.Canvas.Effects.BorderEffect
                    {
                        Name = "GrainBorder",
                        ExtendX = Microsoft.Graphics.Canvas.CanvasEdgeBehavior.Wrap,
                        ExtendY = Microsoft.Graphics.Canvas.CanvasEdgeBehavior.Wrap,
                        Source = new CompositionEffectSourceParameter("GrainSource")
                    };
                    var factory = compositor.CreateEffectFactory(borderEffect);
                    var effectBrush = factory.CreateBrush();
                    effectBrush.SetSourceParameter("GrainSource", surfaceBrush);
                    _sharedGrainBrush = effectBrush;
                    return _sharedGrainBrush;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SeaGlass] BorderEffect wrap fallback: {ex.Message}");
                    _sharedGrainBrush = surfaceBrush;
                    return _sharedGrainBrush;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SeaGlass] Sand grain brush init fallback: {ex.Message}");
                return null;
            }
        }
    }


    public void RenderSurface(SeaGlassRenderContext context, SpriteVisual? targetVisual)
    {
        // 1. Pack 256-byte constant buffer into preallocated scratch staging buffer
        context.FrameState.UniformBlock.WriteToStd140ByteBuffer(context.ScratchArena.StagingBuffer);

        // 2. Quantize blur radius to 0.5px bucket in [0.5..160.0px] to prevent per-frame filter allocations
        float rawFrostRadius = context.FrameState.EffectiveMaterial.ThicknessDp * 1.5f;
        float quantizedBlur = SeaGlassShaderProgramCache.ObtainQuantizedBlurRadiusPx(rawFrostRadius);
        LastQuantizedBlurRadius = quantizedBlur;

        // 3. Update target visual geometry and effect properties without allocations
        if (targetVisual is not null)
        {
            targetVisual.Size = new Vector2(context.WidthPx, context.HeightPx);
            if (targetVisual.Brush is CompositionEffectBrush effectBrush)
            {
                effectBrush.Properties.InsertScalar("Blur.BlurAmount", quantizedBlur);
            }
            else if (targetVisual.Brush is CompositionSurfaceBrush surfaceBrush)
            {
                surfaceBrush.TransformMatrix = Matrix3x2.CreateTranslation(
                    -context.LocalBackdropOffsetPx.X,
                    -context.LocalBackdropOffsetPx.Y);
            }
        }
    }

    public void Dispose()
    {
        _compositor = null;
    }
}
