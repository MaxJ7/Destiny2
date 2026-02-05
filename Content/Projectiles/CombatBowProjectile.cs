using Destiny2.Common.Perks;
using Destiny2.Common.VFX;
using Destiny2.Common.Weapons;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Destiny2.Content.Projectiles
{
    public sealed class CombatBowProjectile : ModProjectile
    {
        // ai[0] = Weapon Element ID
        // ai[1] = Draw Ratio (0.0f - 1.0f)

        public float FalloffStart = 400f; // Default pixels
        public float FalloffEnd = 800f;
        private Vector2 spawnPosition;

        private const int MaxUpdatesHitscan = 100;
        private const int MaxUpdatesNormal = 0;
        private const float GravityDelay = 15f;

        private bool initialized;
        private Destiny2WeaponElement element;
        private float drawRatio;
        internal int baseDamage;

        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 600;
            Projectile.aiStyle = -1;
            Projectile.extraUpdates = 0;
            Projectile.arrow = true;
            Projectile.alpha = 255;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Projectile.oldPosition = Projectile.position;
            spawnPosition = Projectile.Center;
            Initialize();

            // Store base damage to prevent recursive falloff reduction on the same variable
            baseDamage = Projectile.damage;
        }

        private void Initialize()
        {
            if (initialized) return;

            element = (Destiny2WeaponElement)(int)Projectile.ai[0];
            drawRatio = Math.Clamp(Projectile.ai[1], 0f, 1f);

            if (drawRatio >= 0.9f)
            {
                Projectile.extraUpdates = MaxUpdatesHitscan;
                Projectile.timeLeft = 600 * (MaxUpdatesHitscan + 1);
            }
            else
            {
                Projectile.extraUpdates = MaxUpdatesNormal;
            }

            Projectile.DamageType = element.GetDamageClass();

            initialized = true;
        }

        public override void AI()
        {
            if (!initialized) Initialize();

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (Projectile.extraUpdates == 0)
            {
                Projectile.ai[2]++;
                if (Projectile.ai[2] > GravityDelay * drawRatio)
                {
                    Projectile.velocity.Y += 0.15f;
                }
                Projectile.velocity *= 0.99f;
            }

            // --- DAMAGE FALLOFF ---
            float dist = Vector2.Distance(spawnPosition, Projectile.Center);
            if (dist > FalloffStart)
            {
                float ratio = Math.Clamp((dist - FalloffStart) / (FalloffEnd - FalloffStart), 0f, 1f);
                // Reduce damage up to 50%
                float damageMultiplier = MathHelper.Lerp(1.0f, 0.5f, ratio);
                Projectile.damage = (int)(baseDamage * damageMultiplier);
            }

            GenerateDustTrail();
        }

        private void GenerateDustTrail()
        {
            int steps = (Projectile.extraUpdates + 1) * 2;
            Vector2 start = Projectile.oldPosition + Projectile.Size / 2f;
            Vector2 end = Projectile.Center;
            Vector2 dir = (end - start);

            float lenSq = dir.LengthSquared();
            if (lenSq < 0.1f || lenSq > 4000000f) return;

            int dustId = GetDustForElement(element);
            Color baseColor = GetColorForElement(element);

            Vector3 hsl = Main.rgbToHsl(baseColor);

            for (int i = 0; i < steps; i++)
            {
                float progress = i / (float)steps;
                Vector2 pos = Vector2.Lerp(start, end, progress);

                float hueShift = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5f + i * 0.1f) * 0.05f;
                float newHue = (hsl.X + hueShift) % 1f;
                if (newHue < 0) newHue += 1f;

                Color finalColor = Main.hslToRgb(newHue, hsl.Y, 0.6f);

                Dust d = Dust.NewDustPerfect(pos, dustId, Vector2.Zero, 0, finalColor, 0.8f);
                d.noGravity = true;
                d.velocity = Vector2.Zero;
                d.noLight = true;
                d.scale = 0.8f;
            }
        }

        private int GetDustForElement(Destiny2WeaponElement element)
        {
            return DustID.WhiteTorch;
        }

        private Color GetColorForElement(Destiny2WeaponElement element)
        {
            return element switch
            {
                Destiny2WeaponElement.Solar => Color.Orange,
                Destiny2WeaponElement.Void => new Color(160, 32, 240),
                Destiny2WeaponElement.Arc => new Color(0, 255, 255),
                Destiny2WeaponElement.Stasis => new Color(64, 64, 255),
                Destiny2WeaponElement.Strand => new Color(0, 255, 64),
                _ => Color.White
            };
        }
    }
}
