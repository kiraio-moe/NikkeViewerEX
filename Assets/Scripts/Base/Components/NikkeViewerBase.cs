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
        [SerializeField]
        Nikke m_NikkeData = new();
        public Nikke NikkeData
        {
            get => m_NikkeData;
            set => m_NikkeData = value;
        }

        bool isLocked;
        public bool IsLocked
        {
            get => isLocked;
            set => isLocked = value;
        }

        MainControl mainControl;
        public MainControl MainControl
        {
            get => mainControl;
        }
        SettingsManager settingsManager;
        public SettingsManager SettingsManager
        {
            get => settingsManager;
        }
        InputManager inputManager;
        public InputManager InputManager
        {
            get => inputManager;
        }

        readonly float dragSmoothTime = .1f;
        Vector2 dragObjectVelocity;
        Vector3 dragObjectOffset;
        bool isDragged;
        public bool IsDragged
        {
            get => isDragged;
        }

        AudioSource nikkeAudioSource;
        public AudioSource NikkeAudioSource
        {
            get => nikkeAudioSource;
        }
        List<AudioClip> touchVoices = new();
        public List<AudioClip> TouchVoices
        {
            get => touchVoices;
            set => touchVoices = value;
        }
        int touchVoiceIndex = 0;
        public int TouchVoiceIndex
        {
            get => touchVoiceIndex;
            set => touchVoiceIndex = value;
        }

        public virtual void Awake()
        {
            mainControl = FindObjectsByType<MainControl>(FindObjectsSortMode.None)[0];
            inputManager = FindObjectsByType<InputManager>(FindObjectsSortMode.None)[0];
            settingsManager = FindObjectsByType<SettingsManager>(FindObjectsSortMode.None)[0];
            nikkeAudioSource = GetComponent<AudioSource>();
        }

        public virtual void Start()
        {
            inputManager.PointerHold.started += DragNikke;
            // inputManager.PointerHold.canceled += PostDragNikke;
        }

        public virtual void OnDestroy()
        {
            inputManager.PointerHold.started -= DragNikke;
            // inputManager.PointerHold.canceled -= PostDragNikke;
        }

        public void AddMeshCollider()
        {
            MeshCollider meshCollider =
                gameObject.AddComponent(typeof(MeshCollider)) as MeshCollider;
            if (TryGetComponent(out MeshFilter meshFilter))
            {
                meshCollider.sharedMesh = meshFilter.sharedMesh;
                // meshCollider.convex = true;
                // meshCollider.isTrigger = true;
            }
        }

        void DragNikke(InputAction.CallbackContext ctx)
        {
            if (ctx.started && !IsLocked)
            {
                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.collider.TryGetComponent(out NikkeViewerBase _))
                    {
                        DragUpdate(hit.collider.gameObject).Forget();
                    }
                }
            }
        }

        async UniTaskVoid DragUpdate(GameObject clickedObject)
        {
            float initialDistance = Vector3.Distance(
                clickedObject.transform.position,
                Camera.main.transform.position
            );

            Ray initialRay = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            Vector3 initialPoint = initialRay.GetPoint(initialDistance);
            dragObjectOffset = clickedObject.transform.position - initialPoint;

            while (inputManager.PointerHold.ReadValue<float>() != 0)
            {
                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
                Vector3 targetPoint = ray.GetPoint(initialDistance) + dragObjectOffset;
                clickedObject.transform.position = Vector2.SmoothDamp(
                    clickedObject.transform.position,
                    targetPoint,
                    ref dragObjectVelocity,
                    dragSmoothTime
                );
                isDragged = true;
                await UniTask.Yield();
            }

            PostDragNikke();
        }

        void PostDragNikke()
        {
            if (this != null)
            {
                NikkeData.Position = gameObject.transform.position;
                dragObjectVelocity = Vector2.zero;
                isDragged = false;
                settingsManager.SaveSettings().Forget();
            }
        }
    }
}
