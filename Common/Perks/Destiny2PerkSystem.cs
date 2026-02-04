using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace Destiny2.Common.Perks
{
    public sealed class Destiny2PerkSystem : ModSystem
    {
        private static readonly Dictionary<string, Destiny2Perk> PerksByKey = new Dictionary<string, Destiny2Perk>(StringComparer.Ordinal);
        private static readonly List<Destiny2Perk> PerkList = new List<Destiny2Perk>();
        private static readonly List<Destiny2Perk> FramePerkList = new List<Destiny2Perk>();

        public static IReadOnlyList<Destiny2Perk> Perks => PerkList;
        public static IReadOnlyList<Destiny2Perk> FramePerks => FramePerkList;

        public override void OnModLoad()
        {
            InitializePerks();
        }

        public override void Unload()
        {
            PerkList.Clear();
            PerksByKey.Clear();
            FramePerkList.Clear();
        }

        private static void InitializePerks()
        {
            Register(new AdaptiveFramePerk());
            Register(new HighImpactFramePerk());
            Register(new LightweightBowFramePerk());
            Register(new AdaptiveBurstPerk());
            Register(new PrecisionBowFramePerk());
            Register(new PinpointSlugFramePerk());
            Register(new HeavyBurstFramePerk());
            Register(new LightweightFramePerk());
            Register(new RapidFireFramePerk());
            Register(new MicroMissileFramePerk());
            Register(new AreaDenialFramePerk());
            Register(new PrecisionFramePerk());
            Register(new AggressiveFramePerk());
            Register(new AggressiveBurstFramePerk());
            Register(new WaveFramePerk());
            Register(new TheRightChoiceFramePerk());
            Register(new HammerForgedRiflingPerk());
            Register(new SmallborePerk());
            Register(new ExtendedMagPerk());
            Register(new TacticalMagPerk());
            Register(new AlloyMagPerk());
            Register(new CompositeStockPerk());
            Register(new OutlawPerk());
            Register(new RapidHitPerk());
            Register(new KillClipPerk());
            Register(new FrenzyPerk());
            Register(new EyesUpGuardianPerk());
            Register(new ShootToLootPerk());
            Register(new FourthTimesTheCharmPerk());
            Register(new RampagePerk());
            Register(new OnslaughtPerk());
            Register(new KineticTremorsPerk());
            Register(new AdagioPerk());
            Register(new TargetLockPerk());
            Register(new DynamicSwayReductionPerk());
            Register(new FeedingFrenzyPerk());
            Register(new ExplosiveShadowPerk());
            Register(new VorpalWeaponPerk());
            Register(new IncandescentPerk());
            Register(new ArmorPiercingRoundsPerk());
        }

        public static void Register(Destiny2Perk perk)
        {
            if (perk == null)
                throw new ArgumentNullException(nameof(perk));

            if (PerksByKey.ContainsKey(perk.Key))
                throw new InvalidOperationException($"Duplicate perk key '{perk.Key}'.");

            if (perk.IsFrame)
                FramePerkList.Add(perk);
            else
                PerkList.Add(perk);
            PerksByKey.Add(perk.Key, perk);
        }

        public static bool TryGet(string key, out Destiny2Perk perk)
        {
            if (key == null)
            {
                perk = null;
                return false;
            }

            return PerksByKey.TryGetValue(key, out perk);
        }

        public static Destiny2Perk RollRandom()
        {
            if (PerkList.Count == 0)
                return null;

            int index = Main.rand.Next(PerkList.Count);
            return PerkList[index];
        }
    }
}
