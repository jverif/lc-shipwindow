using ShipWindow;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace ShipWindow
{
    public class SpaceSkybox : MonoBehaviour
    {

        public static SpaceSkybox Instance {  get; private set; }

        private HDRISky sky;

        private Transform starSphere;

        public void Awake()
        {
            Instance = this;
        }
        
        public void Start()
        {
            switch (ShipWindowPlugin.spaceOutsideSetting.Value)
            {
                case 0: break;
                case 1:
                    Volume volume = GetComponent<Volume>();
                    volume?.profile?.TryGet(out sky);

                    break;
                case 2:
                    starSphere = transform;
                    break;
                default: break;
            }
        }

        public void Update()
        {
            if (ShipWindowPlugin.rotateSkybox.Value == false) return;

            switch (ShipWindowPlugin.spaceOutsideSetting.Value)
            {
                case 0: break;
                case 1:
                    if (sky == null) break;

                    sky.rotation.value += Time.deltaTime * 0.1f;
                    if (sky.rotation.value >= 360) sky.rotation.value = 0f;
                    break;
                case 2:
                    if (starSphere == null) break;

                    starSphere.Rotate(Vector3.forward * Time.deltaTime * 0.1f);
                    break;
                default: break;
            }
        }

        public void SetRotation(float r)
        {
            switch (ShipWindowPlugin.spaceOutsideSetting.Value)
            {
                case 0: break;
                case 1:
                    if (sky == null) break;

                    float rClamped = r % 360;
                    if (rClamped < 0f) rClamped += 360f;

                    sky.rotation.value = rClamped;
                    break;
                case 2:
                    if (starSphere == null) break;

                    starSphere.rotation = Quaternion.identity;
                    starSphere.Rotate(Vector3.forward * r);
                    break;
                default: break;
            }
        }
    }
}
