using System;
using Destiny2.Common.Perks;
using Destiny2.Common.VFX;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace Destiny2.Common.NPCs
{
    public sealed class NaniteGlobalNPC : GlobalNPC
    {
        public int NaniteStacks;
        public int NaniteTimer;

        public override bool InstancePerEntity => true;

        public override void ResetEffects(NPC npc)
        {
            // Timer decrement logic handled in PostAI or simpler here
        }

        public override void PostAI(NPC npc)
        {
            if (NaniteTimer > 0)
            {
                NaniteTimer--;
                if (NaniteTimer <= 0)
                {
                    NaniteStacks = 0;
                }
            }
        }
    }
}
