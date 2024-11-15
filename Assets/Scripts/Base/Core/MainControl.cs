using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using DynamicPanels;
using Gilzoide.SerializableCollections;
using Microsoft.MixedReality.Toolkit.Experimental.UI;
using NikkeViewerEX.Components;
using NikkeViewerEX.Serialization;
using NikkeViewerEX.UI;
using NikkeViewerEX.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NikkeViewerEX.Core
{
    /// <summary>
    /// The component that tied every functionality together.
    /// </summary>
    [AddComponentMenu("Nikke Viewer EX/Core/Main Control")]
    [RequireComponent(typeof(SettingsManager))]
    public class MainControl : MonoBehaviour
    {
        [Header("Spine Settings")]
        [Tooltip("List of Prefabs containing supported Spine version")]
        [SerializeField]
        private SerializableDictionary<string, NikkeViewerBase> m_NikkeVersions = new();

        [Header("UI")]
        [Tooltip("Main Control UI Group")]
        [SerializeField]
        private CanvasGroup m_MainControlUI;

        [SerializeField]
        private RectTransform m_WelcomePanel;

        // [SerializeField]
        // private NonNativeKeyboard m_OnScreenKeyboardPrefab;

        [Tooltip("Nikke List - List View content")]
        [SerializeField]
        private RectTransform m_NikkeListContent;

        [Tooltip("Nikke List - List View item prefab")]
        [SerializeField]
        private RectTransform m_NikkeListItem;

        [SerializeField]
        private Toggle m_HideUIToggle;

        /// <summary>
        /// The Main Control UI.
        /// </summary>
        /// <value></value>
        public CanvasGroup MainControlUI
        {
            get => m_MainControlUI;
        }

        /// <summary>
        /// The welcome panel.
        /// </summary>
        /// <value></value>
        public RectTransform WelcomePanel
        {
            get => m_WelcomePanel;
        }

        /// <summary>
        /// Nikke List - List View content.
        /// </summary>
        /// <value></value>
        public RectTransform NikkeListContent
        {
            get => m_NikkeListContent;
        }

        /// <summary>
        /// Event triggered after settings has been applied.
        /// </summary>
        public event OnSettingsAppliedHandler OnSettingsApplied;
        public delegate void OnSettingsAppliedHandler();

        private readonly SpineHelperBase spineHelper = new();
        private SettingsManager settingsManager;

        private GameObject _focusedInput;
        private TMP_InputField[] _inputFields;

        // private InputManager inputManager;

        #region Initialization
        private void Awake()
        {
            // inputManager = FindObjectsByType<InputManager>(FindObjectsSortMode.None)[0];
            settingsManager = GetComponent<SettingsManager>();
            _inputFields = FindObjectsByType<TMP_InputField>(FindObjectsSortMode.None);

            NonNativeKeyboard.Instance.OnTextSubmitted += OnScreenKeyboard_OnTextSubmitted;
            PanelNotificationCenter.OnActiveTabChanged +=
                PanelNotificationCenter_OnActiveTabChanged;
        }

        private void OnEnable()
        {
            // inputManager.ToggleUI.performed += ToggleUI;
            m_HideUIToggle.onValueChanged.AddListener(ToggleHideUI);
        }

        private void OnDestroy()
        {
            // inputManager.ToggleHideUI.performed -= ToggleUI;
            m_HideUIToggle.onValueChanged.RemoveListener(ToggleHideUI);

            PanelNotificationCenter.OnActiveTabChanged -=
                PanelNotificationCenter_OnActiveTabChanged;
            NonNativeKeyboard.Instance.OnTextSubmitted -= OnScreenKeyboard_OnTextSubmitted;
            Array.ForEach(
                _inputFields,
                inputField =>
                    inputField.onSelect.RemoveListener(_ =>
                        ShowOnScreenKeyboard("", inputField.gameObject)
                    )
            );
        }
        #endregion

        #region Runtime - UI Bindings
        /// <summary>
        /// Show On-Screen Keyboard.
        /// </summary>
        /// <param name="initialText"></param>
        /// <param name="focusedObject"></param>
        public void ShowOnScreenKeyboard(string initialText, GameObject focusedObject)
        {
            NonNativeKeyboard.Instance.PresentKeyboard();
            _focusedInput = focusedObject;
        }

        public void OnScreenKeyboard_OnTextSubmitted(object sender, EventArgs e)
        {
            NonNativeKeyboard keyboard = sender as NonNativeKeyboard;
            if (_focusedInput.TryGetComponent(out TMP_InputField inputField))
                inputField.text = keyboard.InputField.text;
            _focusedInput = null;
        }

        private void PanelNotificationCenter_OnActiveTabChanged(PanelTab tab)
        {
            Array.ForEach(
                _inputFields,
                inputField =>
                    inputField.onSelect.AddListener(_ =>
                        ShowOnScreenKeyboard("", inputField.gameObject)
                    )
            );
        }

        /// <summary>
        /// Add Nikke List item.
        /// </summary>
        /// <typeparam name="NikkeListItem"></typeparam>
        /// <returns></returns>
        public NikkeListItem AddNikkeListItem() =>
            Instantiate(m_NikkeListItem, m_NikkeListContent).GetComponent<NikkeListItem>();

        public void AddNikkeUI() => AddNikkeListItem();

        /// <summary>
        /// Apply settings.
        /// </summary>
        /// <returns></returns>
        public async void ApplyNikkeSettings()
        {
            NikkeListItem[] listItems = m_NikkeListContent.GetComponentsInChildren<NikkeListItem>();
            List<Nikke> nikkeDataList = new();

            foreach (NikkeListItem item in listItems)
            {
                string nikkeName = item.NikkeNameText.text;
                string skelPath = await GetAssetPath(item.SkelPathText.text);
                string atlasPath = await GetAssetPath(item.AtlasPathText.text);
                List<string> texturesPath = await GetAssetsPath(item.TexturesPathText.text);
                List<string> voicesSource = string.IsNullOrEmpty(item.VoicesSourceText.text)
                    ? new()
                    : item.VoicesSourceText.text.Split(", ").ToList();

                if (item.Viewer == null)
                {
                    item.Viewer = await InitializeViewer(
                        item,
                        nikkeName,
                        skelPath,
                        atlasPath,
                        texturesPath,
                        voicesSource
                    );
                }
                else
                {
                    if (IsAssetFieldsChanged(item))
                    {
                        Destroy(item.Viewer.gameObject);
                        item.Viewer = await InitializeViewer(
                            item,
                            nikkeName,
                            skelPath,
                            atlasPath,
                            texturesPath,
                            voicesSource
                        );
                    }

                    if (
                        item.Viewer.NikkeData.VoicesSource.Count == 0
                        && !string.IsNullOrEmpty(item.VoicesSourceText.text)
                    )
                    {
                        item.Viewer.NikkeData.VoicesSource = voicesSource;
                        item.Viewer.NikkeData.VoicesPath = await GetVoicesPath(voicesSource);
                        item.Viewer.TouchVoices = await CacheTouchVoices(
                            item.Viewer.NikkeData.VoicesPath
                        );
                    }
                }

                nikkeDataList.Add(item.Viewer.NikkeData);
            }

            settingsManager.NikkeSettings.NikkeList = nikkeDataList;
            OnSettingsApplied?.Invoke();

            foreach (NikkeListItem item in listItems)
            {
                await UniTask.WaitUntil(
                    () => item.Viewer.Skins != null && item.Viewer.Skins.Length > 0
                );
                item.SkinDropdown.options = item
                    .Viewer.Skins.Select(skin => new TMP_Dropdown.OptionData(skin))
                    .ToList();
            }

            await settingsManager.SaveSettings();
        }

        public void ToggleHideUI(bool state)
        {
            m_MainControlUI.gameObject.SetActive(!state);
            settingsManager.NikkeSettings.HideUI = state;
            m_HideUIToggle.isOn = state;
        }

        // private void ToggleHideUI(InputAction.CallbackContext ctx) => ToggleHideUI(ctx.performed);

        public void HideInfoUI(bool state)
        {
            m_WelcomePanel.gameObject.SetActive(!state);
            settingsManager.NikkeSettings.IsFirstTime = !state;
        }

        /// <summary>
        /// Has asset path UI Fields value changed?
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private bool IsAssetFieldsChanged(NikkeListItem item)
        {
            return (
                    !string.IsNullOrEmpty(item.SkelPathText.text)
                    && item.SkelPathText.text != item.Viewer.NikkeData.SkelPath
                )
                || (
                    !string.IsNullOrEmpty(item.AtlasPathText.text)
                    && item.AtlasPathText.text != item.Viewer.NikkeData.AtlasPath
                )
                || (
                    !string.IsNullOrEmpty(item.TexturesPathText.text)
                    && !item
                        .TexturesPathText.text.Split(", ")
                        .ToList()
                        .SequenceEqual(item.Viewer.NikkeData.TexturesPath)
                );
        }
        #endregion

        #region Spine Viewer
        /// <summary>
        /// Initialize Viewer data.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="nikkeName"></param>
        /// <param name="skelPath"></param>
        /// <param name="atlasPath"></param>
        /// <param name="texturesPath"></param>
        /// <param name="voicesSource"></param>
        /// <returns></returns>
        private async UniTask<NikkeViewerBase> InitializeViewer(
            NikkeListItem item,
            string nikkeName,
            string skelPath,
            string atlasPath,
            List<string> texturesPath,
            List<string> voicesSource
        )
        {
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
                    VoicesPath = await GetVoicesPath(voicesSource),
                };
                viewer.TouchVoices = await CacheTouchVoices(viewer.NikkeData.VoicesPath);
                item.name = viewer.name = string.IsNullOrEmpty(nikkeName)
                    ? viewer.NikkeData.AssetName
                    : nikkeName;
            }
            return viewer;
        }

        /// <summary>
        /// Instantiate Nikke Viewer.
        /// </summary>
        /// <param name="skelPath"></param>
        /// <returns></returns>
        public async UniTask<NikkeViewerBase> InstantiateViewer(string skelPath)
        {
            string skelVersion = await spineHelper.GetSkelVersion(skelPath);
            return skelVersion switch
            {
                "4.0" => Instantiate(m_NikkeVersions["4.0"]),
                "4.1" => Instantiate(m_NikkeVersions["4.1"]),
                _ => InvalidSkelVersion(skelVersion, skelPath),
            };
        }

        /// <summary>
        /// Return error message from unknown/invalid .skel version.
        /// </summary>
        /// <param name="skelVersion"></param>
        /// <param name="skelPath"></param>
        /// <returns></returns>
        private static NikkeViewerBase InvalidSkelVersion(string skelVersion, string skelPath)
        {
            Debug.LogError(
                $"Unable to load Skeleton asset! Invalid Skeleton version: {skelVersion} in {skelPath}"
            );
            return null;
        }
        #endregion

        #region Asset Utils
        /// <summary>
        /// Get asset path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private async UniTask<string> GetAssetPath(string path)
        {
            return WebRequestHelper.IsHttp(path) && !string.IsNullOrEmpty(path)
                ? await WebRequestHelper.CacheAsset(
                    path,
                    Path.Combine(
                        settingsManager.CachedDataDirectory,
                        Path.GetFileName(new Uri(path).AbsolutePath)
                    )
                )
                : path;
        }

        /// <summary>
        /// Get assets path.
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        private async UniTask<List<string>> GetAssetsPath(string paths) =>
            (await UniTask.WhenAll(paths.Split(", ").Select(GetAssetPath))).ToList();

        /// <summary>
        /// Get voices path.
        /// </summary>
        /// <param name="sources"></param>
        /// <returns></returns>
        private async UniTask<List<string>> GetVoicesPath(List<string> sources)
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

        /// <summary>
        /// Cache touch voices AudioClip.
        /// </summary>
        /// <param name="sources"></param>
        /// <returns></returns>
        private async UniTask<List<AudioClip>> CacheTouchVoices(List<string> sources) =>
            (
                await UniTask.WhenAll(
                    sources.Select(async path =>
                        await WebRequestHelper.GetAudioClip(await GetCachedPath(path))
                    )
                )
            ).ToList();

        /// <summary>
        /// Get cached asset path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private async UniTask<string> GetCachedPath(string path) =>
            WebRequestHelper.IsHttp(path)
                ? await WebRequestHelper.CacheAsset(
                    path,
                    Path.Combine(
                        settingsManager.CachedDataDirectory,
                        Path.GetFileName(new Uri(path).AbsolutePath)
                    )
                )
                : path;
        #endregion
    }
}
