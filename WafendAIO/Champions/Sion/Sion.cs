using System;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using SharpDX;
using WafendAIO.Libraries;
using Color = System.Drawing.Color;
using static WafendAIO.Models.Champion;
using static WafendAIO.Champions.Helpers;


namespace WafendAIO.Champions
{
    public class Sion
    {
        public static bool HitByR;
        public static BuffInstance StunBuff;
        public static AIBaseClient REnemy;
        public static EnsoulSharp.SDK.Geometry.Rectangle Rec;
        public static Geometry.Rectangle EPath;
        public static Geometry.Rectangle CollisionLine;
        public static EnsoulSharp.SDK.Geometry.Rectangle TempRec;
        public static EnsoulSharp.SDK.Geometry.Rectangle MaxRec;
        public static AIHeroClient QTarg;
        public static float QCastGameTime;
        public static IntersectionResult[] IntersectArr;
        public static int Tick;
        public static Items.Item ProwlersClaw;
        public static Items.Item Collector;

        
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


            Collector = new Items.Item(ItemId.The_Collector, 0);
            ProwlersClaw = new Items.Item(ItemId.Prowlers_Claw, 500);

            Config = new Menu("Sion", "[Wafend.Sion]", true);


            var menuCombat = new Menu("combatSettings", "Combat");
            menuCombat.Add(new MenuList("qMode", "Q Modes",
                new[] {"Fast KnockUp Release", "Maximum Q Charge"})).Permashow();
            menuCombat.Add(new MenuBool("qOnExpireStun", "Use Q Before Stun runs out"));
            Config.Add(menuCombat);

            var menuHarass = new Menu("harassSettings", "Harass")
            {
                new MenuBool("useQ", "Use Q", false),
                new MenuBool("useE", "Use E", false)
            };
            Config.Add(menuHarass);

            var menuExploits = new Menu("exploitSettings", "Exploit")
            {
                new MenuBool("lockOnSelectedTarget", "Lock on Selected Target?", false),
                new MenuBool("autoQAfterUlt", "Auto Q after Ult", false),
                new MenuBool("autoWOnUlt", "Auto W on Ult", false),
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
                new MenuBool("eKillsteal", "E Killsteal", false),
                new MenuBool("pcKillsteal", "Prowler + AA Killsteal", false)
            };
            Config.Add(menuKillsteal);

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
            
            var menuDebug = new Menu("debugSettings", "Debug")
            {
                new MenuBool("printDebug", "Print Debug in Chat", false)
            };
            Config.Add(menuDebug);

            Config.Attach();

            GameEvent.OnGameTick += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            AIBaseClient.OnBuffAdd += OnBuffGain;
            AIBaseClient.OnBuffRemove += OnBuffLose;
            AIBaseClient.OnProcessSpellCast += OnProcessSpellCast;
            IntterupterLib.Interrupter.OnInterrupterSpell += OnPossibleInterrupt;
            AIBaseClient.OnIssueOrder += OnIssueOrder;
            AIBaseClient.OnNewPath += OnNewPath;
            AntiGapcloser.OnGapcloser += OnAntiGapcloser;

        }

