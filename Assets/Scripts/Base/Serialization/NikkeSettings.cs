using System;
using System.Collections.Generic;
using UnityEngine;

namespace NikkeViewerEX.Serialization
{
    [Serializable]
    public class NikkeSettings
    {
        public bool IsFirstTime = true;
        public string LastOpenedDirectory;
        public bool HideUI;
        public string FPS = "60";
        public string BackgroundImage;
        public string BackgroundMusic;
        public float BackgroundMusicVolume = 0.5f;
        public bool BackgroundMusicPlaying = true;
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
        public string Skin = "default";
        public Vector2 Position;
        public bool Lock;
    }
}
