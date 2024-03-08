using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using Unity.Collections;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace ShipWindows.Networking
{
    [Serializable]
    internal class NetworkHandler
    {
        internal static CustomMessagingManager MessageManager => NetworkManager.Singleton.CustomMessagingManager;
        internal static bool IsClient => NetworkManager.Singleton.IsClient;
        internal static bool IsHost => NetworkManager.Singleton.IsHost;

        public delegate void OnWindowSyncReceive();
        public static event OnWindowSyncReceive WindowSyncReceivedEvent;

        public static void RegisterMessages()
        {
            ShipWindowPlugin.mls.LogInfo("Registering network message handlers...");
            MessageManager.RegisterNamedMessageHandler("ShipWindow_WindowSyncResponse", ReceiveWindowSync);

            if (IsHost)
            {
                MessageManager.RegisterNamedMessageHandler("ShipWindow_WindowSyncRequest", ReceiveWindowSyncRequest);
            }
        }

        public static void UnregisterMessages()
        {
            ShipWindowPlugin.mls.LogInfo("Unregistering network message handlers...");
            MessageManager.UnregisterNamedMessageHandler("ShipWindow_WindowSyncResponse");
            MessageManager.UnregisterNamedMessageHandler("ShipWindow_WindowSyncRequest");
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

            }
            catch (Exception e)
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

            ShipWindowPlugin.mls.LogInfo("Receiving WindowState sync message...");

            byte[] data = new byte[len];
            reader.ReadBytesSafe(ref data, len);

            WindowState state = DeserializeFromBytes<WindowState>(data);
            WindowState.Instance = state;

            WindowSyncReceivedEvent?.Invoke();

        }

        public static byte[] SerializeToBytes(object val)
        {
            BinaryFormatter bf = new();
            using MemoryStream stream = new();

            try
            {
                bf.Serialize(stream, val);
                return stream.ToArray();
            }
            catch (Exception e)
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
            }
            catch (Exception e)
            {
                ShipWindowPlugin.mls.LogError($"Error deserializing object: \n{e}");
                return default;
            }
        }
    }
}
