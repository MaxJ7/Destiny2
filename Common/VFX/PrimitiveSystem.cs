using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;

namespace Destiny2.Common.VFX
{
    public struct PrimitiveSettings
    {
        public Func<float, float> WidthFunction;
        public Func<float, Color> ColorFunction;
        public Effect Shader;
        public bool Pixelate;

        public PrimitiveSettings(Func<float, float> WidthFunction, Func<float, Color> ColorFunction, Effect Shader = null, bool Pixelate = false)
        {
            this.WidthFunction = WidthFunction;
            this.ColorFunction = ColorFunction;
            this.Shader = Shader;
            this.Pixelate = Pixelate;
        }
    }

    public struct PrimitiveSettingsCircleEdge
    {
        public Func<float, float> WidthFunction;
        public Func<float, Color> ColorFunction;
        public Func<float, float> RadiusFunction;
        public bool Pixelate;
        public Effect Shader;

        public PrimitiveSettingsCircleEdge(Func<float, float> widthFunction, Func<float, Color> colorFunction, Func<float, float> radiusFunction, bool pixelate = false, Effect shader = null)
        {
            WidthFunction = widthFunction;
            ColorFunction = colorFunction;
            RadiusFunction = radiusFunction;
            Pixelate = pixelate;
            Shader = shader;
        }
    }

    public static class PrimitiveSystem
    {
        public static void RenderTrail(List<Vector2> points, PrimitiveSettings settings)
        {
            if (points.Count < 2) return;

            int count = points.Count;
            VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[count * 2];

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)(count - 1);
                float width = settings.WidthFunction(t);
                Color color = settings.ColorFunction(t);
                Vector2 pos = points[i];

                Vector2 normal = Vector2.Zero;
                if (i < count - 1)
                {
                    normal = points[i + 1] - points[i];
                    normal.Normalize();
                    normal = new Vector2(-normal.Y, normal.X);
                }
                else if (i > 0)
                {
                    normal = points[i] - points[i - 1];
                    normal.Normalize();
                    normal = new Vector2(-normal.Y, normal.X);
                }

                Vector2 left = pos + normal * width * 0.5f;
                Vector2 right = pos - normal * width * 0.5f;

                left -= Main.screenPosition;
                right -= Main.screenPosition;

                vertices[i * 2] = new VertexPositionColorTexture(new Vector3(left, 0), color, new Vector2(t, 0));
                vertices[i * 2 + 1] = new VertexPositionColorTexture(new Vector3(right, 0), color, new Vector2(t, 1));
            }

            Effect effect = settings.Shader;
            if (effect == null) return;

            Matrix transform = Main.GameViewMatrix.TransformationMatrix;
            Matrix projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, 0, 1);
            Matrix wvp = transform * projection;

            if (effect.Parameters["uWorldViewProjection"] != null)
                effect.Parameters["uWorldViewProjection"].SetValue(wvp);

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                Main.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices, 0, count * 2 - 2);
            }
        }

        public static void RenderCircleEdge(Vector2 center, PrimitiveSettingsCircleEdge settings, int points)
        {
            // Closed loop
            int count = points + 1;
            VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[count * 2];

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)points;
                float angle = t * MathHelper.TwoPi;

                float radius = settings.RadiusFunction(t);
                float width = settings.WidthFunction(t);
                Color color = settings.ColorFunction(t);

                Vector2 dir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                Vector2 pos = center + dir * radius;

                Vector2 normal = dir; // Pointing out

                Vector2 core = pos - normal * width * 0.5f;
                Vector2 outer = pos + normal * width * 0.5f;

                core -= Main.screenPosition;
                outer -= Main.screenPosition;

                vertices[i * 2] = new VertexPositionColorTexture(new Vector3(core, 0), color, new Vector2(t, 0));
                vertices[i * 2 + 1] = new VertexPositionColorTexture(new Vector3(outer, 0), color, new Vector2(t, 1));
            }

            Effect effect = settings.Shader;

            if (effect == null) return;

            Matrix transform = Main.GameViewMatrix.TransformationMatrix;
            Matrix projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, 0, 1);
            Matrix wvp = transform * projection;

            if (effect.Parameters["uWorldViewProjection"] != null)
                effect.Parameters["uWorldViewProjection"].SetValue(wvp);

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                Main.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices, 0, count * 2 - 2);
            }
        }
    }
}
