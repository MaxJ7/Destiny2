using Terraria;
using Terraria.ModLoader;

namespace Destiny2.Content.Buffs
{
    public class NaniteDebuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        // Use Vanilla "Bleeding" buff texture as placeholder to prevent crash
        public override string Texture => "Terraria/Images/Buff_30";

        public override void Update(NPC npc, ref int buffIndex)
        {
            // Visuals: Red static glitch
            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustDirect(npc.position, npc.width, npc.height, Terraria.ID.DustID.RedTorch, 0f, 0f, 100, default, 1.5f);
                d.noGravity = true;
                d.velocity *= 0.5f;
            }
        }
    }
}
