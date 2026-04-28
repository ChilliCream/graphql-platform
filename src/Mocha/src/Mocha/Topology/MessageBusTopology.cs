using Microsoft.Extensions.Primitives;

namespace Mocha;

/// <summary>
/// Caches the message bus topology snapshot and invalidates it when runtime topology sources change.
/// </summary>
internal sealed class MessageBusTopology : IMessageBusTopology, IDisposable
{
    private readonly object _lock = new();
    private readonly IMessagingRuntime _runtime;
    private readonly List<IDisposable> _changeTokenRegistrations = [];
    private readonly ChangeTokenSource _changeTokens = new();
    private MessageBusDescription? _description;
    private bool _disposed;

    public MessageBusTopology(IMessagingRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);

        _runtime = runtime;

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
            return _changeTokens.Current;
        }
    }

    /// <inheritdoc />
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

        foreach (var registration in registrations)
        {
            registration.Dispose();
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
        }

        _changeTokens.Rotate();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
