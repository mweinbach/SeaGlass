# Material 3 Expressive Tokens & SeaGlass Design System

SeaGlass aligns its color tokens, shape morphing, and motion physics with **Material 3 Expressive**.

---

## 1. Color System: Sea Glass Pastels × M3 Expressive Tonal Palettes

Material 3 Expressive emphasizes higher-chroma, emotionally resonant pastel tones paired with high-contrast foreground typography. SeaGlass provides 8 signature presets (`SeaGlassPalette`) and a dynamic builder (`SeaGlassPalette.fromColorScheme(colorScheme)`) that derives sea-glass optical materials from any Material 3 `ColorScheme`:

| Token Role | SeaGlass Optical Mapping |
| :--- | :--- |
| `primaryGlass` | High-chroma pastel tint (`tintDensity = 0.56f`, `coarseness = 0.38f`) for primary FABs, active navigation pills, and hero chips. |
| `secondaryGlass` | Harmonized tonal companion (`tintDensity = 0.46f`, `coarseness = 0.44f`) for secondary actions and filter chips. |
| `tertiaryGlass` | Complementary accent sea glass (`tintDensity = 0.52f`, `coarseness = 0.36f`) for badges and expressive callouts. |
| `surfaceGlass` | Neutral-tinted milk/coastal glass (`tintDensity = 0.34f`, `coarseness = 0.50f`) for cards, floating bars, and sheets. |
| `onGlassContentColor` | Perceptually contrast-checked foreground color computed in Oklab ($|\Delta L| \ge 0.48$) so icons and labels remain effortless to read over any backdrop. |

---

## 2. Expressive Geometry & State Morphing

Material 3 Expressive moves away from static uniform rounded rectangles toward expressive shapes that morph on interaction:

- **Corner Smoothing (`cornerSmoothing`)**: Superellipse exponent $p \in [2.0, 3.2]$ that eliminates the harsh curvature discontinuity where a straight edge meets a circular arc.
- **State-Driven Corner Morphing**:
  - **Rest State**: Pill (`cornerRadiusFraction = 0.5f`) or soft squircle (`28.dp`).
  - **Pressed / Dragged State**: Corners sharpen or asymmetrically squish (`cornerRadiusDp` animates via spatial spring while scale stretches along the gesture axis) and effective glass `thicknessDp` compresses, subtly deepening the Beer-Lambert tint and brightening the chalky rim.
  - **Expanded / Active State**: Smoothly morphs from a compact droplet or pill into a full card or toolbar while coalescing with adjacent elements via `SeaGlassFluidGroup`.

---

## 3. Dual-Spring Motion System (`SeaGlassMotionScheme`)

Following Material 3 Expressive's separation of **Spatial** and **Effects** springs:

| Spring Token | Damping Ratio ($\zeta$) | Stiffness ($k$) | Used For |
| :--- | :--- | :--- | :--- |
| `fastSpatial` | `0.64f` (Under-damped bouncy) | `680f` | Button press squish, pill selection bounce, chip toggle morph |
| `defaultSpatial` | `0.68f` (Expressive organic) | `420f` | `SeaGlassFluidBar` indicator travel, SDF coalescing bridge separation/merge |
| `slowSpatial` | `0.74f` (Gentle tidal flow) | `240f` | Card/sheet expansion, hero container transitions |
| `fastEffects` | `1.00f` (Critically damped) | `1100f` | Rim glow intensity, press highlight, micro-coarseness modulation |
| `defaultEffects` | `1.00f` (Critically damped) | `550f` | Oklab tint color transitions, frost blur radius, tint density |

**Rule**: Never animate `tintColor`, `frostRadiusDp`, or `coarseness` with an under-damped (`dampingRatio < 1.0f`) spring. Spatial geometry bounces; optical properties flow smoothly without overshoot.
