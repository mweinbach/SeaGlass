using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.Core;
using SeaGlass.Tests.Controls;
using SeaGlass.Tests.E2E.Infrastructure;
using SeaGlass.WinUI;
using Xunit;

namespace SeaGlass.Tests.Ergonomics;

/// <summary>
/// Unit tests verifying fluent C# builders, optics extensions, motion helpers,
/// and DP mutations on both SeaGlass controls and standard FrameworkElements.
/// </summary>
public sealed class FluentApiTests
{
    [Fact]
    public void Button_FluentChaining_SetsPropertiesCorrectly()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            bool clicked = false;
            var button = SeaGlassButton.Create("Submit", () => clicked = true)
                .Amber()
                .Elevated()
                .Large()
                .Coarseness(0.38)
                .ThicknessDp(22.0)
                .Ior(1.58)
                .RimChalkiness(0.82)
                .CornerRadius(18.0)
                .WithTilt(true);

            Assert.Equal("Submit", button.Label);
            Assert.Equal(SeaGlassTone.BeachAmber, button.Tone);
            Assert.Equal(SeaGlassVariant.Elevated, button.Variant);
            Assert.Equal(SeaGlassControlSize.Large, button.Size);
            Assert.Equal(0.38, button.Coarseness, 3);
            Assert.Equal(22.0, button.ThicknessDp, 3);
            Assert.Equal(1.58, button.Ior, 3);
            Assert.Equal(0.82, button.RimChalkiness, 3);
            Assert.Equal(18.0, button.CornerRadius.TopLeft, 3);
            Assert.True(button.TiltEnabled);

