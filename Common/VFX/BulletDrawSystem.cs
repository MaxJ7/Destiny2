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

            public string Technique;

            public TraceVFX(Vector2 start, Vector2 end, Destiny2WeaponElement element, float width, Effect customShader = null, string technique = null)
            {
                Start = start;
                End = end;
                Element = element;
                CustomShader = customShader;
                MaxTime = 15;
                TimeLeft = 15;
                Width = width;
                Technique = technique;
            }
        }

        private static List<TraceVFX> Traces = new();

        public static void SpawnTrace(Vector2 start, Vector2 end, Destiny2WeaponElement element, float width = 15f)
        {
            if (Main.dedServ) return;
            Traces.Add(new TraceVFX(start, end, element, width));
        }

        public static void SpawnTrace(Vector2 start, Vector2 end, Effect customShader, float width = 15f, string technique = null)
        {
            if (Main.dedServ) return;
            // Use Kinetic as placeholder element, but shader will override
            Traces.Add(new TraceVFX(start, end, Destiny2WeaponElement.Kinetic, width, customShader, technique));
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
                RenderTrace(trail, trace.Element, opacity, trace.Width, trace.CustomShader, trace.Technique);

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

        private static void RenderTrace(List<Vector2> trail, Destiny2WeaponElement element, float opacity, float baseWidth, Microsoft.Xna.Framework.Graphics.Effect customShader, string techniqueOverride)
        {
            Effect shader = customShader ?? Destiny2Shaders.GetBulletTrailShader(element);
            string techniqueName = techniqueOverride ?? element.ToString();

            // Common parameters
            if (shader.Parameters["globalTime"] != null)
                shader.Parameters["globalTime"].SetValue(Main.GlobalTimeWrappedHourly);

            if (shader.Parameters["uTime"] != null)
                shader.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly);

            // Set Technique: Use Override if present, else Element Name
            // string techniqueName = techniqueOverride ?? element.ToString();

            // Apply Technique
            if (shader.Techniques[techniqueName] != null)
                shader.CurrentTechnique = shader.Techniques[techniqueName];
            else if (shader.Techniques["Kinetic"] != null)
                shader.CurrentTechnique = shader.Techniques["Kinetic"]; // Fallback

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
                var flow = ModContent.Request<Texture2D>("Destiny2/Assets/Textures/Noise/SolarStreaks", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;

                // Texture 1 (Base Shape): Use MagicPixel (Solid White) so we can shape it mathematically in shader.
                shader.Parameters["sampleTexture1"]?.SetValue(Terraria.GameContent.TextureAssets.MagicPixel.Value);

                shader.Parameters["sampleTexture2"]?.SetValue(noise);   // SolarFlameNoise
                // ROUND 4: Use TakenNoise (Cellular) for Erosion to get "Cracked" look
                var takenNoise = ModContent.Request<Texture2D>("Destiny2/Assets/Textures/Noise/TakenNoise", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                shader.Parameters["sampleTexture3"]?.SetValue(takenNoise); // Was SolarExplosionNoise
                shader.Parameters["sampleTexture4"]?.SetValue(flow);    // SolarStreaks
            }
            else if (techniqueName == "ExplosiveShadow")
            {
                // Explosive Shadow relies on a noise texture for its shape.
                // In Legacy, it used uImage1. In Uber-Shader, we mapped uImage1 -> sampleTexture2.
                var noise = ModContent.Request<Texture2D>("Destiny2/Assets/Textures/SolarNoise", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                shader.Parameters["sampleTexture2"]?.SetValue(noise);
            }

            // Apply Technique
            if (shader.Techniques[techniqueName] != null)
                shader.CurrentTechnique = shader.Techniques[techniqueName];
            else if (shader.Techniques["Kinetic"] != null)
                shader.CurrentTechnique = shader.Techniques["Kinetic"]; // Fallback

            // Solar Uber-Shader Configuration
            if (element == Destiny2WeaponElement.Solar)
            {
                // ... Existing Solar Logic ...
            }

            // Explosive Shadow Setup (Mapped to Noise Texture)
            // IF we can identify it. 
            // For now, let's assume we need to handle "ExplosiveShadow" via a temporary workaround or verify how it's called.
            // Looking at `BulletDrawSystem.cs` original code... explicitly checking `customShader == Destiny2Shaders.ExplosiveShadowTrail`.

            // To support this migration, we need to handle the texture assignment whenever we use the shader.
            // Since `SolarTrail` is the uber shader, we can just assign the noise texture to `sampleTexture2` ALWAYS if we want, or conditionally.
            // `sampleTexture2` is used by Solar (Flame Noise) and ExplosiveShadow (Nebula Noise).
            // Solar sets it explicitly below.
            // We need ExplosiveShadow to set it too.

            // TEMPORARY FIX: Detecting ExplosiveShadow.
            // Since we can't easily, we might need to modify `Destiny2PerkProjectile` to pass a `VisualID` instead of `Effect`.
            // BUT, for this Refactor, if we can't change the interfaces:

            // If `techniqueName == "ExplosiveShadow"`... but `element` won't be that.

            // OKAY: I will modify `Destiny2Shaders.cs` to keep a SEPARATE "ExplosiveShadow" variable that holds a *dummy* effect or just an indicator, 
            // OR we accept that we need to add `Desitny2WeaponElement.ExplosiveShadow` or similar.

            // Wait, look at `RenderElementVFX` args: `Destiny2WeaponElement element, float opacity, float baseWidth, Effect customShader = null`.
            // When `ExplosiveShadow` is used, the projectile sets `CustomTrailShader`.
            // If we update `Destiny2PerkProjectile` to pass a technique name string, that's best.
            // But I can't edit `Destiny2PerkProjectile` easily right now (out of scope/complexity).

            // ALTERNATIVE: Use `shader.Parameters["globalTime"]` check? No.

            // Let's assume for this specific file, we only map ELEMENTS.
            // If `ExplosiveShadow` is used, it might be visually broken unless `element` is matched.
            // Wait, `Destiny2WeaponElement` DOES NOT have `ExplosiveShadow`.

            // Current Workaround:
            // Check if `shader.CurrentTechnique.Name` is "ExplosiveShadow" ? No, that's circular.

            // I will enable "ExplosiveShadow" detection by checking a specific parameter? No.

            // I will add `Technique Override` support to `RenderElementVFX`? No, signature change.

            // I will use `Destiny2WeaponElement.Solar` logic for Solar.
            // I will add logic: If `shader == Destiny2Shaders.SolarTrail` AND `element == Kinetic` (default for many overrides), 
            // maybe we can infer? No.

            // OK, to allow the build to pass and "Most" things to work:
            // I will rely on `element.ToString()`. 
            // `ExplosiveShadow` uses `CorruptionTrail` usually? No, it used `BulletTrailExplosiveShadow`.

            // I will simply set the textures for ALL techniques that need them if possible.
            // `Linear.fx` uses `sampleTexture2` for noise.
            // If I set `sampleTexture2` to `SolarFlameNoise` for Solar, it breaks ExplosiveShadow if it runs on the same frame?
            // `RenderElementVFX` runs per-trail. Setting parameters is immediate for the `DrawUserPrimitives` call that follows.
            // So we CAN overwrite parameters per trace.

            // HACK: I will rename the Effect variable to `ExplosiveShadowTrail` but point it to `SolarTrail`.
            // Object Reference Equality might explicitly fail if they point to the same object.
            // `if (customShader == Destiny2Shaders.ExplosiveShadowTrail)` becomes `if (SolarTrail == SolarTrail)`. Always true.

            // FIX: In `Destiny2Shaders`, I will NOT assign `ExplosiveShadowTrail = SolarTrail`.
            // I will leave it `null` or valid.
            // Actually, if I load it as `SolarTrail` (same file), it will be a DIFFERENT INSTANCE of the Effect class?
            // `mod.Assets.Request<Effect>(...)` returns the SAME asset instance if path is identical.
            // So they will be the same object.

            // SOLUTION: I will look for `ExplosiveShadow` usage. 
            // Generally, visual overrides SHOULD set the Element to something identifiable or we just lose that specific override logic for now
            // and treat it as the Element it is.
            // Most "Explosive Shadow" projectiles likely set `Element = Solar` anyway? No, Kinetic ("MountainTop").

            // I will update the logic to just handle `Destiny2WeaponElement` correctly for now. 
            // I will comment out the `ExplosiveShadow` specific block and rely on generic handling, 
            // knowing that might revert it to a "Kinetic" look until we add `Destiny2WeaponElement.ExplosiveShadow`.

            // (Self-Correction): "Explosive Shadow" is a perk.
            // I'll add a check: `if (element != Solar && element != Arc ...)`.

            // Let's stick to the Plan: "Update RenderElementVFX to ... Select the correct Technique based on Destiny2WeaponElement".
            // If valid: `shader.CurrentTechnique = shader.Techniques[element.ToString()]`.
            // This covers Solar, Arc, Void, Stasis, Strand.
            // Kinetic is default.
            // Corruption is NOT an element. ExplosiveShadow is NOT an element.
            // These will default to `Kinetic` technique for now, effectively "Downgrading" them to Kinetic visuals.
            // This is acceptable for a "Migration" if we note it, BUT user asked for "1:1 visual parity".

            // TO FIX PARITY:
            // I must map `Destiny2WeaponElement` to the correct technique.
            // Use `element.ToString()`.
            // For Corruption/ExplosiveShadow, they are inaccessible via Element enum.
            // I will leave their legacy logic commented out and note that we need to update the Enum or callsite to restore them fully.
            // Or, simply, for this task, I focus on the main 5 elements.

            // WAIT! The user said "move the other bullet effects over... keep their visuals".
            // If I break Corruption/ExplosiveShadow, I fail.

            // I will add `Destiny2WeaponElement.Corruption` and `Destiny2WeaponElement.ExplosiveShadow` to the Enum in a separate step?
            // Modifying Enums breaks binary compatibility but this is source.
            // This is the cleanest way.

            // I'll check `Destiny2WeaponElement.cs` location. `Common/Weapons/Destiny2WeaponElement.cs`.

            if (shader.Parameters["uImage1"] != null)
                shader.Parameters["uImage1"].SetValue(ModContent.Request<Texture2D>("Destiny2/Assets/Textures/SolarNoise", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value);


            Color baseColor = GetElementColor(element) * opacity;

            // Override baseColor for specific techniques if the Element (placeholder) is wrong
            if (techniqueName == "Corruption")
            {
                baseColor = new Color(255, 20, 20) * opacity; // Deep Red
            }
            else if (techniqueName == "ExplosiveShadow")
            {
                baseColor = new Color(255, 255, 255) * opacity; // White
            }
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
