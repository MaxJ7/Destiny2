using System;
using Destiny2.Common.VFX;
using Destiny2.Common.Perks;
using Destiny2.Common.Weapons;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Destiny2.Content.Projectiles
{
    /// <summary>
    /// Pseudo-hitscan bullet projectile. Travel instantly. 
    /// Visuals are handled by BulletDrawSystem via "Decoupled Traces" (Fire-and-Forget).
    /// </summary>
    public sealed class Bullet : ModProjectile
    {
        private const float MaxDistance = 1200f;
        private const float Speed = 32f;
        private const int ExtraUpdates = 100;

        internal Vector2 spawnPosition;
        private bool initialized;
        private float maxDistance = MaxDistance;
        private Destiny2WeaponElement weaponElement = Destiny2WeaponElement.Kinetic;
        private bool hasDealtDamage;

        // VFX
        private ElementalBulletProfile profile;
        private VFXState vfxState;

        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.Bullet}";

        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.friendly = true;
            Projectile.penetrate = 1; // Die on first hit (Standard bullet)
            Projectile.DamageType = Destiny2WeaponElement.Kinetic.GetDamageClass();
            Projectile.timeLeft = 60;
            Projectile.aiStyle = -1;
            Projectile.tileCollide = true;
            Projectile.hide = true;
            Projectile.extraUpdates = ExtraUpdates;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void OnSpawn(IEntitySource source)
        {
            weaponElement = Destiny2WeaponElement.Kinetic;
            hasDealtDamage = false;
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

            if (Projectile.velocity != Vector2.Zero)
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * Speed;

            profile = ElementalBulletProfiles.Get(weaponElement);
            vfxState = new VFXState();

            initialized = true;
            Projectile.netUpdate = true;
        }

        public override void AI()
        {
            if (!initialized) return;

            // Guard against bad data
            if (Projectile.Center == Vector2.Zero || float.IsNaN(Projectile.Center.X) || float.IsNaN(Projectile.Center.Y))
                return;

            // Max Distance Check
            if (Vector2.DistanceSquared(spawnPosition, Projectile.Center) >= maxDistance * maxDistance)
            {
                Projectile.Kill();
                return;
            }

            // VFX (Dust while flying - minimal for instant bullets but good for "feeling")
            if (Projectile.numUpdates == 0)
            {
                ElementalBulletVFX.UpdateTrail(Projectile, weaponElement, profile, ref vfxState);
            }
        }

        public override void OnKill(int timeLeft)
        {
            // VISUAL TRACER SYSTEM (Phase 1)
            // Check for shader override from Destiny2PerkProjectile
            var perkData = Projectile.GetGlobalProjectile<Destiny2PerkProjectile>();
            if (perkData != null && perkData.CustomTrailShader != null)
            {
                BulletDrawSystem.SpawnTrace(spawnPosition, Projectile.Center, perkData.CustomTrailShader);
            }
            else
            {
                BulletDrawSystem.SpawnTrace(spawnPosition, Projectile.Center, GetWeaponElement());
            }

            // Spawn Impact Burst (Hit effect)
            ElementalBulletVFX.SpawnImpactBurst(Projectile, profile);

            base.OnKill(timeLeft);
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Just mark damage for frenzy logic if needed elsewhere
            if (hasDealtDamage) return;
            hasDealtDamage = true;

            // Note: Projectile.penetrate = 1 means engine calls Kill() immediately after this.
            // So we don't need to call SpawnTrace here, OnKill will handle it.
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 start = spawnPosition;
            Vector2 end = Projectile.Center;
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                start,
                end,
                Projectile.width + 4,
                ref collisionPoint);
        }

        private Destiny2WeaponElement GetWeaponElement()
        {
            int elementId = (int)Projectile.ai[0];
            // Safe cast check
            if (Enum.IsDefined(typeof(Destiny2WeaponElement), elementId))
                return (Destiny2WeaponElement)elementId;

            return weaponElement;
        }
    }
}
