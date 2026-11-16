using Mocha.Outbox;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Stages messages for persistence in the same transaction as the owning DbContext's next save.
/// </summary>
public sealed class DbContextOutbox
{
    private readonly DefaultMessageBus _bus;
    private readonly IMessageOutbox _outbox;
    private readonly DbContextOutboxState _state;
    private readonly int _version;

    internal DbContextOutbox(DefaultMessageBus bus, IMessageOutbox outbox, DbContextOutboxState state)
    {
        _bus = bus;
        _outbox = outbox;
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

        await _bus.PublishAsync(message, options, _outbox, cancellationToken);
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

        await _bus.SendAsync(message, options, _outbox, cancellationToken);
    }

    private void EnsureCanStage(object message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (_version != _state.Version)
        {
            throw ThrowHelper.OutboxContextDisposed();
        }

        cancellationToken.ThrowIfCancellationRequested();
    }
}
