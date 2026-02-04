using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace Destiny2.Common.VFX
{
    public class SolarExplosionRenderer : ModSystem
    {
        public static void TriggerExplosion(Vector2 center, bool isIgnition)
        {
            // Minimal restoration: Spawn solar dusts
            int dustCount = isIgnition ? 60 : 30;
            float scale = isIgnition ? 2.5f : 1.5f;

            for (int i = 0; i < dustCount; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(6f, 6f);
                Dust d = Dust.NewDustPerfect(center, Terraria.ID.DustID.Torch, velocity, 0, new Color(255, 100, 0), scale);
                d.noGravity = true;
            }
        }
    }
}
