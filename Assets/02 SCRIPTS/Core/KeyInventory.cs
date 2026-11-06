using System;
using UnityEngine;

public sealed class KeyInventory : MonoBehaviour
{
    public static KeyInventory Instance { get; private set; }

    [SerializeField] private bool hasKey;

    public bool HasKey => hasKey;

    public event Action<bool> KeyStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        GameEvents.OnKeyPickedUp += HandleKeyPickedUp;
    }

    private void OnDisable()
    {
        GameEvents.OnKeyPickedUp -= HandleKeyPickedUp;
    }

    private void HandleKeyPickedUp()
    {
        hasKey = true;
        KeyStateChanged?.Invoke(true);
    }

    public void SetHasKey(bool state)
    {
        hasKey = state;
        KeyStateChanged?.Invoke(state);
    }

    public void ResetInventory()
    {
        hasKey = false;
        KeyStateChanged?.Invoke(false);
    }
}
