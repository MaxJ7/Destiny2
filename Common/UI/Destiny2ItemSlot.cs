using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.UI;

namespace Destiny2.Common.UI
{
	public sealed class Destiny2ItemSlot : UIElement
	{
		private readonly Func<Item, bool> canAccept;
		private readonly int context;
		private readonly float scale;

		private Item item;

		public Item Item => item;

		public Destiny2ItemSlot(Func<Item, bool> canAccept, int context = ItemSlot.Context.InventoryItem, float scale = 1f)
		{
			this.canAccept = canAccept;
			this.context = context;
			this.scale = scale;

			item = new Item();
			item.TurnToAir();

			Width.Set(52f * scale, 0f);
			Height.Set(52f * scale, 0f);
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			base.DrawSelf(spriteBatch);

			Rectangle bounds = GetDimensions().ToRectangle();
			Vector2 position = bounds.TopLeft();

			ItemSlot.Draw(spriteBatch, ref item, context, position, Color.White);

			if (!ContainsPoint(Main.MouseScreen))
				return;

			Main.LocalPlayer.mouseInterface = true;
			Item before = item.Clone();
			ItemSlot.Handle(ref item, context);

			if (!IsItemValid(item))
			{
				Item temp = item;
				item = before;
				Main.mouseItem = temp;
			}
		}

		private bool IsItemValid(Item item)
		{
			if (item == null || item.IsAir)
				return true;

			if (canAccept == null)
				return true;

			return canAccept(item);
		}
	}
}
