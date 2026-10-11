package dev.seaglass.core.backend

/**
 * Target GPU shading language dialects supported by [SeaGlassShaderCompiler].
 */
enum class ShaderDialect(
    val id: String,
    val displayName: String,
    val supportsStd140Ubo: Boolean
) {
    /**
     * Vulkan 1.1+ GLSL 450 core dialect (`#version 450 core`) for SPIR-V compilation via `shaderc` / `glslang`.
     * Uses explicit descriptor set / binding qualifiers (`layout(set = 0, binding = 0, std140)`)
     * and combined `sampler2D` bindings.
     */
    VULKAN_GLSL_450(
        id = "vulkan_glsl_450",
        displayName = "Vulkan 1.1+ GLSL 450 (SPIR-V)",
        supportsStd140Ubo = true
    ),

    /**
     * OpenGL ES 3.0+ dialect (`#version 300 es`) with `layout(std140) uniform SeaGlassUniforms`.
     */
    GLSL_ES_300(
        id = "glsl_es_300",
        displayName = "OpenGL ES 3.00",
        supportsStd140Ubo = true
    ),

    /**
     * Android 13+ (API 33+) Graphics Shading Language (`RuntimeShader`) dialect.
     */
    AGSL_100(
        id = "agsl_100",
        displayName = "Android AGSL (RuntimeShader)",
        supportsStd140Ubo = false
    )
}

/**
 * Complete bundle of shader stage sources generated for a specific [ShaderDialect].
 */
data class CompiledShaderBundle(
    val dialect: ShaderDialect,
    val vertexShaderSource: String?,
    val kawaseDownsampleFragmentSource: String?,
    val kawaseUpsampleFragmentSource: String?,
    val seaGlassCompositeFragmentSource: String,
    val sandGrainOverlayFragmentSource: String?
)

/**
 * Backend-agnostic shader generator for SeaGlass.
 *
 * Keeps the core mathematical model—expressive rounded-box/squircle SDFs, cubic `smin` coalescing,
 * analytical bevel normal reconstruction, procedural local-space sand-grain micro-coarseness,
 * perceptual Oklab Beer-Lambert volumetric absorption, and rough-Fresnel chalky rim lighting—in
 * one shared canonical core and emits dialect-specific bindings for:
 * - **Vulkan 1.1+ (`#version 450 core` / SPIR-V)**
 * - **OpenGL ES 3.00 (`#version 300 es`)**
 * - **Android AGSL (`RuntimeShader`)**
 *
 * Note on mobile GPU IEEE-754 safety: all `pow(x, y)` calls guard against `x <= 0.0` because
 * GPU `exp2(y * log2(x))` intrinsics evaluate `pow(0.0, y)` as `NaN`.
 */
object SeaGlassShaderCompiler {

