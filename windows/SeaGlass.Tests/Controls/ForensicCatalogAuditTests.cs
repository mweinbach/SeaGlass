using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.WinUI;
using Xunit;
using Xunit.Abstractions;

namespace SeaGlass.Tests.Controls;

public sealed class ForensicCatalogAuditTests
{
    private readonly ITestOutputHelper _output;

    public ForensicCatalogAuditTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static readonly (string Name, string FullTypeName)[] All58Controls = new[]
    {
        // Group 1: Foundations & Surfaces (9)
        ("SeaGlassTheme", "SeaGlass.WinUI.SeaGlassTheme"),
        ("SeaGlassStage", "SeaGlass.WinUI.SeaGlassStage"),
        ("SeaGlassFluidGroup", "SeaGlass.WinUI.SeaGlassFluidGroup"),
        ("SeaGlassSurface", "SeaGlass.WinUI.SeaGlassSurface"),
        ("SeaGlassCard", "SeaGlass.WinUI.SeaGlassCard"),
        ("SeaGlassDialogCard", "SeaGlass.WinUI.SeaGlassDialogCard"),
        ("SeaGlassBottomSheetSurface", "SeaGlass.WinUI.SeaGlassBottomSheetSurface"),
        ("SeaGlassCornerEmbeddedPod", "SeaGlass.WinUI.SeaGlassCornerEmbeddedPod"),
        ("SeaGlassAdaptiveCornerLabCard", "SeaGlass.WinUI.SeaGlassAdaptiveCornerLabCard"),

        // Group 2: Buttons & Chips (8)
        ("SeaGlassButton", "SeaGlass.WinUI.SeaGlassButton"),
        ("SeaGlassIconButton", "SeaGlass.WinUI.SeaGlassIconButton"),
        ("SeaGlassExtendedFab", "SeaGlass.WinUI.SeaGlassExtendedFab"),
        ("SeaGlassPill", "SeaGlass.WinUI.SeaGlassPill"),
        ("SeaGlassFilterChip", "SeaGlass.WinUI.SeaGlassFilterChip"),
        ("SeaGlassBadge", "SeaGlass.WinUI.SeaGlassBadge"),
        ("SeaGlassSegmentedButtonRow", "SeaGlass.WinUI.SeaGlassSegmentedButtonRow"),
        ("SeaGlassPebbleOrb", "SeaGlass.WinUI.SeaGlassPebbleOrb"),

        // Group 3: Navigation & Bars (7)
        ("SeaGlassTopAppBar", "SeaGlass.WinUI.SeaGlassTopAppBar"),
        ("SeaGlassFluidBar", "SeaGlass.WinUI.SeaGlassFluidBar"),
        ("SeaGlassMiniPlayerBar", "SeaGlass.WinUI.SeaGlassMiniPlayerBar"),
        ("SeaGlassFloatingToolbar", "SeaGlass.WinUI.SeaGlassFloatingToolbar"),
        ("SeaGlassPillNavBar", "SeaGlass.WinUI.SeaGlassPillNavBar"),
        ("SeaGlassPromptBar", "SeaGlass.WinUI.SeaGlassPromptBar"),
        ("SeaGlassSearchBar", "SeaGlass.WinUI.SeaGlassSearchBar"),

        // Group 4: Inputs & Toggles (5)
        ("SeaGlassTextField", "SeaGlass.WinUI.SeaGlassTextField"),
        ("SeaGlassSwitch", "SeaGlass.WinUI.SeaGlassSwitch"),
        ("SeaGlassCheckbox", "SeaGlass.WinUI.SeaGlassCheckbox"),
        ("SeaGlassRadioButton", "SeaGlass.WinUI.SeaGlassRadioButton"),
        ("SeaGlassLiquidSlider", "SeaGlass.WinUI.SeaGlassLiquidSlider"),

        // Group 5: Feedback & Lists (5)
        ("SeaGlassLinearProgressIndicator", "SeaGlass.WinUI.SeaGlassLinearProgressIndicator"),
        ("SeaGlassCircularProgressIndicator", "SeaGlass.WinUI.SeaGlassCircularProgressIndicator"),
        ("SeaGlassListItem", "SeaGlass.WinUI.SeaGlassListItem"),
        ("SeaGlassSnackbar", "SeaGlass.WinUI.SeaGlassSnackbar"),
        ("SeaGlassIcon", "SeaGlass.WinUI.SeaGlassIcon"),

        // Group 6: Dynamic Morphing Suite (8)
        ("SeaGlassTwoElementMorph", "SeaGlass.WinUI.SeaGlassTwoElementMorph"),
        ("SeaGlassBuddingCluster", "SeaGlass.WinUI.SeaGlassBuddingCluster"),
        ("SeaGlassMorphingButton", "SeaGlass.WinUI.SeaGlassMorphingButton"),
        ("SeaGlassChoreographedShapeOrb", "SeaGlass.WinUI.SeaGlassChoreographedShapeOrb"),
        ("SeaGlassMultiShapeMorph", "SeaGlass.WinUI.SeaGlassMultiShapeMorph"),
        ("SeaGlassMorphingContextMenu", "SeaGlass.WinUI.SeaGlassMorphingContextMenu"),
        ("SeaGlassCascadedModelMorph", "SeaGlass.WinUI.SeaGlassCascadedModelMorph"),
        ("SeaGlassContextRingMorph", "SeaGlass.WinUI.SeaGlassContextRingMorph"),

        // Group 7: iOS 27 & Advanced Overlays (8)
        ("SeaGlassFloatingSheet", "SeaGlass.WinUI.SeaGlassFloatingSheet"),
        ("SeaGlassContextMenuOverlay", "SeaGlass.WinUI.SeaGlassContextMenuOverlay"),
        ("SeaGlassActionSheet", "SeaGlass.WinUI.SeaGlassActionSheet"),
        ("SeaGlassLiveActivityPill", "SeaGlass.WinUI.SeaGlassLiveActivityPill"),
        ("SeaGlassMagnifierScrubber", "SeaGlass.WinUI.SeaGlassMagnifierScrubber"),
        ("SeaGlassNotificationStack", "SeaGlass.WinUI.SeaGlassNotificationStack"),
        ("SeaGlassWheelPicker", "SeaGlass.WinUI.SeaGlassWheelPicker"),
        ("SeaGlassControlCenterGrid", "SeaGlass.WinUI.SeaGlassControlCenterGrid"),

        // Group 8: Windows Native Extras (8)
        ("SeaGlassTitleBar", "SeaGlass.WinUI.SeaGlassTitleBar"),
        ("SeaGlassNavigationView", "SeaGlass.WinUI.SeaGlassNavigationView"),
        ("SeaGlassTabView", "SeaGlass.WinUI.SeaGlassTabView"),
        ("SeaGlassCommandBar", "SeaGlass.WinUI.SeaGlassCommandBar"),
        ("SeaGlassInfoBar", "SeaGlass.WinUI.SeaGlassInfoBar"),
        ("SeaGlassTeachingTip", "SeaGlass.WinUI.SeaGlassTeachingTip"),
        ("SeaGlassFlyout", "SeaGlass.WinUI.SeaGlassFlyout"),
        ("SeaGlassToolTip", "SeaGlass.WinUI.SeaGlassToolTip")
    };

