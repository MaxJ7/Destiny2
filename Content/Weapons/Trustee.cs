using Destiny2.Common.Perks;
using Destiny2.Common.Rarities;
using Destiny2.Common.Weapons;
using Destiny2.Content.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Destiny2.Content.Weapons
{
    public sealed class Trustee : ScoutRifleWeaponItem
    {
        protected override Destiny2WeaponElement GetDefaultWeaponElement()
        {
            return Destiny2WeaponElement.Solar;
        }

        public override Destiny2WeaponStats BaseStats => new Destiny2WeaponStats(
            range: 35f,
            stability: 44f,
            reloadSpeed: 31f,
            roundsPerMinute: 260,
            magazine: 17
        );

        protected override void RollFramePerk()
        {
            SetFramePerk(nameof(RapidFireFramePerk));
        }

        protected override void RollPerks()
        {
            string barrel = RollFrom(nameof(HammerForgedRiflingPerk), nameof(SmallborePerk));
            string magazine = RollFrom(nameof(AlloyMagPerk), nameof(ExtendedMagPerk), nameof(TacticalMagPerk));
            string majorOne = RollFrom(nameof(RapidHitPerk));
            string majorTwo = RollFrom(nameof(RampagePerk), nameof(IncandescentPerk));

            SetPerks(barrel, magazine, majorOne, majorTwo);
        }

        public override void SetDefaults()
        {
            Item.width = 76;
            Item.height = 32;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.scale = 1f;
            Item.DamageType = WeaponElement.GetDamageClass();
            Item.damage = 28;
            Item.knockBack = 2.5f;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 14;
            Item.useAnimation = 14;
            Item.shoot = ModContent.ProjectileType<Bullet>();
            Item.shootSpeed = 12f;
            Item.useAmmo = AmmoID.None;
            Item.UseSound = SoundID.Item41;
            Item.rare = ModContent.RarityType<LegendaryRarity>();
            Item.value = Item.buyPrice(gold: 1, silver: 50);
        }
    }
}
