# AGENTS.md — SeaGlass Graphics & UI Foundation (Android & Windows)

SeaGlass is a high-performance GPU graphics and UI foundation for **Android** (`android/`) and **Windows** (`windows/`) applications that fuses three ideas into one cohesive system:

1. **Sanded Sea Glass Optics**: Ocean-tumbled, sand-frosted glass with isotropic micro-facet surface coarseness (`coarseness`), volumetric Beer-Lambert body tinting in perceptual Oklab color space (`tintColor`, `tintDensity`, `thicknessDp`), softened edge refraction (`ior`), and chalky Oren-Nayar / rough-Fresnel rim illumination (`rimChalkiness`)—never glossy plastic or harsh mirror-specular glare.
2. **Material 3 Expressive Geometry & Pastels**: Saturated-yet-soft sea-glass pastel palettes (Seafoam Mint, Cobalt Tide, Beach Amber, Sea Lavender, Rose Quartz Shell, Kelp Forest, Milk Glass Frost, Obsidian Tide), squircle/superellipse contours, asymmetric expressive corner morphing, and dual-spring physics (`spatial` springs that allow organic bounce vs. `effects` springs with zero overshoot for color/opacity/frost).
3. **Liquid Glass Adaptability & Coalescing**: UI elements are alive and continuous. Neighboring surfaces inside a `SeaGlassFluidGroup` evaluate a shared 2D Signed Distance Field (SDF) with polynomial smooth-minimum (`smin`) unions, flowing into and out of each other like tidal droplets as states expand, collapse, or respond to drag and press gestures.

---

## Repository Structure

| Path | Module / Purpose |
| :--- | :--- |
| `android/` | Root of the Android Gradle project (`settings.gradle.kts`, `build.gradle.kts`, `gradlew`). |
| `android/seaglass-core/` (`:seaglass-core`) | Backend-agnostic Kotlin + C++20 (`seaglass_backend_abi.h`) optical math, Oklab/Beer-Lambert color physics, 2D SDF coalescing engine (`SeaGlassSdfEngine`), 256-byte `std140` UBO ABI (`SeaGlassUniformBlock`), multi-dialect shader compiler (`SeaGlassShaderCompiler`: Vulkan GLSL 450 / SPIR-V, OpenGL ES 3.00, and Android AGSL), and pluggable GPU backends (`VulkanSeaGlassBackend`, `Gles3SeaGlassBackend`, `SeaGlassDeviceTier`). |
| `android/seaglass-compose/` (`:seaglass-compose`) | Jetpack Compose & Material 3 Expressive UI foundation: `SeaGlassStage` zero-copy backdrop capture, `Modifier.seaGlass`, `SeaGlassFluidGroup` SDF coalescing coordinator, `SeaGlassTheme` (`ColorScheme.toSeaGlassMaterials`), `SeaGlassSurface`, `SeaGlassCard`, `SeaGlassButton`, `SeaGlassIconButton`, `SeaGlassExtendedFab`, `SeaGlassPill`, `SeaGlassFilterChip`, `SeaGlassBadge`, `SeaGlassTopAppBar`, `SeaGlassFluidBar`, `SeaGlassMiniPlayerBar`, `SeaGlassFloatingToolbar`, `SeaGlassSearchBar`, `SeaGlassTextField`, `SeaGlassSwitch`, `SeaGlassCheckbox`, `SeaGlassRadioButton`, `SeaGlassSegmentedButtonRow`, `SeaGlassLiquidSlider`, `SeaGlassLinearProgressIndicator`, `SeaGlassCircularProgressIndicator`, `SeaGlassListItem`, `SeaGlassSnackbar`, `SeaGlassDialogCard`, `SeaGlassBottomSheetSurface`, `SeaGlassIcon`, `SeaGlassSkipBridge`, and the dynamic morphing suite (`SeaGlassTwoElementMorph`, `SeaGlassBuddingCluster`, `SeaGlassMorphingButton`, `SeaGlassChoreographedShapeOrb`). |
| `android/seaglass-view/` (`:seaglass-view`) | Classic Android `View` / XML interop (`SeaGlassFrameLayout`, `SeaGlassCardView`, `SeaGlassButtonView`, `SeaGlassDrawable` with multi-node SDF coalescing and 20-shape morphing) backed by `:seaglass-core` so legacy or hybrid View hierarchies can embed SeaGlass without Compose. |
| `android/sample/` (`:sample`) | Interactive **SeaGlass Optical Lab & Expressive Showcase** Android app demonstrating live parameter tuning, M3 Expressive palette morphing, draggable SDF liquid coalescing, and GPU backend telemetry. |
| `windows/` | Windows GPU graphics & UI foundation and sample showcase (companion to `android/`). |
| `docs/agents/` | Deep architectural blueprints and mathematical specifications for agents working on shaders, UI components, or device tiers. |

---

## Agent Documentation Index

Read the relevant blueprint before modifying shaders, optical physics, motion springs, or device fallback tiers:

