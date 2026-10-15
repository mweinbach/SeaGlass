using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using SeaGlass.WinUI;
using SeaGlass.WinUI.Automation;
using SeaGlass.Tests.Controls;
using SeaGlass.Tests.Infrastructure;
using Xunit;

namespace SeaGlass.Tests.Accessibility;

public class CatalogInteractiveControlsAutomationPeerTests
{
    [Fact]
    public void AllInteractiveControls_ResolveGenuinePeers_WithCorrectControlTypeAndPatterns()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            // 1. Button
            var btn = new SeaGlassButton("Test Button");
            var btnPeer = FrameworkElementAutomationPeer.CreatePeerForElement(btn);
            Assert.NotNull(btnPeer);
            Assert.IsType<SeaGlassButtonAutomationPeer>(btnPeer);
            Assert.Equal(AutomationControlType.Button, btnPeer.GetAutomationControlType());
            Assert.False(string.IsNullOrWhiteSpace(btnPeer.GetName()));
            Assert.NotNull(btnPeer.GetPattern(PatternInterface.Invoke));

            // 2. ExtendedFab
            var fab = new SeaGlassExtendedFab { Label = "Add Item" };
            var fabPeer = FrameworkElementAutomationPeer.CreatePeerForElement(fab);
            Assert.NotNull(fabPeer);
            Assert.IsType<SeaGlassExtendedFabAutomationPeer>(fabPeer);
            Assert.Equal(AutomationControlType.Button, fabPeer.GetAutomationControlType());
            Assert.False(string.IsNullOrWhiteSpace(fabPeer.GetName()));
            Assert.NotNull(fabPeer.GetPattern(PatternInterface.Invoke));
            Assert.NotNull(fabPeer.GetPattern(PatternInterface.ExpandCollapse));

            // 3. Switch
            var sw = new SeaGlassSwitch { Content = "Wi-Fi" };
            var swPeer = FrameworkElementAutomationPeer.CreatePeerForElement(sw);
            Assert.NotNull(swPeer);
            Assert.IsType<SeaGlassSwitchAutomationPeer>(swPeer);
            Assert.Equal(AutomationControlType.CheckBox, swPeer.GetAutomationControlType());
            Assert.False(string.IsNullOrWhiteSpace(swPeer.GetName()));
            Assert.NotNull(swPeer.GetPattern(PatternInterface.Toggle));

            // 4. Checkbox
            var cb = new SeaGlassCheckbox { Content = "Accept Terms" };
            var cbPeer = FrameworkElementAutomationPeer.CreatePeerForElement(cb);
            Assert.NotNull(cbPeer);
            Assert.IsType<SeaGlassCheckboxAutomationPeer>(cbPeer);
            Assert.Equal(AutomationControlType.CheckBox, cbPeer.GetAutomationControlType());
            Assert.False(string.IsNullOrWhiteSpace(cbPeer.GetName()));
            Assert.NotNull(cbPeer.GetPattern(PatternInterface.Toggle));

            // 5. RadioButton (Must be RadioButton and ISelectionItemProvider, NOT IToggleProvider)
            var rb = new SeaGlassRadioButton { Content = "Option 1" };
            var rbPeer = FrameworkElementAutomationPeer.CreatePeerForElement(rb);
            Assert.NotNull(rbPeer);
            Assert.IsType<SeaGlassRadioButtonAutomationPeer>(rbPeer);
            Assert.Equal(AutomationControlType.RadioButton, rbPeer.GetAutomationControlType());
            Assert.False(string.IsNullOrWhiteSpace(rbPeer.GetName()));
            Assert.NotNull(rbPeer.GetPattern(PatternInterface.SelectionItem));
            Assert.Null(rbPeer.GetPattern(PatternInterface.Toggle));

            // 6. LiquidSlider
            var slider = new SeaGlassLiquidSlider { Title = "Volume" };
            var sliderPeer = FrameworkElementAutomationPeer.CreatePeerForElement(slider);
            Assert.NotNull(sliderPeer);
            Assert.IsType<SeaGlassLiquidSliderAutomationPeer>(sliderPeer);
            Assert.Equal(AutomationControlType.Slider, sliderPeer.GetAutomationControlType());
            Assert.False(string.IsNullOrWhiteSpace(sliderPeer.GetName()));
            Assert.NotNull(sliderPeer.GetPattern(PatternInterface.RangeValue));

