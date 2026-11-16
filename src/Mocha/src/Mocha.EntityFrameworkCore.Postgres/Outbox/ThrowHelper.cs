using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mocha.EntityFrameworkCore;

namespace Mocha.Outbox;

internal static class ThrowHelper
{
    public static InvalidOperationException OutboxRequiresTransaction(Type contextType)
        => new(
            $"{nameof(IMessageBus)} publishes and sends are stored in the outbox of the first {nameof(DbContext)} "
            + $"registered with {nameof(OutboxServiceCollectionExtensions.UsePostgresOutbox)}(), "
            + $"'{contextType.FullName}', and require a transaction on that {nameof(DbContext)}. "
            + $"Begin a transaction on it, use {nameof(EntityFrameworkCoreBuilderExtensions.UseTransaction)}() "
            + "on it for consumers, "
            + $"or set {nameof(PostgresOutboxOptions.RequireTransaction)} to false in "
            + $"{nameof(OutboxServiceCollectionExtensions.UsePostgresOutbox)}(). "
            + $"To publish in the transaction of another {nameof(DbContext)}, "
            + $"use dbContext.{nameof(DbContextOutboxExtensions.Outbox)}(bus).");

    public static InvalidOperationException OutboxRequiresAtomicSave(Type contextType)
        => new(
            $"The outbox for {nameof(DbContext)} '{contextType.FullName}' requires an atomic save. "
            + "Enable automatic transactions or begin an explicit or ambient transaction before saving.");

    public static InvalidOperationException OutboxDbContextNotRegistered(Type contextType)
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
}
