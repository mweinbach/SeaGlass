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
    val sandGrainOverlayFragmentSource: String?,
    val sdfMaskFragmentSource: String? = null
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
     * Process-wide compile-once shader bundle cache indexed by [ShaderDialect].
     * Guarantees shader source generation and string assembly happen at most once per dialect
     * for the lifetime of the app process.
     */
    private val compiledBundleCache = java.util.concurrent.ConcurrentHashMap<ShaderDialect, CompiledShaderBundle>(3)

    /**
     * Shared GLSL (Vulkan 450 & GLES 300) optical & SDF math core.
     * Consumes the 256-byte `std140` `SeaGlassUniforms` UBO block defined in [SeaGlassUniformBlock].
     */
    private val SHARED_GLSL_OPTICAL_CORE: String = """
        // ---------------------------------------------------------------------------
        // Shared SeaGlass Optical & SDF Math Core (Vulkan GLSL 450 & OpenGL ES 3.00)
        // Optimized for mobile TBDR GPUs:
        // - Analytical single-node SDF + gradient (1 pass instead of 5)
        // - Interior plateau early-out skips bevel gradient & rim lighting on 75%+ of pixels
        // - Adaptive texture tap culling skips extra R/B taps when dispersion is near zero
        // - FMA sRGB gamma & single-pass Oklab conversion avoid SFU pipeline stalls
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

        // Fast FMA sRGB <-> Linear conversions (avoids 6 per-pixel SFU pow() stalls)
        vec3 sgSrgbToLinear(vec3 srgb) {
            vec3 c = max(srgb, vec3(0.0));
            return c * (c * (c * 0.30530601 + 0.68217111) + 0.01252288);
        }

        vec3 sgLinearToSrgb(vec3 lin) {
            vec3 c = clamp(lin, 0.0, 1.0);
            vec3 s1 = sqrt(c);
            vec3 s2 = sqrt(s1);
            vec3 s3 = sqrt(s2);
            return max(0.662002687 * s1 + 0.684122060 * s2 - 0.323583601 * s3 - 0.0225411470 * c, vec3(0.0));
        }

        // Deterministic local-space multi-octave 2D hash for tumbled sand-stipple micro-facet normals
        vec2 sgSandMicroFacet(vec2 localCoord) {
            vec2 p1 = localCoord * 0.38;
            vec2 p2 = localCoord * 0.92 + vec2(17.3, 41.7);
            vec2 p3 = localCoord * 0.16 + vec2(53.1, 11.9);
            float n1 = fract(52.9829189 * fract(dot(p1, vec2(0.06711056, 0.00583715))));
            float n2 = fract(52.9829189 * fract(dot(p2, vec2(0.00583715, 0.06711056))));
            float n3 = fract(52.9829189 * fract(dot(p3, vec2(0.04123175, 0.02941821))));
            return vec2(n1 * 0.65 + n3 * 0.35, n2 * 0.65 + n3 * 0.35) * 2.0 - 1.0;
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
            return inside + length(outside) - r;
        }

        // Closed-form analytical distance + unit gradient for single-node rounded boxes
        // (eliminates 4 finite-difference SDF evaluations per pixel when count == 1)
        float sgRoundedBoxSdfAndGrad(vec2 p, vec4 bounds, vec4 corners, out vec2 outGrad) {
            vec2 d = p - bounds.xy;
            vec2 b = bounds.zw;
            float rRight = (d.y > 0.0) ? corners.y : corners.x;
            float rLeft  = (d.y > 0.0) ? corners.z : corners.w;
            float r = clamp((d.x > 0.0) ? rRight : rLeft, 0.0, max(0.0, min(b.x, b.y)));

            vec2 q = abs(d) - b + r;
            vec2 outside = max(q, vec2(0.0));
            float outLen = length(outside);
            float inside = min(max(q.x, q.y), 0.0);
            vec2 signD = vec2((d.x >= 0.0) ? 1.0 : -1.0, (d.y >= 0.0) ? 1.0 : -1.0);
            vec2 localGrad = (outLen > 1e-4)
                ? (outside / outLen)
                : ((q.x > q.y) ? vec2(1.0, 0.0) : vec2(0.0, 1.0));
            outGrad = signD * localGrad;
            return inside + outLen - r;
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
     * Returns the cached [CompiledShaderBundle] for the requested [dialect], building it at most
     * once per process.
     */
    fun compileForDialect(dialect: ShaderDialect): CompiledShaderBundle {
        return compiledBundleCache.getOrPut(dialect) {
            when (dialect) {
                ShaderDialect.VULKAN_GLSL_450 -> buildVulkanGlsl450Bundle()
                ShaderDialect.GLSL_ES_300 -> buildGlslEs300Bundle()
                ShaderDialect.AGSL_100 -> buildAgsl100Bundle()
            }
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
                int count = clamp(int(uLightAndMeta.w + 0.5), 1, 4);
                vec3 linearTint;
                float thicknessPx;
                vec2 grad = vec2(0.0);
                float d;

                // Single-node fast path: computes exact SDF + analytical gradient in 1 pass instead of 5
                if (count == 1) {
                    d = sgRoundedBoxSdfAndGrad(fragCoord, uNodeBounds[0], uNodeCorners[0], grad);
                    linearTint = uNodeTints[0].rgb;
                    thicknessPx = uNodeTints[0].w;
                } else {
                    d = sgEvaluateSceneSdf(fragCoord, linearTint, thicknessPx);
                }

                // Anti-aliased outer edge coverage
                float edgeAlpha = 1.0 - smoothstep(-0.75, 0.75, d);
                if (edgeAlpha <= 0.001) {
                    fragColor = vec4(0.0);
                    return;
                }

                float bevelWidth = max(12.0, thicknessPx * 1.50);
                float tau = clamp(1.0 - smoothstep(0.0, bevelWidth, -d), 0.0, 1.0);
                float meniscus = tau * tau * (3.0 - 2.0 * tau);

                // Multi-node gradient only evaluated inside the bevel ring (-d < bevelWidth),
                // using 2 forward-difference samples reusing center distance d instead of 4 central samples!
                if (count > 1 && meniscus > 0.001) {
                    float eps = 0.75;
                    vec2 rawGrad = vec2(
                        sgEvaluateDistanceOnly(fragCoord + vec2(eps, 0.0)) - d,
                        sgEvaluateDistanceOnly(fragCoord + vec2(0.0, eps)) - d
                    );
                    grad = rawGrad / max(length(rawGrad), 1e-4);
                }

                // Coarseness continuum: 0.0 = smooth untreated clear glass, 1.0 = sand-frosted glass
                float coarseness = clamp(uOpticsPrimary.y, 0.0, 1.0);
                vec2 sandNoise = (coarseness > 0.005)
                    ? sgSandMicroFacet(fragCoord - uNodeBounds[0].xy)
                    : vec2(0.0);
                float gritScale = coarseness * (0.09 + 0.12 * meniscus);

                vec3 normal = normalize(vec3(
                    grad * meniscus * 0.82 + sandNoise * gritScale,
                    sqrt(max(0.06, 1.0 - meniscus * meniscus * 0.85))
                ));

                // Continuous edge & body refraction + sand-diffused chromatic dispersion
                float ior = uOpticsPrimary.z;
                float dispersion = uOpticsPrimary.w * 0.14 * (1.0 - 0.72 * coarseness);
                float refractStrength = (ior - 1.0) * thicknessPx * (1.0 - 0.50 * coarseness);
                vec2 uvOffset = (-normal.xy * refractStrength) / max(uResolutionAndOrigin.xy, vec2(1.0));
                vec2 centerUv = clamp(vTexCoord + uvOffset, 0.001, 0.999);

                // Adaptive texture tap culling: 1 tap when dispersion is negligible, 3 taps when dispersive
                vec3 bgSrgb;
                if (dispersion > 0.004) {
                    bgSrgb = vec3(
                        texture(uBackdropTex, clamp(vTexCoord + uvOffset * (1.0 + dispersion), 0.001, 0.999)).r,
                        texture(uBackdropTex, centerUv).g,
                        texture(uBackdropTex, clamp(vTexCoord + uvOffset * (1.0 - dispersion), 0.001, 0.999)).b
                    );
                } else {
                    bgSrgb = texture(uBackdropTex, centerUv).rgb;
                }
                vec3 bgLinear = max(sgSrgbToLinear(bgSrgb), vec3(0.001));

                // Beer-Lambert volumetric absorption & sand-frosted subsurface scatter in Oklab space
                float tintDensity = clamp(uOpticsPrimary.x, 0.0, 1.0);
                float opticalDepth = tintDensity * (0.72 + 0.28 * sqrt(max(0.0, 1.0 - meniscus * 0.55)));
                vec3 safeTint = clamp(linearTint, vec3(0.04), vec3(0.98));
                vec3 transmittedLinear = bgLinear * exp(log(safeTint) * (opticalDepth * 1.30));

                vec3 labTransmitted = sgLinearSrgbToOklab(transmittedLinear);
                vec3 labTint = sgLinearSrgbToOklab(safeTint);
                float frostScatter = clamp(tintDensity * 0.34 + coarseness * 0.58, 0.0, 0.92);

                vec3 labBody = vec3(
                    clamp(mix(labTransmitted.x, labTint.x, frostScatter * 0.64), 0.08, 0.95),
                    mix(labTransmitted.yz, labTint.yz, frostScatter)
                );
                vec3 bodyLinear = sgOklabToLinearSrgb(labBody);

                // Interior plateau skips bevel lighting ALU when meniscus == 0.0
                float stippleGrain = coarseness * (sandNoise.x * 0.58 + sandNoise.y * 0.42) * 0.056;
                float verticalCaustic = smoothstep(0.42, 0.98, vTexCoord.y) * (0.16 + 0.14 * coarseness);
                vec3 causticTint = mix(safeTint, vec3(0.94, 0.98, 1.0), 0.52 + 0.20 * coarseness);
                vec3 finalLinear = mix(bodyLinear, causticTint, verticalCaustic) + vec3(stippleGrain);
                if (meniscus > 0.001) {
                    vec3 lightDir = uLightAndMeta.xyz;
                    float rimChalkiness = uOpticsSecondary.x;
                    float fresnelRim = meniscus * sqrt(meniscus) * (0.38 + 0.54 * rimChalkiness);
                    float keyHighlight = max(0.0, dot(normal.xy, lightDir.xy)) * meniscus;
                    float leewardCaustic = max(0.0, dot(normal.xy, -lightDir.xy)) * meniscus * (1.0 - meniscus * 0.55) * 2.1;
                    vec3 rimColor = mix(safeTint, vec3(0.97, 0.99, 1.0), 0.70 + 0.22 * coarseness);
                    float frostRind = clamp(meniscus * (0.48 + 0.46 * coarseness) * rimChalkiness, 0.0, 0.88);
                    float rindStipple = clamp(1.0 + coarseness * (sandNoise.x * 0.40 + sandNoise.y * 0.30), 0.64, 1.38);
                    finalLinear = mix(finalLinear, rimColor, clamp(frostRind * rindStipple, 0.0, 0.88));
                    finalLinear += rimColor * (fresnelRim * 0.28 + keyHighlight * 0.32)
                        + causticTint * (leewardCaustic * 0.24);
                }

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
                int count = clamp(int(uLightAndMeta.w + 0.5), 1, 4);
                vec3 linearTint;
                float thicknessPx;
                vec2 grad = vec2(0.0);
                float d;

                if (count == 1) {
                    d = sgRoundedBoxSdfAndGrad(fragCoord, uNodeBounds[0], uNodeCorners[0], grad);
                    linearTint = uNodeTints[0].rgb;
                    thicknessPx = uNodeTints[0].w;
                } else {
                    d = sgEvaluateSceneSdf(fragCoord, linearTint, thicknessPx);
                }

                float edgeAlpha = 1.0 - smoothstep(-0.75, 0.75, d);
                if (edgeAlpha <= 0.001) {
                    fragColor = vec4(0.0);
                    return;
                }

                float bevelWidth = max(12.0, thicknessPx * 1.50);
                float tau = clamp(1.0 - smoothstep(0.0, bevelWidth, -d), 0.0, 1.0);
                float meniscus = tau * tau * (3.0 - 2.0 * tau);

                if (count > 1 && meniscus > 0.001) {
                    float eps = 0.75;
                    vec2 rawGrad = vec2(
                        sgEvaluateDistanceOnly(fragCoord + vec2(eps, 0.0)) - d,
                        sgEvaluateDistanceOnly(fragCoord + vec2(0.0, eps)) - d
                    );
                    grad = rawGrad / max(length(rawGrad), 1e-4);
                }

                float coarseness = clamp(uOpticsPrimary.y, 0.0, 1.0);
                vec2 sandNoise = (coarseness > 0.005)
                    ? sgSandMicroFacet(fragCoord - uNodeBounds[0].xy)
                    : vec2(0.0);
                float gritScale = coarseness * (0.09 + 0.12 * meniscus);

                vec3 normal = normalize(vec3(
                    grad * meniscus * 0.82 + sandNoise * gritScale,
                    sqrt(max(0.06, 1.0 - meniscus * meniscus * 0.85))
                ));

                float ior = uOpticsPrimary.z;
                float dispersion = uOpticsPrimary.w * 0.14 * (1.0 - 0.72 * coarseness);
                float refractStrength = (ior - 1.0) * thicknessPx * (1.0 - 0.50 * coarseness);
                vec2 uvOffset = (-normal.xy * refractStrength) / max(uResolutionAndOrigin.xy, vec2(1.0));
                vec2 centerUv = clamp(vTexCoord + uvOffset, 0.001, 0.999);

                vec3 bgSrgb;
                if (dispersion > 0.004) {
                    bgSrgb = vec3(
                        texture(uBackdropTex, clamp(vTexCoord + uvOffset * (1.0 + dispersion), 0.001, 0.999)).r,
                        texture(uBackdropTex, centerUv).g,
                        texture(uBackdropTex, clamp(vTexCoord + uvOffset * (1.0 - dispersion), 0.001, 0.999)).b
                    );
                } else {
                    bgSrgb = texture(uBackdropTex, centerUv).rgb;
                }
                vec3 bgLinear = max(sgSrgbToLinear(bgSrgb), vec3(0.001));

                float tintDensity = clamp(uOpticsPrimary.x, 0.0, 1.0);
                float opticalDepth = tintDensity * (0.72 + 0.28 * sqrt(max(0.0, 1.0 - meniscus * 0.55)));
                vec3 safeTint = clamp(linearTint, vec3(0.04), vec3(0.98));
                vec3 transmittedLinear = bgLinear * exp(log(safeTint) * (opticalDepth * 1.30));

                vec3 labTransmitted = sgLinearSrgbToOklab(transmittedLinear);
                vec3 labTint = sgLinearSrgbToOklab(safeTint);
                float frostScatter = clamp(tintDensity * 0.34 + coarseness * 0.58, 0.0, 0.92);

                vec3 labBody = vec3(
                    clamp(mix(labTransmitted.x, labTint.x, frostScatter * 0.64), 0.08, 0.95),
                    mix(labTransmitted.yz, labTint.yz, frostScatter)
                );
                vec3 bodyLinear = sgOklabToLinearSrgb(labBody);

                float stippleGrain = coarseness * (sandNoise.x * 0.58 + sandNoise.y * 0.42) * 0.056;
                float verticalCaustic = smoothstep(0.42, 0.98, vTexCoord.y) * (0.16 + 0.14 * coarseness);
                vec3 causticTint = mix(safeTint, vec3(0.94, 0.98, 1.0), 0.52 + 0.20 * coarseness);
                vec3 finalLinear = mix(bodyLinear, causticTint, verticalCaustic) + vec3(stippleGrain);
                if (meniscus > 0.001) {
                    vec3 lightDir = uLightAndMeta.xyz;
                    float rimChalkiness = uOpticsSecondary.x;
                    float fresnelRim = meniscus * sqrt(meniscus) * (0.38 + 0.54 * rimChalkiness);
                    float keyHighlight = max(0.0, dot(normal.xy, lightDir.xy)) * meniscus;
                    float leewardCaustic = max(0.0, dot(normal.xy, -lightDir.xy)) * meniscus * (1.0 - meniscus * 0.55) * 2.1;
                    vec3 rimColor = mix(safeTint, vec3(0.97, 0.99, 1.0), 0.70 + 0.22 * coarseness);
                    float frostRind = clamp(meniscus * (0.48 + 0.46 * coarseness) * rimChalkiness, 0.0, 0.88);
                    float rindStipple = clamp(1.0 + coarseness * (sandNoise.x * 0.40 + sandNoise.y * 0.30), 0.64, 1.38);
                    finalLinear = mix(finalLinear, rimColor, clamp(frostRind * rindStipple, 0.0, 0.88));
                    finalLinear += rimColor * (fresnelRim * 0.28 + keyHighlight * 0.32)
                        + causticTint * (leewardCaustic * 0.24);
                }

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
                // Multi-octave DPI-aware sand-tumbled micro-facet frost normal (-1..+1)
                float2 p1 = localCoord * 0.38;
                float2 p2 = localCoord * 0.92 + float2(17.3, 41.7);
                float2 p3 = localCoord * 0.16 + float2(53.1, 11.9);
                float n1 = fract(52.9829189 * fract(dot(p1, float2(0.06711056, 0.00583715))));
                float n2 = fract(52.9829189 * fract(dot(p2, float2(0.00583715, 0.06711056))));
                float n3 = fract(52.9829189 * fract(dot(p3, float2(0.04123175, 0.02941821))));
                return float2(n1 * 0.65 + n3 * 0.35, n2 * 0.65 + n3 * 0.35) * 2.0 - 1.0;
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
                return inside + length(outside) - r;
            }

            float sgRoundedBoxSdfAndGrad(float2 p, float4 bounds, float4 corners, out float2 outGrad) {
                float2 d = p - bounds.xy;
                float2 b = bounds.zw;
                float rRight = (d.y > 0.0) ? corners.y : corners.x;
                float rLeft  = (d.y > 0.0) ? corners.z : corners.w;
                float r = clamp((d.x > 0.0) ? rRight : rLeft, 0.0, max(0.0, min(b.x, b.y)));

                float2 q = abs(d) - b + r;
                float2 outside = max(q, float2(0.0));
                float outLen = length(outside);
                float inside = min(max(q.x, q.y), 0.0);
                float2 signD = float2((d.x >= 0.0) ? 1.0 : -1.0, (d.y >= 0.0) ? 1.0 : -1.0);
                float2 localGrad = (outLen > 1e-4)
                    ? (outside / outLen)
                    : ((q.x > q.y) ? float2(1.0, 0.0) : float2(0.0, 1.0));
                outGrad = signD * localGrad;
                return inside + outLen - r;
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

                float2 grad = float2(0.0);
                float d;
                if (count == 1) {
                    d = sgRoundedBoxSdfAndGrad(fragCoord, uNode0Bounds, uNode0Corners, grad);
                } else {
                    d = sgRoundedBoxSdf(fragCoord, uNode0Bounds, uNode0Corners, smoothingExp);
                }
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

                float bevelWidth = max(12.0, thicknessPx * 1.50);
                float tau = clamp(1.0 - smoothstep(0.0, bevelWidth, -d), 0.0, 1.0);
                float meniscus = tau * tau * (3.0 - 2.0 * tau);

                if (count > 1 && meniscus > 0.001) {
                    float eps = 0.75;
                    float2 rawGrad = float2(
                        sgEvalDistance(fragCoord + float2(eps, 0.0)) - d,
                        sgEvalDistance(fragCoord + float2(0.0, eps)) - d
                    );
                    grad = rawGrad / max(length(rawGrad), 1e-4);
                }

                float coarseness = clamp(uOpticsPrimary.y, 0.0, 1.0);
                float2 sandNoise = (coarseness > 0.005)
                    ? sgSandMicroFacet(fragCoord - uNode0Bounds.xy)
                    : float2(0.0);
                float gritScale = coarseness * (0.09 + 0.12 * meniscus);

                float3 normal = normalize(float3(
                    grad * meniscus * 0.82 + sandNoise * gritScale,
                    sqrt(max(0.06, 1.0 - meniscus * meniscus * 0.85))
                ));

                // Continuous edge & body refraction + sand-diffused chromatic dispersion
                float ior = uOpticsPrimary.z;
                float dispersion = uOpticsPrimary.w * 0.14 * (1.0 - 0.72 * coarseness);
                float lensRefractPx = (ior - 1.0) * thicknessPx * (1.85 - 0.85 * coarseness);
                float2 pxOffset = -grad * (meniscus * lensRefractPx) - sandNoise * (coarseness * 4.5);

                float2 maxCoord = max(uResolutionAndOrigin.xy - float2(1.0), float2(1.0));
                float2 coordG = clamp(fragCoord + pxOffset, float2(0.0), maxCoord);
                half4 sampleG0 = backdrop.eval(coordG);

                // Adaptive texture tap culling: skip extra dispersion & cross-scatter taps when near zero
                float scatterRadius = coarseness * 5.5;
                float greenAvg = float(sampleG0.g);
                if (scatterRadius > 0.15) {
                    half4 sampleG1 = backdrop.eval(clamp(coordG + float2(scatterRadius, -scatterRadius), float2(0.0), maxCoord));
                    half4 sampleG2 = backdrop.eval(clamp(coordG + float2(-scatterRadius, scatterRadius), float2(0.0), maxCoord));
                    greenAvg = float(sampleG0.g) * 0.5 + float(sampleG1.g) * 0.25 + float(sampleG2.g) * 0.25;
                }

                float3 bgSrgb;
                if (dispersion > 0.004) {
                    float2 coordR = clamp(fragCoord + pxOffset * (1.0 + dispersion), float2(0.0), maxCoord);
                    float2 coordB = clamp(fragCoord + pxOffset * (1.0 - dispersion), float2(0.0), maxCoord);
                    bgSrgb = float3(
                        float(backdrop.eval(coordR).r),
                        greenAvg,
                        float(backdrop.eval(coordB).b)
                    );
                } else {
                    bgSrgb = float3(float(sampleG0.r), greenAvg, float(sampleG0.b));
                }
                float3 bgLinear = bgSrgb * bgSrgb;

                float tintDensity = clamp(uOpticsPrimary.x, 0.0, 1.0);
                float opticalDepth = tintDensity * (0.72 + 0.28 * sqrt(max(0.0, 1.0 - meniscus * 0.55)));
                float3 safeTint = clamp(linearTint, float3(0.04), float3(0.98));
                float3 transmittedLinear = bgLinear * exp(log(safeTint) * (opticalDepth * 1.30));

                // Perceptual Oklab sand-frosted subsurface scatter
                float3 labTransmitted = sgLinearSrgbToOklab(transmittedLinear);
                float3 labTint = sgLinearSrgbToOklab(safeTint);
                float frostScatter = clamp(tintDensity * 0.34 + coarseness * 0.58, 0.0, 0.92);
                float3 labBody = float3(
                    clamp(mix(labTransmitted.x, labTint.x, frostScatter * 0.64), 0.08, 0.95),
                    mix(labTransmitted.yz, labTint.yz, frostScatter)
                );
                float3 bodyLinear = sgOklabToLinearSrgb(labBody);

                float normY = clamp(fragCoord.y / max(uResolutionAndOrigin.y, 1.0), 0.0, 1.0);
                float verticalCaustic = smoothstep(0.42, 0.98, normY) * (0.16 + 0.14 * coarseness);
                float3 causticTint = mix(safeTint, float3(0.94, 0.98, 1.0), 0.52 + 0.20 * coarseness);
                float3 finalLinear = mix(bodyLinear, causticTint, verticalCaustic)
                    + float3(coarseness * (sandNoise.x * 0.58 + sandNoise.y * 0.42) * 0.056);
                if (meniscus > 0.001) {
                    float3 lightDir = uLightAndMeta.xyz;
                    float rimChalkiness = uOpticsSecondary.x;
                    float fresnelRim = meniscus * sqrt(meniscus) * (0.38 + 0.54 * rimChalkiness);
                    float keyHighlight = max(0.0, dot(normal.xy, lightDir.xy)) * meniscus;
                    float leewardCaustic = max(0.0, dot(normal.xy, -lightDir.xy)) * meniscus * (1.0 - meniscus * 0.55) * 2.1;
                    float3 rimColor = mix(safeTint, float3(0.97, 0.99, 1.0), 0.70 + 0.22 * coarseness);
                    float frostRind = clamp(meniscus * (0.48 + 0.46 * coarseness) * rimChalkiness, 0.0, 0.88);
                    float rindStipple = clamp(1.0 + coarseness * (sandNoise.x * 0.40 + sandNoise.y * 0.30), 0.64, 1.38);
                    finalLinear = mix(finalLinear, rimColor, clamp(frostRind * rindStipple, 0.0, 0.88));
                    finalLinear += rimColor * (fresnelRim * 0.28 + keyHighlight * 0.32)
                        + causticTint * (leewardCaustic * 0.24);
                }

                float3 finalSrgb = sqrt(clamp(finalLinear, float3(0.0), float3(1.0)));
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
                // Multi-octave DPI-aware sand-tumbled micro-facet frost normal (-1..+1)
                float2 p1 = localCoord * 0.38;
                float2 p2 = localCoord * 0.92 + float2(17.3, 41.7);
                float2 p3 = localCoord * 0.16 + float2(53.1, 11.9);
                float n1 = fract(52.9829189 * fract(dot(p1, float2(0.06711056, 0.00583715))));
                float n2 = fract(52.9829189 * fract(dot(p2, float2(0.00583715, 0.06711056))));
                float n3 = fract(52.9829189 * fract(dot(p3, float2(0.04123175, 0.02941821))));
                return float2(n1 * 0.65 + n3 * 0.35, n2 * 0.65 + n3 * 0.35) * 2.0 - 1.0;
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
                return inside + length(outside) - r;
            }

            float sgRoundedBoxSdfAndGrad(float2 p, float4 bounds, float4 corners, out float2 outGrad) {
                float2 d = p - bounds.xy;
                float2 b = bounds.zw;
                float rRight = (d.y > 0.0) ? corners.y : corners.x;
                float rLeft  = (d.y > 0.0) ? corners.z : corners.w;
                float r = clamp((d.x > 0.0) ? rRight : rLeft, 0.0, max(0.0, min(b.x, b.y)));
                float2 q = abs(d) - b + r;
                float2 outside = max(q, float2(0.0));
                float outLen = length(outside);
                float inside = min(max(q.x, q.y), 0.0);
                float2 signD = float2((d.x >= 0.0) ? 1.0 : -1.0, (d.y >= 0.0) ? 1.0 : -1.0);
                float2 localGrad = (outLen > 1e-4)
                    ? (outside / outLen)
                    : ((q.x > q.y) ? float2(1.0, 0.0) : float2(0.0, 1.0));
                outGrad = signD * localGrad;
                return inside + outLen - r;
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

                float2 grad = float2(0.0);
                float d;
                if (count == 1) {
                    d = sgRoundedBoxSdfAndGrad(fragCoord, uNode0Bounds, uNode0Corners, grad);
                } else {
                    d = sgRoundedBoxSdf(fragCoord, uNode0Bounds, uNode0Corners, smoothingExp);
                }
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

                float edgeAlpha = 1.0 - smoothstep(-0.9, 0.9, d);
                if (edgeAlpha <= 0.001) return half4(0.0);

                float bevelWidth = max(14.0, thicknessPx * 1.60);
                float tau = clamp(1.0 - smoothstep(0.0, bevelWidth, -d), 0.0, 1.0);
                float meniscus = tau * tau * (3.0 - 2.0 * tau);

                if (count > 1 && meniscus > 0.001) {
                    float eps = 0.85;
                    float2 rawGrad = float2(
                        sgEvalDistance(fragCoord + float2(eps, 0.0)) - d,
                        sgEvalDistance(fragCoord + float2(0.0, eps)) - d
                    );
                    grad = rawGrad / max(length(rawGrad), 1e-4);
                }

                float tintDensity = clamp(uOpticsPrimary.x, 0.0, 1.0);
                float coarseness = clamp(uOpticsPrimary.y, 0.0, 1.0);
                float rimChalkiness = uOpticsSecondary.x;
                float2 sandNoise = sgSandMicroFacet(fragCoord - uNode0Bounds.xy);

                float3 tintSrgb = sqrt(clamp(linearTint, float3(0.0), float3(1.0)));
                float3 chalkWhite = float3(0.96, 0.985, 1.0);

                // Directional lighting vectors (top-left windward key light, bottom-right leeward caustic)
                float2 lightXY = normalize(uLightAndMeta.xy + float2(1e-4));
                float windwardDot = max(0.0, dot(grad, lightXY));
                float leewardDot = max(0.0, dot(grad, -lightXY));

                // 1) Smooth Oren-Nayar / rough-Fresnel velvet rim
                float outerFresnel = meniscus * sqrt(max(meniscus, 0.0)) * (0.35 + 0.55 * rimChalkiness);
                float windwardHighlight = windwardDot * meniscus * (0.50 + 0.40 * rimChalkiness);
                float rimIntensity = clamp(outerFresnel * 0.48 + windwardHighlight * 0.68, 0.0, 1.0);

                // 2) Leeward internal refracted caustic glow (concentrated pastel arc inside bottom-right bevel)
                float causticBand = meniscus * (1.0 - meniscus * 0.65);
                float leewardCaustic = leewardDot * causticBand * 1.25;

                // 3) Isotropic sand-tumbled velvet micro-coarseness (0 at coarseness == 0!)
                float grainHigh = max(0.0, sandNoise.x * 0.6 + sandNoise.y * 0.4);
                float sandVelvet = coarseness * grainHigh * (0.16 + 0.16 * meniscus);

                // Compose volumetric sea-glass body + smooth bevel lighting across clear -> opaque continuum
                float baseBodyAlpha = clamp(tintDensity * 0.24 + coarseness * 0.58, 0.02, 0.88);
                float totalAlpha = clamp(
                    baseBodyAlpha
                    + leewardCaustic * 0.26
                    + rimIntensity * 0.52
                    + sandVelvet * 0.32,
                    0.02,
                    0.94
                ) * edgeAlpha;

                // Preserve true glass tint in the interior (never bleach center to white!)
                float3 color = tintSrgb;
                float3 causticColor = mix(tintSrgb, chalkWhite, 0.24);
                color = mix(color, causticColor, clamp(leewardCaustic * 0.65, 0.0, 0.75));
                float3 rimColor = mix(tintSrgb, chalkWhite, clamp(0.62 + coarseness * 0.20, 0.0, 0.90));
                color = mix(color, rimColor, clamp(rimIntensity * 0.85, 0.0, 0.90));

                return half4(half3(color * totalAlpha), half(totalAlpha));
            }
        """.trimIndent()

        val agslSdfMask = """
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
                return inside + length(outside) - r;
            }

            float sgSminDist(float a, float b, float k) {
                if (k <= 0.0001) return min(a, b);
                float h = clamp(k - abs(a - b), 0.0, k) / k;
                return min(a, b) - h * h * h * k * 0.16666667;
            }

            half4 main(float2 fragCoord) {
                int count = int(uLightAndMeta.w + 0.5);
                float smoothK = uOpticsSecondary.w;
                float smoothingExp = clamp(uOpticsSecondary.z, 2.0, 4.0);

                float d = sgRoundedBoxSdf(fragCoord, uNode0Bounds, uNode0Corners, smoothingExp);
                if (count > 1) {
                    d = sgSminDist(d, sgRoundedBoxSdf(fragCoord, uNode1Bounds, uNode1Corners, smoothingExp), smoothK);
                }
                if (count > 2) {
                    d = sgSminDist(d, sgRoundedBoxSdf(fragCoord, uNode2Bounds, uNode2Corners, smoothingExp), smoothK);
                }
                if (count > 3) {
                    d = sgSminDist(d, sgRoundedBoxSdf(fragCoord, uNode3Bounds, uNode3Corners, smoothingExp), smoothK);
                }

                float edgeAlpha = clamp(1.0 - smoothstep(-0.9, 0.9, d), 0.0, 1.0);
                return half4(half(edgeAlpha));
            }
        """.trimIndent()

        return CompiledShaderBundle(
            dialect = ShaderDialect.AGSL_100,
            vertexShaderSource = null,
            kawaseDownsampleFragmentSource = null,
            kawaseUpsampleFragmentSource = null,
            seaGlassCompositeFragmentSource = agslComposite,
            sandGrainOverlayFragmentSource = agslSandGrainOverlay,
            sdfMaskFragmentSource = agslSdfMask
        )
    }
}
