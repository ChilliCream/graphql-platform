namespace Mocha;

/// <summary>
/// Strongly-typed topology configuration that provides access to the specific topology type.
/// </summary>
/// <typeparam name="TTopology">The concrete messaging topology type.</typeparam>
public class TopologyConfiguration<TTopology> : TopologyConfiguration where TTopology : MessagingTopology
{
    public new TTopology? Topology
    {
        get => base.Topology as TTopology;
        set => base.Topology = value;
    }
}
