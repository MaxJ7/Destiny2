using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.GameContent;
using ReLogic.Content;
using Destiny2.Common.Weapons;

namespace Destiny2.Common.NPCs
{
    public enum CritArchetype
    {
        Standard,   // Head area (humanoids)
        Beast,      // Frontal head (wolves, birds, bees)
        Face,       // Centered face (Everscream, pumpking)
        Dragon,     // Elongated neck/head (Betsy, Fishron, Wyverns)
        Core,       // Center of mass (slimes)
        Eye,        // Surface tracking with rotation (EoH, Pupil-style)
        Segments,   // Entire hitbox (worms, eaters - reduced bonus)
        None        // No crit spot (entities like shadows/ghosts)
    }

    public sealed class Destiny2CritSpotGlobalNPC : GlobalNPC
    {
        private const float MinRadius = 6f;
        private const float MaxRadius = 32f;
        private const float RadiusScalar = 0.2f;
        private const float JitterXScalar = 0.12f;
        private const float JitterYScalar = 0.08f;
        private const int HitFlashTicks = 10;
        private const int DustSpawnInterval = 2;
        private const float DrawRange = 1400f;
        private const float MagnetismStandardRadius = 18f;
        private const float MagnetismGrowthScalar = 0.015f; // Slight growth over distance

        private static Asset<Texture2D> indicatorTexture;

        private Vector2 critOffset;
        private float critRadius;
        private CritArchetype archetype;
        private bool initialized;
        private int hitFlashTimer;

        public override bool InstancePerEntity => true;

        public override void OnSpawn(NPC npc, IEntitySource source)
        {
            Initialize(npc);
        }

        public override void AI(NPC npc)
        {
            if (!initialized)
                Initialize(npc);

            if (hitFlashTimer > 0)
                hitFlashTimer--;
        }

        // Persistent dust trails for indicators are removed in favor of PostDraw rendering.
        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (!IsActive(npc)) return;

            // Only show crit spots if holding a Destiny weapon
            if (!(Main.LocalPlayer.HeldItem.ModItem is Destiny2WeaponItem)) return;

            DrawCritSpot(npc, spriteBatch);

