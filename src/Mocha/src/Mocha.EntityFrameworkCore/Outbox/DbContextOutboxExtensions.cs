using Microsoft.EntityFrameworkCore;
#if NET9_0_OR_GREATER
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Outbox;
#endif

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

#if !NET9_0_OR_GREATER
        throw ThrowHelper.OutboxRequiresEntityFrameworkCore9();
#else
        if (bus is not DefaultMessageBus defaultBus)
        {
            throw ThrowHelper.OutboxBusNotSupported(bus.GetType());
        }

        var services = context.GetInfrastructure();
        var state = services.GetService<DbContextOutboxState>();
        var outbox = services.GetService<IMessageOutbox>();

        if (state is null || outbox is null)
        {
            throw ThrowHelper.OutboxNotConfigured(context.GetType());
        }

        // The bus belongs to the application of the context when both resolve the same outbox signal singleton.
        var signal = context.GetMessagingServices().OutboxSignal;
        if (signal is null || !ReferenceEquals(signal, defaultBus.Services.GetService<IOutboxSignal>()))
        {
            throw ThrowHelper.OutboxBusFromOtherApplication(context.GetType());
        }

        return new DbContextOutbox(defaultBus, outbox, state);
#endif
    }
}
