using Destiny2.Common.Perks;
using Destiny2.Common.Weapons;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace Destiny2.Content.Buffs
{
    public sealed class BlightDebuff : ModBuff
    {
        public override string Texture => "Destiny2/Assets/Perks/ChargedWithBlight";

        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.GetGlobalNPC<BlightGlobalNPC>().IsBlighted = true;
        }
    }

    public sealed class BlightGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;
        public bool IsBlighted;
        private int dotTimer;

        public override void ResetEffects(NPC npc)
        {
            IsBlighted = false;
        }

        public override void AI(NPC npc)
        {
            if (!IsBlighted)
                return;

            dotTimer++;
            if (dotTimer >= 60) // 1 second
            {
                dotTimer = 0;
                // 10 kinetic damage per second
                int damage = ChargedWithBlightPerk.DotDamage;
                npc.SimpleStrikeNPC(damage, 0, false, 0f);
            }

            // Visuals
            if (Main.rand.NextBool(4))
            {
                Dust d = Dust.NewDustDirect(npc.position, npc.width, npc.height, 31, 0f, 0f, 100, Color.Black, 1.2f);
                d.velocity *= 0.3f;
                d.noGravity = true;

                Dust d2 = Dust.NewDustDirect(npc.position, npc.width, npc.height, 31, 0f, 0f, 100, Color.White, 0.8f);
                d2.velocity *= 0.2f;
                d2.noGravity = true;
            }
        }

        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            if (IsBlighted && IsSorrowWeapon(item))
            {
                modifiers.FinalDamage *= ChargedWithBlightPerk.SorrowVulnerability;
            }
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (IsBlighted && IsSorrowWeapon(projectile))
            {
                modifiers.FinalDamage *= ChargedWithBlightPerk.SorrowVulnerability;
            }
        }

        private bool IsSorrowWeapon(Item item)
        {
            if (item?.ModItem is Destiny2WeaponItem weapon)
                return weapon.IsWeaponOfSorrow;
            return false;
        }

        private bool IsSorrowWeapon(Projectile projectile)
        {
            // Try to find the source item
            Player player = Main.player[projectile.owner];
            if (player != null && player.active)
            {
                Item item = player.HeldItem;
                return IsSorrowWeapon(item);
            }
            return false;
        }
    }
}
