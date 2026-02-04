using System;
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
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.penetrate = -1; // Infinite piercing
            Projectile.DamageType = Destiny2WeaponElement.Arc.GetDamageClass();
            Projectile.timeLeft = 300;
            Projectile.aiStyle = -1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;

            // Immunity frames for piercing
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30; // 0.5s between hits on same target
        }

        public override void AI()
        {
            // Update rotation to match velocity
            Projectile.rotation = Projectile.velocity.ToRotation();

            // 1. Leading Edge (Rounded Arc)
            float coreScale = 1.0f + 0.15f * (float)Math.Sin(Main.GameUpdateCount * 0.15f);
            for (int i = -4; i <= 4; i++)
            {
                float angleOffset = i * (MathHelper.PiOver2 / 4f);
                Vector2 pos = Projectile.Center + (Projectile.rotation + angleOffset).ToRotationVector2() * 14f * coreScale;

                Dust d = Dust.NewDustDirect(pos, 0, 0, DustID.Wraith, 0f, 0f, 180, Color.Black, 1.1f);
                d.velocity = Projectile.velocity * 0.05f;
                d.noGravity = true;
                d.fadeIn = 0.4f;
            }

            // 2. Pointed Tapering Trail
            // We spawn dust in a cone behind the projectile, where the width tapers off
            for (int i = 0; i < 3; i++)
            {
                float distanceBack = Main.rand.NextFloat(5f, 50f); // How far back the dust is
                float taperWidth = (1f - (distanceBack / 60f)) * 12f; // Narrower as it goes further back

                Vector2 trailPos = Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.Zero) * distanceBack;
                trailPos += Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-taperWidth, taperWidth);

                Dust d = Dust.NewDustDirect(trailPos, 0, 0, DustID.Wraith, 0f, 0f, 150, Color.Black, 1.4f * (1f - distanceBack / 60f));
                d.velocity = -Projectile.velocity * 0.15f + Main.rand.NextVector2Circular(0.5f, 0.5f);
                d.noGravity = true;
                d.fadeIn = 1.0f;

                // Occasional white whisps inside the dark tail
                if (Main.rand.NextBool(6))
                {
                    Dust glint = Dust.NewDustDirect(trailPos, 0, 0, DustID.SilverCoin, 0f, 0f, 200, Color.White, 0.5f);
                    glint.velocity = d.velocity * 1.2f;
                    glint.noGravity = true;
                }
            }

            // 3. Central Dense Core (Rounded)
            if (Main.rand.NextBool(2))
            {
                Dust core = Dust.NewDustDirect(Projectile.Center + Main.rand.NextVector2Circular(10f, 10f), 0, 0, DustID.Wraith, 0f, 0f, 100, Color.Black, 1.8f);
                core.velocity = Projectile.velocity * 0.1f;
                core.noGravity = true;
            }

            // Light (Faint dark glow)
            Lighting.AddLight(Projectile.Center, 0.1f, 0f, 0.15f);
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);

            // Impact Burst
            for (int i = 0; i < 40; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(10f, 10f);
                Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Wraith, vel.X, vel.Y, 150, Color.Black, 2.5f);
                d.noGravity = true;
                d.fadeIn = 1.5f;

                if (i % 3 == 0)
                {
                    Dust d2 = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.SilverCoin, -vel.X * 0.4f, -vel.Y * 0.4f, 100, Color.White, 1.2f);
                    d2.noGravity = true;
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<BlightDebuff>(), ChargedWithBlightPerk.DotDuration * 60);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // We draw nothing. All visuals are dust-based.
            return false;
        }
    }
}
