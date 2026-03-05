using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mocha.EntityFrameworkCore.Postgres;

namespace Mocha.Sagas.EfCore;

/// <summary>
/// Configures the EF Core entity mapping for <see cref="SagaState"/> using default Postgres
/// table and column names from <see cref="SagaStateTableInfo"/>.
/// </summary>
internal sealed class SagaStateEntityConfiguration : IEntityTypeConfiguration<SagaState>
{
    // Use default values from SagaStateTableInfo as the source of truth
    private static readonly SagaStateTableInfo Defaults = new();

    /// <summary>
    /// Gets the shared singleton instance of the saga state entity configuration.
    /// </summary>
    public static SagaStateEntityConfiguration Instance { get; } = new();

    /// <summary>
    /// Configures the saga state entity with composite primary key, indexes, concurrency token, and column mappings.
    /// </summary>
    /// <param name="builder">The entity type builder for <see cref="SagaState"/>.</param>
    public void Configure(EntityTypeBuilder<SagaState> builder)
    {
        builder.ToTable(Defaults.Table);

        builder.HasKey(e => new { e.Id, e.SagaName }).HasName(Defaults.IxPrimaryKey);

        builder.HasIndex(x => x.CreatedAt).HasDatabaseName(Defaults.IxCreatedAt);

        builder.Property(x => x.Version).IsConcurrencyToken().HasColumnName(Defaults.Version);
        builder.Property(x => x.Id).HasColumnName(Defaults.Id);
        builder.Property(x => x.SagaName).HasColumnName(Defaults.SagaName);
        builder.Property(x => x.State).HasColumnName(Defaults.State).HasColumnType("json");
        builder.Property(x => x.CreatedAt).HasColumnName(Defaults.CreatedAt);
        builder.Property(x => x.UpdatedAt).HasColumnName(Defaults.UpdatedAt);
    }
}
