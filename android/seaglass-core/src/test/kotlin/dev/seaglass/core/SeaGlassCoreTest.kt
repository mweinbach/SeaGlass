package dev.seaglass.core

import dev.seaglass.core.backend.SeaGlassShaderCompiler
import dev.seaglass.core.backend.SeaGlassShaderProgramCache
import dev.seaglass.core.backend.SeaGlassUniformBlock
import dev.seaglass.core.backend.ShaderDialect
import dev.seaglass.core.backend.VulkanSeaGlassBackend
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertSame
import org.junit.Assert.assertTrue
import org.junit.Test
import kotlin.math.abs
import kotlin.math.sqrt

class SeaGlassCoreTest {

    @Test
    fun oklabRoundTripPreservesSeaGlassPastelColors() {
        for (palette in SeaGlassPalette.entries) {
            val original = palette.primaryGlass.tintColorArgb
            val lab = SeaGlassColorMath.argbToOklab(original)
            val reconstructed = SeaGlassColorMath.oklabToArgb(lab)

            val rDiff = abs(((original ushr 16) and 0xFF) - ((reconstructed ushr 16) and 0xFF))
            val gDiff = abs(((original ushr 8) and 0xFF) - ((reconstructed ushr 8) and 0xFF))
            val bDiff = abs((original and 0xFF) - (reconstructed and 0xFF))

            assertTrue("Red channel drifted by $rDiff for ${palette.name}", rDiff <= 2)
            assertTrue("Green channel drifted by $gDiff for ${palette.name}", gDiff <= 2)
            assertTrue("Blue channel drifted by $bDiff for ${palette.name}", bDiff <= 2)
        }
    }

    @Test
    fun beerLambertVolumetricAbsorptionShiftsHueAndIncreasesWithDensity() {
        val whiteBackdrop = 0xFFFFFFFF.toInt()
        val cobaltTint = SeaGlassPalette.CobaltTide.primaryGlass.tintColorArgb

        val lightTransmission = SeaGlassColorMath.computeBeerLambertTransmission(
            backdropArgb = whiteBackdrop,
            tintArgb = cobaltTint,
            tintDensity = 0.25f,
            coarseness = 0.30f,
            normalizedThickness = 1.0f
        )
        val deepTransmission = SeaGlassColorMath.computeBeerLambertTransmission(
            backdropArgb = whiteBackdrop,
            tintArgb = cobaltTint,
            tintDensity = 0.75f,
            coarseness = 0.30f,
            normalizedThickness = 1.6f
        )

        // Cobalt glass absorbs red more strongly than blue
        val lightRed = (lightTransmission ushr 16) and 0xFF
        val lightBlue = lightTransmission and 0xFF
        val deepRed = (deepTransmission ushr 16) and 0xFF
        val deepBlue = deepTransmission and 0xFF

        assertTrue("Cobalt sea glass must transmit more blue than red", lightBlue > lightRed)
        assertTrue("Deeper cobalt slab must absorb more red ($deepRed < $lightRed)", deepRed < lightRed)
        assertTrue("Deeper cobalt slab must remain blue-dominant", deepBlue > deepRed)
    }

