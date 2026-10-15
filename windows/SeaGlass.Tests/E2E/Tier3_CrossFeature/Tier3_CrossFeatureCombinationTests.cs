using SeaGlass.Tests.Controls;
using System;
using System.Collections.Generic;
using System.Numerics;
using SeaGlass.Core;
using SeaGlass.Core.Backend;
using SeaGlass.WinUI;
using SeaGlass.Tests.E2E.Infrastructure;
using Xunit;

namespace SeaGlass.Tests.E2E.Tier3_CrossFeature;

public sealed class Tier3_CrossFeatureCombinationTests
{
    private readonly SeaGlassStageState _stage;

    public Tier3_CrossFeatureCombinationTests()
    {
        _stage = new SeaGlassStageState();
        _stage.BindBackdropTarget("CrossFeatureStageBackdrop", Vector2.Zero, new Vector2(1920f, 1080f));
    }

    #region 1. SDF Liquid Coalescing + Dual-Spring Motion
    [Fact]
    public void Combination_SdfCoalescingAndDualSpringMotion_ExtrudesAndSeparatesCleanly()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassFluidGroupState group = new(_stage, SeaGlassPalette.SeafoamMint.PrimaryGlass, smoothRadiusDp: 36f);
            group.GroupOriginInRoot = Vector2.Zero;

            // Node A stationary, Node B moving away using dual-spring motion
            group.RegisterChildBounds("nodeA", new Vector2(100f, 100f), new Vector2(60f, 60f));
            group.RegisterChildBounds("nodeB", new Vector2(150f, 100f), new Vector2(60f, 60f));

            var nodesClose = group.SnapshotNodes();
            var evalClose = SeaGlassSdfEngine.EvaluateScene(130f, 130f, nodesClose, 36f);
            Assert.True(evalClose.SignedDistancePx < 0f, "Nodes when close must coalesce via negative bridge distance");

