using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using ShipWindows.Components;
using GameNetcodeStuff;

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

        private static AssetBundle mainAssetBundle;

        // Prefabs
        private static GameObject windowNetworkPrefab;
        private static GameObject windowSwitchPrefab;

        // Spawned objects
        public static GameObject newShipInside;
        public static GameObject outsideSkybox;

        // Various
        public static int switchUnlockableID;
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
                + $"    Shutters:       {WindowConfig.enableShutter.Value}\n"
                + $"    Space Props:    {WindowConfig.hideSpaceProps.Value}\n"
                + $"    Space Sky:      {WindowConfig.spaceOutsideSetting.Value}\n"
                + $"    Window 1:       {WindowConfig.enableWindow1.Value}\n"
                + $"    Window 2:       {WindowConfig.enableWindow2.Value}\n"
                + $"    Window 3:       {WindowConfig.enableWindow3.Value}\n"
                + $"    Bottom Lights:  {WindowConfig.disableUnderLights.Value}\n"
                + $"    Posters:        {WindowConfig.dontMovePosters.Value}\n"
                + $"    Sky Rotation:   {WindowConfig.rotateSkybox.Value}\n"
                + $"    Sky Resolution: {WindowConfig.skyboxResolution.Value}"
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

            new WindowNetworkState();

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
            bool w1 = WindowConfig.enableWindow1.Value;
            bool w2 = WindowConfig.enableWindow2.Value;
            bool w3 = WindowConfig.enableWindow3.Value;
            return $"ShipInsideWithWindow{(w1 ? 1 : 0)}{(w2 ? 1 : 0)}{(w3 ? 1 : 0)}";
        }

        static GameObject FindOrThrow(string name)
        {
            GameObject gameObject = GameObject.Find(name);
            if (!gameObject) throw new Exception($"Could not find {name}! Wrong scene?");

            return gameObject;
        }

        static int AddSwitchToUnlockables()
        {
            string name = "Shutter Switch";
            UnlockablesList unlockablesList = StartOfRound.Instance.unlockablesList;

            // When running in unity editor this function permanently edits the unlockables list.
            // To keep from duplicating a ton, check if the unlockable is already there and use it's ID instead.

            int index = unlockablesList.unlockables.FindIndex(unlockable => unlockable.unlockableName == name);

            if (index == -1)
            {
                UnlockableItem sw = new UnlockableItem();
                sw.unlockableName = name;
                sw.spawnPrefab = false;
                sw.unlockableType = 1;
                sw.IsPlaceable = true;
                sw.maxNumber = 1;
                sw.canBeStored = false;
                sw.alreadyUnlocked = true;

                unlockablesList.unlockables.Capacity++;
                unlockablesList.unlockables.Add(sw);
                switchUnlockableID = unlockablesList.unlockables.FindIndex(unlockable => unlockable.unlockableName == name);
            } else
            {
                switchUnlockableID = index;
            }

            mls.LogInfo($"Added shutter switch to unlockables list at ID {switchUnlockableID}");

            return switchUnlockableID;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GameNetworkManager), "Start")]
        static void Patch_NetworkStart()
        {
            if (windowNetworkPrefab != null) return;

            GameObject winNetAsset = mainAssetBundle.LoadAsset<GameObject>("Assets/LethalCompany/Mods/ShipWindow/WindowNetworkManager.prefab");
            winNetAsset.AddComponent<ShipWindowNetworkManager>();
            NetworkManager.Singleton.AddNetworkPrefab(winNetAsset);

            windowNetworkPrefab = winNetAsset;

            GameObject shutterSwitchAsset = mainAssetBundle.LoadAsset<GameObject>("Assets/LethalCompany/Mods/ShipWindow/WindowShutterSwitch.prefab");
            shutterSwitchAsset.AddComponent<ShipWindowShutterSwitch>();
            NetworkManager.Singleton.AddNetworkPrefab(shutterSwitchAsset);

            windowSwitchPrefab = shutterSwitchAsset;
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
                window.gameObject.AddComponent<ShipWindow>();
        }

        static void ReplaceShip()
        {
            GameObject vanillaShipInside = FindOrThrow("Environment/HangarShip/ShipInside");

            string shipName = GetShipAssetName();

            GameObject newShipPrefab = mainAssetBundle.LoadAsset<GameObject>($"Assets/LethalCompany/Mods/ShipWindow/Ships/{shipName}.prefab");
            if (newShipPrefab == null) throw new Exception($"Could not load requested ship replacement! {shipName}");

            AddWindowScripts(newShipPrefab);

            newShipInside = Instantiate(newShipPrefab, vanillaShipInside.transform.parent);
            newShipInside.transform.position = vanillaShipInside.transform.position;
            newShipInside.transform.rotation = vanillaShipInside.transform.rotation;
            newShipInside.SetActive(true);
            vanillaShipInside.SetActive(false);

            // Rename objects
            vanillaShipInside.name = "ShipInside (Old)";
            newShipInside.name = "ShipInside";

            // Misc objects, TODO: Clean up, move to own function.

            if (WindowConfig.enableWindow2.Value == true && WindowConfig.dontMovePosters.Value == false)
            {
                GameObject movedPostersPrefab = mainAssetBundle.LoadAsset<GameObject>($"Assets/LethalCompany/Mods/ShipWindow/ShipPosters.prefab");
                if (movedPostersPrefab != null)
                {
                    Transform oldPosters = newShipInside.transform.parent.Find("Plane.001");
                    if (oldPosters != null)
                    {
                        GameObject newPosters = Instantiate(movedPostersPrefab, newShipInside.transform.parent);
                        newPosters.transform.position = oldPosters.transform.position;
                        newPosters.transform.rotation = oldPosters.transform.rotation;

                        oldPosters.name = "Plane.001 (Old)";
                        newPosters.name = "Plane.001";

                        oldPosters.gameObject.SetActive(false);
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
                mls.LogInfo("Spawning network window...");
                GameObject windowManagerInstance = Instantiate(windowNetworkPrefab);
                windowManagerInstance.GetComponent<NetworkObject>().Spawn();
            }
        }

        static void SpawnSwitch()
        {
            if (windowSwitchPrefab != null)
            {
                int id = AddSwitchToUnlockables();
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

        static void TrySetWindowClosed(bool closed, bool locked)
        {
            if (WindowConfig.enableShutter.Value == false) return;

            NetworkHandler.SetWindowState(closed, locked);

            /*mls.LogInfo("Setting window closed: " + closed);
            if (ShipWindowNetworkManager.Instance != null)
            {
                if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
                    ShipWindowNetworkManager.Instance.SetWindowStateClientRpc(closed, locked);
                else
                    ShipWindowNetworkManager.Instance.SetWindowState(closed, locked);
            }*/
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

        static void HandleSetWindowState(bool closed, bool locked)
        {
            if (WindowConfig.enableShutter.Value == true)
            {
                ShipWindow[] windows = FindObjectsByType<ShipWindow>(FindObjectsSortMode.None);

                foreach (ShipWindow w in windows)
                {
                    w.SetClosed(closed);
                    w.SetLocked(locked);
                }
                    

                WindowNetworkState.Instance.WindowsClosed = closed;
                WindowNetworkState.Instance.WindowsLocked = locked;
            }
        }

        static void HandleSetVolumeState(bool active)
        {
            var outsideSkybox = ShipWindowPlugin.outsideSkybox;
            outsideSkybox?.SetActive(active);

            WindowNetworkState.Instance.SpaceActive = active;
        }

        static void HandleWindowSyncEvent(WindowNetworkState state)
        {
            WindowNetworkState.Instance = state;
        }

        static IEnumerator OpenWindowCoroutine(float delay)
        {
            mls.LogInfo("Opening window in " + delay + " seconds...");
            yield return new WaitForSeconds(delay);
            TrySetWindowClosed(false, false);
            windowCoroutine = null;
        }

        // ==============================================================================
        // Patches
        // ==============================================================================

        [HarmonyPostfix, HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
        static void InitializeLocalPlayer()
        {
            NetworkHandler.RegisterMessages();

            NetworkHandler.SetVolumeStateEvent += HandleSetVolumeState;
            NetworkHandler.SetWindowStateEvent += HandleSetWindowState;
            NetworkHandler.WindowSyncReceivedEvent += HandleWindowSyncEvent;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GameNetworkManager), "StartDisconnect")]
        static void PlayerLeave()
        {
            NetworkHandler.UnregisterMessages();

            NetworkHandler.SetVolumeStateEvent -= HandleSetVolumeState;
            NetworkHandler.SetWindowStateEvent -= HandleSetWindowState;
            NetworkHandler.WindowSyncReceivedEvent -= HandleWindowSyncEvent;
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

        [HarmonyPostfix, HarmonyPatch(typeof(StartMatchLever), "StartGame")]
        static void Patch_StartGame()
        {
            mls.LogInfo($"StartMatchLever.StartGame -> Is Host:{NetworkHandler.IsHost} / Is Client:{NetworkHandler.IsClient} ");
            TrySetWindowClosed(true, true);
        }

        // TODO: This does not need to be networked anymore.
        [HarmonyPostfix, HarmonyPatch(typeof(RoundManager), "FinishGeneratingNewLevelClientRpc")]
        static void Patch_OpenDoorSequence()
        {
            mls.LogInfo($"RoundManager.FinishGeneratingNewLevelClientRpc -> Is Host:{NetworkHandler.IsHost} / Is Client:{NetworkHandler.IsClient} ");
            if (windowCoroutine != null) StartOfRound.Instance.StopCoroutine(windowCoroutine);
            windowCoroutine = StartOfRound.Instance.StartCoroutine(OpenWindowCoroutine(2f));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(RoundManager), "LoadNewLevel")]
        static void Patch_LoadNewLevel()
        {
            mls.LogInfo($"RoundManager.LoadNewLevel -> Is Host:{NetworkHandler.IsHost} / Is Client:{NetworkHandler.IsClient} ");
            NetworkHandler.SetVolumeState(false);

            /*if (ShipWindowNetworkManager.Instance != null)
            {
                mls.LogInfo("Disabling universe volume / star sphere.");
                if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
                    ShipWindowNetworkManager.Instance.SetVolumeStateClientRpc(false);
                else
                    ShipWindowNetworkManager.Instance.SetVolumeState(false);
            }*/
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), "ShipHasLeft")]
        static void Patch_ShipHasLeft()
        {
            TrySetWindowClosed(true, true);

            if (windowCoroutine != null) StartOfRound.Instance.StopCoroutine(windowCoroutine);
            windowCoroutine = StartOfRound.Instance.StartCoroutine(OpenWindowCoroutine(5f));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(RoundManager), "DespawnPropsAtEndOfRound")]
        static void Patch_DespawnProps()
        {
            switch (WindowConfig.spaceOutsideSetting.Value)
            {
                case 0: break;

                case 1:
                case 2:
                    // If for whatever reason this code errors, the game breaks.
                    int? dayNum = StartOfRound.Instance.gameStats?.daysSpent;
                    SpaceSkybox.Instance?.SetRotation((dayNum ?? 1) * 80f);
                    outsideSkybox?.SetActive(true);
                    break;

                default: break;
            }

            GameObject spaceProps = GameObject.Find("Environment/SpaceProps");
            if (spaceProps != null && WindowConfig.hideSpaceProps.Value == true) spaceProps.SetActive(false);
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
