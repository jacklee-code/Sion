using System;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using SharpDX;
using WafendAIO.Libraries;
using Color = System.Drawing.Color;
using static WafendAIO.Models.Champion;


namespace WafendAIO.Champions
{
    public class Sion
    {
        
        //Values from  https://leagueoflegends.fandom.com/wiki/Sion/LoL#Details_
        //Damages increases every 0.25 seconds
        public static double[] minQdmg = {30, 50, 70, 90, 110};
        public static double[] minQadPercentage = {45, 52.5, 60, 67.5, 75};
        
        public static double[] maxQdmg = {70, 135, 200, 265, 330};
        public static double[] maxQadPercentage = {135, 157.5, 180, 202.5, 225};
        
        
        private static bool hitByR;
        private static BuffInstance stunBuff;
        private static AIBaseClient rEnemy;
        private static EnsoulSharp.SDK.Geometry.Rectangle qRec;
        private static Geometry.Rectangle ePath;
        private static Geometry.Rectangle collisionLine;
        private static EnsoulSharp.SDK.Geometry.Rectangle qTempRec;
        private static EnsoulSharp.SDK.Geometry.Rectangle qMaxRec;
        private static AIBaseClientProcessSpellCastEventArgs qArgs;
        private static AIHeroClient qTarg;
        private static float qGameTime;
        private static IntersectionResult[] intersectArr;
        private static int tick;
        private static Items.Item prowlersClaw;
        private static Items.Item collector;

        
        public static void initializeSion()
        {

            if (ObjectManager.Player.CharacterName != "Sion")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 815);
            Q.SetCharged("SionQ", "SionQ", 500, 775, 0.75f);
            Q.Width = 200;

            W = new Spell(SpellSlot.W, 525);

            E = new Spell(SpellSlot.E, 800);
            E.SetSkillshot(0.25f, 80, 1800, false, SpellType.Line, HitChance.Medium);


            R = new Spell(SpellSlot.R);


            collector = new Items.Item(ItemId.The_Collector, 0);
            prowlersClaw = new Items.Item(ItemId.Prowlers_Claw, 500);

            Config = new Menu("Sion ", "[Wafend.Sion]", true);


            var menuCombat = new Menu("combatSettings", "Combat");
            menuCombat.Add(new MenuList("qMode", "Q Modes",
                new[] {"Fast KnockUp Release", "Maximum Q Charge"})).Permashow();
            Config.Add(menuCombat);

            var menuFarm = new Menu("farmSettings", "Farm")
            {
                new MenuBool("lastHitWithE", "Last hit with E", false)
            };
            Config.Add(menuFarm);

            var menuExploits = new Menu("exploitSettings", "Exploit")
            {
                new MenuBool("lockOnSelectedTarget", "Lock on Selected Target?", false),
                new MenuBool("autoQAfterUlt", "Auto Q after Ult", false),
                new MenuBool("breakSpellShield", "Try to break Spellshield before hitting ult", false),
                new MenuList("ultMode", "R Exploit Mode",  new[] {"Follow Mouse Target", "Follow Selected Target", "Lag Mouse Target", "Beast Mode "})
            };
            menuExploits.Add(new MenuList("ultMode", "R Exploit Mode",
                    new[] {"Follow Mouse Target", "Follow Selected Target", "Lag Mouse Target", "Beast Mode "}))
                .Permashow();
            Config.Add(menuExploits);


            var menuKillsteal = new Menu("killstealSettings", "Killsteal")
            {
                new MenuBool("qKillsteal", "Q Killsteal", false),
                new MenuBool("wKillsteal", "W Killsteal", false),
                new MenuBool("eKillsteal", "E Killsteal", false)
            };
            Config.Add(menuKillsteal);

            var menuMisc = new Menu("miscSettings", "Miscellaneous")
            {
                new MenuBool("useCollector", "Use Collector if Killable", false)
            };
            Config.Add(menuMisc);

