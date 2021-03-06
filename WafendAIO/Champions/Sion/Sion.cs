using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Utility;
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
        public static bool IsKnockUp;
        
        public static BuffInstance StunBuff;
        public static AIBaseClient REnemy;
        public static EnsoulSharp.SDK.Geometry.Rectangle CurrentRec;
        public static Geometry.Rectangle CollisionLine;
        public static EnsoulSharp.SDK.Geometry.Rectangle MaxRec;
        public static AIHeroClient QTarg;
        public static IntersectionResult[] IntersectArr;
        public static Items.Item ProwlersClaw;
        public static Items.Item Collector;
        




        public static void initializeSion()
        {
            Q = new Spell(SpellSlot.Q, 815);
            Q.SetCharged("SionQ", "SionQ", 500, 775, 0.55f);
            Q.Width = 200;

            W = new Spell(SpellSlot.W, 525);

            E = new Spell(SpellSlot.E, 800);
            E.SetSkillshot(0.25f, 80, 1800, false, SpellType.Line, HitChance.Medium);


            R = new Spell(SpellSlot.R);


            Collector = new Items.Item(ItemId.The_Collector, 0);
            ProwlersClaw = new Items.Item(ItemId.Prowlers_Claw, 500);

            Config = new Menu("Sion", "[Wafend.Sion]", true);


            var menuCombat = new Menu("combatSettings", "Combat");
            
            var menuQCombat = new Menu("qCombat", "General Q Settings");
            menuQCombat.Add(new MenuList("qMode", "Q Modes", new[] {"Fast knock up release", "Maximum Q charge"})).Permashow();
            menuQCombat.Add(new MenuList("comboMode", "Combo after ult", new[] {"R Hit -> Q", "R Hit -> E -> Q"})).Permashow();
            menuQCombat.Add(new MenuBool("qOnExpireStun", "Use Q before ult stun runs out"));
            menuQCombat.Add(new MenuBool("qBeforeInterrupt", "Use Q before someone cancels it"));
            menuQCombat.Add(new MenuBool("preventQCancel", "Prevent Orbwalker from canceling Q"));
            menuCombat.Add(menuQCombat);

            var menuCombo = new Menu("comboSettings", "Combo");
            menuCombo.Add(new MenuBool("useQCombo", "Use Q"));
            menuCombo.Add(new MenuBool("useECombo", "Use E"));
            
            menuCombat.Add(menuCombo);
            
            Config.Add(menuCombat);

            var menuHarass = new Menu("harassSettings", "Harass")
            {
                new MenuBool("useEHarass", "Use E", false)
            };
            Config.Add(menuHarass);

            var menuExploits = new Menu("exploitSettings", "Exploit")
            {
                new MenuBool("lockOnSelectedTarget", "Lock on Selected Target?", false),
                new MenuBool("autoQAfterUlt", "Auto Q after Ult", false),
                new MenuBool("autoWOnUlt", "Auto W on Ult", false),
                new MenuBool("breakSpellShield", "Try to break Spellshield before hitting ult", false),
                
            };
            var blacklistSubMenu = new Menu("blacklistUlt", "Ult Blacklist");
            foreach (AIHeroClient enemyHero in GameObjects.EnemyHeroes)
            {
                blacklistSubMenu.Add(new MenuBool(enemyHero.CharacterName, "Use on " + enemyHero.CharacterName));
            }
            menuExploits.Add(blacklistSubMenu);
            menuExploits.Add(new MenuList("ultMode", "R Exploit Mode", new[] {"Follow Mouse Target", "Follow Selected Target", "Lag Mouse Target", "Beast Mode", "Fully Manual Steer"})).Permashow();
            Config.Add(menuExploits);
            
            var menuKillsteal = new Menu("killstealSettings", "Killsteal");
            menuKillsteal.Add(new MenuKeyBind("enableKillsteal", "Enable Killsteal", Keys.M, KeyBindType.Toggle))
                .Permashow();
            menuKillsteal.Add(new MenuBool("qKillsteal", "Q Killsteal", false));
            menuKillsteal.Add(new MenuBool("wKillsteal", "W Killsteal", false));
            menuKillsteal.Add(new MenuBool("eKillsteal", "E Killsteal", false));
            menuKillsteal.Add(new MenuBool("pcKillsteal", "Prowler + AA Killsteal", false));
            menuKillsteal.Add(new MenuBool("includeIncDmgKillsteal", "Include Incoming Damage", false));
            
            Config.Add(menuKillsteal);

            var menuDrawing = new Menu("drawingSettings", "Drawings")
            {
                new MenuBool("drawChampRadius", "Draw Champ Radius", false),
                new MenuBool("drawMinionRadius", "Draw Minion Radius", false),
                new MenuBool("drawQRectangle", "Draw Q Rectangle", false),
                new MenuBool("drawQRange", "Draw Q Range", false),
                new MenuBool("drawERange", "Draw E Range", false),
                new MenuSlider("champRadius", "Cursor Radius Champion", 500, 100, 1000),
                new MenuSlider("minionRadius", "Cursor Radius Minion", 500, 100, 1000)
            };
            Config.Add(menuDrawing);


            var menuMisc = new Menu("miscSettings", "Misc")
            {
                new MenuBool("printDebug", "Print Debug in Chat", false),
                //new MenuBool("eGapCloser", "E on Gapcloser", false),
                new MenuBool("jungleSteal", "Jungle Steal", false),
                //new MenuBool("qGapCloser", "Q on Gapcloser", false)
                new MenuBool("lockCamera", "Disable Camera Lock in Ult", false)
            };
            Config.Add(menuMisc);
            
            

            Config.Attach();

            GameEvent.OnGameTick += OnGameUpdate;
            GameObject.OnCreate += OnCreateGameObject;
            Drawing.OnDraw += OnDraw;
            AIBaseClient.OnBuffAdd += OnBuffGain;
            AIBaseClient.OnDoCast += OnDoCast;
            AIBaseClient.OnBuffRemove += OnBuffLose;
            AIBaseClient.OnProcessSpellCast += OnProcessSpellCast;
            IntterupterLib.Interrupter.OnInterrupterSpell += OnPossibleInterrupt;
            AIBaseClient.OnIssueOrder += OnIssueOrder;
            AIBaseClient.OnNewPath += OnNewPath;
            
        }
        
        #region OnEvents
        
       
        private static void OnGameUpdate(EventArgs args)
        {
            if (ObjectManager.Player.IsDead || ObjectManager.Player.IsRecalling() || MenuGUI.IsChatOpen || ObjectManager.Player.IsWindingUp || ObjectManager.Player.IsZombie()) return;
            
            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    combo();
                    break;
                case OrbwalkerMode.Harass:
                    harass();
                    break;
            }
            
            logicQ();
            killsteal();
            jungleSteal();
            R_Exploit();
            ccChainAfterUlt();
            
        }
        
        private static void OnDoCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender == null || !sender.IsValid || sender.GetType() != typeof(AIHeroClient) ||
                !sender.IsEnemy || !sender.IsVisibleOnScreen)
            {
                return;
            }
            
            //Q Handling
            switch (sender.CharacterName)
            {
                case "Alistar":
                    if (args.Slot == SpellSlot.W && args.Target != null && args.Target.IsValid && args.Target.IsMe)
                    {
                        //Alistar is trying to W us
                        if (Q.IsCharging && IsKnockUp && MaxRec.IsInside(sender.Position))
                        {
                            printDebugMessage("Anti Alistar");
                            Q.ShootChargedSpell(sender.Position);
                        }
                    }
                    break;
               
            }
            
        }

        /*private static void OnAntiGapcloser(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (sender == null || args == null) return; 
            if (OktwCommon.CheckGapcloser(sender, args))
            {
                if (Config["miscSettings"].GetValue<MenuBool>("eGapcloser").Enabled)
                {
                    if (E.IsReady() && sender.IsValidTarget(E.Range))
                    {
                        var pred = E.GetPrediction(sender);
                        if (pred.Hitchance >= HitChance.High)
                        {
                            printDebugMessage("AntiGapcloser E");
                            E.Cast(pred.CastPosition);
                        }
                    }
                }

                if (Config["miscSettings"].GetValue<MenuBool>("qGapcloser").Enabled)
                {
                    if (Q.IsCharging && isQKnockup() && Rec != null && Rec.IsInside(sender))
                    {
                        printDebugMessage("AntiGapcloser Q");
                        Q.ShootChargedSpell(sender.Position);
                    }
                }
                
            }
        }*/
        
        private static void OnCreateGameObject(GameObject sender, EventArgs args)
        {
            
            if (sender.Type.Equals(GameObjectType.EffectEmitter) && sender.Name.Contains("Sion"))
            {
                //Particle appears when Q is ready for knock up
                if (sender.Name.Equals("Sion_Base_Q_Indicator_Red"))
                {
                    IsKnockUp = true;
                }
                    
            }
            
        }

        private static void OnIssueOrder(AIBaseClient sender, AIBaseClientIssueOrderEventArgs args)
        {
            if (sender != null && sender.IsMe)
            {
                if (args.Order == GameObjectOrder.AttackMove && sender.HasBuff("SionR") && UltModes.minionsNearPlayer())
                {
                    //needed so we dont get stuck when we ult through minions
                    args.Process = false;
                }

                if (Config["combatSettings"].GetValue<MenuBool>("preventQCancel").Enabled && Q.IsCharging && args.Order == GameObjectOrder.AttackUnit)
                {
                    //otherwise orbwalker would cancel our Q
                    args.Process = false;
                }
            }
            
        }


        private static void OnNewPath(AIBaseClient sender, AIBaseClientNewPathEventArgs args)
        {
            if (sender != null && args != null && sender.Type == GameObjectType.AIHeroClient && sender.IsEnemy &&
                sender.IsVisibleOnScreen && CurrentRec != null && MaxRec.IsInside(sender))
            {
                if (args.Path.Length == 2)
                {
                    
                    Vector3 currentPosition = args.Path[0];
                    Vector3 desiredPath = args.Path[1];


                    Vector2 p1 = MaxRec.Points[0];
                    Vector2 p2 = MaxRec.Points[1];
                    Vector2 p3 = MaxRec.Points[2];
                    Vector2 p4 = MaxRec.Points[3];

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
            if (Q.IsCharging && IsKnockUp && CurrentRec.IsInside(sender))
            {
                printDebugMessage("Interrupter");
                Q.ShootChargedSpell(args.Sender.Position);
            }
        }

        private static void OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.Slot == SpellSlot.Q)
                {
                    
                    CurrentRec = new Geometry.Rectangle(args.Start, args.Start.Extend(args.To, Q.Range), Q.Width);
                    MaxRec = new Geometry.Rectangle(args.Start, args.Start.Extend(args.To, Q.ChargedMaxRange), Q.Width);
    
                    //Used in Helper class to determine KnockUp
                    //--> using particles works as well and is actually better
                    //BeginChargeTick = Game.Time;
                    
                    //done with particles, chargetick not needed anymore
                    
                    
                    //Get enemy in our Q Rectangle with the Q being on full range
                    var possibleTarget = GameObjects.EnemyHeroes.Where(x => x.IsVisibleOnScreen && MaxRec.IsInside(x));
                    var aiHeroClients = possibleTarget as AIHeroClient[] ?? possibleTarget.ToArray();
                    if (aiHeroClients.Any())
                    {
                        printDebugMessage("Target: " + aiHeroClients.FirstOrDefault().CharacterName);
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
               
                resetQ();
            }

            if (!sender.IsValidTarget() || !sender.IsVisibleOnScreen)
            {
                return;
            }

            if (REnemy != null && sender.Equals(REnemy) && args.Buff.Name.Equals("Stun") && Q.IsCharging && Config["combatSettings"].GetValue<MenuBool>("qOnExpireStun").Enabled)
            {
                printDebugMessage("Releasing Q ; OnBuffLose");
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
            
            if (Config["exploitSettings"].GetValue<MenuBool>("autoQAfterUlt").Enabled)
            {
                if (args.Buff.Name.Equals("sionrtarget") && !Q.IsCharging)
                {
                    printDebugMessage("Found R Target");
                    HitByR = true;
                    REnemy = sender;
                    StunBuff = sender.GetBuff("sionrtarget");
                } 
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

            if (Config["drawingSettings"].GetValue<MenuBool>("drawQRectangle").Enabled && CurrentRec != null)
            {
                CurrentRec.Draw(Color.Chartreuse);
            }

            if (Config["drawingSettings"].GetValue<MenuBool>("drawQRange").Enabled)
            {
                Drawing.DrawCircle(ObjectManager.Player.Position, Q.ChargedMaxRange, Color.Pink);
            }

            if (Config["drawingSettings"].GetValue<MenuBool>("drawERange").Enabled)
            {
                Drawing.DrawCircle(ObjectManager.Player.Position, E.Range, Color.Red);
            }
            
            
        }
        
        #endregion
        
        #region SpellUsagelE
        private static void combo()
        {
            //otherwise it uses
            if (Config["combatSettings"].GetValue<MenuBool>("useQCombo").Enabled && !Q.IsCharging)
            {
                var target = TargetSelector.GetTarget(500, DamageType.Physical);
                if (target != null && target.IsValid)
                {
                    
                    Q.StartCharging(target.Position);
                    //Q logic will do the rest
                }
                
            }
            
            if (Config["combatSettings"].GetValue<MenuBool>("useECombo").Enabled && !Q.IsCharging)
            {
                useE();
            }
        }
        
        private static void harass()
        {
            if (Config["harassSettings"].GetValue<MenuBool>("useEHarass").Enabled)
            {
                useE();
            }
        }
        
        public static void useE()
        {
            if (Config["harassSettings"].GetValue<MenuBool>("useEHarass").Enabled)
            {
                var enemies = GameObjects.EnemyHeroes.Where(x =>
                    x.IsVisibleOnScreen && x.IsValidTarget() && x.IsVisibleOnScreen && x.DistanceToPlayer() <= 1350);

                var aiHeroClients = enemies as AIHeroClient[] ?? enemies.ToArray();
                if (E.IsReady() && aiHeroClients.Any())
                {
                    var target = aiHeroClients.FirstOrDefault();
                    if (target == null) return;
                    CollisionLine = new Geometry.Rectangle(ObjectManager.Player.Position, target.Position, E.Width);

                    if (target.DistanceToPlayer() <= E.Range)
                    {
                        PredictionOutput pOutE = E.GetPrediction(target);
                        DelayAction.Add(100, () =>
                        {
                            if (E.CanCast(target) && pOutE.Hitchance >= HitChance.High)
                            {
                                E.Cast(target);
                                printDebugMessage("Direct Harass Hit with E");
                            }
                        });

                    }
                    else
                    {
                        //Enemy out of our direct E Range --> try through collision
                        var possibleKnockBackEntities = GameObjects.AttackableUnits.Where(x => x.IsVisibleOnScreen &&
                            x.IsValidTarget() && x.Type == GameObjectType.AIMinionClient &&
                            x.DistanceToPlayer() <= E.Range);

                        //possible things we can knockback

                        var knockBackEntities = possibleKnockBackEntities as AttackableUnit[] ??
                                                possibleKnockBackEntities.ToArray();
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
                                    if (predOut.Hitchance >= HitChance.High)
                                    {
                                        if (CollisionLine.IsInside(predOut.CastPosition))
                                        {
                                            //Is the CastPosition in our Collision Line? So does the CastPosition use a collision?
                                            printDebugMessage("Collision Harass");
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

        


        public static void jungleSteal()
        {

            if (Config["miscSettings"].GetValue<MenuBool>("jungleSteal").Enabled)
            {
                var entities = getEntitiesInQ();
                if (entities == null) return;
                var jungleObj = entities.Where(x => x.IsJungle() && x.Health < getQDamage((AIBaseClient) x) && (((AIBaseClient) x).GetJungleType().HasFlag(JungleType.Large) || ((AIBaseClient) x).GetJungleType().HasFlag(JungleType.Legendary)));

                if (jungleObj.Any())
                {
                    var targ = jungleObj.FirstOrDefault();
                    if (targ == null || !Q.CanCast( (AIBaseClient) targ)) return;
                    
                
                    if (Q.IsCharging)
                    {
                        DelayAction.Add(200, () =>
                        {
                            if (!Q.ShootChargedSpell(targ.Position)) return;
                            printDebugMessage("Jungle Charge KS");
                        });

                    } 
                    else
                    {
                        
                        DelayAction.Add(200, () =>
                        {
                            if (!Q.StartCharging(targ.Position) || !Q.ShootChargedSpell(targ.Position)) return;
                            printDebugMessage("Jungle Direct KS");
                        });
                    }
                }
            }
            
        }
        
        #endregion
        
        #region qLogic

        private static void logicQ()
        {
            if (Q.IsCharging && CurrentRec != null)
            {
                CurrentRec.End = CurrentRec.Start.Extend(CurrentRec.End, Q.Range);
                CurrentRec.UpdatePolygon();
                
                if (QTarg != null)
                {
                    //There is a target inside of our Q

                    if (Config["combatSettings"].GetValue<MenuList>("qMode").Index == 0)
                    {
                        //Fast Knockup Release

                        if (IsKnockUp && CurrentRec.IsInside(QTarg))
                        {
                            printDebugMessage("Fast Knockup Release");
                            Q.ShootChargedSpell(QTarg.Position);
                        }
                        
                        //if enemy is trying to leave our Q and no knock up yet, release
                        intersectQ();
                    }
                    else if (Config["combatSettings"].GetValue<MenuList>("qMode").Index == 1)
                    {
                        //Maximum Charge Release
                        if (QTarg.CharacterName.ToLower().Equals("fiora") && QTarg.HasBuff("FioraW"))
                        {
                            
                            if (QTarg.GetBuff("FioraW").EndTime - Game.Time < 2 - getQChargeTime())
                            {
                                //Remaining Time of FioraW Buff is less than the time we could charge our q with 2 being the max time of charge
                                //2 - getQChargeTime gives us the possible reamining time we could charge our Q
                                printDebugMessage("Charging our Q against Parry since we outrun it");
                            }
                            else
                            {
                                //Not possible to charge our Q in time so that FioraW runs out so we cancel our Q
                                var enemiesToCancelWith = getAttackableUnitsInAaRange();
                                if (enemiesToCancelWith.Any()){
                                    printDebugMessage("Cancelling our Q as we cant outcharge the Parry");
                                    
                                    //Otherwise we can't cancel as we are blocking issueorders with this option on
                                    if (Config["combatSettings"].GetValue<MenuBool>("preventQCancel").Enabled)
                                    {
                                        Config["combatSettings"].GetValue<MenuBool>("preventQCancel").Enabled = false;
                                        
                                    }
                                    
                                    ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, enemiesToCancelWith.FirstOrDefault());
                                    
                                    Config["combatSettings"].GetValue<MenuBool>("preventQCancel").Enabled = true;
                                    
                                }
                                else
                                {
                                    //Not sure if there is another way to cancel our Q from charging, didnt test in detail 
                                    printDebugMessage("No Units found to cancel our Q with!");
                                }
                            
                            }
                                
                        }
                        
                        UltModes.tryBreakSpellShield();
                        intersectQ();

                    }
                }
                else
                {
                    //Q is charging but no enemy in it when we charged
                    //No target in our Q -> Check if people walk in
                    var possibleQTarget = TargetSelector.GetTarget(1500, DamageType.Physical);   
                    if (MaxRec != null && QTarg == null && possibleQTarget != null && MaxRec.IsInside(possibleQTarget))
                    {
                        printDebugMessage("Detected possible Q Target");
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
                        //printDebugMessage(intersect.Point.Distance(qTarg).ToString());
                        if (intersect.Point.Distance(QTarg) <= QTarg.BoundingRadius - 15)
                        {
                            Q.ShootChargedSpell(QTarg.Position);
                            printDebugMessage("Releasing Q as Enemy approached Intersection Point");
                        }
                    }
                }
            }
        }


        private static void ccChainAfterUlt()
        {
            if (!HitByR) return;
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
            if (!Q.IsCharging)
            {
                switch (Config["combatSettings"].GetValue<MenuList>("comboMode").Index)
                {
                    case 0:
                        //Ult Hit into Q
                        
                        Q.StartCharging(target.Position);
                        break;
                    case 1:
                        //E into Q
                        if (E.IsReady() && target.IsValidTarget(E.Range))
                        {
                            E.Cast(target);
                        }

                        if (target.hitByE() || !E.IsReady())
                        {
                            //Has been hit by E
                            Q.StartCharging(target.Position);
                        }

                        break;
                }
            }
            if (Config["combatSettings"].GetValue<MenuBool>("qOnExpireStun").Enabled && Q.IsCharging && HitByR &&
                StunBuff != null && (Game.Time - StunBuff.EndTime) >= -0.1)
            {
                printDebugMessage("Releasing Q on Target: " + target.CharacterName + " ; Differential: " +
                           ((Game.Time - StunBuff.EndTime)));
                Q.ShootChargedSpell(target.Position);
                REnemy = null;
            }
        }

        #endregion

        #region ks
        private static void killsteal()
        {
            if (!Config["killstealSettings"].GetValue<MenuKeyBind>("enableKillsteal").Active) return;
            
            var enemies = GameObjects.EnemyHeroes.Where(x => x != null && x.IsVisibleOnScreen && x.IsValidTarget() && !x.HasBuff("UndyingRage") && !x.IsInvulnerable && !x.HasBuffOfType(BuffType.UnKillable) && !x.IsDead && !x.HasBuffOfType(BuffType.Invulnerability));

            foreach (AIHeroClient enemyHero in enemies)
            {
                //W
                var enemyHealth = Collector.IsOwned() ? enemyHero.Health * 0.95 : enemyHero.Health * 1;

                bool useInclude = Config["killstealSettings"].GetValue<MenuBool>("includeIncDmgKillsteal").Enabled;
                
                if (Config["killstealSettings"].GetValue<MenuBool>("wKillsteal").Enabled && isW2Ready() && enemyHero.DistanceToPlayer() <= W.Range && OktwCommon.GetKsDamage(enemyHero, W, useInclude) >= enemyHealth)
                {
                    printDebugMessage("Killstealing with W");
                    W.Cast(enemyHero);
                }
                
                //Q
                if (getQDamage(enemyHero) >= enemyHealth && Config["killstealSettings"].GetValue<MenuBool>("qKillsteal").Enabled && Q.CanCast(enemyHero))
                {
                    if (Q.IsCharging && CurrentRec != null && CurrentRec.IsInside(enemyHero.Position))
                    {
                        DelayAction.Add(100, () =>
                        {
                            if (!Q.ShootChargedSpell(enemyHero.Position)) return;
                            
                            printDebugMessage("Charge Q KS");
                        });
                    }
                    else
                    {
                        if (enemyHero.Position.DistanceToPlayer() <= 500)
                        {
                            
                            DelayAction.Add(100, () =>
                            {
                                if (!Q.StartCharging(enemyHero.Position) || !Q.ShootChargedSpell(enemyHero.Position)) return;
                                printDebugMessage("No Charge Q Ks");
                            });
                            
                        }

                    }
                }
                //E
                if (E.IsReady() && OktwCommon.GetKsDamage(enemyHero, E, useInclude) >= enemyHealth &&
                    Config["killstealSettings"].GetValue<MenuBool>("eKillsteal").Enabled)
                {
                    try
                    {
                        CollisionLine = new Geometry.Rectangle(ObjectManager.Player.Position, enemyHero.Position, E.Width);

                        if (enemyHero.DistanceToPlayer() <= E.Range)
                        {
                            var pOutE = E.GetPrediction(enemyHero);
                            
                            
                            DelayAction.Add(100, () =>
                            {
                                if (!E.CanCast(enemyHero) || pOutE.Hitchance < HitChance.High ||
                                    !E.Cast(pOutE.CastPosition)) return;
                                
                                printDebugMessage("Direct Hit with E");
                                
                            });
                           return;
                        }
                        else
                        {
                            //Enemy ist aus unserer E Range
                            var possibleKnockBackEntities = GameObjects.AttackableUnits.Where(x =>
                               x.IsVisibleOnScreen && x.IsValidTarget() && x.Type == GameObjectType.AIMinionClient &&
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
                                        if (predOut.Hitchance >= HitChance.High)
                                        {
                                            if (CollisionLine.IsInside(predOut.CastPosition))
                                            {
                                                //Is the CastPosition of the prediction in our Collision Line? In other words: does the CastPosition use a collision?
                                                printDebugMessage("Collision Predict");
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
                        printDebugMessage("Error in E Killsteal");
                    }
                    
                }

                if (Config["killstealSettings"].GetValue<MenuBool>("pcKillsteal").Enabled && ProwlersClaw.IsOwned() && ProwlersClaw.IsReady && ProwlersClaw.IsInRange(enemyHero) &&
                    enemyHealth <= ObjectManager.Player.GetAutoAttackDamage(enemyHero) && enemyHero.DistanceToPlayer() >= ObjectManager.Player.GetCurrentAutoAttackRange())
                {
                    ProwlersClaw.Cast(enemyHero);
                    Orbwalker.Attack(enemyHero);
                    printDebugMessage("Killable with Prowler + AA");
                }

            }
        }
        

        #endregion

        


        #region R_Exploit

        private static void R_Exploit()
        {
            if (GameObjects.Player.HasBuff("SionR"))
            {
                if (Config["miscSettings"].GetValue<MenuBool>("lockCamera").Enabled) Hud.SetCameraLockState(false);
                
                
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
                    case 4:
                        UltModes.Sion_R_Fully_Manual_Steer();
                        break;
                }
            }
        }

        #endregion
        
        //kind of useless? took out for now
        /*private static void lastHitWithE()
        {
            
            if (Config["farmSettings"].GetValue<MenuBool>("lastHitWithE").Enabled && E.IsReady())
            {
                var possibleLastHits =
                    GameObjects.EnemyMinions.Where(x => x.IsVisibleOnScreen &&!x.IsDead && E.GetDamage(x) >= x.Health)
                        .OrderBy(x => x.DistanceToPlayer());

                var closestMinion = possibleLastHits.FirstOrDefault();

                if (closestMinion != null &&
                    closestMinion.DistanceToPlayer() <= E.Range) //closestMinion.DistanceToPlayer() >= 500
                {
                    //Is the closest minion even in the range to knockback
                    
                    EPath = new Geometry.Rectangle(ObjectManager.Player.Position,
                        ObjectManager.Player.Position.Extend(closestMinion.Position, 1350), E.Width);
                    
                    var possibleKnockBackEntities = GameObjects.AttackableUnits.Where(x => x.IsVisibleOnScreen && x.IsValidTarget() && x.Type == GameObjectType.AIMinionClient &&
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
                                printDebugMessage("Last Hitting");
                                E.Cast(k.Position);
                            }
                        }
                    }
                }
            }
        }*/
        

       
    }
}