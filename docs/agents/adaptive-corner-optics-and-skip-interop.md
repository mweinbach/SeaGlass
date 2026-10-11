# Adaptive Device & Page Corner Optics and Swift / Skip Interop

This blueprint specifies the mathematical invariants, hardware detection cascade, collision-free layout solver, and Swift / Skip (`skip.tools`) integration architecture for SeaGlass's **Adaptive Device & Page Corner Optics**.

---

## 1. Why Static Corner Radii Fail Near Device & Page Corners

When a translucent glass element with a fixed corner radius $r_{\text{fixed}}$ is placed near a physical device bezel (or inside a rounded page/sheet container) of corner radius $R_{\text{outer}}$ at inset $(d_x, d_y)$, three visual and geometric failures occur unless the element adapts per-corner:

1. **Non-Concentric Gap Pinching**:
   If $r_{\text{fixed}} \neq R_{\text{outer}} - d$, the center of curvature of the element's corner does not coincide with the center of curvature of the bezel. The margin between the element and the screen edge visibly pinches or flares at $45^\circ$.
2. **Diagonal Bezel Protrusion & Clipping (`r_elem < R_outer - d`)**:
   An outer bezel corner of radius $R_{\text{outer}}$ cuts inward along the $45^\circ$ diagonal by:
   $$\Delta_{\text{bezel}} = R_{\text{outer}}\!\left(1 - \frac{\sqrt{2}}{2}\right) \approx 0.2929\,R_{\text{outer}}$$
   An inner element with corner radius $r_{\text{elem}}$ at inset $d$ has its $45^\circ$ arc vertex at $d + 0.2929\,r_{\text{elem}}$. If $r_{\text{elem}}$ is artificially clamped to $0.5\,H_{\text{elem}}$ (for example, $34\text{dp}$ on a $68\text{dp}$ pod) inside a $55\text{dp}$ bezel at $d = 3\text{dp}$, then:
   $$3 + 0.2929(34) = 12.96\text{dp} < 0.2929(55) = 16.11\text{dp}$$
   causing the element's corner to **protrude past and overlap the outer bezel arc** by $3.15\text{dp}$!
3. **Foreground Content vs. Curved Meniscus Collision**:
   When a corner's radius increases to match a deep bezel ($44\text{dp}\text{–}55\text{dp}$), the curved $7\text{dp}$ meniscus lip sweeps inward across the corner. Static rectangular padding allows text baselines or icons to collide with the curved bevel.

---

## 2. 4-Tier Hardware Display Corner Detection Cascade

Implemented in [`SeaGlassCornerAdaptationSolver.detectDeviceCorners`](../../android/seaglass-core/src/main/kotlin/dev/seaglass/core/SeaGlassDeviceCornerOptics.kt):

| Tier | Source | API / Mechanism | Notes |
| :--- | :--- | :--- | :--- |
| **Tier 1** | `WINDOW_INSETS_LIVE` | `WindowInsets.getRoundedCorner(POSITION_TOP_LEFT..BOTTOM_LEFT)` (API 31+) | Queries the live window's attached `RoundedCorner`s in physical pixels. Automatically reflects rotation, multi-window split-screen, and foldable posture. |
| **Tier 2** | `DISPLAY_HARDWARE_API` | `Display.getRoundedCorner(position)` (API 31+) | Direct hardware display query used before `rootWindowInsets` is attached or when window insets report `0`. |
| **Tier 3** | `SYSTEM_RESOURCE_DIMEN` | `Resources.getSystem()` (`rounded_corner_radius`, `rounded_corner_radius_top`, `rounded_corner_radius_bottom`, `rounded_corner_radius_adjustment`) | Reads the exact internal AOSP SystemUI dimension resources used to draw the hardware screen mask on API 26–37+. |
| **Tier 4** | `DEVICE_MODEL_CATALOG` / `PROFILE_OVERRIDE` | `Build.MODEL` / `Build.DEVICE` lookup table + `SeaGlassDeviceHardwareProfile` | Provides calibrated per-corner radii (`Pixel 9/10 Pro` `46dp`, `Pixel 8 Pro` `38dp`, `Galaxy S25 Ultra` `18dp`, `Galaxy S25+` `38dp`, `Pixel Fold Cover` asymmetric `14dp` hinge / `44dp` outer bezel, `iPhone 16/17 Pro` reference `55dp`, tablets `28dp`) when running on emulators (`mRoundedCorners = 0` in `dumpsys window`). |

