---
trigger: model_decision
description: When developing graphics
---

# Destiny 2 Mod: Grpahics Engine Architecture
> **Purpose:** The definitive manual for implementing "AAA" quality Visual Effects (VFX) in this mod. This guide abstracts specific implementation details into architectural patterns.

## 1. The "Uber-Shader" Philosophy
To maintain high performance and developer velocity, we avoid writing unique shaders for every new item. Instead, we use two flexible **Master Shaders** located in `Effects/` (not Assets).

### The "High-Pressure Energy Stream" Pattern (Noise Logic)
The gold standard for energy effects in this mod. Designed to maximize chaos while staying under PS 2.0/3.0 instruction limits.
*   **Prime-Based Scroll Crossing:** Use prime numbers for scroll speeds (e.g., `4.71`, `2.33`) to prevent repeating interference patterns.
*   **Multi-Axis Jitter (Scrossing):** Noise-distort both X and Y axes of UVs to eliminate "conveyor belt" scrolling.
*   **Procedural Thinning:** Use high-order power functions (e.g., `pow(y, 10)`) in the shader to concentrate wide geometry into a needle-thin core.
*   **Physical Breaks (Clip Logic):** Use `clip()` based on a noise threshold to create jagged, high-pressure gaps in the energy stream.

### A. The "Linear Tech" Shader (Beams & Trails)
Used for anything that flows along a path: Laser beams, sword slashes, flowing rivers of fire.

**The 4-Texture Input Model:**
The magic comes from combining four distinct texture channels in the shader:
1.  **Shape Map (Base):** The solid core of the beam. (e.g., A white line for a laser, a jagged bolt for lightning).
2.  **Detail Map (Noise):** High-frequency noise that adds texture. (e.g., Sparks, grain).
3.  **Erosion Map (Alpha):** A texture used to "eat away" at the beam's edges or create gaps.
4.  **Flow Map (Distortion):** A scrolling noise texture that warps the UVs of the other maps to create fluid movement.

**Colorization:**
Never bake color into the textures. Use Grayscale textures and pass a **4-Stop Gradient** (Color1 -> Color2 -> Color3 -> Color4) to the shader. This allows the same "Fire" texture set to be used for "Void Fire" just by changing the gradient coordinates.

### B. The "Radial Tech" Shader (Orbs & Projectiles)
Used for self-contained objects: Nova Bombs, Muzzle Flashes, Shockwaves.

**The Composition:**
1.  **Mask (Caustic):** The static shape of the object (e.g., a ring, a star, a skull).
2.  **Distortion:** A scrolling noise texture that pushes the pixels of the Mask.
    *   *Low Distortion:* Gently pulsating orb.
    *   *High Distortion:* Raging fire ball.
3.  **Gradient Map:** Maps the grayscale intensity of the Mask to a color palette.

---

## 2. The Render Pipeline
Standard `SpriteBatch.Draw` is insufficient for "Glowing" or "Energy" effects. We utilize a split-render pipeline.

### Layer 1: Standard World (Alpha Blend)
*   **What:** Physical objects, solid projectiles, debris.
*   **Method:** Standard `PreDraw`.
*   **Look:** Flat, opaque, reacts to lighting.

### Layer 2: The Bloom Buffer (Additive)
*   **What:** Energy beams, fire, plasma, magic.
*   **Method:** Queue draw calls to a separate `RenderTarget`.
*   **The Trick:**
    1.  Create a low-resolution RenderTarget (e.g., 50% screen size).
    2.  Queue all "glowing" sprites to be drawn here.
    3.  Draw using `BlendState.Additive`.
    4.  Draw the RenderTarget back to the main screen.
    *   *Result:* The upscaling from 50% to 100% creates a free, high-quality blur/bloom effect without expensive Gaussian shaders.

---

## 3. Particle System Architecture
Understanding the hierarchy of "small moving things" is critical for performance.

### Tier 1: Vanilla Dust
*   **Use Case:** Large quantities (100+), simple physics, short life. Impact debris, bullet sparks.
*   **Implementation:** Use `Dust.NewDust`. Use `customData` to attach lightweight behavior scripts if needed (e.g., simple scaling).
*   **Limit:** Cannot use custom shaders efficiently.

