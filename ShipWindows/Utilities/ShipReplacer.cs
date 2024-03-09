using ShipWindows.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ShipWindows.Utilities
{
    internal class ShipReplacer
    {

        public static Coroutine debounceReplace;

        public static GameObject vanillaShipInside;
        public static GameObject newShipInside;

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
                int id;
                if (int.TryParse(window.gameObject.name[window.name.Length - 1].ToString(), out id))
                {
                    window.gameObject.AddComponent<ShipWindow>().ID = id;
                }
            }
        }

        public static void ReplaceDebounced()
        {
            if (WindowConfig.windowsUnlockable.Value == false || WindowConfig.vanillaMode.Value == true) return;
            if (debounceReplace != null) return;

            debounceReplace = StartOfRound.Instance.StartCoroutine(ReplacementCoroutine());
        }

        private static IEnumerator ReplacementCoroutine()
        {
            yield return null; // Wait 1 frame.

            ReplaceShip();

            debounceReplace = null;
        }

        public static void ReplaceShip()
        {
            try
            {

                if (newShipInside != null)
                {
                    ShipWindowPlugin.Log.LogWarning($"Calling ReplaceShip when ship was already replaced! Restoring original...");
                    ObjectReplacer.Restore(vanillaShipInside);
                }

                vanillaShipInside = FindOrThrow("Environment/HangarShip/ShipInside");
                string shipName = GetShipAssetName();

                GameObject newShipPrefab = ShipWindowPlugin.mainAssetBundle.LoadAsset<GameObject>
                    ($"Assets/LethalCompany/Mods/ShipWindow/Ships/{shipName}.prefab");

                if (newShipPrefab == null) throw new Exception($"Could not load requested ship replacement! {shipName}");

                AddWindowScripts(newShipPrefab);

                newShipInside = ObjectReplacer.Replace(vanillaShipInside, newShipPrefab);

            } catch (Exception e)
            {
                ShipWindowPlugin.Log.LogError($"Failed to replace ShipInside! \n{e}");
            }
        }

        public static void RestoreShip()
        {
            if (newShipInside == null) return;

            ObjectReplacer.Restore(vanillaShipInside);
            newShipInside = null;
        }
    }
}
