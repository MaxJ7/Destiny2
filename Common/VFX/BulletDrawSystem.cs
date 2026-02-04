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
            public int TimeLeft;
            public int MaxTime;
            public float Width;

            public TraceVFX(Vector2 start, Vector2 end, Destiny2WeaponElement element)
            {
                Start = start;
                End = end;
                Element = element;
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
                RenderElementVFX(trail, trace.Element, opacity);

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

        private static void RenderElementVFX(List<Vector2> trail, Destiny2WeaponElement element, float opacity)
        {
            Effect shader = Destiny2Shaders.GetBulletTrailShader(element);
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

            Color baseColor = GetElementColor(element) * opacity;
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
