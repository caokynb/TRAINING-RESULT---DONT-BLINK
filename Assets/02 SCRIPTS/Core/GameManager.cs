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
        Time.timeScale = 0f;
    }

    private void HandlePlayerReachedExit()
    {
        if (CurrentState != GameState.Playing || playerWasCaught)
            return;

        LockPlayerControls();
        SetState(GameState.Won);
        Time.timeScale = 0f;
    }

    private void LockPlayerControls()
    {
        // 1. Tắt toàn bộ hệ thống chớp mắt & mỏi mắt
        BlinkSystem[] blinkSystems = FindObjectsByType<BlinkSystem>(FindObjectsSortMode.None);
        foreach (var bs in blinkSystems)
        {
            bs.enabled = false;
        }

        // 2. Tắt & ẩn toàn bộ Blink HUD (thanh mỏi mắt vàng, viền vignette đỏ, màn đen)
        BlinkHudView[] blinkHuds = FindObjectsByType<BlinkHudView>(FindObjectsSortMode.None);
        foreach (var hud in blinkHuds)
        {
            hud.enabled = false;
            Canvas hudCanvas = hud.GetComponentInChildren<Canvas>(true);
            if (hudCanvas != null)
                hudCanvas.gameObject.SetActive(false);
        }

        // 3. Vô hiệu hóa toàn bộ quái vật để quái ngừng di chuyển và không jumpscare sau khi win
        MonsterBrain[] monsters = FindObjectsByType<MonsterBrain>(FindObjectsSortMode.None);
        foreach (var m in monsters)
        {
            m.enabled = false;
        }

        // 4. Khóa điều khiển người chơi
        PlayerMovement[] movements = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        foreach (var movement in movements)
        {
            movement.SetMovementLocked(true);
            movement.enabled = false;
        }

        PlayerLook[] looks = FindObjectsByType<PlayerLook>(FindObjectsSortMode.None);
        foreach (var look in looks)
        {
            look.enabled = false;
        }

        PlayerInputReader[] inputReaders = FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None);
        foreach (var inputReader in inputReaders)
        {
            inputReader.enabled = false;
        }

        PlayerInteractor[] interactors = FindObjectsByType<PlayerInteractor>(FindObjectsSortMode.None);
        foreach (var interactor in interactors)
        {
            interactor.enabled = false;
        }
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