    @Test
    fun sdfEngineEvaluatesSquircleAndSmoothMinCoalescing() {
        val nodeA = SeaGlassSdfEngine.SdfNode(
            centerX = 100f,
            centerY = 100f,
            halfWidth = 40f,
            halfHeight = 40f,
            uniformCornerRadius = 20f,
            cornerSmoothing = 2.5f,
            tintColorArgb = SeaGlassPalette.SeafoamMint.primaryGlass.tintColorArgb
        )
        val nodeB = SeaGlassSdfEngine.SdfNode(
            centerX = 190f,
            centerY = 100f,
            halfWidth = 40f,
            halfHeight = 40f,
            uniformCornerRadius = 20f,
            cornerSmoothing = 2.5f,
            tintColorArgb = SeaGlassPalette.CobaltTide.primaryGlass.tintColorArgb
        )

        // At center of nodeA, distance is -40px (inside)
        val distCenterA = SeaGlassSdfEngine.evaluateNodeSdf(100f, 100f, nodeA)
        assertEquals(-40f, distCenterA, 0.5f)

        // Midpoint (145, 100) is 5px outside both individual boxes (d_A = +5, d_B = +5)
        val dMidA = SeaGlassSdfEngine.evaluateNodeSdf(145f, 100f, nodeA)
        val dMidB = SeaGlassSdfEngine.evaluateNodeSdf(145f, 100f, nodeB)
        assertEquals(5f, dMidA, 0.05f)
        assertEquals(5f, dMidB, 0.05f)

        // With smoothRadiusPx = 36f, smin pulls the midpoint INSIDE the coalesced liquid bridge (d < 0)
        val sceneMerged = SeaGlassSdfEngine.evaluateScene(
            px = 145f,
            py = 100f,
            nodes = listOf(nodeA, nodeB),
            smoothRadiusPx = 36f
        )
        assertTrue(
            "Midpoint between nearby nodes should coalesce inside bridge (d=${sceneMerged.signedDistancePx})",
            sceneMerged.signedDistancePx < 0f
        )

        // Surface normal must be unit length
        val nLen = sqrt(
            sceneMerged.normalX * sceneMerged.normalX +
                sceneMerged.normalY * sceneMerged.normalY +
                sceneMerged.normalZ * sceneMerged.normalZ
        )
        assertEquals(1.0f, nLen, 1e-3f)
    }

    @Test
    fun std140UniformBlockMatches256ByteVulkanAndGles3Abi() {
        val block = SeaGlassUniformBlock()
        val material = SeaGlassPalette.SeaLavender.primaryGlass
        val nodes = listOf(
            SeaGlassSdfEngine.SdfNode(
                centerX = 240f,
                centerY = 120f,
                halfWidth = 180f,
                halfHeight = 56f,
                uniformCornerRadius = 56f,
                cornerSmoothing = 2.5f,
                tintColorArgb = material.tintColorArgb,
                thicknessPx = 28f
            ),
            SeaGlassSdfEngine.SdfNode(
                centerX = 460f,
                centerY = 120f,
                halfWidth = 56f,
                halfHeight = 56f,
                uniformCornerRadius = 56f,
                cornerSmoothing = 2.5f,
                tintColorArgb = SeaGlassPalette.RoseQuartzShell.primaryGlass.tintColorArgb,
                thicknessPx = 32f
            )
        )

        block.pack(
            widthPx = 600f,
            heightPx = 240f,
            backdropOffsetX = 32f,
            backdropOffsetY = 840f,
            material = material,
            density = 2.0f,
            nodes = nodes,
            smoothRadiusPx = 44f
        )

        val directBuffer = block.createDirectStagingBuffer()
        assertEquals(256, directBuffer.remaining())

        val floatView = directBuffer.asFloatBuffer()
        assertEquals(600f, floatView.get(0), 1e-4f)
        assertEquals(240f, floatView.get(1), 1e-4f)
        assertEquals(material.tintDensity, floatView.get(4), 1e-4f)
        assertEquals(material.coarseness, floatView.get(5), 1e-4f)
        assertEquals(material.ior, floatView.get(6), 1e-4f)
        assertEquals(2f, floatView.get(15), 1e-4f) // activeNodeCount == 2

        // Verify Vulkan backend consumes the exact same 256-byte UBO
        val vulkanBackend = VulkanSeaGlassBackend()
        vulkanBackend.syncUniforms(block)
        assertEquals(256, vulkanBackend.stagingBuffer.remaining())
        val graph = vulkanBackend.describeRenderGraph(frostRadiusPx = 32f)
        assertEquals(4, graph.kawasePyramidLevels)
        assertEquals(256, graph.uboByteSize)
    }

