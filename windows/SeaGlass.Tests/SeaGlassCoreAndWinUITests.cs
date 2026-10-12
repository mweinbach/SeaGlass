using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using SeaGlass.Core;
using SeaGlass.Core.Backend;
using SeaGlass.WinUI;
using Xunit;

namespace SeaGlass.Tests;

/// <summary>
/// Comprehensive verification suite for <c>SeaGlass.Core</c> and <c>SeaGlass.WinUI</c>.
/// Mirrors every test in <c>android/seaglass-core/src/test/kotlin/dev/seaglass/core/SeaGlassCoreTest.kt</c>
/// plus WinUI 3 / Composition spring, HLSL shader, and 30+ UI Kit component state tests.
/// </summary>
public sealed class SeaGlassCoreAndWinUITests
{
    [Fact]
    public void OklabRoundTripPreservesSeaGlassPastelColors()
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

            Assert.True(rDiff <= 2, $"Red channel drifted by {rDiff} for {palette.DisplayName}");
            Assert.True(gDiff <= 2, $"Green channel drifted by {gDiff} for {palette.DisplayName}");
            Assert.True(bDiff <= 2, $"Blue channel drifted by {bDiff} for {palette.DisplayName}");
        }
    }

    [Fact]
    public void BeerLambertVolumetricAbsorptionShiftsHueAndIncreasesWithDensity()
    {
        int whiteBackdrop = unchecked((int)0xFFFFFFFF);
        int cobaltTint = SeaGlassPalette.CobaltTide.PrimaryGlass.TintColorArgb;

        int lightTransmission = SeaGlassColorMath.ComputeBeerLambertTransmission(
            backdropArgb: whiteBackdrop,
            tintArgb: cobaltTint,
            tintDensity: 0.25f,
            coarseness: 0.30f,
            normalizedThickness: 1.0f);

        int deepTransmission = SeaGlassColorMath.ComputeBeerLambertTransmission(
            backdropArgb: whiteBackdrop,
            tintArgb: cobaltTint,
            tintDensity: 0.75f,
            coarseness: 0.30f,
            normalizedThickness: 1.6f);

        int lightRed = (int)((unchecked((uint)lightTransmission) >> 16) & 0xFF);
        int lightBlue = (int)(unchecked((uint)lightTransmission) & 0xFF);
        int deepRed = (int)((unchecked((uint)deepTransmission) >> 16) & 0xFF);
        int deepBlue = (int)(unchecked((uint)deepTransmission) & 0xFF);

        Assert.True(lightBlue > lightRed, "Cobalt sea glass must transmit more blue than red");
        Assert.True(deepRed < lightRed, $"Deeper cobalt slab must absorb more red ({deepRed} < {lightRed})");
        Assert.True(deepBlue > deepRed, "Deeper cobalt slab must remain blue-dominant");
    }

    [Fact]
    public void SdfEngineEvaluatesSquircleAndSmoothMinCoalescing()
    {
        SeaGlassSdfEngine.SdfNode nodeA = new(
            centerX: 100f,
            centerY: 100f,
            halfWidth: 40f,
            halfHeight: 40f,
            uniformCornerRadius: 20f,
            cornerSmoothing: 2.5f,
            tintColorArgb: SeaGlassPalette.SeafoamMint.PrimaryGlass.TintColorArgb);

        SeaGlassSdfEngine.SdfNode nodeB = new(
            centerX: 190f,
            centerY: 100f,
            halfWidth: 40f,
            halfHeight: 40f,
            uniformCornerRadius: 20f,
            cornerSmoothing: 2.5f,
            tintColorArgb: SeaGlassPalette.CobaltTide.PrimaryGlass.TintColorArgb);

        float distCenterA = SeaGlassSdfEngine.EvaluateNodeSdf(100f, 100f, nodeA);
        Assert.InRange(distCenterA, -40.5f, -39.5f);

        float dMidA = SeaGlassSdfEngine.EvaluateNodeSdf(145f, 100f, nodeA);
        float dMidB = SeaGlassSdfEngine.EvaluateNodeSdf(145f, 100f, nodeB);
        Assert.InRange(dMidA, 4.95f, 5.05f);
        Assert.InRange(dMidB, 4.95f, 5.05f);

        SeaGlassSdfEngine.SdfEvaluation sceneMerged = SeaGlassSdfEngine.EvaluateScene(
            px: 145f,
            py: 100f,
            nodes: [nodeA, nodeB],
            smoothRadiusPx: 36f);

        Assert.True(
            sceneMerged.SignedDistancePx < 0f,
            $"Midpoint between nearby nodes should coalesce inside bridge (d={sceneMerged.SignedDistancePx})");

        float nLen = MathF.Sqrt(
            sceneMerged.NormalX * sceneMerged.NormalX +
            sceneMerged.NormalY * sceneMerged.NormalY +
            sceneMerged.NormalZ * sceneMerged.NormalZ);
        Assert.InRange(nLen, 0.999f, 1.001f);
    }

    [Fact]
    public void CbufferAndStd140UniformBlockMatches256ByteHlslVulkanAndGles3Abi()
    {
        Assert.Equal(256, Marshal.SizeOf<SeaGlassUniformBlockStd140>());

        SeaGlassUniformBlock block = new();
        SeaGlassMaterial material = SeaGlassPalette.SeaLavender.PrimaryGlass;
        List<SeaGlassSdfEngine.SdfNode> nodes =
        [
            new SeaGlassSdfEngine.SdfNode(
                centerX: 240f,
                centerY: 120f,
                halfWidth: 180f,
                halfHeight: 56f,
                uniformCornerRadius: 56f,
                cornerSmoothing: 2.5f,
                tintColorArgb: material.TintColorArgb,
                thicknessPx: 28f),
            new SeaGlassSdfEngine.SdfNode(
                centerX: 460f,
                centerY: 120f,
                halfWidth: 56f,
                halfHeight: 56f,
                uniformCornerRadius: 56f,
                cornerSmoothing: 2.5f,
                tintColorArgb: SeaGlassPalette.RoseQuartzShell.PrimaryGlass.TintColorArgb,
                thicknessPx: 32f)
        ];

        block.Pack(
            widthPx: 600f,
            heightPx: 240f,
            backdropOffsetX: 32f,
            backdropOffsetY: 840f,
            material: material,
            density: 2.0f,
            nodes: nodes,
            smoothRadiusPx: 44f);

        byte[] directBuffer = block.CreateDirectStagingBuffer();
        Assert.Equal(256, directBuffer.Length);

        SeaGlassUniformBlockStd140 std140 = block.ToStd140Struct();
        Assert.Equal(600f, std140.ResolutionAndOrigin.X, 3);
        Assert.Equal(240f, std140.ResolutionAndOrigin.Y, 3);
        Assert.Equal(material.TintDensity, std140.OpticsPrimary.X, 4);
        Assert.Equal(material.Coarseness, std140.OpticsPrimary.Y, 4);
        Assert.Equal(material.Ior, std140.OpticsPrimary.Z, 4);
        Assert.Equal(2f, std140.LightAndMeta.W, 4);

        Direct2DHlslSeaGlassBackend d2dBackend = new();
        d2dBackend.SyncUniforms(block);
        Assert.Equal(256, d2dBackend.StagingBuffer.Length);
        SeaGlassRenderGraphDescriptor d2dGraph = d2dBackend.DescribeRenderGraph(frostRadiusPx: 32f);
        Assert.Equal(4, d2dGraph.KawasePyramidLevels);
        Assert.Equal(256, d2dGraph.UboByteSize);

        VulkanSeaGlassBackend vulkanBackend = new();
        vulkanBackend.SyncUniforms(block);
        Assert.Equal(256, vulkanBackend.StagingBuffer.Length);
    }

    [Fact]
    public void ShaderCompilerEmitsConsistentHlslD2D1VulkanGles3AndAgslDialects()
    {
        CompiledShaderBundle hlslBundle = SeaGlassShaderCompiler.CompileForDialect(ShaderDialect.HlslSm50);
        CompiledShaderBundle d2dBundle = SeaGlassShaderCompiler.CompileForDialect(ShaderDialect.D2D1Hlsl);
        CompiledShaderBundle vulkanBundle = SeaGlassShaderCompiler.CompileForDialect(ShaderDialect.VulkanGlsl450);
        CompiledShaderBundle gles3Bundle = SeaGlassShaderCompiler.CompileForDialect(ShaderDialect.GlslEs300);
        CompiledShaderBundle agslBundle = SeaGlassShaderCompiler.CompileForDialect(ShaderDialect.Agsl100);

        Assert.Contains("cbuffer SeaGlassUniforms : register(b0)", hlslBundle.SeaGlassCompositeFragmentSource);
        Assert.Contains("Texture2D<float4> uBackdropTex : register(t0)", hlslBundle.SeaGlassCompositeFragmentSource);
        Assert.Contains("sgLinearSrgbToOklab", hlslBundle.SeaGlassCompositeFragmentSource);
        Assert.Contains("sgSandMicroFacet", hlslBundle.SeaGlassCompositeFragmentSource);
        Assert.Contains("sgSminCubic", hlslBundle.SeaGlassCompositeFragmentSource);
        Assert.NotNull(hlslBundle.KawaseDownsampleFragmentSource);
        Assert.NotNull(hlslBundle.KawaseUpsampleFragmentSource);

        Assert.Contains("D2D_PS_ENTRY(PSMain)", d2dBundle.SeaGlassCompositeFragmentSource);
        Assert.Contains("D2DSampleInputAtOffset", d2dBundle.SeaGlassCompositeFragmentSource);
        Assert.NotNull(d2dBundle.SandGrainOverlayFragmentSource);
        Assert.NotNull(d2dBundle.SdfMaskFragmentSource);

        Assert.Contains("#version 450 core", vulkanBundle.SeaGlassCompositeFragmentSource);
        Assert.Contains("layout(set = 0, binding = 0, std140) uniform SeaGlassUniforms", vulkanBundle.SeaGlassCompositeFragmentSource);

        Assert.Contains("#version 300 es", gles3Bundle.SeaGlassCompositeFragmentSource);
        Assert.Contains("layout(std140) uniform SeaGlassUniforms", gles3Bundle.SeaGlassCompositeFragmentSource);

        Assert.Contains("uniform shader backdrop;", agslBundle.SeaGlassCompositeFragmentSource);
        Assert.Contains("half4 main(float2 fragCoord)", agslBundle.SeaGlassCompositeFragmentSource);
    }

    [Fact]
    public void DynamicFlexMaterialAnd20ShapesBoostLensingAndSupportCircleBridgeChoreography()
    {
        SeaGlassMaterial start = SeaGlassPalette.SeafoamMint.PrimaryGlass;
        SeaGlassMaterial end = SeaGlassPalette.CobaltTide.SurfaceGlass;

        SeaGlassMaterial atRestStart = SeaGlassShapeMorph.ComputeDynamicFlexMaterial(
            startMaterial: start,
            endMaterial: end,
            progress: 0f,
            areaExpansionRatio: 1f);

        SeaGlassMaterial midFlight = SeaGlassShapeMorph.ComputeDynamicFlexMaterial(
            startMaterial: start,
            endMaterial: end,
            progress: 0.5f,
            velocityPxPerSec: 900f,
            areaExpansionRatio: 2.25f);

        SeaGlassMaterial atRestEnd = SeaGlassShapeMorph.ComputeDynamicFlexMaterial(
            startMaterial: start,
            endMaterial: end,
            progress: 1f,
            areaExpansionRatio: 1f);

        Assert.True(midFlight.Ior > atRestStart.Ior);
        Assert.True(midFlight.ChromaticDispersion > atRestStart.ChromaticDispersion);
        Assert.True(midFlight.ThicknessDp > atRestStart.ThicknessDp);
        Assert.True(midFlight.Coarseness < atRestStart.Coarseness * 0.25f);
        Assert.True(midFlight.Transparency > atRestStart.Transparency + 0.15f);
        Assert.Equal(end.Coarseness, atRestEnd.Coarseness, 4);
        Assert.Equal(end.Transparency, atRestEnd.Transparency, 4);

        foreach (SeaGlassShapeKind kind in Enum.GetValues<SeaGlassShapeKind>())
        {
            SeaGlassNormalizedPolygon poly = SeaGlassShapeMorph.CreateNormalizedPolygon(kind, aspectRatio: 1.4f);
            Assert.NotNull(poly);
            Assert.NotEmpty(poly.Cubics);

            float polarScale = SeaGlassShapeMorph.EvaluatePolarRadiusScale(
                startKind: kind,
                endKind: SeaGlassShapeKind.Circle,
                progress: 0.5f,
                angleRad: 0.78f);
            Assert.InRange(polarScale, 0.35f, 1.15f);
        }

        // Verify polar polygon SDF nodes coalesce via sminCubic
        SeaGlassSdfEngine.SdfNode cookieNode = new(
            centerX: 100f,
            centerY: 100f,
            halfWidth: 45f,
            halfHeight: 45f,
            uniformCornerRadius: 45f,
            startShapeKind: SeaGlassShapeKind.Cookie9,
            endShapeKind: SeaGlassShapeKind.Clover4,
            shapeMorphProgress: 0.4f);

        SeaGlassSdfEngine.SdfNode flowerNode = new(
            centerX: 180f,
            centerY: 100f,
            halfWidth: 45f,
            halfHeight: 45f,
            uniformCornerRadius: 45f,
            startShapeKind: SeaGlassShapeKind.Flower,
            endShapeKind: SeaGlassShapeKind.Heart,
            shapeMorphProgress: 0.4f);

        SeaGlassSdfEngine.SdfEvaluation eval = SeaGlassSdfEngine.EvaluateScene(
            px: 140f,
            py: 100f,
            nodes: [cookieNode, flowerNode],
            smoothRadiusPx: 42f);
        Assert.True(eval.IsInside);

        // Verify two-phase via-Circle choreographed shape morph (Flower -> Circle -> Cookie9)
        SeaGlassShapeStage stageFlower = new(SeaGlassShapeKind.Flower, Scale: 1.0f, RotationDegrees: 0f, ViaCircleBridge: true);
        SeaGlassShapeStage stageCookie = new(SeaGlassShapeKind.Cookie9, Scale: 1.2f, RotationDegrees: 90f, ViaCircleBridge: true);

        ChoreographedShapeTransform phase1 = SeaGlassShapeMorph.EvaluateChoreographedTransform(stageFlower, stageCookie, 0.25f);
        Assert.Equal(SeaGlassShapeKind.Flower, phase1.ActiveStartShape);
        Assert.Equal(SeaGlassShapeKind.Circle, phase1.ActiveEndShape);

        ChoreographedShapeTransform phase2Overshoot = SeaGlassShapeMorph.EvaluateChoreographedTransform(stageFlower, stageCookie, 1.12f);
        Assert.Equal(SeaGlassShapeKind.Circle, phase2Overshoot.ActiveStartShape);
        Assert.Equal(SeaGlassShapeKind.Cookie9, phase2Overshoot.ActiveEndShape);
        Assert.True(phase2Overshoot.ActiveSubProgress > 1.0f);
    }

    [Fact]
    public void AdaptiveDeviceAndPageCornerSolverMatchesConcentricAndAsymmetricBezelRadii()
    {
        SeaGlassCornerRadiiPx bezelCorners = new(120f, 120f, 120f, 120f);

        SeaGlassCornerRadiiPx tlPodRadii = SeaGlassCornerAdaptationSolver.SolveElementCornerRadiiPx(
            elementLeftPx: 10f,
            elementTopPx: 10f,
            elementRightPx: 270f,
            elementBottomPx: 170f,
            containerWidthPx: 1080f,
            containerHeightPx: 2400f,
            containerCornersPx: bezelCorners,
            baseCornerRadiusPx: 40f,
            minCornerFloorPx: 16f,
            influenceDistancePx: 240f,
            mode: SeaGlassCornerAdaptationMode.FlushCornerEmbed);

        Assert.True(tlPodRadii.TopLeftPx > tlPodRadii.BottomRightPx + 20f);
        Assert.True(tlPodRadii.IsAsymmetric);

        SeaGlassCornerRadiiPx foldBezel = new(38f, 120f, 120f, 38f);
        SeaGlassCornerRadiiPx fullWidthHeader = SeaGlassCornerAdaptationSolver.SolveElementCornerRadiiPx(
            elementLeftPx: 12f,
            elementTopPx: 12f,
            elementRightPx: 1068f,
            elementBottomPx: 180f,
            containerWidthPx: 1080f,
            containerHeightPx: 2400f,
            containerCornersPx: foldBezel,
            baseCornerRadiusPx: 56f,
            minCornerFloorPx: 16f,
            influenceDistancePx: 280f,
            mode: SeaGlassCornerAdaptationMode.ConcentricProximity);

        Assert.True(fullWidthHeader.TopRightPx > fullWidthHeader.TopLeftPx + 30f);

        SeaGlassCornerRadiiPx deepBezel = SeaGlassCornerRadiiPx.Uniform(145f);
        SeaGlassCornerRadiiPx deepPodRadii = SeaGlassCornerAdaptationSolver.SolveElementCornerRadiiPx(
            elementLeftPx: 8f,
            elementTopPx: 8f,
            elementRightPx: 398f,
            elementBottomPx: 208f,
            containerWidthPx: 900f,
            containerHeightPx: 540f,
            containerCornersPx: deepBezel,
            baseCornerRadiusPx: 48f,
            minCornerFloorPx: 20f,
            influenceDistancePx: 250f,
            mode: SeaGlassCornerAdaptationMode.FlushCornerEmbed);

        Assert.True(deepPodRadii.TopLeftPx > 125f);

        SeaGlassCornerPodLayoutPx podLayout = SeaGlassCornerAdaptationSolver.SolveCornerPodLayoutPx(
            containerWidthPx: 600f,
            containerHeightPx: 360f,
            containerCornersPx: deepBezel,
            requestedInsetPx: 80f,
            minHorizontalGapPx: 32f,
            minVerticalGapPx: 28f,
            preferredPodWidthPx: 340f,
            preferredTopPodHeightPx: 180f,
            preferredBottomDockHeightPx: 160f);

        Assert.True(podLayout.HorizontalGapPx >= 31.9f);
        Assert.True(podLayout.VerticalGapPx >= 27.9f);
    }

    [Fact]
    public void WinUIComponentSuiteAndDualSpringsMaintain1To1ParityWithAndroid()
    {
        SeaGlassMotionScheme motion = new();
        Assert.True(motion.FastSpatial.IsUnderDamped);
        Assert.True(motion.DefaultSpatial.IsUnderDamped);
        Assert.True(motion.SlowSpatial.IsUnderDamped);
        Assert.True(motion.FastEffects.IsCriticallyDamped);
        Assert.True(motion.DefaultEffects.IsCriticallyDamped);

        // Verify acyclic backdrop stage guard (Rule #4)
        SeaGlassStageState stage = new();
        stage.BindBackdropTarget("MainBackdropLayer", Vector2.Zero, new Vector2(1920f, 1080f));
        Assert.Throws<InvalidOperationException>(() => stage.EnsureAcyclicForegroundSurface("MainBackdropLayer"));

        // Verify WinUI components evaluate 256-byte uniform blocks and XAML contour geometry
        SeaGlassFluidBar fluidBar = new(
            items: ["Tide", "Shells", "Optics", "Palette"],
            selectedIndex: 0,
            onSelect: _ => { },
            stageState: stage,
            showActionOrb: true,
            actionDetached: false);
        SeaGlassSurfaceFrameState barFrame = fluidBar.StepAndEvaluate(1f / 60f);
        Assert.StartsWith("F1 M", barFrame.XamlContourPathData);
        Assert.Equal(2f, barFrame.UniformBlock.ToStd140Struct().LightAndMeta.W, 3);

        SeaGlassCheckbox checkbox = new(isChecked: false, onCheckedChange: _ => { }, stageState: stage);
        checkbox.Toggle();
        Assert.True(checkbox.Checked);
        SeaGlassSurfaceFrameState checkFrame = checkbox.StepAndEvaluate(0.05f);
        Assert.StartsWith("F1 M", checkFrame.XamlContourPathData);

        SeaGlassMorphingContextMenu menu = new(expanded: true, stageState: stage);
        SeaGlassSurfaceFrameState menuFrame = menu.StepAndEvaluate(0.04f);
        Assert.NotEmpty(menuFrame.XamlContourPathData);

        SeaGlassCascadedModelMorph cascaded = new(stage: 2, stageState: stage);
        SeaGlassSurfaceFrameState cascadedFrame = cascaded.StepAndEvaluate(0.04f);
        Assert.NotEmpty(cascadedFrame.XamlContourPathData);
    }

    [Fact]
    public void All30PlusWinUIComponentsEvaluateOpticsAndValidXamlContours()
    {
        SeaGlassStageState stage = new();
        stage.BindBackdropTarget("StageBackdrop", Vector2.Zero, new Vector2(1920f, 1080f));

        // 1. All 20 vector glyphs
        foreach (SeaGlassGlyph glyph in Enum.GetValues<SeaGlassGlyph>())
        {
            Assert.NotEmpty(SeaGlassIcon.ToUnicodeSymbol(glyph));
            string path = SeaGlassIcon.BuildGlyphXamlPathData(glyph);
            Assert.StartsWith("F1 M", path);
        }

        // 2. Core & UI Kit Controls
        SeaGlassButton btn = new("Primary Action", () => { }, stage);
        btn.OnPointerStateChanged(new Vector2(20f, 10f), true);
        SeaGlassSurfaceFrameState btnFrame = btn.EvaluateFrame(0.03f);
        Assert.True(btnFrame.ScaleX > 1.0f);
        Assert.True(btnFrame.ScaleY < 1.0f);

        SeaGlassIconButton iconBtn = new(SeaGlassGlyph.Sparkles, () => { }, stage, selected: true);
        Assert.StartsWith("F1 M", iconBtn.EvaluateFrame().XamlContourPathData);

        SeaGlassPill pill = new("Seafoam", true, () => { }, stage);
        Assert.Equal(26f, pill.Material.CornerRadiusDp);
        pill.SetSelected(false);
        Assert.Equal(18f, pill.Material.CornerRadiusDp);

        SeaGlassFilterChip filterChip = new("Refraction", true, () => { }, stage);
        Assert.Equal(SeaGlassGlyph.Check, filterChip.LeadingGlyph);

        SeaGlassBadge badge = new("46dp", stage);
        Assert.StartsWith("F1 M", badge.EvaluateFrame().XamlContourPathData);

        SeaGlassExtendedFab fab = new("Create Orb", SeaGlassGlyph.Plus, true, () => { }, stage);
        Assert.Equal(28f, fab.Material.CornerRadiusDp);
        fab.SetExpanded(false);
        Assert.Equal(20f, fab.Material.CornerRadiusDp);

        SeaGlassTopAppBar topAppBar = new("SeaGlass Lab", stage, "WinUI 3");
        Assert.StartsWith("F1 M", topAppBar.EvaluateFrame().XamlContourPathData);

        SeaGlassMiniPlayerBar miniPlayer = new("Ocean Tide", "SeaGlass", true, () => { }, () => { }, stage);
        Assert.StartsWith("F1 M", miniPlayer.EvaluateFrame().XamlContourPathData);

        SeaGlassFloatingToolbar toolbar = new(
            [(SeaGlassGlyph.Home, "Home"), (SeaGlassGlyph.Tune, "Optics")],
            0, _ => { }, SeaGlassGlyph.Plus, true, () => { }, stage);
        Assert.Equal(2f, toolbar.StepAndEvaluate(0.03f).UniformBlock.ToStd140Struct().LightAndMeta.W, 3);

        SeaGlassSearchBar searchBar = new("oklab", _ => { }, stage) { IsFocused = true };
        Assert.True(searchBar.StepFocusAndEvaluate(0.05f).EffectiveMaterial.ThicknessDp > 14f);
        Assert.True(searchBar.Material.ThicknessDp > 16f);

        SeaGlassTextField textField = new("1.54", _ => { }, stage, "IOR") { IsFocused = true };
        Assert.StartsWith("F1 M", textField.StepFocusAndEvaluate(0.05f).XamlContourPathData);

        SeaGlassSwitch toggle = new(false, _ => { }, stage);
        toggle.Toggle();
        Assert.True(toggle.StepAndEvaluate(0.03f).EffectiveMaterial.ThicknessDp >= 12f);

        SeaGlassRadioButton radio = new(true, () => { }, stage);
        Assert.True(radio.StepAndEvaluate(0.03f).EffectiveMaterial.ThicknessDp > 12f);

        SeaGlassSegmentedButtonRow segRow = new(["Flush", "Inset", "Page"], 1, _ => { }, stage);
        segRow.SelectOption(2);
        Assert.True(segRow.StepAndEvaluate(0.02f).EffectiveMaterial.CornerRadiusDp > 0f);
        Assert.True(segRow.IndicatorScaleX >= 1.0f);

        SeaGlassLiquidSlider slider = new("Coarseness", "0.42", 0.42f, 0f, 1f, _ => { }, stage) { IsDragging = true };
        SeaGlassSurfaceFrameState sliderFrame = slider.StepAndEvaluate(0.04f);
        Assert.True(slider.ThumbScaleX > 1.0f);
        Assert.StartsWith("F1 M", sliderFrame.XamlContourPathData);

        SeaGlassLinearProgressIndicator linProg = new(0.68f, stage);
        Assert.InRange(linProg.StepAndEvaluate(0.03f).EffectiveMaterial.CornerRadiusDp, 6f, 8f);

        SeaGlassCircularProgressIndicator circProg = new(0.75f, stage);
        circProg.StepAndEvaluate(0.03f);
        Assert.InRange(circProg.SweepAngleDegrees, 250f, 280f);

        SeaGlassListItem listItem = new("Volumetric Absorption", stage, "Beer-Lambert Oklab", SeaGlassGlyph.Tune);
        Assert.StartsWith("F1 M", listItem.EvaluateFrame().XamlContourPathData);

        SeaGlassSnackbar snackbar = new("Shader compiled", stage, "Inspect", () => { });
        Assert.StartsWith("F1 M", snackbar.EvaluateFrame().XamlContourPathData);

        SeaGlassDialogCard dialog = new("Reset Optics?", "Restore default Seafoam Mint parameters.", "Reset", () => { }, stage);
        Assert.StartsWith("F1 M", dialog.EvaluateFrame().XamlContourPathData);

        SeaGlassBottomSheetSurface sheet = new("Optical Telemetry", stage, "256-Byte cbuffer");
        Assert.StartsWith("F1 M", sheet.EvaluateFrame().XamlContourPathData);

        SeaGlassCornerEmbeddedPod pod = new("TL Corner Pod", "Outer 52dp", stage);
        Assert.Equal(SeaGlassCornerAdaptationMode.FlushCornerEmbed, pod.CornerAdaptationMode);

        // 3. Dynamic Morphing Suite
        SeaGlassBuddingCluster cluster = new(true, [("✦", "Share"), ("♡", "Like")], _ => { }, stage);
        Assert.True(cluster.StepAndEvaluate(0.05f).UniformBlock.ToStd140Struct().LightAndMeta.W >= 2f);

        SeaGlassMorphingButton morphBtn = new(true, () => { }, stage);
        Assert.StartsWith("F1 M", morphBtn.StepAndEvaluate(0.05f).XamlContourPathData);

        SeaGlassChoreographedShapeOrb orb = new(stage);
        orb.AdvanceTo(1);
        Assert.StartsWith("F1 M", orb.StepAndEvaluate(0.05f).XamlContourPathData);

        SeaGlassMultiShapeMorph multiMorph = new([SeaGlassShapeKind.Flower, SeaGlassShapeKind.Cookie9, SeaGlassShapeKind.Heart], 1, stage);
        Assert.StartsWith("F1 M", multiMorph.StepAndEvaluate(0.05f).XamlContourPathData);

        SeaGlassContextRingMorph ringMorph = new(expanded: true, progress: 0.42f, stageState: stage);
        Assert.StartsWith("F1 M", ringMorph.StepAndEvaluate(0.05f).XamlContourPathData);

        SeaGlassAdaptiveCornerLabCard cornerLab = new(SeaGlassDeviceHardwareProfile.Pixel9Pro, stage);
        var (layout, tl, tr, dock) = cornerLab.SolveBezelFrame();
        Assert.True(layout.PodWidthPx > 0f);
        Assert.True(tl.TopLeftPx > tl.BottomRightPx);
        Assert.True(tr.TopRightPx > tr.BottomLeftPx);
        Assert.True(dock.BottomLeftPx > 0f);

        // 4. iOS 27 & Floating Concentric Sheet Components + SeaGlassStageHost
        SeaGlassStageHost stageHost = new(new Vector2(1440f, 920f));
        Assert.Same(stageHost.StageState, SeaGlassStageHost.AmbientStageState);

        SeaGlassFloatingSheet floatingSheet = new(true, () => { });
        floatingSheet.SwitchSubPage(1);
        Assert.StartsWith("F1 M", floatingSheet.StepAndEvaluate(0.04f).XamlContourPathData);

        SeaGlassContextMenuOverlay contextMenu = new(true, () => { }, _ => { });
        contextMenu.ActivateItem(contextMenu.Items[1]); // Submenu item
        Assert.NotNull(contextMenu.ActiveSubmenuParent);
        Assert.StartsWith("F1 M", contextMenu.StepAndEvaluate(0.04f).XamlContourPathData);

        SeaGlassActionSheet actionSheet = new("Optical Preset Actions", true, () => { });
        Assert.StartsWith("F1 M", actionSheet.StepAndEvaluate(0.04f).XamlContourPathData);

        SeaGlassLiveActivityPill liveActivity = new(expanded: true);
        Assert.StartsWith("F1 M", liveActivity.StepAndEvaluate(0.04f).XamlContourPathData);

        SeaGlassMagnifierScrubber magnifierScrubber = new(0.62f, _ => { });
        SeaGlassSurfaceFrameState scrubberFrame = magnifierScrubber.StepAndEvaluate(0.04f);
        Assert.StartsWith("F1 M", scrubberFrame.XamlContourPathData);
        Assert.Contains("Frame", magnifierScrubber.FormatValueLabel());

        SeaGlassNotificationStack notifStack = new(expanded: true);
        Assert.StartsWith("F1 M", notifStack.StepAndEvaluate(0.04f).XamlContourPathData);

        SeaGlassWheelPicker wheelPicker = new(["AGSL 1.0", "HLSL SM 5.0", "Vulkan GLSL 450", "GLES 3.00"], 1, _ => { });
        var (angleRad, scale, alpha) = wheelPicker.EvaluateDrumRowTransform(1);
        Assert.Equal(0f, angleRad, 3);
        Assert.True(scale > 1.0f);
        Assert.Equal(1.0f, alpha, 3);
        Assert.StartsWith("F1 M", wheelPicker.StepAndEvaluate(0.04f).XamlContourPathData);

        SeaGlassControlCenterGrid ccGrid = new();
        ccGrid.TogglePlatterExpansion(0);
        Assert.StartsWith("F1 M", ccGrid.StepAndEvaluate(0.04f).XamlContourPathData);
    }

    [Fact]
    public void CornerAdaptationRespectsDeveloperMaskProximityDistanceAndSymmetry()
    {
        SeaGlassCornerRadiiPx bezel = SeaGlassCornerRadiiPx.Uniform(140f);
        const float baseRadiusPx = 64f;

        // 1. Outside proximityThresholdDp (inset = 90px > influenceDistancePx = 70px):
        SeaGlassCornerRadiiPx outsideThreshold = SeaGlassCornerAdaptationSolver.SolveElementCornerRadiiPx(
            elementLeftPx: 90f,
            elementTopPx: 90f,
            elementRightPx: 990f,
            elementBottomPx: 450f,
            containerWidthPx: 1080f,
            containerHeightPx: 2400f,
            containerCornersPx: bezel,
            baseCornerRadiusPx: baseRadiusPx,
            influenceDistancePx: 70f,
            mode: SeaGlassCornerAdaptationMode.ConcentricProximity);
        Assert.Equal(baseRadiusPx, outsideThreshold.TopLeftPx, 2);
        Assert.Equal(baseRadiusPx, outsideThreshold.TopRightPx, 2);
        Assert.Equal(baseRadiusPx, outsideThreshold.BottomLeftPx, 2);
        Assert.Equal(baseRadiusPx, outsideThreshold.BottomRightPx, 2);

        // 2. Corner Mask (BottomOnly): only bottom corners adapt while top corners stay locked at baseRadiusPx.
        SeaGlassCornerRadiiPx bottomOnlyRadii = SeaGlassCornerAdaptationSolver.SolveElementCornerRadiiPx(
            elementLeftPx: 10f,
            elementTopPx: 10f,
            elementRightPx: 1070f,
            elementBottomPx: 2390f,
            containerWidthPx: 1080f,
            containerHeightPx: 2400f,
            containerCornersPx: bezel,
            baseCornerRadiusPx: baseRadiusPx,
            influenceDistancePx: 120f,
            mode: SeaGlassCornerAdaptationMode.ConcentricProximity,
            cornerMask: SeaGlassCornerMask.BottomOnly,
            allowAsymmetricCorners: true);
        Assert.Equal(baseRadiusPx, bottomOnlyRadii.TopLeftPx, 2);
        Assert.Equal(baseRadiusPx, bottomOnlyRadii.TopRightPx, 2);
        Assert.True(bottomOnlyRadii.BottomLeftPx > baseRadiusPx + 35f);

        // 3. Monotonic non-pinching guarantee at intermediate distance
        SeaGlassCornerRadiiPx midDistanceRadii = SeaGlassCornerAdaptationSolver.SolveElementCornerRadiiPx(
            elementLeftPx: 95f,
            elementTopPx: 95f,
            elementRightPx: 985f,
            elementBottomPx: 600f,
            containerWidthPx: 1080f,
            containerHeightPx: 2400f,
            containerCornersPx: bezel,
            baseCornerRadiusPx: baseRadiusPx,
            influenceDistancePx: 180f,
            mode: SeaGlassCornerAdaptationMode.ConcentricProximity);
        Assert.True(midDistanceRadii.TopLeftPx >= baseRadiusPx - 0.01f);

        // 4. allowAsymmetricCorners = false keeps all 4 corners uniform on a bottom-anchored card
        SeaGlassCornerRadiiPx symmetricCard = SeaGlassCornerAdaptationSolver.SolveElementCornerRadiiPx(
            elementLeftPx: 12f,
            elementTopPx: 1800f,
            elementRightPx: 1068f,
            elementBottomPx: 2388f,
            containerWidthPx: 1080f,
            containerHeightPx: 2400f,
            containerCornersPx: bezel,
            baseCornerRadiusPx: baseRadiusPx,
            config: SeaGlassCornerAdaptationConfig.ConcentricProximity(
                proximityThresholdDp: 40f,
                allowAsymmetricCorners: false),
            density: 2.75f);
        Assert.False(symmetricCard.IsAsymmetric);
        Assert.True(symmetricCard.TopLeftPx > baseRadiusPx + 20f);
    }

    [Fact]
    public void SandTumbledCoarsenessContinuumSpansClearToOpaqueGlassWithoutWhiteCenterBleach()
    {
        SeaGlassMaterial baseMat = SeaGlassPalette.SeafoamMint.PrimaryGlass;

        SeaGlassMaterial clearGlass = baseMat.WithSandWear(0.0f);
        SeaGlassMaterial midVelvet = baseMat.WithSandWear(0.5f);
        SeaGlassMaterial opaqueGlass = baseMat.WithSandWear(1.0f);

        Assert.Equal(0.0f, clearGlass.Coarseness, 4);
        Assert.Equal(1.0f, clearGlass.Transparency, 4);
        Assert.True(clearGlass.EffectiveSandWear <= 0.02f);
        Assert.InRange(midVelvet.EffectiveSandWear, 0.35f, 0.75f);
        Assert.True(opaqueGlass.EffectiveSandWear >= 0.90f);

        int neutralBackdrop = unchecked((int)0xFFE6EEF2);
        int shallowArgb = SeaGlassColorMath.ComputeBeerLambertTransmission(
            backdropArgb: neutralBackdrop,
            tintArgb: baseMat.TintColorArgb,
            tintDensity: 0.65f,
            coarseness: 0.45f,
            normalizedThickness: 0.4f);
        int deepArgb = SeaGlassColorMath.ComputeBeerLambertTransmission(
            backdropArgb: neutralBackdrop,
            tintArgb: baseMat.TintColorArgb,
            tintDensity: 0.65f,
            coarseness: 0.45f,
            normalizedThickness: 1.8f);

        SeaGlassColorMath.OklabColor shallowLab = SeaGlassColorMath.ArgbToOklab(shallowArgb);
        SeaGlassColorMath.OklabColor deepLab = SeaGlassColorMath.ArgbToOklab(deepArgb);
        Assert.True(deepLab.L <= shallowLab.L, $"Greater glass depth must not bleach lightness toward white (deepL={deepLab.L} <= shallowL={shallowLab.L})");
    }

    [Fact]
    public void ShaderProgramCacheAndUniformDeltaTrackingOptimizeGpuCycles()
    {
        foreach (ShaderDialect dialect in Enum.GetValues<ShaderDialect>())
        {
            CompiledShaderBundle first = SeaGlassShaderCompiler.CompileForDialect(dialect);
            CompiledShaderBundle second = SeaGlassShaderCompiler.CompileForDialect(dialect);
            Assert.Same(first, second);
            Assert.Contains("sgRoundedBoxSdfAndGrad", first.SeaGlassCompositeFragmentSource);
        }

        CompiledShaderBundle warmed = SeaGlassShaderProgramCache.WarmUpForDevice(null);
        Assert.True(SeaGlassShaderProgramCache.IsWarmedUp);
        Assert.Same(warmed, SeaGlassShaderProgramCache.WarmUpForDevice(null));

        SeaGlassUniformBlock block = new();
        SeaGlassMaterial mat = SeaGlassPalette.CobaltTide.PrimaryGlass;
        block.Pack(
            widthPx: 500f,
            heightPx: 160f,
            backdropOffsetX: 24f,
            backdropOffsetY: 480f,
            material: mat,
            density: 2.75f,
            nodes: [],
            smoothRadiusPx: 28f,
            lightDirX: -0.5f,
            lightDirY: -0.7f,
            lightDirZ: 0.5f);
        Assert.Equal(SeaGlassUniformBlock.AllSlotsMask, block.DirtySlotsMask);
        long rev1 = block.ContentRevision;
        block.ClearDirtyMask();
        Assert.Equal(0, block.DirtySlotsMask);

        // Re-packing identical values must leave DirtySlotsMask == 0 and revision unchanged
        block.Pack(
            widthPx: 500f,
            heightPx: 160f,
            backdropOffsetX: 24f,
            backdropOffsetY: 480f,
            material: mat,
            density: 2.75f,
            nodes: [],
            smoothRadiusPx: 28f,
            lightDirX: -0.5f,
            lightDirY: -0.7f,
            lightDirZ: 0.5f);
        Assert.Equal(0, block.DirtySlotsMask);
        Assert.Equal(rev1, block.ContentRevision);

        // Changing only light tilt direction must dirty ONLY slot 3 (uLightAndMeta)
        block.Pack(
            widthPx: 500f,
            heightPx: 160f,
            backdropOffsetX: 24f,
            backdropOffsetY: 480f,
            material: mat,
            density: 2.75f,
            nodes: [],
            smoothRadiusPx: 28f,
            lightDirX: 0.35f,
            lightDirY: -0.65f,
            lightDirZ: 0.5f);
        Assert.Equal(1 << 3, block.DirtySlotsMask);
        Assert.Equal(rev1 + 1, block.ContentRevision);
    }

    [Fact]
    public void ShaderFastPathOptimizationsAndLeadLagDualEdgeLozengeSpringsVerify()
    {
        // 1. Verify all 5 shader dialects include the single-node analytical SDF + gradient fast path,
        //    micro-facet coarseness guard, adaptive dispersion tap culling, unbleached frostScatter,
        //    and interior plateau ALU culling.
        foreach (ShaderDialect dialect in Enum.GetValues<ShaderDialect>())
        {
            string src = SeaGlassShaderCompiler.CompileForDialect(dialect).SeaGlassCompositeFragmentSource;
            Assert.Contains("sgRoundedBoxSdfAndGrad", src);
            Assert.Contains("coarseness > 0.005", src);
            Assert.Contains("dispersion > 0.004", src);
            Assert.Contains("frostScatter", src);
            Assert.Contains("meniscus > 0.001", src);
            Assert.DoesNotContain("chalkyLift", src);
        }

        // 2. Verify dual-edge lead-lag liquid lozenge stretch on SeaGlassSegmentedButtonRow & SeaGlassFluidBar
        SeaGlassSegmentedButtonRow segRow = new(["Controls", "Surfaces", "Overlays"], 0, _ => { });
        segRow.SelectOption(2);
        segRow.StepAndEvaluate(1f / 60f);
        segRow.StepAndEvaluate(1f / 60f);
        Assert.True(segRow.LeadIndex > segRow.TrailIndex, "LeadIndex (k=540) must lead TrailIndex (k=310) mid-flight");
        Assert.True(segRow.LozengeSlotSpan > 1.05f, "LozengeSlotSpan must stretch across intermediate slots");
        Assert.True(segRow.ActiveIndicatorMaterial.Coarseness < segRow.IndicatorMaterial.Coarseness,
            "ActiveIndicatorMaterial must de-age during motion");

        SeaGlassFluidBar fluidBar = new(["Lab", "Kit", "Morph", "Stage"], 0, _ => { });
        fluidBar.SelectTab(3);
        fluidBar.StepAndEvaluate(1f / 60f);
        fluidBar.StepAndEvaluate(1f / 60f);
        Assert.True(fluidBar.LeadIndex > fluidBar.TrailIndex);
        Assert.True(fluidBar.LozengeSlotSpan > 1.05f);

        // 3. Verify lead-lag dual spatial springs on SeaGlassTwoElementMorph (width leads height)
        SeaGlassTwoElementMorph morph = new(expanded: false);
        morph.SetExpanded(true);
        morph.StepAndEvaluate(1f / 60f);
        morph.StepAndEvaluate(1f / 60f);
        Assert.True(morph.SpatialWidthProgress > morph.SpatialHeightProgress,
            "Horizontal width spring (k=420) must lead vertical height spring (k=340) during expansion");

        // 4. Verify reverse-rim frosted rind, volumetric core, tonal ink, and organic pebble/capsule contours
        int orchidTint = unchecked((int)0xFFEA96FF);
        int inkArgb = SeaGlassColorMath.OptimalContentColorFor(orchidTint, 0.70f);
        SeaGlassColorMath.OklabColor inkLab = SeaGlassColorMath.ArgbToOklab(inkArgb);
        float inkChroma = MathF.Sqrt(inkLab.A * inkLab.A + inkLab.B * inkLab.B);
        Assert.InRange(inkLab.L, 0.22f, 0.36f);
        Assert.True(inkChroma >= 0.06f);

        SeaGlassVectorPath2D pebblePath = new();
        SeaGlassContourPathBuilder.BuildOrganicPebblePath(68f, 68f, pebblePath);
        Assert.StartsWith("F1 M", pebblePath.ToXamlPathData());

        SeaGlassVectorPath2D capsulePath = new();
        SeaGlassContourPathBuilder.BuildTumbledCapsulePath(320f, 60f, capsulePath);
        Assert.StartsWith("F1 M", capsulePath.ToXamlPathData());
    }
}

