using System;
using System.Collections.Generic;
using Destiny2.Content.Projectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Destiny2.Common.Perks
{
	public sealed class ExplosiveShadowGlobalNPC : GlobalNPC
	{
		public override bool InstancePerEntity => true;

		public int SlugStacks;
		private readonly List<int> slugIds = new List<int>();
		private readonly List<int> chainIds = new List<int>();
		private ulong lastSlugTick;
		private bool chainActive;
		private int chainTimer;
		private int chainIndex;
		private int chainDamage;
		private int chainHitDirection;
		private int chainDelay;
		private int chainCooldown;

		public bool IsExplosionActive => chainActive;
		public bool IsExplosionCoolingDown => chainCooldown > 0;

		public override void SetDefaults(NPC npc)
		{
			SlugStacks = 0;
			slugIds.Clear();
			chainIds.Clear();
			lastSlugTick = 0;
			chainActive = false;
			chainTimer = 0;
			chainIndex = 0;
			chainDamage = 0;
			chainHitDirection = 0;
			chainDelay = 0;
			chainCooldown = 0;
		}

		public int SlugCount => slugIds.Count;

		public void AddSlug(int projectileId)
		{
			if (!slugIds.Contains(projectileId))
				slugIds.Add(projectileId);
		}

		public void MarkSlugApplied()
		{
			lastSlugTick = Main.GameUpdateCount;
		}

		public void CleanupSlugs()
		{
			for (int i = slugIds.Count - 1; i >= 0; i--)
			{
				int id = slugIds[i];
				if (id < 0 || id >= Main.maxProjectiles)
				{
					slugIds.RemoveAt(i);
					continue;
				}

				Projectile proj = Main.projectile[id];
				if (!proj.active || proj.type != ModContent.ProjectileType<ExplosiveShadowSlug>())
					slugIds.RemoveAt(i);
			}

			for (int i = chainIds.Count - 1; i >= 0; i--)
			{
				int id = chainIds[i];
				if (id < 0 || id >= Main.maxProjectiles)
				{
					chainIds.RemoveAt(i);
					continue;
				}

				Projectile proj = Main.projectile[id];
				if (!proj.active || proj.type != ModContent.ProjectileType<ExplosiveShadowSlug>())
					chainIds.RemoveAt(i);
			}
		}

		public void StartExplosionChain(int explosionDamage, int hitDirection, int delayTicks)
		{
			if (chainActive || slugIds.Count == 0)
				return;

			chainIds.Clear();
			int count = Math.Min(ExplosiveShadowPerk.SlugsToExplode, slugIds.Count);
			for (int i = 0; i < count; i++)
				chainIds.Add(slugIds[i]);

			slugIds.RemoveRange(0, count);
			SlugStacks = slugIds.Count;
			chainActive = true;
			chainTimer = 0;
			chainIndex = 0;
			chainDamage = explosionDamage;
			chainHitDirection = hitDirection;
			chainDelay = Math.Max(1, delayTicks);
			chainCooldown = 0;
		}

		public override void PostAI(NPC npc)
		{
			if (chainCooldown > 0)
				chainCooldown--;

			if (!chainActive)
			{
				if (slugIds.Count > 0 && Main.GameUpdateCount - lastSlugTick >= 180)
				{
					ClearSlugs();
					SlugStacks = 0;
				}

				return;
			}

			if (!npc.active || npc.dontTakeDamage || npc.friendly)
			{
				chainActive = false;
				chainIds.Clear();
				return;
			}

			if (chainTimer > 0)
			{
				chainTimer--;
				return;
			}

			if (chainIndex >= chainIds.Count)
			{
				chainActive = false;
				chainCooldown = ExplosiveShadowPerk.ExplosionCooldownTicks;
				chainIds.Clear();
				return;
			}

			int id = chainIds[chainIndex];
			chainIndex++;
			chainTimer = chainDelay;

			Vector2 center = npc.Center;
			if (id >= 0 && id < Main.maxProjectiles)
			{
				Projectile proj = Main.projectile[id];
				if (proj.active && proj.type == ModContent.ProjectileType<ExplosiveShadowSlug>())
				{
					center = proj.Center;
					proj.Kill();
				}
			}

			ExplosiveShadowPerk.SpawnExplosionDust(center);
			npc.SimpleStrikeNPC(chainDamage, chainHitDirection, false, 0f);
			npc.AddBuff(BuffID.Stoned, ExplosiveShadowPerk.StunDurationTicks);
			SoundEngine.PlaySound(SoundID.Item14, center);
		}

		private void ClearSlugs()
		{
			for (int i = 0; i < slugIds.Count; i++)
			{
				int id = slugIds[i];
				if (id < 0 || id >= Main.maxProjectiles)
					continue;

				Projectile proj = Main.projectile[id];
				if (proj.active && proj.type == ModContent.ProjectileType<ExplosiveShadowSlug>())
					proj.Kill();
			}

			slugIds.Clear();
		}
	}
}
