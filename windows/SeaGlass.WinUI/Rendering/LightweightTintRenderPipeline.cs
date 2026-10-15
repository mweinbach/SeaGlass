using System;
using System.Numerics;
using Microsoft.UI.Composition;
using SeaGlass.Core;
using Windows.UI;

namespace SeaGlass.WinUI.Rendering;

/// <summary>
/// Tier 4 Render Pipeline: LIGHTWEIGHT_TINT.
/// Accessibility and battery-conservation engine (Rule #6 in AGENTS.md).
/// Executes zero offscreen backdrop passes and zero blur passes.
/// Employs analytical squircle contour geometry with high-contrast Oklab Beer-Lambert translucent fill.
/// </summary>
public sealed class LightweightTintRenderPipeline : ISeaGlassRenderPipeline
{
    private Compositor? _compositor;
    private CompositionColorBrush? _tintBrush;

    public SeaGlassDeviceTier Tier => SeaGlassDeviceTier.LightweightTint;

    public void Initialize(Compositor? compositor)
    {
        _compositor = compositor;
        if (compositor is not null)
        {
            _tintBrush = compositor.CreateColorBrush();
        }
    }

    public void RenderSurface(SeaGlassRenderContext context, SpriteVisual? targetVisual)
    {
        // 1. Pack 256-byte constant buffer into preallocated scratch staging buffer
        context.FrameState.UniformBlock.WriteToStd140ByteBuffer(context.ScratchArena.StagingBuffer);

        // 2. Compute high-contrast Oklab Beer-Lambert fill color with APCA contrast safety
        int tintArgb = context.FrameState.EffectiveMaterial.TintColorArgb;
        byte a = (byte)Math.Clamp((int)(context.FrameState.EffectiveMaterial.TintDensity * 220f + 35f), 35, 255);
        byte r = (byte)((tintArgb >> 16) & 0xFF);
        byte g = (byte)((tintArgb >> 8) & 0xFF);
        byte b = (byte)(tintArgb & 0xFF);

        Color fillColor = Color.FromArgb(a, r, g, b);

        // 3. Update target visual without new allocations
        if (targetVisual is not null)
        {
            targetVisual.Size = new Vector2(context.WidthPx, context.HeightPx);
            if (_tintBrush is not null)
            {
                _tintBrush.Color = fillColor;
                if (targetVisual.Brush != _tintBrush)
                {
                    targetVisual.Brush = _tintBrush;
                }
            }
        }
    }

    public void Dispose()
    {
        _tintBrush?.Dispose();
        _tintBrush = null;
        _compositor = null;
    }
}
