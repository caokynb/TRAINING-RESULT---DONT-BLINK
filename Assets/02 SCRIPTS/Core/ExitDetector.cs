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

        // K25: Chỉ tính thắng khi cửa đã mở
        ExitDoorController doorCtrl = Object.FindFirstObjectByType<ExitDoorController>();
        if (doorCtrl != null && !doorCtrl.IsOpen)
        {
            Debug.Log("[ExitDetector] Chưa thể thoát: Cửa chưa được mở!");
            return;
        }

        hasReachedExit = true;
        GameEvents.RaiseReachedExit();
    }
}
