using Microsoft.Extensions.Primitives;

namespace Mocha;

/// <summary>
/// Aggregates change-token signals from the runtime's endpoints and transports into a single
/// bus-scoped change token, and caches the latest <see cref="MessageBusDescription"/> snapshot.
/// </summary>
internal sealed class MessageBusChangeTokenSource : IDisposable
{
    private readonly object _lock = new();
    private readonly IMessagingRuntime _runtime;
    private readonly List<IDisposable> _changeTokenRegistrations = [];
    private readonly ChangeTokenSource _changeTokens = new();
    private MessageBusDescription? _description;
    private bool _disposed;

    public MessageBusChangeTokenSource(IMessagingRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);

        _runtime = runtime;

        _changeTokenRegistrations.Add(ChangeToken.OnChange(_runtime.Endpoints.GetChangeToken, Invalidate));

        foreach (var transport in _runtime.Transports)
        {
            _changeTokenRegistrations.Add(ChangeToken.OnChange(transport.GetChangeToken, Invalidate));
        }
    }

    /// <summary>
    /// Gets the current message bus topology snapshot.
    /// </summary>
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

    /// <summary>
    /// Gets a change token that fires when the topology snapshot may have changed.
    /// </summary>
    public IChangeToken GetChangeToken()
    {
        lock (_lock)
        {
            ThrowIfDisposed();
            return _changeTokens.Current;
        }
    }

    public void Dispose()
    {
        List<IDisposable> registrations;

        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            registrations = [.. _changeTokenRegistrations];
            _changeTokenRegistrations.Clear();
            _description = null;
        }

        try
        {
            foreach (var registration in registrations)
            {
                registration.Dispose();
            }
        }
        finally
        {
            _changeTokens.Dispose();
        }
    }

    private void Invalidate()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _description = null;
            _changeTokens.Rotate();
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
