using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Destiny2.Common.Weapons;
using Terraria.DataStructures;

namespace Destiny2.Common.VFX
{
    public class SolarExplosionRenderer : ModSystem
    {
        public static void TriggerExplosion(IEntitySource source, Vector2 center, bool isIgnition)
        {
            if (Main.netMode == Terraria.ID.NetmodeID.MultiplayerClient) return;

            float radius = isIgnition ? 6f : 4f; // tiles
            Destiny2WeaponElement element = Destiny2WeaponElement.Solar;

            // Spawn the PerkExplosion projectile which handles the animated expansion
            Projectile.NewProjectile(source, center, Vector2.Zero,
                ModContent.ProjectileType<Content.Projectiles.PerkExplosion>(), 0, 0f, Main.myPlayer, (float)element, radius);
        }
    }
}
