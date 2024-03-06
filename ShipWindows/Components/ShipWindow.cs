using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ShipWindows.Components
{
    public class ShipWindow : MonoBehaviour
    {

        public static bool Closed { get; set; }
        public static bool Locked {  get; set; }

        public void SetWindowState(bool closed, bool locked)
        {
            Closed = closed;
            Locked = locked;
            GetComponent<Animator>()?.SetBool("Closed", closed);
        }
    }
}
