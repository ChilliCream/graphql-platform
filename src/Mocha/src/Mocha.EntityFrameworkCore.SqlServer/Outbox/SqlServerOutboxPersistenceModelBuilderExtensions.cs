using Microsoft.EntityFrameworkCore;

namespace Mocha.Outbox;

/// <summary>
/// Provides extension methods on <see cref="ModelBuilder"/> for applying the SQL Server outbox
/// entity configuration to the EF Core model.
/// </summary>
public static class SqlServerOutboxPersistenceModelBuilderExtensions
{
    /// <summary>
    /// Applies the <see cref="OutboxMessage"/> entity type configuration to the model,
    /// mapping it to the SQL Server outbox table with default column names and indexes.
    /// </summary>
    /// <param name="modelBuilder">The EF Core model builder to configure.</param>
    public static void AddSqlServerOutbox(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(SqlServerOutboxMessageEntityConfiguration.Instance);
    }
}
