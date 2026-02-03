using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Graphics.Shaders;
using Destiny2.Content.Graphics.Shaders;
using Destiny2.Content.Graphics.Primitives;

namespace Destiny2.Content.Graphics.Renderers
{
    /// <summary>
    /// Solar VFX: Destiny-style. Ignition flash, turbulent flame tongues, shard embers, fast burn-out.
    /// Color ramp: #FFF6E5 → yellow → orange → ember red → charcoal. Additive + alpha layers.
    /// </summary>
    public sealed class SolarExplosionRenderer : ModSystem
    {
        #region Parameter Block

        /// <summary>Tweakable parameters for Solar VFX.</summary>
        public static class Params
        {
            public static float ImpactDurationFrames = 24;   // ~0.4s
            public static float IgnitionDurationFrames = 36; // ~0.6s
            public static float ImpactRadiusTiles = 3f;
            public static float IgnitionRadiusTiles = 5f;

            public static Color CoreWhite = new(255, 246, 229);  // #FFF6E5
            public static Color HotYellow = new(255, 210, 74);
            public static Color HotOrange = new(255, 154, 31);
            public static Color EmberRed = new(201, 42, 0);
            public static Color CharcoalResidue = new(80, 60, 50);

            public static int FlashParticleCount = 12;
            public static int FlameTongueCount = 28;
            public static int EmberShardCount = 22;
            public static int SmokeWispCount = 9;
            public static int FlashFrames = 2;
            public static float EmberUpwardBias = 0.35f;
            public static float LightFlickerAmount = 0.18f;
            public static float ExplosionShapeIrregularityImpact = 0.24f;
            public static float ExplosionShapeIrregularityIgnition = 0.32f;
        }

        #endregion

        #region Instance

        private struct SolarInstance
        {
            public Vector2 Center;
            public int SpawnFrame;
            public bool IsIgnition;
            public int Seed;
        }

        private static readonly List<SolarInstance> Instances = new();

        #endregion

        #region Spawn API

        /// <summary>Spawn a Solar impact (small explosion).</summary>
        public static void SpawnSolarImpact(Vector2 center, Vector2 normal, float scale, int seed = 0)
        {
            if (Main.dedServ) return;
            if (float.IsNaN(center.X) || float.IsNaN(center.Y) || float.IsNaN(scale)) return;
            if (center == Vector2.Zero) return;

            if (global::Destiny2.Destiny2.DiagnosticsEnabled)
                Main.NewText($"[VFX] Spawning Solar Impact at {center}", Params.HotOrange);

            Instances.Add(new SolarInstance { Center = center, SpawnFrame = (int)Main.GameUpdateCount, IsIgnition = false, Seed = seed != 0 ? seed : Main.rand.Next() });
            SpawnParticles(center, false, seed);
        }

        /// <summary>Spawn a Solar ignition (larger chain reaction).</summary>
        public static void SpawnSolarIgnition(Vector2 center, float radius, float intensity, int seed = 0)
        {
            if (Main.dedServ) return;
            if (float.IsNaN(center.X) || float.IsNaN(center.Y)) return;
            if (center == Vector2.Zero) return;

            Main.NewText($"[VFX] Spawning Solar Ignition at {center}", Params.HotYellow);

            Instances.Add(new SolarInstance { Center = center, SpawnFrame = (int)Main.GameUpdateCount, IsIgnition = true, Seed = seed != 0 ? seed : Main.rand.Next() });
            SpawnParticles(center, true, seed);
        }

        /// <summary>Legacy: triggers impact or ignition.</summary>
        public static void TriggerExplosion(Vector2 center, bool isIgnition)
        {
            if (isIgnition)
                SpawnSolarIgnition(center, Params.IgnitionRadiusTiles * 16f, 1f);
            else
                SpawnSolarImpact(center, Vector2.UnitY, 1f);
        }

        #endregion

        #region Particle Archetypes

