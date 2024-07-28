using UnityEngine;
using UnityEngine.InputSystem;

namespace NikkeViewerEX.Core
{
    [AddComponentMenu("Nikke Viewer EX/Core/Input Manager")]
    public class InputManager : MonoBehaviour
    {
        public InputAction PointerClick { get; private set; }
        public InputAction PointerHold { get; private set; }
        public InputAction ToggleUI { get; private set; }

        [SerializeField]
        InputActionAsset inputSettings;

        void Awake()
        {
            PointerClick = inputSettings.FindActionMap("Nikke").FindAction("PointerClick");
            PointerHold = inputSettings.FindActionMap("Nikke").FindAction("PointerHold");
            ToggleUI = inputSettings.FindActionMap("UI").FindAction("ToggleUI");
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
