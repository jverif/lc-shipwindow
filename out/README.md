# Ship Windows
Adds three glass windows to the ship's store as permanent ship upgrades. Availability and price both configurable.

![Screenshot_1](https://raw.githubusercontent.com/jverif/lc-shipwindow/main/Screenshots/showcase.png "Showcase")

## Compatibility
- Toggle "VanillaMode" in your configuration to make this mod client-side only and compatible with vanilla lobbies.
- This mod is not compatible with other mods that change the ship's model.

**Important: If you are updating from < 2.0.0, you will need to enable windows 2 and 3 in your config for them to be purchasable!**

Please report any issues on GitHub or in the #mod-releases thread in the Lethal Company Modding Discord here: https://discord.com/channels/1168655651455639582/1203500753939988532

Thunderstore Link: https://thunderstore.io/c/lethal-company/p/veri/ShipWindows/

## Recommended Mods
- **[Celestial Tint](https://thunderstore.io/c/lethal-company/p/sfDesat/Celestial_Tint/)** by **sfDesat** - For an amazing view of space outside the windows.
OR
- **[Ship Windows 4K Skybox](https://thunderstore.io/c/lethal-company/p/veri/ShipWindows_4K_Skybox/)** by **veri** - For a higher definition space skybox. This does not work with Celestial Tint.

## Planned Features
- Update window shapes and add trim.
- Update shutter mesh
- Window decorations (curtains, etc.)
- Open / close sounds.
- Door windows

## Thanks to
- Soup (@souper194) - Shutter texture used on versions >= 1.2.0

## Update History

- **2.0.3**
    - Add support for custom poster materials when Left window is active.
    - Disable refractive glass by default. It can be enabled in the config still.
    - Moved 4K skybox to a separate mod / addon to reduce main bundle size.

- **2.0.2**
    - Fix black sky with certain mod configurations.
    - Don't show windows unlocked by default in Ship Upgrades section.

- **2.0.1**
    - Fix issue causing furniture desync.

- **2.0.0**
    - Windows as unlockables. Use the terminal store to purchase windows for your ship.
    - Vanilla compatibility option (disables switch and unlockables).
    - Detect Celestial Tint for better compatibility.

- **1.3.6**
    - Fix a bug breaking the lever after the first day.

- **1.3.5**
    - Patch for General Improvements compatibility

- **1.3.4**
    - Compatibilty patch for Lethal Expansion with view distance mods.
    - Actually make default skybox 2K :)

- **1.3.3**
    - Move 4K skybox behind config setting. Make default 2K.
    - Fix skybox seam and increase exposure slightly.

- **1.3.2**
    - Space skybox texture 2K -> 4K.
    - Made floor window a bit smaller so the ship's supports don't intersect it.
    - Add slow rotation to outside skybox (Both HDRI Sky & Star Sphere) for visual effect.
    - Fix layer and tag for floor window

- **1.3.1**
    - Window mesh improvements.
    - Make sure all ship variants have new window design.
    - Fix window 2 shutter not fully covering window.

- **1.3.0**
    - Add two new windows. One is across from the terminal, and the other is on the floor. These are disabled by default. To enable them, toggle the appropriate settings in your configuration.
    - Re-shape existing windows.

- **1.2.0**
    - Replace shutter texture (Thank you Soup (souper194))
    - Add a switch to toggle the window shutter. It can be moved like any other furniture.
    - Re-enable fog in ship lobby.

- **1.1.2**
    - Make window a little less blurry.
    - Fix network desync, add some safeguards.

- **1.1.1**
    - Update readme and add website link.

- **1.1.0**
    - Add config settings for controlling window shutter, outside space props, and outside space volume.

- **1.0.7**
    - Fix network object being destroyed for some reason. (?)

- **1.0.6**
    - Re-add window shutter and outside view in space.

- **1.0.5**
    - Disable window shutter and stars (big oof). Will add back soon.

- **1.0.4**
    - Add a shutter to the window while loading.
    - Fix stars being visible at Company Building.

- **1.0.3**
    - Fix stars being always visible and enemies being invisible through glass.

- **1.0.2**
    - Update description.

- **1.0.1**
    - Added stars outside the window while in space.

- **1.0.0**
    - Initial release.