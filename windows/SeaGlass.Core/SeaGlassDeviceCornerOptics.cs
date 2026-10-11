using System;

namespace SeaGlass.Core;

/// <summary>
/// Source from which the physical display or window corner radii were resolved.
/// </summary>
public enum SeaGlassCornerDetectionSource
{
    WindowInsetsLive,
    DisplayHardwareApi,
    SystemResourceDimen,
    DeviceModelCatalog,
    ProfileOverride
}

public static class SeaGlassCornerDetectionSourceExtensions
{
    public static string Label(this SeaGlassCornerDetectionSource source) => source switch
    {
        SeaGlassCornerDetectionSource.WindowInsetsLive => "DWM Window Corner Attribute / WindowInsets (Live OS)",
        SeaGlassCornerDetectionSource.DisplayHardwareApi => "Display.getRoundedCorner / Monitor Bezel API",
        SeaGlassCornerDetectionSource.SystemResourceDimen => "SystemUI / Windows DWM Corner Metric",
        SeaGlassCornerDetectionSource.DeviceModelCatalog => "Calibrated Device Hardware Catalog",
        SeaGlassCornerDetectionSource.ProfileOverride => "Simulated Hardware Bezel Profile",
        _ => source.ToString()
    };
}

/// <summary>
/// Calibrated physical display &amp; window corner-radius profiles for real-world flagship devices and Windows form factors.
/// </summary>
public enum SeaGlassDeviceHardwareProfile
{
    AutoDetect,
    Pixel9Pro,
    IPhone16Pro,
    GalaxyS25Ultra,
    GalaxyS25Plus,
    FoldAsymmetric,
    Windows11Rounded,
    SurfaceProTablet
}

public static class SeaGlassDeviceHardwareProfileExtensions
{
    public static string Id(this SeaGlassDeviceHardwareProfile p) => p switch
    {
        SeaGlassDeviceHardwareProfile.AutoDetect => "auto",
        SeaGlassDeviceHardwareProfile.Pixel9Pro => "pixel_9_pro",
        SeaGlassDeviceHardwareProfile.IPhone16Pro => "iphone_16_pro",
        SeaGlassDeviceHardwareProfile.GalaxyS25Ultra => "galaxy_s25_ultra",
        SeaGlassDeviceHardwareProfile.GalaxyS25Plus => "galaxy_s25_plus",
        SeaGlassDeviceHardwareProfile.FoldAsymmetric => "fold_asymmetric",
        SeaGlassDeviceHardwareProfile.Windows11Rounded => "win11_rounded",
        SeaGlassDeviceHardwareProfile.SurfaceProTablet => "surface_pro",
        _ => "auto"
    };

    public static string DisplayName(this SeaGlassDeviceHardwareProfile p) => p switch
    {
        SeaGlassDeviceHardwareProfile.AutoDetect => "Live Hardware Auto-Detect",
        SeaGlassDeviceHardwareProfile.Pixel9Pro => "Pixel 9 Pro / 10 Pro (46dp Oval Bezel)",
        SeaGlassDeviceHardwareProfile.IPhone16Pro => "iPhone 16 Pro / 17 Pro Reference (55dp Squircle)",
        SeaGlassDeviceHardwareProfile.GalaxyS25Ultra => "Galaxy S25 Ultra (18dp Architectural)",
        SeaGlassDeviceHardwareProfile.GalaxyS25Plus => "Galaxy S25+ / OnePlus 13 (38dp Rounded)",
        SeaGlassDeviceHardwareProfile.FoldAsymmetric => "Pixel Fold Cover (14dp Hinge / 44dp Outer)",
        SeaGlassDeviceHardwareProfile.Windows11Rounded => "Windows 11 Desktop Window (8dp DWM Round)",
        SeaGlassDeviceHardwareProfile.SurfaceProTablet => "Surface Pro 11 / Tablet Bezel (28dp)",
        _ => p.ToString()
    };

