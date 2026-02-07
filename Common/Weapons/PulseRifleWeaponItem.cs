using System;
using Destiny2.Common.Perks;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace Destiny2.Common.Weapons
{
    public abstract class PulseRifleWeaponItem : Destiny2WeaponItem
    {
        private const float PrecisionFrameRecoilScalar = 0.85f;
        private const float RapidFireFrameRecoilScalar = 0.8f;
        private const float MaxRecoilAngleAtZeroStability = 6f; // Per shot recoil
        private const float MaxRecoilAngleAtHundredStability = 2f;
        private const float MinRecoilStepScalar = 0.08f;
        private const float MaxRecoilStepScalar = 0.3f;

        // Burst Specs
        private const int AdaptiveBurstCount = 3;
        private const int HighImpactBurstCount = 3;
        private const int AggressiveBurstCount = 4;
        private const int HeavyBurstCount = 2;
        private const int LightweightBurstCount = 3;
        private const int RapidFireBurstCount = 3;

        private float recoilAngle;
        private ulong lastShotTick;

        public override Destiny2AmmoType AmmoType => Destiny2AmmoType.Primary;

        public override float GetPrecisionMultiplier()
        {
            if (!TryGetFramePerk(out Destiny2Perk framePerk))
                return 1f;

            if (framePerk is HeavyBurstFramePerk) // Heavy is 2-burst, hits harder
                return 1.8f;
            if (framePerk is AggressiveBurstFramePerk) // Aggressive is 4-burst
                return 1.6f;

            // Standard 3-bursts
            return 1.65f;
        }

        public override int GetBurstCount()
        {
            if (TryGetFramePerk(out Destiny2Perk framePerk))
            {
                if (framePerk is AggressiveBurstFramePerk) return AggressiveBurstCount;
                if (framePerk is HeavyBurstFramePerk) return HeavyBurstCount;
                if (framePerk is HighImpactFramePerk) return HighImpactBurstCount;
                if (framePerk is AdaptiveFramePerk) return AdaptiveBurstCount;
                if (framePerk is LightweightFramePerk) return LightweightBurstCount;
                if (framePerk is TheCorruptionSpreadsFramePerk) return 3; // Outbreak
                if (framePerk is RapidFireFramePerk) return RapidFireBurstCount;
            }
            return 3; // Standard Pulse
        }

        public override ReloadFormula ReloadFormula => new ReloadFormula(2.92627f, -0.0256877f, 0.0000926208f);
        public override float DamageFloor => 0.5f;

        public override RangeFormula RangeFormula
        {
            get
            {
                if (TryGetFramePerk(out Destiny2Perk frame) && (frame is AggressiveBurstFramePerk || frame is HeavyBurstFramePerk))
                {
                    return new RangeFormula(28.8f, 0.135f, 72f, 0f);
                }
                return new RangeFormula(27.2f, 0.1275f, 68f, 0f);
            }
        }

        protected virtual float BaseRecoil => 1.0f;

        private float GetFrameRecoilScalar(string framePerkKey)
        {
            if (string.IsNullOrWhiteSpace(framePerkKey))
                return 1f;

            if (Destiny2PerkSystem.TryGet(framePerkKey, out Destiny2Perk framePerk))
            {
                if (framePerk is PrecisionFramePerk) return PrecisionFrameRecoilScalar;
                if (framePerk is RapidFireFramePerk) return RapidFireFrameRecoilScalar;
            }

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

        protected override int GetFrameRoundsPerMinute(Destiny2Perk framePerk, int currentRpm)
        {
            if (framePerk is AdaptiveFramePerk) return 390;
            if (framePerk is HighImpactFramePerk) return 340;
            if (framePerk is AggressiveBurstFramePerk) return 450;
            if (framePerk is HeavyBurstFramePerk) return 325;
            if (framePerk is LightweightFramePerk) return 450;
            if (framePerk is TheCorruptionSpreadsFramePerk) return 450;
            if (framePerk is RapidFireFramePerk) return 540;

            return currentRpm;
        }

        public override void HoldItem(Player player)
        {
            base.HoldItem(player);
            ApplyBurstSettings(player);
        }

        public override void UpdateInventory(Player player)
        {
            base.UpdateInventory(player);
            ApplyBurstSettings(player);
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

        private void ApplyBurstSettings(Player player)
        {
            int rpm = GetStats().RoundsPerMinute;
            if (rpm <= 0) return;

            // Base calculation: How long a full cycle takes for this RPM
            // e.g. 540 RPM => 9 shots/sec => 6.66 frames/shot (impossible in Terraria without bursts)
            // But for Burst weapons, RPM usually refers to the *bursts* per minute or effective bullets per minute including delay.
            // In Destiny 2, RPM is "Rounds" per minute.
            // 540 RPM = 9 bullets per second.
            // If it's a 3-round burst, that's 3 bursts per second.
            // Cycle Time = 60 / 3 = 20 frames per burst cycle.

            int burstCount = GetBurstCount();
            if (TryGetFramePerk(out Destiny2Perk framePerk))
            {
                // Lightweight Bonus
                if (framePerk is LightweightFramePerk || framePerk is TheCorruptionSpreadsFramePerk)
                {
                    player.moveSpeed += 0.05f;
                }
            }

            // Calculate Frame Data
            // 1. Calculate bullets per second: RPM / 60
            // 2. Calculate frames per bullet theoretical: 60 / (RPM/60) = 3600 / RPM
            // Example: 540 RPM -> 3600 / 540 = 6.66 frames per bullet.

            // Standard Pulse Rifle Logic:
            // High fire rate within the burst (usually 900 RPM or similar -> 4 frames/shot)
            // Long delay between bursts.

            // Let's standardize the intra-burst speed.
            // Rapid-Fire (540): Fast burst.
            // Heavy (325): Slow burst.

            int timeBetweenShots = 4; // Default to ~900 RPM burst speed (Standard for D2 pulses)
            if (framePerk is HeavyBurstFramePerk || framePerk is HighImpactFramePerk) timeBetweenShots = 5; // Slower chunks
            else if (framePerk is RapidFireFramePerk) timeBetweenShots = 3; // Very fast burst

            // Total Active Firing Time = timeBetweenShots * burstCount
            // Total Cycle Time (frames) = 3600 / (RPM / burstCount) <-- No, RPM is bullets, not bursts.
            // Total Cycle Time (frames) = 3600 * burstCount / RPM

            int totalCycleFrames = (int)Math.Round((3600f * burstCount) / rpm);
            int firingDuration = timeBetweenShots * burstCount;

            // Ensure data validity
            if (totalCycleFrames < firingDuration) totalCycleFrames = firingDuration;

            Item.useTime = timeBetweenShots;
            Item.useAnimation = firingDuration;
            Item.reuseDelay = totalCycleFrames - firingDuration;
        }
    }
}
