# Device Compatibility & GPU Performance Guide

SeaGlass is engineered to run embedded inside Kotlin Android apps across a wide range of devices (`minSdk = 26` through `compileSdk = 36+`) and mobile Tile-Based Deferred Rendering (TBDR) GPUs (Qualcomm Adreno, ARM Mali/Immortalis, Samsung Xclipse, Imagination PowerVR).

---

## 1. Device Capability Matrix

| Tier | Android Version | Primary Mechanism | Typical GPU Cost (1080p) | Fallback Trigger |
| :--- | :--- | :--- | :--- | :--- |
| **`AGSL_FULL`** | Android 13+ (API 33–37+) | `GraphicsLayer` (`RenderNode`) + `RenderEffect.createBlurEffect` + `RenderEffect.createRuntimeShaderEffect` (AGSL) | `0.25ms – 0.45ms` | Used automatically on all API 33+ hardware-accelerated canvases unless battery saver / reduce-transparency is enabled. |
| **`RENDER_EFFECT_HYBRID`** | Android 12–12L (API 31–32) | `GraphicsLayer` (`RenderNode`) + `RenderEffect.createBlurEffect` + analytical SDF contour clip + hardware Canvas procedural grain & Oklab tint pass | `0.35ms – 0.60ms` | Used automatically on API 31–32 devices. |
| **`GLES3_NATIVE`** | Android 8.0–11 (API 26–30) | Native C++20 `libseaglass_native.so` Dual-Kawase pyramid /GLSL ES 3.00 shader + downsampled backdrop capture & analytical SDF Canvas composite | `0.55ms – 0.95ms` | Used on API 26–30 devices. |
| **`LIGHTWEIGHT_TINT`** | All APIs (26+) | Analytical SDF smooth-union contour + Oklab Beer-Lambert translucent fill + procedural sand-grain shader + directional chalky bevel rim (no backdrop readback) | `< 0.08ms` | Triggered when `PowerManager.isPowerSaveMode` is active (if configured), accessibility reduce-transparency is enabled, or inside headless/Compose Preview environments. |

---

## 2. Mobile TBDR GPU Optimization Rules

1. **Avoid Dependent Texture Reads Inside Loop Bodies**:
   - In `AGSL_FULL`, hardware blur is executed *prior* to the AGSL `RuntimeShader` via `RenderEffect.createChainEffect(runtimeShaderEffect, blurEffect)` so the fragment shader performs only 3 texture fetches (R, G, B chromatic dispersion offsets) rather than a 25-tap Gaussian kernel inside AGSL.
2. **Bounded SDF Node Count (`MAX_SDF_NODES = 4`)**:
   - Mobile fragment shaders suffer register pressure when loops have dynamic bounds or large uniform arrays. `SeaGlassShaderSource` unrolls/bounds SDF coalescing to 4 active nodes per local cluster, packing node centers/extents (`vec4`), corner radii (`vec4`), and tint colors (`vec4`) into compact uniform arrays.
3. **Half-Precision (`half` / `mediump`) Arithmetic**:
   - In AGSL, color, normal, and noise math use `half` / `half3` / `half4` (16-bit FP16 on Adreno/Mali ALUs, running at 2× throughput of FP32), while SDF spatial coordinates (`fragCoord`, centers, extents) use `float` / `float2` (FP32) to prevent sub-pixel jitter during slow spring animations.
4. **Stationary Local-Space Sand Grain**:
   - The procedural micro-coarseness noise is anchored to `(fragCoord - uPrimaryCenter)` rather than absolute screen coordinates so translating a glass card across the screen does not cause high-frequency aliasing or "swimming" frost.