    @Test
    fun shaderCompilerEmitsConsistentVulkanGles3AndAgslDialects() {
        val vulkanBundle = SeaGlassShaderCompiler.compileForDialect(ShaderDialect.VULKAN_GLSL_450)
        val gles3Bundle = SeaGlassShaderCompiler.compileForDialect(ShaderDialect.GLSL_ES_300)
        val agslBundle = SeaGlassShaderCompiler.compileForDialect(ShaderDialect.AGSL_100)

        assertTrue(vulkanBundle.seaGlassCompositeFragmentSource.contains("#version 450 core"))
        assertTrue(
            vulkanBundle.seaGlassCompositeFragmentSource.contains(
                "layout(set = 0, binding = 0, std140) uniform SeaGlassUniforms"
            )
        )
        assertTrue(vulkanBundle.seaGlassCompositeFragmentSource.contains("sgLinearSrgbToOklab"))
        assertTrue(vulkanBundle.seaGlassCompositeFragmentSource.contains("sgSandMicroFacet"))
        assertTrue(vulkanBundle.seaGlassCompositeFragmentSource.contains("sgSminCubic"))
        assertNotNull(vulkanBundle.kawaseDownsampleFragmentSource)
        assertNotNull(vulkanBundle.kawaseUpsampleFragmentSource)

        assertTrue(gles3Bundle.seaGlassCompositeFragmentSource.contains("#version 300 es"))
        assertTrue(
            gles3Bundle.seaGlassCompositeFragmentSource.contains(
                "layout(std140) uniform SeaGlassUniforms"
            )
        )
        assertTrue(gles3Bundle.seaGlassCompositeFragmentSource.contains("sgEvaluateSceneSdf"))

        assertTrue(agslBundle.seaGlassCompositeFragmentSource.contains("uniform shader backdrop;"))
        assertTrue(agslBundle.seaGlassCompositeFragmentSource.contains("half4 main(float2 fragCoord)"))
        assertTrue(agslBundle.seaGlassCompositeFragmentSource.contains("sgSandMicroFacet"))
        assertNotNull(agslBundle.sandGrainOverlayFragmentSource)
    }

    @Test
    fun dynamicFlexMaterialBoostsLensingAndDispersionAtPeakMorphVelocity() {
        val start = SeaGlassPalette.SeafoamMint.primaryGlass
        val end = SeaGlassPalette.CobaltTide.surfaceGlass

        val atRestStart = SeaGlassShapeMorph.computeDynamicFlexMaterial(
            startMaterial = start,
            endMaterial = end,
            progress = 0f,
            areaExpansionRatio = 1f
        )
        val midFlight = SeaGlassShapeMorph.computeDynamicFlexMaterial(
            startMaterial = start,
            endMaterial = end,
            progress = 0.5f,
            velocityPxPerSec = 900f,
            areaExpansionRatio = 2.25f
        )

        assertTrue(
            "Mid-flight morph must boost IOR lensing (${midFlight.ior} > ${atRestStart.ior})",
            midFlight.ior > atRestStart.ior
        )
        assertTrue(
            "Mid-flight morph must boost chromatic dispersion (${midFlight.chromaticDispersion} > ${atRestStart.chromaticDispersion})",
            midFlight.chromaticDispersion > atRestStart.chromaticDispersion
        )
        assertTrue(
            "Expanded mid-flight morph must increase effective glass thickness",
            midFlight.thicknessDp > atRestStart.thicknessDp
        )

        for (kind in SeaGlassShapeKind.entries) {
            val poly = SeaGlassShapeMorph.createNormalizedPolygon(kind, aspectRatio = 1.4f)
            assertNotNull("Polygon for $kind must be non-null", poly)
            val polarScale = SeaGlassShapeMorph.evaluatePolarRadiusScale(
                startKind = kind,
                endKind = SeaGlassShapeKind.Circle,
                progress = 0.5f,
                angleRad = 0.78f
            )
            assertTrue("Polar scale for $kind must be in [0.35, 1.15], was $polarScale", polarScale in 0.35f..1.15f)
        }

        // Verify polar polygon SDF nodes coalesce via sminCubic
        val cookieNode = SeaGlassSdfEngine.SdfNode(
            centerX = 100f,
            centerY = 100f,
            halfWidth = 45f,
            halfHeight = 45f,
            uniformCornerRadius = 45f,
            startShapeKind = SeaGlassShapeKind.Cookie9,
            endShapeKind = SeaGlassShapeKind.Clover4,
            shapeMorphProgress = 0.4f
        )
        val flowerNode = SeaGlassSdfEngine.SdfNode(
            centerX = 180f,
            centerY = 100f,
            halfWidth = 45f,
            halfHeight = 45f,
            uniformCornerRadius = 45f,
            startShapeKind = SeaGlassShapeKind.Flower,
            endShapeKind = SeaGlassShapeKind.Heart,
            shapeMorphProgress = 0.4f
        )
        val eval = SeaGlassSdfEngine.evaluateScene(
            px = 140f,
            py = 100f,
            nodes = listOf(cookieNode, flowerNode),
            smoothRadiusPx = 42f
        )
        assertTrue("Coalescing Cookie9 + Flower bridge midpoint must be inside (d=${eval.signedDistancePx})", eval.isInside)

        // Verify two-phase via-Circle choreographed shape morph (Flower -> Circle -> Cookie9)
        val stageFlower = SeaGlassShapeStage(
            shape = SeaGlassShapeKind.Flower,
            scale = 1.0f,
            rotationDegrees = 0f,
            viaCircleBridge = true
        )
        val stageCookie = SeaGlassShapeStage(
            shape = SeaGlassShapeKind.Cookie9,
            scale = 1.2f,
            rotationDegrees = 90f,
            viaCircleBridge = true
        )
        val phase1 = SeaGlassShapeMorph.evaluateChoreographedTransform(
            startStage = stageFlower,
            endStage = stageCookie,
            progress = 0.25f
        )
        assertEquals(SeaGlassShapeKind.Flower, phase1.activeStartShape)
        assertEquals(SeaGlassShapeKind.Circle, phase1.activeEndShape)

        val phase2Overshoot = SeaGlassShapeMorph.evaluateChoreographedTransform(
            startStage = stageFlower,
            endStage = stageCookie,
            progress = 1.12f
        )
        assertEquals(SeaGlassShapeKind.Circle, phase2Overshoot.activeStartShape)
        assertEquals(SeaGlassShapeKind.Cookie9, phase2Overshoot.activeEndShape)
        assertTrue("Spring overshoot should exceed 1.0 in phase 2", phase2Overshoot.activeSubProgress > 1.0f)
    }

