using System;
using System.Collections.Generic;
using System.IO;
using Destiny2.Common.Items;
using Destiny2.Common.Players;
using Destiny2.Common.Perks;
using Destiny2.Common.Rarities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI.Chat;

namespace Destiny2.Common.Weapons
{
	public struct Destiny2WeaponStats
	{
		public float Range;
		public float Stability;
		public float ReloadSpeed;
		public int RoundsPerMinute;
		public int Magazine;

		public Destiny2WeaponStats(float range, float stability, float reloadSpeed, int roundsPerMinute, int magazine)
		{
			Range = range;
			Stability = stability;
			ReloadSpeed = reloadSpeed;
			RoundsPerMinute = roundsPerMinute;
			Magazine = magazine;
		}
	}

	public abstract class Destiny2WeaponItem : ModItem
	{
		private const string FrameTooltipPrefix = "Destiny2Frame_";
		private const string PerkSlotTooltipPrefix = "Destiny2PerkSlot_";
		private const string CatalystTooltipPrefix = "Destiny2Catalyst_";
		private const string StatsTooltipPrefix = "Destiny2Stats_";
		private const string ElementTooltipPrefix = "Destiny2Element_";
		private const int TicksPerSecond = 60;
		private static readonly string[] PerkSlotNames = new[]
		{
			"Barrel",
			"Magazine",
			"Major Perk",
			"Major Perk"
		};

		private List<string> perkKeys = new List<string>();
		private bool perksInitialized;
		private bool hasBeenPickedUp;
		private bool hasCustomStats;
		private Destiny2WeaponStats customStats;
		private string framePerkKey;
		private string catalystPerkKey;
		private int catalystItemType;
		private int currentMagazine;
		private bool magazineInitialized;
		private int reloadTimer;
		private int reloadTimerMax;
		private bool isReloading;
		private int outlawTimer;
		private int rapidHitTimer;
		private int rapidHitStacks;
		private int killClipWindowTimer;
		private int killClipTimer;
		private bool killClipPending;
		private int frenzyTimer;
		private int frenzyCombatTimer;
		private int frenzyCombatGraceTimer;

		public abstract Destiny2WeaponStats BaseStats { get; }
		public virtual Destiny2AmmoType AmmoType => Destiny2AmmoType.Primary;
		public virtual Destiny2WeaponElement WeaponElement => Destiny2WeaponElement.Kinetic;

		protected override bool CloneNewInstances => true;

		protected virtual int PerkRollCount => 4;

		public IReadOnlyList<string> PerkKeys => perkKeys;
		public int PerkSlotCount => Math.Max(1, PerkRollCount);
		public bool HasFrame => !string.IsNullOrWhiteSpace(framePerkKey);
		public bool HasCatalyst => !string.IsNullOrWhiteSpace(catalystPerkKey);
		public bool HasCatalystSlot => Item.rare == ModContent.RarityType<ExoticRarity>();

		public override ModItem Clone(Item newEntity)
		{
			Destiny2WeaponItem clone = (Destiny2WeaponItem)base.Clone(newEntity);
			clone.perksInitialized = perksInitialized;
			clone.hasBeenPickedUp = hasBeenPickedUp;
			clone.perkKeys = new List<string>(perkKeys);
			clone.hasCustomStats = hasCustomStats;
			clone.customStats = customStats;
			clone.framePerkKey = framePerkKey;
			clone.catalystPerkKey = catalystPerkKey;
			clone.catalystItemType = catalystItemType;
			clone.currentMagazine = currentMagazine;
			clone.magazineInitialized = magazineInitialized;
			clone.reloadTimer = reloadTimer;
			clone.reloadTimerMax = reloadTimerMax;
			clone.isReloading = isReloading;
			clone.outlawTimer = outlawTimer;
			clone.rapidHitTimer = rapidHitTimer;
			clone.rapidHitStacks = rapidHitStacks;
			clone.killClipWindowTimer = killClipWindowTimer;
			clone.killClipTimer = killClipTimer;
			clone.killClipPending = killClipPending;
			clone.frenzyTimer = frenzyTimer;
			clone.frenzyCombatTimer = frenzyCombatTimer;
			clone.frenzyCombatGraceTimer = frenzyCombatGraceTimer;
			return clone;
		}

