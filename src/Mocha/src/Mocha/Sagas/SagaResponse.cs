using Mocha;

namespace Mocha.Sagas;

/// <summary>
/// Defines a response message to send when a saga reaches a specific state, typically used in request-reply saga patterns.
/// </summary>
public sealed class SagaResponse(Type eventType, Func<object, object> factory)
{
    /// <summary>
    /// Gets the CLR type of the response event.
    /// </summary>
    public Type EventType => eventType;

    /// <summary>
    /// Gets the factory that creates the response event from the saga state.
    /// </summary>
    public Func<object, object> Factory => factory;
}