    @Test
    fun adaptiveDeviceAndPageCornerSolverMatchesConcentricAndAsymmetricBezelRadii() {
        val bezelCorners = SeaGlassCornerRadiiPx(
            topLeftPx = 120f,
            topRightPx = 120f,
            bottomRightPx = 120f,
            bottomLeftPx = 120f
        )

        // 1. Pod embedded in the Top-Left corner (inset = 10px, size = 260x160):
        // Its Top-Left corner is 10px from the 120px bezel corner -> concentric target = 110px (clamped to halfHeight=80px),
        // while its Bottom-Right corner is far in the interior of the 1080x2400 screen -> keeps base 40px radius!
        val tlPodRadii = SeaGlassCornerAdaptationSolver.solveElementCornerRadiiPx(
            elementLeftPx = 10f,
            elementTopPx = 10f,
            elementRightPx = 270f,
            elementBottomPx = 170f,
            containerWidthPx = 1080f,
            containerHeightPx = 2400f,
            containerCornersPx = bezelCorners,
            baseCornerRadiusPx = 40f,
            minCornerFloorPx = 16f,
            influenceDistancePx = 240f,
            mode = SeaGlassCornerAdaptationMode.FLUSH_CORNER_EMBED
        )
        assertTrue(
            "Outer Top-Left corner (${tlPodRadii.topLeftPx}) must be significantly larger than inner Bottom-Right corner (${tlPodRadii.bottomRightPx})",
            tlPodRadii.topLeftPx > tlPodRadii.bottomRightPx + 20f
        )
        assertTrue("Asymmetric corner pod must report isAsymmetric = true", tlPodRadii.isAsymmetric)

        // 2. Asymmetric Foldable Cover Screen (14dp=38px left hinge, 44dp=120px right outer bezel):
        val foldBezel = SeaGlassCornerRadiiPx(
            topLeftPx = 38f,
            topRightPx = 120f,
            bottomRightPx = 120f,
            bottomLeftPx = 38f
        )
        val fullWidthHeader = SeaGlassCornerAdaptationSolver.solveElementCornerRadiiPx(
            elementLeftPx = 12f,
            elementTopPx = 12f,
            elementRightPx = 1068f,
            elementBottomPx = 180f,
            containerWidthPx = 1080f,
            containerHeightPx = 2400f,
            containerCornersPx = foldBezel,
            baseCornerRadiusPx = 56f,
            minCornerFloorPx = 16f,
            influenceDistancePx = 280f,
            mode = SeaGlassCornerAdaptationMode.CONCENTRIC_PROXIMITY
        )
        assertTrue(
            "Right outer bezel corner (${fullWidthHeader.topRightPx}) must curve more deeply than left hinge corner (${fullWidthHeader.topLeftPx})",
            fullWidthHeader.topRightPx > fullWidthHeader.topLeftPx + 30f
        )

        // 3. Verify asymmetric edge-sum capacity (> 0.5 * H) and collision-free pod layout solver:
        // On a 200px tall pod (halfHeight = 100px) inside a 145px (55dp) bezel at 8px inset,
        // the outer corner reaches 137px (> 100px halfHeight!) because the inner corner is 40px (137 + 40 = 177 <= 200).
        val deepBezel = SeaGlassCornerRadiiPx.uniform(145f)
        val deepPodRadii = SeaGlassCornerAdaptationSolver.solveElementCornerRadiiPx(
            elementLeftPx = 8f,
            elementTopPx = 8f,
            elementRightPx = 398f,
            elementBottomPx = 208f,
            containerWidthPx = 900f,
            containerHeightPx = 540f,
            containerCornersPx = deepBezel,
            baseCornerRadiusPx = 48f,
            minCornerFloorPx = 20f,
            influenceDistancePx = 250f,
            mode = SeaGlassCornerAdaptationMode.FLUSH_CORNER_EMBED
        )
        assertTrue(
            "Asymmetric outer corner (${deepPodRadii.topLeftPx}) must exceed 0.5*H (100px) to match the 145px bezel without protruding",
            deepPodRadii.topLeftPx > 125f
        )

        val podLayout = SeaGlassCornerAdaptationSolver.solveCornerPodLayoutPx(
            containerWidthPx = 600f,
            containerHeightPx = 360f,
            containerCornersPx = deepBezel,
            requestedInsetPx = 80f,
            minHorizontalGapPx = 32f,
            minVerticalGapPx = 28f,
            preferredPodWidthPx = 340f,
            preferredTopPodHeightPx = 180f,
            preferredBottomDockHeightPx = 160f
        )
        assertTrue("Horizontal gap (${podLayout.horizontalGapPx}) must be >= 32px", podLayout.horizontalGapPx >= 31.9f)
        assertTrue("Vertical gap (${podLayout.verticalGapPx}) must be >= 28px", podLayout.verticalGapPx >= 27.9f)
    }

