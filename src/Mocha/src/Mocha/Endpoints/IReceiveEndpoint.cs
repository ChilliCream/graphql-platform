namespace Mocha;

/// <summary>
/// Represents a receive endpoint that consumes messages from a transport source.
/// </summary>
/// <remarks>
/// Combines the base <see cref="IEndpoint"/> identity with the transport binding and
/// the endpoint kind (default, error, skipped, or reply) so that the runtime can
/// route incoming messages through the correct receive pipeline.
/// </remarks>
public interface IReceiveEndpoint : IEndpoint
{
    /// <summary>
    /// Gets the classification of this receive endpoint.
    /// </summary>
    /// <remarks>
    /// The kind determines how the endpoint participates in the message lifecycle:
    /// <see cref="ReceiveEndpointKind.Error"/> handles faulted messages,
    /// <see cref="ReceiveEndpointKind.Skipped"/> handles unrecognized messages,
    /// and <see cref="ReceiveEndpointKind.Reply"/> handles request-reply responses.
    /// </remarks>
    ReceiveEndpointKind Kind { get; }

    /// <summary>
    /// Gets the messaging transport that this endpoint is bound to.
    /// </summary>
    MessagingTransport Transport { get; }
}
