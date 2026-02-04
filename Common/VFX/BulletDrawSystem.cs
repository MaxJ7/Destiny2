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

            public TraceVFX(Vector2 start, Vector2 end, Destiny2WeaponElement element, Effect customShader = null)
            {
                Start = start;
                End = end;
                Element = element;
                CustomShader = customShader;
                MaxTime = 15;
                TimeLeft = 15;
                Width = 20f;
            }
        }

        private static List<TraceVFX> Traces = new();

        public static void SpawnTrace(Vector2 start, Vector2 end, Destiny2WeaponElement element)
        {
            if (Main.dedServ) return;
            Traces.Add(new TraceVFX(start, end, element));
        }

        public static void SpawnTrace(Vector2 start, Vector2 end, Effect customShader)
        {
            if (Main.dedServ) return;
            // Use Kinetic as placeholder element, but shader will override
            Traces.Add(new TraceVFX(start, end, Destiny2WeaponElement.Kinetic, customShader));
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
                RenderElementVFX(trail, trace.Element, opacity, trace.CustomShader);

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

        private static void RenderElementVFX(List<Vector2> trail, Destiny2WeaponElement element, float opacity, Effect customShader = null)
        {
            Effect shader = customShader ?? Destiny2Shaders.GetBulletTrailShader(element);
            if (shader == null) return;

            // Common parameters
            if (shader.Parameters["globalTime"] != null)
                shader.Parameters["globalTime"].SetValue(Main.GlobalTimeWrappedHourly);

            if (shader.Parameters["uTime"] != null)
                shader.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly);

            // Specific textures (Solar Noise)
            if (element == Destiny2WeaponElement.Solar)
            {
                if (shader.Parameters["NoiseTexture"] != null)
                    shader.Parameters["NoiseTexture"].SetValue(ModContent.Request<Texture2D>("Destiny2/Assets/Textures/SolarNoise", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value);
            }

            // For custom shaders like ExplosiveShadow, we might want custom setup here, 
            // but for now the shader handles its own noise via uImage1 if passed. 
            // Currently BulletTrailExplosiveShadow uses uImage1, which PrimitiveSystem should construct if used.
            // But PrimitiveSystem usually passes Main.pixel for uImage0.
            // If the shader needs uImage1 (Noise), we need to set it here if PrimitiveSystem allows.
            // Our PrimitiveSettings handles shader parameters? No, PrimitiveSystem does.
            // Let's assume the texture is handled by the shader logic or defaults for now.
            // Wait, Custom Shader uses uImage1. primitiveSystem.RenderTrail usually sets textures?
            // Checking: PrimitiveSystem.RenderTrail -> PrimitiveRenderer -> Uses the provided shader.
            // If shader expects uImage1, we MUST set it here.

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

            float width = 15f * opacity;

            var settings = new PrimitiveSettings(
                WidthFunction: (float completion) => width,
                ColorFunction: (float completion) => baseColor,
                Shader: shader,
                Pixelate: false
            );

            PrimitiveSystem.RenderTrail(trail, settings);
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
