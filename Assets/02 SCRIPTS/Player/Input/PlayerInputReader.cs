using System;
using UnityEngine;
using UnityEngine.InputSystem;

// đọc input của người chơi
public sealed class PlayerInputReader : MonoBehaviour
{
    public event Action BlinkPressed;
    public event Action PausePressed;
    public event Action RestartPressed;
    public event Action DebugPressed;

    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction blinkAction;
    private InputAction pauseAction;
    private InputAction restartAction;
    private InputAction debugAction;

    private void Awake()
    {
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");

        lookAction = new InputAction("Look", InputActionType.Value, "<Mouse>/delta");
        blinkAction = CreateButtonAction("Blink", "<Keyboard>/space");
        pauseAction = CreateButtonAction("Pause", "<Keyboard>/escape");
        restartAction = CreateButtonAction("Restart", "<Keyboard>/r");
        debugAction = CreateButtonAction("Debug", "<Keyboard>/f1");

        blinkAction.performed += OnBlink;
        pauseAction.performed += OnPause;
        restartAction.performed += OnRestart;
        debugAction.performed += OnDebug;
    }

    private static InputAction CreateButtonAction(string actionName, string binding) =>
        new InputAction(actionName, InputActionType.Button, binding);

    private void OnEnable()
    {
        moveAction.Enable();
        lookAction.Enable();
        blinkAction.Enable();
        pauseAction.Enable();
        restartAction.Enable();
        debugAction.Enable();
        LockCursor();
    }

    private void Update()
    {
        MoveInput = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);
        LookInput = lookAction.ReadValue<Vector2>();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        lookAction.Disable();
        blinkAction.Disable();
        pauseAction.Disable();
        restartAction.Disable();
        debugAction.Disable();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnDestroy()
    {
        blinkAction.performed -= OnBlink;
        pauseAction.performed -= OnPause;
        restartAction.performed -= OnRestart;
        debugAction.performed -= OnDebug;
        moveAction.Dispose();
        lookAction.Dispose();
        blinkAction.Dispose();
        pauseAction.Dispose();
        restartAction.Dispose();
        debugAction.Dispose();
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
        Debug.Log("Input: Blink (Space)");
        BlinkPressed?.Invoke();
    }

    private void OnPause(InputAction.CallbackContext context)
    {
        Debug.Log("Input: Pause (Esc)");
        PausePressed?.Invoke();
    }

    private void OnRestart(InputAction.CallbackContext context)
    {
        Debug.Log("Input: Restart (R)");
        RestartPressed?.Invoke();
    }

    private void OnDebug(InputAction.CallbackContext context)
    {
        Debug.Log("Input: Debug (F1)");
        DebugPressed?.Invoke();
    }
}
