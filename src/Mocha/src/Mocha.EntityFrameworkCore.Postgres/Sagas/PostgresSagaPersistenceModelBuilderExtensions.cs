using Microsoft.EntityFrameworkCore;

namespace Mocha.Sagas.EfCore;

/// <summary>
/// Provides extension methods on <see cref="ModelBuilder"/> for applying the Postgres saga state
/// entity configuration to the EF Core model.
/// </summary>
public static class PostgresSagaPersistenceModelBuilderExtensions
{
    /// <summary>
    /// Applies the <see cref="SagaState"/> entity type configuration to the model,
    /// mapping it to the Postgres saga states table with default column names, indexes, and concurrency token.
    /// </summary>
    /// <param name="modelBuilder">The EF Core model builder to configure.</param>
    public static void AddPostgresSagas(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(SagaStateEntityConfiguration.Instance);
    }
}
