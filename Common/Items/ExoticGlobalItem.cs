using Destiny2.Common.Rarities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace Destiny2.Common.Items
{
	public sealed class ExoticGlobalItem : GlobalItem
	{
		private static readonly Color ExoticColor = new Color(255, 210, 64);

		public override void ModifyTooltips(Item item, System.Collections.Generic.List<TooltipLine> tooltips)
		{
			if (item.rare != ModContent.RarityType<ExoticRarity>())
				return;

			tooltips.Add(new TooltipLine(Mod, "ExoticQualifier", "Exotic")
			{
				OverrideColor = ExoticColor
			});
		}
	}
}
