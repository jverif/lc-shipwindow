using ShipWindows.Utilities;
using UnityEngine;

namespace ShipWindows.Components
{
    public class ShipWindowSpawner : MonoBehaviour
    {
        public int ID;

        public void OnStart()
        {
            ShipWindowPlugin.Log.LogInfo($"We should spawn window {ID}");

            // Flag to the mod that we have spawned. It will wait for a moment and then
            // find all ShipWindowSpawners to replace the ship once instead of n times.
            ShipReplacer.ReplaceDebounced(true);
        }

        public void Start()
        {
            OnStart();
        }

        public void OnDestroy()
        {
            // If the ship was already replaced, calling again will revert it.
            ShipReplacer.ReplaceDebounced(false);
        }
    }
}
