using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Provides access to the <see cref="DbContextOutbox"/> of a <see cref="DbContext"/>.
/// </summary>
public static class DbContextOutboxExtensions
{
    extension(DbContext context)
    {
        /// <summary>
        /// Gets the outbox whose messages are written by the next save of this DbContext.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The DbContext is not registered with an outbox through <c>AddEntityFramework</c>.
        /// </exception>
        public DbContextOutbox Outbox
            => context.GetInfrastructure().GetService<DbContextOutbox>()
                ?? throw new InvalidOperationException(
                    $"The DbContext '{context.GetType().FullName}' has no outbox. "
                    + $"Register an outbox for it with AddEntityFramework<{context.GetType().Name}>(), "
                    + "for example by calling UsePostgresOutbox().");
    }
}
