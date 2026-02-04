using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;
using Destiny2.Common.Weapons;

namespace Destiny2.Common.VFX
{
    public static class Destiny2Shaders
    {
        // Direct Effects
        public static Effect SolarTrail;
        public static Effect ArcTrail;
        public static Effect VoidTrail;
        public static Effect StasisTrail;
        public static Effect StrandTrail;
        public static Effect KineticTrail;

        // Native BasicEffect for generic primitives (Circles, etc.)
        private static BasicEffect _basicPrimitive;
        public static BasicEffect BasicPrimitive
        {
            get
            {
                if (_basicPrimitive == null && Terraria.Main.graphics?.GraphicsDevice != null)
                {
                    _basicPrimitive = new BasicEffect(Terraria.Main.graphics.GraphicsDevice)
                    {
                        VertexColorEnabled = true
                    };
                }
                return _basicPrimitive;
            }
        }

        public static bool IsReady => SolarTrail != null;

        public static void Load(Mod mod)
        {
            // BasicPrimitive is now lazy-loaded on main thread usage to prevent ThreadStateException

            // Load standard XNB effects
            // Path: Effects/ (Root level Effects folder)

            SolarTrail = mod.Assets.Request<Effect>("Effects/BulletTrailSolar", AssetRequestMode.ImmediateLoad).Value;
            ArcTrail = mod.Assets.Request<Effect>("Effects/BulletTrailArc", AssetRequestMode.ImmediateLoad).Value;
            VoidTrail = mod.Assets.Request<Effect>("Effects/BulletTrailVoid", AssetRequestMode.ImmediateLoad).Value;
            StasisTrail = mod.Assets.Request<Effect>("Effects/BulletTrailStasis", AssetRequestMode.ImmediateLoad).Value;
            StrandTrail = mod.Assets.Request<Effect>("Effects/BulletTrailStrand", AssetRequestMode.ImmediateLoad).Value;
            KineticTrail = mod.Assets.Request<Effect>("Effects/BulletTrailKinetic", AssetRequestMode.ImmediateLoad).Value;
        }

        public static void Unload()
        {
            SolarTrail = null;
            ArcTrail = null;
            VoidTrail = null;
            StasisTrail = null;
            StrandTrail = null;
            KineticTrail = null;
            _basicPrimitive = null;
        }

        public static Effect GetBulletTrailShader(Destiny2WeaponElement element)
        {
            return element switch
            {
                Destiny2WeaponElement.Solar => SolarTrail,
                Destiny2WeaponElement.Arc => ArcTrail,
                Destiny2WeaponElement.Void => VoidTrail,
                Destiny2WeaponElement.Stasis => StasisTrail,
                Destiny2WeaponElement.Strand => StrandTrail,
                _ => KineticTrail
            };
        }
    }
}
