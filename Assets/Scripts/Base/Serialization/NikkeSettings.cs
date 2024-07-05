using System;
using System.Collections.Generic;

namespace NikkeViewerEX.Serialization
{
    [Serializable]
    public class NikkeSettings
    {
        public List<Nikke> NikkeList = new();
    }

    [Serializable]
    public class Nikke
    {
        public string NikkeName;
        public string AssetName;
        public string SkelPath;
        public string AtlasPath;
        public List<string> TexturesPath = new();
    }
}
