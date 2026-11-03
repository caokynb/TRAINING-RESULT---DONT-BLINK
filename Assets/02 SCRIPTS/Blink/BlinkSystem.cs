using System;
using UnityEngine;

[RequireComponent(typeof(PlayerInputReader), typeof(PlayerMovement))]
public sealed class BlinkSystem : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float fatigueTime = 12f;
    [SerializeField, Min(0.01f)] private float manualBlinkDuration = 0.2f;
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

    private void Awake()
    {
        inputReader = GetComponent<PlayerInputReader>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void OnEnable()
    {
        inputReader.BlinkPressed += TryManualBlink;
        FatigueChanged?.Invoke(Fatigue);
        EyesClosedChanged?.Invoke(false);
    }

    private void OnDisable()
    {
        inputReader.BlinkPressed -= TryManualBlink;

        if (IsBlinking)
            FinishBlink();
    }

    private void Update()
    {
        manualCooldownRemaining = Mathf.Max(0f, manualCooldownRemaining - Time.deltaTime);

        if (IsBlinking)
        {
            blinkTimeRemaining -= Time.deltaTime;
            if (blinkTimeRemaining <= 0f)
                FinishBlink();

            return;
        }

        SetFatigue(Fatigue + Time.deltaTime / fatigueTime);

        if (Fatigue >= 1f)
            StartBlink(forcedBlinkDuration, startsManualCooldown: false);
    }

    private void TryManualBlink()
    {
        if (!IsBlinking && manualCooldownRemaining <= 0f)
            StartBlink(manualBlinkDuration, startsManualCooldown: true);
    }

    private void StartBlink(float duration, bool startsManualCooldown)
    {
        IsBlinking = true;
        blinkTimeRemaining = duration;

        if (startsManualCooldown)
            manualCooldownRemaining = manualBlinkCooldown;

        SetFatigue(0f);
        playerMovement.SetMovementLocked(true);
        BlinkEvents.RaiseClosed(duration);
        EyesClosedChanged?.Invoke(true);
    }

    private void FinishBlink()
    {
        IsBlinking = false;
        blinkTimeRemaining = 0f;
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
