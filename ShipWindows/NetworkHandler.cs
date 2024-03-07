using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using Unity.Collections;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace ShipWindows
{
    [Serializable]
    internal class NetworkHandler
    {
        internal static CustomMessagingManager MessageManager => NetworkManager.Singleton.CustomMessagingManager;
        internal static bool IsClient => NetworkManager.Singleton.IsClient;
        internal static bool IsHost => NetworkManager.Singleton.IsHost;

        public delegate void OnSetWindowState(bool closed, bool locked);
        public delegate void OnSetVolumeState(bool active);
        public delegate void OnWindowSyncReceive(WindowState state);

        public static event OnSetWindowState SetWindowStateEvent;
        public static event OnSetVolumeState SetVolumeStateEvent;
        public static event OnWindowSyncReceive WindowSyncReceivedEvent;

        public static void RegisterMessages()
        {
            ShipWindowPlugin.mls.LogInfo("Registering network message handlers...");
            MessageManager.RegisterNamedMessageHandler("ShipWindow_SetWindowState", ReceiveWindowState);
            MessageManager.RegisterNamedMessageHandler("ShipWindow_SetVolumeState", ReceiveVolumeState);
            MessageManager.RegisterNamedMessageHandler("ShipWindow_WindowSyncResponse", ReceiveWindowSync);

            if (IsHost)
            {
                MessageManager.RegisterNamedMessageHandler("ShipWindow_WindowSyncRequest", ReceiveWindowSyncRequest);
            }
        }

        public static void UnregisterMessages()
        {
            ShipWindowPlugin.mls.LogInfo("Unregistering network message handlers...");
            MessageManager.UnregisterNamedMessageHandler("ShipWindow_SetWindowState");
            MessageManager.UnregisterNamedMessageHandler("ShipWindow_SetVolumeState");
            MessageManager.UnregisterNamedMessageHandler("ShipWindow_WindowSyncResponse");
            MessageManager.UnregisterNamedMessageHandler("ShipWindow_WindowSyncRequest");
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

        private static void ReceiveWindowSyncRequest(ulong clientId, FastBufferReader reader)
        {
            if (!IsHost) return;

            byte[] arr = SerializeToBytes(WindowState.Instance);
            int len = arr.Length;
            int IntSize = sizeof(int);

            using FastBufferWriter stream = new(len + IntSize, Allocator.Temp);

            try
            {

                stream.WriteValueSafe(in len, default);
                stream.WriteBytesSafe(arr);

                MessageManager.SendNamedMessage("ShipWindow_WindowSyncResponse", clientId, stream);

            } catch (Exception e)
            {
                ShipWindowPlugin.mls.LogError($"Error occurred sending window sync message:\n{e}");
            }
        }

        public static void RequestWindowSync()
        {
            if (!IsClient) return;

            using FastBufferWriter stream = new(1, Allocator.Temp);
            MessageManager.SendNamedMessage("ShipWindow_WindowSyncRequest", 0ul, stream);
        }

        public static void ReceiveWindowSync(ulong _, FastBufferReader reader)
        {
            int IntSize = sizeof(int);

            if (!reader.TryBeginRead(IntSize))
            {
                ShipWindowPlugin.mls.LogError("Failed to read window sync message");
                return;
            }

            reader.ReadValueSafe(out int len, default);
            if (!reader.TryBeginRead(len))
            {
                ShipWindowPlugin.mls.LogError("Window sync failed.");
                return;
            }

            byte[] data = new byte[len];
            reader.ReadBytesSafe(ref data, len);

            WindowState state = DeserializeFromBytes<WindowState>(data);
            WindowState.Instance = state;

            WindowSyncReceivedEvent?.Invoke(state);

        }

        public static byte[] SerializeToBytes(object val)
        {
            BinaryFormatter bf = new();
            using MemoryStream stream = new();

            try
            {
                bf.Serialize(stream, val);
                return stream.ToArray();
            } catch (Exception e)
            {
                ShipWindowPlugin.mls.LogError($"Error serializing object: \n{e}");
                return null;
            }
        }

        public static T DeserializeFromBytes<T>(byte[] data)
        {
            BinaryFormatter bf = new();
            using MemoryStream stream = new(data);

            try
            {
                return (T)bf.Deserialize(stream);
            } catch (Exception e)
            {
                ShipWindowPlugin.mls.LogError($"Error deserializing object: \n{e}");
                return default;
            }
        }
    }
}
