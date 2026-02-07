using System;
using System.Collections.Generic;
using Destiny2.Common.Perks;
using Destiny2.Common.Players;
using Destiny2.Content.Projectiles;
using Destiny2.Common.NPCs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Destiny2.Common.Weapons
{
    public abstract partial class Destiny2WeaponItem
    {
        private static bool DiagnosticsEnabled => global::Destiny2.Destiny2.DiagnosticsEnabled;

        internal readonly struct PerkHudEntry
        {
            public readonly string DisplayName;
            public readonly string IconTexture;
            public readonly int Timer;
            public readonly int MaxTimer;
            public readonly int Stacks;
            public readonly bool ShowStacks;

            public PerkHudEntry(string displayName, string iconTexture, int timer, int maxTimer, int stacks, bool showStacks)
            {
                DisplayName = displayName;
                IconTexture = iconTexture;
                Timer = timer;
                MaxTimer = maxTimer;
                Stacks = stacks;
                ShowStacks = showStacks;
            }
        }

        private struct KineticTremorsTargetState
        {
            public int HitCount;
            public int HitTimer;
            public int CooldownTimer;
        }

        private int outlawTimer;
        private int rapidHitTimer;
        private int rapidHitStacks;
        private int fourthTimesHitTimer;
        private int fourthTimesHitCount;
        private int rampageTimer;
        private int rampageStacks;
        private int killClipWindowTimer;
        private int killClipTimer;
        private bool killClipPending;
        private int frenzyTimer;
        private int frenzyCombatTimer;
        private int frenzyCombatGraceTimer;
        private int onslaughtTimer;
        private int onslaughtStacks;
        private int feedingFrenzyTimer;
        private int feedingFrenzyStacks;
        private int adagioTimer;
        private int targetLockHitTimer;
        private int targetLockHitCount;
        private int targetLockTargetId = -1;
        private int dynamicSwayTimer;
        private int dynamicSwayStacks;
        private int rightChoiceShotCount;
        private int touchOfMaliceKillCount;
        private int blightStacks;
        private bool isBlightLauncherActive;
        private int reloadHoldTimer;
        private int corruptionHitCount;
        private ulong lastCorruptionHitTick;
        private int archersTempoTimer;
        private int killingWindTimer;
        private int successfulWarmUpTimer;
        private int fireflyTimer;
        private int precisionInstrumentTimer;
        private int precisionInstrumentStacks;
        private int reconstructionTimer;
        private int focusedFuryHitCount;
        private int focusedFuryTimer;
        private int killingTallyStacks;
        private HashSet<int> oneForAllHitIds = new HashSet<int>();
        private int oneForAllWindowTimer;
        private int oneForAllBuffTimer;
        private int desperadoPrecisionKillTimer;
        private int desperadoBuffTimer;
        private Dictionary<int, KineticTremorsTargetState> kineticTremorsTargets = new Dictionary<int, KineticTremorsTargetState>();
        private readonly List<int> kineticTremorsTargetKeys = new List<int>();

        private void ApplyActivePerkStats(ref Destiny2WeaponStats stats)
        {
            if (outlawTimer > 0)
                stats.ReloadSpeed += OutlawPerk.ReloadSpeedBonus;

            if (rapidHitTimer > 0 && rapidHitStacks > 0)
            {
                int stacks = Math.Clamp(rapidHitStacks, 0, RapidHitPerk.MaxStacks);
                stats.Stability += RapidHitPerk.StabilityBonusByStacks[stacks];
                stats.ReloadSpeed += RapidHitPerk.ReloadSpeedBonusByStacks[stacks];
            }

            if (frenzyTimer > 0)
                stats.ReloadSpeed += FrenzyPerk.ReloadSpeedBonus;

            if (onslaughtTimer > 0 && onslaughtStacks > 0)
            {
                int stacks = Math.Clamp(onslaughtStacks, 0, OnslaughtPerk.MaxStacks);
                stats.ReloadSpeed += OnslaughtPerk.ReloadSpeedByStacks[stacks];
                stats.RoundsPerMinute = ApplyRpmScalar(stats.RoundsPerMinute, OnslaughtPerk.RpmScalarByStacks[stacks]);
            }

            if (feedingFrenzyTimer > 0 && feedingFrenzyStacks > 0)
            {
                int stacks = Math.Clamp(feedingFrenzyStacks, 0, FeedingFrenzyPerk.MaxStacks);
                stats.ReloadSpeed += FeedingFrenzyPerk.ReloadSpeedByStacks[stacks];
            }

            if (adagioTimer > 0)
            {
                stats.Range += AdagioPerk.RangeBonus;
                stats.RoundsPerMinute = ApplyRpmScalar(stats.RoundsPerMinute, AdagioPerk.RpmScalar);
                if (IsBowWeapon())
                    stats.ChargeTime = (int)(stats.ChargeTime * AdagioPerk.BowChargeTimeScalar);
            }

            if (dynamicSwayTimer > 0 && dynamicSwayStacks > 0)
                stats.Stability += dynamicSwayStacks * DynamicSwayReductionPerk.StabilityPerStack;

            if (archersTempoTimer > 0)
                stats.ChargeTime = (int)(stats.ChargeTime * ArchersTempoPerk.ChargeTimeScalar);

            if (killingWindTimer > 0)
                stats.Range += KillingWindPerk.RangeBonus;

            if (fireflyTimer > 0)
                stats.ReloadSpeed += FireflyPerk.ReloadSpeedBonus;

            if (successfulWarmUpTimer > 0)
                stats.ChargeTime = (int)(stats.ChargeTime * SuccessfulWarmUpPerk.ChargeTimeScalar);

            if (desperadoBuffTimer > 0 && GetBurstCount() > 1 && HasPerk<DesperadoPerk>())
                stats.RoundsPerMinute = ApplyRpmScalar(stats.RoundsPerMinute, 1f / (1f - DesperadoPerk.FiringDelayReduction));
        }

        internal bool IsFocusedFuryActive => focusedFuryTimer > 0;
        internal bool IsOneForAllActive => oneForAllBuffTimer > 0;
        internal int KillingTallyStacks => killingTallyStacks;

        internal float GetKillingTallyMultiplier()
        {
            if (killingTallyStacks <= 0) return 1f;
            return KillingTallyPerk.DamageMultiplierByStacks[Math.Clamp(killingTallyStacks, 0, KillingTallyPerk.MaxStacks)];
        }

        internal void AppendPerkHudEntries(List<PerkHudEntry> entries)
        {
            if (entries == null)
                return;

            if (frenzyTimer > 0)
                AddPerkHudEntry(entries, nameof(FrenzyPerk), frenzyTimer, FrenzyPerk.DurationTicks, 1, false);

            if (outlawTimer > 0)
                AddPerkHudEntry(entries, nameof(OutlawPerk), outlawTimer, OutlawPerk.DurationTicks, 1, false);

            if (rapidHitTimer > 0 && rapidHitStacks > 0)
                AddPerkHudEntry(entries, nameof(RapidHitPerk), rapidHitTimer, RapidHitPerk.DurationTicks, rapidHitStacks, true);

            if (killClipTimer > 0)
                AddPerkHudEntry(entries, nameof(KillClipPerk), killClipTimer, KillClipPerk.DurationTicks, 1, false);

            if (rampageTimer > 0 && rampageStacks > 0)
                AddPerkHudEntry(entries, nameof(RampagePerk), rampageTimer, RampagePerk.DurationTicks, rampageStacks, true);

            if (onslaughtTimer > 0 && onslaughtStacks > 0)
                AddPerkHudEntry(entries, nameof(OnslaughtPerk), onslaughtTimer, OnslaughtPerk.DurationTicks, onslaughtStacks, true);

            if (feedingFrenzyTimer > 0 && feedingFrenzyStacks > 0)
                AddPerkHudEntry(entries, nameof(FeedingFrenzyPerk), feedingFrenzyTimer, FeedingFrenzyPerk.DurationTicks, feedingFrenzyStacks, true);

            if (adagioTimer > 0)
                AddPerkHudEntry(entries, nameof(AdagioPerk), adagioTimer, AdagioPerk.DurationTicks, 1, false);

            if (targetLockHitTimer > 0 && targetLockHitCount > 0)
            {
                int stacks = GetTargetLockStacks();
                if (stacks <= 0)
                    stacks = 1;
                AddPerkHudEntry(entries, nameof(TargetLockPerk), targetLockHitTimer, TargetLockPerk.HitWindowTicks, stacks, stacks > 1);
            }

            if (dynamicSwayTimer > 0 && dynamicSwayStacks > 0)
                AddPerkHudEntry(entries, nameof(DynamicSwayReductionPerk), dynamicSwayTimer, DynamicSwayReductionPerk.HoldWindowTicks, dynamicSwayStacks, true);

            if (fourthTimesHitTimer > 0 && fourthTimesHitCount > 0)
                AddPerkHudEntry(entries, nameof(FourthTimesTheCharmPerk), fourthTimesHitTimer, FourthTimesTheCharmPerk.WindowTicks, fourthTimesHitCount, true);

            if (isBlightLauncherActive)
                AddPerkHudEntry(entries, nameof(ChargedWithBlightPerk), 1, 1, 0, false);
            else if (blightStacks > 0)
                AddPerkHudEntry(entries, nameof(ChargedWithBlightPerk), 1, 1, blightStacks, true);

            if (archersTempoTimer > 0)
                AddPerkHudEntry(entries, nameof(ArchersTempoPerk), archersTempoTimer, ArchersTempoPerk.DurationTicks, 1, false);

            if (killingWindTimer > 0)
                AddPerkHudEntry(entries, nameof(KillingWindPerk), killingWindTimer, KillingWindPerk.DurationTicks, 1, false);

            if (successfulWarmUpTimer > 0)
                AddPerkHudEntry(entries, nameof(SuccessfulWarmUpPerk), successfulWarmUpTimer, SuccessfulWarmUpPerk.DurationTicks, 1, false);

            if (fireflyTimer > 0)
                AddPerkHudEntry(entries, nameof(FireflyPerk), fireflyTimer, FireflyPerk.DurationTicks, 1, false);

            if (precisionInstrumentTimer > 0 && precisionInstrumentStacks > 0)
                AddPerkHudEntry(entries, nameof(PrecisionInstrumentPerk), precisionInstrumentTimer, PrecisionInstrumentPerk.DurationTicks, precisionInstrumentStacks, true);

            if (focusedFuryTimer > 0)
                AddPerkHudEntry(entries, nameof(FocusedFuryPerk), focusedFuryTimer, FocusedFuryPerk.DurationTicks, 1, false);

            if (killingTallyStacks > 0)
                AddPerkHudEntry(entries, nameof(KillingTallyPerk), 1, 1, killingTallyStacks, true);

            if (oneForAllBuffTimer > 0)
                AddPerkHudEntry(entries, nameof(OneForAllPerk), oneForAllBuffTimer, OneForAllPerk.DurationTicks, 1, false);

            if (desperadoBuffTimer > 0)
                AddPerkHudEntry(entries, nameof(DesperadoPerk), desperadoBuffTimer, DesperadoPerk.DurationTicks, 1, false);
        }

        private static void AddPerkHudEntry(List<PerkHudEntry> entries, string perkKey, int timer, int maxTimer, int stacks, bool showStacks)
        {
            if (timer <= 0)
                return;

            if (!Destiny2PerkSystem.TryGet(perkKey, out Destiny2Perk perk))
                return;

            string iconTexture = perk.IconTexture;
            if (string.IsNullOrWhiteSpace(iconTexture))
                return;

            entries.Add(new PerkHudEntry(perk.DisplayName, iconTexture, timer, maxTimer, stacks, showStacks));
        }


        private int GetTargetLockStacks()
        {
            int magazineSize = GetStats().Magazine;
            if (magazineSize <= 0 || targetLockHitCount <= 0)
                return 0;

            float ratio = targetLockHitCount / (float)magazineSize;
            int stacks = 0;
            for (int i = 0; i < TargetLockPerk.VisualStackThresholds.Length; i++)
            {
                if (ratio >= TargetLockPerk.VisualStackThresholds[i])
                    stacks = i + 1;
                else
                    break;
            }

            return stacks;
        }

        protected float GetReloadSpeedTimeScalar()
        {
            float scalar = 1f;
            if (outlawTimer > 0)
                scalar *= OutlawPerk.ReloadTimeScalar;

            if (rapidHitTimer > 0 && rapidHitStacks > 0)
            {
                int stacks = Math.Clamp(rapidHitStacks, 0, RapidHitPerk.MaxStacks);
                scalar *= RapidHitPerk.ReloadTimeScalarByStacks[stacks];
            }

            if (feedingFrenzyTimer > 0 && feedingFrenzyStacks > 0)
            {
                int stacks = Math.Clamp(feedingFrenzyStacks, 0, FeedingFrenzyPerk.MaxStacks);
                scalar *= FeedingFrenzyPerk.ReloadTimeScalarByStacks[stacks];
            }

            if (HasPerk<AlloyMagPerk>())
                scalar *= GetAlloyMagScalar();

            return scalar;
        }

        private static int ApplyRpmScalar(int rpm, float scalar)
        {
            if (rpm <= 0)
                return rpm;

            int scaled = (int)Math.Round(rpm * scalar);
            return Math.Max(1, scaled);
        }

        private float GetAlloyMagScalar()
        {
            int magazineSize = GetStats().Magazine;
            if (magazineSize <= 0)
                return 1f;

            float ratio = Math.Clamp(currentMagazine / (float)magazineSize, 0f, 1f);
            if (ratio > 0.5f)
                return 1f;

            float t = ratio / 0.5f;
            return MathHelper.Lerp(AlloyMagPerk.EmptyMagScalar, AlloyMagPerk.HalfMagScalar, t);
        }

        private void UpdatePerkTimers(Player player)
        {
            bool outlawWasActive = outlawTimer > 0;
            bool rapidHitWasActive = rapidHitTimer > 0 && rapidHitStacks > 0;
            bool killClipWasActive = killClipTimer > 0;
            bool frenzyWasActive = frenzyTimer > 0;
            bool rampageWasActive = rampageTimer > 0 && rampageStacks > 0;
            bool onslaughtWasActive = onslaughtTimer > 0 && onslaughtStacks > 0;
            bool feedingFrenzyWasActive = feedingFrenzyTimer > 0 && feedingFrenzyStacks > 0;
            bool adagioWasActive = adagioTimer > 0;

            if (outlawTimer > 0)
                outlawTimer--;

            if (outlawWasActive && outlawTimer <= 0)
                SendPerkDebug(player, "Outlaw expired");

            if (rapidHitTimer > 0)
            {
                rapidHitTimer--;
                if (rapidHitTimer <= 0)
                    rapidHitStacks = 0;
            }

            if (rapidHitWasActive && rapidHitTimer <= 0)
                SendPerkDebug(player, "Rapid Hit expired");

            if (killClipWindowTimer > 0)
                killClipWindowTimer--;

            if (killClipTimer > 0)
                killClipTimer--;

            if (killClipWasActive && killClipTimer <= 0)
                SendPerkDebug(player, "Kill Clip expired");

            if (rampageTimer > 0)
            {
                rampageTimer--;
                if (rampageTimer <= 0)
                    rampageStacks = 0;
            }

            if (rampageWasActive && rampageTimer <= 0)
                SendPerkDebug(player, "Rampage expired");

            if (frenzyTimer > 0)
                frenzyTimer--;

            if (frenzyWasActive && frenzyTimer <= 0)
                SendPerkDebug(player, "Frenzy expired");

            if (onslaughtTimer > 0)
            {
                onslaughtTimer--;
                if (onslaughtTimer <= 0)
                    onslaughtStacks = 0;
            }

            if (onslaughtWasActive && onslaughtTimer <= 0)
                SendPerkDebug(player, "Onslaught expired");

            if (feedingFrenzyTimer > 0)
            {
                feedingFrenzyTimer--;
                if (feedingFrenzyTimer <= 0)
                    feedingFrenzyStacks = 0;
            }

            if (feedingFrenzyWasActive && feedingFrenzyTimer <= 0)
                SendPerkDebug(player, "Feeding Frenzy expired");

            if (adagioTimer > 0)
                adagioTimer--;

            if (adagioWasActive && adagioTimer <= 0)
                SendPerkDebug(player, "Adagio expired");

            if (frenzyCombatGraceTimer > 0)
            {
                frenzyCombatGraceTimer--;
                frenzyCombatTimer++;
                if (frenzyTimer <= 0 && frenzyCombatTimer >= FrenzyPerk.ActivationTicks)
                    ActivateFrenzy(player);
            }
            else
            {
                frenzyCombatTimer = 0;
            }

            if (fourthTimesHitTimer > 0)
            {
                fourthTimesHitTimer--;
                if (fourthTimesHitTimer <= 0)
                    fourthTimesHitCount = 0;
            }

            if (dynamicSwayTimer > 0)
            {
                dynamicSwayTimer--;
                if (dynamicSwayTimer <= 0)
                    dynamicSwayStacks = 0;
            }

            if (archersTempoTimer > 0)
                archersTempoTimer--;

            if (killingWindTimer > 0)
                killingWindTimer--;

            if (successfulWarmUpTimer > 0)
                successfulWarmUpTimer--;

            if (fireflyTimer > 0)
                fireflyTimer--;

            if (precisionInstrumentTimer > 0)
            {
                precisionInstrumentTimer--;
                if (precisionInstrumentTimer <= 0)
                    precisionInstrumentStacks = 0;
            }

            if (targetLockHitCount > 0 && !IsPlayerFiring(player))
                ResetTargetLockState();

            UpdateKineticTremorsTargets();

            // RECONSTRUCTION
            if (HasPerk<ReconstructionPerk>())
            {
                if (!IsPlayerFiring(player) && !isReloading)
                {
                    reconstructionTimer++;
                    if (reconstructionTimer >= ReconstructionPerk.FillIntervalTicks)
                    {
                        reconstructionTimer = 0;
                        int magSize = GetStats().Magazine;
                        if (currentMagazine < magSize * 2)
                        {
                            int toLoad = (int)Math.Ceiling(magSize * ReconstructionPerk.FillPercent);
                            currentMagazine = Math.Min(magSize * 2, currentMagazine + toLoad);
                        }
                    }
                }
                else
                {
                    reconstructionTimer = 0;
                }
            }

            // FOCUSED FURY
            if (focusedFuryTimer > 0)
                focusedFuryTimer--;

            // ONE FOR ALL
            if (oneForAllWindowTimer > 0)
            {
                oneForAllWindowTimer--;
                if (oneForAllWindowTimer <= 0)
                    oneForAllHitIds.Clear();
            }
            if (oneForAllBuffTimer > 0)
                oneForAllBuffTimer--;

            // DESPERADO
            if (desperadoPrecisionKillTimer > 0)
                desperadoPrecisionKillTimer--;

            if (desperadoBuffTimer > 0)
                desperadoBuffTimer--;

            // KILLING TALLY
            // Reset on stow
            if (player?.HeldItem?.ModItem != this)
            {
                killingTallyStacks = 0;
            }

            if (player?.HeldItem?.ModItem == this)
            {
                Destiny2Player modPlayer = player.GetModPlayer<Destiny2Player>();
                modPlayer.RequestFrenzyBuff(frenzyTimer);
                modPlayer.RequestOutlawBuff(outlawTimer);
                modPlayer.RequestRapidHitBuff(rapidHitStacks > 0 ? rapidHitTimer : 0);
                modPlayer.RequestKillClipBuff(killClipTimer);
                modPlayer.RequestRampageBuff(rampageStacks > 0 ? rampageTimer : 0);
                modPlayer.RequestOnslaughtBuff(onslaughtStacks > 0 ? onslaughtTimer : 0);
                modPlayer.RequestFeedingFrenzyBuff(feedingFrenzyStacks > 0 ? feedingFrenzyTimer : 0);
                modPlayer.RequestAdagioBuff(adagioTimer);
                modPlayer.RequestTargetLockBuff(targetLockHitCount > 0 ? targetLockHitTimer : 0);
                modPlayer.RequestDynamicSwayBuff(dynamicSwayStacks > 0 ? dynamicSwayTimer : 0);
                modPlayer.RequestFourthTimesBuff(fourthTimesHitTimer);
                modPlayer.RequestArchersTempoBuff(archersTempoTimer);
            }

            UpdateBlightLauncherMode(player);
        }

        internal void NotifyProjectileHit(Player player, NPC target, NPC.HitInfo hit, int damageDone, bool hasOutlaw, bool hasRapidHit, bool hasKillClip, bool hasFrenzy, bool hasFourthTimes, bool hasRampage, bool hasOnslaught, bool hasAdagio, bool hasFeedingFrenzy, bool hasArchersTempo, bool hasKillingWind, bool hasSuccessfulWarmUp, bool hasFirefly, bool hasOneForAll, bool hasFocusedFury, bool hasKillingTally, bool hasExplosivePayload, bool hasDesperado, bool isKill, bool isBlightProjectile, bool isNaniteProjectile, bool isPrecision)
        {
            if (hasRapidHit && isPrecision && !isNaniteProjectile && !isBlightProjectile)
                AddRapidHitStack(player);

            if (hasFocusedFury)
                RegisterFocusedFuryHit(player, isPrecision);

            if (hasOneForAll && target != null)
                RegisterOneForAllHit(player, target.whoAmI);

            if (hasDesperado && isPrecision && !isNaniteProjectile && !isBlightProjectile && isKill)
                StartDesperadoPrecisionKillWindow(player);

            if (hasFourthTimes && isPrecision && !isNaniteProjectile && !isBlightProjectile)
                RegisterFourthTimesHit(player);

            if (hasFrenzy)
                RegisterCombat(player);

            if (HasFrame && FramePerkKey == nameof(TheCorruptionSpreadsFramePerk) && !isNaniteProjectile)
            {
                RegisterCorruptionHit(player, target);
            }

            if (hasArchersTempo && isPrecision)
                ActivateArchersTempo(player);

            if (HasPerk<ChargedWithBlightPerk>() && isPrecision && !isBlightProjectile)
            {
                if (blightStacks < ChargedWithBlightPerk.MaxStacks)
                {
                    blightStacks++;
                    if (blightStacks >= ChargedWithBlightPerk.MaxStacks)
                        SendPerkDebug(player, "Blight Launcher Ready! Hold Reload to activate.");
                }
            }

            if (target == null || target.friendly || !isKill)
                return;

            if (hasOutlaw && isPrecision)
                ActivateOutlaw(player);

            if (hasKillClip)
                killClipWindowTimer = KillClipPerk.WindowTicks;

            if (hasRampage)
                AddRampageStack(player);

            if (hasOnslaught)
                AddOnslaughtStack(player);

            if (hasAdagio)
                ActivateAdagio(player);

            if (hasFeedingFrenzy)
                AddFeedingFrenzyStack(player);

            if (isKill && HasPerk<TouchOfMalicePerk>())
            {
                touchOfMaliceKillCount++;
                if (touchOfMaliceKillCount >= TouchOfMalicePerk.KillsForHeal)
                {
                    touchOfMaliceKillCount = 0;
                    int heal = TouchOfMalicePerk.HealAmount;
                    player.statLife = Math.Min(player.statLife + heal, player.statLifeMax2);
                    player.HealEffect(heal);
                    SendPerkDebug(player, "Touch of Malice Heal");
                }
            }

            if (isKill)
            {
                if (hasKillingWind)
                    ActivateKillingWind(player);
                if (hasSuccessfulWarmUp)
                    ActivateSuccessfulWarmUp(player);
                if (hasFirefly && isPrecision)
                    ActivateFirefly(player);
                if (hasKillingTally)
                    AddKillingTallyStack(player);
            }
        }

        public void RegisterPrecisionInstrumentHit(Player player, bool isPrecision)
        {
            if (!HasPerk<PrecisionInstrumentPerk>())
                return;

            precisionInstrumentStacks = Math.Min(precisionInstrumentStacks + 1, PrecisionInstrumentPerk.MaxStacks);
            precisionInstrumentTimer = IsBowWeapon() ? PrecisionInstrumentPerk.ChargeDurationTicks : PrecisionInstrumentPerk.DurationTicks;
            if (DiagnosticsEnabled)
                SendPerkDebug(player, $"Precision Instrument x{precisionInstrumentStacks}");
        }

        public void NotifyPrecisionInstrumentMiss()
        {
            if (precisionInstrumentStacks > 0)
            {
                precisionInstrumentStacks = 0;
                precisionInstrumentTimer = 0;
            }
        }

        private void ActivateKillingWind(Player player)
        {
            killingWindTimer = KillingWindPerk.DurationTicks;
            SendPerkDebug(player, "Killing Wind activated");
        }

        private void ActivateSuccessfulWarmUp(Player player)
        {
            successfulWarmUpTimer = SuccessfulWarmUpPerk.DurationTicks;
            SendPerkDebug(player, "Successful Warm-Up activated");
        }

        private void ActivateFirefly(Player player)
        {
            fireflyTimer = FireflyPerk.DurationTicks;
            SendPerkDebug(player, "Firefly: Reload Speed increased");
        }

        internal bool TryConsumeRightChoiceShot()
        {
            rightChoiceShotCount++;

            // Always log shot counter for debugging
            global::Destiny2.Destiny2.LogDiagnostic($"RightChoice shot count={rightChoiceShotCount}/{TheRightChoiceFramePerk.ShotsRequired}");

            if (rightChoiceShotCount < TheRightChoiceFramePerk.ShotsRequired)
            {
                return false;
            }

            rightChoiceShotCount = 0;
            global::Destiny2.Destiny2.LogDiagnostic("RightChoice: 7th shot reached! Ricochet ARMED!");
            return true;
        }

        internal void NotifyPlayerHurt(Player player)
        {
            if (!HasPerk<FrenzyPerk>())
                return;

            RegisterCombat(player);
        }

        private void ActivateOutlaw(Player player)
        {
            outlawTimer = OutlawPerk.DurationTicks;
            SendPerkDebug(player, "Outlaw activated");
        }

        private void AddRapidHitStack(Player player)
        {
            int nextStacks = Math.Min(rapidHitStacks + 1, RapidHitPerk.MaxStacks);
            if (nextStacks != rapidHitStacks)
            {
                rapidHitStacks = nextStacks;
                SendPerkDebug(player, $"Rapid Hit x{rapidHitStacks}");
            }

            rapidHitTimer = RapidHitPerk.DurationTicks;
        }

        private void RegisterFourthTimesHit(Player player)
        {
            if (fourthTimesHitTimer <= 0)
                fourthTimesHitCount = 0;

            fourthTimesHitCount++;
            fourthTimesHitTimer = FourthTimesTheCharmPerk.WindowTicks;

            if (fourthTimesHitCount < FourthTimesTheCharmPerk.HitsRequired)
                return;

            fourthTimesHitCount = 0;
            fourthTimesHitTimer = 0;
            GrantFourthTimesAmmo(player);
        }

        private void GrantFourthTimesAmmo(Player player)
        {
            int magazineSize = GetStats().Magazine;
            if (magazineSize <= 0)
                return;

            int nextMagazine = Math.Min(magazineSize, currentMagazine + FourthTimesTheCharmPerk.AmmoReturned);
            if (nextMagazine == currentMagazine)
                return;

            currentMagazine = nextMagazine;
            SendPerkDebug(player, "Fourth Times the Charm");
        }

        private void AddRampageStack(Player player)
        {
            int nextStacks = Math.Min(rampageStacks + 1, RampagePerk.MaxStacks);
            if (nextStacks != rampageStacks)
            {
                rampageStacks = nextStacks;
                SendPerkDebug(player, $"Rampage x{rampageStacks}");
            }

            rampageTimer = RampagePerk.DurationTicks;
        }

        private void AddOnslaughtStack(Player player)
        {
            int nextStacks = Math.Min(onslaughtStacks + 1, OnslaughtPerk.MaxStacks);
            if (nextStacks != onslaughtStacks)
            {
                onslaughtStacks = nextStacks;
                SendPerkDebug(player, $"Onslaught x{onslaughtStacks}");
            }

            onslaughtTimer = OnslaughtPerk.DurationTicks;
        }

        private void AddFeedingFrenzyStack(Player player)
        {
            int nextStacks = Math.Min(feedingFrenzyStacks + 1, FeedingFrenzyPerk.MaxStacks);
            if (nextStacks != feedingFrenzyStacks)
            {
                feedingFrenzyStacks = nextStacks;
                SendPerkDebug(player, $"Feeding Frenzy x{feedingFrenzyStacks}");
            }

            feedingFrenzyTimer = FeedingFrenzyPerk.DurationTicks;
        }

        private void ActivateAdagio(Player player)
        {
            adagioTimer = AdagioPerk.DurationTicks;
            SendPerkDebug(player, "Adagio activated");
        }

        private void ActivateKillClip(Player player)
        {
            killClipTimer = KillClipPerk.DurationTicks;
            SendPerkDebug(player, "Kill Clip activated");
        }

        private void ActivateFrenzy(Player player)
        {
            frenzyTimer = FrenzyPerk.DurationTicks;
            SendPerkDebug(player, "Frenzy activated");
        }

        private void RegisterCombat(Player player)
        {
            frenzyCombatGraceTimer = FrenzyPerk.CombatGraceTicks;
            if (frenzyTimer > 0)
                frenzyTimer = FrenzyPerk.DurationTicks;
        }

        private void RegisterDynamicSwayShot()
        {
            dynamicSwayTimer = DynamicSwayReductionPerk.HoldWindowTicks;
            if (dynamicSwayStacks < DynamicSwayReductionPerk.MaxStacks)
                dynamicSwayStacks++;
        }

        private void ActivateArchersTempo(Player player)
        {
            archersTempoTimer = ArchersTempoPerk.DurationTicks;
            SendPerkDebug(player, "Archer's Tempo activated");
        }

        internal float RegisterTargetLockHit(NPC target)
        {
            if (target == null || !target.CanBeChasedBy())
                return 0f;

            if (targetLockTargetId != target.whoAmI)
                ResetTargetLockState();

            targetLockTargetId = target.whoAmI;
            targetLockHitCount++;
            targetLockHitTimer = TargetLockPerk.HitWindowTicks;

            int magazineSize = GetStats().Magazine;
            if (magazineSize <= 0)
                return 0f;

            float ratio = targetLockHitCount / (float)magazineSize;
            if (ratio < TargetLockPerk.MinHitsRatio)
                return 0f;

            float t = (ratio - TargetLockPerk.MinHitsRatio) / (TargetLockPerk.MaxHitsRatio - TargetLockPerk.MinHitsRatio);
            t = MathHelper.Clamp(t, 0f, 1f);
            t = MathHelper.Clamp(t, 0f, 1f);
            return MathHelper.Lerp(TargetLockPerk.MinDamageBonus, TargetLockPerk.MaxDamageBonus, t);
        }


        private void ResetTargetLockState()
        {
            targetLockHitCount = 0;
            targetLockHitTimer = 0;
            targetLockTargetId = -1;
        }

        private static bool IsPlayerFiring(Player player)
        {
            if (player == null)
                return false;

            if (player.controlUseItem || player.channel)
                return true;

            return player.itemAnimation > 0 || player.itemTime > 0;
        }

        internal void NotifyTargetLockMiss()
        {
            if (targetLockHitCount <= 0)
                return;

            ResetTargetLockState();
        }

        public float GetPrecisionInstrumentMultiplier()
        {
            if (precisionInstrumentStacks <= 0)
                return 0f;

            int stacks = Math.Clamp(precisionInstrumentStacks, 0, PrecisionInstrumentPerk.MaxStacks);
            return PrecisionInstrumentPerk.PrecisionDamageBonusByStacks[stacks];
        }

        internal void RegisterKineticTremorsHit(Projectile projectile, NPC target, int damageDone)
        {
            if (projectile == null || target == null || !target.CanBeChasedBy())
                return;

            KineticTremorsGlobalNPC global = target.GetGlobalNPC<KineticTremorsGlobalNPC>();
            if (global.KineticTremorsCooldown > 0)
                return;

            int hitsRequired = GetKineticTremorsHitsRequired();
            if (hitsRequired <= 0)
                return;

            if (!kineticTremorsTargets.TryGetValue(target.whoAmI, out KineticTremorsTargetState state))
                state = default;

            if (state.CooldownTimer > 0)
                return;

            if (state.HitTimer <= 0)
                state.HitCount = 0;

            state.HitCount++;
            state.HitTimer = KineticTremorsPerk.HitWindowTicks;

            if (state.HitCount < hitsRequired)
            {
                kineticTremorsTargets[target.whoAmI] = state;
                return;
            }

            state.HitCount = 0;
            state.HitTimer = 0;

            int initialDelay = IsBowWeapon() ? KineticTremorsPerk.BowInitialDelayTicks : KineticTremorsPerk.InitialDelayTicks;
            int totalCooldown = initialDelay
                + (KineticTremorsPerk.PulseCount - 1) * KineticTremorsPerk.PulseIntervalTicks
                + KineticTremorsPerk.CooldownAfterLastPulseTicks;

            state.CooldownTimer = totalCooldown;
            kineticTremorsTargets[target.whoAmI] = state;

            if (global != null)
                global.KineticTremorsCooldown = totalCooldown;

            SpawnKineticTremorsShockwave(projectile, target, damageDone, initialDelay);
        }

        private int GetKineticTremorsHitsRequired()
        {
            if (this is AutoRifleWeaponItem)
                return KineticTremorsPerk.AutoRifleHitsRequired;
            if (this is HandCannonWeaponItem)
                return KineticTremorsPerk.HandCannonHitsRequired;
            if (this is CombatBowWeaponItem)
                return KineticTremorsPerk.BowHitsRequired;

            return KineticTremorsPerk.AutoRifleHitsRequired;
        }

        private bool IsBowWeapon()
        {
            return this is CombatBowWeaponItem;
        }

        private void SpawnKineticTremorsShockwave(Projectile projectile, NPC target, int damageDone, int initialDelay)
        {
            int shockwaveDamage = Math.Min(KineticTremorsPerk.MaxShockwaveDamage, damageDone);
            if (shockwaveDamage <= 0)
                return;

            Vector2 center = target.Center;
            int projId = Projectile.NewProjectile(projectile.GetSource_FromThis(), center, Vector2.Zero,
                ModContent.ProjectileType<KineticTremorsShockwave>(), shockwaveDamage, 0f, projectile.owner);
            if (projId < 0 || projId >= Main.maxProjectiles)
                return;

            Projectile shockwave = Main.projectile[projId];
            shockwave.ai[0] = initialDelay;
            shockwave.ai[1] = KineticTremorsPerk.PulseCount;
            shockwave.localAI[0] = KineticTremorsPerk.PulseIntervalTicks;
            shockwave.DamageType = WeaponElement.GetDamageClass();
            shockwave.direction = projectile.direction != 0 ? projectile.direction : 1;
            shockwave.timeLeft = Math.Max(shockwave.timeLeft, initialDelay + (KineticTremorsPerk.PulseCount - 1) * KineticTremorsPerk.PulseIntervalTicks + 30);
            shockwave.netUpdate = true;
        }

        private void UpdateKineticTremorsTargets()
        {
            if (kineticTremorsTargets.Count == 0)
                return;

            kineticTremorsTargetKeys.Clear();
            foreach (int key in kineticTremorsTargets.Keys)
                kineticTremorsTargetKeys.Add(key);

            for (int i = 0; i < kineticTremorsTargetKeys.Count; i++)
            {
                int npcId = kineticTremorsTargetKeys[i];
                if (npcId < 0 || npcId >= Main.maxNPCs || !Main.npc[npcId].active)
                {
                    kineticTremorsTargets.Remove(npcId);
                    continue;
                }

                KineticTremorsTargetState state = kineticTremorsTargets[npcId];
                if (state.HitTimer > 0)
                {
                    state.HitTimer--;
                    if (state.HitTimer <= 0)
                        state.HitCount = 0;
                }

                if (state.CooldownTimer > 0)
                    state.CooldownTimer--;

                if (state.HitTimer <= 0 && state.CooldownTimer <= 0 && state.HitCount <= 0)
                    kineticTremorsTargets.Remove(npcId);
                else
                    kineticTremorsTargets[npcId] = state;
            }
        }
        private void UpdateBlightLauncherMode(Player player)
        {
            if (player == null || player.HeldItem?.ModItem != this || !HasPerk<ChargedWithBlightPerk>())
            {
                reloadHoldTimer = 0;
                return;
            }

            if (blightStacks >= ChargedWithBlightPerk.MaxStacks && global::Destiny2.Destiny2.ReloadKeybind.Current)
            {
                reloadHoldTimer++;
                if (reloadHoldTimer >= 60) // 1 second hold
                {
                    reloadHoldTimer = 0;
                    if (!isBlightLauncherActive)
                    {
                        isBlightLauncherActive = true;
                        blightStacks = 0;

                        // Cancel reload
                        isReloading = false;
                        reloadTimer = 0;
                        reloadTimerMax = 0;

                        // Heal player
                        int heal = ChargedWithBlightPerk.HealAmount;
                        player.statLife = Math.Min(player.statLife + heal, player.statLifeMax2);
                        player.HealEffect(heal);

                        SoundEngine.PlaySound(SoundID.Item4, player.Center); // Sound for activation
                        SendPerkDebug(player, "Blight Launcher ACTIVE!");
                    }
                }
            }
            else
            {
                reloadHoldTimer = 0;
            }
        }

        private void RegisterCorruptionHit(Player player, NPC target)
        {
            ulong now = Main.GameUpdateCount;
            // < 1 second gap (60 ticks)
            if (now - lastCorruptionHitTick > 60)
            {
                corruptionHitCount = 0;
            }

            corruptionHitCount++;
            lastCorruptionHitTick = now;

            if (corruptionHitCount >= 12)
            {
                corruptionHitCount = 0;
                SpawnNaniteSwarm(player, target);
            }
        }

        private void SpawnNaniteSwarm(Player player, NPC target)
        {
            int count = Main.rand.Next(2, 4); // 2-3 nanites as requested
            for (int i = 0; i < count; i++)
            {
                // Spawn Radius: 40f (Increased to ensure they spawn outside small hitboxes)
                Vector2 spawnPos = target.Center + Main.rand.NextVector2Circular(40f, 40f);
                Vector2 vel = Main.rand.NextVector2Circular(8f, 8f); // Increased spread for "Cloud" effect
                Projectile.NewProjectile(player.GetSource_ItemUse(Item), spawnPos, vel, ModContent.ProjectileType<NaniteProjectile>(), 60, 0f, player.whoAmI);
            }
            SoundEngine.PlaySound(SoundID.Item96, target.Center); // Glitchy sound
        }

        internal void RegisterFocusedFuryHit(Player player, bool isPrecision)
        {
            if (!HasPerk<FocusedFuryPerk>())
                return;

            if (isPrecision)
            {
                focusedFuryHitCount++;
                int threshold = Math.Max(1, (int)(BaseStats.Magazine * 0.5f));
                if (focusedFuryHitCount >= threshold)
                {
                    focusedFuryTimer = FocusedFuryPerk.DurationTicks;
                    focusedFuryHitCount = 0;
                    SendPerkDebug(player, "Focused Fury Activated");
                }
            }
        }

        internal void AddKillingTallyStack(Player player)
        {
            if (!HasPerk<KillingTallyPerk>())
                return;

            killingTallyStacks = Math.Min(KillingTallyPerk.MaxStacks, killingTallyStacks + 1);
            SendPerkDebug(player, $"Killing Tally x{killingTallyStacks}");
        }

        internal void RegisterOneForAllHit(Player player, int targetId)
        {
            if (!HasPerk<OneForAllPerk>() || oneForAllBuffTimer > 0)
                return;

            if (oneForAllHitIds.Add(targetId))
            {
                if (oneForAllHitIds.Count == 1)
                    oneForAllWindowTimer = OneForAllPerk.WindowTicks;

                if (oneForAllHitIds.Count >= OneForAllPerk.TargetsRequired)
                {
                    oneForAllBuffTimer = OneForAllPerk.DurationTicks;
                    oneForAllHitIds.Clear();
                    oneForAllWindowTimer = 0;
                    SendPerkDebug(player, "One For All Activated");
                }
            }
        }

        private void ResetFocusedFuryCounter()
        {
            focusedFuryHitCount = 0;
        }

        private void ResetKillingTally()
        {
            killingTallyStacks = 0;
        }

        private void StartDesperadoPrecisionKillWindow(Player player)
        {
            if (!HasPerk<DesperadoPerk>())
                return;

            desperadoPrecisionKillTimer = DesperadoPerk.PrecisionKillWindowTicks;
            SendPerkDebug(player, "Desperado: Precision Kill! Finish reload within 5.2s for buff");
        }

        private void ActivateDesperado(Player player)
        {
            if (!HasPerk<DesperadoPerk>() || desperadoPrecisionKillTimer <= 0)
                return;

            desperadoBuffTimer = DesperadoPerk.DurationTicks;
            desperadoPrecisionKillTimer = 0;
            SendPerkDebug(player, "Desperado: Buff Activated! -30% Firing Delay for 6s");
        }

    }
}
