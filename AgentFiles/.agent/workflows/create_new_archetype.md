---
description: How to create a new Weapon Type (Archetype) like "Trace Rifle" or "Sidearm".
---

This workflow guides you through creating a new **Weapon Archetype** (e.g., Submachine Gun, Fusion Rifle) that serves as a base for individual weapons.

## 1. Define the Frame Perks
First, define the "Frames" (sub-types) that this weapon type will use in `Common/Perks/FramePerks.cs`.

```csharp
// Example: Common/Perks/FramePerks.cs
public sealed class RapidFireSidearmFramePerk : Destiny2Perk
{
    public override string DisplayName => "Rapid-Fire Frame";
    public override string Description => "Full auto. Deeper ammo reserves. Slightly faster reload when magazine is empty.";
    public override string IconTexture => "Destiny2/Assets/Perks/RapidFire"; // Reuse existing or add new
    public override bool IsFrame => true;
}
```

## 2. Create the Abstract Base Class
Create a new file in `Common/Weapons/` named `[Archetype]WeaponItem.cs` (e.g., `SidearmWeaponItem.cs`).
This class MUST inherit from `Destiny2WeaponItem` and be `abstract`.

### Required Overrides:
- **AmmoType**: `Primary`, `Special`, or `Heavy`.
- **GetPrecisionMultiplier()**: Define crit damage multipliers based on the Frame Perk.
- **GetRecoilStrength()**: Calculate recoil based on Stability stat and Frame.
- **GetFalloffTiles()**: Calculate range falloff based on Range stat.
- **GetReloadSeconds()**: Calculate reload time based on Reload Speed stat.
- **GetFrameRoundsPerMinute()**: Define the RPM for each Frame Perk.

### Example Template:
```csharp
using Destiny2.Common.Perks;
using Microsoft.Xna.Framework;
using Terraria;

namespace Destiny2.Common.Weapons
{
    public abstract class SidearmWeaponItem : Destiny2WeaponItem
    {
        // define constants for tuning
        private const float BaseRecoil = 1.0f;
        
        public override Destiny2AmmoType AmmoType => Destiny2AmmoType.Primary;

        public override float GetPrecisionMultiplier()
        {
            if (TryGetFramePerk(out Destiny2Perk frame) && frame is RapidFireSidearmFramePerk)
                return 1.4f; // Custom crit value
            return 1.3f;
        }

        protected override float GetRecoilStrength()
        {
            Destiny2WeaponStats stats = GetStats();
            float stabilityScalar = MathHelper.Clamp(1f - (stats.Stability / 100f), 0.5f, 1f);
            return BaseRecoil * stabilityScalar;
        }

        public override float GetFalloffTiles()
        {
             // Use helper methods for standard scaling
             Destiny2WeaponStats stats = GetStats();
             return CalculateFalloffTiles(stats.Range, min: 15f, max: 25f, tilesAt50: 20f);
        }

        public override float GetReloadSeconds()
        {
            Destiny2WeaponStats stats = GetStats();
            return CalculateScaledValue(stats.ReloadSpeed, min: 2.5f, max: 1.2f, valueAt50: 1.8f);
        }

        protected override int GetFrameRoundsPerMinute(Destiny2Perk framePerk, int currentRpm)
        {
            if (framePerk is RapidFireSidearmFramePerk) return 450;
            return currentRpm;
        }
    }
}
```

## 3. Implement Visuals (Optional - Advanced)
If your weapon type needs unique firing mechanics (like burst fire or charging), override `ModifyShootStats` or `UltHoldItem`.
- **Recoil Implementation**: See `AutoRifleWeaponItem.cs` for how `recoilAngle` is calculated and applied to velocity.
- **Burst Fire**: See `HandCannonWeaponItem.cs` (`ApplyBurstSettings`) for how `Item.useAnimation` is decoupled from `Item.useTime` to create bursts.

## 4. Create a Concrete Weapon
Now you can create actual guns using this archetype.
See the `/create_new_weapon` workflow, but inherit from your new `SidearmWeaponItem` instead.

```csharp
// Content/Weapons/MySidearm.cs
public sealed class MySidearm : SidearmWeaponItem { ... }
```
