using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;
using SeaGlass.Sample.Common;
using SeaGlass.WinUI;

namespace SeaGlass.Sample.Catalog;

/// <summary>
/// Authoritative catalog registry registering all 58 SeaGlass controls
/// with interactive preview factories, action triggers, and metadata.
/// </summary>
public static class ControlShowcaseCatalog
{
    private static readonly List<ControlShowcaseDescriptor> _descriptors = new();

    static ControlShowcaseCatalog()
    {
        RegisterAllControls();
    }

    public static IReadOnlyList<ControlShowcaseDescriptor> AllDescriptors => _descriptors;

    public static ControlShowcaseDescriptor? FindByName(string name) =>
        _descriptors.FirstOrDefault(d => string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase) ||
                                         string.Equals(d.RequiredPageName, name, StringComparison.OrdinalIgnoreCase));

    private static void ApplyDefaultTuning(FrameworkElement element, TuningViewModel tuning)
    {
        if (element is SeaGlassSurface surface)
        {
            surface.Coarseness = tuning.Coarseness;
            surface.ThicknessDp = tuning.ThicknessDp;
            surface.Ior = tuning.Ior;
            surface.RimChalkiness = tuning.RimChalkiness;
            surface.TintDensity = tuning.TintDensity;
            surface.Tone = tuning.Tone;
            surface.Variant = tuning.Variant;
            surface.Size = tuning.Size;
        }

        SeaGlassTheme.SetCoarseness(element, tuning.Coarseness);
        SeaGlassTheme.SetThicknessDp(element, tuning.ThicknessDp);
        SeaGlassTheme.SetTintDensity(element, tuning.TintDensity);
        SeaGlassTheme.SetTone(element, tuning.Tone);
        SeaGlassTheme.SetVariant(element, tuning.Variant);
        SeaGlassTheme.SetSize(element, tuning.Size);
    }

    private static void RegisterAllControls()
    {
        // ==========================================
        // Group 1: Foundations & Surfaces (9)
        // ==========================================
        _descriptors.Add(new(
            Name: "SeaGlassTheme",
            Category: CategoryDefinitions.Surfaces,
            Description: "Universal theming engine managing M3 Expressive tokens, Oklab color palettes, and dual-spring motion physics.",
            TypeName: "SeaGlass.WinUI.SeaGlassTheme",
            RequiredPageName: "ThemePage",
            PreviewFactory: () => new SeaGlassCard
            {
                Title = "SeaGlass Theme Foundation",
                Subtitle = "Material 3 Expressive & Sanded Glass Optics",
                Content = new TextBlock { Text = "Manages 8 signature sea-glass palettes and spring dynamics across all controls." }
            },
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassStage",
            Category: CategoryDefinitions.Surfaces,
            Description: "Zero-copy backdrop capture stage host isolating background visuals for high-performance refraction shaders.",
            TypeName: "SeaGlass.WinUI.SeaGlassStage",
            RequiredPageName: "StagePage",
            PreviewFactory: () => new SeaGlassStage
            {
                BackdropContent = new Grid
                {
                    Background = new Microsoft.UI.Xaml.Media.LinearGradientBrush
                    {
                        StartPoint = new Windows.Foundation.Point(0, 0),
                        EndPoint = new Windows.Foundation.Point(1, 1),
                        GradientStops =
                        {
                            new Microsoft.UI.Xaml.Media.GradientStop { Color = Windows.UI.Color.FromArgb(255, 13, 30, 45), Offset = 0.0 },
                            new Microsoft.UI.Xaml.Media.GradientStop { Color = Windows.UI.Color.FromArgb(255, 44, 126, 131), Offset = 1.0 }
                        }
                    }
                },
                Content = new SeaGlassCard
                {
                    Title = "Stage Backdrop Host",
                    Subtitle = "Zero-copy optical capture",
                    Content = new TextBlock { Text = "Isolates backdrop rendering into a cached visual surface." }
                }
            },
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassFluidGroup",
            Category: CategoryDefinitions.Surfaces,
            Description: "2D Signed Distance Field (SDF) multi-node coalescing canvas creating fluid tidal droplet bridges between surfaces.",
            TypeName: "SeaGlass.WinUI.SeaGlassFluidGroup",
            RequiredPageName: "FluidGroupPage",
            PreviewFactory: () => new SeaGlassFluidGroup(),
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassSurface",
            Category: CategoryDefinitions.Surfaces,
            Description: "Core volumetric sea-glass container with Beer-Lambert absorption, micro-facet coarseness, and rough-Fresnel rim.",
            TypeName: "SeaGlass.WinUI.SeaGlassSurface",
            RequiredPageName: "SurfacePage",
            PreviewFactory: () => new SeaGlassSurface
            {
                Width = 260,
                Height = 140,
                Content = new TextBlock
                {
                    Text = "Volumetric Sea Glass Surface",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            },
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassCard",
            Category: CategoryDefinitions.Surfaces,
            Description: "Squircle container with header, subtitle, rich body content, and tactile 3D hover response.",
            TypeName: "SeaGlass.WinUI.SeaGlassCard",
            RequiredPageName: "CardPage",
            PreviewFactory: () => new SeaGlassCard
            {
                Width = 360,
                MinHeight = 160,
                Title = "Ocean Wave Diagnostics",
                Subtitle = "Pacific Tidal Sensor Station #42",
                Content = new TextBlock { Text = "Real-time salinity: 34.8 PSU\nWave height: 1.84m\nWater temperature: 14.2°C" }
            },
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassDialogCard",
            Category: CategoryDefinitions.Surfaces,
            Description: "Modal dialog card with glyph icon, header, explanatory message, and Confirm/Dismiss action triggers.",
            TypeName: "SeaGlass.WinUI.SeaGlassDialogCard",
            RequiredPageName: "DialogCardPage",
            PreviewFactory: () => new SeaGlassDialogCard
            {
                Width = 400,
                MinHeight = 200,
                Title = "Apply Optical Preset?",
                Body = "This will update all surface coarseness and refraction parameters to Cobalt Tide preset."
            },
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassBottomSheetSurface",
            Category: CategoryDefinitions.Surfaces,
            Description: "Frosted glass bottom sheet drawer with grab handle and smooth spring slide-up presentation.",
            TypeName: "SeaGlass.WinUI.SeaGlassBottomSheetSurface",
            RequiredPageName: "BottomSheetPage",
            PreviewFactory: () => new SeaGlassBottomSheetSurface
            {
                Width = 400,
                Height = 240
            },
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassCornerEmbeddedPod",
            Category: CategoryDefinitions.Surfaces,
            Description: "Flush-embedded corner pod with hardware bezel matching for top-left, top-right, or bottom screen edges.",
            TypeName: "SeaGlass.WinUI.SeaGlassCornerEmbeddedPod",
            RequiredPageName: "CornerEmbeddedPodPage",
            PreviewFactory: () => new SeaGlassCornerEmbeddedPod(),
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassAdaptiveCornerLabCard",
            Category: CategoryDefinitions.Surfaces,
            Description: "Adaptive corner lab card demonstrating concentric radius alignment with physical device hardware display corners.",
            TypeName: "SeaGlass.WinUI.SeaGlassAdaptiveCornerLabCard",
            RequiredPageName: "AdaptiveCornerLabPage",
            PreviewFactory: () => new SeaGlassAdaptiveCornerLabCard(),
            ConfigurePreview: ApplyDefaultTuning));

        // ==========================================
        // Group 2: Buttons & Chips (8)
        // ==========================================
        _descriptors.Add(new(
            Name: "SeaGlassButton",
            Category: CategoryDefinitions.ButtonsAndChips,
            Description: "Primary tactile button with 3D press squish, intaglio focus ring, and dual-spring mechanical physics.",
            TypeName: "SeaGlass.WinUI.SeaGlassButton",
            RequiredPageName: "ButtonPage",
            PreviewFactory: () => new SeaGlassButton { Label = "Tactile Button" },
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassIconButton",
            Category: CategoryDefinitions.ButtonsAndChips,
            Description: "Circular or squircle icon button hosting vector glyphs with frosted glass refraction and hover glow.",
            TypeName: "SeaGlass.WinUI.SeaGlassIconButton",
            RequiredPageName: "IconButtonPage",
            PreviewFactory: () => new SeaGlassIconButton(),
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassExtendedFab",
            Category: CategoryDefinitions.ButtonsAndChips,
            Description: "Expanding floating action button with icon and text label that animates between compact and extended states.",
            TypeName: "SeaGlass.WinUI.SeaGlassExtendedFab",
            RequiredPageName: "ExtendedFabPage",
            PreviewFactory: () => new SeaGlassExtendedFab { Label = "New Ocean Capture" },
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassPill",
            Category: CategoryDefinitions.ButtonsAndChips,
            Description: "Compact interactive pill badge for status flags, tags, and toggles with smooth selection transitions.",
            TypeName: "SeaGlass.WinUI.SeaGlassPill",
            RequiredPageName: "PillPage",
            PreviewFactory: () => new SeaGlassPill("Active Filter", true),
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassFilterChip",
            Category: CategoryDefinitions.ButtonsAndChips,
            Description: "Filter chip with optional checkmark indicator and interactive toggle states.",
            TypeName: "SeaGlass.WinUI.SeaGlassFilterChip",
            RequiredPageName: "FilterChipPage",
            PreviewFactory: () => new SeaGlassFilterChip("Sanded Texture", true),
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassBadge",
            Category: CategoryDefinitions.ButtonsAndChips,
            Description: "Micro notification badge with counter or dot variant styled with volumetric sea-glass hues.",
            TypeName: "SeaGlass.WinUI.SeaGlassBadge",
            RequiredPageName: "BadgePage",
            PreviewFactory: () => new SeaGlassBadge { Count = 12 },
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassSegmentedButtonRow",
            Category: CategoryDefinitions.ButtonsAndChips,
            Description: "Multi-segment selector row with sliding liquid glass highlight lozenge.",
            TypeName: "SeaGlass.WinUI.SeaGlassSegmentedButtonRow",
            RequiredPageName: "SegmentedButtonRowPage",
            PreviewFactory: () => new SeaGlassSegmentedButtonRow(),
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassPebbleOrb",
            Category: CategoryDefinitions.ButtonsAndChips,
            Description: "Floating circular pebble medallion button with gyroscopic tilt reflection and haptic press feedback.",
            TypeName: "SeaGlass.WinUI.SeaGlassPebbleOrb",
            RequiredPageName: "PebbleOrbPage",
            PreviewFactory: () => new SeaGlassPebbleOrb(),
            ConfigurePreview: ApplyDefaultTuning));

        // ==========================================
        // Group 3: Navigation & Bars (7)
        // ==========================================
        _descriptors.Add(new(
            Name: "SeaGlassTopAppBar",
            Category: CategoryDefinitions.NavigationAndBars,
            Description: "App header bar with navigation back button, title, subtitle, and action icons.",
            TypeName: "SeaGlass.WinUI.SeaGlassTopAppBar",
            RequiredPageName: "TopAppBarPage",
            PreviewFactory: () => new SeaGlassTopAppBar { Title = "SeaGlass Gallery", Subtitle = "WinUI 3 Component Showcase" },
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassFluidBar",
            Category: CategoryDefinitions.NavigationAndBars,
            Description: "Floating bottom navigation bar with coalescing SDF active tab indicator.",
            TypeName: "SeaGlass.WinUI.SeaGlassFluidBar",
            RequiredPageName: "FluidBarPage",
            PreviewFactory: () => new SeaGlassFluidBar(),
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassMiniPlayerBar",
            Category: CategoryDefinitions.NavigationAndBars,
            Description: "Compact media playback bar with Play/Pause toggle, Next track trigger, and track metadata.",
            TypeName: "SeaGlass.WinUI.SeaGlassMiniPlayerBar",
            RequiredPageName: "MiniPlayerBarPage",
            PreviewFactory: () => new SeaGlassMiniPlayerBar(),
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassFloatingToolbar",
            Category: CategoryDefinitions.NavigationAndBars,
            Description: "Detached pill-shaped floating action toolbar with action items and hover refraction.",
            TypeName: "SeaGlass.WinUI.SeaGlassFloatingToolbar",
            RequiredPageName: "FloatingToolbarPage",
            PreviewFactory: () => new SeaGlassFloatingToolbar(),
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassPillNavBar",
            Category: CategoryDefinitions.NavigationAndBars,
            Description: "Pill navigation bar with items and animated selection lozenge.",
            TypeName: "SeaGlass.WinUI.SeaGlassPillNavBar",
            RequiredPageName: "PillNavBarPage",
            PreviewFactory: () => new SeaGlassPillNavBar(),
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassPromptBar",
            Category: CategoryDefinitions.NavigationAndBars,
            Description: "Interactive conversational input prompt bar with frosted glass text box and Send trigger.",
            TypeName: "SeaGlass.WinUI.SeaGlassPromptBar",
            RequiredPageName: "PromptBarPage",
            PreviewFactory: () => new SeaGlassPromptBar(),
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassSearchBar",
            Category: CategoryDefinitions.NavigationAndBars,
            Description: "Frosted search input with search icon, clear button, and text change events.",
            TypeName: "SeaGlass.WinUI.SeaGlassSearchBar",
            RequiredPageName: "SearchBarPage",
            PreviewFactory: () => new SeaGlassSearchBar(),
            ConfigurePreview: ApplyDefaultTuning));

        // ==========================================
        // Group 4: Inputs & Toggles (5)
        // ==========================================
        _descriptors.Add(new(
            Name: "SeaGlassTextField",
            Category: CategoryDefinitions.InputsAndToggles,
            Description: "Text input field with placeholder and intaglio focus glow.",
            TypeName: "SeaGlass.WinUI.SeaGlassTextField",
            RequiredPageName: "TextFieldPage",
            PreviewFactory: () => new SeaGlassTextField { Placeholder = "Enter search keyword..." },
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassSwitch",
            Category: CategoryDefinitions.InputsAndToggles,
            Description: "Tactile toggle switch with sliding glass lens thumb and live On/Off label.",
            TypeName: "SeaGlass.WinUI.SeaGlassSwitch",
            RequiredPageName: "SwitchPage",
            PreviewFactory: () => new SeaGlassSwitch { IsOn = true },
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassCheckbox",
            Category: CategoryDefinitions.InputsAndToggles,
            Description: "Interactive checkbox with smooth checkmark reveal and squircle frame.",
            TypeName: "SeaGlass.WinUI.SeaGlassCheckbox",
            RequiredPageName: "CheckboxPage",
            PreviewFactory: () => new SeaGlassCheckbox { Content = "Enable Caustic Scattering", IsChecked = true },
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassRadioButton",
            Category: CategoryDefinitions.InputsAndToggles,
            Description: "Mutually exclusive radio toggle group with glass dot expansion.",
            TypeName: "SeaGlass.WinUI.SeaGlassRadioButton",
            RequiredPageName: "RadioButtonPage",
            PreviewFactory: () => new SeaGlassRadioButton { Content = "Beer-Lambert Oklab", IsChecked = true },
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassLiquidSlider",
            Category: CategoryDefinitions.InputsAndToggles,
            Description: "Continuous range slider with elastic stretching thumb and value display.",
            TypeName: "SeaGlass.WinUI.SeaGlassLiquidSlider",
            RequiredPageName: "LiquidSliderPage",
            PreviewFactory: () => new SeaGlassLiquidSlider { Title = "Surface Coarseness", Value = 42.0 },
            ConfigurePreview: ApplyDefaultTuning));

        // ==========================================
        // Group 5: Feedback & Lists (5)
        // ==========================================
        _descriptors.Add(new(
            Name: "SeaGlassLinearProgressIndicator",
            Category: CategoryDefinitions.FeedbackAndProgress,
            Description: "Determinate progress slider (0–100%) plus indeterminate mode toggle.",
            TypeName: "SeaGlass.WinUI.SeaGlassLinearProgressIndicator",
            RequiredPageName: "LinearProgressPage",
            PreviewFactory: () => new SeaGlassLinearProgressIndicator { Progress = 0.68 },
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassCircularProgressIndicator",
            Category: CategoryDefinitions.FeedbackAndProgress,
            Description: "Determinate progress arc plus indeterminate spinner toggle.",
            TypeName: "SeaGlass.WinUI.SeaGlassCircularProgressIndicator",
            RequiredPageName: "CircularProgressPage",
            PreviewFactory: () => new SeaGlassCircularProgressIndicator { Progress = 0.82 },
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassListItem",
            Category: CategoryDefinitions.FeedbackAndProgress,
            Description: "Interactive list item with hover tint, leading icon, and trailing chevron.",
            TypeName: "SeaGlass.WinUI.SeaGlassListItem",
            RequiredPageName: "ListItemPage",
            PreviewFactory: () => new SeaGlassListItem { HeadlineText = "Ocean Sensor Feed", SupportingText = "Real-time telemetry stream" },
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassSnackbar",
            Category: CategoryDefinitions.FeedbackAndProgress,
            Description: "Transient feedback notification toast with glass styling and dismiss trigger.",
            TypeName: "SeaGlass.WinUI.SeaGlassSnackbar",
            RequiredPageName: "SnackbarPage",
            PreviewFactory: () => new SeaGlassSnackbar { Message = "Preset saved successfully." },
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassIcon",
            Category: CategoryDefinitions.FeedbackAndProgress,
            Description: "Vector glyph icon engine rendering high-clarity SeaGlass symbols.",
            TypeName: "SeaGlass.WinUI.SeaGlassIcon",
            RequiredPageName: "IconPage",
            PreviewFactory: () => new SeaGlassIcon(),
            ConfigurePreview: ApplyDefaultTuning));

        // ==========================================
        // Group 6: Dynamic Morphing Suite (8)
        // ==========================================
        _descriptors.Add(new(
            Name: "SeaGlassTwoElementMorph",
            Category: CategoryDefinitions.MorphingSuite,
            Description: "2D SDF morphing container transitioning smoothly between State A (pill) and State B (card).",
            TypeName: "SeaGlass.WinUI.SeaGlassTwoElementMorph",
            RequiredPageName: "TwoElementMorphPage",
            PreviewFactory: () => new SeaGlassTwoElementMorph(),
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassBuddingCluster",
            Category: CategoryDefinitions.MorphingSuite,
            Description: "Satellite droplet budding and coalescing using polynomial smooth-minimum unions.",
            TypeName: "SeaGlass.WinUI.SeaGlassBuddingCluster",
            RequiredPageName: "BuddingClusterPage",
            PreviewFactory: () => new SeaGlassBuddingCluster(),
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassMorphingButton",
            Category: CategoryDefinitions.MorphingSuite,
            Description: "Button morphing between idle state and active/expanded loading state.",
            TypeName: "SeaGlass.WinUI.SeaGlassMorphingButton",
            RequiredPageName: "MorphingButtonPage",
            PreviewFactory: () => new SeaGlassMorphingButton(),
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassChoreographedShapeOrb",
            Category: CategoryDefinitions.MorphingSuite,
            Description: "8-stage S-curve shape sequencer with counter-rotating typography.",
            TypeName: "SeaGlass.WinUI.SeaGlassChoreographedShapeOrb",
            RequiredPageName: "ChoreographedShapeOrbPage",
            PreviewFactory: () => new SeaGlassChoreographedShapeOrb(),
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassMultiShapeMorph",
            Category: CategoryDefinitions.MorphingSuite,
            Description: "Continuous morphing container interpolating across all 20 SeaGlass geometric shape models.",
            TypeName: "SeaGlass.WinUI.SeaGlassMultiShapeMorph",
            RequiredPageName: "MultiShapeMorphPage",
            PreviewFactory: () => new SeaGlassMultiShapeMorph(),
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassMorphingContextMenu",
            Category: CategoryDefinitions.MorphingSuite,
            Description: "Expanding context menu morphing outward from touch/pointer origin via 2D SDF dilation.",
            TypeName: "SeaGlass.WinUI.SeaGlassMorphingContextMenu",
            RequiredPageName: "MorphingContextMenuPage",
            PreviewFactory: () => new SeaGlassMorphingContextMenu(),
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassCascadedModelMorph",
            Category: CategoryDefinitions.MorphingSuite,
            Description: "Multi-stage sequential morph cascading across geometric contours.",
            TypeName: "SeaGlass.WinUI.SeaGlassCascadedModelMorph",
            RequiredPageName: "CascadedModelMorphPage",
            PreviewFactory: () => new SeaGlassCascadedModelMorph(),
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassContextRingMorph",
            Category: CategoryDefinitions.MorphingSuite,
            Description: "Radial action ring morphing outward on press with haptic feedback.",
            TypeName: "SeaGlass.WinUI.SeaGlassContextRingMorph",
            RequiredPageName: "ContextRingMorphPage",
            PreviewFactory: () => new SeaGlassContextRingMorph(),
            ConfigurePreview: ApplyDefaultTuning));

        // ==========================================
        // Group 7: iOS 27 & Advanced Overlays (8)
        // ==========================================
        _descriptors.Add(new(
            Name: "SeaGlassFloatingSheet",
            Category: CategoryDefinitions.OverlaysAndSheets,
            Description: "Detached floating glass sheet with interactive drag gestures and spring snap.",
            TypeName: "SeaGlass.WinUI.SeaGlassFloatingSheet",
            RequiredPageName: "FloatingSheetPage",
            PreviewFactory: () => new SeaGlassFloatingSheet(),
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassContextMenuOverlay",
            Category: CategoryDefinitions.OverlaysAndSheets,
            Description: "Glass overlay hosting contextual actions with backdrop optical refraction.",
            TypeName: "SeaGlass.WinUI.SeaGlassContextMenuOverlay",
            RequiredPageName: "ContextMenuOverlayPage",
            PreviewFactory: () => new SeaGlassContextMenuOverlay(),
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassActionSheet",
            Category: CategoryDefinitions.OverlaysAndSheets,
            Description: "Modal action sheet with frosted glass styling, action items, and cancel trigger.",
            TypeName: "SeaGlass.WinUI.SeaGlassActionSheet",
            RequiredPageName: "ActionSheetPage",
            PreviewFactory: () => new SeaGlassActionSheet(),
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassLiveActivityPill",
            Category: CategoryDefinitions.OverlaysAndSheets,
            Description: "Expanding activity pill toggling between compact status and rich detail views.",
            TypeName: "SeaGlass.WinUI.SeaGlassLiveActivityPill",
            RequiredPageName: "LiveActivityPillPage",
            PreviewFactory: () => new SeaGlassLiveActivityPill(),
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassMagnifierScrubber",
            Category: CategoryDefinitions.OverlaysAndSheets,
            Description: "Precision scrubbing waveform bar with draggable glass magnifying lens.",
            TypeName: "SeaGlass.WinUI.SeaGlassMagnifierScrubber",
            RequiredPageName: "MagnifierScrubberPage",
            PreviewFactory: () => new SeaGlassMagnifierScrubber(),
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassNotificationStack",
            Category: CategoryDefinitions.OverlaysAndSheets,
            Description: "Layered glass cards with interactive stacked spring expansion.",
            TypeName: "SeaGlass.WinUI.SeaGlassNotificationStack",
            RequiredPageName: "NotificationStackPage",
            PreviewFactory: () => new SeaGlassNotificationStack(),
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassWheelPicker",
            Category: CategoryDefinitions.OverlaysAndSheets,
            Description: "3D cylindrical drum picker with selectable items and frosted magnifier lens bar.",
            TypeName: "SeaGlass.WinUI.SeaGlassWheelPicker",
            RequiredPageName: "WheelPickerPage",
            PreviewFactory: () => new SeaGlassWheelPicker(),
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassControlCenterGrid",
            Category: CategoryDefinitions.OverlaysAndSheets,
            Description: "Quick-settings modular grid with Wi-Fi, Bluetooth, and Airplane mode toggles.",
            TypeName: "SeaGlass.WinUI.SeaGlassControlCenterGrid",
            RequiredPageName: "ControlCenterGridPage",
            PreviewFactory: () => new SeaGlassControlCenterGrid(),
            ConfigurePreview: ApplyDefaultTuning));

        // ==========================================
        // Group 8: Windows Native Extras (8)
        // ==========================================
        _descriptors.Add(new(
            Name: "SeaGlassTitleBar",
            Category: CategoryDefinitions.WindowsExtras,
            Description: "Windows 11 custom title bar with icon, title, caption buttons, and glass refraction.",
            TypeName: "SeaGlass.WinUI.SeaGlassTitleBar",
            RequiredPageName: "TitleBarPage",
            PreviewFactory: () => new SeaGlassTitleBar { Title = "SeaGlass Window" },
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassNavigationView",
            Category: CategoryDefinitions.WindowsExtras,
            Description: "Windows 11 navigation view with frosted glass pane and selection lozenge.",
            TypeName: "SeaGlass.WinUI.SeaGlassNavigationView",
            RequiredPageName: "NavigationViewPage",
            PreviewFactory: () => new SeaGlassNavigationView(),
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassTabView",
            Category: CategoryDefinitions.WindowsExtras,
            Description: "Tabbed interface with Add Tab button and liquid active highlight.",
            TypeName: "SeaGlass.WinUI.SeaGlassTabView",
            RequiredPageName: "TabViewPage",
            PreviewFactory: () => new SeaGlassTabView(),
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassCommandBar",
            Category: CategoryDefinitions.WindowsExtras,
            Description: "Primary action commands bar with frosted glass overflow menu.",
            TypeName: "SeaGlass.WinUI.SeaGlassCommandBar",
            RequiredPageName: "CommandBarPage",
            PreviewFactory: () => new SeaGlassCommandBar(),
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassInfoBar",
            Category: CategoryDefinitions.WindowsExtras,
            Description: "Status notification banner with severity indicators and dismiss action.",
            TypeName: "SeaGlass.WinUI.SeaGlassInfoBar",
            RequiredPageName: "InfoBarPage",
            PreviewFactory: () => new SeaGlassInfoBar { Title = "Optical Status", Message = "DirectComposition HLSL shader running." },
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassTeachingTip",
            Category: CategoryDefinitions.WindowsExtras,
            Description: "Contextual callout popup pointing to target UI elements with frosted glass lens.",
            TypeName: "SeaGlass.WinUI.SeaGlassTeachingTip",
            RequiredPageName: "TeachingTipPage",
            PreviewFactory: () => new SeaGlassTeachingTip { Title = "Optical Discovery", Subtitle = "Sea glass is tumbled by ocean waves." },
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassFlyout",
            Category: CategoryDefinitions.WindowsExtras,
            Description: "Contextual popup flyout with glass shadow, border, and frosted blur.",
            TypeName: "SeaGlass.WinUI.SeaGlassFlyout",
            RequiredPageName: "FlyoutPage",
            PreviewFactory: () => new SeaGlassFlyout
            {
                FlyoutContent = new TextBlock
                {
                    Text = "Contextual SeaGlass Flyout Menu",
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(240, 255, 255, 255))
                }
            },
            ConfigurePreview: ApplyDefaultTuning));

        _descriptors.Add(new(
            Name: "SeaGlassToolTip",
            Category: CategoryDefinitions.WindowsExtras,
            Description: "Rich tooltip with frosted glass background and high-contrast text.",
            TypeName: "SeaGlass.WinUI.SeaGlassToolTip",
            RequiredPageName: "ToolTipPage",
            PreviewFactory: () => new SeaGlassToolTip { Content = "Frosted tooltip information" },
            ConfigurePreview: ApplyDefaultTuning));
    }
}
