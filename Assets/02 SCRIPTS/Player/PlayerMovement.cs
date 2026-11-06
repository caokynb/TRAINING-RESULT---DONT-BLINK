using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class PlayerMovement : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameSettings settings;

    [Header("Fallbacks (Used if Settings is not assigned)")]
    [SerializeField, Min(0f)] private float walkSpeed = 4f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float footstepInterval = 0.45f;
    private float footstepTimer;

    public GameSettings Settings
    {
        get => settings;
        set => settings = value;
    }

    public float WalkSpeed => settings != null ? settings.WalkSpeed : walkSpeed;
    public float Gravity => settings != null ? settings.Gravity : gravity;

    private CharacterController characterController;
    private PlayerInputReader inputReader;
    private float verticalVelocity;
    private bool isMovementLocked;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        inputReader = GetComponent<PlayerInputReader>();

        if (settings == null)
        {
            var blink = GetComponent<BlinkSystem>();
            if (blink != null && blink.Settings != null)
                settings = blink.Settings;
        }
    }

    private void Update()
    {
        Vector2 input = isMovementLocked ? Vector2.zero : inputReader.MoveInput;
        Vector3 planarVelocity = transform.right * input.x + transform.forward * input.y;

        if (characterController.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += Gravity * Time.deltaTime;
        Vector3 velocity = planarVelocity * WalkSpeed;
        velocity.y = verticalVelocity;
        characterController.Move(velocity * Time.deltaTime);

        if (characterController.isGrounded && input.sqrMagnitude > 0.01f && !isMovementLocked)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                footstepTimer = footstepInterval;
                AudioManager.Instance?.PlayPlayerFootstep(transform.position);
            }
        }
        else
        {
            footstepTimer = 0.05f;
        }
    }

    public void SetMovementLocked(bool isLocked)
    {
        isMovementLocked = isLocked;
    }
}
