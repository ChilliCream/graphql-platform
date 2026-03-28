using System.Collections.Immutable;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace Mocha.Transport.NATS;

/// <summary>
/// Represents a NATS JetStream stream topology resource.
/// </summary>
public sealed class NatsStream : TopologyResource<NatsStreamConfiguration>, INatsResource
{
    /// <summary>
    /// Gets the name of this stream.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Gets the subjects captured by this stream.
    /// </summary>
    public ImmutableArray<string> Subjects { get; private set; } = [];

    /// <summary>
    /// Gets a value indicating whether this stream is automatically provisioned during topology setup.
    /// When <c>null</c>, the transport-level default is used.
    /// </summary>
    public bool? AutoProvision { get; private set; }

    /// <summary>
    /// Gets the maximum number of messages in this stream.
    /// </summary>
    public long? MaxMsgs { get; private set; }

    /// <summary>
    /// Gets the maximum total size in bytes of this stream.
    /// </summary>
    public long? MaxBytes { get; private set; }

    /// <summary>
    /// Gets the maximum age of messages in this stream.
    /// </summary>
    public TimeSpan? MaxAge { get; private set; }

    /// <summary>
    /// Gets the number of replicas for this stream in a NATS JetStream cluster.
    /// </summary>
    public int? Replicas { get; private set; }

    protected override void OnInitialize(NatsStreamConfiguration configuration)
    {
        Name = configuration.Name;
        Subjects = [.. configuration.Subjects];
        AutoProvision = configuration.AutoProvision;
        MaxMsgs = configuration.MaxMsgs;
        MaxBytes = configuration.MaxBytes;
        MaxAge = configuration.MaxAge;
        Replicas = configuration.Replicas;
    }

    protected override void OnComplete(NatsStreamConfiguration configuration)
    {
        var address = new UriBuilder(Topology.Address);
        address.Path = Path.Combine(address.Path, "s", configuration.Name);
        Address = address.Uri;
    }

    /// <summary>
    /// Creates or updates this stream on the NATS server.
    /// </summary>
    /// <param name="js">The JetStream context to use.</param>
    /// <param name="cancellationToken">A token to cancel the provisioning operation.</param>
    public async Task ProvisionAsync(INatsJSContext js, CancellationToken cancellationToken)
    {
        var config = new StreamConfig(Name, Subjects.AsSpan().ToArray());

        if (MaxMsgs.HasValue)
        {
            config.MaxMsgs = MaxMsgs.Value;
        }

        if (MaxBytes.HasValue)
        {
            config.MaxBytes = MaxBytes.Value;
        }

        if (MaxAge.HasValue)
        {
            config.MaxAge = MaxAge.Value;
        }

        if (Replicas.HasValue)
        {
            config.NumReplicas = Replicas.Value;
        }

        await js.CreateOrUpdateStreamAsync(config, cancellationToken);
    }
}
