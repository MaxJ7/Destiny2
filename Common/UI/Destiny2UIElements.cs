using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.UI;
using Terraria.UI.Chat;

namespace Destiny2.Common.UI
{
    public static class Destiny2UIStyle
    {
        public static readonly Color PanelBack = new Color(18, 18, 20) * 0.95f;
        public static readonly Color PanelBorder = new Color(120, 106, 72) * 0.8f;
        public static readonly Color Gold = new Color(255, 212, 89);
        public static readonly Color TextBase = Color.White;
        public static readonly Color TextDim = Color.Gray;
        public static readonly Color ButtonHover = new Color(45, 45, 52);
    }

    public class Destiny2UISlider : UIElement
    {
        private float _percentage;
        public float Percentage
        {
            get => _percentage;
            set => _percentage = MathHelper.Clamp(value, 0f, 1f);
        }

        public Action<float> OnValueChanged;
        private bool _dragging;

        public Destiny2UISlider()
        {
            Height.Set(20f, 0f);
            Width.Set(100f, 0f);
        }

        public override void LeftMouseDown(UIMouseEvent evt)
        {
            base.LeftMouseDown(evt);
            _dragging = true;
        }

        public override void LeftMouseUp(UIMouseEvent evt)
        {
            base.LeftMouseUp(evt);
            _dragging = false;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (_dragging)
            {
                if (!Main.mouseLeft)
                {
                    _dragging = false;
                    return;
                }

                float oldPercentage = _percentage;
                float relativeX = Main.mouseX - GetDimensions().X;
                Percentage = relativeX / GetDimensions().Width;

                if (oldPercentage != _percentage)
                {
                    OnValueChanged?.Invoke(_percentage);
                }
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle dimensions = GetDimensions();

            // Track
            Rectangle trackRect = new Rectangle((int)dimensions.X, (int)dimensions.Y + 8, (int)dimensions.Width, 4);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, trackRect, Color.Black * 0.6f);

            // Fill
            Rectangle fillRect = new Rectangle((int)dimensions.X, (int)dimensions.Y + 8, (int)(dimensions.Width * _percentage), 4);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, fillRect, Destiny2UIStyle.Gold);

