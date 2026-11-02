using Microsoft.EntityFrameworkCore;

namespace Mocha.Inbox;

/// <summary>
/// Provides extension methods on <see cref="ModelBuilder"/> for applying the Postgres inbox
/// entity configuration to the EF Core model.
/// </summary>
public static class PostgresInboxPersistenceModelBuilderExtensions
{
    /// <summary>
    /// Applies the <see cref="InboxMessage"/> entity type configuration to the model,
    /// mapping it to the Postgres inbox table with default column names and indexes.
    /// </summary>
    /// <param name="modelBuilder">The EF Core model builder to configure.</param>
    public static void AddPostgresInbox(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(PostgresInboxMessageEntityConfiguration.Instance);
    }
}
