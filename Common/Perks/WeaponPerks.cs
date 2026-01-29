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

	public sealed class AlloyMagPerk : Destiny2Perk
	{
		internal const float HalfMagScalar = 0.9f;
		internal const float EmptyMagScalar = 0.8f;
		public override string DisplayName => "Alloy Mag";
		public override string Description => "Faster reload when the magazine is nearly empty.";
		public override string IconTexture => "Destiny2/Assets/Perks/AlloyMag";
		public override PerkSlotType SlotType => PerkSlotType.Magazine;
	}

	public sealed class CompositeStockPerk : Destiny2Perk
	{
		public override string DisplayName => "Composite Stock";
		public override string Description => "Increases stability and range.";
		public override string IconTexture => "Destiny2/Assets/Perks/CompositeStock";
		public override PerkSlotType SlotType => PerkSlotType.Major;

		public override void ModifyStats(ref Destiny2WeaponStats stats)
		{
			stats.Stability += 5f;
			stats.Range += 2f;
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

	public sealed class EyesUpGuardianPerk : Destiny2Perk
	{
		internal const int StacksGranted = 7;
		internal const int MaxStacks = 7;
		internal const int RicochetCount = 7;
		internal const float RicochetRangeTiles = 12f;
		internal const int MaxHitsPerTarget = 2;
		public override string DisplayName => "Eye's Up Guardian";
		public override string Description =>
			"Consuming a potion grants 7 ricochet stacks. Each hit consumes 1 stack to ricochet up to 7 times within 12 tiles.";
		public override string IconTexture => "Destiny2/Assets/Perks/EyesUpGuardian";
		public override PerkSlotType SlotType => PerkSlotType.Major;
	}

	public sealed class ShootToLootPerk : Destiny2Perk
	{
		public override string DisplayName => "Shoot to Loot";
		public override string Description => "Does nothing for now.";
		public override string IconTexture => "Destiny2/Assets/Perks/ShootToLoot";
		public override PerkSlotType SlotType => PerkSlotType.Major;
	}

	public sealed class FourthTimesTheCharmPerk : Destiny2Perk
	{
		internal const int HitsRequired = 4;
		internal const int WindowTicks = 120;
		internal const int AmmoReturned = 2;
		public override string IconTexture => "Destiny2/Assets/Perks/FourthTimesTheCharm";
		public override string DisplayName => "Fourth Times the Charm";
		public override string Description =>
			"Rapidly landing hits will return two rounds to the magazine. Scoring 4 hits within 2 seconds refills 2 ammo.";
	}

	public sealed class RampagePerk : Destiny2Perk
	{
		internal const int MaxStacks = 3;
		internal const int DurationTicks = 180;
		internal static readonly float[] DamageMultiplierByStacks = { 1f, 1.08f, 1.16f, 1.24f };
		public override string IconTexture => "Destiny2/Assets/Perks/Rampage";
		public override string DisplayName => "Rampage";
		public override string Description =>
			"Kills with this weapon temporarily grant increased damage. Stacks 3x (8%/16%/24%).";
	}

	public sealed class OnslaughtPerk : Destiny2Perk
	{
		internal const int MaxStacks = 3;
		internal const int DurationTicks = 270;
		internal static readonly float[] RpmScalarByStacks = { 1f, 1.2f, 1.4f, 1.6f };
		internal static readonly float[] ReloadSpeedByStacks = { 0f, 15f, 25f, 35f };
		public override string IconTexture => "Destiny2/Assets/Perks/Onslaught";
		public override string DisplayName => "Onslaught";
		public override string Description =>
			"Weapon kills grant Onslaught for 4.5s (max 3). 1/2/3 stacks: -16.7/-28.6/-37.5% firing delay, +15/+25/+35 reload speed.";
		public override PerkSlotType SlotType => PerkSlotType.Major;
	}

	public sealed class KineticTremorsPerk : Destiny2Perk
	{
		internal const int HitWindowTicks = 180;
		internal const int AutoRifleHitsRequired = 12;
		internal const int HandCannonHitsRequired = 6;
		internal const int PulseCount = 3;
		internal const int PulseIntervalTicks = 60;
		internal const int InitialDelayTicks = 15;
		internal const int BowInitialDelayTicks = 10;
		internal const int CooldownAfterLastPulseTicks = 120;
		internal const int MaxShockwaveDamage = 90;
		internal const float ShockwaveRadiusTiles = 6f;
		public override string IconTexture => "Destiny2/Assets/Perks/KineticTremors";
		public override string DisplayName => "Kinetic Tremors";
		public override string Description =>
			"Rapid hits on a target trigger 3 shockwaves after a short delay (0.25s, 1s between pulses; bows 0.167s). "
			+ "Shockwaves inherit buffs, deal up to 90 in a 6-tile radius. Auto Rifles: 12 hits; Hand Cannons: 6 hits. "
			+ "2s cooldown after last pulse.";
		public override PerkSlotType SlotType => PerkSlotType.Major;
	}

	public sealed class AdagioPerk : Destiny2Perk
	{
		internal const int DurationTicks = 420;
		internal const float DamageMultiplier = 1.3f;
		internal const float RangeBonus = 10f;
		internal const float RpmScalar = 0.8333333f;
		public override string IconTexture => "Destiny2/Assets/Perks/Adagio";
		public override string DisplayName => "Adagio";
		public override string Description =>
			"Weapon kills grant 7s of +10 range, +30% damage, and 20% slower firing delay.";
		public override PerkSlotType SlotType => PerkSlotType.Major;
	}

	public sealed class TargetLockPerk : Destiny2Perk
	{
		internal const int HitWindowTicks = 12;
		internal const float MinHitsRatio = 0.125f;
		internal const float MaxHitsRatio = 1.1033f;
		internal const float MinDamageBonus = 0.1673f;
		internal const float MaxDamageBonus = 0.40f;
		internal static readonly float[] VisualStackThresholds = { 0.125f, 0.355f, 0.545f, 0.745f, 1.1033f };
		public override string IconTexture => "Destiny2/Assets/Perks/TargetLock";
		public override string DisplayName => "Target Lock";
		public override string Description =>
			"Successive hits within 0.2s build a ramping damage bonus after 12.5% of mag hits. "
			+ "Missing deactivates the buff. Bonus ramps from +16.73% to +40% by 110.33% hits.";
		public override PerkSlotType SlotType => PerkSlotType.Major;
	}

	public sealed class DynamicSwayReductionPerk : Destiny2Perk
	{
		internal const int MaxStacks = 10;
		internal const int HoldWindowTicks = 12;
		internal const float StabilityPerStack = 1f;
		public override string IconTexture => "Destiny2/Assets/Perks/DynamicSwayReduction";
		public override string DisplayName => "Dynamic Sway Reduction";
		public override string Description => "Each shot while holding the trigger grants +1 stability.";
		public override PerkSlotType SlotType => PerkSlotType.Major;
	}

	public sealed class FeedingFrenzyPerk : Destiny2Perk
	{
		internal const int MaxStacks = 5;
		internal const int DurationTicks = 210;
		internal static readonly float[] ReloadSpeedByStacks = { 0f, 8f, 50f, 60f, 75f, 100f };
		internal static readonly float[] ReloadTimeScalarByStacks = { 1f, 0.975f, 0.9f, 0.87f, 0.84f, 0.8f };
		public override string IconTexture => "Destiny2/Assets/Perks/FeedingFrenzy";
		public override string DisplayName => "Feeding Frenzy";
		public override string Description =>
			"Weapon kills grant Feeding Frenzy for 3.5s (max 5). 1/2/3/4/5 stacks: +8/+50/+60/+75/+100 reload speed "
			+ "and -2.5/-10/-13/-16/-20% reload duration.";
		public override PerkSlotType SlotType => PerkSlotType.Major;
	}
}
