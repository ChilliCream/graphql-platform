using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mocha.EntityFrameworkCore.SqlServer;

namespace Mocha.Inbox;

/// <summary>
/// Configures the EF Core entity mapping for <see cref="InboxMessage"/> using default SQL Server
/// table and column names from <see cref="InboxTableInfo"/>.
/// </summary>
internal sealed class SqlServerInboxMessageEntityConfiguration
    : IEntityTypeConfiguration<InboxMessage>
{
    // Use default values from InboxTableInfo as the source of truth
    private static readonly InboxTableInfo s_defaults = new();

    /// <summary>
    /// Gets the shared singleton instance of the inbox message entity configuration.
    /// </summary>
    public static SqlServerInboxMessageEntityConfiguration Instance { get; } = new();

    /// <summary>
    /// Configures the inbox message entity with table name, primary key, indexes, and column mappings.
    /// </summary>
    /// <param name="builder">The entity type builder for <see cref="InboxMessage"/>.</param>
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable(s_defaults.Table);

        builder.HasKey(e => new { e.MessageId, e.ConsumerType }).HasName(s_defaults.IxPrimaryKey);

        builder.HasIndex(e => e.ProcessedAt)
            .HasDatabaseName(s_defaults.IxProcessedAt);

        builder.Property(e => e.MessageId)
            .HasColumnName(s_defaults.MessageId)
            .HasMaxLength(450)
            .ValueGeneratedNever();

        builder.Property(e => e.ConsumerType)
            .HasColumnName(s_defaults.ConsumerType)
            .HasMaxLength(450)
            .ValueGeneratedNever();

        builder.Property(e => e.MessageType)
            .HasColumnName(s_defaults.MessageType)
            .HasMaxLength(450);

        builder.Property(e => e.ProcessedAt)
            .HasColumnName(s_defaults.ProcessedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");
    }
}
