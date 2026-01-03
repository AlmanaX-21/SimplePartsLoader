using System.Collections.Generic;
using System.IO;
using UnityEngine;
using BepInEx.Logging;
using SimplePartsLoader.Data;
using System.Reflection;
using System;
using SimplePartsLoader.SimpleJSON;

namespace SimplePartsLoader
{
    public static class PartLoader
    {
        public static ManualLogSource PluginLogger;
        public static List<CustomPartData> CustomParts = new List<CustomPartData>();

        public static void Initialize(ManualLogSource logger)
        {
            PluginLogger = logger;
            LoadCustomParts();
        }

        private static void LoadCustomParts()
        {
            string path = SimplePartsLoaderPlugin.PartsConfigPath;
            if (!Directory.Exists(path)) return;

            string[] files = Directory.GetFiles(path, "*.json", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                // Fix: Ignore legacy Standard folder to prevent recursive loading crashes
                if (file.Contains(Path.DirectorySeparatorChar + "Standard" + Path.DirectorySeparatorChar))
                    continue;

                PluginLogger.LogInfo($"Loading part definition: {Path.GetFileName(file)}");
                try
                {
                    string json = File.ReadAllText(file);
                    var node = JSONNode.Parse(json);

                    CustomPartData part = new CustomPartData();
                    part.id = node["id"];
                    // ... (rest of parsing logic is unchanged, just showing context)
                    part.basePrefabName = node["basePrefabName"];
                    part.name = node["name"];
                    part.description = node["description"];
                    part.price = node["price"].AsFloat;
                    part.category = node["category"];

                    // Components overrides
                    part.overrides = new Dictionary<string, Dictionary<string, object>>();
                    if (node["overrides"] != null && node["overrides"].IsObject)
                    {
                        foreach (KeyValuePair<string, SimplePartsLoader.SimpleJSON.JSONNode> compKey in node["overrides"].AsObject)
                        {
                            string compName = compKey.Key;
                            var fieldsNode = compKey.Value.AsObject;

                            var fieldDict = new Dictionary<string, object>();
                            foreach (KeyValuePair<string, SimplePartsLoader.SimpleJSON.JSONNode> field in fieldsNode)
                            {
                                if (field.Value.IsNumber) fieldDict[field.Key] = field.Value.AsFloat;
                                else if (field.Value.IsBoolean) fieldDict[field.Key] = field.Value.AsBool;
                                else fieldDict[field.Key] = field.Value.Value;
                            }
                            part.overrides[compName] = fieldDict;
                        }
                    }

                    CustomParts.Add(part);
                    PluginLogger.LogInfo($"Loaded custom part: {part.id}");
                }
                catch (Exception ex)
                {
                    PluginLogger.LogError($"Failed to load part {file}: {ex.Message}");
                }
            }
        }

        public static void ApplyStats(GameObject go, Dictionary<string, Dictionary<string, object>> overrides)
        {
            if (overrides == null) return;

            foreach (var compEntry in overrides)
            {
                string compName = compEntry.Key;
                var fields = compEntry.Value;

                Component comp = go.GetComponent(compName);
                if (comp == null)
                {
                    PluginLogger?.LogWarning($"Component {compName} not found on {go.name}");
                    continue;
                }

                foreach (var fieldEntry in fields)
                {
                    string fieldName = fieldEntry.Key;
                    object value = fieldEntry.Value;

                    FieldInfo fi = comp.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
                    if (fi != null)
                    {
                        try
                        {
                            PluginLogger?.LogInfo($"Setting {compName}.{fieldName}...");
                            object convertedValue = Convert.ChangeType(value, fi.FieldType);
                            fi.SetValue(comp, convertedValue);
                            PluginLogger?.LogInfo($"Set {compName}.{fieldName} = {convertedValue}");
                        }
                        catch (Exception ex)
                        {
                            PluginLogger?.LogWarning($"Failed to set {compName}.{fieldName} to {value}: {ex.Message}");
                        }
                    }
                }
            }
        }

