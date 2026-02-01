# Project development pipeline analysis
_Exported on 1/31/2026 at 18:01:10 CST from Cursor (2.4.23)_

---

**User**

@c:\Users\vexga\chat-4bcbe48e-8a7d-4d13-84c1-5f4b6a62ce95.txt @c:\Users\vexga\chat-9e42e3c5-b6f0-4468-bf58-276135f3cc54.txt @c:\Users\vexga\chat-57c7cf2e-b3ef-4429-bea7-54c89f4918d6.txt @c:\Users\vexga\chat-82e42621-94eb-45b6-b598-bed125bbca90.txt @c:\Users\vexga\chat-1f80d515-bb0a-4e5c-aa4a-a9786cd4de1e.txt 

here are chatlogs from codex, analyze them to understand development pipeline for this project so when i use you to make changes you can keep syntax, context, and implementation in parity with previous development

---

**Cursor**

Verifying patterns in the codebase to ensure accuracy:


Here’s a distilled **Destiny2 Mod Development Pipeline** summary based on the Codex chat logs and current codebase.

---

## Project Structure & Namespaces

| Layer | Path | Purpose |
|-------|------|---------|
| **Common** | `Destiny2.Common.*` | Shared systems, base classes, globals |
| **Content** | `Destiny2.Content.*` | Concrete items, weapons, projectiles, tiles, buffs |
| **Assets** | `Assets/Perks`, `Assets/Elements`, `Assets/Ammo` | Icons (PNG) |

---

## Weapon System

- **Base:** `Destiny2WeaponItem` (abstract partial class in `Destiny2WeaponStats.cs` + `Destiny2WeaponItem.Perks.cs`)
- **Frames:** `ScoutRifleWeaponItem`, `HandCannonWeaponItem`, `AutoRifleWeaponItem`, etc.
- **Elements:** `Destiny2WeaponElement` (Kinetic, Arc, Solar, Void, Stasis, Strand)
- **Stats:** `Destiny2WeaponStats` struct (Range, Stability, ReloadSpeed, RoundsPerMinute, Magazine)
- **Perks:** Frame perk + up to 4 column perks via `SetPerks()`, `RollPerks()`, `RollFramePerk()`
- **Concrete weapons:** `Trustee`, `Eyasluna`, `Malfeasance`, `Breakneck`, `Khovostov7G0X`, etc.

---

## Projectile System (Bullet / hitscan)

- **CCD:** Continuous collision via `CheckNPCCollision` (AABB vs line)
- **Position tracking:** `localAI[0]` / `localAI[1]` = last position; `spawnPosition` = origin
- **OnSpawn:** `EntitySource_ItemUse` / `EntitySource_ItemUse_WithAmmo` → `sourceItem?.ModItem is Destiny2WeaponItem`
- **Damage:** `SimpleStrikeNPC` or vanilla hit; `hasDealtDamage` to avoid double-hit
- **Trail:** `SpawnDustSegment` (dust-based; shader trail was reverted)
- **Element:** `Projectile.ai[0]` = element ID; element colors (e.g. Solar: 236,85,0)

---

## Damage & tML API Notes

- **No damage variance:** `Destiny2NoDamageVarianceProjectile` uses `modifiers.DamageVariationScale *= 0f` (not `MultipliableFloat`)
- **DamageType:** `weaponElement.GetDamageClass()` → `Destiny2ElementDamageClass`
- **Obsolete API:** Overriding `OnKill(int)` / `ModProjectile.Kill(int)` should mirror base’s `[Obsolete]` where needed

---

## Perk System

- **Registration:** `Destiny2PerkSystem.Register()` in `OnModLoad`
- **Keys:** `nameof(PerkName)` (e.g. `nameof(RapidHitPerk)`)
- **Types:** Frame vs column perks; `perk.IsFrame` distinguishes them
- **UI:** Perk buff timers in HUD (icon, time, stacks); modification station for perks

---

## Shaders & Visuals (Known Pitfalls)

