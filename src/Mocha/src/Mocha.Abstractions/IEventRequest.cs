namespace Mocha;

/// <summary>
/// Non-generic marker interface for request event messages.
/// </summary>
public interface IEventRequest;

/// <summary>
/// Represents a request event that expects a response of type <typeparamref name="TResponse"/> from its handler.
/// </summary>
/// <typeparam name="TResponse">The event type returned as the response to this request.</typeparam>
public interface IEventRequest<TResponse> : IEventRequest;
