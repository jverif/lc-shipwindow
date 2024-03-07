using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace ShipWindows.Components
{
    public class ShipWindowShutterSwitch : NetworkBehaviour
    {

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            var trigger = transform.Find("WindowSwitch");
            if (trigger == null) return;

            var interactable = trigger.GetComponent<InteractTrigger>();
            if (interactable == null) return;

            interactable.onInteract.AddListener(PlayerUsedSwitch);
        }

        public void PlayerUsedSwitch(PlayerControllerB playerControllerB)
        {
            //FindFirstObjectByType<ShipWindowNetworkManager>().ToggleWindowShutter();
        }
    }
}
