# SeaGlass for Windows (WinUI 3 / Windows App SDK) Architecture & Component Blueprint

## 1. Design Goal

Deliver a **1:1 optical, geometric, and motion-physics twin** of the Android SeaGlass foundation for **Windows 10 (1809+) and Windows 11** applications using **WinUI 3 (`Microsoft.UI.Xaml`)**, **Windows Visual Layer (`Microsoft.UI.Composition`)**, **Direct2D / Win2D (`Microsoft.Graphics.Canvas`)**, and **HLSL Shader Model 5.0 (`ps_5_0` / `d2d1`)**.

Every component, palette, SDF liquid coalescing bridge, 20-shape polygon morph, adaptive corner optic solver, and dual-spring invariant in `windows/` is mathematically identical to `android/`.

---

## 2. Multi-Tier Windows GPU Pipeline & Backdrop DAG

Just as Android SeaGlass separates `Scene & Optical State` from GPU execution across `AGSL_FULL`, `RENDER_EFFECT_HYBRID`, `GLES3_NATIVE`, and `LIGHTWEIGHT_TINT`, **SeaGlass for Windows** routes its 256-byte `SeaGlassUniformBlock` across four Windows GPU tiers:

```text
            ┌────────────────────────────────────────────────────────────┐
            │        UI Layer (SeaGlass.WinUI XAML & Composition)        │
            │        SeaGlassMaterial + IReadOnlyList<SdfNode>           │
            └─────────────────────────────┬──────────────────────────────┘
                                          │
                                          ▼
            ┌────────────────────────────────────────────────────────────┐
            │      SeaGlassUniformBlock (256-byte cbuffer / std140)      │
            │  16 × float4 registers = 64 floats = 256 bytes             │
            │  Matches C++20 `SeaGlassUniformBlockStd140` & Kotlin ABI   │
            └─────────────────────────────┬──────────────────────────────┘
                                          │
         ┌────────────────────────────────┼────────────────────────────────┐
         ▼                                ▼                                ▼
┌──────────────────────┐       ┌──────────────────────┐       ┌──────────────────────┐
│   D2D1_HLSL_FULL     │       │ COMPOSITION_HYBRID   │       │   LIGHTWEIGHT_TINT   │
│  (Win 11 / D3D 11.1) │       │  (Win 10 / Win2D)    │       │  (Battery / A11y)    │
├──────────────────────┤       ├──────────────────────┤       ├──────────────────────┤
│ • Dialect:           │       │ • VisualSurface      │       │ • Triggered when     │
│   HLSL_SM50 (ps_5_0) │       │   backdrop capture   │       │   AdvancedEffects    │
│ • Zero-copy          │       │ • GaussianBlurEffect │       │   disabled or Energy │
│   CompositionVisual  │       │   pyramid +          │       │   Saver active       │
│   Surface backdrop   │       │   analytical SDF     │       │ • Analytical SDF     │
│ • GaussianBlur +     │       │   Catmull-Rom clip + │       │   contour + Oklab    │
│   single-pass HLSL   │       │   Oklab Beer-Lambert │       │   Beer-Lambert fill  │
│   SDF & Oklab shader │       │   meniscus pass      │       │   + chalky bevel rim │
└──────────────────────┘       └──────────────────────┘       └──────────────────────┘
```

