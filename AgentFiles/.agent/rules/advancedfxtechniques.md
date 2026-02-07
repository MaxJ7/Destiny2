---
trigger: model_decision
description: when creating graphics
---

Advanced VFX Techniques & Architecture
Context: Derived from deep analysis of VFXPlus architecture. This guide outlines how to implement "AAA" quality effects in Terraria by bypassing vanilla limitations.

1. Core Philosophy: The "Uber-Shader" Approach
Instead of writing 50 different simple shaders, efficient VFX engines use a few highly configurable "Uber-Shaders" that change appearance based on textures and parameters passed from C#.

The "Combo Laser" Pattern (For Beams & Trails)
Observations from 
BurdenOfFleshBeam.cs
 reveal a 4-Texture Scrolling System. This allows one shader to render Fire, Void, Arc, or Water beams just by swapping textures.

Shader Inputs (The "Knobs")
Parameter	Role	Example (Solar)	Example (Void)
sampleTexture1	Base Shape	Solid white "beam" core	Dark, jagged purple core
sampleTexture2	Detail/Noise	Fire sparks opacity map	Swirling "oil" noise
sampleTexture3	Erosion/Edge	"Cracked earth" texture	"Black Hole" distinct edge
sampleTexture4	Flow/Loop	Upward flowing flame noise	Inward flowing distortion
Color1 - Color4	Gradient Map	Yellow -> Orange -> Red -> Black	White -> Purple -> Black -> Null
uTime	Animation Speed	Fast negative scroll (upward)	Slow pulsing scroll

### The "High-Pressure Energy Stream" Pattern (Noise Logic)
The following techniques are used to create unstable, violent energy effects (Beams, Trails, Orbs) by maximizing the chaos of 2D noise textures within PS 2.0/3.0 limits.

1. **Prime-Based Scroll Crossing**: 
   - **Concept**: Scroll two noise layers at speeds based on prime numbers (e.g., `4.71` and `2.33`).
   - **Purpose**: This ensures the interference pattern between noise layers takes an extremely long time to repeat, creating an unpredictable "boiling" feel.
2. **Multi-Axis Jitter (Scrossing)**: 
   - **Concept**: Apply noise-driven distortion to BOTH the X and Y axes of the UVs.
   - **Y-Jitter**: Breaks the vertical alignment of the stream.
   - **X-Jitter (Scrossing)**: Forces the texture to sample different horizontal slices of the noise as it scrolls.
   - **Purpose**: Eliminates the "mechanical conveyor belt" look and makes the texture feel like it's shifting and morphing in real-time.
3. **Procedural Thinning (Focus Functions)**: 
   - **Concept**: Use high-order power functions (e.g., `pow(y, 10)`) to concentrate the render result into a narrow center-line.
   - **Purpose**: Allows you to use a wide geometry strip (to get more surface area for noise sampling) while presenting a razor-thin needle of light.
4. **Physical Breaks (Clip Logic)**: 
   - **Concept**: Use the `clip()` function to discard pixels based on a noise threshold.
   - **Purpose**: Creates jagged, physical holes in the effect, making energy look high-pressure, unstable, and "tearing" through reality.
2. Rendering Archetypes
Different logic for different needs. Do not use a hammer for every screw.

A. The "Mask & Gradient" Shaders (Orbs, Lightning)
Found in 
ThunderBall.cs
 / 
OrbTests.cs
 Used for non-scrolling shapes like projectiles, shockwaves, or energy balls. Concept: A grayscale "Shape" texture defines where the pixel is, a "Noise" texture distorts it, and a "Gradient" texture defines the color.

Caustic/Mask Texture: The main shape (e.g., a lightning bolt, a ring, a star).
Distort Texture: A scrolling noise texture that pushes the UVs of the mask, making it wobble or flow.
Gradient Texture: A 1D texture (horizontal bar) that maps grayscale values to color (e.g., Dark Blue -> Light Blue -> White).
C# Implementation:

csharp
Effect myEffect = ModContent.Request<Effect>("VFXPlus/Effects/Radial/NewRadialScroll").Value;
myEffect.Parameters["causticTexture"].SetValue(LightningShapeTex);
myEffect.Parameters["distortTexture"].SetValue(NoiseTex);
myEffect.Parameters["gradientTexture"].SetValue(ThunderGradientTex); // Maps 0..1 to Blue..White
myEffect.Parameters["flowSpeed"].SetValue(1f);
myEffect.Parameters["distortStrength"].SetValue(0.1f); // How much the noise warps the shape
B. Flipbook Animation (Smoke, Explosions)
Found in 
WindAnimTest.cs
 Used for complex realistic effects that cannot be procedurally generated (baked fluid simulations, realistic smoke).

