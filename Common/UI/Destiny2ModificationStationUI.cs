using System;
using Destiny2.Common.Items;
using Destiny2.Common.Weapons;
using Destiny2.Content.Tiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;

namespace Destiny2.Common.UI
{
	public sealed class Destiny2ModificationStationUI : UIState
	{
		private const float CloseDistanceTiles = 6f;

		private Destiny2ItemSlot weaponSlot;
		private Destiny2ItemSlot catalystSlot;
		private UIText statusText;

		public override void OnInitialize()
		{
			UIPanel panel = new UIPanel
			{
				Width = { Pixels = 300f },
				Height = { Pixels = 180f },
				HAlign = 0.5f,
				VAlign = 0.5f
			};
			Append(panel);

			UIText title = new UIText("Modification Station");
			title.Left.Set(12f, 0f);
			title.Top.Set(10f, 0f);
			panel.Append(title);

			UIText weaponLabel = new UIText("Weapon");
			weaponLabel.Left.Set(30f, 0f);
			weaponLabel.Top.Set(44f, 0f);
			panel.Append(weaponLabel);

			weaponSlot = new Destiny2ItemSlot(item => item.ModItem is Destiny2WeaponItem);
			weaponSlot.Left.Set(30f, 0f);
			weaponSlot.Top.Set(70f, 0f);
			panel.Append(weaponSlot);

			UIText catalystLabel = new UIText("Catalyst");
			catalystLabel.Left.Set(180f, 0f);
			catalystLabel.Top.Set(44f, 0f);
			panel.Append(catalystLabel);

			catalystSlot = new Destiny2ItemSlot(item => item.ModItem is Destiny2CatalystItem);
			catalystSlot.Left.Set(190f, 0f);
			catalystSlot.Top.Set(70f, 0f);
			panel.Append(catalystSlot);

			statusText = new UIText("Insert weapon and catalyst.");
			statusText.Left.Set(12f, 0f);
			statusText.Top.Set(134f, 0f);
			panel.Append(statusText);
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			if (!IsPlayerNearStation())
			{
				Destiny2ModificationStationSystem.Close();
				return;
			}

			if (weaponSlot?.Item?.ModItem is not Destiny2WeaponItem weapon)
			{
				statusText.SetText("Insert a Destiny2 weapon.");
				return;
			}

			if (catalystSlot?.Item?.ModItem is not Destiny2CatalystItem catalyst)
			{
				statusText.SetText(weapon.HasCatalyst ? "Catalyst installed." : "Insert a catalyst.");
				return;
			}

			if (weapon.TryApplyCatalyst(catalyst))
			{
				catalystSlot.Item.TurnToAir();
				statusText.SetText("Catalyst applied.");
			}
			else
			{
				statusText.SetText("Cannot apply catalyst.");
			}
		}

		private static bool IsPlayerNearStation()
		{
			Player player = Main.LocalPlayer;
			if (player == null || !player.active)
				return false;

			int range = (int)Math.Ceiling(CloseDistanceTiles);
			Point center = player.Center.ToTileCoordinates();
			for (int x = center.X - range; x <= center.X + range; x++)
			{
				for (int y = center.Y - range; y <= center.Y + range; y++)
				{
					if (!WorldGen.InWorld(x, y, 1))
						continue;

					Tile tile = Framing.GetTileSafely(x, y);
					if (tile.HasTile && tile.TileType == ModContent.TileType<ModificationStation>())
						return true;
				}
			}

			return false;
		}
	}
}
