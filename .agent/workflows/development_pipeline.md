# Destiny 2 Mod Development Pipeline

## 1. Architecture Overview
**[> READ THE CODE STYLE GUIDE FIRST <](code_styleguide.md)**

The codebase is strictly divided into `Common` (Systems/Logic) and `Content` (Assets/Implementations).

### The "God Classes"
*   **Weapon Logic:** `Destiny2WeaponItem` (Common/Weapons) - The core is in `Destiny2WeaponStats.cs` (handling Shoot, Reload, Stats) and `Destiny2WeaponItem.Perks.cs` (Perks).
*   **Projectile Logic:** `Destiny2PerkProjectile` (Common/Perks) - Handles *everything* that happens when a bullet hits something (Ricochets, Explosions, Debuff application).
*   **VFX Logic:** `BulletDrawSystem` (Common/VFX) - Handles drawing beam trails for all weapons. **See [bullets.md](bullets.md) for a deep dive.**

---

## 2. Directory Structure & System Map

### common/Weapons/ (The Core)
*   `Destiny2WeaponStats.cs`: **THE HEART.** Contains the main partial class for `Destiny2WeaponItem`. Handles `Shoot`, `CanUseItem`, `Reload`, and `ModifyWeaponDamage`.
*   `Destiny2WeaponItem.Perks.cs`: Partial class containing the *implementation* of Trait Perks (e.g., Rampage Stacks).
*   `[Archetype]WeaponItem.cs`: (e.g., `ScoutRifleWeaponItem`) Defines Recoil, Falloff Curves, and **Frame Perk Overrides** (RPM).

### common/Perks/ (The Enhancers)
*   **`Destiny2PerkSystem.cs`**: Registry.
*   **`FramePerks.cs`**: Definitions for Frame perks (Rapid Fire, Adaptive).
*   **`WeaponPerks.cs`**: Definitions for Trait perks (Rampage, Outlaw).
*   **`Destiny2PerkProjectile.cs`**: GLOBAL PROJECTILE. Intercepts `OnHitNPC` to execute perk logic.
*   **`PerkSlotType.cs`**: Enum (`Barrel`, `Magazine`, `Major`).

### common/VFX/ (The Visuals)
*   `BulletDrawSystem.cs`: The central rendering loop.
*   `Destiny2Shaders.cs`: Loads and compiles `.fx` files.

### common/NPCs/ (The Logic)
*   **GlobalNPCs**: All `GlobalNPC` classes belong here (e.g. `NaniteGlobalNPC.cs`). They track state (debuffs, stacks) on enemies.

### common/UI/ (The Interface)
*   `Destiny2HudSystem.cs`: Draws the HUD.

### Assets/Perks/ (The Visuals)
*   **Icon Library:** Always search this directory before implementing a new perk.
*   **Naming Rule:** Matches class names for seamless loading. (e.g., `Rampage.png` -> `RampagePerk`).
*   **Workflow:** If an icon exists, use its name for the perk class to avoid pathing errors.

---

## 3. Perk Anatomy
Perks are divided into 4 categories.

### A. Frame Perks (The DNA)
*   **Definition:** `Common/Perks/FramePerks.cs`.
*   **Code:** `IsFrame = true`.
*   **Role:** Defines the Archetype's "Feel". Modified RPM, Recoil.
*   **Implementation:** Logic lives in `[Archetype]WeaponItem.cs`.
*   **Rule:** **NEVER** implement Frame logic in the generic `Destiny2WeaponItem.Perks.cs`.

### B. Barrel Perks (The Stats)
*   **Definition:** `Common/Perks/WeaponPerks.cs`.
*   **Code:** `SlotType = PerkSlotType.Barrel`.
*   **Role:** Passive stat bumps (Range+, Stability+).
*   **Implementation:** Override `ModifyStats` in the perk class itself.

### C. Magazine Perks (The Stats + Utility)
*   **Definition:** `Common/Perks/WeaponPerks.cs`.
*   **Code:** `SlotType = PerkSlotType.Magazine`.
*   **Role:** Stat bumps (Mag Size+) or Bullet Behavior (Ricochet).
*   **Implementation:** Override `ModifyStats` OR add mechanics to `Destiny2PerkProjectile.cs`.

### D. Major 'Trait' Perks (The Gameplay)
*   **Definition:** `Common/Perks/WeaponPerks.cs`.
*   **Code:** `SlotType = PerkSlotType.Major`.
*   **Role:** Active gameplay loops (Kill -> Damage Buff).
*   **Implementation:** Logic lives in `Destiny2WeaponItem.Perks.cs` (State/Timers) and `Destiny2PerkProjectile.cs` (Triggers).

---

## 4. Workflows & Checklists

### Workflow 1: Creating a New Weapon
*Goal: Add "The Recluse" (Void SMG).*

1.  **Assets:**
    *   Sprite: `Content/Weapons/TheRecluse.png` (Copy standard size).
    *   Sound: Add firing sound to `Assets/Sounds/`.
2.  **Code:**
    *   Create `Content/Weapons/TheRecluse.cs`.
    *   Inherit `SubmachineGunWeaponItem`.
3.  **Stats:**
    *   Override `BaseStats`. Set `RPM`, `MagSize`, `Stability`.
    *   Set `WeaponElement = Destiny2WeaponElement.Void`.
4.  **Perks:**
    *   Override `RollFramePerk` -> `LightweightFramePerk`.
    *   Override `RollPerks`:
        *   `RollFrom(nameof(ChamberedCompensatorPerk), ...)`
        *   `RollFrom(nameof(RicochetRoundsPerk), ...)`
        *   `RollFrom(nameof(FeedingFrenzyPerk), ...)`
        *   `RollFrom(nameof(MasterOfArmsPerk))` (Unique!).

