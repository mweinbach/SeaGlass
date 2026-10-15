using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using SeaGlass.WinUI;

namespace SeaGlass.Tests.E2E.Infrastructure;

public enum ControlCategory
{
    FoundationsAndSurfaces,
    ButtonsAndChips,
    NavigationAndBars,
    InputsAndToggles,
    FeedbackAndLists,
    DynamicMorphingSuite,
    Ios27AndOverlays,
    WindowsNativeExtras
}

public sealed record ControlContractDescriptor(
    string Name,
    ControlCategory Category,
    string ExpectedTypeName,
    string[] ExpectedPatterns,
    bool IsWindowsExtra,
    string RequiredGalleryPage);

/// <summary>
/// Machine-checkable inventory of all 58 controls specified in ORIGINAL_REQUEST.md,
/// PROJECT.md, and spec_inventory.md (authoritative source for AC-6).
/// </summary>
public static class E2ECatalogContractRegistry
{
    public static readonly ReadOnlyCollection<ControlContractDescriptor> All58Controls = new(
    [
        // Group 1: Foundations & Surfaces (9)
        new("SeaGlassTheme", ControlCategory.FoundationsAndSurfaces, "SeaGlass.WinUI.SeaGlassTheme", [], false, "ThemePage"),
        new("SeaGlassStage", ControlCategory.FoundationsAndSurfaces, "SeaGlass.WinUI.SeaGlassStage", [], false, "StagePage"),
        new("SeaGlassFluidGroup", ControlCategory.FoundationsAndSurfaces, "SeaGlass.WinUI.SeaGlassFluidGroup", [], false, "FluidGroupPage"),
        new("SeaGlassSurface", ControlCategory.FoundationsAndSurfaces, "SeaGlass.WinUI.SeaGlassSurface", [], false, "SurfacePage"),
        new("SeaGlassCard", ControlCategory.FoundationsAndSurfaces, "SeaGlass.WinUI.SeaGlassCard", [], false, "CardPage"),
        new("SeaGlassDialogCard", ControlCategory.FoundationsAndSurfaces, "SeaGlass.WinUI.SeaGlassDialogCard", ["IWindowProvider"], false, "DialogCardPage"),
        new("SeaGlassBottomSheetSurface", ControlCategory.FoundationsAndSurfaces, "SeaGlass.WinUI.SeaGlassBottomSheetSurface", ["IWindowProvider"], false, "BottomSheetPage"),
        new("SeaGlassCornerEmbeddedPod", ControlCategory.FoundationsAndSurfaces, "SeaGlass.WinUI.SeaGlassCornerEmbeddedPod", [], false, "CornerEmbeddedPodPage"),
        new("SeaGlassAdaptiveCornerLabCard", ControlCategory.FoundationsAndSurfaces, "SeaGlass.WinUI.SeaGlassAdaptiveCornerLabCard", [], false, "AdaptiveCornerLabPage"),

        // Group 2: Buttons & Chips (8)
        new("SeaGlassButton", ControlCategory.ButtonsAndChips, "SeaGlass.WinUI.SeaGlassButton", ["IInvokeProvider"], false, "ButtonPage"),
        new("SeaGlassIconButton", ControlCategory.ButtonsAndChips, "SeaGlass.WinUI.SeaGlassIconButton", ["IInvokeProvider"], false, "IconButtonPage"),
        new("SeaGlassExtendedFab", ControlCategory.ButtonsAndChips, "SeaGlass.WinUI.SeaGlassExtendedFab", ["IInvokeProvider", "IExpandCollapseProvider"], false, "ExtendedFabPage"),
        new("SeaGlassPill", ControlCategory.ButtonsAndChips, "SeaGlass.WinUI.SeaGlassPill", ["IToggleProvider"], false, "PillPage"),
        new("SeaGlassFilterChip", ControlCategory.ButtonsAndChips, "SeaGlass.WinUI.SeaGlassFilterChip", ["IToggleProvider"], false, "FilterChipPage"),
        new("SeaGlassBadge", ControlCategory.ButtonsAndChips, "SeaGlass.WinUI.SeaGlassBadge", [], false, "BadgePage"),
        new("SeaGlassSegmentedButtonRow", ControlCategory.ButtonsAndChips, "SeaGlass.WinUI.SeaGlassSegmentedButtonRow", ["ISelectionProvider"], false, "SegmentedButtonRowPage"),
        new("SeaGlassPebbleOrb", ControlCategory.ButtonsAndChips, "SeaGlass.WinUI.SeaGlassPebbleOrb", ["IInvokeProvider"], false, "PebbleOrbPage"),

        // Group 3: Navigation & Bars (7)
        new("SeaGlassTopAppBar", ControlCategory.NavigationAndBars, "SeaGlass.WinUI.SeaGlassTopAppBar", [], false, "TopAppBarPage"),
        new("SeaGlassFluidBar", ControlCategory.NavigationAndBars, "SeaGlass.WinUI.SeaGlassFluidBar", ["ISelectionProvider"], false, "FluidBarPage"),
        new("SeaGlassMiniPlayerBar", ControlCategory.NavigationAndBars, "SeaGlass.WinUI.SeaGlassMiniPlayerBar", [], false, "MiniPlayerBarPage"),
        new("SeaGlassFloatingToolbar", ControlCategory.NavigationAndBars, "SeaGlass.WinUI.SeaGlassFloatingToolbar", ["ISelectionProvider"], false, "FloatingToolbarPage"),
        new("SeaGlassPillNavBar", ControlCategory.NavigationAndBars, "SeaGlass.WinUI.SeaGlassPillNavBar", ["ISelectionProvider"], false, "PillNavBarPage"),
        new("SeaGlassPromptBar", ControlCategory.NavigationAndBars, "SeaGlass.WinUI.SeaGlassPromptBar", ["IValueProvider"], false, "PromptBarPage"),
        new("SeaGlassSearchBar", ControlCategory.NavigationAndBars, "SeaGlass.WinUI.SeaGlassSearchBar", ["IValueProvider", "ITextProvider"], false, "SearchBarPage"),

        // Group 4: Inputs & Toggles (5)
        new("SeaGlassTextField", ControlCategory.InputsAndToggles, "SeaGlass.WinUI.SeaGlassTextField", ["IValueProvider", "ITextProvider"], false, "TextFieldPage"),
        new("SeaGlassSwitch", ControlCategory.InputsAndToggles, "SeaGlass.WinUI.SeaGlassSwitch", ["IToggleProvider"], false, "SwitchPage"),
        new("SeaGlassCheckbox", ControlCategory.InputsAndToggles, "SeaGlass.WinUI.SeaGlassCheckbox", ["IToggleProvider"], false, "CheckboxPage"),
        new("SeaGlassRadioButton", ControlCategory.InputsAndToggles, "SeaGlass.WinUI.SeaGlassRadioButton", ["ISelectionItemProvider"], false, "RadioButtonPage"),
        new("SeaGlassLiquidSlider", ControlCategory.InputsAndToggles, "SeaGlass.WinUI.SeaGlassLiquidSlider", ["IRangeValueProvider"], false, "LiquidSliderPage"),

        // Group 5: Feedback & Lists (5)
        new("SeaGlassLinearProgressIndicator", ControlCategory.FeedbackAndLists, "SeaGlass.WinUI.SeaGlassLinearProgressIndicator", ["IRangeValueProvider"], false, "LinearProgressPage"),
        new("SeaGlassCircularProgressIndicator", ControlCategory.FeedbackAndLists, "SeaGlass.WinUI.SeaGlassCircularProgressIndicator", ["IRangeValueProvider"], false, "CircularProgressPage"),
        new("SeaGlassListItem", ControlCategory.FeedbackAndLists, "SeaGlass.WinUI.SeaGlassListItem", ["IInvokeProvider"], false, "ListItemPage"),
        new("SeaGlassSnackbar", ControlCategory.FeedbackAndLists, "SeaGlass.WinUI.SeaGlassSnackbar", [], false, "SnackbarPage"),
        new("SeaGlassIcon", ControlCategory.FeedbackAndLists, "SeaGlass.WinUI.SeaGlassIcon", [], false, "IconPage"),

        // Group 6: Dynamic Morphing Suite (8)
        new("SeaGlassTwoElementMorph", ControlCategory.DynamicMorphingSuite, "SeaGlass.WinUI.SeaGlassTwoElementMorph", ["IExpandCollapseProvider"], false, "TwoElementMorphPage"),
        new("SeaGlassBuddingCluster", ControlCategory.DynamicMorphingSuite, "SeaGlass.WinUI.SeaGlassBuddingCluster", ["IExpandCollapseProvider"], false, "BuddingClusterPage"),
        new("SeaGlassMorphingButton", ControlCategory.DynamicMorphingSuite, "SeaGlass.WinUI.SeaGlassMorphingButton", ["IInvokeProvider"], false, "MorphingButtonPage"),
        new("SeaGlassChoreographedShapeOrb", ControlCategory.DynamicMorphingSuite, "SeaGlass.WinUI.SeaGlassChoreographedShapeOrb", [], false, "ChoreographedShapeOrbPage"),
        new("SeaGlassMultiShapeMorph", ControlCategory.DynamicMorphingSuite, "SeaGlass.WinUI.SeaGlassMultiShapeMorph", [], false, "MultiShapeMorphPage"),
        new("SeaGlassMorphingContextMenu", ControlCategory.DynamicMorphingSuite, "SeaGlass.WinUI.SeaGlassMorphingContextMenu", ["IExpandCollapseProvider"], false, "MorphingContextMenuPage"),
        new("SeaGlassCascadedModelMorph", ControlCategory.DynamicMorphingSuite, "SeaGlass.WinUI.SeaGlassCascadedModelMorph", [], false, "CascadedModelMorphPage"),
        new("SeaGlassContextRingMorph", ControlCategory.DynamicMorphingSuite, "SeaGlass.WinUI.SeaGlassContextRingMorph", ["IExpandCollapseProvider"], false, "ContextRingMorphPage"),

        // Group 7: iOS 27 & Advanced Overlays (8)
        new("SeaGlassFloatingSheet", ControlCategory.Ios27AndOverlays, "SeaGlass.WinUI.SeaGlassFloatingSheet", ["IWindowProvider"], false, "FloatingSheetPage"),
        new("SeaGlassContextMenuOverlay", ControlCategory.Ios27AndOverlays, "SeaGlass.WinUI.SeaGlassContextMenuOverlay", ["IWindowProvider"], false, "ContextMenuOverlayPage"),
        new("SeaGlassActionSheet", ControlCategory.Ios27AndOverlays, "SeaGlass.WinUI.SeaGlassActionSheet", ["IWindowProvider"], false, "ActionSheetPage"),
        new("SeaGlassLiveActivityPill", ControlCategory.Ios27AndOverlays, "SeaGlass.WinUI.SeaGlassLiveActivityPill", ["IExpandCollapseProvider"], false, "LiveActivityPillPage"),
        new("SeaGlassMagnifierScrubber", ControlCategory.Ios27AndOverlays, "SeaGlass.WinUI.SeaGlassMagnifierScrubber", ["IRangeValueProvider"], false, "MagnifierScrubberPage"),
        new("SeaGlassNotificationStack", ControlCategory.Ios27AndOverlays, "SeaGlass.WinUI.SeaGlassNotificationStack", ["IExpandCollapseProvider"], false, "NotificationStackPage"),
        new("SeaGlassWheelPicker", ControlCategory.Ios27AndOverlays, "SeaGlass.WinUI.SeaGlassWheelPicker", ["ISelectionProvider"], false, "WheelPickerPage"),
        new("SeaGlassControlCenterGrid", ControlCategory.Ios27AndOverlays, "SeaGlass.WinUI.SeaGlassControlCenterGrid", [], false, "ControlCenterGridPage"),

        // Group 8: Windows Native Extras (8)
        new("SeaGlassTitleBar", ControlCategory.WindowsNativeExtras, "SeaGlass.WinUI.SeaGlassTitleBar", [], true, "TitleBarPage"),
        new("SeaGlassNavigationView", ControlCategory.WindowsNativeExtras, "SeaGlass.WinUI.SeaGlassNavigationView", ["ISelectionProvider"], true, "NavigationViewPage"),
        new("SeaGlassTabView", ControlCategory.WindowsNativeExtras, "SeaGlass.WinUI.SeaGlassTabView", ["ISelectionProvider"], true, "TabViewPage"),
        new("SeaGlassCommandBar", ControlCategory.WindowsNativeExtras, "SeaGlass.WinUI.SeaGlassCommandBar", ["IExpandCollapseProvider"], true, "CommandBarPage"),
        new("SeaGlassInfoBar", ControlCategory.WindowsNativeExtras, "SeaGlass.WinUI.SeaGlassInfoBar", [], true, "InfoBarPage"),
        new("SeaGlassTeachingTip", ControlCategory.WindowsNativeExtras, "SeaGlass.WinUI.SeaGlassTeachingTip", ["IWindowProvider"], true, "TeachingTipPage"),
        new("SeaGlassFlyout", ControlCategory.WindowsNativeExtras, "SeaGlass.WinUI.SeaGlassFlyout", ["IWindowProvider"], true, "FlyoutPage"),
        new("SeaGlassToolTip", ControlCategory.WindowsNativeExtras, "SeaGlass.WinUI.SeaGlassToolTip", [], true, "ToolTipPage")
    ]);

    /// <summary>
    /// Checks if a specified control type exists in the WinUI assembly.
    /// </summary>
    public static Type? ProbeControlType(string fullTypeName)
    {
        Assembly winUiAssembly = typeof(SeaGlassSurface).Assembly;
        return winUiAssembly.GetType(fullTypeName);
    }
}
