using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SimplePartsLoader.Patches
{
    [HarmonyPatch(typeof(PartPrefabs), "Awake")]
    public class PartRegistryPatch
    {
        // Use Postfix to ensure array is populated before we add to it (or Pre if we want to add before init, but Post is safer for appending)
        [HarmonyPostfix]
        public static void Postfix(PartPrefabs __instance)
        {
            if (__instance == null) return;

            // 1. Dump existing parts (First Run Logic)
            PartLoader.DumpParts(__instance.partPrefabs);

            // 2. Load Custom Parts
            // We need to clone existing parts from the registry to create new ones
            List<GameObject> newParts = new List<GameObject>();

            foreach (var data in PartLoader.CustomParts)
            {
                // Find base prefab
                GameObject basePrefab = __instance.partPrefabs.FirstOrDefault(p => p.name == data.basePrefabName);
                if (basePrefab == null)
                {
                    PartLoader.PluginLogger.LogError($"Could not find base prefab '{data.basePrefabName}' for custom part '{data.id}'");
                    continue;
                }

                // Instantiate (Clone)
                // Note: Instantiating creates a scene object. We want a prefab-like object.
                // We should keep it inactive.
                GameObject newPart = GameObject.Instantiate(basePrefab);
                newPart.name = data.id;
                newPart.SetActive(false); // Hide until built
                GameObject.DontDestroyOnLoad(newPart); // Persist across scenes

                // Apply Overrides
                if (data.overrides != null)
                {
                    PartLoader.ApplyStats(newPart, data.overrides);
                }

                // Update UI Data
                var buildPart = newPart.GetComponent<BuildingPart>();
                if (buildPart)
                {
                    buildPart.partName = data.name;
                    buildPart.price = data.price;
                    // TODO: Icon
                }

                newParts.Add(newPart);
                PartLoader.PluginLogger.LogInfo($"Registered custom part: {data.id}");
            }

            // 3. Inject into Array
            if (newParts.Count > 0)
            {
                var list = __instance.partPrefabs.ToList();
                list.AddRange(newParts);
                __instance.partPrefabs = list.ToArray();
            }
        }
    }
}