        private static void SpawnParticles(Vector2 center, bool isIgnition, int seed)
        {
            Random rng = seed != 0 ? new Random(seed) : new Random(Main.rand.Next());
            int flashN = Params.FlashParticleCount;
            int flameN = isIgnition ? Params.FlameTongueCount : Params.FlameTongueCount - 4;
            int emberN = isIgnition ? Params.EmberShardCount : Params.EmberShardCount - 4;

            float goldenAngle = 2.39996323f;
            for (int i = 0; i < flashN; i++)
            {
                float a = (i * goldenAngle) % MathHelper.TwoPi + (float)(rng.NextDouble() - 0.5) * 1.2f;
                Vector2 dir = new Vector2((float)Math.Cos(a), (float)Math.Sin(a));
                dir.Y -= 0.25f + (float)rng.NextDouble() * 0.2f;
                dir.Normalize();
                float spd = 6f + (float)rng.NextDouble() * 14f;
                float scale = 1.4f + (float)rng.NextDouble() * 1.2f;
                var d = Dust.NewDustPerfect(center, DustID.Torch, dir * spd, 0, Params.CoreWhite, scale);
                d.noGravity = false; // Give gravity for debris look
                d.alpha = 30 + rng.Next(60);
            }

            for (int i = 0; i < flameN; i++)
            {
                float a = (i * goldenAngle * 0.7f) % MathHelper.TwoPi + (float)(rng.NextDouble() - 0.5) * 1.5f;
                Vector2 dir = new Vector2((float)Math.Cos(a), (float)Math.Sin(a));
                dir.Y -= Params.EmberUpwardBias + (float)(rng.NextDouble() - 0.5) * 0.25f;
                dir.Normalize();
                float spd = isIgnition ? 4f + (float)rng.NextDouble() * 14f : 3f + (float)rng.NextDouble() * 11f;
                Color c = Color.Lerp(Params.HotYellow, Params.HotOrange, (float)rng.NextDouble() * 0.85f);
                float scale = 0.9f + (float)rng.NextDouble() * 1.4f;
                var d = Dust.NewDustPerfect(center, DustID.Torch, dir * spd, 70 + rng.Next(40), c, scale);
                d.noGravity = true;
            }

            for (int i = 0; i < emberN; i++)
            {
                float a = (i * goldenAngle * 1.3f + 0.5f) % MathHelper.TwoPi + (float)(rng.NextDouble() - 0.5) * 1.6f;
                Vector2 dir = new Vector2((float)Math.Cos(a), (float)Math.Sin(a));
                dir.Y -= 0.35f - (float)rng.NextDouble() * 0.2f;
                dir.Normalize();
                float spd = 4f + (float)rng.NextDouble() * 14f;
                Color c = Color.Lerp(Params.HotOrange, Params.EmberRed, (float)rng.NextDouble() * 0.8f);
                float scale = 0.6f + (float)rng.NextDouble() * 0.9f;
                var d = Dust.NewDustPerfect(center, DustID.Torch, dir * spd, 50 + rng.Next(40), c, scale);
                d.noGravity = true;
            }

            if (isIgnition && Params.SmokeWispCount > 0)
            {
                for (int i = 0; i < Params.SmokeWispCount; i++)
                {
                    float a = (float)rng.NextDouble() * MathHelper.TwoPi;
                    Vector2 dir = new Vector2((float)Math.Cos(a), (float)Math.Sin(a));
                    dir.Y -= 0.15f;
                    dir.Normalize();
                    float spd = 2f + (float)rng.NextDouble() * 4f;
                    var d = Dust.NewDustPerfect(center, DustID.Smoke, dir * spd, 140, Params.CharcoalResidue, 0.6f + (float)rng.NextDouble() * 0.4f);
                    d.noGravity = true;
                }
            }
        }

        private static void SpawnSustainedEmbers(Vector2 center, bool isIgnition, float progress, int frame)
        {
            if (progress > 0.55f) return;
            int n = Main.rand.Next(2, 5);
            float baseA = frame * 0.8f + Main.rand.NextFloat(0, MathHelper.TwoPi);
            for (int i = 0; i < n; i++)
            {
                float a = baseA + i * 1.4f + Main.rand.NextFloat(-0.8f, 0.8f);
                Vector2 dir = new Vector2((float)Math.Cos(a), (float)Math.Sin(a));
                dir.Y -= Params.EmberUpwardBias + Main.rand.NextFloat(-0.2f, 0.15f);
                dir.Normalize();
                float spd = Main.rand.NextFloat(4f, 12f) * (1f - progress * 0.8f);
                Color c = Color.Lerp(Params.HotOrange, Params.EmberRed, progress + Main.rand.NextFloat(-0.2f, 0.3f));
                var d = Dust.NewDustPerfect(center, DustID.Torch, dir * spd, 55 + Main.rand.Next(30), c, Main.rand.NextFloat(0.6f, 1.3f));
                d.noGravity = true;
            }
        }

        #endregion

        #region Draw

