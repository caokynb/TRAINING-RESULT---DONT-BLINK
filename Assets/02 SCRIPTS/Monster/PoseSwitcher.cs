using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class PoseSwitcher : MonoBehaviour
{
    [Header("Poses")]
    [Tooltip("List of all monster pose GameObjects (under MonsterVisual)")]
    [SerializeField] private GameObject[] poses;

    [Tooltip("Optional reference to the MonsterVisual root transform")]
    [SerializeField] private Transform visualRoot;

    [SerializeField] private int currentPoseIndex = -1;

    [Header("Model Facing & Scale")]
    [Tooltip("The local direction of the monster front face (default (0, 0, -1) for -Z)")]
    [SerializeField] private Vector3 modelFaceDirection = new Vector3(0f, 0f, -1f);

    [Tooltip("Target scale for monster poses (default 0.18 for ~2m height)")]
    [SerializeField, Range(0.1f, 0.4f)] private float poseScale = 0.18f;

    [Tooltip("Local position offset for MonsterVisual to align feet on ground")]
    [SerializeField] private Vector3 visualOffset = Vector3.zero;

    [Header("Blink Switching")]
    [Tooltip("Switch pose as soon as eyes close (switches in the dark)")]
    [SerializeField] private bool switchOnEyesClosed = true;

    [Tooltip("Switch pose when eyes open")]
    [SerializeField] private bool switchOnEyesOpened = false;

    [Header("Head LookAt (IK / Bone Aim)")]
    [Tooltip("Make monster head bone aim at Player camera in any pose")]
    [SerializeField] private bool enableHeadLookAt = true;

    [SerializeField] private string headBoneName = "Head";
    [SerializeField, Range(30f, 90f)] private float maxLookAngle = 65f;
    [SerializeField, Min(0f)] private float lookSpeed = 12f;
    [SerializeField] private float playerEyeOffset = 1.6f;

    [SerializeField] private Transform activeHeadBone;

    // Pristine rest rotations cached once per pose index at initialization
    private readonly Dictionary<int, Quaternion> pristineLocalRotations = new Dictionary<int, Quaternion>();
    private Quaternion baseHeadLocalRotation;
    private Quaternion currentHeadRotation;
    private bool hasBaseRotation;

    private Transform playerTransform;
    private Transform playerCameraTransform;
    private bool hasSwitchedThisBlink;

    public int CurrentPoseIndex => currentPoseIndex;
    public GameObject CurrentPose => (poses != null && currentPoseIndex >= 0 && currentPoseIndex < poses.Length) ? poses[currentPoseIndex] : null;
    public GameObject[] Poses { get => poses; set => poses = value; }
    public Transform VisualRoot { get => visualRoot; set => visualRoot = value; }
    public float PoseScale { get => poseScale; set => poseScale = value; }
    public Vector3 VisualOffset { get => visualOffset; set => visualOffset = value; }
    public Vector3 ModelFaceDirection { get => modelFaceDirection; set => modelFaceDirection = value; }

    private void Awake()
    {
        ResolvePoses();
        ResolvePlayer();
        CachePristineRotations();

        if (poses != null && poses.Length > 0)
        {
            int initialIndex = (currentPoseIndex >= 0 && currentPoseIndex < poses.Length) ? currentPoseIndex : Random.Range(0, poses.Length);
            SetPose(initialIndex);
        }
    }

    private void Start()
    {
        ResolvePlayer();
    }

    private void OnEnable()
    {
        BlinkEvents.OnEyesClosed += HandleEyesClosed;
        BlinkEvents.OnEyesOpened += HandleEyesOpened;

        if (pristineLocalRotations.Count == 0)
        {
            CachePristineRotations();
        }
    }

    private void OnDisable()
    {
        BlinkEvents.OnEyesClosed -= HandleEyesClosed;
        BlinkEvents.OnEyesOpened -= HandleEyesOpened;
    }

    private void OnValidate()
    {
        if (poses != null && poses.Length > 0 && currentPoseIndex >= 0 && currentPoseIndex < poses.Length)
        {
            if (pristineLocalRotations.Count == 0)
            {
                CachePristineRotations();
            }
            SetPose(currentPoseIndex);
        }
    }

    private void CachePristineRotations()
    {
        pristineLocalRotations.Clear();
        if (poses == null)
            return;

        for (int i = 0; i < poses.Length; i++)
        {
            if (poses[i] != null)
            {
                Transform head = FindHeadBone(poses[i]);
                if (head != null)
                {
                    pristineLocalRotations[i] = head.localRotation;
                }
            }
        }
    }

    private void HandleEyesClosed(float duration)
    {
        hasSwitchedThisBlink = false;

        if (switchOnEyesClosed)
        {
            SwitchToRandomPose();
            hasSwitchedThisBlink = true;
        }
    }

    private void HandleEyesOpened()
    {
        if (switchOnEyesOpened || !hasSwitchedThisBlink)
        {
            SwitchToRandomPose();
        }

        hasSwitchedThisBlink = false;
    }

    [ContextMenu("Next Pose")]
    public void NextPose()
    {
        if (poses == null || poses.Length == 0) return;
        int next = (currentPoseIndex + 1) % poses.Length;
        SetPose(next);
    }

    [ContextMenu("Previous Pose")]
    public void PreviousPose()
    {
        if (poses == null || poses.Length == 0) return;
        int prev = (currentPoseIndex - 1 + poses.Length) % poses.Length;
        SetPose(prev);
    }

    [ContextMenu("Random Pose")]
    public void SwitchToRandomPose()
    {
        if (poses == null || poses.Length <= 1)
            return;

        int nextIndex;
        int attempts = 0;
        do
        {
            nextIndex = Random.Range(0, poses.Length);
            attempts++;
        } while (nextIndex == currentPoseIndex && attempts < 20);

        if (nextIndex == currentPoseIndex)
        {
            nextIndex = (currentPoseIndex + 1) % poses.Length;
        }

        SetPose(nextIndex);
    }

    public void SetPose(int index)
    {
        if (poses == null || poses.Length == 0)
            return;

        index = Mathf.Clamp(index, 0, poses.Length - 1);

        for (int i = 0; i < poses.Length; i++)
        {
            if (poses[i] != null)
            {
                bool shouldBeActive = (i == index);
                if (poses[i].activeSelf != shouldBeActive)
                {
                    poses[i].SetActive(shouldBeActive);
                }

                if (shouldBeActive)
                {
                    if (poseScale > 0.001f)
                    {
                        poses[i].transform.localScale = new Vector3(poseScale, poseScale, poseScale);
                    }
                    poses[i].transform.localPosition = visualOffset;
                }
            }
        }

        currentPoseIndex = index;
        activeHeadBone = FindHeadBone(poses[index]);

        if (activeHeadBone != null)
        {
            // Restore pristine rest rotation from initial cache so old poses never accumulate mutations
            if (pristineLocalRotations.TryGetValue(index, out Quaternion pristineRot))
            {
                activeHeadBone.localRotation = pristineRot;
                baseHeadLocalRotation = pristineRot;
            }
            else
            {
                baseHeadLocalRotation = activeHeadBone.localRotation;
                pristineLocalRotations[index] = baseHeadLocalRotation;
            }

            hasBaseRotation = true;

            // Immediately snap the new pose's head to the correct look target so there is NO lag/twisting when opening eyes
            Quaternion instantTarget = ComputeTargetHeadRotation();
            currentHeadRotation = instantTarget;
            activeHeadBone.rotation = instantTarget;
        }
        else
        {
            hasBaseRotation = false;
        }
    }

    private Transform FindHeadBone(GameObject poseObj)
    {
        if (poseObj == null)
            return null;

        Transform[] allTransforms = poseObj.GetComponentsInChildren<Transform>(true);

        // 1. Look for a transform named headBoneName under "Armature" without renderer
        foreach (Transform t in allTransforms)
        {
            if (t.name == headBoneName && IsDescendantOf(t, "Armature"))
            {
                if (t.GetComponent<SkinnedMeshRenderer>() == null && t.GetComponent<MeshRenderer>() == null)
                {
                    return t;
                }
            }
        }

        // 2. Any transform named headBoneName that does not have a renderer
        foreach (Transform t in allTransforms)
        {
            if (t.name == headBoneName)
            {
                if (t.GetComponent<SkinnedMeshRenderer>() == null && t.GetComponent<MeshRenderer>() == null)
                {
                    return t;
                }
            }
        }

        // 3. Fallback: child of Neck
        foreach (Transform t in allTransforms)
        {
            if (t.parent != null && t.parent.name == "Neck")
            {
                return t;
            }
        }

        return null;
    }

    private static bool IsDescendantOf(Transform t, string ancestorName)
    {
        Transform current = t.parent;
        while (current != null)
        {
            if (current.name == ancestorName)
                return true;
            current = current.parent;
        }
        return false;
    }

    private void LateUpdate()
    {
        if (!enableHeadLookAt || activeHeadBone == null || !hasBaseRotation)
            return;

        Quaternion desiredHeadRotation = ComputeTargetHeadRotation();

        if (Application.isPlaying && lookSpeed > 0f)
        {
            currentHeadRotation = Quaternion.Slerp(currentHeadRotation, desiredHeadRotation, Time.deltaTime * lookSpeed);
            activeHeadBone.rotation = currentHeadRotation;
        }
        else
        {
            currentHeadRotation = desiredHeadRotation;
            activeHeadBone.rotation = desiredHeadRotation;
        }
    }

    private Quaternion ComputeTargetHeadRotation()
    {
        if (activeHeadBone == null)
            return transform.rotation;

        // 1. Always reset bone to pristine rest pose for this pose
        if (pristineLocalRotations.TryGetValue(currentPoseIndex, out Quaternion pristineRot))
        {
            activeHeadBone.localRotation = pristineRot;
        }
        else
        {
            activeHeadBone.localRotation = baseHeadLocalRotation;
        }

        ResolvePlayer();

        Vector3 targetPoint;
        if (playerCameraTransform != null)
        {
            targetPoint = playerCameraTransform.position;
        }
        else if (playerTransform != null)
        {
            targetPoint = playerTransform.position + Vector3.up * playerEyeOffset;
        }
        else
        {
            return activeHeadBone.rotation;
        }

        Vector3 headPos = activeHeadBone.position;
        Vector3 toTarget = targetPoint - headPos;
        if (toTarget.sqrMagnitude < 0.0001f)
            return activeHeadBone.rotation;

        Vector3 targetDir = toTarget.normalized;

        // Convert targetDir to monster local space
        Vector3 localDir = transform.InverseTransformDirection(targetDir);

        // Model's front face is modelFaceDirection (0, 0, -1) in local space
        // Calculate horizontal yaw and vertical pitch relative to face
        float yaw = Mathf.Atan2(localDir.x, -localDir.z) * Mathf.Rad2Deg;
        float pitch = Mathf.Atan2(localDir.y, Mathf.Sqrt(localDir.x * localDir.x + localDir.z * localDir.z)) * Mathf.Rad2Deg;

        // Clamp to natural human/monster limits
        float maxYaw = maxLookAngle; // e.g. 65 degrees
        float maxPitch = 35f;

        // If player is behind monster (|yaw| > 85 deg), smoothly fade head look back to forward facing
        float weight = 1f;
        float absYaw = Mathf.Abs(yaw);
        if (absYaw > 85f)
        {
            weight = Mathf.Clamp01(1f - (absYaw - 85f) / 35f);
        }

        float clampedYaw = Mathf.Clamp(yaw, -maxYaw, maxYaw) * weight;
        float clampedPitch = Mathf.Clamp(pitch, -maxPitch, maxPitch) * weight;

        // Compute aim direction in monster local space and convert to world
        Quaternion aimRotLocal = Quaternion.Euler(-clampedPitch, -clampedYaw, 0f);
        Vector3 aimDirLocal = aimRotLocal * modelFaceDirection;
        Vector3 aimDirWorld = transform.TransformDirection(aimDirLocal).normalized;

        Vector3 monsterFaceDir = transform.TransformDirection(modelFaceDirection).normalized;
        Quaternion deltaRotation = Quaternion.FromToRotation(monsterFaceDir, aimDirWorld);

        return deltaRotation * activeHeadBone.rotation;
    }

    private void ResolvePlayer()
    {
        if (playerCameraTransform == null && Camera.main != null)
        {
            playerCameraTransform = Camera.main.transform;
        }

        if (playerTransform == null)
        {
            PlayerMovement pm = Object.FindFirstObjectByType<PlayerMovement>();
            if (pm != null)
            {
                playerTransform = pm.transform;
            }
            else
            {
                try
                {
                    GameObject tagged = GameObject.FindWithTag("Player");
                    if (tagged != null) playerTransform = tagged.transform;
                }
                catch { }

                if (playerTransform == null && playerCameraTransform != null)
                {
                    playerTransform = playerCameraTransform.root;
                }
            }
        }
    }

    private void ResolvePoses()
    {
        if (poses != null && poses.Length > 0 && poses[0] != null)
            return;

        // Auto-discover under visualRoot or "MonsterVisual"
        if (visualRoot == null)
        {
            Transform foundRoot = transform.Find("MonsterVisual");
            if (foundRoot != null)
            {
                visualRoot = foundRoot;
            }
            else
            {
                visualRoot = transform;
            }
        }

        List<GameObject> foundPoses = new List<GameObject>();
        for (int i = 0; i < visualRoot.childCount; i++)
        {
            Transform child = visualRoot.GetChild(i);
            if (child.name.StartsWith("Pose_") || child.name.StartsWith("Pose"))
            {
                foundPoses.Add(child.gameObject);
            }
        }

        if (foundPoses.Count > 0)
        {
            poses = foundPoses.ToArray();
        }
    }
}
