﻿using ShipWindows.Networking;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace ShipWindows.Components
{
    public class SpaceSkybox : MonoBehaviour
    {

        public static SpaceSkybox Instance { get; private set; }

        private HDRISky sky;

        private Transform starSphere;

        public void Awake()
        {
            Instance = this;
        }

        public void Start()
        {
            switch (WindowConfig.spaceOutsideSetting.Value)
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
            if (WindowConfig.rotateSkybox.Value == false) return;

            switch (WindowConfig.spaceOutsideSetting.Value)
            {
                case 0: break;
                case 1:
                    if (sky == null) break;

                    sky.rotation.value += Time.deltaTime * 0.1f;
                    if (sky.rotation.value >= 360) sky.rotation.value = 0f;
                    WindowState.Instance.VolumeRotation = sky.rotation.value;
                    break;
                case 2:
                    if (starSphere == null) break;

                    starSphere.Rotate(Vector3.forward * Time.deltaTime * 0.1f);
                    WindowState.Instance.VolumeRotation = starSphere.eulerAngles.y;
                    break;
                default: break;
            }
        }

        public void SetRotation(float r)
        {
            switch (WindowConfig.spaceOutsideSetting.Value)
            {
                case 0: break;
                case 1:
                    if (sky == null) break;

                    float rClamped = r % 360;
                    if (rClamped < 0f) rClamped += 360f;

                    sky.rotation.value = rClamped;
                    WindowState.Instance.VolumeRotation = sky.rotation.value;
                    break;
                case 2:
                    if (starSphere == null) break;

                    starSphere.rotation = Quaternion.identity;
                    starSphere.Rotate(Vector3.forward * r);
                    WindowState.Instance.VolumeRotation = starSphere.eulerAngles.y;
                    break;
                default: break;
            }
        }

        public void SetSkyboxTexture(Texture2D skybox)
        {
            switch (WindowConfig.spaceOutsideSetting.Value)
            {
                case 0: break;
                case 1:
                    if (sky == null) return;

                    sky.hdriSky.value = skybox;
                    break;
                default: break;
            }
        }
    }
}
