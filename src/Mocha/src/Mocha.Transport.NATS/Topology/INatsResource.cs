using NATS.Client.JetStream;

namespace Mocha.Transport.NATS;

/// <summary>
/// Represents a NATS topology resource (stream, subject, or consumer) that can be provisioned on the broker.
/// </summary>
public interface INatsResource
{
    /// <summary>
    /// Declares this resource on the broker using the specified JetStream context.
    /// </summary>
    /// <param name="js">The JetStream context to use for provisioning.</param>
    /// <param name="cancellationToken">A token to cancel the provisioning operation.</param>
    Task ProvisionAsync(INatsJSContext js, CancellationToken cancellationToken);
}
