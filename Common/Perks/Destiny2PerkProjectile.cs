using System;
using System.Collections.Generic;
using Destiny2.Common.NPCs;
using Destiny2.Common.Players;
using Destiny2.Common.Weapons;
using Destiny2.Common.VFX;
using Destiny2.Content.Projectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Destiny2.Common.Perks
{
    public sealed class Destiny2PerkProjectile : GlobalProjectile
    {
        private const float RightChoiceRicochetRange = 480f;
        private static readonly float EyesUpGuardianRicochetRange = EyesUpGuardianPerk.RicochetRangeTiles * 16f;
        private static int nextEyesUpChainId;
        private static readonly Dictionary<int, Dictionary<int, int>> EyesUpChainHits = new Dictionary<int, Dictionary<int, int>>();
        private readonly List<Destiny2Perk> perks = new List<Destiny2Perk>();
        private bool hasVorpal;
        private bool hasOutlaw;
        private bool hasRapidHit;
        private bool hasKillClip;
        private bool hasFrenzy;
        private bool hasFourthTimes;
        private bool hasRampage;
        private bool hasOnslaught;
        private bool hasKineticTremors;
        private bool hasAdagio;
        private bool hasTargetLock;
        private bool targetLockShotRegistered;
        private bool targetLockShotHit;
        private bool hasIncandescent;
        private int lastPreHitLife;
        private int lastHitTargetId = -1;
        private bool hasPreHitLife;
        private bool hasFeedingFrenzy;
        private bool hasRightChoice;
        private bool isRightChoiceShot;
        private bool hasEyesUpGuardian;
        private bool isEyesUpGuardianRicochet;
        private bool isEyesUpGuardianShot;
        private int eyesUpRicochetRemaining;
        private int eyesUpChainId;
        private Destiny2WeaponElement eyesUpElement = Destiny2WeaponElement.Kinetic;
        private bool hasArmorPiercingRounds;
        private Destiny2WeaponItem sourceWeaponItem;
        private Destiny2AmmoType ammoType = Destiny2AmmoType.Primary;
        private Destiny2WeaponElement rightChoiceElement = Destiny2WeaponElement.Kinetic;
        private bool hasTouchOfMalice;
        private bool hasChargedWithBlight;
        private bool hasRicochetRounds;
        private bool ricocheted;
        private bool hasParasitism;
        private bool isPrecisionHit; // Track for OnHitNPC
        public bool CanCrit = true;
        public Microsoft.Xna.Framework.Graphics.Effect CustomTrailShader { get; set; }
        public string CustomTrailTechnique { get; set; }

        public override bool InstancePerEntity => true;
        internal bool HasIncandescentPerk => hasIncandescent;

        private static bool DiagnosticsEnabled => global::Destiny2.Destiny2.DiagnosticsEnabled;

        private static bool IsTrackedProjectile(Projectile projectile)
        {
            if (projectile?.ModProjectile?.Mod?.Name != "Destiny2")
                return false;

            int bulletType = ModContent.ProjectileType<Bullet>();
            int slugType = ModContent.ProjectileType<ExplosiveShadowSlug>();
            int blightType = ModContent.ProjectileType<ChargedWithBlightProjectile>();
            return projectile.type == bulletType || projectile.type == slugType || projectile.type == blightType;
        }

        private static void LogDiagnostic(string message)
        {
            if (!DiagnosticsEnabled)
                return;

            global::Destiny2.Destiny2.LogDiagnostic(message);
        }

        /// <summary>
        /// Always logs regardless of DiagnosticsEnabled - use sparingly for critical debug info.
        /// </summary>
        private static void LogAlways(string message)
        {
            global::Destiny2.Destiny2.LogDiagnostic(message);
        }

        /// <summary>
        /// Shows in-game text feedback for perk activation.
        /// </summary>
        private static void ShowPerkFeedback(Player player, string message, Color color)
        {
            if (!DiagnosticsEnabled)
                return;

            if (Main.netMode == NetmodeID.Server)
                return;

            if (player == null || player.whoAmI != Main.myPlayer)
                return;

            Main.NewText(message, color.R, color.G, color.B);
        }

        private static bool IsChildProjectile(IEntitySource source)
        {
            return source is EntitySource_Parent parent && parent.Entity is Projectile;
        }

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            perks.Clear();
            hasVorpal = false;
            hasOutlaw = false;
            hasRapidHit = false;
            hasKillClip = false;
            hasFrenzy = false;
            hasFourthTimes = false;
            hasRampage = false;
            hasOnslaught = false;
            hasKineticTremors = false;
            hasAdagio = false;
            hasTargetLock = false;
            targetLockShotRegistered = false;
            targetLockShotHit = false;
            hasIncandescent = false;
            lastPreHitLife = 0;
            lastHitTargetId = -1;
            hasPreHitLife = false;
            hasFeedingFrenzy = false;
            hasRightChoice = false;
            isRightChoiceShot = false;
            hasEyesUpGuardian = false;
            isEyesUpGuardianRicochet = false;
            isEyesUpGuardianShot = false;
            eyesUpRicochetRemaining = 0;
            eyesUpChainId = 0;
            sourceWeaponItem = null;
            ammoType = Destiny2AmmoType.Primary;
            rightChoiceElement = Destiny2WeaponElement.Kinetic;
            hasArmorPiercingRounds = false;
            hasTouchOfMalice = false;
            hasChargedWithBlight = false;
            hasRicochetRounds = false;
            ricocheted = false;
            hasParasitism = false;
            CanCrit = true;

            // Non-Crit Projectiles (Nanites, Blight, etc)
            if (projectile.type == ModContent.ProjectileType<NaniteProjectile>() ||
                projectile.type == ModContent.ProjectileType<ChargedWithBlightProjectile>())
            {
                CanCrit = false;
            }

            bool isChild = IsChildProjectile(source);
            Destiny2PerkProjectile parentData = null;
            if (isChild && source is EntitySource_Parent parent && parent.Entity is Projectile parentProjectile)
                parentData = parentProjectile.GetGlobalProjectile<Destiny2PerkProjectile>();

            Destiny2WeaponItem weaponItem = GetSourceWeaponItem(source);
            if (weaponItem == null && IsTrackedProjectile(projectile))
            {
                Player owner = GetOwner(projectile.owner);
                if (owner?.HeldItem?.ModItem is Destiny2WeaponItem heldWeapon)
                {
                    weaponItem = heldWeapon;
                    LogDiagnostic($"PerkProj.OnSpawn fallback weapon from held item: {heldWeapon.Item?.Name ?? heldWeapon.GetType().Name}");
                }
            }
            if (weaponItem != null)
            {
                sourceWeaponItem = weaponItem;
                ammoType = weaponItem.AmmoType;
                foreach (Destiny2Perk perk in weaponItem.GetPerks())
                {
                    perks.Add(perk);
                    if (perk is VorpalWeaponPerk)
                        hasVorpal = true;
                    else if (perk is OutlawPerk)
                        hasOutlaw = true;
                    else if (perk is RapidHitPerk)
                        hasRapidHit = true;
                    else if (perk is KillClipPerk)
                        hasKillClip = true;
                    else if (perk is FrenzyPerk)
                        hasFrenzy = true;
                    else if (perk is FourthTimesTheCharmPerk)
                        hasFourthTimes = true;
                    else if (perk is RampagePerk)
                        hasRampage = true;
                    else if (perk is OnslaughtPerk)
                        hasOnslaught = true;
                    else if (perk is KineticTremorsPerk)
                        hasKineticTremors = true;
                    else if (perk is AdagioPerk)
                        hasAdagio = true;
                    else if (perk is TargetLockPerk)
                        hasTargetLock = true;
                    else if (perk is FeedingFrenzyPerk)
                        hasFeedingFrenzy = true;
                    else if (perk is IncandescentPerk)
                        hasIncandescent = true;
                    else if (perk is TheRightChoiceFramePerk)
                        hasRightChoice = true;
                    else if (perk is EyesUpGuardianPerk)
                        hasEyesUpGuardian = true;
                    else if (perk is ArmorPiercingRoundsPerk)
                        hasArmorPiercingRounds = true;
                    else if (perk is TouchOfMalicePerk)
                        hasTouchOfMalice = true;
                    else if (perk is ChargedWithBlightPerk)
                        hasChargedWithBlight = true;
                    else if (perk is RicochetRoundsPerk)
                        hasRicochetRounds = true;
                    else if (perk is ParasitismPerk)
                    {
                        hasParasitism = true;
                        CustomTrailShader = Destiny2Shaders.SolarTrail; // Use Uber Shader
                        CustomTrailTechnique = "Corruption";
                    }
                }

                // THE RIGHT CHOICE: Only count player-fired shots toward the 7-shot cycle
                // Child projectiles (ricochets) don't increment the counter
                if (hasRightChoice && !isChild)
                {
                    bool isAutoRifle = weaponItem is AutoRifleWeaponItem;
                    LogDiagnostic($"RightChoice check: hasRightChoice={hasRightChoice} isAutoRifle={isAutoRifle} isChild={isChild}");

                    if (isAutoRifle)
                    {
                        bool consumed = weaponItem.TryConsumeRightChoiceShot();
                        if (consumed)
                        {
                            isRightChoiceShot = true;
                            rightChoiceElement = weaponItem.WeaponElement;
                            Player owner = GetOwner(projectile.owner);
                            ShowPerkFeedback(owner, "The Right Choice - Ricochet!", new Color(255, 200, 100));
                            LogDiagnostic($"RightChoice ARMED! projId={projectile.identity}");
                        }
                    }
                }

                if (hasEyesUpGuardian)
                    eyesUpElement = weaponItem.WeaponElement;
            }

            // EYES UP GUARDIAN: inherit chain state for ricochet projectiles spawned from an existing chain
            if (parentData != null && (parentData.isEyesUpGuardianRicochet || parentData.eyesUpChainId != 0))
            {
                hasEyesUpGuardian = true;
                isEyesUpGuardianRicochet = true;
                eyesUpChainId = parentData.eyesUpChainId;
                eyesUpRicochetRemaining = parentData.eyesUpRicochetRemaining;
                eyesUpElement = parentData.eyesUpElement;
                LogDiagnostic($"EyesUp chain inherited. projId={projectile.identity} chainId={eyesUpChainId} remaining={eyesUpRicochetRemaining}");
            }

            // EYES UP GUARDIAN: Consume one stack per player-fired shot to arm the chain for that shot.
            if (hasEyesUpGuardian && !isChild)
            {
                Player owner = GetOwner(projectile.owner);
                Destiny2Player modPlayer = owner?.GetModPlayer<Destiny2Player>();

                if (modPlayer != null && modPlayer.TryConsumeEyesUpGuardianStack())
                {
                    isEyesUpGuardianShot = true;
                    int remaining = modPlayer.GetEyesUpGuardianStacks();
                    ShowPerkFeedback(owner, $"Eyes Up, Guardian! ({remaining} remaining)", new Color(100, 200, 255));
                    LogDiagnostic($"EyesUp shot armed. projId={projectile.identity} remaining={remaining}");
                }
                else if (IsTrackedProjectile(projectile))
                {
                    int stacks = modPlayer?.GetEyesUpGuardianStacks() ?? -1;
                    LogDiagnostic($"EyesUp shot not armed. projId={projectile.identity} stacks={stacks} isRightChoiceShot={isRightChoiceShot}");
                }
            }

            // Armor-Piercing Rounds: Allow piercing through one enemy
            if (hasArmorPiercingRounds)
            {
                projectile.penetrate = 2;
                LogDiagnostic($"Armor-Piercing Rounds applied. projId={projectile.identity} penetrate={projectile.penetrate}");
            }

            if (hasTargetLock && !isChild && sourceWeaponItem != null)
            {
                targetLockShotRegistered = true;
                targetLockShotHit = false;
            }

            if (DiagnosticsEnabled && IsTrackedProjectile(projectile))
            {
                string sourceName = source?.GetType().Name ?? "null";
                string weaponName = weaponItem?.Item?.Name ?? weaponItem?.GetType().Name ?? "null";
                LogDiagnostic($"PerkProj.OnSpawn projId={projectile.identity} type={projectile.type} source={sourceName} weapon={weaponName} " +
                    $"hasRightChoice={hasRightChoice} isRightChoiceShot={isRightChoiceShot} hasEyesUp={hasEyesUpGuardian} " +
                    $"isEyesUpShot={isEyesUpGuardianShot} isEyesUpRicochet={isEyesUpGuardianRicochet} chainId={eyesUpChainId} remaining={eyesUpRicochetRemaining}");
            }
        }

        public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
        {
            isPrecisionHit = false;
            // Disable vanilla crits for all projectiles related to this mod
            if (projectile.ModProjectile?.Mod == Mod || sourceWeaponItem != null)
            {
                modifiers.DisableCrit();
            }

            if (target != null)
            {
                lastPreHitLife = target.life;
                lastHitTargetId = target.whoAmI;
                hasPreHitLife = true;
            }

            float multiplier = 1f;
            if (hasVorpal && IsBossTarget(target))
                multiplier *= GetVorpalMultiplier(ammoType);

            if (sourceWeaponItem != null)
            {
                float precisionMultiplier = sourceWeaponItem.GetPrecisionMultiplier();

                // Precision Logic
                if (sourceWeaponItem.CanCrit && CanCrit)
                {
                    Destiny2CritSpotGlobalNPC critSpot = target.GetGlobalNPC<Destiny2CritSpotGlobalNPC>();
                    // Check Precision status INDEPENDENT of multiplier
                    if (critSpot != null && critSpot.IsPrecisionShot(target, projectile.Center - projectile.velocity, projectile.Center))
                    {
                        critSpot.RegisterPrecisionHit(target);
                        isPrecisionHit = true;

                        // Apply damage multiplier if applicable
                        if (precisionMultiplier > 1f)
                        {
                            multiplier *= precisionMultiplier;
                            modifiers.SetCrit(); // Only set "Crit" text if we actually did crit damage
                        }
                    }
                }

                if (hasKillClip && sourceWeaponItem.IsKillClipActive)
                    multiplier *= KillClipPerk.DamageMultiplier;

                if (hasFrenzy && sourceWeaponItem.IsFrenzyActive)
                    multiplier *= FrenzyPerk.DamageMultiplier;

                if (hasRampage)
                {
                    float rampageMultiplier = sourceWeaponItem.GetRampageMultiplier();
                    if (rampageMultiplier > 1f)
                        multiplier *= rampageMultiplier;
                }

                if (hasAdagio && sourceWeaponItem.IsAdagioActive)
                    multiplier *= AdagioPerk.DamageMultiplier;

                if (hasTargetLock)
                {
                    float bonus = sourceWeaponItem.RegisterTargetLockHit(target);
                    if (bonus > 0f)
                        multiplier *= 1f + bonus;
                }

                if (hasTouchOfMalice && sourceWeaponItem.CurrentMagazine == 1)
                    multiplier *= TouchOfMalicePerk.DamageMultiplier;

                if (hasParasitism && projectile.type != ModContent.ProjectileType<NaniteProjectile>())
                {
                    NaniteGlobalNPC naniteGlobal = target.GetGlobalNPC<NaniteGlobalNPC>();
                    if (naniteGlobal != null && naniteGlobal.NaniteStacks > 0)
                    {
                        float bonus = 0f;
                        int stacks = naniteGlobal.NaniteStacks;

                        // Scaling Logic
                        if (stacks <= 5)
                            bonus = 0.30f + (0.25f * (stacks - 1));
                        else
                        {
                            float base5 = 1.30f;
                            int extraStacks = stacks - 5;
                            bonus = base5 + (0.021f * extraStacks);
                        }

                        if (bonus > 3.5f) bonus = 3.5f;

                        if (DiagnosticsEnabled)
                            LogDiagnostic($"[Parasitism] Bonus: {bonus:P0} Stacks: {stacks}");

                        multiplier *= (1f + bonus);
                    }
                }
            }

            if (multiplier > 1f)
                modifiers.FinalDamage *= multiplier;
        }

        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (isPrecisionHit)
            {
                // Find the CombatText that was just spawned and color it Yellow
                // CombatText is usually the last one in the list if spawned this frame.
                // We scan backwards for a text matching the damage amount near the target.
                // Find the CombatText that was just spawned and color it Yellow
                string damageStr = damageDone.ToString();
                float bestLife = -1f;
                int bestIdx = -1;

                for (int i = 0; i < 100; i++)
                {
                    CombatText text = Main.combatText[i];
                    // Match if it's recently spawned (lifeTime near max) and contains our damage
                    if (text != null && text.active && text.lifeTime > bestLife && text.text.Contains(damageStr))
                    {
                        // Match if it's within a reasonable distance of the target's center
                        if (Vector2.DistanceSquared(text.position, target.Center) < 450 * 450)
                        {
                            bestLife = text.lifeTime;
                            bestIdx = i;
                        }
                    }
                }

                if (bestIdx != -1)
                {
                    Main.combatText[bestIdx].color = new Color(255, 235, 4); // Destiny Yellow
                }
            }

            // Standard Perk Processing
            if (DiagnosticsEnabled && isPrecisionHit)
                LogDiagnostic("OnHitNPC: Processing Precision Hit for perks.");

            ProcessHit(projectile, target, hit, damageDone);

            isPrecisionHit = false; // Reset AFTER processing perks
        }


        public override bool OnTileCollide(Projectile projectile, Vector2 oldVelocity)
        {
            if (hasRicochetRounds && !ricocheted)
            {
                ricocheted = true;

                // Handle visual trail segmentation for Bullets
                if (projectile.ModProjectile is Bullet bullet)
                {
                    // Spawn a trace for the segment that just hit the wall
                    BulletDrawSystem.SpawnTrace(bullet.spawnPosition, projectile.Center, (Destiny2WeaponElement)projectile.ai[0]);

                    // Update the bullet's spawn position to the bounce point
                    bullet.spawnPosition = projectile.Center;
                }

                // Reflect velocity
                if (Math.Abs(projectile.velocity.X - oldVelocity.X) > float.Epsilon)
                    projectile.velocity.X = -oldVelocity.X;
                if (Math.Abs(projectile.velocity.Y - oldVelocity.Y) > float.Epsilon)
                    projectile.velocity.Y = -oldVelocity.Y;

                // Impact visuals/sound
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item10, projectile.Center);
                for (int i = 0; i < 5; i++)
                {
                    Dust d = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, DustID.Smoke, 0f, 0f, 100, default, 0.8f);
                    d.velocity *= 0.5f;
                }

                return false; // Don't kill the projectile
            }

            return true;
        }


        private void ProcessHit(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            bool isKill = false;
            if (target != null)
            {
                if (hasPreHitLife && lastHitTargetId == target.whoAmI && lastPreHitLife > 0)
                    isKill = lastPreHitLife - damageDone <= 0;
                else
                    isKill = target.life <= 0;
            }
            hasPreHitLife = false;

            if (DiagnosticsEnabled && perks.Count > 0)
            {
                string targetName = target?.FullName ?? target?.TypeName ?? "null";
                LogDiagnostic($"ProcessHit projId={projectile.identity} target={targetName} damage={damageDone} " +
                    $"isKill={isKill} hasIncandescent={hasIncandescent}");
            }

            // Incandescent: kills trigger explosion (1/4 weapon damage, 4 tiles, 40 scorch stacks)
            if (hasIncandescent && isKill && target != null && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Main.NewText($"[Perk] Incandescent Triggered! Kill detected.", Color.Orange);
                // Use projectile.damage instead of damageDone to ensure consistent explosion damage
                int explosionDamage = Math.Max(1, (int)(projectile.damage * 0.25f));
                ScorchGlobalNPC.TriggerIncandescentExplosion(target.Center, explosionDamage, target.whoAmI);
            }

            if (perks.Count > 0)
            {
                for (int i = 0; i < perks.Count; i++)
                    perks[i].OnProjectileHitNPC(projectile, target, hit, damageDone);
            }

            if (hasTargetLock && targetLockShotRegistered)
                targetLockShotHit = true;

            // Handle ricochet logic - The Right Choice and Eyes Up Guardian can work independently or together
            // Priority: Eyes Up Guardian chain > The Right Choice basic ricochet
            bool handledRicochet = false;

            // Check if this is part of an existing Eyes Up Guardian chain
            if (isEyesUpGuardianRicochet || eyesUpChainId != 0)
            {
                if (DiagnosticsEnabled)
                    LogDiagnostic("ProcessHit: handling EyesUp chain ricochet");
                HandleEyesUpGuardianRicochet(projectile, target, damageDone);
                handledRicochet = true;
            }
            // Check if this shot should start an Eyes Up Guardian chain (enhanced ricochet)
            else if (isEyesUpGuardianShot)
            {
                if (DiagnosticsEnabled)
                    LogDiagnostic("ProcessHit: starting EyesUp chain");
                TryStartEyesUpGuardianChain(projectile, target, damageDone);
                handledRicochet = true;
            }
            // Basic The Right Choice ricochet (when not enhanced by Eyes Up Guardian)
            else if (isRightChoiceShot)
            {
                if (DiagnosticsEnabled)
                    LogDiagnostic($"ProcessHit: triggering RightChoice ricochet projId={projectile.identity}");
                TryRicochet(projectile, target, damageDone, rightChoiceElement);
                handledRicochet = true;
            }

            if (DiagnosticsEnabled && !handledRicochet && (hasRightChoice || hasEyesUpGuardian))
                LogDiagnostic($"ProcessHit: no ricochet triggered. isRightChoiceShot={isRightChoiceShot} isEyesUpShot={isEyesUpGuardianShot}");

            if (sourceWeaponItem == null)
                return;

            if (hasKineticTremors)
                sourceWeaponItem.RegisterKineticTremorsHit(projectile, target, projectile.damage);

            if (!hasOutlaw && !hasRapidHit && !hasKillClip && !hasFrenzy && !hasFourthTimes && !hasRampage
                && !hasOnslaught && !hasAdagio && !hasFeedingFrenzy && !hasChargedWithBlight)
                return;

            Player owner = GetOwner(projectile.owner);
            sourceWeaponItem.NotifyProjectileHit(owner, target, hit, damageDone, hasOutlaw, hasRapidHit, hasKillClip, hasFrenzy, hasFourthTimes, hasRampage,
                hasOnslaught, hasAdagio, hasFeedingFrenzy, isKill, projectile.type == ModContent.ProjectileType<ChargedWithBlightProjectile>(),
                projectile.type == ModContent.ProjectileType<NaniteProjectile>(), isPrecisionHit);
        }

        public override void OnKill(Projectile projectile, int timeLeft)
        {
            if (hasTargetLock && targetLockShotRegistered && !targetLockShotHit && sourceWeaponItem != null)
                sourceWeaponItem.NotifyTargetLockMiss();
        }

        private static Item GetSourceItem(IEntitySource source)
        {
            if (source is EntitySource_ItemUse itemUse)
                return itemUse.Item;
            if (source is EntitySource_ItemUse_WithAmmo itemUseWithAmmo)
                return itemUseWithAmmo.Item;

            return null;
        }

        private static Destiny2WeaponItem GetSourceWeaponItem(IEntitySource source)
        {
            Item sourceItem = GetSourceItem(source);
            if (sourceItem?.ModItem is Destiny2WeaponItem weaponItem)
                return weaponItem;

            if (source is EntitySource_Parent parent)
            {
                if (parent.Entity is Item parentItem && parentItem.ModItem is Destiny2WeaponItem parentWeapon)
                    return parentWeapon;

                if (parent.Entity is Projectile parentProjectile)
                {
                    Destiny2PerkProjectile data = parentProjectile.GetGlobalProjectile<Destiny2PerkProjectile>();
                    if (data?.sourceWeaponItem != null)
                        return data.sourceWeaponItem;
                }
            }

            return null;
        }

        private static bool IsBossTarget(NPC target)
        {
            return target.boss || NPCID.Sets.ShouldBeCountedAsBoss[target.type];
        }

        private static Player GetOwner(int owner)
        {
            if (owner < 0 || owner >= Main.maxPlayers)
                return null;

            Player player = Main.player[owner];
            return player.active ? player : null;
        }

        private static float GetVorpalMultiplier(Destiny2AmmoType ammoType)
        {
            return ammoType switch
            {
                Destiny2AmmoType.Primary => 1.15f,
                Destiny2AmmoType.Special => 1.10f,
                Destiny2AmmoType.Heavy => 1.05f,
                _ => 1f
            };
        }

        private static void TryRicochet(Projectile projectile, NPC target, int damageDone, Destiny2WeaponElement element)
        {
            LogDiagnostic($"TryRicochet ENTER. proj={projectile?.identity} target={target?.whoAmI}");

            if (projectile == null || target == null)
            {
                LogDiagnostic("TryRicochet aborted: null projectile or target");
                return;
            }

            LogDiagnostic($"TryRicochet searching for target. fromNpc={target.whoAmI} ({target.FullName}) range={RightChoiceRicochetRange} targetCenter={target.Center}");

            NPC ricochetTarget = FindRicochetTarget(target, target.Center, RightChoiceRicochetRange);
            if (ricochetTarget == null)
            {
                LogDiagnostic("TryRicochet: NO TARGET FOUND within range!");
                Player owner = GetOwner(projectile.owner);
                ShowPerkFeedback(owner, "The Right Choice - No target in range!", new Color(255, 150, 100));
                return;
            }


            LogDiagnostic($"TryRicochet: Found target! npc={ricochetTarget.whoAmI} ({ricochetTarget.FullName})");

            Vector2 direction = (ricochetTarget.Center - target.Center).SafeNormalize(Vector2.UnitX);
            float offsetDistance = Math.Max(target.width, target.height) * 0.5f + 6f;
            Vector2 spawnPos = target.Center + direction * offsetDistance;
            int ricochetDamage = Math.Max(1, (int)Math.Round(damageDone * TheRightChoiceFramePerk.RicochetDamageMultiplier));

            float aimRotation = direction.ToRotation();
            int projId = Projectile.NewProjectile(projectile.GetSource_FromThis(), spawnPos, direction, projectile.type, ricochetDamage, projectile.knockBack, projectile.owner, 0f, aimRotation);
            if (projId < 0 || projId >= Main.maxProjectiles)
            {
                LogDiagnostic($"TryRicochet: FAILED to spawn projectile! projId={projId}");
                return;
            }

            Projectile ricochet = Main.projectile[projId];
            ricochet.ai[0] = (int)element;
            ricochet.DamageType = element.GetDamageClass();
            ricochet.netUpdate = true;

            LogDiagnostic($"TryRicochet: SUCCESS! Ricochet spawned projId={projId} -> targetNpc={ricochetTarget.whoAmI} ({ricochetTarget.FullName}) damage={ricochetDamage}");
            Player owner2 = GetOwner(projectile.owner);
            ShowPerkFeedback(owner2, $"Ricochet -> {ricochetTarget.FullName}!", new Color(255, 200, 100));
        }

        private static NPC FindRicochetTarget(NPC current, Vector2 origin, float maxRange)
        {
            NPC best = null;
            float bestDistSq = maxRange * maxRange;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy())
                    continue;
                if (current != null && npc.whoAmI == current.whoAmI)
                    continue;

                float distSq = Vector2.DistanceSquared(origin, npc.Center);
                if (distSq >= bestDistSq)
                    continue;

                best = npc;
                bestDistSq = distSq;
            }

            return best;
        }

        private void TryStartEyesUpGuardianChain(Projectile projectile, NPC target, int damageDone)
        {
            if (sourceWeaponItem == null)
            {
                LogDiagnostic("EyesUp.TryStartChain aborted: sourceWeaponItem null.");
                return;
            }

            LogDiagnostic($"EyesUp.TryStartChain starting. projId={projectile.identity} target={target.whoAmI} ricochetCount={EyesUpGuardianPerk.RicochetCount}");

            eyesUpChainId = CreateEyesUpChain();
            RegisterEyesUpHit(eyesUpChainId, target.whoAmI);
            if (!SpawnEyesUpRicochet(projectile, target, damageDone, EyesUpGuardianPerk.RicochetCount))
            {
                RemoveEyesUpChain(eyesUpChainId);
                LogDiagnostic($"EyesUp chain failed to start (no valid target). chainId={eyesUpChainId}");
                Player owner = GetOwner(projectile.owner);
                ShowPerkFeedback(owner, "Eyes Up, Guardian - No target in range!", new Color(100, 150, 255));
            }
            else
            {
                LogDiagnostic($"EyesUp chain STARTED! chainId={eyesUpChainId} remaining={EyesUpGuardianPerk.RicochetCount}");
            }

            isEyesUpGuardianShot = false;
        }

        private void HandleEyesUpGuardianRicochet(Projectile projectile, NPC target, int damageDone)
        {
            if (eyesUpChainId == 0)
            {
                if (DiagnosticsEnabled && IsTrackedProjectile(projectile))
                    LogDiagnostic("EyesUp.HandleRicochet aborted: chainId=0.");
                return;
            }

            RegisterEyesUpHit(eyesUpChainId, target.whoAmI);
            if (eyesUpRicochetRemaining <= 1)
            {
                RemoveEyesUpChain(eyesUpChainId);
                if (DiagnosticsEnabled && IsTrackedProjectile(projectile))
                    LogDiagnostic($"EyesUp chain ended id={eyesUpChainId} remaining={eyesUpRicochetRemaining}");
                return;
            }

            if (!SpawnEyesUpRicochet(projectile, target, damageDone, eyesUpRicochetRemaining - 1))
                RemoveEyesUpChain(eyesUpChainId);
            else if (DiagnosticsEnabled && IsTrackedProjectile(projectile))
                LogDiagnostic($"EyesUp ricochet continue id={eyesUpChainId} remaining={eyesUpRicochetRemaining - 1}");
        }

        private bool SpawnEyesUpRicochet(Projectile projectile, NPC target, int damageDone, int remainingRicochets)
        {
            if (remainingRicochets <= 0)
                return false;

            NPC ricochetTarget = FindEyesUpTarget(target, target.Center, EyesUpGuardianRicochetRange, eyesUpChainId);
            if (ricochetTarget == null)
            {
                if (DiagnosticsEnabled && IsTrackedProjectile(projectile))
                    LogDiagnostic($"EyesUp ricochet target not found. chainId={eyesUpChainId}");
                return false;
            }

            Vector2 direction = (ricochetTarget.Center - target.Center).SafeNormalize(Vector2.UnitX);
            float offsetDistance = Math.Max(target.width, target.height) * 0.5f + 6f;
            Vector2 spawnPos = target.Center + direction * offsetDistance;

            float aimRotation = direction.ToRotation();
            int projId = Projectile.NewProjectile(projectile.GetSource_FromThis(), spawnPos, direction, projectile.type, damageDone, projectile.knockBack, projectile.owner, 0f, aimRotation);
            if (projId < 0 || projId >= Main.maxProjectiles)
                return false;

            Projectile ricochet = Main.projectile[projId];
            ricochet.ai[0] = (int)eyesUpElement;
            ricochet.DamageType = eyesUpElement.GetDamageClass();
            ricochet.netUpdate = true;

            Destiny2PerkProjectile data = ricochet.GetGlobalProjectile<Destiny2PerkProjectile>();
            data.isEyesUpGuardianRicochet = true;
            data.eyesUpRicochetRemaining = remainingRicochets;
            data.eyesUpChainId = eyesUpChainId;
            data.eyesUpElement = eyesUpElement;

            if (DiagnosticsEnabled && IsTrackedProjectile(projectile))
                LogDiagnostic($"EyesUp ricochet spawned projId={projId} targetNpc={ricochetTarget.whoAmI} remaining={remainingRicochets}");

            return true;
        }

        private static NPC FindEyesUpTarget(NPC current, Vector2 origin, float maxRange, int chainId)
        {
            NPC best = null;
            float bestDistSq = maxRange * maxRange;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy())
                    continue;
                if (current != null && npc.whoAmI == current.whoAmI)
                    continue;
                if (HasEyesUpReachedHitLimit(chainId, npc.whoAmI))
                    continue;

                float distSq = Vector2.DistanceSquared(origin, npc.Center);
                if (distSq >= bestDistSq)
                    continue;

                best = npc;
                bestDistSq = distSq;
            }

            return best;
        }

        private static int CreateEyesUpChain()
        {
            int id = ++nextEyesUpChainId;
            EyesUpChainHits[id] = new Dictionary<int, int>();
            return id;
        }

        private static void RegisterEyesUpHit(int chainId, int npcId)
        {
            if (chainId == 0 || npcId < 0)
                return;

            if (!EyesUpChainHits.TryGetValue(chainId, out Dictionary<int, int> hits))
                return;

            if (!hits.TryGetValue(npcId, out int count))
                count = 0;

            hits[npcId] = count + 1;
        }

        private static bool HasEyesUpReachedHitLimit(int chainId, int npcId)
        {
            if (chainId == 0 || npcId < 0)
                return false;

            if (!EyesUpChainHits.TryGetValue(chainId, out Dictionary<int, int> hits))
                return false;

            if (!hits.TryGetValue(npcId, out int count))
                return false;

            return count >= EyesUpGuardianPerk.MaxHitsPerTarget;
        }

        private static void RemoveEyesUpChain(int chainId)
        {
            if (chainId == 0)
                return;

            EyesUpChainHits.Remove(chainId);
        }
    }
}
