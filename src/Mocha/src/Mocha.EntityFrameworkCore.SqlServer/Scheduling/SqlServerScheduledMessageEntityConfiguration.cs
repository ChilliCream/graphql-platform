using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mocha.EntityFrameworkCore.SqlServer;

namespace Mocha.Scheduling;

/// <summary>
/// Configures the EF Core entity mapping for <see cref="ScheduledMessage"/> using default SQL Server
/// table and column names from <see cref="ScheduledMessageTableInfo"/>.
/// </summary>
internal sealed class SqlServerScheduledMessageEntityConfiguration : IEntityTypeConfiguration<ScheduledMessage>
{
    // Use default values from ScheduledMessageTableInfo as the source of truth
    private static readonly ScheduledMessageTableInfo s_defaults = new();

    /// <summary>
    /// Gets the shared singleton instance of the scheduled message entity configuration.
    /// </summary>
    public static SqlServerScheduledMessageEntityConfiguration Instance { get; } = new();

    /// <summary>
    /// Configures the scheduled message entity with table name, primary key, indexes, and column mappings.
    /// </summary>
    /// <param name="builder">The entity type builder for <see cref="ScheduledMessage"/>.</param>
    public void Configure(EntityTypeBuilder<ScheduledMessage> builder)
    {
        builder.ToTable(s_defaults.Table);

        builder.HasKey(e => e.Id).HasName(s_defaults.IxPrimaryKey);

        builder.HasIndex(x => x.ScheduledTime)
            .HasDatabaseName(s_defaults.IxScheduledTime)
            // Only consider messages that are not yet due or have remaining retry attempts.
            .HasFilter($"[{s_defaults.TimesSent}] < [{s_defaults.MaxAttempts}]");

        builder.HasIndex(x => x.TimesSent).HasDatabaseName(s_defaults.IxTimesSent);

        builder.Property(x => x.Id).HasColumnName(s_defaults.Id);
        builder.Property(x => x.Envelope).HasColumnName(s_defaults.Envelope).HasColumnType("nvarchar(max)")
            .HasConversion(
                v => v.RootElement.GetRawText(),
                v => JsonDocument.Parse(v, default));
        builder.Property(x => x.ScheduledTime).HasColumnName(s_defaults.ScheduledTime);
        builder.Property(x => x.TimesSent).HasColumnName(s_defaults.TimesSent);
        builder.Property(x => x.MaxAttempts).HasColumnName(s_defaults.MaxAttempts);
        builder.Property(x => x.LastError).HasColumnName(s_defaults.LastError).HasColumnType("nvarchar(max)")
            .HasConversion(
                v => v!.RootElement.GetRawText(),
                v => JsonDocument.Parse(v, default));
        builder.Property(x => x.CreatedAt).HasColumnName(s_defaults.CreatedAt);
    }
}
