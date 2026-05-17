using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Valendia.Runtime
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class ValendiaFirstPersonController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform cameraRoot;

        [Header("Movement")]
        [SerializeField, Min(0.1f)] private float walkSpeed = 4.8f;
        [SerializeField, Min(0.1f)] private float sprintSpeed = 7.2f;
        [SerializeField, Min(0.1f)] private float acceleration = 18f;
        [SerializeField, Min(0f)] private float jumpHeight = 1.1f;
        [SerializeField, Min(0f)] private float gravity = 24f;

        [Header("Look")]
        [SerializeField, Min(0.01f)] private float mouseSensitivity = 0.12f;
        [SerializeField, Min(1f)] private float gamepadLookSpeed = 145f;
        [SerializeField, Range(0f, 0.5f)] private float gamepadMoveDeadZone = 0.12f;
        [SerializeField, Range(0f, 0.5f)] private float gamepadLookDeadZone = 0.12f;
        [SerializeField, Range(30f, 89f)] private float maxPitch = 82f;
        [SerializeField] private bool lockCursorOnPlay = true;
        [SerializeField] private bool logInputDevicesOnStart = true;

        private CharacterController character;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;
        private float pitch;

        private void Awake()
        {
            character = GetComponent<CharacterController>();

            if (cameraRoot == null)
            {
                Camera mainCamera = Camera.main;
                cameraRoot = mainCamera != null ? mainCamera.transform : transform;
            }
        }

        private void Start()
        {
            if (Application.isPlaying && lockCursorOnPlay)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (Application.isPlaying && logInputDevicesOnStart)
            {
                LogInputDevices();
            }
        }

        private void Update()
        {
            Vector2 moveInput = ReadMove();
            Vector2 lookInput = ReadLook(out bool isMouseLook);
            bool sprintHeld = ReadSprint();
            bool jumpPressed = ReadJump();

            ApplyLook(lookInput, isMouseLook);
            ApplyMovement(moveInput, sprintHeld, jumpPressed);
        }

        private void ApplyLook(Vector2 lookInput, bool isMouseLook)
        {
            float scale = isMouseLook ? mouseSensitivity : gamepadLookSpeed * Time.deltaTime;
            transform.Rotate(Vector3.up, lookInput.x * scale, Space.Self);

            pitch = Mathf.Clamp(pitch - lookInput.y * scale, -maxPitch, maxPitch);
            cameraRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void ApplyMovement(Vector2 moveInput, bool sprintHeld, bool jumpPressed)
        {
            Vector3 wishDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
            wishDirection = Vector3.ClampMagnitude(wishDirection, 1f);

            float targetSpeed = sprintHeld ? sprintSpeed : walkSpeed;
            Vector3 targetVelocity = wishDirection * targetSpeed;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, acceleration * Time.deltaTime);

            if (character.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            if (jumpPressed && character.isGrounded && jumpHeight > 0f)
            {
                verticalVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);
            }

            verticalVelocity -= gravity * Time.deltaTime;

            Vector3 velocity = horizontalVelocity + Vector3.up * verticalVelocity;
            character.Move(velocity * Time.deltaTime);
        }

        private Vector2 ReadMove()
        {
#if ENABLE_INPUT_SYSTEM
            Gamepad gamepad = CurrentGamepad();
            if (gamepad != null)
            {
                Vector2 input = gamepad.leftStick.ReadValue();
                if (input.magnitude > gamepadMoveDeadZone)
                {
                    return Vector2.ClampMagnitude(input, 1f);
                }
            }

            Joystick joystick = CurrentJoystick();
            if (joystick != null)
            {
                Vector2 input = joystick.stick.ReadValue();
                if (input.magnitude > gamepadMoveDeadZone)
                {
                    return Vector2.ClampMagnitude(input, 1f);
                }
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                Vector2 input = Vector2.zero;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
                return Vector2.ClampMagnitude(input, 1f);
            }
#endif
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        }

        private Vector2 ReadLook(out bool isMouseLook)
        {
#if ENABLE_INPUT_SYSTEM
            Gamepad gamepad = CurrentGamepad();
            if (gamepad != null)
            {
                Vector2 input = gamepad.rightStick.ReadValue();
                if (input.magnitude > gamepadLookDeadZone)
                {
                    isMouseLook = false;
                    return Vector2.ClampMagnitude(input, 1f);
                }
            }
#endif
            Vector2 legacyGamepadLook = ReadLegacyGamepadLook();
            if (legacyGamepadLook.sqrMagnitude > 0.0001f)
            {
                isMouseLook = false;
                return legacyGamepadLook;
            }

#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                isMouseLook = true;
                return mouse.delta.ReadValue();
            }
