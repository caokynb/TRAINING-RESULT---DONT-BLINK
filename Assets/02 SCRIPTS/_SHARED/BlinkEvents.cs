using System;

public static class BlinkEvents
{
    public static bool EyesClosed;
    // float - duration
    public static event Action<float> OnEyesClosed;
    public static event Action OnEyesOpened;
    public static void RaiseClosed(float duration)
    {
        EyesClosed = true;
        OnEyesClosed?.Invoke(duration);
    }
    public static void RaiseOpened()
    {
        EyesClosed = false;
        OnEyesOpened?.Invoke();
    }
}