    @Test
    fun cornerAdaptationRespectsDeveloperMaskProximityDistanceAndSymmetry() {
        val bezel = SeaGlassCornerRadiiPx.uniform(140f)
        val baseRadiusPx = 64f

        // 1. Outside proximityThresholdDp (inset = 90px > influenceDistancePx = 70px):
        // Element must strictly keep baseCornerRadiusPx with zero adaptation.
        val outsideThreshold = SeaGlassCornerAdaptationSolver.solveElementCornerRadiiPx(
            elementLeftPx = 90f,
            elementTopPx = 90f,
            elementRightPx = 990f,
            elementBottomPx = 450f,
            containerWidthPx = 1080f,
            containerHeightPx = 2400f,
            containerCornersPx = bezel,
            baseCornerRadiusPx = baseRadiusPx,
            influenceDistancePx = 70f,
            mode = SeaGlassCornerAdaptationMode.CONCENTRIC_PROXIMITY
        )
        assertEquals(baseRadiusPx, outsideThreshold.topLeftPx, 0.01f)
        assertEquals(baseRadiusPx, outsideThreshold.topRightPx, 0.01f)
        assertEquals(baseRadiusPx, outsideThreshold.bottomLeftPx, 0.01f)
        assertEquals(baseRadiusPx, outsideThreshold.bottomRightPx, 0.01f)

        // 2. Corner Mask (BOTTOM_ONLY): even when placed near top-left and bottom-left corners,
        // only the bottom corners adapt while top corners remain locked at baseRadiusPx.
        val bottomOnlyRadii = SeaGlassCornerAdaptationSolver.solveElementCornerRadiiPx(
            elementLeftPx = 10f,
            elementTopPx = 10f,
            elementRightPx = 1070f,
            elementBottomPx = 2390f,
            containerWidthPx = 1080f,
            containerHeightPx = 2400f,
            containerCornersPx = bezel,
            baseCornerRadiusPx = baseRadiusPx,
            influenceDistancePx = 120f,
            mode = SeaGlassCornerAdaptationMode.CONCENTRIC_PROXIMITY,
            cornerMask = SeaGlassCornerMask.BOTTOM_ONLY,
            allowAsymmetricCorners = true
        )
        assertEquals("Top-Left must stay fixed when mask is BOTTOM_ONLY", baseRadiusPx, bottomOnlyRadii.topLeftPx, 0.01f)
        assertEquals("Top-Right must stay fixed when mask is BOTTOM_ONLY", baseRadiusPx, bottomOnlyRadii.topRightPx, 0.01f)
        assertTrue(
            "Bottom-Left (${bottomOnlyRadii.bottomLeftPx}) must expand to match bezel",
            bottomOnlyRadii.bottomLeftPx > baseRadiusPx + 35f
        )

        // 3. Monotonic non-pinching guarantee: at intermediate distance where R_outer - inset < baseRadiusPx,
        // CONCENTRIC_PROXIMITY must never shrink the corner below baseRadiusPx.
        val midDistanceRadii = SeaGlassCornerAdaptationSolver.solveElementCornerRadiiPx(
            elementLeftPx = 95f,
            elementTopPx = 95f,
            elementRightPx = 985f,
            elementBottomPx = 600f,
            containerWidthPx = 1080f,
            containerHeightPx = 2400f,
            containerCornersPx = bezel,
            baseCornerRadiusPx = baseRadiusPx,
            influenceDistancePx = 180f,
            mode = SeaGlassCornerAdaptationMode.CONCENTRIC_PROXIMITY
        )
        assertTrue(
            "Intermediate-distance corner (${midDistanceRadii.topLeftPx}) must never shrink below baseRadiusPx ($baseRadiusPx)",
            midDistanceRadii.topLeftPx >= baseRadiusPx - 0.01f
        )

        // 4. allowAsymmetricCorners = false keeps all 4 corners uniform on a bottom-anchored card
        val symmetricCard = SeaGlassCornerAdaptationSolver.solveElementCornerRadiiPx(
            elementLeftPx = 12f,
            elementTopPx = 1800f,
            elementRightPx = 1068f,
            elementBottomPx = 2388f,
            containerWidthPx = 1080f,
            containerHeightPx = 2400f,
            containerCornersPx = bezel,
            baseCornerRadiusPx = baseRadiusPx,
            config = SeaGlassCornerAdaptationConfig.concentricProximity(
                proximityThresholdDp = 40f,
                allowAsymmetricCorners = false
            ),
            density = 2.75f
        )
        assertTrue("Uniform card must not be asymmetric", !symmetricCard.isAsymmetric)
        assertTrue(
            "Uniform card near bottom bezel must match concentric radius across all 4 corners (${symmetricCard.topLeftPx})",
            symmetricCard.topLeftPx > baseRadiusPx + 20f
        )
    }