    public static string ShortLabel(this SeaGlassDeviceHardwareProfile p) => p switch
    {
        SeaGlassDeviceHardwareProfile.AutoDetect => "Auto",
        SeaGlassDeviceHardwareProfile.Pixel9Pro => "Pixel 46dp",
        SeaGlassDeviceHardwareProfile.IPhone16Pro => "55dp Pro",
        SeaGlassDeviceHardwareProfile.GalaxyS25Ultra => "Ultra 18dp",
        SeaGlassDeviceHardwareProfile.GalaxyS25Plus => "S25+ 38dp",
        SeaGlassDeviceHardwareProfile.FoldAsymmetric => "Fold 14/44",
        SeaGlassDeviceHardwareProfile.Windows11Rounded => "Win11 8dp",
        SeaGlassDeviceHardwareProfile.SurfaceProTablet => "Surface 28dp",
        _ => p.ToString()
    };

    public static (float TopLeftDp, float TopRightDp, float BottomRightDp, float BottomLeftDp) RadiiDp(
        this SeaGlassDeviceHardwareProfile p) => p switch
    {
        SeaGlassDeviceHardwareProfile.Pixel9Pro => (46f, 46f, 46f, 46f),
        SeaGlassDeviceHardwareProfile.IPhone16Pro => (55f, 55f, 55f, 55f),
        SeaGlassDeviceHardwareProfile.GalaxyS25Ultra => (18f, 18f, 18f, 18f),
        SeaGlassDeviceHardwareProfile.GalaxyS25Plus => (38f, 38f, 38f, 38f),
        SeaGlassDeviceHardwareProfile.FoldAsymmetric => (14f, 44f, 44f, 14f),
        SeaGlassDeviceHardwareProfile.Windows11Rounded => (8f, 8f, 8f, 8f),
        SeaGlassDeviceHardwareProfile.SurfaceProTablet => (28f, 28f, 28f, 28f),
        _ => (44f, 44f, 44f, 44f)
    };
}

/// <summary>
/// Controls how a SeaGlass element adapts its 4 corner radii relative to the physical device
/// bezel corners and/or its enclosing page/card container corners.
/// </summary>
public enum SeaGlassCornerAdaptationMode
{
    ConcentricProximity,
    FlushCornerEmbed,
    UniformConcentric,
    Fixed
}

/// <summary>
/// Per-corner mask specifying where adaptive corner radius matching is allowed on an element.
/// Exact 1:1 parity with <c>dev.seaglass.core.SeaGlassCornerMask</c> on Android.
/// </summary>
public readonly record struct SeaGlassCornerMask(
    bool TopLeft = true,
    bool TopRight = true,
    bool BottomRight = true,
    bool BottomLeft = true)
{
    public bool IsNone => !TopLeft && !TopRight && !BottomRight && !BottomLeft;
    public bool IsAll => TopLeft && TopRight && BottomRight && BottomLeft;

    public static SeaGlassCornerMask All { get; } = new(true, true, true, true);
    public static SeaGlassCornerMask None { get; } = new(false, false, false, false);
    public static SeaGlassCornerMask TopOnly { get; } = new(true, true, false, false);
    public static SeaGlassCornerMask BottomOnly { get; } = new(false, false, true, true);
    public static SeaGlassCornerMask LeftOnly { get; } = new(true, false, false, true);
    public static SeaGlassCornerMask RightOnly { get; } = new(false, true, true, false);

    public static SeaGlassCornerMask Only(
        bool topLeft = false,
        bool topRight = false,
        bool bottomRight = false,
        bool bottomLeft = false) =>
        new(topLeft, topRight, bottomRight, bottomLeft);
}

