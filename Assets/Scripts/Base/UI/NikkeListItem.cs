using System;
using System.Collections.Specialized;
using System.IO;
using Cysharp.Threading.Tasks;
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
        public Slider ScaleSlider;
        public TextMeshProUGUI ScaleValueText;

        [Space]
        public CanvasGroup ItemCanvasGroup;
        public Toggle LockButtonToggle;

        /// <summary>
        /// The Nikke Viewer component associated with this item.
        /// </summary>
        /// <value></value>
        public NikkeViewerBase Viewer { get; set; }
        SettingsManager settingsManager;

        private void Awake()
        {
            settingsManager = FindObjectsByType<SettingsManager>(FindObjectsSortMode.None)[0];
            SkinDropdown.onValueChanged.AddListener(_ =>
                Viewer.InvokeChangeSkin(SkinDropdown.value)
            );
            LockButtonToggle.onValueChanged.AddListener(ToggleLockNikke);
            ScaleSlider.onValueChanged.AddListener(AdjustNikkeScale);
        }

        private void OnDestroy()
        {
            SkinDropdown.onValueChanged.RemoveListener(_ =>
                Viewer.InvokeChangeSkin(SkinDropdown.value)
            );
            LockButtonToggle.onValueChanged.RemoveListener(ToggleLockNikke);
            ScaleSlider.onValueChanged.RemoveListener(AdjustNikkeScale);
        }

        public void ResetPositionAndScale()
        {
            if (Viewer == null)
                return;
            AdjustNikkeScale(1);
            Viewer.transform.localPosition = Vector3.zero;
            ScaleSlider.value = 1;
        }

        public void AdjustNikkeScale(float scale)
        {
            if (Viewer == null)
                return;
            Viewer.AdjustNikkeScale(scale);
            ScaleValueText.text = $"{scale:#0.0}x";
        }

        /// <summary>
        /// Lock Nikke position and disable interaction of their associated item UI.
        /// </summary>
        /// <param name="lockNikke"></param>
        private void ToggleLockNikke(bool lockNikke)
        {
            ItemCanvasGroup.interactable = !lockNikke;
            Viewer.NikkeData.Lock = lockNikke;
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

        public async void OpenNikkeAssetsDialog(TMP_InputField inputField)
        {
            FileBrowser.SetFilters(
                false,
                new FileBrowser.Filter("Spine", ".skel", ".atlas", ".png")
            );
            string[] paths = await StorageHelper.OpenFileDialog(
                inputField,
                "Load Nikke Assets",
                initialPath: settingsManager.NikkeSettings.LastOpenedDirectory
            );
            if (paths.Length > 0)
            {
                settingsManager.NikkeSettings.LastOpenedDirectory = Path.GetDirectoryName(paths[0]);
                await settingsManager.SaveSettings();
            }
        }

        public async void OpenNikkeAssetDirectoriesDialog(TMP_InputField inputField)
        {
            string[] paths = await StorageHelper.OpenDirectoryDialog(
                inputField,
                "Load Nikke Asset Directories",
                true,
                initialPath: settingsManager.NikkeSettings.LastOpenedDirectory
            );
            if (paths.Length > 0)
            {
                settingsManager.NikkeSettings.LastOpenedDirectory = Path.GetDirectoryName(paths[0]);
                await settingsManager.SaveSettings();
            }
        }
    }
}
