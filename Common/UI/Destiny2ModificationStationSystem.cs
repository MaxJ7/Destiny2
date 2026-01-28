using Microsoft.Xna.Framework;
using Terraria;
using Terraria.UI;
using Terraria.ModLoader;

namespace Destiny2.Common.UI
{
	public sealed class Destiny2ModificationStationSystem : ModSystem
	{
		private static UserInterface stationInterface;
		private static Destiny2ModificationStationUI stationUI;

		public override void Load()
		{
			if (Main.dedServ)
				return;

			stationUI = new Destiny2ModificationStationUI();
			stationUI.Activate();
			stationInterface = new UserInterface();
		}

		public override void UpdateUI(GameTime gameTime)
		{
			if (stationInterface?.CurrentState != null)
				stationInterface.Update(gameTime);
		}

		public override void ModifyInterfaceLayers(System.Collections.Generic.List<GameInterfaceLayer> layers)
		{
			int mouseTextIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
			if (mouseTextIndex == -1)
				return;

			layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
				"Destiny2: Modification Station",
				delegate
				{
					if (stationInterface?.CurrentState != null)
						stationInterface.Draw(Main.spriteBatch, Main._drawInterfaceGameTime);
					return true;
				},
				InterfaceScaleType.UI));
		}

		public static bool IsOpen => stationInterface?.CurrentState != null;

		public static void Open()
		{
			if (stationInterface == null)
				return;

			stationInterface.SetState(stationUI);
		}

		public static void Close()
		{
			if (stationInterface == null)
				return;

			stationInterface.SetState(null);
		}
	}
}