            var menuDrawing = new Menu("drawingSettings", "Drawings")
            {
                new MenuBool("drawChampRadius", "Draw Champ Radius", false),
                new MenuBool("drawMinionRadius", "Draw Minion Radius", false),
                new MenuBool("drawQRectangle", "Draw Q Rectangle", false),
                new MenuBool("drawQRange", "Draw Q Range", false),
                new MenuBool("drawERange", "Draw E Range", false),
                new MenuBool("drawEPath", "Draw E Collision Path", false),
                new MenuSlider("champRadius", "Cursor Radius Champion", 500, 100, 1000),
                new MenuSlider("minionRadius", "Cursor Radius Minion", 500, 100, 1000)
            };
            Config.Add(menuDrawing);

            Config.Attach();

            GameEvent.OnGameTick += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            AIBaseClient.OnBuffAdd += OnBuffGain;
            AIBaseClient.OnBuffRemove += OnBuffLose;
            AIBaseClient.OnProcessSpellCast += OnProcessSpellCast;
            IntterupterLib.Interrupter.OnInterrupterSpell += OnPossibleInterrupt;
            AIBaseClient.OnNewPath += OnNewPath;

        }

        #region Events

        private static void OnGameUpdate(EventArgs args)
        {
            if (ObjectManager.Player.IsDead)
            {
                return;
            }

            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.LastHit:
                    lastHitWithE();
                    break;
                case OrbwalkerMode.Harass:
                    harass();
                    break;
            }
    
            if (lagFree(0))
            {
                logicQ();
            }
            
            if (lagFree(1))
            {
                killsteal();
            }


            R_Exploit();
            if (hitByR)
            {
                ccChainAfterUlt();
            }


            tick++;

