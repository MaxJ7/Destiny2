using System;
using System.IO;
using Terraria.ModLoader;
using Terraria;

namespace Destiny2
{
	// Please read https://github.com/tModLoader/tModLoader/wiki/Basic-tModLoader-Modding-Guide#mod-skeleton-contents for more information about the various files in a mod.
	public class Destiny2 : Mod
	{
		public static ModKeybind ReloadKeybind;
		public static ModKeybind EditorKeybind;
		public static ModKeybind InfoKeybind;
		private static readonly object HitscanLogLock = new object();
		internal static string HitscanLogPath;

		public override void Load()
		{
			ReloadKeybind = KeybindLoader.RegisterKeybind(this, "Reload Weapon", "R");
			EditorKeybind = KeybindLoader.RegisterKeybind(this, "Toggle Weapon Editor", "O");
			InfoKeybind = KeybindLoader.RegisterKeybind(this, "Toggle Weapon Info", "I");
			InitializeHitscanLog();
		}

		public override void Unload()
		{
			ReloadKeybind = null;
			EditorKeybind = null;
			InfoKeybind = null;
			HitscanLogPath = null;
		}

		internal static void LogHitscan(string message)
		{
			if (Main.dedServ || string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(HitscanLogPath))
				return;

			string line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}";
			lock (HitscanLogLock)
			{
				File.AppendAllText(HitscanLogPath, line);
			}
		}

		private void InitializeHitscanLog()
		{
			if (Main.dedServ)
				return;

			try
			{
				string modDir = Path.Combine(Main.SavePath, "ModSources", Name);
				Directory.CreateDirectory(modDir);
				HitscanLogPath = Path.Combine(modDir, "Destiny2_hitscan.log");
				string header = $"---- Hitscan Log Start {DateTime.Now:yyyy-MM-dd HH:mm:ss} ----{Environment.NewLine}";
				lock (HitscanLogLock)
				{
					File.AppendAllText(HitscanLogPath, header);
				}
			}
			catch
			{
				HitscanLogPath = null;
			}
		}
	}
}
