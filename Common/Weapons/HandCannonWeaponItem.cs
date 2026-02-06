using System;
using Destiny2.Common.Perks;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace Destiny2.Common.Weapons
{
    public abstract class HandCannonWeaponItem : Destiny2WeaponItem
    {
        private const int HeavyBurstShotsPerUse = 2;
        private const int HeavyBurstShotIntervalTicks = 3;
        private const float PrecisionFrameRecoilScalar = 0.93f;
        private const float AggressiveFrameRangeScalar = 1.1f;
        private const float MaxRecoilAngleAtZeroStability = 45f;
        private const float MaxRecoilAngleAtHundredStability = 20f;
        private const float MinRecoilStepScalar = 0.08f;
        private const float MaxRecoilStepScalar = 0.3f;
        private const float HeavyBurstRecoilScalar = 0.5f;
        private float recoilAngle;
        private ulong lastShotTick;

        public override Destiny2AmmoType AmmoType => Destiny2AmmoType.Primary;

        public override float GetPrecisionMultiplier()
        {
            if (!TryGetFramePerk(out Destiny2Perk framePerk))
                return 1f;

            if (framePerk is HeavyBurstFramePerk)
                return 2.2f;
            if (framePerk is AdaptiveFramePerk || framePerk is LightweightFramePerk)
                return 1.79f;
            if (framePerk is PrecisionFramePerk || framePerk is ExplosiveShadowPerk)
                return 1.55f;
            if (framePerk is AggressiveFramePerk)
                return 1.2f;    

            return 1f;
        }

        public override int GetBurstCount()
        {
            if (TryGetFramePerk(out Destiny2Perk framePerk))
            {
                if (framePerk is HeavyBurstFramePerk || framePerk is AggressiveBurstFramePerk)
                    return HeavyBurstShotsPerUse; // 2
            }
            return 1;
        }

        protected virtual float BaseRecoil => 1.25f;
        protected virtual float MinFalloffTiles => 24f;
        protected virtual float MaxFalloffTiles => 40f;
        protected virtual float ReloadSecondsAtZero => 3.3f;
        protected virtual float ReloadSecondsAtHundred => 2.2f;
        protected virtual float ReloadSecondsAtFifty => 2.8f;
        protected virtual float FalloffTilesAtFifty => 30f;

        private static float GetFrameRecoilScalar(string framePerkKey)
        {
            if (string.IsNullOrWhiteSpace(framePerkKey))
                return 1f;

            if (Destiny2PerkSystem.TryGet(framePerkKey, out Destiny2Perk framePerk)
                && (framePerk is PrecisionFramePerk || framePerk is ExplosiveShadowPerk))
                return PrecisionFrameRecoilScalar;

            return 1f;
        }

        public float GetRecoil()
        {
            Destiny2WeaponStats stats = GetStats();
            float stabilityScalar = MathHelper.Clamp(1f - (stats.Stability / 100f), 0.4f, 1f);
            return BaseRecoil * stabilityScalar * GetFrameRecoilScalar(FramePerkKey);
        }

        protected override float GetRecoilStrength()
        {
            return GetRecoil();
        }

        public override float GetFalloffTiles()
        {
            Destiny2WeaponStats stats = GetStats();
            float falloffTiles = CalculateFalloffTiles(stats.Range, MinFalloffTiles, MaxFalloffTiles, FalloffTilesAtFifty);
            if (TryGetFramePerk(out Destiny2Perk framePerk) && framePerk is AggressiveFramePerk)
                falloffTiles *= AggressiveFrameRangeScalar;

            return falloffTiles;
        }

        public override float GetMaxFalloffTiles()
        {
            return MaxFalloffTiles;
        }

        public override float GetReloadSeconds()
        {
            Destiny2WeaponStats stats = GetStats();
            float reloadSeconds = CalculateScaledValue(stats.ReloadSpeed, ReloadSecondsAtZero, ReloadSecondsAtHundred, ReloadSecondsAtFifty);
            return reloadSeconds * GetReloadSpeedTimeScalar();
        }

        protected override int GetFrameRoundsPerMinute(Destiny2Perk framePerk, int currentRpm)
        {
            if (framePerk is AdaptiveFramePerk)
                return 140;
            if (framePerk is PrecisionFramePerk)
                return 180;
            if (framePerk is ExplosiveShadowPerk)
                return 180;
            if (framePerk is AggressiveFramePerk)
                return 120;
            if (framePerk is LightweightFramePerk)
                return 150;

            return currentRpm;
        }

        public override void HoldItem(Player player)
        {
            base.HoldItem(player);
            ApplyBurstSettings();
        }

        public override void UpdateInventory(Player player)
        {
            base.UpdateInventory(player);
            ApplyBurstSettings();
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            ulong tick = Main.GameUpdateCount;
            int cycleTicks = Math.Max(1, Item.useAnimation + Item.reuseDelay);
            ulong resetTicks = (ulong)Math.Max(10, cycleTicks + 2);
            if (tick - lastShotTick > resetTicks)
                recoilAngle = 0f;

            Destiny2WeaponStats stats = GetStats();
            float stability = Math.Clamp(stats.Stability, 0f, 100f);
            float stabilityRatio = stability / 100f;
            float maxAngleDeg = MathHelper.Lerp(MaxRecoilAngleAtZeroStability, MaxRecoilAngleAtHundredStability, stabilityRatio);
            float maxAngle = MathHelper.ToRadians(maxAngleDeg);
            maxAngle *= GetFrameRecoilScalar(FramePerkKey);
            if (TryGetFramePerk(out Destiny2Perk framePerk) && framePerk is HeavyBurstFramePerk)
                maxAngle *= HeavyBurstRecoilScalar;

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

        private void ApplyBurstSettings()
        {
            int rpm = GetStats().RoundsPerMinute;
            if (rpm <= 0)
                return;

            int baseUseTime = Math.Clamp((int)Math.Round(3600f / rpm), 1, 3600);
            Item.reuseDelay = 0;

            int burstCount = GetBurstCount();
            if (burstCount > 1)
            {
                int shotInterval = Math.Max(1, HeavyBurstShotIntervalTicks);
                Item.useTime = shotInterval;
                Item.useAnimation = shotInterval * burstCount;

                int burstCycle = baseUseTime * burstCount;
                Item.reuseDelay = Math.Max(0, burstCycle - Item.useAnimation);
                return;
            }

            Item.useTime = baseUseTime;
            Item.useAnimation = baseUseTime;
        }

    }
}
