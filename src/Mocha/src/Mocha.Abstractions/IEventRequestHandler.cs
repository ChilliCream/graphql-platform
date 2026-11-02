namespace Mocha;

/// <summary>
/// Interface for request handlers that expect a response.
/// </summary>
public interface IEventRequestHandler<in TRequest, TResponse> : IEventRequestHandler
    where TRequest : IEventRequest<TResponse>
{
    /// <summary>
    /// Handles the incoming request and produces a response.
    /// </summary>
    /// <param name="request">The request event to handle.</param>
    /// <param name="cancellationToken">A token to cancel the handling operation.</param>
    /// <returns>A value task containing the response event.</returns>
    ValueTask<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);

    static Type IHandler.RequestType => typeof(TRequest);

    static Type IHandler.ResponseType => typeof(TResponse);
}

/// <summary>
/// Interface for request handlers that process a request without producing a typed response.
/// </summary>
/// <typeparam name="TRequest">The type of request event this handler processes.</typeparam>
public interface IEventRequestHandler<in TRequest> : IEventRequestHandler where TRequest : notnull
{
    /// <summary>
    /// Handles the incoming request.
    /// </summary>
    /// <param name="request">The request event to handle.</param>
    /// <param name="cancellationToken">A token to cancel the handling operation.</param>
    /// <returns>A value task that completes when the request has been processed.</returns>
    ValueTask HandleAsync(TRequest request, CancellationToken cancellationToken);

    static Type IHandler.RequestType => typeof(TRequest);

    static Type? IHandler.ResponseType => null;
}

/// <summary>
/// Non-generic base interface for request handlers, providing default handler metadata that
/// indicates no event type is associated.
/// </summary>
public interface IEventRequestHandler : IHandler
{
    static Type? IHandler.EventType => null;
}
