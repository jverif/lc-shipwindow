using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace ShipWindow
{
    public class ShipWindowHandler : NetworkBehaviour
    {

        private static ShipWindowHandler _instance;
        public static ShipWindowHandler Instance {
            get
            {
                if (_instance == null)
                    _instance = UnityEngine.Object.FindObjectOfType<ShipWindowHandler>();
                if (_instance == null)
                    ShipWindowPlugin.mls.LogWarning("ShipWindowHandler Could Not Be Found! Returning Null!");
                return _instance;
            }
            set { _instance = value; }
        }

        public bool isWindowClosed;
        public bool isWindowLocked = false;

        public override void OnNetworkSpawn()
        {
            Instance = this;

            ShipWindowPlugin.mls.LogInfo("Network window spawn");

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

            if (ShipWindowPlugin.enableShutter.Value == true)
            {
                var windowAnimator = ShipWindowPlugin.newShipInstance.transform.Find("WindowContainer/Window").GetComponent<Animator>();
                if (windowAnimator != null)
                    windowAnimator?.SetBool("Closed", closed);
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
            var universeVolume = ShipWindowPlugin.universeVolume;
            var starSphereLarge = ShipWindowPlugin.starSphereLarge;

            switch (ShipWindowPlugin.spaceOutsideSetting.Value)
            {
                case 0: break;
                case 1: universeVolume?.SetActive(enabled); break;
                case 2: starSphereLarge?.SetActive(enabled); break;
                default: break;
            }
        }
    }
}
