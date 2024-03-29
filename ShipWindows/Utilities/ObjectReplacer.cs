﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShipWindows.Utilities
{
    internal class ObjectReplacer
    {

        private static Dictionary<GameObject, ReplaceInfo> replacedObjects = new();

        public static void ReplaceMaterial(GameObject fromObj, GameObject toObj)
        {
            try
            {
                MeshRenderer mesh1 = fromObj.GetComponent<MeshRenderer>();
                MeshRenderer mesh2 = toObj.GetComponent<MeshRenderer>();

                if (!mesh1 || !mesh2) return;

                mesh2.material = mesh1.material;

            } catch (Exception e)
            {
                ShipWindowPlugin.Log.LogError($"Could not replace object material:\n{e}");
            }
        }

        public static GameObject Replace(GameObject original, GameObject prefab)
        {
            ShipWindowPlugin.Log.LogInfo($"Replacing object {original.name} with {prefab.name}...");
            GameObject newObj = UnityEngine.Object.Instantiate(prefab, original.transform.parent);
            newObj.transform.position = original.transform.position;
            newObj.transform.rotation = original.transform.rotation;

            string originalName = original.name;
            original.name = $"{originalName} (Old)";
            newObj.name = originalName;

            newObj.SetActive(true);
            original.SetActive(false);

            ReplaceInfo info;
            info.name = originalName;
            info.original = original;
            info.replacement = newObj;

            replacedObjects[original] = info;

            return newObj;
        }

        public static void Restore(GameObject original)
        {
            if (!replacedObjects.ContainsKey(original)) return;

            try
            {

                ReplaceInfo info;
                replacedObjects.TryGetValue(original, out info);

                ShipWindowPlugin.Log.LogInfo($"Restoring object {info.name}...");

                info.original.SetActive(true);
                info.original.name = info.name;

                UnityEngine.Object.DestroyImmediate(info.replacement);

                replacedObjects.Remove(original);

            } catch (Exception e)
            {
                ShipWindowPlugin.Log.LogWarning($"GameObject replacement info not found for: " +
                    $"{(original != null ? original.name : "Invalid GameObject")}! Not replaced?");
            }
        }

    }

    struct ReplaceInfo
    {
        public string name;
        public GameObject original;
        public GameObject replacement;
    }
}
