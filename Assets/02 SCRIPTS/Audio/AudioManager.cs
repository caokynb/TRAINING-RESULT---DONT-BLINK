using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("SFX_01: Ambient Hum (Loop)")]
    [SerializeField] private AudioClip humAmbientClip;
    [Range(0f, 1f)]
    [SerializeField] private float humBaseVolume = 0.35f;

    [Header("SFX_02: Light Flicker Click")]
    [SerializeField] private AudioClip lightFlickerClip;
    [Range(0f, 1f)]
    [SerializeField] private float flickerVolume = 0.6f;

    [Header("SFX_03: Monster Footsteps (T12)")]
    [SerializeField] private AudioClip monsterFootstepsClip;
    [Range(0f, 1f)]
    [SerializeField] private float monsterFootstepsVolume = 0.85f;

    [Header("SFX_04: Blink Ambient")]
    [SerializeField] private AudioClip blinkAmbientClip;
    [Range(0f, 1f)]
    [SerializeField] private float blinkAmbientVolume = 0.5f;

    [Header("SFX_05: Player Footsteps")]
    [SerializeField] private AudioClip[] playerFootstepClips;
    [Range(0f, 1f)]
    [SerializeField] private float playerFootstepVolume = 0.45f;

    [Header("SFX_06: Panic Heartbeat")]
    [SerializeField] private AudioClip panicHeartbeatClip;
    [Range(0f, 1f)]
    [SerializeField] private float maxPanicVolume = 0.7f;
    [SerializeField] private float panicDistanceNear = 2f;
    [SerializeField] private float panicDistanceFar = 8f;

    [Header("SFX_07: Jumpscare")]
    [SerializeField] private AudioClip jumpscareClip;
    [Range(0f, 1f)]
    [SerializeField] private float jumpscareVolume = 1.0f;

    [Header("SFX_08: Exit Door")]
    [SerializeField] private AudioClip exitDoorClip;
    [Range(0f, 1f)]
    [SerializeField] private float exitDoorVolume = 0.8f;

    [Header("SFX_09: UI Click")]
    [SerializeField] private AudioClip uiClickClip;
    [Range(0f, 1f)]
    [SerializeField] private float uiClickVolume = 0.6f;

    [Header("SFX_10: Victory Ambient")]
    [SerializeField] private AudioClip victoryClip;
    [Range(0f, 1f)]
    [SerializeField] private float victoryVolume = 0.75f;

    [Header("SFX_11 & SFX_12: Spooky Ambience Stingers")]
    [SerializeField] private AudioClip[] spookyClips;
    [Range(0f, 1f)]
    [SerializeField] private float spookyVolume = 0.3f;
    [SerializeField] private float spookyMinInterval = 30f;
    [SerializeField] private float spookyMaxInterval = 65f;

    // Dedicated Audio Sources
    private AudioSource humSource;
    private AudioSource blinkSource;
    private AudioSource heartbeatSource;
    private AudioSource sfx2DSource;
    private AudioSource spookySource;

    private Coroutine blinkFadeCoroutine;
    private Coroutine spookyCoroutine;
    private Transform playerTransform;
    private Transform monsterTransform;
    private BlinkSystem blinkSystem;
    private float targetHeartbeatVolume = 0f;
    private float lastLightFlickerTime = -1f;

    public AudioClip MonsterFootstepsClip => monsterFootstepsClip;
    public float MonsterFootstepsVolume => monsterFootstepsVolume;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureAudioManager()
    {
        if (Instance != null)
            return;

        AudioManager existing = Object.FindFirstObjectByType<AudioManager>();
        if (existing != null)
        {
            Instance = existing;
            return;
        }

        GameObject audioManagerGO = new GameObject("AudioManager");
        existing = audioManagerGO.AddComponent<AudioManager>();
        DontDestroyOnLoad(audioManagerGO);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

#if UNITY_EDITOR
        if (humAmbientClip == null)
            AutoAssignClips();
#endif

        SetupAudioSources();
    }

    private void Start()
    {
        StartAmbientHum();

        if (spookyCoroutine != null)
            StopCoroutine(spookyCoroutine);
        spookyCoroutine = StartCoroutine(SpookyAmbienceRoutine());
    }

    private void OnEnable()
    {
        BlinkEvents.OnEyesClosed += HandleEyesClosed;
        BlinkEvents.OnEyesOpened += HandleEyesOpened;
        GameEvents.OnPlayerCaught += HandlePlayerCaught;
        GameEvents.OnPlayerReachedExit += HandlePlayerReachedExit;
    }

    private void OnDisable()
    {
        BlinkEvents.OnEyesClosed -= HandleEyesClosed;
        BlinkEvents.OnEyesOpened -= HandleEyesOpened;
        GameEvents.OnPlayerCaught -= HandlePlayerCaught;
        GameEvents.OnPlayerReachedExit -= HandlePlayerReachedExit;
    }

    private void Update()
    {
        UpdatePanicHeartbeat();
    }

    private void SetupAudioSources()
    {
        // 1. Fluorescent Hum Source
        humSource = Create2DAudioSource("AudioSource_HumAmbient", loop: true, volume: humBaseVolume);
        humSource.clip = humAmbientClip;

        // 2. Blink Ambient Source
        blinkSource = Create2DAudioSource("AudioSource_BlinkAmbient", loop: true, volume: 0f);
        blinkSource.clip = blinkAmbientClip;

        // 3. Heartbeat Panic Source
        heartbeatSource = Create2DAudioSource("AudioSource_PanicHeartbeat", loop: true, volume: 0f);
        heartbeatSource.clip = panicHeartbeatClip;

        // 4. SFX 2D One-Shot Source
        sfx2DSource = Create2DAudioSource("AudioSource_SFX2D", loop: false, volume: 1f);

        // 5. Spooky Ambient Source
        spookySource = Create2DAudioSource("AudioSource_SpookyAmbience", loop: false, volume: spookyVolume);
    }

    private AudioSource Create2DAudioSource(string childName, bool loop, float volume)
    {
        Transform child = transform.Find(childName);
        GameObject go = child != null ? child.gameObject : new GameObject(childName);
        go.transform.SetParent(transform);

        AudioSource src = go.GetComponent<AudioSource>();
        if (src == null)
            src = go.AddComponent<AudioSource>();

        src.playOnAwake = false;
        src.loop = loop;
        src.volume = volume;
        src.spatialBlend = 0f; // 2D
        src.dopplerLevel = 0f;
        return src;
    }

    public void StartAmbientHum()
    {
        if (humSource != null && humAmbientClip != null && !humSource.isPlaying)
        {
            humSource.clip = humAmbientClip;
            humSource.volume = humBaseVolume;
            humSource.Play();
        }
    }

    public void StopAmbientHum()
    {
        if (humSource != null && humSource.isPlaying)
        {
            humSource.Stop();
        }
    }

    // --- T11: Light Flicker SFX ---
    public void PlayLightFlicker(Vector3 position)
    {
        if (lightFlickerClip == null)
            return;

        // Throttle rapid flickers slightly to keep audio crisp
        if (Time.time - lastLightFlickerTime < 0.12f)
            return;

        lastLightFlickerTime = Time.time;
        float pitch = Random.Range(0.9f, 1.15f);
        PlaySpatial(lightFlickerClip, position, flickerVolume, minDistance: 2f, maxDistance: 20f, pitch: pitch);
    }

    // --- Player Footsteps (SFX_05) ---
    public void PlayPlayerFootstep(Vector3 position)
    {
        if (playerFootstepClips == null || playerFootstepClips.Length == 0)
            return;

        AudioClip clip = playerFootstepClips[Random.Range(0, playerFootstepClips.Length)];
        if (clip != null)
        {
            float pitch = Random.Range(0.92f, 1.08f);
            PlaySpatial(clip, position, playerFootstepVolume, minDistance: 1f, maxDistance: 12f, pitch: pitch);
        }
    }

    // --- UI Button Click (SFX_09) ---
    public void PlayUIClick()
    {
        if (uiClickClip != null && sfx2DSource != null)
        {
            sfx2DSource.PlayOneShot(uiClickClip, uiClickVolume);
        }
    }

    // --- Exit Door (SFX_08) ---
    public void PlayExitDoor(Vector3 position)
    {
        if (exitDoorClip != null)
        {
            PlaySpatial(exitDoorClip, position, exitDoorVolume, minDistance: 2f, maxDistance: 35f);
        }
    }

    // --- Victory Ambient (SFX_10) ---
    public void PlayVictory()
    {
        if (victoryClip != null && sfx2DSource != null)
        {
            sfx2DSource.PlayOneShot(victoryClip, victoryVolume);
        }
    }

    // --- Jumpscare (SFX_07) ---
    public void PlayJumpscare()
    {
        if (jumpscareClip != null && sfx2DSource != null)
        {
            sfx2DSource.PlayOneShot(jumpscareClip, jumpscareVolume);
        }
    }

    // --- 3D Spatial Audio One-Shot Helper ---
    public void PlaySpatial(AudioClip clip, Vector3 position, float volume = 1f, float minDistance = 1.5f, float maxDistance = 25f, float pitch = 1f)
    {
        if (clip == null)
            return;

        GameObject tempGO = new GameObject($"SpatialAudio_{clip.name}");
        tempGO.transform.position = position;

        AudioSource src = tempGO.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = volume;
        src.pitch = pitch;
        src.spatialBlend = 1f; // Full 3D
        src.minDistance = minDistance;
        src.maxDistance = maxDistance;
        src.rolloffMode = AudioRolloffMode.Logarithmic;
        src.dopplerLevel = 0f;
        src.playOnAwake = false;
        src.Play();

        Destroy(tempGO, (clip.length / Mathf.Max(0.1f, pitch)) + 0.15f);
    }

    // --- Blink Events (SFX_04) ---
    private void HandleEyesClosed(float duration)
    {
        if (blinkSource == null || blinkAmbientClip == null)
            return;

        if (blinkFadeCoroutine != null)
            StopCoroutine(blinkFadeCoroutine);

        if (!blinkSource.isPlaying)
        {
            blinkSource.clip = blinkAmbientClip;
            blinkSource.Play();
        }

        blinkFadeCoroutine = StartCoroutine(FadeAudioSource(blinkSource, blinkAmbientVolume, 0.15f));
    }

    private void HandleEyesOpened()
    {
        if (blinkSource == null)
            return;

        if (blinkFadeCoroutine != null)
            StopCoroutine(blinkFadeCoroutine);

        blinkFadeCoroutine = StartCoroutine(FadeAudioSource(blinkSource, 0f, 0.2f, stopOnZero: true));
    }

    // --- Game Events Handlers ---
    private void HandlePlayerCaught()
    {
        PlayJumpscare();

        // Mute or dim background hum on catch
        if (humSource != null)
            humSource.volume = humBaseVolume * 0.2f;

        if (heartbeatSource != null)
            heartbeatSource.volume = 0f;
    }

    private void HandlePlayerReachedExit()
    {
        ResolvePlayerAndMonster();
        Vector3 doorPos = playerTransform != null ? playerTransform.position : Vector3.zero;
        PlayExitDoor(doorPos);
        PlayVictory();
    }

    // --- Dynamic Panic Heartbeat Update (SFX_06) ---
    private void UpdatePanicHeartbeat()
    {
        if (heartbeatSource == null || panicHeartbeatClip == null)
            return;

        ResolvePlayerAndMonster();

        float targetVol = 0f;

        // 1. Distance to monster panic
        if (playerTransform != null && monsterTransform != null)
        {
            float dist = Vector3.Distance(playerTransform.position, monsterTransform.position);
            if (dist < panicDistanceFar)
            {
                float t = Mathf.InverseLerp(panicDistanceFar, panicDistanceNear, dist);
                targetVol = Mathf.Max(targetVol, t * maxPanicVolume);
            }
        }

        // 2. High eye fatigue panic
        if (blinkSystem != null)
        {
            if (blinkSystem.Fatigue > 0.7f)
            {
                float f = Mathf.InverseLerp(0.7f, 1f, blinkSystem.Fatigue);
                targetVol = Mathf.Max(targetVol, f * (maxPanicVolume * 0.85f));
            }
        }

        targetHeartbeatVolume = targetVol;

        if (targetHeartbeatVolume > 0.02f)
        {
            if (!heartbeatSource.isPlaying)
            {
                heartbeatSource.clip = panicHeartbeatClip;
                heartbeatSource.Play();
            }
        }

        heartbeatSource.volume = Mathf.MoveTowards(heartbeatSource.volume, targetHeartbeatVolume, Time.deltaTime * 1.5f);

        if (heartbeatSource.volume <= 0.005f && heartbeatSource.isPlaying && targetHeartbeatVolume <= 0.005f)
        {
            heartbeatSource.Stop();
        }
    }

    private void ResolvePlayerAndMonster()
    {
        if (playerTransform == null)
        {
            PlayerMovement pm = Object.FindFirstObjectByType<PlayerMovement>();
            if (pm != null)
            {
                playerTransform = pm.transform;
                blinkSystem = pm.GetComponent<BlinkSystem>();
            }
        }

        if (monsterTransform == null)
        {
            MonsterBrain mb = Object.FindFirstObjectByType<MonsterBrain>();
            if (mb != null)
                monsterTransform = mb.transform;
        }
    }

    // --- SFX_11 & SFX_12: Random Spooky Ambience Coroutine ---
    private IEnumerator SpookyAmbienceRoutine()
    {
        while (true)
        {
            float waitTime = Random.Range(spookyMinInterval, spookyMaxInterval);
            yield return new WaitForSeconds(waitTime);

            if (spookyClips != null && spookyClips.Length > 0 && spookySource != null)
            {
                AudioClip clip = spookyClips[Random.Range(0, spookyClips.Length)];
                if (clip != null)
                {
                    spookySource.PlayOneShot(clip, spookyVolume);
                }
            }
        }
    }

    private IEnumerator FadeAudioSource(AudioSource source, float targetVolume, float duration, bool stopOnZero = false)
    {
        float startVolume = source.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
            yield return null;
        }

        source.volume = targetVolume;
        if (stopOnZero && targetVolume <= 0.01f)
        {
            source.Stop();
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Auto-Assign Audio Clips From SFX Folder")]
    public void AutoAssignClips()
    {
        humAmbientClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/05 SFX/SFX_01.mp3");
        lightFlickerClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/05 SFX/SFX_02.mp3");
        monsterFootstepsClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/05 SFX/SFX_03.mp3");
        blinkAmbientClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/05 SFX/SFX_04.mp3");
        playerFootstepClips = new AudioClip[]
        {
            UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/05 SFX/SFX_05_1.mp3"),
            UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/05 SFX/SFX_05_2.mp3"),
            UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/05 SFX/SFX_05_3.mp3")
        };
        panicHeartbeatClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/05 SFX/SFX_06.mp3");
        jumpscareClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/05 SFX/SFX_07.mp3");
        exitDoorClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/05 SFX/SFX_08.mp3");
        uiClickClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/05 SFX/SFX_09.mp3");
        victoryClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/05 SFX/SFX_10.mp3");
        spookyClips = new AudioClip[]
        {
            UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/05 SFX/SFX_11.mp3"),
            UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/05 SFX/SFX_12.mp3")
        };

        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
