using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Destiny2.Common.Weapons;
using Terraria.ID;
using Microsoft.Xna.Framework.Graphics;

namespace Destiny2.Content.Projectiles
{
    public abstract class Destiny2ExplosionProjectile : ModProjectile
    {
        public override string Texture => "Terraria/Images/MagicPixel";

        // Properties that child classes can override
        public virtual int PrimingDelay => 0;
        public virtual int ExpansionTime => 10;
        public virtual float RadiusTiles => Projectile.ai[1];
        public virtual Destiny2WeaponElement Element => (Destiny2WeaponElement)(int)Projectile.ai[0];

        protected int Timer => (int)Projectile.ai[2];

        public override void SetDefaults()
        {
            Projectile.width = 16; // Initial, will be scaled
            Projectile.height = 16;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.hide = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            float radius = RadiusTiles;
            int size = (int)(radius * 16 * 2);
            Projectile.width = size;
            Projectile.height = size;
            // Center correction
            Projectile.position -= new Vector2(size / 2f - 8, size / 2f - 8);

            if (PrimingDelay == 0)
            {
                Projectile.friendly = true;
                OnDetonation();
            }
        }

        public override void AI()
        {
            Projectile.ai[2]++; // Increment timer
            int delay = PrimingDelay;

            if (Timer < delay)
            {
                OnPriming();
            }
            else if (Timer == delay)
            {
                Projectile.friendly = true;
                Projectile.netUpdate = true;
                OnDetonation();
            }
            else
            {
                int expansionTimer = Timer - delay;
                if (expansionTimer < ExpansionTime)
                {
                    OnExpansion(expansionTimer);
                }
                else if (expansionTimer >= ExpansionTime + 2) // Small buffer
                {
                    Projectile.Kill();
                }

                OnGeneralAI(expansionTimer);
            }
        }

        protected virtual void OnPriming()
        {
            Destiny2WeaponElement element = Element;
            Color color = CombatBowWeaponItem.GetColorForElement(element);

            if (Main.rand.NextFloat() < 0.3f)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(12f, 12f), DustID.WhiteTorch, Vector2.Zero, 100, color, 1.2f);
                d.noGravity = true;
                d.velocity = Main.rand.NextVector2Circular(0.5f, 0.5f);
            }
        }

        protected virtual void OnDetonation()
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);

            // Core burst
            for (int i = 0; i < 20; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Smoke, Main.rand.NextVector2Circular(4f, 4f), 150, new Color(40, 40, 40), 1.3f);
                d.noGravity = true;
            }
        }

        protected virtual void OnExpansion(int expansionTimer)
        {
            float progress = MathHelper.Clamp(expansionTimer / (float)ExpansionTime, 0f, 1f);
            float currentMaxDist = RadiusTiles * 16f * progress;
            Color color = CombatBowWeaponItem.GetColorForElement(Element);

            for (int i = 0; i < 8; i++)
            {
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                Vector2 dir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                float dist = currentMaxDist * Main.rand.NextFloat(0.85f, 1.0f);
                Vector2 pos = Projectile.Center + dir * dist;
                Vector2 vel = dir * (1.5f + progress * 2.5f);

                Dust d = Dust.NewDustPerfect(pos, DustID.WhiteTorch, vel, 100, color, 0.7f + Main.rand.NextFloat(0.4f));
                d.noGravity = true;
                d.fadeIn = 0.8f;
            }
        }

        protected virtual void OnGeneralAI(int expansionTimer)
        {
            Color color = CombatBowWeaponItem.GetColorForElement(Element);
            Lighting.AddLight(Projectile.Center, color.ToVector3() * 2f);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.DisableCrit();
        }
    }
}
