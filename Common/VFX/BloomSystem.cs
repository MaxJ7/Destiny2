using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace Destiny2.Common.VFX
{
    public class BloomSystem : ModSystem
    {
        private RenderTarget2D _bloomTarget;
        private List<Action> _bloomQueue = new();

        public override void Load()
        {
            if (Main.dedServ) return;

            Main.OnResolutionChanged += ResizeTarget;
            On_Main.DrawDust += DrawBloomLayer;
        }

        public override void Unload()
        {
            if (Main.dedServ) return;

            Main.OnResolutionChanged -= ResizeTarget;
            On_Main.DrawDust -= DrawBloomLayer;

            Main.QueueMainThreadAction(() =>
            {
                _bloomTarget?.Dispose();
                _bloomTarget = null;
            });
        }

        private void ResizeTarget(Vector2 resolution)
        {
            Main.QueueMainThreadAction(() =>
            {
                _bloomTarget?.Dispose();
                _bloomTarget = new RenderTarget2D(Main.graphics.GraphicsDevice,
                    (int)(resolution.X / 2),
                    (int)(resolution.Y / 2));
            });
        }

        /// <summary>
        /// Queue a drawing action to be rendered to the Additive Bloom buffer.
        /// </summary>
        public void QueueBloomRecord(Action drawAction)
        {
            if (Main.dedServ) return;
            _bloomQueue.Add(drawAction);
        }

        private void DrawBloomLayer(On_Main.orig_DrawDust orig, Main self)
        {
            orig(self);

            if (_bloomQueue.Count == 0 || Main.dedServ) return;

            // 1. Initialize Target if missing
            if (_bloomTarget == null || _bloomTarget.IsDisposed)
            {
                ResizeTarget(new Vector2(Main.screenWidth, Main.screenHeight));
            }

            GraphicsDevice device = Main.graphics.GraphicsDevice;
            RenderTargetBinding[] oldTargets = device.GetRenderTargets();

            // 2. Draw active buffer to the Bloom Target
            device.SetRenderTarget(_bloomTarget);
            device.Clear(Color.Transparent);

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.EffectMatrix);

            foreach (var action in _bloomQueue)
            {
                action?.Invoke();
            }

            Main.spriteBatch.End();

            // 3. Restore Main Target and Draw Bloom Overlay
            device.SetRenderTargets(oldTargets);

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.EffectMatrix);

            Main.spriteBatch.Draw(_bloomTarget, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White);

            Main.spriteBatch.End();

            // 4. Clear Queue
            _bloomQueue.Clear();
        }
    }
}
