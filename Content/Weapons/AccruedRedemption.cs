using Destiny2.Common.Perks;
using Destiny2.Common.Weapons;
using Terraria.ID;
using Terraria.ModLoader;

namespace Destiny2.Content.Weapons
{
    public sealed class AccruedRedemption : CombatBowWeaponItem
    {


        public override Destiny2WeaponStats BaseStats => new Destiny2WeaponStats(
            range: 71f,
            stability: 43f,
            reloadSpeed: 40f, // Good reload
            roundsPerMinute: 60, // Placebo for bows
            magazine: 1, // Bows have 1 arrow
            chargeTime: 640 // 640ms Draw Time
        );

        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.damage = 140; // High single shot damage
            Item.width = 38;
            Item.height = 84;
            Item.scale = .85f;
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
            return Destiny2WeaponElement.Kinetic;
        }

        protected override void RollFramePerk()
        {
            SetFramePerk(nameof(PrecisionBowFramePerk));
        }

        protected override void RollPerks()
        {
            string barrel = RollFrom(nameof(ElasticStringPerk), nameof(NaturalStringPerk));
            string magazine = RollFrom(nameof(CarbonArrowShaftPerk), nameof(CompactArrowShaftPerk));
            string majorOne = RollFrom(nameof(ArchersTempoPerk), nameof(KillingWindPerk), nameof(SuccessfulWarmUpPerk));
            string majorTwo = RollFrom(nameof(ExplosiveHeadPerk), nameof(FireflyPerk), nameof(KineticTremorsPerk));

            SetPerks(barrel, magazine, majorOne, majorTwo);
        }
    }
}
