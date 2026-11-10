namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Identifies what a publish is waiting for.
/// </summary>
internal enum PublishPhase
{
    /// <summary>
    /// The consumer of the reply queue that the request names as its response address.
    /// </summary>
    ReplyConsumer,

    /// <summary>
    /// A publisher channel.
    /// </summary>
    ChannelCapacity,

    /// <summary>
    /// The declaration of the destination's queues, exchanges and bindings.
    /// </summary>
    Topology,

    /// <summary>
    /// The broker's confirmation of the published message.
    /// </summary>
    PublisherConfirm,

    /// <summary>
    /// The delay before the publish is attempted again after a connection loss.
    /// </summary>
    RecoveryBackoff
}
