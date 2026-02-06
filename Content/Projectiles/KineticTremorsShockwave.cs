using System;
using Destiny2.Common.Perks;
using Destiny2.Common.VFX;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Destiny2.Content.Projectiles
{
    public sealed class KineticTremorsShockwave : ModProjectile
    {
        private const float ShockwaveRadius = KineticTremorsPerk.ShockwaveRadiusTiles * 16f;

        public override string Texture => "Terraria/Images/MagicPixel";

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.hide = true;
            Projectile.timeLeft = 600;
        }

        public override void OnSpawn(IEntitySource source)
        {
            if (Projectile.ai[1] <= 0f)
                Projectile.ai[1] = KineticTremorsPerk.PulseCount;
            if (Projectile.ai[0] <= 0f)
                Projectile.ai[0] = KineticTremorsPerk.InitialDelayTicks;
            if (Projectile.localAI[0] <= 0f)
                Projectile.localAI[0] = KineticTremorsPerk.PulseIntervalTicks;
        }

        public override void AI()
        {
            if (Projectile.ai[1] <= 0f)
            {
                Projectile.Kill();
                return;
            }

            if (Projectile.ai[0] > 0f)
            {
                Projectile.ai[0]--;
                return;
            }

            Pulse();
            Projectile.ai[1]--;
            if (Projectile.ai[1] <= 0f)
            {
                Projectile.Kill();
                return;
            }

            Projectile.ai[0] = Projectile.localAI[0];
        }

        private void Pulse()
        {
            Vector2 center = Projectile.Center;
            int pulseIndex = (int)(KineticTremorsPerk.PulseCount - Projectile.ai[1]);

            if (!Main.dedServ)
                KineticShockwaveRenderer.TriggerPulse(Projectile.GetSource_FromThis(), center, pulseIndex);

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;
            float radiusSq = ShockwaveRadius * ShockwaveRadius;
            int damage = Projectile.damage;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy())
                    continue;

                if (Vector2.DistanceSquared(center, npc.Center) > radiusSq)
                    continue;

                int direction = npc.Center.X < center.X ? -1 : 1;
                npc.SimpleStrikeNPC(damage, direction, false, 0f);
            }
        }
    }
}
