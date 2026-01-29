using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace Destiny2.Common.UI
{
	public sealed class Destiny2WeaponInfoSystem : ModSystem
	{
		private static UserInterface infoInterface;
		private static Destiny2WeaponInfoUI infoUI;

		public override void Load()
		{
			if (Main.dedServ)
				return;

			infoUI = new Destiny2WeaponInfoUI();
			infoUI.Activate();
			infoInterface = new UserInterface();
		}

		public override void UpdateUI(GameTime gameTime)
		{
			if (infoInterface?.CurrentState != null)
				infoInterface.Update(gameTime);
		}

		public override void ModifyInterfaceLayers(System.Collections.Generic.List<GameInterfaceLayer> layers)
		{
			int mouseTextIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
			if (mouseTextIndex == -1)
				return;

			layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
				"Destiny2: Weapon Info",
				delegate
				{
					if (infoInterface?.CurrentState != null)
						infoInterface.Draw(Main.spriteBatch, Main._drawInterfaceGameTime);
					return true;
				},
				InterfaceScaleType.UI));
		}

		public static void Toggle()
		{
			if (infoInterface == null)
				return;

			if (infoInterface.CurrentState == null)
				infoInterface.SetState(infoUI);
			else
				infoInterface.SetState(null);
		}
	}
}
