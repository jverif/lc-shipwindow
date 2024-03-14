using System;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using ShipWindows.Components;

namespace ShipWindows.Utilities
{
    internal class ShipReplacer
    {

        public static bool debounceReplace = false;

        public static GameObject vanillaShipInside;
        public static GameObject newShipInside;

        // Only set on the server
        public static GameObject switchInstance;

        static GameObject FindOrThrow(string name)
        {
            GameObject gameObject = GameObject.Find(name);
            if (!gameObject) throw new Exception($"Could not find {name}! Wrong scene?");

            return gameObject;
        }

        static string GetShipAssetName()
        {
            if (WindowConfig.windowsUnlockable.Value == true && WindowConfig.vanillaMode.Value == false)
            {
                ShipWindowSpawner[] spawners = UnityEngine.Object.FindObjectsByType<ShipWindowSpawner>(FindObjectsSortMode.None);

                bool w1 = spawners.FirstOrDefault(spawner => spawner.ID == 1) != null;
                bool w2 = spawners.FirstOrDefault(spawner => spawner.ID == 2) != null;
                bool w3 = spawners.FirstOrDefault(spawner => spawner.ID == 3) != null;
                return $"ShipInsideWithWindow{(w1 ? 1 : 0)}{(w2 ? 1 : 0)}{(w3 ? 1 : 0)}";
            } else
            {
                bool w1 = WindowConfig.enableWindow1.Value;
                bool w2 = WindowConfig.enableWindow2.Value;
                bool w3 = WindowConfig.enableWindow3.Value;
                return $"ShipInsideWithWindow{(w1 ? 1 : 0)}{(w2 ? 1 : 0)}{(w3 ? 1 : 0)}";
            }
        }
        static void AddWindowScripts(GameObject ship)
        {
            Transform container = ship.transform.Find("WindowContainer");
            if (container == null) return;

            foreach (Transform window in container)
            {
                if (window.gameObject.GetComponent<ShipWindow>() != null) continue;

                int id;
                if (int.TryParse(window.gameObject.name[window.name.Length - 1].ToString(), out id))
                {
                    window.gameObject.AddComponent<ShipWindow>().ID = id;
                }
            }
        }

        public static void ReplaceDebounced(bool replace)
        {
            //ShipWindowPlugin.Log.LogInfo($"Debounce replace call. Replace? {replace} Is multiple call: {debounceReplace}");
            if (WindowConfig.windowsUnlockable.Value == false || WindowConfig.vanillaMode.Value == true) return;
            if (debounceReplace) return;

            debounceReplace = true;
            StartOfRound.Instance.StartCoroutine(ReplacementCoroutine(replace));
        }

        private static IEnumerator ReplacementCoroutine(bool replace)
        {
            yield return null; // Wait 1 frame.

            //ShipWindowPlugin.Log.LogInfo("Performing ship replacement/restore.");
            debounceReplace = false;

            if (replace)
            {
                ReplaceShip();
            } else
            {
                RestoreShip();
            }
        }

        public static void ReplaceShip()
        {
            try
            {

                if (newShipInside != null && vanillaShipInside != null)
                {
                    //ShipWindowPlugin.Log.LogInfo($"Calling ReplaceShip when ship was already replaced! Restoring original...");
                    ObjectReplacer.Restore(vanillaShipInside);
                }

                vanillaShipInside = FindOrThrow("Environment/HangarShip/ShipInside");
                string shipName = GetShipAssetName();

                //ShipWindowPlugin.Log.LogInfo($"Replacing ship with {shipName}");

                GameObject newShipPrefab = ShipWindowPlugin.mainAssetBundle.LoadAsset<GameObject>
                    ($"Assets/LethalCompany/Mods/ShipWindow/Ships/{shipName}.prefab");

                if (newShipPrefab == null) throw new Exception($"Could not load requested ship replacement! {shipName}");
                AddWindowScripts(newShipPrefab);

                newShipInside = ObjectReplacer.Replace(vanillaShipInside, newShipPrefab);

                StartOfRound.Instance.StartCoroutine(WaitAndCheckSwitch());

            } catch (Exception e)
            {
                ShipWindowPlugin.Log.LogError($"Failed to replace ShipInside! \n{e}");
            }
        }

        public static void SpawnSwitch()
        {
            ShipWindowPlugin.Log.LogInfo("Spawning shutter switch...");
            if (ShipWindowPlugin.windowSwitchPrefab != null)
            {
                switchInstance = UnityEngine.GameObject.Instantiate(ShipWindowPlugin.windowSwitchPrefab);
                switchInstance.GetComponent<NetworkObject>().Spawn();
                
            }
        }

        public static void CheckShutterSwitch()
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                //ShipWindowPlugin.Log.LogInfo("Checking window switch redundancy...");
                ShipWindow[] windows = UnityEngine.Object.FindObjectsByType<ShipWindow>(FindObjectsSortMode.None);

                if (windows.Length > 0)
                {
                    if (switchInstance == null)
                    {
                        SpawnSwitch();
                    } else
                    {
                        switchInstance.SetActive(true);
                    }
                } else
                {
                    if (switchInstance != null)
                    {
                        UnityEngine.GameObject.Destroy(switchInstance);
                        switchInstance = null;
                    }
                        
                }
            }
        }

        public static IEnumerator WaitAndCheckSwitch()
        {
            yield return null;

            CheckShutterSwitch();
        }

        public static void RestoreShip()
        {
            if (newShipInside == null) return;

            ObjectReplacer.Restore(vanillaShipInside);
            StartOfRound.Instance.StartCoroutine(WaitAndCheckSwitch());
            newShipInside = null;
        }

        // If any of the window spawners still exist without windows, spawn those windows.
        public static IEnumerator CheckForKeptSpawners()
        {
            yield return new WaitForSeconds(1f);

            ShipWindowSpawner[] windows = UnityEngine.Object.FindObjectsByType<ShipWindowSpawner>(FindObjectsSortMode.None);

            foreach (ShipWindowSpawner window in windows)
            {
                ReplaceDebounced(true);
            }
        }
    }
}
