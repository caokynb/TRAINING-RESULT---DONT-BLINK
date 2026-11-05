using UnityEngine;

public sealed class PlayerLook : MonoBehaviour
{
    [SerializeField] private Transform cameraPivot;

    [Header("Settings")]
    [SerializeField] private GameSettings settings;

    [Header("Fallbacks (Used if Settings is not assigned)")]
    [SerializeField, Min(0f)] private float sensitivity = 0.08f;
    [SerializeField, Range(0f, 89f)] private float pitchLimit = 85f;

    public GameSettings Settings
    {
        get => settings;
        set => settings = value;
    }

    public float Sensitivity => settings != null ? settings.MouseSensitivity : sensitivity;

    private PlayerInputReader inputReader;
    private float pitch;

    private void Awake()
    {
        inputReader = GetComponent<PlayerInputReader>();

        if (settings == null)
        {
            var movement = GetComponent<PlayerMovement>();
            if (movement != null && movement.Settings != null)
                settings = movement.Settings;
        }
    }

    private void Update()
    {
        Vector2 lookInput = inputReader.LookInput * Sensitivity;
        transform.Rotate(Vector3.up, lookInput.x, Space.Self);
        pitch = Mathf.Clamp(pitch - lookInput.y, -pitchLimit, pitchLimit);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}
