namespace CallbackViewer.Services;

public class CallbackDataService
{
    private readonly List<string> _callbacks = new();
    private const int MaxCallbacks = 10;
    public event Action? OnChange;

    public IReadOnlyList<string> Callbacks => _callbacks.AsReadOnly();

    public void AddCallback(string json)
    {
        _callbacks.Insert(0, json);

        if (_callbacks.Count > MaxCallbacks)
            _callbacks.RemoveAt(_callbacks.Count - 1);

        NotifyStateChanged();
    }

    public void ClearCallbacks()
    {
        _callbacks.Clear();
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}