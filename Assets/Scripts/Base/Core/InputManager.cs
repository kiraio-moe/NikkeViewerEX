using UnityEngine;
using UnityEngine.InputSystem;

namespace NikkeViewerEX.Core
{
    [AddComponentMenu("Nikke Viewer EX/Core/Input Manager")]
    [RequireComponent(typeof(PlayerInput))]
    public class InputManager : MonoBehaviour
    {
        InputSettings inputSettings;
        public InputAction PointerClick { get; private set; }

        void OnEnable()
        {
            inputSettings.Enable();
        }

        void OnDisable()
        {
            inputSettings.Disable();
        }

        void Awake()
        {
            inputSettings = new();
            PointerClick = inputSettings.Nikke.PointerClick;
        }
    }
}
