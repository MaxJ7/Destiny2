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
		private static readonly object DiagnosticLogLock = new object();
		internal static string HitscanLogPath;
		internal static string DiagnosticLogPath;

		public override void Load()
		{
			ReloadKeybind = KeybindLoader.RegisterKeybind(this, "Reload Weapon", "R");
			EditorKeybind = KeybindLoader.RegisterKeybind(this, "Toggle Weapon Editor", "O");
			InfoKeybind = KeybindLoader.RegisterKeybind(this, "Toggle Weapon Info", "I");
			InitializeHitscanLog();
			InitializeDiagnosticLog();
		}

		public override void Unload()
		{
			ReloadKeybind = null;
			EditorKeybind = null;
			InfoKeybind = null;
			HitscanLogPath = null;
			DiagnosticLogPath = null;
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

		internal static void LogDiagnostic(string message)
		{
			if (Main.dedServ || string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(DiagnosticLogPath))
				return;

			string line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}";
			lock (DiagnosticLogLock)
			{
				File.AppendAllText(DiagnosticLogPath, line);
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

		private void InitializeDiagnosticLog()
		{
			if (Main.dedServ)
				return;

			try
			{
				string modDir = Path.Combine(Main.SavePath, "ModSources", Name);
				Directory.CreateDirectory(modDir);
				DiagnosticLogPath = Path.Combine(modDir, "Destiny2_diagnostic.log");
				string header = $"---- Diagnostic Log Start {DateTime.Now:yyyy-MM-dd HH:mm:ss} ----{Environment.NewLine}";
				lock (DiagnosticLogLock)
				{
					File.AppendAllText(DiagnosticLogPath, header);
				}
			}
			catch
			{
				DiagnosticLogPath = null;
			}
		}
	}
}
