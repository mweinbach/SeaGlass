# SeaGlass for Windows (`SeaGlass.Core` & `SeaGlass.WinUI`)

**SeaGlass for Windows** is the 1:1 Windows / WinUI companion to the Android implementation in [`../android/`](../android/), bringing **sanded ocean sea-glass optics**, **perceptual Oklab Beer-Lambert body tinting**, **2D SDF liquid coalescing (`sminCubic`)**, **Material 3 Expressive 20-shape polygon morphing**, **adaptive bezel/page corner optics**, and **dual-spring motion physics** to Windows applications.

See [`../docs/agents/winui-architecture-and-components.md`](../docs/agents/winui-architecture-and-components.md) for the complete architectural blueprint.

---

## Solution Structure (`windows/SeaGlass.sln`)

| Project | Target | Purpose |
| :--- | :--- | :--- |
| **`SeaGlass.Core`** | `net10.0` | Backend-agnostic optical physics (`SeaGlassMaterial`), Oklab & Beer-Lambert volumetric color math (`SeaGlassColorMath`), 8 signature M3 Expressive palettes (`SeaGlassPalette`), 2D SDF `sminCubic` coalescing engine (`SeaGlassSdfEngine`), analytical stadium-spine & Catmull-Rom contour builder (`SeaGlassContourPathBuilder`), 20-shape expressive polygon morph engine (`SeaGlassShapeMorph`), 4-tier adaptive device/window corner solver (`SeaGlassCornerAdaptationSolver`), byte-exact 256-byte `cbuffer`/`std140` uniform buffer (`SeaGlassUniformBlock` + `include/seaglass_backend_abi.h`), and multi-dialect GPU shader compiler (`SeaGlassShaderCompiler`: `HLSL_SM50`, `D2D1_HLSL`, `VULKAN_GLSL_450`, `GLSL_ES_300`, `AGSL_100`). |
| **`SeaGlass.WinUI`** | `net10.0-windows` | WinUI / Windows Desktop UI Kit & Composition foundation: `SeaGlassTheme`, `SeaGlassMotionScheme` (Spatial vs. Effects damped harmonic springs), `SeaGlassStage` (acyclic backdrop capture), `SeaGlassFluidGroup` (4-node SDF coalescing container), `SeaGlassTiltState`, and the complete 30+ component catalog (`SeaGlassSurface`, `SeaGlassCard`, `SeaGlassButton`, `SeaGlassIconButton`, `SeaGlassExtendedFab`, `SeaGlassPill`, `SeaGlassFilterChip`, `SeaGlassBadge`, `SeaGlassTopAppBar`, `SeaGlassFluidBar`, `SeaGlassMiniPlayerBar`, `SeaGlassFloatingToolbar`, `SeaGlassSearchBar`, `SeaGlassTextField`, `SeaGlassSwitch`, `SeaGlassCheckbox`, `SeaGlassRadioButton`, `SeaGlassSegmentedButtonRow`, `SeaGlassLiquidSlider`, `SeaGlassLinearProgressIndicator`, `SeaGlassCircularProgressIndicator`, `SeaGlassListItem`, `SeaGlassSnackbar`, `SeaGlassDialogCard`, `SeaGlassBottomSheetSurface`, `SeaGlassIcon` / `SeaGlassGlyph`, `SeaGlassCornerEmbeddedPod`, `SeaGlassTwoElementMorph`, `SeaGlassBuddingCluster`, `SeaGlassMorphingButton`, `SeaGlassChoreographedShapeOrb`, `SeaGlassMultiShapeMorph`, `SeaGlassMorphingContextMenu`, `SeaGlassCascadedModelMorph`, `SeaGlassContextRingMorph`). |
| **`SeaGlass.Sample`** | `net10.0-windows` | Interactive **SeaGlass Optical Lab & Expressive Showcase** Windows desktop application with live optical parameter tuning, palette morphing, draggable SDF liquid coalescing, 20-shape choreography, adaptive corner optics lab, and full UI Kit gallery. |
| **`SeaGlass.Tests`** | `net10.0-windows` | Comprehensive xUnit verification suite testing Oklab round-trips, Beer-Lambert transmittance, 256-byte `cbuffer`/`std140` layout parity, SDF `sminCubic` coalescing, 20-shape morphing with elastic spring overshoot, adaptive corner optics, HLSL/GLSL shader compilation, and WinUI component state machines. |

---

## Quickstart (WinUI / C#)

```csharp
using SeaGlass.Core;
using SeaGlass.WinUI;

// 1. Configure a SeaGlass theme with any of the 8 signature palettes
var theme = new SeaGlassTheme(SeaGlassPalette.SeafoamMint);
var stage = new SeaGlassStageState();

// 2. Create adaptive SeaGlass components with 1:1 Android parity
var fluidBar = new SeaGlassFluidBar(
    items: ["Tide", "Shells", "Optics", "Palette"],
    selectedIndex: 0,
    stageState: stage,
    barMaterial: SeaGlassMaterial.SeafoamMint(
        coarseness: 0.42f,
        frostRadiusDp: 24f,
        tintDensity: 0.58f
    ),
    indicatorMaterial: SeaGlassPalette.SeafoamMint.PrimaryGlass,
    showActionOrb: true
);
```

---

## Build & Test Commands

```bash
# Run all unit tests
dotnet test windows/SeaGlass.sln

# Build all libraries and the desktop showcase app
dotnet build windows/SeaGlass.sln

# Launch the interactive Windows Optical Lab & Showcase app
dotnet run --project windows/SeaGlass.Sample/SeaGlass.Sample.csproj
```
