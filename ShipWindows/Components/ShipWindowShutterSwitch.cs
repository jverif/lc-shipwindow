using GameNetcodeStuff;
using ShipWindows.Networking;
using Unity.Netcode;

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
            NetworkHandler.WindowSwitchUsed(WindowState.Instance.WindowsClosed);
        }
    }
}