        private static void OnAntiGapcloser(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        { 
            if (sender != null && !sender.IsMe && Q.IsCharging && isQKnockup() && TempRec.IsInside(sender))
            {
                Game.Print("AntiGapcloser");
                Q.ShootChargedSpell(sender.Position);
            }
        }

        private static void OnGameUpdate(EventArgs args)
        {

            if (ObjectManager.Player.IsDead) return;
            
            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Harass:
                    harass();
                    break;
                default:
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
            if (HitByR)
            {
                ccChainAfterUlt();
            }


            Tick++;

            if (Tick == 2)
            {
                Tick = 0;
            }
            
        }

        private static void OnIssueOrder(AIBaseClient sender, AIBaseClientIssueOrderEventArgs args)
        {
            if (sender != null && sender.IsMe && args.Order == GameObjectOrder.AttackMove && sender.HasBuff("SionR"))
            {
                if (UltModes.minionsNearPlayer())
                {
                    //Game.Print("Not processing as minions near us");
                    args.Process = false;
                }
               
            }

            QTarg = (AIHeroClient) sender;
    
        }

        private static void OnNewPath(AIBaseClient sender, AIBaseClientNewPathEventArgs args)
        {
            if (sender != null && args != null && sender.Type == GameObjectType.AIHeroClient && sender.IsEnemy &&
                sender.IsVisibleOnScreen && TempRec != null && TempRec.IsInside(sender))
            {
                if (args.Path.Length == 2)
                {
                    
                    Vector3 currentPosition = args.Path[0];
                    Vector3 desiredPath = args.Path[1];


                    Vector2 p1 = Rec.Points[0];
                    Vector2 p2 = Rec.Points[1];
                    Vector2 p3 = Rec.Points[2];
                    Vector2 p4 = Rec.Points[3];

                    IntersectionResult p1P2 = p1.Intersection(p2, currentPosition.ToVector2(), desiredPath.ToVector2());
                    IntersectionResult p1P4 = p1.Intersection(p4, currentPosition.ToVector2(), desiredPath.ToVector2());
                    IntersectionResult p2P3 = p2.Intersection(p3, currentPosition.ToVector2(), desiredPath.ToVector2());
                    IntersectionResult p3P4 = p3.Intersection(p4, currentPosition.ToVector2(), desiredPath.ToVector2());

                    IntersectArr = new[] {p1P2, p1P4, p2P3, p3P4};
                    QTarg = (AIHeroClient) sender;

                }
            }
        }
        
        private static void OnPossibleInterrupt(AIHeroClient sender,
            Libraries.IntterupterLib.Interrupter.InterruptSpellArgs args)
        {
            if (Q.IsCharging && isQKnockup() && TempRec.IsInside(sender))
            {
                Game.Print("Interrupter");
                Q.ShootChargedSpell(args.Sender.Position);
            }
        }

        private static void OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.Slot == SpellSlot.Q)
                {
                    QCastGameTime = Game.Time;
                    Rec = new Geometry.Rectangle(args.Start, args.Start.Extend(args.End, Q.Range), Q.Width);
                    MaxRec = new Geometry.Rectangle(args.Start, args.Start.Extend(args.End, Q.ChargedMaxRange), Q.Width);
                    TempRec = Rec;
                    //Get enemy in our Q Rectangle with the Q being on full range
                    var possibleTarget = GameObjects.EnemyHeroes.Where(x => x.IsVisibleOnScreen && MaxRec.IsInside(x));
                    var aiHeroClients = possibleTarget as AIHeroClient[] ?? possibleTarget.ToArray();
                    if (aiHeroClients.Any())
                    {
                        QTarg = aiHeroClients.FirstOrDefault();
                    }
                } else if (args.Slot == SpellSlot.R && Config["exploitSettings"].GetValue<MenuBool>("autoWOnUlt").Enabled && W.IsReady() && !ObjectManager.Player.HasBuff("sionwshieldstacks"))
                {
                    W.Cast();
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

            if (REnemy != null && sender.Equals(REnemy) && args.Buff.Name.Equals("Stun") && Q.IsCharging)
            {
                Game.Print("Releasing Q ; OnBuffLose");
                Q.ShootChargedSpell(sender.Position);
                REnemy = null;
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
                HitByR = true;
                REnemy = sender;
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

            if (Config["drawingSettings"].GetValue<MenuBool>("drawQRectangle").Enabled && Rec != null)
            {
                Rec.Draw(Color.Blue);
            }

            if (Config["drawingSettings"].GetValue<MenuBool>("drawQRange").Enabled && Rec != null)
            {
                Drawing.DrawCircle(ObjectManager.Player.Position, Q.Range, Color.Red);
            }

            if (Config["drawingSettings"].GetValue<MenuBool>("drawERange").Enabled)
            {
                Drawing.DrawCircle(ObjectManager.Player.Position, E.Range, Color.Red);
            }

            if (Config["drawingSettings"].GetValue<MenuBool>("drawEPath").Enabled && EPath != null)
            {
                EPath.Draw(Color.Red, 2);
                EPath = null;
            }
            

            if (Config["drawingSettings"].GetValue<MenuBool>("drawEPath").Enabled && CollisionLine != null)
            {
                CollisionLine.Draw(Color.Coral, 5);
                CollisionLine = null;
            }

            if (Rec != null)
            {
                Drawing.DrawText(Drawing.WorldToScreen(Rec.Points[0].ToVector3()), Color.Aqua, "Point 1");
                Drawing.DrawText(Drawing.WorldToScreen(Rec.Points[1].ToVector3()), Color.Aqua, "Point 2");
                Drawing.DrawText(Drawing.WorldToScreen(Rec.Points[2].ToVector3()), Color.Aqua, "Point 3");
                Drawing.DrawText(Drawing.WorldToScreen(Rec.Points[3].ToVector3()), Color.Aqua, "Point 4");

            }
        }
        

        #region qLogic

        private static void logicQ()
        {
            var possibleQTarget = TargetSelector.GetTarget(1500, DamageType.Physical);
            
            if (Q.IsCharging )
            {
                if (QTarg != null)
                {
                    //There is a target inside of our Q
                    Rec.End = (Vector2) ObjectManager.Player.Position.Extend(TempRec.End, Q.ChargedMaxRange);
                    Rec.UpdatePolygon();

                    if (Config["combatSettings"].GetValue<MenuList>("qMode").Index == 0)
                    {
                        //Fast Knockup Release

                        if (isQKnockup() && Rec.IsInside(QTarg))
                        {
                            Game.Print("Fast Knockup Release");
                            Q.ShootChargedSpell(QTarg.Position);
                        }

                        intersectQ();
                    }
                    else if (Config["combatSettings"].GetValue<MenuList>("qMode").Index == 1)
                    {
                        //Maximum Charge Release
                        UltModes.tryBreakSpellShield();
                        intersectQ();

                    }
                }
                else
                {
                    //No target in our Q -> Check if people walk in
                    if (MaxRec != null && possibleQTarget != null && MaxRec.IsInside(possibleQTarget))
                    {
                        Game.Print("Detected possible Q Target");
                        QTarg = possibleQTarget;
                    }
                }
               

            }
        }

        private static void intersectQ()
        {
            if (IntersectArr != null)
            {
                foreach (var intersect in IntersectArr)
                {
                    if (intersect.Intersects)
                    {
                        //Game.Print(intersect.Point.Distance(qTarg).ToString());
                        if (intersect.Point.Distance(QTarg) <= QTarg.BoundingRadius - 5)
                        {
                            Q.ShootChargedSpell(QTarg.Position);
                            Game.Print("Releasing Q as Enemy approached Intersection Point");
                        }
                    }
                }
            }
        }


        private static void ccChainAfterUlt()
        {
            var target = REnemy;
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
                StunBuff = target.GetBuff("sionrtarget");
                Game.Print("Started Charging Q");
                Q.StartCharging(target.Position);

            }

            if (Config["combatSettings"].GetValue<MenuBool>("qOnExpireStun").Enabled && Q.IsCharging && HitByR && StunBuff != null && (Game.Time - StunBuff.EndTime) >= -0.1)
            {
                Game.Print("Releasing Q on Target: " + target.CharacterName + " ; Differential: " + ((Game.Time - StunBuff.EndTime)));
                Q.ShootChargedSpell(target.Position);
                resetQ();
            }

        }

