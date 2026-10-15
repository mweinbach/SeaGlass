# SeaGlass WinUI 3 — E2E Testing Infrastructure Specification

## 1. Overview & Architectural Principles

This document defines the **requirement-driven, opaque-box End-to-End (E2E) testing infrastructure** for **SeaGlass for Windows (WinUI 3 / Windows App SDK)**.

### 1.1 Opaque-Box & Requirement-Driven Philosophy
In strict adherence to the project guidelines, tests are authored **without inspecting private implementation internals**. Every test is derived directly from:
- `ORIGINAL_REQUEST.md` (WinUI 3 migration, shadcn/SwiftUI ergonomics, 58-control catalog, Windows extras, UI automation, live settings interop).
- `AGENTS.md` (volumetric Beer-Lambert optics, 256-byte uniform block parity, zero per-frame heap allocations, acyclic backdrop graph, dual-spring motion invariants).
- `docs/agents/winui-architecture-and-components.md` (4-tier GPU render pipeline, Direct2D/HLSL SM 5.0, CompositionVisualSurface backdrop, 1:1 component parity matrix).
- `docs/agents/optical-physics-and-shaders.md` (analytical squircle SDF, polynomial cubic smooth-min `sminCubic`, stationary sand micro-coarseness, rough-Fresnel rim illumination, Oklab perceptual color space).
- `docs/agents/material3-expressive-tokens.md` (8 signature sea-glass pastel palettes, spatial vs effects spring discipline).
- `docs/agents/adaptive-corner-optics-and-skip-interop.md` (4-tier hardware corner detection, concentric corner adaptation, non-protrusion edge-sum normalization).

### 1.2 Expected Output Derivation & Authoritative Sources
For every test case across all 4 tiers, expected outputs are derived from explicit authoritative sources:
1. **Mathematical Properties & Oracles**:
   - **Oklab Color Transform**: Forward and inverse cube-root LMS matrix transforms; round-trip color preservation within $\Delta \text{RGB} \le 2$.
   - **Beer-Lambert Volumetric Absorption**: $T = \exp(-\boldsymbol{\sigma} \ell)$ in linear sRGB / Oklab; deeper glass increases chroma and decreases lightness without bleaching toward white.
   - **Polynomial Cubic Smooth Minimum (`sminCubic`)**: $h = \max(k - |d_a - d_b|, 0) / k$; blended distance $\text{smin} = \min(d_a, d_b) - \frac{1}{6} h^3 k$.
   - **2D Superellipse Squircle SDF**: $p \in [2.0, 3.2]$; distance at center equals $-\min(\text{halfW}, \text{halfH})$.
   - **Dual-Spring Dynamics**: Semi-implicit Euler harmonic oscillator; angular frequency $\omega_n = \sqrt{k}$, damping ratio $\zeta$, critically damped $\zeta = 1.0$ (zero overshoot), under-damped $\zeta < 1.0$ (organic bounce).
2. **Standard & ABI Specifications**:
   - **256-Byte Uniform Buffer ABI**: Exact $16 \times \text{vec4}$ (64 floats, 256 bytes) `cbuffer SeaGlassUniforms : register(b0)` layout matching C++20 `SeaGlassUniformBlockStd140` and Vulkan/GLSL `std140`.
   - **Zero Heap Allocations**: In steady-state draw and animation loops, `GC.GetAllocatedBytesForCurrentThread()` delta equals exactly 0 bytes.
   - **Acyclic Backdrop Graph**: Registration of a foreground glass surface as its own backdrop source immediately throws `InvalidOperationException`.
3. **Cross-Platform Behavioral Parity**:
   - Observable behavior and motion tokens match Android `:seaglass-compose` 1:1.

---

## 2. 4-Tier Test Architecture

The E2E test suite is organized into 4 distinct testing tiers:

