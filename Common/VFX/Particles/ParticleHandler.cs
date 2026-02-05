using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace Destiny2.Common.VFX.Particles
{
    public class ParticleHandler : ModSystem
    {
        private static List<ShaderParticle> _particles = new();

        public override void Unload()
        {
            _particles.Clear();
            _particles = null;
        }

        public override void PostUpdateDusts()
        {
            if (Main.dedServ) return;

            for (int i = 0; i < _particles.Count; i++)
            {
                var p = _particles[i];
                p.Update();
                if (!p.Active)
                {
                    _particles.RemoveAt(i);
                    i--;
                }
            }
        }

        public static void SpawnParticle(ShaderParticle particle)
        {
            if (Main.dedServ || _particles == null) return;
            _particles.Add(particle);
        }

        public static void DrawParticles(SpriteBatch spriteBatch)
        {
            if (Main.dedServ || _particles == null) return;

            foreach (var p in _particles)
            {
                p.Draw(spriteBatch);
            }
        }
    }
}
