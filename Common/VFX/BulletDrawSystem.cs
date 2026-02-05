using Destiny2.Common.Weapons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace Destiny2.Common.VFX
{
    public class BulletDrawSystem : ModSystem
    {
        private struct TraceVFX
        {
            public Vector2 Start;
            public Vector2 End;
            public Destiny2WeaponElement Element;
            public Effect CustomShader; // Added Custom Shader support
            public int TimeLeft;
            public int MaxTime;
            public float Width;

            public TraceVFX(Vector2 start, Vector2 end, Destiny2WeaponElement element, float width, Effect customShader = null)
            {
                Start = start;
                End = end;
                Element = element;
                CustomShader = customShader;
                MaxTime = 15;
                TimeLeft = 15;
                Width = width;
            }
        }

        private static List<TraceVFX> Traces = new();

        public static void SpawnTrace(Vector2 start, Vector2 end, Destiny2WeaponElement element, float width = 15f)
        {
            if (Main.dedServ) return;
            Traces.Add(new TraceVFX(start, end, element, width));
        }

        public static void SpawnTrace(Vector2 start, Vector2 end, Effect customShader, float width = 15f)
        {
            if (Main.dedServ) return;
            // Use Kinetic as placeholder element, but shader will override
            Traces.Add(new TraceVFX(start, end, Destiny2WeaponElement.Kinetic, width, customShader));
        }

        public override void PostDrawTiles()
        {
            if (Main.dedServ) return;

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            for (int i = Traces.Count - 1; i >= 0; i--)
            {
                var trace = Traces[i];
                float completion = 1f - (float)trace.TimeLeft / trace.MaxTime;

                // Opacity fade out
                float opacity = 1f - completion;
                if (opacity <= 0)
                {
                    Traces.RemoveAt(i);
                    continue;
                }

                List<Vector2> trail = GenerateBeamTrail(trace.Start, trace.End);
                RenderElementVFX(trail, trace.Element, opacity, trace.Width, trace.CustomShader);

                // Decrement time
                var newTrace = trace;
                newTrace.TimeLeft--;
                Traces[i] = newTrace;
            }

            Main.spriteBatch.End();
        }

        private static List<Vector2> GenerateBeamTrail(Vector2 start, Vector2 end)
        {
            List<Vector2> points = new List<Vector2>();
            points.Add(start);
            points.Add(end);
            return points;
        }

        private static void RenderElementVFX(List<Vector2> trail, Destiny2WeaponElement element, float opacity, float baseWidth, Effect customShader = null)
        {
            Effect shader = customShader ?? Destiny2Shaders.GetBulletTrailShader(element);
            if (shader == null) return;

            // Common parameters
            if (shader.Parameters["globalTime"] != null)
                shader.Parameters["globalTime"].SetValue(Main.GlobalTimeWrappedHourly);

            if (shader.Parameters["uTime"] != null)
                shader.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly);

            // Solar Uber-Shader Configuration
            if (element == Destiny2WeaponElement.Solar)
            {
                // Core Gradient (White -> Yellow -> Orange -> Red)
                // HDR Tuning: Reverted slightly to originally requested colors but kept >1.0 for bloom.
                shader.Parameters["Color1"]?.SetValue(new Vector3(2.0f, 1.8f, 1.5f));       // Intense White/Yellow Center
                shader.Parameters["Color2"]?.SetValue(new Vector3(1.5f, 0.8f, 0.0f));       // Solar Orange
                shader.Parameters["Color3"]?.SetValue(new Vector3(1.0f, 0.2f, 0.0f));       // Deep Red
                shader.Parameters["Color4"]?.SetValue(new Vector3(0.5f, 0.0f, 0.0f));       // Dark Edge

                // Flow & Distortion
                shader.Parameters["flowSpeed"]?.SetValue(1.5f);
                shader.Parameters["uOpacity"]?.SetValue(opacity);

                // Textures
                var noise = ModContent.Request<Texture2D>("Destiny2/Assets/Textures/Noise/SolarFlameNoise", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                var erosion = ModContent.Request<Texture2D>("Destiny2/Assets/Textures/Noise/SolarExplosionNoise", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                var flow = ModContent.Request<Texture2D>("Destiny2/Assets/Textures/Noise/SolarStreaks", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;

                // Texture 1 (Base Shape): Use MagicPixel (Solid White) so we can shape it mathematically in shader.
                shader.Parameters["sampleTexture1"]?.SetValue(Terraria.GameContent.TextureAssets.MagicPixel.Value);

                shader.Parameters["sampleTexture2"]?.SetValue(noise);   // SolarFlameNoise
                // ROUND 4: Use TakenNoise (Cellular) for Erosion to get "Cracked" look
                var takenNoise = ModContent.Request<Texture2D>("Destiny2/Assets/Textures/Noise/TakenNoise", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                shader.Parameters["sampleTexture3"]?.SetValue(takenNoise); // Was SolarExplosionNoise
                shader.Parameters["sampleTexture4"]?.SetValue(flow);    // SolarStreaks
            }

            // CHECK: Does BulletTrailExplosiveShadow use uImage1? Yes.
            // Where is that texture coming from?
            // "Destiny2/Assets/Textures/SolarNoise" is a good noise candidates, or "TakenNoise".
            // Since we implemented it reusing "SolarNoise" logic potentially, we should set it.
            // However, the Custom Shader call passes 'element' as 'Kinetic' (placeholder).
            // So we might need to check if (customShader == Destiny2Shaders.ExplosiveShadowTrail)

            if (customShader == Destiny2Shaders.ExplosiveShadowTrail)
            {
                if (shader.Parameters["uImage1"] != null)
                    shader.Parameters["uImage1"].SetValue(ModContent.Request<Texture2D>("Destiny2/Assets/Textures/SolarNoise", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value);
            }

            Color baseColor = GetElementColor(element) * opacity;
            // Overwrite color for Explosive Shadow manually if needed, or rely on Shader ignoring Vertex Color?
            // The shader: uses `input.Color`.
            // Kinetic returns White.
            // ExplosiveShadow shader hardcodes colors (White Core, Dark Cyan Aura) and multiplies by input.Color.
            // So White input is perfect.

            float width = baseWidth * opacity;
            if (element == Destiny2WeaponElement.Solar)
            {
                // MULTI-PASS RENDERING
                // Pass 1: The "Bloom" (Concentrated Halo)
                var bloomSettings = new PrimitiveSettings(
                    WidthFunction: (float completion) => 24f * opacity, // Restore width for noise room
                    ColorFunction: (float completion) => new Color(255, 100, 0) * 0.95f * opacity, // Solid Orange
                    Shader: shader,
                    Pixelate: false,
                    UVScale: 64f
                );
                PrimitiveSystem.RenderTrail(trail, bloomSettings);

                // Pass 2: The "Core" (Razor Thin Plasma)
                var coreSettings = new PrimitiveSettings(
                    WidthFunction: (float completion) => 8f * opacity, // Restore width for noise room
                    ColorFunction: (float completion) => new Color(255, 255, 255) * 1.0f * opacity, // Pure White Core
                    Shader: shader,
                    Pixelate: false,
                    UVScale: 64f
                );
                PrimitiveSystem.RenderTrail(trail, coreSettings);
            }
            else
            {
                // Standard Single-Pass Rendering for other elements
                var settings = new PrimitiveSettings(
                    WidthFunction: (float completion) => width,
                    ColorFunction: (float completion) => baseColor,
                    Shader: shader,
                    Pixelate: false,
                    UVScale: null
                );
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
