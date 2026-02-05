# AGENT BEHAVIOR PROTOCOL
**CRITICAL INSTRUCTION FOR ALL AI MODELS:**

You are working in a complex, high-context codebase ("Destiny 2" in Terraria). To ensure consistency and prevent regression, you **MUST** follow this initialization sequence before analyzing code or generating plans.

## 1. MANDATORY STARTING SEQUENCE
**Before taking ANY action, verify you have read the following sources of truth:**

1.  **[Task List](../../../../.gemini/antigravity/brain/8d26eeac-9fbf-456e-8626-6a113771ce02/task.md)**
    *   *Why?* Tracks the current state of objectives to avoid redundant work.
2.  **[Development Pipeline](workflows/development_pipeline.md)**
    *   *Why?* Covers the "God Classes", Directory Structure, and Critical Pitfalls.
3.  **[Code Styleguide](workflows/Styleguide.md)**
    *   *Why?* Defines strictly forbidden patterns (e.g., `Enum Pollution`, vanilla `hit.Crit` usage).

## 2. WORKFLOW REFERENCE INDEX
**Consult these specific guides based on your task:**

| Guide | Purpose |
| :--- | :--- |
| **[reference_architecture.md](workflows/reference_architecture.md)** | **System Map.** High-level overview of class responsibilities and file locations. |
| **[bullets.md](workflows/bullets.md)** | **VFX Deep Dive.** Explains the `BulletDrawSystem` and pseudo-hitscan rendering. |
| **[shaders.MD](workflows/shaders.MD)** | **Shader Architecture.** How to compile, load, and implement custom `.fx` shaders. |
| **[create_new_perk.md](workflows/create_new_perk.md)** | **Perk Workflow.** Step-by-step checklist for creating generic, frame, or major perks. |
| **[create_new_weapon.md](workflows/create_new_weapon.md)** | **Weapon Workflow.** Checklist for implementing new guns (Sprites, Sounds, Stats). |
| **[create_new_archetype.md](workflows/create_new_archetype.md)** | **Archetype Workflow.** How to add entirely new weapon classes (e.g., Trace Rifles). |
| **[graphics_architecture.md](workflows/graphics_architecture.md)** | **VFX Bible.** The master guide to Beams, Shaders, and Particle Systems. |

## 3. PERSISTENT SYSTEM RULES (DO NOT HALLUCINATE)
*   **Precision System:** This mod uses a CUSTOM precision system (`Destiny2CritSpotGlobalNPC`).
    *   **Trigger:** Use the `isPrecision` parameter in `NotifyProjectileHit`.
    *   **Visuals:** Use the `isPrecisionHit` flag in `Destiny2PerkProjectile` (independent of damage multiplier).
    *   **Vanilla Code:** Do NOT use `hit.Crit` for logic checks.
*   **VFX System:** Rendering is DECOUPLED.
    *   Bullets use `BulletDrawSystem` + Shaders.
    *   Do NOT put drawing logic in Projectile `PreDraw` if it's a standard bullet trail.
*   **Asset Management:**
    *   Visual-only changes (like "Taken Blight" trail) must NOT pollute the `Destiny2WeaponElement` enum. Use `CustomTrailShader`.

## 4. EXECUTION PROTOCOL
If a user asks for a new feature (Weapon, Perk, Effect), you **MUST**:
1.  Map it to one of the Workflows above.
2.  Read the relevant `.md` file specific to that task.
3.  Follow the checklist step-by-step.
