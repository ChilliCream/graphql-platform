namespace Mocha.EntityFrameworkCore;

internal static class ThrowHelper
{
    public static InvalidOperationException OutboxNotConfigured(Type type)
        => new($"The DbContext '{type.FullName}' has no outbox. "
            + $"Register one with AddEntityFramework<{type.Name}>(x => x.UsePostgresOutbox()) "
            + "and create the context through dependency injection (AddDbContext, AddDbContextFactory, "
            + "AddDbContextPool or AddPooledDbContextFactory); a context constructed by hand is not supported.");

    public static ObjectDisposedException OutboxContextDisposed()
        => new(nameof(DbContextOutbox), "The DbContext owning this outbox has been disposed or returned to its pool.");

    public static InvalidOperationException OutboxBusNotSupported(Type type)
        => new($"The message bus '{type.Name}' does not support preparing messages for a DbContext outbox.");

    public static NotSupportedException OutboxRequiresEntityFrameworkCore9()
        => new("The DbContext outbox requires Entity Framework Core 9 or later. Target .NET 9 or later to use it.");
}
