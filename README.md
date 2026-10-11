# SeaGlass

**SeaGlass** is a GPU-accelerated graphics and UI foundation for Kotlin Android apps that combines the tactile, sand-tumbled optics of **real ocean sea glass**, the vibrant pastel palettes and organic geometry of **Material 3 Expressive**, and the fluid, state-morphing adaptability of **Liquid Glass**.

---

## Why SeaGlass?

Standard translucent UI treatments on Android either rely on a generic Gaussian blur with a flat alpha overlay—which looks muddy and lifeless—or mimic wet, hyper-specular crystal that clashes with Material Design's warm, expressive visual language.

**SeaGlass** models the actual physical optics of tumbled beach glass and pairs them with Material 3 Expressive motion and geometry:

1. **Sand-Tumbled Micro-Coarseness (`coarseness`)**:
   Ocean-tumbled glass is etched by sand and salt water. Instead of sharp mirror reflections, SeaGlass simulates micro-facet surface frost using procedural multi-frequency blue-noise normal perturbation and isotropic subsurface diffusion. You get a velvety, chalky-matte surface with luminous depth underneath.
2. **Volumetric Beer-Lambert Body Tinting in Oklab Space (`tintColor`, `tintDensity`, `thicknessDp`)**:
   Real sea glass is colored throughout its volume (cobalt blue, seafoam jade, beach amber, manganese lavender, rose shell). SeaGlass computes spectral transmittance in perceptual **Oklab** space using Beer-Lambert absorption ($T(\lambda) = e^{-\sigma(\lambda) \cdot d}$), preserving backdrop highlights while richly coloring midtones and thicker glass regions.
3. **Chalky Frosted Rim & Softened Edge Refraction (`ior`, `rimChalkiness`)**:
   Analytical Signed Distance Field (SDF) bevel normals bend background light near the tumbled edge (`IOR ≈ 1.49`, attenuated by surface coarseness) and catch a soft Oren-Nayar / rough-Fresnel frosted rim glow on the lit edge with a subtle translucent inner caustic on the opposite edge.
4. **Liquid Coalescing & Material 3 Expressive State Flow (`SeaGlassFluidGroup`, `smin`)**:
   UI elements are not rigid boxes. Pills, FABs, navigation bars, chips, and cards evaluate a shared 2D Signed Distance Field with polynomial smooth-minimum (`smin`) unions—flowing into and out of each other like tidal droplets when states transition, expand, or respond to touch gestures, driven by Material 3 Expressive dual-spring physics.
5. **Multi-Tier GPU Pipeline Across Android Devices (`minSdk = 26` to `API 36+`)**:
   - **Tier 1 (`AGSL_FULL`, Android 13 / API 33+)**: Zero-copy `RenderNode` backdrop capture + hardware `RenderEffect` blur pyramid + unified `RuntimeShader` (AGSL) evaluating multi-node SDF coalescing, bevel refraction, micro-facet sand grain, Beer-Lambert Oklab tinting, and chalky Fresnel rim in a single fragment pass.
   - **Tier 2 (`RENDER_EFFECT_HYBRID`, Android 12 / API 31–32)**: Hardware `RenderEffect.createBlurEffect` backdrop pipeline paired with hardware-accelerated Canvas SDF contour beveling, procedural grain shader, and Oklab volumetric tint composition.
   - **Tier 3 (`GLES3_NATIVE`, Android 8.0–11 / API 26–30 & Custom GL Offload)**: Native C++20 / JNI + OpenGL ES 3.0 engine (`libseaglass_native.so`) implementing a Dual-Kawase downsample/upsample blur pyramid and GLSL ES 3.00 SDF sea-glass fragment shader with identical optical math.
   - **Tier 4 (`LIGHTWEIGHT_TINT`, Accessibility / Battery Saver / Previews)**: Contrast-guaranteed Oklab frosted surface with analytical bevel rim and zero offscreen passes.

---

## Module Architecture

```text
SeaGlass/
├── android/                 # Android Gradle workspace (Kotlin, Jetpack Compose, Android Views, NDK/AGSL)
│   ├── seaglass-core/       # Kotlin + C++20 (NDK/GLES3/Vulkan) optical physics, Oklab math,
│   │                        # 2D SDF coalescing engine, AGSL & GLSL shaders, device tier & corner detector
│   ├── seaglass-compose/    # Jetpack Compose & Material 3 Expressive UI foundation:
│   │                        # SeaGlassStage, Modifier.seaGlass, SeaGlassFluidGroup,
│   │                        # SeaGlassSurface, SeaGlassCard, SeaGlassButton, SeaGlassIconButton,
│   │                        # SeaGlassExtendedFab, SeaGlassPill, SeaGlassFilterChip, SeaGlassBadge,
│   │                        # SeaGlassTopAppBar, SeaGlassFluidBar, SeaGlassMiniPlayerBar,
│   │                        # SeaGlassFloatingToolbar, SeaGlassSearchBar, SeaGlassTextField,
│   │                        # SeaGlassSwitch, SeaGlassCheckbox, SeaGlassRadioButton,
│   │                        # SeaGlassSegmentedButtonRow, SeaGlassLiquidSlider,
│   │                        # SeaGlassLinearProgressIndicator, SeaGlassCircularProgressIndicator,
│   │                        # SeaGlassListItem, SeaGlassSnackbar, SeaGlassDialogCard,
│   │                        # SeaGlassBottomSheetSurface, SeaGlassTwoElementMorph, SeaGlassBuddingCluster,
│   │                        # SeaGlassSkipBridge (Swift / Skip interop)
│   ├── seaglass-view/       # Classic Android View & Drawable interop:
│   │                        # SeaGlassFrameLayout, SeaGlassCardView, SeaGlassButtonView, SeaGlassDrawable
│   └── sample/              # Interactive SeaGlass Optical Lab & M3 Expressive Fluid Showcase app
├── windows/                 # Windows GPU graphics & UI foundation
└── docs/                    # Deep technical & mathematical blueprints + Swift/Skip interop
    ├── agents/
    └── skip/
```