            // 7. TextField
            var tf = new SeaGlassTextField { Label = "Username" };
            var tfPeer = FrameworkElementAutomationPeer.CreatePeerForElement(tf);
            Assert.NotNull(tfPeer);
            Assert.IsType<SeaGlassTextFieldAutomationPeer>(tfPeer);
            Assert.Equal(AutomationControlType.Edit, tfPeer.GetAutomationControlType());
            Assert.False(string.IsNullOrWhiteSpace(tfPeer.GetName()));
            Assert.NotNull(tfPeer.GetPattern(PatternInterface.Value));

            // 8. SearchBar
            var sb = new SeaGlassSearchBar { Placeholder = "Search files..." };
            var sbPeer = FrameworkElementAutomationPeer.CreatePeerForElement(sb);
            Assert.NotNull(sbPeer);
            Assert.IsType<SeaGlassSearchBarAutomationPeer>(sbPeer);
            Assert.Equal(AutomationControlType.Edit, sbPeer.GetAutomationControlType());
            Assert.False(string.IsNullOrWhiteSpace(sbPeer.GetName()));
            Assert.NotNull(sbPeer.GetPattern(PatternInterface.Value));

            // 9. FilterChip
            var chip = new SeaGlassFilterChip("Filter Tag", false);
            var chipPeer = FrameworkElementAutomationPeer.CreatePeerForElement(chip);
            Assert.NotNull(chipPeer);
            Assert.IsType<SeaGlassFilterChipAutomationPeer>(chipPeer);
            Assert.Equal(AutomationControlType.CheckBox, chipPeer.GetAutomationControlType());
            Assert.False(string.IsNullOrWhiteSpace(chipPeer.GetName()));
            Assert.NotNull(chipPeer.GetPattern(PatternInterface.Toggle));
            Assert.NotNull(chipPeer.GetPattern(PatternInterface.SelectionItem));

            // 10. Pill
            var pill = new SeaGlassPill("Active Pill", false);
            var pillPeer = FrameworkElementAutomationPeer.CreatePeerForElement(pill);
            Assert.NotNull(pillPeer);
            Assert.IsType<SeaGlassPillAutomationPeer>(pillPeer);
            Assert.Equal(AutomationControlType.CheckBox, pillPeer.GetAutomationControlType());
            Assert.False(string.IsNullOrWhiteSpace(pillPeer.GetName()));
            Assert.NotNull(pillPeer.GetPattern(PatternInterface.Toggle));
            Assert.NotNull(pillPeer.GetPattern(PatternInterface.SelectionItem));

            // 11. SegmentedButtonRow
            var row = new SeaGlassSegmentedButtonRow();
            var rowPeer = FrameworkElementAutomationPeer.CreatePeerForElement(row);
            Assert.NotNull(rowPeer);
            Assert.IsType<SeaGlassSegmentedButtonRowAutomationPeer>(rowPeer);
            Assert.Equal(AutomationControlType.List, rowPeer.GetAutomationControlType());
            Assert.NotNull(rowPeer.GetPattern(PatternInterface.Selection));

            // 12. SegmentedButtonItem
            var item = new SeaGlassSegmentedButtonItem("Option A", 0);
            var itemPeer = FrameworkElementAutomationPeer.CreatePeerForElement(item);
            Assert.NotNull(itemPeer);
            Assert.IsType<SeaGlassSegmentedButtonItemAutomationPeer>(itemPeer);
            Assert.Equal(AutomationControlType.ListItem, itemPeer.GetAutomationControlType());
            Assert.NotNull(itemPeer.GetPattern(PatternInterface.SelectionItem));
            Assert.NotNull(itemPeer.GetPattern(PatternInterface.Invoke));

            // 13. BottomSheetSurface
            var sheet = new SeaGlassBottomSheetSurface { Title = "Options Panel" };
            var sheetPeer = FrameworkElementAutomationPeer.CreatePeerForElement(sheet);
            Assert.NotNull(sheetPeer);
            Assert.IsType<SeaGlassBottomSheetSurfaceAutomationPeer>(sheetPeer);
            Assert.Equal(AutomationControlType.Pane, sheetPeer.GetAutomationControlType());
            Assert.NotNull(sheetPeer.GetPattern(PatternInterface.ExpandCollapse));

            // 14. FloatingSheet
            var floatSheet = new SeaGlassFloatingSheet();
            var floatSheetPeer = FrameworkElementAutomationPeer.CreatePeerForElement(floatSheet);
            Assert.NotNull(floatSheetPeer);
            Assert.IsType<SeaGlassFloatingSheetAutomationPeer>(floatSheetPeer);
            Assert.Equal(AutomationControlType.Pane, floatSheetPeer.GetAutomationControlType());
            Assert.NotNull(floatSheetPeer.GetPattern(PatternInterface.ExpandCollapse));

