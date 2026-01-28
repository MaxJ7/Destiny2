using Destiny2.Common.Perks;
using Destiny2.Common.Weapons;
using Destiny2.Common.Rarities;
using Destiny2.Content.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Destiny2.Content.Weapons
{
	public sealed class Malfeasance : HandCannonWeaponItem
	{
		public override Destiny2WeaponElement WeaponElement => Destiny2WeaponElement.Kinetic;

		public override Destiny2WeaponStats BaseStats => new Destiny2WeaponStats(
			range: 45f,
			stability: 95f,
			reloadSpeed: 60f,
			roundsPerMinute: 180,
			magazine: 14
		);

		protected override void RollPerks()
		{
			SetFramePerk(nameof(ExplosiveShadowPerk));
		}

		public override void SetDefaults()
		{
			Item.width = 126;
			Item.height = 64;
			Item.noMelee = true;
			Item.autoReuse = true;
			Item.scale = .5f;
			Item.DamageType = WeaponElement.GetDamageClass();
			Item.damage = 32;
			Item.knockBack = 3f;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.useTime = 20;
			Item.useAnimation = 20;
			Item.shoot = ModContent.ProjectileType<Bullet>();
			Item.shootSpeed = 12f;
			Item.useAmmo = AmmoID.None;
			Item.UseSound = SoundID.Item41;
			Item.rare = ModContent.RarityType<ExoticRarity>();
			Item.value = Item.buyPrice(gold: 2);
		}
	}
}
