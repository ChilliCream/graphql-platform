namespace Mocha;

/// <summary>
/// Describes the messaging patterns supported by a transport.
/// </summary>
/// <remarks>
/// Capabilities are a transport-authored contract and are not user-configurable.
/// </remarks>
[Flags]
public enum MessagingTransportCapabilities
{
    /// <summary>
    /// The transport supports no messaging patterns.
    /// </summary>
    None = 0,

    /// <summary>
    /// The transport supports point-to-point send semantics.
    /// </summary>
    Send = 1 << 0,

    /// <summary>
    /// The transport supports publish-subscribe fan-out semantics.
    /// </summary>
    PublishSubscribe = 1 << 1,

    /// <summary>
    /// The transport supports request/reply correlation with reply endpoints.
    /// </summary>
    RequestReply = 1 << 2,

    /// <summary>
    /// The transport supports delayed or scheduled delivery.
    /// </summary>
    ScheduledDelivery = 1 << 3,

    /// <summary>
    /// The transport supports all messaging patterns.
    /// </summary>
    All = Send | PublishSubscribe | RequestReply | ScheduledDelivery
}
