using ShipWindows.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ShipWindows.Components
{
    public class ShipWindowSpawner : MonoBehaviour
    {
        public int ID;

        public void Start()
        {
            ShipWindowPlugin.Log.LogInfo($"We should spawn window {ID}");

            // Flag to the mod that we have spawned. It will wait for a moment and then
            // find all ShipWindowSpawners to replace the ship once instead of n times.
            ShipReplacer.ReplaceDebounced();
        }
    }
}
