using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class PlayerMovement : MonoBehaviour
{
    [SerializeField, Min(0f)] private float walkSpeed = 4f;
    [SerializeField, Min(0f)] private float gravity = -20f;

    private CharacterController characterController;
    private PlayerInputReader inputReader;
    private float verticalVelocity;
    private bool isMovementLocked;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        inputReader = GetComponent<PlayerInputReader>();
    }

    private void Update()
    {
        Vector2 input = isMovementLocked ? Vector2.zero : inputReader.MoveInput;
        Vector3 planarVelocity = transform.right * input.x + transform.forward * input.y;

        if (characterController.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;
        Vector3 velocity = planarVelocity * walkSpeed;
        velocity.y = verticalVelocity;
        characterController.Move(velocity * Time.deltaTime);
    }

    public void SetMovementLocked(bool isLocked)
    {
        isMovementLocked = isLocked;
    }
}
