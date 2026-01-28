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

			if (weaponItem.MagazineSize <= 0)
				return;

			Vector2 basePos = player.MountedCenter - Main.screenPosition;
			DrawMagazineCount(spriteBatch, weaponItem, basePos);

			if (weaponItem.IsReloading && weaponItem.ReloadTimerMax > 0)
				DrawReloadBar(spriteBatch, weaponItem, basePos);
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
	}
}
