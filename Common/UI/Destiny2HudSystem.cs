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

        private const float PerkHudMarginX = 20f;
        private const float PerkHudMarginY = 20f;
        private const float PerkIconSize = 40f;
        private const float PerkEntryPadding = 8f;
        private const float PerkEntryWidth = 240f; // Wider for names
        private const float PerkEntryHeight = PerkIconSize + (PerkEntryPadding * 2f);
        private const float PerkEntryGap = 8f;
        private const float PerkBarHeight = 6f;
        private const float PerkNameScale = 0.9f;
        private const float PerkTimeScale = 0.75f;
        private const float PerkStackScale = 0.85f;

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

            if (weaponItem is CombatBowWeaponItem bowItem)
                DrawBowChargeBar(spriteBatch, bowItem, basePos);

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

        private static void DrawBowChargeBar(SpriteBatch spriteBatch, CombatBowWeaponItem bowItem, Vector2 basePos)
        {
            float progress = bowItem.DrawRatio;
            if (progress <= 0f) return;

            int barWidth = 40;
            int barHeight = 4;
            Vector2 barPos = basePos + new Vector2(-barWidth * 0.5f, 36f); // Below reload bar position

            // Background
            Rectangle backRect = new Rectangle((int)barPos.X, (int)barPos.Y, barWidth, barHeight);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, backRect, Color.Black * 0.6f);

            // Fill
            int fillWidth = (int)((barWidth - 2) * progress);
            if (fillWidth > 0)
            {
                Rectangle fillRect = new Rectangle((int)barPos.X + 1, (int)barPos.Y + 1, fillWidth, barHeight - 2);

                Color fillColor = Color.White;
                if (bowItem.IsPerfectDraw) fillColor = Color.Gold;
                else if (bowItem.IsOverdrawn) fillColor = Color.Red;

                spriteBatch.Draw(TextureAssets.MagicPixel.Value, fillRect, fillColor);
            }

            // Draw "Perfect Draw" Marker (Visual Guide)
            // Perfect draw is at 100% charge for a short window.
            // We could just change color (which we did).

            // Millisecond Counter
            if (bowItem.CurrentDrawTicks < bowItem.MaxDrawTicks)
            {
                int remainingMs = (int)Math.Max(0, (bowItem.MaxDrawTicks - bowItem.CurrentDrawTicks) * (1000f / 60f));
                string msText = $"{remainingMs}ms";
                Vector2 msSize = FontAssets.MouseText.Value.MeasureString(msText) * 0.7f;
                Vector2 msPos = barPos + new Vector2((barWidth - msSize.X) * 0.5f, 6f);
                Utils.DrawBorderString(spriteBatch, msText, msPos, Color.White, 0.7f);
            }
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
                    if (Destiny2PerkSystem.TryGet(nameof(EyesUpGuardianPerk), out Destiny2Perk perk) && !string.IsNullOrWhiteSpace(perk.IconTexture))
                        ActivePerkEntries.Add(new Destiny2WeaponItem.PerkHudEntry(perk.DisplayName, perk.IconTexture, 1, 1, stacks, true));
                }
            }

            if (ActivePerkEntries.Count == 0)
                return;

            // Anchor to Bottom-Left
            float rowHeight = PerkEntryHeight + PerkEntryGap;
            float totalHeight = (ActivePerkEntries.Count * rowHeight) - PerkEntryGap;

            // Adjust for screen height and safe areas
            Vector2 panelStartPos = new Vector2(PerkHudMarginX, Main.screenHeight - PerkHudMarginY - totalHeight);

            for (int i = 0; i < ActivePerkEntries.Count; i++)
            {
                Rectangle entryRect = new Rectangle(
                    (int)panelStartPos.X,
                    (int)(panelStartPos.Y + i * rowHeight),
                    (int)PerkEntryWidth,
                    (int)PerkEntryHeight);

                DrawPerkEntry(spriteBatch, ActivePerkEntries[i], entryRect);
            }
        }


        private static void DrawPerkEntry(SpriteBatch spriteBatch, Destiny2WeaponItem.PerkHudEntry entry, Rectangle bounds)
        {
            // Background
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, bounds, PerkEntryBackColor);
            Rectangle topBar = new Rectangle(bounds.X, bounds.Y, bounds.Width, 2);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, topBar, HeaderColor * 0.5f);

            // Icon
            Rectangle iconRect = new Rectangle(
                bounds.X + (int)PerkEntryPadding,
                bounds.Y + (int)PerkEntryPadding,
                (int)PerkIconSize,
                (int)PerkIconSize);

            if (!string.IsNullOrWhiteSpace(entry.IconTexture))
            {
                Texture2D icon = ModContent.Request<Texture2D>(entry.IconTexture).Value;
                spriteBatch.Draw(icon, iconRect, Color.White);
            }
            DrawBorder(spriteBatch, iconRect, PerkPanelBorderColor * 0.5f);

            // Text Area
            float textStartX = iconRect.Right + 10f;

            // Perk Name
            string nameText = entry.DisplayName.ToUpper();
            Vector2 nameSize = FontAssets.MouseText.Value.MeasureString(nameText) * PerkNameScale;
            Vector2 namePos = new Vector2(textStartX, bounds.Y + PerkEntryPadding - 2f);
            Utils.DrawBorderString(spriteBatch, nameText, namePos, HeaderColor, PerkNameScale);

            // Timer Bar
            float barWidth = bounds.Width - (textStartX - bounds.X) - PerkEntryPadding;
            Rectangle barBack = new Rectangle((int)textStartX, (int)(namePos.Y + nameSize.Y + 4f), (int)barWidth, (int)PerkBarHeight);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, barBack, PerkBarBackColor);

            float progress = entry.MaxTimer > 0 ? entry.Timer / (float)entry.MaxTimer : 0f;
            progress = MathHelper.Clamp(progress, 0f, 1f);

            if (progress > 0f)
            {
                Rectangle fillRect = new Rectangle(barBack.X + 1, barBack.Y + 1, (int)((barBack.Width - 2) * progress), barBack.Height - 2);
                Color fillColor = Color.Lerp(PerkLowTimeColor, Color.White, progress);
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, fillRect, fillColor);
            }

            // Timer Text (Optional but requested "Timer")
            float remainingSeconds = entry.Timer / 60f;
            string timerText = $"{remainingSeconds:0.0}s";
            Vector2 timerSize = FontAssets.MouseText.Value.MeasureString(timerText) * PerkTimeScale;
            Vector2 timerPos = new Vector2(barBack.Right - timerSize.X, barBack.Bottom + 2f);
            Utils.DrawBorderString(spriteBatch, timerText, timerPos, Color.White * 0.8f, PerkTimeScale);

            // Stacks
            if (entry.ShowStacks && entry.Stacks > 0)
            {
                string stackText = $"x{entry.Stacks}";
                Vector2 stackSize = FontAssets.MouseText.Value.MeasureString(stackText) * PerkStackScale;
                Vector2 stackPos = new Vector2(iconRect.Right - stackSize.X * 0.5f, iconRect.Bottom - stackSize.Y * 0.8f);
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
