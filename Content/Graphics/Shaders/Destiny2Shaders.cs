using Destiny2.Common.Weapons;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace Destiny2.Content.Graphics.Shaders
{
    /// <summary>
    /// Provides access to mod shaders registered via GameShaders.Misc.
    /// Shaders are manually loaded and registered in GameShaders.Misc.
    /// </summary>
    public static class Destiny2Shaders
    {
        public static void Load(Mod mod)
        {
            try
            {
                Register(mod, "BulletTrailSolar");
                Register(mod, "BulletTrailArc");
                Register(mod, "BulletTrailVoid");
                Register(mod, "BulletTrailStasis");
                Register(mod, "BulletTrailStrand");
                Register(mod, "BulletTrailKinetic");

                // Explosion Shaders
                Asset<Effect> explosionAsset = mod.Assets.Request<Effect>("Effects/SolarExplosionShader", AssetRequestMode.ImmediateLoad);
                GameShaders.Misc["Destiny2:SolarExplosionShader"] = new MiscShaderData(explosionAsset, "AutoloadPass");

                Asset<Effect> shockwaveAsset = mod.Assets.Request<Effect>("Effects/SolarShockwaveShader", AssetRequestMode.ImmediateLoad);
                GameShaders.Misc["Destiny2:SolarShockwaveShader"] = new MiscShaderData(shockwaveAsset, "AutoloadPass");
            }
            catch (System.Exception e)
            {
                mod.Logger.Error("Critical error in Destiny2Shaders.Load: " + e.Message);
                mod.Logger.Error(e.StackTrace);
            }
        }

        private static void Register(Mod mod, string shaderName)
        {
            try
            {
                Asset<Effect> effectAsset = mod.Assets.Request<Effect>($"Effects/{shaderName}", AssetRequestMode.ImmediateLoad);
                GameShaders.Misc[$"Destiny2:{shaderName}"] = new MiscShaderData(effectAsset, "AutoloadPass");
            }
            catch (System.Exception e)
            {
                mod.Logger.Error($"Failed to register shader '{shaderName}': " + e.Message);
            }
        }

        public static void Unload()
        {
            // Optional: cleaning up the dictionary on unload
            string[] shaderNames = { "BulletTrailSolar", "BulletTrailArc", "BulletTrailVoid", "BulletTrailStasis", "BulletTrailStrand", "BulletTrailKinetic" };
            foreach (var name in shaderNames)
            {
                GameShaders.Misc.Remove($"Destiny2:{name}");
            }
        }

        public static MiscShaderData GetBulletTrailShader(Destiny2WeaponElement element)
        {
            string shaderName = element switch
            {
                Destiny2WeaponElement.Solar => "Destiny2:BulletTrailSolar",
                Destiny2WeaponElement.Arc => "Destiny2:BulletTrailArc",
                Destiny2WeaponElement.Void => "Destiny2:BulletTrailVoid",
                Destiny2WeaponElement.Stasis => "Destiny2:BulletTrailStasis",
                Destiny2WeaponElement.Strand => "Destiny2:BulletTrailStrand",
                _ => "Destiny2:BulletTrailKinetic"
            };

            if (!GameShaders.Misc.ContainsKey(shaderName))
                return GameShaders.Misc.ContainsKey("Destiny2:BulletTrailKinetic") ? GameShaders.Misc["Destiny2:BulletTrailKinetic"] : null;

            return GameShaders.Misc[shaderName];
        }
    }
}
