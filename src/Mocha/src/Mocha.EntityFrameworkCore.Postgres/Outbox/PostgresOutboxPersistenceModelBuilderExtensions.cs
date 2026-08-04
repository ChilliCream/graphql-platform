using Microsoft.EntityFrameworkCore;

namespace Mocha.Outbox;

/// <summary>
/// Provides extension methods on <see cref="ModelBuilder"/> for applying the Postgres outbox
/// entity configuration to the EF Core model.
/// </summary>
public static class PostgresOutboxPersistenceModelBuilderExtensions
{
    /// <summary>
    /// Applies the <see cref="OutboxMessage"/> entity type configuration to the model,
    /// mapping it to the Postgres outbox table with default column names and indexes.
    /// </summary>
    /// <param name="modelBuilder">The EF Core model builder to configure.</param>
    public static void AddPostgresOutbox(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(PostgresOutboxMessageEntityConfiguration.Instance);
    }
}
