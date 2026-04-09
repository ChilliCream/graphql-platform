namespace Mocha.Transport.Kafka;

/// <summary>
/// Defines bus-level defaults that are applied to all auto-provisioned topics
/// when they are created by topology conventions.
/// </summary>
public sealed class KafkaBusDefaults
{
    /// <summary>
    /// Gets or sets the default topic configuration that is applied to all auto-provisioned topics.
    /// Individual topic settings will override these defaults.
    /// </summary>
    public KafkaDefaultTopicOptions Topic { get; set; } = new();
}
