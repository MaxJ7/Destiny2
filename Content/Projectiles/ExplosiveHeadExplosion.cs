namespace Destiny2.Content.Projectiles
{
    public class ExplosiveHeadExplosion : Destiny2ExplosionProjectile
    {
        public override int PrimingDelay => 30; // 0.5s delay
        public override float RadiusTiles => 2f; // 2 tile radius (4 tiles diameter)

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.timeLeft = 60; // Enough time for priming + expansion
        }
    }
}