		public override void SaveData(TagCompound tag)
		{
			if (perkKeys.Count > 0)
				tag["perkKeys"] = perkKeys;

			tag["currentMagazine"] = currentMagazine;
			tag["reloadTimer"] = reloadTimer;
			tag["reloadTimerMax"] = reloadTimerMax;
			tag["isReloading"] = isReloading;
			tag["hasCustomStats"] = hasCustomStats;
			if (hasCustomStats)
			{
				TagCompound statsTag = new TagCompound
				{
					["range"] = customStats.Range,
					["stability"] = customStats.Stability,
					["reloadSpeed"] = customStats.ReloadSpeed,
					["rpm"] = customStats.RoundsPerMinute,
					["magazine"] = customStats.Magazine
				};
				tag["customStats"] = statsTag;
			}

			if (HasFrame)
				tag["framePerkKey"] = framePerkKey;

			if (HasCatalyst)
			{
				tag["catalystPerkKey"] = catalystPerkKey;
				tag["catalystItemType"] = catalystItemType;
			}
		}

		public override void LoadData(TagCompound tag)
		{
			perkKeys.Clear();
			if (tag.ContainsKey("perkKeys"))
				perkKeys.AddRange(tag.GetList<string>("perkKeys"));

			currentMagazine = tag.GetInt("currentMagazine");
			reloadTimer = tag.GetInt("reloadTimer");
			reloadTimerMax = tag.GetInt("reloadTimerMax");
			isReloading = tag.ContainsKey("isReloading") && tag.GetBool("isReloading");
			hasCustomStats = tag.ContainsKey("hasCustomStats") && tag.GetBool("hasCustomStats");
			if (hasCustomStats && tag.ContainsKey("customStats"))
			{
				TagCompound statsTag = tag.GetCompound("customStats");
				customStats = new Destiny2WeaponStats(
					statsTag.GetFloat("range"),
					statsTag.GetFloat("stability"),
					statsTag.GetFloat("reloadSpeed"),
					statsTag.GetInt("rpm"),
					statsTag.GetInt("magazine"));
			}

			if (tag.ContainsKey("framePerkKey"))
				framePerkKey = tag.GetString("framePerkKey");
			else
				framePerkKey = null;

			if (tag.ContainsKey("catalystPerkKey"))
				catalystPerkKey = tag.GetString("catalystPerkKey");
			else
				catalystPerkKey = null;

			if (tag.ContainsKey("catalystItemType"))
				catalystItemType = tag.GetInt("catalystItemType");
			else
				catalystItemType = 0;

			bool hasPerkData = perkKeys.Count > 0
				|| !string.IsNullOrWhiteSpace(framePerkKey)
				|| !string.IsNullOrWhiteSpace(catalystPerkKey);
			perksInitialized = hasPerkData;
			magazineInitialized = hasPerkData;
		}

		public override void NetSend(BinaryWriter writer)
		{
			writer.Write((byte)perkKeys.Count);
			for (int i = 0; i < perkKeys.Count; i++)
				writer.Write(perkKeys[i]);
			writer.Write((short)currentMagazine);
			writer.Write((short)reloadTimer);
			writer.Write((short)reloadTimerMax);
			writer.Write(isReloading);
			writer.Write(hasCustomStats);
			if (hasCustomStats)
			{
				writer.Write(customStats.Range);
				writer.Write(customStats.Stability);
				writer.Write(customStats.ReloadSpeed);
				writer.Write(customStats.RoundsPerMinute);
				writer.Write(customStats.Magazine);
			}

			writer.Write(HasFrame);
			if (HasFrame)
				writer.Write(framePerkKey ?? string.Empty);

			writer.Write(HasCatalyst);
			if (HasCatalyst)
			{
				writer.Write(catalystPerkKey ?? string.Empty);
				writer.Write(catalystItemType);
			}
		}

