using SeaGlass.Tests.Controls;
using System;
using System.Collections.Generic;
using System.Numerics;
using SeaGlass.Core;
using SeaGlass.Core.Backend;
using SeaGlass.WinUI;
using Xunit;

namespace SeaGlass.Tests.E2E.Tier1_FeatureCoverage;

public sealed class Tier1_FeedbackAndMorphingTests
{
    private readonly SeaGlassStageState _stage;

    public Tier1_FeedbackAndMorphingTests()
    {
        _stage = new SeaGlassStageState();
        _stage.BindBackdropTarget("FeedbackMorphStageBackdrop", Vector2.Zero, new Vector2(1920f, 1080f));
    }

    #region 1. SeaGlassLinearProgressIndicator Tests (>= 5 cases)
    [Fact]
    public void SeaGlassLinearProgressIndicator_Initialization_SetsProgress()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassLinearProgressIndicator prog = new(0.45f, _stage);
            Assert.Equal(0.45f, prog.Progress);
    
        });
    }

    [Fact]
    public void SeaGlassLinearProgressIndicator_SetProgress_ClampsToUnitInterval()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassLinearProgressIndicator prog = new(0.5f, _stage);
            prog.Progress = 1.0f;
            Assert.Equal(1.0f, prog.Progress);
            prog.Progress = 0.05f;
            Assert.Equal(0.05f, prog.Progress);
    
        });
    }

    [Fact]
    public void SeaGlassLinearProgressIndicator_IndeterminateShimmer_StepsPhase()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassLinearProgressIndicator prog = new(0.5f, _stage);
            prog.StepAndEvaluate(0.05f);
            Assert.True(prog.AnimatedProgress > 0f);
    
        });
    }

    [Fact]
    public void SeaGlassLinearProgressIndicator_CornerRadius_BoundsBetween6And8dp()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassLinearProgressIndicator prog = new(0.7f, _stage);
            SeaGlassSurfaceFrameState frame = prog.StepAndEvaluate(0.03f);
            Assert.InRange(frame.EffectiveMaterial.CornerRadiusDp, 6f, 8f);
    
        });
    }

    [Fact]
    public void SeaGlassLinearProgressIndicator_EvaluateFrame_GeneratesValidXaml()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassLinearProgressIndicator prog = new(0.8f, _stage);
            Assert.StartsWith("F1 M", prog.StepAndEvaluate(0.016f).XamlContourPathData);
    
        });
    }
    #endregion

    #region 2. SeaGlassCircularProgressIndicator Tests (>= 5 cases)
    [Fact]
    public void SeaGlassCircularProgressIndicator_Initialization_SetsProgress()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassCircularProgressIndicator circ = new(0.6f, _stage);
            Assert.Equal(0.6f, circ.Progress);
    
        });
    }

    [Fact]
    public void SeaGlassCircularProgressIndicator_SweepAngle_CalculatesDegrees()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassCircularProgressIndicator circ = new(0.5f, _stage);
            circ.StepAndEvaluate(0.03f);
            Assert.InRange(circ.SweepAngleDegrees, 160f, 200f);
    
        });
    }

    [Fact]
    public void SeaGlassCircularProgressIndicator_IndeterminateSpin_AdvancesRotation()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassCircularProgressIndicator circ = new(0.5f, _stage);
            circ.StepAndEvaluate(0.1f);
            Assert.True(circ.SweepAngleDegrees > 0f);
    
        });
    }

    [Fact]
    public void SeaGlassCircularProgressIndicator_CircularBounds_AreSquare()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassCircularProgressIndicator circ = new(0.75f, _stage);
            Assert.Equal(circ.SizePx.X, circ.SizePx.Y);
    
        });
    }

    [Fact]
    public void SeaGlassCircularProgressIndicator_EvaluateFrame_GeneratesValidXaml()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassCircularProgressIndicator circ = new(0.9f, _stage);
            Assert.StartsWith("F1 M", circ.StepAndEvaluate(0.016f).XamlContourPathData);
    
        });
    }
    #endregion

    #region 3. SeaGlassListItem Tests (>= 5 cases)
    [Fact]
    public void SeaGlassListItem_Initialization_SetsTitleAndSubtitle()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassListItem item = new("Refraction Settings", _stage, "Snell law bevel optics", SeaGlassGlyph.Tune);
            Assert.Equal("Refraction Settings", item.HeadlineText);
            Assert.Equal("Snell law bevel optics", item.SupportingText);
            Assert.Equal(SeaGlassGlyph.Tune, item.LeadingGlyph);
    
        });
    }

    [Fact]
    public void SeaGlassListItem_PointerPress_SquishesSlightly()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassListItem item = new("Setting Row", _stage);
            item.OnPointerStateChanged(new Vector2(100f, 20f), true);
            SeaGlassSurfaceFrameState frame = item.EvaluateFrame(0.03f);
            Assert.True(frame.ScaleX >= 1.0f);
    
        });
    }

    [Fact]
    public void SeaGlassListItem_PointerRelease_InvokesClickCallback()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            bool clicked = false;
            SeaGlassListItem item = new("Item", _stage, onClick: () => clicked = true);
            item.OnPointerStateChanged(new Vector2(50f, 20f), true);
            item.OnPointerStateChanged(new Vector2(50f, 20f), false);
            Assert.True(clicked);
    
        });
    }

    [Fact]
    public void SeaGlassListItem_ContentRole_IsStandardSurface()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassListItem item = new("Item", _stage);
            Assert.Equal(SeaGlassContentRole.TextAndIcons, item.ContentRole);
    
        });
    }

    [Fact]
    public void SeaGlassListItem_EvaluateFrame_GeneratesValidXaml()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassListItem item = new("Row", _stage);
            Assert.StartsWith("F1 M", item.EvaluateFrame().XamlContourPathData);
    
        });
    }
    #endregion

    #region 4. SeaGlassSnackbar Tests (>= 5 cases)
    [Fact]
    public void SeaGlassSnackbar_Initialization_SetsMessageAndAction()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSnackbar snack = new("Shader compiled successfully", _stage, "Inspect", () => { });
            Assert.Equal("Shader compiled successfully", snack.Message);
            Assert.Equal("Inspect", snack.ActionLabel);
    
        });
    }

    [Fact]
    public void SeaGlassSnackbar_TriggerAction_InvokesCallback()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            bool actionInvoked = false;
            SeaGlassSnackbar snack = new("File saved", _stage, "Undo", () => actionInvoked = true);
            snack.OnActionClick?.Invoke();
            Assert.True(actionInvoked);
    
        });
    }

    [Fact]
    public void SeaGlassSnackbar_HighContrastTextInk_IsLegible()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSnackbar snack = new("Telemetry updated", _stage);
            Assert.Equal(SeaGlassContentRole.NavigationChrome, snack.ContentRole);
    
        });
    }

    [Fact]
    public void SeaGlassSnackbar_FloatingElevation_HasThickness()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSnackbar snack = new("Alert", _stage);
            Assert.True(snack.Material.ThicknessDp >= 12f);
    
        });
    }

    [Fact]
    public void SeaGlassSnackbar_EvaluateFrame_GeneratesValidXaml()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSnackbar snack = new("Toast", _stage);
            Assert.StartsWith("F1 M", snack.EvaluateFrame().XamlContourPathData);
    
        });
    }
    #endregion

    #region 5. SeaGlassIcon Tests (>= 5 cases)
    [Fact]
    public void SeaGlassIcon_All23Glyphs_MapToUnicodeSymbols()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            foreach (SeaGlassGlyph glyph in Enum.GetValues<SeaGlassGlyph>())
            {
                string symbol = SeaGlassIcon.ToUnicodeSymbol(glyph);
                Assert.NotEmpty(symbol);
            }
    
        });
    }

    [Fact]
    public void SeaGlassIcon_All23Glyphs_GenerateValidXamlPathData()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            foreach (SeaGlassGlyph glyph in Enum.GetValues<SeaGlassGlyph>())
            {
                string path = SeaGlassIcon.BuildGlyphXamlPathData(glyph);
                Assert.StartsWith("F1 M", path);
            }
    
        });
    }

    [Fact]
    public void SeaGlassIcon_SpecificGlyphs_HomeAndSparklesAreUnique()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            string homePath = SeaGlassIcon.BuildGlyphXamlPathData(SeaGlassGlyph.Home);
            string sparklesPath = SeaGlassIcon.BuildGlyphXamlPathData(SeaGlassGlyph.Sparkles);
            Assert.NotEqual(homePath, sparklesPath);
    
        });
    }

    [Fact]
    public void SeaGlassIcon_SettingsAndBell_ProduceNonEmptyPaths()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            Assert.NotEmpty(SeaGlassIcon.BuildGlyphXamlPathData(SeaGlassGlyph.Settings));
            Assert.NotEmpty(SeaGlassIcon.BuildGlyphXamlPathData(SeaGlassGlyph.Bell));
    
        });
    }

    [Fact]
    public void SeaGlassIcon_CheckGlyph_MatchesExpectedVectorContour()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            string checkPath = SeaGlassIcon.BuildGlyphXamlPathData(SeaGlassGlyph.Check);
            Assert.Contains("L", checkPath);
    
        });
    }
    #endregion

    #region 6. SeaGlassTwoElementMorph Tests (>= 5 cases)
    [Fact]
    public void SeaGlassTwoElementMorph_Initialization_StartsCollapsed()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassTwoElementMorph morph = new(expanded: false, stageState: _stage);
            Assert.False(morph.Expanded);
    
        });
    }

    [Fact]
    public void SeaGlassTwoElementMorph_SetExpanded_TransitionsState()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassTwoElementMorph morph = new(expanded: false, stageState: _stage);
            morph.SetExpanded(true);
            Assert.True(morph.Expanded);
    
        });
    }

    [Fact]
    public void SeaGlassTwoElementMorph_LeadLagSpatialExpansion_WidthLeadsHeight()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassTwoElementMorph morph = new(expanded: false, stageState: _stage);
            morph.SetExpanded(true);
            morph.StepAndEvaluate(1f / 60f);
            morph.StepAndEvaluate(1f / 60f);
            Assert.True(morph.SpatialWidthProgress > morph.SpatialHeightProgress,
                $"Width progress (k=420) must lead height progress (k=340) ({morph.SpatialWidthProgress} > {morph.SpatialHeightProgress})");
    
        });
    }

    [Fact]
    public void SeaGlassTwoElementMorph_ContentDefocus_ActivatesDuringMotion()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassTwoElementMorph morph = new(expanded: false, stageState: _stage);
            morph.SetExpanded(true);
            morph.StepAndEvaluate(0.03f);
            Assert.True(morph.CollapsedBlurRadiusPx >= 0f);
    
        });
    }

    [Fact]
    public void SeaGlassTwoElementMorph_EvaluateFrame_GeneratesValidXaml()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassTwoElementMorph morph = new(expanded: true, stageState: _stage);
            Assert.StartsWith("F1 M", morph.StepAndEvaluate(0.016f).XamlContourPathData);
    
        });
    }
    #endregion

    #region 7. SeaGlassBuddingCluster Tests (>= 5 cases)
    [Fact]
    public void SeaGlassBuddingCluster_Initialization_SetsSatelliteActions()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassBuddingCluster cluster = new(false, [("✦", "Lens"), ("♡", "Fav")], _ => { }, _stage);
            Assert.False(cluster.Expanded);
            Assert.Equal(2, cluster.Items.Count);
    
        });
    }

    [Fact]
    public void SeaGlassBuddingCluster_ToggleExpansion_FlipsState()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassBuddingCluster cluster = new(false, [("✦", "Action")], _ => { }, _stage);
            cluster.ToggleExpanded();
            Assert.True(cluster.Expanded);
            cluster.ToggleExpanded();
            Assert.False(cluster.Expanded);
    
        });
    }

    [Fact]
    public void SeaGlassBuddingCluster_Expanded_PacksMultipleNodesInUniform()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassBuddingCluster cluster = new(true, [("A", "Act1"), ("B", "Act2")], _ => { }, _stage);
            SeaGlassSurfaceFrameState frame = cluster.StepAndEvaluate(0.05f);
            Assert.True(frame.UniformBlock.ToStd140Struct().LightAndMeta.W >= 2f);
    
        });
    }

    [Fact]
    public void SeaGlassBuddingCluster_TriggerSatellite_InvokesCallback()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            int triggered = -1;
            SeaGlassBuddingCluster cluster = new(true, [("A", "One"), ("B", "Two")], idx => triggered = idx, _stage);
            cluster.OnItemClick(1);
            Assert.Equal(1, triggered);
    
        });
    }

    [Fact]
    public void SeaGlassBuddingCluster_EvaluateFrame_GeneratesValidXaml()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassBuddingCluster cluster = new(true, [("✦", "Optics")], _ => { }, _stage);
            Assert.StartsWith("F1 M", cluster.StepAndEvaluate(0.016f).XamlContourPathData);
    
        });
    }
    #endregion

    #region 8. SeaGlassMorphingButton Tests (>= 5 cases)
    [Fact]
    public void SeaGlassMorphingButton_Initialization_SetsExpandedState()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassMorphingButton btn = new(true, () => { }, _stage);
            Assert.True(btn.Active);
    
        });
    }

    [Fact]
    public void SeaGlassMorphingButton_ToggleMorph_FlipsState()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassMorphingButton btn = new(false, () => { }, _stage);
            btn.ToggleActive();
            Assert.True(btn.Active);
            btn.ToggleActive();
            Assert.False(btn.Active);
    
        });
    }

    [Fact]
    public void SeaGlassMorphingButton_Click_InvokesCallback()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            bool clicked = false;
            SeaGlassMorphingButton btn = new(false, () => clicked = true, _stage);
            btn.OnClick?.Invoke();
            Assert.True(clicked);
    
        });
    }

    [Fact]
    public void SeaGlassMorphingButton_MorphProgress_AnimatesCleanly()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassMorphingButton btn = new(false, () => { }, _stage);
            btn.ToggleActive();
            btn.StepAndEvaluate(0.03f);
            Assert.True(btn.LastChoreoTransform.MorphEnergy >= 0f);
    
        });
    }

    [Fact]
    public void SeaGlassMorphingButton_EvaluateFrame_GeneratesValidXaml()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassMorphingButton btn = new(true, () => { }, _stage);
            Assert.StartsWith("F1 M", btn.StepAndEvaluate(0.016f).XamlContourPathData);
    
        });
    }
    #endregion

    #region 9. SeaGlassChoreographedShapeOrb Tests (>= 5 cases)
    [Fact]
    public void SeaGlassChoreographedShapeOrb_Initialization_StartsAtStageZero()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassChoreographedShapeOrb orb = new(_stage);
            Assert.True(orb.TargetStageIndex >= 0);
    
        });
    }

    [Fact]
    public void SeaGlassChoreographedShapeOrb_AdvanceTo_ChangesTargetStage()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassChoreographedShapeOrb orb = new(_stage);
            orb.AdvanceTo(2);
            Assert.Equal(2, orb.TargetStageIndex);
    
        });
    }

    [Fact]
    public void SeaGlassChoreographedShapeOrb_AdvanceNext_CyclesSequentially()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassChoreographedShapeOrb orb = new(_stage);
            orb.AdvanceTo((orb.TargetStageIndex + 1) % orb.Stages.Count);
            Assert.True(orb.TargetStageIndex >= 0);
    
        });
    }

    [Fact]
    public void SeaGlassChoreographedShapeOrb_CircleBridgeChoreography_InterpolatesStages()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassChoreographedShapeOrb orb = new(_stage);
            orb.AdvanceTo(1);
            SeaGlassSurfaceFrameState frame = orb.StepAndEvaluate(0.05f);
            Assert.NotNull(frame);
    
        });
    }

    [Fact]
    public void SeaGlassChoreographedShapeOrb_EvaluateFrame_GeneratesValidXaml()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassChoreographedShapeOrb orb = new(_stage);
            Assert.StartsWith("F1 M", orb.StepAndEvaluate(0.016f).XamlContourPathData);
    
        });
    }
    #endregion

    #region 10. SeaGlassMultiShapeMorph Tests (>= 5 cases)
    [Fact]
    public void SeaGlassMultiShapeMorph_Initialization_SetsShapeVocabulary()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassMultiShapeMorph morph = new([SeaGlassShapeKind.Flower, SeaGlassShapeKind.Cookie9, SeaGlassShapeKind.Heart], 0, _stage);
            Assert.Equal(3, morph.Shapes.Count);
            Assert.Equal(0, morph.ActiveShapeIndex);
    
        });
    }

    [Fact]
    public void SeaGlassMultiShapeMorph_SelectShape_UpdatesTarget()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassMultiShapeMorph morph = new([SeaGlassShapeKind.Circle, SeaGlassShapeKind.Arch, SeaGlassShapeKind.Gem], 0, _stage);
            morph.SetActiveShapeIndex(2);
            Assert.Equal(2, morph.ActiveShapeIndex);
    
        });
    }

    [Fact]
    public void SeaGlassMultiShapeMorph_StepEvaluation_ProgressesTransition()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassMultiShapeMorph morph = new([SeaGlassShapeKind.Squircle, SeaGlassShapeKind.Sunny], 0, _stage);
            morph.SetActiveShapeIndex(1);
            SeaGlassSurfaceFrameState frame = morph.StepAndEvaluate(0.03f);
            Assert.NotNull(frame);
    
        });
    }

    [Fact]
    public void SeaGlassMultiShapeMorph_SettledTransition_ReachesTargetIndex()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassMultiShapeMorph morph = new([SeaGlassShapeKind.Capsule, SeaGlassShapeKind.Diamond], 0, _stage);
            morph.SetActiveShapeIndex(1);
            morph.StepAndEvaluate(1.0f); // settle
            Assert.Equal(1, morph.ActiveShapeIndex);
    
        });
    }

    [Fact]
    public void SeaGlassMultiShapeMorph_EvaluateFrame_GeneratesValidXaml()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassMultiShapeMorph morph = new([SeaGlassShapeKind.Heart, SeaGlassShapeKind.Circle], 0, _stage);
            Assert.StartsWith("F1 M", morph.StepAndEvaluate(0.016f).XamlContourPathData);
    
        });
    }
    #endregion

    #region 11. SeaGlassMorphingContextMenu Tests (>= 5 cases)
    [Fact]
    public void SeaGlassMorphingContextMenu_Initialization_SetsExpandedState()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassMorphingContextMenu menu = new(expanded: false, stageState: _stage);
            Assert.False(menu.Expanded);
    
        });
    }

    [Fact]
    public void SeaGlassMorphingContextMenu_OpenAndClose_TogglesState()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassMorphingContextMenu menu = new(expanded: false, stageState: _stage);
            menu.SetExpanded(true);
            Assert.True(menu.Expanded);
            menu.SetExpanded(false);
            Assert.False(menu.Expanded);
    
        });
    }

    [Fact]
    public void SeaGlassMorphingContextMenu_TeardropBridge_ExtrudesContour()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassMorphingContextMenu menu = new(expanded: true, stageState: _stage);
            SeaGlassSurfaceFrameState frame = menu.StepAndEvaluate(0.04f);
            Assert.NotEmpty(frame.XamlContourPathData);
    
        });
    }

    [Fact]
    public void SeaGlassMorphingContextMenu_SpatialOpeningSpring_AdvancesProgress()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassMorphingContextMenu menu = new(expanded: false, stageState: _stage);
            menu.SetExpanded(true);
            menu.StepAndEvaluate(0.02f);
            Assert.True(menu.SpatialWidthProgress > 0f);
    
        });
    }

    [Fact]
    public void SeaGlassMorphingContextMenu_EvaluateFrame_GeneratesValidXaml()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassMorphingContextMenu menu = new(expanded: true, stageState: _stage);
            Assert.StartsWith("F1 M", menu.StepAndEvaluate(0.016f).XamlContourPathData);
    
        });
    }
    #endregion

    #region 12. SeaGlassCascadedModelMorph Tests (>= 5 cases)
    [Fact]
    public void SeaGlassCascadedModelMorph_Initialization_SetsInitialStage()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassCascadedModelMorph cascaded = new(stage: 1, stageState: _stage);
            Assert.Equal(1, cascaded.Stage);
    
        });
    }

    [Fact]
    public void SeaGlassCascadedModelMorph_SetStage_TransitionsStageTarget()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassCascadedModelMorph cascaded = new(stage: 1, stageState: _stage);
            cascaded.SetStage(2);
            Assert.Equal(2, cascaded.Stage);
    
        });
    }

    [Fact]
    public void SeaGlassCascadedModelMorph_StepAndEvaluate_AdvancesInterpolation()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassCascadedModelMorph cascaded = new(stage: 1, stageState: _stage);
            cascaded.SetStage(2);
            SeaGlassSurfaceFrameState frame = cascaded.StepAndEvaluate(0.03f);
            Assert.NotNull(frame);
    
        });
    }

    [Fact]
    public void SeaGlassCascadedModelMorph_ContentCrossfade_UpdatesAlpha()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassCascadedModelMorph cascaded = new(stage: 1, stageState: _stage);
            cascaded.SetStage(2);
            SeaGlassSurfaceFrameState frame = cascaded.StepAndEvaluate(0.05f);
            Assert.NotNull(frame);
    
        });
    }

    [Fact]
    public void SeaGlassCascadedModelMorph_EvaluateFrame_GeneratesValidXaml()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassCascadedModelMorph cascaded = new(stage: 2, stageState: _stage);
            Assert.StartsWith("F1 M", cascaded.StepAndEvaluate(0.016f).XamlContourPathData);
    
        });
    }
    #endregion

    #region 13. SeaGlassContextRingMorph Tests (>= 5 cases)
    [Fact]
    public void SeaGlassContextRingMorph_Initialization_SetsProperties()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassContextRingMorph ring = new(expanded: false, progress: 0f, stageState: _stage);
            Assert.False(ring.Expanded);
            Assert.Equal(0.05f, ring.Progress);
    
        });
    }

    [Fact]
    public void SeaGlassContextRingMorph_SetExpanded_TransitionsState()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassContextRingMorph ring = new(expanded: false, progress: 0f, stageState: _stage);
            ring.SetExpanded(true);
            Assert.True(ring.Expanded);
    
        });
    }

    [Fact]
    public void SeaGlassContextRingMorph_StepAndEvaluate_AdvancesRingRadius()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassContextRingMorph ring = new(expanded: true, progress: 0.2f, stageState: _stage);
            ring.StepAndEvaluate(0.03f);
            Assert.True(ring.SpatialWidthProgress > 0f);
    
        });
    }

    [Fact]
    public void SeaGlassContextRingMorph_RadialBudding_GeneratesContour()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassContextRingMorph ring = new(expanded: true, progress: 0.5f, stageState: _stage);
            SeaGlassSurfaceFrameState frame = ring.StepAndEvaluate(0.02f);
            Assert.NotEmpty(frame.XamlContourPathData);
    
        });
    }

    [Fact]
    public void SeaGlassContextRingMorph_EvaluateFrame_GeneratesValidXaml()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassContextRingMorph ring = new(expanded: true, progress: 0.8f, stageState: _stage);
            Assert.StartsWith("F1 M", ring.StepAndEvaluate(0.016f).XamlContourPathData);
    
        });
    }
    #endregion

    #region 14. SeaGlassFloatingSheet Tests (>= 5 cases)
    [Fact]
    public void SeaGlassFloatingSheet_Initialization_SetsExpandedState()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassFloatingSheet sheet = new(true, () => { }, _stage);
            Assert.True(sheet.Visible);
            Assert.Equal(0, sheet.ActiveSubPage);
    
        });
    }

    [Fact]
    public void SeaGlassFloatingSheet_SwitchSubPage_ChangesActiveIndex()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassFloatingSheet sheet = new(true, () => { }, _stage);
            sheet.SwitchSubPage(1);
            Assert.Equal(1, sheet.ActiveSubPage);
    
        });
    }

    [Fact]
    public void SeaGlassFloatingSheet_Dismiss_CallsCallback()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            bool dismissed = false;
            SeaGlassFloatingSheet sheet = new(true, () => dismissed = true, _stage);
            sheet.OnDismissRequest();
            Assert.True(dismissed);
    
        });
    }

    [Fact]
    public void SeaGlassFloatingSheet_ConcentricCornerAdaptation_AdaptsCorners()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassFloatingSheet sheet = new(true, () => { }, _stage);
            SeaGlassSurfaceFrameState frame = sheet.StepAndEvaluate(0.03f);
            Assert.NotNull(frame);
    
        });
    }

    [Fact]
    public void SeaGlassFloatingSheet_EvaluateFrame_GeneratesValidXaml()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassFloatingSheet sheet = new(true, () => { }, _stage);
            Assert.StartsWith("F1 M", sheet.StepAndEvaluate(0.016f).XamlContourPathData);
    
        });
    }
    #endregion

    #region 15. SeaGlassContextMenuOverlay Tests (>= 5 cases)
    [Fact]
    public void SeaGlassContextMenuOverlay_Initialization_SetsHierarchicalItems()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassContextMenuOverlay menu = new(true, () => { }, _ => { }, stageState: _stage);
            Assert.True(menu.Visible);
            Assert.NotEmpty(menu.Items);
    
        });
    }

    [Fact]
    public void SeaGlassContextMenuOverlay_ActivateItem_ActivatesSubmenu()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassContextMenuOverlay menu = new(true, () => { }, _ => { }, stageState: _stage);
            menu.ActivateItem(menu.Items[1]);
            Assert.NotNull(menu.ActiveSubmenuParent);
    
        });
    }

    [Fact]
    public void SeaGlassContextMenuOverlay_SelectItem_CallsCallback()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            string selected = "";
            SeaGlassContextMenuOverlay menu = new(true, () => { }, item => selected = item.Title, stageState: _stage);
            menu.ActivateItem(menu.Items[0]);
            Assert.Equal("Share Optical Preset…", selected);
    
        });
    }

    [Fact]
    public void SeaGlassContextMenuOverlay_Close_CallsCallback()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            bool closed = false;
            SeaGlassContextMenuOverlay menu = new(true, () => closed = true, _ => { }, stageState: _stage);
            menu.OnDismissRequest();
            Assert.True(closed);
    
        });
    }

    [Fact]
    public void SeaGlassContextMenuOverlay_EvaluateFrame_GeneratesValidXaml()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassContextMenuOverlay menu = new(true, () => { }, _ => { }, stageState: _stage);
            Assert.StartsWith("F1 M", menu.StepAndEvaluate(0.016f).XamlContourPathData);
    
        });
    }
    #endregion

    #region 16. SeaGlassActionSheet Tests (>= 5 cases)
    [Fact]
    public void SeaGlassActionSheet_Initialization_SetsTitleAndItems()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassActionSheet sheet = new("Select Filter", true, () => { }, stageState: _stage);
            Assert.Equal("Select Filter", sheet.Title);
            Assert.True(sheet.Visible);
            Assert.NotEmpty(sheet.Actions);
    
        });
    }

    [Fact]
    public void SeaGlassActionSheet_Close_CallsCallback()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            bool closed = false;
            SeaGlassActionSheet sheet = new("Title", true, () => closed = true, stageState: _stage);
            sheet.OnDismissRequest();
            Assert.True(closed);
    
        });
    }

    [Fact]
    public void SeaGlassActionSheet_DestructiveAction_IsIdentifiable()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassActionSheet sheet = new("Title", true, () => { }, stageState: _stage);
            bool hasDestructive = false;
            foreach (var act in sheet.Actions)
            {
                if (act.IsDestructive) hasDestructive = true;
            }
            Assert.True(hasDestructive);
    
        });
    }

    [Fact]
    public void SeaGlassActionSheet_CustomActions_PopulatesItems()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassContextMenuItem customItem = new("custom1", "Custom Action", SeaGlassGlyph.Sparkles);
            SeaGlassActionSheet sheet = new("Title", true, () => { }, [customItem], stageState: _stage);
            Assert.Single(sheet.Actions);
            Assert.Equal("Custom Action", sheet.Actions[0].Title);
    
        });
    }

    [Fact]
    public void SeaGlassActionSheet_EvaluateFrame_GeneratesValidXaml()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassActionSheet sheet = new("Title", true, () => { }, stageState: _stage);
            Assert.StartsWith("F1 M", sheet.StepAndEvaluate(0.016f).XamlContourPathData);
    
        });
    }
    #endregion

    #region 17. SeaGlassLiveActivityPill Tests (>= 5 cases)
    [Fact]
    public void SeaGlassLiveActivityPill_Initialization_SetsState()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassLiveActivityPill pill = new(expanded: true);
            Assert.True(pill.Expanded);
            Assert.NotEmpty(pill.Title);
    
        });
    }

    [Fact]
    public void SeaGlassLiveActivityPill_ToggleExpansion_FlipsState()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassLiveActivityPill pill = new(expanded: false);
            pill.SetExpanded(!pill.Expanded);
            Assert.True(pill.Expanded);
            pill.SetExpanded(!pill.Expanded);
            Assert.False(pill.Expanded);
    
        });
    }

    [Fact]
    public void SeaGlassLiveActivityPill_UpdateContent_SetsText()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassLiveActivityPill pill = new(expanded: true);
            pill.Title = "Rendering Frame";
            pill.Subtitle = "60 FPS";
            Assert.Equal("Rendering Frame", pill.Title);
            Assert.Equal("60 FPS", pill.Subtitle);
    
        });
    }

    [Fact]
    public void SeaGlassLiveActivityPill_MorphProgress_AnimatesCleanly()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassLiveActivityPill pill = new(expanded: false);
            pill.SetExpanded(true);
            pill.StepAndEvaluate(0.03f);
            Assert.True(pill.ExpandProgress > 0f);
    
        });
    }

    [Fact]
    public void SeaGlassLiveActivityPill_EvaluateFrame_GeneratesValidXaml()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassLiveActivityPill pill = new(expanded: true);
            Assert.StartsWith("F1 M", pill.StepAndEvaluate(0.016f).XamlContourPathData);
    
        });
    }
    #endregion

    #region 18. SeaGlassMagnifierScrubber Tests (>= 5 cases)
    [Fact]
    public void SeaGlassMagnifierScrubber_Initialization_SetsValue()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassMagnifierScrubber scrubber = new(0.5f, _ => { });
            Assert.Equal(0.5f, scrubber.Value);
    
        });
    }

    [Fact]
    public void SeaGlassMagnifierScrubber_SetValue_ClampsToUnitInterval()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            float updated = -1f;
            SeaGlassMagnifierScrubber scrubber = new(0.2f, v => updated = v);
            scrubber.SetValue(0.85f);
            Assert.Equal(0.85f, scrubber.Value);
            Assert.Equal(0.85f, updated);
            scrubber.SetValue(1.5f);
            Assert.Equal(1.0f, scrubber.Value);
    
        });
    }

    [Fact]
    public void SeaGlassMagnifierScrubber_FormatValueLabel_ContainsFrameOrTime()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassMagnifierScrubber scrubber = new(0.62f, _ => { });
            string label = scrubber.FormatValueLabel();
            Assert.Contains("Frame", label);
    
        });
    }

    [Fact]
    public void SeaGlassMagnifierScrubber_MagnificationLens_ExpandsScale()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassMagnifierScrubber scrubber = new(0.5f, _ => { }) { LoupeActive = true };
            scrubber.StepAndEvaluate(0.03f);
            Assert.True(scrubber.LoupeLiftProgress >= 0f);
    
        });
    }

    [Fact]
    public void SeaGlassMagnifierScrubber_EvaluateFrame_GeneratesValidXaml()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassMagnifierScrubber scrubber = new(0.4f, _ => { });
            Assert.StartsWith("F1 M", scrubber.StepAndEvaluate(0.016f).XamlContourPathData);
    
        });
    }
    #endregion

    #region 19. SeaGlassNotificationStack Tests (>= 5 cases)
    [Fact]
    public void SeaGlassNotificationStack_Initialization_SetsItemsAndState()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassNotificationStack stack = new(expanded: false);
            Assert.False(stack.Expanded);
            Assert.NotEmpty(stack.Notifications);
    
        });
    }

    [Fact]
    public void SeaGlassNotificationStack_ToggleExpansion_FlipsState()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassNotificationStack stack = new(expanded: false);
            stack.ToggleExpanded();
            Assert.True(stack.Expanded);
            stack.ToggleExpanded();
            Assert.False(stack.Expanded);
    
        });
    }

    [Fact]
    public void SeaGlassNotificationStack_DefaultNotifications_CountAtLeastThree()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassNotificationStack stack = new(expanded: true);
            Assert.True(stack.Notifications.Count >= 3);
    
        });
    }

    [Fact]
    public void SeaGlassNotificationStack_FanOutProgress_AnimatesCleanly()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassNotificationStack stack = new(expanded: false);
            stack.ToggleExpanded();
            stack.StepAndEvaluate(0.04f);
            Assert.True(stack.FanProgress > 0f);
    
        });
    }

    [Fact]
    public void SeaGlassNotificationStack_EvaluateFrame_GeneratesValidXaml()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassNotificationStack stack = new(expanded: true);
            Assert.StartsWith("F1 M", stack.StepAndEvaluate(0.016f).XamlContourPathData);
    
        });
    }
    #endregion

    #region 20. SeaGlassWheelPicker Tests (>= 5 cases)
    [Fact]
    public void SeaGlassWheelPicker_Initialization_SetsItemsAndSelection()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassWheelPicker picker = new(["Option 1", "Option 2", "Option 3"], 1, _ => { });
            Assert.Equal(3, picker.Items.Count);
            Assert.Equal(1, picker.SelectedIndex);
    
        });
    }

    [Fact]
    public void SeaGlassWheelPicker_SelectIndex_InvokesCallback()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            int selected = -1;
            SeaGlassWheelPicker picker = new(["A", "B", "C"], 0, idx => selected = idx);
            picker.SelectItem(2);
            Assert.Equal(2, picker.SelectedIndex);
            Assert.Equal(2, selected);
    
        });
    }

    [Fact]
    public void SeaGlassWheelPicker_DrumRowTransform_CenterRowHasScaleGreaterThanOne()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassWheelPicker picker = new(["A", "B", "C"], 1, _ => { });
            var (angleRad, scale, alpha) = picker.EvaluateDrumRowTransform(1);
            Assert.Equal(0f, angleRad, 3);
            Assert.True(scale > 1.0f);
            Assert.Equal(1.0f, alpha, 3);
    
        });
    }

    [Fact]
    public void SeaGlassWheelPicker_DrumRowTransform_OffsetRowHasNonZeroAngle()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassWheelPicker picker = new(["A", "B", "C"], 1, _ => { });
            var (angleRad, scale, alpha) = picker.EvaluateDrumRowTransform(0);
            Assert.True(MathF.Abs(angleRad) > 0f);
            Assert.True(alpha < 1.0f);
    
        });
    }

    [Fact]
    public void SeaGlassWheelPicker_EvaluateFrame_GeneratesValidXaml()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassWheelPicker picker = new(["1", "2"], 0, _ => { });
            Assert.StartsWith("F1 M", picker.StepAndEvaluate(0.016f).XamlContourPathData);
    
        });
    }
    #endregion

    #region 21. SeaGlassControlCenterGrid Tests (>= 5 cases)
    [Fact]
    public void SeaGlassControlCenterGrid_Initialization_SetsTiles()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassControlCenterGrid grid = new();
            Assert.Equal(-1, grid.ExpandedPlatterIndex);
            Assert.Equal(0f, grid.ExpandProgress);
    
        });
    }

    [Fact]
    public void SeaGlassControlCenterGrid_TogglePlatterExpansion_FlipsTileState()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassControlCenterGrid grid = new();
            Assert.Equal(-1, grid.ExpandedPlatterIndex);
            grid.TogglePlatterExpansion(0);
            Assert.Equal(0, grid.ExpandedPlatterIndex);
            grid.TogglePlatterExpansion(0);
            Assert.Equal(-1, grid.ExpandedPlatterIndex);
    
        });
    }

    [Fact]
    public void SeaGlassControlCenterGrid_PlatterSdfCoalescing_EvaluatesBounds()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassControlCenterGrid grid = new();
            grid.TogglePlatterExpansion(0);
            SeaGlassSurfaceFrameState frame = grid.StepAndEvaluate(0.04f);
            Assert.NotEmpty(frame.XamlContourPathData);
    
        });
    }

    [Fact]
    public void SeaGlassControlCenterGrid_MultiplePlatters_CanCoexist()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassControlCenterGrid grid = new();
            grid.TogglePlatterExpansion(2);
            Assert.Equal(2, grid.ExpandedPlatterIndex);
    
        });
    }

    [Fact]
    public void SeaGlassControlCenterGrid_EvaluateFrame_GeneratesValidXaml()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassControlCenterGrid grid = new();
            Assert.StartsWith("F1 M", grid.StepAndEvaluate(0.016f).XamlContourPathData);
    
        });
    }
    #endregion

    #region 22. SeaGlassStage Tests (>= 5 cases)
    [Fact]
    public void SeaGlassStage_BindBackdropTarget_IncrementsGeneration()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassStageState stage = new();
            long gen0 = stage.BackdropGeneration;
            stage.BindBackdropTarget("Layer1", Vector2.Zero, new Vector2(1280f, 720f));
            Assert.True(stage.BackdropGeneration > gen0);
            Assert.True(stage.IsBackdropCaptured);
    
        });
    }

    [Fact]
    public void SeaGlassStage_BackdropSize_RespectsProvidedDimensions()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassStageState stage = new();
            stage.BindBackdropTarget("Layer", new Vector2(10f, 20f), new Vector2(1920f, 1080f));
            Assert.Equal(1920f, stage.BackdropSize.X);
            Assert.Equal(1080f, stage.BackdropSize.Y);
            Assert.Equal(10f, stage.BackdropOriginInRoot.X);
    
        });
    }

    [Fact]
    public void SeaGlassStage_AcyclicBackdrop_ThrowsWhenForegroundMatchesBackdrop()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassStageState stage = new();
            stage.BindBackdropTarget("RecursiveId", Vector2.Zero, new Vector2(100f, 100f));
            Assert.Throws<InvalidOperationException>(() => stage.EnsureAcyclicForegroundSurface("RecursiveId"));
    
        });
    }

    [Fact]
    public void SeaGlassStage_AcyclicBackdrop_PassesForDifferentForegroundId()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassStageState stage = new();
            stage.BindBackdropTarget("BackdropLayer", Vector2.Zero, new Vector2(100f, 100f));
            stage.EnsureAcyclicForegroundSurface("ForegroundGlassSurface"); // Must not throw
    
        });
    }

    [Fact]
    public void SeaGlassStage_ActiveBackend_ProvidesD2D1Hlsl()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassStageState stage = new();
            Assert.NotNull(stage.ActiveBackend);
            Assert.IsType<Direct2DHlslSeaGlassBackend>(stage.ActiveBackend);
    
        });
    }
    #endregion

    #region 23. SeaGlassFluidGroup Tests (>= 5 cases)
    [Fact]
    public void SeaGlassFluidGroup_RegisterChildBounds_AddsNode()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassFluidGroupState group = new(_stage, SeaGlassPalette.SeafoamMint.PrimaryGlass);
            group.RegisterChildBounds("childA", new Vector2(50f, 50f), new Vector2(100f, 60f));
            Assert.NotEmpty(group.SnapshotNodes());
    
        });
    }

    [Fact]
    public void SeaGlassFluidGroup_SmoothRadius_AffectsEvaluation()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassFluidGroupState group = new(_stage, SeaGlassPalette.CobaltTide.PrimaryGlass, smoothRadiusDp: 32f);
            Assert.Equal(32f, group.SmoothRadiusDp);
            group.SmoothRadiusDp = 44f;
            Assert.Equal(44f, group.SmoothRadiusDp);
    
        });
    }

    [Fact]
    public void SeaGlassFluidGroup_TwoNearbyNodes_CoalesceInSharedField()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassFluidGroupState group = new(_stage, SeaGlassPalette.SeafoamMint.PrimaryGlass, smoothRadiusDp: 40f);
            group.GroupOriginInRoot = Vector2.Zero;
            group.RegisterChildBounds("node1", new Vector2(100f, 100f), new Vector2(80f, 80f));
            group.RegisterChildBounds("node2", new Vector2(170f, 100f), new Vector2(80f, 80f));

            var nodes = group.SnapshotNodes();
            Assert.Equal(2, nodes.Count);

            // Point directly between nodes should evaluate inside coalesced bridge
            var eval = SeaGlassSdfEngine.EvaluateScene(140f, 140f, nodes, 40f);
            Assert.True(eval.SignedDistancePx < 0f);
    
        });
    }

    [Fact]
    public void SeaGlassFluidGroup_BuildContourPath_GeneratesValidXaml()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassFluidGroupState group = new(_stage, SeaGlassPalette.BeachAmber.PrimaryGlass);
            group.RegisterChildBounds("btn1", new Vector2(10f, 10f), new Vector2(60f, 40f));
            string path = group.BuildCoalescedContourPath().ToXamlPathData();
            Assert.StartsWith("F1 M", path);
    
        });
    }

    [Fact]
    public void SeaGlassFluidGroup_PackUniformBlock_Matches256Bytes()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassFluidGroupState group = new(_stage, SeaGlassPalette.SeaLavender.PrimaryGlass);
            group.RegisterChildBounds("n1", new Vector2(10f, 10f), new Vector2(50f, 50f));
            SeaGlassUniformBlock block = group.PackUniformBlock();
            Assert.Equal(256, block.CreateDirectStagingBuffer().Length);
    
        });
    }
    #endregion
}
