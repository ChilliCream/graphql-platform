using Microsoft.Extensions.Primitives;

namespace HotChocolate.Fusion.Policies.Rego;

/// <summary>
/// A base class for an <see cref="IRegoDataProvider"/> whose source has no native push
/// notification, giving it a change token that fires on a fixed interval.
/// </summary>
public abstract class PollingRegoDataProvider : IRegoDataProvider, IDisposable
{
#if NET9_0_OR_GREATER
    private readonly Lock _sync = new();
#else
    private readonly object _sync = new();
#endif
    private readonly Timer _timer;
    private CancellationTokenSource _tokenSource = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="PollingRegoDataProvider"/>.
    /// </summary>
    /// <param name="pollingInterval">
    /// The interval at which the change token fires, prompting a refresh.
    /// </param>
    protected PollingRegoDataProvider(TimeSpan pollingInterval)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(pollingInterval, TimeSpan.Zero);

        _timer = new Timer(OnTick, null, pollingInterval, pollingInterval);
    }

    /// <inheritdoc />
    public abstract ValueTask<RegoDataSnapshot> GetDataAsync(CancellationToken cancellationToken);

    /// <inheritdoc />
    public IChangeToken GetChangeToken()
    {
        lock (_sync)
        {
            return new CancellationChangeToken(_tokenSource.Token);
        }
    }

    private void OnTick(object? state)
    {
        CancellationTokenSource previous;

        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            previous = _tokenSource;
            _tokenSource = new CancellationTokenSource();
        }

        previous.Cancel();
        previous.Dispose();
    }

    /// <summary>
    /// Stops the polling timer and releases the current change token.
    /// </summary>
    public void Dispose()
    {
        CancellationTokenSource current;

        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            current = _tokenSource;
        }

        _timer.Dispose();
        current.Cancel();
        current.Dispose();
    }
}
