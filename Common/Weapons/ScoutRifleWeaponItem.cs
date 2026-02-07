using System;
using Destiny2.Common.Perks;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace Destiny2.Common.Weapons
{
    public abstract class ScoutRifleWeaponItem : Destiny2WeaponItem
    {
        private const float RapidFireFrameReloadScalar = 0.9f;
        private const float MaxRecoilAngleAtZeroStability = 30f;
        private const float MaxRecoilAngleAtHundredStability = 13.33f;
        private const float MinRecoilStepScalar = 0.08f;
        private const float MaxRecoilStepScalar = 0.3f;
        private float recoilAngle;
        private ulong lastShotTick;

        public override Destiny2AmmoType AmmoType => Destiny2AmmoType.Primary;

        public override float GetPrecisionMultiplier()
        {
            if (!TryGetFramePerk(out Destiny2Perk framePerk))
                return 1f;

            if (framePerk is RapidFireFramePerk || framePerk is TouchOfMalicePerk)
                return 1.5f;

            return 1f;
        }

        public override ReloadFormula ReloadFormula => new ReloadFormula(3.11797f, -0.0276889f, 0.000102915f);
        public override RangeFormula RangeFormula => new RangeFormula(60.5f, 0.3136f, 121.5f, 0f);
        public override float DamageFloor => 0.5f;

        protected virtual float BaseRecoil => 0.83f;

        public float GetRecoil()
        {
            Destiny2WeaponStats stats = GetStats();
            float stabilityScalar = MathHelper.Clamp(1f - (stats.Stability / 100f), 0.4f, 1f);
            return BaseRecoil * stabilityScalar;
        }

        protected override float GetRecoilStrength()
        {
            return GetRecoil();
        }

        public override float GetReloadSeconds()
        {
            return base.GetReloadSeconds() * GetRapidFireFrameReloadScalar();
        }

        private float GetRapidFireFrameReloadScalar()
        {
            if (!TryGetFramePerk(out Destiny2Perk framePerk))
                return 1f;

            if ((framePerk is RapidFireFramePerk || framePerk is TouchOfMalicePerk) && CurrentMagazine <= 0)
                return RapidFireFrameReloadScalar;

            return 1f;
        }

        protected override int GetFrameRoundsPerMinute(Destiny2Perk framePerk, int currentRpm)
        {
            if (framePerk is RapidFireFramePerk || framePerk is TouchOfMalicePerk)
                return 260;
            if (framePerk is LightweightFramePerk)
                return 200;
            if (framePerk is AggressiveFramePerk)
                return 120;
            if (framePerk is PrecisionFramePerk)
                return 180;
            if (framePerk is HighImpactFramePerk)
                return 150;

            return currentRpm;
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            ulong tick = Main.GameUpdateCount;
            ulong resetTicks = (ulong)Math.Max(10, Item.useAnimation + Item.reuseDelay);
            if (tick - lastShotTick > resetTicks)
                recoilAngle = 0f;

            Destiny2WeaponStats stats = GetStats();
            float stability = Math.Clamp(stats.Stability, 0f, 100f);
            float stabilityRatio = stability / 100f;
            float maxAngleDeg = MathHelper.Lerp(MaxRecoilAngleAtZeroStability, MaxRecoilAngleAtHundredStability, stabilityRatio);
            float maxAngle = MathHelper.ToRadians(maxAngleDeg);

            float offset = recoilAngle;
            if (offset > 0f)
            {
                float sign = velocity.X < 0f ? 1f : -1f;
                velocity = velocity.RotatedBy(sign * offset);
            }

            float stepScalar = MathHelper.Lerp(MaxRecoilStepScalar, MinRecoilStepScalar, stabilityRatio);
            float step = maxAngle * stepScalar;
            recoilAngle = Math.Min(maxAngle, recoilAngle + step);

            lastShotTick = tick;

            base.ModifyShootStats(player, ref position, ref velocity, ref type, ref damage, ref knockback);
        }
    }
}