            // DEBUG: Magnetism Cone Visualization
            if (global::Destiny2.Destiny2.DiagnosticsEnabled)
            {
                Player player = Main.LocalPlayer;
                if (player == null || !player.active) return;

                if (Vector2.DistanceSquared(npc.Center, player.Center) > DrawRange * DrawRange) return;

                Vector2 headCenter = GetWorldCenter(npc, false); // Visual center
                headCenter.Y += npc.gfxOffY;
                Vector2 playerPos = player.Center;

                Vector2 coneDir = (playerPos - headCenter);
                float dist = coneDir.Length();
                if (dist > 0)
                {
                    // Core Aim Line
                    Utils.DrawLine(spriteBatch, headCenter - Main.screenPosition, playerPos - Main.screenPosition, Color.Lime * 0.4f, Color.Lime * 0.4f, 6f);
                    Utils.DrawLine(spriteBatch, headCenter - Main.screenPosition, playerPos - Main.screenPosition, Color.White * 0.8f, Color.Lime * 0.8f, 2f);

                    // Parallel Forgiveness Boundaries
                    Vector2 normal = new Vector2(-coneDir.Y, coneDir.X).SafeNormalize(Vector2.Zero);
                    float magRadius = (MagnetismStandardRadius + dist * MagnetismGrowthScalar) * npc.scale;

                    Vector2 edge1Start = headCenter + normal * magRadius;
                    Vector2 edge1End = playerPos + normal * magRadius;
                    Vector2 edge2Start = headCenter - normal * magRadius;
                    Vector2 edge2End = playerPos - normal * magRadius;

                    // Glow
                    Utils.DrawLine(spriteBatch, edge1Start - Main.screenPosition, edge1End - Main.screenPosition, Color.Gold * 0.3f, Color.Gold * 0.3f, 4f);
                    Utils.DrawLine(spriteBatch, edge2Start - Main.screenPosition, edge2End - Main.screenPosition, Color.Gold * 0.3f, Color.Gold * 0.3f, 4f);

                    // Core
                    Utils.DrawLine(spriteBatch, edge1Start - Main.screenPosition, edge1End - Main.screenPosition, Color.Yellow * 0.7f, Color.Yellow * 0.7f, 2f);
                    Utils.DrawLine(spriteBatch, headCenter - normal * magRadius - Main.screenPosition, playerPos - normal * magRadius - Main.screenPosition, Color.Yellow * 0.7f, Color.Yellow * 0.7f, 2f);
                }

                // Orbit Center (Physical Head Anchor)
                Vector2 logicCenter = GetWorldCenter(npc, true);
                Vector2 visualCenter = GetWorldCenter(npc, false);

                spriteBatch.Draw(TextureAssets.MagicPixel.Value, visualCenter - Main.screenPosition - new Vector2(4, 4), new Rectangle(0, 0, 8, 8), Color.Red * 0.8f);
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, logicCenter - Main.screenPosition - new Vector2(3, 3), new Rectangle(0, 0, 6, 6), Color.Yellow * 0.8f);
            }
        }

        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter writer)
        {
            bitWriter.WriteBit(initialized);
            if (!initialized)
                return;

            writer.Write(critOffset.X);
            writer.Write(critOffset.Y);
            writer.Write(critRadius);
            writer.Write((byte)archetype);
        }

        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader reader)
        {
            initialized = bitReader.ReadBit();
            if (!initialized)
                return;

            critOffset.X = reader.ReadSingle();
            critOffset.Y = reader.ReadSingle();
            critRadius = reader.ReadSingle();
            archetype = (CritArchetype)reader.ReadByte();
        }

        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            if (item.ModItem is Destiny2WeaponItem)
            {
                modifiers.DisableCrit();
            }
        }

        public bool IsHitInCritSpot(NPC npc, Vector2 hitPosition)
        {
            if (!IsActive(npc))
                return false;

            Vector2 center = GetWorldCenter(npc, true); // Use logic center for collision
            center.Y += npc.gfxOffY;
            float radius = GetRadius(npc);
            return Vector2.DistanceSquared(hitPosition, center) <= radius * radius;
        }

        public bool IsPrecisionShot(NPC npc, Vector2 projectileOrigin, Vector2 hitPoint)
        {
            if (!IsActive(npc)) return false;

            Vector2 headLogicCenter = GetWorldCenter(npc, true); // Use TRUE internal center for magnetism
            headLogicCenter.Y += npc.gfxOffY;

            // 1. Direct Hitbox Check (Fallback)
            if (IsHitInCritSpot(npc, hitPoint)) return true;

            // 2. Projected Distance Magnetism
            Vector2 shotDir = hitPoint - projectileOrigin;
            float shotDist = shotDir.Length();
            if (shotDist <= 0) return false;
            shotDir /= shotDist;

            Vector2 toHead = headLogicCenter - projectileOrigin;
            float projection = Vector2.Dot(toHead, shotDir);

            float t = MathHelper.Clamp(projection, 0, shotDist);
            Vector2 closestPoint = projectileOrigin + shotDir * t;

            float distToShot = Vector2.Distance(headLogicCenter, closestPoint);

            // 3. Archetype-Specific Magnetism and Logic
            float magnetismRadius = MagnetismStandardRadius;

            if (archetype == CritArchetype.Core)
            {
                // CORES: Huge reward for firing AT the center.
                // If the shot line is extremely close to the true logic center, it's a crit,
                // regardless of where the collision actually happened on the hitbox surface.
                magnetismRadius = 12f;
                if (distToShot < magnetismRadius) return true;
            }
            else if (archetype == CritArchetype.Segments)
            {
                magnetismRadius = 0f; // Must hit hitbox precisely
            }

            magnetismRadius += (projection * MagnetismGrowthScalar);

            // Scale magnetism by NPC scale, but clamp for massive bosses so they aren't "free"
            magnetismRadius *= MathHelper.Clamp(npc.scale, 0.5f, 1.5f);

            return distToShot <= magnetismRadius;
        }

        public void RegisterPrecisionHit(NPC npc)
        {
            hitFlashTimer = HitFlashTicks;

            // Spawn a burst of precision dust ONLY once on hit, instead of trailing.
            Vector2 center = GetWorldCenter(npc, false);
            center.Y += npc.gfxOffY;
            float radius = GetRadius(npc);
            for (int i = 0; i < 15; i++)
            {
                Dust d = Dust.NewDustPerfect(center, DustID.WhiteTorch, Main.rand.NextVector2Circular(radius * 0.5f, radius * 0.5f), 150, Color.Cyan, 1.2f);
                d.noGravity = true;
                d.fadeIn = 1.3f;
            }
        }

        private void Initialize(NPC npc)
        {
            if (initialized)
                return;

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            initialized = true;

            var boss = GetBossOverride(npc);
            if (boss.archetype != CritArchetype.None)
            {
                archetype = boss.archetype;
                critOffset = boss.offset;
            }
            else
            {
                archetype = GetArchetype(npc);
                critOffset = GetBaseOffset(npc, archetype);
            }

            if (archetype == CritArchetype.None)
            {
                critRadius = 0f;
                critOffset = Vector2.Zero;
                return;
            }

            // Radius Heuristics
            float sizeScore = MathF.Sqrt(npc.width * npc.height) * npc.scale;
            float radiusMod = archetype switch
            {
                CritArchetype.Core => 0.14f,     // Cores are now surface-hugging, slightly larger for visibility
                CritArchetype.Segments => 0.5f,
                CritArchetype.Eye => 0.2f,
                CritArchetype.Face => 0.15f,
                CritArchetype.Beast => 0.18f,
                _ => RadiusScalar
            };

            critRadius = MathHelper.Clamp(sizeScore * radiusMod, MinRadius, MaxRadius);

            // Random Jitter (Precise for Bosses/Cores/Eyes)
            bool precise = npc.boss || archetype == CritArchetype.Core || archetype == CritArchetype.Eye || archetype == CritArchetype.Face;
            if (!precise && (archetype == CritArchetype.Standard || archetype == CritArchetype.Beast))
            {
                float xJitter = Main.rand.NextFloat(-npc.width * JitterXScalar, npc.width * JitterXScalar);
                float yJitter = Main.rand.NextFloat(-npc.height * JitterYScalar, npc.height * JitterYScalar);
                critOffset += new Vector2(xJitter, yJitter);
            }

            if (Main.netMode == NetmodeID.Server)
                npc.netUpdate = true;
        }

        private static (CritArchetype archetype, Vector2 offset) GetBossOverride(NPC npc)
        {
            // Hand-crafted offsets for major bosses to ensure perfect placement
            switch (npc.type)
            {
                case NPCID.EyeofCthulhu:
                    // Stage 1: Pupil (Pupil faces Left in sheet), Stage 2: Mouth
                    if (npc.ai[0] < 3 && npc.life > npc.lifeMax * 0.5f) return (CritArchetype.Eye, new Vector2(npc.width * 0.42f, 0));
                    return (CritArchetype.Face, new Vector2(npc.width * 0.15f, 0));

                case NPCID.Unicorn:
                case NPCID.Wolf:
                case NPCID.WalkingAntlion:
                    return (CritArchetype.Beast, new Vector2(npc.width * 0.45f, -npc.height * 0.15f));

                case NPCID.EaterofSouls:
                case NPCID.Crimera:
                    return (CritArchetype.Beast, new Vector2(npc.width * 0.4f, 0));

                case NPCID.DD2Betsy:
                case NPCID.DukeFishron:
                    return (CritArchetype.Dragon, new Vector2(npc.width * 0.65f, -npc.height * 0.05f));

                case NPCID.SantaNK1:
                case NPCID.Everscream:
                    return (CritArchetype.Face, new Vector2(0, -npc.height * 0.4f));

                case NPCID.KingSlime:
                case NPCID.QueenSlimeBoss:
                    return (CritArchetype.Core, Vector2.Zero);

                case NPCID.BrainofCthulhu:
                    return (CritArchetype.Core, Vector2.Zero);

                case NPCID.WallofFlesh:
                case NPCID.WallofFleshEye:
                    return (CritArchetype.Eye, new Vector2(npc.width * 0.4f, 0));

                case NPCID.SkeletronHead:
                case NPCID.SkeletronPrime:
                    return (CritArchetype.Face, new Vector2(0, -npc.height * 0.15f));

                case NPCID.MoonLordHead:
                case NPCID.MoonLordHand:
                    return (CritArchetype.Eye, Vector2.Zero);

                case NPCID.MoonLordCore:
                    return (CritArchetype.Core, Vector2.Zero);

                case NPCID.Retinazer:
                case NPCID.Spazmatism:
                    return (CritArchetype.Dragon, new Vector2(npc.width * 0.55f, 0));

                case NPCID.EaterofWorldsHead:
                    return (CritArchetype.Dragon, new Vector2(npc.width * 0.4f, 0));
            }

            return (CritArchetype.None, Vector2.Zero);
        }

        private static CritArchetype GetArchetype(NPC npc)
        {
            if (!IsValidTarget(npc)) return CritArchetype.None;

            // Boss overrides are handled in Initialize, but we check here too for safety
            var boss = GetBossOverride(npc);
            if (boss.archetype != CritArchetype.None) return boss.archetype;

            // "Face-on" Bossses (Centered eyes/faces)
            if (npc.type == NPCID.MourningWood || npc.type == NPCID.IceQueen || npc.type == NPCID.Pumpking)
                return CritArchetype.Face;

            // Martian Walkers / Tall Bots
            if (npc.type == NPCID.MartianWalker || npc.type == NPCID.MartianTurret || npc.type == NPCID.GiantShelly || npc.type == NPCID.Crawdad)
                return CritArchetype.Standard; // Heads are actually high/biped-like

            // AI Style Heuristics
            switch (npc.aiStyle)
            {
                case NPCAIStyleID.Slime:
                    return CritArchetype.Core;
                case NPCAIStyleID.Fighter:
                case NPCAIStyleID.Caster:
                    // Determine if it's a Biped (Standard) or Beast
                    return (npc.width > npc.height * 1.15f) ? CritArchetype.Beast : CritArchetype.Standard;
                case NPCAIStyleID.Worm:
                    return CritArchetype.Segments;
                case NPCAIStyleID.DemonEye:
                    return CritArchetype.Eye;
                case NPCAIStyleID.Flying:
                case NPCAIStyleID.Bat:
                case NPCAIStyleID.Vulture:
                    // Large fliers behave more like dragons (offset head)
                    if (npc.width > 64 || npc.height > 64) return CritArchetype.Dragon;
                    return CritArchetype.Beast;
            }

            // Fallback
            float aspect = (float)npc.height / npc.width;
            if (aspect > 1.3f) return CritArchetype.Standard;
            if (aspect < 0.9f) return CritArchetype.Beast;

            return CritArchetype.Core;
        }

        private static Vector2 GetBaseOffset(NPC npc, CritArchetype archetype)
        {
            float x = 0f;
            float y = 0f;

            switch (archetype)
            {
                case CritArchetype.Standard:
                    // Over-head (Humanoid)
                    y = -npc.height * 0.45f;
                    x = npc.width * 0.12f;
                    break;
                case CritArchetype.Beast:
                    // Frontal head (Wolf, Bee, Bird, etc.)
                    x = npc.width * 0.48f;
                    y = -npc.height * 0.05f;
                    break;
                case CritArchetype.Face:
                    // Centered Face (Everscream, Skeletron Prime, etc.)
                    x = 0f;
                    y = -npc.height * 0.28f; // Higher up for bosses like Everscream/Santa
                    if (npc.type == NPCID.SantaNK1) y = -npc.height * 0.35f; // Santa is tall
                    break;
                case CritArchetype.Dragon:
                    // Significantly further forward (Betsy, Fishron, Wyverns)
                    x = npc.width * 0.7f; // Push it to the very tip
                    y = -npc.height * 0.1f;
                    break;
                case CritArchetype.Core:
                    // Center mass (Slimes)
                    y = -npc.height * 0.1f;
                    break;
                case CritArchetype.Eye:
                    // Outer Pupil edge
                    x = npc.width * 0.45f;
                    y = 0f;
                    break;
                case CritArchetype.Segments:
                    y = 0f;
                    break;
            }

            return new Vector2(x, y);
        }

        private static bool FacesLeftInSheet(NPC npc)
        {
            // HEURISTIC: These NPCs face LEFT in their default sprite sheet (Rotation 0).
            if (npc.type == NPCID.EyeofCthulhu || npc.type == NPCID.DemonEye || npc.aiStyle == NPCAIStyleID.DemonEye) return true;
            if (npc.type == NPCID.EaterofSouls || npc.type == NPCID.Crimera || npc.type == NPCID.EaterofWorldsHead) return true;
            if (npc.type == NPCID.DukeFishron || npc.type == NPCID.DD2Betsy || npc.type == NPCID.Retinazer || npc.type == NPCID.Spazmatism) return true;
            if (npc.type == NPCID.Unicorn || npc.type == NPCID.Wolf || npc.type == NPCID.WalkingAntlion) return true;
            if (npc.aiStyle == NPCAIStyleID.Bat || npc.aiStyle == NPCAIStyleID.Vulture || npc.aiStyle == NPCAIStyleID.Flying) return true;
            return false;
        }

        private bool IsActive(NPC npc)
        {
            return initialized && critRadius > 0f && IsValidTarget(npc);
        }

        private static bool IsValidTarget(NPC npc)
        {
            if (npc == null || !npc.active || npc.dontTakeDamage)
                return false;

            if (npc.friendly || npc.townNPC)
                return false;

            return npc.CanBeChasedBy();
        }

        private Vector2 GetWorldCenter(NPC npc, bool logicOnly)
        {
            Player player = Main.LocalPlayer;
            Vector2 toPlayer = (player != null && player.active) ? (player.MountedCenter - npc.Center).SafeNormalize(Vector2.Zero) : Vector2.Zero;

            // 1. SURFACE HUGGING (Visual-only hint for Cores)
            if (!logicOnly && archetype == CritArchetype.Core)
            {
                if (toPlayer != Vector2.Zero)
                {
                    float halfWidth = (npc.width * 0.5f);
                    float halfHeight = (npc.height * 0.5f);
                    float dist = 1f / Math.Max(Math.Abs(toPlayer.X) / halfWidth, Math.Abs(toPlayer.Y) / halfHeight);
                    return npc.Center + toPlayer * (dist * 0.9f) * npc.scale;
                }
            }

            // 2. Base physical offset
            Vector2 localOffset = critOffset * npc.scale;

            // Heuristic for default sheet orientation (Faces Left = -1, Faces Right = 1)
            float sheetForward = FacesLeftInSheet(npc) ? -1f : 1f;

            // Current facing direction (World facing)
            int worldFacing = npc.spriteDirection;
            if (worldFacing == 0) worldFacing = npc.direction != 0 ? npc.direction : 1;

            // Apply sheet-relative forward direction
            localOffset.X *= sheetForward;

            // If the NPC is flipped in world-space compared to its default sheet orientation, negate X.
            if (worldFacing != (int)sheetForward)
            {
                localOffset.X *= -1;
            }

            // Finally, apply rotation
            if (npc.rotation != 0f)
            {
                localOffset = localOffset.RotatedBy(npc.rotation);
            }

            return npc.Center + localOffset;
        }

        private float GetRadius(NPC npc)
        {
            return critRadius * npc.scale;
        }

        private void DrawCritSpot(NPC npc, SpriteBatch spriteBatch)
        {
            Vector2 center = GetWorldCenter(npc, false);
            center.Y += npc.gfxOffY;
            float radius = GetRadius(npc);
            if (radius <= 0f) return;

            // Load indicator if needed
            indicatorTexture ??= ModContent.Request<Texture2D>("Destiny2/Assets/Textures/CritSpotIndicator");

            Texture2D tex = indicatorTexture.Value;
            Vector2 origin = tex.Size() / 2f;

            // Dynamic pulse for that Destiny feel
            float pulse = 1f + 0.22f * MathF.Sin(Main.GlobalTimeWrappedHourly * 10f);
            float flash = hitFlashTimer > 0 ? 2.5f : 1f;

            // Color logic: Pure Additive works best with high brightness
            // We use a slightly darker base to avoid "blown out" white boxes in additive mode
            Color drawColor = new Color(50, 160, 255) * (0.8f * flash);

            float rotation = 0f;
            if (archetype == CritArchetype.Eye && npc.rotation != 0)
                rotation = npc.rotation;

            // Additive blending + PointClamp is the key to removing the border artifact.
            // This makes black pixels (0,0,0) in the texture contribute 0 to the sum, hiding the box.
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            float finalScale = (radius * 3.2f) / tex.Width;

            // Layer 1: Soft Bloom
            spriteBatch.Draw(tex, center - Main.screenPosition, null, drawColor * 0.4f, rotation, origin, finalScale * pulse * 1.5f, SpriteEffects.None, 0f);

            // Layer 2: Main Reticle (Stronger core)
            spriteBatch.Draw(tex, center - Main.screenPosition, null, drawColor * 1.2f, rotation, origin, finalScale * pulse, SpriteEffects.None, 0f);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
}