        public static void DumpParts(GameObject[] parts)
        {
            try
            {
                // Determine Dump Directory - moved to PartDumps folder to prevent recursive loading
                string dumpDir = SimplePartsLoaderPlugin.PartDumpsPath;
                if (!Directory.Exists(dumpDir)) Directory.CreateDirectory(dumpDir);

                foreach (var part in parts)
                {
                    if (part == null) continue;

                    try
                    {
                        // Sanitize filename
                        string safeName = string.Join("_", part.name.Split(Path.GetInvalidFileNameChars()));
                        string filePath = Path.Combine(dumpDir, safeName + ".json");

                        if (File.Exists(filePath))
                        {
                            // LOAD EXISTING overrides
                            try
                            {
                                PluginLogger?.LogInfo($"Loading overrides for standard part: {part.name}");
                                string json = File.ReadAllText(filePath);
                                PluginLogger?.LogInfo($"Read file content ({json.Length} chars): {json}");
                                PluginLogger?.LogInfo("Starting Parse...");

                                if (string.IsNullOrEmpty(json))
                                {
                                    PluginLogger?.LogWarning("JSON is empty.");
                                    continue;
                                }

                                var node = JSONNode.Parse(json);
                                PluginLogger?.LogInfo("Parse completed.");
                                PluginLogger?.LogInfo($"Parsed JSON. Node type: {node?.GetType().Name ?? "null"}");

                                if (node == null)
                                {
                                    PluginLogger?.LogWarning("Parsed node is null.");
                                    continue;
                                }

                                if (node["overrides"] != null && node["overrides"].IsObject)
                                {
                                    PluginLogger?.LogInfo("Found overrides object. Building dictionary...");
                                    var overrides = new Dictionary<string, Dictionary<string, object>>();
                                    foreach (KeyValuePair<string, SimplePartsLoader.SimpleJSON.JSONNode> compKey in node["overrides"].AsObject)
                                    {
                                        string compName = compKey.Key;
                                        if (compKey.Value == null || !compKey.Value.IsObject) continue;

                                        var fieldsNode = compKey.Value.AsObject;

                                        var fieldDict = new Dictionary<string, object>();
                                        foreach (KeyValuePair<string, SimplePartsLoader.SimpleJSON.JSONNode> field in fieldsNode)
                                        {
                                            if (field.Value.IsNumber) fieldDict[field.Key] = field.Value.AsFloat;
                                            else if (field.Value.IsBoolean) fieldDict[field.Key] = field.Value.AsBool;
                                            else fieldDict[field.Key] = field.Value.Value;
                                        }
                                        overrides[compName] = fieldDict;
                                    }

                                    PluginLogger?.LogInfo($"Dictionary built with {overrides.Count} components. Calling ApplyStats...");
                                    ApplyStats(part, overrides);
                                    PluginLogger?.LogInfo("ApplyStats returned.");
                                }
                                else
                                {
                                    PluginLogger?.LogInfo("No 'overrides' object found in JSON.");
                                }
                            }
                            catch (Exception ex)
                            {
                                PluginLogger?.LogError($"Failed to load overrides for {part.name}: {ex.Message}");
                            }
                            continue;
                        }

                        var root = new JSONClass();
                        root.Add("id", part.name);
                        root.Add("basePrefabName", part.name);

                        var bp = part.GetComponent<BuildingPart>();
                        if (bp)
                        {
                            root.Add("name", bp.partName);
                            root.Add("price", new JSONData(bp.price.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                            root.Add("category", "Building");
                        }

                        var newOverrides = new JSONClass();

                        // PlanePart Dump
                        var pp = part.GetComponent<PlanePart>();
                        if (pp)
                        {
                            var compNode = new JSONClass();
                            foreach (var f in pp.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                            {
                                object val = f.GetValue(pp);
                                if (val == null) continue;

                                if (f.FieldType == typeof(float)) compNode.Add(f.Name, new JSONData(((float)val).ToString(System.Globalization.CultureInfo.InvariantCulture)));
                                else if (f.FieldType == typeof(int)) compNode.Add(f.Name, new JSONData(val.ToString()));
                                else if (f.FieldType == typeof(bool)) compNode.Add(f.Name, new JSONData(val.ToString().ToLower()));
                                else if (f.FieldType == typeof(string)) compNode.Add(f.Name, (string)val);
                            }
                            newOverrides.Add(pp.GetType().Name, compNode);
                        }

                        // Also dump BuildingPart specifics to allow mass/drag editing if they exist there (BuildingPart usually doesn't have physics, but just in case)
                        if (bp)
                        {
                            var compNode = new JSONClass();
                            compNode.Add("price", new JSONData(bp.price.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                            newOverrides.Add("BuildingPart", compNode);
                        }

                        root.Add("overrides", newOverrides);

                        File.WriteAllText(filePath, root.ToJSON(1));
                        PluginLogger?.LogInfo($"Dumped standard part: {part.name}");
                    }
                    catch (Exception loopEx)
                    {
                        PluginLogger?.LogError($"Error processing part {part.name}: {loopEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLogger?.LogError($"Failed to dump parts: {ex.Message}");
            }
        }
    }
}
