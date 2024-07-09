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
        Nikke m_NikkeData;

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
        readonly float dragSmoothTime = .1f;
        Vector2 dragObjectVelocity;
        Vector3 dragObjectOffset;

        void Awake()
        {
            mainControl = FindObjectsByType<MainControl>(FindObjectsSortMode.None)[0];
            inputManager = FindObjectsByType<InputManager>(FindObjectsSortMode.None)[0];
            settingsManager = FindObjectsByType<SettingsManager>(FindObjectsSortMode.None)[0];

            inputManager.PointerClick.started += MousePressed;
            inputManager.PointerClick.canceled += SaveNikkePosition;
        }

        void OnDestroy()
        {
            inputManager.PointerClick.started -= MousePressed;
            inputManager.PointerClick.canceled -= SaveNikkePosition;
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

        void MousePressed(InputAction.CallbackContext ctx)
        {
            if (IsLocked)
                return;

            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider != null)
                    DragUpdate(hit.collider.gameObject).Forget();
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

            while (inputManager.PointerClick.ReadValue<float>() != 0)
            {
                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
                Vector3 targetPoint = ray.GetPoint(initialDistance) + dragObjectOffset;
                clickedObject.transform.position = Vector2.SmoothDamp(
                    clickedObject.transform.position,
                    targetPoint,
                    ref dragObjectVelocity,
                    dragSmoothTime
                );

                await UniTask.Yield();
            }
        }

        void SaveNikkePosition(InputAction.CallbackContext ctx)
        {
            if (this != null)
                NikkeData.Position = gameObject.transform.position;
        }
    }
}
