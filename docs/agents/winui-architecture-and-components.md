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
3. **Backdrop bounds registration (WPF `VisualBrush` path)**: The sharp and frosted backdrop visuals are sampled with a `Viewbox` relative to each visual's *content bounding box*. Those bounds must equal the stage rect exactly — clip the backdrop's `OnRender` to `(0, 0, stageW, stageH)` and set `ClipToBounds` on the blurred host — otherwise overdraw (oversized gradient pools, blur padding) silently misregisters refraction for every glass surface.
4. **Desktop composition (`SeaGlass.Sample` / SeaGlass Studio)**: The backdrop renders the real window layer stack (Mica title bar + sidebar, edge-to-edge content layer with an 8px top-left corner, document canvas, pane dividers). Window chrome, sidebar, inspector, and status bar are flat layers; sea glass is reserved for surfaces that float above content (toolbars, flyouts, HUDs, specimens, droplets, controls) — never cards on a wallpaper.

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

---

## 6. Consumer App Integration & Pure C# Page Hosting Guidance

When consuming SeaGlass via NuGet (`<PackageReference Include="SeaGlass.WinUI" Version="1.0.0" />`):

1. **Theme Registration (`App.xaml`)**:
   ```xml
   <ResourceDictionary.MergedDictionaries>
       <controls:XamlControlsResources />
       <sg:SeaGlassThemeResources />
   </ResourceDictionary.MergedDictionaries>
   ```

2. **Hosting Pure C# (Code-Only) Pages**:
   - **WinUI 3 Pitfall**: Calling `Frame.Navigate(typeof(PureCSharpPage))` invokes `ABI.Microsoft.UI.Xaml.Controls.INavigateMethods.Navigate`, which queries `IXamlMetadataProvider`. When given a C#-only `Page` subclass without XAML code generation, WinUI 3 native runtime crashes with `0xC0000005` (Access Violation).
   - **Recommended Pattern**: Host code-only pages via direct content assignment (`ContentControl.Content = new MyFluentPage()` or `ContentFrame.Content = pageInstance`). If `Frame.Navigate` is strictly required, provide a minimal `.xaml` companion file to register `IXamlMetadataProvider`.

---

## 7. 58-Control WinUI 3 Gallery Architecture & Universal Showcase Pattern

The interactive Windows showcase (`windows/SeaGlass.Sample/`) implements the **Universal Showcase Pattern** to present and test all 58 controls across 8 categories:

1. **Category Navigation (`ControlShowcaseCatalog.cs`)**:
   - **Surfaces & Pods**: `SeaGlassSurface`, `SeaGlassCard`, `SeaGlassAdaptiveCornerLabCard`, `SeaGlassCornerEmbeddedPod`, `SeaGlassSurfaceContainer`
   - **Buttons & Chips**: `SeaGlassButton`, `SeaGlassIconButton`, `SeaGlassExtendedFab`, `SeaGlassPill`, `SeaGlassFilterChip`, `SeaGlassBadge`, `SeaGlassSegmentedButtonRow`, `SeaGlassSegmentedButtonItem`, `SeaGlassPebbleOrb`
   - **Navigation & Bars**: `SeaGlassTopAppBar`, `SeaGlassFluidBar`, `SeaGlassMiniPlayerBar`, `SeaGlassFloatingToolbar`, `SeaGlassSearchBar`
   - **Inputs & Toggles**: `SeaGlassTextField`, `SeaGlassSwitch`, `SeaGlassCheckbox`, `SeaGlassRadioButton`, `SeaGlassLiquidSlider`
   - **Feedback & Lists**: `SeaGlassLinearProgressIndicator`, `SeaGlassCircularProgressIndicator`, `SeaGlassListItem`, `SeaGlassSnackbar`, `SeaGlassIcon`
   - **Dynamic Morphing**: `SeaGlassTwoElementMorph`, `SeaGlassBuddingCluster`, `SeaGlassMorphingButton`, `SeaGlassChoreographedShapeOrb`, `SeaGlassMultiShapeMorph`, `SeaGlassMorphingContextMenu`, `SeaGlassCascadedModelMorph`, `SeaGlassContextRingMorph`
   - **Overlays & Sheets**: `SeaGlassBottomSheetSurface`, `SeaGlassFloatingSheet`, `SeaGlassDialogCard`, `SeaGlassActionSheet`, `SeaGlassContextMenuOverlay`, `SeaGlassLiveActivityPill`, `SeaGlassMagnifierScrubber`, `SeaGlassNotificationStack`, `SeaGlassWheelPicker`, `SeaGlassControlCenterGrid`
   - **Windows Extras**: `SeaGlassTitleBar`, `SeaGlassNavigationView`, `SeaGlassWindowBackdropHost`, `SeaGlassAppWindowController`, `SeaGlassDropTargetOverlay`, `SeaGlassWindowSnappingGuide`, `SeaGlassWindowTabStrip`, `SeaGlassCompactOverlayPresenter`, `SeaGlassAdaptiveCornerPresenter`, `SeaGlassMultiMonitorCoordinator`, `SeaGlassTaskbarThumbnailPresenter`, `SeaGlassWindowGlowHost`
