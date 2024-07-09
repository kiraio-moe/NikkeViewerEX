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
        RectTransform m_NikkeListContent;

        [SerializeField]
        RectTransform m_NikkeListItem;

        public delegate void OnSettingsAppliedHandler();
        public event OnSettingsAppliedHandler OnSettingsApplied;

        static readonly string logFileName = "log.txt";
        readonly SpineHelperBase spineHelper = new();

        SettingsManager settingsManager;

        void Awake()
        {
            SetupLoggerConfig();
            settingsManager = GetComponent<SettingsManager>();
        }

        /// <summary>
        /// Add Nikke to list.
        /// </summary>
        public void AddNikkeUI()
        {
            AddNikke();
        }

        public NikkeListItem AddNikke()
        {
            return Instantiate(m_NikkeListItem, m_NikkeListContent).GetComponent<NikkeListItem>();
        }

        /// <summary>
        /// Apply Nikke settings.
        /// </summary>
        /// <returns></returns>
        public async void ApplyNikkeSettings()
        {
            NikkeListItem[] listItems = m_NikkeListContent.GetComponentsInChildren<NikkeListItem>();
            List<Nikke> nikkeDataList = new();

            foreach (NikkeListItem item in listItems)
            {
                if (item.Viewer == null)
                {
                    string cacheSavePath = Path.Combine(
                        StorageHelper.GetApplicationPath(),
                        Directory.CreateDirectory("Data").Name
                    );
                    string nikkeName = item.NikkeNameText.text;
                    string skelPath = item.SkelPathText.text.StartsWith("http")
                        ? await CacheAsset(
                            item.SkelPathText.text,
                            Path.Combine(
                                cacheSavePath,
                                Path.GetFileName(new Uri(item.SkelPathText.text).AbsolutePath)
                            )
                        )
                        : item.SkelPathText.text;
                    string atlasPath = item.AtlasPathText.text.StartsWith("http")
                        ? await CacheAsset(
                            item.AtlasPathText.text,
                            Path.Combine(
                                cacheSavePath,
                                Path.GetFileName(new Uri(item.AtlasPathText.text).AbsolutePath)
                            )
                        )
                        : item.AtlasPathText.text;
                    List<string> texturesPath = (
                        await UniTask.WhenAll(
                            item.TexturesPathText.text.Split(", ")
                                .Select(async path =>
                                    path.StartsWith("http")
                                        ? await CacheAsset(
                                            path,
                                            Path.Combine(
                                                cacheSavePath,
                                                Path.GetFileName(new Uri(path).AbsolutePath)
                                            )
                                        )
                                        : path
                                )
                        )
                    ).ToList();

                    // Update the viewer data
                    NikkeViewerBase viewer = await InstantiateViewer(skelPath);
                    if (viewer != null)
                    {
                        viewer.NikkeData.NikkeName = nikkeName;
                        viewer.NikkeData.AssetName = Path.GetFileNameWithoutExtension(skelPath);
                        viewer.NikkeData.SkelPath = skelPath;
                        viewer.NikkeData.AtlasPath = atlasPath;
                        viewer.NikkeData.TexturesPath = texturesPath;
                        item.Viewer = viewer; // Assign the Viewer

                        // Just change the Game Objet name to make a distinction in the Editor.
                        item.name = item.Viewer.name = string.IsNullOrEmpty(nikkeName)
                            ? item.Viewer.NikkeData.AssetName
                            : nikkeName;

                        nikkeDataList.Add(item.Viewer.NikkeData);
                    }
                }
            }

            settingsManager.NikkeSettings.NikkeList = nikkeDataList;
            OnSettingsApplied?.Invoke();
        }

        public async UniTask<NikkeViewerBase> InstantiateViewer(string skelPath)
        {
            string skelVersion = await spineHelper.GetSkelVersion(skelPath);
            switch (skelVersion)
            {
                case "4.0":
                    return Instantiate(m_NikkeVersions["4.0"]);
                case "4.1":
                    return Instantiate(m_NikkeVersions["4.1"]);
                default:
                    Log.Error(
                        $"Unable to load Skeleton asset! Invalid Skeleton version: {skelVersion} in {skelPath}"
                    );
                    return null;
            }
        }

        async UniTask<string> CacheAsset(string uri, string savePath)
        {
            byte[] data = await WebRequestHelper.GetBinaryData(uri);
            await using FileStream fs =
                new(savePath, FileMode.Create, FileAccess.Write, FileShare.Read);
            await fs.WriteAsync(data);
            return savePath;
        }

        /// <summary>
        /// Setup logger configurations.
        /// </summary>
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
                        $"{Path.Combine(StorageHelper.GetApplicationPath(), logFileName)}",
                        minLevel: LogLevel.Verbose
                    )
                    .WriteTo.UnityEditorConsole()
            );
        }
    }
}
