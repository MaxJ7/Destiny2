using Destiny2.Common.Perks;
using Destiny2.Common.Rarities;
using Destiny2.Common.Weapons;
using Destiny2.Content.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Destiny2.Content.Weapons
{
    public sealed class OutbreakPerfected : PulseRifleWeaponItem
    {

        public override Destiny2WeaponStats BaseStats => new Destiny2WeaponStats(
            range: 44f,
            stability: 40f,
            reloadSpeed: 45f,
            roundsPerMinute: 450,
            magazine: 36
        );

        protected override Destiny2WeaponElement GetDefaultWeaponElement()
        {
            return Destiny2WeaponElement.Kinetic;
        }

        public override void SetDefaults()
        {
            base.SetDefaults(); // Important!
            Item.width = 46;
            Item.height = 20;
            Item.damage = 26;
            Item.knockBack = 2f;
            Item.noMelee = true; // Guns don't hit
            Item.autoReuse = true;
            Item.scale = .85f;

            // Damage Type from Element
            Item.DamageType = WeaponElement.GetDamageClass();

            // Projectile Settings (Destiny 2 Mod uses custom bullet logic)
            Item.shoot = ModContent.ProjectileType<Bullet>();
            Item.shootSpeed = 12f;
            Item.useAmmo = AmmoID.None; // We use internal magazine logic

            // Visuals
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 4; // Placeholder, overridden by Frame Logic
            Item.useAnimation = 12; // Placeholder, overridden by Frame Logic
            Item.UseSound = SoundID.Item11; // Standard gun sound, user can replace

            // Rarity
            Item.rare = ModContent.RarityType<ExoticRarity>();
            Item.value = Item.buyPrice(gold: 5);
        }

        protected override void RollFramePerk()
        {
            SetFramePerk(nameof(TheCorruptionSpreadsFramePerk));
        }

        protected override void RollPerks()
        {
            string barrel = RollFrom(nameof(SmallborePerk));
            string magazine = RollFrom(nameof(AlloyMagPerk));
            string major1 = RollFrom(nameof(OutlawPerk));
            SetPerks(barrel, magazine, major1, RollFrom(nameof(ParasitismPerk)));
        }
    }
}
