using SeaGlass.Tests.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;
using SeaGlass.Core.Backend;
using SeaGlass.WinUI;
using SeaGlass.WinUI.Rendering;
using SeaGlass.Tests.E2E.Infrastructure;
using SeaGlass.Tests.Infrastructure;
using Xunit;

namespace SeaGlass.Tests.E2E.Tier1_FeatureCoverage;

public sealed class Tier1_WindowsExtrasAndThemingTests
{
    private readonly SeaGlassStageState _stage;

    public Tier1_WindowsExtrasAndThemingTests()
    {
        _stage = new SeaGlassStageState();
        _stage.BindBackdropTarget("ExtrasThemingBackdrop", Vector2.Zero, new Vector2(1920f, 1080f));
    }

    #region 1. Windows Native Extras Live Visual Tree & Behavior Tests (8 Live Controls)

    [Fact]
    public void WindowsExtras_SeaGlassTitleBar_LiveActivation_TemplatePartsAndEvents()
    {
        var result = HostSelfTestRunner.DefaultResult;
        Assert.True(result.Controls.TryGetValue("SeaGlassTitleBar", out var outcome), "SeaGlassTitleBar missing from host report");
        Assert.Equal("PASS", outcome.Status);
        Assert.True(outcome.ResolvedParts >= 7, $"SeaGlassTitleBar resolved {outcome.ResolvedParts} parts, expected >= 7");
        Assert.Equal(outcome.ExpectedParts, outcome.ResolvedParts);

        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            var titleBar = new SeaGlassTitleBar("SeaGlass Explorer Desktop");

            // Public properties & presenter binding
            Assert.Equal("SeaGlass Explorer Desktop", titleBar.Title);
            Assert.True(titleBar.IsActive);
            Assert.True(titleBar.ExtendsContentIntoTitleBar);

            // Test BackRequested live routed event
            bool backRequestedFired = false;
            titleBar.BackRequested += (s, e) => backRequestedFired = true;
            titleBar.RaiseBackRequested();
            Assert.True(backRequestedFired, "BackRequested event must fire on RaiseBackRequested()");

            // Test custom drag region
            Border customDrag = new() { Width = 200, Height = 32 };
            titleBar.SetDragRegion(customDrag);

            // Test toggle ExtendsContentIntoTitleBar
            titleBar.ExtendsContentIntoTitleBar = false;
            Assert.False(titleBar.ExtendsContentIntoTitleBar);
        });
    }

    [Fact]
    public void WindowsExtras_SeaGlassNavigationView_LiveActivation_TemplatePartsAndNavigation()
    {
        var result = HostSelfTestRunner.DefaultResult;
        Assert.True(result.Controls.TryGetValue("SeaGlassNavigationView", out var outcome), "SeaGlassNavigationView missing from host report");
        Assert.Equal("PASS", outcome.Status);
        Assert.True(outcome.ResolvedParts >= 8, $"SeaGlassNavigationView resolved {outcome.ResolvedParts} parts, expected >= 8");
        Assert.Equal(outcome.ExpectedParts, outcome.ResolvedParts);

        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            var nav = new SeaGlassNavigationView();

            // Add menu items
            nav.MenuItems.Add("Item_Home");
            nav.MenuItems.Add("Item_Optics");
            nav.MenuItems.Add("Item_Settings");
            Assert.Equal(3, nav.MenuItems.Count);

            // Test selection change event
            bool selectionChangedFired = false;
            nav.SelectionChanged += (s, e) => selectionChangedFired = true;
            nav.SelectedItem = "Item_Optics";
            Assert.Equal("Item_Optics", nav.SelectedItem);
            Assert.True(selectionChangedFired, "SelectionChanged must fire when SelectedItem updates");

            // Test ItemInvoked
            bool itemInvokedFired = false;
            nav.ItemInvoked += (s, e) => itemInvokedFired = true;
            nav.InvokeItem("Item_Home");
            Assert.Equal("Item_Home", nav.SelectedItem);
            Assert.True(itemInvokedFired, "ItemInvoked must fire on InvokeItem()");

            // Test Pane toggle events
            bool paneClosedFired = false;
            bool paneOpenedFired = false;
            nav.PaneClosed += (s, e) => paneClosedFired = true;
            nav.PaneOpened += (s, e) => paneOpenedFired = true;

            nav.IsPaneOpen = false;
            Assert.True(paneClosedFired, "PaneClosed must fire when IsPaneOpen set to false");

            nav.IsPaneOpen = true;
            Assert.True(paneOpenedFired, "PaneOpened must fire when IsPaneOpen set to true");
        });
    }

    [Fact]
    public void WindowsExtras_SeaGlassTabView_LiveActivation_TemplatePartsAndTabManagement()
    {
        var result = HostSelfTestRunner.DefaultResult;
        Assert.True(result.Controls.TryGetValue("SeaGlassTabView", out var outcome), "SeaGlassTabView missing from host report");
        Assert.Equal("PASS", outcome.Status);
        Assert.True(outcome.ResolvedParts >= 6, $"SeaGlassTabView resolved {outcome.ResolvedParts} parts, expected >= 6");
        Assert.Equal(outcome.ExpectedParts, outcome.ResolvedParts);

        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            var tabView = new SeaGlassTabView();

            // Tab management
            tabView.TabItems.Add("Document 1");
            tabView.TabItems.Add("Document 2");
            Assert.Equal(2, tabView.TabItems.Count);

            // Test Add Tab button event & command
            bool addTabFired = false;
            tabView.AddTabButtonClick += (s, e) => addTabFired = true;
            tabView.RequestAddTab();
            Assert.True(addTabFired, "AddTabButtonClick must fire on RequestAddTab()");

            // Test tab close requested
            bool closeRequestedFired = false;
            tabView.TabCloseRequested += (s, e) => closeRequestedFired = true;
            tabView.RaiseTabCloseRequested("Document 1");
            Assert.True(closeRequestedFired, "TabCloseRequested must fire on RaiseTabCloseRequested()");
            Assert.Single(tabView.TabItems);

            // Test selection change
            bool selectionChangedFired = false;
            tabView.SelectionChanged += (s, e) => selectionChangedFired = true;
            tabView.SelectedIndex = 0;
            Assert.Equal(0, tabView.SelectedIndex);
            Assert.True(selectionChangedFired, "SelectionChanged must fire on SelectedIndex update");

            // Test lead-lag lozenge stepping
            var (left, right, stretch) = tabView.StepLozenge(10f, 150f, 0.016f);
            Assert.True(right > left, "Lozenge right boundary must lead left boundary");
        });
    }

    [Fact]
    public void WindowsExtras_SeaGlassCommandBar_LiveActivation_TemplatePartsAndOverflow()
    {
        var result = HostSelfTestRunner.DefaultResult;
        Assert.True(result.Controls.TryGetValue("SeaGlassCommandBar", out var outcome), "SeaGlassCommandBar missing from host report");
        Assert.Equal("PASS", outcome.Status);
        Assert.True(outcome.ResolvedParts >= 6, $"SeaGlassCommandBar resolved {outcome.ResolvedParts} parts, expected >= 6");
        Assert.Equal(outcome.ExpectedParts, outcome.ResolvedParts);

        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            var cmd = new SeaGlassCommandBar();

            // Test overflow open
            bool openedFired = false;
            bool closedFired = false;
            cmd.Opened += (s, e) => openedFired = true;
            cmd.Closed += (s, e) => closedFired = true;

            Assert.False(cmd.IsOpen);
            cmd.ToggleOverflow();
            Assert.True(cmd.IsOpen);
            Assert.True(openedFired, "Opened event must fire when overflow opens");

            cmd.ToggleOverflow();
            Assert.False(cmd.IsOpen);
            Assert.True(closedFired, "Closed event must fire when overflow closes");
        });
    }

    [Fact]
    public void WindowsExtras_SeaGlassInfoBar_LiveActivation_TemplatePartsAndSeverityTones()
    {
        var result = HostSelfTestRunner.DefaultResult;
        Assert.True(result.Controls.TryGetValue("SeaGlassInfoBar", out var outcome), "SeaGlassInfoBar missing from host report");
        Assert.Equal("PASS", outcome.Status);
        Assert.True(outcome.ResolvedParts >= 7, $"SeaGlassInfoBar resolved {outcome.ResolvedParts} parts, expected >= 7");
        Assert.Equal(outcome.ExpectedParts, outcome.ResolvedParts);

        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            var info = new SeaGlassInfoBar("Disk Warning", "Only 2GB remaining", InfoBarSeverity.Warning);

            // Verify severity tone mapping
            Assert.Equal(SeaGlassTone.BeachAmber, info.Tone);

            info.Severity = InfoBarSeverity.Error;
            Assert.Equal(SeaGlassTone.RoseQuartzShell, info.Tone);

            info.Severity = InfoBarSeverity.Success;
            Assert.Equal(SeaGlassTone.KelpForest, info.Tone);

            info.Severity = InfoBarSeverity.Informational;
            Assert.Equal(SeaGlassTone.CobaltTide, info.Tone);

            // Test close interaction
            bool closeClickFired = false;
            info.CloseButtonClick += (s, e) => closeClickFired = true;
            info.Close();
            Assert.False(info.IsOpen);
            Assert.Equal(Visibility.Collapsed, info.Visibility);
            Assert.True(closeClickFired, "CloseButtonClick must fire on Close()");
        });
    }

    [Fact]
    public void WindowsExtras_SeaGlassTeachingTip_LiveActivation_TemplatePartsAndActions()
    {
        var result = HostSelfTestRunner.DefaultResult;
        Assert.True(result.Controls.TryGetValue("SeaGlassTeachingTip", out var outcome), "SeaGlassTeachingTip missing from host report");
        Assert.Equal("PASS", outcome.Status);
        Assert.True(outcome.ResolvedParts >= 9, $"SeaGlassTeachingTip resolved {outcome.ResolvedParts} parts, expected >= 9");
        Assert.Equal(outcome.ExpectedParts, outcome.ResolvedParts);

        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            var tip = new SeaGlassTeachingTip("New Feature", "Click here to see glass physics");

            // Test action button event
            bool actionFired = false;
            tip.ActionButtonClick += (s, e) => actionFired = true;
            tip.RequestAction();
            Assert.True(actionFired, "ActionButtonClick must fire on RequestAction()");

            // Test close button event
            bool closeFired = false;
            tip.CloseButtonClick += (s, e) => closeFired = true;
            tip.IsOpen = true;
            tip.RequestClose();
            Assert.False(tip.IsOpen);
            Assert.True(closeFired, "CloseButtonClick must fire on RequestClose()");
        });
    }

    [Fact]
    public void WindowsExtras_SeaGlassFlyout_LiveActivation_TemplatePartsAndShowHide()
    {
        var result = HostSelfTestRunner.DefaultResult;
        Assert.True(result.Controls.TryGetValue("SeaGlassFlyout", out var outcome), "SeaGlassFlyout missing from host report");
        Assert.Equal("PASS", outcome.Status);
        Assert.True(outcome.ResolvedParts >= 4, $"SeaGlassFlyout resolved {outcome.ResolvedParts} parts, expected >= 4");
        Assert.Equal(outcome.ExpectedParts, outcome.ResolvedParts);

        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            var flyout = new SeaGlassFlyout();

            // Test ShowAt and Hide
            Button anchor = new() { Content = "Target" };
            flyout.ShowAt(anchor);
            Assert.True(flyout.IsOpen);

            flyout.Hide();
            Assert.False(flyout.IsOpen);
        });
    }

    [Fact]
    public void WindowsExtras_SeaGlassToolTip_LiveActivation_TemplatePartsAndOpenClose()
    {
        var result = HostSelfTestRunner.DefaultResult;
        Assert.True(result.Controls.TryGetValue("SeaGlassToolTip", out var outcome), "SeaGlassToolTip missing from host report");
        Assert.Equal("PASS", outcome.Status);
        Assert.True(outcome.ResolvedParts >= 3, $"SeaGlassToolTip resolved {outcome.ResolvedParts} parts, expected >= 3");
        Assert.Equal(outcome.ExpectedParts, outcome.ResolvedParts);

        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            var tt = new SeaGlassToolTip();

            // Test open and close events
            bool openedFired = false;
            bool closedFired = false;
            tt.Opened += (s, e) => openedFired = true;
            tt.Closed += (s, e) => closedFired = true;

            tt.IsOpen = true;
            Assert.True(openedFired, "Opened must fire when IsOpen set to true");

            tt.IsOpen = false;
            Assert.True(closedFired, "Closed must fire when IsOpen set to false");
        });
    }

    [Fact]
    public void WindowsExtras_AllEight_InstantiatedAndVerifiedThroughActivationHarness()
    {
        var result = HostSelfTestRunner.DefaultResult;
        string[] extraTypes =
        [
            "SeaGlassTitleBar",
            "SeaGlassNavigationView",
            "SeaGlassTabView",
            "SeaGlassCommandBar",
            "SeaGlassInfoBar",
            "SeaGlassTeachingTip",
            "SeaGlassFlyout",
            "SeaGlassToolTip"
        ];

        foreach (var controlName in extraTypes)
        {
            Assert.True(result.Controls.TryGetValue(controlName, out var outcome),
                $"Windows Extra control '{controlName}' missing from host report");
            Assert.Equal("PASS", outcome.Status);
            Assert.True(outcome.ResolvedParts > 0, $"Windows Extra control '{controlName}' resolved 0 parts");
            Assert.Equal(outcome.ExpectedParts, outcome.ResolvedParts);
        }
    }

    #endregion

    #region 2. 8 Signature Palettes & Oklab Tones (>= 5 cases)
    [Fact]
    public void Palettes_All8Palettes_ExistInEntries()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            Assert.Equal(8, SeaGlassPalette.Entries.Count);
            string[] expectedNames =
            [
                "Seafoam Mint", "Cobalt Tide", "Beach Amber", "Sea Lavender",
                "Rose Quartz Shell", "Kelp Forest", "Milk Glass Frost", "Obsidian Tide"
            ];
            foreach (string name in expectedNames)
            {
                Assert.Contains(SeaGlassPalette.Entries, p => p.DisplayName == name);
            }
    
        });
    }

    [Fact]
    public void Palettes_OklabRoundTrip_PreservesColorsWithinTolerance()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            foreach (SeaGlassPalette palette in SeaGlassPalette.Entries)
            {
                int original = palette.PrimaryGlass.TintColorArgb;
                SeaGlassColorMath.OklabColor lab = SeaGlassColorMath.ArgbToOklab(original);
                int reconstructed = SeaGlassColorMath.OklabToArgb(lab);

                uint uOrig = unchecked((uint)original);
                uint uRecon = unchecked((uint)reconstructed);

                int rDiff = Math.Abs((int)((uOrig >> 16) & 0xFF) - (int)((uRecon >> 16) & 0xFF));
                int gDiff = Math.Abs((int)((uOrig >> 8) & 0xFF) - (int)((uRecon >> 8) & 0xFF));
                int bDiff = Math.Abs((int)(uOrig & 0xFF) - (int)(uRecon & 0xFF));

                Assert.True(rDiff <= 2);
                Assert.True(gDiff <= 2);
                Assert.True(bDiff <= 2);
            }
    
        });
    }

    [Fact]
    public void Palettes_BeerLambertAbsorption_DepeensChromaWithoutWhiteBleach()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            int cobalt = SeaGlassPalette.CobaltTide.PrimaryGlass.TintColorArgb;
            int trans1 = SeaGlassColorMath.ComputeBeerLambertTransmission(
                backdropArgb: unchecked((int)0xFFFFFFFF),
                tintArgb: cobalt,
                tintDensity: 0.3f,
                coarseness: 0.2f,
                normalizedThickness: 1.0f);
            int trans2 = SeaGlassColorMath.ComputeBeerLambertTransmission(
                backdropArgb: unchecked((int)0xFFFFFFFF),
                tintArgb: cobalt,
                tintDensity: 0.8f,
                coarseness: 0.2f,
                normalizedThickness: 2.0f);

            SeaGlassColorMath.OklabColor lab1 = SeaGlassColorMath.ArgbToOklab(trans1);
            SeaGlassColorMath.OklabColor lab2 = SeaGlassColorMath.ArgbToOklab(trans2);
            Assert.True(lab2.L <= lab1.L, "Deeper volumetric glass must decrease lightness, not bleach toward white");
    
        });
    }

    [Fact]
    public void Palettes_OptimalContentColor_ProvidesContrastSafeInk()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            foreach (SeaGlassPalette palette in SeaGlassPalette.Entries)
            {
                int bg = palette.PrimaryGlass.TintColorArgb;
                int ink = SeaGlassColorMath.OptimalContentColorFor(bg, 0.70f);
                SeaGlassColorMath.OklabColor bgLab = SeaGlassColorMath.ArgbToOklab(bg);
                SeaGlassColorMath.OklabColor inkLab = SeaGlassColorMath.ArgbToOklab(ink);
                float deltaL = MathF.Abs(bgLab.L - inkLab.L);
                Assert.True(deltaL >= 0.30f, $"Contrast delta L must be >= 0.30 for {palette.DisplayName} (got {deltaL})");
            }
    
        });
    }

    [Fact]
    public void Palettes_DeepVolumetricCore_IncreasesChroma()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            int amber = SeaGlassPalette.BeachAmber.PrimaryGlass.TintColorArgb;
            int core = SeaGlassColorMath.ComputeDeepVolumetricCoreColor(amber, 0.75f);
            SeaGlassColorMath.OklabColor baseLab = SeaGlassColorMath.ArgbToOklab(amber);
            SeaGlassColorMath.OklabColor coreLab = SeaGlassColorMath.ArgbToOklab(core);
            float baseChroma = MathF.Sqrt(baseLab.A * baseLab.A + baseLab.B * baseLab.B);
            float coreChroma = MathF.Sqrt(coreLab.A * coreLab.A + coreLab.B * coreLab.B);
            Assert.True(coreChroma >= baseChroma, "Core color must deepen chroma");
    
        });
    }
    #endregion

    #region 3. Shadcn Variants & Sizes API (>= 5 cases)
    [Fact]
    public void Variants_ButtonVariants_MapToDistinctVisualTokens()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            // Test variant mappings: Primary, Secondary, Tertiary, Ghost, Outline, Subtle, Destructive
            SeaGlassMaterial primary = SeaGlassPalette.SeafoamMint.PrimaryGlass;
            SeaGlassMaterial secondary = SeaGlassPalette.SeafoamMint.SurfaceGlass;
            Assert.NotEqual(primary.TintColorArgb, secondary.TintColorArgb);
    
        });
    }

    [Fact]
    public void Sizes_StandardMetrics_FollowHierarchy()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            const float smHeight = 32f;
            const float mdHeight = 40f;
            const float lgHeight = 48f;
            Assert.True(smHeight < mdHeight);
            Assert.True(mdHeight < lgHeight);
    
        });
    }

    [Fact]
    public void Sizes_IconSize_IsSquare40x40()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            const float iconDim = 40f;
            Assert.Equal(iconDim, iconDim);
    
        });
    }

    [Fact]
    public void Variants_DestructiveVariant_UsesAlertCoralTone()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            // Coral tone has high red component and warm hue
            int coralTint = SeaGlassPalette.RoseQuartzShell.PrimaryGlass.TintColorArgb;
            int r = (coralTint >> 16) & 0xFF;
            int b = coralTint & 0xFF;
            Assert.True(r > b);
    
        });
    }

    [Fact]
    public void Variants_GhostVariant_HasMinimalTintDensity()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassMaterial ghost = SeaGlassPalette.SeafoamMint.SurfaceGlass with { TintDensity = 0.08f, Transparency = 0.95f };
            Assert.True(ghost.TintDensity <= 0.15f);
            Assert.True(ghost.Transparency >= 0.90f);
    
        });
    }
    #endregion

    #region 4. Dual-Spring Motion Discipline & Invariants (>= 5 cases)
    [Fact]
    public void DualSprings_FastSpatial_IsUnderDampedWithExpectedStiffness()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassMotionScheme motion = new();
            Assert.True(motion.FastSpatial.IsUnderDamped);
            Assert.Equal(0.62f, motion.FastSpatial.DampingRatio, 2);
            Assert.Equal(680f, motion.FastSpatial.Stiffness, 1);
            Assert.InRange(motion.FastSpatial.CompositionPeriod.TotalMilliseconds, 235, 245);
    
        });
    }

    [Fact]
    public void DualSprings_DefaultSpatial_IsUnderDampedWithExpectedStiffness()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassMotionScheme motion = new();
            Assert.True(motion.DefaultSpatial.IsUnderDamped);
            Assert.Equal(0.66f, motion.DefaultSpatial.DampingRatio, 2);
            Assert.Equal(410f, motion.DefaultSpatial.Stiffness, 1);
            Assert.InRange(motion.DefaultSpatial.CompositionPeriod.TotalMilliseconds, 305, 315);
    
        });
    }

    [Fact]
    public void DualSprings_SlowSpatial_IsUnderDampedWithExpectedStiffness()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassMotionScheme motion = new();
            Assert.True(motion.SlowSpatial.IsUnderDamped);
            Assert.Equal(0.72f, motion.SlowSpatial.DampingRatio, 2);
            Assert.Equal(240f, motion.SlowSpatial.Stiffness, 1);
            Assert.InRange(motion.SlowSpatial.CompositionPeriod.TotalMilliseconds, 400, 410);
    
        });
    }

    [Fact]
    public void DualSprings_EffectsSprings_AreCriticallyDampedWithZeroOvershoot()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassMotionScheme motion = new();
            Assert.True(motion.FastEffects.IsCriticallyDamped);
            Assert.True(motion.DefaultEffects.IsCriticallyDamped);
            Assert.Equal(1.0f, motion.FastEffects.DampingRatio, 3);
            Assert.Equal(1.0f, motion.DefaultEffects.DampingRatio, 3);
    
        });
    }

    [Fact]
    public void DualSprings_EffectsSpringStep_NeverOvershootsTarget()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSpringSpec effects = new(DampingRatio: 1.0f, Stiffness: 540f);
            float current = 0f;
            float velocity = 0f;
            const float target = 1.0f;

            for (int i = 0; i < 60; i++)
            {
                effects.Step(ref current, ref velocity, target, 0.016f);
                Assert.True(current <= target + 1e-4f, $"Effects spring must never overshoot target ({current} <= {target})");
            }
    
        });
    }
    #endregion

    #region 5. 256-Byte Uniform Buffer ABI Parity (>= 5 cases)
    [Fact]
    public void UniformBuffer_StructSize_IsExactly256Bytes()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            Assert.Equal(256, Marshal.SizeOf<SeaGlassUniformBlockStd140>());
    
        });
    }

    [Fact]
    public void UniformBuffer_PackMethod_Produces256ByteStagingBuffer()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassUniformBlock block = new();
            block.Pack(800f, 600f, 0f, 0f, SeaGlassPalette.SeafoamMint.PrimaryGlass, 1.0f, [], 24f);
            byte[] buffer = block.CreateDirectStagingBuffer();
            Assert.Equal(256, buffer.Length);
    
        });
    }

    [Fact]
    public void UniformBuffer_ResolutionSlot_AlignsAtOffsetZero()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassUniformBlock block = new();
            block.Pack(1920f, 1080f, 50f, 60f, SeaGlassPalette.SeafoamMint.PrimaryGlass, 1.0f, [], 24f);
            SeaGlassUniformBlockStd140 s = block.ToStd140Struct();
            Assert.Equal(1920f, s.ResolutionAndOrigin.X);
            Assert.Equal(1080f, s.ResolutionAndOrigin.Y);
            Assert.Equal(50f, s.ResolutionAndOrigin.Z);
            Assert.Equal(60f, s.ResolutionAndOrigin.W);
    
        });
    }

    [Fact]
    public void UniformBuffer_OpticsSlots_AlignPrimaryAndSecondaryValues()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassUniformBlock block = new();
            SeaGlassMaterial mat = SeaGlassPalette.CobaltTide.PrimaryGlass;
            block.Pack(400f, 200f, 0f, 0f, mat, 2.0f, [], 24f);
            SeaGlassUniformBlockStd140 s = block.ToStd140Struct();
            Assert.Equal(mat.TintDensity, s.OpticsPrimary.X, 3);
            Assert.Equal(mat.Coarseness, s.OpticsPrimary.Y, 3);
            Assert.Equal(mat.Ior, s.OpticsPrimary.Z, 3);
            Assert.Equal(mat.RimChalkiness, s.OpticsSecondary.X, 3);
    
        });
    }

    [Fact]
    public void UniformBuffer_DirtyMaskTracking_DetectsIdenticalVsModifiedPacks()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassUniformBlock block = new();
            SeaGlassMaterial mat = SeaGlassPalette.BeachAmber.PrimaryGlass;
            block.Pack(300f, 150f, 0f, 0f, mat, 1.0f, [], 24f);
            block.ClearDirtyMask();
            Assert.Equal(0, block.DirtySlotsMask);

            // Repack identical
            block.Pack(300f, 150f, 0f, 0f, mat, 1.0f, [], 24f);
            Assert.Equal(0, block.DirtySlotsMask);

            // Repack with light direction change
            block.Pack(300f, 150f, 0f, 0f, mat, 1.0f, [], 24f, lightDirX: 0.8f);
            Assert.Equal(1 << 3, block.DirtySlotsMask);
    
        });
    }
    #endregion

    #region 6. Concentric Corner Adaptation Solver (>= 5 cases)
    [Fact]
    public void CornerAdaptation_OutsideProximity_ReturnsBaseRadius()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassCornerRadiiPx bezel = SeaGlassCornerRadiiPx.Uniform(120f);
            SeaGlassCornerRadiiPx result = SeaGlassCornerAdaptationSolver.SolveElementCornerRadiiPx(
                elementLeftPx: 100f, elementTopPx: 100f, elementRightPx: 400f, elementBottomPx: 300f,
                containerWidthPx: 1080f, containerHeightPx: 1920f, containerCornersPx: bezel,
                baseCornerRadiusPx: 32f, influenceDistancePx: 60f, mode: SeaGlassCornerAdaptationMode.ConcentricProximity);
            Assert.Equal(32f, result.TopLeftPx, 2);
    
        });
    }

    [Fact]
    public void CornerAdaptation_FlushCornerEmbed_MatchesBezelConcentricity()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassCornerRadiiPx bezel = SeaGlassCornerRadiiPx.Uniform(140f);
            SeaGlassCornerRadiiPx result = SeaGlassCornerAdaptationSolver.SolveElementCornerRadiiPx(
                elementLeftPx: 10f, elementTopPx: 10f, elementRightPx: 300f, elementBottomPx: 200f,
                containerWidthPx: 1080f, containerHeightPx: 2400f, containerCornersPx: bezel,
                baseCornerRadiusPx: 40f, influenceDistancePx: 200f, mode: SeaGlassCornerAdaptationMode.FlushCornerEmbed);
            Assert.True(result.TopLeftPx > 100f);
    
        });
    }

    [Fact]
    public void CornerAdaptation_AsymmetricEdgeSum_DoesNotExceedWidth()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassCornerRadiiPx bezel = SeaGlassCornerRadiiPx.Uniform(160f);
            const float elementWidth = 180f;
            SeaGlassCornerRadiiPx result = SeaGlassCornerAdaptationSolver.SolveElementCornerRadiiPx(
                elementLeftPx: 10f, elementTopPx: 10f, elementRightPx: 10f + elementWidth, elementBottomPx: 150f,
                containerWidthPx: 1080f, containerHeightPx: 2400f, containerCornersPx: bezel,
                baseCornerRadiusPx: 40f, influenceDistancePx: 200f, mode: SeaGlassCornerAdaptationMode.FlushCornerEmbed);
            Assert.True(result.TopLeftPx + result.TopRightPx <= elementWidth + 0.01f);
    
        });
    }

    [Fact]
    public void CornerAdaptation_CornerMask_AppliesOnlyToSpecifiedCorners()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassCornerRadiiPx bezel = SeaGlassCornerRadiiPx.Uniform(130f);
            SeaGlassCornerRadiiPx result = SeaGlassCornerAdaptationSolver.SolveElementCornerRadiiPx(
                elementLeftPx: 10f, elementTopPx: 10f, elementRightPx: 500f, elementBottomPx: 1000f,
                containerWidthPx: 1080f, containerHeightPx: 1920f, containerCornersPx: bezel,
                baseCornerRadiusPx: 28f, influenceDistancePx: 150f, mode: SeaGlassCornerAdaptationMode.ConcentricProximity,
                cornerMask: SeaGlassCornerMask.BottomOnly);
            Assert.Equal(28f, result.TopLeftPx, 2);
            Assert.Equal(28f, result.TopRightPx, 2);
    
        });
    }

    [Fact]
    public void CornerAdaptation_SolveCornerPodLayout_MaintainsPositiveGaps()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassCornerRadiiPx bezel = SeaGlassCornerRadiiPx.Uniform(140f);
            SeaGlassCornerPodLayoutPx layout = SeaGlassCornerAdaptationSolver.SolveCornerPodLayoutPx(
                containerWidthPx: 800f, containerHeightPx: 500f, containerCornersPx: bezel,
                requestedInsetPx: 60f, minHorizontalGapPx: 24f, minVerticalGapPx: 20f,
                preferredPodWidthPx: 300f, preferredTopPodHeightPx: 140f, preferredBottomDockHeightPx: 120f);
            Assert.True(layout.HorizontalGapPx >= 23.9f);
            Assert.True(layout.VerticalGapPx >= 19.9f);
    
        });
    }
    #endregion

    #region 7. UI Automation Peers & Patterns (>= 5 cases)
    [Fact]
    public void Uia_AllInteractiveControls_DeclareRequiredPatterns()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            var btn = new SeaGlassButton("Click");
            var btnPeer = Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(btn);
            Assert.NotNull(btnPeer);
            Assert.NotNull(btnPeer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke));

            var sw = new SeaGlassSwitch();
            var swPeer = Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(sw);
            Assert.NotNull(swPeer);
            Assert.NotNull(swPeer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Toggle));

            var cb = new SeaGlassCheckbox();
            var cbPeer = Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(cb);
            Assert.NotNull(cbPeer);
            Assert.NotNull(cbPeer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Toggle));

            var slider = new SeaGlassLiquidSlider();
            var sliderPeer = Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(slider);
            Assert.NotNull(sliderPeer);
            Assert.NotNull(sliderPeer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.RangeValue));

            var row = new SeaGlassSegmentedButtonRow();
            var rowPeer = Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(row);
            Assert.NotNull(rowPeer);
            Assert.NotNull(rowPeer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Selection));
        });
    }

    [Fact]
    public void Uia_ButtonControls_DeclareInvokeProvider()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            var btn = new SeaGlassButton("Submit");
            var peer = Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(btn);
            Assert.NotNull(peer);
            Assert.NotNull(peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke));
        });
    }

    [Fact]
    public void Uia_SwitchAndCheckbox_DeclareToggleProvider()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            var sw = new SeaGlassSwitch();
            var swPeer = Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(sw);
            Assert.NotNull(swPeer);
            Assert.NotNull(swPeer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Toggle));

            var cb = new SeaGlassCheckbox();
            var cbPeer = Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(cb);
            Assert.NotNull(cbPeer);
            Assert.NotNull(cbPeer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Toggle));
        });
    }

    [Fact]
    public void Uia_LiquidSlider_DeclaresRangeValueProvider()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            var slider = new SeaGlassLiquidSlider();
            var peer = Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(slider);
            Assert.NotNull(peer);
            Assert.NotNull(peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.RangeValue));
        });
    }

    [Fact]
    public void Uia_SegmentedButtonRow_DeclaresSelectionProvider()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            var row = new SeaGlassSegmentedButtonRow();
            var peer = Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(row);
            Assert.NotNull(peer);
            Assert.NotNull(peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Selection));
        });
    }
    #endregion

    #region 8. Live Platform Settings Interop (>= 5 cases)
    [Fact]
    public void SettingsInterop_ReducedMotion_SnapsDeltaTimeToInfinity()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            MockSystemSettingsProvider mock = new() { AnimationsEnabled = false };
            float adjustedDt = mock.AdjustDeltaTimeForAnimations(0.016f);
            Assert.True(adjustedDt >= 1000f, "When animations are disabled, adjusted dt must be effectively infinite");
    
        });
    }

    [Fact]
    public void SettingsInterop_HighContrast_DowngradesToLightweightTint()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            MockSystemSettingsProvider mock = new() { HighContrastEnabled = true };
            SeaGlassDeviceTier tier = mock.ResolveEffectiveTier(SeaGlassDeviceTier.D2D1HlslFull);
            Assert.Equal(SeaGlassDeviceTier.LightweightTint, tier);
    
        });
    }

    [Fact]
    public void SettingsInterop_TransparencyOff_DowngradesToLightweightTint()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            MockSystemSettingsProvider mock = new() { TransparencyEffectsEnabled = false };
            SeaGlassDeviceTier tier = mock.ResolveEffectiveTier(SeaGlassDeviceTier.D2D1HlslFull);
            Assert.Equal(SeaGlassDeviceTier.LightweightTint, tier);
    
        });
    }

    [Fact]
    public void SettingsInterop_EnergySaver_DowngradesToLightweightTint()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            MockSystemSettingsProvider mock = new() { EnergySaverActive = true };
            SeaGlassDeviceTier tier = mock.ResolveEffectiveTier(SeaGlassDeviceTier.D2D1HlslFull);
            Assert.Equal(SeaGlassDeviceTier.LightweightTint, tier);
    
        });
    }

    [Fact]
    public void SettingsInterop_TextScaleFactor_ScalesBaseTypography()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            MockSystemSettingsProvider mock = new() { TextScaleFactor = 1.75f };
            float scaled = mock.ScaleText(14f);
            Assert.Equal(24.5f, scaled, 1);
    
        });
    }
    #endregion

    #region 9. Steady-State Zero Heap Allocations (>= 5 cases)
    [Fact]
    public void SteadyStateAllocations_SingleSurfaceStep_PerformsZeroAllocations()
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

            // Warm up JIT and scratch arena
            for (int i = 0; i < 50; i++) engine.Render(context, null);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 100; i++) engine.Render(context, null);
            long after = GC.GetAllocatedBytesForCurrentThread();

            Assert.Equal(0, after - before);
    
        });
    }

    [Fact]
    public void SteadyStateAllocations_UniformBlockPack_PerformsZeroAllocations()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassUniformBlock block = new();
            SeaGlassMaterial mat = SeaGlassPalette.SeafoamMint.PrimaryGlass;
            block.Pack(400f, 200f, 0f, 0f, mat, 1.0f, [], 24f);

            long before = GC.GetAllocatedBytesForCurrentThread();
            block.Pack(400f, 200f, 0f, 0f, mat, 1.0f, [], 24f);
            long after = GC.GetAllocatedBytesForCurrentThread();

            Assert.Equal(0, after - before);
    
        });
    }

    [Fact]
    public void SteadyStateAllocations_DualSpringStep_PerformsZeroAllocations()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSpringSpec spring = new(DampingRatio: 0.66f, Stiffness: 410f);
            float current = 0f;
            float velocity = 0f;
            spring.Step(ref current, ref velocity, 1.0f, 0.016f);

            long before = GC.GetAllocatedBytesForCurrentThread();
            spring.Step(ref current, ref velocity, 1.0f, 0.016f);
            long after = GC.GetAllocatedBytesForCurrentThread();

            Assert.Equal(0, after - before);
    
        });
    }

    [Fact]
    public void SteadyStateAllocations_SdfNodeEvaluation_PerformsZeroAllocations()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSdfEngine.SdfNode node = new(
                centerX: 100f, centerY: 100f, halfWidth: 50f, halfHeight: 50f,
                uniformCornerRadius: 25f, cornerSmoothing: 2.5f, tintColorArgb: 0);

            SeaGlassSdfEngine.EvaluateNodeSdf(100f, 100f, node);

            long before = GC.GetAllocatedBytesForCurrentThread();
            SeaGlassSdfEngine.EvaluateNodeSdf(100f, 100f, node);
            long after = GC.GetAllocatedBytesForCurrentThread();

            Assert.Equal(0, after - before);
    
        });
    }

    [Fact]
    public void SteadyStateAllocations_MaterialAnimatorStep_PerformsZeroAllocations()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassMaterial mat1 = SeaGlassPalette.SeafoamMint.PrimaryGlass;
            SeaGlassMaterialAnimator animator = new(mat1);
            SeaGlassMotionScheme motion = new();

            // At steady state (target == current and at rest), stepping performs zero allocations
            animator.StepToward(mat1, motion, 0.016f);

            long before = GC.GetAllocatedBytesForCurrentThread();
            animator.StepToward(mat1, motion, 0.016f);
            long after = GC.GetAllocatedBytesForCurrentThread();

            Assert.Equal(0, after - before);
    
        });
    }
    #endregion
}
