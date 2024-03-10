using System;
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

        public delegate void OnWindowSwitchToggled();
        public static event OnWindowSwitchToggled WindowSwitchToggledEvent;

        public static void RegisterMessages()
        {
            ShipWindowPlugin.Log.LogInfo("Registering network message handlers...");
            MessageManager.RegisterNamedMessageHandler("ShipWindow_WindowSyncResponse", ReceiveWindowSync);

            MessageManager.RegisterNamedMessageHandler("ShipWindow_WindowSwitchUsed", ReceiveWindowSwitchUsed_Server);
            MessageManager.RegisterNamedMessageHandler("ShipWindow_WindowSwitchUsedBroadcast", ReceiveWindowSwitchUsed_Client);

            if (IsHost)
            {
                MessageManager.RegisterNamedMessageHandler("ShipWindow_WindowSyncRequest", ReceiveWindowSyncRequest);
            }
        }

        public static void UnregisterMessages()
        {
            ShipWindowPlugin.Log.LogInfo("Unregistering network message handlers...");
            MessageManager.UnregisterNamedMessageHandler("ShipWindow_WindowSyncResponse");
            MessageManager.UnregisterNamedMessageHandler("ShipWindow_WindowSyncRequest");

            MessageManager.UnregisterNamedMessageHandler("ShipWindow_WindowSwitchUsed");
            MessageManager.UnregisterNamedMessageHandler("ShipWindow_WindowSwitchUsedBroadcast");
        }

        public static void WindowSwitchUsed(bool currentState)
        {
            using FastBufferWriter stream = new(1, Allocator.Temp);
            stream.WriteValueSafe(currentState);

            ShipWindowPlugin.Log.LogInfo("Sending window switch toggle message...");

            MessageManager.SendNamedMessage("ShipWindow_WindowSwitchUsed", 0ul, stream);
        }

        public static void ReceiveWindowSwitchUsed_Server(ulong clientId, FastBufferReader reader)
        {

            bool currentState;
            reader.ReadValueSafe(out currentState);

            using FastBufferWriter stream = new(1, Allocator.Temp);
            stream.WriteValueSafe(currentState);

            ShipWindowPlugin.Log.LogInfo($"Received window switch toggle message from client {clientId}");

            MessageManager.SendNamedMessageToAll("ShipWindow_WindowSwitchUsedBroadcast", stream);
        }

        public static void ReceiveWindowSwitchUsed_Client(ulong _, FastBufferReader reader)
        {
            bool currentState;
            reader.ReadValueSafe(out currentState);

            ShipWindowPlugin.Log.LogInfo("Received window switch toggle message from server...");

            WindowState.Instance.SetWindowState(!currentState, WindowState.Instance.WindowsLocked);
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
                ShipWindowPlugin.Log.LogError($"Error occurred sending window sync message:\n{e}");
            }
        }

        public static void RequestWindowSync()
        {
            if (!IsClient) return;

            ShipWindowPlugin.Log.LogInfo("Requesting WindowState sync...");

            using FastBufferWriter stream = new(1, Allocator.Temp);
            MessageManager.SendNamedMessage("ShipWindow_WindowSyncRequest", 0ul, stream);
        }

        public static void ReceiveWindowSync(ulong _, FastBufferReader reader)
        {
            int IntSize = sizeof(int);

            if (!reader.TryBeginRead(IntSize))
            {
                ShipWindowPlugin.Log.LogError("Failed to read window sync message");
                return;
            }

            reader.ReadValueSafe(out int len, default);
            if (!reader.TryBeginRead(len))
            {
                ShipWindowPlugin.Log.LogError("Window sync failed.");
                return;
            }

            ShipWindowPlugin.Log.LogInfo("Receiving WindowState sync message...");

            byte[] data = new byte[len];
            reader.ReadBytesSafe(ref data, len);

            WindowState state = DeserializeFromBytes<WindowState>(data);
            WindowState.Instance = state;

            ShipWindowPlugin.Log.LogInfo($"{state.WindowsClosed}, {state.WindowsLocked}, {state.VolumeActive}, {state.VolumeRotation}");

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
                ShipWindowPlugin.Log.LogError($"Error serializing object: \n{e}");
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
                ShipWindowPlugin.Log.LogError($"Error deserializing object: \n{e}");
                return default;
            }
        }
    }
}
