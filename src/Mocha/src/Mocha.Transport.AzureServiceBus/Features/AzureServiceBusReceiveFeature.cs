using Azure.Messaging.ServiceBus;
using Mocha.Features;

namespace Mocha.Transport.AzureServiceBus.Features;

/// <summary>
/// Pooled feature that carries the Azure Service Bus <see cref="ProcessMessageEventArgs"/> through the
/// receive middleware pipeline, enabling acknowledgement and message parsing middleware to access
/// the raw delivery context.
/// </summary>
/// <remarks>
/// Also exposes the <see cref="IAzureServiceBusMessageContext"/> facade so handler code can drive
/// native broker primitives (dead-lettering, abandon with property modifications) without taking a
/// direct dependency on the Service Bus SDK's <see cref="ProcessMessageEventArgs"/>.
/// </remarks>
public sealed class AzureServiceBusReceiveFeature : IPooledFeature, IAzureServiceBusMessageContext
{
    /// <summary>
    /// Gets or sets the processor event args containing the received message and settlement methods.
    /// </summary>
    public ProcessMessageEventArgs ProcessMessageEventArgs { get; set; } = null!;

    /// <inheritdoc />
    public ServiceBusReceivedMessage Message => ProcessMessageEventArgs.Message;

    /// <inheritdoc />
    public string EntityPath => ProcessMessageEventArgs.EntityPath;

    /// <inheritdoc />
    public int DeliveryCount => (int)Message.DeliveryCount;

    /// <inheritdoc />
    public DateTimeOffset LockedUntil => Message.LockedUntil;

    /// <inheritdoc />
    public Task DeadLetterAsync(
        string reason,
        string? description = null,
        IDictionary<string, object>? properties = null,
        CancellationToken cancellationToken = default)
        => ProcessMessageEventArgs.DeadLetterMessageAsync(
            Message,
            properties,
            reason,
            description,
            cancellationToken);

    /// <inheritdoc />
    public Task AbandonAsync(
        IDictionary<string, object>? propertiesToModify = null,
        CancellationToken cancellationToken = default)
        => ProcessMessageEventArgs.AbandonMessageAsync(
            Message,
            propertiesToModify,
            cancellationToken);

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
