using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CatchSequence : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("Tổng thời gian sequence bắt (mặc định 2 giây).")]
    [SerializeField, Range(1f, 5f)] private float sequenceDuration = 2.0f;

    [Tooltip("Thời gian camera xoay và zoom vào mặt quái (giây).")]
    [SerializeField, Range(0.05f, 0.5f)] private float zoomDuration = 0.16f;

    [Header("Baldi's Basics Zoom Settings")]
    [Tooltip("FOV khi zoom cận cảnh vào mặt quái (càng nhỏ càng zoom sát).")]
    [SerializeField, Range(15f, 45f)] private float zoomedFOV = 28f;

    [Tooltip("Khoảng cách quái đứng ngay trước mặt người chơi (m).")]
    [SerializeField, Range(0.8f, 1.8f)] private float standDistance = 1.15f;

    [Tooltip("Độ giật/rung vi mô kiểu Baldi's Basics jumpscare (độ).")]
    [SerializeField, Range(0f, 2f)] private float glitchJitter = 0.6f;

    private bool hasTriggered;
    private bool isSequenceActive;
    private bool hasEnsuredGameManager;
    private Transform playerTransform;
    private Camera playerCamera;
    private Transform monsterHead;
    private float originalCameraFOV = 60f;
    private Quaternion baseCameraRot;
    private bool hasBaseCameraRot;

    private void Awake()
    {
        EnsureGameManagerWaits();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        EnsureGameManagerWaits();
        ResolveReferences();
    }

    private void OnEnable()
    {
        GameEvents.OnPlayerCaught += HandlePlayerCaught;
        EnsureGameManagerWaits();
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerCaught -= HandlePlayerCaught;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureGameManagerWaits();
    }

    private void Update()
    {
        if (!hasEnsuredGameManager)
        {
            EnsureGameManagerWaits();
        }

        // Trong lúc sequence đang chạy, luôn xóa overlay đen nếu người chơi thả/bấm Space
        if (isSequenceActive)
        {
            ClearBlackScreenOverlay();
        }
    }

    private void EnsureGameManagerWaits()
    {
        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        if (gm != null)
        {
            FieldInfo field = typeof(GameManager).GetField("waitForCatchSequence", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(gm, true);
                hasEnsuredGameManager = true;
            }
        }
    }

    private void HandlePlayerCaught()
    {
        if (hasTriggered)
            return;

        hasTriggered = true;
        EnsureGameManagerWaits();
        StartCoroutine(ExecuteCatchSequence());
    }

    private IEnumerator ExecuteCatchSequence()
    {
        isSequenceActive = true;
        GameObject endPanel = GetEndScreenPanel();

        try
        {
            ResolveReferences();

            // 1. Khắc phục triệt để lỗi màn hình đen: Ép mở mắt và xóa overlay đen
            ClearBlackScreenOverlay();

            // 2. Ẩn panel kết thúc nếu vô tình bị bật sớm
            if (endPanel != null && endPanel.activeSelf)
            {
                endPanel.SetActive(false);
            }

            // 3. Dừng AI di chuyển của quái
            MonsterBrain brain = GetComponent<MonsterBrain>() ?? Object.FindFirstObjectByType<MonsterBrain>();
            if (brain != null)
            {
                brain.enabled = false;
            }

            // 4. Chuyển pose về Pose_01 và tắt PoseSwitcher để giữ nguyên tư thế đứng thẳng
            SetPose01();

            // 5. Đặt quái đứng thẳng đối diện ngay trước mặt người chơi (kiểu Baldi's Basics)
            AlignMonsterWithPlayer();

            // 6. Camera xoay và zoom sát vào mặt quái (Baldi's Basics zoom)
            Transform camTransform = playerCamera != null ? playerCamera.transform : null;
            if (camTransform != null)
            {
                yield return StartCoroutine(ZoomCameraIntoMonsterFace(camTransform));
            }

            // 7. Chờ hết thời gian sequence (tổng 2 giây)
            float remainingTime = Mathf.Max(0.1f, sequenceDuration - zoomDuration);
            yield return new WaitForSeconds(remainingTime);
        }
        finally
        {
            isSequenceActive = false;

            // Khôi phục lại FOV mặc định của camera
            if (playerCamera != null)
            {
                playerCamera.fieldOfView = originalCameraFOV;
            }

            // Bật lại panel kết thúc nếu có
            if (endPanel != null && !endPanel.activeSelf)
            {
                endPanel.SetActive(true);
            }

            // 8. Bắn sự kiện hoàn tất sau đúng 2s -> GameManager hiển thị màn hình thua
            Debug.Log("[CatchSequence] Baldi jumpscare complete -> Raising OnCatchSequenceDone");
            GameEvents.RaiseCatchSequenceDone();
        }
    }

    private void LateUpdate()
    {
        if (!isSequenceActive || playerCamera == null || !hasBaseCameraRot)
            return;

        // Giật nhẹ vi mô kiểu jumpscare Baldi's Basics trong lúc camera đang zoom sát mặt quái
        if (glitchJitter > 0.001f)
        {
            float jitterX = Random.Range(-glitchJitter, glitchJitter);
            float jitterY = Random.Range(-glitchJitter, glitchJitter);
            playerCamera.transform.rotation = baseCameraRot * Quaternion.Euler(jitterX, jitterY, 0f);
        }
    }

    private void SetPose01()
    {
        // 1. Tắt PoseSwitcher để nó không tự động đổi pose hay xoay đầu
        PoseSwitcher switcher = GetComponent<PoseSwitcher>() ?? GetComponentInChildren<PoseSwitcher>();
        if (switcher != null)
        {
            switcher.enabled = false;

            if (switcher.Poses != null)
            {
                for (int i = 0; i < switcher.Poses.Length; i++)
                {
                    if (switcher.Poses[i] != null)
                    {
                        bool isPose01 = switcher.Poses[i].name.Equals("Pose_01", System.StringComparison.OrdinalIgnoreCase);
                        switcher.Poses[i].SetActive(isPose01);
                    }
                }
            }
        }

        // 2. Quét trực tiếp các child dưới MonsterVisual để đảm bảo chỉ có Pose_01 active
        Transform visual = transform.Find("MonsterVisual") ?? transform;
        foreach (Transform child in visual)
        {
            if (child.name.StartsWith("Pose", System.StringComparison.OrdinalIgnoreCase))
            {
                bool isPose01 = child.name.Equals("Pose_01", System.StringComparison.OrdinalIgnoreCase);
                child.gameObject.SetActive(isPose01);
            }
        }
    }

    private void AlignMonsterWithPlayer()
    {
        if (playerTransform == null)
            return;

        Vector3 playerPos = playerTransform.position;
        Vector3 playerForward = playerTransform.forward;
        playerForward.y = 0f;
        if (playerForward.sqrMagnitude < 0.001f)
            playerForward = Vector3.forward;
        playerForward.Normalize();

        // Đặt người chơi đứng thẳng nhìn về phía trước
        playerTransform.rotation = Quaternion.LookRotation(playerForward, Vector3.up);

        // Đặt quái vật đứng ngay trước mặt người chơi ở cự ly standDistance
        Vector3 monsterPos = playerPos + playerForward * standDistance;

        // Bám sàn theo Raycast
        if (Physics.Raycast(monsterPos + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 5f))
        {
            monsterPos.y = hit.point.y;
        }
        else
        {
            monsterPos.y = playerPos.y;
        }

        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null && cc.enabled)
        {
            cc.enabled = false;
            transform.position = monsterPos;
            cc.enabled = true;
        }
        else
        {
            transform.position = monsterPos;
        }

        // Quái đứng thẳng mặt đối mặt trực tiếp với người chơi
        // Do mô hình quái quay mặt về hướng -Z, cần offset Quaternion
        Vector3 toPlayer = (playerPos - monsterPos);
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude > 0.001f)
        {
            Quaternion lookRot = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
            Quaternion modelOffset = Quaternion.FromToRotation(new Vector3(0f, 0f, -1f), Vector3.forward);
            transform.rotation = lookRot * modelOffset;
        }
    }

    private IEnumerator ZoomCameraIntoMonsterFace(Transform camTransform)
    {
        if (camTransform == null || playerCamera == null)
            yield break;

        originalCameraFOV = playerCamera.fieldOfView;
        Quaternion startCamRot = camTransform.rotation;
        Vector3 targetFacePos = GetMonsterFaceWorldPos();

        float elapsed = 0f;

        // Zoom FOV và xoay nhanh vào tâm mặt quái
        while (elapsed < zoomDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / zoomDuration);

            playerCamera.fieldOfView = Mathf.Lerp(originalCameraFOV, zoomedFOV, t);

            targetFacePos = GetMonsterFaceWorldPos();
            Vector3 toFace = (targetFacePos - camTransform.position).normalized;
            if (toFace.sqrMagnitude > 0.001f)
            {
                camTransform.rotation = Quaternion.Slerp(startCamRot, Quaternion.LookRotation(toFace, Vector3.up), t);
            }
            yield return null;
        }

        // Giữ camera ở mức FOV zoom cận cảnh
        playerCamera.fieldOfView = zoomedFOV;

        targetFacePos = GetMonsterFaceWorldPos();
        Vector3 finalToFace = (targetFacePos - camTransform.position).normalized;
        if (finalToFace.sqrMagnitude > 0.001f)
        {
            baseCameraRot = Quaternion.LookRotation(finalToFace, Vector3.up);
            camTransform.rotation = baseCameraRot;
            hasBaseCameraRot = true;
        }
    }

    private Vector3 GetMonsterFaceWorldPos()
    {
        if (monsterHead != null)
            return monsterHead.position;

        return transform.position + Vector3.up * 1.55f;
    }

    private void ClearBlackScreenOverlay()
    {
        BlinkEvents.RaiseOpened();

        BlinkHudView hudView = Object.FindFirstObjectByType<BlinkHudView>();
        if (hudView != null)
        {
            FieldInfo targetAlphaField = typeof(BlinkHudView).GetField("targetOverlayAlpha", BindingFlags.NonPublic | BindingFlags.Instance);
            if (targetAlphaField != null) targetAlphaField.SetValue(hudView, 0f);

            FieldInfo alphaField = typeof(BlinkHudView).GetField("overlayAlpha", BindingFlags.NonPublic | BindingFlags.Instance);
            if (alphaField != null) alphaField.SetValue(hudView, 0f);

            FieldInfo overlayField = typeof(BlinkHudView).GetField("closedEyesOverlay", BindingFlags.NonPublic | BindingFlags.Instance);
            if (overlayField != null)
            {
                Image img = overlayField.GetValue(hudView) as Image;
                if (img != null)
                {
                    Color c = img.color;
                    c.a = 0f;
                    img.color = c;
                }
            }
        }

        BlinkSystem blinkSystem = Object.FindFirstObjectByType<BlinkSystem>();
        if (blinkSystem != null)
        {
            FieldInfo eventField = typeof(BlinkSystem).GetField("EyesClosedChanged", BindingFlags.NonPublic | BindingFlags.Instance);
            if (eventField != null)
            {
                System.Action<bool> del = eventField.GetValue(blinkSystem) as System.Action<bool>;
                del?.Invoke(false);
            }
        }
    }

    private void ResolveReferences()
    {
        if (playerTransform == null)
        {
            PlayerMovement pm = Object.FindFirstObjectByType<PlayerMovement>();
            if (pm != null)
                playerTransform = pm.transform;
            else
            {
                GameObject p = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
                if (p != null) playerTransform = p.transform;
            }
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
                playerCamera = Object.FindFirstObjectByType<Camera>();

            if (playerCamera != null)
                originalCameraFOV = playerCamera.fieldOfView;
        }

        if (monsterHead == null)
        {
            monsterHead = FindMonsterHead();
        }
    }

    private Transform FindMonsterHead()
    {
        Transform[] all = GetComponentsInChildren<Transform>(true);
        foreach (Transform t in all)
        {
            if (t.gameObject.activeInHierarchy && t.name == "Head")
                return t;
        }

        foreach (Transform t in all)
        {
            if (t.gameObject.activeInHierarchy && t.name.IndexOf("Head", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return t;
        }

        return null;
    }

    private GameObject GetEndScreenPanel()
    {
        EndScreenView endScreenView = Object.FindFirstObjectByType<EndScreenView>();
        if (endScreenView != null)
        {
            FieldInfo panelField = typeof(EndScreenView).GetField("resultPanel", BindingFlags.NonPublic | BindingFlags.Instance);
            if (panelField != null)
            {
                return panelField.GetValue(endScreenView) as GameObject;
            }
        }
        return null;
    }
}
