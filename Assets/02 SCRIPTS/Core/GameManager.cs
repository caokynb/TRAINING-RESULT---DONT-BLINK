using System;
using UnityEngine;

public sealed class GameManager : MonoBehaviour
{
    public enum GameState
    {
        Playing,
        Won,
        Lost
    }

    [SerializeField] private bool waitForCatchSequence;

    public GameState CurrentState { get; private set; } = GameState.Playing;
    public event Action<GameState> StateChanged;

    private bool playerWasCaught;

    private void OnEnable()
    {
        GameEvents.OnPlayerCaught += HandlePlayerCaught;
        GameEvents.OnCatchSequenceDone += HandleCatchSequenceDone;
        GameEvents.OnPlayerReachedExit += HandlePlayerReachedExit;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerCaught -= HandlePlayerCaught;
        GameEvents.OnCatchSequenceDone -= HandleCatchSequenceDone;
        GameEvents.OnPlayerReachedExit -= HandlePlayerReachedExit;
    }

    private void HandlePlayerCaught()
    {
        if (CurrentState != GameState.Playing || playerWasCaught)
            return;

        playerWasCaught = true;
        LockPlayerControls();

        // Use the direct fallback until the monster catch sequence raises this event itself.
        if (!waitForCatchSequence)
            GameEvents.RaiseCatchSequenceDone();
    }

    private void HandleCatchSequenceDone()
    {
        if (CurrentState != GameState.Playing || !playerWasCaught)
            return;

        SetState(GameState.Lost);
    }

    private void HandlePlayerReachedExit()
    {
        if (CurrentState != GameState.Playing || playerWasCaught)
            return;

        LockPlayerControls();
        SetState(GameState.Won);
    }

    private void LockPlayerControls()
    {
        BlinkSystem blinkSystem = FindFirstObjectByType<BlinkSystem>();
        if (blinkSystem != null)
            blinkSystem.enabled = false;

        PlayerMovement movement = FindFirstObjectByType<PlayerMovement>();
        if (movement != null)
            movement.SetMovementLocked(true);

        PlayerLook look = FindFirstObjectByType<PlayerLook>();
        if (look != null)
            look.enabled = false;

        PlayerInputReader inputReader = FindFirstObjectByType<PlayerInputReader>();
        if (inputReader != null)
            inputReader.enabled = false;
    }

    private void SetState(GameState newState)
    {
        if (CurrentState == newState)
            return;

        CurrentState = newState;
        StateChanged?.Invoke(CurrentState);
        Debug.Log($"[GameManager] State changed to {CurrentState}");
    }
}
