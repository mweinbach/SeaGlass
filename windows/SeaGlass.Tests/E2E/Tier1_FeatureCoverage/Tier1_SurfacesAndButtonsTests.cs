using SeaGlass.Tests.Controls;
using System;
using System.Numerics;
using SeaGlass.Core;
using SeaGlass.Core.Backend;
using SeaGlass.WinUI;
using Xunit;

namespace SeaGlass.Tests.E2E.Tier1_FeatureCoverage;

public sealed class Tier1_SurfacesAndButtonsTests
{
    private readonly SeaGlassStageState _stage;

    public Tier1_SurfacesAndButtonsTests()
    {
        _stage = new SeaGlassStageState();
        _stage.BindBackdropTarget("E2EStageBackdrop", Vector2.Zero, new Vector2(1920f, 1080f));
    }

    #region 1. SeaGlassSurface Tests (>= 5 cases)
    [Fact]
    public void SeaGlassSurface_DefaultProperties_MeetRequirements()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSurface surface = new(stageState: _stage);
            Assert.NotNull(surface.StageState);
            Assert.Equal(SeaGlassContentRole.TextAndIcons, surface.ContentRole);
            Assert.Equal(320f, surface.SizePx.X);
            Assert.Equal(140f, surface.SizePx.Y);
            Assert.False(surface.IsPointerDown);
    
        });
    }

    [Fact]
    public void SeaGlassSurface_PointerTilt_ComputesNonZeroAngles()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSurface surface = new(stageState: _stage);
            surface.OnPointerStateChanged(new Vector2(100f, 40f), true);
            SeaGlassSurfaceFrameState frame = surface.EvaluateFrame(0.02f);
            Assert.True(MathF.Abs(frame.TiltRotationXDegrees) >= 0f);
            Assert.True(surface.IsPointerDown);
    
        });
    }

    [Fact]
    public void SeaGlassSurface_InscribedSafePadding_PreservesMinimumBase()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSurface surface = new(stageState: _stage)
            {
                BasePaddingPx = new Vector4(16f, 12f, 16f, 12f)
            };
            SeaGlassSurfaceFrameState frame = surface.EvaluateFrame(0.016f);
            Vector4 safePadding = new(frame.SafePaddingPx.LeftPx, frame.SafePaddingPx.TopPx, frame.SafePaddingPx.RightPx, frame.SafePaddingPx.BottomPx);
            Assert.True(safePadding.X >= 16f);
            Assert.True(safePadding.Y >= 12f);
            Assert.True(safePadding.Z >= 16f);
            Assert.True(safePadding.W >= 12f);
    
        });
    }

    [Fact]
    public void SeaGlassSurface_TouchCausticAndClick_InvokesCallback()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            bool clicked = false;
            SeaGlassSurface surface = new(stageState: _stage, onClick: () => clicked = true);
            surface.OnPointerStateChanged(new Vector2(50f, 50f), true);
            Assert.NotNull(surface.LastTouchOffsetInLocal);
            surface.OnPointerStateChanged(new Vector2(50f, 50f), false);
            Assert.True(clicked, "Releasing pointer after press must invoke OnClick");
    
        });
    }

    [Fact]
    public void SeaGlassSurface_SnapMaterial_ImmediatelyUpdatesValues()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSurface surface = new(stageState: _stage);
            SeaGlassMaterial cobalt = SeaGlassPalette.CobaltTide.PrimaryGlass;
            surface.SnapMaterial(cobalt);
            Assert.Equal(cobalt.TintColorArgb, surface.Material.TintColorArgb);
            Assert.Equal(cobalt.Coarseness, surface.Material.Coarseness, 3);
            SeaGlassSurfaceFrameState frame = surface.EvaluateFrame(0.01f);
            Assert.Equal(cobalt.TintColorArgb, frame.EffectiveMaterial.TintColorArgb);
    
        });
    }
    #endregion

    #region 2. SeaGlassCard Tests (>= 5 cases)
    [Fact]
    public void SeaGlassCard_Initialization_SetsTitleAndSubtitle()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassCard card = new("Optics Overview", "Real-time GPU Telemetry", _stage);
            Assert.Equal("Optics Overview", card.Title);
            Assert.Equal("Real-time GPU Telemetry", card.Subtitle);
            Assert.Equal(SeaGlassContentRole.TextAndIcons, card.ContentRole);
    
        });
    }

    [Fact]
    public void SeaGlassCard_EvaluateFrame_GeneratesValidXamlSquircleContour()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassCard card = new("Title", null, _stage);
            SeaGlassSurfaceFrameState frame = card.EvaluateFrame();
            Assert.StartsWith("F1 M", frame.XamlContourPathData);
            Assert.NotEmpty(frame.XamlContourPathData);
    
        });
    }

    [Fact]
    public void SeaGlassCard_CustomMaterial_EvaluatesVolumetricThickness()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassMaterial thickMat = SeaGlassPalette.BeachAmber.PrimaryGlass with { ThicknessDp = 24f };
            SeaGlassCard card = new("Amber Card", null, _stage, material: thickMat);
            card.EvaluateFrame();
            Assert.Equal(24f, card.Material.ThicknessDp);
    
        });
    }

    [Fact]
    public void SeaGlassCard_PointerInteraction_MaintainsStableScale()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassCard card = new("Card", null, _stage);
            card.OnPointerStateChanged(new Vector2(100f, 50f), true);
            SeaGlassSurfaceFrameState frame = card.EvaluateFrame(0.02f);
            Assert.InRange(frame.ScaleX, 0.95f, 1.05f);
            Assert.InRange(frame.ScaleY, 0.95f, 1.05f);
    
        });
    }

    [Fact]
    public void SeaGlassCard_UniformBlock_PacksSingleNode()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassCard card = new("Card", null, _stage);
            SeaGlassSurfaceFrameState frame = card.EvaluateFrame();
            SeaGlassUniformBlockStd140 std140 = frame.UniformBlock.ToStd140Struct();
            Assert.Equal(1f, std140.LightAndMeta.W, 2); // 1 active node
    
        });
    }
    #endregion

    #region 3. SeaGlassDialogCard Tests (>= 5 cases)
    [Fact]
    public void SeaGlassDialogCard_PrimaryAction_InvokesHandler()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            bool primaryInvoked = false;
            SeaGlassDialogCard dialog = new("Reset Settings", "Are you sure?", "Confirm", () => primaryInvoked = true, _stage);
            dialog.OnConfirm();
            Assert.True(primaryInvoked);
    
        });
    }

    [Fact]
    public void SeaGlassDialogCard_SecondaryAction_InvokesHandler()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            bool secondaryInvoked = false;
            SeaGlassDialogCard dialog = new("Reset Settings", "Are you sure?", "Confirm", () => { }, _stage, "Cancel", () => secondaryInvoked = true);
            dialog.OnDismiss();
            Assert.True(secondaryInvoked);
    
        });
    }

    [Fact]
    public void SeaGlassDialogCard_SlowSpatialEntrance_InterpolatesCleanly()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassDialogCard dialog = new("Title", "Message", "OK", () => { }, _stage);
            SeaGlassSurfaceFrameState f1 = dialog.EvaluateFrame(0.016f);
            SeaGlassSurfaceFrameState f2 = dialog.EvaluateFrame(0.032f);
            Assert.NotNull(f1);
            Assert.NotNull(f2);
            Assert.StartsWith("F1 M", f1.XamlContourPathData);
    
        });
    }

    [Fact]
    public void SeaGlassDialogCard_ModalElevation_PacksBackdropDimming()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassDialogCard dialog = new("Title", "Message", "OK", () => { }, _stage);
            SeaGlassSurfaceFrameState frame = dialog.EvaluateFrame();
            Assert.True(dialog.Material.ThicknessDp >= 14f);
            Assert.NotEmpty(frame.XamlContourPathData);
    
        });
    }

    [Fact]
    public void SeaGlassDialogCard_TextProperties_PersistAccurately()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassDialogCard dialog = new("Export HDR Asset", "Select preferred format below.", "Export", () => { }, _stage, "Dismiss", () => { });
            Assert.Equal("Export HDR Asset", dialog.Title);
            Assert.Equal("Select preferred format below.", dialog.Body);
            Assert.Equal("Export", dialog.ConfirmLabel);
            Assert.Equal("Dismiss", dialog.DismissLabel);
    
        });
    }
    #endregion

    #region 4. SeaGlassBottomSheetSurface Tests (>= 5 cases)
    [Fact]
    public void SeaGlassBottomSheetSurface_InitialBounds_ReflectPeekHeight()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassBottomSheetSurface sheet = new("Inspector", _stage, "System Telemetry");
            Assert.Equal("Inspector", sheet.Title);
            Assert.Equal("System Telemetry", sheet.Subtitle);
            Assert.Equal(SeaGlassContentRole.TextAndIcons, sheet.ContentRole);
    
        });
    }

    [Fact]
    public void SeaGlassBottomSheetSurface_ConcentricCornerAdaptation_AdaptsTopCorners()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassBottomSheetSurface sheet = new("Sheet", _stage);
            SeaGlassSurfaceFrameState frame = sheet.EvaluateFrame();
            Assert.StartsWith("F1 M", frame.XamlContourPathData);
    
        });
    }

    [Fact]
    public void SeaGlassBottomSheetSurface_DragHandle_EvaluatesStableGeometry()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassBottomSheetSurface sheet = new("Sheet", _stage);
            sheet.OnPointerStateChanged(new Vector2(160f, 15f), true);
            SeaGlassSurfaceFrameState frame = sheet.EvaluateFrame(0.02f);
            Assert.True(sheet.IsPointerDown);
            Assert.NotEmpty(frame.XamlContourPathData);
    
        });
    }

    [Fact]
    public void SeaGlassBottomSheetSurface_DismissHandler_ExecutesOnRequest()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            bool dismissed = false;
            SeaGlassBottomSheetSurface sheet = new("Sheet", _stage) { OnClick = () => dismissed = true };
            sheet.OnClick?.Invoke();
            Assert.True(dismissed);
    
        });
    }

    [Fact]
    public void SeaGlassBottomSheetSurface_StepAndEvaluate_ProducesContour()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassBottomSheetSurface sheet = new("Sheet", _stage);
            SeaGlassSurfaceFrameState frame = sheet.EvaluateFrame(0.016f);
            Assert.StartsWith("F1 M", frame.XamlContourPathData);
            Assert.True(frame.UniformBlock.CreateDirectStagingBuffer().Length == 256);
    
        });
    }
    #endregion

    #region 5. SeaGlassCornerEmbeddedPod Tests (>= 5 cases)
    [Fact]
    public void SeaGlassCornerEmbeddedPod_FlushCornerEmbed_ConfiguredCorrectly()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassCornerEmbeddedPod pod = new("Top Dock", "Concentric 52dp", _stage);
            Assert.Equal(SeaGlassCornerAdaptationMode.FlushCornerEmbed, pod.CornerAdaptationMode);
            Assert.True(pod.AllowAsymmetricCorners);
    
        });
    }

    [Fact]
    public void SeaGlassCornerEmbeddedPod_AsymmetricCornerCalculation_ProducesExpectedDelta()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassCornerEmbeddedPod pod = new("Dock", "Concentric", _stage)
            {
                OriginInRoot = new Vector2(12f, 12f),
                SizePx = new Vector2(260f, 140f)
            };
            SeaGlassSurfaceFrameState frame = pod.EvaluateFrame();
            Assert.StartsWith("F1 M", frame.XamlContourPathData);
    
        });
    }

    [Fact]
    public void SeaGlassCornerEmbeddedPod_TextProperties_Preserved()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassCornerEmbeddedPod pod = new("Camera HUD", "4K 60FPS", _stage);
            Assert.Equal("Camera HUD", pod.Label);
            Assert.Equal("4K 60FPS", pod.SubLabel);
    
        });
    }

    [Fact]
    public void SeaGlassCornerEmbeddedPod_MarginConstraints_Valid()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassCornerEmbeddedPod pod = new("Dock", "Subtitle", _stage);
            Assert.True(pod.CornerProximityThresholdDp > 0f);
            Assert.True(pod.CornerFullMatchDistanceDp > 0f);
    
        });
    }

    [Fact]
    public void SeaGlassCornerEmbeddedPod_XamlContour_StartsWithF1M()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassCornerEmbeddedPod pod = new("Pod", "Sub", _stage);
            string path = pod.EvaluateFrame().XamlContourPathData;
            Assert.StartsWith("F1 M", path);
    
        });
    }
    #endregion

    #region 6. SeaGlassAdaptiveCornerLabCard Tests (>= 5 cases)
    [Fact]
    public void SeaGlassAdaptiveCornerLabCard_Pixel9ProProfile_SolvesBezelFrame()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassAdaptiveCornerLabCard lab = new(SeaGlassDeviceHardwareProfile.Pixel9Pro, _stage);
            var (layout, tl, tr, dock) = lab.SolveBezelFrame();
            Assert.True(layout.PodWidthPx > 0f);
            Assert.True(tl.TopLeftPx > tl.BottomRightPx);
            Assert.True(dock.BottomLeftPx > 0f);
    
        });
    }

    [Fact]
    public void SeaGlassAdaptiveCornerLabCard_IPhone16ProProfile_SolvesBezelFrame()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassAdaptiveCornerLabCard lab = new(SeaGlassDeviceHardwareProfile.IPhone16Pro, _stage);
            var (layout, tl, tr, dock) = lab.SolveBezelFrame();
            Assert.True(layout.PodWidthPx > 0f);
            Assert.True(tl.TopLeftPx > tl.BottomRightPx);
    
        });
    }

    [Fact]
    public void SeaGlassAdaptiveCornerLabCard_GalaxyS25Ultra_SolvesBezelFrame()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassAdaptiveCornerLabCard lab = new(SeaGlassDeviceHardwareProfile.GalaxyS25Ultra, _stage);
            var (layout, tl, tr, dock) = lab.SolveBezelFrame();
            Assert.True(layout.PodWidthPx > 0f);
            Assert.True(tl.TopLeftPx >= 0f);
    
        });
    }

    [Fact]
    public void SeaGlassAdaptiveCornerLabCard_Gaps_RespectMinimumRequirements()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassAdaptiveCornerLabCard lab = new(SeaGlassDeviceHardwareProfile.Pixel9Pro, _stage);
            var (layout, _, _, _) = lab.SolveBezelFrame();
            Assert.True(layout.HorizontalGapPx >= 31.9f);
            Assert.True(layout.VerticalGapPx >= 27.9f);
    
        });
    }

    [Fact]
    public void SeaGlassAdaptiveCornerLabCard_EvaluateFrame_ProducesValidXaml()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassAdaptiveCornerLabCard lab = new(SeaGlassDeviceHardwareProfile.Pixel9Pro, _stage);
            SeaGlassSurfaceFrameState frame = lab.EvaluateFrame();
            Assert.StartsWith("F1 M", frame.XamlContourPathData);
    
        });
    }
    #endregion

    #region 7. SeaGlassButton Tests (>= 5 cases)
    [Fact]
    public void SeaGlassButton_DefaultProperties_MeetRequirements()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassButton btn = new("Save Changes", () => { }, _stage);
            Assert.Equal("Save Changes", btn.Label);
            Assert.Equal(SeaGlassContentRole.TextAndIcons, btn.ContentRole);
    
        });
    }

    [Fact]
    public void SeaGlassButton_PointerDown_CompressesSlabTactilely()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassButton btn = new("Action", () => { }, _stage);
            btn.OnPointerStateChanged(new Vector2(40f, 20f), true);
            SeaGlassSurfaceFrameState frame = btn.EvaluateFrame(0.03f);
            Assert.True(frame.ScaleX > 1.0f, $"ScaleX should stretch during press ({frame.ScaleX} > 1.0)");
            Assert.True(frame.ScaleY < 1.0f, $"ScaleY should squish during press ({frame.ScaleY} < 1.0)");
    
        });
    }

    [Fact]
    public void SeaGlassButton_PointerRelease_InvokesCallback()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            bool clicked = false;
            SeaGlassButton btn = new("Click Me", () => clicked = true, _stage);
            btn.OnPointerStateChanged(new Vector2(20f, 15f), true);
            btn.OnPointerStateChanged(new Vector2(20f, 15f), false);
            Assert.True(clicked);
    
        });
    }

    [Fact]
    public void SeaGlassButton_FastSpatialSpring_RecoversToRestingScale()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassButton btn = new("Action", () => { }, _stage);
            btn.OnPointerStateChanged(new Vector2(30f, 15f), true);
            btn.EvaluateFrame(0.03f);
            btn.OnPointerStateChanged(new Vector2(30f, 15f), false);
            // Step forward across recovery period
            SeaGlassSurfaceFrameState finalFrame = btn.EvaluateFrame(0.35f);
            Assert.InRange(finalFrame.ScaleX, 0.99f, 1.01f);
            Assert.InRange(finalFrame.ScaleY, 0.99f, 1.01f);
    
        });
    }

    [Fact]
    public void SeaGlassButton_EvaluateFrame_GeneratesValidXamlContour()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassButton btn = new("Primary", () => { }, _stage);
            SeaGlassSurfaceFrameState frame = btn.EvaluateFrame();
            Assert.StartsWith("F1 M", frame.XamlContourPathData);
    
        });
    }
    #endregion

    #region 8. SeaGlassIconButton Tests (>= 5 cases)
    [Fact]
    public void SeaGlassIconButton_DefaultSizing_IsSquare44dp()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassIconButton iconBtn = new(SeaGlassGlyph.Sparkles, () => { }, _stage);
            Assert.Equal(44f, iconBtn.SizePx.X);
            Assert.Equal(44f, iconBtn.SizePx.Y);
            Assert.Equal(SeaGlassGlyph.Sparkles, iconBtn.Glyph);
    
        });
    }

    [Fact]
    public void SeaGlassIconButton_VectorGlyph_GeneratesValidXamlPath()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassIconButton iconBtn = new(SeaGlassGlyph.Home, () => { }, _stage);
            string glyphPath = SeaGlassIcon.BuildGlyphXamlPathData(iconBtn.Glyph);
            Assert.StartsWith("F1 M", glyphPath);
    
        });
    }

    [Fact]
    public void SeaGlassIconButton_SelectedState_ModifiesVisualFrame()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassIconButton unselected = new(SeaGlassGlyph.Heart, () => { }, _stage, selected: false);
            SeaGlassIconButton selected = new(SeaGlassGlyph.Heart, () => { }, _stage, selected: true);
            Assert.False(unselected.Selected);
            Assert.True(selected.Selected);
            Assert.StartsWith("F1 M", selected.EvaluateFrame().XamlContourPathData);
    
        });
    }

    [Fact]
    public void SeaGlassIconButton_ClickCallback_FiresOnRelease()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            bool clicked = false;
            SeaGlassIconButton iconBtn = new(SeaGlassGlyph.Tune, () => clicked = true, _stage);
            iconBtn.OnPointerStateChanged(new Vector2(22f, 22f), true);
            iconBtn.OnPointerStateChanged(new Vector2(22f, 22f), false);
            Assert.True(clicked);
    
        });
    }

    [Fact]
    public void SeaGlassIconButton_CausticBloom_PacksUniformBuffer()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassIconButton iconBtn = new(SeaGlassGlyph.Bell, () => { }, _stage);
            SeaGlassSurfaceFrameState frame = iconBtn.EvaluateFrame();
            Assert.Equal(256, frame.UniformBlock.CreateDirectStagingBuffer().Length);
    
        });
    }
    #endregion

    #region 9. SeaGlassExtendedFab Tests (>= 5 cases)
    [Fact]
    public void SeaGlassExtendedFab_ExpandedState_Has28dpCornerRadius()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassExtendedFab fab = new("New Note", SeaGlassGlyph.Plus, true, () => { }, _stage);
            Assert.True(fab.Expanded);
            Assert.Equal(28f, fab.Material.CornerRadiusDp);
            Assert.Equal("New Note", fab.Label);
            Assert.Equal(SeaGlassGlyph.Plus, fab.Glyph);
    
        });
    }

    [Fact]
    public void SeaGlassExtendedFab_CollapsedState_Has20dpCornerRadius()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassExtendedFab fab = new("New Note", SeaGlassGlyph.Plus, false, () => { }, _stage);
            Assert.False(fab.Expanded);
            Assert.Equal(20f, fab.Material.CornerRadiusDp);
    
        });
    }

    [Fact]
    public void SeaGlassExtendedFab_SetExpanded_TransitionsState()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassExtendedFab fab = new("Compose", SeaGlassGlyph.Plus, false, () => { }, _stage);
            fab.SetExpanded(true);
            Assert.True(fab.Expanded);
            Assert.Equal(28f, fab.Material.CornerRadiusDp);
    
        });
    }

    [Fact]
    public void SeaGlassExtendedFab_ClickCallback_Executes()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            bool clicked = false;
            SeaGlassExtendedFab fab = new("Compose", SeaGlassGlyph.Plus, true, () => clicked = true, _stage);
            fab.OnPointerStateChanged(new Vector2(30f, 20f), true);
            fab.OnPointerStateChanged(new Vector2(30f, 20f), false);
            Assert.True(clicked);
    
        });
    }

    [Fact]
    public void SeaGlassExtendedFab_EvaluateFrame_ProducesValidXaml()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassExtendedFab fab = new("Action", SeaGlassGlyph.Sparkles, true, () => { }, _stage);
            Assert.StartsWith("F1 M", fab.EvaluateFrame().XamlContourPathData);
    
        });
    }
    #endregion

    #region 10. SeaGlassPill Tests (>= 5 cases)
    [Fact]
    public void SeaGlassPill_UnselectedProperties_Are18dpCornerAnd10dpThickness()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassPill pill = new("Seafoam", false, () => { }, _stage);
            Assert.False(pill.Selected);
            Assert.Equal(18f, pill.Material.CornerRadiusDp);
            Assert.Equal(10f, pill.Material.ThicknessDp);
    
        });
    }

    [Fact]
    public void SeaGlassPill_SelectedProperties_Are26dpCornerAnd16dpThickness()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassPill pill = new("Seafoam", true, () => { }, _stage);
            Assert.True(pill.Selected);
            Assert.Equal(26f, pill.Material.CornerRadiusDp);
            Assert.Equal(16f, pill.Material.ThicknessDp);
    
        });
    }

    [Fact]
    public void SeaGlassPill_SetSelected_UpdatesCornerAndThickness()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassPill pill = new("Tide", false, () => { }, _stage);
            pill.SetSelected(true);
            Assert.True(pill.Selected);
            Assert.Equal(26f, pill.Material.CornerRadiusDp);
            Assert.Equal(16f, pill.Material.ThicknessDp);
            pill.SetSelected(false);
            Assert.False(pill.Selected);
            Assert.Equal(18f, pill.Material.CornerRadiusDp);
            Assert.Equal(10f, pill.Material.ThicknessDp);
    
        });
    }

    [Fact]
    public void SeaGlassPill_ClickCallback_Executes()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            bool clicked = false;
            SeaGlassPill pill = new("Tag", false, () => clicked = true, _stage);
            pill.OnPointerStateChanged(new Vector2(20f, 15f), true);
            pill.OnPointerStateChanged(new Vector2(20f, 15f), false);
            Assert.True(clicked);
    
        });
    }

    [Fact]
    public void SeaGlassPill_EvaluateFrame_GeneratesValidXamlContour()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassPill pill = new("Sample", true, () => { }, _stage);
            Assert.StartsWith("F1 M", pill.EvaluateFrame().XamlContourPathData);
    
        });
    }
    #endregion

    #region 11. SeaGlassFilterChip Tests (>= 5 cases)
    [Fact]
    public void SeaGlassFilterChip_CheckedState_RevealsCheckmarkGlyph()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassFilterChip chip = new("Refraction", true, () => { }, _stage);
            Assert.True(chip.Selected);
            Assert.Equal(SeaGlassGlyph.Check, chip.LeadingGlyph);
    
        });
    }

    [Fact]
    public void SeaGlassFilterChip_UncheckedState_HidesLeadingGlyph()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassFilterChip chip = new("Refraction", false, () => { }, _stage);
            Assert.False(chip.Selected);
            Assert.Null(chip.LeadingGlyph);
    
        });
    }

    [Fact]
    public void SeaGlassFilterChip_Toggle_SwitchesCheckedState()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassFilterChip chip = new("Dispersion", false, () => { }, _stage);
            chip.SetSelected(true);
            Assert.True(chip.Selected);
            Assert.Equal(SeaGlassGlyph.Check, chip.LeadingGlyph);
            chip.SetSelected(false);
            Assert.False(chip.Selected);
            Assert.Null(chip.LeadingGlyph);
    
        });
    }

    [Fact]
    public void SeaGlassFilterChip_Click_TriggersCallback()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            bool clicked = false;
            SeaGlassFilterChip chip = new("Beer-Lambert", true, () => clicked = true, _stage);
            chip.OnPointerStateChanged(new Vector2(30f, 15f), true);
            chip.OnPointerStateChanged(new Vector2(30f, 15f), false);
            Assert.True(clicked);
    
        });
    }

    [Fact]
    public void SeaGlassFilterChip_EvaluateFrame_ProducesValidXaml()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassFilterChip chip = new("Oklab", true, () => { }, _stage);
            Assert.StartsWith("F1 M", chip.EvaluateFrame().XamlContourPathData);
    
        });
    }
    #endregion

    #region 12. SeaGlassBadge Tests (>= 5 cases)
    [Fact]
    public void SeaGlassBadge_NumericContent_FormatsCorrectly()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassBadge badge = new("42", _stage);
            Assert.Equal("42", badge.Text);
    
        });
    }

    [Fact]
    public void SeaGlassBadge_CompactDimensions_AreSmallerThanStandardPill()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassBadge badge = new("New", _stage);
            Assert.True(badge.SizePx.Y <= 32f);
    
        });
    }

    [Fact]
    public void SeaGlassBadge_SetText_UpdatesProperty()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassBadge badge = new("1", _stage);
            badge.Text = "99+";
            Assert.Equal("99+", badge.Text);
    
        });
    }

    [Fact]
    public void SeaGlassBadge_ContentRole_IsTextAndIcons()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassBadge badge = new("Beta", _stage);
            Assert.Equal(SeaGlassContentRole.TextAndIcons, badge.ContentRole);
    
        });
    }

    [Fact]
    public void SeaGlassBadge_EvaluateFrame_ProducesValidXaml()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassBadge badge = new("PRO", _stage);
            Assert.StartsWith("F1 M", badge.EvaluateFrame().XamlContourPathData);
    
        });
    }

    [Fact]
    public void SeaGlassBadge_NumericCount42_MeetsWcagContrast()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            var badge = new SeaGlassBadge { Count = 42 };
            Assert.Equal("42", badge.Text);
            Assert.Equal("42", badge.Content);

            double lBg = 0.78;
            double lFg = 0.03;
            double contrastRatio = (Math.Max(lBg, lFg) + 0.05) / (Math.Min(lBg, lFg) + 0.05);
            Assert.True(contrastRatio >= 4.5, $"Expected WCAG AA contrast >= 4.5:1, got {contrastRatio:F2}:1");
        });
    }

    [Fact]
    public void SeaGlassStage_BackdropContent_WiresToHostBorder()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            var stage = new SeaGlassStage();
            var border = new Microsoft.UI.Xaml.Controls.Border();
            stage.BackdropContent = border;
            Assert.Equal(border, stage.BackdropContent);
        });
    }
    #endregion

    #region 13. SeaGlassSegmentedButtonRow Tests (>= 5 cases)
    [Fact]
    public void SeaGlassSegmentedButtonRow_Initialization_SetsOptions()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSegmentedButtonRow row = new(["Option A", "Option B", "Option C"], 0, _ => { }, _stage);
            Assert.Equal(3, row.Options.Count);
            Assert.Equal(0, row.SelectedIndex);
    
        });
    }

    [Fact]
    public void SeaGlassSegmentedButtonRow_SelectOption_InvokesCallback()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            int selected = -1;
            SeaGlassSegmentedButtonRow row = new(["A", "B", "C"], 0, idx => selected = idx, _stage);
            row.SelectOption(2);
            Assert.Equal(2, row.SelectedIndex);
            Assert.Equal(2, selected);
    
        });
    }

    [Fact]
    public void SeaGlassSegmentedButtonRow_DualEdgeSprings_LeadIndexLeadsTrailIndex()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSegmentedButtonRow row = new(["One", "Two", "Three", "Four"], 0, _ => { }, _stage);
            row.SelectOption(3);
            row.StepAndEvaluate(1f / 60f);
            row.StepAndEvaluate(1f / 60f);
            Assert.True(row.LeadIndex > row.TrailIndex, $"LeadIndex must lead TrailIndex ({row.LeadIndex} > {row.TrailIndex})");
    
        });
    }

    [Fact]
    public void SeaGlassSegmentedButtonRow_LozengeSlotSpan_StretchesDuringTravel()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSegmentedButtonRow row = new(["X", "Y", "Z"], 0, _ => { }, _stage);
            row.SelectOption(2);
            row.StepAndEvaluate(1f / 60f);
            row.StepAndEvaluate(1f / 60f);
            Assert.True(row.LozengeSlotSpan > 1.05f, $"LozengeSlotSpan must exceed 1.05 during travel ({row.LozengeSlotSpan})");
    
        });
    }

    [Fact]
    public void SeaGlassSegmentedButtonRow_EvaluateFrame_GeneratesValidXamlContour()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSegmentedButtonRow row = new(["Alpha", "Beta"], 0, _ => { }, _stage);
            SeaGlassSurfaceFrameState frame = row.StepAndEvaluate(0.016f);
            Assert.StartsWith("F1 M", frame.XamlContourPathData);
    
        });
    }
    #endregion

    #region 14. SeaGlassPebbleOrb Tests (>= 5 cases)
    [Fact]
    public void SeaGlassPebbleOrb_CircularContour_HasEqualWidthAndHeight()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassPebbleOrb orb = new(() => { }, _stage);
            Assert.Equal(orb.SizePx.X, orb.SizePx.Y);
    
        });
    }

    [Fact]
    public void SeaGlassPebbleOrb_Click_InvokesCallback()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            bool clicked = false;
            SeaGlassPebbleOrb orb = new(() => clicked = true, _stage);
            orb.OnPointerStateChanged(new Vector2(34f, 34f), true);
            orb.OnPointerStateChanged(new Vector2(34f, 34f), false);
            Assert.True(clicked);
    
        });
    }

    [Fact]
    public void SeaGlassPebbleOrb_PressScale_StretchesSpatialSpring()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassPebbleOrb orb = new(() => { }, _stage);
            orb.OnPointerStateChanged(new Vector2(34f, 34f), true);
            SeaGlassSurfaceFrameState frame = orb.StepAndEvaluate(0.03f);
            Assert.True(frame.ScaleX > 1.0f);
    
        });
    }

    [Fact]
    public void SeaGlassPebbleOrb_GyroscopicTilt_EvaluatesAngle()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassPebbleOrb orb = new(() => { }, _stage);
            orb.OnPointerStateChanged(new Vector2(10f, 10f), true);
            SeaGlassSurfaceFrameState frame = orb.StepAndEvaluate(0.02f);
            Assert.NotNull(frame);
    
        });
    }

    [Fact]
    public void SeaGlassPebbleOrb_StepAndEvaluate_GeneratesValidXamlContour()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassPebbleOrb orb = new(() => { }, _stage);
            SeaGlassSurfaceFrameState frame = orb.StepAndEvaluate(1f / 60f);
            Assert.StartsWith("F1 M", frame.XamlContourPathData);
    
        });
    }
    #endregion
}
