using Unity.Netcode;
using UnityEngine;
using ShipWindows.Components;

namespace ShipWindows
{
    public class ShipWindowDef
    {
        public int ID;
        public int UnlockableID;
        public GameObject Prefab;
        public int BaseCost;

        private ShipWindowDef(int id, GameObject prefab, int baseCost)
        {
            ID = id;
            Prefab = prefab;
            BaseCost = baseCost;
        }

        public static ShipWindowDef Register(int id, int baseCost)
        {
            ShipWindowPlugin.Log.LogInfo($"Registering window prefab: Window {id}");
            GameObject windowSpawner = ShipWindowPlugin.mainAssetBundle.LoadAsset<GameObject>($"Assets/LethalCompany/Mods/ShipWindow/SpawnWindow{id}.prefab");
            windowSpawner.AddComponent<ShipWindowSpawner>().ID = id;

            NetworkManager.Singleton.AddNetworkPrefab(windowSpawner);

            ShipWindowDef def = new(id, windowSpawner, baseCost);
            //def.UnlockableID = Unlockables.AddWindowToUnlockables(def);

            return def;
        }
    }
}
