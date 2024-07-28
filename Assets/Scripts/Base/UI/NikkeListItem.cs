using NikkeViewerEX.Components;
using NikkeViewerEX.Core;
using NikkeViewerEX.Utils;
using SimpleFileBrowser;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NikkeViewerEX.UI
{
    [AddComponentMenu("Nikke Viewer EX/UI/Nikke List Item")]
    public class NikkeListItem : MonoBehaviour
    {
        [Header("UI")]
        public TMP_InputField NikkeNameText;
        public TMP_InputField SkelPathText;
        public TMP_InputField AtlasPathText;
        public TMP_InputField TexturesPathText;
        public TMP_InputField VoicesSourceText;
        public TMP_Dropdown SkinDropdown;

        [Space]
        public CanvasGroup ItemCanvasGroup;
        public Toggle LockButtonToggle;

        public NikkeViewerBase Viewer { get; set; }
        SettingsManager settingsManager;

        void Awake()
        {
            settingsManager = FindObjectsByType<SettingsManager>(FindObjectsSortMode.None)[0];
            FileBrowser.SetFilters(
                false,
                new FileBrowser.Filter("Spine", ".skel", ".atlas", ".png")
            );

            SkinDropdown.onValueChanged.AddListener(_ =>
                Viewer.InvokeChangeSkin(SkinDropdown.value)
            );
        }

        void OnDestroy()
        {
            SkinDropdown.onValueChanged.RemoveListener(_ =>
                Viewer.InvokeChangeSkin(SkinDropdown.value)
            );
        }

        public void LockNikke()
        {
            ItemCanvasGroup.interactable = !ItemCanvasGroup.interactable;
            Viewer.NikkeData.Lock = !Viewer.NikkeData.Lock;
        }

        /// <summary>
        /// Remove Nikke from the list.
        /// </summary>
        public void RemoveNikke()
        {
            if (Viewer != null)
            {
                settingsManager.NikkeSettings.NikkeList.Remove(Viewer.NikkeData);
                Destroy(Viewer.gameObject);
            }
            Destroy(gameObject);
        }

        /// <summary>
        /// Update Nikke when leave Input Field.
        /// </summary>
        public void RefreshNikkeName()
        {
            if (Viewer != null && !string.IsNullOrEmpty(NikkeNameText.text))
                Viewer.NikkeData.NikkeName = Viewer.name = NikkeNameText.text;
        }

        public void OpenFileDialog(TMP_InputField inputField)
        {
            FileBrowser.ShowLoadDialog(
                (paths) => inputField.text = string.Join(", ", paths),
                () => { },
                FileBrowser.PickMode.Files,
                true,
                StorageHelper.GetApplicationPath(),
                null,
                "Load Nikke Assets"
            );
        }

        public void OpenDirectoryDialog(TMP_InputField inputField)
        {
            FileBrowser.ShowLoadDialog(
                (paths) => inputField.text = string.Join(", ", paths),
                () => { },
                FileBrowser.PickMode.Folders,
                true,
                StorageHelper.GetApplicationPath(),
                null,
                "Load Nikke Directory Asset"
            );
        }
    }
}
