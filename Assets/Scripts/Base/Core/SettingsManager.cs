using System;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using NikkeViewerEX.Components;
using NikkeViewerEX.Serialization;
using NikkeViewerEX.UI;
using NikkeViewerEX.Utils;
using TMPro;
using Unity.Logging;
using UnityEngine;

namespace NikkeViewerEX.Core
{
    [AddComponentMenu("Nikke Viewer EX/Core/Settings Manager")]
    [RequireComponent(typeof(MainControl))]
    public class SettingsManager : MonoBehaviour
    {
        [Header("Settings")]
        public int FPS = 60;

        [SerializeField]
        string m_SettingsFile = "settings.json";

        [SerializeField]
        string m_CachedDataDirectoryName = "cache";

        public string[] SupportedAudioFiles = { ".mp3", ".ogg", ".wav" };

        public NikkeSettings NikkeSettings { get; set; } = new();
        public string CachedDataDirectory { get; private set; }

        public delegate void OnSettingsLoadedHandler();
        public event OnSettingsLoadedHandler OnSettingsLoaded;

        string settingsFilePath;
        MainControl mainControl;

        void OnValidate()
        {
            if (NikkeSettings != null)
                SetFrameRate(NikkeSettings.FPS = FPS);
        }

        async void Awake()
        {
            mainControl = GetComponent<MainControl>();
            await Setup();
        }

        async void OnDestroy()
        {
            await SaveSettings();
        }

        async UniTask Setup()
        {
            settingsFilePath = Path.Combine(StorageHelper.GetApplicationPath(), m_SettingsFile);
            CachedDataDirectory = Path.Combine(
                StorageHelper.GetApplicationPath(),
                Directory.CreateDirectory(m_CachedDataDirectoryName).Name
            );

            if (File.Exists(settingsFilePath))
            {
                NikkeSettings = await LoadSettings() ?? new();
                if (NikkeSettings != null)
                    await LoadSaveData();
            }
        }

        async UniTask LoadSaveData()
        {
            mainControl.InitInfoUI.gameObject.SetActive(NikkeSettings.IsFirstTime);
            mainControl.ToggleUI(NikkeSettings.HideUI);
            SetFrameRate(NikkeSettings.FPS);

            foreach (Nikke nikkeData in NikkeSettings.NikkeList)
            {
                // Update UI
                NikkeListItem item = mainControl.AddNikke();
                item.NikkeNameText.text = nikkeData.NikkeName;
                item.SkelPathText.text = nikkeData.SkelPath;
                item.AtlasPathText.text = nikkeData.AtlasPath;
                item.TexturesPathText.text = string.Join(", ", nikkeData.TexturesPath);
                item.VoicesSourceText.text = string.Join(", ", nikkeData.VoicesSource);
                FPS = NikkeSettings.FPS;

                // Update the viewer data
                NikkeViewerBase viewer = mainControl
                    .InstantiateViewer(nikkeData.SkelPath)
                    .AsValueTask()
                    .Result;
                item.Viewer = viewer;
                item.Viewer.NikkeData = nikkeData;
                item.Viewer.gameObject.transform.position = item.Viewer.NikkeData.Position;
                item.Viewer.TouchVoices = (
                    await UniTask.WhenAll(
                        nikkeData.VoicesPath.Select(async path =>
                            await WebRequestHelper.GetAudioClip(
                                WebRequestHelper.IsHttp(path)
                                    ? await WebRequestHelper.CacheAsset(
                                        path,
                                        Path.Combine(
                                            CachedDataDirectory,
                                            Path.GetFileName(new Uri(path).AbsolutePath)
                                        )
                                    )
                                    : path
                            )
                        )
                    )
                ).ToList();

                // Update lock button toggle state
                item.ItemCanvasGroup.interactable = !nikkeData.Lock;
                item.LockButtonToggle.isOn = nikkeData.Lock;

                // Change GameObject name
                item.name = viewer.name = string.IsNullOrEmpty(nikkeData.NikkeName)
                    ? Path.GetFileNameWithoutExtension(nikkeData.SkelPath)
                    : nikkeData.NikkeName;
            }

            OnSettingsLoaded?.Invoke();

            foreach (
                NikkeListItem item in mainControl.NikkeListContent.GetComponentsInChildren<NikkeListItem>()
            )
            {
                // Wait until Skins list is gathered before assigning the to the dropdown list
                await UniTask.WaitUntil(() => item.Viewer.Skins.Length > 0);
                item.SkinDropdown.options = item
                    .Viewer.Skins.Select(skin => new TMP_Dropdown.OptionData(skin))
                    .ToList();

                // Load saved skin
                for (int i = 0; i < item.Viewer.Skins.Length; i++)
                {
                    if (item.SkinDropdown.options[i].text == item.Viewer.NikkeData.Skin)
                    {
                        item.Viewer.InvokeChangeSkin(i);
                        item.SkinDropdown.value = i;
                        break;
                    }
                }
            }
        }

        public async UniTask<string> SaveSettings()
        {
            try
            {
                string settings = JsonUtility.ToJson(NikkeSettings);
                await UniTask.RunOnThreadPool(() =>
                {
                    using FileStream fs =
                        new(
                            settingsFilePath,
                            FileMode.Create,
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
