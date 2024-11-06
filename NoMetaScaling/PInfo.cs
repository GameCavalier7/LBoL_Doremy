﻿using HarmonyLib;

namespace NoMetaScalling
{
    public static class PInfo
    {
        // each loaded plugin needs to have a unique GUID. usually author+generalCategory+Name is good enough
        public const string GUID = "neo.lbol.modifiers.noMetaScaling";
        public const string Name = "No Meta scaling";
        public const string version = "0.5.0";
        public static readonly Harmony harmony = new Harmony(GUID);

    }
}