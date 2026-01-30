using Terraria;
using Terraria.ModLoader;

namespace Destiny2.Common.Perks
{
	public sealed class KineticTremorsGlobalNPC : GlobalNPC
	{
		public int KineticTremorsCooldown;

		public override bool InstancePerEntity => true;

		public override void ResetEffects(NPC npc)
		{
			if (KineticTremorsCooldown < 0)
				KineticTremorsCooldown = 0;
		}

		public override void PostAI(NPC npc)
		{
			if (KineticTremorsCooldown > 0)
				KineticTremorsCooldown--;
		}
	}
}
