using Microsoft.Extensions.DependencyInjection;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Provides the message bus of the application scope that owns an outbox operation.
/// </summary>
internal sealed class DbContextOutboxScope(IServiceProvider services) : IDisposable
{
    private IServiceProvider? _services = services;
    private IMessageBus? _bus;

    public IMessageBus Bus
    {
        get
        {
            var services = _services;
            ObjectDisposedException.ThrowIf(services is null, this);
            return _bus ??= services.GetRequiredService<IMessageBus>();
        }
    }

    public void Dispose()
    {
        _bus = null;
        _services = null;
    }
}
