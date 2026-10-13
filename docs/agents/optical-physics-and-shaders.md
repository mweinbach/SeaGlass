# Optical Physics & Shader Specification

This document defines the mathematical model shared across:
- `SeaGlassShaderSource.AGSL_SEA_GLASS_SHADER` (Android 13+ AGSL)
- `seaglass_native.cpp` (GLSL ES 3.00 & Native C++20 evaluator)
- `SeaGlassColorMath` & `SeaGlassSdfEngine` (Kotlin reference & CPU contour engine)

---

## 1. 2D Signed Distance Fields & Liquid Coalescing (`smin`)

### 1.1 Expressive Rounded Box / Squircle Primitive
Each UI element $i \in \{0 \dots N-1\}$ is represented by a center $\mathbf{c}_i = (x_i, y_i)$, half-extents $\mathbf{b}_i = (w_i/2, h_i/2)$, per-corner radii $\mathbf{r}_i = (r_{TR}, r_{BR}, r_{BL}, r_{TL})$, and corner smoothing exponent $p_i \ge 2$ (where $p = 2$ is a circular arc and $p \in (2.2, 3.2]$ produces a Material 3 Expressive squircle/superellipse):

$$
\mathbf{q} = |\mathbf{p} - \mathbf{c}_i| - \mathbf{b}_i + r
$$

$$
d_{\text{box}}(\mathbf{p}) = \min(\max(q_x, q_y), 0) + \left( \max(q_x, 0)^p + \max(q_y, 0)^p \right)^{1/p} - r
$$

### 1.2 Polynomial Cubic Smooth Minimum (`sminCubic`)
When two or more sea glass elements belong to the same `SeaGlassFluidGroup` with smoothness radius $k > 0$ (in pixels), their distance fields $d_a$ and $d_b$ coalesce via Inigo Quilez's $C^2$-continuous cubic polynomial smooth minimum:

$$
h = \frac{\max(k - |d_a - d_b|, 0)}{k}
$$

$$
\text{smin}(d_a, d_b, k) = \min(d_a, d_b) - \frac{1}{6} h^3 k
$$

The material blend weight $\alpha_{ab} \in [0, 1]$ between two coalescing shapes is:

$$
\alpha_{ab} = \begin{cases}
\frac{1}{2} h^2 & \text{if } d_a < d_b \\
1 - \frac{1}{2} h^2 & \text{otherwise}
\end{cases}
$$

This allows two differently tinted sea glass pills (e.g., `SeafoamMint` and `CobaltTide`) to smoothly blend both their geometry and their volumetric tint across the liquid bridge.

---

## 2. Tumbled Glass Bevel Profile & Surface Normal Reconstruction

Inside the shape ($d < 0$), sea glass is not flat—its edges are tumbled and rounded by ocean surf over a bevel width $w_{\text{bevel}}$ (typically `10dp..24dp` scaled by `thicknessDp`).

We define the normalized edge depth $\tau \in [0, 1]$:

$$
\tau(d) = 1 - \text{smoothstep}(0, w_{\text{bevel}}, -d)
$$

The physical slab thickness profile $H(d)$ (greatest in the interior, tapering smoothly around the tumbled rim) is:

$$
H(d) = d_{\text{thickness}} \cdot \sqrt{1 - \tau(d)^2}
$$

Using the analytical SDF gradient $\mathbf{g} = \nabla d(\mathbf{p}) = (\partial_x d, \partial_y d)$ with $|\mathbf{g}| \approx 1$, the 3D outward surface normal $\mathbf{N}_0$ of the tumbled glass dome is:

$$
\mathbf{N}_0(\mathbf{p}) = \text{normalize}\left( g_x \cdot \tau(d) \cdot s_{\text{bevel}},\; g_y \cdot \tau(d) \cdot s_{\text{bevel}},\; \sqrt{\max(0.05, 1 - \tau(d)^2)} \right)
$$

---

