﻿using ShipWindows.Components;
using System;
using UnityEngine;

namespace ShipWindows.Networking
{
    [Serializable]
    internal class WindowState
    {
        public static WindowState Instance { get; set; }

        public bool WindowsClosed = false;
        public bool WindowsLocked = false;
        public bool VolumeActive = true;
        public float VolumeRotation = 0f;

        public WindowState()
        {
            Instance = this;
        }

        public void SetWindowState(bool closed, bool locked)
        {
            if (WindowConfig.enableShutter.Value == true)
            {
                ShipWindow[] windows = UnityEngine.Object.FindObjectsByType<ShipWindow>(FindObjectsSortMode.None);

                foreach (ShipWindow w in windows)
                    w.SetClosed(closed);

                WindowsClosed = closed;
                WindowsLocked = locked;
            }
        }

        public void SetVolumeState(bool active)
        {
            var outsideSkybox = ShipWindowPlugin.outsideSkybox;
            outsideSkybox?.SetActive(active);

            VolumeActive = active;
        }

        public void SetVolumeRotation(float rotation)
        {
            SpaceSkybox.Instance?.SetRotation(rotation);
            VolumeRotation = rotation;
        }

        public void ReceiveSync()
        {
            // By this point the Instance has already been replaced, so we can just update the actual objects
            // with what the values should be.

            ShipWindowPlugin.Log.LogInfo("Receiving window sync message...");

            SetWindowState(WindowsClosed, WindowsLocked);
            SetVolumeState(VolumeActive);
            SetVolumeRotation(VolumeRotation);
        }
    }
}
