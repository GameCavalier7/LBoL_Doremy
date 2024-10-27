﻿using HarmonyLib;
using LBoL.ConfigData;
using LBoL.Core;
using LBoL.Core.Battle;
using LBoL.Core.Battle.BattleActions;
using LBoL.Core.StatusEffects;
using LBoL.Core.Units;
using LBoL.Presentation.Units;
using LBoL_Doremy.ExtraAssets;
using LBoL_Doremy.RootTemplates;
using LBoLEntitySideloader.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LBoL_Doremy.DoremyChar.SE
{
    public sealed class DC_NightmareSEDef : DStatusEffectDef
    {
        public override StatusEffectConfig PreConfig()
        {
            var con = DefaultConfig();
            con.Type = LBoL.Base.StatusEffectType.Special;

            

            return con;
        }
    }

    [EntityLogic(typeof(DC_NightmareSEDef))]
    public sealed class DC_NightmareSE : DStatusEffect
    {
        protected override void OnAdded(Unit unit)
        {
            NightmareSource = unit;
            ReactOwnerEvent(unit.DamageReceived, DamageReceived, (GameEventPriority)(-99));
            ReactOwnerEvent(unit.HealingReceived, HealingReceived, (GameEventPriority)(-99));

            UpdateOrCreateNightmareBar();

            React(DoKill());
        }

        public readonly static string nightmareBarGoName = "NightmareBar";
        public readonly static string actualHpGoName = "ActualHp";


        public void UpdateOrCreateNightmareBar()
        {
            if (Owner?.View is UnitView view)
            {
                var sw = view._statusWidget;
                var hpBar = sw.hpBar;
                var hpBarGo = hpBar.gameObject;

                var nBarGo = hpBarGo.transform.Find(nightmareBarGoName)?.gameObject;
                GameObject actualHpGo = null;

                if (nBarGo == null)
                {
                    nBarGo = GameObject.Instantiate(hpBarGo.transform.Find("HealthBarHealth").gameObject, hpBarGo.transform, worldPositionStays: true);
                    nBarGo.name = nightmareBarGoName;
                    nBarGo.GetComponent<Image>().sprite = AssetManager.DoremyAssets.purpleBar;

                    //actualHpGo = GameObject.Instantiate(hpBarGo.transform.Find("HealthText.HealthTmp").gameObject, nBarGo.transform);
                    //actualHpGo.name = actualHpGoName;
                }
                else
                {
                    actualHpGo = nBarGo.transform.Find("actualHpGoName").gameObject;
                }

                var nBarImage = nBarGo.GetComponent<Image>();

                nBarImage.fillAmount = ((Level - 1) / (float)Owner.Hp) * hpBar.healthImage.fillAmount;

                //var txt = actualHpGo.GetComponent<TextMeshProUGUI>();
                //txt.text = Owner.Hp + 1 - Level;
            }
        }
            

        public override void NotifyChanged()
        {
            base.NotifyChanged();
            UpdateOrCreateNightmareBar();

        }

        private Unit _nightmareSource;
        public Unit NightmareSource 
        { 
            get 
            { 
                if (_nightmareSource == null) 
                {
                    Log.LogWarning($"{this.Name} doesn't have NightmareSource set. Defaulting to PlayerUnit.");
                    return Battle.Player;
                } 
                return _nightmareSource;
            }
            private set => _nightmareSource = value;
        }

        

        public override bool Stack(StatusEffect other)
        {
            var rez = base.Stack(other);
            React(DoKill());

            return rez;
        }

        public bool CheckKill()
        {
            return Level > Owner.Hp;
        }

        IEnumerable<BattleAction> DoKill()
        {
            if (CheckKill())
            {
                NotifyActivating();
                yield return new ForceKillAction(NightmareSource, Owner);
            }
        }

        private IEnumerable<BattleAction> HealingReceived(HealEventArgs args)
        {
            return DoKill();
        }

        private IEnumerable<BattleAction> DamageReceived(DamageEventArgs args)
        {
            return DoKill();
        }

        [HarmonyPatch]
        class MaxHp_Patch
        {

            [HarmonyPatch(typeof(Unit), nameof(Unit.SetMaxHp))]
            [HarmonyPostfix]
            static void EnemyPostfix(Unit __instance)
            {
                CheckNightmare(__instance);
            }


            private static void CheckNightmare(Unit unit)
            {
                if (unit.Battle != null && unit.TryGetStatusEffect<DC_NightmareSE>(out var nightmareSE))
                {
                    nightmareSE.UpdateOrCreateNightmareBar();
                    foreach (var a in nightmareSE.DoKill())
                        // technically is not correct and will react outside of action stack where SetMaxHp was invoked.
                        // but currently there's no action for setting max hp so w/e
                        // ALSO action source will be null
                        nightmareSE.Battle.RequestDebugAction(a, "Nightmare maxHp check");
                }
            }
        }


    }

}
