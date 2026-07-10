namespace Mocha;

/// <summary>
/// Base interface that exposes static abstract metadata about a handler's associated message types.
/// </summary>
/// <remarks>
/// Implementations supply the request, response, and event types at compile time through
/// static abstract members, enabling the message bus infrastructure to resolve handlers
/// without runtime reflection.
/// </remarks>
public interface IHandler
{
    /// <summary>
    /// Gets the request event type this handler processes, or <c>null</c> if not applicable.
    /// </summary>
    static abstract Type? RequestType { get; }

    /// <summary>
    /// Gets the response event type this handler produces, or <c>null</c> if no response is expected.
    /// </summary>
    static abstract Type? ResponseType { get; }

    /// <summary>
    /// Gets the notification event type this handler processes, or <c>null</c> if not applicable.
    /// </summary>
    static abstract Type? EventType { get; }
}