```text
┌────────────────────────────────────────────────────────────────────────┐
│               SeaGlass WinUI 3 E2E Test Suite Architecture             │
├────────────────────────────────────────────────────────────────────────┤
│  Tier 1: Feature Coverage (>=5 test cases per feature across 58 controls│
│          and theming/ergonomics/optics)                                │
├────────────────────────────────────────────────────────────────────────┤
│  Tier 2: Boundary & Corner Cases (>=5 test cases per feature covering   │
│          limits, degenerate inputs, rapid gestures, extreme scales)    │
├────────────────────────────────────────────────────────────────────────┤
│  Tier 3: Cross-Feature Combinations (Pairwise coverage of major         │
│          subsystem interactions: SDF + Springs, Theme + Contrast, etc.)│
├────────────────────────────────────────────────────────────────────────┤
│  Tier 4: Real-World Application Scenarios (>=5 realistic application-  │
│          level end-to-end user workflows)                              │
└────────────────────────────────────────────────────────────────────────┘
```

### 2.1 Tier 1: Feature Coverage
Every feature and control in the specification is tested across at least 5 primary behaviors (happy path, state transitions, visual frame evaluation, optical property packing, and interaction callbacks):
- **Surfaces & Foundation Controls**: `SeaGlassSurface`, `SeaGlassCard`, `SeaGlassDialogCard`, `SeaGlassBottomSheetSurface`, `SeaGlassCornerEmbeddedPod`, `SeaGlassAdaptiveCornerLabCard`.
- **Buttons, Chips & Indicators**: `SeaGlassButton`, `SeaGlassIconButton`, `SeaGlassExtendedFab`, `SeaGlassPill`, `SeaGlassFilterChip`, `SeaGlassBadge`, `SeaGlassSegmentedButtonRow`, `SeaGlassPebbleOrb`.
- **Navigation & Bars**: `SeaGlassTopAppBar`, `SeaGlassFluidBar`, `SeaGlassMiniPlayerBar`, `SeaGlassFloatingToolbar`, `SeaGlassPillNavBar`, `SeaGlassPromptBar`, `SeaGlassSearchBar`.
- **Inputs & Toggles**: `SeaGlassTextField`, `SeaGlassSwitch`, `SeaGlassCheckbox`, `SeaGlassRadioButton`, `SeaGlassLiquidSlider`.
- **Feedback & Lists**: `SeaGlassLinearProgressIndicator`, `SeaGlassCircularProgressIndicator`, `SeaGlassListItem`, `SeaGlassSnackbar`, `SeaGlassIcon` (all 23 `SeaGlassGlyph`s).
- **Dynamic Morphing Suite**: `SeaGlassTwoElementMorph`, `SeaGlassBuddingCluster`, `SeaGlassMorphingButton`, `SeaGlassChoreographedShapeOrb`, `SeaGlassMultiShapeMorph`, `SeaGlassMorphingContextMenu`, `SeaGlassCascadedModelMorph`, `SeaGlassContextRingMorph`.
- **iOS 27 & Advanced Overlays**: `SeaGlassFloatingSheet`, `SeaGlassContextMenuOverlay`, `SeaGlassActionSheet`, `SeaGlassLiveActivityPill`, `SeaGlassMagnifierScrubber`, `SeaGlassNotificationStack`, `SeaGlassWheelPicker`, `SeaGlassControlCenterGrid`.
- **Windows Extras**: `SeaGlassTitleBar`, `SeaGlassNavigationView`, `SeaGlassTabView`, `SeaGlassCommandBar`, `SeaGlassInfoBar`, `SeaGlassTeachingTip`, `SeaGlassFlyout` / `SeaGlassMenuFlyout`, `SeaGlassToolTip`.
- **Theming, Ergonomics & Optics**:
  - 8 Signature Palettes (`SeafoamMint`, `CobaltTide`, `BeachAmber`, `SeaLavender`, `RoseQuartzShell`, `KelpForest`, `MilkGlassFrost`, `ObsidianTide`).
  - Shadcn Variants (`Primary`, `Secondary`, `Tertiary`, `Ghost`, `Outline`, `Subtle`, `Destructive`).
  - Shadcn Sizes (`Sm`, `Md`, `Lg`, `Icon`).
  - Dual-Spring Motion Scheme (`FastSpatial`, `DefaultSpatial`, `SlowSpatial`, `FastEffects`, `DefaultEffects`).
  - Multi-Node SDF Coalescing & Bridge Separation (`sminCubic`).
  - 256-Byte Uniform Buffer ABI Parity (`std140` / `cbuffer`).
  - Concentric Corner Adaptation Solver.
  - UI Automation Peers & Patterns.
  - Live Platform Settings Interop.
  - Steady-State Zero Heap Allocations.

