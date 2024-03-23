using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace ShipWindows.Utilities
{
    static class ShipWindow4K
    {
        static DirectoryInfo baseDir = new DirectoryInfo(Assembly.GetExecutingAssembly().Location);

        public static AssetBundle TextureBundle { get; private set; }
        public static Texture2D Skybox4K { get; private set; }

        public static bool TryToLoad()
        {
            try
            {
                string pluginsFolder = baseDir.Parent.Parent.FullName;

                foreach (string file in Directory.GetFiles(pluginsFolder, "ship_window_4k", SearchOption.AllDirectories))
                {
                    FileInfo fileInfo = new FileInfo(file);
                    if (fileInfo.Extension.Equals(".old")) break;

                    TextureBundle = AssetBundle.LoadFromFile(fileInfo.FullName);
                    Skybox4K = TextureBundle.LoadAsset<Texture2D>("Assets/LethalCompany/Mods/ShipWindow/Textures/Space4KCube.png");

                    ShipWindowPlugin.Log.LogInfo("Found 4K skybox texture!");
                    return true;
                }
            } catch (Exception e)
            {
                ShipWindowPlugin.Log.LogError($"Failed to find and load 4K skybox AssetBundle!\n{e}");
                return false;
            }

            ShipWindowPlugin.Log.LogInfo("Did not locate 4K skybox bundle.");
            return false;
        }
    }
}
