using System;
using UnityEngine;
using UnityEngine.InputSystem;

// đọc input của người chơi kết nối trực tiếp với Unity Input System
public sealed class PlayerInputReader : MonoBehaviour
{
    public event Action BlinkPressed;
    public event Action BlinkReleased;
    public event Action PausePressed;
    public event Action InteractPressed;

    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }

    [Header("Input Action Asset")]
    [Tooltip("Asset Input Actions (mặc định lấy InputSystem_Actions của Project Settings)")]
    [SerializeField] private InputActionAsset actionsAsset;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction blinkAction;
    private InputAction pauseAction;
    private InputAction interactAction;
    private bool isUsingAssetActions;
    private int lastInteractFrame = -1;

    private void Awake()
    {
        InitializeActions();
    }

    private void InitializeActions()
    {
        // 1. Thử lấy InputActionAsset nếu chưa gán trực tiếp
        if (actionsAsset == null)
        {
#if UNITY_2020_1_OR_NEWER
            try
            {
                var prop = typeof(InputSystem).GetProperty("actions");
                if (prop != null)
                {
                    actionsAsset = prop.GetValue(null) as InputActionAsset;
                }
            }
            catch
            {
                // Fallback nếu không hỗ trợ reflection
            }
#endif
            if (actionsAsset == null)
            {
                InputActionAsset[] foundAssets = Resources.FindObjectsOfTypeAll<InputActionAsset>();
                for (int i = 0; i < foundAssets.Length; i++)
                {
                    if (foundAssets[i].name == "InputSystem_Actions")
                    {
                        actionsAsset = foundAssets[i];
                        break;
                    }
                }
            }
        }

        // 2. Lấy các Action từ Map "Player" nếu có Asset
        InputActionMap playerMap = actionsAsset != null ? actionsAsset.FindActionMap("Player") : null;
        if (playerMap != null)
        {
            moveAction = playerMap.FindAction("Move");
            lookAction = playerMap.FindAction("Look");
            interactAction = playerMap.FindAction("Interact");
            blinkAction = playerMap.FindAction("Jump") ?? playerMap.FindAction("Blink");
            pauseAction = actionsAsset.FindAction("UI/Cancel") ?? playerMap.FindAction("Pause");
            isUsingAssetActions = true;
        }

        // 3. Fallback programmatic nếu chưa có action tương ứng
        if (moveAction == null)
        {
            moveAction = new InputAction("Move", InputActionType.Value);
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        }

        if (lookAction == null)
        {
            lookAction = new InputAction("Look", InputActionType.Value, "<Mouse>/delta");
        }

        if (blinkAction == null)
        {
            blinkAction = CreateButtonAction("Blink", "<Keyboard>/space");
        }

        if (pauseAction == null)
        {
            pauseAction = CreateButtonAction("Pause", "<Keyboard>/escape");
        }

        if (interactAction == null)
        {
            interactAction = new InputAction("Interact", InputActionType.Button);
            interactAction.AddBinding("<Mouse>/leftButton");
            interactAction.AddBinding("<Keyboard>/e");
            interactAction.AddBinding("<Gamepad>/buttonNorth");
        }

        // Đăng ký sự kiện
        blinkAction.performed += OnBlink;
        blinkAction.canceled += OnBlinkReleased;
        pauseAction.performed += OnPause;

        interactAction.performed += OnInteractAction;
        interactAction.started += OnInteractAction;
    }

    private static InputAction CreateButtonAction(string actionName, string binding) =>
        new InputAction(actionName, InputActionType.Button, binding);

    private void OnEnable()
    {
        moveAction?.Enable();
        lookAction?.Enable();
        blinkAction?.Enable();
        pauseAction?.Enable();
        interactAction?.Enable();
        LockCursor();
    }

    private void Update()
    {
        if (moveAction != null)
            MoveInput = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);

        if (lookAction != null)
            LookInput = lookAction.ReadValue<Vector2>();

        // Fallback kiểm tra trực tiếp phím bấm bàn phím / chuột để đảm bảo không bị sót khung hình
        CheckDirectInputFallbacks();
    }

    private void CheckDirectInputFallbacks()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                TriggerInteract();
            }
        }

        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                TriggerInteract();
            }
        }
    }

    private void OnDisable()
    {
        moveAction?.Disable();
        lookAction?.Disable();
        blinkAction?.Disable();
        pauseAction?.Disable();
        interactAction?.Disable();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnDestroy()
    {
        if (blinkAction != null)
        {
            blinkAction.performed -= OnBlink;
            blinkAction.canceled -= OnBlinkReleased;
        }

        if (pauseAction != null)
        {
            pauseAction.performed -= OnPause;
        }

        if (interactAction != null)
        {
            interactAction.performed -= OnInteractAction;
            interactAction.started -= OnInteractAction;
        }

        if (!isUsingAssetActions)
        {
            moveAction?.Dispose();
            lookAction?.Dispose();
            blinkAction?.Dispose();
            pauseAction?.Dispose();
            interactAction?.Dispose();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && isActiveAndEnabled)
            LockCursor();
    }

    private static void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnBlink(InputAction.CallbackContext context)
    {
        BlinkPressed?.Invoke();
    }

    private void OnBlinkReleased(InputAction.CallbackContext context)
    {
        BlinkReleased?.Invoke();
    }

    private void OnPause(InputAction.CallbackContext context)
    {
        PausePressed?.Invoke();
    }

    private void OnInteractAction(InputAction.CallbackContext context)
    {
        TriggerInteract();
    }

    private void TriggerInteract()
    {
        if (Time.frameCount == lastInteractFrame)
            return;

        lastInteractFrame = Time.frameCount;
        InteractPressed?.Invoke();
    }
}
