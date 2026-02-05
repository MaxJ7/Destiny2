using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Destiny2.Common.Weapons;
using Terraria.ID;

namespace Destiny2.Content.Projectiles
{
    public class ExplosiveHeadExplosion : ModProjectile
    {
        public override string Texture => "Terraria/Images/MagicPixel";

        public override void SetDefaults()
        {
            Projectile.width = 64; // 4 tiles wide (2 tile radius)
            Projectile.height = 64;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 3;
            Projectile.hide = true;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Destiny2WeaponElement element = (Destiny2WeaponElement)(int)Projectile.ai[0];
            SpawnVFX(element);
        }

        private void SpawnVFX(Destiny2WeaponElement element)
        {
            int dustId = CombatBowWeaponItem.GetDustForElement(element);
            Color color = CombatBowWeaponItem.GetColorForElement(element);

            for (int i = 0; i < 20; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(4f, 4f);
                Dust d = Dust.NewDustPerfect(Projectile.Center, dustId, vel, 100, color, 1.5f);
                d.noGravity = true;
                d.noLight = false;
                d.scale = 1.2f;
            }

            // Small flash
            Lighting.AddLight(Projectile.Center, color.ToVector3() * 0.5f);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            // Explosion cannot crit as per user request
            modifiers.DisableCrit();
        }
    }
}
