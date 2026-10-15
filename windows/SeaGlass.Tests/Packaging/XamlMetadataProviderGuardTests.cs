using System;
using System.Linq;
using Microsoft.UI.Xaml.Markup;
using SeaGlass.WinUI;
using Xunit;

namespace SeaGlass.Tests.Packaging;

public sealed class XamlMetadataProviderGuardTests
{
    [Fact]
    public void SeaGlassWinUI_Exports_Valid_IXamlMetadataProvider_With_Controls_And_Properties()
    {
        var asm = typeof(SeaGlassSurface).Assembly;
        var providerType = asm.GetTypes()
            .FirstOrDefault(t => typeof(IXamlMetadataProvider).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        Assert.NotNull(providerType);

        var provider = Activator.CreateInstance(providerType) as IXamlMetadataProvider;
        Assert.NotNull(provider);

        // 1. Verify SeaGlassCard and Title/Subtitle dependency properties
        var cardType = provider.GetXamlType(typeof(SeaGlassCard));
        Assert.NotNull(cardType);
        var cardTitleMember = cardType.GetMember("Title");
        Assert.NotNull(cardTitleMember);
        Assert.Equal("String", cardTitleMember.Type.FullName);
        Assert.NotNull(cardType.GetMember("Subtitle"));

        // 2. Verify SeaGlassSurface base DPs (Variant, Tone, Size)
        var surfaceType = provider.GetXamlType(typeof(SeaGlassSurface));
        Assert.NotNull(surfaceType);
        Assert.NotNull(surfaceType.GetMember("Variant"));
        Assert.NotNull(surfaceType.GetMember("Tone"));
        Assert.NotNull(surfaceType.GetMember("Size"));

        // 3. Verify SeaGlassButton and custom DPs
        var buttonType = provider.GetXamlType(typeof(SeaGlassButton));
        Assert.NotNull(buttonType);
        Assert.NotNull(buttonType.GetMember("Label"));
        Assert.NotNull(buttonType.GetMember("IconGlyph"));

        // 4. Verify SeaGlassSwitch
        var switchType = provider.GetXamlType(typeof(SeaGlassSwitch));
        Assert.NotNull(switchType);
        Assert.NotNull(switchType.GetMember("IsOn"));

        // 5. Verify SeaGlassLiquidSlider
        var sliderType = provider.GetXamlType(typeof(SeaGlassLiquidSlider));
        Assert.NotNull(sliderType);
        Assert.NotNull(sliderType.GetMember("Value"));

        // 6. Verify SeaGlassBottomSheetSurface
        var sheetType = provider.GetXamlType(typeof(SeaGlassBottomSheetSurface));
        Assert.NotNull(sheetType);
        Assert.NotNull(sheetType.GetMember("Title"));
        Assert.NotNull(sheetType.GetMember("CurrentState"));

        // 7. Verify SeaGlassNavigationView
        var navType = provider.GetXamlType(typeof(SeaGlassNavigationView));
        Assert.NotNull(navType);
        Assert.NotNull(navType.GetMember("SelectedItem"));
        Assert.NotNull(navType.GetMember("PaneTitle"));
    }
}
