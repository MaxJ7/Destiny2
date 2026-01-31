using System;
using System.Collections.Generic;
using Destiny2.Common.Perks;
using Destiny2.Common.Players;
using Destiny2.Common.Weapons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace Destiny2.Common.UI
{
	public sealed class Destiny2HudSystem : ModSystem
	{
		private static readonly Color HeaderColor = new Color(255, 212, 89);
		private static readonly Color PerkPanelBackColor = new Color(18, 18, 20) * 0.85f;
		private static readonly Color PerkPanelTopColor = new Color(35, 35, 40) * 0.85f;
		private static readonly Color PerkPanelBorderColor = new Color(120, 106, 72) * 0.85f;
		private static readonly Color PerkEntryBackColor = new Color(24, 24, 28) * 0.9f;
		private static readonly Color PerkEntryTopColor = new Color(45, 45, 52) * 0.6f;
		private static readonly Color PerkEntryShadowColor = new Color(0, 0, 0) * 0.35f;
		private static readonly Color PerkBarBackColor = new Color(0, 0, 0) * 0.6f;
		private static readonly Color PerkLowTimeColor = new Color(210, 80, 60);
		private static readonly Color PerkStackBackColor = new Color(10, 10, 12) * 0.9f;
		private static readonly List<Destiny2WeaponItem.PerkHudEntry> ActivePerkEntries = new List<Destiny2WeaponItem.PerkHudEntry>();

		private const float PerkHudOffsetY = 44f;
		private const float PerkIconSize = 26f;
		private const float PerkEntryPadding = 6f;
		private const float PerkEntryWidth = PerkIconSize + (PerkEntryPadding * 2f);
		private const float PerkEntryHeight = PerkIconSize + 26f;
		private const float PerkEntryGap = 6f;
		private const float PerkPanelPadding = 6f;
		private const float PerkBarHeight = 4f;
		private const float PerkTimeScale = 0.65f;
		private const float PerkStackScale = 0.7f;
		private const int MaxPerkEntriesPerRow = 6;

		public override void PostDrawInterface(SpriteBatch spriteBatch)
		{
			if (Main.gameMenu)
				return;

			Player player = Main.LocalPlayer;
			if (!player.active || player.dead)
				return;

			Item heldItem = player.HeldItem;
			if (heldItem?.ModItem is not Destiny2WeaponItem weaponItem)
				return;

			Vector2 basePos = player.MountedCenter - Main.screenPosition;
			if (weaponItem.MagazineSize > 0)
				DrawMagazineCount(spriteBatch, weaponItem, basePos);

			if (weaponItem.IsReloading && weaponItem.ReloadTimerMax > 0)
				DrawReloadBar(spriteBatch, weaponItem, basePos);

			DrawPerkBuffs(spriteBatch, weaponItem, basePos);
		}

		private static void DrawMagazineCount(SpriteBatch spriteBatch, Destiny2WeaponItem weaponItem, Vector2 basePos)
		{
			string ammoText = weaponItem.CurrentMagazine.ToString();
			Vector2 textSize = FontAssets.MouseText.Value.MeasureString(ammoText);
			Vector2 textPos = basePos + new Vector2(-textSize.X * 0.5f, -54f);

			Utils.DrawBorderString(spriteBatch, ammoText, textPos, HeaderColor);
		}

		private static void DrawReloadBar(SpriteBatch spriteBatch, Destiny2WeaponItem weaponItem, Vector2 basePos)
		{
			float progress = 1f - weaponItem.ReloadTimer / (float)weaponItem.ReloadTimerMax;
			progress = MathHelper.Clamp(progress, 0f, 1f);

			int barWidth = 52;
			int barHeight = 6;
			Vector2 barPos = basePos + new Vector2(-barWidth * 0.5f, 28f);

			Rectangle backRect = new Rectangle((int)barPos.X, (int)barPos.Y, barWidth, barHeight);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, backRect, Color.Black * 0.6f);

			int fillWidth = (int)((barWidth - 2) * progress);
			if (fillWidth > 0)
			{
				Rectangle fillRect = new Rectangle((int)barPos.X + 1, (int)barPos.Y + 1, fillWidth, barHeight - 2);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, fillRect, HeaderColor);
			}

			float remainingSeconds = weaponItem.ReloadTimer / 60f;
			string timerText = $"{remainingSeconds:0.0}s";
			Vector2 timerSize = FontAssets.MouseText.Value.MeasureString(timerText);
			Vector2 timerPos = barPos + new Vector2((barWidth - timerSize.X) * 0.5f, -16f);

			Utils.DrawBorderString(spriteBatch, timerText, timerPos, Color.White);
		}

		private static void DrawPerkBuffs(SpriteBatch spriteBatch, Destiny2WeaponItem weaponItem, Vector2 basePos)
		{
			ActivePerkEntries.Clear();
			weaponItem.AppendPerkHudEntries(ActivePerkEntries);

			// Eyes Up Guardian: show stacks when player has them and weapon has the perk
			Player player = Main.LocalPlayer;
			bool hasEyesUp = false;
			foreach (var p in weaponItem.GetPerks())
			{
				if (p is EyesUpGuardianPerk) { hasEyesUp = true; break; }
			}
			if (player != null && hasEyesUp)
			{
				Destiny2Player modPlayer = player.GetModPlayer<Destiny2Player>();
				int stacks = modPlayer?.GetEyesUpGuardianStacks() ?? 0;
				if (stacks > 0)
				{
					string iconTexture = Destiny2PerkSystem.TryGet(nameof(EyesUpGuardianPerk), out Destiny2Perk perk) ? perk.IconTexture : null;
					if (!string.IsNullOrWhiteSpace(iconTexture))
						ActivePerkEntries.Add(new Destiny2WeaponItem.PerkHudEntry(iconTexture, 1, 1, stacks, true));
				}
			}

			if (ActivePerkEntries.Count == 0)
				return;

			int entriesPerRow = Math.Min(MaxPerkEntriesPerRow, ActivePerkEntries.Count);
			int rowCount = (int)Math.Ceiling(ActivePerkEntries.Count / (float)entriesPerRow);
			float panelWidth = (entriesPerRow * PerkEntryWidth) + ((entriesPerRow - 1) * PerkEntryGap) + (PerkPanelPadding * 2f);
			float panelHeight = (rowCount * PerkEntryHeight) + ((rowCount - 1) * PerkEntryGap) + (PerkPanelPadding * 2f);

			Vector2 panelPos = basePos + new Vector2(-panelWidth * 0.5f, PerkHudOffsetY);
			Rectangle panelRect = new Rectangle((int)panelPos.X, (int)panelPos.Y, (int)panelWidth, (int)panelHeight);
			DrawPerkPanel(spriteBatch, panelRect);

			int entryIndex = 0;
			for (int row = 0; row < rowCount; row++)
			{
				int rowEntries = Math.Min(entriesPerRow, ActivePerkEntries.Count - (row * entriesPerRow));
				float rowWidth = (rowEntries * PerkEntryWidth) + ((rowEntries - 1) * PerkEntryGap);
				float rowStartX = panelPos.X + (panelWidth - rowWidth) * 0.5f;
				float rowStartY = panelPos.Y + PerkPanelPadding + row * (PerkEntryHeight + PerkEntryGap);

				for (int col = 0; col < rowEntries; col++)
				{
					Rectangle entryRect = new Rectangle(
						(int)(rowStartX + col * (PerkEntryWidth + PerkEntryGap)),
						(int)rowStartY,
						(int)PerkEntryWidth,
						(int)PerkEntryHeight);
					DrawPerkEntry(spriteBatch, ActivePerkEntries[entryIndex], entryRect);
					entryIndex++;
				}
			}
		}

		private static void DrawPerkPanel(SpriteBatch spriteBatch, Rectangle bounds)
		{
			Rectangle shadow = bounds;
			shadow.Offset(2, 2);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, shadow, Color.Black * 0.4f);

			spriteBatch.Draw(TextureAssets.MagicPixel.Value, bounds, PerkPanelBackColor);
			Rectangle top = new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height / 2);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, top, PerkPanelTopColor);
			DrawBorder(spriteBatch, bounds, PerkPanelBorderColor);
		}

		private static void DrawPerkEntry(SpriteBatch spriteBatch, Destiny2WeaponItem.PerkHudEntry entry, Rectangle bounds)
		{
			Rectangle shadow = bounds;
			shadow.Offset(1, 1);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, shadow, PerkEntryShadowColor);

			spriteBatch.Draw(TextureAssets.MagicPixel.Value, bounds, PerkEntryBackColor);
			Rectangle top = new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height / 2);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, top, PerkEntryTopColor);
			DrawBorder(spriteBatch, bounds, PerkPanelBorderColor * 0.8f);

			Rectangle iconRect = new Rectangle(
				bounds.X + (int)PerkEntryPadding,
				bounds.Y + (int)PerkEntryPadding,
				(int)PerkIconSize,
				(int)PerkIconSize);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, iconRect, Color.Black * 0.4f);
			if (!string.IsNullOrWhiteSpace(entry.IconTexture))
			{
				Texture2D icon = ModContent.Request<Texture2D>(entry.IconTexture).Value;
				spriteBatch.Draw(icon, iconRect, Color.White);
			}

			float progress = entry.MaxTimer > 0 ? entry.Timer / (float)entry.MaxTimer : 0f;
			progress = MathHelper.Clamp(progress, 0f, 1f);
			Rectangle barBack = new Rectangle(bounds.X + 4, bounds.Bottom - (int)PerkBarHeight, bounds.Width - 8, (int)PerkBarHeight);
			if (barBack.Width > 0 && barBack.Height > 0)
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, barBack, PerkBarBackColor);

			int fillWidth = barBack.Width > 2 ? (int)((barBack.Width - 2) * progress) : 0;
			if (fillWidth > 0)
			{
				Rectangle fillRect = new Rectangle(barBack.X + 1, barBack.Y + 1, fillWidth, barBack.Height - 2);
				Color fillColor = Color.Lerp(PerkLowTimeColor, HeaderColor, progress);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, fillRect, fillColor);
			}

			float remainingSeconds = entry.Timer / 60f;
			string timerText = $"{remainingSeconds:0.0}s";
			Vector2 timerSize = FontAssets.MouseText.Value.MeasureString(timerText) * PerkTimeScale;
			float barTop = barBack.Y;
			float timerY = iconRect.Bottom + 1f;
			float maxTimerY = barTop - timerSize.Y - 1f;
			if (timerY > maxTimerY)
				timerY = maxTimerY;
			Vector2 timerPos = new Vector2(bounds.Center.X - timerSize.X * 0.5f, timerY);
			Utils.DrawBorderString(spriteBatch, timerText, timerPos, Color.White, PerkTimeScale);

			if (entry.ShowStacks && entry.Stacks > 0)
			{
				string stackText = entry.Stacks.ToString();
				Vector2 stackSize = FontAssets.MouseText.Value.MeasureString(stackText) * PerkStackScale;
				float padding = 2f;
				Rectangle badgeRect = new Rectangle(
					(int)(iconRect.Right - stackSize.X - (padding * 2f) + 2f),
					(int)(iconRect.Y - 4f),
					(int)(stackSize.X + padding * 2f),
					(int)(stackSize.Y + padding * 2f));
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, badgeRect, PerkStackBackColor);
				DrawBorder(spriteBatch, badgeRect, HeaderColor * 0.9f);
				Vector2 stackPos = new Vector2(badgeRect.X + padding, badgeRect.Y + padding - 1f);
				Utils.DrawBorderString(spriteBatch, stackText, stackPos, Color.White, PerkStackScale);
			}
		}

		private static void DrawBorder(SpriteBatch spriteBatch, Rectangle bounds, Color color)
		{
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(bounds.X, bounds.Y, bounds.Width, 1), color);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(bounds.X, bounds.Bottom - 1, bounds.Width, 1), color);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(bounds.X, bounds.Y, 1, bounds.Height), color);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(bounds.Right - 1, bounds.Y, 1, bounds.Height), color);
		}
	}
}
