using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerLook : MonoBehaviour
{
    [SerializeField] private Transform cameraPivot;
    [SerializeField, Min(0f)] private float sensitivity = 0.08f;
    [SerializeField, Range(0f, 89f)] private float pitchLimit = 85f;

    private InputAction lookAction;
    private float pitch;

    private void Awake()
    {
        lookAction = new InputAction("Look", InputActionType.Value, "<Mouse>/delta");
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable() => lookAction.Enable();
    private void OnDisable() => lookAction.Disable();

    private void OnDestroy()
    {
        lookAction?.Dispose();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        Vector2 lookInput = lookAction.ReadValue<Vector2>() * sensitivity;
        transform.Rotate(Vector3.up, lookInput.x, Space.Self);
        pitch = Mathf.Clamp(pitch - lookInput.y, -pitchLimit, pitchLimit);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}
