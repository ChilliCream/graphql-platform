using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mocha.EntityFrameworkCore.SqlServer;

namespace Mocha.Sagas.EfCore;

/// <summary>
/// Configures the EF Core entity mapping for <see cref="SagaState"/> using default SQL Server
/// table and column names from <see cref="SagaStateTableInfo"/>.
/// </summary>
internal sealed class SqlServerSagaStateEntityConfiguration : IEntityTypeConfiguration<SagaState>
{
    // Use default values from SagaStateTableInfo as the source of truth
    private static readonly SagaStateTableInfo s_defaults = new();

    /// <summary>
    /// Gets the shared singleton instance of the saga state entity configuration.
    /// </summary>
    public static SqlServerSagaStateEntityConfiguration Instance { get; } = new();

    /// <summary>
    /// Configures the saga state entity with composite primary key, indexes, concurrency token, and column mappings.
    /// </summary>
    /// <param name="builder">The entity type builder for <see cref="SagaState"/>.</param>
    public void Configure(EntityTypeBuilder<SagaState> builder)
    {
        builder.ToTable(s_defaults.Table);

        builder.HasKey(e => new { e.Id, e.SagaName }).HasName(s_defaults.IxPrimaryKey);

        builder.HasIndex(x => x.CreatedAt).HasDatabaseName(s_defaults.IxCreatedAt);

        builder.Property(x => x.Version).IsConcurrencyToken().HasColumnName(s_defaults.Version);
        builder.Property(x => x.Id).HasColumnName(s_defaults.Id);
        builder.Property(x => x.SagaName).HasColumnName(s_defaults.SagaName).HasMaxLength(450);
        builder.Property(x => x.State).HasColumnName(s_defaults.State).HasColumnType("nvarchar(max)")
            .HasConversion(
                v => v.RootElement.GetRawText(),
                v => JsonDocument.Parse(v, default));
        builder.Property(x => x.CreatedAt).HasColumnName(s_defaults.CreatedAt);
        builder.Property(x => x.UpdatedAt).HasColumnName(s_defaults.UpdatedAt);
    }
}
