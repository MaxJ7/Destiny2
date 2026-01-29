using System;
using System.Collections.Generic;
using Destiny2.Common.Players;
using Destiny2.Common.Weapons;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Destiny2.Common.Perks
{
	public sealed class Destiny2PerkProjectile : GlobalProjectile
	{
		private const float RightChoiceRicochetRange = 480f;
		private static readonly float EyesUpGuardianRicochetRange = EyesUpGuardianPerk.RicochetRangeTiles * 16f;
		private static int nextEyesUpChainId;
		private static readonly Dictionary<int, Dictionary<int, int>> EyesUpChainHits = new Dictionary<int, Dictionary<int, int>>();
		private readonly List<Destiny2Perk> perks = new List<Destiny2Perk>();
		private bool hasVorpal;
		private bool hasOutlaw;
		private bool hasRapidHit;
		private bool hasKillClip;
		private bool hasFrenzy;
		private bool hasFourthTimes;
		private bool hasRampage;
		private bool hasOnslaught;
		private bool hasKineticTremors;
		private bool hasAdagio;
		private bool hasTargetLock;
		private bool hasFeedingFrenzy;
		private bool hasRightChoice;
		private bool isRightChoiceShot;
		private bool hasEyesUpGuardian;
		private bool isEyesUpGuardianRicochet;
		private int eyesUpRicochetRemaining;
		private int eyesUpChainId;
		private Destiny2WeaponElement eyesUpElement = Destiny2WeaponElement.Kinetic;
		private Destiny2WeaponItem sourceWeaponItem;
		private Destiny2AmmoType ammoType = Destiny2AmmoType.Primary;
		private Destiny2WeaponElement rightChoiceElement = Destiny2WeaponElement.Kinetic;

		public override bool InstancePerEntity => true;

		public override void OnSpawn(Projectile projectile, IEntitySource source)
		{
			perks.Clear();
			hasVorpal = false;
			hasOutlaw = false;
			hasRapidHit = false;
			hasKillClip = false;
			hasFrenzy = false;
			hasFourthTimes = false;
			hasRampage = false;
			hasOnslaught = false;
			hasKineticTremors = false;
			hasAdagio = false;
			hasTargetLock = false;
			hasFeedingFrenzy = false;
			hasRightChoice = false;
			isRightChoiceShot = false;
			hasEyesUpGuardian = false;
			isEyesUpGuardianRicochet = false;
			eyesUpRicochetRemaining = 0;
			eyesUpChainId = 0;
			sourceWeaponItem = null;
			ammoType = Destiny2AmmoType.Primary;
			rightChoiceElement = Destiny2WeaponElement.Kinetic;

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
					else if (perk is FourthTimesTheCharmPerk)
						hasFourthTimes = true;
					else if (perk is RampagePerk)
						hasRampage = true;
					else if (perk is OnslaughtPerk)
						hasOnslaught = true;
					else if (perk is KineticTremorsPerk)
						hasKineticTremors = true;
					else if (perk is AdagioPerk)
						hasAdagio = true;
					else if (perk is TargetLockPerk)
						hasTargetLock = true;
					else if (perk is FeedingFrenzyPerk)
						hasFeedingFrenzy = true;
					else if (perk is TheRightChoiceFramePerk)
						hasRightChoice = true;
					else if (perk is EyesUpGuardianPerk)
						hasEyesUpGuardian = true;
				}

				if (hasRightChoice && weaponItem is AutoRifleWeaponItem && weaponItem.TryConsumeRightChoiceShot())
				{
					isRightChoiceShot = true;
					rightChoiceElement = weaponItem.WeaponElement;
				}

				if (hasEyesUpGuardian)
					eyesUpElement = weaponItem.WeaponElement;
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

				if (hasRampage)
				{
					float rampageMultiplier = sourceWeaponItem.GetRampageMultiplier();
					if (rampageMultiplier > 1f)
						multiplier *= rampageMultiplier;
				}

				if (hasAdagio && sourceWeaponItem.IsAdagioActive)
					multiplier *= AdagioPerk.DamageMultiplier;

				if (hasTargetLock)
				{
					float bonus = sourceWeaponItem.RegisterTargetLockHit(target);
					if (bonus > 0f)
						multiplier *= 1f + bonus;
				}
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

			if (isRightChoiceShot)
				TryRicochet(projectile, target, damageDone, rightChoiceElement);

			if (isEyesUpGuardianRicochet)
				HandleEyesUpGuardianRicochet(projectile, target, damageDone);
			else if (hasEyesUpGuardian)
				TryStartEyesUpGuardianChain(projectile, target, damageDone);

			if (sourceWeaponItem == null)
				return;

			if (hasKineticTremors)
				sourceWeaponItem.RegisterKineticTremorsHit(projectile, target, damageDone);

			if (!hasOutlaw && !hasRapidHit && !hasKillClip && !hasFrenzy && !hasFourthTimes && !hasRampage
				&& !hasOnslaught && !hasAdagio && !hasFeedingFrenzy)
				return;

			Player owner = GetOwner(projectile.owner);
			sourceWeaponItem.NotifyProjectileHit(owner, target, hit, damageDone, hasOutlaw, hasRapidHit, hasKillClip, hasFrenzy, hasFourthTimes, hasRampage,
				hasOnslaught, hasAdagio, hasFeedingFrenzy);
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

		private static void TryRicochet(Projectile projectile, NPC target, int damageDone, Destiny2WeaponElement element)
		{
			if (projectile == null || target == null)
				return;

			NPC ricochetTarget = FindRicochetTarget(target, target.Center, RightChoiceRicochetRange);
			if (ricochetTarget == null)
				return;

			Vector2 direction = (ricochetTarget.Center - target.Center).SafeNormalize(Vector2.UnitX);
			float offsetDistance = Math.Max(target.width, target.height) * 0.5f + 6f;
			Vector2 spawnPos = target.Center + direction * offsetDistance;
			int ricochetDamage = Math.Max(1, (int)Math.Round(damageDone * TheRightChoiceFramePerk.RicochetDamageMultiplier));

			int projId = Projectile.NewProjectile(projectile.GetSource_FromThis(), spawnPos, direction, projectile.type, ricochetDamage, projectile.knockBack, projectile.owner);
			if (projId < 0 || projId >= Main.maxProjectiles)
				return;

			Projectile ricochet = Main.projectile[projId];
			ricochet.ai[0] = (int)element;
			ricochet.DamageType = element.GetDamageClass();
			ricochet.netUpdate = true;
		}

		private static NPC FindRicochetTarget(NPC current, Vector2 origin, float maxRange)
		{
			NPC best = null;
			float bestDistSq = maxRange * maxRange;

			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC npc = Main.npc[i];
				if (!npc.CanBeChasedBy())
					continue;
				if (current != null && npc.whoAmI == current.whoAmI)
					continue;

				float distSq = Vector2.DistanceSquared(origin, npc.Center);
				if (distSq >= bestDistSq)
					continue;

				best = npc;
				bestDistSq = distSq;
			}

			return best;
		}

		private void TryStartEyesUpGuardianChain(Projectile projectile, NPC target, int damageDone)
		{
			if (sourceWeaponItem == null)
				return;

			Player owner = GetOwner(projectile.owner);
			if (owner == null)
				return;

			Destiny2Player modPlayer = owner.GetModPlayer<Destiny2Player>();
			if (modPlayer == null || !modPlayer.TryConsumeEyesUpGuardianStack())
				return;

			eyesUpChainId = CreateEyesUpChain();
			RegisterEyesUpHit(eyesUpChainId, target.whoAmI);
			if (!SpawnEyesUpRicochet(projectile, target, damageDone, EyesUpGuardianPerk.RicochetCount))
				RemoveEyesUpChain(eyesUpChainId);
		}

		private void HandleEyesUpGuardianRicochet(Projectile projectile, NPC target, int damageDone)
		{
			if (eyesUpChainId == 0)
				return;

			RegisterEyesUpHit(eyesUpChainId, target.whoAmI);
			if (eyesUpRicochetRemaining <= 1)
			{
				RemoveEyesUpChain(eyesUpChainId);
				return;
			}

			if (!SpawnEyesUpRicochet(projectile, target, damageDone, eyesUpRicochetRemaining - 1))
				RemoveEyesUpChain(eyesUpChainId);
		}

		private bool SpawnEyesUpRicochet(Projectile projectile, NPC target, int damageDone, int remainingRicochets)
		{
			if (remainingRicochets <= 0)
				return false;

			NPC ricochetTarget = FindEyesUpTarget(target, target.Center, EyesUpGuardianRicochetRange, eyesUpChainId);
			if (ricochetTarget == null)
				return false;

			Vector2 direction = (ricochetTarget.Center - target.Center).SafeNormalize(Vector2.UnitX);
			float offsetDistance = Math.Max(target.width, target.height) * 0.5f + 6f;
			Vector2 spawnPos = target.Center + direction * offsetDistance;

			int projId = Projectile.NewProjectile(projectile.GetSource_FromThis(), spawnPos, direction, projectile.type, damageDone, projectile.knockBack, projectile.owner);
			if (projId < 0 || projId >= Main.maxProjectiles)
				return false;

			Projectile ricochet = Main.projectile[projId];
			ricochet.ai[0] = (int)eyesUpElement;
			ricochet.DamageType = eyesUpElement.GetDamageClass();
			ricochet.netUpdate = true;

			Destiny2PerkProjectile data = ricochet.GetGlobalProjectile<Destiny2PerkProjectile>();
			data.isEyesUpGuardianRicochet = true;
			data.eyesUpRicochetRemaining = remainingRicochets;
			data.eyesUpChainId = eyesUpChainId;
			data.eyesUpElement = eyesUpElement;

			return true;
		}

		private static NPC FindEyesUpTarget(NPC current, Vector2 origin, float maxRange, int chainId)
		{
			NPC best = null;
			float bestDistSq = maxRange * maxRange;

			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC npc = Main.npc[i];
				if (!npc.CanBeChasedBy())
					continue;
				if (current != null && npc.whoAmI == current.whoAmI)
					continue;
				if (HasEyesUpReachedHitLimit(chainId, npc.whoAmI))
					continue;

				float distSq = Vector2.DistanceSquared(origin, npc.Center);
				if (distSq >= bestDistSq)
					continue;

				best = npc;
				bestDistSq = distSq;
			}

			return best;
		}

		private static int CreateEyesUpChain()
		{
			int id = ++nextEyesUpChainId;
			EyesUpChainHits[id] = new Dictionary<int, int>();
			return id;
		}

		private static void RegisterEyesUpHit(int chainId, int npcId)
		{
			if (chainId == 0 || npcId < 0)
				return;

			if (!EyesUpChainHits.TryGetValue(chainId, out Dictionary<int, int> hits))
				return;

			if (!hits.TryGetValue(npcId, out int count))
				count = 0;

			hits[npcId] = count + 1;
		}

		private static bool HasEyesUpReachedHitLimit(int chainId, int npcId)
		{
			if (chainId == 0 || npcId < 0)
				return false;

			if (!EyesUpChainHits.TryGetValue(chainId, out Dictionary<int, int> hits))
				return false;

			if (!hits.TryGetValue(npcId, out int count))
				return false;

			return count >= EyesUpGuardianPerk.MaxHitsPerTarget;
		}

		private static void RemoveEyesUpChain(int chainId)
		{
			if (chainId == 0)
				return;

			EyesUpChainHits.Remove(chainId);
		}
	}
}
