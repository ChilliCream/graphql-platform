using Microsoft.Extensions.Primitives;

namespace Mocha.Resources;

/// <summary>
/// An <see cref="IChangeToken"/> that never fires.
/// </summary>
internal sealed class NullChangeToken : IChangeToken
{
    public static NullChangeToken Singleton { get; } = new();

    private NullChangeToken() { }

    public bool HasChanged => false;

    public bool ActiveChangeCallbacks => false;

    public IDisposable RegisterChangeCallback(Action<object?> callback, object? state) => EmptyDisposable.Instance;

    private sealed class EmptyDisposable : IDisposable
    {
        public static EmptyDisposable Instance { get; } = new();
        public void Dispose() { }
    }
}
