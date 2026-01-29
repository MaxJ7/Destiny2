using Destiny2.Common.Perks;
using Destiny2.Common.Rarities;
using Destiny2.Common.Weapons;
using Destiny2.Content.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Destiny2.Content.Weapons
{
	public sealed class WardensLaw : HandCannonWeaponItem
	{
		public override string Texture => $"Terraria/Images/Item_{ItemID.FlintlockPistol}";

		public override Destiny2WeaponStats BaseStats => new Destiny2WeaponStats(
			range: 56f,
			stability: 29f,
			reloadSpeed: 27f,
			roundsPerMinute: 120,
			magazine: 16
		);

		protected override void RollFramePerk()
		{
			SetFramePerk(nameof(HeavyBurstFramePerk));
		}

		protected override void RollPerks()
		{
			string barrel = RollFrom(nameof(SmallborePerk), nameof(HammerForgedRiflingPerk));
			string magazine = RollFrom(nameof(ExtendedMagPerk), nameof(TacticalMagPerk));
			string majorOne = RollFrom(nameof(FourthTimesTheCharmPerk), nameof(OutlawPerk), nameof(RapidHitPerk));
			string majorTwo = RollFrom(nameof(KillClipPerk), nameof(FrenzyPerk), nameof(RampagePerk));

			SetPerks(barrel, magazine, majorOne, majorTwo);
		}

		private static string RollFrom(params string[] keys)
		{
			if (keys == null || keys.Length == 0)
				return null;

			int index = Main.rand.Next(keys.Length);
			return keys[index];
		}

		public override void SetDefaults()
		{
			Item.width = 126;
			Item.height = 64;
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
