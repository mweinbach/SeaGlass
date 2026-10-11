using System;
using System.Numerics;

namespace SeaGlass.WinUI;

/// <summary>
/// Real-time 3D device / pointer tilt state for WinUI.
///
/// Drives tilt-adaptive side refraction and specular rim rotation, matching
/// <c>dev.seaglass.compose.SeaGlassTiltState</c> on Android.
/// </summary>
public sealed class SeaGlassTiltState
{
    private float _rawSensorTiltX;
    private float _rawSensorTiltY;
    private float _smoothedTiltX = -0.35f;
    private float _smoothedTiltY = -0.45f;
    private float _velX;
    private float _velY;
    private float _driftPhase;

    public bool HasHardwareSensor { get; }
    public Vector2? ManualTiltOverride { get; set; }
    public bool AmbientDriftEnabled { get; set; } = true;

    public float TiltX => Math.Clamp(_smoothedTiltX, -1f, 1f);
    public float TiltY => Math.Clamp(_smoothedTiltY, -1f, 1f);

    public float SpecularAngleDegrees
    {
        get
        {
            float rad = MathF.Atan2(TiltY, TiltX);
            float deg = rad * (180f / MathF.PI);
            return (deg + 360f) % 360f;
        }
    }

    public float TiltMagnitude =>
        Math.Clamp(MathF.Sqrt(TiltX * TiltX + TiltY * TiltY), 0f, 1f);

    public Vector3 LightDirection
    {
        get
        {
            Vector3 v = new(TiltX * 0.75f - 0.25f, TiltY * 0.75f - 0.35f, 0.48f);
            float len = MathF.Max(1e-4f, v.Length());
            return v / len;
        }
    }

    public SeaGlassTiltState(bool hasHardwareSensor = false)
    {
        HasHardwareSensor = hasHardwareSensor;
    }

    public void SetSensorInput(float normX, float normY)
    {
        _rawSensorTiltX = Math.Clamp(normX, -1f, 1f);
        _rawSensorTiltY = Math.Clamp(normY, -1f, 1f);
    }

    public void UpdateFrame(float dtSeconds)
    {
        float dt = Math.Clamp(dtSeconds, 0.001f, 0.1f);
        _driftPhase = (_driftPhase + dt * (2f * MathF.PI / 9.0f)) % (2f * MathF.PI);

        float sensorMag = MathF.Sqrt(_rawSensorTiltX * _rawSensorTiltX + _rawSensorTiltY * _rawSensorTiltY);
        float targetX;
        float targetY;

        if (ManualTiltOverride is { } manual)
        {
            targetX = Math.Clamp(manual.X, -1f, 1f);
            targetY = Math.Clamp(manual.Y, -1f, 1f);
        }
        else if (sensorMag > 0.08f)
        {
            targetX = _rawSensorTiltX;
            targetY = _rawSensorTiltY;
        }
        else if (AmbientDriftEnabled)
        {
            targetX = -0.36f + MathF.Cos(_driftPhase) * 0.28f;
            targetY = -0.44f + MathF.Sin(_driftPhase * 1.3f) * 0.24f;
        }
        else
        {
            targetX = -0.36f;
            targetY = -0.44f;
        }

        SeaGlassSpringSpec tiltSpring = new(DampingRatio: 0.82f, Stiffness: 280f);
        tiltSpring.Step(ref _smoothedTiltX, ref _velX, targetX, dt);
        tiltSpring.Step(ref _smoothedTiltY, ref _velY, targetY, dt);
    }
}
