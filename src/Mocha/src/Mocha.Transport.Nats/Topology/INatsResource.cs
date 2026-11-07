using NATS.Client.JetStream;

namespace Mocha.Transport.Nats;

/// <summary>
/// Represents a JetStream topology resource that can be provisioned on the server.
/// </summary>
public interface INatsResource
{
    /// <summary>
    /// Gets a value indicating whether this resource is provisioned during topology setup.
    /// When <see langword="null"/>, the transport-level default is used.
    /// </summary>
    bool? AutoProvision { get; }

    /// <summary>
    /// Creates or updates this resource on the JetStream server.
    /// </summary>
    /// <param name="context">The JetStream context used to issue the management request.</param>
    /// <param name="cancellationToken">A token to cancel the provisioning operation.</param>
    ValueTask ProvisionAsync(INatsJSContext context, CancellationToken cancellationToken);
}
