using Destiny2.Common.Weapons;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Graphics.Shaders;
using System.Collections.Generic;
using Terraria.DataStructures;
using ReLogic.Content;
using System;

namespace Destiny2.Common.VFX
{
    // ModItem proxies to get unique shader IDs for each element
    public class ElementalDyeProxy : ModItem { public override string Texture => "Terraria/Images/Item_0"; }
    public class SolarDyeProxy : ElementalDyeProxy { }
    public class VoidDyeProxy : ElementalDyeProxy { }
    public class ArcDyeProxy : ElementalDyeProxy { }
    public class StasisDyeProxy : ElementalDyeProxy { }
    public class StrandDyeProxy : ElementalDyeProxy { }

    public class ElementalShaderSystem : ModSystem
    {
        private static Dictionary<Destiny2WeaponElement, int> elementShaderIds;
        private static Dictionary<Destiny2WeaponElement, ArmorShaderData> elementShaders;

        public override void PostSetupContent()
        {
            if (Main.dedServ) return;

            elementShaderIds = new Dictionary<Destiny2WeaponElement, int>();
            elementShaders = new Dictionary<Destiny2WeaponElement, ArmorShaderData>();

            // Request the vanilla pixel shader asset
            var pixelShader = ModContent.Request<Effect>("Terraria/PixelShader", AssetRequestMode.ImmediateLoad);

            if (pixelShader == null || pixelShader.Value == null)
            {
                Mod.Logger.Error("CRITICAL: Vanilla PixelShader asset not found or failed to load!");
                return;
            }

            // Register shaders for each element proxy using "ArmorColored" pass for multi-color gradients
            RegisterShader(Destiny2WeaponElement.Solar, ModContent.ItemType<SolarDyeProxy>(), pixelShader, new Color(255, 140, 0), new Color(255, 220, 0));
            RegisterShader(Destiny2WeaponElement.Void, ModContent.ItemType<VoidDyeProxy>(), pixelShader, new Color(138, 43, 226), new Color(255, 0, 255));
            RegisterShader(Destiny2WeaponElement.Arc, ModContent.ItemType<ArcDyeProxy>(), pixelShader, new Color(10, 200, 255), new Color(200, 240, 255));
            RegisterShader(Destiny2WeaponElement.Stasis, ModContent.ItemType<StasisDyeProxy>(), pixelShader, new Color(30, 144, 255), new Color(200, 200, 255));
            RegisterShader(Destiny2WeaponElement.Strand, ModContent.ItemType<StrandDyeProxy>(), pixelShader, new Color(0, 255, 64), new Color(0, 100, 0));
        }

        private void RegisterShader(Destiny2WeaponElement element, int itemType, Asset<Effect> shader, Color primary, Color secondary)
        {
            if (itemType == 0)
            {
                Mod.Logger.Error($"CRITICAL: ModItem proxy for {element} not found!");
                return;
            }

            // "ArmorColored" is the standard pass for multi-color gradients based on texture luminance.
            // It uses UseColor for primary and UseSecondaryColor for secondary.
            var shaderData = new ArmorShaderData(shader, "ArmorColored")
                .UseColor(primary)
                .UseSecondaryColor(secondary)
                .UseSaturation(1.1f);

            GameShaders.Armor.BindShader(itemType, shaderData);
            int id = GameShaders.Armor.GetShaderIdFromItemId(itemType);
            elementShaderIds[element] = id;
            elementShaders[element] = shaderData;

            if (id == 0)
            {
                Mod.Logger.Warn($"CRITICAL: Failed to get shader ID for {element}! itemType={itemType}");
            }
            else
            {
                Mod.Logger.Debug($"Registered {element} shader: ID={id}");
            }
        }

        public override void Unload()
        {
            elementShaderIds = null;
            elementShaders = null;
        }

        public static int GetShaderId(Destiny2WeaponElement element)
        {
            if (elementShaderIds != null && elementShaderIds.TryGetValue(element, out int id))
            {
                return id;
            }

            // Log once per session per element if not found
            // (Avoiding spam by just logging if dict is null or element missing)
            if (elementShaderIds == null) ModLoader.GetMod("Destiny2").Logger.Warn("ElementalShaderSystem: elementShaderIds is NULL!");

            return 0;
        }

        public static void Apply(Destiny2WeaponElement element, Entity entity, DrawData? drawData)
        {
            int id = GetShaderId(element);
            if (id > 0)
            {
                GameShaders.Armor.Apply(id, entity, drawData);
            }
        }

        public static ArmorShaderData GetShader(Destiny2WeaponElement element)
        {
            if (elementShaders != null && elementShaders.TryGetValue(element, out var shader))
            {
                return shader;
            }
            return null;
        }
    }
}
