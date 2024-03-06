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

        public delegate void OnSetWindowState(bool open, bool locked);
        public delegate void OnSetVolumeState(bool active);

        public static OnSetWindowState SetWindowStateEvent;
        public static OnSetVolumeState SetVolumeStateEvent;

        public static void RegisterMessages()
        {
            MessageManager.RegisterNamedMessageHandler("ShipWindow_SetWindowState", ReceiveWindowState);
            MessageManager.RegisterNamedMessageHandler("ShipWindow_SetVolumeState", ReceiveVolumeState);
        }

        public static void UnregisterMessages()
        {
            MessageManager.UnregisterNamedMessageHandler("ShipWindow_SetWindowState");
            MessageManager.UnregisterNamedMessageHandler("ShipWindow_SetVolumeState");
        }

        public static void SetWindowState(bool open, bool locked)
        {
            if (!IsHost)
            {
                SetWindowStateEvent.Invoke(open, locked);
                return;
            }
                
            using FastBufferWriter stream = new(2, Allocator.Temp);

            try
            {
                stream.WriteValueSafe(open);
                stream.WriteValueSafe(locked);

                MessageManager.SendNamedMessageToAll("ShipWindow_SetWindowState", stream);
            } catch (Exception e)
            {
                ShipWindowPlugin.mls.LogError($"Error occurred sending window state message:\n{e}");
            }
        }

        private static void ReceiveWindowState(ulong _, FastBufferReader reader)
        {
            bool open;
            bool locked;

            reader.ReadValueSafe(out open);
            reader.ReadValueSafe(out locked);

            SetWindowStateEvent.Invoke(open, locked);
        }

        public static void SetVolumeState(bool active)
        {
            if (!IsHost)
            {
                SetVolumeStateEvent.Invoke(active);
                return;
            } 

            using FastBufferWriter stream = new(2, Allocator.Temp);

            try
            {
                stream.WriteValueSafe(active);

                MessageManager.SendNamedMessageToAll("ShipWindow_SetVolumeState", stream);
            } catch (Exception e)
            {
                ShipWindowPlugin.mls.LogError($"Error occurred sending window state message:\n{e}");
            }
        }

        private static void ReceiveVolumeState(ulong _, FastBufferReader reader)
        {
            bool active;

            reader.ReadValueSafe(out active);

            SetVolumeStateEvent.Invoke(active);
        }
    }
}
