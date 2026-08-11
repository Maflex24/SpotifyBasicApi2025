namespace Frontend;

public sealed class BlindModeService
{
    private const string StoreKey = "albums:blind-mode";
    private readonly LocalStorageService store;
    private bool initialized;

    public BlindModeService(LocalStorageService store)
    {
        this.store = store;
    }

    public bool IsEnabled { get; private set; }

    public event Action? Changed;

    public async Task InitializeAsync()
    {
        if (initialized)
            return;

        IsEnabled = await store.GetItemAsync<bool?>(StoreKey) ?? false;
        initialized = true;
        Changed?.Invoke();
    }

    public async Task SetEnabledAsync(bool enabled)
    {
        await InitializeAsync();

        if (IsEnabled == enabled)
            return;

        IsEnabled = enabled;
        await store.SetItemAsync(StoreKey, enabled);
        Changed?.Invoke();
    }
}