    /**
     * Shared GLSL (Vulkan 450 & GLES 300) optical & SDF math core.
     * Consumes the 256-byte `std140` `SeaGlassUniforms` UBO block defined in [SeaGlassUniformBlock].
     */
    private val SHARED_GLSL_OPTICAL_CORE: String = """
        // ---------------------------------------------------------------------------
        // Shared SeaGlass Optical & SDF Math Core (Vulkan GLSL 450 & OpenGL ES 3.00)
        // ---------------------------------------------------------------------------

        float sgCbrt(float x) {
            return (x <= 0.0) ? 0.0 : pow(max(x, 1e-6), 0.33333333);
        }

        vec3 sgLinearSrgbToOklab(vec3 c) {
            vec3 safeC = max(c, vec3(0.0));
            float l = 0.41222147 * safeC.r + 0.53633254 * safeC.g + 0.05144599 * safeC.b;
            float m = 0.21190350 * safeC.r + 0.68069955 * safeC.g + 0.10739696 * safeC.b;
            float s = 0.08830246 * safeC.r + 0.28171884 * safeC.g + 0.62997870 * safeC.b;

            float l_ = sgCbrt(l);
            float m_ = sgCbrt(m);
            float s_ = sgCbrt(s);

            return vec3(
                0.21045426 * l_ + 0.79361779 * m_ - 0.00407205 * s_,
                1.97799850 * l_ - 2.42859220 * m_ + 0.45059371 * s_,
                0.02590404 * l_ + 0.78277177 * m_ - 0.80867577 * s_
            );
        }

        vec3 sgOklabToLinearSrgb(vec3 lab) {
            float l_ = lab.x + 0.39633778 * lab.y + 0.21580376 * lab.z;
            float m_ = lab.x - 0.10556135 * lab.y - 0.06385417 * lab.z;
            float s_ = lab.x - 0.08948418 * lab.y - 1.29148555 * lab.z;

            float l = l_ * l_ * l_;
            float m = m_ * m_ * m_;
            float s = s_ * s_ * s_;

            return clamp(vec3(
                +4.07674166 * l - 3.30771159 * m + 0.23096993 * s,
                -1.26843800 * l + 2.60975740 * m - 0.34131940 * s,
                -0.00419609 * l - 0.70341861 * m + 1.70761470 * s
            ), 0.0, 1.0);
        }

        vec3 sgSrgbToLinear(vec3 srgb) {
            return pow(max(srgb, vec3(1e-5)), vec3(2.2));
        }

        vec3 sgLinearToSrgb(vec3 lin) {
            return pow(clamp(lin, 1e-5, 1.0), vec3(1.0 / 2.2));
        }

        // Deterministic local-space 2D hash for tumbled sand-grain micro-facet normals
        vec2 sgSandMicroFacet(vec2 localCoord) {
            vec2 p1 = localCoord * 0.75;
            vec2 p2 = localCoord * 1.85 + vec2(17.3, 41.7);
            float n1 = fract(52.9829189 * fract(dot(p1, vec2(0.06711056, 0.00583715))));
            float n2 = fract(52.9829189 * fract(dot(p2, vec2(0.00583715, 0.06711056))));
            return vec2(n1, n2) * 2.0 - 1.0;
        }

        float sgRoundedBoxSdf(vec2 p, vec4 bounds, vec4 corners, float smoothingExp) {
            vec2 d = p - bounds.xy;
            vec2 b = bounds.zw;
            float rRight = (d.y > 0.0) ? corners.y : corners.x;
            float rLeft  = (d.y > 0.0) ? corners.z : corners.w;
            float r = (d.x > 0.0) ? rRight : rLeft;
            r = clamp(r, 0.0, max(0.0, min(b.x, b.y)));

            vec2 q = abs(d) - b + r;
            vec2 outside = max(q, vec2(0.0));
            float inside = min(max(q.x, q.y), 0.0);
            float len2 = length(outside);
            float minOut = min(outside.x, outside.y);
            float cornerDist = len2 - minOut * 0.18 * clamp(smoothingExp - 2.0, 0.0, 2.0);
            return inside + cornerDist - r;
        }

        float sgSminCubic(float a, float b, float k, out float blendWeight) {
            if (k <= 0.0001) {
                blendWeight = (a <= b) ? 0.0 : 1.0;
                return min(a, b);
            }
            float h = clamp(k - abs(a - b), 0.0, k) / k;
            float m = h * h * 0.5;
            blendWeight = (a < b) ? m : (1.0 - m);
            return min(a, b) - h * h * h * k * (1.0 / 6.0);
        }

        float sgEvaluateSceneSdf(vec2 p, out vec3 blendedLinearTint, out float blendedThickness) {
            int count = clamp(int(uLightAndMeta.w + 0.5), 1, 4);
            float smoothK = uOpticsSecondary.w;
            float smoothingExp = clamp(uOpticsSecondary.z, 2.0, 4.0);

            float d = sgRoundedBoxSdf(p, uNodeBounds[0], uNodeCorners[0], smoothingExp);
            blendedLinearTint = uNodeTints[0].rgb;
            blendedThickness = uNodeTints[0].w;

            for (int i = 1; i < 4; ++i) {
                if (i >= count) break;
                float di = sgRoundedBoxSdf(p, uNodeBounds[i], uNodeCorners[i], smoothingExp);
                float w = 0.0;
                d = sgSminCubic(d, di, smoothK, w);
                blendedLinearTint = mix(blendedLinearTint, uNodeTints[i].rgb, w);
                blendedThickness = mix(blendedThickness, uNodeTints[i].w, w);
            }
            return d;
        }

        float sgEvaluateDistanceOnly(vec2 p) {
            int count = clamp(int(uLightAndMeta.w + 0.5), 1, 4);
            float smoothK = uOpticsSecondary.w;
            float smoothingExp = clamp(uOpticsSecondary.z, 2.0, 4.0);
            float d = sgRoundedBoxSdf(p, uNodeBounds[0], uNodeCorners[0], smoothingExp);
            for (int i = 1; i < 4; ++i) {
                if (i >= count) break;
                float di = sgRoundedBoxSdf(p, uNodeBounds[i], uNodeCorners[i], smoothingExp);
                float w = 0.0;
                d = sgSminCubic(d, di, smoothK, w);
            }
            return d;
        }
    """.trimIndent()

    /**
     * Generates a complete [CompiledShaderBundle] for the requested [dialect].
     */
    fun compileForDialect(dialect: ShaderDialect): CompiledShaderBundle {
        return when (dialect) {
            ShaderDialect.VULKAN_GLSL_450 -> buildVulkanGlsl450Bundle()
            ShaderDialect.GLSL_ES_300 -> buildGlslEs300Bundle()
            ShaderDialect.AGSL_100 -> buildAgsl100Bundle()
        }
    }