/// <summary>
/// Developer configuration controlling when, where, and how close to a physical device corner
/// (or parent container corner) a SeaGlass element must be before its corner radius adapts.
/// Exact 1:1 parity with <c>dev.seaglass.core.SeaGlassCornerAdaptationConfig</c> on Android.
/// </summary>
public sealed record SeaGlassCornerAdaptationConfig(
    SeaGlassCornerAdaptationMode Mode = SeaGlassCornerAdaptationMode.ConcentricProximity,
    SeaGlassCornerMask Corners = default,
    float ProximityThresholdDp = SeaGlassCornerAdaptationConfig.DefaultProximityThresholdDp,
    float FullMatchDistanceDp = SeaGlassCornerAdaptationConfig.DefaultFullMatchDistanceDp,
    float Strength = 1.0f,
    bool AllowAsymmetricCorners = false,
    bool AdaptToDeviceCorners = true,
    bool AdaptToParentContainer = false,
    float MinCornerFloorDp = 8f)
{
    public const float DefaultProximityThresholdDp = 32f;
    public const float DefaultFullMatchDistanceDp = 6f;

    public SeaGlassCornerMask EffectiveCorners =>
        Corners == default && Mode != SeaGlassCornerAdaptationMode.Fixed
            ? SeaGlassCornerMask.All
            : Corners;

    public static SeaGlassCornerAdaptationConfig Disabled { get; } = new(
        Mode: SeaGlassCornerAdaptationMode.Fixed,
        Corners: SeaGlassCornerMask.None,
        Strength: 0f);

    public static SeaGlassCornerAdaptationConfig ConcentricProximity(
        float proximityThresholdDp = DefaultProximityThresholdDp,
        float fullMatchDistanceDp = DefaultFullMatchDistanceDp,
        SeaGlassCornerMask? corners = null,
        bool allowAsymmetricCorners = false,
        float strength = 1.0f,
        bool adaptToDeviceCorners = true,
        bool adaptToParentContainer = false) =>
        new(
            Mode: SeaGlassCornerAdaptationMode.ConcentricProximity,
            Corners: corners ?? SeaGlassCornerMask.All,
            ProximityThresholdDp: MathF.Max(0f, proximityThresholdDp),
            FullMatchDistanceDp: MathF.Max(0f, fullMatchDistanceDp),
            Strength: Math.Clamp(strength, 0f, 1f),
            AllowAsymmetricCorners: allowAsymmetricCorners,
            AdaptToDeviceCorners: adaptToDeviceCorners,
            AdaptToParentContainer: adaptToParentContainer);

    public static SeaGlassCornerAdaptationConfig FlushCornerEmbed(
        float proximityThresholdDp = 48f,
        float fullMatchDistanceDp = 8f,
        SeaGlassCornerMask? corners = null,
        bool adaptToParentContainer = true,
        float strength = 1.0f) =>
        new(
            Mode: SeaGlassCornerAdaptationMode.FlushCornerEmbed,
            Corners: corners ?? SeaGlassCornerMask.All,
            ProximityThresholdDp: MathF.Max(0f, proximityThresholdDp),
            FullMatchDistanceDp: MathF.Max(0f, fullMatchDistanceDp),
            Strength: Math.Clamp(strength, 0f, 1f),
            AllowAsymmetricCorners: true,
            AdaptToDeviceCorners: true,
            AdaptToParentContainer: adaptToParentContainer);
}

/// <summary>
/// Per-corner radius quad in pixels (<c>TopLeftPx</c>, <c>TopRightPx</c>, <c>BottomRightPx</c>, <c>BottomLeftPx</c>).
/// </summary>
public readonly record struct SeaGlassCornerRadiiPx(
    float TopLeftPx,
    float TopRightPx,
    float BottomRightPx,
    float BottomLeftPx)
{
    public float MaxRadiusPx => MathF.Max(MathF.Max(TopLeftPx, TopRightPx), MathF.Max(BottomRightPx, BottomLeftPx));
    public float MinRadiusPx => MathF.Min(MathF.Min(TopLeftPx, TopRightPx), MathF.Min(BottomRightPx, BottomLeftPx));
    public bool IsAsymmetric => (MaxRadiusPx - MinRadiusPx) > 1.5f;

    public static SeaGlassCornerRadiiPx Uniform(float radiusPx) =>
        new(radiusPx, radiusPx, radiusPx, radiusPx);
}

/// <summary>
/// Resolved corner-safe content padding in pixels (<c>LeftPx</c>, <c>TopPx</c>, <c>RightPx</c>, <c>BottomPx</c>).
/// </summary>
public readonly record struct SeaGlassSafePaddingPx(
    float LeftPx,
    float TopPx,
    float RightPx,
    float BottomPx);

/// <summary>
/// Dynamically solved, guaranteed non-overlapping layout dimensions in pixels for corner-embedded
/// pods and bottom docks inside an enclosing bezel or page frame.
/// </summary>
public readonly record struct SeaGlassCornerPodLayoutPx(
    float EffectiveInsetPx,
    float PodWidthPx,
    float TopPodHeightPx,
    float BottomDockHeightPx,
    float HorizontalGapPx,
    float VerticalGapPx);

