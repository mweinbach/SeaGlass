using System;
using System.Collections.Generic;
using Microsoft.UI.Composition;
using SeaGlass.Core;

namespace SeaGlass.WinUI.Rendering;

/// <summary>
/// Master rendering engine managing 4-tier GPU fallback, live system telemetry
/// (UISettings, PowerManager, High Contrast), and per-frame pipeline dispatch.
/// </summary>
public sealed class SeaGlassTieredRenderEngine : IDisposable
{
    public static SeaGlassTieredRenderEngine Instance { get; } = new();

    private readonly Dictionary<SeaGlassDeviceTier, ISeaGlassRenderPipeline> _pipelines = new();
    private SeaGlassDeviceTier? _tierOverride;
    private bool _advancedEffectsEnabled = true;
    private bool _energySaverActive;
    private bool _highContrast;
    private Compositor? _compositor;

    public SeaGlassDeviceTier ActiveTier =>
        _tierOverride ??
        (_highContrast || !_advancedEffectsEnabled || _energySaverActive
            ? SeaGlassDeviceTier.LightweightTint
            : SeaGlassDeviceTier.D2D1HlslFull);

    public ISeaGlassRenderPipeline ActivePipeline => _pipelines[ActiveTier];

    public bool AdvancedEffectsEnabled => _advancedEffectsEnabled;
    public bool EnergySaverActive => _energySaverActive;
    public bool HighContrast => _highContrast;
    public SeaGlassDeviceTier? TierOverride => _tierOverride;

    public SeaGlassTieredRenderEngine()
    {
        _pipelines[SeaGlassDeviceTier.D2D1HlslFull] = new D2D1HlslRenderPipeline();
        _pipelines[SeaGlassDeviceTier.CompositionBlurHybrid] = new CompositionHybridRenderPipeline();
        _pipelines[SeaGlassDeviceTier.D3D11NativePyramid] = new D3D11WarpRenderPipeline();
        _pipelines[SeaGlassDeviceTier.LightweightTint] = new LightweightTintRenderPipeline();
    }

    public void Initialize(Compositor? compositor)
    {
        _compositor = compositor;
        foreach (var pipeline in _pipelines.Values)
        {
            pipeline.Initialize(compositor);
        }
    }

    /// <summary>
    /// Updates system accessibility and power state live from Windows telemetry.
    /// Dynamically shifts active tier to <see cref="SeaGlassDeviceTier.LightweightTint"/> when appropriate.
    /// </summary>
    public void UpdateSystemSettings(bool advancedEffectsEnabled, bool energySaverActive, bool highContrast)
    {
        _advancedEffectsEnabled = advancedEffectsEnabled;
        _energySaverActive = energySaverActive;
        _highContrast = highContrast;
    }

    /// <summary>
    /// Sets a manual device tier override for testing or developer preference.
    /// Pass <c>null</c> to resume automatic telemetry-driven tier selection.
    /// </summary>
    public void SetTierOverride(SeaGlassDeviceTier? overrideTier)
    {
        _tierOverride = overrideTier;
    }

    /// <summary>
    /// Renders or updates a surface visual using the currently active pipeline.
    /// </summary>
    public void Render(SeaGlassRenderContext context, SpriteVisual? targetVisual = null)
    {
        ActivePipeline.RenderSurface(context, targetVisual);
    }

    public void Dispose()
    {
        foreach (var pipeline in _pipelines.Values)
        {
            pipeline.Dispose();
        }
        _pipelines.Clear();
        _compositor = null;
    }
}
