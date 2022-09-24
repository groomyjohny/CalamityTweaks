using Terraria;
using System;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.ID;
using System.Collections.Generic;

namespace CalamityTweaks
{
	public class CalamityTweaks : Mod
	{
	}
}

namespace CalamityTweaks.Enemies
{
    [AutoloadBossHead]
    public class SupremeCnidrion : ModNPC
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Supreme Cnidrion");			
		}

		public override void SetDefaults()
		{
			NPC.width = 365;
			NPC.height = 236;
			NPC.damage = targetDamage_nonPredictiveCharge;
			NPC.defense = 150;
			NPC.lifeMax = 4000000;
			NPC.knockBackResist = 0;			
			NPC.value = Item.buyPrice(platinum: 50);
			NPC.aiStyle = -1;

            NPC.boss = true;
            NPC.noGravity = true;
            NPC.lavaImmune = true;
            NPC.noTileCollide = true;			
        }

		public override void AI()
		{
            NPC.TargetClosestUpgraded();
            this.targetPlayer = Main.player[NPC.target];
            ticksInCurrentPhase++;

			float lifePct = NPC.GetLifePercent();

			if (lifePct > 0.60) SetTargetPhase(1);
			else if (lifePct > 0.30) SetTargetPhase(2);
            //TODO: implement: if clones are alive, enter phase 3
            else SetTargetPhase(4);

            currentAttackTickCounter++;
			int patternDurationTicks = 700;
			int currentPatternTick = ticksInCurrentPhase % patternDurationTicks;

			if (currentPatternTick >= 0 && currentPatternTick < 320) ChargeAttack(80, false);
			else if (currentPatternTick < 520) WaterBoltAttack(50, (float)(20*Math.PI/180), 4);
			else if (currentPatternTick < 700) ChargeAttack(90, true);

            ticksSinceSpawn++;
        }

		protected void SetTargetPhase(int phase)
		{
			prevBossPhase = currBossPhase;
            currBossPhase = phase;

			if (prevBossPhase != currBossPhase) //on phase transition
			{
				ticksInCurrentPhase = 0;

				if (phase == 1) Talk("Long have I waited for this. I've heard rumors about you. Nobody took me seriously, they kept laughing at me, saying I'm a weakling mini-boss! Now, I will prove everyone wrong by defeating you!");
				if (phase == 2)
				{
					Talk("Your performance is surprising. Given how much I worked on myself, you are a powerful opponent. I can respect that, but I have a few tricks too.");
					if (Main.netMode != NetmodeID.MultiplayerClient)
					{
						for (int i = 0; i < 3; ++i)
						{
							spawns.Add(NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + i * 100, (int)NPC.position.Y, ModContent.NPCType<SupremeCnidrionClone>(), 1, ai0: i, ai1: NPC.netID));
						}
					}
				}
				if (phase == 3) Talk("Alright, I'll leave fighting to the little ones for now");
				if (phase == 4) Talk("Time to get serious!");
			}
		}
		protected void ChargeAttack(int targetTickDuration, bool predictive = false)
		{
			int idleTicks = targetTickDuration / 3;

            if (currentAttackTickCounter == idleTicks)
			{
                int chargeTickDuration = targetTickDuration / 3;
                Vector2 targetPos = !predictive ? targetPlayer.Center : targetPlayer.Center + targetPlayer.velocity * (chargeTickDuration*0.5f);
				Vector2 dir = targetPos - NPC.Center;
				Vector2 unitDir = dir.SafeNormalize(Vector2.Zero);

				float dirLen = dir.Length();
				Vector2 chargeTargetPoint = NPC.Center + unitDir * Math.Min(dirLen + 400, 1800);
				
				this.currentChargeVelocity = (chargeTargetPoint - NPC.Center) / chargeTickDuration;
			}

			if (currentAttackTickCounter < idleTicks || currentAttackTickCounter > targetTickDuration - idleTicks) NPC.velocity = Vector2.Zero;
			else
			{
				NPC.velocity = this.currentChargeVelocity;
            }

			if (currentAttackTickCounter == targetTickDuration)
			{
                currentAttackTickCounter = 0;
			}
        }

		protected void WaterBoltAttack(int boltCount, float maxSpreadRadians, int ticksPerBolt)
		{
			NPC.velocity = Vector2.Zero;

			int fullAttackDuration = ticksPerBolt * (boltCount-1);
			if (currentAttackTickCounter > fullAttackDuration)
			{
				currentAttackTickCounter = 0;
				return;
			}

			if (currentAttackTickCounter % ticksPerBolt == 0)
			{
                if (NPC.HasValidTarget && Main.netMode != NetmodeID.MultiplayerClient)
                {
					 
                    var source = NPC.GetSource_FromAI();
                    Vector2 position = NPC.Center;
                    Vector2 targetPosition = targetPlayer.Center;
                    Vector2 direction = targetPosition - position;

					float radius = direction.Length();
					float targetAngle = position.AngleTo(targetPosition);
					float currentSpreadPhase = (float)(currentAttackTickCounter) / fullAttackDuration; //from 0 to 1
					float spreadPhaseAdjusted = currentSpreadPhase - 0.5f; //from -0.5 to 0.5
					float alterationModifier = ((currentAttackTickCounter / ticksPerBolt) % 2) * 2 - 1; //-1 and 1. Makes bolts converge onto the player
					float projectileAngle = targetAngle + maxSpreadRadians * spreadPhaseAdjusted * alterationModifier;

					direction = new Vector2(radius * (float)(Math.Cos(projectileAngle)), radius * (float)Math.Sin(projectileAngle));
                    direction.Normalize();
                    float speed = 16f;
                    int type = ProjectileID.PinkLaser; //TODO: change it to something watery
					int damage = targetDamage_supremeWaterBolt_contact;
                    Projectile.NewProjectile(source, position, direction * speed, type, damage, 0f, Main.myPlayer);
                }
            }
		}

        protected void Talk(string message) //TODO: add localization stuff?
        {
            if (Main.netMode != NetmodeID.Server)
            {
				//string text = Language.GetTextValue("Mods.ExampleMod.NPCTalk", Lang.GetNPCNameValue(npc.type), message);
				string text = message;
                Main.NewText(text, 150, 250, 150);
            }
            else
            {
				//Terraria.Localization.NetworkText text = Terraria.Localization.NetworkText.FromKey("Mods.ExampleMod.NPCTalk", Lang.GetNPCNameValue(npc.type), message);
				Terraria.Localization.NetworkText text = Terraria.Localization.NetworkText.FromLiteral(message);
                Terraria.Chat.ChatHelper.BroadcastChatMessage(text, new Color(150, 250, 150));
            }
        }

        protected int currBossPhase = -1;
        protected int prevBossPhase = -2;
        protected int ticksInCurrentPhase = 0;
		protected int currentAttackTickCounter = 0; //how much ticks from beginning of last attack. Reset it to 0 when attack is completed
		protected int ticksSinceSpawn = 0;
		protected List<int> spawns = new List<int>();

		protected Vector2 currentChargeVelocity;

		protected Player targetPlayer;

		//Damage values are designed for Master Death mode originally (first number) and are scaled appropriately (second number, the multiplier) 
		protected static int targetDamage_nonPredictiveCharge = (int)(1250*0.4);
        protected static int targetDamage_predictiveCharge = (int)(950*0.4);
		protected static int targetDamage_cloneCharge = (int)(850 * 0.4);
        protected static int targetDamage_supremeWaterBolt_contact = (int)(960 * 0.2);
		protected static int targetDamage_supremeWaterBolt_ascending = (int)(780 * 0.2);
		protected static int targetDamage_waterTide = (int)(1250 * 0.2);
		protected static int targetDamage_steamBreath = (int)(1250 * 0.2);
		protected static int targetDamage_waterDeathhail = (int)(840*0.2);
		protected static int targetDamage_predictiveWaterArrow = (int)(910 * 0.2);
    }

    [AutoloadBossHead]
    public class SupremeCnidrionClone : SupremeCnidrion
	{
        protected float orbitRadianOffset;
		protected NPC ownerNpc = null;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Supreme Cnidrion Spawn");
        }
        public override void SetDefaults()
        {
            NPC.width = 365/2;
            NPC.height = 236/2;
			//NPC.damage = (int)(850*0.4);
			NPC.damage = 0;
            NPC.defense = 110;
            NPC.lifeMax = 350000;
            NPC.knockBackResist = 0;
            NPC.value = Item.buyPrice(platinum: 3);
            NPC.aiStyle = -1;

            NPC.boss = true;
            NPC.noGravity = true;
            NPC.lavaImmune = true;
            NPC.noTileCollide = true;

			//ticksInCurrentPhase = (int)(NPC.ai[0] * 100);
			orbitRadianOffset = NPC.ai[0] * 120.0f * (float)Math.PI / 180.0f;			
        }

        public override void AI()
        {
			if (ownerNpc == null) ownerNpc = Main.npc[(int)NPC.ai[1]];

            NPC.TargetClosestUpgraded();
            this.targetPlayer = Main.player[NPC.target];

			ticksInCurrentPhase++;            
            currentAttackTickCounter++;
            int patternDurationTicks = 700; //TODO: decouple spawn's patterns
            int currentPatternTick = ticksInCurrentPhase % patternDurationTicks;

			int orbitTick = ticksSinceSpawn % 600;
			float currentAngle = orbitRadianOffset + orbitTick / 300.0f * (float)Math.PI;
			this.NPC.position = ownerNpc.position + new Vector2(400.0f * (float)Math.Sin(currentAngle), 400.0f * (float)Math.Cos(currentAngle));
			ticksSinceSpawn++;
            //if (currentPatternTick >= 0 && currentPatternTick < 320) ChargeAttack(80, false);

        }
    }

	
}