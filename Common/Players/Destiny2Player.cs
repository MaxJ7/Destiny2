using System;
using Destiny2.Common.Perks;
using Destiny2.Common.UI;
using Destiny2.Common.Weapons;
using Destiny2.Content.Buffs;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace Destiny2.Common.Players
{
	public sealed class Destiny2Player : ModPlayer
	{
		private int frenzyBuffTimer;
		private int outlawBuffTimer;
		private int rapidHitBuffTimer;
		private int killClipBuffTimer;
		private int rampageBuffTimer;
		private int onslaughtBuffTimer;
		private int feedingFrenzyBuffTimer;
		private int adagioBuffTimer;
		private int targetLockBuffTimer;
		private int dynamicSwayBuffTimer;
		private int fourthTimesBuffTimer;
		private int eyesUpGuardianStacks;

		public override void ResetEffects()
		{
			frenzyBuffTimer = 0;
			outlawBuffTimer = 0;
			rapidHitBuffTimer = 0;
			killClipBuffTimer = 0;
			rampageBuffTimer = 0;
			onslaughtBuffTimer = 0;
			feedingFrenzyBuffTimer = 0;
			adagioBuffTimer = 0;
			targetLockBuffTimer = 0;
			dynamicSwayBuffTimer = 0;
			fourthTimesBuffTimer = 0;
		}

		public override void ProcessTriggers(TriggersSet triggersSet)
		{
			if (Destiny2.EditorKeybind?.JustPressed == true)
				Destiny2WeaponEditorSystem.Toggle();
			if (Destiny2.InfoKeybind?.JustPressed == true)
				Destiny2WeaponInfoSystem.Toggle();

			if (Destiny2.ReloadKeybind?.JustPressed != true)
				return;

			Item heldItem = Player.HeldItem;
			if (heldItem?.ModItem is Destiny2WeaponItem weaponItem)
				weaponItem.TryStartReload(Player);
		}

		public override void OnHitByNPC(NPC npc, Player.HurtInfo hurtInfo)
		{
			NotifyFrenzyCombat();
		}

		public override void OnHitByProjectile(Projectile proj, Player.HurtInfo hurtInfo)
		{
			NotifyFrenzyCombat();
		}

		private void NotifyFrenzyCombat()
		{
			Item heldItem = Player.HeldItem;
			if (heldItem?.ModItem is Destiny2WeaponItem weaponItem)
				weaponItem.NotifyPlayerHurt(Player);
		}

		internal void RequestFrenzyBuff(int timer)
		{
			if (timer > frenzyBuffTimer)
				frenzyBuffTimer = timer;
		}

		internal void RequestOutlawBuff(int timer)
		{
			if (timer > outlawBuffTimer)
				outlawBuffTimer = timer;
		}

		internal void RequestRapidHitBuff(int timer)
		{
			if (timer > rapidHitBuffTimer)
				rapidHitBuffTimer = timer;
		}

		internal void RequestKillClipBuff(int timer)
		{
			if (timer > killClipBuffTimer)
				killClipBuffTimer = timer;
		}

		internal void RequestRampageBuff(int timer)
		{
			if (timer > rampageBuffTimer)
				rampageBuffTimer = timer;
		}

		internal void RequestOnslaughtBuff(int timer)
		{
			if (timer > onslaughtBuffTimer)
				onslaughtBuffTimer = timer;
		}

		internal void RequestFeedingFrenzyBuff(int timer)
		{
			if (timer > feedingFrenzyBuffTimer)
				feedingFrenzyBuffTimer = timer;
		}

		internal void RequestAdagioBuff(int timer)
		{
			if (timer > adagioBuffTimer)
				adagioBuffTimer = timer;
		}

		internal void RequestTargetLockBuff(int timer)
		{
			if (timer > targetLockBuffTimer)
				targetLockBuffTimer = timer;
		}

		internal void RequestDynamicSwayBuff(int timer)
		{
			if (timer > dynamicSwayBuffTimer)
				dynamicSwayBuffTimer = timer;
		}

		internal void RequestFourthTimesBuff(int timer)
		{
			if (timer > fourthTimesBuffTimer)
				fourthTimesBuffTimer = timer;
		}

		internal bool TryConsumeEyesUpGuardianStack()
		{
			if (eyesUpGuardianStacks <= 0)
				return false;

			eyesUpGuardianStacks--;
			return true;
		}

		internal void GrantEyesUpGuardianStacks(int amount)
		{
			if (amount <= 0)
				return;

			int nextStacks = Math.Min(EyesUpGuardianPerk.MaxStacks, eyesUpGuardianStacks + amount);
			eyesUpGuardianStacks = nextStacks;
		}

		public override void PostUpdate()
		{
			ApplyTimedBuff(ModContent.BuffType<FrenzyBuff>(), frenzyBuffTimer);
			ApplyTimedBuff(ModContent.BuffType<OutlawBuff>(), outlawBuffTimer);
			ApplyTimedBuff(ModContent.BuffType<RapidHitBuff>(), rapidHitBuffTimer);
			ApplyTimedBuff(ModContent.BuffType<KillClipBuff>(), killClipBuffTimer);
			ApplyTimedBuff(ModContent.BuffType<RampageBuff>(), rampageBuffTimer);
			ApplyTimedBuff(ModContent.BuffType<OnslaughtBuff>(), onslaughtBuffTimer);
			ApplyTimedBuff(ModContent.BuffType<FeedingFrenzyBuff>(), feedingFrenzyBuffTimer);
			ApplyTimedBuff(ModContent.BuffType<AdagioBuff>(), adagioBuffTimer);
			ApplyTimedBuff(ModContent.BuffType<TargetLockBuff>(), targetLockBuffTimer);
			ApplyTimedBuff(ModContent.BuffType<DynamicSwayReductionBuff>(), dynamicSwayBuffTimer);
			ApplyTimedBuff(ModContent.BuffType<FourthTimesTheCharmBuff>(), fourthTimesBuffTimer);
		}

		private void ApplyTimedBuff(int buffType, int timer)
		{
			if (timer > 0)
			{
				Player.AddBuff(buffType, timer);
				int index = Player.FindBuffIndex(buffType);
				if (index != -1)
					Player.buffTime[index] = timer;
			}
			else
			{
				Player.ClearBuff(buffType);
			}
		}
	}
}
