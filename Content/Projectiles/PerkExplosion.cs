namespace Destiny2.Content.Projectiles
{
    public class PerkExplosion : Destiny2ExplosionProjectile
    {
        public override void SetStaticDefaults()
        {
            // ProjectileID.Sets.Ranged[Type] = true; or similar if needed for specific scaling
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            // PerkExplosion is instant by default (PrimingDelay = 0)
        }
    }
}
