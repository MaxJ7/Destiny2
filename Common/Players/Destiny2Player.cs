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

		public override void ResetEffects()
		{
			frenzyBuffTimer = 0;
		}

		public override void ProcessTriggers(TriggersSet triggersSet)
		{
			if (Destiny2.EditorKeybind?.JustPressed == true)
				Destiny2WeaponEditorSystem.Toggle();

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

		public override void PostUpdate()
		{
			int buffType = ModContent.BuffType<FrenzyBuff>();
			if (frenzyBuffTimer > 0)
			{
				Player.AddBuff(buffType, frenzyBuffTimer);
				int index = Player.FindBuffIndex(buffType);
				if (index != -1)
					Player.buffTime[index] = frenzyBuffTimer;
			}
			else
			{
				Player.ClearBuff(buffType);
			}
		}
	}
}
