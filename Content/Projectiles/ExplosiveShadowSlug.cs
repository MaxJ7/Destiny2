// ExplosiveShadowSlug.cs - Updated to use Bullet-style ribbon trails
using System;
using System.Collections.Generic;
using Destiny2.Common.Perks;
using Destiny2.Common.Weapons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Destiny2.Content.Projectiles
{
    public sealed class ExplosiveShadowSlug : ModProjectile
    {
        private const float MaxDistance = 1200f;
        private const float Speed = 28f;
        private const int ExtraUpdates = 3;
        private const int TrailCacheMax = 40;

        // Flare/Sticky constants
        private const float FlareLength = 24f;
        private const float FlareThickness = 3f;
        private const int FlareDustCount = 6;
        private const int StickTime = 60 * 10;

        private Vector2 spawnPosition;
        private bool initialized;
        private readonly List<Vector2> trailCache = new List<Vector2>();
        private float maxDistance = MaxDistance;

        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.Bullet}";

        public override void SetDefaults()
        {
            Projectile.width = 6;
            Projectile.height = 6;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.timeLeft = 60;
            Projectile.aiStyle = -1;
            Projectile.extraUpdates = ExtraUpdates;
            Projectile.tileCollide = true;
            Projectile.hide = true; // Use ribbon rendering
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Item sourceItem = null;
            if (source is EntitySource_ItemUse itemUse)
                sourceItem = itemUse.Item;
            else if (source is EntitySource_ItemUse_WithAmmo itemUseWithAmmo)
                sourceItem = itemUseWithAmmo.Item;

            if (sourceItem?.ModItem is Destiny2WeaponItem weaponItem)
            {
                float maxFalloffTiles = weaponItem.GetMaxFalloffTiles();
                if (maxFalloffTiles > 0f)
                    maxDistance = Math.Max(16f, maxFalloffTiles * 3f * 16f);
            }

            spawnPosition = Projectile.Center;
            trailCache.Clear();
            if (Projectile.Center != Vector2.Zero)
                trailCache.Add(Projectile.Center);

            if (Projectile.velocity != Vector2.Zero)
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * Speed;

            initialized = true;
        }

        public bool IsStickingToTarget
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value ? 1f : 0f;
        }

        public int TargetWhoAmI
        {
            get => (int)Projectile.ai[1];
            set => Projectile.ai[1] = value;
        }

        public override void AI()
        {
            if (IsStickingToTarget)
            {
                StickyAI();
                // Clear trail cache slowly or instantly? 
                // Let's keep the last segment for a few frames or just clear it so the ribbon disappears.
                if (trailCache.Count > 0) trailCache.RemoveAt(0);
                return;
            }

            if (!initialized) return;

            // Cache Logic (Same as Bullet.cs for perfect parity)
            if (trailCache.Count > 0)
            {
                Vector2 lastPoint = trailCache[trailCache.Count - 1];
                float distSq = Vector2.DistanceSquared(lastPoint, Projectile.Center);

                if (distSq > 100f * 100f)
                {
                    trailCache.Clear();
                    trailCache.Add(Projectile.Center);
                }
                else if (distSq >= 2f * 2f)
                {
                    trailCache.Add(Projectile.Center);
                }
            }
            else
            {
                trailCache.Add(Projectile.Center);
            }

            if (trailCache.Count > TrailCacheMax)
                trailCache.RemoveAt(0);

            // Distance Check
            if (Vector2.Distance(spawnPosition, Projectile.Center) >= maxDistance)
            {
                Projectile.Kill();
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (IsStickingToTarget) return false;

            Vector2 start = Projectile.Center - Projectile.velocity;
            Vector2 end = Projectile.Center;
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                start,
                end,
                Projectile.width,
                ref collisionPoint);
        }

        private void StickyAI()
        {
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.friendly = false;

            int npcTarget = TargetWhoAmI;
            if (Projectile.timeLeft <= 0 || npcTarget < 0 || npcTarget >= Main.maxNPCs)
            {
                Projectile.Kill();
                return;
            }

            NPC target = Main.npc[npcTarget];
            if (!target.active || target.dontTakeDamage || target.friendly)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = target.Center + Projectile.velocity;
            Projectile.gfxOffY = target.gfxOffY;

            // Visual Flare Logic
            Vector2 flareDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            SpawnStickyFlare(Projectile.Center, flareDirection);
        }

        private void SpawnStickyFlare(Vector2 hitPoint, Vector2 direction)
        {
            Vector2 perpendicular = direction.RotatedBy(MathHelper.PiOver2);
            if (Projectile.timeLeft % 2 != 0) return;

            for (int i = 0; i < FlareDustCount; i++)
            {
                float t = i / (float)(FlareDustCount - 1);
                Vector2 linePos = hitPoint + direction * (t * FlareLength);
                float randomOffset = Main.rand.NextFloat(-FlareThickness, FlareThickness);
                Vector2 finalPos = linePos + perpendicular * randomOffset;
                Vector2 driftVelocity = new Vector2(Main.rand.NextFloat(-0.1f, 0.1f), Main.rand.NextFloat(-0.3f, -0.05f));
                float scaleMult = 1f - (t * 0.25f);

                Dust light = Dust.NewDustPerfect(finalPos, DustID.WhiteTorch, driftVelocity, 100, Color.White, 1.3f * scaleMult);
                light.noGravity = true;
                light.fadeIn = 1.1f;

                Vector2 darkPos = finalPos - direction * 2f + perpendicular * Main.rand.NextFloat(-1.5f, 1.5f);
                Dust dark = Dust.NewDustPerfect(darkPos, DustID.Wraith, driftVelocity * 0.8f, 180, new Color(0, 177, 255), 1.1f * scaleMult);
                dark.noGravity = true;
                dark.fadeIn = 0.9f;
            }

            float pulse = (float)Math.Sin(Main.GameUpdateCount * 0.2f) * 0.03f + 0.08f;
            Lighting.AddLight(hitPoint, pulse, pulse, pulse * 1.2f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (IsStickingToTarget) return;

            IsStickingToTarget = true;
            TargetWhoAmI = target.whoAmI;
            Projectile.tileCollide = false;
            Projectile.friendly = false;
            Projectile.timeLeft = StickTime;
            Projectile.damage = 0;
            Projectile.velocity = Projectile.Center - target.Center;
            Projectile.netUpdate = true;
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public (IReadOnlyList<Vector2> trail, Destiny2WeaponElement element, Vector2 center, float rotation) GetDrawData()
        {
            return (new List<Vector2>(trailCache), Destiny2WeaponElement.Void, Projectile.Center, Projectile.velocity.ToRotation());
        }
    }
}
