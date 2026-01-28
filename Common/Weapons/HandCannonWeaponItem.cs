using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace Destiny2.Common.Weapons
{
	public abstract class HandCannonWeaponItem : Destiny2WeaponItem
	{
		public override Destiny2AmmoType AmmoType => Destiny2AmmoType.Primary;

		protected virtual float BaseRecoil => 1.25f;
		protected virtual float MinFalloffTiles => 24f;
		protected virtual float MaxFalloffTiles => 40f;
		protected virtual float ReloadSecondsAtZero => 3.3f;
		protected virtual float ReloadSecondsAtHundred => 2.2f;
		protected virtual float ReloadSecondsAtFifty => 2.8f;
		protected virtual float FalloffTilesAtFifty => 30f;

		public float GetRecoil()
		{
			Destiny2WeaponStats stats = GetStats();
			float stabilityScalar = MathHelper.Clamp(1f - (stats.Stability / 100f), 0.4f, 1f);
			return BaseRecoil * stabilityScalar;
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
			float reloadSeconds = CalculateScaledValue(stats.ReloadSpeed, ReloadSecondsAtZero, ReloadSecondsAtHundred, ReloadSecondsAtFifty);
			return reloadSeconds * GetReloadSpeedTimeScalar();
		}
	}
}