		public override void NetReceive(BinaryReader reader)
		{
			perkKeys.Clear();
			int count = reader.ReadByte();
			for (int i = 0; i < count; i++)
				perkKeys.Add(reader.ReadString());

			currentMagazine = reader.ReadInt16();
			reloadTimer = reader.ReadInt16();
			reloadTimerMax = reader.ReadInt16();
			isReloading = reader.ReadBoolean();
			hasCustomStats = reader.ReadBoolean();
			if (hasCustomStats)
			{
				customStats.Range = reader.ReadSingle();
				customStats.Stability = reader.ReadSingle();
				customStats.ReloadSpeed = reader.ReadSingle();
				customStats.RoundsPerMinute = reader.ReadInt32();
				customStats.Magazine = reader.ReadInt32();
			}

			if (reader.ReadBoolean())
				framePerkKey = reader.ReadString();
			else
				framePerkKey = null;

			if (reader.ReadBoolean())
			{
				catalystPerkKey = reader.ReadString();
				catalystItemType = reader.ReadInt32();
			}
			else
			{
				catalystPerkKey = null;
				catalystItemType = 0;
			}

			bool hasPerkData = perkKeys.Count > 0
				|| !string.IsNullOrWhiteSpace(framePerkKey)
				|| !string.IsNullOrWhiteSpace(catalystPerkKey);
			perksInitialized = hasPerkData;
			magazineInitialized = hasPerkData;
		}

		public virtual Destiny2WeaponStats GetStats()
		{
			Destiny2WeaponStats stats = hasCustomStats ? customStats : BaseStats;
			foreach (Destiny2Perk perk in GetPerks())
				perk.ModifyStats(ref stats);
			ApplyActivePerkStats(ref stats);
			return stats;
		}

		public Destiny2WeaponStats GetEditableStats()
		{
			return hasCustomStats ? customStats : BaseStats;
		}

		public string FramePerkKey => framePerkKey;
		public string CatalystPerkKey => catalystPerkKey;

		public void ApplyCustomStats(Destiny2WeaponStats stats)
		{
			customStats = stats;
			hasCustomStats = true;
			SyncMagazineAfterStatChange();
		}

		public void ClearCustomStats()
		{
			hasCustomStats = false;
			customStats = default;
			SyncMagazineAfterStatChange();
		}

		public virtual float GetFalloffTiles()
		{
			return 0f;
		}

		public virtual float GetMaxFalloffTiles()
		{
			return 0f;
		}

		public virtual float GetReloadSeconds()
		{
			return 0f;
		}

		public int CurrentMagazine => currentMagazine;
		public int MagazineSize => GetStats().Magazine;
		public bool HasCustomStats => hasCustomStats;
		public bool IsReloading => isReloading;
		public int ReloadTimer => reloadTimer;
		public int ReloadTimerMax => reloadTimerMax;
		internal bool IsKillClipActive => killClipTimer > 0;
		internal bool IsFrenzyActive => frenzyTimer > 0;

		public override bool CanUseItem(Player player)
		{
			int magazineSize = GetStats().Magazine;
			if (magazineSize <= 0)
				return base.CanUseItem(player);

			if (isReloading)
				return false;

			if (currentMagazine <= 0)
				return false;

			return base.CanUseItem(player);
		}

