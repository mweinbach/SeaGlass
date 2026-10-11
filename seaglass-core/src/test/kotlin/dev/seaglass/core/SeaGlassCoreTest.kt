package dev.seaglass.core

import dev.seaglass.core.backend.SeaGlassShaderCompiler
import dev.seaglass.core.backend.SeaGlassUniformBlock
import dev.seaglass.core.backend.ShaderDialect
import dev.seaglass.core.backend.VulkanSeaGlassBackend
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
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
}
