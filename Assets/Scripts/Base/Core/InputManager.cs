using UnityEngine;
using UnityEngine.InputSystem;

namespace NikkeViewerEX.Core
{
    [AddComponentMenu("Nikke Viewer EX/Core/Input Manager")]
    [RequireComponent(typeof(PlayerInput))]
    public class InputManager : MonoBehaviour
    {
        public InputAction PointerClick { get; private set; }
        public InputAction PointerHold { get; private set; }
        public InputAction ToggleUI { get; private set; }

        PlayerInput playerInput;

        void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            playerInput.actions.Enable();

            PointerClick = playerInput.actions.FindActionMap("Nikke").FindAction("PointerClick");
            PointerHold = playerInput.actions.FindActionMap("Nikke").FindAction("PointerHold");
            ToggleUI = playerInput.actions.FindActionMap("UI").FindAction("ToggleUI");
        }

        void OnDestroy()
        {
            playerInput.actions.Disable();
        }
    }
}
