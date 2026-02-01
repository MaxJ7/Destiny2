using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace Destiny2.Common.Rarities
{
	/// <summary>Exotic rarity—golden glow like vanilla Expert/Master rarities.</summary>
	public sealed class ExoticRarity : ModRarity
	{
		private static readonly Color ExoticDark = new(180, 140, 40);
		private static readonly Color ExoticBright = new(255, 255, 200);

		/// <summary>Pulsating golden glow—similar to Master's fiery amber animation.</summary>
		public override Color RarityColor
		{
			get
			{
				float t = (float)((Math.Sin(Main.GlobalTimeWrappedHourly * 6f) + 1f) * 0.5f);
				return Color.Lerp(ExoticDark, ExoticBright, t);
			}
		}
	}
}
