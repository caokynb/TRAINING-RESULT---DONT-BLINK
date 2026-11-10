using UnityEngine;

[CreateAssetMenu(fileName = "GameSettings", menuName = "Backrooms/Game Settings", order = 100)]
public sealed class GameSettings : ScriptableObject
{
    [Header("Player Movement")]
    [Tooltip("Tốc độ di chuyển của người chơi (m/s).")]
    [SerializeField, Range(1f, 10f)] private float walkSpeed = 4f;

    [Tooltip("Gia tốc trọng lực (m/s^2, giá trị âm).")]
    [SerializeField, Range(-40f, -5f)] private float gravity = -20f;

    [Tooltip("Hệ số tốc độ di chuyển khi đang nhắm mắt chủ động (0.1 - 1.0).")]
    [SerializeField, Range(0.1f, 1f)] private float blindSpeedMultiplier = 0.7f;

    [Header("Player Look")]
    [Tooltip("Độ nhạy góc nhìn chuột.")]
    [SerializeField, Range(0.01f, 0.5f)] private float mouseSensitivity = 0.08f;

    [Header("Blink & Fatigue")]
    [Tooltip("Thời gian (giây) để thanh fatigue đầy.")]
    [SerializeField, Range(3f, 30f)] private float fatigueTime = 9.5f;

    [Tooltip("Tốc độ giảm fatigue mỗi giây khi giữ Space.")]
    [SerializeField, Range(0.1f, 5f)] private float manualFatigueRecoveryRate = 0.45f;

    [Tooltip("Thời gian hồi chiêu (giây) tính từ khi MỞ MẮT trước khi được phép blink tiếp.")]
    [SerializeField, Range(0f, 10f)] private float manualBlinkCooldown = 2.5f;

    [Tooltip("Thời gian tối thiểu mắt phải nhắm trong một lần chớp mắt chủ động (giây). Ngăn spam nhấp nhả Space tức thì.")]
    [SerializeField, Range(0.1f, 1.5f)] private float minManualBlinkDuration = 0.4f;

    [Tooltip("Mức mỏi mắt tối thiểu (0-1) để có thể chớp mắt chủ động. Ngăn spam chớp mắt liên tục khi chưa mỏi.")]
    [SerializeField, Range(0.0f, 0.5f)] private float minFatigueToBlink = 0.2f;

    [Tooltip("Thời gian (giây) bị ép nhắm mắt và khóa di chuyển khi fatigue đạt tối đa.")]
    [SerializeField, Range(0.2f, 5f)] private float forcedBlinkDuration = 1.2f;

    public float WalkSpeed => walkSpeed;
    public float Gravity => gravity;
    public float BlindSpeedMultiplier => blindSpeedMultiplier;
    public float MouseSensitivity => mouseSensitivity;
    public float FatigueTime => fatigueTime;
    public float ManualFatigueRecoveryRate => manualFatigueRecoveryRate;
    public float ManualBlinkCooldown => manualBlinkCooldown;
    public float MinManualBlinkDuration => minManualBlinkDuration;
    public float MinFatigueToBlink => minFatigueToBlink;
    public float ForcedBlinkDuration => forcedBlinkDuration;
}
