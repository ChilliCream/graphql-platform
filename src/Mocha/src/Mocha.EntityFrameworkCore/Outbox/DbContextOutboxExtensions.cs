using Microsoft.EntityFrameworkCore;
#if NET9_0_OR_GREATER
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
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
    /// <param name="context">The context that stages the messages.</param>
    /// <param name="bus">
    /// The message bus that Mocha registers, resolved from the service provider that registers the outbox of
    /// the context. A bus that wraps or replaces it is not supported.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// The context has no outbox, or the bus is not supported or belongs to another service provider.
    /// </exception>
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
        var sink = services.GetService<IDbContextOutboxSink>();

        if (state is null || sink is null)
        {
            throw ThrowHelper.OutboxNotConfigured(context.GetType());
        }

        // The bus belongs to the application of the context when both use the same messaging services singleton.
        if (!ReferenceEquals(
            context.GetMessagingServices(),
            defaultBus.Services.GetService<MessagingDbContextServices>()))
        {
            throw ThrowHelper.OutboxBusFromOtherApplication(context.GetType());
        }

        return new DbContextOutbox(defaultBus, sink, state);
#endif
    }
}
