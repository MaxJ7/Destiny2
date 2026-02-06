using System;
using Destiny2.Common.Perks;
using Destiny2.Common.Weapons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.DataStructures;

namespace Destiny2.Common.VFX
{
    public class KineticShockwaveRenderer : ModSystem
    {
        public static void TriggerPulse(IEntitySource source, Vector2 center, float pulseOption = 0f)
        {
            if (Main.netMode == Terraria.ID.NetmodeID.MultiplayerClient) return;

            float radius = KineticTremorsPerk.ShockwaveRadiusTiles;
            Destiny2WeaponElement element = Destiny2WeaponElement.Kinetic;

            // Spawn the PerkExplosion projectile which handles the animated expansion
            Projectile.NewProjectile(source, center, Vector2.Zero,
                ModContent.ProjectileType<Content.Projectiles.PerkExplosion>(), 0, 0f, Main.myPlayer, (float)element, radius);
        }
    }
}
