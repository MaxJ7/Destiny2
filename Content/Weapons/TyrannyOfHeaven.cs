using Destiny2.Common.Perks;
using Destiny2.Common.Weapons;
using Terraria.ID;
using Terraria.ModLoader;

namespace Destiny2.Content.Weapons
{
    public sealed class TyrannyOfHeaven : CombatBowWeaponItem
    {

        public override Destiny2WeaponStats BaseStats => new Destiny2WeaponStats(
            range: 65f,
            stability: 50f,
            reloadSpeed: 60f, // Good reload
            roundsPerMinute: 60, // Placebo for bows
            magazine: 1, // Bows have 1 arrow
            chargeTime: 640 // 640ms Draw Time
        );

        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.damage = 120; // High single shot damage
            Item.width = 16;
            Item.height = 74;
            Item.shootSpeed = 16f; // Fast arrow
            // Item.shoot = ModContent.ProjectileType<Destiny2Arrow>(); // Future? For now default or Bullet?
            // Bows usually shoot arrows. Let's use Wooden Arrow for now to test mechanics, 
            // or Bullet if the mod is Bullet-only architecture (based on BulletDrawSystem).
            // Agent Behavior says "Bullet.cs" is the actor.
            // If we use Bullet.cs, it's instant hitscan. 
            // Use Custom Projectile
            Item.shoot = ModContent.ProjectileType<Projectiles.CombatBowProjectile>();

            // Element
            // SetWeaponElement(Destiny2WeaponElement.Solar); // Removed in favor of GetDefaultWeaponElement
        }

        public override Destiny2AmmoType AmmoType => Destiny2AmmoType.Primary;

        protected override Destiny2WeaponElement GetDefaultWeaponElement()
        {
            return Destiny2WeaponElement.Solar;
        }

        protected override void RollFramePerk()
        {
            SetFramePerk(nameof(LightweightBowFramePerk));
        }

        protected override void RollPerks()
        {
            string barrel = RollFrom(nameof(ElasticStringPerk), nameof(NaturalStringPerk));
            string magazine = RollFrom(nameof(CarbonArrowShaftPerk), nameof(CompactArrowShaftPerk));
            string majorOne = RollFrom(nameof(ArchersTempoPerk), nameof(ExplosiveHeadPerk));
            string majorTwo = RollFrom(nameof(IncandescentPerk), nameof(AdagioPerk));

            SetPerks(barrel, magazine, majorOne, majorTwo);
        }
    }
}