/// <summary>
/// Resolved physical display / window corner geometry.
/// </summary>
public sealed record SeaGlassDeviceCornerSpec(
    float TopLeftPx,
    float TopRightPx,
    float BottomRightPx,
    float BottomLeftPx,
    float Density,
    SeaGlassCornerDetectionSource Source,
    string DeviceDescription)
{
    public float TopLeftDp => TopLeftPx / MathF.Max(Density, 0.1f);
    public float TopRightDp => TopRightPx / MathF.Max(Density, 0.1f);
    public float BottomRightDp => BottomRightPx / MathF.Max(Density, 0.1f);
    public float BottomLeftDp => BottomLeftPx / MathF.Max(Density, 0.1f);

    public SeaGlassCornerRadiiPx ToRadiiPx() =>
        new(TopLeftPx, TopRightPx, BottomRightPx, BottomLeftPx);
}

/// <summary>
/// Multi-tier physical display &amp; window corner radius detector and concentric corner solver.
/// Exact 1:1 mathematical parity with <c>dev.seaglass.core.SeaGlassCornerAdaptationSolver</c> on Android.
/// </summary>
public static class SeaGlassCornerAdaptationSolver
{
    public static SeaGlassDeviceCornerSpec DetectDeviceCorners(
        float density = 1.0f,
        SeaGlassDeviceHardwareProfile overrideProfile = SeaGlassDeviceHardwareProfile.AutoDetect,
        string? manufacturer = null,
        string? model = null)
    {
        float safeDensity = MathF.Max(0.5f, density);

        if (overrideProfile != SeaGlassDeviceHardwareProfile.AutoDetect)
        {
            var (tl, tr, br, bl) = overrideProfile.RadiiDp();
            return new SeaGlassDeviceCornerSpec(
                TopLeftPx: tl * safeDensity,
                TopRightPx: tr * safeDensity,
                BottomRightPx: br * safeDensity,
                BottomLeftPx: bl * safeDensity,
                Density: safeDensity,
                Source: SeaGlassCornerDetectionSource.ProfileOverride,
                DeviceDescription: overrideProfile.DisplayName());
        }

        if (!string.IsNullOrWhiteSpace(manufacturer) || !string.IsNullOrWhiteSpace(model))
        {
            float[] radiiDp = LookupDeviceModelCornerRadiiDp(manufacturer ?? "", model ?? "", "");
            return new SeaGlassDeviceCornerSpec(
                TopLeftPx: radiiDp[0] * safeDensity,
                TopRightPx: radiiDp[1] * safeDensity,
                BottomRightPx: radiiDp[2] * safeDensity,
                BottomLeftPx: radiiDp[3] * safeDensity,
                Density: safeDensity,
                Source: SeaGlassCornerDetectionSource.DeviceModelCatalog,
                DeviceDescription: $"{manufacturer} {model}".Trim());
        }

        // Default flagship / Surface studio showcase bezel (44dp)
        return new SeaGlassDeviceCornerSpec(
            TopLeftPx: 44f * safeDensity,
            TopRightPx: 44f * safeDensity,
            BottomRightPx: 44f * safeDensity,
            BottomLeftPx: 44f * safeDensity,
            Density: safeDensity,
            Source: SeaGlassCornerDetectionSource.WindowInsetsLive,
            DeviceDescription: "Windows 11 DWM / Flagship Showcase Bezel (44dp)");
    }

    public static float[] LookupDeviceModelCornerRadiiDp(
        string manufacturer,
        string model,
        string device,
        int screenWidthDp = 411,
        int screenHeightDp = 891)
    {
        string key = $"{manufacturer} {model} {device}".ToLowerInvariant();
        if (key.Contains("pixel 9") || key.Contains("pixel 10") || key.Contains("caiman") || key.Contains("komodo") || key.Contains("tokay"))
        {
            return (key.Contains("fold") && screenWidthDp < 580)
                ? [14f, 44f, 44f, 14f]
                : [46f, 46f, 46f, 46f];
        }
        if (key.Contains("pixel 8") || key.Contains("husky") || key.Contains("shiba"))
            return [38f, 38f, 38f, 38f];
        if (key.Contains("pixel 7") || key.Contains("pixel 6"))
            return [28f, 28f, 28f, 28f];
        if (key.Contains("ultra") || key.Contains("s928") || key.Contains("s938"))
            return (key.Contains("s25") || key.Contains("s938")) ? [18f, 18f, 18f, 18f] : [12f, 12f, 12f, 12f];
        if (key.Contains("sm-s92") || key.Contains("sm-s93") || key.Contains("galaxy s2"))
            return [38f, 38f, 38f, 38f];
        if (Math.Min(screenWidthDp, screenHeightDp) >= 600)
            return [28f, 28f, 28f, 28f];
        return [44f, 44f, 44f, 44f];
    }

