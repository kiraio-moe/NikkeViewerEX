using System;
using System.Collections.Generic;
using UnityEngine;

namespace NikkeViewerEX.Serialization
{
    [Serializable]
    public class NikkeSettings
    {
        public bool IsFirstTime = true;
        public bool HideUI;
        public int FPS = 60;
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
        public List<string> VoicesSource = new();
        public List<string> VoicesPath = new();
        public Vector2 Position;
    }
}