---

## 3. Mathematical Model for Per-Corner Concentric & Asymmetric Adaptation

### 3.1 Developer Control: Where, When & How Close (`SeaGlassCornerAdaptationConfig` & `SeaGlassCornerMask`)
Developers control corner adaptation at the theme level (`LocalSeaGlassCornerAdaptationConfig`) or per-component (`Modifier.seaGlass`, `SeaGlassSurface`, `SeaGlassCard`, `SeaGlassCornerEmbeddedPod`, `SeaGlassSkipBridge.Surface`):
- **When (`mode: SeaGlassCornerAdaptationMode`)**: `FIXED` (disabled, default for buttons, pills, chips, and inner controls), `CONCENTRIC_PROXIMITY`, `FLUSH_CORNER_EMBED`, or `UNIFORM_CONCENTRIC`.
- **Where (`corners: SeaGlassCornerMask` & `allowAsymmetricCorners: Boolean`)**: Enable adaptation on `ALL`, `NONE`, `TOP_ONLY`, `BOTTOM_ONLY`, `LEFT_ONLY`, `RIGHT_ONLY`, or any custom combination of `(topLeft, topRight, bottomRight, bottomLeft)`. By default, `allowAsymmetricCorners = false` on general surfaces so floating cards and multi-line inputs never warp into lopsided shapes, and `adaptToParentContainer = false` so child buttons/pills inside a card retain their own geometry unless opted in.
- **How Close (`proximityThresholdDp` & `fullMatchDistanceDp`)**: Corners outside `proximityThresholdDp` (default `32dp`) stay strictly at `baseCornerRadiusPx`. Within `[fullMatchDistanceDp .. proximityThresholdDp]`, the corner transitions smoothly via a $C^1$ cubic smoothstep, and in `CONCENTRIC_PROXIMITY` mode a corner approaching a larger bezel never shrinks below `baseCornerRadiusPx` at intermediate distances.

### 3.2 Per-Corner Concentric Target & Proximity Field
For each enabled corner $c \in \{\text{TL}, \text{TR}, \text{BR}, \text{BL}\}$ of an element with inset $(d_x, d_y)$ from the corresponding corner of an enclosing container (the physical device screen or an opted-in parent `LocalSeaGlassContainerCornerScope` page/card):

1. **Radial Corner Distance & $C^1$ Smoothstep Proximity Weight**:
   $$d_c = \sqrt{d_x^2 + d_y^2}, \qquad t_c = \text{clamp}\!\left(\frac{D_{\text{threshold}} - d_c}{D_{\text{threshold}} - D_{\text{fullMatch}}},\; 0,\; 1\right), \qquad \alpha_c = t_c^2(3 - 2t_c)$$
2. **Harmonic Concentric Offset**:
   $$d_{\text{eff}} = 0.65\min(d_x, d_y) + 0.35\max(d_x, d_y), \qquad R_{\text{concentric}, c} = \max\!\big(r_{\text{floor}},\; R_{\text{outer}, c} - d_{\text{eff}}\big)$$
3. **Asymmetric Edge-Sum Normalization (Avoiding the $0.5\,H$ Trap)**:
   In a rounded rectangle of width $W$ and height $H$, adjacent corners share an edge. Rather than clamping every corner independently to $0.5\min(W, H)$, `SeaGlassCornerAdaptationSolver` and `SeaGlassContourPathBuilder.buildSingleNodePath` allow an outer bezel-facing corner to use up to $0.78\min(W, H)$ when its opposite inner corner on that edge is small ($r_{\text{inner}} \approx 12\text{dp}\text{–}16\text{dp}$), normalizing only by the exact edge-sum constraints:
   $$r_{\text{TL}} + r_{\text{TR}} \le W, \quad r_{\text{BL}} + r_{\text{BR}} \le W, \quad r_{\text{TL}} + r_{\text{BL}} \le H, \quad r_{\text{TR}} + r_{\text{BR}} \le H$$
   This allows a $76\text{dp}$-tall corner pod with a $14\text{dp}$ inner corner to achieve a full **$52\text{dp}$ outer corner radius** ($52 + 14 = 66\text{dp} \le 76\text{dp}$) inside a $55\text{dp}$ bezel at $3\text{dp}$ inset with zero bezel protrusion.

