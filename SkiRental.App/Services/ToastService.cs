namespace SkiRental.App.Services;

public enum ToastLevel { Info, Success, Warning, Error }

public sealed record ToastMessage(string Text, ToastLevel Level, DateTime CreatedAt);

/// <summary>Scoped toast bus for Blazor UI feedback.</summary>
public sealed class ToastService
{
    public event Action? Changed;

    private readonly List<ToastMessage> _messages = [];

    public IReadOnlyList<ToastMessage> Messages => _messages;

    public void Show(string text, ToastLevel level = ToastLevel.Info)
    {
        _messages.Add(new ToastMessage(text, level, DateTime.UtcNow));
        if (_messages.Count > 5) _messages.RemoveAt(0);
        Changed?.Invoke();
    }

    public void Success(string text) => Show(text, ToastLevel.Success);
    public void Error(string text) => Show(text, ToastLevel.Error);
    public void Warning(string text) => Show(text, ToastLevel.Warning);

    public void Dismiss(ToastMessage msg)
    {
        _messages.Remove(msg);
        Changed?.Invoke();
    }
}
