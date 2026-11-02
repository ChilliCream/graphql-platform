namespace Mocha.Sagas;

/// <summary>
/// Specifies the kind of message that triggers a saga transition.
/// </summary>
public enum SagaTransitionKind
{
    /// <summary>
    /// The transition is triggered by a published (broadcast) event.
    /// </summary>
    Event,

    /// <summary>
    /// The transition is triggered by a sent (point-to-point) message.
    /// </summary>
    Send,

    /// <summary>
    /// The transition is triggered by an incoming request that expects a reply.
    /// </summary>
    Request,

    /// <summary>
    /// The transition is triggered by a reply to a previously sent request.
    /// </summary>
    Reply
}