## 3. Sanded Micro-Coarseness (Isotropic Micro-Facet Frost)

Real sea glass is mechanically frosted by sand grains. Rather than a smooth normal $\mathbf{N}_0$, the surface exhibits microscopic pits at spatial scales of $1\text{–}3\text{px}$.

We evaluate a two-octave deterministic interleaved gradient / simplex-hash noise vector $\boldsymbol{\xi}(\mathbf{p}) \in [-1, 1]^2$ that is stationary in the glass element's local coordinate space (so the sand grain moves *with* the glass element rather than swimming in screen space):

$$
\mathbf{N}(\mathbf{p}) = \text{normalize}\left( \mathbf{N}_0(\mathbf{p}) + c_{\text{grit}} \cdot (\xi_x(\mathbf{p}), \xi_y(\mathbf{p}), 0) \right)
$$

where $c_{\text{grit}} = \text{coarseness} \cdot (0.08 + 0.14 \cdot \tau(d))$ increases micro-roughness slightly near the tumbled edge, matching how real sea glass is most heavily frosted along its exposed perimeter.

---

## 4. Softened Edge Refraction & Chromatic Dispersion

Light passing from the background through the sea glass slab is refracted by the perturbed surface normal $\mathbf{N}(\mathbf{p})$ according to Snell's law approximation, attenuated by surface coarseness (since heavily sanded glass scatters light isotropically rather than acting as a sharp clear lens):

$$
k_{\text{refract}} = (\text{ior} - 1.0) \cdot H_{\text{px}} \cdot (1.0 - 0.55 \cdot \text{coarseness})
$$

$$
\Delta \mathbf{u} = -\mathbf{N}_{xy}(\mathbf{p}) \cdot k_{\text{refract}}
$$

With chromatic dispersion $\delta_{\text{disp}} = \text{chromaticDispersion} \cdot 0.18$, the R, G, and B channels sample the frosted backdrop at slightly separated offsets along $\Delta \mathbf{u}$:

$$
\begin{aligned}
C_R &= \text{backdrop}(\mathbf{p} + \Delta \mathbf{u} \cdot (1 + \delta_{\text{disp}}))_R \\
C_G &= \text{backdrop}(\mathbf{p} + \Delta \mathbf{u})_G \\
C_B &= \text{backdrop}(\mathbf{p} + \Delta \mathbf{u} \cdot (1 - \delta_{\text{disp}}))_B
\end{aligned}
$$

---

## 5. Volumetric Beer-Lambert Body Tinting in Oklab Space

Flat alpha blending ($\text{mix}(C_{\text{bg}}, C_{\text{tint}}, \alpha)$) destroys backdrop contrast and looks like muddy paint.

Instead, SeaGlass models **volumetric absorption** in Björn Ottosson's perceptual **Oklab** $(L, a, b)$ space combined with physical Beer-Lambert exponential transmittance:

1. Convert backdrop color $\mathbf{C}_{\text{bg}}$ and sea-glass tint $\mathbf{C}_{\text{tint}}$ to Oklab: $(L_{\text{bg}}, a_{\text{bg}}, b_{\text{bg}})$ and $(L_{\text{tint}}, a_{\text{tint}}, b_{\text{tint}})$.
2. Compute the effective optical path length $\ell(d)$ through the tumbled glass slab:
   $$
   \ell(d) = \text{tintDensity} \cdot (0.65 + 0.35 \cdot \sqrt{1 - \tau(d)^2})
   $$