- [`docs/agents/architecture.md`](docs/agents/architecture.md) — Multi-tier GPU rendering pipeline (`AGSL_FULL`, `RENDER_EFFECT_HYBRID`, `GLES3_NATIVE`, `LIGHTWEIGHT_TINT`), zero-copy `RenderNode` backdrop graph, and JNI native engine lifecycle.
- [`docs/agents/optical-physics-and-shaders.md`](docs/agents/optical-physics-and-shaders.md) — Exact equations and uniform layouts for SDF smooth-union (`smin`), analytical bevel normals, procedural sand-grain micro-coarseness, Oklab Beer-Lambert volumetric absorption, and rough-Fresnel chalky rim glow.
- [`docs/agents/material3-expressive-tokens.md`](docs/agents/material3-expressive-tokens.md) — Material 3 Expressive color harmonization, sea-glass pastel palettes, squircle/corner-morphing geometry, and `spatial` vs. `effects` spring physics invariants.
- [`docs/agents/device-compatibility-and-performance.md`](docs/agents/device-compatibility-and-performance.md) — Android API 26–37+ tier matrix, TBDR GPU tile-memory rules (Adreno, Mali, Xclipse, PowerVR), frame-time budgets, and verification procedures.
- [`docs/agents/adaptive-corner-optics-and-skip-interop.md`](docs/agents/adaptive-corner-optics-and-skip-interop.md) — 4-tier hardware display corner detection (`WindowInsets.getRoundedCorner` / `Display.getRoundedCorner` / `SystemUI dimen` / `Build.MODEL`), concentric & asymmetric per-corner math, collision-free corner pod layout (`solveCornerPodLayoutPx`), and Swift / Skip (`skip.tools`) interop (`SeaGlassSkipBridge` & [`docs/skip/SeaGlassSkipUI.swift`](docs/skip/SeaGlassSkipUI.swift)).

---

## Non-Negotiable Engineering Rules

1. **Sea Glass Is Frosted & Volumetric, Not Mirror-Glossy**:
   - Never replace Beer-Lambert volumetric absorption (`T = exp(-sigma * thickness)`) with a naive `srcOver` flat alpha fill. A flat alpha wash muddies the backdrop; real sea glass transmits luminance while shifting hue and chroma through its thickness.
   - Surface coarseness (`coarseness` in `0f..1f`) must perturb both the refraction vector and the micro-facet surface grain so higher coarseness diffuses edge distortion into a velvety sanded matte finish and boosts chalky rim scatter.
2. **Backend-Agnostic `std140` UBO & Shader Parity Across Vulkan, GLSL ES 3.00, and AGSL**:
   - All GPU backends consume the exact same 256-byte (`16 × vec4`) `std140` uniform block (`SeaGlassUniformBlock` in Kotlin / `SeaGlassUniformBlockStd140` in `seaglass_backend_abi.h`).
   - `SeaGlassShaderCompiler` generates `ShaderDialect.VULKAN_GLSL_450` (`#version 450 core`), `ShaderDialect.GLSL_ES_300` (`#version 300 es`), and `ShaderDialect.AGSL_100` (`RuntimeShader`) from a shared mathematical core. Any change to optical parameters or SDF operations must keep all three dialects and `SeaGlassSdfEngine` in 1:1 parity.
3. **Zero Per-Frame Heap Allocations in Draw Loops**:
   - Inside Compose `drawWithCache` / `drawBehind` and `View.onDraw`, never allocate `Paint`, `Path`, `RuntimeShader`, `RenderEffect`, or `FloatArray` instances per frame. Pre-allocate and mutate uniform buffers in place.
4. **Prevent Backdrop Feedback Loops**:
   - `SeaGlassStage` records the background content layer into a dedicated `GraphicsLayer` / `RenderNode` *before* any `Modifier.seaGlass` surface draws. Never record a glass surface into the same backdrop layer it samples from.
5. **Dual-Spring Motion Discipline (Material 3 Expressive)**:
   - Use **spatial springs** (`dampingRatio = 0.62f..0.75f`, `stiffness = 380f..520f`) for geometry, position, scale, corner radii, and SDF coalescing bridges so shapes feel elastic and tactile.
   - Use **effects springs** (`dampingRatio = 1.0f` critically damped, `stiffness = 600f..1200f`) for tint color, coarseness, frost radius, and opacity so visual properties never flicker or overshoot out of gamut.
6. **Multi-Device Resilience (API 26–37+)**:
   - Every public API in `:seaglass-compose` and `:seaglass-view` must compile and run cleanly from `minSdk = 26` through `compileSdk = 36+`. Guard API 31 (`RenderEffect`) and API 33 (`RuntimeShader`) code paths with `@RequiresApi` and route through `SeaGlassDeviceTier`.

---

## Build, Test, and Verification Commands (Android)

Run Gradle commands from `android/` (or pass `-p android`):

- **Run all JVM unit tests (Oklab math, Beer-Lambert optics, SDF geometry & coalescing, uniform packing, shader source validation)**:
  ```bash
  cd android && ./gradlew testDebugUnitTest
  ```
- **Assemble all library AARs and the `:sample` debug APK**:
  ```bash
  cd android && ./gradlew assembleDebug
  ```
- **Install and launch the `:sample` showcase on a connected device or emulator**:
  ```bash
  adb install -r android/sample/build/outputs/apk/debug/sample-debug.apk
  adb shell am start -n dev.seaglass.sample/.MainActivity
  ```
- **Inspect layout & capture visual evidence on device**:
  - Use `android screen capture` or `adb exec-out screencap -p` to verify optical rendering, tint legibility, and SDF coalescing on live hardware/emulator frames.
