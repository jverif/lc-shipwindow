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

        public void SetWindowState(bool closed)
        {
            GetComponent<Animator>()?.SetBool("Closed", closed);
        }
    }
}
