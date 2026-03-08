namespace Mocha.Transport.InMemory;

/// <summary>
/// Holds configuration values for the in-memory messaging transport.
/// </summary>
/// <remarks>
/// By default the transport uses the name and URI schema "memory". Override these
/// values when multiple in-memory transports coexist in the same host.
/// </remarks>
public class InMemoryTransportConfiguration : MessagingTransportConfiguration
{
    /// <summary>
    /// The default transport name used when no explicit name is provided.
    /// </summary>
    public const string DefaultName = "memory";

    /// <summary>
    /// The default URI schema used to address in-memory endpoints.
    /// </summary>
    public const string DefaultSchema = "memory";

    /// <summary>
    /// Creates a new configuration initialized with <see cref="DefaultName"/> and <see cref="DefaultSchema"/>.
    /// </summary>
    public InMemoryTransportConfiguration()
    {
        Name = DefaultName;
        Schema = DefaultSchema;
    }

    public List<InMemoryTopicConfiguration> Topics { get; set; } = [];

    public List<InMemoryQueueConfiguration> Queues { get; set; } = [];

    public List<InMemoryBindingConfiguration> Bindings { get; set; } = [];
}
