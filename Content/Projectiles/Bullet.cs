using System;
using System.Collections.Generic;
using Destiny2.Content.Graphics.Particles;
using Destiny2.Content.Graphics.Renderers;
using Destiny2.Common.Weapons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Destiny2.Content.Projectiles
{
    public sealed class Bullet : ModProjectile
    {
        private const float MaxDistance = 1200f;
        private const float Speed = 32f;
        private const int ExtraUpdates = 3;
        private const int TrailPoints = 14;
        private const int TrailCacheMax = 40;
        /// <summary>Don't draw trail when shorter than this (point-blank = core only, avoids degenerate/goofy strip).</summary>
        private const float MinTrailLengthPixels = 10f;

        private Vector2 spawnPosition;
        private bool initialized;
        /// <summary>World positions (oldest to newest) so we always have 3+ points for Luminance RenderTrail.</summary>
        private readonly List<Vector2> trailCache = new List<Vector2>();
        private float maxDistance = MaxDistance;
        private Destiny2WeaponElement weaponElement = Destiny2WeaponElement.Kinetic;

        // Safety flag to prevent double-hit handling in edge cases.
        private bool hasDealtDamage;

        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.Bullet}";

        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.DamageType = Destiny2WeaponElement.Kinetic.GetDamageClass();
            Projectile.timeLeft = 60;
            Projectile.aiStyle = -1;
            Projectile.tileCollide = true;
            Projectile.hide = true;
            Projectile.extraUpdates = ExtraUpdates;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }


        private ElementalBulletProfile profile;
        private VFXState vfxState;

        public override void OnSpawn(IEntitySource source)
        {
            weaponElement = Destiny2WeaponElement.Kinetic;
            hasDealtDamage = false; // Initialize as not having hit yet
            Projectile.ai[0] = (int)weaponElement;

            Item sourceItem = null;
            if (source is EntitySource_ItemUse itemUse)
            {
                sourceItem = itemUse.Item;
            }
            else if (source is EntitySource_ItemUse_WithAmmo itemUseWithAmmo)
            {
                sourceItem = itemUseWithAmmo.Item;
            }

            if (sourceItem?.ModItem is Destiny2WeaponItem weaponItem)
            {
                float maxFalloffTiles = weaponItem.GetMaxFalloffTiles();
                if (maxFalloffTiles > 0f)
                    maxDistance = Math.Max(16f, maxFalloffTiles * 3f * 16f);

                weaponElement = weaponItem.WeaponElement;
                Projectile.ai[0] = (int)weaponElement;
                Projectile.DamageType = weaponElement.GetDamageClass();

            }

            spawnPosition = Projectile.Center;
            Projectile.localAI[0] = Projectile.Center.X;
            Projectile.localAI[1] = Projectile.Center.Y;

            // INSTANT BEAM LOGIC
            if (Projectile.velocity != Vector2.Zero)
            {
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * Speed;

                // Predict end point
                Vector2 endPoint = ElementalBulletRenderer.GetBeamEndPoint(spawnPosition, Projectile.velocity.SafeNormalize(Vector2.UnitX), maxDistance);

                // Spawn Instant Trace
                ElementalBulletRenderer.SpawnTrace(spawnPosition, endPoint, weaponElement);
            }

            trailCache.Clear();
            // Do NOT add to trailCache. We want the projectile itself to be invisible/trail-less.
            // The visual is now purely the Trace.

            // Initialize Visuals
            profile = ElementalBulletProfiles.Get(weaponElement);
            vfxState = new VFXState();

            initialized = true;
            Projectile.netUpdate = true;
        }


        public override void AI()
        {
            if (!initialized)
                return;

            // VFX Framework Update
            // Guard against bad data entering the cache (Source of artifacts)
            if (Projectile.Center == Vector2.Zero || float.IsNaN(Projectile.Center.X) || float.IsNaN(Projectile.Center.Y))
            {
                return;
            }

            Projectile.localAI[0] = Projectile.Center.X;
            Projectile.localAI[1] = Projectile.Center.Y;

            // CACHE LOGIC: Disabled for Instant Beam mode.
            // We do NOT add to trailCache, so GetDrawData returns an empty list, 
            // and ElementalBulletRenderer renders nothing for the moving projectile.
            /*
            if (trailCache.Count > 0)
            {
                // ...
            }
            */

            // Limit cache size
            if (trailCache.Count > TrailCacheMax)
                trailCache.RemoveAt(0);

            if (Vector2.Distance(spawnPosition, Projectile.Center) >= maxDistance)
            {
                Projectile.Kill();
            }

            ElementalBulletVFX.UpdateTrail(Projectile, profile, ref vfxState);
        }

        public override void OnKill(int timeLeft)
        {
            // VFX Framework Impact
            // Profile handles the impact burst (or lack thereof for Solar)
            ElementalBulletVFX.SpawnImpactBurst(Projectile, profile);

            base.OnKill(timeLeft);
        }

        /// <summary>No drawing here — BulletDrawSystem draws in PostDrawTiles (same path as Kinetic Tremors) so Luminance primitives work.</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            return false; // Skip default draw; BulletDrawSystem draws us in PostDrawTiles.
        }

        /// <summary>Data for BulletDrawSystem to draw trail + core in PostDrawTiles.</summary>
        public (IReadOnlyList<Vector2> trail, Destiny2WeaponElement element, Vector2 center, float rotation) GetDrawData()
        {
            return (new List<Vector2>(trailCache), GetWeaponElement(), Projectile.Center, Projectile.velocity.ToRotation());
        }

        /// <summary>
        /// Fallback vanilla hit handler. Prevents double-handling in edge cases.
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (hasDealtDamage)
                return;

            hasDealtDamage = true;
            global::Destiny2.Destiny2.LogDiagnostic($"Bullet OnHitNPC. projId={Projectile.identity} target={target?.FullName} damage={damageDone}");
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
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

        private static Color GetElementColor(Destiny2WeaponElement element)
        {
            return element switch
            {
                Destiny2WeaponElement.Void => new Color(196, 0, 240),
                Destiny2WeaponElement.Strand => new Color(55, 218, 100),
                Destiny2WeaponElement.Stasis => new Color(51, 91, 196),
                Destiny2WeaponElement.Solar => new Color(236, 85, 0),
                Destiny2WeaponElement.Arc => new Color(7, 208, 255),
                Destiny2WeaponElement.Kinetic => new Color(255, 248, 163),
                _ => new Color(255, 248, 163)
            };
        }

        private Destiny2WeaponElement GetWeaponElement()
        {
            int elementId = (int)Projectile.ai[0];
            if (elementId < 0 || elementId > (int)Destiny2WeaponElement.Void)
                return weaponElement;

            return (Destiny2WeaponElement)elementId;
        }
    }
}