		public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
		{
			if (WeaponElement == Destiny2WeaponElement.Kinetic)
				damage *= 1.05f;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
		{
			int magazineSize = GetStats().Magazine;
			if (magazineSize > 0)
			{
				if (isReloading || currentMagazine <= 0)
					return false;

				currentMagazine--;
			}

			return true;
		}

		public override void HoldItem(Player player)
		{
			MarkPickedUp();
			EnsurePerksRolled();
			UpdatePerkTimers(player);
			UpdateReload(player);
			UpdateUseTimeFromStats();
		}

		public override void UpdateInventory(Player player)
		{
			MarkPickedUp();
			EnsurePerksRolled();
			UpdatePerkTimers(player);
			UpdateReload(player);
			UpdateUseTimeFromStats();
		}

		public override void ModifyTooltips(List<TooltipLine> tooltips)
		{
			tooltips.RemoveAll(line => line.Mod == "Terraria" && VanillaStatLines.Contains(line.Name));

			Destiny2WeaponStats stats = GetStats();
			Color headerColor = new Color(255, 212, 89);

			tooltips.Add(new TooltipLine(Mod, "Destiny2StatsHeader", "Destiny 2 Stats")
			{
				OverrideColor = headerColor
			});
			Destiny2WeaponElement element = WeaponElement;
			tooltips.Add(new TooltipLine(Mod, ElementTooltipPrefix + element, $"Element: {element}"));
			Destiny2AmmoType ammoType = AmmoType;
			tooltips.Add(new TooltipLine(Mod, "Destiny2AmmoType", $"Ammo Type: {ammoType}"));
			tooltips.Add(new TooltipLine(Mod, StatsTooltipPrefix + "Damage", $"Damage: {Item.damage}"));
			tooltips.Add(new TooltipLine(Mod, StatsTooltipPrefix + "Range", $"Range: {stats.Range:0}"));
			float effectiveRange = GetFalloffTiles();
			if (effectiveRange > 0f)
				tooltips.Add(new TooltipLine(Mod, StatsTooltipPrefix + "EffectiveRange", $"Effective Range: {effectiveRange:0} tiles"));
			tooltips.Add(new TooltipLine(Mod, StatsTooltipPrefix + "Stability", $"Stability: {stats.Stability:0}"));
			tooltips.Add(new TooltipLine(Mod, StatsTooltipPrefix + "Reload", $"Reload: {stats.ReloadSpeed:0}"));
			float reloadSeconds = GetReloadSeconds();
			if (reloadSeconds > 0f)
				tooltips.Add(new TooltipLine(Mod, StatsTooltipPrefix + "ReloadTime", $"Reload Time: {reloadSeconds:0.0}s"));
			tooltips.Add(new TooltipLine(Mod, StatsTooltipPrefix + "RPM", $"RPM: {stats.RoundsPerMinute}"));
			tooltips.Add(new TooltipLine(Mod, StatsTooltipPrefix + "Magazine", $"Magazine: {stats.Magazine}"));

			tooltips.Add(new TooltipLine(Mod, "Destiny2FrameHeader", "Weapon Frame")
			{
				OverrideColor = headerColor
			});

			string frameLabel = "Frame: --";
			string frameKey = "Empty";
			if (!string.IsNullOrWhiteSpace(framePerkKey) && Destiny2PerkSystem.TryGet(framePerkKey, out Destiny2Perk framePerk))
			{
				frameLabel = $"Frame: {framePerk.DisplayName}";
				frameKey = framePerk.Key;
			}

			tooltips.Add(new TooltipLine(Mod, FrameTooltipPrefix + frameKey, frameLabel));

			tooltips.Add(new TooltipLine(Mod, "Destiny2PerksHeader", "Weapon Perks")
			{
				OverrideColor = headerColor
			});

			for (int i = 0; i < PerkSlotCount; i++)
			{
				string slotName = i < PerkSlotNames.Length ? PerkSlotNames[i] : $"Slot {i + 1}";
				string slotLabel = $"{slotName}: --";
				Destiny2Perk perk = null;
				if (i < perkKeys.Count && Destiny2PerkSystem.TryGet(perkKeys[i], out Destiny2Perk slotPerk))
				{
					perk = slotPerk;
					slotLabel = $"{slotName}: {perk.DisplayName}";
				}

				string key = i < perkKeys.Count ? perkKeys[i] : $"Empty_{i}";
				tooltips.Add(new TooltipLine(Mod, PerkSlotTooltipPrefix + key, slotLabel));

				if (perk != null && !string.IsNullOrWhiteSpace(perk.Description))
					tooltips.Add(new TooltipLine(Mod, $"Destiny2PerkDesc_{key}_{i}", perk.Description));
			}

			tooltips.Add(new TooltipLine(Mod, "Destiny2ModsHeader", "Weapon Mods")
			{
				OverrideColor = headerColor
			});
			tooltips.Add(new TooltipLine(Mod, "Destiny2ModsSlot", "Mod Slot: --"));

			if (HasCatalystSlot)
			{
				string catalystLabel = "Catalyst: Empty";
				string catalystKey = "Empty";
				if (!string.IsNullOrWhiteSpace(catalystPerkKey) && Destiny2PerkSystem.TryGet(catalystPerkKey, out Destiny2Perk catalystPerk))
				{
					catalystLabel = $"Catalyst: {catalystPerk.DisplayName}";
					catalystKey = catalystPerk.Key;
				}

				tooltips.Add(new TooltipLine(Mod, CatalystTooltipPrefix + catalystKey, catalystLabel));
			}
			else
			{
				tooltips.Add(new TooltipLine(Mod, "Destiny2CatalystLocked", "Catalyst: Locked"));
			}
		}

		public override bool PreDrawTooltipLine(DrawableTooltipLine line, ref int yOffset)
		{
			if (line.Mod != Mod.Name)
				return true;

			string perkKey = null;
			if (line.Name.StartsWith(FrameTooltipPrefix, StringComparison.Ordinal))
				perkKey = line.Name.Substring(FrameTooltipPrefix.Length);
			else if (line.Name.StartsWith(PerkSlotTooltipPrefix, StringComparison.Ordinal))
				perkKey = line.Name.Substring(PerkSlotTooltipPrefix.Length);
			else if (line.Name.StartsWith(CatalystTooltipPrefix, StringComparison.Ordinal))
				perkKey = line.Name.Substring(CatalystTooltipPrefix.Length);
			else if (line.Name == "Destiny2AmmoType")
			{
				Destiny2AmmoType ammoType = AmmoType;
				Texture2D ammoIcon = ModContent.Request<Texture2D>(ammoType.GetIconTexture()).Value;
				Vector2 ammoIconPos = new Vector2(line.X, line.Y);
				Vector2 ammoTextPos = new Vector2(line.X + ammoIcon.Width + 6f, line.Y);

				Main.spriteBatch.Draw(ammoIcon, ammoIconPos, null, line.OverrideColor ?? line.Color);
				ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, line.Font, line.Text, ammoTextPos, line.Color, line.Rotation, line.Origin, line.BaseScale);

				return false;
			}
			else if (line.Name.StartsWith(ElementTooltipPrefix, StringComparison.Ordinal))
			{
				string elementText = line.Name.Substring(ElementTooltipPrefix.Length);
				if (!Enum.TryParse(elementText, out Destiny2WeaponElement element))
					return true;

				Texture2D elementIcon = ModContent.Request<Texture2D>(element.GetIconTexture()).Value;
				Vector2 elementIconPos = new Vector2(line.X, line.Y);
				Vector2 elementTextPos = new Vector2(line.X + elementIcon.Width + 6f, line.Y);

				Main.spriteBatch.Draw(elementIcon, elementIconPos, null, line.OverrideColor ?? line.Color);
				ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, line.Font, line.Text, elementTextPos, line.Color, line.Rotation, line.Origin, line.BaseScale);

				return false;
			}
			else
				return true;

