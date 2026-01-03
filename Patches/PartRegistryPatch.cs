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

        [HarmonyPostfix]
        public static void Postfix(PartPrefabs __instance)
        {
            if (__instance == null) return;


            PartLoader.DumpParts(__instance.partPrefabs);


            List<GameObject> newParts = new List<GameObject>();

            foreach (var data in PartLoader.CustomParts)
            {

                GameObject basePrefab = __instance.partPrefabs.FirstOrDefault(p => p.name == data.basePrefabName);
                if (basePrefab == null)
                {
                    PartLoader.PluginLogger.LogError($"Could not find base prefab '{data.basePrefabName}' for custom part '{data.id}'");
                    continue;
                }


                GameObject newPart = GameObject.Instantiate(basePrefab);
                newPart.name = data.id;
                newPart.SetActive(false);
                GameObject.DontDestroyOnLoad(newPart);


                if (data.overrides != null)
                {
                    PartLoader.ApplyStats(newPart, data.overrides);
                }


                var buildPart = newPart.GetComponent<BuildingPart>();
                if (buildPart)
                {
                    buildPart.partName = data.name;
                    buildPart.price = data.price;

                }

                newParts.Add(newPart);
                PartLoader.PluginLogger.LogInfo($"Registered custom part: {data.id}");
            }


            if (newParts.Count > 0)
            {
                var list = __instance.partPrefabs.ToList();
                list.AddRange(newParts);
                __instance.partPrefabs = list.ToArray();
            }
        }
    }
}
