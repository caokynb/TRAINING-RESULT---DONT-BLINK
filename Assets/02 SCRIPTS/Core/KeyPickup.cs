using UnityEngine;

[DisallowMultipleComponent]
public class KeyPickup : MonoBehaviour
{
    [Header("Hover Animation")]
    [SerializeField] private bool enableHover = true;
    [SerializeField] private float rotateSpeed = 60f;
    [SerializeField] private float bobAmplitude = 0.04f;
    [SerializeField] private float bobFrequency = 2f;

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

    // Dùng cho tương tác Raycast của Kỳ (K23 / K25)
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
