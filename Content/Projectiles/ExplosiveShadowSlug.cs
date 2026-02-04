using System;
using System.Collections.Generic;
using Destiny2.Common.Perks;
using Destiny2.Common.VFX;
using Destiny2.Common.Weapons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace Destiny2.Content.Projectiles
{
    public sealed class ExplosiveShadowSlug : ModProjectile
    {
        private const float MaxDistance = 1200f;
        private const float Speed = 28f;
        private const int ExtraUpdates = 3;

        private Vector2 spawnPosition;
        private bool initialized;
        private float maxDistance = MaxDistance;

        // Sticky State
        public bool IsStickingToTarget { get; private set; }
        private int targetWhoAmI = -1;
        private Vector2 stickOffset;

        // VFX
        private ElementalBulletProfile profile;
        private VFXState vfxState;

        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.Bullet}";

        public override void SetDefaults()
        {
            Projectile.width = 6;
            Projectile.height = 6;
            Projectile.friendly = true;
            Projectile.penetrate = -1; // Infinite penetrate to allow sticking logic
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.timeLeft = 600; // Increased duration for sticking
            Projectile.aiStyle = -1;
            Projectile.extraUpdates = ExtraUpdates;
            Projectile.tileCollide = true;
            Projectile.hide = true;
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

            if (Projectile.velocity != Vector2.Zero)
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * Speed;

            profile = ElementalBulletProfiles.Get(Destiny2WeaponElement.Void);
            vfxState = new VFXState();
            initialized = true;
            Projectile.netUpdate = true;
        }

        public override void AI()
        {
            if (!initialized) return;

            if (IsStickingToTarget)
            {
                UpdateStickyState();
            }
            else
            {
                UpdateFlightState();
            }
        }

        private void UpdateFlightState()
        {
            // Max Distance Check
            if (Vector2.DistanceSquared(spawnPosition, Projectile.Center) >= maxDistance * maxDistance)
            {
                Projectile.Kill();
                return;
            }

            // Visuals: While flying, we don't need dusts if we're using the Trace System on hit.
            // But a little trail doesn't hurt.
            // ElementalBulletVFX.UpdateTrail(Projectile, profile, ref vfxState); // Optional: Commented out to match "Bullet" logic stricter
        }

        private void UpdateStickyState()
        {
            NPC target = Main.npc[targetWhoAmI];
            if (!target.active || target.life <= 0)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = target.Center + stickOffset;
            Projectile.gfxOffY = target.gfxOffY;

            // Stick Visuals: Pulsing or glowing dust to show it's "armed"
            if (Main.rand.NextBool(10))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleCrystalShard, Vector2.Zero, 100, default, 0.5f);
                d.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (IsStickingToTarget) return;

            // 1. Spawn the BULLET TRACE (Beam)
            BulletDrawSystem.SpawnTrace(spawnPosition, Projectile.Center, Destiny2WeaponElement.Void);

            // 2. Begin Sticky Mode
            IsStickingToTarget = true;
            targetWhoAmI = target.whoAmI;
            stickOffset = Projectile.Center - target.Center;

            Projectile.velocity = Vector2.Zero;
            Projectile.friendly = false; // Stop dealing damage
            Projectile.tileCollide = false;
            Projectile.timeLeft = 600; // Stay alive for 10s (or until exploded by Perk)
            Projectile.netUpdate = true;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // If we hit a tile, we just die and show the tracer.
            BulletDrawSystem.SpawnTrace(spawnPosition, Projectile.Center, Destiny2WeaponElement.Void);
            Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            // Only spawn impact effects. Tracer is handled in OnHitNPC or OnTileCollide.
            ElementalBulletVFX.SpawnImpactBurst(Projectile, profile);
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
