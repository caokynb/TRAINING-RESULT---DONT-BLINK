using UnityEngine;

/// <summary>
/// Script tạo cơ chế Easter Egg / Lối thoát bí mật cho bức tranh ASPIRARE.
/// Khi người chơi lại gần bấm Chuột trái hoặc E (hoặc chạm vào tranh),
/// game sẽ lập tức kích hoạt chiến thắng (Win) và hiện màn hình nhập MSSV.
/// </summary>
[DisallowMultipleComponent]
public class SecretWinInteractable : MonoBehaviour, IInteractable
{
    [Header("Interaction Prompt")]
    [Tooltip("Dòng chữ hiển thị khi nhìn vào bức tranh (để trống nếu muốn bí mật hoàn toàn)")]
    [SerializeField] private string promptMessage = "Chuột trái / E: ???";

    [Header("Trigger Options")]
    [Tooltip("Cho phép thắng ngay khi người chơi đi chạm vào bức tranh (cần Collider là Trigger)")]
    [SerializeField] private bool winOnTouch = false;

    [Tooltip("Khoảng cách tương tác tối đa nếu dùng click trực tiếp (m)")]
    [SerializeField] private float maxClickDistance = 4.0f;

    private bool hasWon = false;

    // --- Triển khai IInteractable cho PlayerInteractor (ngắm tâm chuột + bấm E / Chuột trái) ---
    public string GetInteractionPrompt()
    {
        return promptMessage;
    }

    public bool CanInteract()
    {
        return !hasWon;
    }

    public void Interact()
    {
        TriggerWin();
    }

    // --- Hỗ trợ thêm click chuột trực tiếp (OnMouseDown) phòng trường hợp mở chuột ---
    private void OnMouseDown()
    {
        if (hasWon)
            return;

        Camera cam = Camera.main;
        if (cam != null)
        {
            float dist = Vector3.Distance(cam.transform.position, transform.position);
            if (dist > maxClickDistance)
                return;
        }

        TriggerWin();
    }

    // --- Hỗ trợ chạm vào tranh là win nếu bật winOnTouch ---
    private void OnTriggerEnter(Collider other)
    {
        if (!winOnTouch || hasWon || other == null)
            return;

        if (IsPlayer(other))
        {
            TriggerWin();
        }
    }

    /// <summary>
    /// Kích hoạt trạng thái thắng game và mở EndScreen
    /// </summary>
    public void TriggerWin()
    {
        if (hasWon)
            return;

        hasWon = true;
        Debug.Log("[SecretWin] Người chơi đã kích hoạt lối thoát bí mật từ bức tranh ASPIRARE! Kích hoạt WIN.");

        AudioManager.Instance?.PlayUIClick();
        GameEvents.RaiseReachedExit();
    }

    private bool IsPlayer(Collider other)
    {
        if (other == null)
            return false;

        return other.CompareTag("Player") || other.name == "Player" || other.GetComponentInParent<PlayerMovement>() != null;
    }
}
