---
trigger: model_decision
description: creating new perk
---

description: How to add a new Perk (Barrel, Mag, or Trait) to the mod.
This workflow guides you through creating a new Perk (e.g., Rampage, Outlaw, Extened Mag) for the Destiny 2 Mod.

1. Define the Perk Class
Create a new class in 
Common/Perks/WeaponPerks.cs
 (or 
FramePerks.cs
 if it's a Frame). The class must inherit from 
Destiny2Perk
 and be sealed.

namespace Destiny2.Common.Perks
{
    public sealed class MyNewPerk : Destiny2Perk
    {
        public override string DisplayName => "My New Perk";
        public override string Description => "Brief description of what it does.";
        public override string IconTexture => "Destiny2/Assets/Perks/MyNewPerk"; // Ensure this texture exists!
        
        // Define the slot type (Barrel, Magazine, Trait, etc.)
        public override PerkSlotType SlotType => PerkSlotType.Trait;
        // Optional: Modify Stats
        public override void ModifyStats(ref Destiny2WeaponStats stats)
        {
            stats.Range += 10;
            stats.ReloadSpeed += 5;
        }
    }
}
2. CRITICAL: Register the Perk
You MUST register the perk in 
Destiny2PerkSystem.cs
 or it will NOT load.

Open 
Common/Perks/Destiny2PerkSystem.cs
 and add your perk to the 
InitializePerks
 method:

private void InitializePerks()
{
    // ... existing perks ...
    Register(new MyNewPerk()); // <--- ADD THIS LINE
}
3. Implement Logic (If applicable)
If your perk has active effects (like damage bonuses, OnKill triggers, etc.), you need to handle them in 
Common/Weapons/Destiny2WeaponItem.Perks.cs
.

Common Hooks:
ModifyShootStats
: Change bullet behavior (velocity, damage) before firing.
NotifyProjectileHit
: Handle on-hit effects (applying buffs, spawning projectiles). MUST use the isPrecision parameter for precision-based perks.
NotifyPlayerHurt
: Handle trigger when player takes damage (e.g., Frenzy).
UpdatePerkTimers
: Handle buff management and timers.
// Example in Destiny2WeaponItem.Perks.cs
internal void NotifyProjectileHit(...)
{
    if (HasPerk<MyNewPerk>() && isKill)
    {
        // Do something cool
    }
}