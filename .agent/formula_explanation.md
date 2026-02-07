AlAk# Weapon Formula Explanations (Flat Model)

This document explains the "Flat" weapon metrics provided in the spreadsheet. These numbers have been pre-calculated to include all hidden multipliers (like Zoom), making them ready for direct implementation in systems without separate ADSing mechanics.

## 1. Reload Speed (Seconds)

Reload time follows a **Quadratic Model**. This determines the physical duration of the reload animation.

### The Math
`Time = (Curvature * Stat^2) + (Linear Gain * Stat) + Base Value`

### Terms
- **Base Value (Reload Base)**: The inherent reload time in seconds at Stat 0.
- **Linear Gain**: The primary speed improvement per Stat point.
- **Curvature**: The quadratic adjustment that finalizes the scaling curve.
- **Reload (Stat 100)**: The fastest possible reload time achieving a "capped" performance.

---

## 2. Damage Falloff (Meters)

Range determines the distance at which your bullets begin to lose damage.

### The Math
The range values in the spreadsheet are **Flat Ranges**. This means the multiplier for the weapon's archetype (e.g., 1.5x for any Hand Cannon) is already baked into the numbers.

`Reach = (Range Gain * Stat) + Range Start (Stat 0)`

### Column Reference
- **Range Start (Stat 0)**: The distance where damage drop-off **begins** at base stats.
- **Range Start (Stat 100)**: The distance where damage drop-off **begins** at max stats.
- **Range End**: The distance where damage hits the minimum "floor" and stops dropping.
- **Range Gain**: The exact number of **meters** added to your reach for every 1 point of Range stat.

### Example: Hand Cannon (140 RPM)
- **Base Reach**: 24.0m
- **Range Gain**: 0.135m per stat point.
- **Calculation at 50 Stat**: `24.0 + (50 * 0.135) = 30.75m`

---

## 3. The "Damage Floor"
When a weapon exceeds its **Range End** distance, it deals its minimum damage.
- Common floors are **50%** (Auto/Pulse/Scout) and **33%** (Hand Cannons).
- Shotguns have a near-zero floor (effectively a hard range limit).
