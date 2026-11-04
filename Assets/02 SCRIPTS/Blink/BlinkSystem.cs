using System;
using UnityEngine;

[RequireComponent(typeof(PlayerInputReader), typeof(PlayerMovement))]
public sealed class BlinkSystem : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float fatigueTime = 12f;
    [SerializeField, Min(0.01f)] private float manualFatigueRecoveryRate = 1f;
    [SerializeField, Min(0f)] private float manualBlinkCooldown = 1.5f;
    [SerializeField, Min(0.01f)] private float forcedBlinkDuration = 1f;

    public event Action<float> FatigueChanged;
    public event Action<bool> EyesClosedChanged;

    public float Fatigue { get; private set; }
    public bool IsBlinking { get; private set; }

    private PlayerInputReader inputReader;
    private PlayerMovement playerMovement;
    private float blinkTimeRemaining;
    private float manualCooldownRemaining;
    private bool isManualBlink;

    private void Awake()
    {
        inputReader = GetComponent<PlayerInputReader>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void OnEnable()
    {
        inputReader.BlinkPressed += TryManualBlink;
        inputReader.BlinkReleased += StopManualBlink;
        FatigueChanged?.Invoke(Fatigue);
        EyesClosedChanged?.Invoke(false);
    }

    private void OnDisable()
    {
        inputReader.BlinkPressed -= TryManualBlink;
        inputReader.BlinkReleased -= StopManualBlink;

        if (IsBlinking)
            FinishBlink();
    }

    private void Update()
    {
        manualCooldownRemaining = Mathf.Max(0f, manualCooldownRemaining - Time.deltaTime);

        if (IsBlinking)
        {
            if (isManualBlink)
            {
                // Space controls how long the eyes stay closed; reaching zero fatigue does not open them.
                SetFatigue(Fatigue - manualFatigueRecoveryRate * Time.deltaTime);
            }
            else
            {
                blinkTimeRemaining -= Time.deltaTime;
                if (blinkTimeRemaining <= 0f)
                    FinishBlink();
            }

            return;
        }

        SetFatigue(Fatigue + Time.deltaTime / fatigueTime);

        if (Fatigue >= 1f)
            StartForcedBlink();
    }

    private void TryManualBlink()
    {
        if (IsBlinking || manualCooldownRemaining > 0f || Fatigue <= 0f)
            return;

        float expectedDuration = Fatigue / manualFatigueRecoveryRate;
        StartBlink(expectedDuration, isManual: true);
        manualCooldownRemaining = manualBlinkCooldown;
    }

    private void StopManualBlink()
    {
        if (IsBlinking && isManualBlink)
            FinishBlink();
    }

    private void StartForcedBlink()
    {
        SetFatigue(0f);
        StartBlink(forcedBlinkDuration, isManual: false);
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
        bool shouldUnlockMovement = !isManualBlink;
        IsBlinking = false;
        isManualBlink = false;
        blinkTimeRemaining = 0f;

        if (shouldUnlockMovement)
            playerMovement.SetMovementLocked(false);

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
