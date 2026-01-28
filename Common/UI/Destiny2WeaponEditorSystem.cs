using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;

namespace Destiny2.Common.UI
{
	public sealed class Destiny2WeaponEditorSystem : ModSystem
	{
		private static UserInterface editorInterface;
		private static Destiny2WeaponEditorUI editorUI;

		public override void Load()
		{
			if (Main.dedServ)
				return;

			editorUI = new Destiny2WeaponEditorUI();
			editorUI.Activate();
			editorInterface = new UserInterface();
		}

		public override void UpdateUI(GameTime gameTime)
		{
			if (editorInterface?.CurrentState != null)
				editorInterface.Update(gameTime);
		}

		public override void ModifyInterfaceLayers(System.Collections.Generic.List<GameInterfaceLayer> layers)
		{
			int mouseTextIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
			if (mouseTextIndex == -1)
				return;

			layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
				"Destiny2: Weapon Editor",
				delegate
				{
					if (editorInterface?.CurrentState != null)
						editorInterface.Draw(Main.spriteBatch, Main._drawInterfaceGameTime);
					return true;
				},
				InterfaceScaleType.UI));
		}

		public static void Toggle()
		{
			if (editorInterface == null)
				return;

			if (editorInterface.CurrentState == null)
				editorInterface.SetState(editorUI);
			else
				editorInterface.SetState(null);
		}
	}
}