        #endregion

        #region ks
        private static void killsteal()
        {
            var enemies = GameObjects.EnemyHeroes.Where(x => x != null && x.IsVisibleOnScreen && x.IsValidTarget() && !x.IsInvulnerable && !x.HasBuffOfType(BuffType.UnKillable) && !x.IsDead);

            foreach (AIHeroClient enemyHero in enemies)
            {
                //W
                var enemyHealth = Collector.IsOwned() ? enemyHero.Health * 0.95 : enemyHero.Health * 1;
                
                if (Config["killstealSettings"].GetValue<MenuBool>("wKillsteal").Enabled && isW2Ready() && enemyHero.DistanceToPlayer() <= W.Range && OktwCommon.GetKsDamage(enemyHero, W) >= enemyHealth)
                {
                    Game.Print("Killstealing with W");
                    W.Cast(enemyHero);
                }
                
                //Q
                if (getQDamage(enemyHero) >= enemyHealth && Config["killstealSettings"].GetValue<MenuBool>("qKillsteal").Enabled )
                {
                    var targetPos = enemyHero.Position;
                    if (Q.IsCharging && Rec != null && Rec.IsInside(targetPos))
                    {
                        Game.Print("Killstealing with Q Charge");
                        Q.ShootChargedSpell(targetPos);
                    }
                    else
                    {
                        if (targetPos.DistanceToPlayer() <= 500 && Q.IsReady() && !ObjectManager.Player.HasBuff("SionQ"))
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
                        CollisionLine = new Geometry.Rectangle(ObjectManager.Player.Position, enemyHero.Position, E.Width);

                        if (enemyHero.DistanceToPlayer() <= E.Range)
                        {
                            var pOutE = E.GetPrediction(enemyHero);
                            if (E.CanCast(enemyHero) && pOutE.Hitchance == HitChance.High)
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

                            var knockBackEntities = possibleKnockBackEntities as AttackableUnit[] ?? possibleKnockBackEntities.ToArray();
                            if (knockBackEntities.Any() && possibleKnockBackEntities != null)
                            {
                                foreach (var k in knockBackEntities)
                                {
                                    //Iterate through the knockBackEntities

                                    if (CollisionLine.IsInside(k))
                                    {
                                        //Is there a collision between the direct line between us and the target and the knockBackEntities?

                                        var predOut = E.GetPrediction(enemyHero, false, 1350f,
                                            new[] {CollisionObjects.Heroes, CollisionObjects.YasuoWall});
                                        //GetPrediction of Knockback Range (1350)
                                        if (predOut.Hitchance == HitChance.High)
                                        {
                                            if (CollisionLine.IsInside(predOut.CastPosition))
                                            {
                                                //Is the CastPosition of the prediction in our Collision Line? In other words: does the CastPosition use a collision?
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

                if (Config["killstealSettings"].GetValue<MenuBool>("pcKillsteal").Enabled && ProwlersClaw.IsOwned() && ProwlersClaw.IsReady && ProwlersClaw.IsInRange(enemyHero) &&
                    enemyHealth <= ObjectManager.Player.GetAutoAttackDamage(enemyHero) && enemyHero.DistanceToPlayer() >= ObjectManager.Player.GetCurrentAutoAttackRange())
                {
                    ProwlersClaw.Cast(enemyHero);
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
        

        private static void lastHitWithE()
        {
            
            //kind of useless? took out for now
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
                    
                    EPath = new Geometry.Rectangle(ObjectManager.Player.Position,
                        ObjectManager.Player.Position.Extend(closestMinion.Position, 1350), E.Width);
                    
                    var possibleKnockBackEntities = GameObjects.AttackableUnits.Where(x =>
                        x.IsValidTarget() && x.Type == GameObjectType.AIMinionClient &&
                        x.DistanceToPlayer() > E.Range && x.DistanceToPlayer() <= 1350 && E.GetDamage((AIBaseClient) x) >= x.Health);

                    //possible things we can knockback

                    var knockBackEntities = possibleKnockBackEntities as AttackableUnit[] ?? possibleKnockBackEntities.ToArray();
                    if (knockBackEntities.Any())
                    {
                        foreach (var k in knockBackEntities)
                        {
                            //Iterate through the knockBackEntities

                            if (EPath.IsInside(k))
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
            if (Config["harassSettings"].GetValue<MenuBool>("useE").Enabled)
            {
                var enemies = GameObjects.EnemyHeroes.Where(x =>
                x.IsValidTarget() && x.IsVisibleOnScreen && x.DistanceToPlayer() <= 1350);

                var aiHeroClients = enemies as AIHeroClient[] ?? enemies.ToArray();
                if (E.IsReady() && aiHeroClients.Any())
                {
                    var target = aiHeroClients.FirstOrDefault();
                    if (target == null) return;
                    CollisionLine = new Geometry.Rectangle(ObjectManager.Player.Position, target.Position, E.Width);

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
                        if (knockBackEntities.Any())
                            
                        {
                            foreach (var k in knockBackEntities)
                            {
                                //Iterate through the knockBackEntities

                                if (CollisionLine.IsInside(k))
                                {
                                    //Is there a collision between the direct line between us and the target and the knockBackEntities?

                                    var predOut = E.GetPrediction(target, false, 1350f,
                                        new[] {CollisionObjects.Heroes, CollisionObjects.YasuoWall});
                                    //GetPrediction of Knockback Range (1350)
                                    if (predOut.Hitchance == HitChance.High)
                                    {
                                        if (CollisionLine.IsInside(predOut.CastPosition))
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


            if (Config["harassSettings"].GetValue<MenuBool>("useQ").Enabled)
            {
                var qtarget = TargetSelector.GetTarget(500, DamageType.Physical);

                if (qtarget == null) return;

                if (Q.IsReady() && !Q.IsCharging)
                {
                    if (qtarget.IsDashing() && qtarget.GetDashInfo() != null)
                    {
                        Game.Print("Enemy is dashing -> Casting Q On End Pos");
                        Q.StartCharging(qtarget.GetDashInfo().EndPos);
                    }
                    else
                    {
                        //TODO check if enemy has dash ready if yes dont cast Q
                        //Basically only Cast if we are not in danger
                        Q.StartCharging(qtarget.Position);
                    }
                    QTarg = qtarget;
                }
            }

            
            

        }

       
    }
}