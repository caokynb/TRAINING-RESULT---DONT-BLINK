using UnityEngine;

[DisallowMultipleComponent]
public class KeyPickup : MonoBehaviour, IInteractable
{
    [Header("Hover Animation")]
    [SerializeField] private bool enableHover = true;
    [SerializeField] private float rotateSpeed = 60f;
    [SerializeField] private float bobAmplitude = 0.04f;
    [SerializeField] private float bobFrequency = 2f;

    [Header("Interaction Prompt")]
    [Tooltip("Dòng chữ hiển thị khi nhìn vào chìa khóa")]
    [SerializeField] private string promptMessage = "Chuột trái / E: Nhặt chìa khóa";

    private Vector3 initialPosition;
    private bool isCollected;

    private void Start()
    {
        initialPosition = transform.position;
    }

    private void Update()
    {
        if (!enableHover || isCollected)
            return;

        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
        float newY = initialPosition.y + Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected)
            return;

        if (IsPlayer(other))
        {
            CollectKey();
        }
    }

    // Triển khai IInteractable cho Raycast
    public string GetInteractionPrompt()
    {
        return !string.IsNullOrEmpty(promptMessage) ? promptMessage : "LMB / E: Pick up Key";
    }

    public bool CanInteract()
    {
        return !isCollected;
    }

    public void Interact()
    {
        if (isCollected)
            return;

        CollectKey();
    }

    public void CollectKey()
    {
        if (isCollected)
            return;

        isCollected = true;
        Debug.Log("[KeyPickup] Người chơi đã nhặt chìa khóa!");

        if (KeyInventory.Instance != null)
        {
            KeyInventory.Instance.SetHasKey(true);
        }

        GameEvents.RaiseKeyPickedUp();
        gameObject.SetActive(false);
    }

    private bool IsPlayer(Collider other)
    {
        if (other == null)
            return false;

        if (other.CompareTag("Player"))
            return true;

        if (other.GetComponentInParent<PlayerMovement>() != null)
            return true;

        return other.name == "Player";
    }
}
