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
    /// Attaches the caller's messaging scope to this DbContext lease.
    /// </summary>
    public static void UseOutbox(this DbContext context, IMessageBus bus)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(bus);
        context.Outbox.Attach(bus);
    }

    extension(DbContext context)
    {
        /// <summary>
        /// Gets the outbox whose messages are written by the next save of this DbContext.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The DbContext is not registered with an outbox through <c>AddEntityFramework</c>.
        /// </exception>
        public DbContextOutbox Outbox
            => context.GetInfrastructure().GetService<DbContextOutboxScope>()?.GetOutbox(context)
                ?? throw ThrowHelper.OutboxNotConfigured(context.GetType());
    }
}