    @Test
    fun sandTumbledCoarsenessContinuumSpansClearToOpaqueGlassWithoutWhiteCenterBleach() {
        val base = SeaGlassPalette.SeafoamMint.primaryGlass

        val clearGlass = base.withSandWear(0.0f)
        val midVelvet = base.withSandWear(0.5f)
        val opaqueGlass = base.withSandWear(1.0f)

        assertEquals(0.0f, clearGlass.coarseness, 1e-4f)
        assertEquals(1.0f, clearGlass.transparency, 1e-4f)
        assertTrue("Untreated clear glass must have near-zero effectiveSandWear (${clearGlass.effectiveSandWear})", clearGlass.effectiveSandWear <= 0.02f)

        assertTrue(
            "Mid velvet glass must have intermediate sandWear (${midVelvet.effectiveSandWear})",
            midVelvet.effectiveSandWear in 0.35f..0.75f
        )
        assertTrue(
            "Opaque sand-tumbled glass must reach high sandWear (${opaqueGlass.effectiveSandWear})",
            opaqueGlass.effectiveSandWear >= 0.90f
        )

        // Verify increasing depth (normalizedThickness) deepens tint absorption rather than bleaching toward white
        val neutralBackdrop = 0xFFE6EEF2.toInt()
        val shallowArgb = SeaGlassColorMath.computeBeerLambertTransmission(
            backdropArgb = neutralBackdrop,
            tintArgb = base.tintColorArgb,
            tintDensity = 0.65f,
            coarseness = 0.45f,
            normalizedThickness = 0.4f
        )
        val deepArgb = SeaGlassColorMath.computeBeerLambertTransmission(
            backdropArgb = neutralBackdrop,
            tintArgb = base.tintColorArgb,
            tintDensity = 0.65f,
            coarseness = 0.45f,
            normalizedThickness = 1.8f
        )
        val shallowLab = SeaGlassColorMath.argbToOklab(shallowArgb)
        val deepLab = SeaGlassColorMath.argbToOklab(deepArgb)
        assertTrue(
            "Greater glass depth must not bleach lightness toward white (deepL=${deepLab.l} <= shallowL=${shallowLab.l})",
            deepLab.l <= shallowLab.l
        )
    }

