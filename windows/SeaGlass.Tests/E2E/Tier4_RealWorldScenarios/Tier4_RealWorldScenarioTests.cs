using SeaGlass.Tests.Controls;
using System;
using System.Numerics;
using SeaGlass.Core;
using SeaGlass.Core.Backend;
using SeaGlass.WinUI;
using SeaGlass.Tests.E2E.Infrastructure;
using Xunit;

namespace SeaGlass.Tests.E2E.Tier4_RealWorldScenarios;

public sealed class Tier4_RealWorldScenarioTests
{
    #region Scenario 1: Optical Lab & Telemetry Inspector
    [Fact]
    public void Scenario1_OpticalLabAndTelemetryInspector_ExecutesFullTuningWorkflow()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            // 1. Initialize Stage & Lab Card
            SeaGlassStageState stage = new();
            stage.BindBackdropTarget("LabBackdrop", Vector2.Zero, new Vector2(1920f, 1080f));

            SeaGlassAdaptiveCornerLabCard labCard = new(SeaGlassDeviceHardwareProfile.Pixel9Pro, stage);
            var (layout, tl, tr, dock) = labCard.SolveBezelFrame();
            Assert.True(layout.PodWidthPx > 0f);
            Assert.True(tl.TopLeftPx > tl.BottomRightPx);

            // 2. Adjust Optical Sliders (Coarseness & IOR)
            float currentCoarseness = 0.35f;
            float currentIor = 1.48f;
            SeaGlassLiquidSlider coarsenessSlider = new("Coarseness", "0.35", currentCoarseness, 0f, 1f, v => currentCoarseness = v, stage);
            SeaGlassLiquidSlider iorSlider = new("IOR", "1.48", currentIor, 1f, 2f, v => currentIor = v, stage);

            coarsenessSlider.SetValue(0.55f);
            iorSlider.SetValue(1.54f);
            Assert.Equal(0.55f, currentCoarseness);
            Assert.Equal(1.54f, currentIor);

            // 3. Toggle Refraction Pass via Switch
            bool refractionEnabled = true;
            SeaGlassSwitch refractionSwitch = new(true, state => refractionEnabled = state, stage);
            refractionSwitch.Toggle();
            Assert.False(refractionEnabled);
            refractionSwitch.Toggle();
            Assert.True(refractionEnabled);

            // 4. Update Surface Material & Validate 256-Byte Uniform Packing
            SeaGlassMaterial tunedMat = SeaGlassPalette.SeafoamMint.PrimaryGlass with
            {
                Coarseness = currentCoarseness,
                Ior = currentIor
            };
            labCard.SnapMaterial(tunedMat);
            SeaGlassSurfaceFrameState frame = labCard.EvaluateFrame(0.016f);

            Assert.Equal(currentCoarseness, frame.EffectiveMaterial.Coarseness, 2);
            Assert.Equal(currentIor, frame.EffectiveMaterial.Ior, 2);
            Assert.Equal(256, frame.UniformBlock.CreateDirectStagingBuffer().Length);

