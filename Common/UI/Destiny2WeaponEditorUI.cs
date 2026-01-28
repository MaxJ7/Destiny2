using System;
using System.Collections.Generic;
using Destiny2.Common.Perks;
using Destiny2.Common.Weapons;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace Destiny2.Common.UI
{
	public sealed class Destiny2WeaponEditorUI : UIState
	{
		private const float PanelWidth = 560f;
		private const float PanelHeight = 440f;
		private const float LeftPadding = 12f;
		private const float RightPadding = 12f;
		private const float TopButtonWidth = 150f;
		private const float TopButtonHeight = 26f;
		private const float TopButtonLeft = PanelWidth - RightPadding - TopButtonWidth;
		private const float RowHeight = 26f;
		private const float RowLabelOffset = 4f;
		private const float RowButtonWidth = 26f;
		private const float RowButtonHeight = 22f;
		private const float RowButtonGap = 6f;
		private const float PrevButtonLeft = PanelWidth - RightPadding - (RowButtonWidth * 2f) - RowButtonGap;
		private const float NextButtonLeft = PanelWidth - RightPadding - RowButtonWidth;
		private const float StatValueLeft = 120f;
		private const float StatMinusLeft = 320f;
		private const float StatPlusLeft = StatMinusLeft + RowButtonWidth + RowButtonGap;
		private const float PerkValueLeft = 140f;
		private const float ResetButtonWidth = 130f;
		private const float ResetButtonHeight = 26f;
		private const float ResetButtonTop = PanelHeight - ResetButtonHeight - 12f;

		private readonly Dictionary<string, UIText> statTexts = new Dictionary<string, UIText>(StringComparer.Ordinal);
		private readonly List<UIText> perkTexts = new List<UIText>();
		private static readonly string[] PerkSlotNames = new[]
		{
			"Barrel",
			"Magazine",
			"Major Perk",
			"Major Perk"
		};
		private static readonly PerkSlotType[] PerkSlotTypes = new[]
		{
			PerkSlotType.Barrel,
			PerkSlotType.Magazine,
			PerkSlotType.Major,
			PerkSlotType.Major
		};
		private UIText itemText;
		private UIText statsSourceText;
		private UIText frameText;
		private Item selectedItem;

		public override void OnInitialize()
		{
			UIPanel panel = new UIPanel
			{
				Width = { Pixels = PanelWidth },
				Height = { Pixels = PanelHeight },
				HAlign = 0.5f,
				VAlign = 0.5f
			};
			Append(panel);

			UIText title = new UIText("Destiny2 Weapon Editor");
			title.Left.Set(LeftPadding, 0f);
			title.Top.Set(10f, 0f);
			panel.Append(title);

			itemText = new UIText("Selected: (hold a Destiny2 weapon)");
			itemText.Left.Set(LeftPadding, 0f);
			itemText.Top.Set(34f, 0f);
			panel.Append(itemText);

			statsSourceText = new UIText("Stats: Base");
			statsSourceText.Left.Set(LeftPadding, 0f);
			statsSourceText.Top.Set(52f, 0f);
			panel.Append(statsSourceText);

			UITextPanel<string> useHeldButton = CreateButton("Use Held Item", () =>
			{
				Item held = Main.LocalPlayer.HeldItem;
				if (held?.ModItem is Destiny2WeaponItem)
					selectedItem = held;
			});
			useHeldButton.Left.Set(TopButtonLeft, 0f);
			useHeldButton.Top.Set(28f, 0f);
			useHeldButton.Width.Set(TopButtonWidth, 0f);
			useHeldButton.Height.Set(TopButtonHeight, 0f);
			panel.Append(useHeldButton);

			UITextPanel<string> clearButton = CreateButton("Clear", () => selectedItem = null);
			clearButton.Left.Set(TopButtonLeft, 0f);
			clearButton.Top.Set(58f, 0f);
			clearButton.Width.Set(TopButtonWidth, 0f);
			clearButton.Height.Set(TopButtonHeight, 0f);
			panel.Append(clearButton);

			float rowTop = 86f;
			AddStatRow(panel, "Range", "Range", rowTop, -1f, 1f, ApplyRange);
			rowTop += RowHeight;
			AddStatRow(panel, "Stability", "Stability", rowTop, -1f, 1f, ApplyStability);
			rowTop += RowHeight;
			AddStatRow(panel, "Reload", "Reload", rowTop, -1f, 1f, ApplyReload);
			rowTop += RowHeight;
			AddStatRow(panel, "RPM", "RPM", rowTop, -5f, 5f, ApplyRpm);
			rowTop += RowHeight;
			AddStatRow(panel, "Magazine", "Magazine", rowTop, -1f, 1f, ApplyMagazine);
			rowTop += RowHeight;

			AddFrameRow(panel, rowTop);
			rowTop += RowHeight;
			for (int i = 0; i < PerkSlotNames.Length; i++)
			{
				AddPerkRow(panel, rowTop, i);
				rowTop += RowHeight;
			}

			UITextPanel<string> resetStatsButton = CreateButton("Reset Stats", () =>
			{
				if (TryGetWeapon(out Destiny2WeaponItem weapon, out _))
					weapon.ClearCustomStats();
			});
			resetStatsButton.Left.Set(LeftPadding, 0f);
			resetStatsButton.Top.Set(ResetButtonTop, 0f);
			resetStatsButton.Width.Set(ResetButtonWidth, 0f);
			resetStatsButton.Height.Set(ResetButtonHeight, 0f);
			panel.Append(resetStatsButton);

			UITextPanel<string> resetPerksButton = CreateButton("Reset Perks", () =>
			{
				if (TryGetWeapon(out Destiny2WeaponItem weapon, out _))
					weapon.ResetPerks();
			});
			resetPerksButton.Left.Set(LeftPadding + ResetButtonWidth + 10f, 0f);
			resetPerksButton.Top.Set(ResetButtonTop, 0f);
			resetPerksButton.Width.Set(ResetButtonWidth, 0f);
			resetPerksButton.Height.Set(ResetButtonHeight, 0f);
			panel.Append(resetPerksButton);
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			if (!TryGetWeapon(out Destiny2WeaponItem weapon, out Item item))
			{
				itemText.SetText("Selected: (hold a Destiny2 weapon)");
				statsSourceText.SetText("Stats: Base");
				SetStatText("Range", "--");
				SetStatText("Stability", "--");
				SetStatText("Reload", "--");
				SetStatText("RPM", "--");
				SetStatText("Magazine", "--");
				for (int i = 0; i < perkTexts.Count; i++)
					perkTexts[i].SetText("--");
				return;
			}

			itemText.SetText($"Selected: {item.Name}");
			statsSourceText.SetText(weapon.HasCustomStats ? "Stats: Custom" : "Stats: Base");

			Destiny2WeaponStats stats = weapon.GetEditableStats();
			SetStatText("Range", stats.Range.ToString("0"));
			SetStatText("Stability", stats.Stability.ToString("0"));
			SetStatText("Reload", stats.ReloadSpeed.ToString("0"));
			SetStatText("RPM", stats.RoundsPerMinute.ToString());
			SetStatText("Magazine", stats.Magazine.ToString());

			if (frameText != null)
			{
				string frameName = "None";
				if (!string.IsNullOrWhiteSpace(weapon.FramePerkKey) && Destiny2PerkSystem.TryGet(weapon.FramePerkKey, out Destiny2Perk framePerk))
					frameName = framePerk.DisplayName;
				frameText.SetText($"Frame: {frameName}");
			}

			for (int i = 0; i < perkTexts.Count; i++)
			{
				string perkName = "None";
				if (weapon.PerkKeys.Count > i && Destiny2PerkSystem.TryGet(weapon.PerkKeys[i], out Destiny2Perk perk))
					perkName = perk.DisplayName;
				perkTexts[i].SetText(perkName);
			}
		}

		private void AddStatRow(UIElement panel, string label, string key, float top, float minusStep, float plusStep, Action<float> apply)
		{
			UIText labelText = new UIText($"{label}:");
			labelText.Left.Set(LeftPadding, 0f);
			labelText.Top.Set(top + RowLabelOffset, 0f);
			panel.Append(labelText);

			UIText valueText = new UIText("--");
			valueText.Left.Set(StatValueLeft, 0f);
			valueText.Top.Set(top + RowLabelOffset, 0f);
			panel.Append(valueText);
			statTexts[key] = valueText;

			UITextPanel<string> minusButton = CreateButton("-", () => apply(minusStep));
			minusButton.Left.Set(StatMinusLeft, 0f);
			minusButton.Top.Set(top, 0f);
			minusButton.Width.Set(RowButtonWidth, 0f);
			minusButton.Height.Set(RowButtonHeight, 0f);
			panel.Append(minusButton);

			UITextPanel<string> plusButton = CreateButton("+", () => apply(plusStep));
			plusButton.Left.Set(StatPlusLeft, 0f);
			plusButton.Top.Set(top, 0f);
			plusButton.Width.Set(RowButtonWidth, 0f);
			plusButton.Height.Set(RowButtonHeight, 0f);
			panel.Append(plusButton);
		}

		private void AddPerkRow(UIElement panel, float top, int slotIndex)
		{
			string slotName = slotIndex < PerkSlotNames.Length ? PerkSlotNames[slotIndex] : $"Slot {slotIndex + 1}";
			UIText labelText = new UIText($"{slotName}:");
			labelText.Left.Set(LeftPadding, 0f);
			labelText.Top.Set(top + RowLabelOffset, 0f);
			panel.Append(labelText);

			UIText perkText = new UIText("--");
			perkText.Left.Set(PerkValueLeft, 0f);
			perkText.Top.Set(top + RowLabelOffset, 0f);
			panel.Append(perkText);
			perkTexts.Add(perkText);

			UITextPanel<string> prevButton = CreateButton("<", () => CyclePerk(slotIndex, -1));
			prevButton.Left.Set(PrevButtonLeft, 0f);
			prevButton.Top.Set(top, 0f);
			prevButton.Width.Set(RowButtonWidth, 0f);
			prevButton.Height.Set(RowButtonHeight, 0f);
			panel.Append(prevButton);

			UITextPanel<string> nextButton = CreateButton(">", () => CyclePerk(slotIndex, 1));
			nextButton.Left.Set(NextButtonLeft, 0f);
			nextButton.Top.Set(top, 0f);
			nextButton.Width.Set(RowButtonWidth, 0f);
			nextButton.Height.Set(RowButtonHeight, 0f);
			panel.Append(nextButton);
		}

		private void AddFrameRow(UIElement panel, float top)
		{
			UIText labelText = new UIText("Frame:");
			labelText.Left.Set(LeftPadding, 0f);
			labelText.Top.Set(top + RowLabelOffset, 0f);
			panel.Append(labelText);

			frameText = new UIText("Frame: --");
			frameText.Left.Set(PerkValueLeft, 0f);
			frameText.Top.Set(top + RowLabelOffset, 0f);
			panel.Append(frameText);

			UITextPanel<string> prevButton = CreateButton("<", () => CycleFrame(-1));
			prevButton.Left.Set(PrevButtonLeft, 0f);
			prevButton.Top.Set(top, 0f);
			prevButton.Width.Set(RowButtonWidth, 0f);
			prevButton.Height.Set(RowButtonHeight, 0f);
			panel.Append(prevButton);

			UITextPanel<string> nextButton = CreateButton(">", () => CycleFrame(1));
			nextButton.Left.Set(NextButtonLeft, 0f);
			nextButton.Top.Set(top, 0f);
			nextButton.Width.Set(RowButtonWidth, 0f);
			nextButton.Height.Set(RowButtonHeight, 0f);
			panel.Append(nextButton);
		}

		private static UITextPanel<string> CreateButton(string text, Action onClick)
		{
			UITextPanel<string> button = new UITextPanel<string>(text);
			button.OnLeftClick += (_, _) => onClick?.Invoke();
			return button;
		}

		private void ApplyRange(float delta)
		{
			if (!TryGetWeapon(out Destiny2WeaponItem weapon, out _))
				return;

			Destiny2WeaponStats stats = weapon.GetEditableStats();
			stats.Range = Math.Clamp(stats.Range + delta, 0f, 100f);
			weapon.ApplyCustomStats(stats);
		}

		private void ApplyStability(float delta)
		{
			if (!TryGetWeapon(out Destiny2WeaponItem weapon, out _))
				return;

			Destiny2WeaponStats stats = weapon.GetEditableStats();
			stats.Stability = Math.Clamp(stats.Stability + delta, 0f, 100f);
			weapon.ApplyCustomStats(stats);
		}

		private void ApplyReload(float delta)
		{
			if (!TryGetWeapon(out Destiny2WeaponItem weapon, out _))
				return;

			Destiny2WeaponStats stats = weapon.GetEditableStats();
			stats.ReloadSpeed = Math.Clamp(stats.ReloadSpeed + delta, 0f, 100f);
			weapon.ApplyCustomStats(stats);
		}

		private void ApplyRpm(float delta)
		{
			if (!TryGetWeapon(out Destiny2WeaponItem weapon, out _))
				return;

			Destiny2WeaponStats stats = weapon.GetEditableStats();
			int rpm = Math.Max(1, stats.RoundsPerMinute + (int)delta);
			stats.RoundsPerMinute = rpm;
			weapon.ApplyCustomStats(stats);
		}

		private void ApplyMagazine(float delta)
		{
			if (!TryGetWeapon(out Destiny2WeaponItem weapon, out _))
				return;

			Destiny2WeaponStats stats = weapon.GetEditableStats();
			int magazine = Math.Max(0, stats.Magazine + (int)delta);
			stats.Magazine = magazine;
			weapon.ApplyCustomStats(stats);
		}

		private void CyclePerk(int slotIndex, int direction)
		{
			if (!TryGetWeapon(out Destiny2WeaponItem weapon, out _))
				return;

			List<Destiny2Perk> perks = GetPerksForSlot(slotIndex);
			if (perks.Count == 0)
				return;

			string currentKey = weapon.PerkKeys.Count > slotIndex ? weapon.PerkKeys[slotIndex] : null;
			int index = 0;
			if (currentKey != null)
			{
				for (int i = 0; i < perks.Count; i++)
				{
					if (perks[i].Key == currentKey)
					{
						index = i;
						break;
					}
				}
			}

			int nextIndex = (index + direction) % perks.Count;
			if (nextIndex < 0)
				nextIndex += perks.Count;

			weapon.ReplacePerkAtSlot(slotIndex, perks[nextIndex].Key);
		}

		private void CycleFrame(int direction)
		{
			if (!TryGetWeapon(out Destiny2WeaponItem weapon, out _))
				return;

			IReadOnlyList<Destiny2Perk> frames = Destiny2PerkSystem.FramePerks;
			if (frames.Count == 0)
				return;

			string currentKey = weapon.FramePerkKey;
			int index = 0;
			if (!string.IsNullOrWhiteSpace(currentKey))
			{
				for (int i = 0; i < frames.Count; i++)
				{
					if (frames[i].Key == currentKey)
					{
						index = i;
						break;
					}
				}
			}

			int nextIndex = (index + direction) % frames.Count;
			if (nextIndex < 0)
				nextIndex += frames.Count;

			weapon.ReplaceFramePerk(frames[nextIndex].Key);
		}

		private bool TryGetWeapon(out Destiny2WeaponItem weapon, out Item item)
		{
			Item candidate = selectedItem;
			if (candidate == null || candidate.IsAir)
				candidate = Main.LocalPlayer.HeldItem;

			if (candidate?.ModItem is Destiny2WeaponItem weaponItem)
			{
				weapon = weaponItem;
				item = candidate;
				return true;
			}

			weapon = null;
			item = null;
			return false;
		}

		private void SetStatText(string key, string value)
		{
			if (statTexts.TryGetValue(key, out UIText text))
				text.SetText(value);
		}

		private static List<Destiny2Perk> GetPerksForSlot(int slotIndex)
		{
			PerkSlotType slotType = slotIndex < PerkSlotTypes.Length ? PerkSlotTypes[slotIndex] : PerkSlotType.Major;
			List<Destiny2Perk> perks = new List<Destiny2Perk>();
			foreach (Destiny2Perk perk in Destiny2PerkSystem.Perks)
			{
				if (perk.SlotType == slotType)
					perks.Add(perk);
			}

			return perks;
		}
	}
}
