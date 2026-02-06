using Destiny2.Common.Perks;
using Destiny2.Common.Weapons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Destiny2.Content.Projectiles;

using System.Collections.Generic;

namespace Destiny2.Common.Weapons
{
    public abstract class CombatBowWeaponItem : Destiny2WeaponItem
    {

        private int currentDrawTicks;
        private int heldTicks;
        private bool wasChanneling;
        private int maxDrawTicks;

        // Public State for HUD
        public float DrawRatio { get; private set; }
        public bool IsPerfectDraw { get; private set; }
        public bool IsOverdrawn { get; private set; }

        public int CurrentDrawTicks => currentDrawTicks;
        public int MaxDrawTicks => maxDrawTicks;

        // Stat-driven Windows
        public int PerfectDrawWindowTicks { get; private set; }
        public int MaxOverdrawTicks { get; private set; }

        // Drawing properties for PlayerDrawLayer
        public Vector2 VisualNockPos { get; private set; }
        public Vector2 VisualHeadPos { get; private set; }
        public Vector2 VisualAimDir { get; private set; }
        public bool VisualIsDrawing => wasChanneling;

        public override void SetDefaults()
        {
            Item.useTime = 10;
            Item.useAnimation = 10;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 4f;
            Item.value = 10000;
            Item.rare = ItemRarityID.Blue;
            Item.autoReuse = false;
            Item.shoot = ModContent.ProjectileType<CombatBowProjectile>();
            Item.useAmmo = AmmoID.None;
            Item.channel = true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            return false;
        }

        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
        }

        internal static int GetDustForElement(Destiny2WeaponElement element)
        {
            return DustID.WhiteTorch;
        }

        internal static Color GetColorForElement(Destiny2WeaponElement element)
        {
            return element switch
            {
                Destiny2WeaponElement.Solar => Color.Orange,
                Destiny2WeaponElement.Void => new Color(160, 32, 240),
                Destiny2WeaponElement.Arc => new Color(0, 255, 255),
                Destiny2WeaponElement.Stasis => new Color(64, 64, 255),
                Destiny2WeaponElement.Strand => new Color(0, 255, 64),
                _ => Color.White
            };
        }

        public override void HoldItem(Player player)
        {
            if (player.whoAmI != Main.myPlayer) return;

            UpdateWeaponState(player); // CRITICAL: Updates reload timer!

            Destiny2WeaponStats stats = GetStats();

            // --- STAT SCALING ---
            // Stability increases Perfect Draw Window (10 to 60 ticks)
            PerfectDrawWindowTicks = (int)MathHelper.Lerp(10, 30, stats.Stability / 100f);
            // Stability increases Max Overdraw hold time (120 to 600 ticks)
            MaxOverdrawTicks = (int)MathHelper.Lerp(120, 28, stats.Stability / 100f);

            // --- RELOAD / NOCKING LOGIC ---
            if (IsReloading)
            {
                float t = 1f - ((float)ReloadTimer / ReloadTimerMax);

                float baseFrontRot = (Main.MouseWorld - player.Center).ToRotation() - MathHelper.PiOver2;
                float reachRot = baseFrontRot + MathHelper.PiOver2 * 1.2f * player.direction;
                float frontArmRot = baseFrontRot;

                if (t > 0.5f)
                {
                    float subT = (t - 0.5f) * 2f;
                    frontArmRot = MathHelper.Lerp(reachRot, baseFrontRot, subT);
                }
                else
                {
                    float subT = t * 2f;
                    frontArmRot = MathHelper.Lerp(baseFrontRot, reachRot, subT);
                }

                player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.ThreeQuarters, frontArmRot);
                player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, baseFrontRot);

