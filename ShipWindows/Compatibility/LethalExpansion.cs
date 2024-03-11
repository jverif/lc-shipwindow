using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ShipWindows.Compatibility
{
    static class LethalExpansion
    {
        public static bool Enabled { get; private set; }

        static void Initialize()
        {
            Enabled = true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), "Start")]
        static void Patch_RoundStart()
        {
            if (!Enabled) return;

            // https://github.com/jverif/lc-shipwindow/issues/8
            // Lethal Expansion "terrainfixer" is positioned at 0, -500, 0 and becomes
            // visible when a mod that increases view distance is installed.
            GameObject terrainfixer = GameObject.Find("terrainfixer");
            if (terrainfixer != null)
            {
                terrainfixer.transform.position = new Vector3(0, -5000, 0);
            }
        }
    }
}