### 2.1 Zero-Copy Backdrop Capture Graph (`SeaGlassStage`)
To prevent recursive blur/tint feedback loops (Rule #4 in `AGENTS.md`), `SeaGlassStage` in WinUI 3 enforces an acyclic **Stage → Backdrop Visual → Foreground Glass Visual** graph:
1. **`SeaGlassStage.BackdropContent`**: Hosted in a dedicated background XAML container (`BackdropHost`). Its underlying `Microsoft.UI.Composition.Visual` (`ElementCompositionPreview.GetElementVisual(BackdropHost)`) is bound to a `CompositionVisualSurface` (`Compositor.CreateVisualSurface()`).
2. **`SeaGlassSurface` / `SeaGlassFluidGroup`**: Foreground glass controls never sit inside `BackdropHost`. Each foreground glass surface samples the `CompositionVisualSurface` translated by `-localOffsetInStage`, chains a pre-pass Gaussian/Dual-Kawase blur (`frostRadiusDp`), and evaluates the 256-byte `SeaGlassUniformBlock`.

### 2.2 Byte-Exact 256-Byte Constant Buffer (`cbuffer SeaGlassUniforms : register(b0)`)
Direct3D 11 / HLSL `cbuffer` packing rules align every `float4` register on a 16-byte boundary—identical to Vulkan and OpenGL ES 3.0 `std140`:

```hlsl
cbuffer SeaGlassUniforms : register(b0)
{
    float4 uResolutionAndOrigin; // [widthPx, heightPx, backdropOffsetX, backdropOffsetY]
    float4 uOpticsPrimary;       // [tintDensity, coarseness, ior, chromaticDispersion]
    float4 uOpticsSecondary;     // [rimChalkiness, thicknessPx, cornerSmoothing, smoothRadiusPx]
    float4 uLightAndMeta;        // [lightDirX, lightDirY, lightDirZ, activeNodeCount]
    float4 uNodeBounds[4];       // 4 x [centerX, centerY, halfWidth, halfHeight]
    float4 uNodeCorners[4];      // 4 x [rTopRight, rBottomRight, rBottomLeft, rTopLeft]
    float4 uNodeTints[4];        // 4 x [linearR, linearG, linearB, nodeThicknessPx]
};
```

In C# (`SeaGlass.Core.Backend.SeaGlassUniformBlock`), the backing buffer is a pre-allocated 64-element `float[]` and a `[StructLayout(LayoutKind.Explicit, Size = 256)]` blittable struct `SeaGlassUniformBlockStd140` that copies directly to native memory with **zero per-frame heap allocations** (Rule #3 in `AGENTS.md`).

---

## 3. Dual-Spring Motion Discipline in WinUI (`SeaGlassMotionScheme`)

WinUI 3's `Microsoft.UI.Composition` provides hardware-accelerated damped harmonic oscillators (`SpringVector3NaturalMotionAnimation` and `SpringScalarNaturalMotionAnimation`) running on the DWM composition thread at 60–240Hz.

Converting the Material 3 Expressive stiffness $k$ and damping ratio $\zeta$ from [`docs/agents/material3-expressive-tokens.md`](material3-expressive-tokens.md) uses the exact angular frequency relation $\omega_n = \sqrt{k} \implies T = \frac{2\pi}{\sqrt{k}}$:

| Spring Token | Damping Ratio ($\zeta$) | Stiffness ($k$) | WinUI Composition `Period` ($T = 2\pi / \sqrt{k}$) | Target Properties |
| :--- | :--- | :--- | :--- | :--- |
| **`FastSpatial`** | `0.62f` | `680f` | `240.9 ms` | Button press squish, pill selection bounce, corner radius morph, checkbox clover bloom |
| **`DefaultSpatial`** | `0.66f` | `410f` | `310.3 ms` | `SeaGlassFluidBar` indicator travel, SDF coalescing bridge merge/split, `thicknessDp` |
| **`SlowSpatial`** | `0.72f` | `240f` | `405.6 ms` | Sheet/dialog container expansion, choreographed shape-orb transitions |
| **`FastEffects`** | `1.00f` (Critically damped) | `1100f` | `189.4 ms` | `rimChalkiness`, focus ring glow, touch caustic intensity |
| **`DefaultEffects`** | `1.00f` (Critically damped) | `540f` | `270.4 ms` | Oklab `(L, a, b)` tint transitions, `tintDensity`, `coarseness`, `frostRadiusDp`, `ior`, `transparency` |

**Invariant**: Never animate `tintColor`, `coarseness`, `frostRadiusDp`, or `transparency` with an under-damped ($\zeta < 1.0$) spring. Spatial geometry bounces; optical properties converge smoothly without overshoot.

---

## 4. Adaptive Window, Bezel & Page Corner Optics on Windows

[`SeaGlassCornerAdaptationSolver`](../../windows/SeaGlass.Core/SeaGlassDeviceCornerOptics.cs) ports the 4-tier detection cascade and per-corner concentric solver to Windows:

1. **Windows 11 DWM Window Corner Detection (`DWM_WINDOW_CORNER_PREFERENCE`)**:
   - `DWMWCP_ROUND` (`8dp` outer window radius)
   - `DWMWCP_ROUNDSMALL` (`4dp` tool window radius)
   - Simulated flagship device bezel profiles (`Pixel 9/10 Pro 46dp`, `iPhone 16/17 Pro 55dp`, `Galaxy S25 Ultra 18dp`, `Galaxy S25+ 38dp`, `Pixel Fold Asymmetric 14/44dp`, `Surface Pro / Windows 11 Tablet 28dp`) for cross-platform design previews.
2. **Enclosing Page/Sheet Container Scope (`SeaGlassContainerCornerScope`)**:
   - Any nested `SeaGlassSurface`, `SeaGlassCard`, or `SeaGlassCornerEmbeddedPod` inside a rounded parent card/sheet (`32dp..34dp`) solves its 4 individual corners `(TL, TR, BR, BL)` concentrically via:
     $$d_{\text{eff}} = 0.65\min(d_x, d_y) + 0.35\max(d_x, d_y), \qquad R_{\text{concentric}, c} = \max(r_{\text{floor}}, R_{\text{outer}, c} - d_{\text{eff}})$$
   - Asymmetric edge-sum normalization (`tl + tr <= W`, `bl + br <= W`, `tl + bl <= H`, `tr + br <= H`) allows outer bezel-facing corners to reach up to $0.78\min(W, H)$ when paired with a compact inner corner (`14dp`).

---

## 5. 1:1 Component Parity Matrix (Android Compose $\leftrightarrow$ WinUI 3)

| Category | Android (`:seaglass-compose`) | Windows (`SeaGlass.WinUI`) | Optical & Interaction Behavior |
| :--- | :--- | :--- | :--- |
| **Theme & Stage** | `SeaGlassTheme`, `SeaGlassStage`, `Modifier.seaGlassBackdrop` | `SeaGlassTheme`, `SeaGlassStage` | Provides ambient `SeaGlassPalette`, `SeaGlassMotionScheme`, `SeaGlassTiltState`, `SeaGlassDeviceTier`, and acyclic backdrop capture. |
| **Fluid Coalescing** | `SeaGlassFluidGroup`, `Modifier.seaGlassFluidNode` | `SeaGlassFluidGroup` | Evaluates shared 4-node `sminCubic` SDF field with Oklab tint & thickness blending across liquid bridges. |
| **Surfaces & Pods** | `SeaGlassSurface`, `SeaGlassCard`, `SeaGlassDialogCard`, `SeaGlassBottomSheetSurface`, `SeaGlassCornerEmbeddedPod` | `SeaGlassSurface`, `SeaGlassCard`, `SeaGlassDialogCard`, `SeaGlassBottomSheetSurface`, `SeaGlassCornerEmbeddedPod` | 3D off-center pointer tilt (`4.8°`), HDR touch caustic wave, Oklab contrast-safe foreground content (`|\Delta L| >= 0.48`), and concentric corner adaptation. |
| **Buttons & Chips** | `SeaGlassButton`, `SeaGlassIconButton`, `SeaGlassExtendedFab`, `SeaGlassPill`, `SeaGlassFilterChip`, `SeaGlassBadge`, `SeaGlassSegmentedButtonRow` | `SeaGlassButton`, `SeaGlassIconButton`, `SeaGlassExtendedFab`, `SeaGlassPill`, `SeaGlassFilterChip`, `SeaGlassBadge`, `SeaGlassSegmentedButtonRow` | Physical slab compression on press (`pressed()`), velocity-stretched segmented lozenge indicator, and animated checkmark reveal. |
| **Navigation & Bars** | `SeaGlassTopAppBar`, `SeaGlassFluidBar`, `SeaGlassMiniPlayerBar`, `SeaGlassFloatingToolbar`, `SeaGlassSearchBar` | `SeaGlassTopAppBar`, `SeaGlassFluidBar`, `SeaGlassMiniPlayerBar`, `SeaGlassFloatingToolbar`, `SeaGlassSearchBar` | `NAVIGATION_CHROME` subsurface diffusion, coalescing detachable action orb (`sminCubic`), and focus-reactive search rim glow. |
| **Inputs & Toggles** | `SeaGlassTextField`, `SeaGlassSwitch`, `SeaGlassCheckbox`, `SeaGlassRadioButton`, `SeaGlassLiquidSlider` | `SeaGlassTextField`, `SeaGlassSwitch`, `SeaGlassCheckbox`, `SeaGlassRadioButton`, `SeaGlassLiquidSlider` | 3D water-drop lens thumb with velocity stretch (`scaleX = 1 + 0.28 * stretch`), `Squircle -> Cookie4` clover morphing checkbox, and magnifying slider lens. |
| **Feedback & Lists** | `SeaGlassLinearProgressIndicator`, `SeaGlassCircularProgressIndicator`, `SeaGlassListItem`, `SeaGlassSnackbar`, `SeaGlassIcon` (`SeaGlassGlyph`) | `SeaGlassLinearProgressIndicator`, `SeaGlassCircularProgressIndicator`, `SeaGlassListItem`, `SeaGlassSnackbar`, `SeaGlassIcon` (`SeaGlassGlyph`) | Oklab gradient progress bars, medallion arc sweep, setting rows, floating toast banners, and 20 vector glyphs. |
| **Dynamic Morphing** | `SeaGlassTwoElementMorph`, `SeaGlassBuddingCluster`, `SeaGlassMorphingButton`, `SeaGlassChoreographedShapeOrb`, `SeaGlassMultiShapeMorph`, `SeaGlassMorphingContextMenu`, `SeaGlassCascadedModelMorph`, `SeaGlassContextRingMorph` | `SeaGlassTwoElementMorph`, `SeaGlassBuddingCluster`, `SeaGlassMorphingButton`, `SeaGlassChoreographedShapeOrb`, `SeaGlassMultiShapeMorph`, `SeaGlassMorphingContextMenu`, `SeaGlassCascadedModelMorph`, `SeaGlassContextRingMorph` | 2-node teardrop `sminCubic` popover morph, in-flight Gaussian content defocus, 20-shape `RoundedPolygon` morph with circle midpoint bridge (`pivot = 0.42`), and budding action satellites. |