- **Asset loading:** Use `ModContent.Assets.Request<Effect>(path)` with mod-local paths; vanilla `Assets/Effects/` will not load
- **Build:** Shaders go through MGCB / `.mgcontent`; `.xnb` must be produced and included in the mod
- **StarlightRiver pattern:** `Mod.File`, `FileEntry`, etc. are not in tML; don’t copy StarlightRiver’s shader loader
- **Bullet trail:** Current approach is dust-based; shader trail caused crashes and was reverted

---

## Precision Multipliers (from logs; crit system removed)

If crit spots return, use:

- Precision auto rifles: 1.75  
- Khvostov / adaptive auto rifles: 1.75  
- Adaptive hand cannons: 1.79  
- Precision hand cannons & Malfeasance: 1.55  
- Heavy burst hand cannons: 2.2  

---

## Style & Conventions

- Use `sealed class` for leaf types
- XML `<summary>` on public / complex methods
- Constants at top of class
- `if (Main.dedServ) return;` for client-only logic
- `ModContent.ItemType<>` / `ModContent.ProjectileType<>` for IDs

---

## Build & Errors

- **MSB3073 / exit -2147450730:** Often from tML’s build step crashing; check mod code for runtime exceptions
- **NETSDK1127:** .NET targeting pack missing → `dotnet restore` / SDK install
- **Asset not found:** Use mod paths (e.g. `Destiny2/Assets/Effects/...`), not vanilla paths

---

## Current Architecture Snapshot

- Crit spots system: removed
- Bullet trail: dust-based (`SpawnDustSegment`), not shaders
- `Destiny2NoDamageVarianceProjectile`: uses `HitModifiers`, not `MultipliableFloat`
- Perks: split into `Destiny2WeaponItem.Perks.cs` partial; Explosive Shadow lives in `Destiny2PerkProjectile` (not `ExplosiveShadowGlobalItem`)

When you request changes, I’ll follow these patterns, naming, and architecture so new code matches the existing style and behavior.

---

**User**

now save this all to your memeory so you can make changes without upheaving or introducing redundant systems

---

**Cursor**

Creating a Cursor rule so this pipeline is available across sessions.


Saved the development pipeline as a Cursor rule at `.cursor/rules/destiny2-mod-pipeline.mdc`.

This rule:

- Uses **`alwaysApply: true`** so it’s loaded whenever you work on this project
- Describes architecture, weapon/projectile/perk patterns, and anti-patterns
- Captures the pitfalls from the Codex logs (shader loading, MultipliableFloat, StarlightRiver incompatibilities, crit system removal)

Future edits will follow these conventions and avoid adding redundant or conflicting systems.

---

**User**

make this more detained for yourself so i know you get it

---

**Cursor**

The rule file is expanded with more concrete detail. Summary of what was added:

### New / Expanded Sections

1. **File layout** – Paths, namespaces, and roles for each folder.

2. **Weapon perk flow** – Exact steps: `RollFramePerk()` → `SetFramePerk()`, `RollPerks()` → `RollFrom()` → `SetPerks()`, plus code examples for `Trustee`, `WardensLaw`, etc.

3. **Data persistence** – What to store in `TagCompound`, `NetSend`/`NetReceive`, and what to copy in `Clone()`.

4. **Projectile system** – Bullet vs ExplosiveShadowSlug, and how Bullet uses `Colliding()` (not CCD in AI) and `hasDealtDamage` to avoid double-hit.

5. **Destiny2PerkProjectile** – How `GetSourceWeaponItem()` and `GetPerks()` work, and which perks use `Destiny2PerkProjectile` vs `GlobalNPC` vs weapon partials.

6. **Perk logic placement** – When to use GlobalProjectile vs GlobalNPC vs weapon partial vs ModProjectile, with concrete examples.

7. **Perk HUD** – `AppendPerkHudEntries`, icon path convention (`Assets/Perks/{perkKey}.png`), and how `Destiny2HudSystem` draws the panel.

8. **Damage API** – `HitModifiers` (no `MultipliableFloat`) and how to handle obsolete members.

9. **Anti-patterns** – Shader loader, asset paths, and removal of `ExplosiveShadowGlobalItem`.

10. **Precision multipliers** – Table of per-frame values for future use.

11. **Build/error notes** – MSB3073, NETSDK1127, and asset loading issues.

This should give enough context for consistent changes across weapons, perks, projectiles, and UI without introducing new or conflicting systems.

