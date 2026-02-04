---
description: bullet logic
---

Destiny 2 Bullet & Shader System Architecture
This document provides a technical deep-dive into the "Pseudo-Hitscan" projectile and decoupled rendering system used for weapons in the Destiny 2 Terraria mod.

Core Philosophy: Decoupled Tracers
Unlike vanilla Terraria projectiles where the "sprite" follows the "logic", this mod uses a Decoupled Tracer System.

The logic (collisions, damage, perks) is handled by an invisible, high-speed projectile (
Bullet.cs
).
The visuals (trails, shaders, glows) are handled by a global system (
BulletDrawSystem.cs
) that spawns persistent "segments" of light.
This approach allows for instantaneous-feeling bullets (32+ speed with 100+ extraUpdates) while maintaining perfectly smooth, high-fidelity trails that don't "stutter" at high speeds.

Component Breakdown
1. The Actor: 
Bullet.cs
Bullet.cs
 is the invisible projectile that travels instantly across the screen.

Instant Travel: Uses extraUpdates = 100 and Speed = 32f. It effectively moves thousands of pixels in a single frame.
State Tracking: Stores spawnPosition (where it started the current segment) and maxDistance (falloff range).
Tracer Emission: In 
OnKill
, it calls BulletDrawSystem.SpawnTrace(spawnPosition, Projectile.Center, element). This creates the final visual line from where it started to where it hit.
2. The Renderer: 
BulletDrawSystem.cs
The "Source of Truth" for bullet visuals.

Segment Storage: Maintains a list of 
TraceVFX
 structs. Each trace has a start/end point, an element, and a lifetime.
Post-Logic Drawing: Runs in 
PostDrawTiles
. This ensures trails are drawn on top of the world but under the UI.
Additive Blending: Uses BlendState.Additive to make elemental effects glow vibrantly.
Lifetime Fade: Trails exist for a few frames (default 15) and fade out linearly, creating a lingering "after-image" effect.
3. The GPU Layer: 
Destiny2Shaders.cs
Handles the loading and selection of 
.fx
 shaders.

Shader Registry: Maps Destiny2WeaponElement to specific pixel shaders (e.g., Solar -> BulletTrailSolar).
Lazy Loading: BasicPrimitive is lazy-loaded to avoid threading issues during mod startup.
Parameter Passing: Sets crucial uniform variables like globalTime and uWorldViewProjection before every draw call.
4. The Geometry Engine: 
PrimitiveSystem.cs
A utility for turning 2D points into 3D meshes for the GPU.

Triangle Strips: Takes a list of points and generates a triangle strip mesh that follows the path.
Width/Color Functions: Allows trails to change width (tapering) or color over their length using lambda expressions.
World-to-Screen: Automatically handles camera position and matrix transformations.
5. The Aesthetics: 
ElementalBulletVFX.cs
Defines the "personality" of each element.

Profile System: Stores specific DustID, colors, and trail frequencies for Solar, Arc, Void, etc.
Impact Bursts: Handles the particle explosion when a bullet hits a tile or NPC.
Flight Dust: Periodic dust emission during flight for added texture.
The Life of a Ricochet Bullet
Fire: 
Bullet.cs
 is spawned. Its spawnPosition is set to the barrel of the gun.
First Flight: Because of high extraUpdates, it immediately hits a wall.
Collision: Destiny2PerkProjectile.OnTileCollide is triggered.
Segment 1 (Visual):
BulletDrawSystem.SpawnTrace is called using the original spawn position.
This creates the first visual line from the Gun -> Wall.
Bounce Logic:
bullet.spawnPosition is updated to the current wall hit position.
projectile.velocity is reflected.
projectile.penetrate remains > 0 (it hasn't died yet).
Second Flight: The bullet travels toward its next target.
Final Impact: The bullet hits an enemy or reaches its max range.
Segment 2 (Visual):
Bullet.OnKill is triggered.
BulletDrawSystem.SpawnTrace is called again using the updated spawn position.
This creates the second visual line from the Wall -> End Point.
This creates the zigzagging "connected" trail effect seen with Ricochet Rounds.

Deep Dive: The Bullet Lifecycle
The key to the "instant" feel is the interaction between Projectile.extraUpdates and the spawnPosition state.

1. Instant Travel Explained
A bullet with Speed = 32f and extraUpdates = 100 travels 3,232 pixels in one game frame.

Terraria's engine iterates the projectile's logic 101 times (1 core update + 100 extra updates) per frame.
In each iteration, Collision.CanHit and TileCollision are checked.
If it hits something on the 45th iteration, it stops and dies immediately.
2. The spawnPosition Variable
This is the most critical variable in the system. It tracks the visual origin of the current segment.

On Fire: Initialized in 
OnSpawn
 to the weapon's muzzle.
During Flight: The projectile is invisible (hide = true). No drawing occurs in real-time.
On Hit: When the bullet finally dies, it knows exactly where it started (spawnPosition) and where it ended (Projectile.Center).
3. Trail Spawning Mechanism
Trails are Fire-and-Forget.

// Inside Bullet.cs
public override void OnKill(int timeLeft) {
    BulletDrawSystem.SpawnTrace(spawnPosition, Projectile.Center, GetWeaponElement());
}
When 
SpawnTrace
 is called, it hands these two coordinates to the 
BulletDrawSystem
, which adds them to a persistent list. This list is rendered in the global draw loop, allowing the trail to linger even after the projectile object has been destroyed and its memory potentially reused.

4. Ricochet: The "Segmentation" Problem
Standard bullets hit one thing and die, spawning one trace. Ricochet bullets hit twice.

If we didn't update spawnPosition, a ricochet would draw a straight line from the gun, through the wall, to the final enemy.
The Fix: In Destiny2PerkProjectile.OnTileCollide, we manually intercept the bounce:
Spawn a trace from spawnPosition (Gun) to 
Center
 (Wall). This "finishes" the first segment.
Update spawnPosition to the 
Center
 (Wall). This "starts" the next segment.
Reflect velocity.
When the bullet finally dies after the second hit, its own 
OnKill
 will then draw from the Wall onwards.
Troubleshooting & Debugging Tips
Trails not showing up? Check if initialized is true in 
Bullet.cs
. If the projectile kills itself too early (before 
OnSpawn
 fully finishes or due to immediate collision), it might skip the 
SpawnTrace
 call.
Trails drawing through walls? Ensure bullet.spawnPosition is being updated in EVERY method that redirects the projectile (Ricochet, Teleport, etc.).
Trail stutter? Check extraUpdates. If it's too low, the bullet might take multiple frames to hit its target, causing the visual tracer to be "drawn" in chunks rather than one clean segment.
Shader errors? Verity the 
.xnb
 path in 
Destiny2Shaders.cs
. Shaders must be in Effects/BulletTrail[Name].xnb.