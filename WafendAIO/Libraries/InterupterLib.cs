using System;
using System.Collections.Generic;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;

namespace WafendAIO.Libraries
{
  public class IntterupterLib
  {
    // Decompiled with JetBrains decompiler
    // Type: EnsoulSharp.SDK.Interrupter
    // Assembly: EnsoulSharp.SDK, Version=1.0.0.0, Culture=neutral, PublicKeyToken=bcecb37b3b5656dd
    // MVID: 50B4C6F4-885D-413C-897A-5025CD7C3714
    // Assembly location: C:\Users\Pascal\Desktop\EnsoulSharp2\Reference\EnsoulSharp.SDK.dll


    public static class Interrupter
    {
      public static readonly Dictionary<int, InterruptSpellDB> CastingSpell =
        new Dictionary<int, InterruptSpellDB>();

      public static readonly Dictionary<string, List<InterruptSpellDB>> SpellDatabase =
        new Dictionary<string, List<InterruptSpellDB>>();

      static Interrupter()
      {
        addInterruptSpellInDb("MasterYi", SpellSlot.W, DangerLevel.Low);
        addInterruptSpellInDb("Amumu", SpellSlot.Q, DangerLevel.High);
        addInterruptSpellInDb("AurelionSol", SpellSlot.E, DangerLevel.High);
        addInterruptSpellInDb("Rakan", SpellSlot.W, DangerLevel.Medium);
        addInterruptSpellInDb("Nautilus", SpellSlot.Q, DangerLevel.High);
        addInterruptSpellInDb("Corki", SpellSlot.W, DangerLevel.Medium);
        addInterruptSpellInDb("Diana", SpellSlot.E, DangerLevel.Medium);
        addInterruptSpellInDb("Alistar", SpellSlot.W, DangerLevel.High);
        addInterruptSpellInDb("Sion", SpellSlot.Q, DangerLevel.Low);
        addInterruptSpellInDb("FiddleSticks", SpellSlot.W, DangerLevel.Medium);
        addInterruptSpellInDb("Janna", SpellSlot.R, DangerLevel.Medium);
        addInterruptSpellInDb("Lucian", SpellSlot.R, DangerLevel.Medium);
        addInterruptSpellInDb("Pantheon", SpellSlot.R, DangerLevel.Medium);
        addInterruptSpellInDb("Quinn", SpellSlot.R, DangerLevel.Medium);
        addInterruptSpellInDb("Quinn", SpellSlot.E, DangerLevel.Medium);
        addInterruptSpellInDb("Shen", SpellSlot.R, DangerLevel.Medium);
        addInterruptSpellInDb("TahmKench", SpellSlot.R, DangerLevel.Medium);
        addInterruptSpellInDb("TwistedFate", SpellSlot.R, DangerLevel.Medium);
        addInterruptSpellInDb("Varus", SpellSlot.Q, DangerLevel.Medium);
        addInterruptSpellInDb("Vi", SpellSlot.Q, DangerLevel.Medium);
        addInterruptSpellInDb("Ezreal", SpellSlot.E, DangerLevel.Medium);
        addInterruptSpellInDb("Xerath", SpellSlot.Q, DangerLevel.Medium);
        addInterruptSpellInDb("Zac", SpellSlot.E, DangerLevel.Medium);
        addInterruptSpellInDb("Caitlyn", SpellSlot.R, DangerLevel.High);
        addInterruptSpellInDb("FiddleSticks", SpellSlot.R, DangerLevel.High);
        addInterruptSpellInDb("Galio", SpellSlot.R, DangerLevel.High);
        addInterruptSpellInDb("Jhin", SpellSlot.R, DangerLevel.High);
        addInterruptSpellInDb("Karthus", SpellSlot.R, DangerLevel.High);
        addInterruptSpellInDb("Katarina", SpellSlot.R, DangerLevel.High);
        addInterruptSpellInDb("Malzahar", SpellSlot.R, DangerLevel.High);
        addInterruptSpellInDb("MissFortune", SpellSlot.R, DangerLevel.High);
        addInterruptSpellInDb("Nunu", SpellSlot.R, DangerLevel.High);
        addInterruptSpellInDb("Velkoz", SpellSlot.R, DangerLevel.High);
        addInterruptSpellInDb("Warwick", SpellSlot.R, DangerLevel.High);
        addInterruptSpellInDb("Xerath", SpellSlot.R, DangerLevel.High);
        addInterruptSpellInDb("Samira", SpellSlot.R, DangerLevel.High);
        addInterruptSpellInDb("Sett", SpellSlot.E, DangerLevel.High);
        addInterruptSpellInDb("TahmKench", SpellSlot.W, DangerLevel.High);
        addInterruptSpellInDb("Zac", SpellSlot.E, DangerLevel.Medium);
        foreach (AIHeroClient enemyHero in GameObjects.EnemyHeroes)
          addInterruptSpellInDb(enemyHero.CharacterName, SpellSlot.Unknown, DangerLevel.High);
        Game.OnUpdate += OnUpdate;
        AIBaseClient.OnDoCast += OnDoCast;
      }

