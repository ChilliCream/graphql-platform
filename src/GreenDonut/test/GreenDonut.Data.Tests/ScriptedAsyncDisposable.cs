namespace GreenDonut.Data;

/// <summary>
/// An <see cref="IAsyncDisposable"/> for unit tests that counts how often it was disposed, for
/// asserting the release of a lifetime object passed to the paging APIs.
/// </summary>
public sealed class ScriptedAsyncDisposable : IAsyncDisposable
{
    private Exception? _disposeException;

    /// <summary>
    /// The number of times this instance was disposed.
    /// </summary>
    public int DisposeCount { get; private set; }

    /// <summary>
    /// Makes <c>DisposeAsync</c> throw <paramref name="exception"/> after still counting the
    /// disposal.
    /// </summary>
    /// <param name="exception">
    /// The exception thrown when this instance is disposed.
    /// </param>
    public void ThrowOnDispose(Exception exception) => _disposeException = exception;

    public ValueTask DisposeAsync()
    {
        DisposeCount++;

        if (_disposeException is { } exception)
        {
            throw exception;
        }

        return ValueTask.CompletedTask;
    }
}
