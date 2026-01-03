using System.Collections.Generic;
using System;

namespace SimplePartsLoader.Data
{

    [Serializable]
    public class PartDump
    {
        public string id;
        public string partName;
        public Dictionary<string, Dictionary<string, object>> components;

        public PartDump()
        {
            components = new Dictionary<string, Dictionary<string, object>>();
        }
    }
}
