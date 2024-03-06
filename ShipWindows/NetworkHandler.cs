using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using Unity.Collections;

namespace ShipWindows
{
    internal class NetworkHandler
    {
        internal static CustomMessagingManager MessageManager => NetworkManager.Singleton.CustomMessagingManager;
        internal static bool IsClient => NetworkManager.Singleton.IsClient;
        internal static bool IsHost => NetworkManager.Singleton.IsHost;

        public delegate void OnSetWindowState(bool closed, bool locked);
        public delegate void OnSetVolumeState(bool active);

        public static event OnSetWindowState SetWindowStateEvent;
        public static event OnSetVolumeState SetVolumeStateEvent;

        public static void RegisterMessages()
        {
            ShipWindowPlugin.mls.LogInfo("Subscribing to ShipWindow events");
            MessageManager.RegisterNamedMessageHandler("ShipWindow_SetWindowState", ReceiveWindowState);
            MessageManager.RegisterNamedMessageHandler("ShipWindow_SetVolumeState", ReceiveVolumeState);
        }

        public static void UnregisterMessages()
        {
            ShipWindowPlugin.mls.LogInfo("Unsubscribing from ShipWindow events");
            MessageManager.UnregisterNamedMessageHandler("ShipWindow_SetWindowState");
            MessageManager.UnregisterNamedMessageHandler("ShipWindow_SetVolumeState");
        }

        public static void SetWindowState(bool closed, bool locked)
        {
            if (!IsHost)
            {
                SetWindowStateEvent?.Invoke(closed, locked);
                return;
            }
                
            using FastBufferWriter stream = new(2, Allocator.Temp);

            try
            {
                stream.WriteValueSafe(closed);
                stream.WriteValueSafe(locked);

                MessageManager.SendNamedMessageToAll("ShipWindow_SetWindowState", stream);
            } catch (Exception e)
            {
                ShipWindowPlugin.mls.LogError($"Error occurred sending window state message:\n{e}");
            }
        }

        private static void ReceiveWindowState(ulong _, FastBufferReader reader)
        {
            bool closed;
            bool locked;

            reader.ReadValueSafe(out closed);
            reader.ReadValueSafe(out locked);

            SetWindowStateEvent?.Invoke(closed, locked);
        }

        public static void SetVolumeState(bool active)
        {
            if (!IsHost)
            {
                SetVolumeStateEvent?.Invoke(active);
                return;
            } 

            using FastBufferWriter stream = new(2, Allocator.Temp);

            try
            {
                stream.WriteValueSafe(active);

                MessageManager.SendNamedMessageToAll("ShipWindow_SetVolumeState", stream);
            } catch (Exception e)
            {
                ShipWindowPlugin.mls.LogError($"Error occurred sending volume state message:\n{e}");
            }
        }

        private static void ReceiveVolumeState(ulong _, FastBufferReader reader)
        {
            bool active;

            reader.ReadValueSafe(out active);

            SetVolumeStateEvent?.Invoke(active);
        }
    }
}
