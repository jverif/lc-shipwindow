using BepInEx.Configuration;

namespace ShipWindows
{
    public class WindowConfig
    {
        public static ConfigEntry<bool> vanillaMode;
        public static ConfigEntry<bool> enableShutter;
        public static ConfigEntry<bool> hideSpaceProps;
        public static ConfigEntry<int> spaceOutsideSetting;
        public static ConfigEntry<bool> disableUnderLights;
        public static ConfigEntry<bool> dontMovePosters;
        public static ConfigEntry<bool> rotateSkybox;
        public static ConfigEntry<int> skyboxResolution;

        public static ConfigEntry<bool> windowsUnlockable;

        // TODO: Prune
        public static ConfigEntry<bool> enableWindow1;
        public static ConfigEntry<bool> enableWindow2;
        public static ConfigEntry<bool> enableWindow3;

        public WindowConfig(ConfigFile cfg)
        {
            vanillaMode = cfg.Bind("General", "VanillaMode", true,
                "Enable this to preserve vanilla network compatability. This will disable unlockables and the shutter toggle switch. (default = false)");
            enableShutter =         cfg.Bind("General", "EnableWindowShutter", true, 
                "Enable the window shutter to hide transitions between space and the current moon. (default = true)");
            hideSpaceProps =        cfg.Bind("General", "HideSpaceProps", false, 
                "Should the planet and moon outside the ship be hidden? (default = false)");
            spaceOutsideSetting =   cfg.Bind("General", "SpaceOutside", 1,
                "Set this value to control how the outside space looks. (0 = Let other mods handle, 1 = Space HDRI Volume (default), 2 = Black sky with stars)");

            disableUnderLights =    cfg.Bind("General", "DisableUnderLights", false, 
                "Disable the flood lights added under the ship if you have the floor window enabled.");
            dontMovePosters =       cfg.Bind("General", "DontMovePosters", false, 
                "Don't move the poster that blocks the second window if set to true.");
            rotateSkybox =          cfg.Bind("General", "RotateSpaceSkybox", true, 
                "Enable slow rotation of the space skybox for visual effect. Requires 'SpaceOutside' to be set to 1 or 2.");
            skyboxResolution =      cfg.Bind("General", "SkyboxResolution", 0,
                "Sets the skybox resolution (0 = 2K, 1 = 4K) 4K textures may cause performance issues. Requires 'SpaceOutside' to be set to 1 or 2.");

            windowsUnlockable = cfg.Bind("General", "WindowsUnlockable", true,
                "Adds the windows to the terminal as ship upgrades. Set this to false and use below settings to have them enabled by default.");

            // If windows are set to not be purchasable...
            enableWindow1 = cfg.Bind("General", "EnableWindow1", true,
                "If not set as  purchasable, enable the window to the right of the switch, behind the terminal.");
            enableWindow2 = cfg.Bind("General", "EnableWindow2", false,
                "If not set as  purchasable, enable the window to the left of the switch, across from the first window.");
            enableWindow3 = cfg.Bind("General", "EnableWindow3", false,
                "If not set as purchasable, enable the large glass floor.");
        }
    }
}
