﻿using LBoL.Base;
using LBoL.ConfigData;
using LBoL.Core;
using LBoL.Core.Battle;
using LBoL.Core.StatusEffects;
using LBoL_Doremy.DoremyChar.SE;
using LBoL_Doremy.RootTemplates;
using LBoLEntitySideloader.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace LBoL_Doremy.DoremyChar.Cards.Uncommon
{
    public sealed class DoremyHypnagogicEffluviumDef : DCardDef
    {
        public override CardConfig PreConfig()
        {
            var con = DefaultConfig();
            con.Rarity = Rarity.Uncommon;

            con.Type = LBoL.Base.CardType.Skill;
            con.TargetType = TargetType.AllEnemies;

            con.Colors = new List<ManaColor>() { ManaColor.Blue };
            con.Cost = new ManaGroup() { Blue = 1, Any = 2 };


            con.Value1 = 3;
            con.UpgradedValue1 = 5;

            con.Value2 = 22;
            con.UpgradedValue2 = 30;

            con.Keywords = Keyword.Exile;
            con.UpgradedKeywords = Keyword.Exile;


            con.RelativeEffects = new List<string>() { nameof(Weak), nameof(DC_NightmareSE),  };
            con.UpgradedRelativeEffects = new List<string>() { nameof(Weak), nameof(DC_NightmareSE) };

            return con;
        }
    }


    [EntityLogic(typeof(DoremyHypnagogicEffluviumDef))]
    public sealed class DoremyHypnagogicEffluvium : DCard
    {
        protected override IEnumerable<BattleAction> Actions(UnitSelector selector, ManaGroup consumingMana, Interaction precondition)
        {

            foreach (var e in UnitSelector.AllEnemies.GetEnemies(Battle))
            {
                yield return DebuffAction<Weak>(e, duration: Value1);
                yield return DebuffAction<DC_NightmareSE>(e, Value2);
            }
        }

    }
}