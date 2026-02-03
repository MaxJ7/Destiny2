using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Shaders;

namespace Destiny2.Content.Graphics.Primitives
{
    public struct PrimitiveSettings
    {
        public Func<float, float> WidthFunction;
        public Func<float, Color> ColorFunction;
        public MiscShaderData Shader;
        public bool Pixelate;

        public PrimitiveSettings(Func<float, float> WidthFunction, Func<float, Color> ColorFunction, MiscShaderData Shader = null, bool Pixelate = false)
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
        public MiscShaderData Shader;

        public PrimitiveSettingsCircleEdge(Func<float, float> widthFunction, Func<float, Color> colorFunction, Func<float, float> radiusFunction, bool pixelate = false, MiscShaderData shader = null)
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

            Vector2 screenPos = Main.screenPosition;

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)(count - 1);
                float width = settings.WidthFunction(t);
                Color color = settings.ColorFunction(t);

                // Manual screen-space subtraction for maximum stability
                Vector2 pos = points[i] - screenPos;

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

                vertices[i * 2] = new VertexPositionColorTexture(new Vector3(left, 0), color, new Vector2(t, 0));
                vertices[i * 2 + 1] = new VertexPositionColorTexture(new Vector3(right, 0), color, new Vector2(t, 1));
            }

            MiscShaderData shader = settings.Shader;
            if (shader == null) return;

            // Flat orthographic projection (no extra matrices, we did the work in C#)
            Matrix projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, 0, 1);
            shader.Shader.Parameters["uWorldViewProjection"]?.SetValue(projection);
            shader.Apply(null);

            Main.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices, 0, count * 2 - 2);
        }

        public static void RenderCircleEdge(Vector2 center, PrimitiveSettingsCircleEdge settings, int points)
        {
            int count = points + 1;
            VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[count * 2];
            Vector2 screenPos = Main.screenPosition;

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)points;
                float angle = t * MathHelper.TwoPi;

                float radius = settings.RadiusFunction(t);
                float width = settings.WidthFunction(t);
                Color color = settings.ColorFunction(t);

                Vector2 dir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                // Manual screen-space subtraction
                Vector2 pos = (center + dir * radius) - screenPos;

                Vector2 normal = dir;

                Vector2 core = pos - normal * width * 0.5f;
                Vector2 outer = pos + normal * width * 0.5f;

                vertices[i * 2] = new VertexPositionColorTexture(new Vector3(core, 0), color, new Vector2(t, 0));
                vertices[i * 2 + 1] = new VertexPositionColorTexture(new Vector3(outer, 0), color, new Vector2(t, 1));
            }

            MiscShaderData shader = settings.Shader;
            if (shader == null) return;

            Matrix projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, 0, 1);
            shader.Shader.Parameters["uWorldViewProjection"]?.SetValue(projection);
            shader.Apply(null);

            Main.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices, 0, count * 2 - 2);
        }
    }
}
