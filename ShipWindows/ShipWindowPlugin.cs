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
using System.Linq;
using BepInEx.Bootstrap;
using ShipWindows.Compatibility;

namespace ShipWindows
{
    [BepInPlugin(modGUID, modName, modVersion)]

    [CompatibleDependency("NightSkyPlugin", typeof(CelestialTint))]
    [CompatibleDependency("LethalExpansion", typeof(LethalExpansion))]
    [CompatibleDependency("com.github.lethalmods.lethalexpansioncore", typeof(LethalExpansion))]

    public class ShipWindowPlugin : BaseUnityPlugin
    {
        private const string modGUID = "veri.lc.shipwindow";
        private const string modName = "Ship Window";
        private const string modVersion = "2.0.0";

        public readonly Harmony harmony = new Harmony(modGUID);

        public static ShipWindowPlugin Instance { get; private set; }
        public static WindowConfig Cfg { get; internal set; }
        static internal ManualLogSource Log;

        public static AssetBundle mainAssetBundle;

        // Prefabs
        public static GameObject windowSwitchPrefab;
        public static Dictionary<int, ShipWindowDef> windowRegistry = [];

        // Vanilla object references
        public static GameObject spaceProps;

        // Spawned objects
        public static GameObject outsideSkybox;

        // Various
        private static Coroutine windowCoroutine;

        void Awake()
        {
            if (Instance == null) Instance = this;
            Log = BepInEx.Logging.Logger.CreateLogSource(modGUID);
            Cfg = new(base.Config);

            if (WindowConfig.enableWindow1.Value == false && WindowConfig.enableWindow2.Value == false && WindowConfig.enableWindow3.Value == false)
            {
                Log.LogWarning("All windows are disabled. Please enable any window in your settings for this mod to have any effect.");
                return;
            }

            Log.LogInfo($"\nCurrent settings:\n"
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
                Log.LogError("Failed to load asset bundle! Abort mission!");
                return;
            }

            try
            {
                NetcodePatcher();
            } catch(Exception e)
            {
                Log.LogError("Something went wrong with the netcode patcher!");
                Log.LogError(e);
                return;
            }

            new WindowState();

            harmony.PatchAll(typeof(ShipWindowPlugin));
            harmony.PatchAll(typeof(Unlockables));

            CompatibleDependencyAttribute.Init(this);

            Log.LogInfo("Loaded successfully!");
        }

        bool LoadAssetBundle()
        {
            Log.LogInfo("Loading ShipWindow AssetBundle...");
            string sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            mainAssetBundle = AssetBundle.LoadFromFile(Path.Combine(sAssemblyLocation, "ship_window"));

            if (mainAssetBundle == null)
                return false;

            return true;
        }