            button.OnClick?.Invoke();
            Assert.True(clicked);
        });
    }

    [Fact]
    public void Card_FluentChaining_ConfiguresHeaderAndDepth()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            var card = SeaGlassCard.Create("Telemetry", "Live Feed")
                .Cobalt()
                .Elevated()
                .WithElevationDepth(24.0)
                .CornerRadius(20.0);

            Assert.Equal("Telemetry", card.Title);
            Assert.Equal("Live Feed", card.Subtitle);
            Assert.Equal(SeaGlassTone.CobaltTide, card.Tone);
            Assert.Equal(SeaGlassVariant.Elevated, card.Variant);
            Assert.Equal(24.0, card.ElevationDepthDp);
            Assert.Equal(20.0, card.CornerRadius.TopLeft);
        });
    }

    [Fact]
    public void PillAndFilterChip_FluentChaining_SetsSelectionAndKind()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            bool pillSelected = false;
            var pill = SeaGlassPill.Create("Coastal", false)
                .Mint()
                .Tonal()
                .Kind(SeaGlassPillKind.Filter)
                .OnSelectedChanged(s => pillSelected = s);

            pill.Selected(true);
            Assert.True(pill.Selected);
            Assert.Equal(SeaGlassPillKind.Filter, pill.Kind);
            Assert.Equal(SeaGlassTone.SeafoamMint, pill.Tone);
            Assert.Equal(SeaGlassVariant.Tonal, pill.Variant);

            bool chipSelected = false;
            var chip = SeaGlassFilterChip.Create("Active Only", true)
                .Lavender()
                .Outlined()
                .ShowLeadingCheck(true)
                .OnSelectedChanged(s => chipSelected = s);

            Assert.True(chip.Selected);
            Assert.True(chip.ShowLeadingCheck);
            Assert.Equal(SeaGlassTone.SeaLavender, chip.Tone);
            Assert.Equal(SeaGlassVariant.Outlined, chip.Variant);
        });
    }

    [Fact]
    public void TextField_FluentChaining_SetsTextAndGlyph()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            string currentText = string.Empty;
            var textField = SeaGlassTextField.Create("Enter query...")
                .Text("Hydrophone-01")
                .LeadingGlyph(SeaGlassGlyph.Search)
                .Medium()
                .OnTextChanged(t => currentText = t);

            Assert.Equal("Enter query...", textField.Placeholder);
            Assert.Equal("Hydrophone-01", textField.Text);
            Assert.Equal(SeaGlassGlyph.Search, textField.LeadingGlyph);
            Assert.Equal(SeaGlassControlSize.Medium, textField.Size);
        });
    }

    [Fact]
    public void SwitchAndSlider_FluentChaining_SetsValuesAndRanges()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            bool toggleState = false;
            var toggle = SeaGlassSwitch.Create(true)
                .Mint()
                .OnToggled(on => toggleState = on);

            Assert.True(toggle.IsOn);
            Assert.Equal(SeaGlassTone.SeafoamMint, toggle.Tone);

            double sliderValue = 0;
            var slider = SeaGlassLiquidSlider.Create(75, 0, 100)
                .Cobalt()
                .Step(5)
                .OnValueChanged(v => sliderValue = v);

            Assert.Equal(75, slider.Value);
            Assert.Equal(0, slider.Minimum);
            Assert.Equal(100, slider.Maximum);
            Assert.Equal(5, slider.Step);
            Assert.Equal(SeaGlassTone.CobaltTide, slider.Tone);
        });
    }

    [Fact]
    public void Badge_FluentChaining_SetsLabelAndCount()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            var badge = SeaGlassBadge.Create(42)
                .Rose()
                .Filled();

            Assert.Equal("42", badge.Text);
            Assert.Equal(SeaGlassTone.RoseQuartzShell, badge.Tone);
            Assert.Equal(SeaGlassVariant.Filled, badge.Variant);
        });
    }

    [Fact]
    public void StandardFrameworkElement_OpticsExtensions_AttachProperties()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            var border = new Border()
                .WithTone(SeaGlassTone.KelpForest)
                .WithVariant(SeaGlassVariant.Outlined)
                .WithSize(SeaGlassControlSize.Small)
                .WithCoarseness(0.25)
                .WithThickness(14.0)
                .WithCornerRadius(12.0);

            Assert.Equal(SeaGlassTone.KelpForest, SeaGlassTheme.GetTone(border));
            Assert.Equal(SeaGlassVariant.Outlined, SeaGlassTheme.GetVariant(border));
            Assert.Equal(SeaGlassControlSize.Small, SeaGlassTheme.GetSize(border));
            Assert.Equal(0.25, SeaGlassTheme.GetCoarseness(border));
            Assert.Equal(14.0, SeaGlassTheme.GetThicknessDp(border));
            Assert.Equal(12.0, border.CornerRadius.TopLeft);
        });
    }

    [Fact]
    public void MotionExtensions_AttachSpringsAndChoreography()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            var button = new SeaGlassButton()
                .WithSpatialSpring(0.62f, 680f)
                .WithEffectsSpring(1.0f, 1100f);

            SeaGlassSpringSpec? spatial = SeaGlassTheme.GetSpatialSpring(button);
            SeaGlassSpringSpec? effects = SeaGlassTheme.GetEffectsSpring(button);

            Assert.NotNull(spatial);
            Assert.Equal(0.62f, spatial.Value.DampingRatio);
            Assert.Equal(680f, spatial.Value.Stiffness);

            Assert.NotNull(effects);
            Assert.Equal(1.0f, effects.Value.DampingRatio);
            Assert.Equal(1100f, effects.Value.Stiffness);
            Assert.True(effects.Value.IsCriticallyDamped);
        });
    }

    [Fact]
    public void InSeaGlassSurface_WrapsContent_ReturnsConfiguredContainer()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            var textBlock = new TextBlock { Text = "Sanded Sea Glass" };
            var surface = textBlock.InSeaGlassSurface(
                variant: SeaGlassVariant.Elevated,
                tone: SeaGlassTone.ObsidianTide,
                cornerRadius: 16.0);

            Assert.Same(textBlock, surface.Content);
            Assert.Equal(SeaGlassVariant.Elevated, surface.Variant);
            Assert.Equal(SeaGlassTone.ObsidianTide, surface.Tone);
            Assert.Equal(16.0, surface.CornerRadius.TopLeft);
        });
    }

    [Fact]
    public void TintProperty_CustomColor_AltersMaterialWithoutRetemplating()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            var surface = new SeaGlassSurface();
            Windows.UI.Color testColor = Windows.UI.Color.FromArgb(255, 216, 152, 50);

            surface.Tint(testColor, density: 0.75);

            Assert.Equal(testColor, surface.Tint);
            int expectedArgb = unchecked((int)0xFFD89832);
            Assert.Equal(expectedArgb, surface.Material.TintColorArgb);
            Assert.Equal(0.75, surface.Material.TintDensity, 2);
        });
    }

    [Fact]
    public void TwoWayBindingHelpers_SyncStateBetweenModelAndControls()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            bool switchState = false;
            var toggle = new SeaGlassSwitch().IsOn(() => switchState, v => switchState = v);
            Assert.False(toggle.IsOn);
            toggle.IsOn = true;
            Assert.True(switchState);

            double sliderValue = 25.0;
            var slider = new SeaGlassLiquidSlider().Value(() => sliderValue, v => sliderValue = v);
            Assert.Equal(25.0, slider.Value);
            slider.Value = 88.0;
            Assert.Equal(88.0, sliderValue);

            string textValue = "initial";
            var textField = new SeaGlassTextField().Text(() => textValue, v => textValue = v);
            Assert.Equal("initial", textField.Text);
            textField.Text = "updated";
            Assert.Equal("updated", textValue);

            bool pillSelected = false;
            var pill = new SeaGlassPill().Selected(() => pillSelected, v => pillSelected = v);
            Assert.False(pill.Selected);
            pill.Selected = true;
            Assert.True(pillSelected);
        });
    }

    [Fact]
    public void FullPage_DeclarativeSingleExpressionTree_BuildsCorrectVisualTree()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            bool syncTriggered = false;
            string searchQuery = "Station Alpha-7";
            bool sonarActive = true;
            double frequencyKhz = 45.0;

            // Construct a full nontrivial page in a single declarative expression tree
            var page = SeaGlassControlBuilders.VStack(24,
                // Top Telemetry Header Card
                SeaGlassControlBuilders.Card("Ocean Telemetry", "Sub-surface Acoustic Array",
                    SeaGlassControlBuilders.HStack(16,
                        SeaGlassTextField.Create("Search sensor nodes...")
                            .Text(() => searchQuery, v => searchQuery = v)
                            .LeadingGlyph(SeaGlassGlyph.Search)
                            .Medium(),
                        SeaGlassBadge.Create("LIVE")
                            .Mint()
                            .Filled(),
                        SeaGlassControlBuilders.Spacer(),
                        SeaGlassIconButton.Create(SeaGlassGlyph.Tune)
                            .Medium()
                    )
                )
                .Cobalt()
                .Elevated()
                .ElevationDepth(28.0)
                .CornerRadius(24.0),

                // Middle Controls Card with inputs & two-way bindings
                SeaGlassControlBuilders.Card("Array Controls", "Power & Frequency Modulation",
                    SeaGlassControlBuilders.VStack(12,
                        SeaGlassControlBuilders.HStack(12,
                            new TextBlock { Text = "Acoustic Sonar Ping" }.Center(),
                            SeaGlassControlBuilders.Spacer(),
                            SeaGlassSwitch.Create(true)
                                .Mint()
                                .IsOn(() => sonarActive, v => sonarActive = v)
                        ),
                        SeaGlassLiquidSlider.Create(45.0, 0.0, 100.0)
                            .Cobalt()
                            .Step(1.0)
                            .ValueText("45 kHz")
                            .Value(() => frequencyKhz, v => frequencyKhz = v),
                        SeaGlassControlBuilders.HStack(8,
                            SeaGlassPill.Create("Continuous", true).Kind(SeaGlassPillKind.Filter),
                            SeaGlassPill.Create("Pulse", false).Kind(SeaGlassPillKind.Filter),
                            SeaGlassFilterChip.Create("HDR Echo", true).ShowLeadingCheck()
                        )
                    )
                )
                .Mint()
                .Padding(16),

                // Fluid Coalescing Group with interactive buttons
                SeaGlassControlBuilders.FluidGroup(28.0,
                    SeaGlassButton.Create("Transmit Ping")
                        .Amber()
                        .Filled()
                        .FluidNode("btn-tx", isFluid: true),
                    SeaGlassButton.Create("Standby")
                        .Rose()
                        .Outlined()
                        .FluidNode("btn-rx", isFluid: true)
                ),

                // Footer Actions
                SeaGlassControlBuilders.HStack(12,
                    SeaGlassControlBuilders.Spacer(),
                    SeaGlassButton.Create("Emergency Shutdown")
                        .Rose()
                        .Destructive()
                        .Large(),
                    SeaGlassButton.Create("Synchronize Array", () => syncTriggered = true)
                        .Mint()
                        .Elevated()
                        .Large()
                )
            ).Padding(32);

            // 1. Assert root container
            Assert.NotNull(page);
            Assert.Equal(Orientation.Vertical, page.Orientation);
            Assert.Equal(24, page.Spacing);
            Assert.Equal(32, page.Padding.Left);
            Assert.Equal(32, page.Padding.Top);
            Assert.Equal(4, page.Children.Count);

            // 2. Assert Header Card (page.Children[0])
            var headerCard = Assert.IsType<SeaGlassCard>(page.Children[0]);
            Assert.Equal("Ocean Telemetry", headerCard.Title);
            Assert.Equal("Sub-surface Acoustic Array", headerCard.Subtitle);
            Assert.Equal(SeaGlassTone.CobaltTide, headerCard.Tone);
            Assert.Equal(SeaGlassVariant.Elevated, headerCard.Variant);
            Assert.Equal(28.0, headerCard.ElevationDepthDp);
            Assert.Equal(24.0, headerCard.CornerRadius.TopLeft);

            var headerRow = Assert.IsType<StackPanel>(headerCard.Content);
            Assert.Equal(Orientation.Horizontal, headerRow.Orientation);
            Assert.Equal(4, headerRow.Children.Count);
            var searchInput = Assert.IsType<SeaGlassTextField>(headerRow.Children[0]);
            Assert.Equal("Station Alpha-7", searchInput.Text);
            Assert.Equal(SeaGlassGlyph.Search, searchInput.LeadingGlyph);
            Assert.Equal(SeaGlassControlSize.Medium, searchInput.Size);
            var liveBadge = Assert.IsType<SeaGlassBadge>(headerRow.Children[1]);
            Assert.Equal("LIVE", liveBadge.Text);
            Assert.Equal(SeaGlassTone.SeafoamMint, liveBadge.Tone);
            Assert.IsType<Border>(headerRow.Children[2]); // Spacer

            // 3. Assert Middle Card (page.Children[1])
            var controlsCard = Assert.IsType<SeaGlassCard>(page.Children[1]);
            Assert.Equal("Array Controls", controlsCard.Title);
            Assert.Equal(SeaGlassTone.SeafoamMint, controlsCard.Tone);
            var controlsCol = Assert.IsType<StackPanel>(controlsCard.Content);
            Assert.Equal(3, controlsCol.Children.Count);

            // 4. Assert Fluid Group (page.Children[2])
            var fluidGroup = Assert.IsType<SeaGlassFluidGroup>(page.Children[2]);
            Assert.Equal(28.0, fluidGroup.SmoothRadiusDp);
            Assert.Equal(2, fluidGroup.Children.Count);
            var txBtn = Assert.IsType<SeaGlassButton>(fluidGroup.Children[0]);
            Assert.Equal("Transmit Ping", txBtn.Label);
            Assert.Equal(SeaGlassTone.BeachAmber, txBtn.Tone);
            Assert.True(SeaGlassFluidGroup.GetIsFluidNode(txBtn));
            Assert.Equal("btn-tx", SeaGlassFluidGroup.GetNodeId(txBtn));

            // 5. Assert Footer (page.Children[3])
            var footerRow = Assert.IsType<StackPanel>(page.Children[3]);
            Assert.Equal(Orientation.Horizontal, footerRow.Orientation);
            var syncBtn = Assert.IsType<SeaGlassButton>(footerRow.Children[2]);
            Assert.Equal("Synchronize Array", syncBtn.Label);
            Assert.Equal(SeaGlassTone.SeafoamMint, syncBtn.Tone);
            Assert.Equal(SeaGlassVariant.Elevated, syncBtn.Variant);
            Assert.Equal(SeaGlassControlSize.Large, syncBtn.Size);

            syncBtn.OnClick?.Invoke();
            Assert.True(syncTriggered);
        });
    }

    [Fact]
    public void Catalog_All58Controls_AreReachableFluently_AndSupportGenericModifiers()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            var registry = E2ECatalogContractRegistry.All58Controls;
            Assert.Equal(58, registry.Count);

            int configuredControlsCount = 0;

            foreach (var descriptor in registry)
            {
                Type? type = E2ECatalogContractRegistry.ProbeControlType(descriptor.ExpectedTypeName);
                Assert.True(type != null, $"Control type '{descriptor.ExpectedTypeName}' must exist in SeaGlass.WinUI.");

                var expectedMaterial = SeaGlassMaterial.BeachAmber();

                if (descriptor.Name == "SeaGlassTheme")
                {
                    // SeaGlassTheme is the global attached property theme manager; verify it configures FrameworkElements fluently
                    var host = new Border();
                    var hostConfigured = host
                        .Tone(SeaGlassTone.BeachAmber, SeaGlassVariant.Elevated)
                        .Size(SeaGlassControlSize.Large)
                        .Material(expectedMaterial)
                        .Coarseness(0.42)
                        .ThicknessDp(20.0)
                        .Ior(1.55)
                        .RimChalkiness(0.78)
                        .Padding(16.0);

                    Assert.Same(host, hostConfigured);
                    Assert.Equal(SeaGlassTone.BeachAmber, SeaGlassTheme.GetTone(host));
                    Assert.Equal(SeaGlassVariant.Elevated, SeaGlassTheme.GetVariant(host));
                    Assert.Equal(SeaGlassControlSize.Large, SeaGlassTheme.GetSize(host));
                    Assert.Equal(expectedMaterial, SeaGlassTheme.GetMaterial(host));
                    Assert.Equal(0.42, SeaGlassTheme.GetCoarseness(host), 3);
                    Assert.Equal(20.0, SeaGlassTheme.GetThicknessDp(host), 3);
                    configuredControlsCount++;
                    continue;
                }

                // 1. Assert type derives from FrameworkElement
                Assert.True(typeof(FrameworkElement).IsAssignableFrom(type),
                    $"Catalog entry '{descriptor.Name}' ({type.FullName}) must inherit from FrameworkElement.");

                // 2. Assert instantiation on STA thread
                object? instance = Activator.CreateInstance(type);
                Assert.NotNull(instance);
                var element = Assert.IsAssignableFrom<FrameworkElement>(instance);

                // 3. Configure fluently using generic modifiers catalog-wide
                var returned = element
                    .Tone(SeaGlassTone.BeachAmber)
                    .Variant(SeaGlassVariant.Elevated)
                    .Size(SeaGlassControlSize.Large)
                    .Padding(12.0)
                    .Material(expectedMaterial)
                    .Coarseness(0.42)
                    .ThicknessDp(20.0)
                    .Ior(1.55)
                    .RimChalkiness(0.78)
                    .AccessibleName($"Accessible {descriptor.Name}")
                    .ToolTip($"Tooltip for {descriptor.Name}");

                // 4. Assert method chaining preserves the exact instance (fluent identity)
                Assert.Same(element, returned);

                // 5. Assert values were set and are queryable
                if (element is SeaGlassSurface surface)
                {
                    Assert.Equal(SeaGlassTone.BeachAmber, surface.Tone);
                    Assert.Equal(SeaGlassVariant.Elevated, surface.Variant);
                    Assert.Equal(SeaGlassControlSize.Large, surface.Size);
                    Assert.Equal(0.42, surface.Coarseness, 3);
                    Assert.Equal(20.0, surface.ThicknessDp, 3);
                    Assert.Equal(1.55, surface.Ior, 3);
                    Assert.NotNull(surface.Material);
                    Assert.Equal(0.42, surface.Material.Coarseness, 2);
                    Assert.Equal(20.0, surface.Material.ThicknessDp, 2);
                    Assert.Equal(1.55, surface.Material.Ior, 2);
                    Assert.Equal(0.78, surface.Material.RimChalkiness, 2);
                }
                else
                {
                    Assert.Equal(SeaGlassTone.BeachAmber, SeaGlassTheme.GetTone(element));
                    Assert.Equal(SeaGlassVariant.Elevated, SeaGlassTheme.GetVariant(element));
                    Assert.Equal(SeaGlassControlSize.Large, SeaGlassTheme.GetSize(element));
                    Assert.Equal(expectedMaterial, SeaGlassTheme.GetMaterial(element));
                    Assert.Equal(0.42, SeaGlassTheme.GetCoarseness(element), 3);
                    Assert.Equal(20.0, SeaGlassTheme.GetThicknessDp(element), 3);
                }

                configuredControlsCount++;
            }

            Assert.Equal(58, configuredControlsCount);
        });
    }
}
