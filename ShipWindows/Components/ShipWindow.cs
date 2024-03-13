using ShipWindows.Utilities;
using UnityEngine;

namespace ShipWindows.Components
{
    public class ShipWindow : MonoBehaviour
    {
        public int ID;

        // Misc variables held by the windows. This is kind of nasty.

        // Window 2
        GameObject oldPostersObject;

        // Window 3
        public static string[] window3DisabledList = [
            "UnderbellyMachineParts",
            "NurbsPath.001"
        ];

        public void SetClosed(bool closed)
        {
            GetComponent<Animator>()?.SetBool("Closed", closed);
        }

        public void OnStart()
        {
            switch (ID)
            {
                case 1:
                    break;

                case 2:
                    if (WindowConfig.dontMovePosters.Value == false)
                    {
                        GameObject movedPostersPrefab = ShipWindowPlugin.mainAssetBundle.LoadAsset<GameObject>($"Assets/LethalCompany/Mods/ShipWindow/ShipPosters.prefab");
                        if (movedPostersPrefab != null)
                        {
                            Transform oldPosters = ShipReplacer.newShipInside?.transform.parent.Find("Plane.001");
                            if (oldPosters != null)
                            {
                                oldPostersObject = oldPosters.gameObject;
                                ObjectReplacer.Replace(oldPostersObject, movedPostersPrefab);
                            }
                        }
                    }
                    break;

                case 3:
                    foreach (string go in window3DisabledList)
                    {
                        var obj = GameObject.Find($"Environment/HangarShip/{go}");
                        if (obj == null)
                            continue;

                        obj.gameObject.SetActive(false);
                    }

                    if (WindowConfig.disableUnderLights.Value == true)
                    {
                        Transform floodLights = ShipReplacer.newShipInside?.transform.Find("WindowContainer/Window3/Lights");
                        if (floodLights != null) floodLights.gameObject.SetActive(false);
                    }
                    break;

                default: break;
            }
        }

        public void Start()
        {
            OnStart();
        }

        public void OnDestroy()
        {
            switch (ID)
            {
                case 1:
                    break;

                case 2:
                    ObjectReplacer.Restore(oldPostersObject);
                    break;

                case 3:
                    foreach (string go in window3DisabledList)
                    {
                        var obj = GameObject.Find($"Environment/HangarShip/{go}");
                        if (obj == null)
                            continue;

                        obj.gameObject.SetActive(true);
                    }
                    break;

                default: break;
            }
        }
    }
}
