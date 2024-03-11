using BepInEx.Bootstrap;
using BepInEx;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ShipWindows.Compatibility
{

    // https://discord.com/channels/1168655651455639582/1216761387343151134
    // Automatic Soft Dependency Initializer
    // by Kittenji
    internal class CompatibleDependencyAttribute : BepInDependency
    {
        public System.Type Handler;

        /// <summary>
        /// Marks this BepInEx.BaseUnityPlugin as soft depenant on another plugin.
        /// The handler type must have an Initialize() method that will automatically be invoked if the compatible dependency is present.
        /// </summary>
        /// <param name="guid">The GUID of the referenced plugin.</param>
        /// <param name="handlerType">The class type that will handle this compatibility. Must contain a private method called Initialize()</param>
        public CompatibleDependencyAttribute(string guid, System.Type handlerType) : base(guid, DependencyFlags.SoftDependency)
        {
            Handler = handlerType;
        }

        /// <summary>
        /// Global initializer for this class.
        /// You must call this method from your base plugin Awake method and pass the plugin instance to the source parameter.
        /// </summary>
        /// <param name="source">The source plugin instance with the BepInPlugin attribute.</param>
        public static void Init(BepInEx.BaseUnityPlugin source)
        {
            const BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Static;

            IEnumerable<CompatibleDependencyAttribute> attributes = source.GetType().GetCustomAttributes<CompatibleDependencyAttribute>();
            foreach (CompatibleDependencyAttribute attr in attributes)
            {
                if (Chainloader.PluginInfos.ContainsKey(attr.DependencyGUID))
                {
                    ShipWindowPlugin.Log.LogInfo($"Found compatible mod:  {attr.DependencyGUID}");
                    attr.Handler.GetMethod("Initialize", bindingFlags)?.Invoke(null, null);

                    // we do a little hehe
                    ShipWindowPlugin.Instance.harmony.PatchAll(attr.Handler);

                    attr.Handler = null;
                } else
                {
                    // Log.Info("Compatibility not found: " + attr.DependencyGUID);
                }
            }
        }
    }
}
