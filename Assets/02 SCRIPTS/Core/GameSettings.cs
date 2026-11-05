using UnityEngine;

[CreateAssetMenu(fileName = "GameSettings", menuName = "Backrooms/Game Settings", order = 100)]
public sealed class GameSettings : ScriptableObject
{
    [Header("Player Movement")]
    [Tooltip("Tốc độ di chuyển của người chơi (m/s).")]
    [SerializeField, Range(1f, 10f)] private float walkSpeed = 4f;

    [Tooltip("Gia tốc trọng lực (m/s^2, giá trị âm).")]
    [SerializeField, Range(-40f, -5f)] private float gravity = -20f;

    [Header("Player Look")]
    [Tooltip("Độ nhạy góc nhìn chuột.")]
    [SerializeField, Range(0.01f, 0.5f)] private float mouseSensitivity = 0.08f;

    [Header("Blink & Fatigue")]
    [Tooltip("Thời gian (giây) để thanh fatigue đầy.")]
    [SerializeField, Range(3f, 30f)] private float fatigueTime = 12f;

    [Tooltip("Tốc độ giảm fatigue mỗi giây khi giữ Space.")]
    [SerializeField, Range(0.1f, 5f)] private float manualFatigueRecoveryRate = 1f;

    [Tooltip("Thời gian hồi chiêu (giây) giữa các lần blink chủ động.")]
    [SerializeField, Range(0f, 5f)] private float manualBlinkCooldown = 1.5f;

    [Tooltip("Thời gian (giây) bị ép nhắm mắt và khóa di chuyển khi fatigue đạt tối đa.")]
    [SerializeField, Range(0.2f, 5f)] private float forcedBlinkDuration = 1f;

    public float WalkSpeed => walkSpeed;
    public float Gravity => gravity;
    public float MouseSensitivity => mouseSensitivity;
    public float FatigueTime => fatigueTime;
    public float ManualFatigueRecoveryRate => manualFatigueRecoveryRate;
    public float ManualBlinkCooldown => manualBlinkCooldown;
    public float ForcedBlinkDuration => forcedBlinkDuration;
}
