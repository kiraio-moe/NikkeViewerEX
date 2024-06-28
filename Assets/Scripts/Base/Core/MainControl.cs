using System.IO;
using Cysharp.Threading.Tasks;
using NikkeViewerEX.Utils;
using Unity.Logging;
using Unity.Logging.Sinks;
using UnityEngine;
using Logger = Unity.Logging.Logger;

namespace NikkeViewerEX.Core
{
    public class MainControl : MonoBehaviour
    {
        [SerializeField]
        string m_LogFileName = "log.txt";

        async UniTaskVoid Awake()
        {
            SetupLoggerConfig();
        }

        /// <summary>
        /// Setup logger configurations.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        void SetupLoggerConfig()
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
                        $"{Path.Combine(StorageHelper.GetApplicationPath(), m_LogFileName)}",
                        minLevel: LogLevel.Verbose
                    )
                    .WriteTo.UnityEditorConsole()
            );
        }
    }
}
