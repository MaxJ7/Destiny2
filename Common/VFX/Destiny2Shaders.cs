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
        public static Effect CorruptionTrail;
        public static Effect ExplosiveShadowTrail;

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

            SolarTrail = mod.Assets.Request<Effect>("Effects/Linear", AssetRequestMode.ImmediateLoad).Value;

            // LEGACY: All other trails are now techniques within "Linear" (SolarTrail)
            // We keep the static properties null to enforce usage of the Uber Shader
            ArcTrail = SolarTrail;
            VoidTrail = SolarTrail;
            StasisTrail = SolarTrail;
            StrandTrail = SolarTrail;
            KineticTrail = SolarTrail;
            CorruptionTrail = SolarTrail;
            ExplosiveShadowTrail = SolarTrail;
        }

        public static void Unload()
        {
            SolarTrail = null;
            ArcTrail = null;
            VoidTrail = null;
            StasisTrail = null;
            StrandTrail = null;
            KineticTrail = null;
            ExplosiveShadowTrail = null;
            _basicPrimitive = null;
        }

        public static Effect GetBulletTrailShader(Destiny2WeaponElement element)
        {
            // PROXY: All elements now use the Linear Uber-Shader ("SolarTrail")
            // The specific look is determined by the Technique passed in BulletDrawSystem
            return SolarTrail;
        }
    }
}
