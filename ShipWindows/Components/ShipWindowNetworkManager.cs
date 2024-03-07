using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace ShipWindows.Components
{
    public class ShipWindowNetworkManager : NetworkBehaviour
    {

        /*private static ShipWindowNetworkManager _instance;
        public static ShipWindowNetworkManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<ShipWindowNetworkManager>();
                if (_instance == null)
                    ShipWindowPlugin.mls.LogWarning("ShipWindowNetworkManager could not be found! Returning null!");
                return _instance;
            }
            set { _instance = value; }
        }

        public bool isWindowClosed;
        public bool isWindowLocked = false;

        public override void OnNetworkSpawn()
        {
            Instance = this;

            ShipWindowPlugin.mls.LogInfo("Window network manager spawned.");

            DontDestroyOnLoad(gameObject);
            SetWindowState(false, false);

            base.OnNetworkSpawn();
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetWindowStateServerRpc(bool closed, bool locked)
        {
            SetWindowStateClientRpc(closed, locked);
        }

        [ClientRpc]
        public void SetWindowStateClientRpc(bool closed, bool locked)
        {
            SetWindowState(closed, locked);
        }

        public void SetWindowState(bool closed, bool locked)
        {
            isWindowClosed = closed;
            isWindowLocked = locked;

            if (WindowConfig.enableShutter.Value == true)
            {
                ShipWindow[] windows = FindObjectsByType<ShipWindow>(FindObjectsSortMode.None);

               // foreach (ShipWindow w in windows)
                    //w.SetWindowState(closed);

                //var windowAnimator = ShipWindowPlugin.newShipInstance.transform.Find("WindowContainer/Window").GetComponent<Animator>();
                //if (windowAnimator != null)
                //    windowAnimator?.SetBool("Closed", closed);
            }

        }

        public void ToggleWindowShutter()
        {
            if (isWindowLocked) return;
            SetWindowStateServerRpc(!isWindowClosed, false);
        }

        [ClientRpc]
        public void SetVolumeStateClientRpc(bool enabled)
        {
            SetVolumeState(enabled);
        }

        public void SetVolumeState(bool enabled)
        {
            var outsideSkybox = ShipWindowPlugin.outsideSkybox;
            outsideSkybox?.SetActive(enabled);

            /*var starSphereLarge = ShipWindowPlugin.starsSphereLarge;

            switch (ShipWindowPlugin.spaceOutsideSetting.Value)
            {
                case 0: break;
                case 1: universeVolume?.SetActive(enabled); break;
                case 2: starSphereLarge?.SetActive(enabled); break;
                default: break;
            }*/
        //}
    }
}
