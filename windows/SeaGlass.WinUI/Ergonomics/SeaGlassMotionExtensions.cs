using System;
using Microsoft.UI.Xaml;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Fluent C# extension methods for Material 3 Expressive dual-spring motion and choreography.
/// Enforces Rule #5 in <c>AGENTS.md</c>:
/// Spatial springs for lively geometry/bounce, and critically damped Effects springs (zeta = 1.0)
/// for color, opacity, and frost.
/// </summary>
public static class SeaGlassMotionExtensions
{
    /// <summary>Attaches a custom full motion scheme to this element.</summary>
    public static T Choreography<T>(this T element, SeaGlassMotionScheme motionScheme) where T : FrameworkElement
    {
        SeaGlassTheme.SetMotionScheme(element, motionScheme);
        return element;
    }

    /// <summary>Alias for Choreography.</summary>
    public static T WithChoreography<T>(this T element, SeaGlassMotionScheme motionScheme) where T : FrameworkElement =>
        element.Choreography(motionScheme);

    /// <summary>Attaches a custom full motion scheme (alias for Choreography).</summary>
    public static T WithExpressiveMotion<T>(this T element, SeaGlassMotionScheme motionScheme) where T : FrameworkElement =>
        element.Choreography(motionScheme);

    /// <summary>Attaches a custom spatial spring specification for geometry and layout.</summary>
    public static T SpatialSpring<T>(this T element, SeaGlassSpringSpec spring) where T : FrameworkElement
    {
        SeaGlassTheme.SetSpatialSpring(element, spring);
        return element;
    }

    /// <summary>Alias for SpatialSpring.</summary>
    public static T WithSpatialSpring<T>(this T element, SeaGlassSpringSpec spring) where T : FrameworkElement =>
        element.SpatialSpring(spring);

    /// <summary>Attaches a custom spatial spring specification for geometry and layout.</summary>
    public static T SpatialSpring<T>(this T element, float dampingRatio, float stiffness) where T : FrameworkElement =>
        element.SpatialSpring(new SeaGlassSpringSpec(dampingRatio, stiffness));

    /// <summary>Alias for SpatialSpring(dampingRatio, stiffness).</summary>
    public static T WithSpatialSpring<T>(this T element, float dampingRatio, float stiffness) where T : FrameworkElement =>
        element.SpatialSpring(new SeaGlassSpringSpec(dampingRatio, stiffness));

    /// <summary>Attaches a custom effects spring specification for optical parameters.</summary>
    public static T EffectsSpring<T>(this T element, SeaGlassSpringSpec spring) where T : FrameworkElement
    {
        SeaGlassTheme.SetEffectsSpring(element, spring);
        return element;
    }

    /// <summary>Alias for EffectsSpring.</summary>
    public static T WithEffectsSpring<T>(this T element, SeaGlassSpringSpec spring) where T : FrameworkElement =>
        element.EffectsSpring(spring);

    /// <summary>Attaches a custom effects spring specification for optical parameters.</summary>
    public static T EffectsSpring<T>(this T element, float dampingRatio, float stiffness) where T : FrameworkElement =>
        element.EffectsSpring(new SeaGlassSpringSpec(dampingRatio, stiffness));

    /// <summary>Alias for EffectsSpring(dampingRatio, stiffness).</summary>
    public static T WithEffectsSpring<T>(this T element, float dampingRatio, float stiffness) where T : FrameworkElement =>
        element.EffectsSpring(new SeaGlassSpringSpec(dampingRatio, stiffness));

    /// <summary>Configures standard bouncy spatial motion (under-damped).</summary>
    public static T BouncySpatialMotion<T>(this T element, float dampingRatio = 0.62f, float stiffness = 680f) where T : FrameworkElement =>
        element.SpatialSpring(new SeaGlassSpringSpec(dampingRatio, stiffness));

    /// <summary>Alias for BouncySpatialMotion.</summary>
    public static T WithBouncySpatialMotion<T>(this T element, float dampingRatio = 0.62f, float stiffness = 680f) where T : FrameworkElement =>
        element.BouncySpatialMotion(dampingRatio, stiffness);

    /// <summary>Configures standard critically damped effects motion (zeta = 1.0, zero overshoot).</summary>
    public static T CriticallyDampedEffects<T>(this T element, float stiffness = 1100f) where T : FrameworkElement =>
        element.EffectsSpring(new SeaGlassSpringSpec(1.0f, stiffness));

    /// <summary>Alias for CriticallyDampedEffects.</summary>
    public static T WithCriticallyDampedEffects<T>(this T element, float stiffness = 1100f) where T : FrameworkElement =>
        element.CriticallyDampedEffects(stiffness);

    /// <summary>Enables or disables interactive 3D perspective tilt on pointer hover/press.</summary>
    public static T Tilt<T>(this T element, bool enabled = true) where T : FrameworkElement
    {
        if (element is SeaGlassSurface surface)
            surface.TiltEnabled = enabled;
        return element;
    }

    /// <summary>Alias for Tilt.</summary>
    public static T WithTilt<T>(this T element, bool enabled = true) where T : FrameworkElement =>
        element.Tilt(enabled);
}
