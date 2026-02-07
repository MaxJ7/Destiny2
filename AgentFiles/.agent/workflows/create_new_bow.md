---
description: How to add a new Combat Bow to the mod.
---

Combat Bows use a unique charging mechanic (`DrawRatio`) and custom physics.

## 1. Prerequisites
- Inherit from `CombatBowWeaponItem`.
- Sprite must match class name.
- Projectile must be `CombatBowProjectile`.

## 2. Implementation Template
```csharp
public sealed class MyBow : CombatBowWeaponItem
{
    public override Destiny2WeaponStats BaseStats => new Destiny2WeaponStats(
        range: 60f,
        stability: 50f,
        reloadSpeed: 50f,
        roundsPerMinute: 60, // Placebo
        magazine: 1, // MANDATORY
        chargeTime: 640 // Draw time in ms
    );

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.damage = 100;
        Item.shoot = ModContent.ProjectileType<Projectiles.CombatBowProjectile>();
        Item.shootSpeed = 16f;
    }

    protected override Destiny2WeaponElement GetDefaultWeaponElement() => Destiny2WeaponElement.Solar;

    protected override void RollFramePerk() => SetFramePerk(nameof(LightweightBowFramePerk));

    protected override void RollPerks()
    {
        string barrel = RollFrom(nameof(ElasticStringPerk), nameof(NaturalStringPerk));
        string magazine = RollFrom(nameof(CarbonArrowShaftPerk), nameof(CompactArrowShaftPerk));
        string major1 = RollFrom(nameof(ArchersTempoPerk));
        string major2 = RollFrom(nameof(ExplosiveHeadPerk));

        SetPerks(barrel, magazine, major1, major2);
    }
}
```

## 3. Key Mechanics
- **Magazine Size**: Must ALWAYS be 1. Reloading triggers the nocking animation.
- **Charge Time**: Controlled by `chargeTime` in `BaseStats`. Frames (Lightweight vs Precision) override this value via `GetFrameChargeTime`.
- **Precision Multiplier**: Lightweight Bows have a **1.6x** precision multiplier (default is 1.5x).
- **Perfect Draw**: Releasing within the window (scaled by Stability) grants +5% damage.
- **Overdraw**: Holding too long applies a damage/velocity penalty.
