using Destiny2.Common.Perks;
using Destiny2.Common.Rarities;
using Destiny2.Common.Weapons;
using Destiny2.Content.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Destiny2.Content.Weapons
{
    public sealed class TouchOfMalice : ScoutRifleWeaponItem
    {
        protected override Destiny2WeaponElement GetDefaultWeaponElement()
        {
            return Destiny2WeaponElement.Kinetic;
        }

        public override bool IsWeaponOfSorrow => true;

        public override Destiny2WeaponStats BaseStats => new Destiny2WeaponStats(
            range: 53f,
            stability: 36f,
            reloadSpeed: 46f,
            roundsPerMinute: 260,
            magazine: 17
        );

        protected override void RollFramePerk()
        {
            SetFramePerk(nameof(TouchOfMalicePerk));
        }

        protected override void RollPerks()
        {
            string barrel = RollFrom(nameof(PolygonalRiflingPerk));
            string magazine = RollFrom(nameof(FlaredMagwellPerk));
            string majorOne = RollFrom(nameof(ChargedWithBlightPerk));
            string majorTwo = RollFrom(nameof(HandLaidStockPerk));

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
            Item.rare = ModContent.RarityType<ExoticRarity>();
            Item.value = Item.buyPrice(gold: 1, silver: 50);
        }
    }
}
