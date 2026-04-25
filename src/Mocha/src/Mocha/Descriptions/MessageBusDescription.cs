namespace Mocha;

/// <summary>
/// Provides a complete diagnostic description of a message bus runtime, including host info, message types, consumers, routes, transports, and sagas.
/// </summary>
/// <remarks>
/// Public for release N to support the deprecated <c>MapMessageBusDeveloperTopology</c> bridge;
/// scheduled for internalization in the next major. New code should consume the resource graph via
/// <c>MochaResourceSource</c> rather than depending on this record tree directly.
/// </remarks>
/// <param name="Host">The host application description.</param>
/// <param name="MessageTypes">All registered message type descriptions.</param>
/// <param name="Consumers">All registered consumer descriptions.</param>
/// <param name="Routes">The inbound and outbound route descriptions.</param>
/// <param name="Transports">All configured transport descriptions.</param>
/// <param name="Sagas">The saga descriptions, or <c>null</c> if no sagas are registered.</param>
public sealed record MessageBusDescription(
    HostDescription Host,
    IReadOnlyList<MessageTypeDescription> MessageTypes,
    IReadOnlyList<ConsumerDescription> Consumers,
    RoutesDescription Routes,
    IReadOnlyList<TransportDescription> Transports,
    IReadOnlyList<SagaDescription>? Sagas);
