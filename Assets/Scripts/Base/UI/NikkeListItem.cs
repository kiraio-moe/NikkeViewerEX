using NikkeViewerEX.Components;
using NikkeViewerEX.Utils;
using SimpleFileBrowser;
using TMPro;
using UnityEngine;

namespace NikkeViewerEX.UI
{
    [AddComponentMenu("Nikke Viewer EX/UI/Nikke List Item")]
    public class NikkeListItem : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField]
        TMP_InputField m_NikkeNameText;

        [SerializeField]
        TMP_InputField m_SkelPathText;

        [SerializeField]
        TMP_InputField m_AtlasPathText;

        [SerializeField]
        TMP_InputField m_TexturesPathText;

        public TMP_InputField NikkeNameText
        {
            get => m_NikkeNameText;
        }
        public TMP_InputField SkelPathText
        {
            get => m_SkelPathText;
        }
        public TMP_InputField AtlasPathText
        {
            get => m_AtlasPathText;
        }
        public TMP_InputField TexturesPathText
        {
            get => m_TexturesPathText;
        }

        NikkeViewerBase viewer;
        public NikkeViewerBase Viewer
        {
            get => viewer;
            set => viewer = value;
        }

        void Start()
        {
            FileBrowser.SetFilters(
                false,
                new FileBrowser.Filter("Spine", ".skel", ".atlas", ".png")
            );
        }

        /// <summary>
        /// Remove Nikke from the list.
        /// </summary>
        public void RemoveNikke()
        {
            Destroy(gameObject);
        }

        /// <summary>
        /// Update Nikke when leave Input Field.
        /// </summary>
        public void RefreshNikkeName()
        {
            if (Viewer != null)
                Viewer.NikkeData.NikkeName = Viewer.name = NikkeNameText.text;
        }

        public void OpenFileDialog(TMP_InputField inputField)
        {
            FileBrowser.ShowLoadDialog(
                (paths) =>
                {
                    inputField.text = string.Join(", ", paths);
                },
                () => { },
                FileBrowser.PickMode.Files,
                true,
                StorageHelper.GetApplicationPath(),
                null,
                "Load Nikke Assets"
            );
        }
    }
}
