namespace Mocha;

/// <summary>
/// Configuration for a topology resource, linking it to its parent <see cref="MessagingTopology"/>.
/// </summary>
public class TopologyConfiguration : MessagingConfiguration
{
    /// <summary>
    /// Gets or sets the messaging topology that owns this resource.
    /// </summary>
    public MessagingTopology? Topology { get; set; }
}
