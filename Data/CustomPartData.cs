using System.Collections.Generic;
using System;

namespace SimplePartsLoader.Data
{
    [Serializable]
    public class CustomPartData
    {
        public string id;
        public string basePrefabName;
        public string name;
        public string description;
        public float price;
        public string category;

        public PrefabData prefabs;
        public Dictionary<string, Dictionary<string, object>> overrides;
    }

    [Serializable]
    public class PrefabData
    {
        public string modelPath;
        public string colliderFit; // "Box", "Mesh", "Convex"
    }
}
