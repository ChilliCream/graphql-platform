using Microsoft.EntityFrameworkCore;

namespace Mocha.Sagas.EfCore;

/// <summary>
/// Provides extension methods on <see cref="ModelBuilder"/> for applying the SQL Server saga state
/// entity configuration to the EF Core model.
/// </summary>
public static class SqlServerSagaPersistenceModelBuilderExtensions
{
    /// <summary>
    /// Applies the <see cref="SagaState"/> entity type configuration to the model,
    /// mapping it to the SQL Server saga states table with default column names, indexes, and concurrency token.
    /// </summary>
    /// <param name="modelBuilder">The EF Core model builder to configure.</param>
    public static void AddSqlServerSagas(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(SqlServerSagaStateEntityConfiguration.Instance);
    }
}
