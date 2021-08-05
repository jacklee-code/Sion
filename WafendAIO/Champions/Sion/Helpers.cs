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
        
        
        public static bool isQKnockup()
        {
            return Q.IsCharging && Math.Abs(Game.Time - QCastGameTime) >= 0.925;
            
        }

        public static bool hitByE(this AIBaseClient target)
        {
            return target.HasBuff("sionearmorshred");
        }

        public static void resetQ()
        {
            Rec = null;
            TempRec = null;
            QTarg = null;
            IntersectArr = null;
            HitByR = false;
            StunBuff = null;
        }
        
        public static bool lagFree(int offset)
        {
            return Tick == offset;
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
            
            if (Q.IsCharging)
            {
                var minQRawDmg =  MinQdmg[level] + (ObjectManager.Player.TotalAttackDamage * (MinQadPercentage[level]/100)); //t = 0 
                var maxQRawDmg =  MaxQdmg[level] + (ObjectManager.Player.TotalAttackDamage * (MaxQadPercentage[level]/100)); //t = 2
                var dmgIncreaseStep = (maxQRawDmg - minQRawDmg) / 8; // 2 / 0.25 = 8 --> Difference / 8 as there are damage tiers
                
                var chargeDmg = minQRawDmg + (dmgIncreaseStep * ((Game.Time - Q.ChargedCastedTime / 1000) / 0.25));

                //Calculate dmg (enemy armor, lethality and other factors...)
                dmg = ObjectManager.Player.CalculateDamage(target, DamageType.Physical, chargeDmg);
            }
            else
            {
                var rawDmg = MinQdmg[level] + (ObjectManager.Player.TotalAttackDamage * (MinQadPercentage[level]/100));
                dmg = ObjectManager.Player.CalculateDamage(target, DamageType.Physical, rawDmg);
            }

            var targ = target as AIHeroClient;
            if (targ != null)
            {
                dmg += OktwCommon.GetIncomingDamage((AIHeroClient) target);
            }
            else
            {
                dmg *= 1.5;
            }

            return dmg - 10;
            
        }

        public static IEnumerable<AttackableUnit> getEntitiesInQ()
        {
            if (Rec == null || !Q.IsCharging) return null;

            return GameObjects.AttackableUnits.Where(x => !x.IsDead && x.IsTargetable && x.Team != ObjectManager.Player.Team && Rec.IsInside(x.Position));
        }

        public static void printDebugMessage(Object message)
        {
            if (Config["miscSettings"].GetValue<MenuBool>("printDebug").Enabled)
            {
                Game.Print(message + "");
            }
            
        }
    }
}