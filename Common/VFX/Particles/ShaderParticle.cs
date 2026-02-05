using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace Destiny2.Common.VFX.Particles
{
    public abstract class ShaderParticle
    {
        public bool Active = true;
        public Vector2 Center;
        public Vector2 Velocity;
        public float Rotation;
        public float Scale = 1f;
        public Color Color = Color.White;
        public float Alpha = 1f;
        public int Timer = 0;

        /// <summary>
        /// Update logic. Return false to kill the particle.
        /// </summary>
        public virtual void Update()
        {
            Center += Velocity;
            Timer++;
        }

        public virtual void Draw(SpriteBatch spriteBatch) { }

        public virtual void DrawWithShader(SpriteBatch spriteBatch) { }

        public void Kill() => Active = false;
    }
}
