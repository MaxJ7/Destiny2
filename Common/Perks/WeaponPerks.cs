using System;
using Destiny2.Common.Weapons;

namespace Destiny2.Common.Perks
{
	internal static class PerkStatUtils
	{
		public static int ApplyMagazinePercent(int magazine, float percent)
		{
			if (magazine <= 0)
				return magazine;

			double scaled = magazine * (1.0 + percent);
			return (int)Math.Ceiling(scaled);
		}
	}

	public sealed class HammerForgedRiflingPerk : Destiny2Perk
	{
		public override string DisplayName => "Hammer-Forged Rifling";
		public override string Description => "Increases range.";
		public override string IconTexture => "Destiny2/Assets/Perks/HammerForgedRifling";
		public override PerkSlotType SlotType => PerkSlotType.Barrel;

		public override void ModifyStats(ref Destiny2WeaponStats stats)
		{
			stats.Range += 10f;
		}
	}

	public sealed class SmallborePerk : Destiny2Perk
	{
		public override string DisplayName => "Smallbore";
		public override string Description => "Increases range and stability.";
		public override string IconTexture => "Destiny2/Assets/Perks/Smallbore";
		public override PerkSlotType SlotType => PerkSlotType.Barrel;

		public override void ModifyStats(ref Destiny2WeaponStats stats)
		{
			stats.Range += 7f;
			stats.Stability += 7f;
		}
	}

	public sealed class ExtendedMagPerk : Destiny2Perk
	{
		public override string DisplayName => "Extended Mag";
		public override string Description => "Increases magazine size and decreases reload speed.";
		public override string IconTexture => "Destiny2/Assets/Perks/ExtendedMag";
		public override PerkSlotType SlotType => PerkSlotType.Magazine;

		public override void ModifyStats(ref Destiny2WeaponStats stats)
		{
			stats.Magazine = PerkStatUtils.ApplyMagazinePercent(stats.Magazine, 0.10f);
			stats.ReloadSpeed -= 20f;
		}
	}

	public sealed class TacticalMagPerk : Destiny2Perk
	{
		public override string DisplayName => "Tactical Magazine";
		public override string Description => "Increases magazine size, reload speed, and stability.";
		public override string IconTexture => "Destiny2/Assets/Perks/TacticalMag";
		public override PerkSlotType SlotType => PerkSlotType.Magazine;

		public override void ModifyStats(ref Destiny2WeaponStats stats)
		{
			stats.Magazine = PerkStatUtils.ApplyMagazinePercent(stats.Magazine, 0.05f);
			stats.ReloadSpeed += 10f;
			stats.Stability += 5f;
		}
	}

	public sealed class OutlawPerk : Destiny2Perk
	{
		internal const int DurationTicks = 180;
		internal const float ReloadSpeedBonus = 70f;
		internal const float ReloadTimeScalar = 0.9f;
		public override string IconTexture => "Destiny2/Assets/Perks/Outlaw";
		public override string DisplayName => "Outlaw";
		public override string Description => "Kills grant +70 reload speed and faster reloads for 3 seconds.";
	}

	public sealed class RapidHitPerk : Destiny2Perk
	{
		internal const int MaxStacks = 3;
		internal const int DurationTicks = 180;
		internal static readonly float[] StabilityBonusByStacks = { 0f, 3f, 6f, 9f };
		internal static readonly float[] ReloadSpeedBonusByStacks = { 0f, 5f, 10f, 15f };
		internal static readonly float[] ReloadTimeScalarByStacks = { 1f, 0.96f, 0.94f, 0.92f };
		public override string IconTexture => "Destiny2/Assets/Perks/RapidHit";
		public override string DisplayName => "Rapid Hit";
		public override string Description => "Critical hits grant stacking reload speed and stability bonuses.";
	}

	public sealed class KillClipPerk : Destiny2Perk
	{
		internal const int WindowTicks = 180;
		internal const int DurationTicks = 300;
		internal const float DamageMultiplier = 1.2f;
		public override string IconTexture => "Destiny2/Assets/Perks/KillClip";
		public override string DisplayName => "Kill Clip";
		public override string Description => "Reloading shortly after a kill grants a 20% damage bonus.";
	}

	public sealed class FrenzyPerk : Destiny2Perk
	{
		internal const int CombatGraceTicks = 300;
		internal const int ActivationTicks = 720;
		internal const int DurationTicks = 432;
		internal const float DamageMultiplier = 1.15f;
		internal const float ReloadSpeedBonus = 100f;
		public override string IconTexture => "Destiny2/Assets/Perks/Frenzy";
		public override string DisplayName => "Frenzy";
		public override string Description => "After 12 seconds in combat, gain increased damage and reload speed for a short time.";
	}
}