3. Apply Beer-Lambert spectral absorption coefficient $\boldsymbol{\sigma} = -\ln(\max(\mathbf{C}_{\text{tint,linear}}, 0.04))$ scaled by $\ell(d)$, blended with perceptual Oklab chroma shift $(a_{\text{tint}}, b_{\text{tint}})$ and a sand-frosted subsurface scatter proportional to the sand-erosion continuum (`coarseness` / `effectiveSandWear`):
   - **Untreated Clear Glass (`coarseness = 0, transparency = 1`)**: Zero sand-grain surface treatment and near-zero subsurface diffusion—pure optical refraction and Beer-Lambert transmission.
   - **Sand-Frosted Continuum (`0 < coarseness < 1`)**: Progressive ocean sand abrasion develops an isotropic sand-frosted micro-facet finish while coupling subsurface diffusion across both the interior and the curved bevel so edges and corners never exhibit a harsh unblurred seam.
   - **Effectively Opaque Sand-Frosted Glass (`coarseness = 1, transparency = 0`)**: Deep sand-frosted subsurface diffusion and Oklab body scatter render the slab effectively opaque to sharp background details while still refracting and diffusing ambient light and maintaining uniform chroma (never bleaching the center toward white with depth).

---

## 6. Chalky Oren-Nayar / Rough-Fresnel Rim & Inner Bevel Lighting

Given a normalized directional light vector $\mathbf{L} = \text{normalize}(-0.55, -0.72, 0.42)$ (top-left key light):

1. **Rough-Fresnel Rim Glow**:
   $$
   F_{\text{rim}} = \tau(d)^{1.6} \cdot (0.35 + 0.65 \cdot \text{rimChalkiness})
   $$
2. **Directional Bevel Specular-Diffuse Highlight (Windward Edge)**:
   $$
   I_{\text{key}} = \max(0, \text{dot}(\mathbf{N}_{xy}, \mathbf{L}_{xy})) \cdot \tau(d)
   $$
3. **Internal Refracted Rim Caustic (Leeward Edge)**:
   Light entering the top-left bevel refracts through the slab and illuminates the bottom-right inner rim with a tint-saturated glow:
   $$
   I_{\text{caustic}} = \max(0, \text{dot}(\mathbf{N}_{xy}, -\mathbf{L}_{xy})) \cdot \tau(d) \cdot (1 - \tau(d)) \cdot 2.2
   $$
4. **Sanded Micro-Grain Sparkle**:
   A subtle chalky micro-facet modulation proportional to $\text{coarseness} \cdot \xi(\mathbf{p})$ gives the surface the unmistakable tactile feel of dry, ocean-tumbled sea glass.

---

## 7. Content Roles: Quiet Chrome vs. Optical Lenses (Compose)

`Modifier.seaGlass(contentRole = ...)` picks how much relief a surface carries.

- **`PURE_OPTICAL_LENS`** draws the full tumbled treatment: pillow bevel, frosted rind, pinpoint pitting (`SeaGlassSandGrainTexture`, `OVERLAY`), bevel stipple, the lower caustic pool, a blurred grounding shadow plus caustic shadow, and the AGSL intaglio etch on its content.
- **`TEXT_AND_ICONS` and `NAVIGATION_CHROME`** are *quiet chrome* for dense UI (bars, sheets, menus, cards). They keep a gentle Beer-Lambert `Modulate` absorption (dark glass keeps the full filter), the tinted body scatter, a soft `SrcOver` velvet grain (`SeaGlassVelvetGrainTexture`), a faint unblurred two-step bevel, an unblurred edge lip and a tight 1.5 dp contact shadow. They skip the rind, stipple, caustic pool and blur mask filters, which read as milky and gritty on text-heavy surfaces and cost blurred strokes every frame.
- Quiet chrome etches content only when the glass is dark (Oklab L < 0.48), where the ink is light, at 70% strength. Light glass skips the etch pass and does not build its `RuntimeShader`.
- Floating sheets draw their content beside the glass rather than inside it, so `SeaGlassFloatingSheet` applies the same etch to the content layer through `seaGlassEtchedSiblingContent` as a layer `RenderEffect`.
- `SeaGlassSheetGroup` uses a quiet `contentColor` fill (6% light, 10% dark) and a hairline border; dark detection uses the content color's luminance. `SeaGlassSwitch` and `SeaGlassSheetToggleSection` default to the palette's `surfaceGlass` when off.
