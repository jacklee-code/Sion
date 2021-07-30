using System.Windows.Forms;
using EnsoulSharp;
using EnsoulSharp.SDK;

namespace WafendAIO.Utility
{
    public static class Fun
    {
        private static Items.Item pinkWard = new Items.Item(2055, 600);
        public static void Spammer()
        {
            var text = Clipboard.GetText() ?? "Gumba";
            Game.Say(text, true);         
        }

        public static void MVP_Exploit()
        {
            pinkWard.Buy();
            ObjectManager.Player.SellItem(0);
        }

        
    }
}