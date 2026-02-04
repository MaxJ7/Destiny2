using Destiny2.Common.Perks;
using Destiny2.Common.Weapons;
using Destiny2.Content.Buffs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Destiny2.Content.Projectiles
{
    public sealed class ChargedWithBlightProjectile : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_927"; // Use a dark sphere texture if possible, or just hide and use dust

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.DamageType = Destiny2WeaponElement.Arc.GetDamageClass();
            Projectile.timeLeft = 300;
            Projectile.aiStyle = -1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation += 0.1f;

            // Taken Dust Visuals
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, 31, 0f, 0f, 100, Color.Black, 1.5f);
                d.velocity = Projectile.velocity * 0.5f;
                d.noGravity = true;

                Dust d2 = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, 31, 0f, 0f, 100, Color.White, 1.0f);
                d2.velocity = -Projectile.velocity * 0.3f;
                d2.noGravity = true;
            }

            // Core light
            Lighting.AddLight(Projectile.Center, 0.5f, 0.5f, 1.0f); // Arc-ish light
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);

            // Impact Burst
            for (int i = 0; i < 20; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(6f, 6f);
                Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, 31, vel.X, vel.Y, 100, Color.Black, 2.0f);
                d.noGravity = true;

                Dust d2 = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, 31, -vel.X * 0.5f, -vel.Y * 0.5f, 100, Color.White, 1.2f);
                d2.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<BlightDebuff>(), ChargedWithBlightPerk.DotDuration * 60);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Custom draw for the "Taken" orb
            Main.instance.LoadProjectile(Projectile.type);
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 center = Projectile.Center - Main.screenPosition;

            // Draw back glow
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, center, new Rectangle(0, 0, 1, 1), Color.Black * 0.7f, 0f, new Vector2(0.5f), 40f, SpriteEffects.None, 0f);

            // Draw main sphere (simple white/black pulse)
            float scale = 1f + 0.1f * (float)System.Math.Sin(Main.GameUpdateCount * 0.2f);
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, center, new Rectangle(0, 0, 1, 1), Color.White * 0.9f, Projectile.rotation, new Vector2(0.5f), 24f * scale, SpriteEffects.None, 0f);

            return false;
        }
    }
}