    private fun buildVulkanGlsl450Bundle(): CompiledShaderBundle {
        val vertex = """
            #version 450 core
            layout(location = 0) in vec2 aPosition;
            layout(location = 1) in vec2 aTexCoord;
            layout(location = 0) out vec2 vTexCoord;

            void main() {
                vTexCoord = aTexCoord;
                gl_Position = vec4(aPosition, 0.0, 1.0);
            }
        """.trimIndent()

        val kawaseDown = """
            #version 450 core
            layout(location = 0) in vec2 vTexCoord;
            layout(location = 0) out vec4 fragColor;

            layout(set = 0, binding = 1) uniform sampler2D uSourceTex;
            layout(push_constant) uniform KawasePush {
                vec2 uHalfPixel;
                float uOffset;
                float uReserved;
            } pc;

            void main() {
                vec2 d = pc.uHalfPixel * pc.uOffset;
                vec4 sum = texture(uSourceTex, vTexCoord) * 4.0;
                sum += texture(uSourceTex, vTexCoord - d);
                sum += texture(uSourceTex, vTexCoord + d);
                sum += texture(uSourceTex, vTexCoord + vec2(d.x, -d.y));
                sum += texture(uSourceTex, vTexCoord - vec2(d.x, -d.y));
                fragColor = sum * 0.125;
            }
        """.trimIndent()

        val kawaseUp = """
            #version 450 core
            layout(location = 0) in vec2 vTexCoord;
            layout(location = 0) out vec4 fragColor;

            layout(set = 0, binding = 1) uniform sampler2D uSourceTex;
            layout(push_constant) uniform KawasePush {
                vec2 uHalfPixel;
                float uOffset;
                float uReserved;
            } pc;

            void main() {
                vec2 d = pc.uHalfPixel * pc.uOffset;
                vec4 sum = texture(uSourceTex, vTexCoord + vec2(-d.x * 2.0, 0.0));
                sum += texture(uSourceTex, vTexCoord + vec2(-d.x, d.y)) * 2.0;
                sum += texture(uSourceTex, vTexCoord + vec2(0.0, d.y * 2.0));
                sum += texture(uSourceTex, vTexCoord + vec2(d.x, d.y)) * 2.0;
                sum += texture(uSourceTex, vTexCoord + vec2(d.x * 2.0, 0.0));
                sum += texture(uSourceTex, vTexCoord + vec2(d.x, -d.y)) * 2.0;
                sum += texture(uSourceTex, vTexCoord + vec2(0.0, -d.y * 2.0));
                sum += texture(uSourceTex, vTexCoord + vec2(-d.x, -d.y)) * 2.0;
                fragColor = sum * (1.0 / 12.0);
            }
        """.trimIndent()

        val composite = """
            #version 450 core
            layout(location = 0) in vec2 vTexCoord;
            layout(location = 0) out vec4 fragColor;

            layout(set = 0, binding = 0, std140) uniform SeaGlassUniforms {
                vec4 uResolutionAndOrigin;
                vec4 uOpticsPrimary;
                vec4 uOpticsSecondary;
                vec4 uLightAndMeta;
                vec4 uNodeBounds[4];
                vec4 uNodeCorners[4];
                vec4 uNodeTints[4];
            };

            layout(set = 0, binding = 1) uniform sampler2D uBackdropTex;

            $SHARED_GLSL_OPTICAL_CORE

            void main() {
                vec2 fragCoord = vTexCoord * uResolutionAndOrigin.xy;
                vec3 linearTint;
                float thicknessPx;
                float d = sgEvaluateSceneSdf(fragCoord, linearTint, thicknessPx);

                // Anti-aliased outer edge coverage
                float edgeAlpha = 1.0 - smoothstep(-0.75, 0.75, d);
                if (edgeAlpha <= 0.001) {
                    fragColor = vec4(0.0);
                    return;
                }

                // Analytical SDF gradient & tumbled bevel normal
                float eps = 0.75;
                vec2 rawGrad = vec2(
                    sgEvaluateDistanceOnly(fragCoord + vec2(eps, 0.0)) - sgEvaluateDistanceOnly(fragCoord - vec2(eps, 0.0)),
                    sgEvaluateDistanceOnly(fragCoord + vec2(0.0, eps)) - sgEvaluateDistanceOnly(fragCoord - vec2(0.0, eps))
                );
                vec2 grad = rawGrad / max(length(rawGrad), 1e-4);

                float bevelWidth = max(6.0, thicknessPx * 1.25);
                float tau = clamp(1.0 - smoothstep(0.0, bevelWidth, -d), 0.0, 1.0);

                float coarseness = uOpticsPrimary.y;
                vec2 sandNoise = sgSandMicroFacet(fragCoord - uNodeBounds[0].xy);
                float gritScale = coarseness * (0.08 + 0.14 * tau);

                vec3 normal = normalize(vec3(
                    grad * tau * 0.85 + sandNoise * gritScale,
                    sqrt(max(0.06, 1.0 - tau * tau))
                ));

                // Softened edge refraction & chromatic dispersion
                float ior = uOpticsPrimary.z;
                float dispersion = uOpticsPrimary.w * 0.18;
                float refractStrength = (ior - 1.0) * thicknessPx * (1.0 - 0.55 * coarseness);
                vec2 uvOffset = (-normal.xy * refractStrength) / max(uResolutionAndOrigin.xy, vec2(1.0));

                vec3 bgSrgb = vec3(
                    texture(uBackdropTex, clamp(vTexCoord + uvOffset * (1.0 + dispersion), 0.001, 0.999)).r,
                    texture(uBackdropTex, clamp(vTexCoord + uvOffset, 0.001, 0.999)).g,
                    texture(uBackdropTex, clamp(vTexCoord + uvOffset * (1.0 - dispersion), 0.001, 0.999)).b
                );
                vec3 bgLinear = max(sgSrgbToLinear(bgSrgb), vec3(0.001));

                // Beer-Lambert volumetric absorption in perceptual Oklab space
                float tintDensity = uOpticsPrimary.x;
                float opticalDepth = tintDensity * (0.65 + 0.35 * sqrt(max(0.0, 1.0 - tau * tau)));
                vec3 safeTint = max(linearTint, vec3(0.04));
                vec3 transmittedLinear = bgLinear * exp(log(safeTint) * (opticalDepth * 1.35));

                vec3 labTransmitted = sgLinearSrgbToOklab(transmittedLinear);
                vec3 labTint = sgLinearSrgbToOklab(safeTint);
                float frostScatter = clamp(tintDensity * 0.46 + coarseness * 0.30, 0.0, 0.82);
                float chalkyLift = coarseness * 0.15 * (1.0 - labTransmitted.x * 0.5);

                vec3 labBody = vec3(
                    clamp(mix(labTransmitted.x, labTint.x, frostScatter * 0.58) + chalkyLift, 0.08, 0.96),
                    mix(labTransmitted.yz, labTint.yz, frostScatter)
                );
                vec3 bodyLinear = sgOklabToLinearSrgb(labBody);

                // Chalky Oren-Nayar / rough-Fresnel rim & inner caustic lighting
                vec3 lightDir = uLightAndMeta.xyz;
                float rimChalkiness = uOpticsSecondary.x;
                float fresnelRim = tau * sqrt(max(tau, 0.0)) * (0.35 + 0.65 * rimChalkiness);
                float keyHighlight = max(0.0, dot(normal.xy, lightDir.xy)) * tau;
                float leewardCaustic = max(0.0, dot(normal.xy, -lightDir.xy)) * tau * (1.0 - tau) * 2.2;
                float grainSparkle = coarseness * sandNoise.x * 0.035;

                vec3 rimColor = mix(safeTint, vec3(0.98, 0.99, 1.0), 0.72 + 0.20 * coarseness);
                vec3 finalLinear = bodyLinear
                    + rimColor * (fresnelRim * 0.26 + keyHighlight * 0.34)
                    + safeTint * (leewardCaustic * 0.22)
                    + vec3(grainSparkle);

                vec3 finalSrgb = sgLinearToSrgb(finalLinear);
                fragColor = vec4(finalSrgb * edgeAlpha, edgeAlpha);
            }
        """.trimIndent()

        return CompiledShaderBundle(
            dialect = ShaderDialect.VULKAN_GLSL_450,
            vertexShaderSource = vertex,
            kawaseDownsampleFragmentSource = kawaseDown,
            kawaseUpsampleFragmentSource = kawaseUp,
            seaGlassCompositeFragmentSource = composite,
            sandGrainOverlayFragmentSource = null
        )
    }

