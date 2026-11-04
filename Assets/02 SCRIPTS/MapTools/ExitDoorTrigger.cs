using UnityEngine;

public class ExitDoorTrigger : MonoBehaviour
{
    [SerializeField] private float raycastDistance = 3f;
    [SerializeField] private Transform doorTransform;
    [SerializeField] private BoxCollider exitTriggerCollider;

    private Transform playerTransform;
    private Camera playerCamera;
    private bool isTriggerUnlocked;
    private bool hasOpenedDoor;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureExitDoorSetup()
    {
        ExitDoorTrigger existing = Object.FindFirstObjectByType<ExitDoorTrigger>();
        if (existing != null)
            return;

        GameObject doorObj = FindDoorExitObject();
        GameObject exitZoneObj = FindExitTriggerObject(doorObj);

        if (doorObj == null && exitZoneObj == null)
            return;

        GameObject hostObj = exitZoneObj != null ? exitZoneObj : doorObj;
        ExitDoorTrigger trigger = hostObj.AddComponent<ExitDoorTrigger>();
        trigger.SetupReferences(doorObj != null ? doorObj.transform : null, exitZoneObj);
    }

    public void SetupReferences(Transform door, GameObject exitZoneObj)
    {
        if (door != null)
            doorTransform = door;

        if (exitZoneObj != null)
        {
            exitTriggerCollider = exitZoneObj.GetComponent<BoxCollider>();
            if (exitTriggerCollider == null)
            {
                exitTriggerCollider = exitZoneObj.AddComponent<BoxCollider>();
                exitTriggerCollider.size = new Vector3(2f, 3f, 2f);
                exitTriggerCollider.center = new Vector3(0f, 1.5f, 0f);
            }
            exitTriggerCollider.isTrigger = true;
        }
    }

    private void Awake()
    {
        if (doorTransform == null)
        {
            GameObject doorObj = FindDoorExitObject();
            if (doorObj != null)
                doorTransform = doorObj.transform;
        }

        if (exitTriggerCollider == null)
        {
            exitTriggerCollider = GetComponent<BoxCollider>();
            if (exitTriggerCollider == null)
            {
                GameObject exitZoneObj = FindExitTriggerObject(doorTransform != null ? doorTransform.gameObject : null);
                if (exitZoneObj != null)
                {
                    exitTriggerCollider = exitZoneObj.GetComponent<BoxCollider>();
                    if (exitTriggerCollider == null)
                    {
                        exitTriggerCollider = exitZoneObj.AddComponent<BoxCollider>();
                        exitTriggerCollider.size = new Vector3(2f, 3f, 2f);
                        exitTriggerCollider.center = new Vector3(0f, 1.5f, 0f);
                    }
                }
            }
        }

        if (exitTriggerCollider != null)
        {
            exitTriggerCollider.isTrigger = true;
            exitTriggerCollider.enabled = false;
        }

        ResolvePlayer();
    }

    private void Update()
    {
        if (hasOpenedDoor)
            return;

        ResolvePlayer();

        if (!isTriggerUnlocked && CheckPlayerRaycastHitDoor())
        {
            UnlockExitTrigger();
        }

        if (isTriggerUnlocked && IsPlayerInsideTriggerZone())
        {
            OpenDoorAndEndGame();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasOpenedDoor || !isTriggerUnlocked)
            return;

        if (IsPlayerCollider(other))
        {
            OpenDoorAndEndGame();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (hasOpenedDoor || !isTriggerUnlocked)
            return;

        if (IsPlayerCollider(other))
        {
            OpenDoorAndEndGame();
        }
    }

    private bool CheckPlayerRaycastHitDoor()
    {
        if (playerTransform == null && playerCamera == null)
            return false;

        Vector3 origin = playerCamera != null
            ? playerCamera.transform.position
            : playerTransform.position + Vector3.up * 1.6f;

        Vector3 direction = playerCamera != null
            ? playerCamera.transform.forward
            : playerTransform.forward;

        if (CastDoorRay(origin, direction, raycastDistance))
            return true;

        if (playerTransform != null)
        {
            Vector3 chestOrigin = playerTransform.position + Vector3.up * 1.1f;
            Vector3 flatForward = playerTransform.forward;
            flatForward.y = 0f;
            if (flatForward.sqrMagnitude > 0.001f && CastDoorRay(chestOrigin, flatForward.normalized, raycastDistance))
                return true;
        }

        return false;
    }

    private bool CastDoorRay(Vector3 origin, Vector3 direction, float maxDist)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, maxDist, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            Transform hitTransform = hits[i].transform;
            if (hitTransform == null)
                continue;

            if (playerTransform != null && (hitTransform == playerTransform || hitTransform.IsChildOf(playerTransform)))
                continue;

            if (IsDoorTransform(hitTransform))
                return true;
        }

