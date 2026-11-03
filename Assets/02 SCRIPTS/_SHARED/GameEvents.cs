using System;

// EVENT CHUNG CỦA GAME
public static class GameEvents
{
    // monster gọi
    public static event Action OnPlayerCaught;
    // bắt xong gọi
    public static event Action OnCatchSequenceDone;
    // vào exit gọi
    public static event Action OnPlayerReachedExit;
    public static void RaisePlayerCaught()
    {
        OnPlayerCaught?.Invoke();
    }
    public static void RaiseCatchSequenceDone()
    {
        OnCatchSequenceDone?.Invoke();
    }
    public static void RaiseReachedExit()
    {
        OnPlayerReachedExit?.Invoke();
    }
}
