using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ShipWindows.Compatibility
{
    static class CelestialTint
    {
        public static bool Enabled { get; private set; }

        static bool Initialize()
        {
            if (Enabled) return false;
            Enabled = true;

            return true;
        }
    }
}
