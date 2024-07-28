using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NikkeViewerEX.Core;
using NikkeViewerEX.Serialization;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NikkeViewerEX.Components
{
    public abstract class NikkeViewerBase : MonoBehaviour
    {
        public Nikke NikkeData = new();
        public string[] Skins { get; set; }

        public delegate void OnSkinChangedHandler(int index);
        public event OnSkinChangedHandler OnSkinChanged;

        public MainControl MainControl { get; private set; }
        public SettingsManager SettingsManager { get; private set; }
        public InputManager InputManager { get; private set; }

        readonly float dragSmoothTime = .1f;
        Vector2 dragObjectVelocity;
        Vector3 dragObjectOffset;
        public bool IsDragged { get; private set; }

        public AudioSource NikkeAudioSource { get; private set; }
        public List<AudioClip> TouchVoices { get; set; } = new();
        public int TouchVoiceIndex = 0;

        public bool AllowInteraction { get; set; } = true;

        void Awake()
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

        public void InvokeChangeSkin(int index) => OnSkinChanged?.Invoke(index);

        public void AddMeshCollider()
        {
            MeshCollider meshCollider =
                gameObject.AddComponent(typeof(MeshCollider)) as MeshCollider;
            if (TryGetComponent(out MeshFilter meshFilter))
                meshCollider.sharedMesh = meshFilter.sharedMesh;
        }

        async void DragNikke(InputAction.CallbackContext ctx)
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

        async UniTask DragUpdate(GameObject clickedObject)
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

        async UniTask PostDragNikke()
        {
            if (this != null)
            {
                NikkeData.Position = gameObject.transform.position;
                dragObjectVelocity = Vector2.zero;
                IsDragged = false;
                await SettingsManager.SaveSettings();
            }
        }
    }
}
