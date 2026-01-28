namespace Destiny2.Common.Weapons
{
	public enum Destiny2AmmoType
	{
		Primary,
		Special,
		Heavy
	}

	public static class Destiny2AmmoTypeExtensions
	{
		public static string GetIconTexture(this Destiny2AmmoType ammoType)
		{
			return ammoType switch
			{
				Destiny2AmmoType.Primary => "Destiny2/Assets/Ammo/Primary",
				Destiny2AmmoType.Special => "Destiny2/Assets/Ammo/Special",
				Destiny2AmmoType.Heavy => "Destiny2/Assets/Ammo/Heavy",
				_ => "Destiny2/Assets/Ammo/Primary"
			};
		}
	}
}
