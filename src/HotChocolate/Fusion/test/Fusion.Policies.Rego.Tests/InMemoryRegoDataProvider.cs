using System.Text;
using Microsoft.Extensions.Primitives;

namespace HotChocolate.Fusion.Policies.Rego;

/// <summary>
/// An in-memory <see cref="IRegoDataProvider"/> test double whose snapshot can be replaced by a
/// test at will, and whose <see cref="GetDataAsync"/> behavior can be overridden to simulate a
/// slow, throwing, or otherwise misbehaving source.
/// </summary>
internal sealed class InMemoryRegoDataProvider : IRegoDataProvider
{
#if NET9_0_OR_GREATER
    private readonly Lock _sync = new();
#else
    private readonly object _sync = new();
#endif
    private RegoDataSnapshot _snapshot;
    private CancellationTokenSource _tokenSource = new();
    private int _callCount;

    public InMemoryRegoDataProvider(string json, string version = "v1")
    {
        _snapshot = new RegoDataSnapshot(Encoding.UTF8.GetBytes(json), version);
    }

    /// <summary>
    /// Overrides the default behavior of returning the current snapshot. Set to simulate a
    /// throwing or a slow (cancellation-observing) provider.
    /// </summary>
    public Func<CancellationToken, ValueTask<RegoDataSnapshot>>? Handler { get; set; }

    /// <summary>
    /// Gets the number of times <see cref="GetDataAsync"/> has been called.
    /// </summary>
    public int CallCount => Volatile.Read(ref _callCount);

    /// <summary>
    /// Replaces the current snapshot and fires the change token.
    /// </summary>
    public void Publish(string json, string version)
    {
        var snapshot = new RegoDataSnapshot(Encoding.UTF8.GetBytes(json), version);
        CancellationTokenSource previous;

        lock (_sync)
        {
            _snapshot = snapshot;
            previous = _tokenSource;
            _tokenSource = new CancellationTokenSource();
        }

        previous.Cancel();
        previous.Dispose();
    }

    public ValueTask<RegoDataSnapshot> GetDataAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _callCount);

        if (Handler is { } handler)
        {
            return handler(cancellationToken);
        }

        lock (_sync)
        {
            return ValueTask.FromResult(_snapshot);
        }
    }

    public IChangeToken GetChangeToken()
    {
        lock (_sync)
        {
            return new CancellationChangeToken(_tokenSource.Token);
        }
    }
}
