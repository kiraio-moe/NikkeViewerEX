using Cysharp.Threading.Tasks;
using NikkeViewerEX.Core;
using NikkeViewerEX.Utils;
using Spine.Unity;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NikkeViewerEX.Components
{
    [AddComponentMenu("Nikke Viewer EX/Components/Nikke Viewer 4.0")]
    public class NikkeViewer : NikkeViewerBase
    {
        [Header("Spine Settings")]
        [SerializeField]
        string m_DefaultAnimation = "idle";

        [SerializeField]
        string m_TouchAnimation = "action";

        SkeletonAnimation skeletonAnimation;
        bool allowInteraction = true;

        public override void Awake()
        {
            base.Awake();
            MainControl.OnSettingsApplied += SpawnNikke;
            SettingsManager.OnSettingsLoaded += SpawnNikke;
            InputManager.PointerClick.performed += Interact;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            MainControl.OnSettingsApplied -= SpawnNikke;
            SettingsManager.OnSettingsLoaded -= SpawnNikke;
            InputManager.PointerClick.performed -= Interact;
        }

        async void SpawnNikke()
        {
            if (skeletonAnimation == null)
            {
                skeletonAnimation = await CreateNikke();
                AddMeshCollider();
            }
        }

        async UniTask<SkeletonAnimation> CreateNikke()
        {
            return await SpineHelper.InstantiateSpine(
                NikkeData.SkelPath,
                NikkeData.AtlasPath,
                NikkeData.TexturesPath,
                gameObject,
                Shader.Find("Universal Render Pipeline/Spine 4.0/Skeleton"),
                spineScale: 0.25f,
                loop: true
            );
        }

        void Interact(InputAction.CallbackContext ctx)
        {
            if (ctx.performed && allowInteraction)
            {
                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.collider.TryGetComponent(out NikkeViewer viewer) && !IsDragged)
                    {
                        if (viewer == this)
                        {
                            allowInteraction = false;

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
                            skeletonAnimation.AnimationState.GetCurrent(0).Complete += async _ =>
                            {
                                await UniTask.WaitUntil(() => !NikkeAudioSource.isPlaying);
                                allowInteraction = true;
                            };
                        }
                    }
                }
            }
        }
    }
}
