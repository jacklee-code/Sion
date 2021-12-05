using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using log4net.ObjectRenderer;
using SharpDX;
using WafendAIO.Libraries;
using Color = System.Drawing.Color;
using static WafendAIO.Models.Champion;
using static WafendAIO.Champions.Helpers;


namespace WafendAIO.Champions
{
    public class GalioTest
    {

        public static Vector3 pos;
        public static Vector3 testPos = new Vector3(6232, 11398, 56.35131f);
        public static Spell hexflash;
        public static Geometry.Circle hexflashCircle = new Geometry.Circle(ObjectManager.Player.Position, 400);
        public static void initializeGalio()
        {
            Q = new Spell(SpellSlot.Q, 825);

            hexflash = new Spell(SpellSlot.Summoner2, 400);
            hexflash.SetCharged("SummonerFlashPerksHextechFlashtraptionV2", "SummonerFlashPerksHextechFlashtraptionV2", 200, 400, 2.5f);


            Config = new Menu("Galio", "[Wafend.Galio]", true);

            var menuExploit = new Menu("exploitSettings", "Exploit")
            {
                new MenuKeyBind("qExp", "Toggle Exploit", Keys.U, KeyBindType.Press),
                new MenuKeyBind("hexFlash", "Toggle Hexflash", Keys.M, KeyBindType.Press)
            };
            Config.Add(menuExploit);
            Config.Attach();


            GameEvent.OnGameTick += OnGameUpdate;
            Drawing.OnDraw += OnDraw;

        }
        

    

        private static void OnDraw(EventArgs args)
        {
            if (pos != null && pos != Vector3.Zero)
            {
                Drawing.DrawText(100, 265, Color.White, pos + "");
            }
            Drawing.DrawCircle(testPos, 30, Color.Pink);
            hexflashCircle.Draw(Color.Pink, 5);
        }

        private static void OnGameUpdate(EventArgs args)
        {
            hexflashCircle.Center = (Vector2) ObjectManager.Player.Position;
            hexflashCircle.UpdatePolygon();
            
            if (Config["exploitSettings"].GetValue<MenuKeyBind>("qExp").Active)
            {
                pos = ObjectManager.Player.Position;
                Q.Cast(ObjectManager.Player.Position + 0.5f);
            }
            
            if (Config["exploitSettings"].GetValue<MenuKeyBind>("hexFlash").Active)
            {
                if (hexflashCircle.IsInside(testPos))
                {
                    hexflash.StartCharging(testPos);
                }
                else
                {
                    Game.Print("Point not in range");
                }
                
            }
        }
    }
}