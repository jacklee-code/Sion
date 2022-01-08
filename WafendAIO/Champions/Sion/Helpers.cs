using System;
using System.Collections.Generic;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using WafendAIO.Libraries;
using static WafendAIO.Champions.Sion;
using static WafendAIO.Models.Champion;

namespace WafendAIO.Champions
{
    public static class Helpers
    {
        
        //Values from  https://leagueoflegends.fandom.com/wiki/Sion/LoL#Details_
        //Damages increases every 0.25 seconds
        private static readonly double[] MinQdmg = {30, 50, 70, 90, 110};
        private static readonly double[] MinQadPercentage = {45, 52.5, 60, 67.5, 75};

        private static readonly double[] MaxQdmg = {70, 135, 200, 265, 330};
        private static readonly double[] MaxQadPercentage = {135, 157.5, 180, 202.5, 225};
        
        //Deprecated, using particles now
            /*public static bool isQKnockup()
            {
                return Q.IsCharging && Math.Abs(Game.Time - Q.ChargeRequestSentTime) >= 0.925;
                
            }*/

        public static bool hitByE(this AIBaseClient target)
        {
            return target.HasBuff("sionearmorshred");
        }

        public static void resetQ()
        {
            CurrentRec = null;
            QTarg = null;
            IntersectArr = null;
            HitByR = false;
            StunBuff = null;
            IsKnockUp = false;
        }
        
   
        public static bool isW2Ready()
        {
            return W.IsReady() && ObjectManager.Player.HasBuff("sionwshieldstacks");
        }

        public static double getQDamage(AIBaseClient target)
        {
            double dmg;
            var level = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).Level - 1;

            if (level == -1) return 0;
            var minQRawDmg =  MinQdmg[level] + (ObjectManager.Player.TotalAttackDamage * (MinQadPercentage[level]/100)); //t = 0 
            if (Q.IsCharging)
            {
                
                var maxQRawDmg =  MaxQdmg[level] + (ObjectManager.Player.TotalAttackDamage * (MaxQadPercentage[level]/100)); //t = 2
                var dmgIncreaseStep = (maxQRawDmg - minQRawDmg) / 8; // 2 / 0.25 = 8 --> Difference / 8 as there are damage tiers
                
                var chargeDmg = minQRawDmg + (dmgIncreaseStep * (getQChargeTime() / 0.25));

                //Calculate dmg (enemy armor, lethality and other factors...)
                dmg = ObjectManager.Player.CalculateDamage(target, DamageType.Physical, chargeDmg);
            }
            else
            {
                dmg = ObjectManager.Player.CalculateDamage(target, DamageType.Physical, minQRawDmg);
            }

            var targ = target as AIHeroClient;

            
            
            if (Config["killstealSettings"].GetValue<MenuBool>("includeIncDmgKillsteal").Enabled)
            {
                dmg = targ != null ?  dmg += OktwCommon.GetIncomingDamage((AIHeroClient) target) : dmg *= 1.5;
            }
            else
            {
                dmg = targ != null ?  dmg : dmg *= 1.5;

            }
            //TODO -10 is a random value --> need to find more accurate way on how to get exact charge time to calculate the damage properly
            return dmg - 10;
            
        }

        public static double getQChargeTime()
        {
            return (Game.Time - Q.ChargedCastedTime / 1000);
        }

        public static IEnumerable<AttackableUnit> getEntitiesInQ()
        {
            if (CurrentRec == null || !Q.IsCharging) return null;

            return GameObjects.AttackableUnits.Where(x => !x.IsDead && x.IsTargetable && x.Team != ObjectManager.Player.Team && MaxRec.IsInside(x.Position));
        }

        public static void printDebugMessage(Object message)
        {
            if (Config["miscSettings"].GetValue<MenuBool>("printDebug").Enabled)
            {
                Game.Print(message + "");
            }
            
        }

        public static IEnumerable<AttackableUnit> getAttackableUnitsInAaRange()
        {
            return GameObjects.AttackableUnits.Where(x => x.IsValid && x.IsVisibleOnScreen && x.InAutoAttackRange());
        }
        
        public static void handlePossibleInterrupt(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (Config["combatSettings"].GetValue<MenuBool>("qBeforeInterrupt").Enabled && Q.IsCharging)
            {
                var entities = getEntitiesInQ().Any(x => x.Type == GameObjectType.AIHeroClient);
                //Check if we are charging our Q and if there is an AiHeroClient Entitiy in our Q
                if (entities)
                {
                    printDebugMessage("Detected possible Spell that can interrupt us");
                    Q.ShootChargedSpell(ObjectManager.Player.Position);
                }
            }

        }


    }
}