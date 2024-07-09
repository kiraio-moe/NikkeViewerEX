using System;
using System.IO;
using Cysharp.Threading.Tasks;
using NikkeViewerEX.Components;
using NikkeViewerEX.Serialization;
using NikkeViewerEX.UI;
using NikkeViewerEX.Utils;
using Unity.Logging;
using UnityEngine;

namespace NikkeViewerEX.Core
{
    [AddComponentMenu("Nikke Viewer EX/Core/Settings Manager")]
    [RequireComponent(typeof(MainControl))]
    public class SettingsManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField]
        int m_FPS = 60;

        [SerializeField]
        string m_SettingsFile = "settings.json";

        NikkeSettings nikkeSettings;
        public NikkeSettings NikkeSettings
        {
            get => nikkeSettings;
            set => nikkeSettings = value;
        }
        public int FPS
        {
            get => m_FPS;
            set => m_FPS = value;
        }

        public delegate void OnSettingsLoadedHandler();
        public event OnSettingsLoadedHandler OnSettingsLoaded;
        MainControl mainControl;
        string settingsFilePath;

        void OnValidate()
        {
            if (NikkeSettings != null)
                SetFrameRate(NikkeSettings.FPS = m_FPS);
        }

        void Awake()
        {
            mainControl = GetComponent<MainControl>();
            Setup().Forget();
        }

        async UniTaskVoid Setup()
        {
            NikkeSettings = new();
            settingsFilePath = Path.Combine(StorageHelper.GetApplicationPath(), m_SettingsFile);
            if (File.Exists(settingsFilePath))
            {
                NikkeSettings = await LoadSettings();
                if (NikkeSettings != null)
                {
                    LoadSettingsOnStart();
                    SetFrameRate(NikkeSettings.FPS);
                }
            }
        }

        void LoadSettingsOnStart()
        {
            foreach (Nikke nikkeData in NikkeSettings.NikkeList)
            {
                // Update UI
                NikkeListItem item = mainControl.AddNikke();
                item.NikkeNameText.text = nikkeData.NikkeName;
                item.SkelPathText.text = nikkeData.SkelPath;
                item.AtlasPathText.text = nikkeData.AtlasPath;
                item.TexturesPathText.text = string.Join(", ", nikkeData.TexturesPath);
                FPS = NikkeSettings.FPS;

                // Update the viewer data
                NikkeViewerBase viewer = mainControl
                    .InstantiateViewer(nikkeData.SkelPath)
                    .AsValueTask()
                    .Result;
                viewer.NikkeData = nikkeData;
                viewer.gameObject.transform.position = nikkeData.Position;
                item.Viewer = viewer;

                // Change GameObject name
                item.name = viewer.name = string.IsNullOrEmpty(nikkeData.NikkeName)
                    ? Path.GetFileNameWithoutExtension(nikkeData.SkelPath)
                    : nikkeData.NikkeName;
            }

            OnSettingsLoaded?.Invoke();
            Application.focusChanged += _ => SaveSettings().Forget();
            Application.quitting += () => SaveSettings().Forget();
        }

        public async UniTask<string> SaveSettings()
        {
            try
            {
                string settings = JsonUtility.ToJson(nikkeSettings);

                await UniTask.RunOnThreadPool(() =>
                {
                    using FileStream fs =
                        new(
                            settingsFilePath,
                            FileMode.Truncate,
                            FileAccess.Write,
                            FileShare.ReadWrite
                        );
                    using StreamWriter writer = new(fs);
                    writer.Write(settings);
                });

                return settings;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to save the settings at {settingsFilePath}! {ex.Message}");
                return null;
            }
        }

        async UniTask<NikkeSettings> LoadSettings()
        {
            try
            {
                return JsonUtility.FromJson<NikkeSettings>(
                    await WebRequestHelper.GetTextData(settingsFilePath)
                );
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to get the settings at {settingsFilePath}! {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Set application frame rate.
        /// </summary>
        /// <param name="fps"></param>
        /// <returns><paramref name="fps"/></returns>
        int SetFrameRate(int fps)
        {
            return Application.targetFrameRate = fps;
        }
    }
}
