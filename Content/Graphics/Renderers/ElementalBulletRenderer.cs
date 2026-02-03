using Destiny2.Common.Weapons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using ReLogic.Content;
using Terraria.Graphics.Shaders;
using Destiny2.Content.Graphics.Primitives;
using Destiny2.Content.Graphics.Shaders;
using Destiny2.Content.Projectiles;

namespace Destiny2.Content.Graphics.Renderers
{
    public class ElementalBulletRenderer : ModSystem
    {
        private struct TraceVFX
        {
            public Vector2 Start;
            public Vector2 End;
            public Destiny2WeaponElement Element;
            public int TimeLeft;
            public int MaxTime;
            public float Width;
            public float Seed;
            public float Length;
            public bool IsTaken;

            public TraceVFX(Vector2 start, Vector2 end, Destiny2WeaponElement element, bool isTaken = false)
            {
                Start = start;
                End = end;
                Element = element;
                IsTaken = isTaken;
                MaxTime = 20;
                TimeLeft = 20;
                Width = 25f;
                Seed = Main.rand.NextFloat();
                Length = Vector2.Distance(start, end);
            }
        }

        private static List<TraceVFX> Traces = new();

        public static void SpawnTrace(Vector2 start, Vector2 end, Destiny2WeaponElement element, bool isTaken = false)
        {
            if (Main.dedServ) return;
            Traces.Add(new TraceVFX(start, end, element, isTaken));
        }

        /// <summary>
        /// Calculates the beam end point using collision checks.
        /// </summary>
        public static Vector2 GetBeamEndPoint(Vector2 start, Vector2 velocity, float maxDistance)
        {
            Vector2 end = start + velocity * maxDistance;

            // Manual collision check (Terraria's collision methods are weird with points)
            // CanHitLine works for vision, but we want solid collision.
            // Let's use a step-based raycast for moderate precision.
            Vector2 unit = velocity;
            if (unit == Vector2.Zero) return start;

            for (float dist = 0; dist < maxDistance; dist += 8f)
            {
                Vector2 pos = start + unit * dist;
                Point tileCoords = pos.ToTileCoordinates();

                // Active/Solid check
                if (WorldGen.InWorld(tileCoords.X, tileCoords.Y, 2))
                {
                    Tile tile = Main.tile[tileCoords.X, tileCoords.Y];
                    if (tile != null && tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType])
                    {
                        return pos;
                    }
                }
            }
            return end;
        }

        public override void PostDrawTiles()
        {
            if (Main.dedServ) return;

            // SCORCHED EARTH RESTORATION: 
            // 1. Draw all primitives OUTSIDE SpriteBatch to avoid state clobbering.
            // 2. Use manual screen-space logic in PrimitiveSystem.

            // 1. Draw Active Projectiles
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (!p.active) continue;

                if (p.ModProjectile is Bullet bullet)
                {
                    var data = bullet.GetDrawData();
                    if (data.trail.Count >= 2)
                    {
                        MiscShaderData shader = Destiny2Shaders.GetBulletTrailShader(data.element);
                        if (shader != null)
                        {
                            RenderElementVFX(new List<Vector2>(data.trail), data.element, 1f, (float)(p.whoAmI * 0.1), 0f, shader);
                        }
                    }
                }
                else if (p.ModProjectile is ExplosiveShadowSlug slug)
                {
                    var data = slug.GetDrawData();
                    if (data.trail.Count >= 2)
                    {
                        // Use Taken shader for Slug
                        if (GameShaders.Misc.TryGetValue("Destiny2:BulletTrailTaken", out MiscShaderData shader))
                        {
                            RenderElementVFX(new List<Vector2>(data.trail), data.element, 1f, (float)(p.whoAmI * 0.1), 0f, shader);
                        }
                        {
                            RenderElementVFX(new List<Vector2>(data.trail), data.element, 1f, (float)(p.whoAmI * 0.1), 0f, shader);
                        }
                    }
                }
            }

            // 2. Draw Impact Traces
            for (int i = Traces.Count - 1; i >= 0; i--)
            {
                var trace = Traces[i];
                float completion = 1f - (float)trace.TimeLeft / trace.MaxTime;
                float opacity = 1f - completion;

                if (opacity <= 0)
                {
                    Traces.RemoveAt(i);
                    continue;
                }

                List<Vector2> trail = new List<Vector2> { trace.Start, trace.End };
                // Correct Shader Selection
                MiscShaderData shader = null;
                if (trace.IsTaken && GameShaders.Misc.TryGetValue("Destiny2:BulletTrailTaken", out var takenShader))
                {
                    shader = takenShader;
                }
                else
                {
                    shader = Destiny2Shaders.GetBulletTrailShader(trace.Element);
                }

                if (shader != null)
                {
                    RenderElementVFX(trail, trace.Element, opacity, trace.Seed, trace.Length, shader);
                }

                var newTrace = trace;
                newTrace.TimeLeft--;
                Traces[i] = newTrace;
            }
        }

        private static void RenderElementVFX(List<Vector2> trail, Destiny2WeaponElement element, float opacity, float seed, float length, MiscShaderData shader)
        {
            if (shader == null || trail.Count < 2) return;

            bool isTaken = shader.Shader.Name == "BulletTrailTaken"; // Or check logic

            if (element == Destiny2WeaponElement.Solar)
            {
                // ... Solar Logic ...
                shader.SetShaderTexture(ModContent.Request<Texture2D>("Destiny2/Assets/Textures/Noise/SolarFlameNoise"), 1);
                shader.SetShaderTexture(ModContent.Request<Texture2D>("Destiny2/Assets/Textures/Noise/SolarStreaks"), 2);

                float effectiveLength = length > 0 ? length : Vector2.Distance(trail[0], trail[trail.Count - 1]);
                shader.Shader.Parameters["uLengthRatio"]?.SetValue(effectiveLength / 400f);
                shader.Shader.Parameters["uSeed"]?.SetValue(seed);
            }
            else if (isTaken)
            {
                // TAKEN VISUALS
                shader.SetShaderTexture(ModContent.Request<Texture2D>("Destiny2/Assets/Textures/Noise/TakenNoise"), 1);
            }

            shader.Shader.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);

            Color baseColor = GetElementColor(element) * opacity;
            float width = (element == Destiny2WeaponElement.Solar ? 36f : 16f) * opacity;

            if (isTaken) width = 24f * opacity; // Slightly thicker for Taken

            var settings = new PrimitiveSettings(
                WidthFunction: _ => width,
                ColorFunction: _ => baseColor,
                Shader: shader
            );

            // Use NonPremultiplied for Taken to allow "Black" subtractive rendering
            if (isTaken)
            {
                Main.spriteBatch.End(); // Flush current batch
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

                PrimitiveSystem.RenderTrail(trail, settings);

                Main.spriteBatch.End();
                // We are outside SpriteBatch in PostDrawTiles, so no need to restart it here.
            }
            else
            {
                PrimitiveSystem.RenderTrail(trail, settings);
            }
        }

        private static Color GetElementColor(Destiny2WeaponElement element)
        {
            return element switch
            {
                Destiny2WeaponElement.Solar => new Color(255, 100, 0),
                Destiny2WeaponElement.Arc => new Color(0, 200, 255),
                Destiny2WeaponElement.Void => new Color(150, 0, 255),
                Destiny2WeaponElement.Stasis => new Color(0, 50, 200),
                Destiny2WeaponElement.Strand => new Color(0, 255, 100),
                _ => Color.White
            };
        }
    }
}
