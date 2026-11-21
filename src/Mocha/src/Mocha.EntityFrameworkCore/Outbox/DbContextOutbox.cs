using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Mocha.Middlewares;
using Mocha.Outbox;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Stages messages for persistence in the same transaction as the owning DbContext's next save.
/// A publish or send that throws stages nothing.
/// </summary>
public sealed class DbContextOutbox
{
    private readonly DefaultMessageBus _bus;
    private readonly IDbContextOutboxSink _sink;
    private readonly DbContextOutboxState _state;
    private readonly int _version;

    internal DbContextOutbox(DefaultMessageBus bus, IDbContextOutboxSink sink, DbContextOutboxState state)
    {
        _bus = bus;
        _sink = sink;
        _state = state;
        _version = state.Version;
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
        EnsureCanStage(message, cancellationToken);

        var operation = new StagingOperation(this);

        try
        {
            await _bus.PublishAsync(message, options, operation, cancellationToken);
        }
        catch
        {
            operation.Undo();
            throw;
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
        EnsureCanStage(message, cancellationToken);

        var operation = new StagingOperation(this);

        try
        {
            await _bus.SendAsync(message, options, operation, cancellationToken);
        }
        catch
        {
            operation.Undo();
            throw;
        }
    }

    private bool IsContextCurrent => _version == _state.Version;

    private void EnsureCanStage(object message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        EnsureContextIsCurrent();
        cancellationToken.ThrowIfCancellationRequested();
    }

    private void EnsureContextIsCurrent()
    {
        if (!IsContextCurrent)
        {
            throw ThrowHelper.OutboxContextDisposed();
        }
    }

    /// <summary>
    /// The outbox of a single publish or send. It stages the envelope in the context when the context is
    /// still the one that created the <see cref="DbContextOutbox"/>.
    /// </summary>
    private sealed class StagingOperation(DbContextOutbox outbox) : IMessageOutbox
    {
        private EntityEntry? _entry;

        public ValueTask PersistAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
        {
            outbox.EnsureContextIsCurrent();
            _entry = outbox._sink.Stage(envelope);

            return default;
        }

        /// <summary>
        /// Removes the staged entity from the context, unless a save already wrote it.
        /// </summary>
        public void Undo()
        {
            if (outbox.IsContextCurrent && _entry is { State: EntityState.Added } entry)
            {
                entry.State = EntityState.Detached;
            }
        }
    }
}
