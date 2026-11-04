using UnityEngine;
using UnityEngine.InputSystem;

public class MonsterBrain : MonoBehaviour
{
    [SerializeField] private float simulatedBlinkDuration = 0.25f;

    private void OnEnable()
    {
        BlinkEvents.OnEyesClosed += HandleEyesClosed;
        BlinkEvents.OnEyesOpened += HandleEyesOpened;
    }

    private void OnDisable()
    {
        BlinkEvents.OnEyesClosed -= HandleEyesClosed;
        BlinkEvents.OnEyesOpened -= HandleEyesOpened;
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            BlinkEvents.RaiseClosed(simulatedBlinkDuration);
        }
        else if (Keyboard.current.tKey.wasReleasedThisFrame)
        {
            BlinkEvents.RaiseOpened();
        }
    }

    private void HandleEyesClosed(float duration)
    {
        Debug.Log($"[MonsterBrain] Eyes Closed (duration: {duration:F2}s)");
    }

    private void HandleEyesOpened()
    {
        Debug.Log("[MonsterBrain] Eyes Opened");
    }
}
