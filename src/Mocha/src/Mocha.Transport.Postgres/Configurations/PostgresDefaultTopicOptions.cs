namespace Mocha.Transport.Postgres;

/// <summary>
/// Default options for topics created by topology conventions.
/// </summary>
public sealed class PostgresDefaultTopicOptions
{
    /// <summary>
    /// Gets or sets whether topics are auto-provisioned by default.
    /// Default is null (uses the topic default of true).
    /// </summary>
    public bool? AutoProvision { get; set; }

    /// <summary>
    /// Applies these defaults to a topic configuration, without overriding explicitly set values.
    /// </summary>
    internal void ApplyTo(PostgresTopicConfiguration configuration)
    {
        configuration.AutoProvision ??= AutoProvision;
    }
}
