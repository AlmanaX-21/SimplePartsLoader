using System.Collections.Generic;
using System;

namespace SimplePartsLoader.Data
{
    // This class is used when dumping existing parts to JSON
    // Or reading overrides for existing parts
    [Serializable]
    public class PartDump
    {
        public string id; // Prefab Name
        public string partName; // UI Name
        public Dictionary<string, Dictionary<string, object>> components; // "Engine" -> { "thrust": 500 }

        public PartDump()
        {
            components = new Dictionary<string, Dictionary<string, object>>();
        }
    }
}
