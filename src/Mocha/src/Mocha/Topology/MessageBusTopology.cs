using Microsoft.Extensions.Primitives;

namespace Mocha;

/// <summary>
/// Caches the message bus topology snapshot and invalidates it when runtime topology sources change.
/// </summary>
public sealed class MessageBusTopology : IMessageBusTopology, IDisposable
{
    private readonly object _lock = new();

    private readonly IMessagingRuntime _runtime;
    private readonly List<IDisposable> _changeTokenRegistrations = [];
    private CancellationTokenSource _changeTokenSource = new();
    private IChangeToken _changeToken;
    private MessageBusDescription? _description;
    private bool _disposed;

    public MessageBusTopology(IMessagingRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);

        _runtime = runtime;
        _changeToken = new CancellationChangeToken(_changeTokenSource.Token);

        _changeTokenRegistrations.Add(ChangeToken.OnChange(_runtime.Endpoints.GetChangeToken, Invalidate));

        foreach (var transport in _runtime.Transports)
        {
            _changeTokenRegistrations.Add(ChangeToken.OnChange(transport.GetChangeToken, Invalidate));
        }
    }

    /// <inheritdoc />
    public MessageBusDescription Description
    {
        get
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                return _description ??= MessageBusDescriptionVisitor.Visit(_runtime);
            }
        }
    }

    /// <inheritdoc />
    public IChangeToken GetChangeToken()
    {
        lock (_lock)
        {
            ThrowIfDisposed();
            return _changeToken;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var registration in _changeTokenRegistrations)
        {
            registration.Dispose();
        }

        _changeTokenRegistrations.Clear();
        _changeTokenSource.Dispose();
        _changeToken = NullChangeToken.Singleton;
        _description = null;
    }

    private void Invalidate()
    {
        CancellationTokenSource? changeTokenSource;

        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _description = null;
            changeTokenSource = CreateChangeTokenUnsynchronized();
        }

        CancelChangeToken(changeTokenSource);
    }

    private CancellationTokenSource CreateChangeTokenUnsynchronized()
    {
        var previous = _changeTokenSource;
        _changeTokenSource = new CancellationTokenSource();
        _changeToken = new CancellationChangeToken(_changeTokenSource.Token);
        return previous;
    }

    private static void CancelChangeToken(CancellationTokenSource changeTokenSource)
    {
        try
        {
            changeTokenSource.Cancel();
        }
        finally
        {
            changeTokenSource.Dispose();
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