			if (!Destiny2PerkSystem.TryGet(perkKey, out Destiny2Perk perk) || string.IsNullOrWhiteSpace(perk.IconTexture))
				return true;

			Texture2D icon = ModContent.Request<Texture2D>(perk.IconTexture).Value;
			Vector2 iconSize = icon.Size();
			Vector2 perkIconPos = new Vector2(line.X, line.Y);
			Vector2 perkTextPos = new Vector2(line.X + iconSize.X + 6f, line.Y);

			Main.spriteBatch.Draw(icon, perkIconPos, null, line.OverrideColor ?? line.Color);
			ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, line.Font, line.Text, perkTextPos, line.Color, line.Rotation, line.Origin, line.BaseScale);

			return false;
		}

		private static readonly HashSet<string> VanillaStatLines = new HashSet<string>(StringComparer.Ordinal)
		{
			"Damage",
			"CritChance",
			"Knockback",
			"Speed",
			"UseMana",
			"AxePower",
			"PickPower",
			"HammerPower",
			"TileBoost",
			"ArmorPenetration",
			"HealLife",
			"HealMana",
			"Defense",
			"Ammo"
		};

		protected static float CalculateFalloffTiles(float range, float minTiles, float maxTiles, float tilesAtFifty)
		{
			if (maxTiles <= minTiles)
				return minTiles;

			float clampedRange = Math.Clamp(range, 0f, 100f);
			float ratio = clampedRange / 100f;
			float target = Math.Clamp(tilesAtFifty, minTiles, maxTiles);
			float baseSpan = maxTiles - minTiles;
			float targetSpan = target - minTiles;
			float exponent = 1f;

			if (targetSpan > 0f && baseSpan > 0f)
			{
				float normalizedTarget = targetSpan / baseSpan;
				exponent = (float)(Math.Log(normalizedTarget) / Math.Log(0.5f));
			}

			float scaled = (float)Math.Pow(ratio, exponent);
			return minTiles + baseSpan * scaled;
		}

		protected static float CalculateScaledValue(float stat, float valueAtZero, float valueAtHundred, float valueAtFifty)
		{
			float clampedStat = Math.Clamp(stat, 0f, 100f);
			float ratio = clampedStat / 100f;
			float baseSpan = valueAtHundred - valueAtZero;

			if (Math.Abs(baseSpan) < 0.0001f)
				return valueAtZero;

			float targetSpan = valueAtFifty - valueAtZero;
			float normalizedTarget = targetSpan / baseSpan;
			float exponent = 1f;

			if (normalizedTarget > 0f && normalizedTarget < 1f)
				exponent = (float)(Math.Log(normalizedTarget) / Math.Log(0.5f));

			float scaled = (float)Math.Pow(ratio, exponent);
			return valueAtZero + baseSpan * scaled;
		}

		private void InitializeMagazine()
		{
			int magazineSize = GetStats().Magazine;
			if (magazineSize > 0)
				currentMagazine = magazineSize;
			else
				currentMagazine = 0;

			magazineInitialized = true;
		}

		public void TryStartReload(Player player)
		{
			int magazineSize = GetStats().Magazine;
			if (magazineSize <= 0)
				return;

			if (isReloading)
				return;

			if (currentMagazine >= magazineSize)
				return;

			StartReload(player);
		}

		private void StartReload(Player player)
		{
			if (killClipWindowTimer > 0)
			{
				killClipPending = true;
				killClipWindowTimer = 0;
			}

			float reloadSeconds = GetReloadSeconds();
			if (reloadSeconds <= 0f)
			{
				currentMagazine = GetStats().Magazine;
				reloadTimer = 0;
				reloadTimerMax = 0;
				isReloading = false;
				if (killClipPending)
				{
					killClipPending = false;
					ActivateKillClip(player);
				}
				return;
			}

			reloadTimer = (int)Math.Ceiling(reloadSeconds * TicksPerSecond);
			reloadTimerMax = reloadTimer;
			isReloading = true;
		}

		private void UpdateReload(Player player)
		{
			if (!isReloading)
				return;

			if (reloadTimer > 0)
				reloadTimer--;

			if (reloadTimer <= 0)
			{
				isReloading = false;
				reloadTimer = 0;
				reloadTimerMax = 0;
				currentMagazine = GetStats().Magazine;
				if (killClipPending)
				{
					killClipPending = false;
					ActivateKillClip(player);
				}
			}
		}

		private void SyncMagazineAfterStatChange()
		{
			int magazineSize = GetStats().Magazine;
			if (magazineSize <= 0)
			{
				currentMagazine = 0;
				return;
			}

			currentMagazine = Math.Clamp(currentMagazine, 0, magazineSize);
		}

		private void UpdateUseTimeFromStats()
		{
			int rpm = GetStats().RoundsPerMinute;
			if (rpm <= 0)
				return;

			int useTime = Math.Clamp((int)Math.Round(3600f / rpm), 1, 3600);
			Item.useTime = useTime;
			Item.useAnimation = useTime;
		}

		private void ApplyActivePerkStats(ref Destiny2WeaponStats stats)
		{
			if (outlawTimer > 0)
				stats.ReloadSpeed += OutlawPerk.ReloadSpeedBonus;

			if (rapidHitTimer > 0 && rapidHitStacks > 0)
			{
				int stacks = Math.Clamp(rapidHitStacks, 0, RapidHitPerk.MaxStacks);
				stats.Stability += RapidHitPerk.StabilityBonusByStacks[stacks];
				stats.ReloadSpeed += RapidHitPerk.ReloadSpeedBonusByStacks[stacks];
			}

			if (frenzyTimer > 0)
				stats.ReloadSpeed += FrenzyPerk.ReloadSpeedBonus;
		}

		protected float GetReloadSpeedTimeScalar()
		{
			float scalar = 1f;
			if (outlawTimer > 0)
				scalar *= OutlawPerk.ReloadTimeScalar;

			if (rapidHitTimer > 0 && rapidHitStacks > 0)
			{
				int stacks = Math.Clamp(rapidHitStacks, 0, RapidHitPerk.MaxStacks);
				scalar *= RapidHitPerk.ReloadTimeScalarByStacks[stacks];
			}

			return scalar;
		}

		private void UpdatePerkTimers(Player player)
		{
			bool outlawWasActive = outlawTimer > 0;
			bool rapidHitWasActive = rapidHitTimer > 0 && rapidHitStacks > 0;
			bool killClipWasActive = killClipTimer > 0;
			bool frenzyWasActive = frenzyTimer > 0;

			if (outlawTimer > 0)
				outlawTimer--;

			if (outlawWasActive && outlawTimer <= 0)
				SendPerkDebug(player, "Outlaw expired");

			if (rapidHitTimer > 0)
			{
				rapidHitTimer--;
				if (rapidHitTimer <= 0)
					rapidHitStacks = 0;
			}

			if (rapidHitWasActive && rapidHitTimer <= 0)
				SendPerkDebug(player, "Rapid Hit expired");

			if (killClipWindowTimer > 0)
				killClipWindowTimer--;

			if (killClipTimer > 0)
				killClipTimer--;

			if (killClipWasActive && killClipTimer <= 0)
				SendPerkDebug(player, "Kill Clip expired");

			if (frenzyTimer > 0)
				frenzyTimer--;

			if (frenzyWasActive && frenzyTimer <= 0)
				SendPerkDebug(player, "Frenzy expired");

			if (frenzyCombatGraceTimer > 0)
			{
				frenzyCombatGraceTimer--;
				frenzyCombatTimer++;
				if (frenzyTimer <= 0 && frenzyCombatTimer >= FrenzyPerk.ActivationTicks)
					ActivateFrenzy(player);
			}
			else
			{
				frenzyCombatTimer = 0;
			}

			if (player?.HeldItem?.ModItem == this)
				player.GetModPlayer<Destiny2Player>().RequestFrenzyBuff(frenzyTimer);
		}

		internal void NotifyProjectileHit(Player player, NPC target, NPC.HitInfo hit, int damageDone, bool hasOutlaw, bool hasRapidHit, bool hasKillClip, bool hasFrenzy)
		{
			if (hasRapidHit && hit.Crit)
				AddRapidHitStack(player);

			if (hasFrenzy)
				RegisterCombat(player);

			if (target == null || target.friendly || target.life > 0)
				return;

			if (hasOutlaw)
				ActivateOutlaw(player);

			if (hasKillClip)
				killClipWindowTimer = KillClipPerk.WindowTicks;
		}

		internal void NotifyPlayerHurt(Player player)
		{
			if (!HasPerk<FrenzyPerk>())
				return;

			RegisterCombat(player);
		}

		private void ActivateOutlaw(Player player)
		{
			outlawTimer = OutlawPerk.DurationTicks;
			SendPerkDebug(player, "Outlaw activated");
		}

		private void AddRapidHitStack(Player player)
		{
			int nextStacks = Math.Min(rapidHitStacks + 1, RapidHitPerk.MaxStacks);
			if (nextStacks != rapidHitStacks)
			{
				rapidHitStacks = nextStacks;
				SendPerkDebug(player, $"Rapid Hit x{rapidHitStacks}");
			}

			rapidHitTimer = RapidHitPerk.DurationTicks;
		}

		private void ActivateKillClip(Player player)
		{
			killClipTimer = KillClipPerk.DurationTicks;
			SendPerkDebug(player, "Kill Clip activated");
		}

		private void ActivateFrenzy(Player player)
		{
			frenzyTimer = FrenzyPerk.DurationTicks;
			SendPerkDebug(player, "Frenzy activated");
		}

		private void RegisterCombat(Player player)
		{
			frenzyCombatGraceTimer = FrenzyPerk.CombatGraceTicks;
			if (frenzyTimer > 0)
				frenzyTimer = FrenzyPerk.DurationTicks;
		}

		private bool HasPerk<TPerk>() where TPerk : Destiny2Perk
		{
			foreach (Destiny2Perk perk in GetPerks())
			{
				if (perk is TPerk)
					return true;
			}

			return false;
		}

		private static void SendPerkDebug(Player player, string message)
		{
			if (Main.netMode == NetmodeID.Server)
				return;

			if (player == null || player.whoAmI != Main.myPlayer)
				return;

			Main.NewText(message, 255, 215, 100);
		}

		public IEnumerable<Destiny2Perk> GetPerks()
		{
			EnsurePerksRolled();
			bool hasCatalystPerk = false;

			if (!string.IsNullOrWhiteSpace(framePerkKey) && Destiny2PerkSystem.TryGet(framePerkKey, out Destiny2Perk framePerk))
				yield return framePerk;

			for (int i = 0; i < perkKeys.Count; i++)
			{
				if (Destiny2PerkSystem.TryGet(perkKeys[i], out Destiny2Perk perk))
				{
					if (!string.IsNullOrWhiteSpace(catalystPerkKey) && perk.Key == catalystPerkKey)
						hasCatalystPerk = true;
					yield return perk;
				}
			}

			if (!hasCatalystPerk && !string.IsNullOrWhiteSpace(catalystPerkKey) && Destiny2PerkSystem.TryGet(catalystPerkKey, out Destiny2Perk catalystPerk))
				yield return catalystPerk;
		}

		private void MarkPickedUp()
		{
			if (hasBeenPickedUp)
				return;

			hasBeenPickedUp = true;
		}

		protected void EnsurePerksRolled()
		{
			if (perksInitialized || !hasBeenPickedUp)
				return;

			perksInitialized = true;
			if (string.IsNullOrWhiteSpace(framePerkKey))
				RollFramePerk();
			if (perkKeys.Count == 0)
				RollPerks();

			if (!magazineInitialized)
				InitializeMagazine();
		}

		protected virtual void RollFramePerk()
		{
			if (Destiny2PerkSystem.FramePerks.Count == 0)
				return;

			int index = Main.rand.Next(Destiny2PerkSystem.FramePerks.Count);
			framePerkKey = Destiny2PerkSystem.FramePerks[index].Key;
		}

		protected virtual void RollPerks()
		{
			perkKeys.Clear();
			if (Destiny2PerkSystem.Perks.Count < PerkRollCount)
				return;

			int rollCount = Math.Min(PerkRollCount, Destiny2PerkSystem.Perks.Count);
			if (rollCount <= 0)
				return;

			List<Destiny2Perk> pool = new List<Destiny2Perk>(Destiny2PerkSystem.Perks);
			for (int i = 0; i < rollCount && pool.Count > 0; i++)
			{
				int index = Main.rand.Next(pool.Count);
				perkKeys.Add(pool[index].Key);
				pool.RemoveAt(index);
			}
		}

		protected void SetPerks(params string[] keys)
		{
			perkKeys.Clear();
			if (keys == null)
				return;

			for (int i = 0; i < keys.Length; i++)
			{
				string key = keys[i];
				if (!string.IsNullOrWhiteSpace(key))
					perkKeys.Add(key);
			}

			perksInitialized = true;
		}

		protected void SetFramePerk(string key)
		{
			if (string.IsNullOrWhiteSpace(key))
				return;

			framePerkKey = key;
			perksInitialized = true;
		}

		public void ReplacePerks(params string[] keys)
		{
			SetPerks(keys);
		}

		public void ReplacePerkAtSlot(int slotIndex, string key)
		{
			if (slotIndex < 0)
				return;

			EnsurePerksRolled();
			while (perkKeys.Count <= slotIndex)
				perkKeys.Add(string.Empty);

			perkKeys[slotIndex] = key ?? string.Empty;
			perksInitialized = true;
		}

		public void ResetPerks()
		{
			perkKeys.Clear();
			perksInitialized = false;
			EnsurePerksRolled();
		}

		public void ReplaceFramePerk(string key)
		{
			SetFramePerk(key);
		}

		public bool TryApplyCatalyst(Destiny2CatalystItem catalyst)
		{
			if (catalyst == null)
				return false;

			if (!HasCatalystSlot || HasCatalyst)
				return false;

			if (!catalyst.CanApplyTo(Item))
				return false;

			catalystPerkKey = catalyst.CatalystPerkKey;
			catalystItemType = catalyst.Type;
			return true;
		}
	}
}
