using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ShipWindows
{
    [Serializable]
    internal class WindowState
    {
        public static WindowState Instance { get; set; }

        public bool WindowsClosed = false;
        public bool WindowsLocked = false;
        public bool SpaceActive = true;
        public float VolumeRotation = 0f;

        // From lever, looking out:
        public bool Window1Active = false; // Left
        public bool Window2Active = false; // Right
        public bool Window3Active = false; // Floor

        public WindowState()
        {
            Instance = this;
        }
    }
}
