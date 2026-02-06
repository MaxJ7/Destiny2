using Destiny2.Common.Perks;
using Destiny2.Common.VFX;
using Destiny2.Common.Weapons;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Graphics.Shaders;
using Terraria.DataStructures;
using Terraria.ID;

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
            Projectile.alpha = 0; // Visible by default
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
                Projectile.alpha = 255; // Hitscan is invisible, uses BulletDrawSystem traces
            }
            else
            {
                Projectile.extraUpdates = MaxUpdatesNormal;
                Projectile.alpha = 0; // Non-hitscan is visible
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

                // Only generate dust trail for normal projectiles
                GenerateDustTrail();
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
        }

        public override void OnKill(int timeLeft)
        {
            // If it was a hitscan shot, spawn a persistent visual trace
            if (Projectile.extraUpdates > 0)
            {
                BulletDrawSystem.SpawnTrace(spawnPosition, Projectile.Center, element, 12f);
            }

            // Standard impact effects (Dust)
            for (int i = 0; i < 10; i++)
            {
                Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, GetDustForElement(element), 0, 0, 100, GetColorForElement(element), 1f);
                d.velocity *= 1.5f;
                d.noGravity = true;
            }
        }

        private void GenerateDustTrail()
        {
            int steps = 2; // Fixed steps for AI-only trail
            Vector2 start = Projectile.oldPosition + Projectile.Size / 2f;
            Vector2 end = Projectile.Center;

            int dustId = GetDustForElement(element);
            Color baseColor = GetColorForElement(element);

            for (int i = 0; i < steps; i++)
            {
                float progress = i / (float)steps;
                Vector2 pos = Vector2.Lerp(start, end, progress);

                Dust d = Dust.NewDustPerfect(pos, dustId, Vector2.Zero, 120, baseColor, 1.2f);
                d.noGravity = true;
                d.velocity *= 0.1f;
            }
        }

        private int GetDustForElement(Destiny2WeaponElement element)
        {
            return DustID.WhiteTorch;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Only draw the arrow texture for non-hitscan projectiles
            if (Projectile.extraUpdates > 0) return false;

            Texture2D texture = ModContent.Request<Texture2D>("Destiny2/Assets/Textures/Arrow").Value;
            Vector2 origin = texture.Size() / 2f;
            float rotation = Projectile.rotation;

            ArmorShaderData shader = ElementalShaderSystem.GetShader(element);

            DrawData drawData = new DrawData(
                texture,
                Projectile.Center - Main.screenPosition,
                null,
                lightColor,
                rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0
            );

            int shaderId = ElementalShaderSystem.GetShaderId(element);

            if (shaderId > 0 && shader != null)
            {
                drawData.shader = shaderId;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
                shader.Apply(Projectile, drawData);
                drawData.Draw(Main.spriteBatch);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
                return false;
            }

            drawData.Draw(Main.spriteBatch);
            return false;
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