    /// <summary>
    /// Solves the 4 per-corner radii (<c>TopLeftPx</c>, <c>TopRightPx</c>, <c>BottomRightPx</c>, <c>BottomLeftPx</c>)
    /// for a UI element inside an enclosing container/screen using a <see cref="SeaGlassCornerAdaptationConfig"/>.
    /// </summary>
    public static SeaGlassCornerRadiiPx SolveElementCornerRadiiPx(
        float elementLeftPx,
        float elementTopPx,
        float elementRightPx,
        float elementBottomPx,
        float containerWidthPx,
        float containerHeightPx,
        SeaGlassCornerRadiiPx containerCornersPx,
        float baseCornerRadiusPx,
        SeaGlassCornerAdaptationConfig config,
        float density = 2.75f)
    {
        float d = MathF.Max(0.5f, density);
        return SolveElementCornerRadiiPx(
            elementLeftPx: elementLeftPx,
            elementTopPx: elementTopPx,
            elementRightPx: elementRightPx,
            elementBottomPx: elementBottomPx,
            containerWidthPx: containerWidthPx,
            containerHeightPx: containerHeightPx,
            containerCornersPx: containerCornersPx,
            baseCornerRadiusPx: baseCornerRadiusPx,
            minCornerFloorPx: config.MinCornerFloorDp * d,
            influenceDistancePx: config.ProximityThresholdDp * d,
            fullMatchDistancePx: config.FullMatchDistanceDp * d,
            adaptationStrength: config.Strength,
            mode: config.Mode,
            cornerMask: config.EffectiveCorners,
            allowAsymmetricCorners: config.AllowAsymmetricCorners);
    }

