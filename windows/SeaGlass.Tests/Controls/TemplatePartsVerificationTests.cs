using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SeaGlass.WinUI;
using SeaGlass.Tests.E2E.Infrastructure;
using SeaGlass.Tests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace SeaGlass.Tests.Controls;

public sealed class TemplatePartsVerificationTests
{
    private readonly ITestOutputHelper _output;

    public TemplatePartsVerificationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public static IEnumerable<object[]> All58ControlDescriptors()
    {
        foreach (var desc in E2ECatalogContractRegistry.All58Controls)
        {
            yield return new object[] { desc.Name, desc.ExpectedTypeName };
        }
    }

    public static IEnumerable<object[]> All10InteractionDescriptors()
    {
        string[] interactions =
        [
            "SeaGlassTextField",
            "SeaGlassSwitch",
            "SeaGlassLiquidSlider",
            "SeaGlassCheckbox",
            "SeaGlassRadioButton",
            "SeaGlassDialogCard",
            "SeaGlassSearchBar",
            "SeaGlassPromptBar",
            "SeaGlassBuddingCluster",
            "SeaGlassTwoElementMorph"
        ];
        foreach (var item in interactions)
        {
            yield return new object[] { item };
        }
    }

    [Theory]
    [MemberData(nameof(All58ControlDescriptors))]
    public void CatalogControl_MustInstantiateOnSta_ApplyTemplate_AndResolveAllDeclaredTemplateParts(string controlName, string fullTypeName)
    {
        Assert.False(string.IsNullOrWhiteSpace(fullTypeName));
        var result = HostSelfTestRunner.DefaultResult;
        Assert.True(result.Controls.TryGetValue(controlName, out var outcome),
            $"Control '{controlName}' was not reported by host verification runner! Output:\n{result.StandardOutput}");

        _output.WriteLine($"Control '{controlName}': parts={outcome.ResolvedParts}/{outcome.ExpectedParts}, status={outcome.Status}");
        Assert.Equal("PASS", outcome.Status);
        if (outcome.ExpectedParts > 0)
        {
            Assert.Equal(outcome.ExpectedParts, outcome.ResolvedParts);
        }
    }

    [Fact]
    public void Inputs_MustNotUseStubTemplate_AllDeclaredInteractivePartsMustExist()
    {
        var result = HostSelfTestRunner.DefaultResult;
        string[] inputControls = ["SeaGlassTextField", "SeaGlassSwitch", "SeaGlassLiquidSlider", "SeaGlassCheckbox", "SeaGlassRadioButton"];
        foreach (var name in inputControls)
        {
            Assert.True(result.Controls.TryGetValue(name, out var outcome), $"Input control '{name}' missing from host output");
            Assert.Equal("PASS", outcome.Status);
            Assert.True(outcome.ResolvedParts > 0, $"Input control '{name}' resolved 0 parts");
            Assert.Equal(outcome.ExpectedParts, outcome.ResolvedParts);
        }
    }

    [Fact]
    public void WindowsExtras_MustNotUseStubTemplate_AllDeclaredPartsMustExist()
    {
        var result = HostSelfTestRunner.DefaultResult;
        string[] extraControls =
        [
            "SeaGlassTitleBar", "SeaGlassNavigationView", "SeaGlassTabView",
            "SeaGlassCommandBar", "SeaGlassInfoBar", "SeaGlassTeachingTip",
            "SeaGlassFlyout", "SeaGlassToolTip"
        ];
        foreach (var name in extraControls)
        {
            Assert.True(result.Controls.TryGetValue(name, out var outcome), $"Windows Extra control '{name}' missing from host output");
            Assert.Equal("PASS", outcome.Status);
            Assert.True(outcome.ResolvedParts > 0, $"Windows Extra control '{name}' resolved 0 parts");
            Assert.Equal(outcome.ExpectedParts, outcome.ResolvedParts);
        }
    }

    [Theory]
    [MemberData(nameof(All10InteractionDescriptors))]
    public void InteractiveControl_MustExecuteLiveInteractionsInHostWindow(string interactionName)
    {
        var result = HostSelfTestRunner.DefaultResult;
        Assert.True(result.Interactions.TryGetValue(interactionName, out var outcome),
            $"Interaction '{interactionName}' missing from host output");
        _output.WriteLine($"Interaction '{interactionName}': status={outcome.Status}");
        Assert.Equal("PASS", outcome.Status);
    }

    [Fact]
    public void CanaryFailure_MustFailWithNonZeroExitCodeAndReportFailure()
    {
        var canaryResult = HostSelfTestRunner.RunHost(canaryFail: true);
        Assert.NotEqual(0, canaryResult.ExitCode);
        Assert.True(canaryResult.Controls.TryGetValue("SeaGlassButton", out var buttonOutcome));
        Assert.Equal("FAIL", buttonOutcome.Status);
        Assert.Contains("Canary simulated template part failure", buttonOutcome.Error ?? string.Empty);
    }

    [Fact]
    public void HeadlessHost_AllPalettesShadersAndCbufferAbiMustPass()
    {
        var result = HostSelfTestRunner.DefaultResult;
        Assert.Equal(0, result.ExitCode);
        Assert.True(result.PalettesAndOpticsPassed, "Palettes, shader dialects, and cbuffer ABI must pass in headless host");
    }
}