        static GameObject FindOrThrow(string name)
        {
            GameObject gameObject = GameObject.Find(name);
            if (!gameObject) throw new Exception($"Could not find {name}! Wrong scene?");

            return gameObject;
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

        public static bool IsWindowEnabled(int id)
        {
            switch (id)
            {
                case 1: return WindowConfig.enableWindow1.Value;
                case 2: return WindowConfig.enableWindow2.Value;
                case 3: return WindowConfig.enableWindow3.Value;
            }

            return false;
        }

        public static bool IsWindowDefaultUnlocked(int id)
        {
            switch (id)
            {
                case 1: return WindowConfig.defaultWindow1.Value;
                case 2: return WindowConfig.defaultWindow2.Value;
                case 3: return WindowConfig.defaultWindow3.Value;
            }

            return false;
        }

        static void RegisterWindows()
        {
            for (int id = 1; id <= 3; id++)
            {
                if (!IsWindowEnabled(id)) continue;

                ShipWindowDef def = ShipWindowDef.Register(id, GetWindowBaseCost(id));
                windowRegistry.Add(id, def);
            }
        }

        static void AddStars()
        {
            if (CelestialTint.Enabled) return;

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
            if (CelestialTint.Enabled == true) return;

            if (WindowConfig.hideSpaceProps.Value == true)
            {
                GameObject spaceProps = GameObject.Find("Environment/SpaceProps");
                if (spaceProps != null) spaceProps.SetActive(false);
            }
        }

        public static void OpenWindowDelayed(float delay)
        {
            if (windowCoroutine != null) StartOfRound.Instance.StopCoroutine(windowCoroutine);
            windowCoroutine = StartOfRound.Instance.StartCoroutine(OpenWindowCoroutine(delay));
        }

        private static IEnumerator OpenWindowCoroutine(float delay)
        {
            Log.LogInfo("Opening window in " + delay + " seconds...");
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

        [HarmonyPostfix, HarmonyPatch(typeof(GameNetworkManager), "Start")]
        static void Patch_NetworkStart()
        {
            if (WindowConfig.vanillaMode.Value == true) return;

            GameObject shutterSwitchAsset = mainAssetBundle.LoadAsset<GameObject>("Assets/LethalCompany/Mods/ShipWindow/WindowShutterSwitch.prefab");
            shutterSwitchAsset.AddComponent<ShipWindowShutterSwitch>();
            NetworkManager.Singleton.AddNetworkPrefab(shutterSwitchAsset);

            windowSwitchPrefab = shutterSwitchAsset;

            RegisterWindows();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Terminal), "Awake")]
        static void Patch_TerminalAwake(Terminal __instance)
        {
            try
            {
                if (WindowConfig.windowsUnlockable.Value == false || WindowConfig.vanillaMode.Value == true) return;

                foreach (var entry in windowRegistry)
                {
                    int id = Unlockables.AddWindowToUnlockables(__instance, entry.Value);
                    entry.Value.UnlockableID = id;
                }
            } catch(Exception e)
            {
                Log.LogError($"Error occurred registering window unlockables...\n{e}");
            }
            
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), "Awake")]
        static void Patch_RoundAwake()
        {
            try
            {
                if (WindowConfig.vanillaMode.Value == false)
                    Unlockables.AddSwitchToUnlockables();

                // The debounce coroutine is cancelled when quitting the game because StartOfRound is destroyed.
                // This means the flag doesn't get reset. So, we have to manually reset it at the start.
                ShipReplacer.debounceReplace = false;

            } catch (Exception e)
            {
                Log.LogError(e);
            }

        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), "Start")]
        static void Patch_RoundStart()
        {
            try
            {

                if (WindowConfig.windowsUnlockable.Value == false || WindowConfig.vanillaMode.Value == true)
                    ShipReplacer.ReplaceShip();

                AddStars();
                HideSpaceProps();

            } catch (Exception e)
            {
                Log.LogError(e);
            }

        }

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
            if (CelestialTint.Enabled == true) return;
            // Make the stars follow the player when they get sucked out of the ship.
            if (outsideSkybox != null)
            {
                if (StartOfRound.Instance.suckingPlayersOutOfShip)
                {
                    outsideSkybox.transform.position = GameNetworkManager.Instance.localPlayerController.transform.position;
                } else
                {
                    outsideSkybox.transform.localPosition = Vector3.zero;
                }
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
            //Log.LogInfo($"StartMatchLever.StartGame -> Is Host:{NetworkHandler.IsHost} / Is Client:{NetworkHandler.IsClient} ");
            if (leverPulled)
            {
                WindowState.Instance.SetWindowState(true, true);
            }
        }

        // TODO: This does not need to be networked anymore.
        [HarmonyPostfix, HarmonyPatch(typeof(RoundManager), "FinishGeneratingNewLevelClientRpc")]
        static void Patch_OpenDoorSequence()
        {
            //Log.LogInfo($"RoundManager.FinishGeneratingNewLevelClientRpc -> Is Host:{NetworkHandler.IsHost} / Is Client:{NetworkHandler.IsClient} ");
            OpenWindowDelayed(2f);
            WindowState.Instance.SetVolumeState(false);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), "ShipHasLeft")]
        static void Patch_ShipHasLeft()
        {
            //Log.LogInfo($"StartOfRound.ShipHasLeft -> Is Host:{NetworkHandler.IsHost} / Is Client:{NetworkHandler.IsClient} ");
            WindowState.Instance.SetWindowState(true, true);
            OpenWindowDelayed(5f);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), "ResetShip")]
        static void Patch_ResetShip()
        {
            StartOfRound.Instance.StartCoroutine(ShipReplacer.CheckForKeptSpawners());
        }

        [HarmonyPostfix, HarmonyPatch(typeof(RoundManager), "DespawnPropsAtEndOfRound")]
        static void Patch_DespawnProps()
        {
            //Log.LogInfo($"RoundManager.DespawnPropsAtEndOfRound -> Is Host:{NetworkHandler.IsHost} / Is Client:{NetworkHandler.IsClient} ");

            if (CelestialTint.Enabled == true) return;

            try
            {
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
            } catch(Exception e)
            {
                Log.LogError(e);
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
