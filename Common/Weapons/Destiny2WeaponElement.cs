using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace Destiny2.Common.Weapons
{
    public enum Destiny2WeaponElement
    {
        Kinetic,
        Stasis,
        Strand,
        Solar,
        Arc,
        Void,
        ExplosiveShadow
    }

    public static class Destiny2WeaponElementExtensions
    {
        private static KineticDamageClass kinetic;
        private static StasisDamageClass stasis;
        private static StrandDamageClass strand;
        private static SolarDamageClass solar;
        private static ArcDamageClass arc;
        private static VoidDamageClass voidElement;

        public static DamageClass GetDamageClass(this Destiny2WeaponElement element)
        {
            return element switch
            {
                Destiny2WeaponElement.Kinetic => kinetic ??= ModContent.GetInstance<KineticDamageClass>(),
                Destiny2WeaponElement.Stasis => stasis ??= ModContent.GetInstance<StasisDamageClass>(),
                Destiny2WeaponElement.Strand => strand ??= ModContent.GetInstance<StrandDamageClass>(),
                Destiny2WeaponElement.Solar => solar ??= ModContent.GetInstance<SolarDamageClass>(),
                Destiny2WeaponElement.Arc => arc ??= ModContent.GetInstance<ArcDamageClass>(),
                Destiny2WeaponElement.Void => voidElement ??= ModContent.GetInstance<VoidDamageClass>(),
                _ => DamageClass.Ranged
            };
        }

        public static string GetIconTexture(this Destiny2WeaponElement element)
        {
            return element switch
            {
                Destiny2WeaponElement.Kinetic => "Destiny2/Assets/Elements/Kinetic",
                Destiny2WeaponElement.Stasis => "Destiny2/Assets/Elements/Stasis",
                Destiny2WeaponElement.Strand => "Destiny2/Assets/Elements/Strand",
                Destiny2WeaponElement.Solar => "Destiny2/Assets/Elements/Solar",
                Destiny2WeaponElement.Arc => "Destiny2/Assets/Elements/Arc",
                Destiny2WeaponElement.Void => "Destiny2/Assets/Elements/Void",
                _ => "Destiny2/Assets/Elements/Kinetic"
            };
        }

        public static Color GetElementColor(this Destiny2WeaponElement element)
        {
            return element switch
            {
                Destiny2WeaponElement.Void => new Color(196, 0, 240),
                Destiny2WeaponElement.Strand => new Color(55, 218, 100),
                Destiny2WeaponElement.Stasis => new Color(51, 91, 196),
                Destiny2WeaponElement.Solar => new Color(236, 85, 0),
                Destiny2WeaponElement.Arc => new Color(7, 208, 255),
                Destiny2WeaponElement.Kinetic => new Color(255, 248, 163),
                _ => new Color(255, 248, 163)
            };
        }
    }

    public abstract class Destiny2ElementDamageClass : DamageClass
    {
        public override StatInheritanceData GetModifierInheritance(DamageClass damageClass)
        {
            if (damageClass == DamageClass.Generic)
                return StatInheritanceData.Full;

            return StatInheritanceData.None;
        }

        public override bool GetEffectInheritance(DamageClass damageClass)
        {
            return damageClass == DamageClass.Generic;
        }
    }

    public sealed class KineticDamageClass : Destiny2ElementDamageClass
    {
    }

    public sealed class StasisDamageClass : Destiny2ElementDamageClass
    {
    }

    public sealed class StrandDamageClass : Destiny2ElementDamageClass
    {
    }

    public sealed class SolarDamageClass : Destiny2ElementDamageClass
    {
    }

    public sealed class ArcDamageClass : Destiny2ElementDamageClass
    {
    }

    public sealed class VoidDamageClass : Destiny2ElementDamageClass
    {
    }
}
