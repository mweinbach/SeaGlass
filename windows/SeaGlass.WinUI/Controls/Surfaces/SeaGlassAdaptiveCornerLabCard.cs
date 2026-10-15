using System;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;

namespace SeaGlass.WinUI;

/// <summary>
/// Interactive Device & Page Corner Radius Adaptation Lab control.
/// </summary>
public class SeaGlassAdaptiveCornerLabCard : SeaGlassCard
{
    private float _animatedInsetDp = 3f;
    private float _insetVel;

    public SeaGlassDeviceHardwareProfile ActiveProfile { get; private set; } = SeaGlassDeviceHardwareProfile.Pixel9Pro;
    public int InsetPresetIndex { get; private set; }
    public SeaGlassCornerMask CornerMaskPreset { get; private set; } = SeaGlassCornerMask.All;
    public float ProximityThresholdDp { get; private set; } = 96f;
    public float AnimatedInsetDp => _animatedInsetDp;

    public float TargetInsetDp => InsetPresetIndex switch
    {
        0 => 3f,
        1 => 12f,
        _ => 24f
    };

    public SeaGlassAdaptiveCornerLabCard()
        : base(
            title: "Concentric Bezel & Page Corner Matching",
            subtitle: "ADAPTIVE DEVICE & PAGE CORNER OPTICS")
    {
        DefaultStyleKey = typeof(SeaGlassAdaptiveCornerLabCard);
        SizePx = new Vector2(540f, 340f);
    }

    public SeaGlassAdaptiveCornerLabCard(
        SeaGlassDeviceHardwareProfile activeProfile = SeaGlassDeviceHardwareProfile.Pixel9Pro,
        SeaGlassStageState? stageState = null,
        SeaGlassMaterial? surfaceMaterial = null)
        : base(
            title: "Concentric Bezel & Page Corner Matching",
            subtitle: "ADAPTIVE DEVICE & PAGE CORNER OPTICS",
            stage: stageState,
            material: surfaceMaterial ?? SeaGlassTheme.Current.Palette.SurfaceGlass)
    {
        DefaultStyleKey = typeof(SeaGlassAdaptiveCornerLabCard);
        ActiveProfile = activeProfile;
        SizePx = new Vector2(540f, 340f);
    }

    public void SelectProfile(SeaGlassDeviceHardwareProfile profile) => ActiveProfile = profile;
    public void SelectInsetPreset(int presetIndex) => InsetPresetIndex = Math.Clamp(presetIndex, 0, 2);
    public void SelectCornerMask(SeaGlassCornerMask mask) => CornerMaskPreset = mask;
    public void SetProximityThresholdDp(float thresholdDp) => ProximityThresholdDp = Math.Clamp(thresholdDp, 0f, 240f);

    public (SeaGlassCornerPodLayoutPx Layout, SeaGlassCornerRadiiPx TopLeftPodRadii, SeaGlassCornerRadiiPx TopRightPodRadii, SeaGlassCornerRadiiPx BottomDockRadii) SolveBezelFrame(
        float frameWidthPx = 500f,
        float frameHeightPx = 206f,
        float density = 1.0f,
        float dtSeconds = 1f / 60f)
    {
        SeaGlassSpringSpec insetSpring = new(DampingRatio: 0.64f, Stiffness: 380f);
        insetSpring.Step(ref _animatedInsetDp, ref _insetVel, TargetInsetDp, dtSeconds);

        SeaGlassDeviceCornerSpec spec = SeaGlassCornerAdaptationSolver.DetectDeviceCorners(density, ActiveProfile);
        SeaGlassCornerRadiiPx bezelRadii = spec.ToRadiiPx();
        float reqInsetPx = _animatedInsetDp * density;

        SeaGlassCornerPodLayoutPx layout = SeaGlassCornerAdaptationSolver.SolveCornerPodLayoutPx(
            containerWidthPx: frameWidthPx,
            containerHeightPx: frameHeightPx,
            containerCornersPx: bezelRadii,
            requestedInsetPx: reqInsetPx,
            minHorizontalGapPx: 14f * density,
            minVerticalGapPx: 14f * density,
            preferredPodWidthPx: 148f * density,
            preferredTopPodHeightPx: 78f * density,
            preferredBottomDockHeightPx: 68f * density);

        float insetPx = layout.EffectiveInsetPx;
        float podW = layout.PodWidthPx;
        float podH = layout.TopPodHeightPx;
        float dockH = layout.BottomDockHeightPx;
        float basePodCornerPx = 18f * density;
        float influencePx = ProximityThresholdDp * density;

        SeaGlassCornerRadiiPx tlPod = SeaGlassCornerAdaptationSolver.SolveElementCornerRadiiPx(
            insetPx, insetPx, insetPx + podW, insetPx + podH,
            frameWidthPx, frameHeightPx, bezelRadii, basePodCornerPx,
            minCornerFloorPx: 8f * density, influenceDistancePx: influencePx, adaptationStrength: 1f,
            mode: SeaGlassCornerAdaptationMode.FlushCornerEmbed, cornerMask: CornerMaskPreset);

        SeaGlassCornerRadiiPx trPod = SeaGlassCornerAdaptationSolver.SolveElementCornerRadiiPx(
            MathF.Max(0f, frameWidthPx - insetPx - podW), insetPx, MathF.Max(podW, frameWidthPx - insetPx), insetPx + podH,
            frameWidthPx, frameHeightPx, bezelRadii, basePodCornerPx,
            minCornerFloorPx: 8f * density, influenceDistancePx: influencePx, adaptationStrength: 1f,
            mode: SeaGlassCornerAdaptationMode.FlushCornerEmbed, cornerMask: CornerMaskPreset);

        SeaGlassCornerRadiiPx dock = SeaGlassCornerAdaptationSolver.SolveElementCornerRadiiPx(
            insetPx, MathF.Max(0f, frameHeightPx - insetPx - dockH), MathF.Max(podW, frameWidthPx - insetPx), MathF.Max(dockH, frameHeightPx - insetPx),
            frameWidthPx, frameHeightPx, bezelRadii, basePodCornerPx,
            minCornerFloorPx: 8f * density, influenceDistancePx: influencePx, adaptationStrength: 1f,
            mode: SeaGlassCornerAdaptationMode.FlushCornerEmbed, cornerMask: CornerMaskPreset);

        return (layout, tlPod, trPod, dock);
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new SeaGlass.WinUI.Automation.SeaGlassAdaptiveCornerLabCardAutomationPeer(this);
}
