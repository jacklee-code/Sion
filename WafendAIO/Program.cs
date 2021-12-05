using System;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using WafendAIO.Champions;
using WafendAIO.Models;


namespace WafendAIO
{
    internal class Program
    {

        [STAThread]
        private static void Main(string[] args)
        {
            GameEvent.OnGameLoad += OnGameLoad;
        }

        private static void OnGameLoad()
        {
            switch (GameObjects.Player.CharacterName)
            {
                case "Sion":
                    Sion.initializeSion();
                    break;
                case "Galio": 
                    GalioTest.initializeGalio();
                    break;
                default:
                    Console.WriteLine($"[WafendAIO]: {GameObjects.Player.CharacterName} not supported!");
                    Game.Print("<font size='26'><font color='#9999CC'>WafendAIO</font></font> <font color='#FF5640'> Champion not supported</font>");
                    return;
            }
            
            Console.WriteLine($"[WafendAIO]: {GameObjects.Player.CharacterName} loaded");
            Game.Print("<font size='26'><font color='#9999CC'>WafendAIO</font></font> <font color='#FF5640'> Loaded Successfully</font>");
            
        }
    }
}