namespace Mocha.Analyzers;

/// <summary>
/// Represents the kind of MessageBus handler discovered by the source generator.
/// </summary>
public enum MessagingHandlerKind
{
    /// <summary>
    /// An <c>IEventHandler&lt;TEvent&gt;</c> implementation.
    /// </summary>
    Event,

    /// <summary>
    /// An <c>IEventRequestHandler&lt;TRequest, TResponse&gt;</c> implementation.
    /// </summary>
    RequestResponse,

    /// <summary>
    /// An <c>IEventRequestHandler&lt;TRequest&gt;</c> implementation (void return).
    /// </summary>
    Send,

    /// <summary>
    /// An <c>IConsumer&lt;TMessage&gt;</c> implementation.
    /// </summary>
    Consumer,

    /// <summary>
    /// An <c>IBatchEventHandler&lt;TEvent&gt;</c> implementation.
    /// </summary>
    Batch
}
