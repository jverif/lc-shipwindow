using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShipWindows.Networking;
using UnityEngine;

namespace ShipWindows.Components
{
    public class ShipWindow : MonoBehaviour
    {
        public void SetClosed(bool closed)
        {
            GetComponent<Animator>()?.SetBool("Closed", closed);
        }
    }
}
