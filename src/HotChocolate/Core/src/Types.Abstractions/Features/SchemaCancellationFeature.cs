namespace HotChocolate.Features;

/// <summary>
/// Provides schema cancellation support for HotChocolate.
/// </summary>
/// <remarks>
/// This feature attaches a <see cref="CancellationToken"/> to a schema, allowing
/// long-lived operations such as subscriptions to be gracefully canceled when
/// the schema is phased out (for example, during schema version replacement).
/// </remarks>
public sealed class SchemaCancellationFeature : IAsyncDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaCancellationFeature"/> class.
    /// </summary>
    /// <remarks>
    /// The internal <see cref="CancellationTokenSource"/> is created upon construction,
    /// and the <see cref="CancellationToken"/> property is set to its token.
    /// </remarks>
    public SchemaCancellationFeature()
    {
        CancellationToken = _cts.Token;
    }

    /// <summary>
    /// Gets the schema cancellation token.
    /// </summary>
    /// <remarks>
    /// This token will be triggered when the schema is phased out. Components such
    /// as subscriptions and background tasks can observe this token and terminate
    /// gracefully when cancellation is requested.
    /// </remarks>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// Disposes the <see cref="SchemaCancellationFeature"/> and requests cancellation.
    /// </summary>
    /// <remarks>
    /// This method cancels the underlying <see cref="CancellationTokenSource"/>,
    /// ensuring that all operations observing the token are notified. It should be
    /// called when the schema is being phased out.
    /// </remarks>
    /// <returns>A task representing the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await _cts.CancelAsync().ConfigureAwait(false);
            _cts.Dispose();
            _disposed = true;
        }
    }
}
