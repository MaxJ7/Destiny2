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
		Void
	}

	public static class Destiny2WeaponElementExtensions
	{
		public static DamageClass GetDamageClass(this Destiny2WeaponElement element)
		{
			return element switch
			{
				Destiny2WeaponElement.Kinetic => ModContent.GetInstance<KineticDamageClass>(),
				Destiny2WeaponElement.Stasis => ModContent.GetInstance<StasisDamageClass>(),
				Destiny2WeaponElement.Strand => ModContent.GetInstance<StrandDamageClass>(),
				Destiny2WeaponElement.Solar => ModContent.GetInstance<SolarDamageClass>(),
				Destiny2WeaponElement.Arc => ModContent.GetInstance<ArcDamageClass>(),
				Destiny2WeaponElement.Void => ModContent.GetInstance<VoidDamageClass>(),
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
	}

	public abstract class Destiny2ElementDamageClass : DamageClass
	{
		public override StatInheritanceData GetModifierInheritance(DamageClass damageClass)
		{
			if (damageClass == DamageClass.Ranged)
				return StatInheritanceData.Full;

			return StatInheritanceData.None;
		}

		public override bool GetEffectInheritance(DamageClass damageClass)
		{
			return damageClass == DamageClass.Ranged;
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