                currentDrawTicks = 0;
                DrawRatio = 0f;
                heldTicks = 0;
                wasChanneling = false;
                return;
            }

            if (CurrentMagazine <= 0)
            {
                TryStartReload(player);
                return;
            }

            // --- DRAWING / FIRING LOGIC ---

            Vector2 aimDir = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX);

            if (player.channel)
            {
                UpdateWeaponState(player); // CRITICAL: Updates reload timer!
                // Arm Animation
                Player.CompositeArmStretchAmount stretch = Player.CompositeArmStretchAmount.Full;
                if (DrawRatio > 0.4f) stretch = Player.CompositeArmStretchAmount.ThreeQuarters;
                if (DrawRatio > 0.8f) stretch = Player.CompositeArmStretchAmount.None;

                player.SetCompositeArmFront(true, stretch, aimDir.ToRotation() - MathHelper.PiOver2);
                player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, aimDir.ToRotation() - MathHelper.PiOver2);

                maxDrawTicks = (int)Math.Ceiling(stats.ChargeTime / 1000f * 60f);

                DrawRatio = Math.Clamp((float)currentDrawTicks / maxDrawTicks, 0f, 1f);
                IsPerfectDraw = currentDrawTicks >= maxDrawTicks && heldTicks <= PerfectDrawWindowTicks;
                IsOverdrawn = heldTicks > PerfectDrawWindowTicks;

                if (aimDir.X != 0 && player.direction != Math.Sign(aimDir.X))
                {
                    player.direction = Math.Sign(aimDir.X);
                }

                player.itemRotation = aimDir.ToRotation();
                player.itemRotation -= player.direction > 0 ? 0 : MathHelper.Pi;

                player.itemTime = 2;
                player.itemAnimation = 2;

                if (currentDrawTicks < maxDrawTicks)
                {
                    currentDrawTicks++;
                    if (currentDrawTicks == maxDrawTicks)
                    {
                        SoundEngine.PlaySound(SoundID.Item5, player.Center);
                    }
                }
                else
                {
                    heldTicks++;
                    // AUTO-RESET / FORCE FIRE if held too long
                    if (heldTicks > MaxOverdrawTicks)
                    {
                        FireBow(player, stats, DrawRatio);
                        currentDrawTicks = 0;
                        heldTicks = 0;
                        wasChanneling = false;
                        return;
                    }
                }

                // --- ARROW RENDERING ---
                if (currentDrawTicks > 2)
                {
                    // handPos at shoulder/arm height
                    Vector2 handPos = player.MountedCenter + new Vector2(0f, -4f);

                    // Refined offsets for hand alignment
                    float bowOffset = 22f;
                    if (stretch == Player.CompositeArmStretchAmount.ThreeQuarters) bowOffset = 16f;
                    if (stretch == Player.CompositeArmStretchAmount.None) bowOffset = 8f;

                    Vector2 bowStavePos = handPos + aimDir * bowOffset;

                    // Sync the item sprite position with the hand
                    player.itemLocation = bowStavePos;

                    // nockPos pulls BACKWARDS towards the face (Max pull 16f)
                    Vector2 nockPos = bowStavePos - aimDir * (DrawRatio * 16f);

                    // FIXED LENGTH ARROW: Arrow head moves back in sync with the nock/string
                    float arrowLength = 28f;
                    Vector2 headPos = nockPos + aimDir * arrowLength;

                    // Store for PlayerDrawLayer
                    VisualNockPos = nockPos;
                    VisualHeadPos = headPos;
                    VisualAimDir = aimDir;
                }

                wasChanneling = true;
            }
            else
            {
                if (wasChanneling)
                {
                    if (DrawRatio >= 0.2f)
                    {
                        FireBow(player, stats, DrawRatio);
                    }

                    currentDrawTicks = 0;
                    DrawRatio = 0f;
                    heldTicks = 0;
                    wasChanneling = false;
                }
                else
                {
                    DrawRatio = 0f;
                    heldTicks = 0;
                }
            }
        }

        private void SpawnDustLine(Vector2 start, Vector2 end, int dustId, Color baseColor)
        {
            Vector2 dir = end - start;
            float length = dir.Length();
            int count = (int)(length / 2f);

            if (count > 0) dir.Normalize();

            Vector3 hsl = Main.rgbToHsl(baseColor);

            for (int i = 0; i < count; i++)
            {
                Vector2 pos = start + dir * (i * 2f);

                float hueShift = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5f + i * 0.2f) * 0.05f;
                float newHue = (hsl.X + hueShift) % 1f;
                if (newHue < 0) newHue += 1f;

                Color finalColor = Main.hslToRgb(newHue, hsl.Y, 0.6f);

                Dust d = Dust.NewDustPerfect(pos, dustId, Vector2.Zero, 0, finalColor, 1f);
                d.noGravity = true;
                d.velocity = Vector2.Zero;
                d.noLight = true;
                d.scale = 0.8f;
            }
        }

        private void FireBow(Player player, Destiny2WeaponStats stats, float drawRatio)
        {
            float velocityScale = MathHelper.Lerp(0.5f, 1f, drawRatio);
            float damageScale = MathHelper.Lerp(0.3f, 1f, drawRatio);

            // RANGE / FALLOFF logic
            // Base Range is from item stats
            float rangeStart = GetFalloffTiles();
            float rangeEnd = GetMaxFalloffTiles();

            // Draw Ratio Scalar: Underdrawn arrows lose half their effective range
            float falloffScalar = drawRatio < 1.0f ? 0.5f : 1.0f;

            if (IsPerfectDraw)
            {
                damageScale *= 1.05f;
            }
            else if (IsOverdrawn)
            {
                damageScale *= 0.7f;
                velocityScale *= 0.8f;
                falloffScalar *= 0.8f; // Overdraw penalty
            }

            Vector2 aimDir = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX);
            Vector2 velocity = aimDir * Item.shootSpeed * velocityScale;

            int baseDamage = player.GetWeaponDamage(Item);
            int damage = (int)(baseDamage * damageScale);
            float knockback = Item.knockBack * drawRatio;

            var source = new EntitySource_ItemUse_WithAmmo(player, Item, Item.useAmmo);
            int type = Item.shoot;

            // Sync spawn position with visual bow stave
            float finalBowOffset = 22f;
            if (drawRatio > 0.4f) finalBowOffset = 16f;
            if (drawRatio > 0.8f) finalBowOffset = 8f;
            Vector2 spawnPos = player.MountedCenter + new Vector2(0f, -4f) + aimDir * finalBowOffset;

            // ai[0] = Element
            // ai[1] = DrawRatio
            // ai[2] = Falloff Start (scaled) [Packed as bits or just used as AI?] 
            // We'll use 4/5/6 for falloff

            int p = Projectile.NewProjectile(source, spawnPos, velocity, type, damage, knockback, player.whoAmI, (int)WeaponElement, drawRatio);
            if (p >= 0 && p < Main.maxProjectiles)
            {
                // Passing Falloff Data via custom Projectile properties
                if (Main.projectile[p].ModProjectile is CombatBowProjectile bowProj)
                {
                    bowProj.FalloffStart = rangeStart * 16f * falloffScalar; // Convert tiles to pixels
                    bowProj.FalloffEnd = rangeEnd * 16f * falloffScalar;
                }
                Main.projectile[p].netUpdate = true;
            }

            currentMagazine--;
        }

        protected override float GetRecoilStrength() => 0f;
        public override float GetFalloffTiles() => 60f;
        public override float GetMaxFalloffTiles() => 80f;

        public override float GetPrecisionMultiplier()
        {
            if (HasFrame && FramePerkKey == nameof(LightweightBowFramePerk))
                return 1.6f;
            if (HasFrame && FramePerkKey == nameof(PrecisionBowFramePerk))
                return 1.45f;

            return 1.5f; // Default for Precision Bows or others
        }

        public override float GetReloadSeconds()
        {
            return CalculateScaledValue(GetStats().ReloadSpeed, 0.8f, 0.5f, 0.6f);
        }

        protected override int GetFrameChargeTime(Destiny2Perk framePerk, int currentChargeTime)
        {
            int baseTime = currentChargeTime;
            if (framePerk is LightweightBowFramePerk) baseTime = 567;
            if (framePerk is PrecisionBowFramePerk) baseTime = 667;

            // Adagio: 30% increase to draw time
            //if (HasPerk<AdagioPerk>() && IsAdagioActive)
            //{
            //    baseTime = (int)(baseTime * 1.3f);
            //}

            return baseTime;
        }
    }
}
