using Microsoft.EntityFrameworkCore;

namespace Mocha.Scheduling;

/// <summary>
/// Provides extension methods on <see cref="ModelBuilder"/> for applying the SQL Server scheduled message
/// entity configuration to the EF Core model.
/// </summary>
public static class SqlServerScheduledMessagePersistenceModelBuilderExtensions
{
    /// <summary>
    /// Applies the <see cref="ScheduledMessage"/> entity type configuration to the model,
    /// mapping it to the SQL Server scheduled messages table with default column names and indexes.
    /// </summary>
    /// <param name="modelBuilder">The EF Core model builder to configure.</param>
    public static void AddSqlServerScheduledMessages(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(SqlServerScheduledMessageEntityConfiguration.Instance);
    }
}
