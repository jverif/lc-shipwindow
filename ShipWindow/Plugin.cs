using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using System.IO;
using System.Reflection;
using System;
using System.Collections;
using Unity.Netcode;

namespace ShipWindow
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class ShipWindowPlugin : BaseUnityPlugin
    {
        private const string modGUID = "veri.lc.shipwindow";
        private const string modName = "Ship Window";
        private const string modVersion = "1.1.0";

        private readonly Harmony harmony = new Harmony(modGUID);
        private static ShipWindowPlugin Instance;

        static internal ManualLogSource mls;

        private static AssetBundle windowBundle;

        public static GameObject newShipInstance;
        public static GameObject windowManager;
        public static GameObject universeVolume;
        public static GameObject starSphereLarge;
        public static GameObject spaceProps;
        public static GameObject renderingObject;

        private static Coroutine windowCoroutine;

        // Configuration
        public static ConfigEntry<bool> enableShutter;
        public static ConfigEntry<bool> hideSpaceProps;
        public static ConfigEntry<int> spaceOutsideSetting;

        void Awake()
        {
            if (Instance == null) Instance = this;
            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);

            enableShutter = Config.Bind<bool>("General", "EnableWindowShutter", true, "Enable the window shutter to hide level transitions? (default = true)");
            hideSpaceProps = Config.Bind<bool>("General", "HideSpaceProps", true, "Should the planet and moon outside the ship be hidden? (default = true)");
            spaceOutsideSetting = Config.Bind<int>("General", "SpaceOutside", 1, 
                "Set this value to control how the outside space looks. (0 = Let other mods handle, 1 = Space HDRI Volume (default), 2 = Black sky with stars)");

            mls.LogInfo($"Settings Shutter: {enableShutter.Value} | Hide Space Props: {hideSpaceProps.Value} | Space Outside: {spaceOutsideSetting.Value}");

            string sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            windowBundle = AssetBundle.LoadFromFile(Path.Combine(sAssemblyLocation, "ship_window"));

            if (windowBundle == null)
            {
                mls.LogError("Failed to load asset bundle! Aborting load...");
                return;
            }

            NetcodePatcher();

            harmony.PatchAll(typeof(ShipWindowPlugin));

            mls.LogInfo("Loaded successfully");
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GameNetworkManager), "Start")]
        static void InitNetwork()
        {
            if (windowManager != null)
                return;

            GameObject windowAsset = windowBundle.LoadAsset<GameObject>("Assets/LethalCompany/Mods/ShipWindow/WindowNetworkManager.prefab");
            if (windowAsset == null) { mls.LogError("Could not load Window object!"); }

            windowAsset.AddComponent<ShipWindowHandler>();
            windowAsset.GetComponent<NetworkObject>().DontDestroyWithOwner = true;
            windowAsset.GetComponent<NetworkObject>().SceneMigrationSynchronization = true;
            windowAsset.GetComponent<NetworkObject>().DestroyWithScene = false;
            NetworkManager.Singleton.AddNetworkPrefab(windowAsset);

            windowManager = windowAsset;

        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), "Awake")]
        static void ReplaceShip()
        {
            try
            {
                // ==============================================================================
                // Ship replacement
                // ==============================================================================
                GameObject baseShipObject = GameObject.Find("Environment/HangarShip/ShipInside");
                if (baseShipObject == null) return;

                GameObject newShipPrefab = windowBundle.LoadAsset<GameObject>("Assets/LethalCompany/Mods/ShipWindow/ShipInsideWithWindow.prefab");
                if (newShipPrefab == null) { mls.LogError("Could not load new ship model!"); return; }

                newShipInstance = Instantiate(newShipPrefab, baseShipObject.transform.parent);
                newShipInstance.transform.position = baseShipObject.transform.position;
                newShipInstance.transform.rotation = baseShipObject.transform.rotation;
                newShipInstance.SetActive(true);
                baseShipObject.SetActive(false);

                renderingObject = GameObject.Find("Systems/Rendering");

                if (windowManager == null)
                {
                    mls.LogError("WindowNetworkManager does not exist!");
                    return;
                }

                // ==============================================================================
                // Universe Volume
                // ==============================================================================
                GameObject originalStarSphere = GameObject.Find("Systems/Rendering/StarsSphere");
                switch (spaceOutsideSetting.Value)
                {
                    // do nothing
                    case 0:
                        break;

                    // spawn Volume sphere
                    case 1:
                        if (renderingObject == null) { mls.LogError("Could not find Systems/Rendering. Game update?"); return; }

                        GameObject universePrefab = windowBundle.LoadAsset<GameObject>("Assets/LethalCompany/Mods/ShipWindow/UniverseVolume.prefab");
                        if (universePrefab == null) { mls.LogError("Could not load universe volume prefab!"); return; }

                        universeVolume = Instantiate(universePrefab, renderingObject.transform);
                        originalStarSphere.GetComponent<MeshRenderer>().enabled = false;

                        break;

                    // spawn large star sphere
                    case 2:
                        if (originalStarSphere == null) { mls.LogError("Could not find star sphere object. Game update?"); return; }
                        if (renderingObject == null) { mls.LogError("Could not find Systems/Rendering. Game update?"); return; }

                        GameObject starSpherePrefab = windowBundle.LoadAsset<GameObject>("Assets/LethalCompany/Mods/ShipWindow/StarsSphereLarge.prefab");
                        if (starSpherePrefab == null) { mls.LogError("Could not load star sphere large prefab!"); return; }

                        starSphereLarge = Instantiate(starSpherePrefab, renderingObject.transform);
                        originalStarSphere.GetComponent<MeshRenderer>().enabled = false;
                        
                        break;

                    default:
                        break;
                }

                if (hideSpaceProps.Value == true)
                {
                    spaceProps = GameObject.Find("Environment/SpaceProps");
                    if (spaceProps != null) spaceProps.SetActive(false);
                }

                if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
                {
                    mls.LogInfo("Spawning network window...");
                    GameObject windowManagerInstance = Instantiate(windowManager);
                    windowManagerInstance.GetComponent<NetworkObject>().Spawn();
                }

            } catch(Exception e)
            {
                mls.LogError(e);
            }
            
        }

        static void TrySetWindowClosed(bool closed)
        {
            if (enableShutter.Value == false) return;

            mls.LogInfo("Setting window closed: " + closed);
            if (ShipWindowHandler.Instance != null)
            {
                if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
                    ShipWindowHandler.Instance.SetWindowStateClientRpc(closed);
                else
                    ShipWindowHandler.Instance.SetWindowState(closed);
            }
        }

        static IEnumerator OpenWindowCoroutine(float delay)
        {
            mls.LogInfo("Opening window in " + delay + " seconds...");
            yield return new WaitForSeconds(delay);
            TrySetWindowClosed(false);
            windowCoroutine = null;
        }

        // ==============================================================================
        // Patches
        // ==============================================================================

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), "LateUpdate")]
        static void StarsFollowPlayer()
        {
            // Make the stars follow the player when they get sucked out of the ship.
            if (StartOfRound.Instance.suckingPlayersOutOfShip)
            {
                if (starSphereLarge != null)
                    starSphereLarge.transform.position = GameNetworkManager.Instance.localPlayerController.transform.position;
            } else
            {
                if (starSphereLarge != null && renderingObject != null)
                    starSphereLarge.transform.position = renderingObject.transform.position;
            }
                
        }

        [HarmonyPostfix, HarmonyPatch(typeof(EnemyAI), "Start")]
        static void EnemyVisibleBase(EnemyAI __instance)
        {
            if (GameNetworkManager.Instance.localPlayerController != null)
                __instance.EnableEnemyMesh(true);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(MaskedPlayerEnemy), "Start")]
        static void EnemyVisibleMask(MaskedPlayerEnemy __instance)
        {
            if (GameNetworkManager.Instance.localPlayerController != null)
                __instance.EnableEnemyMesh(true);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), "SetPlayerSafeInShip")]
        static void EnableAllMeshes()
        {
            EnemyAI[] array = UnityEngine.Object.FindObjectsOfType<EnemyAI>();
            for (int j = 0; j < array.Length; j++)
                array[j].EnableEnemyMesh(enable: true);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartMatchLever), "StartGame")]
        static void StartOfGame()
        {
            mls.LogInfo("StartOfGame");
            TrySetWindowClosed(true);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(RoundManager), "LoadNewLevel")]
        static void OpenAfterLevelGenerates()
        {
            mls.LogInfo("OpenAfterLevelGenerates");

            if (windowCoroutine != null) StartOfRound.Instance.StopCoroutine(windowCoroutine);
            windowCoroutine = StartOfRound.Instance.StartCoroutine(OpenWindowCoroutine(4f));

            if (ShipWindowHandler.Instance != null)
            {
                mls.LogInfo("Disabling universe volume / star sphere.");
                if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
                    ShipWindowHandler.Instance.SetVolumeStateClientRpc(false);
                else
                    ShipWindowHandler.Instance.SetVolumeState(false);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), "ShipHasLeft")]
        static void EndOfGame()
        {
            mls.LogInfo("EndOfGame");
            TrySetWindowClosed(true);

            if (windowCoroutine != null) StartOfRound.Instance.StopCoroutine(windowCoroutine);
            windowCoroutine = StartOfRound.Instance.StartCoroutine(OpenWindowCoroutine(5f));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(RoundManager), "DespawnPropsAtEndOfRound")]
        static void EnableStarsAgain()
        {
            switch(spaceOutsideSetting.Value)
            {
                case 0: break;

                case 1:
                    if (universeVolume != null) universeVolume.SetActive(true);
                    break;

                case 2:
                    if (starSphereLarge != null) starSphereLarge.SetActive(true);
                    break;

                default: break;
            }

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