            // 15. WheelPicker
            var picker = new SeaGlassWheelPicker { Items = new[] { "Red", "Green", "Blue" } };
            var pickerPeer = FrameworkElementAutomationPeer.CreatePeerForElement(picker);
            Assert.NotNull(pickerPeer);
            Assert.IsType<SeaGlassWheelPickerAutomationPeer>(pickerPeer);
            Assert.Equal(AutomationControlType.List, pickerPeer.GetAutomationControlType());
            Assert.NotNull(pickerPeer.GetPattern(PatternInterface.Selection));
            Assert.NotNull(pickerPeer.GetPattern(PatternInterface.Value));

            // 16. MagnifierScrubber
            var scrubber = new SeaGlassMagnifierScrubber();
            var scrubberPeer = FrameworkElementAutomationPeer.CreatePeerForElement(scrubber);
            Assert.NotNull(scrubberPeer);
            Assert.IsType<SeaGlassMagnifierScrubberAutomationPeer>(scrubberPeer);
            Assert.Equal(AutomationControlType.Slider, scrubberPeer.GetAutomationControlType());
            Assert.NotNull(scrubberPeer.GetPattern(PatternInterface.RangeValue));

            // 17. ActionSheet
            var actionSheet = new SeaGlassActionSheet { Title = "Share Via" };
            var actionSheetPeer = FrameworkElementAutomationPeer.CreatePeerForElement(actionSheet);
            Assert.NotNull(actionSheetPeer);
            Assert.IsType<SeaGlassActionSheetAutomationPeer>(actionSheetPeer);
            Assert.Equal(AutomationControlType.Pane, actionSheetPeer.GetAutomationControlType());
            Assert.NotNull(actionSheetPeer.GetPattern(PatternInterface.ExpandCollapse));

            // 18. ContextMenuOverlay
            var ctxOverlay = new SeaGlassContextMenuOverlay();
            var ctxOverlayPeer = FrameworkElementAutomationPeer.CreatePeerForElement(ctxOverlay);
            Assert.NotNull(ctxOverlayPeer);
            Assert.IsType<SeaGlassContextMenuOverlayAutomationPeer>(ctxOverlayPeer);
            Assert.Equal(AutomationControlType.Menu, ctxOverlayPeer.GetAutomationControlType());
            Assert.NotNull(ctxOverlayPeer.GetPattern(PatternInterface.ExpandCollapse));

            // 19. TeachingTip
            var tip = new SeaGlassTeachingTip { Title = "Tip Title" };
            var tipPeer = FrameworkElementAutomationPeer.CreatePeerForElement(tip);
            Assert.NotNull(tipPeer);
            Assert.IsType<SeaGlassTeachingTipAutomationPeer>(tipPeer);
            Assert.Equal(AutomationControlType.ToolTip, tipPeer.GetAutomationControlType());
            Assert.NotNull(tipPeer.GetPattern(PatternInterface.Window));

            // 20. CommandBar
            var cmdBar = new SeaGlassCommandBar();
            var cmdBarPeer = FrameworkElementAutomationPeer.CreatePeerForElement(cmdBar);
            Assert.NotNull(cmdBarPeer);
            Assert.IsType<SeaGlassCommandBarAutomationPeer>(cmdBarPeer);
            Assert.Equal(AutomationControlType.ToolBar, cmdBarPeer.GetAutomationControlType());
            Assert.NotNull(cmdBarPeer.GetPattern(PatternInterface.ExpandCollapse));

            // 21. MorphingButton
            var morphBtn = new SeaGlassMorphingButton { ButtonLabel = "Morph" };
            var morphBtnPeer = FrameworkElementAutomationPeer.CreatePeerForElement(morphBtn);
            Assert.NotNull(morphBtnPeer);
            Assert.IsType<SeaGlassMorphingButtonAutomationPeer>(morphBtnPeer);
            Assert.Equal(AutomationControlType.Button, morphBtnPeer.GetAutomationControlType());
            Assert.NotNull(morphBtnPeer.GetPattern(PatternInterface.Invoke));