            // 5. Status Telemetry Badge
            SeaGlassBadge statusBadge = new("OKLAB CALIBRATED", stage);
            Assert.StartsWith("F1 M", statusBadge.EvaluateFrame().XamlContourPathData);
    
        });
    }
    #endregion

    #region Scenario 2: Media Player & Live Activity Hub
    [Fact]
    public void Scenario2_MediaPlayerAndLiveActivityHub_ExecutesPlaybackAndActivityWorkflow()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            // 1. Initialize Stage & Media Player
            SeaGlassStageState stage = new();
            stage.BindBackdropTarget("MediaBackdrop", Vector2.Zero, new Vector2(1920f, 1080f));

            bool playPauseToggled = false;
            bool nextTrackRequested = false;
            SeaGlassMiniPlayerBar player = new(
                "Pacific Swell", "SeaGlass Acoustic", false,
                onTogglePlay: () => playPauseToggled = true,
                onNextTrack: () => nextTrackRequested = true,
                stageState: stage);

            // 2. Playback Controls Interaction
            player.OnTogglePlay();
            Assert.True(playPauseToggled);

            player.OnNextTrack();
            Assert.True(nextTrackRequested);

            player.Title = "Cobalt Horizon";
            player.Subtitle = "SeaGlass Ambient";
            Assert.Equal("Cobalt Horizon", player.Title);
            Assert.Equal("SeaGlass Ambient", player.Subtitle);

            // 3. Expand Live Activity Pill
            SeaGlassLiveActivityPill livePill = new(expanded: false);
            livePill.SetExpanded(!livePill.Expanded);
            Assert.True(livePill.Expanded);
            livePill.Title = "Playing: Cobalt Horizon";
            livePill.Subtitle = "02:45 / 04:12";
            Assert.Equal("Playing: Cobalt Horizon", livePill.Title);
            Assert.Equal("02:45 / 04:12", livePill.Subtitle);

            // 4. Volume Scrubber Interaction
            float volume = 0.70f;
            SeaGlassMagnifierScrubber volumeScrubber = new(volume, v => volume = v);
            volumeScrubber.SetValue(0.85f);
            Assert.Equal(0.85f, volume);

            // 5. Steady-State Frame Step & XAML Contours
            SeaGlassSurfaceFrameState playerFrame = player.EvaluateFrame(0.016f);
            SeaGlassSurfaceFrameState pillFrame = livePill.StepAndEvaluate(0.016f);
            Assert.StartsWith("F1 M", playerFrame.XamlContourPathData);
            Assert.StartsWith("F1 M", pillFrame.XamlContourPathData);
    
        });
    }
    #endregion

    #region Scenario 3: Multi-Tab Document Navigation with Fluid Indicator
    [Fact]
    public void Scenario3_MultiTabNavigationAndFluidIndicator_ExecutesNavigationAndBuddingWorkflow()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            // 1. Initialize Stage & Fluid Bar
            SeaGlassStageState stage = new();
            stage.BindBackdropTarget("NavBackdrop", Vector2.Zero, new Vector2(1920f, 1080f));

            int selectedTab = 0;
            SeaGlassFluidBar fluidBar = new(["Overview", "Optics", "Shaders", "Export"], 0, idx => selectedTab = idx, stage);
            Assert.Equal(0, selectedTab);

            // 2. Select Tab with Lead-Lag Lozenge Travel
            fluidBar.SelectTab(2);
            Assert.Equal(2, selectedTab);
            fluidBar.StepAndEvaluate(1f / 60f);
            Assert.True(fluidBar.LeadIndex > fluidBar.TrailIndex);

            // Settle navigation indicator
            for (int i = 0; i < 30; i++) fluidBar.StepAndEvaluate(1f / 60f);
            Assert.Equal(fluidBar.LeadIndex, fluidBar.TrailIndex, 1);

            // 3. Detachable Budding Action Cluster
            int selectedSatellite = -1;
            SeaGlassBuddingCluster cluster = new(
                expanded: false,
                items: [("✦", "Enhance"), ("♡", "Favorite"), ("⤴", "Share")],
                onItemClick: idx => selectedSatellite = idx,
                stageState: stage);

            cluster.ToggleExpanded();
            Assert.True(cluster.Expanded);
            cluster.OnItemClick(0);
            Assert.Equal(0, selectedSatellite);

            // 4. Confirmation Dialog Presentation
            bool dialogConfirmed = false;
            SeaGlassDialogCard confirmDialog = new(
                "Apply Optical Enhance?",
                "This will recalculate 4 Kawase downsample blur pyramid passes.",
                "Apply",
                () => dialogConfirmed = true,
                stage,
                "Cancel",
                () => { });

            confirmDialog.OnConfirm();
            Assert.True(dialogConfirmed);

            // 5. Frame Validation
            Assert.StartsWith("F1 M", confirmDialog.EvaluateFrame().XamlContourPathData);
    
        });
    }
    #endregion

    #region Scenario 4: High-Contrast & Battery Saver Platform Transition
    [Fact]
    public void Scenario4_PlatformSettingsTransition_AdaptsTiersAndSettlesImmediately()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            // 1. Initial State: Normal GPU Rendering
            MockSystemSettingsProvider sysSettings = new()
            {
                AnimationsEnabled = true,
                TransparencyEffectsEnabled = true,
                HighContrastEnabled = false,
                EnergySaverActive = false
            };

            Assert.Equal(SeaGlassDeviceTier.D2D1HlslFull, sysSettings.ResolveEffectiveTier());

            SeaGlassButton btn = new("System Action", () => { });
            SeaGlassLiquidSlider slider = new("Backdrop Blur", "24dp", 24f, 0f, 64f, _ => { });

            // Normal animation frame
            btn.OnPointerStateChanged(new Vector2(20f, 10f), true);
            SeaGlassSurfaceFrameState f1 = btn.EvaluateFrame(0.016f);
            Assert.True(f1.ScaleX > 1.0f);

            // 2. Event: High Contrast Enabled by User
            sysSettings.HighContrastEnabled = true;
            Assert.Equal(SeaGlassDeviceTier.LightweightTint, sysSettings.ResolveEffectiveTier());

            // 3. Event: Battery Saver & Animations Off
            sysSettings.EnergySaverActive = true;
            sysSettings.AnimationsEnabled = false;

            float infiniteDt = sysSettings.AdjustDeltaTimeForAnimations(0.016f);
            Assert.True(infiniteDt >= 1000f);

            // Settle button immediately under reduced motion
            btn.OnPointerStateChanged(new Vector2(20f, 10f), false);
            SeaGlassSurfaceFrameState settledFrame = btn.EvaluateFrame(infiniteDt);
            Assert.InRange(settledFrame.ScaleX, 0.99f, 1.01f);
            Assert.InRange(settledFrame.ScaleY, 0.99f, 1.01f);

            // 4. Slider interaction under High Contrast
            slider.SetValue(48f);
            Assert.Equal(48f, slider.Value);

            // 5. Verify Accessibility Automation Pattern Still Operates Control
            var btnDesc = E2ECatalogContractRegistry.All58Controls.First(c => c.Name == "SeaGlassButton");
            Assert.Contains("IInvokeProvider", btnDesc.ExpectedPatterns);
    
        });
    }
    #endregion

    #region Scenario 5: Form Input & Context Menu Workflow
    [Fact]
    public void Scenario5_FormInputAndContextMenuWorkflow_ExecutesCompleteDataEntry()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            // 1. Initialize Stage
            SeaGlassStageState stage = new();
            stage.BindBackdropTarget("FormBackdrop", Vector2.Zero, new Vector2(1920f, 1080f));

            // 2. Search & Text Input Entry
            string query = "";
            SeaGlassSearchBar searchBar = new("", q => query = q, stage);
            searchBar.SetQuery("volumetric absorption");
            Assert.Equal("volumetric absorption", query);

            string thicknessVal = "";
            SeaGlassTextField textField = new("", t => thicknessVal = t, stage, label: "Thickness (dp)") { IsFocused = true };
            textField.SetValue("18.5");
            Assert.Equal("18.5", thicknessVal);
            SeaGlassSurfaceFrameState textFrame = textField.StepFocusAndEvaluate(0.03f);
            Assert.True(textFrame.EffectiveMaterial.ThicknessDp >= 10f);

            // 3. Checkbox & Radio Button Toggle
            bool agreed = false;
            SeaGlassCheckbox checkbox = new(false, state => agreed = state, stage);
            checkbox.Toggle();
            Assert.True(agreed);
            Assert.True(checkbox.Checked);

            bool optionSelected = false;
            SeaGlassRadioButton radio = new(false, () => optionSelected = true, stage);
            radio.SetSelected(true);
            radio.OnSelect();
            Assert.True(optionSelected);
            Assert.True(radio.Selected);

            // 4. Segmented Option Selection
            int selectedSegment = 0;
            SeaGlassSegmentedButtonRow segmentRow = new(["Draft", "Review", "Published"], 0, idx => selectedSegment = idx, stage);
            segmentRow.SelectOption(1);
            Assert.Equal(1, selectedSegment);

            // 5. Open Morphing Context Menu & Submit
            SeaGlassMorphingContextMenu contextMenu = new(expanded: false, stageState: stage);
            contextMenu.SetExpanded(true);
            Assert.True(contextMenu.Expanded);

            // 6. Snackbar Confirmation Toast
            bool snackbarActionFired = false;
            SeaGlassSnackbar toast = new("Record saved to catalog", stage, "Undo", () => snackbarActionFired = true);
            toast.OnActionClick?.Invoke();
            Assert.True(snackbarActionFired);
            Assert.StartsWith("F1 M", toast.EvaluateFrame().XamlContourPathData);
    
        });
    }
    #endregion
}
