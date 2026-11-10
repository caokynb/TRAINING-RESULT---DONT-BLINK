using System;
using UnityEngine;

[RequireComponent(typeof(PlayerInputReader), typeof(PlayerMovement))]
public sealed class BlinkSystem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameSettings settings;

    [Header("Fallbacks (Used if Settings is not assigned)")]
    [SerializeField, Min(0.1f)] private float fatigueTime = 9.5f;
    [SerializeField, Min(0.01f)] private float manualFatigueRecoveryRate = 0.45f;
    [SerializeField, Min(0f)] private float manualBlinkCooldown = 2.5f;
    [SerializeField, Min(0.1f)] private float minManualBlinkDuration = 0.4f;
    [SerializeField, Range(0f, 0.5f)] private float minFatigueToBlink = 0.2f;
    [SerializeField, Min(0.01f)] private float forcedBlinkDuration = 1.2f;
    [SerializeField, Range(0.1f, 1f)] private float blindSpeedMultiplier = 0.7f;

    public GameSettings Settings
    {
        get => settings;
        set => settings = value;
    }

    public float FatigueTime => settings != null ? settings.FatigueTime : fatigueTime;
    public float ManualFatigueRecoveryRate => settings != null ? settings.ManualFatigueRecoveryRate : manualFatigueRecoveryRate;
    public float ManualBlinkCooldown => settings != null ? settings.ManualBlinkCooldown : manualBlinkCooldown;
    public float MinManualBlinkDuration => settings != null ? settings.MinManualBlinkDuration : minManualBlinkDuration;
    public float MinFatigueToBlink => settings != null ? settings.MinFatigueToBlink : minFatigueToBlink;
    public float ForcedBlinkDuration => settings != null ? settings.ForcedBlinkDuration : forcedBlinkDuration;
    public float BlindSpeedMultiplier => settings != null ? settings.BlindSpeedMultiplier : blindSpeedMultiplier;

    public event Action<float> FatigueChanged;
    public event Action<bool> EyesClosedChanged;

    public float Fatigue { get; private set; }
    public bool IsBlinking { get; private set; }
    public bool IsManualBlink => isManualBlink;
    public float ManualCooldownRemaining => manualCooldownRemaining;

    private PlayerInputReader inputReader;
    private PlayerMovement playerMovement;
    private float blinkTimeRemaining;
    private float manualCooldownRemaining;
    private float currentBlinkElapsed;
    private bool isManualBlink;
    private bool wantsToOpen;

    private void Awake()
    {
        inputReader = GetComponent<PlayerInputReader>();
        playerMovement = GetComponent<PlayerMovement>();

        if (settings == null)
        {
            var movement = GetComponent<PlayerMovement>();
            if (movement != null && movement.Settings != null)
                settings = movement.Settings;
        }
    }

    private void OnEnable()
    {
        inputReader.BlinkPressed += TryManualBlink;
        inputReader.BlinkReleased += StopManualBlink;
        GameEvents.OnPlayerReachedExit += HandleGameWon;
        FatigueChanged?.Invoke(Fatigue);
        EyesClosedChanged?.Invoke(false);
    }

    private void OnDisable()
    {
        inputReader.BlinkPressed -= TryManualBlink;
        inputReader.BlinkReleased -= StopManualBlink;
        GameEvents.OnPlayerReachedExit -= HandleGameWon;

        if (IsBlinking)
            FinishBlink();
    }

    private void HandleGameWon()
    {
        if (IsBlinking)
            FinishBlink();

        SetFatigue(0f);
        enabled = false;
    }

    private void Update()
    {
        if (IsBlinking)
        {
            currentBlinkElapsed += Time.deltaTime;

            if (isManualBlink)
            {
                // Space controls how long the eyes stay closed; reaching zero fatigue does not open them.
                SetFatigue(Fatigue - ManualFatigueRecoveryRate * Time.deltaTime);

                // If player released Space earlier, open eyes once minimum blink duration is met
                if (wantsToOpen && currentBlinkElapsed >= MinManualBlinkDuration)
                {
                    FinishBlink();
                    return;
                }
            }
            else
            {
                blinkTimeRemaining -= Time.deltaTime;
                if (blinkTimeRemaining <= 0f)
                {
                    FinishBlink();
                    return;
                }
            }

            return;
        }

        // Cooldown counts down only when eyes are open!
        manualCooldownRemaining = Mathf.Max(0f, manualCooldownRemaining - Time.deltaTime);

        SetFatigue(Fatigue + Time.deltaTime / FatigueTime);

        if (Fatigue >= 1f)
            StartForcedBlink();
    }

    private void TryManualBlink()
    {
        if (IsBlinking || manualCooldownRemaining > 0f)
            return;

        // Cannot blink if fatigue has not reached minimum threshold
        if (Fatigue < MinFatigueToBlink)
            return;

        wantsToOpen = false;
        currentBlinkElapsed = 0f;
        float expectedDuration = Mathf.Max(MinManualBlinkDuration, Fatigue / ManualFatigueRecoveryRate);
        StartBlink(expectedDuration, isManual: true);
    }

    private void StopManualBlink()
    {
        if (!IsBlinking || !isManualBlink)
            return;

        if (currentBlinkElapsed >= MinManualBlinkDuration)
        {
            FinishBlink();
        }
        else
        {
            // Player let go before minimum duration was reached; wait until MinManualBlinkDuration
            wantsToOpen = true;
        }
    }

    private void StartForcedBlink()
    {
        SetFatigue(0f);
        wantsToOpen = false;
        currentBlinkElapsed = 0f;
        StartBlink(ForcedBlinkDuration, isManual: false);
    }

    private void StartBlink(float duration, bool isManual)
    {
        IsBlinking = true;
        isManualBlink = isManual;
        blinkTimeRemaining = isManual ? 0f : duration;

        if (!isManualBlink)
            playerMovement.SetMovementLocked(true);

        BlinkEvents.RaiseClosed(duration);
        EyesClosedChanged?.Invoke(true);
    }

    private void FinishBlink()
    {
        bool wasManual = isManualBlink;
        bool shouldUnlockMovement = !isManualBlink;

        IsBlinking = false;
        isManualBlink = false;
        wantsToOpen = false;
        currentBlinkElapsed = 0f;
        blinkTimeRemaining = 0f;

        if (shouldUnlockMovement)
            playerMovement.SetMovementLocked(false);

        // Cooldown starts AFTER eyes open so player is forced to keep eyes open!
        if (wasManual)
        {
            manualCooldownRemaining = ManualBlinkCooldown;
        }

        BlinkEvents.RaiseOpened();
        EyesClosedChanged?.Invoke(false);
    }

    private void SetFatigue(float value)
    {
        float clampedValue = Mathf.Clamp01(value);
        if (Mathf.Approximately(Fatigue, clampedValue))
            return;

        Fatigue = clampedValue;
        FatigueChanged?.Invoke(Fatigue);
    }
}
