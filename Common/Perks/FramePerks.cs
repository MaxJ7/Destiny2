namespace Destiny2.Common.Perks
{
    public sealed class AdaptiveFramePerk : Destiny2Perk
    {
        public override string DisplayName => "AdaptIve Frame";
        public override string Description => "A well-rounded grip, reliable and sturdy.";
        public override string IconTexture => "Destiny2/Assets/Perks/AdaptiveFrame";
        public override bool IsFrame => true;
    }

    public sealed class HighImpactFramePerk : Destiny2Perk
    {
        public override string DisplayName => "High-Impact Frame";
        public override string Description => "Slow firing and high damage. This weapon is more accurate when stationary.";
        public override string IconTexture => "Destiny2/Assets/Perks/HighImpactFrame";
        public override bool IsFrame => true;
    }

    public sealed class LightweightBowFramePerk : Destiny2Perk
    {
        public override string DisplayName => "Lightweight Bow Frame";
        public override string Description =>
            "Recurve Bow. Draw quickly and move faster while this weapon is equipped.\n[Reload]: Use while drawn to cancel the shot.";
        public override string IconTexture => "Destiny2/Assets/Perks/LightweightBowFrame";
        public override bool IsFrame => true;
    }

    public sealed class AdaptiveBurstPerk : Destiny2Perk
    {
        public override string DisplayName => "Adaptive Burst";
        public override string Description => "Fires a three-round burst.";
        public override string IconTexture => "Destiny2/Assets/Perks/AdaptiveBurst";
        public override bool IsFrame => true;
    }

    public sealed class PrecisionBowFramePerk : Destiny2Perk
    {
        public override string DisplayName => "Precision Bow Frame";
        public override string Description => "Compound Bow. Longer draw time optimized for damage.\n[Reload]: Use while drawn to cancel the shot.";
        public override string IconTexture => "Destiny2/Assets/Perks/PrecisionBowFrame";
        public override bool IsFrame => true;
    }

    public sealed class PinpointSlugFramePerk : Destiny2Perk
    {
        public override string DisplayName => "Pinpoint Slug Frame";
        public override string Description => "Fires a single-slug round. This weapon's recoil pattern is more predictably vertical.";
        public override string IconTexture => "Destiny2/Assets/Perks/PinpointSlugFrame";
        public override bool IsFrame => true;
    }

    public sealed class HeavyBurstFramePerk : Destiny2Perk
    {
        public override string DisplayName => "Heavy Burst";
        public override string Description => "Fires a hard-hitting, two-round burst.";
        public override string IconTexture => "Destiny2/Assets/Perks/HeavyBurst";
        public override bool IsFrame => true;
    }

    public sealed class LightweightFramePerk : Destiny2Perk
    {
        public override string DisplayName => "Lightweight Frame";
        public override string Description => "Move faster with this weapon equipped.";
        public override string IconTexture => "Destiny2/Assets/Perks/LightweightFrame";
        public override bool IsFrame => true;
    }

    public sealed class RapidFireFramePerk : Destiny2Perk
    {
        public override string DisplayName => "Rapid Fire";
        public override string Description => "Deeper ammo reserves. Slightly faster reload when magazine is empty.";
        public override string IconTexture => "Destiny2/Assets/Perks/RapidFire";
        public override bool IsFrame => true;
    }

    public sealed class MicroMissileFramePerk : Destiny2Perk
    {
        public override string DisplayName => "Micro-Missile Frame";
        public override string Description =>
            "This weapon fires a high-speed micro-missile in a straight line and deals reduced self-damage. Move faster with this weapon equipped.";
        public override string IconTexture => "Destiny2/Assets/Perks/MicroMissileFrame";
        public override bool IsFrame => true;
    }

    public sealed class AreaDenialFramePerk : Destiny2Perk
    {
        public override string DisplayName => "Area Denial Frame";
        public override string Description =>
            "Burst fire Grenade Launcher. Each projectile creates a lingering pool on impact that deals damage over time.";
        public override string IconTexture => "Destiny2/Assets/Perks/AreaDenialFrame";
        public override bool IsFrame => true;
    }

    public sealed class PrecisionFramePerk : Destiny2Perk
    {
        public override string DisplayName => "Precision Frame";
        public override string Description => "This weapon's suffers less recoil.";
        public override string IconTexture => "Destiny2/Assets/Perks/PrecisionFrame";
        public override bool IsFrame => true;
    }

    public sealed class AggressiveFramePerk : Destiny2Perk
    {
        public override string DisplayName => "Aggressive Frame";
        public override string Description => "High damage, high recoil.";
        public override string IconTexture => "Destiny2/Assets/Perks/AggressiveFrame";
        public override bool IsFrame => true;
    }

    public sealed class AggressiveBurstFramePerk : Destiny2Perk
    {
        public override string DisplayName => "Aggressive Burst";
        public override string Description => "Fires a hard-hitting, two-round burst.";
        public override string IconTexture => "Destiny2/Assets/Perks/AggressiveFrame";
        public override bool IsFrame => true;
    }

    public sealed class WaveFramePerk : Destiny2Perk
    {
        public override string DisplayName => "Wave Frame";
        public override string Description =>
            "One-shot handheld Grenade Launcher. Projectiles release a wave of energy when they contact the ground.";
        public override string IconTexture => "Destiny2/Assets/Perks/WaveFrame";
        public override bool IsFrame => true;
    }

    public sealed class TheRightChoiceFramePerk : Destiny2Perk
    {
        public const int ShotsRequired = 7;
        public const float RicochetDamageMultiplier = 2.5f;

        public override string DisplayName => "The Right Choice";
        public override string Description => "Every l7th shot ricochets to a new enemy and deals increased damage.";
        public override string IconTexture => "Destiny2/Assets/Perks/TheRightChoiceFrame";
        public override bool IsFrame => true;
    }

    public sealed class TouchOfMalicePerk : Destiny2Perk
    {
        public const int KillsForHeal = 3;
        public const int HealAmount = 20;
        public const float HealthDrainPercent = 0.035f; // 3.5%
        public const float DamageMultiplier = 1.4f; // 140% Increase

        public override string DisplayName => "Touch of Malice";
        public override string Description => "Final round regenerates ammo and deals bonus damage, but drains health.\nRapid kills heal the wielder.";
        public override string IconTexture => "Destiny2/Assets/Perks/TouchOfMalice";
        public override bool IsFrame => true;
    }
}