2. **Universal Showcase Engine (`ControlShowcasePage.cs`)**:
   - **Interactive Viewport**: Live specimen hosted within an acyclic `SeaGlassStage` with ocean-gradient refraction backdrop.
   - **Parameter Tuning Drawer**: Real-time sliders for `coarseness`, `tintDensity`, `thicknessDp`, `ior`, and `rimChalkiness` driving live UBO updates.
   - **Reactive Snippet Viewer**: Generates copy-pasteable XAML and C# fluent API markup reflecting current live property mutations.
3. **Automated Headless & Self-Test Verification**:
   - `--self-test`: Instantiates and stress-tests all 58 control pages, exercises interactive gestures on 10 core controls, and reports telemetry.
   - `--headless-verify`: Executes full off-screen verification cycle without displaying desktop UI.
   - `--verify-catalog`: Asserts 100% compliance with `E2ECatalogContractRegistry`.

---

## 8. Accessibility & UIA Automation Peers Architecture

All interactive SeaGlass controls provide complete Microsoft UI Automation (UIA) support via custom `AutomationPeer` implementations in `windows/SeaGlass.WinUI/Automation/`:

- **Modular Standalone Architecture**: Peers are partitioned across 8 per-category source files (`SeaGlassSurfaceAutomationPeers.cs`, `SeaGlassButtonAutomationPeers.cs`, `SeaGlassInputAutomationPeers.cs`, `SeaGlassNavigationAutomationPeers.cs`, `SeaGlassFeedbackAutomationPeers.cs`, `SeaGlassMorphingAutomationPeers.cs`, `SeaGlassOverlayAutomationPeers.cs`, `SeaGlassExtrasAutomationPeers.cs`) to ensure standalone extraction tools (`Copy-SeaGlassComponent.ps1`) compile with 0 missing type references.
- **Implemented UIA Provider Patterns**:
  - `IInvokeProvider`: `SeaGlassButton`, `SeaGlassIconButton`, `SeaGlassExtendedFab`, `SeaGlassSegmentedButtonItem`, `SeaGlassListItem`, `SeaGlassSnackbar`, `SeaGlassDialogCard`
  - `IToggleProvider`: `SeaGlassPill`, `SeaGlassFilterChip`, `SeaGlassSwitch`, `SeaGlassCheckbox`
  - `IRangeValueProvider`: `SeaGlassLiquidSlider`, `SeaGlassLinearProgressIndicator`, `SeaGlassCircularProgressIndicator`, `SeaGlassMagnifierScrubber`
  - `IValueProvider`: `SeaGlassTextField`, `SeaGlassWheelPicker`
  - `IExpandCollapseProvider`: `SeaGlassExtendedFab`, `SeaGlassBottomSheetSurface`, `SeaGlassFloatingSheet`, `SeaGlassActionSheet`, `SeaGlassContextMenuOverlay`, `SeaGlassLiveActivityPill`, `SeaGlassNotificationStack`
  - `IWindowProvider`: `SeaGlassBottomSheetSurface`, `SeaGlassFloatingSheet`, `SeaGlassDialogCard`, `SeaGlassActionSheet`, `SeaGlassContextMenuOverlay`
  - `ISelectionProvider` / `ISelectionItemProvider`: `SeaGlassSegmentedButtonRow`, `SeaGlassSegmentedButtonItem`, `SeaGlassPill`, `SeaGlassFilterChip`, `SeaGlassRadioButton`, `SeaGlassListItem`, `SeaGlassWheelPicker`

---

## 9. DWM Window Capture & Optical Variance Verification

To guarantee genuine optical rendering without relying on mock or synthetic assets:

1. **DWM Window Capture (`DwmWindowCapture.cs`)**:
   - Top-down 32-bit BGRA DIBSection capture using Win32 GDI `BitBlt` with `CAPTUREBLT` flag.
   - Multi-layer compositing support (`PrintWindow` with `PW_RENDERFULLCONTENT` fallback).
   - Rejects blank, completely black, or zero-variance uniform buffers to detect unmapped or minimized windows.
2. **Optical Variance Verifier (`OpticalVarianceVerifier.cs`)**:
   - Computes perceptual luminance standard deviation across the central refractive aperture, requiring $\sigma_L \ge 1.5$ LSB to verify real sand-grain micro-coarseness and Beer-Lambert volumetric absorption.
   - Measures rough-Fresnel chalky rim illumination delta between the edge bevel and interior core, requiring $\Delta L_{\text{rim}} \ge 4.0$ LSB to guarantee non-flat rim scatter.


