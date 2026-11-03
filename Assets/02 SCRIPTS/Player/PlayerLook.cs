using UnityEngine;

public sealed class PlayerLook : MonoBehaviour
{
    [SerializeField] private Transform cameraPivot;
    [SerializeField, Min(0f)] private float sensitivity = 0.08f;
    [SerializeField, Range(0f, 89f)] private float pitchLimit = 85f;

    private PlayerInputReader inputReader;
    private float pitch;

    private void Awake()
    {
        inputReader = GetComponent<PlayerInputReader>();
    }

    private void Update()
    {
        Vector2 lookInput = inputReader.LookInput * sensitivity;
        transform.Rotate(Vector3.up, lookInput.x, Space.Self);
        pitch = Mathf.Clamp(pitch - lookInput.y, -pitchLimit, pitchLimit);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}
