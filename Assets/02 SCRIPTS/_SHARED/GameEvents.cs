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

    // Chìa khóa & Cửa (K22 / A20 / K25 / T20)
    public static event Action OnKeyPickedUp;
    public static event Action OnDoorOpened;
    public static event Action OnDoorLocked;

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

    public static void RaiseKeyPickedUp()
    {
        OnKeyPickedUp?.Invoke();
    }
    public static void RaiseDoorOpened()
    {
        OnDoorOpened?.Invoke();
    }
    public static void RaiseDoorLocked()
    {
        OnDoorLocked?.Invoke();
    }
}
