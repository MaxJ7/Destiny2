using Destiny2.Common.Perks;
using Destiny2.Common.Rarities;
using Destiny2.Common.Weapons;
using Destiny2.Content.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Destiny2.Content.Weapons
{
	public sealed class PlaceholderAutoRifle : AutoRifleWeaponItem
	{
		protected override Destiny2WeaponElement GetDefaultWeaponElement()
		{
			return Destiny2WeaponElement.Arc;
		}

		public override string Texture => $"Terraria/Images/Item_{ItemID.Minishark}";

		public override Destiny2WeaponStats BaseStats => new Destiny2WeaponStats(
			range: 44f,
			stability: 52f,
			reloadSpeed: 41f,
			roundsPerMinute: 600,
			magazine: 36
		);

		protected override void RollFramePerk()
		{
			SetFramePerk(nameof(AdaptiveFramePerk));
		}

		protected override void RollPerks()
		{
			string barrel = RollFrom(nameof(SmallborePerk), nameof(HammerForgedRiflingPerk));
			string magazine = RollFrom(nameof(ExtendedMagPerk), nameof(TacticalMagPerk));
			string majorOne = RollFrom(nameof(OutlawPerk), nameof(RapidHitPerk), nameof(FourthTimesTheCharmPerk));
			string majorTwo = RollFrom(nameof(KillClipPerk), nameof(FrenzyPerk), nameof(RampagePerk));

			SetPerks(barrel, magazine, majorOne, majorTwo);
		}

		public override void SetDefaults()
		{
			Item.width = 54;
			Item.height = 20;
			Item.noMelee = true;
			Item.autoReuse = true;
			Item.scale = 1f;
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
			Item.rare = ModContent.RarityType<LegendaryRarity>();
			Item.value = Item.buyPrice(gold: 1, silver: 25);
		}
	}
}
