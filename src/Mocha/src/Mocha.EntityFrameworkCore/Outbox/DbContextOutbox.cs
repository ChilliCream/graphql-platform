using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Middlewares;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Prepares messages for persistence in the same transaction as the owning DbContext's next save.
/// The outbox must not be used after the context is disposed or returned to its pool.
/// </summary>
public sealed class DbContextOutbox : IResettableService, IDisposable
{
    private readonly DbContext _context;
    private readonly Action<MessageEnvelope, CancellationToken> _stage;
    private IServiceProvider? _services;
    private IMessageBus? _bus;
    private IDbContextOutboxProvider? _provider;
    private int _preparing;

    internal DbContextOutbox(DbContext context)
    {
        _context = context;
        _stage = Stage;
    }

    internal bool IsPreparing => _preparing != 0;

    /// <summary>
    /// Prepares a publication for the next save using the attached messaging scope.
    /// </summary>
    public ValueTask PublishAsync<T>(T message, CancellationToken cancellationToken)
        where T : notnull
        => PublishAsync(message, PublishOptions.Default, cancellationToken);

    /// <summary>
    /// Prepares a publication with the specified options for the next save.
    /// Scheduled messages are not supported.
    /// </summary>
    public async ValueTask PublishAsync<T>(
        T message,
        PublishOptions options,
        CancellationToken cancellationToken)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(message);
        if (options.ScheduledTime is not null)
        {
            throw ThrowHelper.ScheduledOutboxMessage(nameof(options));
        }

        if (Interlocked.CompareExchange(ref _preparing, 1, 0) != 0)
        {
            throw ThrowHelper.ConcurrentOutboxOperation();
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var bus = GetBus();
            var dispatcher = GetDispatcher(bus);
            var provider = _provider ??= GetProvider();
            provider.Validate(_context);

            await dispatcher.PreparePublishAsync(message, options, _stage, cancellationToken);
        }
        finally
        {
            _preparing = 0;
        }
    }

    /// <summary>
    /// Prepares a directed message for the next save using the attached messaging scope.
    /// </summary>
    public ValueTask SendAsync<T>(T message, CancellationToken cancellationToken)
        where T : notnull
        => SendAsync(message, SendOptions.Default, cancellationToken);

    /// <summary>
    /// Prepares a directed message with the specified options for the next save.
    /// Scheduled messages are not supported.
    /// </summary>
    public async ValueTask SendAsync<T>(
        T message,
        SendOptions options,
        CancellationToken cancellationToken)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(message);
        if (options.ScheduledTime is not null)
        {
            throw ThrowHelper.ScheduledOutboxMessage(nameof(options));
        }

        if (Interlocked.CompareExchange(ref _preparing, 1, 0) != 0)
        {
            throw ThrowHelper.ConcurrentOutboxOperation();
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var bus = GetBus();
            var dispatcher = GetDispatcher(bus);
            var provider = _provider ??= GetProvider();
            provider.Validate(_context);

            await dispatcher.PrepareSendAsync(message, options, _stage, cancellationToken);
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
        if (bus is not IMessageDispatcher)
        {
            throw ThrowHelper.OutboxBusNotSupported(bus.GetType());
        }

        var current = _bus ?? _services?.GetRequiredService<IMessageBus>();
        if (current is not null && !ReferenceEquals(current, bus))
        {
            throw ThrowHelper.OutboxAlreadyAttached();
        }

        _provider ??= GetProvider();
        _bus = bus;
    }

    internal IMessageBus GetBus()
        => _bus ??= _services?.GetRequiredService<IMessageBus>()
            ?? throw ThrowHelper.OutboxNotAttached(_context.GetType());

    private void Stage(MessageEnvelope envelope, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (envelope.ScheduledTime is not null)
        {
            throw ThrowHelper.ScheduledOutboxMessage(nameof(envelope));
        }

        var provider = _provider ??= GetProvider();
        provider.Validate(_context);
        provider.Stage(_context, envelope, cancellationToken);
    }

    private void Reset()
    {
        _services = null;
        _bus = null;
        _provider = null;
        _preparing = 0;
    }

    private IDbContextOutboxProvider GetProvider()
        => _context.GetInfrastructure().GetService<IDbContextOutboxProvider>()
            ?? throw ThrowHelper.OutboxProviderNotSupported(_context.GetType());

    private static IMessageDispatcher GetDispatcher(IMessageBus bus)
        => bus as IMessageDispatcher ?? throw ThrowHelper.OutboxBusNotSupported(bus.GetType());
}
