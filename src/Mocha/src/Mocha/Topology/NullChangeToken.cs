using Microsoft.Extensions.Primitives;

namespace Mocha;

/// <summary>
/// An <see cref="IChangeToken"/> that never fires.
/// </summary>
/// <remarks>
/// Used by sources whose contents never change. Mirrors ASP.NET Core's internal <c>NullChangeToken</c>.
/// </remarks>
internal sealed class NullChangeToken : IChangeToken
{
    public static NullChangeToken Singleton { get; } = new();

    private NullChangeToken() { }

    public bool HasChanged => false;

    public bool ActiveChangeCallbacks => false;

    public IDisposable RegisterChangeCallback(Action<object?> callback, object? state)
        => EmptyDisposable.Instance;

    private sealed class EmptyDisposable : IDisposable
    {
        public static EmptyDisposable Instance { get; } = new();

        private EmptyDisposable() { }

        public void Dispose() { }
    }
}
