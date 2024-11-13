using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NikkeViewerEX.Core;
using NikkeViewerEX.Serialization;
using NikkeViewerEX.Utils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NikkeViewerEX.Components
{
    /// <summary>
    /// The base class of Nikke Viewer.
    /// </summary>
    public abstract class NikkeViewerBase : MonoBehaviour
    {
        /// <summary>
        /// The data of Nikke.
        /// </summary>
        /// <returns></returns>
        public Nikke NikkeData = new();
        public string[] Skins { get; set; }

        public delegate void OnSkinChangedHandler(int index);
        public event OnSkinChangedHandler OnSkinChanged;

        public MainControl MainControl { get; private set; }
        public SettingsManager SettingsManager { get; private set; }
        public InputManager InputManager { get; private set; }
        readonly SpineHelperBase spineHelper = new SpineHelperBase();

        readonly float dragSmoothTime = .1f;
        Vector2 dragObjectVelocity;
        Vector3 dragObjectOffset;

        readonly float _scrollSensitivity = 0.05f;
        readonly float _nikkeMinScale = 0.2f;
        readonly float _nikkeMaxScale = 5f;

        /// <summary>
        /// Does Nikke currently being dragged?
        /// </summary>
        /// <value></value>
        public bool IsDragged { get; private set; }

        /// <summary>
        /// The AudioSource component of the Nikke.
        /// </summary>
        /// <value></value>
        public AudioSource NikkeAudioSource { get; private set; }

        /// <summary>
        /// List of touch voices AudioClip.
        /// </summary>
        /// <returns></returns>
        public List<AudioClip> TouchVoices { get; set; } = new();

        /// <summary>
        /// Current touch voice index that has been/being played from TouchVoices list.
        /// </summary>
        public int TouchVoiceIndex = 0;

        /// <summary>
        /// Allow interacting with the Nikke?
        /// </summary>
        /// <value></value>
        public bool AllowInteraction { get; set; } = true;

        private void Awake()
        {
            MainControl = FindObjectsByType<MainControl>(FindObjectsSortMode.None)[0];
            InputManager = FindObjectsByType<InputManager>(FindObjectsSortMode.None)[0];
            SettingsManager = FindObjectsByType<SettingsManager>(FindObjectsSortMode.None)[0];
            NikkeAudioSource = GetComponent<AudioSource>();
        }

        public virtual void OnEnable()
        {
            InputManager.PointerHold.started += DragNikke;
        }

        public virtual void OnDestroy()
        {
            InputManager.PointerHold.started -= DragNikke;
        }

        private void Update()
        {
            // if (!NikkeData.Lock)
            ChangeNikkeScale();
        }

        /// <summary>
        /// Invoke OnSkinChanged event.
        /// </summary>
        /// <param name="index"></param>
        public void InvokeChangeSkin(int index) => OnSkinChanged?.Invoke(index);

        /// <summary>
        /// Add MeshCollider component to Nikke.
        /// </summary>
        public void AddMeshCollider()
        {
            MeshCollider meshCollider =
                gameObject.AddComponent(typeof(MeshCollider)) as MeshCollider;
            if (TryGetComponent(out MeshFilter meshFilter))
                meshCollider.sharedMesh = meshFilter.sharedMesh;
        }

        #region Drag & Drop Nikke
        /// <summary>
        /// Perform Raycast from pointer to start dragging.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        private async void DragNikke(InputAction.CallbackContext ctx)
        {
            if (ctx.started && !NikkeData.Lock)
            {
                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.collider.TryGetComponent(out NikkeViewerBase _))
                    {
                        await DragUpdate(hit.collider.gameObject);
                    }
                }
            }
        }

        /// <summary>
        /// Update Nikke position based on pointer.
        /// </summary>
        /// <param name="clickedObject"></param>
        /// <returns></returns>
        private async UniTask DragUpdate(GameObject clickedObject)
        {
            if (NikkeData.Lock)
                return;

            float initialDistance = Vector3.Distance(
                clickedObject.transform.position,
                Camera.main.transform.position
            );

            Ray initialRay = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            Vector3 initialPoint = initialRay.GetPoint(initialDistance);
            dragObjectOffset = clickedObject.transform.position - initialPoint;

            while (InputManager.PointerHold.ReadValue<float>() != 0)
            {
                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
                Vector3 targetPoint = ray.GetPoint(initialDistance) + dragObjectOffset;
                clickedObject.transform.position = Vector2.SmoothDamp(
                    clickedObject.transform.position,
                    targetPoint,
                    ref dragObjectVelocity,
                    dragSmoothTime
                );
                IsDragged = true;
                await UniTask.Yield();
            }

            await PostDragNikke();
        }

        /// <summary>
        /// Post action after dropping the Nikke.
        /// </summary>
        /// <returns></returns>
        private async UniTask PostDragNikke()
        {
            if (this != null)
            {
                NikkeData.Position = gameObject.transform.position;
                dragObjectVelocity = Vector2.zero;
                IsDragged = false;
                await SettingsManager.SaveSettings();
            }
        }
        #endregion

        private void ChangeNikkeScale()
        {
            if (!NikkeData.Lock)
            {
                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.collider.TryGetComponent(out NikkeViewerBase viewer))
                    {
                        float scrollDelta = Mouse.current.scroll.ReadValue().y;
                        if (scrollDelta != 0 && !viewer.NikkeData.Lock)
                        {
                            Vector3 newScale = spineHelper.ClampVector3(
                                hit.transform.localScale
                                    + _scrollSensitivity * scrollDelta * Vector3.one,
                                _nikkeMinScale,
                                _nikkeMaxScale
                            );
                            hit.transform.localScale = Vector3.Lerp(
                                hit.transform.localScale,
                                newScale,
                                0.5f
                            );
                            NikkeData.Scale = newScale;
                            SettingsManager.SaveSettings().Forget();
                        }
                    }
                }
            }
        }

        public void ResetNikkeScale()
        {
            transform.localScale = Vector3.one;
        }
    }
}