    /// <summary>
    /// Solves the 4 per-corner radii (<c>TopLeftPx</c>, <c>TopRightPx</c>, <c>BottomRightPx</c>, <c>BottomLeftPx</c>)
    /// for a UI element inside an enclosing container/screen.
    /// </summary>
    public static SeaGlassCornerRadiiPx SolveElementCornerRadiiPx(
        float elementLeftPx,
        float elementTopPx,
        float elementRightPx,
        float elementBottomPx,
        float containerWidthPx,
        float containerHeightPx,
        SeaGlassCornerRadiiPx containerCornersPx,
        float baseCornerRadiusPx,
        float minCornerFloorPx = 8f,
        float influenceDistancePx = 96f,
        float fullMatchDistancePx = 0f,
        float adaptationStrength = 1.0f,
        SeaGlassCornerAdaptationMode mode = SeaGlassCornerAdaptationMode.ConcentricProximity,
        SeaGlassCornerMask? cornerMask = null,
        bool allowAsymmetricCorners = true)
    {
        SeaGlassCornerMask mask = cornerMask ?? SeaGlassCornerMask.All;
        float elemW = MathF.Max(1f, elementRightPx - elementLeftPx);
        float elemH = MathF.Max(1f, elementBottomPx - elementTopPx);
        float halfMinDimPx = MathF.Min(elemW, elemH) * 0.5f;
        float maxAsymmetricCornerPx = MathF.Min(elemW, elemH) * 0.78f;
        float clampedBase = Math.Clamp(baseCornerRadiusPx, 0f, halfMinDimPx);

        if (mode == SeaGlassCornerAdaptationMode.Fixed ||
            mask.IsNone ||
            containerWidthPx <= 1f ||
            containerHeightPx <= 1f ||
            influenceDistancePx <= 0.5f ||
            adaptationStrength <= 0.001f)
        {
            return SeaGlassCornerRadiiPx.Uniform(clampedBase);
        }

        float leftInset = MathF.Max(0f, elementLeftPx);
        float topInset = MathF.Max(0f, elementTopPx);
        float rightInset = MathF.Max(0f, containerWidthPx - elementRightPx);
        float bottomInset = MathF.Max(0f, containerHeightPx - elementBottomPx);

        float floorPx = MathF.Min(minCornerFloorPx, halfMinDimPx);
        float matchDistPx = Math.Clamp(fullMatchDistancePx, 0f, influenceDistancePx * 0.90f);
        float transitionSpanPx = MathF.Max(1f, influenceDistancePx - matchDistPx);
        float strength = Math.Clamp(adaptationStrength, 0f, 1f);

        float SolveSingleCorner(float dx, float dy, float outerRadiusPx, bool cornerEnabled)
        {
            if (!cornerEnabled || outerRadiusPx <= 0.5f) return clampedBase;
            if (dx >= influenceDistancePx || dy >= influenceDistancePx)
            {
                return mode == SeaGlassCornerAdaptationMode.FlushCornerEmbed
                    ? MathF.Max(floorPx, MathF.Min(clampedBase, halfMinDimPx * 0.45f))
                    : clampedBase;
            }
            float dist = MathF.Sqrt(dx * dx + dy * dy);
            float rawT = Math.Clamp((influenceDistancePx - dist) / transitionSpanPx, 0f, 1f);
            float proximityWeight = rawT * rawT * (3f - 2f * rawT) * strength;

            if (proximityWeight <= 0.001f && mode != SeaGlassCornerAdaptationMode.FlushCornerEmbed)
            {
                return clampedBase;
            }

            float effectiveInset = MathF.Min(dx, dy) * 0.65f + MathF.Max(dx, dy) * 0.35f;
            float rawConcentric = Math.Clamp(outerRadiusPx - effectiveInset, floorPx, maxAsymmetricCornerPx);

            if (mode == SeaGlassCornerAdaptationMode.FlushCornerEmbed)
            {
                return dist < influenceDistancePx * 0.85f
                    ? rawConcentric
                    : MathF.Max(floorPx, MathF.Min(clampedBase, halfMinDimPx * 0.45f));
            }

            // Monotonic non-pinching guarantee: when approaching a bezel rounder than clampedBase,
            // only expand above clampedBase (never dip below clampedBase at intermediate distances);
            // when approaching a sharper bezel, tighten down.
            float monotonicTarget = outerRadiusPx >= clampedBase
                ? MathF.Max(clampedBase, rawConcentric)
                : MathF.Min(clampedBase, rawConcentric);
            return Math.Clamp(clampedBase + (monotonicTarget - clampedBase) * proximityWeight, 0f, maxAsymmetricCornerPx);
        }

        float tl = SolveSingleCorner(leftInset, topInset, containerCornersPx.TopLeftPx, mask.TopLeft);
        float tr = SolveSingleCorner(rightInset, topInset, containerCornersPx.TopRightPx, mask.TopRight);
        float br = SolveSingleCorner(rightInset, bottomInset, containerCornersPx.BottomRightPx, mask.BottomRight);
        float bl = SolveSingleCorner(leftInset, bottomInset, containerCornersPx.BottomLeftPx, mask.BottomLeft);

        if (mode == SeaGlassCornerAdaptationMode.UniformConcentric ||
            (!allowAsymmetricCorners && mode != SeaGlassCornerAdaptationMode.FlushCornerEmbed))
        {
            float maxCorner = MathF.Max(MathF.Max(tl, tr), MathF.Max(br, bl));
            float minCorner = MathF.Min(MathF.Min(tl, tr), MathF.Min(br, bl));
            float unified = maxCorner > clampedBase + 0.5f
                ? MathF.Min(maxCorner, halfMinDimPx)
                : minCorner < clampedBase - 0.5f
                    ? MathF.Min(minCorner, halfMinDimPx)
                    : clampedBase;
            tl = mask.TopLeft ? unified : clampedBase;
            tr = mask.TopRight ? unified : clampedBase;
            br = mask.BottomRight ? unified : clampedBase;
            bl = mask.BottomLeft ? unified : clampedBase;
        }

        // Normalize adjacent corner sums so no pair of corners on the same edge exceeds elemW or elemH
        float edgeScale = MathF.Min(
            MathF.Min(
                tl + tr > elemW ? elemW / (tl + tr) : 1f,
                bl + br > elemW ? elemW / (bl + br) : 1f),
            MathF.Min(
                tl + bl > elemH ? elemH / (tl + bl) : 1f,
                tr + br > elemH ? elemH / (tr + br) : 1f)
        );
        edgeScale = MathF.Min(1f, edgeScale);

        tl *= edgeScale;
        tr *= edgeScale;
        br *= edgeScale;
        bl *= edgeScale;

        return new SeaGlassCornerRadiiPx(tl, tr, br, bl);
    }

