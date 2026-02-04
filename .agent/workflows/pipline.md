---
description: Entire development pipeline
---

Destiny 2 Mod Development Pipeline
1. Architecture Overview
The codebase is strictly divided into Common (Systems/Logic) and Content (Assets/Implementations).

The "God Classes"
Weapon Logic: 
Destiny2WeaponItem
 (Common/Weapons) - The core is in 
Destiny2WeaponStats.cs
 (handling Shoot, Reload, Stats) and 
Destiny2WeaponItem.Perks.cs
 (Perks).
Projectile Logic: 
Destiny2PerkProjectile
 (Common/Perks) - Handles everything that happens when a bullet hits something (Ricochets, Explosions, Debuff application).
VFX Logic: 
BulletDrawSystem
 (Common/VFX) - Handles drawing beam trails for all weapons.
2. Directory Structure & System Map
common/Weapons/ (The Core)
Destiny2WeaponStats.cs
: THE HEART. Contains the main partial class for 
Destiny2WeaponItem
. Handles 
Shoot
, 
CanUseItem
, 
Reload
, and 
ModifyWeaponDamage
.
Destiny2WeaponItem.Perks.cs
: Partial class containing the implementation of Trait Perks (e.g., Rampage Stacks).
[Archetype]WeaponItem.cs: (e.g., 
ScoutRifleWeaponItem
) Defines Recoil, Falloff Curves, and Frame Perk Overrides (RPM).
common/Perks/ (The Enhancers)
Destiny2PerkSystem.cs
: Registry.
FramePerks.cs
: Definitions for Frame perks (Rapid Fire, Adaptive).
WeaponPerks.cs
: Definitions for Trait perks (Rampage, Outlaw).
Destiny2PerkProjectile.cs
: GLOBAL PROJECTILE. Intercepts 
OnHitNPC
 to execute perk logic.
PerkSlotType.cs
: Enum (Barrel, 
Magazine
, Major).
common/VFX/ (The Visuals)
BulletDrawSystem.cs
: The central rendering loop.
Destiny2Shaders.cs
: Loads and compiles 
.fx
 files.
common/UI/ (The Interface)
Destiny2HudSystem.cs
: Draws the HUD.
Assets/Perks/ (The Visuals)
Icon Library: Always search this directory before implementing a new perk.
Naming Rule: Matches class names for seamless loading. (e.g., 
Rampage.png
 -> 
RampagePerk
).
Workflow: If an icon exists, use its name for the perk class to avoid pathing errors.
3. Perk Anatomy
Perks are divided into 4 categories.

A. Frame Perks (The DNA)
Definition: 
Common/Perks/FramePerks.cs
.
Code: IsFrame = true.
Role: Defines the Archetype's "Feel". Modified RPM, Recoil.
Implementation: Logic lives in [Archetype]WeaponItem.cs.
Rule: NEVER implement Frame logic in the generic 
Destiny2WeaponItem.Perks.cs
.
B. Barrel Perks (The Stats)
Definition: 
Common/Perks/WeaponPerks.cs
.
Code: SlotType = PerkSlotType.Barrel.
Role: Passive stat bumps (Range+, Stability+).
Implementation: Override 
ModifyStats
 in the perk class itself.
C. Magazine Perks (The Stats + Utility)
Definition: 
Common/Perks/WeaponPerks.cs
.
Code: SlotType = PerkSlotType.Magazine.
Role: Stat bumps (Mag Size+) or Bullet Behavior (Ricochet).
Implementation: Override 
ModifyStats
 OR add mechanics to 
Destiny2PerkProjectile.cs
.
D. Major 'Trait' Perks (The Gameplay)
Definition: 
Common/Perks/WeaponPerks.cs
.
Code: SlotType = PerkSlotType.Major.
Role: Active gameplay loops (Kill -> Damage Buff).
Implementation: Logic lives in 
Destiny2WeaponItem.Perks.cs
 (State/Timers) and 
Destiny2PerkProjectile.cs
 (Triggers).
4. Workflows & Checklists
Workflow 1: Creating a New Weapon
Goal: Add "The Recluse" (Void SMG).

Assets:
Sprite: Content/Weapons/TheRecluse.png (Copy standard size).
Sound: Add firing sound to Assets/Sounds/.
Code:
Create Content/Weapons/TheRecluse.cs.
Inherit SubmachineGunWeaponItem.
Stats:
Override BaseStats. Set RPM, MagSize, Stability.
Set WeaponElement = Destiny2WeaponElement.Void.
Perks:
Override 
RollFramePerk
 -> 
LightweightFramePerk
.
Override 
RollPerks
:
RollFrom(nameof(ChamberedCompensatorPerk), ...)
RollFrom(nameof(RicochetRoundsPerk), ...)
RollFrom(nameof(FeedingFrenzyPerk), ...)
RollFrom(nameof(MasterOfArmsPerk))
 (Unique!).
Workflow 2: Creating a Simple Perk (Stat Buff)
Goal: Add "Smallbore" (Barrel).

Define: Open 
Common/Perks/WeaponPerks.cs
.
Create public sealed class SmallborePerk : Destiny2Perk.
Set PerkSlotType.Barrel.
Logic: Override 
ModifyStats
.
public override void ModifyStats(ref Destiny2WeaponStats stats) {
    stats.Range += 7;
    stats.Stability += 7;
}
Done. No other files needed.
Workflow 3: Creating a Complex Perk (On Kill Effect)
Goal: Add "Master of Arms" (Major).

Define: Open 
Common/Perks/WeaponPerks.cs
.
Create class. Set PerkSlotType.Major.
State: Open 
Common/Weapons/Destiny2WeaponItem.Perks.cs
.
Add private int masterOfArmsTimer;.
Add to 
UpdatePerkTimers
: if (masterOfArmsTimer > 0) masterOfArmsTimer--;.
Trigger: In 
NotifyProjectileHit
 (same file):
if (isKill && HasPerk<MasterOfArmsPerk>()) {
    masterOfArmsTimer = MasterOfArmsPerk.Duration;
}
Buff: In 
ApplyActivePerkStats
 (same file):
if (masterOfArmsTimer > 0) stats.DamageMultiplier *= 1.2f;
HUD: In 
AppendPerkHudEntries
 (same file):
if (masterOfArmsTimer > 0) AddPerkHudEntry(..., nameof(MasterOfArmsPerk), ...);
Workflow 4: Creating a Projectile Effect (Explosion)
Goal: Add "Dragonfly" (Explosion on Precision Kill).

Define: 
Common/Perks/WeaponPerks.cs
.
Trigger: Open 
Common/Perks/Destiny2PerkProjectile.cs
.
Logic: In 
OnHitNPC
:
if (isKill && isPrecision && perks.Contains<DragonflyPerk>()) {
    SpawnExplosion(target.Center); // Custom method
