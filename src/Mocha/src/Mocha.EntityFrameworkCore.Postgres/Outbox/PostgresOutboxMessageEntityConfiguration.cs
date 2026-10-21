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
    private static readonly OutboxTableInfo s_defaults = new();

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
        builder.ToTable(s_defaults.Table);

        builder.HasKey(e => e.Id).HasName(s_defaults.IxPrimaryKey);

        builder.HasIndex(x => x.CreatedAt).HasDatabaseName(s_defaults.IxCreatedAt).IsDescending();

        builder.HasIndex(x => x.TimesSent).HasDatabaseName(s_defaults.IxTimesSent);
        builder.HasIndex(x => new { x.GroupId, x.Sequence }).IsUnique().HasDatabaseName(s_defaults.IxGroupSequence);

        builder.Property(x => x.Id).HasColumnName(s_defaults.Id);
        builder.Property(x => x.Envelope).HasColumnName(s_defaults.Envelope).HasColumnType("json");
        builder.Property(x => x.TimesSent).HasColumnName(s_defaults.TimesSent);
        builder.Property(x => x.CreatedAt).HasColumnName(s_defaults.CreatedAt);
        builder.Property(x => x.GroupId).HasColumnName(s_defaults.GroupId);
        builder.Property(x => x.Sequence).HasColumnName(s_defaults.Sequence).HasDefaultValue(0L);
        builder.Property(x => x.ScheduledAt).HasColumnName(s_defaults.ScheduledAt);
    }
}
