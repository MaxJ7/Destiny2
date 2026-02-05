# Destiny 2 Mod Architecture Reference

## 1. System Map Coverage
This codebase is strictly divided into `Common` (Systems/Logic) and `Content` (Assets/Implementations).

### The "God Classes"
*   **Weapon Logic:** `Destiny2WeaponItem` (Common/Weapons)
    *   Core logic lives in `Destiny2WeaponStats.cs` (Shoot, Reload, Stat logic, Tooltips).
    *   Partial logic in `Destiny2WeaponItem.Perks.cs` (Trait state/hooks).
*   **Projectile Logic:** `Destiny2PerkProjectile` (Common/Perks)
    *   Handles: `OnHitNPC` / `OnKill` logic for **all** perks (Explosions, Debuffs, Ricochets).
*   **VFX Logic:** `BulletDrawSystem` (Common/VFX)
    *   Handles: Drawing all bullet trails via Shaders.
    *   **Reference:** See [bullets.md](bullets.md) for the complete breakdown of the "Pseudo-Hitscan" renderer.

---

## 2. Directory Responsibilities (Deep Dive)

### Common/Weapons/
*   `Destiny2WeaponStats.cs`: **Core Class Definition**. Main partial class for `Destiny2WeaponItem`.
*   `Destiny2WeaponItem.Perks.cs`: **Major Perk Implementations**. Stores timers (`rampageStacks`) and Hooks (`NotifyProjectileHit`).
*   `[Archetype]WeaponItem.cs`: **Archetype Logic**. Defines Recoil, Falloff, and **Frame Perk Overrides** (RPM).
*   `Common/Players/`: `ModPlayer` classes (e.g., `Destiny2Player`).
*   `Common/NPCs/`: `GlobalNPC` classes (e.g., `NaniteGlobalNPC`). Logic/State only.
*   `Common/UI/`: User Interface systems.
*   `Destiny2AmmoType.cs`: Enum.
*   `Destiny2WeaponElement.cs`: Enum. **PURELY for Damage Types.** Do not add visual-only elements here.

### Common/Perks/
*   `WeaponPerks.cs`: **Definitions**. Defines properties (Name, Icon, Description, SlotType).
*   `FramePerks.cs`: **Definitions**. Defines Frame perks (Rapid Fire, etc).
*   `Destiny2PerkProjectile.cs`: **Global Projectile**. The logic hub for bullet effects.
*   `Destiny2PerkSystem.cs`: **Registry**. Register all perks here.

### Common/VFX/
*   `Destiny2Shaders.cs`: Shader loader.
*   `BulletDrawSystem.cs`: Trail renderer.
*   `ElementalBulletVFX.cs`: Color logic.
*   `PrimitiveSystem.cs`: Drawing utils.

### Content/Weapons/ (Implementation Examples)
*   **Gold Standard**: [OutbreakPerfected.cs](file:///C:/Users/vexga/Documents/My%20Games/Terraria/tModLoader/ModSources/Destiny2/Content/Weapons/OutbreakPerfected.cs)
    *   **Structure**: `sealed class`, inherits `[Archetype]WeaponItem`.
    *   **Sprite**: Auto-loaded from `OutbreakPerfected.png` in the same directory (No `Texture` override needed).
    *   **Stats**: Overrides `BaseStats` (property) and `GetDefaultWeaponElement`.
    *   **Defaults**: Uses `SetDefaults` to set DamageType, Shoot, ShootSpeed (Critical).
    *   **Perks**: Uses `RollFramePerk` and `RollPerks` (with `RollFrom`/`SetPerks`).

*   **Gold Standard (Shaders)**: [Linear.fx](file:///C:/Users/vexga/Documents/My%20Games/Terraria/tModLoader/ModSources/Destiny2/Effects/Linear.fx)
    *   **Technique**: High-Pressure Energy Stream Logic.
    *   **Features**: Prime-speed scrossing, pow12 thinning, 63-instruction PS 2.0 optimization.

---

## 3. Perk Types & Rules

### A. Frame Perks (The DNA)
*   **Location:** `Common/Perks/FramePerks.cs` + `[Archetype]WeaponItem.cs`.
*   **Rule:** Logic belongs in the Archetype class (e.g. RPM override). Never in generic perks.

### B. Barrel/Mag Perks (Stats)
*   **Location:** `Common/Perks/WeaponPerks.cs`.
*   **Rule:** Simple `ModifyStats` override. "Five and Forget".

### C. Major Perks (SlotType.Major)
*   **Location:** `Common/Perks/WeaponPerks.cs` (Def) + `Destiny2WeaponItem.Perks.cs` (Logic).
*   **Rule:** Requires State (Timer), Trigger (OnHit), and Effect (Stat Buff).

---

## 4. Critical Interactions
*   **Networking:** `Destiny2WeaponItem` syncs state. `Destiny2PerkProjectile` generally does not (impact logic is local/server authoritative).
*   **Performance (Critical):** In `Destiny2PerkProjectile`, **ALWAYS** cache perk presence in `OnSpawn` (e.g., `bool hasPerk = ...`). Do **NOT** call `HasPerk<T>()` in the hot loops like `ModifyHitNPC`.
*   **VFX:** Shaders must be compiled (`EasyXnb`) before `Destiny2Shaders` can load them.