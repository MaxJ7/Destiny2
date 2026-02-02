# NoxusBoss Effect Analysis & Destiny2 Alignment

## What NoxusBoss Does (from code inspection)

### 1. ShockwaveShader (DarkWave, LightWave, RodOfHarmonyExplosion)
**Technique**: Full-screen draw with distance-based ring

- **Draw**: `new DrawData(DendriticNoise, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White)`
- **Space**: Screen-space. UV coords → `(coords * screenSize - projectilePosition)` = pixel distance from center
- **Ring**: `signedDistanceFromExplosion = (offsetFromProjectile - explosionDistance) / screenSize.x`
- **Effect**: `color += shockwaveColor * 0.041 * opacity / distanceFromExplosion`
- **Expansion**: `explosionDistance = Radius * Projectile.scale * 0.5f` grows over time → ring moves outward

**Result**: A crisp, thin expanding ring that grows from the center. The ring is always sharp because it's computed in screen space.

### 2. FireExplosionShader (ExplodingStar)
**Technique**: Object-space quad, polar coords, noise for irregular edge

- **Draw**: `InvisiblePixel` scaled to `Projectile.width * Projectile.scale * 1.3f`
- **Noise**: FireNoiseA (s1), PerlinNoise (s2) — custom NoxusBoss PNGs
- **Edge irregularity**: `distanceFromCenter += tex2D(edgeAccentTexture, polar + time*offset) * explosionShapeIrregularity`
- **Expansion**: `fadeFromWithin = smoothstep(-0.15, 0, distanceFromCenter - pow(lifetimeRatio, 2.5) * 0.65)` — grows from center
- **accentColor**: `(0.8, -0.85, -1, 0)` — can produce subtractive/weird color effects

**Result**: Expanding fire ball with irregular flame-tongue edges.

### 3. MassiveElectricShockwaveShader
**Technique**: Object-space quad, polar coords, ring at fixed edge with noise-jagged boundary

- **Ring position**: `edge = 0.4` (normalized 0–1)
- **Jagged edge**: `distanceNoise = tex2D(electricNoiseTexture, polar * ...)` → `distanceFromEdge = distance(distanceFromCenter + distanceNoise * 0.02, edge)`
- **Glow**: `glowIntensityCoefficient / pow(distanceFromEdge, exponent)` — sharp bright ring
- **Result**: Expanding circle with noisy/jagged ring boundary.

### 4. FreezingWave
**Technique**: PrimitiveRenderer.RenderQuad with noise texture

- **Texture**: DendriticNoiseZoomedOut
- **Shader**: FreezingWaveShader (BurnNoise, WavyBlotchNoise)
- **Size**: `diameter = Radius * 2` — world-space expanding quad

---

## What Destiny2 Currently Does (Gaps)

### Kinetic Tremors
- World-space: PrimitiveRenderer circles + shader quad
- Ring is drawn as primitives/shader in world space
- **Missing**: NoxusBoss ShockwaveShader-style screen-space expanding ring — the "true" expanding wave

### Incandescent
- Object-space FireExplosionShader-style quad
- Has noise-jagged edge, inner glow, accent
- **Possibly missing**: Clear "wave then flames inside" layering — user wanted "expanding wave" first, then "jagged edges or flames inside"

---

## Recommended Changes

### Kinetic Tremors: Use ShockwaveShader Pattern
1. Add a shader that draws a full-screen (or large) quad with DendriticNoise
2. Compute distance from center in screen pixels
3. Ring at `explosionDistance` that grows: `(offsetFromProjectile - explosionDistance)`
4. Produces a crisp expanding wave — physical shockwave, not magical circles

**Caveat**: Full-screen draw affects everything. NoxusBoss uses it for boss-scale shockwaves. For 3-tile radius we may need to either:
- Use full screen (ring is small, rest is dark/transparent), or
- Draw a large world-space quad (e.g. 2× screen) and use the same distance-from-center math in UV space

### Incandescent: Emphasize Wave + Jagged Flames
1. **Wave first**: A clear expanding boundary (like fadeFromWithin but more pronounced)
2. **Inside**: Jagged flame edges — noise modulates the boundary, creating flame tongues
3. Simplify: Less inner glow/ember noise, stronger leading edge
4. Ensure expansion reads as "wave" then "chaos inside"

---

## Destiny 2 Visual Reference (from user prompts)

- **Solar**: "solar flare erupting", "expanding and explosive fire", "wave of flames expanding outwards", "fiery"
- **Incandescent**: "expanding wave", "jagged edges or flames inside"
- **Kinetic Tremors**: "expanding thin circle", "violent shockwave", "sonic boom tearing the air apart", "jagged edges"
