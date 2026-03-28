namespace Mocha.Transport.NATS;

/// <summary>
/// Represents a NATS subject topology resource.
/// </summary>
public sealed class NatsSubject : TopologyResource<NatsSubjectConfiguration>
{
    /// <summary>
    /// Gets the name of this subject.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Gets the name of the stream that captures this subject.
    /// </summary>
    public string? StreamName { get; private set; }

    protected override void OnInitialize(NatsSubjectConfiguration configuration)
    {
        Name = configuration.Name;
        StreamName = configuration.StreamName;
    }

    protected override void OnComplete(NatsSubjectConfiguration configuration)
    {
        var address = new UriBuilder(Topology.Address);
        address.Path = Path.Combine(address.Path, "s", configuration.Name);
        Address = address.Uri;
    }
}