### 2.2 Tier 2: Boundary & Corner Cases
Tests edge conditions, extreme ranges, degenerate geometry, and rapid interactions (>=5 test cases per feature domain):
- **Zero and Negative Dimensions**: Controls handling $W=0, H=0$, negative bounds clamping, and zero-thickness surfaces.
- **Empty and Long Text Strings**: Empty labels, single-character labels, and 10,000-character overflow strings without null references or layout crashes.
- **Rapid Gesture Flooding**: Multi-click rapid pointer events during in-flight spring animations; velocity accumulation without jumping.
- **Extreme Optical Parameters**: `coarseness = 0.0` (pure specular) vs `1.0` (deep diffuse matte), `tintDensity = 0.0` vs `1.0`, `ior = 1.0` vs `2.0`.
- **Boundary Corner Adaptation**: Outer bezel $R_{\text{bezel}} = 0$ (sharp window), $R_{\text{bezel}} = 200\text{dp}$ (extreme tablet curve), elements placed far beyond `proximityThresholdDp`, and edge-sum clamping ($r_1 + r_2 \le W$).
- **Slider Drag Bounds**: Values dragged below `Minimum` or above `Maximum`, fractional steps, and zero-range sliders.
- **Extreme Text Scaling**: Windows text scaling at 100%, 150%, 200%, and 250% asserting non-clipping inscribed safe padding.
- **Continuous 1,000-Frame Draw Loop**: Verifying strict zero GC allocation in steady state.

### 2.3 Tier 3: Cross-Feature Combinations
Pairwise integration testing verifying orthogonal subsystems interact without regressions:
1. **SDF Liquid Coalescing + Dual-Spring Motion**: Moving elements animate spatial position while polynomial cubic `smin` continuously evaluates liquid bridge extrusion and separation.
2. **Adaptive Corner Optics + 20-Shape Polygon Morphing**: A morphing container adapting its corner radii concentrically to enclosing window bezels.
3. **Beer-Lambert Volumetric Absorption + High-DPI Scaling**: Optical transmittance and chromatic dispersion evaluating cleanly at 100%, 150%, and 200% DPI rasterization.
4. **Live System Settings Interop + Interactive Controls**: High contrast, reduced motion, and energy saver modes applied to buttons, switches, and sliders while actively receiving user input.
5. **Acyclic Backdrop Host + Multi-Element Fluid Groups**: Fluid group elements sampling from backdrop visual surface without recursive feedback loops.
6. **Lead-Lag Lozenge + Temporal De-Aging**: Asymmetric dual-edge spring travel transiently reducing frost crystallization during high-velocity jumps.
7. **256-Byte Uniform Packing + Delta Slot Tracking**: Multiple controls updating optical and geometric parameters, verifying selective dirty-slot bitmask flags.