    private fun buildGlslEs300Bundle(): CompiledShaderBundle {
        val vertex = """
            #version 300 es
            precision highp float;
            layout(location = 0) in vec2 aPosition;
            layout(location = 1) in vec2 aTexCoord;
            out vec2 vTexCoord;

            void main() {
                vTexCoord = aTexCoord;
                gl_Position = vec4(aPosition, 0.0, 1.0);
            }
        """.trimIndent()

        val kawaseDown = """
            #version 300 es
            precision mediump float;
            in vec2 vTexCoord;
            out vec4 fragColor;

            uniform sampler2D uSourceTex;
            uniform vec2 uHalfPixel;
            uniform float uOffset;

            void main() {
                vec2 d = uHalfPixel * uOffset;
                vec4 sum = texture(uSourceTex, vTexCoord) * 4.0;
                sum += texture(uSourceTex, vTexCoord - d);
                sum += texture(uSourceTex, vTexCoord + d);
                sum += texture(uSourceTex, vTexCoord + vec2(d.x, -d.y));
                sum += texture(uSourceTex, vTexCoord - vec2(d.x, -d.y));
                fragColor = sum * 0.125;
            }
        """.trimIndent()

        val kawaseUp = """
            #version 300 es
            precision mediump float;
            in vec2 vTexCoord;
            out vec4 fragColor;

            uniform sampler2D uSourceTex;
            uniform vec2 uHalfPixel;
            uniform float uOffset;

            void main() {
                vec2 d = uHalfPixel * uOffset;
                vec4 sum = texture(uSourceTex, vTexCoord + vec2(-d.x * 2.0, 0.0));
                sum += texture(uSourceTex, vTexCoord + vec2(-d.x, d.y)) * 2.0;
                sum += texture(uSourceTex, vTexCoord + vec2(0.0, d.y * 2.0));
                sum += texture(uSourceTex, vTexCoord + vec2(d.x, d.y)) * 2.0;
                sum += texture(uSourceTex, vTexCoord + vec2(d.x * 2.0, 0.0));
                sum += texture(uSourceTex, vTexCoord + vec2(d.x, -d.y)) * 2.0;
                sum += texture(uSourceTex, vTexCoord + vec2(0.0, -d.y * 2.0));
                sum += texture(uSourceTex, vTexCoord + vec2(-d.x, -d.y)) * 2.0;
                fragColor = sum * (1.0 / 12.0);
            }
        """.trimIndent()

        val composite = """
            #version 300 es
            precision highp float;
            in vec2 vTexCoord;
            out vec4 fragColor;

            layout(std140) uniform SeaGlassUniforms {
                vec4 uResolutionAndOrigin;
                vec4 uOpticsPrimary;
                vec4 uOpticsSecondary;
                vec4 uLightAndMeta;
                vec4 uNodeBounds[4];
                vec4 uNodeCorners[4];
                vec4 uNodeTints[4];
            };

            uniform sampler2D uBackdropTex;

            $SHARED_GLSL_OPTICAL_CORE

            void main() {
                vec2 fragCoord = vTexCoord * uResolutionAndOrigin.xy;
                vec3 linearTint;
                float thicknessPx;
                float d = sgEvaluateSceneSdf(fragCoord, linearTint, thicknessPx);

                float edgeAlpha = 1.0 - smoothstep(-0.75, 0.75, d);
                if (edgeAlpha <= 0.001) {
                    fragColor = vec4(0.0);
                    return;
                }

                float eps = 0.75;
                vec2 rawGrad = vec2(
                    sgEvaluateDistanceOnly(fragCoord + vec2(eps, 0.0)) - sgEvaluateDistanceOnly(fragCoord - vec2(eps, 0.0)),
                    sgEvaluateDistanceOnly(fragCoord + vec2(0.0, eps)) - sgEvaluateDistanceOnly(fragCoord - vec2(0.0, eps))
                );
                vec2 grad = rawGrad / max(length(rawGrad), 1e-4);

                float bevelWidth = max(6.0, thicknessPx * 1.25);
                float tau = clamp(1.0 - smoothstep(0.0, bevelWidth, -d), 0.0, 1.0);

                float coarseness = uOpticsPrimary.y;
                vec2 sandNoise = sgSandMicroFacet(fragCoord - uNodeBounds[0].xy);
                float gritScale = coarseness * (0.08 + 0.14 * tau);

                vec3 normal = normalize(vec3(
                    grad * tau * 0.85 + sandNoise * gritScale,
                    sqrt(max(0.06, 1.0 - tau * tau))
                ));

                float ior = uOpticsPrimary.z;
                float dispersion = uOpticsPrimary.w * 0.18;
                float refractStrength = (ior - 1.0) * thicknessPx * (1.0 - 0.55 * coarseness);
                vec2 uvOffset = (-normal.xy * refractStrength) / max(uResolutionAndOrigin.xy, vec2(1.0));

                vec3 bgSrgb = vec3(
                    texture(uBackdropTex, clamp(vTexCoord + uvOffset * (1.0 + dispersion), 0.001, 0.999)).r,
                    texture(uBackdropTex, clamp(vTexCoord + uvOffset, 0.001, 0.999)).g,
                    texture(uBackdropTex, clamp(vTexCoord + uvOffset * (1.0 - dispersion), 0.001, 0.999)).b
                );
                vec3 bgLinear = max(sgSrgbToLinear(bgSrgb), vec3(0.001));

                float tintDensity = uOpticsPrimary.x;
                float opticalDepth = tintDensity * (0.65 + 0.35 * sqrt(max(0.0, 1.0 - tau * tau)));
                vec3 safeTint = max(linearTint, vec3(0.04));
                vec3 transmittedLinear = bgLinear * exp(log(safeTint) * (opticalDepth * 1.35));

                vec3 labTransmitted = sgLinearSrgbToOklab(transmittedLinear);
                vec3 labTint = sgLinearSrgbToOklab(safeTint);
                float frostScatter = clamp(tintDensity * 0.46 + coarseness * 0.30, 0.0, 0.82);
                float chalkyLift = coarseness * 0.15 * (1.0 - labTransmitted.x * 0.5);

                vec3 labBody = vec3(
                    clamp(mix(labTransmitted.x, labTint.x, frostScatter * 0.58) + chalkyLift, 0.08, 0.96),
                    mix(labTransmitted.yz, labTint.yz, frostScatter)
                );
                vec3 bodyLinear = sgOklabToLinearSrgb(labBody);

                vec3 lightDir = uLightAndMeta.xyz;
                float rimChalkiness = uOpticsSecondary.x;
                float fresnelRim = tau * sqrt(max(tau, 0.0)) * (0.35 + 0.65 * rimChalkiness);
                float keyHighlight = max(0.0, dot(normal.xy, lightDir.xy)) * tau;
                float leewardCaustic = max(0.0, dot(normal.xy, -lightDir.xy)) * tau * (1.0 - tau) * 2.2;
                float grainSparkle = coarseness * sandNoise.x * 0.035;

                vec3 rimColor = mix(safeTint, vec3(0.98, 0.99, 1.0), 0.72 + 0.20 * coarseness);
                vec3 finalLinear = bodyLinear
                    + rimColor * (fresnelRim * 0.26 + keyHighlight * 0.34)
                    + safeTint * (leewardCaustic * 0.22)
                    + vec3(grainSparkle);

                vec3 finalSrgb = sgLinearToSrgb(finalLinear);
                fragColor = vec4(finalSrgb * edgeAlpha, edgeAlpha);
            }
        """.trimIndent()

        return CompiledShaderBundle(
            dialect = ShaderDialect.GLSL_ES_300,
            vertexShaderSource = vertex,
            kawaseDownsampleFragmentSource = kawaseDown,
            kawaseUpsampleFragmentSource = kawaseUp,
            seaGlassCompositeFragmentSource = composite,
            sandGrainOverlayFragmentSource = null
        )
    }

