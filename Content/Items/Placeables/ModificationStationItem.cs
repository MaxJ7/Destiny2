using Destiny2.Content.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Destiny2.Content.Items.Placeables
{
	public sealed class ModificationStationItem : ModItem
	{
		public override string Texture => $"Terraria/Images/Item_{ItemID.Furnace}";

		public override void SetDefaults()
		{
			Item.width = 28;
			Item.height = 14;
			Item.maxStack = 99;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.useTime = 10;
			Item.useAnimation = 15;
			Item.useTurn = true;
			Item.autoReuse = true;
			Item.consumable = true;
			Item.createTile = ModContent.TileType<ModificationStation>();
			Item.value = Item.buyPrice(silver: 50);
			Item.rare = ItemRarityID.Blue;
		}
	}
}
