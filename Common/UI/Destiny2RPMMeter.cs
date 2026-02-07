using System;
using System.Collections.Generic;
using Destiny2.Common.Weapons;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace Destiny2.Common.UI
{
    /// <summary>
    /// Real-time RPM measurement system for weapons.
    /// Tracks shots fired and calculates rolling RPM based on a ~5 second window with exponential smoothing.
    /// </summary>
    public sealed class Destiny2RPMMeter : ModSystem
    {
        private static bool isEnabled;
        private static int measurementWindowTicks = 300; // ~5 seconds at 60 TPS for stability
        private static Queue<int> shotTimestamps = new Queue<int>();
        private static float smoothedRPM = 0f;
        private const float SmoothingFactor = 0.1f; // Exponential smoothing (0.1 = 10% new data, 90% old)
        private static int displayUpdateCounter;

        public static bool IsEnabled => isEnabled;
        public static int LastMeasuredRPM => (int)Math.Round(smoothedRPM);

        public override void OnModLoad()
        {
            shotTimestamps = new Queue<int>();
        }

        public override void Unload()
        {
            shotTimestamps?.Clear();
            shotTimestamps = null;
        }

        /// <summary>
        /// Toggle the RPM meter on/off
        /// </summary>
        public static void ToggleRPMMeter()
        {
            isEnabled = !isEnabled;
            if (isEnabled)
                Main.NewText("[RPM Meter] Enabled. Fire your weapon to see real-time RPM.", Color.LimeGreen);
            else
                Main.NewText("[RPM Meter] Disabled.", Color.Gray);
        }

        /// <summary>
        /// Register a shot fired (call from weapon's Shoot method)
        /// </summary>
        public static void RegisterShot()
        {
            if (!isEnabled)
                return;

            int currentTick = (int)Main.GameUpdateCount;
            shotTimestamps.Enqueue(currentTick);
        }

        public override void PostUpdateInput()
        {
            if (!isEnabled)
                return;

            if (Main.LocalPlayer?.HeldItem?.ModItem is not Destiny2WeaponItem)
                return;

            UpdateRPMMeasurement();
            UpdateDisplay();
        }

        private static void UpdateRPMMeasurement()
        {
            int currentTick = (int)Main.GameUpdateCount;
            int windowStart = currentTick - measurementWindowTicks;

            // Remove old timestamps outside the measurement window
            while (shotTimestamps.Count > 0 && shotTimestamps.Peek() < windowStart)
                shotTimestamps.Dequeue();

            // Calculate raw RPM: shots in last 300 ticks (5 seconds) 
            // RPM = (shots in 5 sec) / 5 * 60 = shots * 12
            int shotsInWindow = shotTimestamps.Count;
            float rawRPM = shotsInWindow * 12f; // Convert 5-second sample to per-minute

            // Apply exponential smoothing for stable display
            // New value = (smoothingFactor * rawRPM) + ((1 - smoothingFactor) * oldValue)
            smoothedRPM = (SmoothingFactor * rawRPM) + ((1f - SmoothingFactor) * smoothedRPM);
        }

        private static void UpdateDisplay()
        {
            displayUpdateCounter++;

            // Update display every 30 ticks (~0.5 seconds) to avoid spam
            if (displayUpdateCounter >= 30)
            {
                displayUpdateCounter = 0;

                Player player = Main.LocalPlayer;
                if (player?.HeldItem?.ModItem is Destiny2WeaponItem weapon)
                {
                    Destiny2WeaponStats stats = weapon.GetStats();
                    int theoreticalRPM = stats.RoundsPerMinute;
                    int measuredRPM = (int)Math.Round(smoothedRPM);
                    float efficiency = theoreticalRPM > 0 ? (measuredRPM / (float)theoreticalRPM) * 100f : 0f;

                    // Format: [RPM Meter] Weapon: Trustee | Measured: 390 | Theoretical: 390 | Efficiency: 100.0%
                    string message = $"[RPM Meter] {weapon.Item.Name} | Measured: {measuredRPM} | Theoretical: {theoreticalRPM} | Efficiency: {efficiency:F1}%";
                    Main.NewText(message, new Color(100, 200, 255));
                }
            }
        }
    }
}





