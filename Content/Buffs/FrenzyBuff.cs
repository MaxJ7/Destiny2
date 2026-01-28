using Terraria;
using Terraria.ModLoader;

namespace Destiny2.Content.Buffs
{
	public sealed class FrenzyBuff : ModBuff
	{
		public override string Texture => "Destiny2/Assets/Perks/Frenzy";

		public override void SetStaticDefaults()
		{
			Main.buffNoTimeDisplay[Type] = false;
			Main.buffNoSave[Type] = true;
		}
	}
}
