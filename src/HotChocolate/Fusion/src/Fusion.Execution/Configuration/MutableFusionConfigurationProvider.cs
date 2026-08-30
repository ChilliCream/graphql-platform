using System.Collections.Immutable;

namespace HotChocolate.Fusion.Configuration;

/// <summary>
/// A configuration provider whose current configuration can be replaced programmatically. It replays
/// the current configuration to a new subscriber and notifies all subscribers whenever a new
/// configuration is published.
/// </summary>
/// <remarks>
/// This is the single, schema-generation-scoped delivery channel a policy provider subscribes to for
/// its content: the request executor manager owns one instance per generation and publishes into it
/// whenever a policy-only configuration update is adopted without rebuilding the executor. It also
/// serves as test infrastructure for driving the configuration stream without a package on disk.
/// </remarks>
internal sealed class MutableFusionConfigurationProvider : IFusionConfigurationProvider
{
#if NET9_0_OR_GREATER
    private readonly Lock _sync = new();
#else
    private readonly object _sync = new();
#endif
    private ImmutableArray<ObserverSession> _sessions = [];
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="MutableFusionConfigurationProvider"/>.
    /// </summary>
    /// <param name="configuration">The initial configuration, or <c>null</c> when none is available yet.</param>
    public MutableFusionConfigurationProvider(FusionConfiguration? configuration = null)
    {
        Configuration = configuration;
    }

    /// <inheritdoc />
    public FusionConfiguration? Configuration { get; private set; }

    /// <summary>
    /// Publishes a new configuration and notifies all current subscribers.
    /// </summary>
    /// <param name="configuration">The configuration to publish.</param>
    public void Publish(FusionConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ObjectDisposedException.ThrowIf(_disposed, this);

        ImmutableArray<ObserverSession> sessions;

        lock (_sync)
        {
            Configuration = configuration;
            sessions = _sessions;
        }

        foreach (var session in sessions)
        {
            session.Notify(configuration);
        }
    }

    /// <inheritdoc />
    public IDisposable Subscribe(IObserver<FusionConfiguration> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        ObjectDisposedException.ThrowIf(_disposed, this);

        var session = new ObserverSession(this, observer);

        lock (_sync)
        {
            _sessions = _sessions.Add(session);

            // The replay is delivered while the lock is held so that it is ordered with respect to a
            // concurrent Publish. Delivering it outside the lock would allow a newer configuration to
            // reach the new subscriber before this replayed value, reverting it to stale content.
            if (Configuration is not null)
            {
                observer.OnNext(Configuration);
            }
        }

        return session;
    }

    private void Unsubscribe(ObserverSession session)
    {
        lock (_sync)
        {
            _sessions = _sessions.Remove(session);
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _disposed = true;

        foreach (var session in _sessions)
        {
            session.Complete();
        }

        return ValueTask.CompletedTask;
    }

    private sealed class ObserverSession(
        MutableFusionConfigurationProvider provider,
        IObserver<FusionConfiguration> observer)
        : IDisposable
    {
        public void Notify(FusionConfiguration configuration)
        {
            try
            {
                observer.OnNext(configuration);
            }
            catch (Exception ex)
            {
                observer.OnError(ex);
            }
        }

        public void Complete()
        {
            try
            {
                observer.OnCompleted();
            }
            catch
            {
                // Do not surface exceptions thrown by an observer on completion.
            }
        }

        public void Dispose() => provider.Unsubscribe(this);
    }
}
