using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Gilzoide.SerializableCollections;
using NikkeViewerEX.Components;
using NikkeViewerEX.Serialization;
using NikkeViewerEX.UI;
using NikkeViewerEX.Utils;
using Unity.Logging;
using Unity.Logging.Sinks;
using UnityEngine;
using UnityEngine.InputSystem;
using Logger = Unity.Logging.Logger;

namespace NikkeViewerEX.Core
{
    [AddComponentMenu("Nikke Viewer EX/Core/Main Control")]
    [RequireComponent(typeof(SettingsManager))]
    public class MainControl : MonoBehaviour
    {
        [Header("Spine Settings")]
        [SerializeField]
        SerializableDictionary<string, NikkeViewerBase> m_NikkeVersions = new();

        [Header("UI")]
        [SerializeField]
        CanvasGroup m_MainControlUI;

        [SerializeField]
        RectTransform m_InitInfoUI;

        [SerializeField]
        RectTransform m_NikkeListContent;

        [SerializeField]
        RectTransform m_NikkeListItem;

        public CanvasGroup MainControlUI
        {
            get => m_MainControlUI;
        }
        public RectTransform InitInfoUI
        {
            get => m_InitInfoUI;
        }

        public delegate void OnSettingsAppliedHandler();
        public event OnSettingsAppliedHandler OnSettingsApplied;

        static readonly string logFileName = "log.txt";
        readonly SpineHelperBase spineHelper = new();

        SettingsManager settingsManager;
        InputManager inputManager;

        void Awake()
        {
            SetupLoggerConfig();

            inputManager = FindObjectsByType<InputManager>(FindObjectsSortMode.None)[0];
            settingsManager = GetComponent<SettingsManager>();
        }

        void OnEnable()
        {
            inputManager.ToggleUI.performed += ToggleUI;
        }

        void OnDestroy()
        {
            inputManager.ToggleUI.performed -= ToggleUI;
        }

        public void AddNikkeUI() => AddNikke();

        public NikkeListItem AddNikke() =>
            Instantiate(m_NikkeListItem, m_NikkeListContent).GetComponent<NikkeListItem>();

        public async void ApplyNikkeSettings()
        {
            NikkeListItem[] listItems = m_NikkeListContent.GetComponentsInChildren<NikkeListItem>();
            List<Nikke> nikkeDataList = new();

            foreach (NikkeListItem item in listItems)
            {
                if (item.Viewer == null)
                {
                    string nikkeName = item.NikkeNameText.text;
                    string skelPath = await GetAssetPath(item.SkelPathText.text);
                    string atlasPath = await GetAssetPath(item.AtlasPathText.text);
                    List<string> texturesPath = await GetAssetsPath(item.TexturesPathText.text);
                    List<string> voicesSource = string.IsNullOrEmpty(item.VoicesSourceText.text)
                        ? new List<string>()
                        : item.VoicesSourceText.text.Split(", ").ToList();

                    NikkeViewerBase viewer = await InstantiateViewer(skelPath);
                    if (viewer != null)
                    {
                        viewer.NikkeData = new Nikke
                        {
                            NikkeName = nikkeName,
                            AssetName = Path.GetFileNameWithoutExtension(skelPath),
                            SkelPath = skelPath,
                            AtlasPath = atlasPath,
                            TexturesPath = texturesPath,
                            VoicesSource = voicesSource,
                            VoicesPath = await GetVoicesPath(voicesSource)
                        };
                        viewer.TouchVoices = await CacheTouchVoices(viewer.NikkeData.VoicesPath);
                        item.Viewer = viewer;
                        item.name = item.Viewer.name = string.IsNullOrEmpty(nikkeName)
                            ? item.Viewer.NikkeData.AssetName
                            : nikkeName;
                    }
                }
                else
                {
                    if (
                        item.Viewer.NikkeData.VoicesSource.Count == 0
                        && !string.IsNullOrEmpty(item.VoicesSourceText.text)
                    )
                    {
                        item.Viewer.NikkeData.VoicesSource = item
                            .VoicesSourceText.text.Split(", ")
                            .ToList();
                        item.Viewer.NikkeData.VoicesPath = await GetVoicesPath(
                            item.Viewer.NikkeData.VoicesSource
                        );
                        item.Viewer.TouchVoices = await CacheTouchVoices(
                            item.Viewer.NikkeData.VoicesPath
                        );
                    }
                }
                nikkeDataList.Add(item.Viewer.NikkeData);
            }

            settingsManager.NikkeSettings.NikkeList = nikkeDataList;
            OnSettingsApplied?.Invoke();
            await settingsManager.SaveSettings();
        }

