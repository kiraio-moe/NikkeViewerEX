using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Gilzoide.SerializableCollections;
using NikkeViewerEX.Components;
using NikkeViewerEX.UI;
using NikkeViewerEX.Utils;
using Unity.Logging;
using Unity.Logging.Sinks;
using UnityEngine;
using Logger = Unity.Logging.Logger;

namespace NikkeViewerEX.Core
{
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

        void Awake()
        {
            SetupLoggerConfig();
        }

        /// <summary>
        /// Add Nikke to list.
        /// </summary>
        public void AddNikke()
        {
            Instantiate(m_NikkeListItem, m_NikkeListContent).GetComponent<NikkeListItem>();
        }

        /// <summary>
        /// Apply Nikke settings.
        /// </summary>
        /// <returns></returns>
        public async void ApplyNikkeSettings()
        {
            foreach (
                NikkeListItem item in m_NikkeListContent.GetComponentsInChildren<NikkeListItem>()
            )
            {
                string nikkeName = item.NikkeNameText.text;
                string skelPath = item.SkelPathText.text;
                string atlasPath = item.AtlasPathText.text;
                List<string> texturesPath = item.TexturesPathText.text.Split(", ").ToList() ?? null;

                if (string.IsNullOrEmpty(skelPath))
                {
                    Log.Error("No .skel asset path provided!");
                    if (string.IsNullOrEmpty(atlasPath))
                    {
                        Log.Error("No .atlas asset path provided!");
                        if (string.IsNullOrEmpty(texturesPath[0]))
                        {
                            Log.Error("No image textures path provided!");
                            return;
                        }
                    }
                }

                string skelVersion = await spineHelper.GetSkelVersion(skelPath);
                if (string.IsNullOrEmpty(skelVersion))
                {
                    Log.Error(
                        $"Unable to load Skeleton asset! Invalid Skeleton version: {skelVersion} in {skelPath}"
                    );
                    return;
                }

                if (item.Viewer != null)
                    Destroy(item.Viewer.gameObject);

                NikkeViewerBase viewer = null;
                switch (skelVersion)
                {
                    case "4.0":
                        viewer = Instantiate(m_NikkeVersions["4.0"]);
                        break;
                    case "4.1":
                        viewer = Instantiate(m_NikkeVersions["4.1"]);
                        break;
                }

                // Update the viewer data
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
            }

            OnSettingsApplied?.Invoke();
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
