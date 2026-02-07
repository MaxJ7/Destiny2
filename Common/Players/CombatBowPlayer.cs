using Destiny2.Common.Weapons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Destiny2.Common.VFX;
using Terraria.Graphics.Shaders;
using System;

namespace Destiny2.Common.Players
{
    public class CombatBowPlayer : ModPlayer
    {
        public override void PostUpdate()
        {
            // Reset visual state if not drawing
            if (Player.HeldItem.ModItem is CombatBowWeaponItem bow)
            {
                // Logic is handled in bow.HoldItem
            }
        }
    }

    public class BowArrowDrawLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.HeldItem);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            return drawInfo.drawPlayer.HeldItem.ModItem is CombatBowWeaponItem bow && bow.VisualIsDrawing;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            if (player.HeldItem.ModItem is not CombatBowWeaponItem bow) return;

            Texture2D texture = ModContent.Request<Texture2D>("Destiny2/Assets/Textures/Arrow").Value;
            Vector2 nockPos = bow.VisualNockPos;
            Vector2 headPos = bow.VisualHeadPos;
            Vector2 aimDir = bow.VisualAimDir;

            if (nockPos == Vector2.Zero || headPos == Vector2.Zero) return;

            Vector2 center = (nockPos + headPos) / 2f;
            float rotation = aimDir.ToRotation() + MathHelper.PiOver2;

            DrawData drawData = new DrawData(
                texture,
                center - Main.screenPosition,
                null,
                Color.White,
                rotation,
                texture.Size() / 2f,
                1f,
                SpriteEffects.None,
                0
            );

            // Apply Shader based on element
            int shaderId = ElementalShaderSystem.GetShaderId(bow.WeaponElement);

            if (shaderId > 0)
            {
                drawData.shader = shaderId;
            }

            drawInfo.DrawDataCache.Add(drawData);
        }

    }
}
