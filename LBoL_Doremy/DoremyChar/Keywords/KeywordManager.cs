﻿using HarmonyLib;
using LBoL.Base;
using LBoL.Core;
using LBoL.Core.Cards;
using LBoL.Core.StatusEffects;
using LBoL.EntityLib.EnemyUnits.Character;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;

namespace LBoL_Doremy.DoremyChar.Keywords
{
    public static class KeywordManager
    {
        static ConditionalWeakTable<Card, Dictionary<string, CardKeyword>> cwt_keywords = new ConditionalWeakTable<Card, Dictionary<string, CardKeyword>>();


        static Dictionary<string, CardKeyword> GetKeywords(Card card) {
            return cwt_keywords.GetValue(card, (_) => new Dictionary<string, CardKeyword>());
        }

        public static IEnumerable<CardKeyword> AllCustomKeywords(this Card card)
        {
            if(cwt_keywords.TryGetValue(card, out Dictionary<string, CardKeyword> keywords))
                return keywords.Values;
            return new HashSet<CardKeyword>();
        }

        public static bool HasCustomKeyword(this Card card, string kwId) 
        {
            return GetKeywords(card).ContainsKey(kwId);
        }

        public static bool AddCustomKeyword(this Card card, CardKeyword keyword)
        {
            return GetKeywords(card).TryAdd(keyword.kwSEid, keyword);
        }
        public static bool RemoveCustomKeyword(this Card card, CardKeyword keyword)
        {
            return GetKeywords(card).Remove(keyword.kwSEid);
        }

        public static CardKeyword GetOrAddCustomKeyword(this Card card, CardKeyword keyword)
        {
            if (!card.TryGetCustomKeyword(keyword.kwSEid, out var rezKeyword))
            { 
                card.AddCustomKeyword(keyword);
                return keyword;
            }
            return rezKeyword;
        }

        public static T GetOrAddCustomKeyword<T>(this Card card, T keyword) where T : CardKeyword
        {
            if (!card.TryGetCustomKeyword<T>(keyword.kwSEid, out var rezKeyword))
            {
                card.AddCustomKeyword(keyword);
                return keyword;
            }
            return rezKeyword;
        }

        public static bool TryGetCustomKeyword(this Card card, string kwId, out CardKeyword rezKeyword)
        {
            return GetKeywords(card).TryGetValue(kwId, out rezKeyword);
        }

        public static bool TryGetCustomKeyword<T>(this Card card, string kwId, out T rezKeyword) where T : CardKeyword
        {
            var rez = TryGetCustomKeyword(card, kwId, out var foundKw);
            rezKeyword = (T)foundKw;
            return rez;
        }

        public static CardKeyword GetCustomKeyword(this Card card, string kwId)
        {
            card.TryGetCustomKeyword(kwId, out var rezKeyword);
            return rezKeyword;
        }

        public static T GetCustomKeyword<T>(this Card card, string kwId) where T : CardKeyword
        {
            return GetCustomKeyword(card, kwId) as T;
        }


        [HarmonyPatch(typeof(Card), nameof(Card.EnumerateDisplayWords), MethodType.Enumerator)]
        class Card_Tooltip_Patch
        {

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
            {
                return new CodeMatcher(instructions)
                    .MatchEndForward(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Library), nameof(Library.InternalEnumerateDisplayWords))))
                    .MatchEndBackwards(OpCodes.Ldloc_S)
                    .Advance(1)
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(originalMethod.DeclaringType, "verbose")))
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_2))

                    .Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Card_Tooltip_Patch), nameof(Card_Tooltip_Patch.AppendCustomKeywords))))

                    .InstructionEnumeration();
            }

            private static IEnumerable<string> AppendCustomKeywords(IReadOnlyList<string> keywords, bool displayVerbose, Card card)
            {
                return keywords.Concat(KeywordManager.AllCustomKeywords(card).Where(kw => !kw.isVerbose || displayVerbose).Select(kw => kw.kwSEid));
            }
        }



        [HarmonyPatch(typeof(Card), nameof(Card.EnumerateAutoAppendKeywordNames))]
        class Card_Description_Patch
        {
            static void Postfix(Card __instance, ref IEnumerable<string> __result)
            {
                var card = __instance;
                var kwToAppend = card.AllCustomKeywords().Where(kw => kw.descPos != KwDescPos.DoNotDisplay)
                    .GroupBy(kw => kw.descPos, kw => TypeFactory<StatusEffect>.LocalizeProperty(kw.kwSEid, "Name", true, false) );

                __result = kwToAppend.SelectMany(g => g.Key == KwDescPos.First ? g : Enumerable.Empty<string>())
                    .Concat(__result)
                    .Concat(kwToAppend.SelectMany(g => g.Key == KwDescPos.Last ? g : Enumerable.Empty<string>()));

            }
        }



        [HarmonyPatch(typeof(Card), nameof(Card.Description), MethodType.Getter)]
        class DescCheckCustomKw_Patch
        {

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                return new CodeMatcher(instructions)
                    .MatchEndForward(new CodeInstruction(OpCodes.Call, AccessTools.DeclaredPropertyGetter(typeof(Card), nameof(Card.Keywords))))
                    .Advance(1)
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DescCheckCustomKw_Patch), nameof(DescCheckCustomKw_Patch.CheckCustomKws))))
                    
                    .InstructionEnumeration();
            }

            private static bool CheckCustomKws(Keyword keywords, Card card)
            {
                return keywords != Keyword.None || card.AllCustomKeywords().Count() > 0;
            }
        }



    }
}
