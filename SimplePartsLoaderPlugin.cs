using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.IO;

namespace SimplePartsLoader
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class SimplePartsLoaderPlugin : BaseUnityPlugin
    {
        public static SimplePartsLoaderPlugin Instance { get; private set; }
        public static string PartsConfigPath { get; private set; }
        public static string PartDumpsPath { get; private set; }

        private void Awake()
        {
            Instance = this;
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");


            PartsConfigPath = Path.Combine(Paths.ConfigPath, "SimplePartsLoader", "Parts");
            PartDumpsPath = Path.Combine(Paths.ConfigPath, "SimplePartsLoader", "PartDumps");

            if (!Directory.Exists(PartsConfigPath))
                Directory.CreateDirectory(PartsConfigPath);

            if (!Directory.Exists(PartDumpsPath))
                Directory.CreateDirectory(PartDumpsPath);


            PartLoader.Initialize(Logger);


            Harmony.CreateAndPatchAll(typeof(Patches.PartRegistryPatch));
        }
    }
}
