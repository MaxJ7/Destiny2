using Destiny2.Common.UI;
using Destiny2.Content.Items.Placeables;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace Destiny2.Content.Tiles
{
	public sealed class ModificationStation : ModTile
	{
		public override string Texture => $"Terraria/Images/Tiles_{TileID.Furnaces}";

		public override void SetStaticDefaults()
		{
			Main.tileFrameImportant[Type] = true;
			Main.tileLavaDeath[Type] = true;
			TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
			TileObjectData.newTile.Origin = new Point16(1, 1);
			TileObjectData.addTile(Type);
			AddMapEntry(new Color(255, 210, 64), CreateMapEntryName());
			AdjTiles = new int[] { TileID.Furnaces };
		}

		public override bool RightClick(int i, int j)
		{
			if (Destiny2ModificationStationSystem.IsOpen)
				Destiny2ModificationStationSystem.Close();
			else
				Destiny2ModificationStationSystem.Open();
			return true;
		}

		public override void MouseOver(int i, int j)
		{
			Player player = Main.LocalPlayer;
			player.noThrow = 2;
			player.cursorItemIconEnabled = true;
			player.cursorItemIconID = ModContent.ItemType<ModificationStationItem>();
		}
	}
}
