using System;
using System.Numerics;
using SeaGlass.Core;
using SeaGlass.WinUI;
using SeaGlass.WinUI.Rendering;
using SeaGlass.Tests.E2E.Infrastructure;
using Xunit;


namespace SeaGlass.Tests.Controls;

public sealed class WinUiControlBaselineTests
{
    [Fact]
    public void SeaGlassSurface_PointerPress_Squishes_And_Restores_Via_SpatialSpring()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassSurface surface = new();
            surface.OnPointerStateChanged(new Vector2(50f, 25f), isDown: true);

            SeaGlassSurfaceFrameState downFrame = surface.EvaluateFrame(0.02f);
            Assert.True(downFrame.ScaleX > 1.0f, "Tactile squish expands width");
            Assert.True(downFrame.ScaleY < 1.0f, "Tactile squish compresses height");

            // Release pointer
            surface.OnPointerStateChanged(Vector2.Zero, isDown: false);
            for (int i = 0; i < 30; i++)
            {
                surface.EvaluateFrame(1f / 60f);
            }

            SeaGlassSurfaceFrameState restFrame = surface.EvaluateFrame(1f / 60f);
            Assert.InRange(restFrame.ScaleX, 0.99f, 1.01f);
            Assert.InRange(restFrame.ScaleY, 0.99f, 1.01f);
        });
    }

    [Fact]
    public void DualSpring_MotionDiscipline_Enforces_DampingRatio_Ranges()
    {
        SeaGlassMotionScheme scheme = new();

        // Spatial springs must be under-damped for bounce (Rule #5 in AGENTS.md)
        Assert.InRange(scheme.FastSpatial.DampingRatio, 0.60f, 0.65f);
        Assert.InRange(scheme.DefaultSpatial.DampingRatio, 0.64f, 0.68f);
        Assert.InRange(scheme.SlowSpatial.DampingRatio, 0.70f, 0.75f);

        // Effects springs must be critically damped with zero overshoot
        Assert.Equal(1.0f, scheme.FastEffects.DampingRatio);
        Assert.Equal(1.0f, scheme.DefaultEffects.DampingRatio);
    }

    [Fact]
    public void BackdropStage_Guards_Against_Feedback_Loops()
    {
        SeaGlassStageState stage = new();
        stage.BindBackdropTarget("SharedStageBackdrop", Vector2.Zero, new Vector2(1920f, 1080f));

        // Sampling from own target in foreground must throw
        Assert.Throws<InvalidOperationException>(() =>
            stage.EnsureAcyclicForegroundSurface("SharedStageBackdrop"));
    }

    [Fact]
    public void AllPalettes_Provide_ContrastSafe_ContentColors()
    {
        foreach (SeaGlassPalette palette in SeaGlassPalette.Entries)
        {
            int tint = palette.PrimaryGlass.TintColorArgb;
            int onGlass = SeaGlassColorMath.OptimalContentColorFor(tint, 0.48f);

            var tintLab = SeaGlassColorMath.ArgbToOklab(tint);
            var onGlassLab = SeaGlassColorMath.ArgbToOklab(onGlass);

            float deltaL = Math.Abs(onGlassLab.L - tintLab.L);
            Assert.True(deltaL >= 0.30f,
                $"Palette '{palette.DisplayName}' content contrast delta ({deltaL}) must meet 0.30+ threshold");
        }
    }

    [Theory]
    [InlineData(0xFF0D1E2D, 0.75f, true)]   // Oceanic dark backdrop
    [InlineData(0xFF143D4E, 0.60f, true)]   // Deep tide dark backdrop
    [InlineData(0xFF1D4E82, 0.70f, true)]   // Obsidian tide dark glass
    [InlineData(0xFFF0F6F8, 0.40f, false)]  // Milk glass light backdrop
    [InlineData(0xFFE2F4F0, 0.45f, false)]  // Seafoam mint light backdrop
    [InlineData(0xFFFFFFFF, 0.30f, false)]  // Pure white daylight backdrop
    public void GlassAndBackdrop_Guarantees_WCAG_AA_And_Oklab_Contrast(uint bgArgb, float density, bool expectDark)
    {
        int backgroundArgb = unchecked((int)bgArgb);
        var bgLab = SeaGlassColorMath.ArgbToOklab(backgroundArgb);

        if (expectDark)
        {
            Assert.True(bgLab.L < 0.48f, "Background must be dark");
        }
        else
        {
            Assert.True(bgLab.L >= 0.48f, "Background must be light");
        }

        int optimalContentColor = SeaGlassColorMath.OptimalContentColorFor(backgroundArgb, density);
        var contentLab = SeaGlassColorMath.ArgbToOklab(optimalContentColor);

        // Assert Oklab delta L >= 0.48
        float deltaL = Math.Abs(contentLab.L - bgLab.L);
        Assert.True(deltaL >= 0.48f, $"Text contrast delta L ({deltaL:F3}) must satisfy Oklab ΔL >= 0.48 threshold");

        // Assert WCAG 2.1 AA relative luminance contrast ratio >= 4.5:1
        float yBg = ComputeWcagRelativeLuminance(backgroundArgb);
        float yContent = ComputeWcagRelativeLuminance(optimalContentColor);
        float y1 = MathF.Max(yBg, yContent);
        float y2 = MathF.Min(yBg, yContent);
        float contrastRatio = (y1 + 0.05f) / (y2 + 0.05f);
        Assert.True(contrastRatio >= 4.5f, $"WCAG 2.1 AA contrast ratio {contrastRatio:F2}:1 must be >= 4.5:1");

        if (expectDark)
        {
            Assert.True(contentLab.L >= 0.85f, $"Dark glass must produce luminous text (got L={contentLab.L:F3})");
        }
        else
        {
            Assert.True(contentLab.L <= 0.35f, $"Light glass must produce deep ink text (got L={contentLab.L:F3})");
        }
    }

    private static float ComputeWcagRelativeLuminance(int argb)
    {
        float rLin = SeaGlassColorMath.SrgbChannelToLinear((argb >> 16) & 0xFF);
        float gLin = SeaGlassColorMath.SrgbChannelToLinear((argb >> 8) & 0xFF);
        float bLin = SeaGlassColorMath.SrgbChannelToLinear(argb & 0xFF);
        return 0.2126f * rLin + 0.7152f * gLin + 0.0722f * bLin;
    }

    [Fact]
    public void Surface_Attaches_Composition_Glass_Visual_Chain_With_Blur_BeerLambert_And_SandGrain()
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            if (Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread() == null)
            {
                Microsoft.UI.Dispatching.DispatcherQueueController.CreateOnCurrentThread();
            }

            SeaGlassSurface surface = new()
            {
                Width = 240,
                Height = 64,
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(20),
                Tone = SeaGlassTone.SeafoamMint,
                Coarseness = 0.45,
                TintDensity = 0.65,
                ThicknessDp = 18.0,
                RimChalkiness = 0.82
            };

            var borderHost = new Microsoft.UI.Xaml.Controls.Border
            {
                Width = 240,
                Height = 64,
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(20)
            };

            surface.AttachGlassCompositionVisual(borderHost);

            // Assert ContainerVisual is attached
            var childVisual = Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.GetElementChildVisual(borderHost);
            Assert.NotNull(childVisual);
            var container = Assert.IsAssignableFrom<Microsoft.UI.Composition.ContainerVisual>(childVisual);
            Assert.Same(surface.GlassContainerVisual, container);

            // Assert Squircle / Geometric Clip attached and configured
            Assert.NotNull(container.Clip);
            var geomClip = Assert.IsAssignableFrom<Microsoft.UI.Composition.CompositionGeometricClip>(container.Clip);
            var roundGeom = Assert.IsAssignableFrom<Microsoft.UI.Composition.CompositionRoundedRectangleGeometry>(geomClip.Geometry);
            Assert.Equal(new Vector2(240f, 64f), roundGeom.Size);
            Assert.Equal(new Vector2(20f, 20f), roundGeom.CornerRadius);

            // Assert Layering & Transparency (Border background must be transparent)
            Assert.NotNull(borderHost.Background);
            var bgBrush = Assert.IsAssignableFrom<Microsoft.UI.Xaml.Media.SolidColorBrush>(borderHost.Background);
            Assert.Equal(Microsoft.UI.Colors.Transparent, bgBrush.Color);

            // Assert Rough-Fresnel Rim illumination
            Assert.NotNull(borderHost.BorderBrush);
            var rimBrush = Assert.IsAssignableFrom<Microsoft.UI.Xaml.Media.SolidColorBrush>(borderHost.BorderBrush);
            Assert.True(rimBrush.Color.A > 0, "Rim border brush must have non-zero alpha");

            // Assert Composition Visual Children: Blur/BeerLambert + Sand Grain
            Assert.NotNull(surface.BlurVisual);
            Assert.NotNull(surface.GrainVisual);
            Assert.True(surface.BlurVisual.IsVisible);
            Assert.True(surface.GrainVisual.IsVisible);
            Assert.True(surface.GrainVisual.Opacity > 0f);

            // Assert Effect Brush exists and has parameters
            Assert.NotNull(surface.GlassEffectBrush);

            // Assert ActiveRenderTier telemetry
            Assert.Equal(SeaGlassTieredRenderEngine.Instance.ActiveTier, surface.ActiveRenderTier);

            // Assert Tier Fallback behavior when in LightweightTint
            SeaGlassTieredRenderEngine.Instance.SetTierOverride(SeaGlassDeviceTier.LightweightTint);
            try
            {
                surface.UpdateGlassCompositionParameters();
                Assert.False(surface.BlurVisual.IsVisible, "Blur visual must be hidden in LightweightTint");
                Assert.False(surface.GrainVisual.IsVisible, "Grain visual must be hidden in LightweightTint");
                var fallbackBg = Assert.IsAssignableFrom<Microsoft.UI.Xaml.Media.SolidColorBrush>(borderHost.Background);
                Assert.NotEqual(Microsoft.UI.Colors.Transparent, fallbackBg.Color);

                // Restore
                SeaGlassTieredRenderEngine.Instance.SetTierOverride(null);
                surface.UpdateGlassCompositionParameters();
                Assert.True(surface.BlurVisual.IsVisible, "Blur visual restored in Hybrid tier");
                Assert.True(surface.GrainVisual.IsVisible, "Grain visual restored in Hybrid tier");
                Assert.Equal(Microsoft.UI.Colors.Transparent, ((Microsoft.UI.Xaml.Media.SolidColorBrush)borderHost.Background).Color);
            }
            finally
            {
                SeaGlassTieredRenderEngine.Instance.SetTierOverride(null);
            }
        });
    }

    [Theory]
    [InlineData("SeaGlass.WinUI.SeaGlassButton", "Tactile Button")]
    [InlineData("SeaGlass.WinUI.SeaGlassExtendedFab", "Add Action")]
    [InlineData("SeaGlass.WinUI.SeaGlassFilterChip", "Seafoam Filter")]
    [InlineData("SeaGlass.WinUI.SeaGlassPill", "Diagnostic Pill")]
    public void ButtonFamilyControls_HaveExactlyOneVisibleTextElement_PerLabel(string fullTypeName, string labelText)
    {
        WinUiControlTestHarness.RunOnStaThread(() =>
        {
            SeaGlassButton btn = fullTypeName switch
            {
                "SeaGlass.WinUI.SeaGlassButton" => new SeaGlassButton(),
                "SeaGlass.WinUI.SeaGlassExtendedFab" => new SeaGlassExtendedFab(),
                "SeaGlass.WinUI.SeaGlassFilterChip" => new SeaGlassFilterChip(),
                "SeaGlass.WinUI.SeaGlassPill" => new SeaGlassPill(),
                _ => throw new System.ArgumentException(fullTypeName)
            };

            var iconPresenter = new Microsoft.UI.Xaml.Controls.ContentPresenter();
            var labelPresenter = new Microsoft.UI.Xaml.Controls.TextBlock();
            var contentPresenter = new Microsoft.UI.Xaml.Controls.ContentPresenter();

            btn.Label = labelText;
            btn.SetPresentersForTesting(iconPresenter, labelPresenter, contentPresenter);

            // Assert that LabelPresenter is resolved and Visible
            Assert.NotNull(btn.LabelPresenter);
            Assert.Equal(Microsoft.UI.Xaml.Visibility.Visible, btn.LabelPresenter.Visibility);
            Assert.Equal(labelText, btn.LabelPresenter.Text);

            // Assert that IconPresenter is Collapsed (no icon set)
            Assert.NotNull(btn.IconPresenter);
            Assert.Equal(Microsoft.UI.Xaml.Visibility.Collapsed, btn.IconPresenter.Visibility);

            // Assert that ContentPresenter is Collapsed (so label is not rendered twice)
            Assert.NotNull(btn.ContentPresenterChild);
            Assert.Equal(Microsoft.UI.Xaml.Visibility.Collapsed, btn.ContentPresenterChild.Visibility);
        });
    }
}




