using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Middlewares;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Stages messages for persistence in the same transaction as the owning DbContext's next save.
/// </summary>
public sealed class DbContextOutbox : IResettableService, IDisposable, IDispatchMessageStore
{
    private readonly DbContext _context;
    private IServiceProvider? _services;
    private IMessageBus? _bus;
    private IDbContextOutbox? _outbox;
    private int _preparing;

    internal DbContextOutbox(DbContext context)
    {
        _context = context;
    }

    internal bool IsPreparing => _preparing != 0;

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
        if (Interlocked.CompareExchange(ref _preparing, 1, 0) != 0)
        {
            throw ThrowHelper.ConcurrentOutboxOperation();
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var bus = GetBus();
            if (bus is not DefaultMessageBus defaultBus)
            {
                throw ThrowHelper.OutboxBusNotSupported(bus.GetType());
            }

            var outbox = _outbox ??= GetOutbox();
            var checkpoint = outbox.CreateCheckpoint();
            try
            {
                await defaultBus.PublishAsync(message, options, this, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch
            {
                outbox.Discard(_context, checkpoint);
                throw;
            }
        }
        finally
        {
            _preparing = 0;
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
        if (Interlocked.CompareExchange(ref _preparing, 1, 0) != 0)
        {
            throw ThrowHelper.ConcurrentOutboxOperation();
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var bus = GetBus();
            if (bus is not DefaultMessageBus defaultBus)
            {
                throw ThrowHelper.OutboxBusNotSupported(bus.GetType());
            }

            var outbox = _outbox ??= GetOutbox();
            var checkpoint = outbox.CreateCheckpoint();
            try
            {
                await defaultBus.SendAsync(message, options, this, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch
            {
                outbox.Discard(_context, checkpoint);
                throw;
            }
        }
        finally
        {
            _preparing = 0;
        }
    }

    void IResettableService.ResetState() => Reset();

    Task IResettableService.ResetStateAsync(CancellationToken cancellationToken)
    {
        Reset();
        return Task.CompletedTask;
    }

    void IDisposable.Dispose() => Reset();

    internal void Attach(IServiceProvider services)
    {
        if ((_services is not null && !ReferenceEquals(_services, services))
            || (_bus is not null && !ReferenceEquals(_bus, services.GetRequiredService<IMessageBus>())))
        {
            throw ThrowHelper.OutboxAlreadyAttached();
        }

        _services = services;
    }

    internal void Attach(IMessageBus bus)
    {
        if (bus is not DefaultMessageBus)
        {
            throw ThrowHelper.OutboxBusNotSupported(bus.GetType());
        }

        var current = _bus ?? _services?.GetRequiredService<IMessageBus>();
        if (current is not null && !ReferenceEquals(current, bus))
        {
            throw ThrowHelper.OutboxAlreadyAttached();
        }

        _outbox ??= GetOutbox();
        _bus = bus;
    }

    internal IMessageBus GetBus()
        => _bus ??= _services?.GetRequiredService<IMessageBus>()
            ?? throw ThrowHelper.OutboxNotAttached(_context.GetType());

    ValueTask IDispatchMessageStore.PersistAsync(IDispatchContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();
        _outbox!.Stage(_context, context.Envelope!, context.CancellationToken);

        return default;
    }

    private void Reset()
    {
        _services = null;
        _bus = null;
        _outbox = null;
        _preparing = 0;
    }

    private IDbContextOutbox GetOutbox()
        => _context.GetInfrastructure().GetService<IDbContextOutbox>()
            ?? throw ThrowHelper.OutboxStagingNotSupported(_context.GetType());
}