---

## Quickstart (Jetpack Compose)

```kotlin
import dev.seaglass.compose.*
import dev.seaglass.core.*

@Composable
fun CoastalPlayerScreen() {
    SeaGlassTheme(palette = SeaGlassPalette.SeafoamMint) {
        val stageState = rememberSeaGlassStageState()

        Box(modifier = Modifier.fillMaxSize()) {
            // 1. Mark the background layer to be captured once per frame on the GPU
            RichExpressiveBackground(
                modifier = Modifier
                    .fillMaxSize()
                    .seaGlassBackdrop(stageState)
            )

            // 2. Render adaptive SeaGlass UI elements that refract, frost, and coalesce
            SeaGlassFluidGroup(
                stageState = stageState,
                smoothRadiusDp = 22f,
                modifier = Modifier.align(Alignment.BottomCenter)
            ) {
                SeaGlassFluidBar(
                    items = listOf("Tide", "Shells", "Optics", "Palette"),
                    selectedIndex = selectedTab,
                    onSelect = { selectedTab = it },
                    material = SeaGlassMaterial.seafoamMint(
                        coarseness = 0.42f,
                        frostRadiusDp = 24f,
                        tintDensity = 0.58f
                    )
                )
            }
        }
    }
}
```

## Quickstart (Classic Android Views)

```kotlin
val seaGlassLayout = SeaGlassFrameLayout(context).apply {
    material = SeaGlassPalette.CobaltTide.defaultMaterial.copy(
        coarseness = 0.45f,
        frostRadiusDp = 20f,
        cornerRadiusDp = 28f
    )
    bindBackdropTarget(backgroundContainerView)
}
```

---

## Curated Sea Glass × Material 3 Expressive Palettes

| Palette | Inspiration | Primary Tint | Default Coarseness | Optical Character |
| :--- | :--- | :--- | :--- | :--- |
| **`SeafoamMint`** | Surf-tumbled turquoise soda-lime glass | `#6EE7C8` | `0.38` | Bright coastal aquamarine pastel with crisp chalky frost rim |
| **`CobaltTide`** | Vintage apothecary & ocean bottle glass | `#6898FF` | `0.34` | Deep periwinkle-cobalt absorption with luminous azure edges |
| **`BeachAmber`** | Sun-warmed honey & driftwood bottle glass | `#FFB86B` | `0.42` | Warm golden-apricot pastel with soft sand-etched diffusion |
| **`SeaLavender`** | Sun-purpled manganese sea glass | `#C59BFF` | `0.40` | Expressive lilac-violet tint with velvety matte frost |
| **`RoseQuartzShell`** | Rare coral & cranberry tumbled glass | `#FF96B8` | `0.36` | Warm rose-blush translucency with pearlescent bevel glow |
| **`KelpForest`** | Deep botanical forest-green sea glass | `#58C991` | `0.44` | Rich emerald-sage body absorption with high sand grit |
| **`MilkGlassFrost`** | Opaque-translucent chalky white glass | `#ECF4F8` | `0.58` | High-scatter milky sea glass with soft alabaster rim |
| **`ObsidianTide`** | Volcanic & deep-water smoked glass | `#1D2B3A` | `0.35` | Dark-mode smoked slate glass with cool moonlight rim |

---

## Documentation

- [AGENTS.md](AGENTS.md) — Engineering rules, module invariants, and verification commands.
- [Architecture Blueprint](docs/agents/architecture.md) — Multi-tier GPU pipeline, backdrop graph, and C++ GLES3 fallback.
- [Optical Physics & Shaders](docs/agents/optical-physics-and-shaders.md) — SDF `smin` coalescing, Oklab Beer-Lambert absorption, and micro-facet frost shaders.
- [Material 3 Expressive Tokens](docs/agents/material3-expressive-tokens.md) — Shape morphing, tonal palettes, and dual-spring physics.
- [Device Compatibility & Performance](docs/agents/device-compatibility-and-performance.md) — API 26–37+ tier matrix and TBDR GPU optimization guide.
- [Adaptive Device/Page Corner Optics & Swift/Skip Interop](docs/agents/adaptive-corner-optics-and-skip-interop.md) — Concentric bezel/page corner matching, collision-free corner pod layout, and Swift/Skip (`skip.tools`) integration via `SeaGlassSkipBridge` & [`docs/skip/SeaGlassSkipUI.swift`](docs/skip/SeaGlassSkipUI.swift).
