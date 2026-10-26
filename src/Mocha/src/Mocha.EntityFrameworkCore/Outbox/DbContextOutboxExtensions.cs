using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Provides access to the <see cref="DbContextOutbox"/> of a <see cref="DbContext"/>.
/// </summary>
public static class DbContextOutboxExtensions
{
    /// <summary>
    /// Creates an outbox for this context using the specified message bus.
    /// The outbox is valid for the current lifetime of the context.
    /// </summary>
    public static DbContextOutbox Outbox(this DbContext context, IMessageBus bus)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(bus);

        if (bus is not DefaultMessageBus defaultBus)
        {
            throw ThrowHelper.OutboxBusNotSupported(bus.GetType());
        }

        var services = context.GetInfrastructure();
        var state = services.GetService<DbContextOutboxState>()
            ?? throw ThrowHelper.OutboxNotConfigured(context.GetType());
        var outbox = services.GetService<IDbContextOutbox>()
            ?? throw ThrowHelper.OutboxStagingNotSupported(context.GetType());

        return new DbContextOutbox(context, defaultBus, outbox, state);
    }
}