            // 22. TwoElementMorph
            var morph = new SeaGlassTwoElementMorph();
            var morphPeer = FrameworkElementAutomationPeer.CreatePeerForElement(morph);
            Assert.NotNull(morphPeer);
            Assert.IsType<SeaGlassTwoElementMorphAutomationPeer>(morphPeer);
            Assert.Equal(AutomationControlType.Button, morphPeer.GetAutomationControlType());
            Assert.NotNull(morphPeer.GetPattern(PatternInterface.ExpandCollapse));
            Assert.NotNull(morphPeer.GetPattern(PatternInterface.Toggle));

            // 23. BuddingCluster
            var cluster = new SeaGlassBuddingCluster();
            var clusterPeer = FrameworkElementAutomationPeer.CreatePeerForElement(cluster);
            Assert.NotNull(clusterPeer);
            Assert.IsType<SeaGlassBuddingClusterAutomationPeer>(clusterPeer);
            Assert.Equal(AutomationControlType.Button, clusterPeer.GetAutomationControlType());
            Assert.NotNull(clusterPeer.GetPattern(PatternInterface.ExpandCollapse));
            Assert.NotNull(clusterPeer.GetPattern(PatternInterface.Invoke));

            // 24. MultiShapeMorph
            var multiShape = new SeaGlassMultiShapeMorph();
            var multiShapePeer = FrameworkElementAutomationPeer.CreatePeerForElement(multiShape);
            Assert.NotNull(multiShapePeer);
            Assert.IsType<SeaGlassMultiShapeMorphAutomationPeer>(multiShapePeer);
            Assert.Equal(AutomationControlType.Slider, multiShapePeer.GetAutomationControlType());
            Assert.NotNull(multiShapePeer.GetPattern(PatternInterface.RangeValue));

            // 25. ChoreographedShapeOrb
            var orb = new SeaGlassChoreographedShapeOrb();
            var orbPeer = FrameworkElementAutomationPeer.CreatePeerForElement(orb);
            Assert.NotNull(orbPeer);
            Assert.IsType<SeaGlassChoreographedShapeOrbAutomationPeer>(orbPeer);
            Assert.Equal(AutomationControlType.Button, orbPeer.GetAutomationControlType());
            Assert.NotNull(orbPeer.GetPattern(PatternInterface.Invoke));

            // 26. CascadedModelMorph
            var cascaded = new SeaGlassCascadedModelMorph();
            var cascadedPeer = FrameworkElementAutomationPeer.CreatePeerForElement(cascaded);
            Assert.NotNull(cascadedPeer);
            Assert.IsType<SeaGlassCascadedModelMorphAutomationPeer>(cascadedPeer);
            Assert.Equal(AutomationControlType.Button, cascadedPeer.GetAutomationControlType());
            Assert.NotNull(cascadedPeer.GetPattern(PatternInterface.Invoke));

            // 27. LinearProgressIndicator
            var linProg = new SeaGlassLinearProgressIndicator();
            var linProgPeer = FrameworkElementAutomationPeer.CreatePeerForElement(linProg);
            Assert.NotNull(linProgPeer);
            Assert.IsType<SeaGlassLinearProgressIndicatorAutomationPeer>(linProgPeer);
            Assert.Equal(AutomationControlType.ProgressBar, linProgPeer.GetAutomationControlType());
            Assert.NotNull(linProgPeer.GetPattern(PatternInterface.RangeValue));

            // 28. CircularProgressIndicator
            var circProg = new SeaGlassCircularProgressIndicator();
            var circProgPeer = FrameworkElementAutomationPeer.CreatePeerForElement(circProg);
            Assert.NotNull(circProgPeer);
            Assert.IsType<SeaGlassCircularProgressIndicatorAutomationPeer>(circProgPeer);
            Assert.Equal(AutomationControlType.ProgressBar, circProgPeer.GetAutomationControlType());
            Assert.NotNull(circProgPeer.GetPattern(PatternInterface.RangeValue));

            // 29. ListItem
            var li = new SeaGlassListItem("Item Headline");
            var liPeer = FrameworkElementAutomationPeer.CreatePeerForElement(li);
            Assert.NotNull(liPeer);
            Assert.IsType<SeaGlassListItemAutomationPeer>(liPeer);
            Assert.Equal(AutomationControlType.ListItem, liPeer.GetAutomationControlType());
            Assert.NotNull(liPeer.GetPattern(PatternInterface.Invoke));
            Assert.NotNull(liPeer.GetPattern(PatternInterface.SelectionItem));

