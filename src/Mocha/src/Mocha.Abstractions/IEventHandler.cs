namespace Mocha;

/// <summary>
/// Interface for notification handlers that do not expect a response.
/// </summary>
public interface IEventHandler<in TEvent> : IEventHandler
{
    /// <summary>
    /// Handles an incoming event notification.
    /// </summary>
    /// <param name="message">The event to handle.</param>
    /// <param name="cancellationToken">A token to cancel the handling operation.</param>
    /// <returns>A value task that completes when the event has been processed.</returns>
    ValueTask HandleAsync(TEvent message, CancellationToken cancellationToken);

    static Type IHandler.EventType => typeof(TEvent);
}

/// <summary>
/// Non-generic base interface for event handlers, providing default handler metadata that
/// indicates no request or response types are associated.
/// </summary>
public interface IEventHandler : IHandler
{
    static Type? IHandler.ResponseType => null;

    static Type? IHandler.RequestType => null;
}
