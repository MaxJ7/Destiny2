using Destiny2.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace Destiny2.Common.VFX.Particles
{
    public class SolarTrailParticle : ShaderParticle
    {
        private float _initialScale;

        public SolarTrailParticle(Vector2 position, Vector2 velocity, float scale)
        {
            Center = position;
            Velocity = velocity;
            Scale = scale;
            _initialScale = scale;
            Color = new Color(255, 150, 50); // Orange-ish
            Rotation = Main.rand.NextFloat(6.28f);
        }

        public override void Update()
        {
            base.Update();

            // Slow down quickly (air resistance)
            Velocity *= 0.9f;

            // Grow slightly then shrink
            float lifeRatio = (float)Timer / 40f; // Live for 40 frames
            if (lifeRatio >= 1f)
            {
                Kill();
                return;
            }

            // Alpha fade out using EaseInSine (slow start, fast end)
            Alpha = 1f - Easings.EaseInSine(lifeRatio);

            // Subtle rotation
            Rotation += 0.05f;

            Scale = _initialScale * (1f - lifeRatio * 0.5f);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            // Register to Bloom System
            ModContent.GetInstance<BloomSystem>().QueueBloomRecord(() =>
            {
                var tex = ModContent.Request<Texture2D>("Destiny2/Assets/Textures/SolarNoise", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                var origin = tex.Size() / 2f;

                // Draw additive glow
                Main.spriteBatch.Draw(tex, Center - Main.screenPosition, null, Color * Alpha, Rotation, origin, Scale * 0.5f, SpriteEffects.None, 0f);
            });
        }
    }
}
