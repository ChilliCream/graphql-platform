using Microsoft.EntityFrameworkCore;
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

    public static InvalidOperationException OutboxMessageNotInModel(Type contextType, Exception innerException)
        => new(
            $"The model of the {nameof(DbContext)} '{contextType.FullName}' has no outbox message entity. "
            + $"Call modelBuilder.{nameof(PostgresOutboxPersistenceModelBuilderExtensions.AddPostgresOutbox)}() "
            + "in OnModelCreating.",
            innerException);
}