            // 30. Snackbar
            var snack = new SeaGlassSnackbar("Action Done");
            var snackPeer = FrameworkElementAutomationPeer.CreatePeerForElement(snack);
            Assert.NotNull(snackPeer);
            Assert.IsType<SeaGlassSnackbarAutomationPeer>(snackPeer);
            Assert.Equal(AutomationControlType.StatusBar, snackPeer.GetAutomationControlType());
            Assert.NotNull(snackPeer.GetPattern(PatternInterface.Invoke));

            // 31. FluidBar
            var fluidBar = new SeaGlassFluidBar();
            var fluidBarPeer = FrameworkElementAutomationPeer.CreatePeerForElement(fluidBar);
            Assert.NotNull(fluidBarPeer);
            Assert.IsType<SeaGlassFluidBarAutomationPeer>(fluidBarPeer);
            Assert.Equal(AutomationControlType.ToolBar, fluidBarPeer.GetAutomationControlType());
            Assert.NotNull(fluidBarPeer.GetPattern(PatternInterface.Selection));

            // 32. MiniPlayerBar
            var miniPlayer = new SeaGlassMiniPlayerBar();
            var miniPlayerPeer = FrameworkElementAutomationPeer.CreatePeerForElement(miniPlayer);
            Assert.NotNull(miniPlayerPeer);
            Assert.IsType<SeaGlassMiniPlayerBarAutomationPeer>(miniPlayerPeer);
            Assert.Equal(AutomationControlType.ToolBar, miniPlayerPeer.GetAutomationControlType());
            Assert.NotNull(miniPlayerPeer.GetPattern(PatternInterface.Invoke));

            // 33. FloatingToolbar
            var floatToolbar = new SeaGlassFloatingToolbar();
            var floatToolbarPeer = FrameworkElementAutomationPeer.CreatePeerForElement(floatToolbar);
            Assert.NotNull(floatToolbarPeer);
            Assert.IsType<SeaGlassFloatingToolbarAutomationPeer>(floatToolbarPeer);
            Assert.Equal(AutomationControlType.ToolBar, floatToolbarPeer.GetAutomationControlType());
            Assert.NotNull(floatToolbarPeer.GetPattern(PatternInterface.Selection));
            Assert.NotNull(floatToolbarPeer.GetPattern(PatternInterface.Invoke));

            // 34. PillNavBar
            var pillNav = new SeaGlassPillNavBar();
            var pillNavPeer = FrameworkElementAutomationPeer.CreatePeerForElement(pillNav);
            Assert.NotNull(pillNavPeer);
            Assert.IsType<SeaGlassPillNavBarAutomationPeer>(pillNavPeer);
            Assert.Equal(AutomationControlType.ToolBar, pillNavPeer.GetAutomationControlType());
            Assert.NotNull(pillNavPeer.GetPattern(PatternInterface.Selection));

            // 35. PromptBar
            var prompt = new SeaGlassPromptBar();
            var promptPeer = FrameworkElementAutomationPeer.CreatePeerForElement(prompt);
            Assert.NotNull(promptPeer);
            Assert.IsType<SeaGlassPromptBarAutomationPeer>(promptPeer);
            Assert.Equal(AutomationControlType.Edit, promptPeer.GetAutomationControlType());
            Assert.NotNull(promptPeer.GetPattern(PatternInterface.Value));

            // 36. NavigationView
            var nav = new SeaGlassNavigationView();
            var navPeer = FrameworkElementAutomationPeer.CreatePeerForElement(nav);
            Assert.NotNull(navPeer);
            Assert.IsType<SeaGlassNavigationViewAutomationPeer>(navPeer);
            Assert.Equal(AutomationControlType.Menu, navPeer.GetAutomationControlType());
            Assert.NotNull(navPeer.GetPattern(PatternInterface.Selection));

            // 37. TabView
            var tab = new SeaGlassTabView();
            var tabPeer = FrameworkElementAutomationPeer.CreatePeerForElement(tab);
            Assert.NotNull(tabPeer);
            Assert.IsType<SeaGlassTabViewAutomationPeer>(tabPeer);
            Assert.Equal(AutomationControlType.Tab, tabPeer.GetAutomationControlType());
            Assert.NotNull(tabPeer.GetPattern(PatternInterface.Selection));
        });
    }

    [Fact]
    public void Button_DoesNotImplementTogglePattern_CanaryValidation()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            var btn = new SeaGlassButton("Canary");
            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(btn);
            Assert.NotNull(peer);
            // Canary test: SeaGlassButton must only implement Invoke, not Toggle!
            Assert.Null(peer.GetPattern(PatternInterface.Toggle));
        });
    }
}
