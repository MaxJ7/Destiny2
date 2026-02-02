using Destiny2.Common.Perks;
using Destiny2.Common.Rarities;
using Destiny2.Common.Weapons;
using Destiny2.Content.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Destiny2.Content.Weapons
{
	public sealed class Eyasluna : HandCannonWeaponItem
	{
		protected override Destiny2WeaponElement GetDefaultWeaponElement()
		{
			return Destiny2WeaponElement.Stasis;
		}


		public override Destiny2WeaponStats BaseStats => new Destiny2WeaponStats(
			range: 51f,
			stability: 64f,
			reloadSpeed: 45f,
			roundsPerMinute: 140,
			magazine: 11
		);

		protected override void RollFramePerk()
		{
			SetFramePerk(nameof(AdaptiveFramePerk));
		}

		protected override void RollPerks()
		{
			string barrel = RollFrom(nameof(SmallborePerk), nameof(HammerForgedRiflingPerk));
			string magazine = RollFrom(nameof(ExtendedMagPerk), nameof(TacticalMagPerk));
			string majorOne = RollFrom(nameof(OutlawPerk), nameof(RapidHitPerk), nameof(VorpalWeaponPerk));
			string majorTwo = RollFrom(nameof(KillClipPerk), nameof(FrenzyPerk));

			SetPerks(barrel, magazine, majorOne, majorTwo);
		}

		public override void SetDefaults()
		{
			Item.width = 55;
			Item.height = 28;
			Item.noMelee = true;
			Item.autoReuse = true;
			Item.scale = 1f;
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
			Item.rare = ModContent.RarityType<LegendaryRarity>();
			Item.value = Item.buyPrice(gold: 1, silver: 50);
		}
	}
}
