using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using ShipWindows.Components;
using GameNetcodeStuff;
using ShipWindows.Utilities;
using ShipWindows.Networking;
using System.Collections.Generic;

namespace ShipWindows
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class ShipWindowPlugin : BaseUnityPlugin
    {
        private const string modGUID = "veri.lc.shipwindow";
        private const string modName = "Ship Window";
        private const string modVersion = "2.0.0";

        private readonly Harmony harmony = new Harmony(modGUID);

        public static ShipWindowPlugin Instance { get; private set; }
        public static WindowConfig Cfg { get; internal set; }
        static internal ManualLogSource mls;

        public static AssetBundle mainAssetBundle;

        // Game Objects
        private static GameObject vanillaShipInside;

        // Prefabs
        private static GameObject windowSwitchPrefab;
        private static Dictionary<int, ShipWindowDef> windowRegistry = new();

        // Spawned objects
        public static GameObject newShipInside;
        public static GameObject outsideSkybox;

        // Various
        private static Coroutine windowCoroutine;

        public static string[] window3DisabledList = [
            "UnderbellyMachineParts",
            "NurbsPath.001"
        ];

        void Awake()
        {
            if (Instance == null) Instance = this;
            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);
            Cfg = new(base.Config);

            if (WindowConfig.enableWindow1.Value == false && WindowConfig.enableWindow2.Value == false && WindowConfig.enableWindow3.Value == false)
            {
                mls.LogWarning("All windows are disabled. Please enable any window in your settings for this mod to have any effect.");
                return;
            }

            mls.LogInfo($"\nCurrent settings:\n"
                + $"    Vanilla Mode:       {WindowConfig.vanillaMode.Value}\n"
                + $"    Shutters:           {WindowConfig.enableShutter.Value}\n"
                + $"    Hide Space Props:   {WindowConfig.hideSpaceProps.Value}\n"
                + $"    Space Sky:          {WindowConfig.spaceOutsideSetting.Value}\n"
                + $"    Bottom Lights:      {WindowConfig.disableUnderLights.Value}\n"
                + $"    Posters:            {WindowConfig.dontMovePosters.Value}\n"
                + $"    Sky Rotation:       {WindowConfig.rotateSkybox.Value}\n"
                + $"    Sky Resolution:     {WindowConfig.skyboxResolution.Value}\n"
                + $"    Windows Unlockable: {WindowConfig.windowsUnlockable.Value}\n"

                + $"    Window 1 Enabled:   {WindowConfig.enableWindow1.Value}\n"
                + $"    Window 2 Enabled:   {WindowConfig.enableWindow2.Value}\n"
                + $"    Window 3 Enabled:   {WindowConfig.enableWindow3.Value}\n"
            );

            

            if (!LoadAssetBundle())
            {
                mls.LogError("Failed to load asset bundle! Abort mission!");
                return;
            }

            try
            {
                NetcodePatcher();
            } catch(Exception e)
            {
                mls.LogError("Something went wrong with the netcode patcher!");
                mls.LogError(e);
                return;
            }

            new WindowState();

            RegisterWindows();

            harmony.PatchAll(typeof(ShipWindowPlugin));
            mls.LogInfo("Loaded successfully!");
        }

        bool LoadAssetBundle()
        {
            mls.LogInfo("Loading ShipWindow AssetBundle...");
            string sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            mainAssetBundle = AssetBundle.LoadFromFile(Path.Combine(sAssemblyLocation, "ship_window"));

            if (mainAssetBundle == null)
                return false;

            return true;
        }

        static string GetShipAssetName()
        {
            if (WindowConfig.windowsUnlockable.Value == true && WindowConfig.vanillaMode.Value == false)
            {
                bool w1 = WindowState.Instance.Window1Active;
                bool w2 = WindowState.Instance.Window2Active;
                bool w3 = WindowState.Instance.Window3Active;
                return $"ShipInsideWithWindow{(w1 ? 1 : 0)}{(w2 ? 1 : 0)}{(w3 ? 1 : 0)}";
            } else
            {
                bool w1 = WindowConfig.enableWindow1.Value;
                bool w2 = WindowConfig.enableWindow2.Value;
                bool w3 = WindowConfig.enableWindow3.Value;
                return $"ShipInsideWithWindow{(w1 ? 1 : 0)}{(w2 ? 1 : 0)}{(w3 ? 1 : 0)}";
            }
        }

        static GameObject FindOrThrow(string name)
        {
            GameObject gameObject = GameObject.Find(name);
            if (!gameObject) throw new Exception($"Could not find {name}! Wrong scene?");

            return gameObject;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GameNetworkManager), "Start")]
        static void Patch_NetworkStart()
        {
            if (WindowConfig.vanillaMode.Value == true) return;

            GameObject shutterSwitchAsset = mainAssetBundle.LoadAsset<GameObject>("Assets/LethalCompany/Mods/ShipWindow/WindowShutterSwitch.prefab");
            shutterSwitchAsset.AddComponent<ShipWindowShutterSwitch>();
            NetworkManager.Singleton.AddNetworkPrefab(shutterSwitchAsset);

            windowSwitchPrefab = shutterSwitchAsset;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Terminal), "Awake")]
        static void Patch_TerminalAwake(Terminal __instance)
        {
            if (WindowConfig.vanillaMode.Value == true) return;

            foreach (var entry in windowRegistry)
            {
                Unlockables.AddWindowToUnlockables(__instance, entry.Value);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), "Awake")]
        static void Patch_RoundAwake()
        {
            try
            {
                ReplaceShip();
                AddStars();
                HideSpaceProps();
                SpawnNetworkManager();
                SpawnSwitch();

            } catch(Exception e)
            {
                mls.LogError(e);
            }
            
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), "Start")]
        static void Patch_RoundStart()
        {
            try
            {
                RunCompatPatches();
            } catch (Exception e)
            {
                mls.LogError(e);
            }

        }

        static void AddWindowScripts(GameObject ship)
        {
            Transform container = ship.transform.Find("WindowContainer");
            if (container == null) return;

            foreach (Transform window in container)
            {
                int id;
                if (int.TryParse(window.gameObject.name[window.name.Length - 1].ToString(), out id)) {
                    window.gameObject.AddComponent<ShipWindow>().ID = id;
                }
            }
        }

        static int GetWindowBaseCost(int id)
        {
            switch(id)
            {
                case 1: return WindowConfig.window1Cost.Value;
                case 2: return WindowConfig.window2Cost.Value;
                case 3: return WindowConfig.window3Cost.Value;
            }

            return 60; // Shouldn't happen, but just in case.
        }
        
        static void RegisterWindows()
        {
            for (int id = 1; id <= 3; id++)
            {
                ShipWindowDef def = ShipWindowDef.Register(id, GetWindowBaseCost(id));
                windowRegistry.Add(id, def);
            }
        }

        static void ReplaceShip()
        {
            vanillaShipInside = FindOrThrow("Environment/HangarShip/ShipInside");

            string shipName = GetShipAssetName();

            GameObject newShipPrefab = mainAssetBundle.LoadAsset<GameObject>($"Assets/LethalCompany/Mods/ShipWindow/Ships/{shipName}.prefab");
            if (newShipPrefab == null) throw new Exception($"Could not load requested ship replacement! {shipName}");

            AddWindowScripts(newShipPrefab);

            newShipInside = ObjectReplacer.Replace(vanillaShipInside, newShipPrefab);

            // Misc objects, TODO: Clean up, move to own function.

            if (WindowConfig.enableWindow2.Value == true && WindowConfig.dontMovePosters.Value == false)
            {
                GameObject movedPostersPrefab = mainAssetBundle.LoadAsset<GameObject>($"Assets/LethalCompany/Mods/ShipWindow/ShipPosters.prefab");
                if (movedPostersPrefab != null)
                {
                    Transform oldPosters = newShipInside.transform.parent.Find("Plane.001");
                    if (oldPosters != null)
                    {
                        ObjectReplacer.Replace(oldPosters.gameObject, movedPostersPrefab);
                    }
                }
            }

            if (WindowConfig.enableWindow3.Value == false) return;
            mls.LogInfo($"Disabling misc objects under ship... {WindowConfig.enableWindow3.Value}");

            foreach (string go in window3DisabledList)
            {
                var obj = GameObject.Find($"Environment/HangarShip/{go}");
                if (obj == null) {
                    mls.LogWarning($"Searched for {go}, but could not find!");
                    continue; 
                };

                mls.LogInfo($"Found {go}, disabling...");

                obj.gameObject.SetActive(false);
            }

            if (WindowConfig.disableUnderLights.Value == true)
            {
                mls.LogInfo("Disabling flood lights under ship...");
                Transform floodLights = newShipInside.transform.Find("WindowContainer/Window3/Lights");
                if (floodLights != null) floodLights.gameObject.SetActive(false);
            }
        }

        static void AddStars()
        {
            GameObject renderingObject = GameObject.Find("Systems/Rendering");
            GameObject vanillaStarSphere = GameObject.Find("Systems/Rendering/StarsSphere");

            switch (WindowConfig.spaceOutsideSetting.Value)
            {
                // do nothing
                case 0:
                    break;

                // spawn Volume sphere
                case 1:
                    if (renderingObject == null) throw new Exception("Could not find Systems/Rendering. Wrong scene?");

                    GameObject universePrefab = mainAssetBundle.LoadAsset<GameObject>("Assets/LethalCompany/Mods/ShipWindow/UniverseVolume.prefab");

                    outsideSkybox = Instantiate(universePrefab, renderingObject.transform);
                    vanillaStarSphere.GetComponent<MeshRenderer>().enabled = false;

                    outsideSkybox.AddComponent<SpaceSkybox>();

                    // Load texture
                    if (WindowConfig.skyboxResolution.Value == 1) // 4K
                    {
                        Texture2D skybox4K = mainAssetBundle.LoadAsset<Texture2D>("Assets/LethalCompany/Mods/ShipWindow/Textures/Space4KCube.png");
                        if (skybox4K != null)
                            outsideSkybox.GetComponent<SpaceSkybox>()?.SetSkyboxTexture(skybox4K);
                    }

                    break;

                // spawn large star sphere
                case 2:
                    if (vanillaStarSphere == null) throw new Exception("Could not find vanilla Stars Sphere. Wrong scene?");
                    if (renderingObject == null) throw new Exception("Could not find Systems/Rendering. Wrong scene?");

                    GameObject starSpherePrefab = mainAssetBundle.LoadAsset<GameObject>("Assets/LethalCompany/Mods/ShipWindow/StarsSphereLarge.prefab");
                    if (starSpherePrefab == null) throw new Exception("Could not load star sphere large prefab!");

                    outsideSkybox = Instantiate(starSpherePrefab, renderingObject.transform);
                    vanillaStarSphere.GetComponent<MeshRenderer>().enabled = false;

                    outsideSkybox.AddComponent<SpaceSkybox>();

                    break;

                default:
                    break;
            }
        }

        static void HideSpaceProps()
        {
            if (WindowConfig.hideSpaceProps.Value == true)
            {
                GameObject spaceProps = GameObject.Find("Environment/SpaceProps");
                if (spaceProps != null) spaceProps.SetActive(false);
            }
        }

        static void SpawnNetworkManager()
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                //mls.LogInfo("Spawning network window...");
                //GameObject windowManagerInstance = Instantiate(windowNetworkPrefab);
                //windowManagerInstance.GetComponent<NetworkObject>().Spawn();
            }
        }

        static void SpawnSwitch()
        {
            if (windowSwitchPrefab != null)
            {
                int id = Unlockables.AddSwitchToUnlockables();
                var shipObject = windowSwitchPrefab.GetComponentInChildren<PlaceableShipObject>();
                shipObject.unlockableID = id;
            }

            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                mls.LogInfo("Spawning shutter switch...");
                if (windowSwitchPrefab != null)
                {
                    GameObject switchInstance = Instantiate(windowSwitchPrefab);
                    switchInstance.GetComponent<NetworkObject>().Spawn();
                }
            }
        }

        static void RunCompatPatches()
        {
            // https://github.com/jverif/lc-shipwindow/issues/8
            // Lethal Expansion "terrainfixer" is positioned at 0, -500, 0 and becomes
            // visible when a mod that increases view distance is installed.
            GameObject terrainfixer = GameObject.Find("terrainfixer");
            if (terrainfixer != null)
            {
                terrainfixer.transform.position = new Vector3(0, -5000, 0);
            }
        }

        public static void OpenWindowDelayed(float delay)
        {
            if (windowCoroutine != null) StartOfRound.Instance.StopCoroutine(windowCoroutine);
            windowCoroutine = StartOfRound.Instance.StartCoroutine(OpenWindowCoroutine(delay));
        }

        private static IEnumerator OpenWindowCoroutine(float delay)
        {
            mls.LogInfo("Opening window in " + delay + " seconds...");
            yield return new WaitForSeconds(delay);
            WindowState.Instance.SetWindowState(false, false);
            windowCoroutine = null;
        }

        private static void HandleWindowSync()
        {
            WindowState.Instance.ReceiveSync();
        }

        // ==============================================================================
        // Patches
        // ==============================================================================

        [HarmonyPostfix, HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
        static void Patch_InitializeLocalPlayer()
        {
            NetworkHandler.RegisterMessages();
            NetworkHandler.WindowSyncReceivedEvent += HandleWindowSync;

            if (!NetworkHandler.IsHost)
            {
                NetworkHandler.RequestWindowSync();
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GameNetworkManager), "StartDisconnect")]
        static void Patch_PlayerLeave()
        {
            NetworkHandler.UnregisterMessages();
            NetworkHandler.WindowSyncReceivedEvent -= HandleWindowSync;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), "LateUpdate")]
        static void Patch_RoundLateUpdate()
        {
            // Make the stars follow the player when they get sucked out of the ship.
            if (StartOfRound.Instance.suckingPlayersOutOfShip)
            {
                if (outsideSkybox != null)
                    outsideSkybox.transform.position = GameNetworkManager.Instance.localPlayerController.transform.position;
            } else
            {
                // TODO: Patch something else to move this back so we aren't doing it every frame.
                GameObject renderingObject = GameObject.Find("Systems/Rendering");
                if (outsideSkybox != null && renderingObject != null)
                    outsideSkybox.transform.position = renderingObject.transform.position;
            }

        }

        [HarmonyPostfix, HarmonyPatch(typeof(EnemyAI), "Start")]
        static void Patch_AIStart(EnemyAI __instance)
        {
            if (GameNetworkManager.Instance.localPlayerController != null)
                __instance.EnableEnemyMesh(true);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(MaskedPlayerEnemy), "Start")]
        static void Patch_MaskStart(MaskedPlayerEnemy __instance)
        {
            if (GameNetworkManager.Instance.localPlayerController != null)
                __instance.EnableEnemyMesh(true);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), "SetPlayerSafeInShip")]
        static void Patch_SafeInShip()
        {
            EnemyAI[] array = UnityEngine.Object.FindObjectsOfType<EnemyAI>();
            for (int j = 0; j < array.Length; j++)
                array[j].EnableEnemyMesh(enable: true);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartMatchLever), "PullLeverAnim")]
        static void Patch_StartGame(bool leverPulled)
        {
            mls.LogInfo($"StartMatchLever.StartGame -> Is Host:{NetworkHandler.IsHost} / Is Client:{NetworkHandler.IsClient} ");
            if (leverPulled)
            {
                WindowState.Instance.SetWindowState(true, true);
            }
        }

        // TODO: This does not need to be networked anymore.
        [HarmonyPostfix, HarmonyPatch(typeof(RoundManager), "FinishGeneratingNewLevelClientRpc")]
        static void Patch_OpenDoorSequence()
        {
            mls.LogInfo($"RoundManager.FinishGeneratingNewLevelClientRpc -> Is Host:{NetworkHandler.IsHost} / Is Client:{NetworkHandler.IsClient} ");
            OpenWindowDelayed(2f);
            WindowState.Instance.SetVolumeState(false);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), "ShipHasLeft")]
        static void Patch_ShipHasLeft()
        {
            mls.LogInfo($"StartOfRound.ShipHasLeft -> Is Host:{NetworkHandler.IsHost} / Is Client:{NetworkHandler.IsClient} ");
            WindowState.Instance.SetWindowState(true, true);
            OpenWindowDelayed(5f);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(RoundManager), "DespawnPropsAtEndOfRound")]
        static void Patch_DespawnProps()
        {
            mls.LogInfo($"RoundManager.DespawnPropsAtEndOfRound -> Is Host:{NetworkHandler.IsHost} / Is Client:{NetworkHandler.IsClient} ");
            switch (WindowConfig.spaceOutsideSetting.Value)
            {
                case 0: break;

                case 1:
                case 2:
                    // If for whatever reason this code errors, the game breaks.
                    int? dayNum = StartOfRound.Instance.gameStats?.daysSpent;
                    float rotation = (dayNum ?? 1) * 80f;
                    WindowState.Instance.SetVolumeRotation(rotation);
                    WindowState.Instance.SetVolumeState(true);
                    break;

                default: break;
            }

            GameObject spaceProps = GameObject.Find("Environment/SpaceProps");
            if (spaceProps != null && WindowConfig.hideSpaceProps.Value == true) spaceProps.SetActive(false);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), "ResetShip")]
        static void Patch_ResetShip()
        {
            mls.LogInfo($"StartOfRound.ResetShip -> Is Host:{NetworkHandler.IsHost} / Is Client:{NetworkHandler.IsClient} ");

            if (WindowConfig.windowsUnlockable.Value == true && WindowConfig.vanillaMode.Value == false)
            {
                ObjectReplacer.Restore(vanillaShipInside);
            }
        }

        private static void NetcodePatcher()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        }
    }
}
