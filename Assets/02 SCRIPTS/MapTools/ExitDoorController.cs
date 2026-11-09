using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class ExitDoorController : MonoBehaviour, IInteractable
{
    [Header("Door Leaf Reference")]
    [Tooltip("Cánh cửa (Plane.001) đã được đặt pivot tại bản lề")]
    [SerializeField] private Transform doorLeaf;

    [Header("Rotation Settings")]
    [Tooltip("Góc xoay khi mở cửa quanh trục Z (mặc định -130 độ)")]
    [SerializeField] private float openAngleZ = -130f;
    [Tooltip("Thời gian mở cửa (giây)")]
    [SerializeField] private float openDuration = 1.0f;
    [SerializeField] private AnimationCurve openCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("State")]
    [SerializeField] private bool isOpen;
    [SerializeField] private bool requiresKey = true;

    [Header("Interaction Prompts")]
    [Tooltip("Dòng chữ khi cửa đang bị khóa")]
    [SerializeField] private string lockedPrompt = "Cửa bị khóa (Cần chìa khóa)";
    [Tooltip("Dòng chữ khi đã có chìa khóa để mở cửa")]
    [SerializeField] private string openPrompt = "Chuột trái / E: Mở cửa";

    private Quaternion closedLocalRotation;
    private Quaternion openLocalRotation;
    private Coroutine rotateCoroutine;
    private bool hasKey;

    public bool IsOpen => isOpen;
    public bool RequiresKey { get => requiresKey; set => requiresKey = value; }
    public bool HasKey => hasKey;

    private void Awake()
    {
        ResolveDoorLeaf();
        CacheRotations();

        if (KeyInventory.Instance != null && KeyInventory.Instance.HasKey)
        {
            hasKey = true;
        }
    }

    private void OnEnable()
    {
        GameEvents.OnKeyPickedUp += HandleKeyPickedUp;
    }

    private void OnDisable()
    {
        GameEvents.OnKeyPickedUp -= HandleKeyPickedUp;
    }

    private void HandleKeyPickedUp()
    {
        hasKey = true;
    }

    public void ResolveDoorLeaf()
    {
        if (doorLeaf != null)
            return;

        // Tự động tìm con tên Plane.001 hoặc Door_Component_013/Plane.001
        Transform found = transform.Find("Door_Component_013/Plane.001");
        if (found == null)
            found = transform.Find("Plane.001");

        if (found == null)
        {
            PlaneMarker pm = GetComponentInChildren<PlaneMarker>();
            if (pm != null)
                found = pm.transform;
        }

        if (found == null)
        {
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Plane.001")
                {
                    found = t;
                    break;
                }
            }
        }

        if (found == null)
        {
            Transform searchRoot = transform.root;
            if (searchRoot != null)
            {
                foreach (Transform t in searchRoot.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "Plane.001")
                    {
                        found = t;
                        break;
                    }
                }
            }
        }

        doorLeaf = found;
    }

    public void CacheRotations()
    {
        if (doorLeaf != null)
        {
            closedLocalRotation = Quaternion.Euler(0f, 0f, 0f);
            openLocalRotation = Quaternion.Euler(0f, 0f, openAngleZ);
        }
    }

    // Triển khai IInteractable
    public string GetInteractionPrompt()
    {
        if (isOpen)
            return string.Empty;

        bool playerHoldsKey = hasKey || (KeyInventory.Instance != null && KeyInventory.Instance.HasKey);
        if (requiresKey && !playerHoldsKey)
        {
            return !string.IsNullOrEmpty(lockedPrompt) ? lockedPrompt : "Door is locked (Key required)";
        }

        return !string.IsNullOrEmpty(openPrompt) ? openPrompt : "LMB / E: Open Door";
    }

    public bool CanInteract()
    {
        return !isOpen;
    }

    public void Interact()
    {
        TryInteractDoor();
    }

    public bool TryInteractDoor()
    {
        if (isOpen)
            return true;

        bool playerHoldsKey = hasKey || (KeyInventory.Instance != null && KeyInventory.Instance.HasKey);
        if (requiresKey && !playerHoldsKey)
        {
            Debug.Log("[ExitDoorController] Cửa bị khóa! Cần tìm chìa khóa trước.");
            GameEvents.RaiseDoorLocked();
            return false;
        }

        OpenDoor();
        return true;
    }

    [ContextMenu("Test Open (-130 deg Z)")]
    public void OpenDoor()
    {
        if (isOpen)
            return;

        ResolveDoorLeaf();
        CacheRotations();

        if (doorLeaf == null)
        {
            Debug.LogError("[ExitDoorController] Chưa tìm thấy doorLeaf (Plane.001)!");
            return;
        }

        isOpen = true;

        if (Application.isPlaying)
        {
            if (rotateCoroutine != null)
                StopCoroutine(rotateCoroutine);
            rotateCoroutine = StartCoroutine(RotateDoorRoutine(openLocalRotation, openDuration));

            AudioManager.Instance?.PlayExitDoor(transform.position);
            GameEvents.RaiseDoorOpened();
        }
        else
        {
            doorLeaf.localRotation = openLocalRotation;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(doorLeaf);
#endif
        }
    }

    [ContextMenu("Test Close (0 deg Z)")]
    public void CloseDoor()
    {
        ResolveDoorLeaf();
        CacheRotations();

        if (doorLeaf == null)
            return;

        isOpen = false;

        if (Application.isPlaying)
        {
            if (rotateCoroutine != null)
                StopCoroutine(rotateCoroutine);
            rotateCoroutine = StartCoroutine(RotateDoorRoutine(closedLocalRotation, openDuration * 0.7f));
        }
        else
        {
            doorLeaf.localRotation = closedLocalRotation;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(doorLeaf);
#endif
        }
    }

    private IEnumerator RotateDoorRoutine(Quaternion targetRotation, float duration)
    {
        Quaternion startRotation = doorLeaf.localRotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curveT = openCurve != null ? openCurve.Evaluate(t) : t;
            doorLeaf.localRotation = Quaternion.Slerp(startRotation, targetRotation, curveT);
            yield return null;
        }

        doorLeaf.localRotation = targetRotation;
        rotateCoroutine = null;
    }

    // Marker class để tìm kiếm nhanh
    private class PlaneMarker : MonoBehaviour { }
}
