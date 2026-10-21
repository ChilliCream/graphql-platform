namespace Mocha.EntityFrameworkCore.Postgres;

internal static class ThrowHelper
{
    public static InvalidOperationException OutboxRequiresTransaction(Type contextType)
        => new(
            $"The outbox for DbContext '{contextType.FullName}' "
            + "requires a transaction to publish or send a message. "
            + "Begin a transaction on that DbContext, use UseTransaction() for consumers, "
            + "or set RequireTransaction to false in UsePostgresOutbox().");

    public static InvalidOperationException OutboxRequiresAtomicSave(Type contextType)
        => new(
            $"The outbox for DbContext '{contextType.FullName}' requires an atomic save. "
            + "Enable automatic transactions or begin an explicit or ambient transaction before saving.");

    public static InvalidOperationException OutboxDestinationNotRegistered(Type contextType)
        => new(
            $"The DbContext '{contextType.FullName}' targets a database or table that is not served by its outbox worker. "
            + "Configure a separate outbox registration for this destination before publishing.");
}
