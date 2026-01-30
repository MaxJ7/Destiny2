using System;
using System.Collections.Generic;
using System.IO;
using Destiny2.Common.Items;
using Destiny2.Common.Players;
using Destiny2.Common.Perks;
using Destiny2.Common.Rarities;
using Destiny2.Content.Projectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.GameContent;
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
		private const string StatsTooltipPrefix = "Destiny2Stats_";
		private const string ElementTooltipPrefix = "Destiny2Element_";
		private const string PerkIconsTooltipName = "Destiny2PerkIcons";
		private const int TicksPerSecond = 60;
		private struct KineticTremorsTargetState
		{
			public int HitCount;
			public int HitTimer;
			public int CooldownTimer;
		}

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
		private int fourthTimesHitTimer;
		private int fourthTimesHitCount;
		private int rampageTimer;
		private int rampageStacks;
		private int killClipWindowTimer;
		private int killClipTimer;
		private bool killClipPending;
		private int frenzyTimer;
		private int frenzyCombatTimer;
		private int frenzyCombatGraceTimer;
		private int onslaughtTimer;
		private int onslaughtStacks;
		private int feedingFrenzyTimer;
		private int feedingFrenzyStacks;
		private int adagioTimer;
		private int targetLockHitTimer;
		private int targetLockHitCount;
		private int targetLockTargetId = -1;
		private int dynamicSwayTimer;
		private int dynamicSwayStacks;
		private int rightChoiceShotCount;
		private Dictionary<int, KineticTremorsTargetState> kineticTremorsTargets = new Dictionary<int, KineticTremorsTargetState>();
		private readonly List<int> kineticTremorsTargetKeys = new List<int>();
		private bool hasElementOverride;
		private Destiny2WeaponElement elementOverride;

		public abstract Destiny2WeaponStats BaseStats { get; }
		public virtual Destiny2AmmoType AmmoType => Destiny2AmmoType.Primary;
		public Destiny2WeaponElement WeaponElement => hasElementOverride ? elementOverride : GetDefaultWeaponElement();

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
			clone.fourthTimesHitTimer = fourthTimesHitTimer;
			clone.fourthTimesHitCount = fourthTimesHitCount;
			clone.rampageTimer = rampageTimer;
			clone.rampageStacks = rampageStacks;
			clone.killClipWindowTimer = killClipWindowTimer;
			clone.killClipTimer = killClipTimer;
			clone.killClipPending = killClipPending;
			clone.frenzyTimer = frenzyTimer;
			clone.frenzyCombatTimer = frenzyCombatTimer;
			clone.frenzyCombatGraceTimer = frenzyCombatGraceTimer;
			clone.onslaughtTimer = onslaughtTimer;
			clone.onslaughtStacks = onslaughtStacks;
			clone.feedingFrenzyTimer = feedingFrenzyTimer;
			clone.feedingFrenzyStacks = feedingFrenzyStacks;
			clone.adagioTimer = adagioTimer;
			clone.targetLockHitTimer = targetLockHitTimer;
			clone.targetLockHitCount = targetLockHitCount;
			clone.targetLockTargetId = targetLockTargetId;
			clone.dynamicSwayTimer = dynamicSwayTimer;
			clone.dynamicSwayStacks = dynamicSwayStacks;
			clone.rightChoiceShotCount = rightChoiceShotCount;
			clone.kineticTremorsTargets = new Dictionary<int, KineticTremorsTargetState>(kineticTremorsTargets);
			clone.hasElementOverride = hasElementOverride;
			clone.elementOverride = elementOverride;
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

			if (hasElementOverride)
				tag["elementOverride"] = (int)elementOverride;
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

			if (tag.ContainsKey("elementOverride"))
			{
				hasElementOverride = true;
				elementOverride = (Destiny2WeaponElement)tag.GetInt("elementOverride");
			}
			else
			{
				hasElementOverride = false;
				elementOverride = default;
			}
			SyncDamageTypeToElement();

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

			writer.Write(hasElementOverride);
			if (hasElementOverride)
				writer.Write((int)elementOverride);
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

			hasElementOverride = reader.ReadBoolean();
			if (hasElementOverride)
				elementOverride = (Destiny2WeaponElement)reader.ReadInt32();
			else
				elementOverride = default;
			SyncDamageTypeToElement();

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
			ApplyFrameRateOfFire(ref stats);
			ApplyActivePerkStats(ref stats);
			return stats;
		}

		public Destiny2WeaponStats GetEditableStats()
		{
			return hasCustomStats ? customStats : BaseStats;
		}

		public string FramePerkKey => framePerkKey;
		public string CatalystPerkKey => catalystPerkKey;
		public bool HasElementOverride => hasElementOverride;

		protected virtual Destiny2WeaponElement GetDefaultWeaponElement()
		{
			return Destiny2WeaponElement.Kinetic;
		}

		protected virtual int GetFrameRoundsPerMinute(Destiny2Perk framePerk, int currentRpm)
		{
			return currentRpm;
		}

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

		public void SetWeaponElement(Destiny2WeaponElement element)
		{
			elementOverride = element;
			hasElementOverride = true;
			SyncDamageTypeToElement();
		}

		public void ClearWeaponElementOverride()
		{
			hasElementOverride = false;
			elementOverride = default;
			SyncDamageTypeToElement();
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
		internal bool IsRampageActive => rampageTimer > 0 && rampageStacks > 0;
		internal bool IsAdagioActive => adagioTimer > 0;

		internal float GetRampageMultiplier()
		{
			if (!IsRampageActive)
				return 1f;

			int stacks = Math.Clamp(rampageStacks, 0, RampagePerk.MaxStacks);
			return RampagePerk.DamageMultiplierByStacks[stacks];
		}

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

		public override void ModifyHitNPC(Player player, NPC target, ref NPC.HitModifiers modifiers)
		{
			modifiers.DamageVariationScale *= 0f;
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

			if (HasPerk<DynamicSwayReductionPerk>())
				RegisterDynamicSwayShot();

			int bulletType = ModContent.ProjectileType<Bullet>();
			int slugType = ModContent.ProjectileType<ExplosiveShadowSlug>();
			if (type == bulletType || type == slugType)
			{
				Vector2 aimDirection = GetAimDirection(player, velocity);
				float aimRotation = aimDirection.ToRotation();
				int projId = Projectile.NewProjectile(source, position, aimDirection, type, damage, knockback, player.whoAmI, 0f, aimRotation);
				if (projId >= 0 && projId < Main.maxProjectiles)
				{
					Projectile proj = Main.projectile[projId];
					proj.netUpdate = true;
				}

				return false;
			}

			return true;
		}

		private static Vector2 GetAimDirection(Player player, Vector2 velocity)
		{
			if (velocity.LengthSquared() > 0.0001f)
				return velocity.SafeNormalize(Vector2.UnitX);

			if (player != null)
			{
				if (Main.netMode != NetmodeID.Server && player.whoAmI == Main.myPlayer)
				{
					Vector2 aim = Main.MouseWorld - player.MountedCenter;
					if (aim.LengthSquared() > 0.0001f)
						return aim.SafeNormalize(Vector2.UnitX);
				}

				return new Vector2(player.direction, 0f);
			}

			return Vector2.UnitX;
		}

		public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
		{
			Vector2 direction = velocity.SafeNormalize(Vector2.UnitX);
			Vector2 origin = position;
			float muzzleDistance = MathHelper.Clamp(Item.width * Item.scale * 0.35f, 8f, 26f);
			Vector2 muzzleOffset = direction * muzzleDistance;

			position = origin;
			if (Collision.CanHit(origin, 0, 0, origin + muzzleOffset, 0, 0))
				position += muzzleOffset;
		}

		public override void HoldItem(Player player)
		{
			UpdateWeaponState(player);
		}

		public override void UpdateInventory(Player player)
		{
			if (player?.HeldItem?.ModItem == this)
				return;

			UpdateWeaponState(player);
		}

		public override void ModifyTooltips(List<TooltipLine> tooltips)
		{
			tooltips.RemoveAll(line => line.Mod == "Terraria" && VanillaStatLines.Contains(line.Name));

			Destiny2WeaponStats stats = BaseStats;
			Destiny2WeaponElement element = WeaponElement;
			tooltips.Add(new TooltipLine(Mod, ElementTooltipPrefix + element, $"{Item.damage} {element} Damage")
			{
				OverrideColor = GetElementColor(element)
			});
			tooltips.Add(new TooltipLine(Mod, StatsTooltipPrefix + "Range", $"Range: {stats.Range:0}"));
			tooltips.Add(new TooltipLine(Mod, StatsTooltipPrefix + "Stability", $"Stability: {stats.Stability:0}"));
			tooltips.Add(new TooltipLine(Mod, StatsTooltipPrefix + "Reload", $"Reload: {stats.ReloadSpeed:0}"));
			tooltips.Add(new TooltipLine(Mod, StatsTooltipPrefix + "RPM", $"RPM: {stats.RoundsPerMinute}"));
			tooltips.Add(new TooltipLine(Mod, StatsTooltipPrefix + "Magazine", $"Magazine: {stats.Magazine}"));
			tooltips.Add(new TooltipLine(Mod, StatsTooltipPrefix + "AmmoType", $"Ammo Type: {AmmoType}"));
			tooltips.Add(new TooltipLine(Mod, PerkIconsTooltipName, " "));
		}

		public override void UseStyle(Player player, Rectangle heldItemFrame)
		{
			base.UseStyle(player, heldItemFrame);

			if (player.itemAnimationMax <= 0)
				return;

			float recoilStrength = GetRecoilStrength();
			if (recoilStrength <= 0f)
				return;

			float progress = 1f - (player.itemAnimation / (float)player.itemAnimationMax);
			float kick = Math.Clamp(1f - progress, 0f, 1f);
			kick *= kick;

			float rotationKick = MathHelper.ToRadians(recoilStrength * 4f) * kick;
			player.itemRotation -= rotationKick * player.direction;

			float offsetKick = recoilStrength * 4f * kick;
			Vector2 recoilDir = player.itemRotation.ToRotationVector2();
			player.itemLocation -= recoilDir * offsetKick;
		}

		public override bool PreDrawTooltipLine(DrawableTooltipLine line, ref int yOffset)
		{
			if (line.Mod != Mod.Name)
				return true;

			if (line.Name.StartsWith(ElementTooltipPrefix, StringComparison.Ordinal))
			{
				string elementText = line.Name.Substring(ElementTooltipPrefix.Length);
				if (!Enum.TryParse(elementText, out Destiny2WeaponElement element))
					return true;

				Texture2D elementIcon = ModContent.Request<Texture2D>(element.GetIconTexture()).Value;
				Vector2 elementIconPos = new Vector2(line.X, line.Y);
				Vector2 elementTextPos = new Vector2(line.X + elementIcon.Width + 6f, line.Y);

				Color textColor = line.OverrideColor ?? line.Color;
				Main.spriteBatch.Draw(elementIcon, elementIconPos, null, Color.White);
				ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, line.Font, line.Text, elementTextPos, textColor, line.Rotation, line.Origin, line.BaseScale);

				return false;
			}
			if (line.Name == PerkIconsTooltipName)
			{
				const float iconSize = 20f;
				const float gap = 6f;
				Texture2D[] icons = new Texture2D[5];
				icons[0] = GetPerkIcon(framePerkKey);
				for (int i = 0; i < 4; i++)
					icons[i + 1] = GetPerkIcon(i < perkKeys.Count ? perkKeys[i] : null);

				float x = line.X;
				float y = line.Y;
				for (int i = 0; i < icons.Length; i++)
				{
					Rectangle iconRect = new Rectangle((int)x, (int)y, (int)iconSize, (int)iconSize);
					Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, iconRect, Color.Black * 0.4f);
					if (icons[i] != null)
						Main.spriteBatch.Draw(icons[i], iconRect, Color.White);
					x += iconSize + gap;
				}

				float lineHeight = line.Font.MeasureString(line.Text).Y * line.BaseScale.Y;
				int extra = (int)Math.Max(0f, iconSize - lineHeight);
				if (extra > 0)
					yOffset += extra;

				return false;
			}

			return true;
		}

		private static Texture2D GetPerkIcon(string perkKey)
		{
			if (string.IsNullOrWhiteSpace(perkKey))
				return null;

			if (!Destiny2PerkSystem.TryGet(perkKey, out Destiny2Perk perk))
				return null;

			if (string.IsNullOrWhiteSpace(perk.IconTexture))
				return null;

			return ModContent.Request<Texture2D>(perk.IconTexture).Value;
		}

		private static Color GetElementColor(Destiny2WeaponElement element)
		{
			return element switch
			{
				Destiny2WeaponElement.Void => new Color(196, 0, 240),
				Destiny2WeaponElement.Strand => new Color(55, 218, 100),
				Destiny2WeaponElement.Stasis => new Color(51, 91, 196),
				Destiny2WeaponElement.Solar => new Color(236, 85, 0),
				Destiny2WeaponElement.Arc => new Color(7, 208, 255),
				Destiny2WeaponElement.Kinetic => new Color(255, 248, 163),
				_ => new Color(255, 248, 163)
			};
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

		protected virtual float GetRecoilStrength()
		{
			return 0f;
		}

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

		private void UpdateReloadAnimation(Player player)
		{
			if (!isReloading || reloadTimerMax <= 0 || player == null)
				return;

			if (player.itemAnimation <= 0)
			{
				// Keep the weapon held out while the reload animation plays.
				player.itemAnimation = 2;
				player.itemTime = 2;
				player.itemAnimationMax = Math.Max(player.itemAnimationMax, Item.useAnimation);
			}

			float progress = 1f - (reloadTimer / (float)reloadTimerMax);
			float downUp = (float)Math.Sin(progress * MathHelper.Pi);
			float maxAngle = MathHelper.ToRadians(45f);
			float offset = maxAngle * downUp * -player.direction;

			player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, player.itemRotation + offset);
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

			if (onslaughtTimer > 0 && onslaughtStacks > 0)
			{
				int stacks = Math.Clamp(onslaughtStacks, 0, OnslaughtPerk.MaxStacks);
				stats.ReloadSpeed += OnslaughtPerk.ReloadSpeedByStacks[stacks];
				stats.RoundsPerMinute = ApplyRpmScalar(stats.RoundsPerMinute, OnslaughtPerk.RpmScalarByStacks[stacks]);
			}

			if (feedingFrenzyTimer > 0 && feedingFrenzyStacks > 0)
			{
				int stacks = Math.Clamp(feedingFrenzyStacks, 0, FeedingFrenzyPerk.MaxStacks);
				stats.ReloadSpeed += FeedingFrenzyPerk.ReloadSpeedByStacks[stacks];
			}

			if (adagioTimer > 0)
			{
				stats.Range += AdagioPerk.RangeBonus;
				stats.RoundsPerMinute = ApplyRpmScalar(stats.RoundsPerMinute, AdagioPerk.RpmScalar);
			}

			if (dynamicSwayTimer > 0 && dynamicSwayStacks > 0)
				stats.Stability += dynamicSwayStacks * DynamicSwayReductionPerk.StabilityPerStack;
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

			if (feedingFrenzyTimer > 0 && feedingFrenzyStacks > 0)
			{
				int stacks = Math.Clamp(feedingFrenzyStacks, 0, FeedingFrenzyPerk.MaxStacks);
				scalar *= FeedingFrenzyPerk.ReloadTimeScalarByStacks[stacks];
			}

			if (HasPerk<AlloyMagPerk>())
				scalar *= GetAlloyMagScalar();

			return scalar;
		}

		private static int ApplyRpmScalar(int rpm, float scalar)
		{
			if (rpm <= 0)
				return rpm;

			int scaled = (int)Math.Round(rpm * scalar);
			return Math.Max(1, scaled);
		}

		private float GetAlloyMagScalar()
		{
			int magazineSize = GetStats().Magazine;
			if (magazineSize <= 0)
				return 1f;

			float ratio = Math.Clamp(currentMagazine / (float)magazineSize, 0f, 1f);
			if (ratio > 0.5f)
				return 1f;

			float t = ratio / 0.5f;
			return MathHelper.Lerp(AlloyMagPerk.EmptyMagScalar, AlloyMagPerk.HalfMagScalar, t);
		}

		private void UpdatePerkTimers(Player player)
		{
			bool outlawWasActive = outlawTimer > 0;
			bool rapidHitWasActive = rapidHitTimer > 0 && rapidHitStacks > 0;
			bool killClipWasActive = killClipTimer > 0;
			bool frenzyWasActive = frenzyTimer > 0;
			bool rampageWasActive = rampageTimer > 0 && rampageStacks > 0;
			bool onslaughtWasActive = onslaughtTimer > 0 && onslaughtStacks > 0;
			bool feedingFrenzyWasActive = feedingFrenzyTimer > 0 && feedingFrenzyStacks > 0;
			bool adagioWasActive = adagioTimer > 0;

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

			if (rampageTimer > 0)
			{
				rampageTimer--;
				if (rampageTimer <= 0)
					rampageStacks = 0;
			}

			if (rampageWasActive && rampageTimer <= 0)
				SendPerkDebug(player, "Rampage expired");

			if (frenzyTimer > 0)
				frenzyTimer--;

			if (frenzyWasActive && frenzyTimer <= 0)
				SendPerkDebug(player, "Frenzy expired");

			if (onslaughtTimer > 0)
			{
				onslaughtTimer--;
				if (onslaughtTimer <= 0)
					onslaughtStacks = 0;
			}

			if (onslaughtWasActive && onslaughtTimer <= 0)
				SendPerkDebug(player, "Onslaught expired");

			if (feedingFrenzyTimer > 0)
			{
				feedingFrenzyTimer--;
				if (feedingFrenzyTimer <= 0)
					feedingFrenzyStacks = 0;
			}

			if (feedingFrenzyWasActive && feedingFrenzyTimer <= 0)
				SendPerkDebug(player, "Feeding Frenzy expired");

			if (adagioTimer > 0)
				adagioTimer--;

			if (adagioWasActive && adagioTimer <= 0)
				SendPerkDebug(player, "Adagio expired");

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

			if (fourthTimesHitTimer > 0)
			{
				fourthTimesHitTimer--;
				if (fourthTimesHitTimer <= 0)
					fourthTimesHitCount = 0;
			}

			if (targetLockHitTimer > 0)
			{
				targetLockHitTimer--;
				if (targetLockHitTimer <= 0)
					ResetTargetLockState();
			}

			if (dynamicSwayTimer > 0)
			{
				dynamicSwayTimer--;
				if (dynamicSwayTimer <= 0)
					dynamicSwayStacks = 0;
			}

			UpdateKineticTremorsTargets();


			if (player?.HeldItem?.ModItem == this)
			{
				Destiny2Player modPlayer = player.GetModPlayer<Destiny2Player>();
				modPlayer.RequestFrenzyBuff(frenzyTimer);
				modPlayer.RequestOutlawBuff(outlawTimer);
				modPlayer.RequestRapidHitBuff(rapidHitStacks > 0 ? rapidHitTimer : 0);
				modPlayer.RequestKillClipBuff(killClipTimer);
				modPlayer.RequestRampageBuff(rampageStacks > 0 ? rampageTimer : 0);
				modPlayer.RequestOnslaughtBuff(onslaughtStacks > 0 ? onslaughtTimer : 0);
				modPlayer.RequestFeedingFrenzyBuff(feedingFrenzyStacks > 0 ? feedingFrenzyTimer : 0);
				modPlayer.RequestAdagioBuff(adagioTimer);
				modPlayer.RequestTargetLockBuff(targetLockHitCount > 0 ? targetLockHitTimer : 0);
				modPlayer.RequestDynamicSwayBuff(dynamicSwayStacks > 0 ? dynamicSwayTimer : 0);
				modPlayer.RequestFourthTimesBuff(fourthTimesHitTimer);
			}
		}

		internal void NotifyProjectileHit(Player player, NPC target, NPC.HitInfo hit, int damageDone, bool hasOutlaw, bool hasRapidHit, bool hasKillClip, bool hasFrenzy, bool hasFourthTimes, bool hasRampage, bool hasOnslaught, bool hasAdagio, bool hasFeedingFrenzy)
		{
			if (hasRapidHit && hit.Crit)
				AddRapidHitStack(player);

			if (hasFourthTimes)
				RegisterFourthTimesHit(player);

			if (hasFrenzy)
				RegisterCombat(player);

			if (target == null || target.friendly || target.life > 0)
				return;

			if (hasOutlaw)
				ActivateOutlaw(player);

			if (hasKillClip)
				killClipWindowTimer = KillClipPerk.WindowTicks;

			if (hasRampage)
				AddRampageStack(player);

			if (hasOnslaught)
				AddOnslaughtStack(player);

			if (hasAdagio)
				ActivateAdagio(player);

			if (hasFeedingFrenzy)
				AddFeedingFrenzyStack(player);
		}

		internal bool TryConsumeRightChoiceShot()
		{
			rightChoiceShotCount++;
			if (rightChoiceShotCount < TheRightChoiceFramePerk.ShotsRequired)
				return false;

			rightChoiceShotCount = 0;
			return true;
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

		private void RegisterFourthTimesHit(Player player)
		{
			if (fourthTimesHitTimer <= 0)
				fourthTimesHitCount = 0;

			fourthTimesHitCount++;
			fourthTimesHitTimer = FourthTimesTheCharmPerk.WindowTicks;

			if (fourthTimesHitCount < FourthTimesTheCharmPerk.HitsRequired)
				return;

			fourthTimesHitCount = 0;
			fourthTimesHitTimer = 0;
			GrantFourthTimesAmmo(player);
		}

		private void GrantFourthTimesAmmo(Player player)
		{
			int magazineSize = GetStats().Magazine;
			if (magazineSize <= 0)
				return;

			int nextMagazine = Math.Min(magazineSize, currentMagazine + FourthTimesTheCharmPerk.AmmoReturned);
			if (nextMagazine == currentMagazine)
				return;

			currentMagazine = nextMagazine;
			SendPerkDebug(player, "Fourth Times the Charm");
		}

		private void AddRampageStack(Player player)
		{
			int nextStacks = Math.Min(rampageStacks + 1, RampagePerk.MaxStacks);
			if (nextStacks != rampageStacks)
			{
				rampageStacks = nextStacks;
				SendPerkDebug(player, $"Rampage x{rampageStacks}");
			}

			rampageTimer = RampagePerk.DurationTicks;
		}

		private void AddOnslaughtStack(Player player)
		{
			int nextStacks = Math.Min(onslaughtStacks + 1, OnslaughtPerk.MaxStacks);
			if (nextStacks != onslaughtStacks)
			{
				onslaughtStacks = nextStacks;
				SendPerkDebug(player, $"Onslaught x{onslaughtStacks}");
			}

			onslaughtTimer = OnslaughtPerk.DurationTicks;
		}

		private void AddFeedingFrenzyStack(Player player)
		{
			int nextStacks = Math.Min(feedingFrenzyStacks + 1, FeedingFrenzyPerk.MaxStacks);
			if (nextStacks != feedingFrenzyStacks)
			{
				feedingFrenzyStacks = nextStacks;
				SendPerkDebug(player, $"Feeding Frenzy x{feedingFrenzyStacks}");
			}

			feedingFrenzyTimer = FeedingFrenzyPerk.DurationTicks;
		}

		private void ActivateAdagio(Player player)
		{
			adagioTimer = AdagioPerk.DurationTicks;
			SendPerkDebug(player, "Adagio activated");
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

		private void RegisterDynamicSwayShot()
		{
			dynamicSwayTimer = DynamicSwayReductionPerk.HoldWindowTicks;
			if (dynamicSwayStacks < DynamicSwayReductionPerk.MaxStacks)
				dynamicSwayStacks++;
		}

		internal float RegisterTargetLockHit(NPC target)
		{
			if (target == null || !target.CanBeChasedBy())
				return 0f;

			if (targetLockHitTimer <= 0 || targetLockTargetId != target.whoAmI)
				ResetTargetLockState();

			targetLockTargetId = target.whoAmI;
			targetLockHitCount++;
			targetLockHitTimer = TargetLockPerk.HitWindowTicks;

			int magazineSize = GetStats().Magazine;
			if (magazineSize <= 0)
				return 0f;

			float ratio = targetLockHitCount / (float)magazineSize;
			if (ratio < TargetLockPerk.MinHitsRatio)
				return 0f;

			float t = (ratio - TargetLockPerk.MinHitsRatio) / (TargetLockPerk.MaxHitsRatio - TargetLockPerk.MinHitsRatio);
			t = MathHelper.Clamp(t, 0f, 1f);
			return MathHelper.Lerp(TargetLockPerk.MinDamageBonus, TargetLockPerk.MaxDamageBonus, t);
		}

		private void ResetTargetLockState()
		{
			targetLockHitCount = 0;
			targetLockHitTimer = 0;
			targetLockTargetId = -1;
		}

		internal void RegisterKineticTremorsHit(Projectile projectile, NPC target, int damageDone)
		{
			if (projectile == null || target == null || !target.CanBeChasedBy())
				return;

			KineticTremorsGlobalNPC global = target.GetGlobalNPC<KineticTremorsGlobalNPC>();
			if (global.KineticTremorsCooldown > 0)
				return;

			int hitsRequired = GetKineticTremorsHitsRequired();
			if (hitsRequired <= 0)
				return;

			if (!kineticTremorsTargets.TryGetValue(target.whoAmI, out KineticTremorsTargetState state))
				state = default;

			if (state.CooldownTimer > 0)
				return;

			if (state.HitTimer <= 0)
				state.HitCount = 0;

			state.HitCount++;
			state.HitTimer = KineticTremorsPerk.HitWindowTicks;

			if (state.HitCount < hitsRequired)
			{
				kineticTremorsTargets[target.whoAmI] = state;
				return;
			}

			state.HitCount = 0;
			state.HitTimer = 0;

			int initialDelay = IsBowWeapon() ? KineticTremorsPerk.BowInitialDelayTicks : KineticTremorsPerk.InitialDelayTicks;
			int totalCooldown = initialDelay
				+ (KineticTremorsPerk.PulseCount - 1) * KineticTremorsPerk.PulseIntervalTicks
				+ KineticTremorsPerk.CooldownAfterLastPulseTicks;

			state.CooldownTimer = totalCooldown;
			kineticTremorsTargets[target.whoAmI] = state;

			if (global != null)
				global.KineticTremorsCooldown = totalCooldown;

			SpawnKineticTremorsShockwave(projectile, target, damageDone, initialDelay);
		}

		private int GetKineticTremorsHitsRequired()
		{
			if (this is AutoRifleWeaponItem)
				return KineticTremorsPerk.AutoRifleHitsRequired;
			if (this is HandCannonWeaponItem)
				return KineticTremorsPerk.HandCannonHitsRequired;

			return KineticTremorsPerk.AutoRifleHitsRequired;
		}

		private bool IsBowWeapon()
		{
			return Item.useAmmo == AmmoID.Arrow;
		}

		private void SpawnKineticTremorsShockwave(Projectile projectile, NPC target, int damageDone, int initialDelay)
		{
			int shockwaveDamage = Math.Min(KineticTremorsPerk.MaxShockwaveDamage, damageDone);
			if (shockwaveDamage <= 0)
				return;

			Vector2 center = target.Center;
			int projId = Projectile.NewProjectile(projectile.GetSource_FromThis(), center, Vector2.Zero,
				ModContent.ProjectileType<KineticTremorsShockwave>(), shockwaveDamage, 0f, projectile.owner);
			if (projId < 0 || projId >= Main.maxProjectiles)
				return;

			Projectile shockwave = Main.projectile[projId];
			shockwave.ai[0] = initialDelay;
			shockwave.ai[1] = KineticTremorsPerk.PulseCount;
			shockwave.localAI[0] = KineticTremorsPerk.PulseIntervalTicks;
			shockwave.DamageType = WeaponElement.GetDamageClass();
			shockwave.direction = projectile.direction != 0 ? projectile.direction : 1;
			shockwave.timeLeft = Math.Max(shockwave.timeLeft, initialDelay + (KineticTremorsPerk.PulseCount - 1) * KineticTremorsPerk.PulseIntervalTicks + 30);
			shockwave.netUpdate = true;
		}

		private void UpdateKineticTremorsTargets()
		{
			if (kineticTremorsTargets.Count == 0)
				return;

			kineticTremorsTargetKeys.Clear();
			foreach (int key in kineticTremorsTargets.Keys)
				kineticTremorsTargetKeys.Add(key);

			for (int i = 0; i < kineticTremorsTargetKeys.Count; i++)
			{
				int npcId = kineticTremorsTargetKeys[i];
				if (npcId < 0 || npcId >= Main.maxNPCs || !Main.npc[npcId].active)
				{
					kineticTremorsTargets.Remove(npcId);
					continue;
				}

				KineticTremorsTargetState state = kineticTremorsTargets[npcId];
				if (state.HitTimer > 0)
				{
					state.HitTimer--;
					if (state.HitTimer <= 0)
						state.HitCount = 0;
				}

				if (state.CooldownTimer > 0)
					state.CooldownTimer--;

				if (state.HitTimer <= 0 && state.CooldownTimer <= 0 && state.HitCount <= 0)
					kineticTremorsTargets.Remove(npcId);
				else
					kineticTremorsTargets[npcId] = state;
			}
		}

		private void UpdateWeaponState(Player player)
		{
			MarkPickedUp();
			EnsurePerksRolled();
			UpdatePerkTimers(player);
			UpdateReload(player);
			UpdateReloadAnimation(player);
			UpdateUseTimeFromStats();
			SyncDamageTypeToElement();
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

		private void ApplyFrameRateOfFire(ref Destiny2WeaponStats stats)
		{
			if (string.IsNullOrWhiteSpace(framePerkKey))
				return;

			if (!Destiny2PerkSystem.TryGet(framePerkKey, out Destiny2Perk framePerk))
				return;

			int frameRpm = GetFrameRoundsPerMinute(framePerk, stats.RoundsPerMinute);
			if (frameRpm > 0)
				stats.RoundsPerMinute = frameRpm;
		}

		private void SyncDamageTypeToElement()
		{
			DamageClass damageClass = WeaponElement.GetDamageClass();
			if (Item.DamageType != damageClass)
				Item.DamageType = damageClass;
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

		protected static string RollFrom(params string[] keys)
		{
			if (keys == null || keys.Length == 0)
				return null;

			int index = Main.rand.Next(keys.Length);
			return keys[index];
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