### 2.4 Tier 4: Real-World Application Scenarios
End-to-end validation of complete user application scenarios matching real-world WinUI 3 desktop use cases:
1. **Scenario 1: Optical Lab & Telemetry Inspector**: User adjusts refractive index, coarseness, and thickness using `SeaGlassLiquidSlider`, toggles shaders via `SeaGlassSwitch`, inspects live 256-byte uniform values in `SeaGlassAdaptiveCornerLabCard`, and views status badges.
2. **Scenario 2: Media Player & Live Activity Hub**: Streaming media player workflow featuring `SeaGlassMiniPlayerBar` with track title, album art, play/pause and next track actions, expandable `SeaGlassLiveActivityPill`, and volume slider with tactile feedback.
3. **Scenario 3: Multi-Tab Document Navigation with Fluid Indicator**: Productivity workspace featuring `SeaGlassFluidBar` and `SeaGlassPillNavBar`, switching across tabs with dual-edge lead-lag lozenge stretching, satellite budding action cluster, and modal dialog confirmations.
4. **Scenario 4: High-Contrast & Battery Saver Platform Transition**: Dynamic system state transition from full GPU glass rendering to Windows High Contrast mode and Energy Saver, verifying instant spring settling, fallback to `LightweightTint`, and accessible UI automation.
5. **Scenario 5: Form Input & Context Menu Workflow**: Complete user data entry workflow featuring `SeaGlassTextField` with focus-reactive rim glow, `SeaGlassCheckbox` with clover morphing, `SeaGlassSegmentedButtonRow` for category selection, and `SeaGlassMorphingContextMenu` for contextual operations.

---

## 3. Test Runner & Execution Guide

### 3.1 Test Framework & Dependencies
- **Platform**: .NET 10.0 (`net10.0-windows`) / C# 13.
- **Framework**: xUnit 2.9.3 + `Microsoft.NET.Test.Sdk` 18.10.1.
- **Runner**: `dotnet test`.

### 3.2 Directory Structure
All E2E test files are located in `windows/SeaGlass.Tests/E2E/`:
```text
windows/SeaGlass.Tests/E2E/
├── Infrastructure/
│   ├── E2EInfrastructure.cs               # Test fixtures, mock settings, math oracles, assertions
│   └── E2ECatalogContractRegistry.cs       # 58-control machine-checkable inventory contracts
├── Tier1_FeatureCoverage/
│   ├── Tier1_SurfacesAndButtonsTests.cs    # Surfaces, cards, buttons, chips, indicators
│   ├── Tier1_NavigationAndInputsTests.cs   # App bars, fluid bars, search, text, switches, sliders
│   ├── Tier1_FeedbackAndMorphingTests.cs   # Progress, list items, morphing suite, iOS 27 overlays
│   └── Tier1_WindowsExtrasAndThemingTests.cs # Windows extras, 8 palettes, variants, sizes, UIA
├── Tier2_BoundaryCorner/
│   └── Tier2_BoundaryAndCornerCaseTests.cs  # Edge cases, bounds, rapid gestures, 1000-frame GC
├── Tier3_CrossFeature/
│   └── Tier3_CrossFeatureCombinationTests.cs# Pairwise cross-feature interactions
└── Tier4_RealWorldScenarios/
    └── Tier4_RealWorldScenarioTests.cs     # 5 realistic application-level scenarios
```

### 3.3 Test Execution Command
Run the complete test suite from the repository root:
```bash
dotnet test windows/SeaGlass.sln
```
Or target the test project directly with detailed output:
```bash
dotnet test windows/SeaGlass.Tests/SeaGlass.Tests.csproj --verbosity normal
```

---

## 4. Acceptance Verification & Reporting

The E2E test suite generates machine-readable test execution metrics. When all tests pass:
1. `TEST_READY.md` is published at `c:\Users\maxw6\Projects\SeaGlass\TEST_READY.md` with:
   - Total test counts broken down by Tier 1, 2, 3, 4.
   - Exact command line to run the test suite.
   - Comprehensive checklist of all 58 controls and features verified.
2. The orchestrator and QA tracks use `TEST_READY.md` as the definitive milestone gate for Phase 5 (E2E Test Pass & Hardening).