            if (tick == 2)
            {
                tick = 0;
            }
        }



        private static void OnNewPath(AIBaseClient sender, AIBaseClientNewPathEventArgs args)
        {
            if (sender != null && args != null && sender.Type == GameObjectType.AIHeroClient && sender.IsEnemy &&
                sender.IsVisibleOnScreen && qTempRec != null && qTempRec.IsInside(sender))
            {
                if (args.Path.Length == 2)
                {
                    if (args.IsDash)
                    {
                        Game.Print("Intercepting Dash");
                        Q.ShootChargedSpell(sender.Position);
                        return;
                    }



                    Vector3 currentPosition = args.Path[0];
                    Vector3 desiredPath = args.Path[1];


                    Vector2 p1 = qRec.Points[0];
                    Vector2 p2 = qRec.Points[1];
                    Vector2 p3 = qRec.Points[2];
                    Vector2 p4 = qRec.Points[3];

                    IntersectionResult p1P2 = p1.Intersection(p2, currentPosition.ToVector2(), desiredPath.ToVector2());
                    IntersectionResult p1P4 = p1.Intersection(p4, currentPosition.ToVector2(), desiredPath.ToVector2());
                    IntersectionResult p2P3 = p2.Intersection(p3, currentPosition.ToVector2(), desiredPath.ToVector2());
                    IntersectionResult p3P4 = p3.Intersection(p4, currentPosition.ToVector2(), desiredPath.ToVector2());

                    intersectArr = new[] {p1P2, p1P4, p2P3, p3P4};
                    qTarg = (AIHeroClient) sender;

                }
            }
        }

        private static void OnPossibleInterrupt(AIHeroClient sender,
            Libraries.IntterupterLib.Interrupter.InterruptSpellArgs args)
        {
            if (Q.IsCharging && isQKnockup() && qTempRec.IsInside(sender))
            {
                Game.Print("Interrupter");
                Q.ShootChargedSpell(args.Sender.Position);
            }
        }

        private static void OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && args.Slot == SpellSlot.Q)
            {
                qGameTime = Game.Time;
                qArgs = args;
                qRec = new Geometry.Rectangle(args.Start, args.Start.Extend(args.End, Q.Range), Q.Width);
                qMaxRec = new Geometry.Rectangle(args.Start, args.Start.Extend(args.End, Q.ChargedMaxRange), Q.Width);
                qTempRec = qRec;
                //Get enemy in our Q Rectangle with the Q being on full range
                var possibleTarget = GameObjects.EnemyHeroes.Where(x => x.IsVisibleOnScreen && qMaxRec.IsInside(x));
                var aiHeroClients = possibleTarget as AIHeroClient[] ?? possibleTarget.ToArray();
                if (aiHeroClients.Any())
                {
                    qTarg = aiHeroClients.FirstOrDefault();
                }
            }
        }


        private static void OnBuffLose(AIBaseClient sender, AIBaseClientBuffRemoveEventArgs args)
        {
            if (sender.IsMe && args.Buff.Name.Equals("SionQ"))
            {
                //Resetting Q as we fired it
                resetQ();
            }

            if (!sender.IsValidTarget() || !sender.IsVisibleOnScreen)
            {
                return;
            }

            if (rEnemy != null && sender.Equals(rEnemy) && args.Buff.Name.Equals("Stun") && Q.IsCharging)
            {
                Game.Print("Releasing Q ; OnBuffLose");
                Q.ShootChargedSpell(sender.Position);
                rEnemy = null;
            }

        }

        private static void OnBuffGain(AIBaseClient sender, AIBaseClientBuffAddEventArgs args)
        {

            if (!sender.IsValidTarget() || !sender.IsVisibleOnScreen)
            {
                return;
            }

            if (args.Buff.Name.Equals("sionrtarget") && !Q.IsCharging &&
                Config["exploitSettings"].GetValue<MenuBool>("autoQAfterUlt").Enabled)
            {
                Game.Print("Started Charging Q ; OnBuffGain");
                hitByR = true;
                rEnemy = sender;
            }


        }

        private static void OnDraw(EventArgs args)
        {

            if (Config["drawingSettings"].GetValue<MenuBool>("drawChampRadius").Enabled)
            {
                Drawing.DrawCircle(Game.CursorPos, Config["drawingSettings"].GetValue<MenuSlider>("champRadius").Value,
                    Color.Red);
            }

            if (Config["drawingSettings"].GetValue<MenuBool>("drawMinionRadius").Enabled)
            {
                Drawing.DrawCircle(Game.CursorPos, Config["drawingSettings"].GetValue<MenuSlider>("minionRadius").Value,
                    Color.Blue);
            }

            if (Config["drawingSettings"].GetValue<MenuBool>("drawMinionRadius").Enabled)
            {
                Drawing.DrawCircle(Game.CursorPos, Config["drawingSettings"].GetValue<MenuSlider>("minionRadius").Value,
                    Color.Blue);
            }

            if (Config["drawingSettings"].GetValue<MenuBool>("drawQRectangle").Enabled && qRec != null)
            {
                qRec.Draw(Color.Blue);
            }

            if (Config["drawingSettings"].GetValue<MenuBool>("drawQRange").Enabled && qRec != null)
            {
                Drawing.DrawCircle(ObjectManager.Player.Position, Q.Range, Color.Red);
            }

            if (Config["drawingSettings"].GetValue<MenuBool>("drawERange").Enabled)
            {
                Drawing.DrawCircle(ObjectManager.Player.Position, E.Range, Color.Red);
            }

            if (Config["drawingSettings"].GetValue<MenuBool>("drawEPath").Enabled && ePath != null)
            {
                ePath.Draw(Color.Red, 2);
                ePath = null;
            }
            

            if (Config["drawingSettings"].GetValue<MenuBool>("drawEPath").Enabled && collisionLine != null)
            {
                collisionLine.Draw(Color.Coral, 5);
                collisionLine = null;
            }

            if (qRec != null)
            {
                Drawing.DrawText(Drawing.WorldToScreen(qRec.Points[0].ToVector3()), Color.Aqua, "Point 1");
                Drawing.DrawText(Drawing.WorldToScreen(qRec.Points[1].ToVector3()), Color.Aqua, "Point 2");
                Drawing.DrawText(Drawing.WorldToScreen(qRec.Points[2].ToVector3()), Color.Aqua, "Point 3");
                Drawing.DrawText(Drawing.WorldToScreen(qRec.Points[3].ToVector3()), Color.Aqua, "Point 4");

            }
        }

        #endregion

        #region qLogic

        private static void logicQ()
        {
            if (Q.IsCharging && qTarg != null)
            {
                qRec.End = (Vector2) ObjectManager.Player.Position.Extend(qTempRec.End, Q.ChargedMaxRange);
                qRec.UpdatePolygon();

                if (Config["combatSettings"].GetValue<MenuList>("qMode").Index == 0)
                {
                    //Fast Knockup Release

                    if (isQKnockup() && qRec.IsInside(qTarg))
                    {
                        Game.Print("Fast Knockup Release");
                        Q.ShootChargedSpell(qTarg.Position);
                    }

                    intersectQ();
                }
                else if (Config["combatSettings"].GetValue<MenuList>("qMode").Index == 1 && qTarg != null)
                {
                    //Maximum Charge Release
                    UltModes.tryBreakSpellShield();
                    intersectQ();

                }

            }
        }

        private static void intersectQ()
        {
            if (intersectArr != null)
            {
                foreach (var intersect in intersectArr)
                {
                    if (intersect.Intersects)
                    {
                        //Game.Print(intersect.Point.Distance(qTarg).ToString());
                        if (intersect.Point.Distance(qTarg) <= qTarg.BoundingRadius - 5)
                        {
                            Q.ShootChargedSpell(qTarg.Position);
                            Game.Print("Releasing Q as Enemy approached Intersection Point");
                        }
                    }
                }
            }
        }


        private static void ccChainAfterUlt()
        {
            var target = rEnemy;
            if (target is null || !target.IsValidTarget() || !target.IsVisibleOnScreen || !target.IsEnemy ||
                target.Type != GameObjectType.AIHeroClient)
            {
                return;
            }

            releaseQAfterUltNoFlash((AIHeroClient) target);
        }

        private static void releaseQAfterUltNoFlash(AIBaseClient target)
        {
            if (target.HasBuff("sionrtarget") && Q.IsReady() && !Q.IsCharging)
            {
                E.Cast(target);
                stunBuff = target.GetBuff("sionrtarget");
                Game.Print("Started Charging Q");
                Q.StartCharging(target.Position);

            }

            if (Q.IsCharging && hitByR && stunBuff != null && (Game.Time - stunBuff.EndTime) >= -0.1)
            {
                Game.Print("Releasing Q on Target: " + target.CharacterName + " ; Differential: " +
                           ((Game.Time - stunBuff.EndTime)));
                Q.ShootChargedSpell(target.Position);
                resetQ();
            }

        }

        #endregion

        #region ks



        private static void killsteal()
        {
            var enemies = GameObjects.EnemyHeroes.Where(x => x != null && x.IsVisibleOnScreen && x.IsValidTarget() && !x.IsInvulnerable && !x.IsDead);

            foreach (AIHeroClient enemyHero in enemies)
            {
                //W
                var enemyHealth = collector.IsOwned() ? enemyHero.Health * 0.95 : enemyHero.Health * 1;
                
                if (Config["killstealSettings"].GetValue<MenuBool>("wKillsteal").Enabled && isW2Ready() && enemyHero.DistanceToPlayer() <= W.Range &&
                    OktwCommon.GetKsDamage(enemyHero, W) >= enemyHealth)
                {
                    Game.Print("Killstealing with W");
                    W.Cast(enemyHero);
                }
                
                //Q
                if (getQDamage(enemyHero) >= enemyHealth && Config["killstealSettings"].GetValue<MenuBool>("qKillsteal").Enabled )
                {
                    var targetPos = enemyHero.Position;
                    if (Q.IsCharging && qRec != null && qRec.IsInside(targetPos))
                    {
                        Game.Print("Killstealing with Q Charge");
                        Q.ShootChargedSpell(targetPos);
                    }
                    else
                    {
                        if (targetPos.DistanceToPlayer() <= 500 && Q.IsReady())
                        {
                            Game.Print("No Charge Q Ks");
                            Q.StartCharging(targetPos);
                            Q.ShootChargedSpell(targetPos);
                        }

                    }
                }
                //E
                if (E.IsReady() && OktwCommon.GetKsDamage(enemyHero, E) >= enemyHealth &&
                    Config["killstealSettings"].GetValue<MenuBool>("eKillsteal").Enabled)
                {
                    try
                    {
                        var target = enemyHero;
                        collisionLine = new Geometry.Rectangle(ObjectManager.Player.Position, target.Position, E.Width);

                        if (target.DistanceToPlayer() <= E.Range)
                        {
                            var pOutE = E.GetPrediction(target);
                            if (E.CanCast(target) && pOutE.Hitchance == HitChance.High)
                            {
                                Game.Print("Direct Hit with E");
                                E.Cast(pOutE.CastPosition);
                            }
                        }
                        else
                        {
                            //Enemy ist aus unserer E Range
                            var possibleKnockBackEntities = GameObjects.AttackableUnits.Where(x =>
                                x.IsValidTarget() && x.Type == GameObjectType.AIMinionClient &&
                                x.DistanceToPlayer() <= E.Range);

                            //possible things we can knockback

                            if (possibleKnockBackEntities.Any() && possibleKnockBackEntities != null)
                            {
                                foreach (AttackableUnit k in possibleKnockBackEntities)
                                {
                                    //Iterate through the knockBackEntities

                                    if (collisionLine.IsInside(k))
                                    {
                                        //Is there a collision between the direct line between us and the target and the knockBackEntities?

                                        var predOut = E.GetPrediction(target, false, 1350f,
                                            new[] {CollisionObjects.Heroes, CollisionObjects.YasuoWall});
                                        //GetPrediction of Knockback Range (1350)
                                        if (predOut.Hitchance == HitChance.High)
                                        {
                                            if (collisionLine.IsInside(predOut.CastPosition))
                                            {
                                                //Is the CastPosition in our Collision Line? So does the CastPosition use a collision?
                                                Game.Print("Collision Predict");
                                                E.Cast(predOut.CastPosition);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        Game.Print("Error in E Killsteal");
                    }
                    
                }

                if (prowlersClaw.IsOwned() && prowlersClaw.IsReady && prowlersClaw.IsInRange(enemyHero) &&
                    enemyHealth <= ObjectManager.Player.GetAutoAttackDamage(enemyHero) && enemyHero.DistanceToPlayer() >= ObjectManager.Player.GetCurrentAutoAttackRange())
                {
                    prowlersClaw.Cast(enemyHero);
                    Orbwalker.Attack(enemyHero);
                    Game.Print("Killable with Prowler + AA");
                }

            }
        }
        

        #endregion


        #region R_Exploit

        private static void R_Exploit()
        {
            if (GameObjects.Player.HasBuff("SionR"))
            {

                switch (Config["exploitSettings"].GetValue<MenuList>("ultMode").Index)
                {
                    case 0:
                        UltModes.Sion_R_Exploit_Target_Nearest_Mouse();
                        break;
                    case 1:
                        UltModes.Sion_R_Exploit_Target_Selected();
                        break;
                    case 2:
                        UltModes.Sion_R_Lag_Mouse();
                        break;
                    case 3:
                        UltModes.Sion_R_Experiment();
                        break;
                }
            }
        }

        #endregion


        #region helpers

        private static bool isQKnockup()
        {
            if (Q.IsCharging && qArgs != null && Math.Abs(Game.Time - qGameTime) >= 0.925)
            {
                //Game.Print("Knockup");
                return true;
            }

            return false;
        }

        private static void resetQ()
        {
            qRec = null;
            qTempRec = null;
            qArgs = null;
            qTarg = null;
            intersectArr = null;
            hitByR = false;
            stunBuff = null;
        }

        private static bool lagFree(int offset)
        {
            return tick == offset;
        }

        private static bool isW2Ready()
        {
            return W.IsReady() && ObjectManager.Player.HasBuff("sionwshieldstacks");
        }

        private static double getQDamage(AIBaseClient target)
        {
            double dmg;
            var level = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).Level - 1;
            if (Q.IsCharging)
            {
                var minQRawDmg =  minQdmg[level] + (ObjectManager.Player.TotalAttackDamage * (minQadPercentage[level]/100)); //t = 0 
                var maxQRawDmg =  maxQdmg[level] + (ObjectManager.Player.TotalAttackDamage * (maxQadPercentage[level]/100)); //t = 2
                var dmgIncreaseStep = (maxQRawDmg - minQRawDmg) / 8; // 2 / 0.25 = 8 --> Difference / 8 as there are damage tiers
               
                
                var chargeDmg = minQRawDmg + (dmgIncreaseStep * ((Game.Time - Q.ChargedCastedTime / 1000) / 0.25));
                
                //Calculate dmg (enemy armor, lethality and other factors...)
                dmg = ObjectManager.Player.CalculateDamage(target, DamageType.Physical, chargeDmg) + OktwCommon.GetIncomingDamage((AIHeroClient) target);
            }
            else
            {
                var rawDmg = minQdmg[level] + (ObjectManager.Player.TotalAttackDamage * (minQadPercentage[level]/100));
                dmg = ObjectManager.Player.CalculateDamage(target, DamageType.Physical, rawDmg) + OktwCommon.GetIncomingDamage((AIHeroClient) target);
            }
            
            return dmg;


        }
        #endregion

        private static void lastHitWithE()
        {
            if (Config["farmSettings"].GetValue<MenuBool>("lastHitWithE").Enabled && E.IsReady())
            {
                var possibleLastHits =
                    GameObjects.EnemyMinions.Where(x => !x.IsDead && x.IsVisibleOnScreen && E.GetDamage(x) >= x.Health)
                        .OrderBy(x => x.DistanceToPlayer());

                var closestMinion = possibleLastHits.FirstOrDefault();

                if (closestMinion != null &&
                    closestMinion.DistanceToPlayer() <= E.Range) //closestMinion.DistanceToPlayer() >= 500
                {
                    //Is the closest minion even in the range to knockback
                    
                    ePath = new Geometry.Rectangle(ObjectManager.Player.Position,
                        ObjectManager.Player.Position.Extend(closestMinion.Position, 1350), E.Width);
                    
                    var possibleKnockBackEntities = GameObjects.AttackableUnits.Where(x =>
                        x.IsValidTarget() && x.Type == GameObjectType.AIMinionClient &&
                        x.DistanceToPlayer() > E.Range && x.DistanceToPlayer() <= 1350 && E.GetDamage((AIBaseClient) x) >= x.Health);

                    //possible things we can knockback

                    var knockBackEntities = possibleKnockBackEntities as AttackableUnit[] ?? possibleKnockBackEntities.ToArray();
                    if (knockBackEntities.Any() && possibleKnockBackEntities != null)
                    {
                        foreach (AttackableUnit k in knockBackEntities)
                        {
                            //Iterate through the knockBackEntities

                            if (ePath.IsInside(k))
                            {
                                //Is there a collision between the direct line between us and the target and the knockBackEntities?
                                Game.Print("Last Hitting");
                                E.Cast(k.Position);
                            }
                        }
                    }
                }
            }
        }

        
        
        private static void harass()
        {
            var enemies = GameObjects.EnemyHeroes.Where(x =>
                x.IsValidTarget() && x.IsVisibleOnScreen && x.DistanceToPlayer() <= 1350);
            
            if (E.IsReady() && enemies.Any())
            {
                var target = enemies.FirstOrDefault();
                if (target == null) return;
                collisionLine = new Geometry.Rectangle(ObjectManager.Player.Position, target.Position, E.Width);

                if (target.DistanceToPlayer() <= E.Range)
                {
                    PredictionOutput pOutE = E.GetPrediction(target);
                    if (E.CanCast(target) && pOutE.Hitchance == HitChance.High)
                    {
                        Game.Print("Direct Harass Hit with E");
                        E.Cast(pOutE.CastPosition);
                    }
                }
                else
                {
                    //Enemy ist aus unserer E Range
                    var possibleKnockBackEntities = GameObjects.AttackableUnits.Where(x =>
                        x.IsValidTarget() && x.Type == GameObjectType.AIMinionClient &&
                        x.DistanceToPlayer() <= E.Range);

                    //possible things we can knockback

                    var knockBackEntities = possibleKnockBackEntities as AttackableUnit[] ?? possibleKnockBackEntities.ToArray();
                    if (knockBackEntities.Any() && possibleKnockBackEntities != null)
                    {
                        foreach (AttackableUnit k in knockBackEntities)
                        {
                            //Iterate through the knockBackEntities

                            if (collisionLine.IsInside(k))
                            {
                                //Is there a collision between the direct line between us and the target and the knockBackEntities?

                                var predOut = E.GetPrediction(target, false, 1350f,
                                    new[] {CollisionObjects.Heroes, CollisionObjects.YasuoWall});
                                //GetPrediction of Knockback Range (1350)
                                if (predOut.Hitchance == HitChance.High)
                                {
                                    if (collisionLine.IsInside(predOut.CastPosition))
                                    {
                                        //Is the CastPosition in our Collision Line? So does the CastPosition use a collision?
                                        Game.Print("Collision Harass");
                                        E.Cast(predOut.CastPosition);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

       
    }
}