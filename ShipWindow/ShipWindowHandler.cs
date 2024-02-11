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
        public Animator windowAnimator;
        public GameObject universeVolume;
        public GameObject starSphereLarge;

        public override void OnNetworkSpawn()
        {

            ShipWindowPlugin.mls.LogInfo("Network window spawn");

            DontDestroyOnLoad(gameObject);
            Instance = this;

            GameObject windowShipObject = ShipWindowPlugin.newShipInstance;
            if (windowShipObject != null)
                windowAnimator = windowShipObject.transform.Find("WindowContainer/Window").GetComponent<Animator>();

            universeVolume = ShipWindowPlugin.universeVolume;
            starSphereLarge = ShipWindowPlugin.starSphereLarge;

            SetWindowState(false);

            base.OnNetworkSpawn();
        }

        [ClientRpc]
        public void SetWindowStateClientRpc(bool closed)
        {
            SetWindowState(closed);
        }

        public void SetWindowState(bool closed)
        {
            if (ShipWindowPlugin.enableShutter.Value == true)
                windowAnimator?.SetBool("Closed", closed);
        }

        [ClientRpc]
        public void SetVolumeStateClientRpc(bool enabled)
        {
            SetVolumeState(enabled);
        }

        public void SetVolumeState(bool enabled)
        {
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
