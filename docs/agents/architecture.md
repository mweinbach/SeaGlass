# SeaGlass Backend-Agnostic GPU Architecture Blueprint

## 1. Design Goal

Deliver a backend-agnostic GPU graphics and UI foundation for Kotlin Android applications that renders authentic **sanded sea glass** with **Material 3 Expressive** geometry/colors and **Liquid Glass** fluid state coalescing at 60–120fps across **Vulkan 1.1+**, **Android AGSL (`RuntimeShader` + `RenderNode`)**, **OpenGL ES 3.0**, and **Hardware Canvas** from `minSdk = 26` through `compileSdk = 36+`.

---

## 2. Backend-Agnostic `std140` Uniform ABI & Multi-Dialect Shader Compiler

Rather than coupling UI components to a single graphics API, `:seaglass-core` separates **Scene & Optical State** from **GPU Backend Execution**:

```text
           ┌────────────────────────────────────────────────────────┐
           │     UI Layer (:seaglass-compose / :seaglass-view)      │
           │     SeaGlassMaterial + List<SeaGlassSdfEngine.SdfNode> │
           └───────────────────────────┬────────────────────────────┘
                                       │
                                       ▼
           ┌────────────────────────────────────────────────────────┐
           │         SeaGlassUniformBlock (256-byte std140)         │
           │  16 × vec4 slots = 64 floats = 256 bytes               │
           │  Matches C++20 `SeaGlassUniformBlockStd140` byte-exact │
           └───────────────────────────┬────────────────────────────┘
                                       │
           ┌───────────────────────────┼───────────────────────────┐
           ▼                           ▼                           ▼
┌──────────────────────┐   ┌──────────────────────┐   ┌──────────────────────┐
│    VULKAN 1.1+       │   │   AGSL RENDER NODE   │   │   OPENGL ES 3.0      │
│  VulkanSeaGlass      │   │  AgslSeaGlass        │   │  Gles3SeaGlass       │
│  Backend             │   │  Binding             │   │  Backend             │
├──────────────────────┤   ├──────────────────────┤   ├──────────────────────┤
│ • Dialect:           │   │ • Dialect:           │   │ • Dialect:           │
│   VULKAN_GLSL_450    │   │   AGSL_100           │   │   GLSL_ES_300        │
│ • DescriptorSet 0:   │   │ • Zero-copy hardware │   │ • GL_UNIFORM_BUFFER  │
│   binding 0 = UBO    │   │   RenderNode backdrop│   │   binding 0 (256 B)  │
│   binding 1 = sampler│   │ • RenderEffect blur  │   │ • Dual-Kawase FBO    │
│ • Dual-Kawase push   │   │   + RuntimeShader    │   │   downsample/upsample│
│   constants (16 B)   │   │   single-pass SDF    │   │   + SDF composite    │
└──────────────────────┘   └──────────────────────┘   └──────────────────────┘
```

### 2.1 The 256-Byte `std140` Uniform Buffer Layout (`SeaGlassUniformBlock`)

Every parameter required to evaluate a coalescing cluster of up to 4 SeaGlass shapes is packed into 16 `vec4` registers (`256 bytes`):

| Vec4 Index | Byte Offset | Uniform Name | Components (`x, y, z, w`) |
| :--- | :--- | :--- | :--- |
| `0` | `0..15` | `uResolutionAndOrigin` | `widthPx`, `heightPx`, `backdropOffsetX`, `backdropOffsetY` |
| `1` | `16..31` | `uOpticsPrimary` | `tintDensity`, `coarseness`, `ior`, `chromaticDispersion` |
| `2` | `32..47` | `uOpticsSecondary` | `rimChalkiness`, `thicknessPx`, `cornerSmoothing`, `smoothRadiusPx` |
| `3` | `48..63` | `uLightAndMeta` | `lightDirX`, `lightDirY`, `lightDirZ`, `activeNodeCount` |
| `4..7` | `64..127` | `uNodeBounds[0..3]` | `centerX`, `centerY`, `halfWidth`, `halfHeight` |
| `8..11` | `128..191` | `uNodeCorners[0..3]` | `rTopRight`, `rBottomRight`, `rBottomLeft`, `rTopLeft` |
| `12..15` | `192..255` | `uNodeTints[0..3]` | `linearR`, `linearG`, `linearB`, `nodeThicknessPx` |

Because every member is a `vec4`, there is **zero alignment padding ambiguity** between Vulkan `std140`/`std430`, OpenGL ES 3.0 `std140`, and C++20 `struct alignas(16) SeaGlassUniformBlockStd140` (`android/seaglass-core/src/main/cpp/seaglass_backend_abi.h`).

### 2.2 Multi-Dialect Shader Compiler (`SeaGlassShaderCompiler`)
`SeaGlassShaderCompiler.compileForDialect(dialect)` generates complete shader sources from a shared optical & SDF core:
- **`ShaderDialect.VULKAN_GLSL_450`**: Emits `#version 450 core` vertex, Dual-Kawase downsample/upsample (with `layout(push_constant)`), and the SeaGlass SDF + Oklab Beer-Lambert composite fragment shader with `layout(set = 0, binding = 0, std140) uniform SeaGlassUniforms` and `layout(set = 0, binding = 1) uniform sampler2D uBackdropTex`.
- **`ShaderDialect.GLSL_ES_300`**: Emits `#version 300 es` vertex, Dual-Kawase downsample/upsample, and the composite fragment shader consuming `layout(std140) uniform SeaGlassUniforms`.
- **`ShaderDialect.AGSL_100`**: Emits the Android 13+ `RuntimeShader` composite shader and sand-grain overlay shader.

---

## 3. Backdrop Capture Graph & Acyclic Invariant

A common bug in real-time glass libraries is capturing a parent View/Composable that already contains the glass element, causing infinite recursive blur/tint feedback loops.

SeaGlass enforces an explicit **Stage → Backdrop → Glass Surface** DAG:

1. **`Modifier.seaGlassBackdrop(stageState)`**: Attached ONLY to the background content hierarchy. Records background draw commands into `stageState.backdropLayer` (`GraphicsLayer`) and tracks the backdrop's root window coordinates.
2. **`Modifier.seaGlass(stageState, material, ...)`**: Attached to foreground UI elements. Reads `stageState.backdropLayer`, translates the backdrop into the glass element's local coordinate space (`-localOffset`), packs the 256-byte `SeaGlassUniformBlock`, and executes the active backend tier.
3. **`SeaGlassFluidGroup`**: Coordinates multiple sibling `SeaGlassNode` instances (e.g., navigation bar items, morphing FAB + action pills). Each child registers its bounds, corner radii, and material state into the group's shared `SeaGlassUniformBlock`, allowing adjacent elements to smoothly coalesce (`smin`) and exchange tint/thickness as they animate.