Texture: A grid of frames (e.g., 8x8 smoke puff).
Logic: Calculate FrameX and FrameY based on projectile.timeLeft.
The "Top Blur" Trick: Draw the sprite twice.
Layer 1 (Body): Darker color (Blue/Gray), standard opacity.
Layer 2 (Highlight): Lighter color (White/Cyan), additive blend, scaled slightly larger or offset.
Result: Gives volume and simulated lighting to 2D sprites.
C. Layered Bloom (The "Glow")
Found in 
AdditivePixelationSystem.cs
 To make things truly glow, do NOT just draw them efficiently. Queue them to a separate render target.

The Queue: PixelationSystem.QueueRenderAction("UnderProjectiles", () => { ... })
The Target: The system draws these queued sprites to a lower-resolution RenderTarget using BlendState.Additive.
The Upscale: When drawing the Target back to the screen, the bilinear filtering softens the edges, creating a free "Bloom" effect without an expensive Gaussian blur shader.
3. The Particle Hierarchy
Terraria has 
Dust
. VFXPlus adds Particles. Know the difference.

1. Vanilla Dust (with Behavior)
Best for: Debris, sparks, simple trails. High performance, dumb logic.

Technique: Use Dust.customData to attach a "Behavior" class that runs typically only rotation/scale logic.
Example: 
GlowPixel.cs
2. Custom ShaderParticle
Best for: Complex, low-quantity effects (Fire embers, floating runes).

Technique: A custom class (not real Dust).
Capabilities:
Supports 
PreDraw
 / 
PostDraw
.
Has 
DrawWithShader
 method (can use the Mask & Gradient shaders!).
Manual opacity/scale fading curves (Easings.easeInSine).
Example: 
FireParticle.cs
4. API & Utility Reference
Core Systems (VFXPlus.Common.Drawing)
Class	Method	Purpose
PixelationSystem	
QueueRenderAction(RenderLayer, Action)
Queues a draw call to a specific additive bloom layer.
ScreenTargetHandler	
AddTarget(ScreenTarget)
Registers a new RenderTarget (Internal use mostly).
Utility Helpers (VFXPlus.Common.Utilities)
Class	Method	Purpose
Easings	easeInSine(float t)	Slow start, fast end. Good for fading out.
Easings	easeOutQuint(float t)	Fast start, very slow end. Good for explosions/impacts.
DustBehaviorUtil	
AssignBehavior_LSBase(...)
Factory for LineSpark behavior (Scaling lines).
DustBehaviorUtil	
AssignBehavior_SGDBase(...)
Factory for SoftGlowDust behavior (Fading orbs).
Standard Shader Parameters
Use this cheat sheet when configuring NewRadialScroll or similar effects.

csharp
// The "Mask & Gradient" Standard
effect.Parameters["causticTexture"].SetValue(Texture2D);   // The Shape
effect.Parameters["distortTexture"].SetValue(Texture2D);   // The Noise
effect.Parameters["gradientTexture"].SetValue(Texture2D);  // The Color Map
effect.Parameters["distortStrength"].SetValue(float);      // 0.0 to 1.0 (Wobble Amount)
effect.Parameters["flowSpeed"].SetValue(float);            // Speed of noise scroll
effect.Parameters["uTime"].SetValue(float);                // Main.GlobalTimeWrappedHourly
effect.Parameters["vignetteSize"].SetValue(float);         // Softness of edges (1.0 = soft, 0.0 = hard)
5. Destiny 2 Element Mapping (Updated)
Solar (1K Voices, Sunshot)
Visuals: Fluid, Upward Flow, Scorching.
Technique: ComboLaser shader.
Textures: Base = White Beam. Noise = "Perlin Fire".
Color: Gradient Map (White -> Yellow -> Red -> Black).
Distortion: High distortion strength on the edges for "heat haze".
Void (Graviton Lance, Nova Bomb)
Visuals: Implosion, Distortion, Negative Space.
Technique: Mask & Gradient shader on a ShaderParticle.
Mask: "Voronoi" cells (cellular noise).
Blend: Subtractive or AlphaBlend with Dark Purple.
Motion: Negative uTime (texture scrolls inwards towards center).
Arc (Riskrunner, Thunderlord)
Visuals: Jagged, Instant, bright Core.
Technique: Mask & Gradient shader.
Mask: Lightning Bolt shape.
Distortion: "Jitter" noise (noise texture offset changes randomly every frame).
Bloom: Heavy reliance on the 
AdditivePixelationSystem
. An Arc effect is 90% white additive brightness, 10% blue texture.