### 3.2 Dynamic Collision-Free Corner Pod Layout (`solveCornerPodLayoutPx`)
When embedding pods or docks in the corners of a bezel or page container of size $(W_{\text{frame}}, H_{\text{frame}})$, `SeaGlassCornerAdaptationSolver.solveCornerPodLayoutPx` enforces two hard invariants:
1. **Diagonal Bezel Clearance**:
   $$d_{\text{safe}} = \max\!\Big(d_{\text{requested}},\; 0.30\,\big(R_{\text{bezel, max}} - r_{\text{pod, max}}\big)\Big)$$
2. **Non-Overlapping Pod Bounds**:
   $$W_{\text{pod}} = \min\!\left(W_{\text{pref}},\; \frac{W_{\text{frame}} - 2d_{\text{safe}} - g_{x,\min}}{2}\right), \qquad H_{\text{top}} + H_{\text{bottom}} \le H_{\text{frame}} - 2d_{\text{safe}} - g_{y,\min}$$

### 3.3 Inscribed Corner-Safe Content Padding (`computeCornerSafePaddingPx`)
For any element using `FLUSH_CORNER_EMBED`, `SeaGlassSurface` automatically expands content padding along any deeply curved corner edge:
$$p_{\text{left}} = \max(p_{\text{base, left}},\; 0.34\max(r_{\text{TL}}, r_{\text{BL}})), \qquad p_{\text{top}} = \max(p_{\text{base, top}},\; 0.22\max(r_{\text{TL}}, r_{\text{TR}}))$$

---

## 4. Swift & Skip (`skip.tools`) Interoperability Guide

SeaGlass is designed to work out of the box in cross-platform **Swift / SwiftUI apps targeting Android via Skip** (both **Skip Fuse** compiled Swift and **Skip Lite** transpiled Swift).

### 4.1 Architecture for Skip Apps
- **Ambient GPU Stage (`LocalSeaGlassStageState`)**:
  In SwiftUI, views are value types composed deeply across multiple structs. Passing a Kotlin `SeaGlassStageState` object down through every SwiftUI view is unnecessary: wrapping the root view in `SeaGlassSkipBridge.StageRoot` provides `LocalSeaGlassStageState` automatically to all descendant `Modifier.seaGlass()`, `SeaGlassSurface`, `SeaGlassCard`, `SeaGlassCornerEmbeddedPod`, and `SeaGlassSkipBridge.*` calls.
- **Primitive-Typed Bridge (`SeaGlassSkipBridge`)**:
  All methods on `dev.seaglass.compose.SeaGlassSkipBridge` use primitive `String`, `Float`, `Int`, and `Boolean` parameters (avoiding Kotlin `@JvmInline value class` types like `Dp` and `Color`) so `#if SKIP` blocks can invoke them directly.

### 4.2 Usage in a Swift / Skip Project (`#if SKIP`)

Drop [`docs/skip/SeaGlassSkipUI.swift`](../skip/SeaGlassSkipUI.swift) into your Skip module and wrap your screen or components:

```swift
import SwiftUI
#if SKIP
import dev.seaglass.compose.SeaGlassSkipBridge
#endif

public struct CoastalPlayerRootView: View {
    public init() {}

    public var body: some View {
        #if SKIP
        ComposeView { ctx in
            SeaGlassSkipBridge.StageRoot(
                paletteId: "seafoam_mint",
                deviceCornerProfileId: "auto",
                backgroundContent: {
                    // Rendered once per frame into the GPU backdrop blur + meniscus pyramid
                    CoastalArtworkBackground().Compose(ctx)
                },
                foregroundContent: {
                    CoastalForegroundOverlay().Compose(ctx)
                }
            )
        }
        #else
        ZStack {
            CoastalArtworkBackground()
            CoastalForegroundOverlay()
        }
        #endif
    }
}
```
