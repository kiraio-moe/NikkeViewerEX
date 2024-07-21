using UnityEngine;
using UnityEngine.InputSystem;

namespace NikkeViewerEX.Core
{
    [AddComponentMenu("Nikke Viewer EX/Core/Input Manager")]
    // [RequireComponent(typeof(PlayerInput))]
    public class InputManager : MonoBehaviour
    {
        public InputAction PointerClick { get; private set; }
        public InputAction PointerHold { get; private set; }
        public InputAction ToggleUI { get; private set; }

        InputSettings inputSettings;

        void Awake()
        {
            inputSettings = new();
            PointerClick = inputSettings.Nikke.PointerClick;
            PointerHold = inputSettings.Nikke.PointerHold;
            ToggleUI = inputSettings.UI.ToggleUI;
        }

        void OnEnable()
        {
            inputSettings.Enable();
        }

        void OnDestroy()
        {
            inputSettings.Disable();
        }
    }
}