    [Fact]
    public void Verify_All_58_Controls_Are_Genuine_WinUI_Controls_With_Authentic_DPs()
    {
        Assembly winUiAssembly = typeof(SeaGlassSurface).Assembly;
        int verifiedCount = 0;
        int nonControlServices = 0;
        List<string> verifiedList = new();
        List<string> issues = new();

        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            foreach (var (name, fullTypeName) in All58Controls)
            {
                Type? type = winUiAssembly.GetType(fullTypeName);
                if (type == null)
                {
                    issues.Add($"MISSING: Type '{fullTypeName}' not found in SeaGlass.WinUI");
                    continue;
                }

                if (name == "SeaGlassTheme")
                {
                    // SeaGlassTheme is the global theme manager & attached property provider
                    Assert.True(type.GetField("ToneProperty", BindingFlags.Public | BindingFlags.Static) != null);
                    Assert.True(type.GetField("VariantProperty", BindingFlags.Public | BindingFlags.Static) != null);
                    nonControlServices++;
                    verifiedList.Add($"{name} -> Attached DP Provider & Theme Manager (PASS)");
                    continue;
                }

                bool isPanel = typeof(Panel).IsAssignableFrom(type);
                bool isControl = typeof(Control).IsAssignableFrom(type);
                bool isContentControl = typeof(ContentControl).IsAssignableFrom(type);
                bool isFrameworkElement = typeof(FrameworkElement).IsAssignableFrom(type);

                if (!isFrameworkElement)
                {
                    issues.Add($"NOT A WINUI ELEMENT: '{fullTypeName}' does not inherit FrameworkElement");
                    continue;
                }

                if (!isControl && !isPanel)
                {
                    issues.Add($"NOT A CONTROL OR PANEL: '{fullTypeName}' inherits FrameworkElement but not Control or Panel");
                    continue;
                }

                // Verify instantiation on STA thread
                object? instance;
                try
                {
                    instance = Activator.CreateInstance(type);
                }
                catch (Exception ex)
                {
                    issues.Add($"ACTIVATION FAILURE: '{fullTypeName}' failed to instantiate on STA thread: {ex.Message}");
                    continue;
                }

                if (instance == null)
                {
                    issues.Add($"NULL INSTANCE: '{fullTypeName}' instantiated to null");
                    continue;
                }

                // Verify DependencyProperties (Variant, Tone, Size, Material)
                BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;
                FieldInfo? variantDp = type.GetField("VariantProperty", flags);
                FieldInfo? toneDp = type.GetField("ToneProperty", flags);
                FieldInfo? sizeDp = type.GetField("SizeProperty", flags);

                if (isPanel) // e.g. SeaGlassFluidGroup
                {
                    FieldInfo? groupMatDp = type.GetField("GroupMaterialProperty", flags);
                    FieldInfo? smoothDp = type.GetField("SmoothRadiusDpProperty", flags);
                    if (toneDp == null || groupMatDp == null || smoothDp == null)
                    {
                        issues.Add($"DP FAILURE: Panel '{fullTypeName}' missing expected DPs");
                        continue;
                    }
                }
                else if (name == "SeaGlassStage")
                {
                    FieldInfo? backdropContentDp = type.GetField("BackdropContentProperty", flags);
                    if (backdropContentDp == null)
                    {
                        issues.Add($"DP FAILURE: SeaGlassStage missing BackdropContentProperty");
                        continue;
                    }
                }
                else
                {
                    // Standard SeaGlass Controls
                    if (variantDp == null) issues.Add($"DP FAILURE: '{fullTypeName}' missing VariantProperty");
                    if (toneDp == null) issues.Add($"DP FAILURE: '{fullTypeName}' missing ToneProperty");
                    if (sizeDp == null) issues.Add($"DP FAILURE: '{fullTypeName}' missing SizeProperty");
                }

                // Verify base class chain
                string baseChain = type.BaseType?.Name ?? "none";
                verifiedCount++;
                verifiedList.Add($"{name} ({type.Name} : {baseChain}) -> Genuine WinUI Control with DPs (PASS)");
            }
        });

        foreach (string v in verifiedList)
        {
            _output.WriteLine(v);
        }

        Assert.Empty(issues);
        Assert.Equal(57, verifiedCount); // 57 WinUI controls/panels + 1 theme manager
        Assert.Equal(1, nonControlServices); // SeaGlassTheme
        Assert.Equal(58, verifiedCount + nonControlServices);
    }
}
