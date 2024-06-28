using System;
using System.Collections.Generic;

namespace NikkeViewerEX.Serialization
{
    [Serializable]
    public class NikkeSettings
    {
        public string nikkeName;
        public List<string> nikkeTextures = new();
    }
}
