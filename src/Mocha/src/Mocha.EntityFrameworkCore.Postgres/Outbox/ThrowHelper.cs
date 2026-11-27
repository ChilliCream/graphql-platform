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
            + $"{nameof(OutboxServiceCollectionExtensions.UsePostgresOutbox)}().");
}
