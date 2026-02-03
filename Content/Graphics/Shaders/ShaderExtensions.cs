using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Reflection;
using Terraria.Graphics.Shaders;

namespace Destiny2.Content.Graphics.Shaders
{
    /// <summary>
    /// Extension methods for shader data, inspired by Calamity's DrawingUtils.
    /// </summary>
    public static class ShaderExtensions
    {
        // Cached reflection fields for performance
        private static FieldInfo _uImage0Field;
        private static FieldInfo _uImage1Field;
        private static FieldInfo _uImage2Field;

        /// <summary>
        /// Sets a texture on a MiscShaderData using reflection.
        /// Calamity's SetShaderTexture extension method pattern.
        /// </summary>
        /// <param name="shader">The shader data to modify</param>
        /// <param name="texture">The texture asset to bind</param>
        /// <param name="index">Sampler index (0 = uImage0, 1 = uImage1, 2 = uImage2)</param>
        public static MiscShaderData SetShaderTexture(this MiscShaderData shader, Asset<Texture2D> texture, int index = 1)
        {
            FieldInfo field = index switch
            {
                0 => _uImage0Field ??= typeof(MiscShaderData).GetField("_uImage0", BindingFlags.NonPublic | BindingFlags.Instance),
                1 => _uImage1Field ??= typeof(MiscShaderData).GetField("_uImage1", BindingFlags.NonPublic | BindingFlags.Instance),
                2 => _uImage2Field ??= typeof(MiscShaderData).GetField("_uImage2", BindingFlags.NonPublic | BindingFlags.Instance),
                _ => null
            };

            field?.SetValue(shader, texture);
            return shader;
        }
    }
}
