using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Owns the messaging services used by one DbContext lease.
/// </summary>
internal sealed class DbContextOutboxScope(IServiceScopeFactory scopeFactory)
    : IResettableService, IDisposable, IAsyncDisposable
{
    private IServiceScope? _scope;
    private IMessageBus? _bus;
    private bool _disposed;

    public IMessageBus Bus
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_bus is null)
            {
                _scope ??= scopeFactory.CreateScope();
                _bus = _scope.ServiceProvider.GetRequiredService<IMessageBus>();
            }

            return _bus;
        }
    }

    public void Dispose()
    {
        _disposed = true;
        ReleaseScope()?.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        _disposed = true;
        return ReleaseScopeAsync();
    }

    void IResettableService.ResetState() => ReleaseScope()?.Dispose();

    Task IResettableService.ResetStateAsync(CancellationToken cancellationToken)
        => ReleaseScopeAsync().AsTask();

    private IServiceScope? ReleaseScope()
    {
        var scope = _scope;
        _scope = null;
        _bus = null;
        return scope;
    }

    private ValueTask ReleaseScopeAsync()
    {
        var scope = ReleaseScope();

        if (scope is IAsyncDisposable asyncDisposable)
        {
            return asyncDisposable.DisposeAsync();
        }

        scope?.Dispose();
        return default;
    }
}
