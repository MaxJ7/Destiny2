using Terraria;
using Terraria.ModLoader;

namespace Destiny2.Common.Perks
{
	public sealed class KineticTremorsGlobalNPC : GlobalNPC
	{
		public override bool InstancePerEntity => true;

		public int KineticTremorsCooldown;

		public override void SetDefaults(NPC npc)
		{
			KineticTremorsCooldown = 0;
		}

		public override void PostAI(NPC npc)
		{
			if (KineticTremorsCooldown > 0)
				KineticTremorsCooldown--;
		}
	}
}
