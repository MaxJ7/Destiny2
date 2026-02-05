---
description: CodeStyleguide
---

Destiny 2 Mod Code Style Guide
IMPORTANT

Adherence to this guide is mandatory. Deviations will break the build or cause runtime errors.

1. Perk Architecture (The God Class Rule)
Rule: Do NOT create separate files for simple Perks. Reasoning: To prevent file sprawl. We have hundreds of perks; separate files for 5-line classes is unmanageable.

Correct Location:
Weapon Perks: Define in 
Common/Perks/WeaponPerks.cs
.
Frame Perks: Define in 
Common/Perks/FramePerks.cs
.
Incorrect Location:
Common/Perks/MyNewPerk.cs (DO NOT DO THIS).
2. Perk Implementation Contract
Rule: You must correct overrides for 
Destiny2Perk
.

public sealed class MyPerk : Destiny2Perk
{
    // CORRECT
    public override string DisplayName => "My Perk"; 
    
    // INCORRECT (Will cause build error)
    public override string Name => "My Perk";
    
    // STARTDARD
    public override string Description => "...";
    public override string IconTexture => "Destiny2/Assets/Perks/MyPerk";
    public override PerkSlotType SlotType => PerkSlotType.Major; // or Barrel, Magazine
}
3. The "Pseudo-Partial" Pattern (Perks)
Rule: Logic for perks is split across specific God Classes based on function.

Stat Logic (Barrels/Mags): Override 
ModifyStats
 inside the Perk Class in 
WeaponPerks.cs
.
Gameplay State (Timers/Stacks): Implementation lives in 
Common/Weapons/Destiny2WeaponItem.Perks.cs
.
Do NOT put state variables in the 
Perk
 class itself. Perks are singletons/definitions.
State must be instanced per weapon in 
Destiny2WeaponItem
.
Projectile Effects (OnHit): Implementation lives in 
Common/Perks/Destiny2PerkProjectile.cs
.
4. Weapon Architecture
Rule: Weapons must be lightweight implementers of Archetypes.

Inheritance: Must be sealed class and inherit from [Archetype]WeaponItem.
Location: Content/Weapons/.
Assets: Sprite MUST match class name and be in the same folder.
Content/Weapons/MyGun.cs -> Content/Weapons/MyGun.png.
Forbidden: Overriding Texture property manually. Let tModLoader auto-load.
Boilerplate: SetDefaults must ALWAYS explicitly set:
Item.DamageType = WeaponElement.GetDamageClass();
Item.shoot = ModContent.ProjectileType<Bullet>(); // Or CombatBowProjectile for Bows
Item.useAmmo = AmmoID.None;
5. Archetype Architecture
Rule: Archetypes define the "feel" and math.

Inheritance: Must be abstract class and inherit from Destiny2WeaponItem.
Location: Common/Weapons/.
Responsibility:
MUST override GetRecoilStrength (Stability -> Vector2).
MUST override GetFalloffTiles (Range -> Tiles).
MUST override GetReloadSeconds (Reload -> Seconds).
MUST override GetFrameRoundsPerMinute (Frame Perk -> RPM). Or GetFrameChargeTime for Bows.
Forbidden: Implementing concrete stats (Damage, MagSize) in the Archetype. These belong in the specific Weapon. (Note: Bows MUST set Magazine = 1 in GetStats or SetDefaults).
6. NPC & logic Architecture
Rule: Separate Logic/Systems from Content/Assets.

GlobalNPCs: Must go in Common/NPCs/.
Reasoning: They are systems that apply to all or many NPCs (e.g., Nanite tracking, freezing). They are NOT content themselves.
ModNPCs: Must go in Content/NPCs/.
Reasoning: These are specific enemies (e.g., a Fallen Dreg).
7. Naming Conventions
Perk Classes: [Name]Perk (e.g., RampagePerk).
Frame Classes: [Name]FramePerk (e.g., AdaptiveFramePerk).
Weapon Classes: [Name] (e.g., OutbreakPerfected).
No "Item" suffix for content weapons.
Archetype Classes: [Name]WeaponItem (e.g., AutoRifleWeaponItem).
Assets: Match the class name.
8. Critical "Gotchas"
ModifyHitNPC Duplication: Do NOT override ModifyHitNPC in Destiny2WeaponStats.cs. It is already overridden in Destiny2WeaponItem.Perks.cs.
Bullet Logic: Destiny2WeaponItem.ModifyHitNPC DOES NOT work for bullets. Use Destiny2PerkProjectile.ModifyHitNPC for all ranged weapon perks.
Perk Registration: You MUST register new perks in Destiny2PerkSystem.InitializePerks().
Enum Pollution: Do NOT add new values to Destiny2WeaponElement for visual-only effects (e.g., "Corruption" or "Taken"). Use the Visual Override pattern in Destiny2PerkProjectile instead.