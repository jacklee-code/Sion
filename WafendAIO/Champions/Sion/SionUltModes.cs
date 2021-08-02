using System.Collections.Generic;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using WafendAIO.Models;
using static WafendAIO.Champions.Helpers;

namespace WafendAIO.Champions
{
    public static class UltModes
    {
        public static void Sion_R_Exploit_Target_Nearest_Mouse()
        {
            
            var target = GameObjects.AttackableUnits.Where(x => x.IsValidTarget() && !x.Name.Contains("Turret") && !x.Name.Contains("Inhib") && Game.CursorPos.Distance(x.Position) <= 500).OrderBy(x => Game.CursorPos.Distance(x.Position) <= 500);
            foreach (var targ in target)
            {
                
                ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, targ);
            }
            
        }

        public static void Sion_R_Exploit_Target_Selected()
        {
            var target = TargetSelector.SelectedTarget;
            if (target is null)
            {
                target = TargetSelector.GetTarget(3000, DamageType.Physical);
            }
                        
            if (target is null) return;
                        
            ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, target);
        }

        public static void Sion_R_Lag_Mouse()
        {
            var targ = GameObjects.AttackableUnits.FirstOrDefault(x => x != null && x.IsValid && x.Position.DistanceToCursor() < 500);
            if (targ is null)
            {
                ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, ObjectManager.Player);
                return;
            }
            
            ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, targ);

        }
        public static void Sion_R_Experiment()
        {
            if (Champion.Config["breakSpellShield"].GetValue<MenuBool>().Enabled)
            {
                tryBreakSpellShield();
            }
            
            if (Champion.Config["exploitSettings"].GetValue<MenuBool>("lockOnSelectedTarget").Enabled && TargetSelector.SelectedTarget != null)
            {
                ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, TargetSelector.SelectedTarget);
            }
            else
            {
                IEnumerable<AIHeroClient> possibleHeroes = GameObjects.EnemyHeroes
                .Where(x => x.IsValidTarget() &&
                            Game.CursorPos.Distance(x.Position) <= Champion.Config["champRadius"].GetValue<MenuSlider>().Value)
                .OrderBy(x => Game.CursorPos.Distance(x.Position));
                var aiHeroClients = possibleHeroes.ToList();
            
                if (aiHeroClients.Any())
                {
                    var target = aiHeroClients.FirstOrDefault();
                    if (target != null)
                    {
                        //Game.Print("Targeting Champion: " + target.Name);
                        ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, target);
            
                    }
            
                }
                else
                {
                    IEnumerable<AIMinionClient> possibleEnemyMinions = GameObjects.EnemyMinions
                        .Where(x => x.IsValidTarget() &&
                                    Game.CursorPos.Distance(x.Position) <= Champion.Config["minionRadius"].GetValue<MenuSlider>().Value)
                        .OrderBy(x => Game.CursorPos.Distance(x.Position));
            
                    var aiMinionClients = possibleEnemyMinions.ToList();
                    if (aiMinionClients.Any())
                    {
                        var target = aiMinionClients.FirstOrDefault();
                        if (target != null)
                        {
                            //Game.Print("Targeting Minion");
                            ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, target);
            
                        }
                    }
                    else
                    {
                        //Cursor is not near any champions nor any Minions --> Follow Mouse Cursor
                        ObjectManager.Player.IssueOrder(GameObjectOrder.AttackMove, ObjectManager.Player.Position + (Game.CursorPos - ObjectManager.Player.Position));
            
                    }
                }
            }
            
            //ObjectManager.Player.IssueOrder(GameObjectOrder.AttackMove, ObjectManager.Player.Position + (Game.CursorPos - ObjectManager.Player.Position));


        }

        private static bool enemyHasRemovableSpellShield(this AIBaseClient target)
        {
            return target.HasBuffOfType(BuffType.SpellShield) || target.HasBuff("malzaharpassiveshield");
        }

        public static void tryBreakSpellShield()
        {
            IEnumerable<AIHeroClient> possibleHeroes = GameObjects.EnemyHeroes
                .Where(x => x.IsValidTarget() && x.DistanceToPlayer() <= Champion.W.Range && 
                            x.enemyHasRemovableSpellShield());
            
            if (possibleHeroes.Any() && isW2Ready())
            {
                Game.Print("Breaking Spellshield");
                Champion.W.Cast(possibleHeroes.FirstOrDefault());
            }

        }
        
        public static bool minionsNearPlayer()
        {
            return GameObjects.AttackableUnits.Any(x => x.IsValidTarget() && x.Team != GameObjectTeam.Neutral &&!x.Name.Contains("Plant") && x.DistanceToPlayer() < 760);
        }
        
        
    }
}