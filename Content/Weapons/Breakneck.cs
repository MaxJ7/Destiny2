using Destiny2.Common.Perks;
using Destiny2.Common.Rarities;
using Destiny2.Common.Weapons;
using Destiny2.Content.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Destiny2.Content.Weapons
{
	public sealed class Breakneck : AutoRifleWeaponItem
	{
		protected override Destiny2WeaponElement GetDefaultWeaponElement()
		{
			return Destiny2WeaponElement.Kinetic;
		}


		public override Destiny2WeaponStats BaseStats => new Destiny2WeaponStats(
			range: 64f,
			stability: 45f,
			reloadSpeed: 46f,
			roundsPerMinute: 450,
			magazine: 34
		);

		protected override void RollFramePerk()
		{
			SetFramePerk(nameof(PrecisionFramePerk));
		}

		protected override void RollPerks()
		{
			string barrel = RollFrom(nameof(HammerForgedRiflingPerk), nameof(SmallborePerk));
			string magazine = RollFrom(nameof(ExtendedMagPerk), nameof(TacticalMagPerk), nameof(AlloyMagPerk));
			string majorOne = RollFrom(nameof(FeedingFrenzyPerk), nameof(DynamicSwayReductionPerk));
			string majorTwo = RollFrom(nameof(OnslaughtPerk), nameof(KineticTremorsPerk), nameof(TargetLockPerk), nameof(AdagioPerk));

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
			Item.rare = ModContent.RarityType<LegendaryRarity>();
			Item.value = Item.buyPrice(gold: 1, silver: 10);
		}
	}
}
