using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class MonsterBrain : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Cấu hình quái vật và độ khó (ScriptableObject). Nếu để trống, sẽ dùng các thông số mặc định bên dưới.")]
    [SerializeField] private MonsterSettings settings;

    [Header("Grace Period & Catch (Fallbacks)")]
    [SerializeField] private float initialGracePeriod = 10f;
    [SerializeField] private float catchDistance = 1.8f;

    [Header("Step Counts (Fallbacks)")]
    [SerializeField] private int nearCellThreshold = 4;
    [SerializeField] private int farCellThreshold = 10;
    [SerializeField] private int nearBlinkSteps = 1;
    [SerializeField] private int normalMinBlinkSteps = 1;
    [SerializeField] private int normalMaxBlinkSteps = 2;
    [SerializeField] private int farBlinkSteps = 3;

    [Header("Hold Intervals (Fallbacks)")]
    [SerializeField] private float nearHoldStepInterval = 0.40f;
    [SerializeField] private float normalHoldStepInterval = 0.35f;
    [SerializeField] private float farHoldStepInterval = 0.30f;
    [SerializeField] private float minHoldStepInterval = 0.12f;
    [SerializeField] private float holdAcceleration = 0.10f;

    [Header("Debug & Events")]
    [SerializeField] private float simulatedBlinkDuration = 0.25f;
    [SerializeField] private UnityEvent onPlayerCaught;

    [Header("Model Facing Direction")]
    [Tooltip("The local direction the monster face points in (default -Z)")]
    [SerializeField] private Vector3 modelFaceDirection = new Vector3(0f, 0f, -1f);

    [Header("Audio (T12)")]
    [SerializeField] private AudioSource monsterAudioSource;
    [SerializeField] private AudioClip footstepsClip;
    [Range(0f, 1f)]
    [SerializeField] private float footstepsVolume = 0.85f;
    [SerializeField] private float minFootstepDistance = 2f;
    [SerializeField] private float maxFootstepDistance = 30f;

    private MapGrid mapGrid;
    private Transform playerTransform;
    private BlinkSystem blinkSystem;
    private float spawnTime;
    private float keyPickupTime = -1f;
    private float holdStepTimer;
    private float closedDuration;
    private int stepsTakenInCurrentBlink;
    private bool wasEyesClosed;
    private bool hasCaughtPlayer;

    public MonsterSettings Settings
    {
        get => settings;
        set => settings = value;
    }

    public float InitialGracePeriod => settings != null ? settings.InitialGracePeriod : initialGracePeriod;
    public float CatchDistance => settings != null ? settings.CatchDistance : catchDistance;
    public int NearCellThreshold => settings != null ? settings.NearCellThreshold : nearCellThreshold;
    public int FarCellThreshold => settings != null ? settings.FarCellThreshold : farCellThreshold;
    public int NearBlinkSteps => settings != null ? settings.NearBlinkSteps : nearBlinkSteps;
    public int NormalMinBlinkSteps => settings != null ? settings.NormalMinBlinkSteps : normalMinBlinkSteps;
    public int NormalMaxBlinkSteps => settings != null ? settings.NormalMaxBlinkSteps : normalMaxBlinkSteps;
    public int FarBlinkSteps => settings != null ? settings.FarBlinkSteps : farBlinkSteps;
    public float NearHoldStepInterval => settings != null ? settings.NearHoldStepInterval : nearHoldStepInterval;
    public float NormalHoldStepInterval => settings != null ? settings.NormalHoldStepInterval : normalHoldStepInterval;
    public float FarHoldStepInterval => settings != null ? settings.FarHoldStepInterval : farHoldStepInterval;
    public float MinHoldStepInterval => settings != null ? settings.MinHoldStepInterval : minHoldStepInterval;
    public float HoldAcceleration => settings != null ? settings.HoldAcceleration : holdAcceleration;

    private bool HasKey => KeyInventory.Instance != null && KeyInventory.Instance.HasKey;
    private bool IsForcedBlink => blinkSystem != null && blinkSystem.IsBlinking && !blinkSystem.IsManualBlink;

    public float TimeSinceGracePeriod => Mathf.Max(0f, (Time.time - spawnTime) - InitialGracePeriod);
    public float TimeSinceKeyPickup => (keyPickupTime >= 0f) ? Mathf.Max(0f, Time.time - keyPickupTime) : 0f;

    public float CurrentDifficulty => settings != null 
        ? settings.EvaluateDifficulty(TimeSinceGracePeriod, HasKey, TimeSinceKeyPickup) 
        : (HasKey ? 0.35f : 0f);

    public int CurrentBonusSteps => settings != null 
        ? settings.GetBonusSteps(TimeSinceGracePeriod, HasKey, TimeSinceKeyPickup, IsForcedBlink) 
        : (HasKey ? 1 : 0);

    private void SetupMonsterAudio()
    {
        if (monsterAudioSource == null)
            monsterAudioSource = GetComponent<AudioSource>();
        if (monsterAudioSource == null)
            monsterAudioSource = gameObject.AddComponent<AudioSource>();

        monsterAudioSource.playOnAwake = false;
        monsterAudioSource.spatialBlend = 1f; // 3D spatial
        monsterAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        monsterAudioSource.minDistance = minFootstepDistance;
        monsterAudioSource.maxDistance = maxFootstepDistance;
        monsterAudioSource.dopplerLevel = 0f;

        if (footstepsClip == null && AudioManager.Instance != null)
            footstepsClip = AudioManager.Instance.MonsterFootstepsClip;
    }

    private void Awake()
    {
        SetupMonsterAudio();
        mapGrid = Object.FindFirstObjectByType<MapGrid>();
        ResolvePlayer();
    }

    private void Start()
    {
        spawnTime = Time.time;
        keyPickupTime = -1f;
        ResolvePlayer();
        FacePlayer();
    }

    private void OnEnable()
    {
        BlinkEvents.OnEyesClosed += HandleEyesClosed;
        BlinkEvents.OnEyesOpened += HandleEyesOpened;
        GameEvents.OnPlayerCaught += Die;
        GameEvents.OnPlayerReachedExit += HandlePlayerReachedExit;
        GameEvents.OnKeyPickedUp += HandleKeyPickedUp;
    }

    private void OnDisable()
    {
        BlinkEvents.OnEyesClosed -= HandleEyesClosed;
        BlinkEvents.OnEyesOpened -= HandleEyesOpened;
        GameEvents.OnPlayerCaught -= Die;
        GameEvents.OnPlayerReachedExit -= HandlePlayerReachedExit;
        GameEvents.OnKeyPickedUp -= HandleKeyPickedUp;
        StopFootstepSound();
    }

    private void HandleKeyPickedUp()
    {
        keyPickupTime = Time.time;
        Debug.Log("[MonsterBrain] Player picked up key! Enraged hunt mode activated (Exponential scaling).");
        FacePlayer();
    }

    private void HandlePlayerReachedExit()
    {
        hasCaughtPlayer = true;
        StopFootstepSound();
        enabled = false;
    }

    private void Update()
    {
        HandleDebugInput();

        if (hasCaughtPlayer)
            return;

        ResolvePlayer();
        CheckCatchDistance();
        if (hasCaughtPlayer)
            return;

        // Đồng bộ thời gian nhặt chìa khóa dự phòng
        if (HasKey && keyPickupTime < 0f)
        {
            keyPickupTime = Time.time;
        }

        bool eyesClosed = IsEyesClosed();

        if (!eyesClosed)
        {
            if (wasEyesClosed)
            {
                wasEyesClosed = false;
                holdStepTimer = 0f;
                closedDuration = 0f;
                stepsTakenInCurrentBlink = 0;
                StopFootstepSound();
                FacePlayer();
            }
            return;
        }

        if (!wasEyesClosed)
        {
            wasEyesClosed = true;
            holdStepTimer = 0f;
            closedDuration = 0f;
            stepsTakenInCurrentBlink = 0;
        }

        if (!IsGracePeriodOver())
            return;

        // Giới hạn số bước tối đa trong một đợt nhắm mắt (đặc biệt khi mỏi mắt / forced blink)
        int maxStepsAllowed = settings != null ? settings.GetMaxStepsPerBlink(HasKey) : (HasKey ? 10 : 3);
        if (stepsTakenInCurrentBlink >= maxStepsAllowed)
        {
            // Đã đạt số bước tối đa cho phép trong lần nhắm mắt này -> Dừng lại chờ người chơi mở mắt
            StopFootstepSound();
            return;
        }

        closedDuration += Time.deltaTime;
        holdStepTimer += Time.deltaTime;

        int cellDist = GetCurrentCellDistance();
        float baseInterval = GetBaseIntervalByDistance(cellDist);
        float intervalMult = settings != null 
            ? settings.GetHoldIntervalMultiplier(TimeSinceGracePeriod, HasKey, TimeSinceKeyPickup) 
            : (HasKey ? 0.8f : 1.6f);

        // Quái vật chỉ bứt tốc dồn ép khi người chơi bị ép nhắm mắt (Forced Blink) SAU KHI ĐÃ CÓ CHÌA KHÓA
        if (IsForcedBlink && HasKey)
        {
            intervalMult *= 0.75f;
        }

        float minInterval = settings != null ? settings.MinHoldStepInterval : minHoldStepInterval;
        float currentInterval = Mathf.Max(
            minInterval,
            (baseInterval - closedDuration * HoldAcceleration) * intervalMult
        );

        if (holdStepTimer >= currentInterval)
        {
            holdStepTimer -= currentInterval;
            int holdSteps = GetHoldSteps(cellDist, closedDuration);
            int remainingSteps = maxStepsAllowed - stepsTakenInCurrentBlink;
            int actualSteps = Mathf.Clamp(holdSteps, 1, remainingSteps);

            if (actualSteps > 0)
            {
                StepTowardsPlayer(actualSteps);
                stepsTakenInCurrentBlink += actualSteps;
            }
        }
    }

    private void HandleDebugInput()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            BlinkEvents.RaiseClosed(simulatedBlinkDuration);
        }
        else if (Keyboard.current.tKey.wasReleasedThisFrame)
        {
            BlinkEvents.RaiseOpened();
        }
    }

    private void HandleEyesClosed(float duration)
    {
        wasEyesClosed = true;
        holdStepTimer = 0f;
        closedDuration = 0f;
        stepsTakenInCurrentBlink = 0;
        Debug.Log($"[MonsterBrain] Eyes Closed (duration: {duration:F2}s, forced: {IsForcedBlink})");

        if (hasCaughtPlayer)
            return;

        if (!IsGracePeriodOver())
        {
            FacePlayer();
            return;
        }

        int cellDist = GetCurrentCellDistance();
        int maxStepsAllowed = settings != null ? settings.GetMaxStepsPerBlink(HasKey) : (HasKey ? 10 : 3);
        int initialSteps = GetInitialBlinkSteps(cellDist);

        int stepsToTake = Mathf.Clamp(initialSteps, 1, maxStepsAllowed);
        StepTowardsPlayer(stepsToTake);
        stepsTakenInCurrentBlink += stepsToTake;
    }

    private void HandleEyesOpened()
    {
        wasEyesClosed = false;
        holdStepTimer = 0f;
        closedDuration = 0f;
        stepsTakenInCurrentBlink = 0;
        StopFootstepSound();
        FacePlayer();
        Debug.Log("[MonsterBrain] Eyes Opened");
    }

    private void Die()
    {
        Debug.Log("Die");
        StopFootstepSound();
        onPlayerCaught?.Invoke();
    }

    private void PlayFootstepSound()
    {
        if (monsterAudioSource == null)
            SetupMonsterAudio();

        if (footstepsClip == null && AudioManager.Instance != null)
            footstepsClip = AudioManager.Instance.MonsterFootstepsClip;

        if (monsterAudioSource != null && footstepsClip != null)
        {
            monsterAudioSource.clip = footstepsClip;
            monsterAudioSource.volume = footstepsVolume;
            
            // Pitch thay đổi theo độ khó: lúc roaming giữ chuẩn (1.0), khi có chìa tăng dần tạo cảm giác dồn dập
            float diffPitch = HasKey ? Mathf.Lerp(1.0f, 1.25f, CurrentDifficulty) : 1.0f;
            monsterAudioSource.pitch = diffPitch * Random.Range(0.96f, 1.04f);

            if (!monsterAudioSource.isPlaying)
            {
                monsterAudioSource.Play();
            }
        }
    }

    private void StopFootstepSound()
    {
        if (monsterAudioSource != null && monsterAudioSource.isPlaying)
        {
            monsterAudioSource.Stop();
        }
    }

    private int GetInitialBlinkSteps(int cellDist)
    {
        if (!HasKey)
        {
            // Giai đoạn Roaming: di chuyển nhẹ nhàng, cự ly xa tối đa preKeyFarBlinkSteps
            if (cellDist <= NearCellThreshold)
                return NearBlinkSteps;

            int farLimit = settings != null ? settings.PreKeyFarBlinkSteps : 2;
            if (cellDist >= FarCellThreshold)
                return farLimit;

            return Random.Range(NormalMinBlinkSteps, Mathf.Min(NormalMaxBlinkSteps + 1, farLimit + 1));
        }

        // Giai đoạn Escape: di chuyển hung hãn theo độ khó Exponential
        int baseSteps;
        if (cellDist <= NearCellThreshold)
            baseSteps = NearBlinkSteps;
        else if (cellDist >= FarCellThreshold * 2)
            baseSteps = FarBlinkSteps + 1;
        else if (cellDist >= FarCellThreshold)
            baseSteps = FarBlinkSteps;
        else
            baseSteps = Random.Range(NormalMinBlinkSteps, NormalMaxBlinkSteps + 1);

        int bonusSteps = CurrentBonusSteps;
        return baseSteps + bonusSteps;
    }

    private float GetBaseIntervalByDistance(int cellDist)
    {
        if (cellDist <= NearCellThreshold)
            return NearHoldStepInterval;

        if (cellDist >= FarCellThreshold)
            return FarHoldStepInterval;

        return NormalHoldStepInterval;
    }

    private int GetHoldSteps(int cellDist, float heldTime)
    {
        if (!HasKey)
        {
            // Roaming: mỗi nhịp hold chỉ bước đúng 1 ô
            return 1;
        }

        // Escape: Càng giữ mắt nhắm lâu quái càng sải bước dài hơn
        int baseSteps;
        if (cellDist <= NearCellThreshold)
            baseSteps = heldTime >= 1.0f ? 2 : 1;
        else if (cellDist >= FarCellThreshold)
            baseSteps = heldTime >= 0.5f ? 2 : 1;
        else
            baseSteps = heldTime >= 0.7f ? 2 : 1;

        int bonusSteps = CurrentBonusSteps;
        return baseSteps + (bonusSteps >= 2 ? 1 : 0);
    }

    private int GetCurrentCellDistance()
    {
        if (mapGrid == null)
            mapGrid = Object.FindFirstObjectByType<MapGrid>();

        if (mapGrid != null && !mapGrid.IsLoaded)
            mapGrid.LoadMap();

        ResolvePlayer();
        if (playerTransform == null)
            return 0;

        if (mapGrid != null && mapGrid.IsLoaded)
        {
            Vector2Int monsterCell = mapGrid.WorldToCell(transform.position);
            Vector2Int playerCell = mapGrid.FindNearestWalkableCell(mapGrid.WorldToCell(playerTransform.position));
            int bfsDist = mapGrid.GetBFSDistance(monsterCell, playerCell);
            if (bfsDist >= 0)
                return bfsDist;
        }

        float cellSize = mapGrid != null ? mapGrid.CellSize : 3f;
        float worldDist = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.z),
            new Vector2(playerTransform.position.x, playerTransform.position.z)
        );
        return Mathf.RoundToInt(worldDist / Mathf.Max(0.1f, cellSize));
    }

    private bool IsEyesClosed()
    {
        if (BlinkEvents.EyesClosed)
            return true;

        if (blinkSystem != null && blinkSystem.IsBlinking)
            return true;

        return false;
    }

    private bool IsGracePeriodOver()
    {
        return (Time.time - spawnTime) >= InitialGracePeriod;
    }

    private void StepTowardsPlayer(int steps)
    {
        if (steps <= 0 || hasCaughtPlayer)
            return;

        // T12: Monster footsteps sound plays only when stepping during closed eyes
        PlayFootstepSound();

        if (mapGrid == null)
            mapGrid = Object.FindFirstObjectByType<MapGrid>();

        if (mapGrid != null && !mapGrid.IsLoaded)
            mapGrid.LoadMap();

        ResolvePlayer();
        if (playerTransform == null)
            return;

        // 1. Direct pursuit if there is unobstructed line-of-sight to the player in open room/corridor
        if (HasLineOfSightToPlayer())
        {
            MoveDirectTowardsPlayer(steps);
            FacePlayer();
            CheckCatchDistance();
            return;
        }

        // 2. Otherwise navigate around walls using 8-directional A* optimal path
        if (mapGrid != null && mapGrid.IsLoaded)
        {
            Vector2Int monsterCell = mapGrid.WorldToCell(transform.position);
            Vector2Int playerCell = mapGrid.FindNearestWalkableCell(mapGrid.WorldToCell(playerTransform.position));

            List<Vector2Int> path = mapGrid.GetBFSPath(monsterCell, playerCell);
            if (path != null && path.Count > 1)
            {
                int targetIndex = Mathf.Min(steps, path.Count - 1);
                Vector2Int nextCell = path[targetIndex];

                if (nextCell == playerCell || targetIndex >= path.Count - 1)
                {
                    MoveInsidePlayerCell();
                }
                else
                {
                    Vector3 targetWorld = mapGrid.CellToWorld(nextCell);
                    MoveMonsterTo(new Vector3(targetWorld.x, transform.position.y, targetWorld.z));
                }

                FacePlayer();
                CheckCatchDistance();
                return;
            }

            if (path != null && path.Count == 1)
            {
                MoveInsidePlayerCell();
                FacePlayer();
                CheckCatchDistance();
                return;
            }
        }

        MoveDirectTowardsPlayer(steps);
        FacePlayer();
        CheckCatchDistance();
    }

    private bool HasLineOfSightToPlayer()
    {
        if (playerTransform == null)
            return false;

        if (mapGrid != null && mapGrid.IsLoaded)
        {
            Vector2Int monsterCell = mapGrid.WorldToCell(transform.position);
            Vector2Int playerCell = mapGrid.WorldToCell(playerTransform.position);
            if (!mapGrid.HasLineOfSight(monsterCell, playerCell))
                return false;
        }

        Vector3 from = transform.position + Vector3.up * 1.5f;
        Vector3 to = playerTransform.position + Vector3.up * 1.5f;
        Vector3 dir = to - from;
        float dist = dir.magnitude;

        if (dist > 0.1f)
        {
            if (Physics.Raycast(from, dir.normalized, out RaycastHit hit, dist))
            {
                if (!hit.transform.CompareTag("Player") && hit.transform.root != playerTransform.root)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private void MoveInsidePlayerCell()
    {
        Vector3 playerPos = playerTransform.position;
        Vector3 toMonster = transform.position - playerPos;
        toMonster.y = 0f;

        float stopDist = Mathf.Min(1.0f, CatchDistance * 0.5f);
        Vector3 dir = toMonster.sqrMagnitude > 0.001f ? toMonster.normalized : -playerTransform.forward;
        dir.y = 0f;

        Vector3 targetPos = playerPos + dir * stopDist;
        MoveMonsterTo(new Vector3(targetPos.x, transform.position.y, targetPos.z));
    }

    private void MoveDirectTowardsPlayer(int steps)
    {
        float cellSize = mapGrid != null ? mapGrid.CellSize : 3f;
        float maxStepDistance = steps * cellSize;
        Vector3 currentXZ = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 playerXZ = new Vector3(playerTransform.position.x, 0f, playerTransform.position.z);
        float dist = Vector3.Distance(currentXZ, playerXZ);
        float stopDist = Mathf.Min(1.0f, CatchDistance * 0.5f);

        if (dist <= stopDist)
            return;

        float moveDist = Mathf.Min(maxStepDistance, dist - stopDist);
        Vector3 nextXZ = Vector3.MoveTowards(currentXZ, playerXZ, moveDist);
        MoveMonsterTo(new Vector3(nextXZ.x, transform.position.y, nextXZ.z));
    }

    private void MoveMonsterTo(Vector3 targetPosition)
    {
        float floorY = mapGrid != null ? mapGrid.transform.position.y : transform.position.y;
        if (Physics.Raycast(new Vector3(targetPosition.x, floorY + 2f, targetPosition.z), Vector3.down, out RaycastHit hit, 5f))
        {
            targetPosition.y = hit.point.y;
        }
        else
        {
            targetPosition.y = floorY;
        }

        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null && cc.enabled)
        {
            cc.enabled = false;
            transform.position = targetPosition;
            cc.enabled = true;
        }
        else
        {
            transform.position = targetPosition;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            rb.position = targetPosition;
            rb.linearVelocity = Vector3.zero;
        }
    }

    private void FacePlayer()
    {
        if (playerTransform == null)
            return;

        Vector3 direction = playerTransform.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion lookRot = Quaternion.LookRotation(direction.normalized, Vector3.up);
            Quaternion modelOffset = Quaternion.FromToRotation(modelFaceDirection.normalized, Vector3.forward);
            transform.rotation = lookRot * modelOffset;
        }
    }

    private void CheckCatchDistance()
    {
        if (hasCaughtPlayer || playerTransform == null)
            return;

        Vector2 monsterXZ = new Vector2(transform.position.x, transform.position.z);
        Vector2 playerXZ = new Vector2(playerTransform.position.x, playerTransform.position.z);
        float distance = Vector2.Distance(monsterXZ, playerXZ);

        if (distance < CatchDistance)
        {
            hasCaughtPlayer = true;
            FacePlayer();
            GameEvents.RaisePlayerCaught();
        }
    }

    private void ResolvePlayer()
    {
        if (playerTransform == null)
        {
            PlayerMovement playerMovement = Object.FindFirstObjectByType<PlayerMovement>();
            if (playerMovement != null)
            {
                playerTransform = playerMovement.transform;
            }
            else
            {
                try
                {
                    GameObject taggedPlayer = GameObject.FindWithTag("Player");
                    if (taggedPlayer != null)
                        playerTransform = taggedPlayer.transform;
                }
                catch
                {
                }

                if (playerTransform == null)
                {
                    GameObject namedPlayer = GameObject.Find("Player");
                    if (namedPlayer != null)
                        playerTransform = namedPlayer.transform;
                }

                if (playerTransform == null && Camera.main != null)
                {
                    playerTransform = Camera.main.transform;
                }
            }
        }

        if (playerTransform != null && blinkSystem == null)
        {
            blinkSystem = playerTransform.GetComponentInParent<BlinkSystem>() ?? Object.FindFirstObjectByType<BlinkSystem>();
        }
    }
}
