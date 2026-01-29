using Destiny2.Common.Perks;
using Destiny2.Common.Rarities;
using Destiny2.Common.Weapons;
using Destiny2.Content.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Destiny2.Content.Weapons
{
	public sealed class Khovostov7G0X : AutoRifleWeaponItem
	{
		protected override Destiny2WeaponElement GetDefaultWeaponElement()
		{
			return Destiny2WeaponElement.Kinetic;
		}

		public override Destiny2WeaponStats BaseStats => new Destiny2WeaponStats(
			range: 37f,
			stability: 72f,
			reloadSpeed: 79f,
			roundsPerMinute: 600,
			magazine: 43
		);

		protected override void RollFramePerk()
		{
			SetFramePerk(nameof(TheRightChoiceFramePerk));
		}

		protected override void RollPerks()
		{
			SetPerks(
				nameof(HammerForgedRiflingPerk),
				nameof(AlloyMagPerk),
				nameof(EyesUpGuardianPerk),
				nameof(CompositeStockPerk));
		}

		public override void SetDefaults()
		{
			Item.width = 54;
			Item.height = 20;
			Item.noMelee = true;
			Item.autoReuse = true;
			Item.scale = .25f;
			Item.DamageType = WeaponElement.GetDamageClass();
			Item.damage = 18;
			Item.knockBack = 2.5f;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.useTime = 6;
			Item.useAnimation = 6;
			Item.shoot = ModContent.ProjectileType<Bullet>();
			Item.shootSpeed = 12f;
			Item.useAmmo = AmmoID.None;
			Item.UseSound = SoundID.Item41;
			Item.rare = ModContent.RarityType<ExoticRarity>();
			Item.value = Item.buyPrice(gold: 2);
		}
	}
}
