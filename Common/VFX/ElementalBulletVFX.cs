using Destiny2.Common.Weapons;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace Destiny2.Common.VFX
{
    public struct ElementalBulletProfile
    {
        public Color Color;
        public int DustType;
        public float DustScale;
        public int TrailFrequency;
    }

    public struct VFXState
    {
        public float Timer;
    }

    public static class ElementalBulletProfiles
    {
        public static ElementalBulletProfile Get(Destiny2WeaponElement element)
        {
            return element switch
            {
                Destiny2WeaponElement.Solar => new ElementalBulletProfile { Color = new Color(255, 100, 0), DustType = DustID.Torch, DustScale = 1.2f, TrailFrequency = 4 },
                Destiny2WeaponElement.Arc => new ElementalBulletProfile { Color = new Color(0, 200, 255), DustType = DustID.Electric, DustScale = 0.8f, TrailFrequency = 3 },
                Destiny2WeaponElement.Void => new ElementalBulletProfile { Color = new Color(150, 0, 255), DustType = DustID.Shadowflame, DustScale = 1.0f, TrailFrequency = 5 },
                Destiny2WeaponElement.Stasis => new ElementalBulletProfile { Color = new Color(0, 50, 200), DustType = DustID.Ice, DustScale = 1.1f, TrailFrequency = 5 },
                Destiny2WeaponElement.Strand => new ElementalBulletProfile { Color = new Color(0, 255, 100), DustType = DustID.GreenFairy, DustScale = 0.9f, TrailFrequency = 4 },
                _ => new ElementalBulletProfile { Color = Color.White, DustType = DustID.Smoke, DustScale = 0.5f, TrailFrequency = 10 }
            };
        }
    }

    public static class ElementalBulletVFX
    {
        public static void UpdateTrail(Projectile projectile, ElementalBulletProfile profile, ref VFXState state)
        {
            state.Timer++;
            if (state.Timer % profile.TrailFrequency == 0)
            {
                Dust d = Dust.NewDustPerfect(projectile.Center, profile.DustType, Vector2.Zero, 0, profile.Color, profile.DustScale);
                d.noGravity = true;
                d.velocity = projectile.velocity * 0.2f;
            }
        }

        public static void SpawnImpactBurst(Projectile projectile, ElementalBulletProfile profile)
        {
            for (int i = 0; i < 5; i++)
            {
                Dust d = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, profile.DustType, 0, 0, 0, profile.Color, profile.DustScale);
                d.noGravity = true;
                d.velocity *= 2f;
            }
        }
    }
}
