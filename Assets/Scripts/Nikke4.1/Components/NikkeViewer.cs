using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using NikkeViewerEX.Core;
using NikkeViewerEX.Utils;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NikkeViewerEX.Components
{
    [AddComponentMenu("Nikke Viewer EX/Components/Nikke Viewer 4.1")]
    public class NikkeViewer : NikkeViewerBase
    {
        [Header("Spine Settings")]
        [SerializeField]
        string m_DefaultAnimation = "idle";

        [SerializeField]
        string m_TouchAnimation = "action";

        [Header("UI")]
        [SerializeField]
        TextMeshPro m_NikkeNamePrefab;

        SkeletonAnimation skeletonAnimation;

        public override void OnEnable()
        {
            base.OnEnable();
            MainControl.OnSettingsApplied += SpawnNikke;
            MainControl.HideUIToggle.onValueChanged.AddListener(ToggleDisplayName);
            SettingsManager.OnSettingsLoaded += SpawnNikke;
            InputManager.PointerClick.performed += Interact;
            OnSkinChanged += ChangeSkin;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            MainControl.OnSettingsApplied -= SpawnNikke;
            MainControl.HideUIToggle.onValueChanged.RemoveListener(ToggleDisplayName);
            SettingsManager.OnSettingsLoaded -= SpawnNikke;
            InputManager.PointerClick.performed -= Interact;
            OnSkinChanged -= ChangeSkin;
        }

        public override void Update()
        {
            base.Update();
            if (NikkeNameText != null)
                UpdateDisplayName(NikkeNameText);
        }

        void ChangeSkin(int index)
        {
            skeletonAnimation.Skeleton.SetSkin(Skins[index]);
            skeletonAnimation.Skeleton.SetSlotsToSetupPose();
            skeletonAnimation.Update(0);
            //! Some background skins have weird mesh colliders, so you can interact with them outside of the visible character texture. It's best to disable them.
            // AddMeshCollider();
            // skeletonAnimation.LateUpdate();
            NikkeData.Skin = Skins[index];
        }

        private async void SpawnNikke()
        {
            try
            {
                if (skeletonAnimation == null)
                {
                    skeletonAnimation = await CreateNikke();
                    Skins = skeletonAnimation
                        .Skeleton.Data.Skins?.Select(skin => skin.Name)
                        .ToArray();
                    AddMeshCollider();
                    NikkeNameText = CreateDisplayName(NikkeData.NikkeName);
                    ToggleDisplayName(NikkeNameText);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating Nikke Viewer! {ex}");
            }
        }

        async UniTask<SkeletonAnimation> CreateNikke()
        {
            return await SpineHelper.InstantiateSpine(
                NikkeData.SkelPath,
                NikkeData.AtlasPath,
                NikkeData.TexturesPath,
                gameObject,
                Shader.Find("Universal Render Pipeline/Spine 4.1/Skeleton"),
                spineScale: 0.25f,
                loop: true
            );
        }

        private TextMeshPro CreateDisplayName(string name)
        {
            TextMeshPro tmp = Instantiate(m_NikkeNamePrefab, Vector3.zero, Quaternion.identity);
            tmp.transform.SetParent(SettingsManager.BackgroundImage.transform.parent, false);
            tmp.transform.localScale = Vector3.one;
            tmp.name = tmp.text = name;
            return tmp;
        }

        private void UpdateDisplayName(TextMeshPro tmp)
        {
            if (skeletonAnimation == null)
                return;

            Vector2 skeletonBounds = SpineHelper.GetSkeletonBounds(skeletonAnimation.Skeleton);

            Vector3 worldPosition = new Vector3(
                transform.position.x * transform.localScale.x,
                (transform.position.y + skeletonBounds.y + 0.5f) * transform.localScale.y,
                transform.position.z
            );

            Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);

            // Convert screen position to Canvas space
            RectTransform canvasRect = tmp.canvas.GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                Camera.main,
                out Vector2 canvasPosition
            );

            tmp.rectTransform.anchoredPosition = canvasPosition;
        }

        void Interact(InputAction.CallbackContext ctx)
        {
            if (ctx.performed && AllowInteraction)
            {
                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.collider.TryGetComponent(out NikkeViewer viewer))
                    {
                        if (viewer == this)
                        {
                            AllowInteraction = false;
                            Spine.Animation touchAnimation = skeletonAnimation
                                .skeletonDataAsset.GetAnimationStateData()
                                .SkeletonData.FindAnimation(m_TouchAnimation);

                            if (touchAnimation != null)
                            {
                                if (TouchVoices.Count > 0)
                                {
                                    NikkeAudioSource.clip = TouchVoices[TouchVoiceIndex];
                                    NikkeAudioSource.Play();
                                    TouchVoiceIndex = (TouchVoiceIndex + 1) % TouchVoices.Count;
                                }

                                skeletonAnimation.AnimationState.SetAnimation(
                                    0,
                                    m_TouchAnimation,
                                    false
                                );
                                skeletonAnimation.AnimationState.AddAnimation(
                                    0,
                                    m_DefaultAnimation,
                                    true,
                                    0
                                );
                                skeletonAnimation.AnimationState.GetCurrent(0).Complete +=
                                    async _ =>
                                    {
                                        if (NikkeAudioSource != null)
                                        {
                                            await UniTask.WaitUntil(
                                                () =>
                                                    NikkeAudioSource != null
                                                    && !NikkeAudioSource.isPlaying
                                            );
                                            AllowInteraction = true;
                                        }
                                    };
                            }
                        }
                    }
                }
            }
        }
    }
}
