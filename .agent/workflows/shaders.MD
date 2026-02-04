---
description: Shaders
---

Destiny 2 Mod: Comprehensive VFX Architecture
This document serves as the master guide for the Visual Effects (VFX) system in the Destiny 2 Terraria mod. It aggregates knowledge from Bullet Rendering, Shader Loading, and Primitive Systems into a single source of truth.

1. System Overview
The mod's VFX system is built on Decoupled Rendering, meaning visual effects are largely independent of the game logic entities (Projectiles/NPCs) that spawn them.

Core Components
Component	Responsibility	File Location
Shaders (.fx)	GPU instructions for rendering.	Effects/*.fx
Destiny2Shaders	Loads and manages 
Effect
 objects.	
Common/VFX/Destiny2Shaders.cs
PrimitiveSystem	Generates 3D vertex meshes (triangle strips) for trails.	
Common/VFX/PrimitiveSystem.cs
BulletDrawSystem	Manages persistent bullet trails specifically.	
Common/VFX/BulletDrawSystem.cs
2. The Shader Pipeline
2.1 Creation & Compilation
Write HLSL: Create Effects/MyNewShader.fx.
Target: Shader Model 3.0 (vs_3_0, ps_3_0).
Pass Name: Must be named AutoloadPass for tModLoader to recognize it.
Compile: Run compile_effects.ps1.
This uses EasyXnb.exe to compile 
.fx
 -> 
.xnb
.
Output: Assets/AutoloadedEffects/Shaders/Primitives/MyNewShader.xnb.
Load: tModLoader auto-loads the 
.xnb
.
Access via ModContent.Request<Effect>().
2.2 Parameters
Shaders receive data from C# via Parameters. Common parameters used in this mod:

wvp (Matrix): WorldViewProjection matrix (transforms 3D world coords to 2D screen coords).
uTime (float): Global time for animation.
uOpacity (float): Overall transparency.
uColor (float3): Base color tint.
uSecondaryColor (float3): Secondary/Edge color.
uTexture (Texture2D): The main texture (implied by SpriteBatch or passed explicitly).
uNoiseTexture (Texture2D): Extra noise for scrolling textures.
3. Primitive Rendering (Trails)
The mod uses Triangle Strips to render smooth trails. This avoids the "segmented" look of drawing individual sprites.

3.1 The Geometry
PrimitiveSystem
 converts a list of points (vector2) into a mesh:

Spine: The central line defined by the points.
Extrusion: For each point, calculate the normal (perpendicular vector).
Vertices: Create two vertices per point: Center + Normal * Width and Center - Normal * Width.
UVs: Map texture coordinates (0 to 1) along the length of the strip.
3.2 Usage
To render a trail:

// 1. Define Width Function (how thick is the trail at 'factor' 0..1)
float WidthFunc(float factor) => MathHelper.Lerp(20f, 0f, factor);
// 2. Define Color Function (color at 'factor' 0..1)
Color ColorFunc(float factor) => Color.Lerp(Color.Red, Color.Yellow, factor);
// 3. Draw
// 'positions' is List<Vector2> of the trail history
PrimitiveSystem.DrawTrail(
    positions, 
    WidthFunc, 
    ColorFunc, 
    Destiny2Shaders.SolarTrail, // The shader to use
    Main.screenPosition // Offset
);
4. How to Implement a New Effect
Scenario: Creating a "Void Pulse"
Goal: A circular shockwave that distorts the background.

Step 1: HLSL (Effects/VoidPulse.fx)
sampler uImage0 : register(s0);
float uTime;
float3 uColor;
float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);
    // ... math for pulse ...
    return color * float4(uColor, 1.0);
}
technique Technique1 {
    pass AutoloadPass {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
Step 2: Compile
Run compile_effects.ps1. Verify VoidPulse.xnb exists in Assets/AutoloadedEffects/.

Step 3: Load (
Destiny2Shaders.cs
)
public static Effect VoidPulse;
public static void Load(Mod mod) {
    VoidPulse = mod.Assets.Request<Effect>("Assets/AutoloadedEffects/Shaders/Primitives/VoidPulse").Value;
}
Step 4: Render (VoidPulseProjectile.cs)
public override bool PreDraw(ref Color lightColor) {
    Effect shader = Destiny2Shaders.VoidPulse;
    
    // Set Parameters
    shader.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly);
    shader.Parameters["uColor"].SetValue(Color.Purple.ToVector3());
    
    // Apply Shader
    shader.CurrentTechnique.Passes[0].Apply();
    
    // Draw Sprite (or Primitives)
    Main.spriteBatch.Draw(Texture, Projectile.Center - Main.screenPosition, ...);
    
    return false; // Stop vanilla draw
}
5. Troubleshooting Checklist
Shader is Null?
Did you run compile_effects.ps1?
Is the path in 
Destiny2Shaders.cs
 exactly matching the AutoloadedEffects folder structure?
Shader renders nothing?
Check MatrixTransform. Most shaders need uWorldViewProjection set to Main.GameViewMatrix.ZoomMatrix.
Check VertexShader. If you are using SpriteBatch, you usually don't need a custom Vertex Shader. If using Primitives, you MUST match the Vertex Declaration (Position, Color, Texture).
Shader crashes?
6. Advanced: Visual Overrides
Sometimes a weapon needs a unique bullet trail (e.g., SIVA Red, Taken Blight) but logically behaves like a standard element (Kinetic).

Rule: Do NOT add new values to 
Destiny2WeaponElement
 for purely visual effects.

Implementation Pattern
Create Shader: Effects/BulletTrailMyEffect.fx.
Load Shader: Add to 
Destiny2Shaders.cs
.
Override in Code:
Use 
Destiny2PerkProjectile
 to intercept the 
OnKill
 (or 
OnSpawn
) logic.
Set the CustomTrailShader property on the 
Destiny2PerkProjectile
 instance.
Example: Corruption (Outbreak Perfected)
The weapon is 
Kinetic
 (damage logic), but the trail is SIVA Red (visuals).

Destiny2PerkProjectile.cs
:

public override void OnSpawn(Projectile projectile, IEntitySource source) {
    if (HasPerk<ParasitismPerk>()) {
        CustomTrailShader = Destiny2Shaders.CorruptionTrail;
    }
}
Bullet.cs
 (Automatic Handling): The bullet automatically checks Destiny2PerkProjectile.CustomTrailShader. If set, it uses that shader instead of the default Element shader.