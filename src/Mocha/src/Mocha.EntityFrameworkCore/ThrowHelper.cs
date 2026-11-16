using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mocha.EntityFrameworkCore;

internal static class ThrowHelper
{
    public static InvalidOperationException OutboxNotConfigured(Type type)
        => new($"The {nameof(DbContext)} '{type.FullName}' has no outbox. "
            + $"Register one with {nameof(MessageBusHostBuilderExtensions.AddEntityFramework)}<{type.Name}>"
            + "(x => x.UsePostgresOutbox()) "
            + "and create the context through dependency injection "
            + $"({nameof(EntityFrameworkServiceCollectionExtensions.AddDbContext)}, "
            + $"{nameof(EntityFrameworkServiceCollectionExtensions.AddDbContextFactory)}, "
            + $"{nameof(EntityFrameworkServiceCollectionExtensions.AddDbContextPool)} or "
            + $"{nameof(EntityFrameworkServiceCollectionExtensions.AddPooledDbContextFactory)}), "
            + "or construct it with options resolved from dependency injection; "
            + "options built by hand are not supported.");

    public static ObjectDisposedException OutboxContextDisposed()
        => new(
            nameof(DbContextOutbox),
            $"The {nameof(DbContext)} owning this outbox has been disposed or returned to its pool.");

    public static InvalidOperationException OutboxBusNotSupported(Type type)
        => new(
            $"The message bus '{type.Name}' does not support preparing messages for a "
            + $"{nameof(DbContext)} outbox.");

    public static NotSupportedException OutboxRequiresEntityFrameworkCore9()
        => new($"The {nameof(DbContext)} outbox requires Entity Framework Core 9 or later. "
            + "Target .NET 9 or later to use it.");
}
