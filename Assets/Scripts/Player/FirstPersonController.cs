using UnityEngine;
using UnityEngine.InputSystem;

namespace LudumDare.Player
{
    /// <summary>
    /// FPS movement: CharacterController on root, yaw from look X, pitch on camera child, walk on XZ, gravity when airborne.
    /// Uses InputSystem_Actions map "Player" actions Move and Look.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        [SerializeField] InputActionAsset inputActions;
        [SerializeField] Transform cameraTransform;
        [SerializeField] PlayerInput playerInput;

        CharacterController _controller;
        InputActionMap _playerMap;
        InputAction _moveAction;
        InputAction _lookAction;

        float _pitch;
        float _verticalVelocity;

        [Header("Movement")]
        [SerializeField] float walkSpeed = 5f;
        [SerializeField] float groundedStickDownVelocity = -2f;
        [SerializeField] float gravityMultiplier = 2f;

        [Header("Look")]
        [SerializeField] float mouseSensitivity = 0.12f;
        [SerializeField] float gamepadLookDegreesPerSecond = 120f;
        [SerializeField] float pitchMin = -89f;
        [SerializeField] float pitchMax = 89f;

        [Header("Cursor")]
        [SerializeField] bool lockCursorOnStart = true;
        [SerializeField] bool skipLookWhileCursorUnlocked = true;

        void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (playerInput == null)
                playerInput = GetComponent<PlayerInput>();
            if (inputActions == null && playerInput != null)
                inputActions = playerInput.actions;

            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            BindActions();
        }

        void BindActions()
        {
            if (inputActions == null)
                return;

            _playerMap = inputActions.FindActionMap("Player");
            _moveAction = _playerMap.FindAction("Move");
            _lookAction = _playerMap.FindAction("Look");
        }

        void OnEnable()
        {
            if (playerInput == null)
                _playerMap?.Enable();
        }

        void OnDisable()
        {
            if (playerInput == null)
                _playerMap?.Disable();
        }

        void Start()
        {
            if (lockCursorOnStart)
                ApplyCursorLock(true);
        }

        void Update()
        {
            HandleCursorToggle();

            if (_moveAction == null || _lookAction == null)
                return;

            var move = _moveAction.ReadValue<Vector2>();
            var look = _lookAction.ReadValue<Vector2>();

            ApplyLook(look);
            ApplyMovement(move);
        }

        void ApplyLook(Vector2 look)
        {
            if (skipLookWhileCursorUnlocked && Cursor.lockState != CursorLockMode.Locked)
                return;

            bool gamepadScheme = playerInput != null && playerInput.currentControlScheme == "Gamepad";

            float yawDelta;
            float pitchDelta;
            if (gamepadScheme)
            {
                yawDelta = look.x * gamepadLookDegreesPerSecond * Time.deltaTime;
                pitchDelta = look.y * gamepadLookDegreesPerSecond * Time.deltaTime;
            }
            else
            {
                yawDelta = look.x * mouseSensitivity;
                pitchDelta = look.y * mouseSensitivity;
            }

            transform.Rotate(0f, yawDelta, 0f, Space.World);
            _pitch -= pitchDelta;
            _pitch = Mathf.Clamp(_pitch, pitchMin, pitchMax);

            if (cameraTransform != null)
                cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        void ApplyMovement(Vector2 move)
        {
            var grounded = _controller.isGrounded;
            if (grounded && _verticalVelocity < 0f)
                _verticalVelocity = groundedStickDownVelocity;

            var forward = transform.forward;
            var right = transform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            var worldMove = forward * move.y + right * move.x;
            if (worldMove.sqrMagnitude > 1f)
                worldMove.Normalize();

            worldMove *= walkSpeed * Time.deltaTime;

            _verticalVelocity += Physics.gravity.y * gravityMultiplier * Time.deltaTime;

            var displacement = worldMove + Vector3.up * (_verticalVelocity * Time.deltaTime);
            _controller.Move(displacement);
        }

        void HandleCursorToggle()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                ApplyCursorLock(false);

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame &&
                Cursor.lockState != CursorLockMode.Locked)
                ApplyCursorLock(true);
        }

        void ApplyCursorLock(bool locked)
        {
            if (locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
}
