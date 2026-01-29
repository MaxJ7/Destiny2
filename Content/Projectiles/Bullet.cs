using System;
using Destiny2.Common.Weapons;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Destiny2.Content.Projectiles
{
    /// <summary>
    /// Hitscan bullet projectile following the exact pattern used by vanilla Terraria beam weapons.
    /// Based on LastPrismLaser, PhantasmalDeathray, and other vanilla hitscan implementations.
    /// </summary>
    public sealed class Bullet : ModProjectile
    {
        private const float MaxDistance = 2400f;
        private const float DustSpacing = 4f;
        private const float LaserScanWidth = 1f;
        private const int LaserSampleCount = 3;
        private const float HitboxCollisionWidth = 10f;
        private const bool EnableScanDebug = true;

        private static ulong lastScanDebugTick;

        private Vector2 lineStart;
        private Vector2 lineEnd;
        private float maxDistance = MaxDistance;
        private Destiny2WeaponElement weaponElement = Destiny2WeaponElement.Kinetic;

        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.Bullet}";

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.hide = true;
            Projectile.extraUpdates = 0;
            Projectile.timeLeft = 2;
        }

        public override void OnSpawn(IEntitySource source)
        {
            // Extract weapon element from source
            weaponElement = Destiny2WeaponElement.Kinetic;
            
            if (source is EntitySource_ItemUse itemUse && itemUse.Item?.ModItem is Destiny2WeaponItem weaponItem)
            {
                weaponElement = weaponItem.WeaponElement;
                Projectile.DamageType = weaponElement.GetDamageClass();

                float maxFalloffTiles = weaponItem.GetMaxFalloffTiles();
                if (maxFalloffTiles > 0f)
                    maxDistance = Math.Max(16f, maxFalloffTiles * 3f * 16f);
            }
            else
            {
                Projectile.DamageType = Destiny2WeaponElement.Kinetic.GetDamageClass();
            }

            if (float.IsNaN(Projectile.ai[1]) || float.IsInfinity(Projectile.ai[1]))
                Projectile.ai[1] = 0f;

            string aimSource = "ai";
            if (Projectile.ai[1] == 0f)
            {
                float velLenSq = Projectile.velocity.LengthSquared();
                if (velLenSq > 0.0001f)
                {
                    float velRot = Projectile.velocity.ToRotation();
                    if (Math.Abs(velRot) > 0.0001f)
                    {
                        Projectile.ai[1] = velRot;
                        aimSource = "vel";
                    }
                }

                if (Projectile.ai[1] == 0f && velLenSq <= 0.0001f)
                {
                    Projectile.ai[1] = GetFallbackAimRotation();
                    aimSource = "fallback";
                }
            }

            if (EnableScanDebug && Main.netMode != NetmodeID.Server && aimSource == "fallback")
                LogSpawnDebug(Projectile, aimSource);
        }

        public override void AI()
        {
            // Calculate beam start and end on first tick only
            Vector2 startPos = Projectile.Center;
            float aimRot = Projectile.ai[1];
            Vector2 direction = aimRot.ToRotationVector2();
            if (!IsFinite(direction))
            {
                direction = GetFallbackAimDirection();
                aimRot = direction.ToRotation();
                Projectile.ai[1] = aimRot;
            }
            
            // Perform hitscan using vanilla Terraria's method
            bool ignoreTiles = Projectile.ai[2] != 0f;
            float beamLength = PerformHitscan(startPos, direction, maxDistance, ignoreTiles, out float tileDistance, out float npcDistance);
            
            // Set line endpoints
            lineStart = startPos;
            lineEnd = startPos + direction * beamLength;
            
            // Spawn visual dust trail
            SpawnDust(lineStart, lineEnd, weaponElement);

            if (EnableScanDebug && Main.netMode != NetmodeID.Server)
                LogScanDebug(Projectile, startPos, direction, maxDistance, tileDistance, npcDistance, beamLength, aimRot, ignoreTiles);
            
            // Projectile dies immediately after hitscan
            Projectile.Kill();
        }

        /// <summary>
        /// Performs a hitscan using Collision.LaserScan exactly like vanilla beam weapons.
        /// Returns the distance to the first collision (tile or NPC).
        /// </summary>
        private float PerformHitscan(Vector2 start, Vector2 direction, float maxRange, bool ignoreTiles, out float tileDistance, out float npcDistance)
        {
            // Scan for tile collisions using vanilla method
            tileDistance = ignoreTiles ? maxRange : ScanForTileCollision(start, direction, maxRange);
            
            // Scan for NPC collisions
            npcDistance = ScanForNpcCollision(start, direction, tileDistance);
            
            // Return whichever is closer
            return Math.Min(tileDistance, npcDistance);
        }

        /// <summary>
        /// Scans for tile collision using Collision.LaserScan.
        /// This is exactly how vanilla beam weapons detect walls.
        /// </summary>
        private float ScanForTileCollision(Vector2 start, Vector2 direction, float maxRange)
        {
            Vector2 end = start + direction * maxRange;
            
            // Use Collision.LaserScan - it takes start, end, width, sampleCount, and samples array
            float[] samples = new float[LaserSampleCount];
            Collision.LaserScan(start, end, LaserScanWidth, LaserSampleCount, samples);

            float distance = 0f;
            for (int i = 0; i < LaserSampleCount; i++)
                distance += samples[i];

            distance /= LaserSampleCount;
            return MathHelper.Clamp(distance, 0f, maxRange);
        }

        /// <summary>
        /// Scans for NPC collision along the line.
        /// Checks AABB vs line collision for each active hostile NPC.
        /// </summary>
        private float ScanForNpcCollision(Vector2 start, Vector2 direction, float maxRange)
        {
            float closestDistance = maxRange;
            Vector2 end = start + direction * maxRange;
            
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                
                // Skip inactive, friendly, or invulnerable NPCs
                if (!npc.active || npc.friendly || npc.dontTakeDamage)
                    continue;
                
                // Check line vs AABB collision
                float collisionPoint = 0f;
                if (Collision.CheckAABBvLineCollision(
                    npc.position,
                    npc.Size,
                    start,
                    end,
                    HitboxCollisionWidth,
                    ref collisionPoint))
                {
                    if (collisionPoint < closestDistance)
                    {
                        closestDistance = collisionPoint;
                    }
                }
            }
            
            return closestDistance;
        }

        private static void LogScanDebug(Projectile projectile, Vector2 start, Vector2 direction, float maxDistance, float tileDistance,
            float npcDistance, float beamLength, float aimRot, bool ignoreTiles)
        {
            ulong tick = Main.GameUpdateCount;
            if (tick == lastScanDebugTick)
                return;

            lastScanDebugTick = tick;
            int owner = projectile?.owner ?? -1;
            bool ownerOnGround = false;
            if (owner >= 0 && owner < Main.maxPlayers)
                ownerOnGround = Main.player[owner].velocity.Y == 0f;

            float velLen = projectile?.velocity.Length() ?? 0f;
            Destiny2.LogHitscan(
                $"Bullet scan: owner={owner} onGround={ownerOnGround} start=({start.X:0.0},{start.Y:0.0}) velLen={velLen:0.00} aimRot={aimRot:0.00} dir=({direction.X:0.00},{direction.Y:0.00}) ignoreTiles={ignoreTiles} max={maxDistance:0} tile={tileDistance:0.0} npc={npcDistance:0.0} dist={beamLength:0.0}");
        }

        private static void LogSpawnDebug(Projectile projectile, string aimSource)
        {
            ulong tick = Main.GameUpdateCount;
            if (tick == lastScanDebugTick)
                return;

            lastScanDebugTick = tick;
            int owner = projectile?.owner ?? -1;
            float velLen = projectile?.velocity.Length() ?? 0f;
            float aimRot = projectile?.ai[1] ?? 0f;
            Vector2 dir = aimRot.ToRotationVector2();
            Destiny2.LogHitscan(
                $"Bullet spawn: owner={owner} velLen={velLen:0.00} aimSource={aimSource} aimRot={aimRot:0.00} dir=({dir.X:0.00},{dir.Y:0.00}) pos=({projectile?.Center.X:0.0},{projectile?.Center.Y:0.0})");
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // Use line-based collision detection
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                lineStart,
                lineEnd,
                HitboxCollisionWidth,
                ref collisionPoint
            );
        }

        public override bool ShouldUpdatePosition() => false;

        private static bool IsFinite(Vector2 value)
        {
            return !float.IsNaN(value.X) && !float.IsNaN(value.Y) && !float.IsInfinity(value.X) && !float.IsInfinity(value.Y);
        }

        private Vector2 GetFallbackAimDirection()
        {
            int owner = Projectile.owner;
            if (owner >= 0 && owner < Main.maxPlayers)
            {
                Player player = Main.player[owner];
                if (player != null)
                {
                    if (Main.netMode != NetmodeID.Server && player.whoAmI == Main.myPlayer)
                    {
                        Vector2 aim = Main.MouseWorld - player.MountedCenter;
                        if (aim.LengthSquared() > 0.0001f)
                            return aim.SafeNormalize(Vector2.UnitX);
                    }

                    return new Vector2(player.direction, 0f);
                }
            }

            return Vector2.UnitX;
        }

        private float GetFallbackAimRotation()
        {
            Vector2 direction = GetFallbackAimDirection();
            return direction.ToRotation();
        }

        #region Dust Visual Effects (Preserved Original Code)
        
        private static void SpawnDust(Vector2 start, Vector2 end, Destiny2WeaponElement element)
        {
            if (Main.dedServ)
                return;

            GetDustStyle(element, out int dustType, out Color dustColor);

            Vector2 direction = (end - start).SafeNormalize(Vector2.UnitX);
            Vector2 perpendicular = direction.RotatedBy(MathHelper.PiOver2);
            float length = Vector2.Distance(start, end);
            int count = Math.Max(2, (int)(length / DustSpacing));

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)(count - 1);
                Vector2 pos = Vector2.Lerp(start, end, t);

                int dustCount = Main.rand.Next(2, 4);
                for (int j = 0; j < dustCount; j++)
                {
                    float hueShift = (j - 1) * 0.03f;
                    Color shifted = ShiftHue(dustColor, hueShift);

                    Vector2 offset = perpendicular * Main.rand.NextFloat(-2f, 2f) + direction * Main.rand.NextFloat(-1f, 1f);
                    float scale = Main.rand.NextFloat(1.0f, 1.45f);

                    Dust dust = Dust.NewDustDirect(pos + offset - new Vector2(2f), 4, 4, dustType, 0f, 0f, 40, shifted, scale);
                    dust.noGravity = true;
                    dust.noLight = false;
                    dust.velocity *= 0.3f;
                    dust.color = shifted;
                }
            }
        }

        private static void GetDustStyle(Destiny2WeaponElement element, out int dustType, out Color dustColor)
        {
            dustType = DustID.WhiteTorch;
            dustColor = GetElementColor(element);
        }

        private static Color GetElementColor(Destiny2WeaponElement element)
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

        private static Color ShiftHue(Color color, float shift)
        {
            Vector3 rgb = color.ToVector3();
            float max = Math.Max(rgb.X, Math.Max(rgb.Y, rgb.Z));
            float min = Math.Min(rgb.X, Math.Min(rgb.Y, rgb.Z));
            float delta = max - min;

            float hue = 0f;
            if (delta > 0.0001f)
            {
                if (max == rgb.X)
                    hue = (rgb.Y - rgb.Z) / delta;
                else if (max == rgb.Y)
                    hue = 2f + (rgb.Z - rgb.X) / delta;
                else
                    hue = 4f + (rgb.X - rgb.Y) / delta;

                hue /= 6f;
            }

            if (hue < 0f)
                hue += 1f;

            float saturation = max <= 0f ? 0f : delta / max;
            float value = max;

            float shiftedHue = hue + shift;
            if (shiftedHue < 0f)
                shiftedHue += 1f;
            else if (shiftedHue >= 1f)
                shiftedHue -= 1f;

            return ColorFromHsv(shiftedHue, saturation, value);
        }

        private static Color ColorFromHsv(float hue, float saturation, float value)
        {
            float c = value * saturation;
            float x = c * (1f - Math.Abs((hue * 6f) % 2f - 1f));
            float m = value - c;

            float r;
            float g;
            float b;

            float h = hue * 6f;
            if (h < 1f)
            {
                r = c;
                g = x;
                b = 0f;
            }
            else if (h < 2f)
            {
                r = x;
                g = c;
                b = 0f;
            }
            else if (h < 3f)
            {
                r = 0f;
                g = c;
                b = x;
            }
            else if (h < 4f)
            {
                r = 0f;
                g = x;
                b = c;
            }
            else if (h < 5f)
            {
                r = x;
                g = 0f;
                b = c;
            }
            else
            {
                r = c;
                g = 0f;
                b = x;
            }

            return new Color(r + m, g + m, b + m);
        }
        
        #endregion
    }
}
