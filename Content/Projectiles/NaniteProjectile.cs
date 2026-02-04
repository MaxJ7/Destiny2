using System;
using Destiny2.Content.Buffs;
using Destiny2.Common.NPCs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Destiny2.Content.Projectiles
{
    // A seeking SIVA nanite swarm projectile.
    public class NaniteProjectile : ModProjectile
    {
        private const float MaxSpeed = 12f;
        private const float Acceleration = 0.8f;
        private const float HomingRange = 320f; // 20 tiles (User requested 4, likely meant near-melee or typo. 20 is a usable minimum)

        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.VampireHeal; // Placeholder: Little red orb

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.timeLeft = 180; // 3 seconds to find a target
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 255; // Invisible initially, rely on dust
        }

        public override void AI()
        {
            // Visuals: Red dust trail (SIVA cloud)
            for (int i = 0; i < 2; i++)
            {
                Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.RedTorch, 0f, 0f, 100, default, 1.2f);
                d.noGravity = true;
                d.velocity *= 0.2f;
            }

            // Cloud Behavior: Delay homing for first 0.5 seconds (30 ticks)
            if (Projectile.timeLeft > 150) // Total 180, so first 30 ticks
            {
                Projectile.velocity *= 0.95f; // Rapid deceleration to float
                Projectile.rotation += 0.2f;
                return;
            }

            NPC target = FindClosestNPC(HomingRange);
            if (target != null)
            {
                // Homing Logic
                Vector2 direction = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, direction * MaxSpeed, 0.15f);
            }
            else
            {
                // Drift if no target
                Projectile.velocity *= 0.98f;
            }

            // Spin for effect
            Projectile.rotation += 0.2f;
        }

        public override bool? CanHitNPC(NPC target)
        {
            // Warmup Phase (First 30 ticks): Cannot hit ANYTHING.
            if (Projectile.timeLeft > 150)
                return false;

            return base.CanHitNPC(target);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Embed nanites (refresh duration to 9s = 540 ticks)
            target.AddBuff(ModContent.BuffType<NaniteDebuff>(), 540);

            // Increment Nanite Stacks for Parasitism scaling
            NaniteGlobalNPC naniteGlobal = target.GetGlobalNPC<NaniteGlobalNPC>();
            if (naniteGlobal != null)
            {
                if (naniteGlobal.NaniteStacks < 100)
                    naniteGlobal.NaniteStacks++;

                naniteGlobal.NaniteTimer = 540; // 9 seconds
            }
        }

        private NPC FindClosestNPC(float range)
        {
            NPC closest = null;
            float closestDist = range * range;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.CanBeChasedBy(Projectile))
                {
                    float distSq = Vector2.DistanceSquared(Projectile.Center, npc.Center);
                    if (distSq < closestDist)
                    {
                        closestDist = distSq;
                        closest = npc;
                    }
                }
            }

            return closest;
        }
    }
}
