using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class ExitDetector : MonoBehaviour
{
    [SerializeField] private string exitTag = "Exit";

    private bool hasReachedExit;

    private void OnTriggerEnter(Collider other)
    {
        if (hasReachedExit || !other.CompareTag(exitTag))
            return;

        hasReachedExit = true;
        GameEvents.RaiseReachedExit();
    }
}