### Tier 2: The "Flipbook" Particle
*   **Use Case:** Realistic complex motion (Smoke, Explosions, Liquid Splashes).
*   **Implementation:** Pre-rendered sprite sheets (e.g., 8x8 grid of a smoke puff animation).
*   **Rendering:** Draw twice for volumetric effect:
    *   *Pass 1:* Darker/Opaque body.
    *   *Pass 2:* Lighter/Additive highlight (slightly offset or scaled).

### Tier 3: The Shader Particle
*   **Use Case:** Hero effects. Embers that wiggle, runes that pulse.
*   **Implementation:** Custom managed class (not `Dust`).
*   **Features:**
    *   Full Access to `Radial Tech` shaders.
    *   Custom update logic (Sine wave motion, homing).
    *   **Lifetime Erosion:** Use `Alpha` (0..1) to drive the shader's `Threshold` parameter. As the particle fades, it physically "burns away" instead of just becoming transparent.
    *   Heavy performance cost; keep counts low (<50).

---

## 5. Advanced Rendering Patterns (The "Juice")

### A. The Triple-Pass Technique
For high-importance projectiles (e.g., Boss attacks, Snipe Shots), draw the sprite **three times**:
1.  **The Shadow (Pass 1):** Draw slightly larger, black, low opacity. Creates contrast against bright backgrounds.
2.  **The Body (Pass 2):** Draw normal color, normal size. The "physical" object.
3.  **The Bloom (Pass 3):** Draw slightly smaller, white/bright color, `Additive` blending. Creates the "hot core" look.

### B. Sine-Wave Animation
Don't let sprites sit still.
*   **Breathing:** `scale = baseScale + sin(Main.time * speed) * amount`.
*   **Wobble:** `rotation = velocity.ToRotation() + sin(Main.time * speed) * amount`.
*   **Flash:** `color = Color.Lerp(Color.White, Color.Red, sin(Main.time))`.

---

## 4. Technique Cookbook (Destiny 2 Styles)

### Recipe: Solar Energy (e.g., 1000 Voices)
*   **Concept:** "Superheated Fluid Flow"
*   **Shader:** Linear Tech.
*   **Textures:**
    *   Flow: Vertical upward scrolling Perlin noise.
    *   Detail: High-contrast "cracks".
*   **Gradient:** White (Core) -> Yellow -> Deep Orange -> Red (Edge).
*   **Feel:** Fast flow speed, heavy distortion on edges to simulate heat haze.
*   **Optimization:** Uses the **High-Pressure Energy Stream** pattern for the "Boiling Needle" look.

### Recipe: Void Energy (e.g., Graviton Lance)
*   **Concept:** "Negative Space & Implosion"
*   **Shader:** Radial Tech (Subtractive).
*   **Textures:**
    *   Mask: Cellular/Voronoi noise (sharply defined cells).
    *   Distortion: Inward scrolling spiral.
*   **Gradient:** White -> Magenta -> Purple -> **Black**.
*   **Feel:** Use Subtractive blending to make the center look "darker" than the background universe.

### Recipe: Arc Energy (e.g., Riskrunner)
*   **Concept:** "Chaotic & Instant"
*   **Shader:** Linear Tech (Instanced).
*   **Textures:**
    *   Shape: Jagged Bolt.
    *   Distortion: "Jitter" noise (random offset every frame).
*   **Gradient:** White (90%) -> Cyan (10%).
*   **Feel:** Arc is mostly bright white bloom. The blue is only on the very fringe. Movement must be jerky, not smooth.

### Recipe: Stasis (e.g., Ager's Scepter)
*   **Concept:** "Perfect Crystalline Order"
*   **Shader:** Radial Tech.
*   **Textures:**
    *   Mask: Sharp geometric polygons.
    *   Distortion: None (Static).
*   **Gradient:** Deep Blue -> Cyan -> White.
*   **Feel:** High "Shininess" (specular output). Particles should shatter/fade instantly, not drift like smoke.
