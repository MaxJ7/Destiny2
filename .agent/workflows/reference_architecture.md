---
description: 
---

Destiny 2 Mod Architecture Reference
1. System Map Coverage
This codebase is strictly divided into Common (Systems/Logic) and Content (Assets/Implementations).

The "God Classes"
Weapon Logic: 
Destiny2WeaponItem
 (Common/Weapons)
Core logic lives in 
Destiny2WeaponStats.cs
 (Shoot, Reload, Stat logic, Tooltips).
Partial logic in 
Destiny2WeaponItem.Perks.cs
 (Trait state/hooks).
Projectile Logic: 
Destiny2PerkProjectile
 (Common/Perks)
Handles: 
OnHitNPC
 / 
OnKill
 logic for all perks (Explosions, Debuffs, Ricochets).
VFX Logic: 
BulletDrawSystem
 (Common/VFX)
Handles: Drawing all bullet trails via Shaders.
Reference: See 
bullets.md
 for the complete breakdown of the "Pseudo-Hitscan" renderer.
2. Directory Responsibilities (Deep Dive)
Common/Weapons/
Destiny2WeaponStats.cs
: Core Class Definition. Main partial class for 
Destiny2WeaponItem
.
Destiny2WeaponItem.Perks.cs
: Major Perk Implementations. Stores timers (rampageStacks) and Hooks (
NotifyProjectileHit
).
[Archetype]WeaponItem.cs: Archetype Logic. Defines Recoil, Falloff, and Frame Perk Overrides (RPM).
Common/Players/: ModPlayer classes (e.g., Destiny2Player).
Common/NPCs/: GlobalNPC classes (e.g., NaniteGlobalNPC). Logic/State only.
Common/UI/: User Interface systems.
Destiny2AmmoType.cs: Enum.
Destiny2WeaponElement.cs: Enum.
Common/Perks/
WeaponPerks.cs: Definitions. Defines properties (Name, Icon, Description, SlotType).
FramePerks.cs: Definitions. Defines Frame perks (Rapid Fire, etc).
Destiny2PerkProjectile.cs: Global Projectile. The logic hub for bullet effects.
Destiny2PerkSystem.cs: Registry. Register all perks here.
Common/VFX/
Destiny2Shaders.cs: Shader loader.
BulletDrawSystem.cs: Trail renderer.
ElementalBulletVFX.cs: Color logic.
PrimitiveSystem.cs: Drawing utils.
Content/Weapons/ (Implementation 