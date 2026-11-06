using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class ExitDetector : MonoBehaviour
{
    [SerializeField] private string exitTag = "Exit";

    private bool hasReachedExit;

    private void OnTriggerEnter(Collider other)
    {
        TryTriggerExit(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryTriggerExit(other);
    }

    private void TryTriggerExit(Collider other)
    {
        if (hasReachedExit || other == null)
            return;

        if (!other.CompareTag(exitTag) && other.name != "Exit")
            return;

        // K25: Chỉ tính thắng khi cửa đã mở
        ExitDoorController doorCtrl = Object.FindFirstObjectByType<ExitDoorController>();
        if (doorCtrl != null && !doorCtrl.IsOpen)
        {
            return;
        }

        hasReachedExit = true;
        Debug.Log("[ExitDetector] Người chơi đã thoát thành công qua cửa Exit!");
        GameEvents.RaiseReachedExit();
    }
}