### Workflow 2: Creating a Simple Perk (Stat Buff)
*Goal: Add "Smallbore" (Barrel).*

1.  **Define:** Open `Common/Perks/WeaponPerks.cs`.
    *   Create `public sealed class SmallborePerk : Destiny2Perk`.
    *   Set `PerkSlotType.Barrel`.
2.  **Logic:** Override `ModifyStats`.
    ```csharp
    public override void ModifyStats(ref Destiny2WeaponStats stats) {
        stats.Range += 7;
        stats.Stability += 7;
    }
    ```
3.  **Done.** No other files needed.

### Workflow 3: Creating a Complex Perk (On Kill Effect)
*Goal: Add "Master of Arms" (Major).*

1.  **Define:** Open `Common/Perks/WeaponPerks.cs`.
    *   Create class. Set `PerkSlotType.Major`.
2.  **State:** Open `Common/Weapons/Destiny2WeaponItem.Perks.cs`.
    *   Add `private int masterOfArmsTimer;`.
    *   Add to `UpdatePerkTimers`: `if (masterOfArmsTimer > 0) masterOfArmsTimer--;`.
3.  **Trigger:** In `NotifyProjectileHit` (same file):
    ```csharp
    if (isKill && HasPerk<MasterOfArmsPerk>()) {
        masterOfArmsTimer = MasterOfArmsPerk.Duration;
    }
    ```
4.  **Buff:** In `ApplyActivePerkStats` (same file):
    ```csharp
    if (masterOfArmsTimer > 0) stats.DamageMultiplier *= 1.2f;
    ```
5.  **HUD:** In `AppendPerkHudEntries` (same file):
    ```csharp
    if (masterOfArmsTimer > 0) AddPerkHudEntry(..., nameof(MasterOfArmsPerk), ...);
    ```

### Workflow 4: Creating a Projectile Effect (Explosion)
*Goal: Add "Dragonfly" (Explosion on Precision Kill).*

1.  **Define:** `Common/Perks/WeaponPerks.cs`.
2.  **Trigger:** Open `Common/Perks/Destiny2PerkProjectile.cs`.
3.  **Logic:** In `OnHitNPC`:
    ```csharp
    if (isKill && isPrecision && perks.Contains<DragonflyPerk>()) {
        SpawnExplosion(target.Center); // Custom method
    }
    ```

### Workflow 5: Alternate Firing Modes (Hold Activation)
*Goal: Add "Blight Launcher" (Hold Reload to swap shot type).*

1.  **State Management:** In `Destiny2WeaponItem.Perks.cs`:
    *   Add `stacks`, `isActive`, and `holdTimer`.
    *   Implement an `UpdateMode` method called via `UpdatePerkTimers`.
    *   Check `global::Destiny2.Destiny2.ReloadKeybind.Current` for the hold timer.
2.  **Firing Logic:** In `Destiny2WeaponStats.cs`:
    *   Intercept the `Shoot` override.
    *   If `isActive`, fire the custom projectile and set `isActive = false`.
3.  **Heal/VFX:** Activation logic in `UpdateMode` handles the `player.HealEffect` and sound.

### Workflow 6: Creating a New Archetype
*Goal: Add "Trace Rifles" as a new weapon class.*
See **[create_new_archetype.md](create_new_archetype.md)** for the full guide.

---

## 5. Critical Gotchas & Common Pitfalls

### ⚠️ The "Hit Gate" Pitfall
In `Destiny2PerkProjectile.cs`, the `NotifyProjectileHit` call is **gated** by a check for active perks.
*   **The Problem:** If you add a new hit-based perk but forget to add its `hasPerk` check to the `if (!hasOutlaw && !hasRapidHit ...)` guard clause, the perk will **never** trigger unless the weapon *also* has one of those other perks.
*   **The Fix:** Always add a `hasYourNewPerk` flag and include it in that final guard.

### ⚠️ Weapon Stat Consistency
*   **The Rule:** Never reference `BaseStats` for tooltips or UI calculations.
*   **The Fix:** Always use `GetStats()` (or `weapon.GetStats()`). This ensures that barrel/magazine buffs and active trait bonuses (like Target Lock) are accurately reflected in the numbers shown to the player.

### ⚠️ Perk Trigger Ordering (Hit vs. Kill)
In `NotifyProjectileHit`, the order of logic is critical to prevent "leaking" effects or missing triggers.
*   **The Rule:** Always place **Hit-based** logic (Stacks, Combat Registration) **ABOVE** the `isKill` guard. Place **Kill-based** logic (Outlaw, Feeding Frenzy) **BELOW** it.
*   **The Gotcha:** If you accidentally place a Kill-based perk like `Feeding Frenzy` above the guard, it will become an infinite-uptime perk that triggers on every shot. If you place a Hit-based perk like `Charged with Blight` below the guard, it will feel "broken" because it only charges when you finish off an enemy.

### ⚠️ Precision Hit Logic
*   **The Rule:** Never rely on `hit.Crit` or `damageMultiplier` for logic.
*   **The Fix:** Use the `isPrecision` parameter passed to `NotifyProjectileHit`. This ensures that even weapons with a 1.0x precision multiplier (e.g. Explosive Payload) still trigger precision perks.

### ⚠️ Weapon of Sorrow Identification
*   **The Requirement:** To benefit from the Blight debuff's +50% damage bonus, a weapon must be tagged.
*   **The Implementation:** Override `public override bool IsWeaponOfSorrow => true;` in the specific weapon class (e.g., `TouchOfMalice.cs`).