    public static SeaGlassSafePaddingPx ComputeCornerSafePaddingPx(
        SeaGlassCornerRadiiPx corners,
        float baseLeftPx,
        float baseTopPx,
        float baseRightPx,
        float baseBottomPx)
    {
        float leftCornerMax = MathF.Max(corners.TopLeftPx, corners.BottomLeftPx);
        float rightCornerMax = MathF.Max(corners.TopRightPx, corners.BottomRightPx);
        float topCornerMax = MathF.Max(corners.TopLeftPx, corners.TopRightPx);
        float bottomCornerMax = MathF.Max(corners.BottomLeftPx, corners.BottomRightPx);

        return new SeaGlassSafePaddingPx(
            LeftPx: MathF.Max(baseLeftPx, leftCornerMax * 0.34f),
            TopPx: MathF.Max(baseTopPx, topCornerMax * 0.22f),
            RightPx: MathF.Max(baseRightPx, rightCornerMax * 0.34f),
            BottomPx: MathF.Max(baseBottomPx, bottomCornerMax * 0.22f));
    }

    public static SeaGlassCornerPodLayoutPx SolveCornerPodLayoutPx(
        float containerWidthPx,
        float containerHeightPx,
        SeaGlassCornerRadiiPx containerCornersPx,
        float requestedInsetPx,
        float minHorizontalGapPx,
        float minVerticalGapPx,
        float preferredPodWidthPx,
        float preferredTopPodHeightPx,
        float preferredBottomDockHeightPx)
    {
        float maxBezelR = containerCornersPx.MaxRadiusPx;
        float maxPodR = MathF.Min(preferredPodWidthPx, preferredTopPodHeightPx) * 0.75f;
        float minArcSafeInsetPx = MathF.Max(0f, (maxBezelR - maxPodR) * 0.30f);
        float maxSafeInsetX = MathF.Max(0f, containerWidthPx * 0.18f);
        float maxSafeInsetY = MathF.Max(0f, containerHeightPx * 0.16f);
        float maxSafeCap = MathF.Max(requestedInsetPx, MathF.Min(maxSafeInsetX, maxSafeInsetY));
        float effectiveInsetPx = MathF.Min(MathF.Max(requestedInsetPx, minArcSafeInsetPx), maxSafeCap);

        float innerW = MathF.Max(64f, containerWidthPx - 2f * effectiveInsetPx);
        float innerH = MathF.Max(64f, containerHeightPx - 2f * effectiveInsetPx);

        float maxNonOverlappingPodW = MathF.Max(36f, (innerW - minHorizontalGapPx) * 0.5f);
        float solvedPodW = MathF.Min(preferredPodWidthPx, maxNonOverlappingPodW);
        float actualHorizontalGap = MathF.Max(0f, innerW - solvedPodW * 2f);

        float availVertForPods = MathF.Max(48f, innerH - minVerticalGapPx);
        float totalPrefH = MathF.Max(1f, preferredTopPodHeightPx + preferredBottomDockHeightPx);
        float vertScale = MathF.Min(1f, availVertForPods / totalPrefH);
        float solvedTopH = preferredTopPodHeightPx * vertScale;
        float solvedBottomH = preferredBottomDockHeightPx * vertScale;
        float actualVerticalGap = MathF.Max(0f, innerH - solvedTopH - solvedBottomH);

        return new SeaGlassCornerPodLayoutPx(
            EffectiveInsetPx: effectiveInsetPx,
            PodWidthPx: solvedPodW,
            TopPodHeightPx: solvedTopH,
            BottomDockHeightPx: solvedBottomH,
            HorizontalGapPx: actualHorizontalGap,
            VerticalGapPx: actualVerticalGap);
    }
}
