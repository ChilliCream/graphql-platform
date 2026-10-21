using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Associates one DbContext lease with its caller's messaging services.
/// </summary>
internal sealed class DbContextOutboxScope : IResettableService, IDisposable
{
    private IServiceProvider? _services;
    private IMessageBus? _bus;
    private DbContextOutbox? _outbox;
    private bool _disposed;

    internal bool IsPreparing => _outbox?.IsPreparing ?? false;

    internal DbContextOutbox GetOutbox(DbContext context)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _outbox ??= new DbContextOutbox(context, this);
    }

    internal void Attach(IServiceProvider services)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if ((_services is not null && !ReferenceEquals(_services, services))
            || (_bus is not null && !ReferenceEquals(_bus, services.GetRequiredService<IMessageBus>())))
        {
            throw ThrowHelper.OutboxAlreadyAttached();
        }

        _services = services;
    }

    internal void Attach(IMessageBus bus)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var current = _bus ?? _services?.GetRequiredService<IMessageBus>();
        if (current is not null && !ReferenceEquals(current, bus))
        {
            throw ThrowHelper.OutboxAlreadyAttached();
        }

        _bus = bus;
    }

    internal IMessageBus GetBus(DbContext context)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _bus ??= _services?.GetRequiredService<IMessageBus>()
            ?? throw ThrowHelper.OutboxNotAttached(context.GetType());
    }

    void IResettableService.ResetState() => Reset();

    Task IResettableService.ResetStateAsync(CancellationToken cancellationToken)
    {
        Reset();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _disposed = true;
        Reset();
    }

    private void Reset()
    {
        _services = null;
        _bus = null;
        _outbox = null;
    }
}
