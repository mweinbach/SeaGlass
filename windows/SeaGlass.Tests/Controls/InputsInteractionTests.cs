using System;
using SeaGlass.Tests.Infrastructure;
using Xunit;

namespace SeaGlass.Tests.Controls;

public sealed class InputsInteractionTests
{
    [Fact]
    public void Diagnostic_ResourceResolution()
    {
        var result = HostSelfTestRunner.DefaultResult;
        Assert.NotNull(result);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public void TextField_LiveTyping_SynchronizesValue_AndRaisesEvents()
    {
        var result = HostSelfTestRunner.DefaultResult;
        Assert.True(result.Interactions.TryGetValue("SeaGlassTextField", out var outcome),
            "SeaGlassTextField interaction missing from host report");
        Assert.Equal("PASS", outcome.Status);
    }

    [Fact]
    public void Switch_LiveToggle_FlipsState_AndAnimatesThumbPhysics()
    {
        var result = HostSelfTestRunner.DefaultResult;
        Assert.True(result.Interactions.TryGetValue("SeaGlassSwitch", out var outcome),
            "SeaGlassSwitch interaction missing from host report");
        Assert.Equal("PASS", outcome.Status);
    }

    [Fact]
    public void LiquidSlider_LiveDragging_UpdatesValue_AndStretchesThumb()
    {
        var result = HostSelfTestRunner.DefaultResult;
        Assert.True(result.Interactions.TryGetValue("SeaGlassLiquidSlider", out var outcome),
            "SeaGlassLiquidSlider interaction missing from host report");
        Assert.Equal("PASS", outcome.Status);
    }

    [Fact]
    public void Checkbox_LiveToggle_AppliesTemplate_AndTogglesState()
    {
        var result = HostSelfTestRunner.DefaultResult;
        Assert.True(result.Interactions.TryGetValue("SeaGlassCheckbox", out var outcome),
            "SeaGlassCheckbox interaction missing from host report");
        Assert.Equal("PASS", outcome.Status);
    }

    [Fact]
    public void RadioButton_LiveToggle_AppliesTemplate_AndSelectsState()
    {
        var result = HostSelfTestRunner.DefaultResult;
        Assert.True(result.Interactions.TryGetValue("SeaGlassRadioButton", out var outcome),
            "SeaGlassRadioButton interaction missing from host report");
        Assert.Equal("PASS", outcome.Status);
    }
}
