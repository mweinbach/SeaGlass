using System;
using System.Collections.Generic;
using System.Numerics;
using SeaGlass.Core;
using SeaGlass.Core.Backend;

namespace SeaGlass.WinUI;

/// <summary>
/// Semantic optical role of a SeaGlass surface in the UI hierarchy.
/// Matches <c>SeaGlassContentRole</c> in <c>SeaGlassStage.kt</c>.
/// </summary>
public enum SeaGlassContentRole
{
    StandardSurface,
    TextAndIcons,
    NavigationChrome,
    PureOpticalLens
}

/// <summary>
/// Zero-copy backdrop capture coordinator and acyclic render-graph stage for WinUI.
///
/// Enforces Rule #4 in <c>AGENTS.md</c>: records background visual content into a dedicated
/// <c>CompositionVisualSurface</c> / offscreen layer *before* any foreground SeaGlass surface
/// samples from it, preventing recursive feedback loops.
/// </summary>
public sealed class SeaGlassStageState
{
    private readonly HashSet<string> _backdropNodeIds = new(StringComparer.Ordinal);
    private readonly Direct2DHlslSeaGlassBackend _d2dBackend = new();

    public Vector2 BackdropOriginInRoot { get; private set; } = Vector2.Zero;
    public Vector2 BackdropSize { get; private set; } = new(1280f, 800f);
    public long BackdropGeneration { get; private set; }
    public bool IsBackdropCaptured => BackdropGeneration > 0;
    public ISeaGlassRendererBackend ActiveBackend => _d2dBackend;

    /// <summary>
    /// Marks a background element as the stage backdrop target and updates its root bounds.
    /// </summary>
    public void BindBackdropTarget(string backdropElementId, Vector2 originInRoot, Vector2 sizePx)
    {
        if (!string.IsNullOrWhiteSpace(backdropElementId))
        {
            _backdropNodeIds.Add(backdropElementId);
        }
        BackdropOriginInRoot = originInRoot;
        BackdropSize = new Vector2(MathF.Max(1f, sizePx.X), MathF.Max(1f, sizePx.Y));
        BackdropGeneration++;
    }

    /// <summary>
    /// Verifies that a foreground glass element is not registered as its own backdrop source
    /// (Rule #4 acyclic backdrop invariant).
    /// </summary>
    public void EnsureAcyclicForegroundSurface(string foregroundSurfaceId)
    {
        if (!string.IsNullOrWhiteSpace(foregroundSurfaceId) && _backdropNodeIds.Contains(foregroundSurfaceId))
        {
            throw new InvalidOperationException(
                $"Backdrop feedback loop detected: '{foregroundSurfaceId}' is registered as a SeaGlass backdrop source and cannot sample itself.");
        }
    }

    /// <summary>
    /// Computes the local backdrop UV offset for a foreground glass surface at <paramref name="surfaceOriginInRoot"/>.
    /// </summary>
    public Vector2 ComputeLocalBackdropOffset(Vector2 surfaceOriginInRoot) =>
        surfaceOriginInRoot - BackdropOriginInRoot;
}

/// <summary>
/// Zero-ceremony drop-in stage host that creates and binds a shared <see cref="SeaGlassStageState"/>,
/// warms up the process-wide <see cref="SeaGlassShaderProgramCache"/>, and exposes the ambient stage
/// to all descendant <see cref="SeaGlassSurface"/> controls.
/// Exact 1:1 parity with <c>dev.seaglass.compose.SeaGlassStageHost</c> on Android.
/// </summary>
public sealed class SeaGlassStageHost
{
    public static SeaGlassStageState AmbientStageState { get; private set; } = new();

    public SeaGlassStageState StageState { get; }
    public string BackdropId { get; }

    public SeaGlassStageHost(
        Vector2? viewportSizePx = null,
        string backdropId = "seaglass_stage_host_backdrop")
    {
        _ = SeaGlassShaderProgramCache.WarmUpForDevice();
        BackdropId = backdropId;
        StageState = new SeaGlassStageState();
        StageState.BindBackdropTarget(backdropId, Vector2.Zero, viewportSizePx ?? new Vector2(1440f, 920f));
        AmbientStageState = StageState;
    }

    public void ResizeViewport(Vector2 newSizePx) =>
        StageState.BindBackdropTarget(BackdropId, Vector2.Zero, newSizePx);
}
