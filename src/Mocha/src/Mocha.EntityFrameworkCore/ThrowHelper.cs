using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mocha.EntityFrameworkCore;

internal static class ThrowHelper
{
    public static InvalidOperationException DbContextNotRegistered(Type contextType)
        => new(
            $"The {nameof(DbContext)} '{contextType.FullName}' is not registered as a service. "
            + $"Register it with {nameof(EntityFrameworkServiceCollectionExtensions.AddDbContext)}, "
            + $"{nameof(EntityFrameworkServiceCollectionExtensions.AddDbContextPool)}, "
            + $"{nameof(EntityFrameworkServiceCollectionExtensions.AddDbContextFactory)} or "
            + $"{nameof(EntityFrameworkServiceCollectionExtensions.AddPooledDbContextFactory)}. "
            + $"Before EF Core 10, {nameof(EntityFrameworkServiceCollectionExtensions.AddPooledDbContextFactory)} "
            + "does not register the context itself; also add "
            + $"services.{nameof(ServiceCollectionServiceExtensions.AddScoped)}(sp => "
            + $"sp.{nameof(ServiceProviderServiceExtensions.GetRequiredService)}"
            + $"<{nameof(IDbContextFactory<DbContext>)}<{contextType.Name}>>()"
            + $".{nameof(IDbContextFactory<DbContext>.CreateDbContext)}()).");

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
            $"The message bus '{type.Name}' wraps or replaces the message bus that Mocha registers, and a "
            + $"{nameof(DbContext)} outbox cannot stage messages through it. "
            + "Register behavior for outgoing messages as dispatch middleware instead.");

    public static InvalidOperationException OutboxBusFromOtherApplication(Type type)
        => new(
            "The message bus was not resolved from the service provider that registers the outbox of the "
            + $"{nameof(DbContext)} '{type.FullName}'. Pass the {nameof(IMessageBus)} resolved from that "
            + "service provider.");

    public static NotSupportedException OutboxRequiresEntityFrameworkCore9()
        => new($"The {nameof(DbContext)} outbox requires Entity Framework Core 9 or later. "
            + "Target .NET 9 or later to use it.");
}
