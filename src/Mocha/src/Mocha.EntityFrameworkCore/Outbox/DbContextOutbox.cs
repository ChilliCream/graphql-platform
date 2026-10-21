using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Middlewares;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Prepares messages for persistence in the same transaction as the owning DbContext's next save.
/// </summary>
public sealed class DbContextOutbox : IMessageDispatchTarget
{
    private readonly DbContext _context;
    private readonly DbContextId _lease;
    private readonly DbContextOutboxScope _scope;
    private IDbContextOutboxProvider? _provider;
    private IPreparedOutboxMessage? _prepared;
    private int _preparing;

    internal DbContextOutbox(DbContext context, DbContextOutboxScope scope)
    {
        _context = context;
        _lease = context.ContextId;
        _scope = scope;
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
    /// </summary>
    public async ValueTask PublishAsync<T>(
        T message,
        PublishOptions options,
        CancellationToken cancellationToken)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(message);
        var dispatcher = Begin(cancellationToken);
        try
        {
            await dispatcher.PublishAsync(message, options, this, cancellationToken);
            Complete(cancellationToken);
        }
        finally
        {
            End();
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
    /// </summary>
    public async ValueTask SendAsync<T>(
        T message,
        SendOptions options,
        CancellationToken cancellationToken)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(message);
        var dispatcher = Begin(cancellationToken);
        try
        {
            await dispatcher.SendAsync(message, options, this, cancellationToken);
            Complete(cancellationToken);
        }
        finally
        {
            End();
        }
    }

    internal void Attach(IMessageBus bus)
    {
        EnsureCurrentLease();
        _ = GetProvider();
        _ = GetDispatcher(bus);
        _scope.Attach(bus);
    }

    void IMessageDispatchTarget.Capture(MessageEnvelope envelope)
    {
        EnsureCurrentLease();
        if (!IsPreparing || _prepared is not null)
        {
            throw ThrowHelper.InvalidOutboxDispatch();
        }

        _prepared = GetProvider().Prepare(envelope);
    }

    private IMessageDispatcher Begin(CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref _preparing, 1, 0) != 0)
        {
            throw ThrowHelper.ConcurrentOutboxOperation();
        }

        try
        {
            EnsureCurrentLease();
            cancellationToken.ThrowIfCancellationRequested();
            var dispatcher = GetDispatcher(_scope.GetBus(_context));
            GetProvider().Validate(_context);
            return dispatcher;
        }
        catch
        {
            _preparing = 0;
            throw;
        }
    }

    private void Complete(CancellationToken cancellationToken)
    {
        EnsureCurrentLease();
        cancellationToken.ThrowIfCancellationRequested();
        if (_prepared is not { } prepared)
        {
            throw ThrowHelper.InvalidOutboxDispatch();
        }

        GetProvider().Stage(_context, prepared);
        _prepared = null;
    }

    private void End()
    {
        var prepared = _prepared;
        _prepared = null;
        _preparing = 0;
        prepared?.Dispose();
    }

    private void EnsureCurrentLease()
    {
        _ = _context.GetInfrastructure();
        if (_context.ContextId != _lease)
        {
            throw ThrowHelper.OutboxLeaseExpired();
        }
    }

    private IDbContextOutboxProvider GetProvider()
        => _provider ??= _context.GetInfrastructure().GetService<IDbContextOutboxProvider>()
            ?? throw ThrowHelper.OutboxProviderNotSupported(_context.GetType());

    private static IMessageDispatcher GetDispatcher(IMessageBus bus)
        => bus as IMessageDispatcher ?? throw ThrowHelper.OutboxBusNotSupported(bus.GetType());
}