        async UniTask<string> GetAssetPath(string path) =>
            WebRequestHelper.IsHttp(path)
                ? await WebRequestHelper.CacheAsset(
                    path,
                    Path.Combine(
                        settingsManager.CachedDataDirectory,
                        Path.GetFileName(new Uri(path).AbsolutePath)
                    )
                )
                : path;

        async UniTask<List<string>> GetAssetsPath(string paths) =>
            (await UniTask.WhenAll(paths.Split(", ").Select(GetAssetPath))).ToList();

        public async UniTask<NikkeViewerBase> InstantiateViewer(string skelPath)
        {
            string skelVersion = await spineHelper.GetSkelVersion(skelPath);
            return skelVersion switch
            {
                "4.0" => Instantiate(m_NikkeVersions["4.0"]),
                "4.1" => Instantiate(m_NikkeVersions["4.1"]),
                _ => InvalidSkelVersion(skelVersion, skelPath)
            };
        }

        private static NikkeViewerBase InvalidSkelVersion(string skelVersion, string skelPath)
        {
            Log.Error(
                $"Unable to load Skeleton asset! Invalid Skeleton version: {skelVersion} in {skelPath}"
            );
            return null;
        }

        async UniTask<List<string>> GetVoicesPath(List<string> sources)
        {
            List<string> voicesPath = new();
            foreach (string source in sources)
            {
                if (string.IsNullOrEmpty(source))
                    continue;
                if (WebRequestHelper.IsHttp(source))
                {
                    try
                    {
                        voicesPath.AddRange(
                            JsonUtility.FromJson<List<string>>(
                                await WebRequestHelper.GetTextData(source)
                            )
                        );
                        await UniTask.WhenAll(
                            voicesPath.Select(path =>
                                WebRequestHelper.CacheAsset(
                                    path,
                                    Path.Combine(
                                        settingsManager.CachedDataDirectory,
                                        Path.GetFileName(new Uri(path).AbsolutePath)
                                    )
                                )
                            )
                        );
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Unable to retrieve JSON data: {source} {ex.Message}");
                    }
                }
                else
                {
                    voicesPath.AddRange(
                        Directory
                            .EnumerateFiles(source, "*.*", SearchOption.AllDirectories)
                            .Where(file =>
                                settingsManager.SupportedAudioFiles.Contains(
                                    Path.GetExtension(file).ToLower()
                                )
                            )
                            .ToList()
                    );
                }
            }
            return voicesPath;
        }

        async UniTask<List<AudioClip>> CacheTouchVoices(List<string> sources) =>
            (
                await UniTask.WhenAll(
                    sources.Select(async path =>
                        await WebRequestHelper.GetAudioClip(await GetCachedPath(path))
                    )
                )
            ).ToList();

        async UniTask<string> GetCachedPath(string path) =>
            WebRequestHelper.IsHttp(path)
                ? await WebRequestHelper.CacheAsset(
                    path,
                    Path.Combine(
                        settingsManager.CachedDataDirectory,
                        Path.GetFileName(new Uri(path).AbsolutePath)
                    )
                )
                : path;

        void ToggleUI(InputAction.CallbackContext ctx)
        {
            settingsManager.NikkeSettings.HideUI = Convert.ToBoolean(m_MainControlUI.alpha);
            ToggleUI(settingsManager.NikkeSettings.HideUI);
        }

        public void ToggleUI(bool state)
        {
            m_MainControlUI.alpha = state ? 0 : 1;
            m_MainControlUI.interactable = !state;
        }

        public void HideInfoUI(bool state)
        {
            m_InitInfoUI.gameObject.SetActive(!state);
            settingsManager.NikkeSettings.IsFirstTime = !state;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void SetupLoggerConfig()
        {
            Log.Logger = new Logger(
                new LoggerConfig()
                    .MinimumLevel.Debug()
                    .CaptureStacktrace()
                    .RedirectUnityLogs()
                    .OutputTemplate(
                        "[{Timestamp}] <b>[{Level}]</b> <b>{Message}</b>{NewLine}<i>{Stacktrace}</i>"
                    )
                    .WriteTo.File(
                        Path.Combine(StorageHelper.GetApplicationPath(), logFileName),
                        minLevel: LogLevel.Verbose
                    )
                    .WriteTo.UnityEditorConsole()
            );
        }
    }
}
