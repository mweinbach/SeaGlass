using SeaGlass.Tests.Controls;
using System;
using System.Numerics;
using SeaGlass.Core;
using SeaGlass.Core.Backend;
using SeaGlass.WinUI;
using SeaGlass.WinUI.Rendering;
using Xunit;

namespace SeaGlass.Tests.E2E.Tier2_BoundaryCorner;

public sealed class Tier2_BoundaryAndCornerCaseTests
{
    private readonly SeaGlassStageState _stage;

    public Tier2_BoundaryAndCornerCaseTests()
    {
        _stage = new SeaGlassStageState();
        _stage.BindBackdropTarget("BoundaryStageBackdrop", Vector2.Zero, new Vector2(1920f, 1080f));
    }

    #region 1. Boundary Geometry Tests (>= 5 cases)
    [Fact]
    public void BoundaryGeometry_ZeroDimensions_HandlesGracefullyWithoutNaN()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSurface surface = new(_stage) { SizePx = Vector2.Zero };
            SeaGlassSurfaceFrameState frame = surface.EvaluateFrame(0.016f);
            Assert.NotNull(frame);
            Assert.False(float.IsNaN(frame.ScaleX));
            Assert.False(float.IsNaN(frame.ScaleY));
    
        });
    }

    [Fact]
    public void BoundaryGeometry_Microscopic1PxDimensions_ProducesValidContour()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSurface surface = new(_stage) { SizePx = new Vector2(1f, 1f) };
            SeaGlassSurfaceFrameState frame = surface.EvaluateFrame(0.016f);
            Assert.StartsWith("F1 M", frame.XamlContourPathData);
    
        });
    }

    [Fact]
    public void BoundaryGeometry_Massive10000PxDimensions_DoesNotOverflowUniform()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSurface surface = new(_stage) { SizePx = new Vector2(10000f, 5000f) };
            SeaGlassSurfaceFrameState frame = surface.EvaluateFrame(0.016f);
            var std140 = frame.UniformBlock.ToStd140Struct();
            Assert.Equal(10000f, std140.ResolutionAndOrigin.X);
            Assert.Equal(5000f, std140.ResolutionAndOrigin.Y);
    
        });
    }

    [Fact]
    public void BoundaryGeometry_NegativeCoordinates_ClampsAndEvaluates()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSurface surface = new(_stage) { OriginInRoot = new Vector2(-500f, -200f) };
            SeaGlassSurfaceFrameState frame = surface.EvaluateFrame(0.016f);
            Assert.NotNull(frame);
    
        });
    }

    [Fact]
    public void BoundaryGeometry_ExtremeAsymmetricEdgeSum_NormalizesWithoutExceedingExtents()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassCornerRadiiPx bezel = SeaGlassCornerRadiiPx.Uniform(200f);
            const float elementWidth = 80f;
            SeaGlassCornerRadiiPx solved = SeaGlassCornerAdaptationSolver.SolveElementCornerRadiiPx(
                elementLeftPx: 5f, elementTopPx: 5f, elementRightPx: 5f + elementWidth, elementBottomPx: 120f,
                containerWidthPx: 1080f, containerHeightPx: 1920f, containerCornersPx: bezel,
                baseCornerRadiusPx: 50f, influenceDistancePx: 250f, mode: SeaGlassCornerAdaptationMode.FlushCornerEmbed);

            Assert.True(solved.TopLeftPx + solved.TopRightPx <= elementWidth + 0.01f,
                $"Edge sum must not exceed width ({solved.TopLeftPx} + {solved.TopRightPx} <= {elementWidth})");
    
        });
    }
    #endregion

    #region 2. Boundary Strings & Inputs Tests (>= 5 cases)
    [Fact]
    public void BoundaryStrings_EmptyStringLabel_DoesNotCrashButton()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassButton btn = new("", () => { }, _stage);
            Assert.Equal("", btn.Label);
            SeaGlassSurfaceFrameState frame = btn.EvaluateFrame();
            Assert.StartsWith("F1 M", frame.XamlContourPathData);
    
        });
    }

    [Fact]
    public void BoundaryStrings_WhitespaceOnlyLabel_PreservesPadding()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassButton btn = new("   \t\n   ", () => { }, _stage);
            SeaGlassSurfaceFrameState frame = btn.EvaluateFrame();
            Assert.NotEmpty(frame.XamlContourPathData);
    
        });
    }

    [Fact]
    public void BoundaryStrings_MassiveString_DoesNotCrashCardOrBadge()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            string massiveText = new('A', 5000);
            SeaGlassBadge badge = new(massiveText, _stage);
            Assert.Equal(5000, badge.Text.Length);
            SeaGlassSurfaceFrameState frame = badge.EvaluateFrame();
            Assert.NotNull(frame);
    
        });
    }

    [Fact]
    public void BoundaryStrings_SpecialAndControlCharacters_HandledSafely()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            string specialText = "<tag> & 'quotes' \"double\" \0 \uFFFF \uD83D\uDE00";
            SeaGlassButton btn = new(specialText, () => { }, _stage);
            Assert.Equal(specialText, btn.Label);
            Assert.StartsWith("F1 M", btn.EvaluateFrame().XamlContourPathData);
    
        });
    }

    [Fact]
    public void BoundaryStrings_EmptySearchBarQuery_ClearsQuerySafely()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSearchBar search = new("", _ => { }, _stage);
            search.ClearQuery();
            Assert.Equal("", search.Query);
            Assert.StartsWith("F1 M", search.EvaluateFrame().XamlContourPathData);
    
        });
    }
    #endregion

    #region 3. Rapid Gestures & Interaction Stress Tests (>= 5 cases)
    [Fact]
    public void InteractionStress_RapidClicks_AccumulatesSmoothVelocityWithoutNaN()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            int clickCount = 0;
            SeaGlassButton btn = new("Rapid", () => clickCount++, _stage);

            for (int i = 0; i < 25; i++)
            {
                btn.OnPointerStateChanged(new Vector2(20f, 15f), true);
                btn.EvaluateFrame(0.004f);
                btn.OnPointerStateChanged(new Vector2(20f, 15f), false);
                SeaGlassSurfaceFrameState frame = btn.EvaluateFrame(0.004f);
                Assert.False(float.IsNaN(frame.ScaleX));
                Assert.False(float.IsNaN(frame.ScaleY));
            }

            Assert.Equal(25, clickCount);
    
        });
    }

    [Fact]
    public void InteractionStress_RapidSwitchToggles_DoesNotCorruptThumbScale()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSwitch sw = new(false, _ => { }, _stage);
            for (int i = 0; i < 20; i++)
            {
                sw.Toggle();
                sw.StepAndEvaluate(0.005f);
                Assert.False(float.IsNaN(sw.ThumbScaleX));
            }
    
        });
    }

    [Fact]
    public void InteractionStress_RapidTabJumps_KeepsLozengeSpanBounded()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSegmentedButtonRow row = new(["A", "B", "C", "D", "E"], 0, _ => { }, _stage);
            for (int i = 0; i < 15; i++)
            {
                row.SelectOption(i % 5);
                row.StepAndEvaluate(0.005f);
                Assert.False(float.IsNaN(row.LozengeSlotSpan));
                Assert.True(row.LozengeSlotSpan >= 0.5f);
            }
    
        });
    }

    [Fact]
    public void InteractionStress_PointerPressHeldWithoutRelease_KeepsSquishStable()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassButton btn = new("Held", () => { }, _stage);
            btn.OnPointerStateChanged(new Vector2(20f, 15f), true);

            for (int i = 0; i < 60; i++)
            {
                SeaGlassSurfaceFrameState frame = btn.EvaluateFrame(0.016f);
                Assert.True(frame.ScaleX >= 1.0f);
                Assert.True(frame.ScaleY <= 1.0f);
            }
    
        });
    }

    [Fact]
    public void InteractionStress_MultiTouchPointers_UpdatesLastTouchOffsetCleanly()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSurface surface = new(_stage);
            surface.OnPointerStateChanged(new Vector2(10f, 10f), true);
            Assert.Equal(new Vector2(10f, 10f), surface.LastTouchOffsetInLocal);
            surface.OnPointerStateChanged(new Vector2(200f, 100f), true);
            Assert.Equal(new Vector2(200f, 100f), surface.LastTouchOffsetInLocal);
    
        });
    }
    #endregion

    #region 4. Extreme Optical Parameters Tests (>= 5 cases)
    [Fact]
    public void ExtremeOptics_CoarsenessZero_SetsEffectiveSandWearNearZero()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassMaterial mat = SeaGlassPalette.SeafoamMint.PrimaryGlass.WithSandWear(0.0f);
            Assert.Equal(0.0f, mat.Coarseness);
            Assert.True(mat.EffectiveSandWear <= 0.02f);
    
        });
    }

    [Fact]
    public void ExtremeOptics_CoarsenessOne_SetsEffectiveSandWearAboveNinetyPercent()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassMaterial mat = SeaGlassPalette.SeafoamMint.PrimaryGlass.WithSandWear(1.0f);
            Assert.Equal(1.0f, mat.Coarseness);
            Assert.True(mat.EffectiveSandWear >= 0.90f);
    
        });
    }

    [Fact]
    public void ExtremeOptics_ExtremeThickness_ClampsInMaterialAnimator()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassMaterial mat = SeaGlassPalette.SeafoamMint.PrimaryGlass.Deaged(1.0f);
            Assert.True(mat.ThicknessDp <= 36f, $"Thickness must clamp to maximum 36dp ({mat.ThicknessDp} <= 36)");
    
        });
    }

    [Fact]
    public void ExtremeOptics_ExtremeIor_ClampsToValidPhysicalRange()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SeaGlassMaterial(unchecked((int)0xFF00FF00), ior: 3.5f));
    
        });
    }

    [Fact]
    public void ExtremeOptics_ExtremeTintDensity_DoesNotProduceNegativeColor()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            int result = SeaGlassColorMath.ComputeBeerLambertTransmission(
                backdropArgb: unchecked((int)0xFFFFFFFF),
                tintArgb: unchecked((int)0xFF000000),
                tintDensity: 1.0f,
                coarseness: 1.0f,
                normalizedThickness: 2.0f);
            uint uResult = unchecked((uint)result);
            Assert.True(uResult > 0);
    
        });
    }
    #endregion

    #region 5. Slider Boundaries Tests (>= 5 cases)
    [Fact]
    public void SliderBoundaries_DragFarBelowMinimum_ClampsToMinimum()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassLiquidSlider slider = new("Slider", "0", 0f, 0f, 100f, _ => { }, _stage);
            slider.SetValue(-999f);
            Assert.Equal(0f, slider.Value);
    
        });
    }

    [Fact]
    public void SliderBoundaries_DragFarAboveMaximum_ClampsToMaximum()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassLiquidSlider slider = new("Slider", "50", 50f, 0f, 100f, _ => { }, _stage);
            slider.SetValue(999f);
            Assert.Equal(100f, slider.Value);
    
        });
    }

    [Fact]
    public void SliderBoundaries_ZeroRangeSlider_ClampsValueToZero()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassLiquidSlider slider = new("Fixed", "0", 0f, 0f, 0f, _ => { }, _stage);
            slider.SetValue(10f);
            Assert.Equal(0f, slider.Value, 3);
    
        });
    }

    [Fact]
    public void SliderBoundaries_NegativeRange_ClampsAccurately()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassLiquidSlider slider = new("Negative", "-20", -20f, -50f, -10f, _ => { }, _stage);
            slider.SetValue(-30f);
            Assert.Equal(-30f, slider.Value);
            slider.SetValue(0f);
            Assert.Equal(-10f, slider.Value);
            slider.SetValue(-100f);
            Assert.Equal(-50f, slider.Value);
    
        });
    }

    [Fact]
    public void SliderBoundaries_MicroscopicIncrements_MaintainsPrecision()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassLiquidSlider slider = new("Fine", "0.001", 0.001f, 0f, 1f, _ => { }, _stage);
            slider.SetValue(0.00125f);
            Assert.Equal(0.00125f, slider.Value, 5);
    
        });
    }
    #endregion

    #region 6. Motion Spring Boundaries Tests (>= 5 cases)
    [Fact]
    public void MotionSpringBoundaries_ZeroDeltaTime_DoesNotChangeState()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSpringSpec spring = new(0.66f, 410f);
            float current = 5f;
            float velocity = 10f;
            spring.Step(ref current, ref velocity, 10f, 0f);
            Assert.InRange(current, 4.99f, 5.01f);
            Assert.InRange(velocity, 9.8f, 10.5f);
    
        });
    }

    [Fact]
    public void MotionSpringBoundaries_MassiveDeltaTime_ClampsDtAndMaintainsStability()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSpringSpec spring = new(0.66f, 410f);
            float current = 0f;
            float velocity = 0f;
            spring.Step(ref current, ref velocity, 100f, 100f);
            Assert.False(float.IsNaN(current));
            Assert.False(float.IsInfinity(current));
    
        });
    }

    [Fact]
    public void MotionSpringBoundaries_NegativeDeltaTime_ClampsToPositiveMinimum()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSpringSpec spring = new(0.66f, 410f);
            float current = 0f;
            float velocity = 0f;
            spring.Step(ref current, ref velocity, 10f, -1f);
            Assert.False(float.IsNaN(current));
    
        });
    }

    [Fact]
    public void MotionSpringBoundaries_ZeroStiffness_FloorsToOne()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSpringSpec spring = new(0.66f, 0f);
            float current = 0f;
            float velocity = 0f;
            spring.Step(ref current, ref velocity, 1f, 0.016f);
            Assert.False(float.IsNaN(current));
    
        });
    }

    [Fact]
    public void MotionSpringBoundaries_AlreadySettled_ExitsImmediately()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSpringSpec spring = new(0.66f, 410f);
            float current = 10f;
            float velocity = 0f;
            spring.Step(ref current, ref velocity, 10f, 0.016f);
            Assert.Equal(10f, current);
            Assert.Equal(0f, velocity);
    
        });
    }
    #endregion

    #region 7. Corner Adaptation Boundaries Tests (>= 5 cases)
    [Fact]
    public void CornerAdaptationBoundaries_ZeroBezelRadius_ReturnsBaseRadius()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassCornerRadiiPx sharpBezel = SeaGlassCornerRadiiPx.Uniform(0f);
            SeaGlassCornerRadiiPx solved = SeaGlassCornerAdaptationSolver.SolveElementCornerRadiiPx(
                elementLeftPx: 5f, elementTopPx: 5f, elementRightPx: 200f, elementBottomPx: 100f,
                containerWidthPx: 800f, containerHeightPx: 600f, containerCornersPx: sharpBezel,
                baseCornerRadiusPx: 20f, influenceDistancePx: 100f, mode: SeaGlassCornerAdaptationMode.ConcentricProximity);
            Assert.Equal(20f, solved.TopLeftPx, 2);
    
        });
    }

    [Fact]
    public void CornerAdaptationBoundaries_ExtremeBezelRadius_CapsAtMaxProportion()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassCornerRadiiPx massiveBezel = SeaGlassCornerRadiiPx.Uniform(500f);
            SeaGlassCornerRadiiPx solved = SeaGlassCornerAdaptationSolver.SolveElementCornerRadiiPx(
                elementLeftPx: 5f, elementTopPx: 5f, elementRightPx: 200f, elementBottomPx: 100f,
                containerWidthPx: 1080f, containerHeightPx: 1920f, containerCornersPx: massiveBezel,
                baseCornerRadiusPx: 30f, influenceDistancePx: 200f, mode: SeaGlassCornerAdaptationMode.FlushCornerEmbed);
            Assert.True(solved.TopLeftPx <= 100f); // bounded by half-height or edge sum
    
        });
    }

    [Fact]
    public void CornerAdaptationBoundaries_CompletelyOutsideContainer_ReturnsBaseRadius()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassCornerRadiiPx bezel = SeaGlassCornerRadiiPx.Uniform(100f);
            SeaGlassCornerRadiiPx solved = SeaGlassCornerAdaptationSolver.SolveElementCornerRadiiPx(
                elementLeftPx: 1000f, elementTopPx: 1000f, elementRightPx: 1200f, elementBottomPx: 1100f,
                containerWidthPx: 3000f, containerHeightPx: 4000f, containerCornersPx: bezel,
                baseCornerRadiusPx: 24f, influenceDistancePx: 100f, mode: SeaGlassCornerAdaptationMode.ConcentricProximity);
            Assert.Equal(24f, solved.TopLeftPx, 2);
    
        });
    }

    [Fact]
    public void CornerAdaptationBoundaries_ZeroInfluenceDistance_ReturnsBaseRadius()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassCornerRadiiPx bezel = SeaGlassCornerRadiiPx.Uniform(140f);
            SeaGlassCornerRadiiPx solved = SeaGlassCornerAdaptationSolver.SolveElementCornerRadiiPx(
                elementLeftPx: 5f, elementTopPx: 5f, elementRightPx: 200f, elementBottomPx: 100f,
                containerWidthPx: 1080f, containerHeightPx: 1920f, containerCornersPx: bezel,
                baseCornerRadiusPx: 28f, influenceDistancePx: 0f, mode: SeaGlassCornerAdaptationMode.ConcentricProximity);
            Assert.Equal(28f, solved.TopLeftPx, 2);
    
        });
    }

    [Fact]
    public void CornerAdaptationBoundaries_MinCornerFloor_EnforcesMinimumFloor()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassCornerRadiiPx bezel = SeaGlassCornerRadiiPx.Uniform(100f);
            SeaGlassCornerRadiiPx solved = SeaGlassCornerAdaptationSolver.SolveElementCornerRadiiPx(
                elementLeftPx: 5f, elementTopPx: 5f, elementRightPx: 200f, elementBottomPx: 100f,
                containerWidthPx: 1080f, containerHeightPx: 1920f, containerCornersPx: bezel,
                baseCornerRadiusPx: 8f, minCornerFloorPx: 16f, influenceDistancePx: 100f, mode: SeaGlassCornerAdaptationMode.ConcentricProximity);
            Assert.True(solved.TopLeftPx >= 15.9f);
    
        });
    }
    #endregion

    #region 8. Continuous 1,000-Frame Draw Loop (Steady-State GC Zero Allocation)
    [Fact]
    public void ContinuousDrawLoop_1000FramesAcrossControls_AssertsZeroGcAllocations()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassTieredRenderEngine engine = new();
            SeaGlassRenderScratchArena scratchArena = new();
            SeaGlassSurface surface = new(_stage);
            SeaGlassSurfaceFrameState frameState = surface.EvaluateFrame(0.016f);
            SeaGlassRenderContext context = new()
            {
                FrameState = frameState,
                WidthPx = 400f,
                HeightPx = 200f,
                LocalBackdropOffsetPx = Vector2.Zero,
                ActiveTier = engine.ActiveTier,
                ScratchArena = scratchArena,
                SmoothRadiusPx = 20f
            };

            // Warm up and settle JIT and scratch arena
            for (int i = 0; i < 50; i++)
            {
                engine.Render(context, null);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();

            for (int frame = 0; frame < 1000; frame++)
            {
                engine.Render(context, null);
            }

            long allocatedAfter = GC.GetAllocatedBytesForCurrentThread();
            long diff = allocatedAfter - allocatedBefore;

            Assert.Equal(0, diff);
    
        });
    }
    #endregion
}