      public static event InterrupterSpellHandler OnInterrupterSpell;

      private static void OnUpdate(EventArgs args)
      {
        foreach (AIHeroClient aiHeroClient in GameObjects.EnemyHeroes.Where(
          h => h != null && h.IsValid && (CastingSpell.ContainsKey(h.NetworkId) && !h.Spellbook.IsChanneling) && !h.Spellbook.IsCharging && !h.Spellbook.IsCastingSpell))
          
          CastingSpell.Remove(aiHeroClient.NetworkId);
        if (OnInterrupterSpell == null)
          return;
        foreach (InterruptSpellArgs args1 in GameObjects.EnemyHeroes
          .Select(
            getSpell)
          .Where(h => h != null))
          OnInterrupterSpell(args1.Sender, args1);
      }

      public static bool isCastingImporantSpell(AIHeroClient unit) => !(unit == null) &&
                                                                      unit.IsValid &&
                                                                      SpellDatabase.ContainsKey(
                                                                        unit.CharacterName) && SpellDatabase[unit.CharacterName]
                                                                        .Any(
                                                                          x =>
                                                                            unit.GetLastCastedSpell() != null &&
                                                                            (unit.GetSpellSlot(unit
                                                                               .GetLastCastedSpell().Name) ==
                                                                             x.SpellSlot &&
                                                                             x.SpellSlot != SpellSlot.Unknown ||
                                                                             unit.GetLastCastedSpell().Name ==
                                                                             "SummonerTeleport" &&
                                                                             x.SpellSlot == SpellSlot.Unknown) &&
                                                                            Variables.GameTimeTickCount -
                                                                            (double) unit.GetLastCastedSpell()
                                                                              .EndTime < 350.0);

      private static void addInterruptSpellInDb(
        string champName,
        SpellSlot slot,
        DangerLevel dangerLevel)
      {
        if (!SpellDatabase.ContainsKey(champName))
          SpellDatabase.Add(champName, new List<InterruptSpellDB>());
        SpellDatabase[champName].Add(new InterruptSpellDB
        {
          SpellSlot = slot
        });
      }

      private static void OnDoCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
      {
        if (sender == null || !sender.IsValid ||
            (sender.Type != GameObjectType.AIHeroClient || CastingSpell.ContainsKey(sender.NetworkId)) ||
            !SpellDatabase.ContainsKey(sender.CharacterName))
          return;
        InterruptSpellDB interruptSpellDb = SpellDatabase[sender.CharacterName].Find(
          o =>
          {
            if (o.SpellSlot == args.Slot)
              return true;
            return o.SpellSlot == SpellSlot.Unknown && args.SData.Name == "SummonerTeleport";
          });
        if (interruptSpellDb == null)
          return;
        CastingSpell.Add(sender.NetworkId, interruptSpellDb);
      }

      private static InterruptSpellArgs getSpell(AIHeroClient target)
      {
        if (!target.IsValid || target.IsDead || !CastingSpell.ContainsKey(target.NetworkId))
          return null;
        InterruptSpellDB interruptSpellDb = CastingSpell[target.NetworkId];
        return new InterruptSpellArgs
        {
          Sender = target
        };
      }

      public delegate void InterrupterSpellHandler(
        AIHeroClient sender,
        InterruptSpellArgs args);

      public enum DangerLevel
      {
        High,
        Medium,
        Low,
      }

      public class InterruptSpellDB
      {
        public Interrupter.DangerLevel DangerLevel { get; internal set; }

        public SpellSlot SpellSlot { get; internal set; }
      }

      public class InterruptSpellArgs
      {
        public AIHeroClient Sender { get; set; }

        public SpellSlot Slot { get; internal set; }

        public Interrupter.DangerLevel DangerLevel { get; internal set; }

        public float EndTime { get; internal set; }
      }
    }
  }
}   
