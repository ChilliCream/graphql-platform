namespace Mocha;

/// <summary>
/// Handler that receives a batch of events for efficient bulk processing.
/// </summary>
/// <typeparam name="TEvent">The type of event in the batch.</typeparam>
public interface IBatchEventHandler<in TEvent> : IBatchEventHandler
{
    /// <summary>
    /// Handles an incoming batch of events.
    /// </summary>
    /// <param name="batch">The batch of events to process.</param>
    /// <param name="cancellationToken">A token to cancel the handling operation.</param>
    /// <returns>A value task that completes when the batch has been processed.</returns>
    ValueTask HandleAsync(IMessageBatch<TEvent> batch, CancellationToken cancellationToken);

    static Type IHandler.EventType => typeof(TEvent);
}

/// <summary>
/// Non-generic base interface for batch event handlers, providing default handler metadata that
/// indicates no request or response types are associated.
/// </summary>
public interface IBatchEventHandler : IHandler
{
    static Type? IHandler.ResponseType => null;

    static Type? IHandler.RequestType => null;
}
