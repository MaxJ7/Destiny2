using System;
using Destiny2.Common.Perks;
using Microsoft.Xna.Framework;
using Terraria;

namespace Destiny2.Common.Weapons
{
	public abstract class AutoRifleWeaponItem : Destiny2WeaponItem
	{
		private const float PrecisionFrameRecoilScalar = 0.9f;
		private const float MaxRecoilAngleAtZeroStability = 38f;
		private const float MaxRecoilAngleAtHundredStability = 12f;
		private const float MinRecoilStepScalar = 0.06f;
		private const float MaxRecoilStepScalar = 0.22f;
		private const float MaxBloomAngleDeg = 12f;
		private const float MaxBloomStabilityReduction = 0.12f;
		private const float BloomIncreasePerShotDeg = 1.1f;

		private float recoilAngle;
		private float bloomAngle;
		private ulong lastShotTick;

		public override Destiny2AmmoType AmmoType => Destiny2AmmoType.Primary;

		public override float GetPrecisionMultiplier()
		{
			if (!TryGetFramePerk(out Destiny2Perk framePerk))
				return 1f;

			if (framePerk is PrecisionFramePerk || framePerk is RapidFireFramePerk || framePerk is HighImpactFramePerk || framePerk is AdaptiveFramePerk || framePerk is TheRightChoiceFramePerk || framePerk is LightweightFramePerk)
				return 1.75f;

			return 1f;
		}

		protected virtual float BaseRecoil => 1.1f;
		protected virtual float MinFalloffTiles => 23f;
		protected virtual float MaxFalloffTiles => 36f;
		protected virtual float FalloffTilesAtFifty => 29.5f;
		protected virtual float ReloadSecondsAtZero => 2.5f;
		protected virtual float ReloadSecondsAtHundred => 1.3f;
		protected virtual float ReloadSecondsAtFifty => 1.9f;

		private float GetFrameRecoilScalar()
		{
			if (string.IsNullOrWhiteSpace(FramePerkKey))
				return 1f;

			if (Destiny2PerkSystem.TryGet(FramePerkKey, out Destiny2Perk framePerk)
				&& framePerk is PrecisionFramePerk)
				return PrecisionFrameRecoilScalar;

			return 1f;
		}

		public float GetRecoil()
		{
			Destiny2WeaponStats stats = GetStats();
			float stabilityScalar = MathHelper.Clamp(1f - (stats.Stability / 100f), 0.35f, 1f);
			return BaseRecoil * stabilityScalar * GetFrameRecoilScalar();
		}

		protected override float GetRecoilStrength()
		{
			return GetRecoil();
		}

		public override float GetFalloffTiles()
		{
			Destiny2WeaponStats stats = GetStats();
			return CalculateFalloffTiles(stats.Range, MinFalloffTiles, MaxFalloffTiles, FalloffTilesAtFifty);
		}

		public override float GetMaxFalloffTiles()
		{
			return MaxFalloffTiles;
		}

		public override float GetReloadSeconds()
		{
			Destiny2WeaponStats stats = GetStats();
			return CalculateScaledValue(stats.ReloadSpeed, ReloadSecondsAtZero, ReloadSecondsAtHundred, ReloadSecondsAtFifty)
				* GetReloadSpeedTimeScalar();
		}

		protected override int GetFrameRoundsPerMinute(Destiny2Perk framePerk, int currentRpm)
		{
			if (framePerk is AdaptiveFramePerk)
				return 600;
			if (framePerk is PrecisionFramePerk)
				return 450;
			if (framePerk is TheRightChoiceFramePerk)
				return 600;
			if (framePerk is LightweightFramePerk)
				return 720;
			if (framePerk is RapidFireFramePerk)
				return 720;	
			if(framePerk is HighImpactFramePerk)
				return 360;

			return currentRpm;
		}

		public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
		{
			ulong tick = Main.GameUpdateCount;
			ulong resetTicks = (ulong)Math.Max(10, Item.useTime + 2);
			if (tick - lastShotTick > resetTicks)
			{
				recoilAngle = 0f;
				bloomAngle = 0f;
			}

			Destiny2WeaponStats stats = GetStats();
			float stability = Math.Clamp(stats.Stability, 0f, 100f);
			float stabilityRatio = stability / 100f;

			float maxAngleDeg = MathHelper.Lerp(MaxRecoilAngleAtZeroStability, MaxRecoilAngleAtHundredStability, stabilityRatio);
			float maxAngle = MathHelper.ToRadians(maxAngleDeg);
			maxAngle *= GetFrameRecoilScalar();

			float offset = recoilAngle;
			if (offset > 0f)
			{
				float sign = velocity.X < 0f ? 1f : -1f;
				velocity = velocity.RotatedBy(sign * offset);
			}

			float stepScalar = MathHelper.Lerp(MaxRecoilStepScalar, MinRecoilStepScalar, stabilityRatio);
			float step = maxAngle * stepScalar;
			recoilAngle = Math.Min(maxAngle, recoilAngle + step);

			float bloomMaxDeg = MaxBloomAngleDeg * (1f - MaxBloomStabilityReduction * stabilityRatio);
			bloomAngle = Math.Min(bloomMaxDeg, bloomAngle + BloomIncreasePerShotDeg);
			if (bloomAngle > 0f)
			{
				float bloomOffset = MathHelper.ToRadians(Main.rand.NextFloat(-bloomAngle, bloomAngle));
				velocity = velocity.RotatedBy(bloomOffset);
			}

			lastShotTick = tick;

			base.ModifyShootStats(player, ref position, ref velocity, ref type, ref damage, ref knockback);
		}
	}
}
