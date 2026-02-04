using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace Destiny2.Common.VFX
{
    public class KineticShockwaveRenderer : ModSystem
    {
        public static void TriggerPulse(Vector2 center, float pulseOption = 0f)
        {
            // Minimal restoration: Spawn basic dusts to represent the pulse
            for (int i = 0; i < 20; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(4f, 4f);
                Dust.NewDust(center, 0, 0, Terraria.ID.DustID.Smoke, velocity.X, velocity.Y, 100, Color.White, 1.5f);
            }
        }
    }
}
