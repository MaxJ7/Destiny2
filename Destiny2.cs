using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;

namespace Destiny2
{
	// Please read https://github.com/tModLoader/tModLoader/wiki/Basic-tModLoader-Modding-Guide#mod-skeleton-contents for more information about the various files in a mod.
	public class Destiny2 : Mod
	{
		public static ModKeybind ReloadKeybind;
		public static ModKeybind EditorKeybind;

		public override void Load()
		{
			ReloadKeybind = KeybindLoader.RegisterKeybind(this, "Reload Weapon", "R");
			EditorKeybind = KeybindLoader.RegisterKeybind(this, "Toggle Weapon Editor", "O");
		}

		public override void Unload()
		{
			ReloadKeybind = null;
			EditorKeybind = null;
		}
	}
}