        return false;
    }

    private bool IsDoorTransform(Transform t)
    {
        if (t == null)
            return false;

        if (doorTransform != null && (t == doorTransform || t.IsChildOf(doorTransform)))
            return true;

        Transform current = t;
        while (current != null)
        {
            if (current.name.StartsWith("DoorExit") || current.name.StartsWith("DoorFrame_Exit"))
                return true;

            current = current.parent;
        }

        return false;
    }

    private void UnlockExitTrigger()
    {
        isTriggerUnlocked = true;
        if (exitTriggerCollider != null)
        {
            exitTriggerCollider.enabled = true;
        }
    }

    private bool IsPlayerInsideTriggerZone()
    {
        if (playerTransform == null)
            return false;

        if (exitTriggerCollider != null && exitTriggerCollider.enabled)
        {
            Bounds triggerBounds = exitTriggerCollider.bounds;
            CharacterController cc = playerTransform.GetComponent<CharacterController>();
            if (cc != null)
                return triggerBounds.Intersects(cc.bounds);

            Vector3 playerCheckPos = playerTransform.position + Vector3.up * 1f;
            return triggerBounds.Contains(playerCheckPos);
        }

        if (doorTransform != null)
        {
            Vector2 playerXZ = new Vector2(playerTransform.position.x, playerTransform.position.z);
            Vector2 doorXZ = new Vector2(doorTransform.position.x, doorTransform.position.z);
            return Vector2.Distance(playerXZ, doorXZ) <= raycastDistance;
        }

        return false;
    }

    private bool IsPlayerCollider(Collider other)
    {
        if (other == null)
            return false;

        if (playerTransform != null && (other.transform == playerTransform || other.transform.IsChildOf(playerTransform)))
            return true;

        if (other.GetComponentInParent<PlayerMovement>() != null)
            return true;

        try
        {
            if (other.CompareTag("Player"))
                return true;
        }
        catch
        {
        }

        return other.name == "Player";
    }

    private void OpenDoorAndEndGame()
    {
        if (hasOpenedDoor)
            return;

        hasOpenedDoor = true;
        Debug.Log("mở cửa");
        GameEvents.RaiseReachedExit();
    }

    private void ResolvePlayer()
    {
        if (playerTransform == null)
        {
            PlayerMovement pm = Object.FindFirstObjectByType<PlayerMovement>();
            if (pm != null)
            {
                playerTransform = pm.transform;
            }
            else
            {
                GameObject named = GameObject.Find("Player");
                if (named != null)
                    playerTransform = named.transform;
            }
        }

        if (playerCamera == null)
        {
            if (playerTransform != null)
                playerCamera = playerTransform.GetComponentInChildren<Camera>();

            if (playerCamera == null)
                playerCamera = Camera.main;
        }
    }

    private static GameObject FindDoorExitObject()
    {
        GameObject exact = GameObject.Find("DoorExit_28_23");
        if (exact != null)
            return exact;

        Transform[] all = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t != null && (t.name.StartsWith("DoorExit_") || t.name.StartsWith("DoorFrame_Exit")))
                return t.gameObject;
        }

        return null;
    }

    private static GameObject FindExitTriggerObject(GameObject doorObj)
    {
        Transform[] all = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t == null)
                continue;

            if (doorObj != null && (t.gameObject == doorObj || t.IsChildOf(doorObj.transform)))
                continue;

            if (t.name == "Exit")
                return t.gameObject;

            try
            {
                if (t.CompareTag("Exit") && t.GetComponent<BoxCollider>() != null)
                    return t.gameObject;
            }
            catch
            {
            }
        }

        return null;
    }
}