#endif
            isMouseLook = true;
            return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        }

        private Vector2 ReadLegacyGamepadLook()
        {
            Vector2 preferredLook = new Vector2(TryGetAxisRaw("RightStickX"), TryGetAxisRaw("RightStickY"));
            if (preferredLook.magnitude > gamepadLookDeadZone)
            {
                return Vector2.ClampMagnitude(preferredLook, 1f);
            }

            Vector2 look = BestLookCandidate(gamepadLookDeadZone,
                new Vector2(TryGetAxisRaw("RightStickX_Axis3"), TryGetAxisRaw("RightStickY_Axis4")),
                new Vector2(TryGetAxisRaw("RightStickX_Axis4"), TryGetAxisRaw("RightStickY_Axis5")),
                new Vector2(TryGetAxisRaw("RightStickX_Axis5"), TryGetAxisRaw("RightStickY_Axis6")),
                new Vector2(TryGetAxisRaw("RightStickX_Axis3"), TryGetAxisRaw("RightStickY_Axis5")));

            return look.magnitude > gamepadLookDeadZone ? Vector2.ClampMagnitude(look, 1f) : Vector2.zero;
        }

        private static Vector2 BestLookCandidate(float deadZone, params Vector2[] candidates)
        {
            Vector2 best = Vector2.zero;
            float bestMagnitude = 0f;

            for (int i = 0; i < candidates.Length; i++)
            {
                if (LooksLikeTriggerRest(candidates[i], deadZone))
                {
                    continue;
                }

                float magnitude = candidates[i].sqrMagnitude;
                if (magnitude <= bestMagnitude)
                {
                    continue;
                }

                bestMagnitude = magnitude;
                best = candidates[i];
            }

            return best;
        }

        private static bool LooksLikeTriggerRest(Vector2 candidate, float deadZone)
        {
            return Mathf.Abs(candidate.x) > 0.92f && Mathf.Abs(candidate.y) < deadZone
                || Mathf.Abs(candidate.y) > 0.92f && Mathf.Abs(candidate.x) < deadZone;
        }

        private static float TryGetAxisRaw(string axisName)
        {
            try
            {
                return Input.GetAxisRaw(axisName);
            }
            catch (System.ArgumentException)
            {
                return 0f;
            }
        }

        private static bool ReadJump()
        {
#if ENABLE_INPUT_SYSTEM
            Gamepad gamepad = CurrentGamepad();
            if (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame)
            {
                return true;
            }

            Joystick joystick = CurrentJoystick();
            if (joystick != null && joystick.trigger.wasPressedThisFrame)
            {
                return true;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                return keyboard.spaceKey.wasPressedThisFrame;
            }
#endif
            return Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.JoystickButton0);
        }

        private static bool ReadSprint()
        {
#if ENABLE_INPUT_SYSTEM
            Gamepad gamepad = CurrentGamepad();
            if (gamepad != null)
            {
                return gamepad.leftStickButton.isPressed || gamepad.leftShoulder.isPressed;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                return keyboard.leftShiftKey.isPressed;
            }
#endif
            return Input.GetKey(KeyCode.LeftShift)
                || Input.GetKey(KeyCode.JoystickButton4)
                || Input.GetKey(KeyCode.JoystickButton8);
        }

#if ENABLE_INPUT_SYSTEM
        private static Gamepad CurrentGamepad()
        {
            if (Gamepad.current != null)
            {
                return Gamepad.current;
            }

            return Gamepad.all.Count > 0 ? Gamepad.all[0] : null;
        }

        private static Joystick CurrentJoystick()
        {
            if (Joystick.current != null)
            {
                return Joystick.current;
            }

            return Joystick.all.Count > 0 ? Joystick.all[0] : null;
        }
#endif

        private static void LogInputDevices()
        {
#if ENABLE_INPUT_SYSTEM
            Debug.Log($"Valendia input: Input System gamepads = {InputSystemGamepadSummary()}");
            Debug.Log($"Valendia input: Input System joysticks = {InputSystemJoystickSummary()}");
#else
            Debug.Log("Valendia input: Input System scripting define is disabled.");
#endif
            string[] joystickNames = Input.GetJoystickNames();
            string legacySummary = joystickNames.Length == 0 ? "none" : string.Join(", ", joystickNames);
            Debug.Log($"Valendia input: legacy joysticks = {legacySummary}");
        }

#if ENABLE_INPUT_SYSTEM
        private static string InputSystemGamepadSummary()
        {
            if (Gamepad.all.Count == 0)
            {
                return "none";
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int i = 0; i < Gamepad.all.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                Gamepad gamepad = Gamepad.all[i];
                builder.Append(gamepad.displayName);
                builder.Append(" [");
                builder.Append(gamepad.deviceId);
                builder.Append(']');
            }

            return builder.ToString();
        }

        private static string InputSystemJoystickSummary()
        {
            if (Joystick.all.Count == 0)
            {
                return "none";
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int i = 0; i < Joystick.all.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                Joystick joystick = Joystick.all[i];
                builder.Append(joystick.displayName);
                builder.Append(" [");
                builder.Append(joystick.deviceId);
                builder.Append(']');
            }

            return builder.ToString();
        }
#endif
    }
}
