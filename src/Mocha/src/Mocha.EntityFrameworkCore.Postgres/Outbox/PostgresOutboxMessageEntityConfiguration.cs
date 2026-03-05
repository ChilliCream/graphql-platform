using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mocha.EntityFrameworkCore.Postgres;

namespace Mocha.Outbox;

/// <summary>
/// Configures the EF Core entity mapping for <see cref="OutboxMessage"/> using default Postgres
/// table and column names from <see cref="OutboxTableInfo"/>.
/// </summary>
internal sealed class PostgresOutboxMessageEntityConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    // Use default values from OutboxTableInfo as the source of truth
    private static readonly OutboxTableInfo Defaults = new();

    /// <summary>
    /// Gets the shared singleton instance of the outbox message entity configuration.
    /// </summary>
    public static PostgresOutboxMessageEntityConfiguration Instance { get; } = new();

    /// <summary>
    /// Configures the outbox message entity with table name, primary key, indexes, and column mappings.
    /// </summary>
    /// <param name="builder">The entity type builder for <see cref="OutboxMessage"/>.</param>
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable(Defaults.Table);

        builder.HasKey(e => e.Id).HasName(Defaults.IxPrimaryKey);

        builder.HasIndex(x => x.CreatedAt).HasDatabaseName(Defaults.IxCreatedAt).IsDescending();

        builder.HasIndex(x => x.TimesSent).HasDatabaseName(Defaults.IxTimesSent);

        builder.Property(x => x.Id).HasColumnName(Defaults.Id);
        builder.Property(x => x.Envelope).HasColumnName(Defaults.Envelope).HasColumnType("json");
        builder.Property(x => x.TimesSent).HasColumnName(Defaults.TimesSent);
        builder.Property(x => x.CreatedAt).HasColumnName(Defaults.CreatedAt);
    }
}
