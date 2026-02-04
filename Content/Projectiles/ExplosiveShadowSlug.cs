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

        // VFX
        private ElementalBulletProfile profile;
        private VFXState vfxState;

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

            // Phase 19: Use Void Profile
            profile = ElementalBulletProfiles.Get(Destiny2WeaponElement.Void);
            vfxState = new VFXState();

            initialized = true;
        }

        public override void AI()
        {
            if (!initialized) return;

            // Distance Check
            if (Vector2.Distance(spawnPosition, Projectile.Center) >= maxDistance)
            {
                Projectile.Kill();
                return;
            }

            // VFX: Dust Trail (Phase 19 Style)
            if (Projectile.numUpdates == 0)
            {
                ElementalBulletVFX.UpdateTrail(Projectile, profile, ref vfxState);
            }
        }

        public override void OnKill(int timeLeft)
        {
            // IMPACT VFX (Phase 19 Style)
            ElementalBulletVFX.SpawnImpactBurst(Projectile, profile);

            // Standard explosion sound/light
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
                Lighting.AddLight(Projectile.Center, 0.5f, 0f, 0.5f);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Simple hit logic (Stickiness was likely added later? User requested Phase 18/19 revert)
            // If Phase 19 had sticky, it was likely simple. 
            // I'll keep it simple for now as requested "One to One".
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
