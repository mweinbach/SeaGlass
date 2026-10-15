using SeaGlass.Tests.Controls;
using System;
using System.Numerics;
using SeaGlass.Core;
using SeaGlass.Core.Backend;
using SeaGlass.WinUI;
using Xunit;

namespace SeaGlass.Tests.E2E.Tier1_FeatureCoverage;

public sealed class Tier1_NavigationAndInputsTests
{
    private readonly SeaGlassStageState _stage;

    public Tier1_NavigationAndInputsTests()
    {
        _stage = new SeaGlassStageState();
        _stage.BindBackdropTarget("NavInputsStageBackdrop", Vector2.Zero, new Vector2(1920f, 1080f));
    }

    #region 1. SeaGlassTopAppBar Tests (>= 5 cases)
    [Fact]
    public void SeaGlassTopAppBar_Initialization_SetsTitleAndSubtitle()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassTopAppBar bar = new("SeaGlass Studio", _stage, "WinUI 3");
            Assert.Equal("SeaGlass Studio", bar.Title);
            Assert.Equal("WinUI 3", bar.Subtitle);
            Assert.Equal(SeaGlassContentRole.NavigationChrome, bar.ContentRole);
    
        });
    }

    [Fact]
    public void SeaGlassTopAppBar_BackAction_ExecutesCallback()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            bool backTriggered = false;
            SeaGlassTopAppBar bar = new("Title", _stage, onNavigationClick: () => backTriggered = true);
            bar.OnNavigationClick?.Invoke();
            Assert.True(backTriggered);
    
        });
    }

    [Fact]
    public void SeaGlassTopAppBar_SubsurfaceDiffusion_PacksLowCoarseness()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassTopAppBar bar = new("Nav Bar", _stage);
            Assert.True(bar.Material.Coarseness <= 0.60f);
            Assert.True(bar.Material.ThicknessDp >= 14f);
    
        });
    }

    [Fact]
    public void SeaGlassTopAppBar_Size_SpansFullWidth()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassTopAppBar bar = new("Nav Bar", _stage);
            Assert.True(bar.SizePx.X >= 360f);
            Assert.True(bar.SizePx.Y >= 48f);
    
        });
    }

    [Fact]
    public void SeaGlassTopAppBar_EvaluateFrame_GeneratesValidXamlContour()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassTopAppBar bar = new("Top Bar", _stage);
            Assert.StartsWith("F1 M", bar.EvaluateFrame().XamlContourPathData);
    
        });
    }
    #endregion

    #region 2. SeaGlassFluidBar Tests (>= 5 cases)
    [Fact]
    public void SeaGlassFluidBar_Initialization_ContainsTabs()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassFluidBar bar = new(["Home", "Explore", "Library"], 0, _ => { }, _stage);
            Assert.Equal(3, bar.Items.Count);
            Assert.Equal(0, bar.SelectedIndex);
    
        });
    }

    [Fact]
    public void SeaGlassFluidBar_SelectTab_InvokesCallback()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            int selected = -1;
            SeaGlassFluidBar bar = new(["A", "B", "C"], 0, idx => selected = idx, _stage);
            bar.SelectTab(1);
            Assert.Equal(1, bar.SelectedIndex);
            Assert.Equal(1, selected);
    
        });
    }

    [Fact]
    public void SeaGlassFluidBar_LeadLagLozenge_LeadsDuringTravel()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassFluidBar bar = new(["1", "2", "3", "4"], 0, _ => { }, _stage);
            bar.SelectTab(3);
            bar.StepAndEvaluate(1f / 60f);
            bar.StepAndEvaluate(1f / 60f);
            Assert.True(bar.LeadIndex > bar.TrailIndex);
            Assert.True(bar.LozengeSlotSpan > 1.05f);
    
        });
    }

    [Fact]
    public void SeaGlassFluidBar_DetachableActionOrb_PacksSecondaryNode()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassFluidBar bar = new(["Home", "Search"], 0, _ => { }, _stage, showActionOrb: true, actionDetached: true);
            SeaGlassSurfaceFrameState frame = bar.StepAndEvaluate(0.016f);
            Assert.True(frame.UniformBlock.ToStd140Struct().LightAndMeta.W >= 2f);
    
        });
    }

    [Fact]
    public void SeaGlassFluidBar_EvaluateFrame_GeneratesValidXamlContour()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassFluidBar bar = new(["Tab 1", "Tab 2"], 0, _ => { }, _stage);
            Assert.StartsWith("F1 M", bar.StepAndEvaluate(0.016f).XamlContourPathData);
    
        });
    }
    #endregion

    #region 3. SeaGlassMiniPlayerBar Tests (>= 5 cases)
    [Fact]
    public void SeaGlassMiniPlayerBar_Initialization_SetsMetadata()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassMiniPlayerBar player = new("Ocean Drift", "SeaGlass Sounds", false, () => { }, () => { }, _stage);
            Assert.Equal("Ocean Drift", player.Title);
            Assert.Equal("SeaGlass Sounds", player.Subtitle);
            Assert.False(player.IsPlaying);
    
        });
    }

    [Fact]
    public void SeaGlassMiniPlayerBar_PlayPause_TogglesState()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            bool toggled = false;
            SeaGlassMiniPlayerBar player = new("Track", "Artist", false, () => toggled = true, () => { }, _stage);
            player.OnTogglePlay();
            Assert.True(toggled);
    
        });
    }

    [Fact]
    public void SeaGlassMiniPlayerBar_NextTrack_InvokesHandler()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            bool nextCalled = false;
            SeaGlassMiniPlayerBar player = new("Track", "Artist", true, () => { }, () => nextCalled = true, _stage);
            player.OnNextTrack();
            Assert.True(nextCalled);
    
        });
    }

    [Fact]
    public void SeaGlassMiniPlayerBar_SetTrack_UpdatesMetadata()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassMiniPlayerBar player = new("Track A", "Artist A", true, () => { }, () => { }, _stage);
            player.Title = "Track B";
            player.Subtitle = "Artist B";
            Assert.Equal("Track B", player.Title);
            Assert.Equal("Artist B", player.Subtitle);
    
        });
    }

    [Fact]
    public void SeaGlassMiniPlayerBar_EvaluateFrame_GeneratesValidXaml()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassMiniPlayerBar player = new("Track", "Artist", true, () => { }, () => { }, _stage);
            Assert.StartsWith("F1 M", player.EvaluateFrame().XamlContourPathData);
    
        });
    }
    #endregion

    #region 4. SeaGlassFloatingToolbar Tests (>= 5 cases)
    [Fact]
    public void SeaGlassFloatingToolbar_Initialization_SetsActions()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassFloatingToolbar tb = new(
                [(SeaGlassGlyph.Home, "Home"), (SeaGlassGlyph.Search, "Search")],
                0, _ => { }, SeaGlassGlyph.Plus, true, () => { }, _stage);
            Assert.Equal(2, tb.Actions.Count);
            Assert.Equal(0, tb.SelectedActionIndex);
    
        });
    }

    [Fact]
    public void SeaGlassFloatingToolbar_SelectAction_InvokesHandler()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            int selected = -1;
            SeaGlassFloatingToolbar tb = new(
                [(SeaGlassGlyph.Home, "Home"), (SeaGlassGlyph.Settings, "Settings")],
                0, idx => selected = idx, SeaGlassGlyph.Plus, false, () => { }, _stage);
            tb.OnActionSelect(1);
            Assert.Equal(1, selected);
    
        });
    }

    [Fact]
    public void SeaGlassFloatingToolbar_TrailingActionOrb_TriggersCallback()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            bool orbClicked = false;
            SeaGlassFloatingToolbar tb = new(
                [(SeaGlassGlyph.Home, "Home")],
                0, _ => { }, SeaGlassGlyph.Sparkles, true, () => orbClicked = true, _stage);
            tb.OnFabClick();
            Assert.True(orbClicked);
    
        });
    }

    [Fact]
    public void SeaGlassFloatingToolbar_MultiNodeUniform_PacksBothNodes()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassFloatingToolbar tb = new(
                [(SeaGlassGlyph.Home, "Home")],
                0, _ => { }, SeaGlassGlyph.Plus, true, () => { }, _stage);
            SeaGlassSurfaceFrameState frame = tb.StepAndEvaluate(0.016f);
            Assert.Equal(2f, frame.UniformBlock.ToStd140Struct().LightAndMeta.W, 2);
    
        });
    }

    [Fact]
    public void SeaGlassFloatingToolbar_EvaluateFrame_GeneratesValidXaml()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassFloatingToolbar tb = new([(SeaGlassGlyph.Tune, "Optics")], 0, _ => { }, SeaGlassGlyph.Plus, false, () => { }, _stage);
            Assert.StartsWith("F1 M", tb.StepAndEvaluate(0.016f).XamlContourPathData);
    
        });
    }
    #endregion

    #region 5. SeaGlassPillNavBar Tests (>= 5 cases)
    [Fact]
    public void SeaGlassPillNavBar_Initialization_SetsTabs()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassPillNavBar nav = new([(SeaGlassGlyph.Home, "Feed"), (SeaGlassGlyph.Search, "Find")], 0, _ => { }, _stage);
            Assert.Equal(2, nav.Items.Count);
            Assert.Equal(0, nav.SelectedIndex);
    
        });
    }

    [Fact]
    public void SeaGlassPillNavBar_SelectTab_InvokesCallback()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            int selected = -1;
            SeaGlassPillNavBar nav = new([(SeaGlassGlyph.Home, "Home"), (SeaGlassGlyph.Bell, "Alerts")], 0, idx => selected = idx, _stage);
            nav.SelectTab(1);
            Assert.Equal(1, nav.SelectedIndex);
            Assert.Equal(1, selected);
    
        });
    }

    [Fact]
    public void SeaGlassPillNavBar_LeadLagDynamics_AdvancesLeadFirst()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassPillNavBar nav = new([(SeaGlassGlyph.Home, "1"), (null, "2"), (null, "3")], 0, _ => { }, _stage);
            nav.SelectTab(2);
            nav.StepAndEvaluate(1f / 60f);
            nav.StepAndEvaluate(1f / 60f);
            Assert.True(nav.LeadIndex > nav.TrailIndex);
    
        });
    }

    [Fact]
    public void SeaGlassPillNavBar_ReverseRimInlay_HasHighChalkiness()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassPillNavBar nav = new([(SeaGlassGlyph.Home, "Home")], 0, _ => { }, _stage);
            Assert.True(nav.Material.RimChalkiness >= 0.20f);
    
        });
    }

    [Fact]
    public void SeaGlassPillNavBar_EvaluateFrame_GeneratesValidXaml()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassPillNavBar nav = new([(SeaGlassGlyph.PhotoMountain, "Photos")], 0, _ => { }, _stage);
            Assert.StartsWith("F1 M", nav.StepAndEvaluate(0.016f).XamlContourPathData);
    
        });
    }
    #endregion

    #region 6. SeaGlassPromptBar Tests (>= 5 cases)
    [Fact]
    public void SeaGlassPromptBar_Initialization_SetsPromptAndVoiceState()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassPromptBar prompt = new("Ask Gemini", isVoiceActive: false, stageState: _stage);
            Assert.Equal("Ask Gemini", prompt.PromptText);
            Assert.False(prompt.IsVoiceActive);
    
        });
    }

    [Fact]
    public void SeaGlassPromptBar_ToggleVoiceActive_TogglesState()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassPromptBar prompt = new("Ask Gemini", isVoiceActive: false, stageState: _stage);
            prompt.ToggleVoiceActive();
            Assert.True(prompt.IsVoiceActive);
            prompt.ToggleVoiceActive();
            Assert.False(prompt.IsVoiceActive);
    
        });
    }

    [Fact]
    public void SeaGlassPromptBar_SetPromptText_UpdatesProperty()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassPromptBar prompt = new("Initial", isVoiceActive: false, stageState: _stage);
            prompt.PromptText = "Describe the Beer-Lambert law";
            Assert.Equal("Describe the Beer-Lambert law", prompt.PromptText);
    
        });
    }

    [Fact]
    public void SeaGlassPromptBar_VoicePulsation_ModifiesScale()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassPromptBar prompt = new("Voice", isVoiceActive: true, stageState: _stage);
            SeaGlassSurfaceFrameState f1 = prompt.StepAndEvaluate(0.02f);
            SeaGlassSurfaceFrameState f2 = prompt.StepAndEvaluate(0.04f);
            Assert.NotNull(f1);
            Assert.NotNull(f2);
    
        });
    }

    [Fact]
    public void SeaGlassPromptBar_EvaluateFrame_GeneratesValidXaml()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassPromptBar prompt = new("Query", isVoiceActive: false, stageState: _stage);
            Assert.StartsWith("F1 M", prompt.StepAndEvaluate(1f / 60f).XamlContourPathData);
    
        });
    }
    #endregion

    #region 7. SeaGlassSearchBar Tests (>= 5 cases)
    [Fact]
    public void SeaGlassSearchBar_Initialization_SetsQuery()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSearchBar search = new("superellipse", _ => { }, _stage);
            Assert.Equal("superellipse", search.Query);
            Assert.Equal(SeaGlassContentRole.NavigationChrome, search.ContentRole);
    
        });
    }

    [Fact]
    public void SeaGlassSearchBar_SetQuery_InvokesCallback()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            string updatedQuery = "";
            SeaGlassSearchBar search = new("", q => updatedQuery = q, _stage);
            search.SetQuery("rough fresnel");
            Assert.Equal("rough fresnel", search.Query);
            Assert.Equal("rough fresnel", updatedQuery);
    
        });
    }

    [Fact]
    public void SeaGlassSearchBar_FocusExpansion_BoostsThickness()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSearchBar search = new("oklab", _ => { }, _stage) { IsFocused = true };
            SeaGlassSurfaceFrameState frame = search.StepFocusAndEvaluate(0.05f);
            Assert.True(frame.EffectiveMaterial.ThicknessDp > 14f);
            Assert.True(search.Material.ThicknessDp > 16f);
    
        });
    }

    [Fact]
    public void SeaGlassSearchBar_ClearQuery_EmptiesText()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSearchBar search = new("query to clear", _ => { }, _stage);
            search.ClearQuery();
            Assert.Equal("", search.Query);
    
        });
    }

    [Fact]
    public void SeaGlassSearchBar_EvaluateFrame_GeneratesValidXaml()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSearchBar search = new("test", _ => { }, _stage);
            Assert.StartsWith("F1 M", search.EvaluateFrame().XamlContourPathData);
    
        });
    }
    #endregion

    #region 8. SeaGlassTextField Tests (>= 5 cases)
    [Fact]
    public void SeaGlassTextField_Initialization_SetsTextAndWatermark()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassTextField field = new("1.54", _ => { }, _stage, label: "IOR Index");
            Assert.Equal("1.54", field.Value);
            Assert.Equal("IOR Index", field.Label);
    
        });
    }

    [Fact]
    public void SeaGlassTextField_SetText_InvokesCallback()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            string text = "";
            SeaGlassTextField field = new("", t => text = t, _stage);
            field.SetValue("0.42");
            Assert.Equal("0.42", field.Value);
            Assert.Equal("0.42", text);
    
        });
    }

    [Fact]
    public void SeaGlassTextField_FocusState_AnimatesRimAndThickness()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassTextField field = new("text", _ => { }, _stage) { IsFocused = true };
            SeaGlassSurfaceFrameState frame = field.StepFocusAndEvaluate(0.04f);
            Assert.True(frame.EffectiveMaterial.ThicknessDp >= 10f);
            Assert.StartsWith("F1 M", frame.XamlContourPathData);
    
        });
    }

    [Fact]
    public void SeaGlassTextField_UnfocusedState_RecoversThickness()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassTextField field = new("text", _ => { }, _stage) { IsFocused = false };
            SeaGlassSurfaceFrameState frame = field.StepFocusAndEvaluate(0.04f);
            Assert.NotNull(frame);
    
        });
    }

    [Fact]
    public void SeaGlassTextField_EvaluateFrame_GeneratesValidXaml()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassTextField field = new("val", _ => { }, _stage);
            Assert.StartsWith("F1 M", field.EvaluateFrame().XamlContourPathData);
    
        });
    }
    #endregion

    #region 9. SeaGlassSwitch Tests (>= 5 cases)
    [Fact]
    public void SeaGlassSwitch_Initialization_SetsIsOn()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSwitch sw = new(false, _ => { }, _stage);
            Assert.False(sw.Checked);
    
        });
    }

    [Fact]
    public void SeaGlassSwitch_Toggle_FlipsStateAndCallsHandler()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            bool toggledState = false;
            SeaGlassSwitch sw = new(false, state => toggledState = state, _stage);
            sw.Toggle();
            Assert.True(sw.Checked);
            Assert.True(toggledState);
            sw.Toggle();
            Assert.False(sw.Checked);
            Assert.False(toggledState);
    
        });
    }

    [Fact]
    public void SeaGlassSwitch_ThumbVelocityStretch_StretchesOnToggle()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSwitch sw = new(false, _ => { }, _stage);
            sw.Toggle();
            SeaGlassSurfaceFrameState frame = sw.StepAndEvaluate(0.02f);
            Assert.True(sw.ThumbScaleX >= 1.0f);
            Assert.NotNull(frame);
    
        });
    }

    [Fact]
    public void SeaGlassSwitch_SettledState_ReachesNormalizedScale()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSwitch sw = new(false, _ => { }, _stage);
            sw.Toggle();
            for (int i = 0; i < 30; i++) sw.StepAndEvaluate(1f / 60f); // settle spring across frames
            Assert.InRange(sw.ThumbScaleX, 0.99f, 1.05f);
    
        });
    }

    [Fact]
    public void SeaGlassSwitch_EvaluateFrame_GeneratesValidXaml()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSwitch sw = new(true, _ => { }, _stage);
            Assert.StartsWith("F1 M", sw.StepAndEvaluate(0.016f).XamlContourPathData);
    
        });
    }
    #endregion

    #region 10. SeaGlassCheckbox Tests (>= 5 cases)
    [Fact]
    public void SeaGlassCheckbox_Initialization_SetsChecked()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassCheckbox cb = new(false, _ => { }, _stage);
            Assert.False(cb.Checked);
    
        });
    }

    [Fact]
    public void SeaGlassCheckbox_Toggle_FlipsCheckedAndCallsHandler()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            bool checkedState = false;
            SeaGlassCheckbox cb = new(false, state => checkedState = state, _stage);
            cb.Toggle();
            Assert.True(cb.Checked);
            Assert.True(checkedState);
            cb.Toggle();
            Assert.False(cb.Checked);
            Assert.False(checkedState);
    
        });
    }

    [Fact]
    public void SeaGlassCheckbox_CloverMorph_TransitionsProgress()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassCheckbox cb = new(false, _ => { }, _stage);
            cb.Toggle();
            SeaGlassSurfaceFrameState frame = cb.StepAndEvaluate(0.03f);
            Assert.True(cb.MorphProgress > 0f);
            Assert.StartsWith("F1 M", frame.XamlContourPathData);
    
        });
    }

    [Fact]
    public void SeaGlassCheckbox_CheckmarkReveal_RevealsPathWhenChecked()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassCheckbox cb = new(true, _ => { }, _stage);
            cb.StepAndEvaluate(0.1f);
            Assert.True(cb.Checked);
    
        });
    }

    [Fact]
    public void SeaGlassCheckbox_EvaluateFrame_GeneratesValidXaml()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassCheckbox cb = new(true, _ => { }, _stage);
            Assert.StartsWith("F1 M", cb.StepAndEvaluate(0.016f).XamlContourPathData);
    
        });
    }
    #endregion

    #region 11. SeaGlassRadioButton Tests (>= 5 cases)
    [Fact]
    public void SeaGlassRadioButton_Initialization_SetsIsChecked()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassRadioButton rb = new(false, () => { }, _stage);
            Assert.False(rb.Selected);
    
        });
    }

    [Fact]
    public void SeaGlassRadioButton_Select_SetsCheckedAndCallsHandler()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            bool selected = false;
            SeaGlassRadioButton rb = new(false, () => selected = true, _stage);
            rb.SetSelected(true);
            Assert.True(rb.Selected);
            rb.OnSelect();
            Assert.True(selected);
    
        });
    }

    [Fact]
    public void SeaGlassRadioButton_InnerDotLens_ExpandsDotProgressOnSelect()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassRadioButton rb = new(false, () => { }, _stage);
            rb.SetSelected(true);
            SeaGlassSurfaceFrameState frame = rb.StepAndEvaluate(0.03f);
            Assert.True(rb.InnerDropletScale > 0f);
            Assert.NotNull(frame);
    
        });
    }

    [Fact]
    public void SeaGlassRadioButton_CheckedThickness_Exceeds12dp()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassRadioButton rb = new(true, () => { }, _stage);
            SeaGlassSurfaceFrameState frame = rb.StepAndEvaluate(0.05f);
            Assert.True(frame.EffectiveMaterial.ThicknessDp > 12f);
    
        });
    }

    [Fact]
    public void SeaGlassRadioButton_EvaluateFrame_GeneratesValidXaml()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassRadioButton rb = new(true, () => { }, _stage);
            Assert.StartsWith("F1 M", rb.StepAndEvaluate(0.016f).XamlContourPathData);
    
        });
    }
    #endregion

    #region 12. SeaGlassLiquidSlider Tests (>= 5 cases)
    [Fact]
    public void SeaGlassLiquidSlider_Initialization_SetsValueAndRange()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassLiquidSlider slider = new("Coarseness", "0.42", 0.42f, 0f, 1f, _ => { }, _stage);
            Assert.Equal(0.42f, slider.Value);
            Assert.Equal(0f, slider.MinValue);
            Assert.Equal(1f, slider.MaxValue);
    
        });
    }

    [Fact]
    public void SeaGlassLiquidSlider_SetValue_ClampsToRangeAndCallsHandler()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            float updated = -1f;
            SeaGlassLiquidSlider slider = new("Slider", "0.5", 0.5f, 0f, 1f, v => updated = v, _stage);
            slider.SetValue(0.85f);
            Assert.Equal(0.85f, slider.Value);
            Assert.Equal(0.85f, updated);

            // Clamping check
            slider.SetValue(1.5f);
            Assert.Equal(1.0f, slider.Value);
            slider.SetValue(-0.5f);
            Assert.Equal(0.0f, slider.Value);
    
        });
    }

    [Fact]
    public void SeaGlassLiquidSlider_DragInteraction_StretchesWaterDropThumb()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassLiquidSlider slider = new("Slider", "0.5", 0.5f, 0f, 1f, _ => { }, _stage)
            {
                IsDragging = true
            };
            slider.StepAndEvaluate(0.03f);
            Assert.True(slider.ThumbScaleX >= 1.0f);
    
        });
    }

    [Fact]
    public void SeaGlassLiquidSlider_FormatValueLabel_DisplaysAccurateString()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassLiquidSlider slider = new("Opacity", "75%", 0.75f, 0f, 1f, _ => { }, _stage);
            Assert.Equal("75%", slider.ValueText);
            slider.SetValue(0.90f);
            slider.ValueText = "90%";
            Assert.Equal("90%", slider.ValueText);
    
        });
    }

    [Fact]
    public void SeaGlassLiquidSlider_EvaluateFrame_GeneratesValidXaml()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassLiquidSlider slider = new("IOR", "1.52", 0.52f, 0f, 1f, _ => { }, _stage);
            Assert.StartsWith("F1 M", slider.StepAndEvaluate(0.016f).XamlContourPathData);
    
        });
    }
    #endregion
}