        /// <summary>
        /// Chaotic radius function for solar wisps (tongues of fire).
        /// </summary>
        private static float SolarWispRadius(float interpolant, float radius, float t, int seed)
        {
            float angle = interpolant * MathHelper.TwoPi;
            // Heavy, slow-moving distortion for fire tongues
            float tongues = (float)Math.Sin(angle * 5f + t * 2f + seed) * 0.3f;
            float noise = (float)Math.Sin(angle * 13f - t * 4f) * 0.15f;
            float shape = 1f + tongues + noise;
            return radius * shape;
        }

        public override void PostDrawTiles()
        {
            if (Main.gameMenu || Main.dedServ || Instances.Count == 0) return;

            MiscShaderData explosionShader = GameShaders.Misc["Destiny2:SolarExplosionShader"];
            MiscShaderData shockwaveShader = GameShaders.Misc["Destiny2:SolarShockwaveShader"];
            if (explosionShader == null || shockwaveShader == null) return;

            Asset<Texture2D> streakyNoise = ModContent.Request<Texture2D>("Destiny2/Assets/Textures/Noise/SolarStreaks");
            Asset<Texture2D> expansionRing = ModContent.Request<Texture2D>("Destiny2/Assets/Textures/Noise/SolarExpansionRing");

            explosionShader.SetShaderTexture(ModContent.Request<Texture2D>("Destiny2/Assets/Textures/Noise/SolarExplosionNoise"), 0);
            explosionShader.SetShaderTexture(ModContent.Request<Texture2D>("Destiny2/Assets/Textures/Noise/SolarExplosionAccent"), 1);
            explosionShader.SetShaderTexture(streakyNoise, 2);

            shockwaveShader.SetShaderTexture(expansionRing, 1);

            int now = (int)Main.GameUpdateCount;
            float t = (float)Main.timeForVisualEffects * 0.04f;

            for (int i = Instances.Count - 1; i >= 0; i--)
            {
                var inst = Instances[i];
                float dur = inst.IsIgnition ? Params.IgnitionDurationFrames : Params.ImpactDurationFrames;
                int elapsed = now - inst.SpawnFrame;
                if (elapsed >= dur)
                {
                    Instances.RemoveAt(i);
                    continue;
                }

                float progress = elapsed / dur;
                float radiusPx = (inst.IsIgnition ? Params.IgnitionRadiusTiles : Params.ImpactRadiusTiles) * 16f;
                float flash = elapsed <= 6 ? (1f - elapsed / 6f) : 0f;
                float opacity = (1f - progress) * (1f - progress) * (1f + flash * 0.5f);

                if (opacity <= 0.01f) continue;

                // 1. Shockwave Layer
                shockwaveShader.Shader.Parameters["uTime"]?.SetValue(t * 3.0f + inst.Seed % 50);
                shockwaveShader.Shader.Parameters["uOpacity"]?.SetValue(opacity);

                var shockwaveSettings = new PrimitiveSettingsCircleEdge(
                    _ => radiusPx * 0.15f * (1f - progress),
                    _ => Params.HotYellow with { A = (byte)(opacity * 200) },
                    _ => radiusPx * 0.9f * progress,
                    false, shockwaveShader);
                PrimitiveSystem.RenderCircleEdge(inst.Center, shockwaveSettings, 40);

                // 2. Fire Core & Shards
                explosionShader.Shader.Parameters["uTime"]?.SetValue(t + inst.Seed % 100);
                explosionShader.Shader.Parameters["uProgress"]?.SetValue(progress);
                explosionShader.Shader.Parameters["uAccentColor"]?.SetValue(Params.HotOrange.ToVector4());
                explosionShader.Shader.Parameters["uIrregularity"]?.SetValue(inst.IsIgnition ? Params.ExplosionShapeIrregularityIgnition : Params.ExplosionShapeIrregularityImpact);

                float wispExpansion = 1f - (float)Math.Pow(1f - progress, 0.7f);
                float wispRadius = radiusPx * wispExpansion;

                var flameSettings = new PrimitiveSettingsCircleEdge(
                    _ => wispRadius * 0.65f,
                    _ => Params.HotOrange with { A = (byte)(opacity * 200) },
                    interpolant => SolarWispRadius(interpolant, wispRadius * 0.45f, t, inst.Seed),
                    false, explosionShader);
                PrimitiveSystem.RenderCircleEdge(inst.Center, flameSettings, 80);

                if (elapsed <= 14 && elapsed % 2 == 0)
                    SpawnSustainedEmbers(inst.Center, inst.IsIgnition, progress, elapsed);

                float light = opacity * (1.5f + flash * 2.2f);
                Lighting.AddLight(inst.Center, 1f * light, 0.55f * light, 0.18f * light);
            }
        }

        #endregion
    }
}
