using Microsoft.EntityFrameworkCore;
using Mocha.Middlewares;
using Mocha.Outbox;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Stages messages for persistence in the same transaction as the owning DbContext's next save.
/// </summary>
public sealed class DbContextOutbox : IMessageOutbox
{
    private readonly DbContext _context;
    private readonly DefaultMessageBus _bus;
    private readonly IDbContextOutbox _outbox;
    private readonly DbContextOutboxState _state;
    private readonly int _version;
    private readonly Action<IDispatchContext> _configureDispatch;

    internal DbContextOutbox(
        DbContext context,
        DefaultMessageBus bus,
        IDbContextOutbox outbox,
        DbContextOutboxState state)
    {
        _context = context;
        _bus = bus;
        _outbox = outbox;
        _state = state;
        _version = state.Version;
        _configureDispatch = dispatchContext => dispatchContext.Features.Set<IMessageOutbox>(this);
    }

    /// <summary>
    /// Stages a publication for the next save.
    /// </summary>
    public ValueTask PublishAsync<T>(T message, CancellationToken cancellationToken)
        where T : notnull
        => PublishAsync(message, PublishOptions.Default, cancellationToken);

    /// <summary>
    /// Stages a publication with the specified options for the next save.
    /// </summary>
    public async ValueTask PublishAsync<T>(
        T message,
        PublishOptions options,
        CancellationToken cancellationToken)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(message);
        if (_version != _state.Version)
        {
            throw ThrowHelper.OutboxContextDisposed();
        }

        if (Interlocked.CompareExchange(ref _state.Preparing, 1, 0) != 0)
        {
            throw ThrowHelper.ConcurrentOutboxOperation();
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var checkpoint = _outbox.CreateCheckpoint();
            try
            {
                await _bus.PublishAsync(message, options, _configureDispatch, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch
            {
                _outbox.Discard(_context, checkpoint);
                throw;
            }
        }
        finally
        {
            Volatile.Write(ref _state.Preparing, 0);
        }
    }

    /// <summary>
    /// Stages a directed message for the next save.
    /// </summary>
    public ValueTask SendAsync<T>(T message, CancellationToken cancellationToken)
        where T : notnull
        => SendAsync(message, SendOptions.Default, cancellationToken);

    /// <summary>
    /// Stages a directed message with the specified options for the next save.
    /// </summary>
    public async ValueTask SendAsync<T>(
        T message,
        SendOptions options,
        CancellationToken cancellationToken)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(message);
        if (_version != _state.Version)
        {
            throw ThrowHelper.OutboxContextDisposed();
        }

        if (Interlocked.CompareExchange(ref _state.Preparing, 1, 0) != 0)
        {
            throw ThrowHelper.ConcurrentOutboxOperation();
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var checkpoint = _outbox.CreateCheckpoint();
            try
            {
                await _bus.SendAsync(message, options, _configureDispatch, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch
            {
                _outbox.Discard(_context, checkpoint);
                throw;
            }
        }
        finally
        {
            Volatile.Write(ref _state.Preparing, 0);
        }
    }

    ValueTask IMessageOutbox.PersistAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_version != _state.Version)
        {
            throw ThrowHelper.OutboxContextDisposed();
        }

        _outbox.Stage(_context, envelope, cancellationToken);

        return default;
    }
}