    private fun buildAgsl100Bundle(): CompiledShaderBundle {
        val agslComposite = """
            uniform shader backdrop;
            uniform float4 uResolutionAndOrigin;
            uniform float4 uOpticsPrimary;
            uniform float4 uOpticsSecondary;
            uniform float4 uLightAndMeta;
            uniform float4 uNode0Bounds;
            uniform float4 uNode0Corners;
            uniform float4 uNode0Tint;
            uniform float4 uNode1Bounds;
            uniform float4 uNode1Corners;
            uniform float4 uNode1Tint;
            uniform float4 uNode2Bounds;
            uniform float4 uNode2Corners;
            uniform float4 uNode2Tint;
            uniform float4 uNode3Bounds;
            uniform float4 uNode3Corners;
            uniform float4 uNode3Tint;

            float sgCbrt(float x) {
                return (x <= 0.0) ? 0.0 : pow(max(x, 1e-6), 0.33333333);
            }

            float3 sgLinearSrgbToOklab(float3 c) {
                float3 safeC = max(c, float3(0.0));
                float l = 0.41222147 * safeC.r + 0.53633254 * safeC.g + 0.05144599 * safeC.b;
                float m = 0.21190350 * safeC.r + 0.68069955 * safeC.g + 0.10739696 * safeC.b;
                float s = 0.08830246 * safeC.r + 0.28171884 * safeC.g + 0.62997870 * safeC.b;

                float l_ = sgCbrt(l);
                float m_ = sgCbrt(m);
                float s_ = sgCbrt(s);

                return float3(
                    0.21045426 * l_ + 0.79361779 * m_ - 0.00407205 * s_,
                    1.97799850 * l_ - 2.42859220 * m_ + 0.45059371 * s_,
                    0.02590404 * l_ + 0.78277177 * m_ - 0.80867577 * s_
                );
            }

            float3 sgOklabToLinearSrgb(float3 lab) {
                float l_ = lab.x + 0.39633778 * lab.y + 0.21580376 * lab.z;
                float m_ = lab.x - 0.10556135 * lab.y - 0.06385417 * lab.z;
                float s_ = lab.x - 0.08948418 * lab.y - 1.29148555 * lab.z;

                float l = l_ * l_ * l_;
                float m = m_ * m_ * m_;
                float s = s_ * s_ * s_;

                return clamp(float3(
                    +4.07674166 * l - 3.30771159 * m + 0.23096993 * s,
                    -1.26843800 * l + 2.60975740 * m - 0.34131940 * s,
                    -0.00419609 * l - 0.70341861 * m + 1.70761470 * s
                ), 0.0, 1.0);
            }

            float2 sgSandMicroFacet(float2 localCoord) {
                float2 p1 = localCoord * 0.75;
                float2 p2 = localCoord * 1.85 + float2(17.3, 41.7);
                float n1 = fract(52.9829189 * fract(dot(p1, float2(0.06711056, 0.00583715))));
                float n2 = fract(52.9829189 * fract(dot(p2, float2(0.00583715, 0.06711056))));
                return float2(n1, n2) * 2.0 - 1.0;
            }

            float sgRoundedBoxSdf(float2 p, float4 bounds, float4 corners, float smoothingExp) {
                float2 d = p - bounds.xy;
                float2 b = bounds.zw;
                float rRight = (d.y > 0.0) ? corners.y : corners.x;
                float rLeft  = (d.y > 0.0) ? corners.z : corners.w;
                float r = (d.x > 0.0) ? rRight : rLeft;
                r = clamp(r, 0.0, max(0.0, min(b.x, b.y)));

                float2 q = abs(d) - b + r;
                float2 outside = max(q, float2(0.0));
                float inside = min(max(q.x, q.y), 0.0);
                float len2 = length(outside);
                float minOut = min(outside.x, outside.y);
                float cornerDist = len2 - minOut * 0.18 * clamp(smoothingExp - 2.0, 0.0, 2.0);
                return inside + cornerDist - r;
            }

            float sgSminWeight(float a, float b, float k) {
                if (k <= 0.0001) return (a <= b) ? 0.0 : 1.0;
                float h = clamp(k - abs(a - b), 0.0, k) / k;
                float m = h * h * 0.5;
                return (a < b) ? m : (1.0 - m);
            }

            float sgSminDist(float a, float b, float k) {
                if (k <= 0.0001) return min(a, b);
                float h = clamp(k - abs(a - b), 0.0, k) / k;
                return min(a, b) - h * h * h * k * 0.16666667;
            }

            float sgEvalDistance(float2 p) {
                int count = int(uLightAndMeta.w + 0.5);
                float smoothK = uOpticsSecondary.w;
                float smoothingExp = clamp(uOpticsSecondary.z, 2.0, 4.0);
                float d = sgRoundedBoxSdf(p, uNode0Bounds, uNode0Corners, smoothingExp);
                if (count > 1) {
                    d = sgSminDist(d, sgRoundedBoxSdf(p, uNode1Bounds, uNode1Corners, smoothingExp), smoothK);
                }
                if (count > 2) {
                    d = sgSminDist(d, sgRoundedBoxSdf(p, uNode2Bounds, uNode2Corners, smoothingExp), smoothK);
                }
                if (count > 3) {
                    d = sgSminDist(d, sgRoundedBoxSdf(p, uNode3Bounds, uNode3Corners, smoothingExp), smoothK);
                }
                return d;
            }

            half4 main(float2 fragCoord) {
                int count = int(uLightAndMeta.w + 0.5);
                float smoothK = uOpticsSecondary.w;
                float smoothingExp = clamp(uOpticsSecondary.z, 2.0, 4.0);

                float d = sgRoundedBoxSdf(fragCoord, uNode0Bounds, uNode0Corners, smoothingExp);
                float3 linearTint = uNode0Tint.rgb;
                float thicknessPx = uNode0Tint.w;

                if (count > 1) {
                    float d1 = sgRoundedBoxSdf(fragCoord, uNode1Bounds, uNode1Corners, smoothingExp);
                    float w1 = sgSminWeight(d, d1, smoothK);
                    d = sgSminDist(d, d1, smoothK);
                    linearTint = mix(linearTint, uNode1Tint.rgb, w1);
                    thicknessPx = mix(thicknessPx, uNode1Tint.w, w1);
                }
                if (count > 2) {
                    float d2 = sgRoundedBoxSdf(fragCoord, uNode2Bounds, uNode2Corners, smoothingExp);
                    float w2 = sgSminWeight(d, d2, smoothK);
                    d = sgSminDist(d, d2, smoothK);
                    linearTint = mix(linearTint, uNode2Tint.rgb, w2);
                    thicknessPx = mix(thicknessPx, uNode2Tint.w, w2);
                }
                if (count > 3) {
                    float d3 = sgRoundedBoxSdf(fragCoord, uNode3Bounds, uNode3Corners, smoothingExp);
                    float w3 = sgSminWeight(d, d3, smoothK);
                    d = sgSminDist(d, d3, smoothK);
                    linearTint = mix(linearTint, uNode3Tint.rgb, w3);
                    thicknessPx = mix(thicknessPx, uNode3Tint.w, w3);
                }

                float edgeAlpha = 1.0 - smoothstep(-0.75, 0.75, d);
                if (edgeAlpha <= 0.001) {
                    return half4(0.0);
                }

                float eps = 0.75;
                float2 rawGrad = float2(
                    sgEvalDistance(fragCoord + float2(eps, 0.0)) - sgEvalDistance(fragCoord - float2(eps, 0.0)),
                    sgEvalDistance(fragCoord + float2(0.0, eps)) - sgEvalDistance(fragCoord - float2(0.0, eps))
                );
                float2 grad = rawGrad / max(length(rawGrad), 1e-4);

                float bevelWidth = max(6.0, thicknessPx * 1.25);
                float tau = clamp(1.0 - smoothstep(0.0, bevelWidth, -d), 0.0, 1.0);

                float coarseness = uOpticsPrimary.y;
                float2 sandNoise = sgSandMicroFacet(fragCoord - uNode0Bounds.xy);
                float gritScale = coarseness * (0.08 + 0.14 * tau);

                float3 normal = normalize(float3(
                    grad * tau * 0.85 + sandNoise * gritScale,
                    sqrt(max(0.06, 1.0 - tau * tau))
                ));

                float ior = uOpticsPrimary.z;
                float dispersion = uOpticsPrimary.w * 0.18;
                float refractStrength = (ior - 1.0) * thicknessPx * (1.0 - 0.55 * coarseness);
                float2 pxOffset = -normal.xy * refractStrength;

                float2 maxCoord = max(uResolutionAndOrigin.xy - float2(1.0), float2(1.0));
                float2 coordR = clamp(fragCoord + pxOffset * (1.0 + dispersion), float2(0.0), maxCoord);
                float2 coordG = clamp(fragCoord + pxOffset, float2(0.0), maxCoord);
                float2 coordB = clamp(fragCoord + pxOffset * (1.0 - dispersion), float2(0.0), maxCoord);

                half4 sampleR = backdrop.eval(coordR);
                half4 sampleG = backdrop.eval(coordG);
                half4 sampleB = backdrop.eval(coordB);

                float3 bgSrgb = float3(sampleR.r, sampleG.g, sampleB.b);
                float3 bgLinear = pow(max(bgSrgb, float3(0.001)), float3(2.2));

                float tintDensity = uOpticsPrimary.x;
                float opticalDepth = tintDensity * (0.65 + 0.35 * sqrt(max(0.0, 1.0 - tau * tau)));
                float3 safeTint = max(linearTint, float3(0.04));
                float3 transmittedLinear = bgLinear * exp(log(safeTint) * (opticalDepth * 1.35));

                float3 labTransmitted = sgLinearSrgbToOklab(transmittedLinear);
                float3 labTint = sgLinearSrgbToOklab(safeTint);
                float frostScatter = clamp(tintDensity * 0.46 + coarseness * 0.30, 0.0, 0.82);
                float chalkyLift = coarseness * 0.15 * (1.0 - labTransmitted.x * 0.5);

                float3 labBody = float3(
                    clamp(mix(labTransmitted.x, labTint.x, frostScatter * 0.58) + chalkyLift, 0.08, 0.96),
                    mix(labTransmitted.yz, labTint.yz, frostScatter)
                );
                float3 bodyLinear = sgOklabToLinearSrgb(labBody);

                float3 lightDir = uLightAndMeta.xyz;
                float rimChalkiness = uOpticsSecondary.x;
                float fresnelRim = tau * sqrt(max(tau, 0.0)) * (0.35 + 0.65 * rimChalkiness);
                float keyHighlight = max(0.0, dot(normal.xy, lightDir.xy)) * tau;
                float leewardCaustic = max(0.0, dot(normal.xy, -lightDir.xy)) * tau * (1.0 - tau) * 2.2;
                float grainSparkle = coarseness * sandNoise.x * 0.035;

                float3 rimColor = mix(safeTint, float3(0.98, 0.99, 1.0), 0.72 + 0.20 * coarseness);
                float3 finalLinear = bodyLinear
                    + rimColor * (fresnelRim * 0.26 + keyHighlight * 0.34)
                    + safeTint * (leewardCaustic * 0.22)
                    + float3(grainSparkle);

                float3 finalSrgb = pow(clamp(finalLinear, float3(1e-5), float3(1.0)), float3(1.0 / 2.2));
                return half4(half3(finalSrgb * edgeAlpha), half(edgeAlpha));
            }
        """.trimIndent()

        val agslSandGrainOverlay = """
            uniform float4 uResolutionAndOrigin;
            uniform float4 uOpticsPrimary;
            uniform float4 uOpticsSecondary;
            uniform float4 uLightAndMeta;
            uniform float4 uNode0Bounds;
            uniform float4 uNode0Corners;
            uniform float4 uNode0Tint;
            uniform float4 uNode1Bounds;
            uniform float4 uNode1Corners;
            uniform float4 uNode1Tint;
            uniform float4 uNode2Bounds;
            uniform float4 uNode2Corners;
            uniform float4 uNode2Tint;
            uniform float4 uNode3Bounds;
            uniform float4 uNode3Corners;
            uniform float4 uNode3Tint;

            float2 sgSandMicroFacet(float2 localCoord) {
                float2 p1 = localCoord * 0.75;
                float2 p2 = localCoord * 1.85 + float2(17.3, 41.7);
                float n1 = fract(52.9829189 * fract(dot(p1, float2(0.06711056, 0.00583715))));
                float n2 = fract(52.9829189 * fract(dot(p2, float2(0.00583715, 0.06711056))));
                return float2(n1, n2) * 2.0 - 1.0;
            }

            float sgRoundedBoxSdf(float2 p, float4 bounds, float4 corners, float smoothingExp) {
                float2 d = p - bounds.xy;
                float2 b = bounds.zw;
                float rRight = (d.y > 0.0) ? corners.y : corners.x;
                float rLeft  = (d.y > 0.0) ? corners.z : corners.w;
                float r = (d.x > 0.0) ? rRight : rLeft;
                r = clamp(r, 0.0, max(0.0, min(b.x, b.y)));
                float2 q = abs(d) - b + r;
                float2 outside = max(q, float2(0.0));
                float inside = min(max(q.x, q.y), 0.0);
                float len2 = length(outside);
                float minOut = min(outside.x, outside.y);
                float cornerDist = len2 - minOut * 0.18 * clamp(smoothingExp - 2.0, 0.0, 2.0);
                return inside + cornerDist - r;
            }

            float sgSminWeight(float a, float b, float k) {
                if (k <= 0.0001) return (a <= b) ? 0.0 : 1.0;
                float h = clamp(k - abs(a - b), 0.0, k) / k;
                float m = h * h * 0.5;
                return (a < b) ? m : (1.0 - m);
            }

            float sgSminDist(float a, float b, float k) {
                if (k <= 0.0001) return min(a, b);
                float h = clamp(k - abs(a - b), 0.0, k) / k;
                return min(a, b) - h * h * h * k * 0.16666667;
            }

            float sgEvalDistance(float2 p) {
                int count = int(uLightAndMeta.w + 0.5);
                float smoothK = uOpticsSecondary.w;
                float smoothingExp = clamp(uOpticsSecondary.z, 2.0, 4.0);
                float d = sgRoundedBoxSdf(p, uNode0Bounds, uNode0Corners, smoothingExp);
                if (count > 1) {
                    d = sgSminDist(d, sgRoundedBoxSdf(p, uNode1Bounds, uNode1Corners, smoothingExp), smoothK);
                }
                if (count > 2) {
                    d = sgSminDist(d, sgRoundedBoxSdf(p, uNode2Bounds, uNode2Corners, smoothingExp), smoothK);
                }
                if (count > 3) {
                    d = sgSminDist(d, sgRoundedBoxSdf(p, uNode3Bounds, uNode3Corners, smoothingExp), smoothK);
                }
                return d;
            }

            half4 main(float2 fragCoord) {
                int count = int(uLightAndMeta.w + 0.5);
                float smoothK = uOpticsSecondary.w;
                float smoothingExp = clamp(uOpticsSecondary.z, 2.0, 4.0);

                float d = sgRoundedBoxSdf(fragCoord, uNode0Bounds, uNode0Corners, smoothingExp);
                float3 linearTint = uNode0Tint.rgb;
                float thicknessPx = uNode0Tint.w;

                if (count > 1) {
                    float d1 = sgRoundedBoxSdf(fragCoord, uNode1Bounds, uNode1Corners, smoothingExp);
                    float w1 = sgSminWeight(d, d1, smoothK);
                    d = sgSminDist(d, d1, smoothK);
                    linearTint = mix(linearTint, uNode1Tint.rgb, w1);
                    thicknessPx = mix(thicknessPx, uNode1Tint.w, w1);
                }
                if (count > 2) {
                    float d2 = sgRoundedBoxSdf(fragCoord, uNode2Bounds, uNode2Corners, smoothingExp);
                    float w2 = sgSminWeight(d, d2, smoothK);
                    d = sgSminDist(d, d2, smoothK);
                    linearTint = mix(linearTint, uNode2Tint.rgb, w2);
                    thicknessPx = mix(thicknessPx, uNode2Tint.w, w2);
                }
                if (count > 3) {
                    float d3 = sgRoundedBoxSdf(fragCoord, uNode3Bounds, uNode3Corners, smoothingExp);
                    float w3 = sgSminWeight(d, d3, smoothK);
                    d = sgSminDist(d, d3, smoothK);
                    linearTint = mix(linearTint, uNode3Tint.rgb, w3);
                    thicknessPx = mix(thicknessPx, uNode3Tint.w, w3);
                }

                float edgeAlpha = 1.0 - smoothstep(-0.85, 0.85, d);
                if (edgeAlpha <= 0.001) return half4(0.0);

                float eps = 0.85;
                float2 rawGrad = float2(
                    sgEvalDistance(fragCoord + float2(eps, 0.0)) - sgEvalDistance(fragCoord - float2(eps, 0.0)),
                    sgEvalDistance(fragCoord + float2(0.0, eps)) - sgEvalDistance(fragCoord - float2(0.0, eps))
                );
                float2 grad = rawGrad / max(length(rawGrad), 1e-4);

                float bevelWidth = max(8.0, thicknessPx * 1.35);
                float tau = clamp(1.0 - smoothstep(0.0, bevelWidth, -d), 0.0, 1.0);

                float tintDensity = uOpticsPrimary.x;
                float coarseness = uOpticsPrimary.y;
                float rimChalkiness = uOpticsSecondary.x;
                float2 sandNoise = sgSandMicroFacet(fragCoord - uNode0Bounds.xy);

                float3 tintSrgb = sqrt(clamp(linearTint, float3(0.0), float3(1.0)));
                float3 chalkWhite = float3(0.97, 0.99, 1.0);

                // Windward key light & leeward internal refracted caustic
                float2 lightXY = uLightAndMeta.xy;
                float windward = max(0.0, dot(grad, lightXY)) * tau;
                float leeward = max(0.0, dot(grad, -lightXY)) * tau * (1.0 - tau) * 2.4;
                float fresnelRim = tau * sqrt(max(tau, 0.0)) * (0.28 + 0.58 * rimChalkiness);

                // Micro-facet sand-grain frost modulation
                float grainPattern = (sandNoise.x * 0.5 + sandNoise.y * 0.5) * 0.5 + 0.5;
                float sandFrost = coarseness * (0.10 + 0.14 * grainPattern + 0.12 * tau);

                float3 bodyColor = mix(tintSrgb, chalkWhite, clamp(coarseness * 0.34 + fresnelRim * 0.48 + windward * 0.42, 0.0, 0.92));
                bodyColor = mix(bodyColor, tintSrgb, clamp(leeward * 0.55, 0.0, 0.65));

                float totalAlpha = clamp(
                    0.28 + tintDensity * 0.36 + sandFrost + fresnelRim * 0.38 + windward * 0.24 + leeward * 0.18,
                    0.22,
                    0.92
                ) * edgeAlpha;

                return half4(half3(bodyColor * totalAlpha), half(totalAlpha));
            }
        """.trimIndent()

        return CompiledShaderBundle(
            dialect = ShaderDialect.AGSL_100,
            vertexShaderSource = null,
            kawaseDownsampleFragmentSource = null,
            kawaseUpsampleFragmentSource = null,
            seaGlassCompositeFragmentSource = agslComposite,
            sandGrainOverlayFragmentSource = agslSandGrainOverlay
        )
    }
}