    @Test
    fun shaderProgramCacheAndUniformDeltaTrackingOptimizeGpuCycles() {
        // 1. Verify compile-once memoization per dialect & device warm-up
        for (dialect in ShaderDialect.entries) {
            val first = SeaGlassShaderCompiler.compileForDialect(dialect)
            val second = SeaGlassShaderCompiler.compileForDialect(dialect)
            assertSame("Dialect $dialect must compile once and return cached bundle", first, second)
            assertTrue(
                "Dialect $dialect must include analytical single-pass sgRoundedBoxSdfAndGrad",
                first.seaGlassCompositeFragmentSource.contains("sgRoundedBoxSdfAndGrad")
            )
        }

        val warmed = SeaGlassShaderProgramCache.warmUpForDevice(null)
        assertTrue(SeaGlassShaderProgramCache.isWarmedUp)
        assertSame(warmed, SeaGlassShaderProgramCache.warmUpForDevice(null))

        // 2. Verify dirty-slot bitmask delta tracking in SeaGlassUniformBlock
        val block = SeaGlassUniformBlock()
        val mat = SeaGlassPalette.CobaltTide.primaryGlass
        block.pack(
            widthPx = 500f,
            heightPx = 160f,
            backdropOffsetX = 24f,
            backdropOffsetY = 480f,
            material = mat,
            density = 2.75f,
            nodes = emptyList(),
            smoothRadiusPx = 28f,
            lightDirX = -0.5f,
            lightDirY = -0.7f,
            lightDirZ = 0.5f
        )
        assertEquals(SeaGlassUniformBlock.ALL_SLOTS_MASK, block.dirtySlotsMask)
        val rev1 = block.contentRevision
        block.clearDirtyMask()
        assertEquals(0, block.dirtySlotsMask)

        // Re-packing identical values must leave dirtySlotsMask == 0 and revision unchanged
        block.pack(
            widthPx = 500f,
            heightPx = 160f,
            backdropOffsetX = 24f,
            backdropOffsetY = 480f,
            material = mat,
            density = 2.75f,
            nodes = emptyList(),
            smoothRadiusPx = 28f,
            lightDirX = -0.5f,
            lightDirY = -0.7f,
            lightDirZ = 0.5f
        )
        assertEquals("Unchanged frame must produce 0 dirty uniform slots", 0, block.dirtySlotsMask)
        assertEquals(rev1, block.contentRevision)

        // Changing only light tilt direction must dirty ONLY slot 3 (uLightAndMeta)
        block.pack(
            widthPx = 500f,
            heightPx = 160f,
            backdropOffsetX = 24f,
            backdropOffsetY = 480f,
            material = mat,
            density = 2.75f,
            nodes = emptyList(),
            smoothRadiusPx = 28f,
            lightDirX = 0.35f,
            lightDirY = -0.65f,
            lightDirZ = 0.5f
        )
        assertEquals("Tilt-only update must only dirty slot 3 (uLightAndMeta)", 1 shl 3, block.dirtySlotsMask)
        assertEquals(rev1 + 1, block.contentRevision)
    }
}