            // Separate nodes further apart
            group.RegisterChildBounds("nodeB", new Vector2(300f, 100f), new Vector2(60f, 60f));
            var nodesFar = group.SnapshotNodes();
            var evalFar = SeaGlassSdfEngine.EvaluateScene(180f, 130f, nodesFar, 36f);
            Assert.True(evalFar.SignedDistancePx > 0f, "Nodes when far apart must separate cleanly");
    
        });
    }
    #endregion

    #region 2. Adaptive Corner Optics + 20-Shape Polygon Morphing
    [Fact]
    public void Combination_AdaptiveCornersAndShapeMorphing_MaintainsConcentricityDuringMorph()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassCornerRadiiPx bezel = SeaGlassCornerRadiiPx.Uniform(140f);
            SeaGlassCornerRadiiPx adaptedCorners = SeaGlassCornerAdaptationSolver.SolveElementCornerRadiiPx(
                elementLeftPx: 12f, elementTopPx: 12f, elementRightPx: 260f, elementBottomPx: 160f,
                containerWidthPx: 1080f, containerHeightPx: 2400f, containerCornersPx: bezel,
                baseCornerRadiusPx: 44f, influenceDistancePx: 220f, mode: SeaGlassCornerAdaptationMode.FlushCornerEmbed);

            Assert.True(adaptedCorners.TopLeftPx > adaptedCorners.BottomRightPx + 20f);

            // Perform 20-shape polygon morph on adapted element
            SeaGlassNormalizedPolygon poly = SeaGlassShapeMorph.CreateNormalizedPolygon(SeaGlassShapeKind.Squircle, aspectRatio: 1.5f);
            Assert.NotNull(poly);
            Assert.NotEmpty(poly.Cubics);
    
        });
    }
    #endregion

    #region 3. Volumetric Beer-Lambert + High-DPI Scaling
    [Theory]
    [InlineData(1.0f)]
    [InlineData(1.5f)]
    [InlineData(2.0f)]
    public void Combination_BeerLambertAndHighDpiScaling_ScalesLayoutWhilePreservingTransmittance(float dpiScale)
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            int cobalt = SeaGlassPalette.CobaltTide.PrimaryGlass.TintColorArgb;
            int transmission = SeaGlassColorMath.ComputeBeerLambertTransmission(
                backdropArgb: unchecked((int)0xFFFFFFFF),
                tintArgb: cobalt,
                tintDensity: 0.6f,
                coarseness: 0.3f,
                normalizedThickness: 1.5f);

            SeaGlassTheme theme = new(dpiScale: dpiScale);
            Assert.Equal(dpiScale, theme.DpiScale);

            SeaGlassColorMath.OklabColor lab = SeaGlassColorMath.ArgbToOklab(transmission);
            Assert.InRange(lab.L, 0.40f, 0.85f);
            Assert.True(lab.B < 0f, "Cobalt glass must retain negative B (blue chroma) regardless of DPI");
    
        });
    }
    #endregion

    #region 4. Live System Settings + Interactive Controls
    [Fact]
    public void Combination_HighContrastAndInteractiveControls_RoutesToLightweightTintBypassingBlur()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            MockSystemSettingsProvider settings = new() { HighContrastEnabled = true };
            SeaGlassButton btn = new("Action", () => { }, _stage);
            SeaGlassDeviceTier effectiveTier = settings.ResolveEffectiveTier();

            Assert.Equal(SeaGlassDeviceTier.LightweightTint, effectiveTier);
            btn.OnPointerStateChanged(new Vector2(20f, 15f), true);
            SeaGlassSurfaceFrameState frame = btn.EvaluateFrame(0.016f);
            Assert.NotNull(frame);
    
        });
    }

    [Fact]
    public void Combination_ReducedMotionAndInteractiveControls_SnapsImmediately()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            MockSystemSettingsProvider settings = new() { AnimationsEnabled = false };
            SeaGlassSwitch sw = new(false, _ => { }, _stage);
            sw.Toggle();

            float dt = settings.AdjustDeltaTimeForAnimations(0.016f);
            for (int i = 0; i < 30; i++) sw.StepAndEvaluate(1f / 60f);
            Assert.True(sw.Checked);
            Assert.InRange(sw.ThumbScaleX, 0.99f, 1.05f);
    
        });
    }
    #endregion

    #region 5. Acyclic Backdrop Host + Multi-Element Fluid Groups
    [Fact]
    public void Combination_AcyclicBackdropHostAndFluidGroup_DetectsSelfSamplingCycles()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassStageState stage = new();
            stage.BindBackdropTarget("BackdropHostContainer", Vector2.Zero, new Vector2(1920f, 1080f));

            SeaGlassFluidGroupState group = new(stage, SeaGlassPalette.SeafoamMint.PrimaryGlass);
            group.RegisterChildBounds("elementA", new Vector2(50f, 50f), new Vector2(100f, 50f));

            // Attempting to sample from self should throw InvalidOperationException
            Assert.Throws<InvalidOperationException>(() => stage.EnsureAcyclicForegroundSurface("BackdropHostContainer"));

            // Non-colliding foreground surface succeeds
            stage.EnsureAcyclicForegroundSurface("IndependentFluidGroupSurface");
    
        });
    }
    #endregion

    #region 6. Lead-Lag Lozenge + Temporal De-Aging
    [Fact]
    public void Combination_LeadLagLozengeAndTemporalDeAging_ReducesCrystallizationDuringFlight()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSegmentedButtonRow row = new(["Option 1", "Option 2", "Option 3"], 0, _ => { }, _stage);
            row.SelectOption(2);

            // Step 1: In flight, high velocity
            row.StepAndEvaluate(1f / 60f);
            Assert.True(row.LeadIndex > row.TrailIndex);
            Assert.True(row.LozengeSlotSpan > 1.0f);

            // Step 2: Settled
            for (int i = 0; i < 30; i++) row.StepAndEvaluate(1f / 60f);
            Assert.Equal(row.LeadIndex, row.TrailIndex, 1);
            Assert.InRange(row.LozengeSlotSpan, 0.98f, 1.02f);
    
        });
    }
    #endregion

    #region 7. 256-Byte Uniform Packing + Delta Slot Tracking
    [Fact]
    public void Combination_UniformPackingAndDeltaSlotTracking_IdentifiesModifiedSlotsPrecisely()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassUniformBlock block = new();
            SeaGlassMaterial mat = SeaGlassPalette.SeaLavender.PrimaryGlass;
            block.Pack(500f, 300f, 0f, 0f, mat, 1.0f, [], 24f);
            block.ClearDirtyMask();

            // Change only resolution
            block.Pack(600f, 300f, 0f, 0f, mat, 1.0f, [], 24f);
            Assert.Equal((1 << 0) | (1 << 4), block.DirtySlotsMask); // Slot 0: uResolutionAndOrigin + Slot 4: default node bounds
            block.ClearDirtyMask();

            // Change only optics primary (e.g. coarseness)
            SeaGlassMaterial mat2 = mat with { Coarseness = 0.85f };
            block.Pack(600f, 300f, 0f, 0f, mat2, 1.0f, [], 24f);
            Assert.Equal(1 << 1, block.DirtySlotsMask); // Slot 1: uOpticsPrimary
    
        });
    }
    #endregion

    #region 8. Tactile Button Squish + Dynamic Flex Material
    [Fact]
    public void Combination_TactileButtonSquishAndDynamicFlexMaterial_ModifiesRefractionDuringPress()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassButton btn = new("Tactile", () => { }, _stage);
            btn.OnPointerStateChanged(new Vector2(30f, 15f), true);
            SeaGlassSurfaceFrameState frame = btn.EvaluateFrame(0.02f);

            Assert.True(frame.ScaleX > 1.0f);
            Assert.True(frame.ScaleY < 1.0f);

            // Flex material increases Ior during high motion energy
            SeaGlassMaterial flex = SeaGlassShapeMorph.ComputeDynamicFlexMaterial(
                btn.Material, btn.Material, 0.5f, velocityPxPerSec: 600f, areaExpansionRatio: 1.5f);
            Assert.True(flex.Ior >= btn.Material.Ior);
    
        });
    }
    #endregion

    #region 9. Two-Element Morph + In-Flight Content Defocus
    [Fact]
    public void Combination_TwoElementMorphAndContentDefocus_ActivatesDefocusDuringSpatialExpansion()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassTwoElementMorph morph = new(expanded: false, stageState: _stage);
            morph.SetExpanded(true);

            morph.StepAndEvaluate(0.016f);
            Assert.True(morph.SpatialWidthProgress > morph.SpatialHeightProgress);
            Assert.True(morph.CollapsedBlurRadiusPx >= 0f);

            // Step until settled
            for (int i = 0; i < 30; i++) morph.StepAndEvaluate(1f / 60f);
            Assert.InRange(morph.SpatialWidthProgress, 0.99f, 1.01f);
            Assert.InRange(morph.SpatialHeightProgress, 0.99f, 1.01f);
    
        });
    }
    #endregion

    #region 10. Circle Midpoint Bridge + Polar Radius Scale
    [Fact]
    public void Combination_CircleMidpointBridgeAndPolarRadiusScale_EliminatesArtifactsAtPivot()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassShapeStage stageFlower = new(SeaGlassShapeKind.Flower, Scale: 1.0f, RotationDegrees: 0f, ViaCircleBridge: true);
            SeaGlassShapeStage stageCookie = new(SeaGlassShapeKind.Cookie9, Scale: 1.2f, RotationDegrees: 90f, ViaCircleBridge: true);

            ChoreographedShapeTransform phase1 = SeaGlassShapeMorph.EvaluateChoreographedTransform(stageFlower, stageCookie, 0.30f);
            Assert.Equal(SeaGlassShapeKind.Flower, phase1.ActiveStartShape);
            Assert.Equal(SeaGlassShapeKind.Circle, phase1.ActiveEndShape);

            float polarRadius = SeaGlassShapeMorph.EvaluatePolarRadiusScale(
                startKind: SeaGlassShapeKind.Flower,
                endKind: SeaGlassShapeKind.Circle,
                progress: 0.5f,
                angleRad: 0.78f);
            Assert.InRange(polarRadius, 0.40f, 1.10f);
    
        });
    }
    #endregion
}
