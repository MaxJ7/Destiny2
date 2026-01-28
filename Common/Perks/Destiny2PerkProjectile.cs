using System.Collections.Generic;
using Destiny2.Common.Weapons;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Destiny2.Common.Perks
{
	public sealed class Destiny2PerkProjectile : GlobalProjectile
	{
		private readonly List<Destiny2Perk> perks = new List<Destiny2Perk>();
		private bool hasVorpal;
		private bool hasOutlaw;
		private bool hasRapidHit;
		private bool hasKillClip;
		private bool hasFrenzy;
		private Destiny2WeaponItem sourceWeaponItem;
		private Destiny2AmmoType ammoType = Destiny2AmmoType.Primary;

		public override bool InstancePerEntity => true;

		public override void OnSpawn(Projectile projectile, IEntitySource source)
		{
			perks.Clear();
			hasVorpal = false;
			hasOutlaw = false;
			hasRapidHit = false;
			hasKillClip = false;
			hasFrenzy = false;
			sourceWeaponItem = null;
			ammoType = Destiny2AmmoType.Primary;

			Item sourceItem = GetSourceItem(source);
			if (sourceItem?.ModItem is Destiny2WeaponItem weaponItem)
			{
				sourceWeaponItem = weaponItem;
				ammoType = weaponItem.AmmoType;
				foreach (Destiny2Perk perk in weaponItem.GetPerks())
				{
					perks.Add(perk);
					if (perk is VorpalWeaponPerk)
						hasVorpal = true;
					else if (perk is OutlawPerk)
						hasOutlaw = true;
					else if (perk is RapidHitPerk)
						hasRapidHit = true;
					else if (perk is KillClipPerk)
						hasKillClip = true;
					else if (perk is FrenzyPerk)
						hasFrenzy = true;
				}
			}
		}

		public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
		{
			float multiplier = 1f;
			if (hasVorpal && IsBossTarget(target))
				multiplier *= GetVorpalMultiplier(ammoType);

			if (sourceWeaponItem != null)
			{
				if (hasKillClip && sourceWeaponItem.IsKillClipActive)
					multiplier *= KillClipPerk.DamageMultiplier;

				if (hasFrenzy && sourceWeaponItem.IsFrenzyActive)
					multiplier *= FrenzyPerk.DamageMultiplier;
			}

			if (multiplier > 1f)
				modifiers.FinalDamage *= multiplier;
		}

		public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (perks.Count > 0)
			{
				for (int i = 0; i < perks.Count; i++)
					perks[i].OnProjectileHitNPC(projectile, target, hit, damageDone);
			}

			if (sourceWeaponItem == null)
				return;

			if (!hasOutlaw && !hasRapidHit && !hasKillClip && !hasFrenzy)
				return;

			Player owner = GetOwner(projectile.owner);
			sourceWeaponItem.NotifyProjectileHit(owner, target, hit, damageDone, hasOutlaw, hasRapidHit, hasKillClip, hasFrenzy);
		}

		private static Item GetSourceItem(IEntitySource source)
		{
			if (source is EntitySource_ItemUse itemUse)
				return itemUse.Item;

			return null;
		}

		private static bool IsBossTarget(NPC target)
		{
			return target.boss || NPCID.Sets.ShouldBeCountedAsBoss[target.type];
		}

		private static Player GetOwner(int owner)
		{
			if (owner < 0 || owner >= Main.maxPlayers)
				return null;

			Player player = Main.player[owner];
			return player.active ? player : null;
		}

		private static float GetVorpalMultiplier(Destiny2AmmoType ammoType)
		{
			return ammoType switch
			{
				Destiny2AmmoType.Primary => 1.15f,
				Destiny2AmmoType.Special => 1.10f,
				Destiny2AmmoType.Heavy => 1.05f,
				_ => 1f
			};
		}
	}
}
