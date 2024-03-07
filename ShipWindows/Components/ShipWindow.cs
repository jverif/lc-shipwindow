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

        public bool closed = false;

        public void SetClosed(bool closed)
        {
            this.closed = closed;
            GetComponent<Animator>()?.SetBool("Closed", closed);
        }

        public void OnEnable()
        {
            NetworkHandler.WindowSyncReceivedEvent += HandleWindowSync;
        }

        public void OnDisable()
        {
            NetworkHandler.WindowSyncReceivedEvent -= HandleWindowSync;
        }

        private void HandleWindowSync(WindowState state)
        {
            SetClosed(state.WindowsClosed);
        }
    }
}
