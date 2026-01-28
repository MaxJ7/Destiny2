 using Destiny2.Common.Weapons;
using Terraria;

namespace Destiny2.Common.Perks
{
	public abstract class Destiny2Perk
	{
		public virtual string Key => GetType().Name;
		public virtual string DisplayName => Key;
		public virtual string Description => string.Empty;
		public virtual string IconTexture => null;
		public virtual bool IsFrame => false;
		public virtual PerkSlotType SlotType => PerkSlotType.Major;

		public virtual void ModifyStats(ref Destiny2WeaponStats stats)
		{
		}

		public virtual void OnProjectileHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
		{
		}
	}
}
