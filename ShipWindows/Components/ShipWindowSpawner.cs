using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ShipWindows.Components
{
    public class ShipWindowSpawner : MonoBehaviour
    {
        public int ID;

        public void Start()
        {
            ShipWindowPlugin.mls.LogInfo($"We should spawn window {ID}");
        }
    }
}
