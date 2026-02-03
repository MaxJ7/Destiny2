using Terraria;
using Terraria.ModLoader;

namespace Destiny2.Content.Buffs
{
	public sealed class FrenzyBuff : ModBuff
	{
		public override string Texture => "Destiny2/Assets/Perks/Frenzy";

		public override void SetStaticDefaults()
		{
			Main.buffNoTimeDisplay[Type] = true;
			Main.buffNoSave[Type] = true;
		}
	}

	public sealed class OutlawBuff : ModBuff
	{
		public override string Texture => "Destiny2/Assets/Perks/Outlaw";

		public override void SetStaticDefaults()
		{
			Main.buffNoTimeDisplay[Type] = true;
			Main.buffNoSave[Type] = true;
		}
	}

	public sealed class RapidHitBuff : ModBuff
	{
		public override string Texture => "Destiny2/Assets/Perks/RapidHit";

		public override void SetStaticDefaults()
		{
			Main.buffNoTimeDisplay[Type] = true;
			Main.buffNoSave[Type] = true;
		}
	}

	public sealed class KillClipBuff : ModBuff
	{
		public override string Texture => "Destiny2/Assets/Perks/KillClip";

		public override void SetStaticDefaults()
		{
			Main.buffNoTimeDisplay[Type] = true;
			Main.buffNoSave[Type] = true;
		}
	}

	public sealed class RampageBuff : ModBuff
	{
		public override string Texture => "Destiny2/Assets/Perks/Rampage";

		public override void SetStaticDefaults()
		{
			Main.buffNoTimeDisplay[Type] = true;
			Main.buffNoSave[Type] = true;
		}
	}

	public sealed class OnslaughtBuff : ModBuff
	{
		public override string Texture => "Destiny2/Assets/Perks/Onslaught";

		public override void SetStaticDefaults()
		{
			Main.buffNoTimeDisplay[Type] = true;
			Main.buffNoSave[Type] = true;
		}
	}

	public sealed class FeedingFrenzyBuff : ModBuff
	{
		public override string Texture => "Destiny2/Assets/Perks/FeedingFrenzy";

		public override void SetStaticDefaults()
		{
			Main.buffNoTimeDisplay[Type] = true;
			Main.buffNoSave[Type] = true;
		}
	}

	public sealed class AdagioBuff : ModBuff
	{
		public override string Texture => "Destiny2/Assets/Perks/Adagio";

		public override void SetStaticDefaults()
		{
			Main.buffNoTimeDisplay[Type] = true;
			Main.buffNoSave[Type] = true;
		}
	}

	public sealed class TargetLockBuff : ModBuff
	{
		public override string Texture => "Destiny2/Assets/Perks/TargetLock";

		public override void SetStaticDefaults()
		{
			Main.buffNoTimeDisplay[Type] = true;
			Main.buffNoSave[Type] = true;
		}
	}

	public sealed class DynamicSwayReductionBuff : ModBuff
	{
		public override string Texture => "Destiny2/Assets/Perks/DynamicSwayReduction";

		public override void SetStaticDefaults()
		{
			Main.buffNoTimeDisplay[Type] = true;
			Main.buffNoSave[Type] = true;
		}
	}

	public sealed class FourthTimesTheCharmBuff : ModBuff
	{
		public override string Texture => "Destiny2/Assets/Perks/FourthTimesTheCharm";

		public override void SetStaticDefaults()
		{
			Main.buffNoTimeDisplay[Type] = false;
			Main.buffNoSave[Type] = true;
		}
	}
}
