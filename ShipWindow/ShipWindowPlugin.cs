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

namespace ShipWindow
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class ShipWindowPlugin : BaseUnityPlugin
    {
        private const string modGUID = "veri.lc.shipwindow";
        private const string modName = "Ship Window";
        private const string modVersion = "1.3.0";

        private readonly Harmony harmony = new Harmony(modGUID);

        public static ShipWindowPlugin Instance { get; private set; }
        static internal ManualLogSource mls;

        // Configuration
        public static ConfigEntry<bool> enableShutter;
        public static ConfigEntry<bool> hideSpaceProps;
        public static ConfigEntry<int> spaceOutsideSetting;
        public static ConfigEntry<bool> enableWindow1;
        public static ConfigEntry<bool> enableWindow2;
        public static ConfigEntry<bool> enableWindow3;

        private static AssetBundle mainAssetBundle;

        // Prefabs
        private static GameObject windowNetworkPrefab;
        private static GameObject windowSwitchPrefab;

        // Spawned objects
        public static GameObject newShipInside;
        public static GameObject universeVolume;
        public static GameObject starsSphereLarge;

        // Various
        public static int switchUnlockableID;
        private static Coroutine windowCoroutine;

        void Awake()
        {
            if (Instance == null) Instance = this;
            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);

            enableShutter = Config.Bind<bool>("General", "EnableWindowShutter", true, "Enable the window shutter to hide level transitions? (default = true)");
            hideSpaceProps = Config.Bind<bool>("General", "HideSpaceProps", true, "Should the planet and moon outside the ship be hidden? (default = true)");
            spaceOutsideSetting = Config.Bind<int>("General", "SpaceOutside", 1,
                "Set this value to control how the outside space looks. (0 = Let other mods handle, 1 = Space HDRI Volume (default), 2 = Black sky with stars)");

            enableWindow1 = Config.Bind<bool>("General", "EnableWindow1", true, "Enable the window to the right of the switch, behind the terminal.");
            enableWindow2 = Config.Bind<bool>("General", "EnableWindow2", false, "Enable the window to the left of the switch, across from the first window.");
            enableWindow3 = Config.Bind<bool>("General", "EnableWindow3", false, "Enable the large glass floor.");

            if (enableWindow1.Value == false && enableWindow2.Value == false && enableWindow3.Value == false)
            {
                mls.LogWarning("All windows are disabled. Please enable any window in your settings for this mod to have any effect.");
                return;
            }

            mls.LogInfo($"Settings Shutter: {enableShutter.Value} | Hide Space Props: {hideSpaceProps.Value} | Space Outside: {spaceOutsideSetting.Value}");

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
            bool w1 = enableWindow1.Value;
            bool w2 = enableWindow2.Value;
            bool w3 = enableWindow3.Value;
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
            winNetAsset.GetComponent<NetworkObject>().DontDestroyWithOwner = true;
            winNetAsset.GetComponent<NetworkObject>().SceneMigrationSynchronization = true;
            winNetAsset.GetComponent<NetworkObject>().DestroyWithScene = false;
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
        }

        static void AddStars()
        {
            GameObject renderingObject = GameObject.Find("Systems/Rendering");
            GameObject vanillaStarSphere = GameObject.Find("Systems/Rendering/StarsSphere");

            switch (spaceOutsideSetting.Value)
            {
                // do nothing
                case 0:
                    break;

                // spawn Volume sphere
                case 1:
                    if (renderingObject == null) throw new Exception("Could not find Systems/Rendering. Wrong scene?");

                    GameObject universePrefab = mainAssetBundle.LoadAsset<GameObject>("Assets/LethalCompany/Mods/ShipWindow/UniverseVolume.prefab");

                    universeVolume = Instantiate(universePrefab, renderingObject.transform);
                    vanillaStarSphere.GetComponent<MeshRenderer>().enabled = false;

                    break;

                // spawn large star sphere
                case 2:
                    if (vanillaStarSphere == null) throw new Exception("Could not find vanilla Stars Sphere. Wrong scene?");
                    if (renderingObject == null) throw new Exception("Could not find Systems/Rendering. Wrong scene?");

                    GameObject starSpherePrefab = mainAssetBundle.LoadAsset<GameObject>("Assets/LethalCompany/Mods/ShipWindow/StarsSphereLarge.prefab");
                    if (starSpherePrefab == null) throw new Exception("Could not load star sphere large prefab!");

                    starsSphereLarge = Instantiate(starSpherePrefab, renderingObject.transform);
                    vanillaStarSphere.GetComponent<MeshRenderer>().enabled = false;

                    break;

                default:
                    break;
            }
        }

        static void HideSpaceProps()
        {
            if (hideSpaceProps.Value == true)
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
            if (enableShutter.Value == false) return;

            mls.LogInfo("Setting window closed: " + closed);
            if (ShipWindowNetworkManager.Instance != null)
            {
                if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
                    ShipWindowNetworkManager.Instance.SetWindowStateClientRpc(closed, locked);
                else
                    ShipWindowNetworkManager.Instance.SetWindowState(closed, locked);
            }
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
        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), "LateUpdate")]
        static void Patch_RoundLateUpdate()
        {
            // Make the stars follow the player when they get sucked out of the ship.
            if (StartOfRound.Instance.suckingPlayersOutOfShip)
            {
                if (starsSphereLarge != null)
                    starsSphereLarge.transform.position = GameNetworkManager.Instance.localPlayerController.transform.position;
            } else
            {
                // TODO: Patch something else to move this back so we aren't doing it every frame.
                GameObject renderingObject = GameObject.Find("Systems/Rendering");
                if (starsSphereLarge != null && renderingObject != null)
                    starsSphereLarge.transform.position = renderingObject.transform.position;
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
            mls.LogInfo("StartOfGame");
            TrySetWindowClosed(true, true);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(RoundManager), "LoadNewLevel")]
        static void Patch_LoadNewLevel()
        {

            if (windowCoroutine != null) StartOfRound.Instance.StopCoroutine(windowCoroutine);
            windowCoroutine = StartOfRound.Instance.StartCoroutine(OpenWindowCoroutine(4f));

            if (ShipWindowNetworkManager.Instance != null)
            {
                mls.LogInfo("Disabling universe volume / star sphere.");
                if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
                    ShipWindowNetworkManager.Instance.SetVolumeStateClientRpc(false);
                else
                    ShipWindowNetworkManager.Instance.SetVolumeState(false);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), "ShipHasLeft")]
        static void Patch_ShipHasLeft()
        {
            mls.LogInfo("EndOfGame");
            TrySetWindowClosed(true, true);

            if (windowCoroutine != null) StartOfRound.Instance.StopCoroutine(windowCoroutine);
            windowCoroutine = StartOfRound.Instance.StartCoroutine(OpenWindowCoroutine(5f));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(RoundManager), "DespawnPropsAtEndOfRound")]
        static void Patch_DespawnProps()
        {
            switch (spaceOutsideSetting.Value)
            {
                case 0: break;

                case 1:
                    if (universeVolume != null) universeVolume.SetActive(true);
                    break;

                case 2:
                    if (starsSphereLarge != null) starsSphereLarge.SetActive(true);
                    break;

                default: break;
            }

            GameObject spaceProps = GameObject.Find("Environment/SpaceProps");
            if (spaceProps != null && hideSpaceProps.Value == true) spaceProps.SetActive(false);
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