            // Handle
            Rectangle handleRect = new Rectangle((int)(dimensions.X + dimensions.Width * _percentage) - 4, (int)dimensions.Y, 8, (int)dimensions.Height);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, handleRect, IsMouseHovering ? Color.White : Destiny2UIStyle.Gold);
        }
    }

    public class Destiny2UITextInput : UIElement
    {
        public string Text = "";
        public Action<string> OnTextChange;
        public Action OnEnter;
        private bool _focused;
        private int _cursorTimer;

        public Destiny2UITextInput()
        {
            Height.Set(24f, 0f);
            Width.Set(60f, 0f);
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            base.LeftClick(evt);
            _focused = true;
            Main.blockInput = true;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (!IsMouseHovering && Main.mouseLeft) _focused = false;

            if (_focused)
            {
                PlayerInput.WritingText = true;
                Main.instance.HandleIME();
                string newText = Main.GetInputText(Text);
                if (newText != Text)
                {
                    Text = newText;
                    OnTextChange?.Invoke(Text);
                }

                if (Main.keyState.IsKeyDown(Keys.Enter))
                {
                    _focused = false;
                    OnEnter?.Invoke();
                }

                _cursorTimer++;
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle dimensions = GetDimensions();
            Rectangle box = dimensions.ToRectangle();

            spriteBatch.Draw(TextureAssets.MagicPixel.Value, box, Color.Black * 0.8f);
            if (_focused)
            {
                Rectangle border = box;
                border.Inflate(1, 1);
                DrawBorder(spriteBatch, border, Destiny2UIStyle.Gold);
            }

            string display = Text;
            if (_focused && (_cursorTimer / 30) % 2 == 0) display += "|";

            Vector2 pos = dimensions.Position() + new Vector2(4, 2);
            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.MouseText.Value, display, pos, Color.White, 0f, Vector2.Zero, new Vector2(0.8f));
        }

        private void DrawBorder(SpriteBatch spriteBatch, Rectangle r, Color color)
        {
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(r.X, r.Y, r.Width, 1), color);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(r.X, r.Bottom - 1, r.Width, 1), color);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(r.X, r.Y, 1, r.Height), color);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(r.Right - 1, r.Y, 1, r.Height), color);
        }
    }

    public class Destiny2UIDropdown : UIElement
    {
        public static Destiny2UIDropdown OpenDropdown;

        private string _selectedLabel = "Select...";
        public List<string> Options = new List<string>();
        public Action<int> OnSelected;
        private bool _isOpen;
        private float _maxHeight;
        private float _scrollOffset;
        private bool _isDraggingScrollbar;

        public Destiny2UIDropdown(string label, float maxHeight = 200f)
        {
            _selectedLabel = label;
            _maxHeight = maxHeight;
            Height.Set(24f, 0f);
            Width.Set(180f, 0f);
        }

        public void SetSelected(string label) => _selectedLabel = label;

        public override void LeftClick(UIMouseEvent evt)
        {
            base.LeftClick(evt);
            _isOpen = !_isOpen;

            if (_isOpen)
            {
                _scrollOffset = 0;
                if (OpenDropdown != null && OpenDropdown != this)
                    OpenDropdown._isOpen = false;
                OpenDropdown = this;
            }
            else if (OpenDropdown == this)
            {
                OpenDropdown = null;
            }
        }

        public override void ScrollWheel(UIScrollWheelEvent evt)
        {
            base.ScrollWheel(evt);
            if (!_isOpen) return;

            float itemHeight = 22f;
            float totalOptionsHeight = Options.Count * itemHeight;
            if (totalOptionsHeight <= _maxHeight) return;

            _scrollOffset -= evt.ScrollWheelValue;
            _scrollOffset = MathHelper.Clamp(_scrollOffset, 0, totalOptionsHeight - _maxHeight);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            CalculatedStyle dims = GetDimensions();
            float itemHeight = 22f;
            float totalOptionsHeight = Options.Count * itemHeight;
            float drawHeight = Math.Min(totalOptionsHeight, _maxHeight);
            Rectangle fullBounds = _isOpen
                ? new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)(dims.Height + drawHeight))
                : dims.ToRectangle();

            bool hovered = fullBounds.Contains(Main.MouseScreen.ToPoint());
            if (hovered)
            {
                Main.LocalPlayer.mouseInterface = true;
            }

            if (_isOpen && Main.mouseLeft)
            {
                if (!hovered && !_isDraggingScrollbar)
                {
                    _isOpen = false;
                    if (OpenDropdown == this) OpenDropdown = null;
                }
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle dimensions = GetDimensions();
            Rectangle box = dimensions.ToRectangle();

            spriteBatch.Draw(TextureAssets.MagicPixel.Value, box, _isOpen ? Destiny2UIStyle.ButtonHover : Color.Black * 0.8f);
            DrawBorder(spriteBatch, box, Destiny2UIStyle.PanelBorder * 0.5f);

            Vector2 pos = dimensions.Position() + new Vector2(8, 4);
            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.MouseText.Value, _selectedLabel, pos, Destiny2UIStyle.TextBase, 0f, Vector2.Zero, new Vector2(0.85f));

            // Arrow
            string arrow = _isOpen ? "▲" : "▼";
            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.MouseText.Value, arrow, dimensions.Position() + new Vector2(dimensions.Width - 20, 6), Destiny2UIStyle.Gold, 0f, Vector2.Zero, new Vector2(0.7f));

            // Note: Options are drawn externally by the UIState to ensure they appear on top
        }

        public void DrawOptions(SpriteBatch spriteBatch)
        {
            if (!_isOpen) return;

            CalculatedStyle parent = GetDimensions();
            float itemHeight = 22f;
            float totalOptionsHeight = Options.Count * itemHeight;
            float drawHeight = Math.Min(totalOptionsHeight, _maxHeight);
            Rectangle dropdownBox = new Rectangle((int)parent.X, (int)(parent.Y + parent.Height), (int)parent.Width, (int)drawHeight);

            spriteBatch.Draw(TextureAssets.MagicPixel.Value, dropdownBox, Destiny2UIStyle.PanelBack);
            DrawBorder(spriteBatch, dropdownBox, Destiny2UIStyle.PanelBorder);

            // Scrollbar logic
            bool hasScrollbar = totalOptionsHeight > _maxHeight;
            Rectangle scrollbarTrack = new Rectangle(dropdownBox.Right - 10, dropdownBox.Y + 2, 8, (int)drawHeight - 4);
            if (hasScrollbar)
            {
                float handleSize = (drawHeight / totalOptionsHeight) * scrollbarTrack.Height;
                float handlePos = (_scrollOffset / (totalOptionsHeight - _maxHeight)) * (scrollbarTrack.Height - handleSize);
                Rectangle scrollbarHandle = new Rectangle(scrollbarTrack.X, (int)(scrollbarTrack.Y + handlePos), scrollbarTrack.Width, (int)handleSize);

                spriteBatch.Draw(TextureAssets.MagicPixel.Value, scrollbarTrack, Color.Black * 0.4f);

                bool handleHovered = scrollbarHandle.Contains(Main.MouseScreen.ToPoint());
                if (Main.mouseLeft && (handleHovered || _isDraggingScrollbar))
                {
                    _isDraggingScrollbar = true;
                    float mouseY = Main.MouseScreen.Y - scrollbarTrack.Y;
                    float relativePos = (mouseY - handleSize / 2f) / (scrollbarTrack.Height - handleSize);
                    _scrollOffset = MathHelper.Clamp(relativePos * (totalOptionsHeight - _maxHeight), 0, totalOptionsHeight - _maxHeight);
                }
                else if (!Main.mouseLeft)
                {
                    _isDraggingScrollbar = false;
                }

                spriteBatch.Draw(TextureAssets.MagicPixel.Value, scrollbarHandle, _isDraggingScrollbar ? Destiny2UIStyle.Gold : (handleHovered ? Color.White : Color.Gray));
            }

            // Scissor rectangle for clipping
            Rectangle originalScissor = spriteBatch.GraphicsDevice.ScissorRectangle;
            Rectangle scissor = Rectangle.Intersect(dropdownBox, originalScissor);
            spriteBatch.GraphicsDevice.ScissorRectangle = scissor;

            RasterizerState rasterizerState = new RasterizerState { ScissorTestEnable = true };
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, rasterizerState, null, Main.UIScaleMatrix);

            for (int i = 0; i < Options.Count; i++)
            {
                float y = (parent.Y + parent.Height) + i * itemHeight - _scrollOffset;

                // Optimized: Skip drawing if completely outside view
                if (y + itemHeight < dropdownBox.Y || y > dropdownBox.Bottom) continue;

                Rectangle itemRect = new Rectangle((int)parent.X, (int)y, (int)(parent.Width - (hasScrollbar ? 12 : 0)), (int)itemHeight);
                bool hovered = itemRect.Contains(Main.MouseScreen.ToPoint()) && dropdownBox.Contains(Main.MouseScreen.ToPoint());

                if (hovered && !_isDraggingScrollbar)
                {
                    spriteBatch.Draw(TextureAssets.MagicPixel.Value, itemRect, Destiny2UIStyle.ButtonHover);
                    if (Main.mouseLeft && Main.mouseLeftRelease)
                    {
                        OnSelected?.Invoke(i);
                        _isOpen = false;
                        if (OpenDropdown == this) OpenDropdown = null;
                    }
                }

                Vector2 textPos = new Vector2(itemRect.X + 8, itemRect.Y + 3);
                ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.MouseText.Value, Options[i], textPos, hovered ? Color.White : Color.Gray, 0f, Vector2.Zero, new Vector2(0.8f));
            }

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, null, null, Main.UIScaleMatrix);
            spriteBatch.GraphicsDevice.ScissorRectangle = originalScissor;
        }

        private void DrawBorder(SpriteBatch spriteBatch, Rectangle r, Color color)
        {
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(r.X, r.Y, r.Width, 1), color);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(r.X, r.Bottom - 1, r.Width, 1), color);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(r.X, r.Y, 1, r.Height), color);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(r.Right - 1, r.Y, 1, r.Height), color);
        }
    }
}
