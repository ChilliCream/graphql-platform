using Azure.Messaging.ServiceBus;
using Mocha.Features;

namespace Mocha.Transport.AzureServiceBus.Features;

/// <summary>
/// Pooled feature that carries the Azure Service Bus <see cref="ProcessMessageEventArgs"/> through the
/// receive middleware pipeline, enabling acknowledgement and message parsing middleware to access
/// the raw delivery context.
/// </summary>
public sealed class AzureServiceBusReceiveFeature : IPooledFeature
{
    /// <summary>
    /// Gets or sets the processor event args containing the received message and settlement methods.
    /// </summary>
    public ProcessMessageEventArgs ProcessMessageEventArgs { get; set; } = null!;

    /// <inheritdoc />
    public void Initialize(object state)
    {
        ProcessMessageEventArgs = null!;
    }

    /// <inheritdoc />
    public void Reset()
    {
        ProcessMessageEventArgs = null!;
    }
}
