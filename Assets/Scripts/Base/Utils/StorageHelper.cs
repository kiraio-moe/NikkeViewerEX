using System.IO;
using Cysharp.Threading.Tasks;
using SimpleFileBrowser;
using TMPro;
using UnityEngine;

namespace NikkeViewerEX.Utils
{
    public static class StorageHelper
    {
        /// <summary>
        /// Get current application/project root directory.
        /// </summary>
        /// <returns>Application/project root full path.</returns>
        public static string GetApplicationPath()
        {
#if UNITY_ANDROID || UNITY_WEBGl && !UNITY_EDITOR
            return $"file:///{Directory.GetParent(Application.persistentDataPath)!.ToString()}";
#elif UNITY_STANDALONE_OSX
            return $"file://{Directory.GetParent(Application.dataPath)!.ToString()}";
#else
            return Directory.GetParent(Application.dataPath)!.ToString();
#endif
        }

        /// <summary>
        /// Open file dialog and fill the <paramref name="inputField"/>.
        /// </summary>
        /// <param name="inputField"></param>
        /// <param name="windowTitle"></param>
        public static async UniTask<string[]> OpenFileDialog(
            TMP_InputField inputField,
            string windowTitle = "Load File",
            bool allowMultiple = false,
            string initialPath = ""
        )
        {
            UniTaskCompletionSource<string[]> completionSource = new();
            FileBrowser.ShowLoadDialog(
                (paths) =>
                {
                    inputField.text = string.Join(", ", paths);
                    completionSource.TrySetResult(paths);
                },
                () => completionSource.TrySetResult(null), // Set result to null if canceled
                FileBrowser.PickMode.Files,
                allowMultiple,
                string.IsNullOrEmpty(initialPath) ? GetApplicationPath() : initialPath,
                null,
                windowTitle
            );
            return await completionSource.Task ?? new string[0];
        }

        /// <summary>
        /// Open directory dialog and fill the <paramref name="inputField"/>.
        /// </summary>
        /// <param name="inputField"></param>
        public static async UniTask<string[]> OpenDirectoryDialog(
            TMP_InputField inputField,
            string windowTitle = "Load Directory",
            bool allowMultiple = false,
            string initialPath = ""
        )
        {
            UniTaskCompletionSource<string[]> completionSource = new();
            FileBrowser.ShowLoadDialog(
                (paths) =>
                {
                    inputField.text = string.Join(", ", paths);
                    completionSource.TrySetResult(paths);
                },
                () => completionSource.TrySetResult(null), // Set result to null if canceled
                FileBrowser.PickMode.Folders,
                allowMultiple,
                string.IsNullOrEmpty(initialPath) ? GetApplicationPath() : initialPath,
                null,
                windowTitle
            );
            return await completionSource.Task ?? new string[0];
        }
    }
}
