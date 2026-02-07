---
description: How to add a new Weapon (Gun) to the mod.
---

This workflow guides you through creating a new Weapon (e.g., The Recluse, Fatebringer) derived from an existing Archetype.

## 1. Prerequisites
- The **Archetype** must exist (e.g., `AutoRifleWeaponItem`). If not, see `/create_new_archetype`.
- For **Bows**, see `/create_new_bow` specifically.
- The **Frame Perk** must exist (e.g., `LightweightFramePerk`).
- The **Sprite** (`.png`) must exist in the same folder as the `.cs` file.

## 2. Create the Weapon File
Create a new file in `Content/Weapons/[Name].cs`.
**Ensure the sprite `[Name].png` is in the same folder.** tModLoader will load it automatically.
The class must be `sealed` and inherit from the Archetype class.

```csharp
namespace Destiny2.Content.Weapons
{
    public sealed class MyWeapon : AutoRifleWeaponItem
    {
        // ... implementation ...
    }
}
```

## 3. Override BaseStats
Define the base stats. **Do NOT set these in `SetDefaults`.**

```csharp
public override Destiny2WeaponStats BaseStats => new Destiny2WeaponStats(
    range: 50f,
    stability: 60f,
    reloadSpeed: 40f,
    roundsPerMinute: 600,
    magazine: 30
);
```

## 4. Define Element
Override `GetDefaultWeaponElement` to set the Kinetic/Energy type.

```csharp
protected override Destiny2WeaponElement GetDefaultWeaponElement()
{
    // Options: Kinetic, Solar, Arc, Void, Stasis, Strand
    return Destiny2WeaponElement.Void;
}
```

## 5. Implement SetDefaults (CRITICAL)
Set standard Terraria item properties here.
**You MUST include the DamageType, Shoot, ShootSpeed, and UseAmmo settings exactly as shown.**

```csharp
public override void SetDefaults()
{
    base.SetDefaults(); // Always call base
    Item.width = 46;
    Item.height = 20;
    Item.damage = 25; // Base damage for tooltips/scaling
    Item.knockBack = 2f;
    Item.noMelee = true;
    Item.autoReuse = true;
    Item.scale = 1f;

    // CRITICAL: Connects Element System to Damage Class
    Item.DamageType = WeaponElement.GetDamageClass();

    // CRITICAL: Uses Mod's Bullet System
    Item.shoot = ModContent.ProjectileType<Bullet>();
    Item.shootSpeed = 12f;
    Item.useAmmo = AmmoID.None; // We handle ammo internally

    Item.useStyle = ItemUseStyleID.Shoot;
    Item.useTime = 10; // Overridden by Frame, but good to set defaults
    Item.useAnimation = 10;
    
    // Rarity and Value
    Item.rare = ModContent.RarityType<LegendaryRarity>(); // or ExoticRarity
    Item.value = Item.buyPrice(gold: 5);
}
```

## 6. Roll Frame Perk
Override `RollFramePerk` and use `SetFramePerk`.
This defines the intrinsic behavior (RPM, Burst, Handling).

```csharp
protected override void RollFramePerk()
{
    SetFramePerk(nameof(AdaptiveFramePerk));
}
```

## 7. Roll Perks (CRITICAL)
Override `RollPerks`.
**Strict Ordering**: `Barrel` -> `Magazine` -> `Major 1` -> `Major 2`.
Use `RollFrom(...)` to select from a pool (or single fixed perk) and `SetPerks(...)` to apply them.
**Note: We do NOT use the term "Trait". Use "Major Perk" (SlotType.Major).**

```csharp
protected override void RollPerks()
{
    // 1. Barrel Slot
    string barrel = RollFrom(nameof(SmallborePerk), nameof(CorkscrewRiflingPerk));
    
    // 2. Magazine Slot
    string magazine = RollFrom(nameof(ExtendedMagPerk), nameof(TacticalMagPerk));
    
    // 3. Major Slot 1 (Primary gameplay perk like Outlaw)
    string major1 = RollFrom(nameof(OutlawPerk)); // Fixed Perk example
    
    // 4. Major Slot 2 (Secondary gameplay perk like Rampage)
    string major2 = RollFrom(nameof(RampagePerk), nameof(KillClipPerk)); // Random Pool example

    // Apply exact order: Barrel, Magazine, Major1, Major2
    SetPerks(barrel, magazine, major1, major2);
}
```
